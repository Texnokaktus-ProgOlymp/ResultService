using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Grpc.Interceptors;

public class RequestValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        if (serviceProvider.GetService<IValidator<TRequest>>() is { } validator)
            await ValidateAsync(validator, request, context.CancellationToken);

        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        if (serviceProvider.GetService<IValidator<TRequest>>() is { } validator)
            await ValidateAsync(validator, request, context.CancellationToken);

        await continuation(request, responseStream, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation
    ) =>
        continuation(ValidateStream(requestStream, context.CancellationToken), context);

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation
    ) =>
        continuation(ValidateStream(requestStream, context.CancellationToken), responseStream, context);

    private IAsyncStreamReader<TRequest> ValidateStream<TRequest>(
        IAsyncStreamReader<TRequest> requestStream,
        CancellationToken cancellationToken
    ) =>
        serviceProvider.GetService<IValidator<TRequest>>() is { } validator
            ? new ValidatingStreamReader<TRequest>(requestStream, validator, cancellationToken)
            : requestStream;

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new RpcException(
                new(
                    StatusCode.InvalidArgument,
                    string.Join("; ", result.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"))
                )
            );
    }

    private sealed class ValidatingStreamReader<TRequest>(
        IAsyncStreamReader<TRequest> requestStream,
        IValidator<TRequest> validator,
        CancellationToken callCancellationToken
    ) : IAsyncStreamReader<TRequest>
    {
        public TRequest Current => requestStream.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (!await requestStream.MoveNext(cancellationToken))
                return false;

            using var cancellation = cancellationToken.CanBeCanceled && cancellationToken != callCancellationToken
                                         ? CancellationTokenSource.CreateLinkedTokenSource(
                                             callCancellationToken,
                                             cancellationToken
                                         )
                                         : null;

            await ValidateAsync(validator, Current, cancellation?.Token ?? callCancellationToken);
            return true;
        }
    }
}
