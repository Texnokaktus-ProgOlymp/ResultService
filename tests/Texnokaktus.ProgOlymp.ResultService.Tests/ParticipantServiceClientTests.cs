using Grpc.Core;
using NSubstitute;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using ParticipantClient = Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.ParticipantServiceClient;

namespace Texnokaktus.ProgOlymp.ResultService.Tests;

public class ParticipantServiceClientTests
{
    [Test]
    public async Task ParticipantLookup_ForwardsRequestAndCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        CallOptions? receivedOptions = null;
        GetParticipantIdRequest? receivedRequest = null;
        var client = CreateClient(Task.FromResult(new GetParticipantIdResponse { ParticipantId = 17 }), (options, request) =>
        {
            receivedOptions = options;
            receivedRequest = request;
        });

        var participantId = await client.GetParticipantIdAsync("test", 42, cancellation.Token);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(participantId, Is.EqualTo(17));
        Assert.That(receivedRequest?.ContestName, Is.EqualTo("test"));
        Assert.That(receivedRequest?.UserId, Is.EqualTo(42));
        Assert.That(receivedOptions?.CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task CancelledLookup_PreservesTaskCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = CreateClient(Task.FromCanceled<GetParticipantIdResponse>(cancellation.Token));

        var exception = await Assert.CatchAsync<OperationCanceledException>(async () =>
            await client.GetParticipantIdAsync("test", 42, cancellation.Token));

        Assert.That(exception!.CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task CancelledRpc_PreservesOriginalException()
    {
        var expected = new RpcException(new(StatusCode.Cancelled, "Lookup cancelled"));
        var client = CreateClient(Task.FromException<GetParticipantIdResponse>(expected));

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetParticipantIdAsync("test", 42, CancellationToken.None));

        Assert.That(exception, Is.SameAs(expected));
    }

    [Test]
    public async Task ParticipantNotFound_ReturnsNull()
    {
        var client = CreateClient(Task.FromException<GetParticipantIdResponse>(
            new RpcException(new(StatusCode.NotFound, "Participant not found"))));

        var participantId = await client.GetParticipantIdAsync("test", 42, CancellationToken.None);

        Assert.That(participantId, Is.Null);
    }

    private static ParticipantClient CreateClient(Task<GetParticipantIdResponse> response,
        Action<CallOptions, GetParticipantIdRequest>? onCall = null)
    {
        var invoker = Substitute.For<CallInvoker>();
        invoker.AsyncUnaryCall(Arg.Any<Method<GetParticipantIdRequest, GetParticipantIdResponse>>(),
                Arg.Any<string>(), Arg.Any<CallOptions>(), Arg.Any<GetParticipantIdRequest>())
            .Returns(info =>
            {
                onCall?.Invoke(info.Arg<CallOptions>(), info.Arg<GetParticipantIdRequest>());
                return new AsyncUnaryCall<GetParticipantIdResponse>(response,
                    Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            });
        return new(new ParticipantService.ParticipantServiceClient(invoker));
    }
}
