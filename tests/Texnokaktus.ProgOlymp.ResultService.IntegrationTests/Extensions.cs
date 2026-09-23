using Grpc.Core;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

internal static class Extensions
{
    public static AsyncUnaryCall<T> ToAsyncUnaryCall<T>(this T data) => new(
        Task.FromResult(data),
        null!,
        null!,
        null!,
        null!
    );
}
