using FluentValidation;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Texnokaktus.ProgOlymp.ResultService.Services.Grpc.Interceptors;

namespace Texnokaktus.ProgOlymp.ResultService.Tests;

public class RequestValidationInterceptorTests
{
    [Test]
    public async Task InvalidRequest_ThrowsInvalidArgumentWithoutInvokingHandler()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();
        validator.RuleFor(request => request.Id).GreaterThan(0);
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var interceptor = new RequestValidationInterceptor(scope.ServiceProvider);
        var invoked = false;

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await interceptor.UnaryServerHandler(new TestRequest("", 0), Substitute.For<ServerCallContext>(), (_, _) =>
            {
                invoked = true;
                return Task.FromResult(new object());
            }));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(invoked, Is.False);
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Does.Contain("Name").And.Contain("Id"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AsyncValidator_UsesCallScopeAndCancellationToken(bool valid)
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken? receivedToken = null;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).MustAsync(async (_, token) =>
        {
            await Task.Yield();
            receivedToken = token;
            return valid;
        });
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var interceptor = new RequestValidationInterceptor(scope.ServiceProvider);
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(cancellation.Token);
        var request = new TestRequest("test", 1);
        var expected = new object();
        var invoked = false;

        Task<object> InvokeAsync() => interceptor.UnaryServerHandler(request, context, (receivedRequest, receivedContext) =>
        {
            invoked = true;
            Assert.That(receivedRequest, Is.SameAs(request));
            Assert.That(receivedContext, Is.SameAs(context));
            return Task.FromResult(expected);
        });

        if (valid)
            Assert.That(await InvokeAsync(), Is.SameAs(expected));
        else
            await Assert.ThrowsAsync<RpcException>(async () => await InvokeAsync());

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(receivedToken, Is.EqualTo(cancellation.Token));
        Assert.That(invoked, Is.EqualTo(valid));
    }

    [Test]
    public async Task NoValidator_InvokesHandler()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var interceptor = new RequestValidationInterceptor(services);
        var context = Substitute.For<ServerCallContext>();
        var request = new TestRequest("", 0);
        var expected = new object();

        var result = await interceptor.UnaryServerHandler(request, context, (receivedRequest, receivedContext) =>
        {
            Assert.That(receivedRequest, Is.SameAs(request));
            Assert.That(receivedContext, Is.SameAs(context));
            return Task.FromResult(expected);
        });

        Assert.That(result, Is.SameAs(expected));
    }

    [Test]
    public async Task ValidRequest_PreservesHandlerException()
    {
        var validator = new InlineValidator<TestRequest>();
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider();
        var interceptor = new RequestValidationInterceptor(services);
        var expected = new RpcException(new(StatusCode.NotFound, "missing"));

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await interceptor.UnaryServerHandler<TestRequest, object>(new("test", 1), Substitute.For<ServerCallContext>(),
                                                                     (_, _) => throw expected));

        Assert.That(exception, Is.SameAs(expected));
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public async Task ServerStreaming_ValidatesBeforeInvokingHandler(bool hasValidator, bool valid)
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken? receivedToken = null;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).MustAsync(async (_, token) =>
        {
            await Task.Yield();
            receivedToken = token;
            return valid;
        });
        var collection = new ServiceCollection();
        if (hasValidator)
            collection.AddScoped<IValidator<TestRequest>>(_ => validator);
        using var services = collection.BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var interceptor = new RequestValidationInterceptor(scope.ServiceProvider);
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(cancellation.Token);
        var request = new TestRequest("test", 1);
        var responseStream = Substitute.For<IServerStreamWriter<object>>();
        var invoked = false;

        Task InvokeAsync() => interceptor.ServerStreamingServerHandler(request, responseStream, context, (receivedRequest, receivedStream, receivedContext) =>
        {
            invoked = true;
            Assert.That(receivedRequest, Is.SameAs(request));
            Assert.That(receivedStream, Is.SameAs(responseStream));
            Assert.That(receivedContext, Is.SameAs(context));
            return Task.CompletedTask;
        });

        if (hasValidator && !valid)
        {
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await InvokeAsync());
            Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        }
        else
            await InvokeAsync();

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(invoked, Is.EqualTo(!hasValidator || valid));
        Assert.That(receivedToken, Is.EqualTo(hasValidator ? cancellation.Token : (CancellationToken?)null));
    }

    [TestCase(false, 0)]
    [TestCase(false, 1)]
    [TestCase(true, 0)]
    [TestCase(true, 1)]
    public async Task RequestStream_InvalidMessageStopsReadingBeforeDelivery(bool duplex, int invalidIndex)
    {
        List<TestRequest> validated = [];
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).MustAsync(async (request, name, _) =>
        {
            await Task.Yield();
            validated.Add(request);
            return name.Length > 0;
        });
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var interceptor = new RequestValidationInterceptor(scope.ServiceProvider);
        TestRequest[] requests = [new("first", 1), new("second", 2), new("third", 3)];
        requests[invalidIndex] = new("", 0);
        var stream = new TestStreamReader(requests);
        List<TestRequest> received = [];

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await InvokeStreamAsync(interceptor, duplex, stream, Substitute.For<ServerCallContext>(), async reader =>
            {
                Assert.That(stream.ReadCount, Is.Zero);
                while (await reader.MoveNext(CancellationToken.None))
                    received.Add(reader.Current);
                return new object();
            }));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Does.Contain("Name:"));
        Assert.That(received, Is.EqualTo(requests.Take(invalidIndex)));
        Assert.That(validated, Is.EqualTo(requests.Take(invalidIndex + 1)));
        Assert.That(stream.ReadCount, Is.EqualTo(invalidIndex + 1));
    }

    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    [TestCase(true, false)]
    public async Task RequestStream_ValidMessagesOrNoValidatorReachHandler(bool duplex, bool hasValidator)
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();
        var collection = new ServiceCollection();
        if (hasValidator)
            collection.AddScoped<IValidator<TestRequest>>(_ => validator);
        using var services = collection.BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var interceptor = new RequestValidationInterceptor(scope.ServiceProvider);
        TestRequest[] requests = [new(hasValidator ? "first" : "", 1), new(hasValidator ? "second" : "", 2)];
        var stream = new TestStreamReader(requests);
        List<TestRequest> received = [];
        var expected = new object();

        var result = await InvokeStreamAsync(interceptor, duplex, stream, Substitute.For<ServerCallContext>(), async reader =>
        {
            Assert.That(stream.ReadCount, Is.Zero);
            if (!hasValidator)
                Assert.That(reader, Is.SameAs(stream));
            while (await reader.MoveNext(CancellationToken.None))
                received.Add(reader.Current);
            return expected;
        });

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(received, Is.EqualTo(requests));
        Assert.That(result, Is.SameAs(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RequestStream_EmptyStreamDoesNotInvokeValidator(bool duplex)
    {
        var validated = false;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).Must(_ => validated = true);
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider();
        var interceptor = new RequestValidationInterceptor(services);

        await InvokeStreamAsync(interceptor, duplex, new TestStreamReader(), Substitute.For<ServerCallContext>(), async reader =>
        {
            Assert.That(await reader.MoveNext(CancellationToken.None), Is.False);
            return new object();
        });

        Assert.That(validated, Is.False);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task RequestStream_CallOrReadCancellationCancelsValidation(bool duplex, bool cancelRead)
    {
        using var callCancellation = new CancellationTokenSource();
        using var readCancellation = new CancellationTokenSource();
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).MustAsync(async (_, token) =>
        {
            (cancelRead ? readCancellation : callCancellation).Cancel();
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            return true;
        });
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider();
        var interceptor = new RequestValidationInterceptor(services);
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(callCancellation.Token);
        var stream = new TestStreamReader(new TestRequest("test", 1));
        var delivered = false;

        await Assert.CatchAsync<OperationCanceledException>(async () =>
            await InvokeStreamAsync(interceptor, duplex, stream, context, async reader =>
            {
                await reader.MoveNext(readCancellation.Token);
                delivered = true;
                return new object();
            }));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(delivered, Is.False);
        Assert.That(stream.LastReadToken, Is.EqualTo(readCancellation.Token));
    }

    private static async Task<object?> InvokeStreamAsync(RequestValidationInterceptor interceptor,
                                                        bool duplex,
                                                        IAsyncStreamReader<TestRequest> requestStream,
                                                        ServerCallContext context,
                                                        Func<IAsyncStreamReader<TestRequest>, Task<object>> continuation)
    {
        if (!duplex)
            return await interceptor.ClientStreamingServerHandler<TestRequest, object>(requestStream, context, (reader, receivedContext) =>
            {
                Assert.That(receivedContext, Is.SameAs(context));
                return continuation(reader);
            });

        var responseStream = Substitute.For<IServerStreamWriter<object>>();
        object? response = null;
        await interceptor.DuplexStreamingServerHandler(requestStream, responseStream, context, async (reader, writer, receivedContext) =>
        {
            Assert.That(writer, Is.SameAs(responseStream));
            Assert.That(receivedContext, Is.SameAs(context));
            response = await continuation(reader);
        });
        return response;
    }

    private sealed class TestStreamReader(params TestRequest[] requests) : IAsyncStreamReader<TestRequest>
    {
        private int index = -1;

        public int ReadCount { get; private set; }
        public CancellationToken LastReadToken { get; private set; }
        public TestRequest Current => requests[index];

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            ReadCount++;
            LastReadToken = cancellationToken;
            return Task.FromResult(++index < requests.Length);
        }
    }

    private sealed record TestRequest(string Name, int Id);
}
