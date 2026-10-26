using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.Endpoints.Filters;

namespace Texnokaktus.ProgOlymp.ResultService.Tests;

public class ValidationFilterTests
{
    [Test]
    public async Task InvalidRequests_ReturnAllErrorsWithoutInvokingHandler()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();
        using var services = new ServiceCollection().AddScoped<IValidator<TestRequest>>(_ => validator).BuildServiceProvider(true);
        using var scope = services.CreateScope();
        var invoked = false;
        var filter = ValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<TestRequest, TestRequest, object>)MultipleRequestHandler).Method
        }, _ =>
        {
            invoked = true;
            return ValueTask.FromResult<object?>(null);
        });
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext { RequestServices = scope.ServiceProvider },
                                                            new TestRequest(""), new TestRequest(" "));

        var result = await filter(context);

        Assert.That(result, Is.TypeOf<ValidationProblem>());
        var problem = ((ValidationProblem)result!).ProblemDetails;
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(invoked, Is.False);
        Assert.That(problem.Status, Is.EqualTo(400));
        Assert.That(problem.Errors.Keys, Is.EquivalentTo(new[] { "Name" }));
        Assert.That(problem.Errors["Name"], Has.Length.EqualTo(2));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AsyncValidator_UsesRequestScopeAndCancellationToken(bool valid)
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
        var expected = new object();
        EndpointFilterInvocationContext? receivedContext = null;
        var filter = ValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<TestRequest, object>)RequestHandler).Method
        }, context =>
        {
            receivedContext = context;
            return ValueTask.FromResult<object?>(expected);
        });
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellation.Token
        }, new TestRequest("test"));

        var result = await filter(context);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(receivedToken, Is.EqualTo(cancellation.Token));
        if (valid)
        {
            Assert.That(result, Is.SameAs(expected));
            Assert.That(receivedContext, Is.SameAs(context));
        }
        else
        {
            Assert.That(result, Is.TypeOf<ValidationProblem>());
            Assert.That(receivedContext, Is.Null);
        }
    }

    [Test]
    public async Task NoValidator_InvokesHandlerWithOriginalArguments()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var expected = new object();
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext { RequestServices = services }, new TestRequest(""));
        EndpointFilterInvocationContext? receivedContext = null;
        var filter = ValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<TestRequest, object>)RequestHandler).Method
        }, invocationContext =>
        {
            receivedContext = invocationContext;
            return ValueTask.FromResult<object?>(expected);
        });

        var result = await filter(context);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(result, Is.SameAs(expected));
        Assert.That(receivedContext, Is.SameAs(context));
    }

    private sealed record TestRequest(string Name);

    private static object RequestHandler(TestRequest request) => new();

    private static object MultipleRequestHandler(TestRequest first, TestRequest second) => new();
}
