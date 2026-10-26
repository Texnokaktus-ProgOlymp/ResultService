using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class RequestValidationTests : SetupBase
{
    [TestCase("")]
    [TestCase("/personal")]
    public async Task InvalidRestRequest_ReturnsErrorsForAllInvalidFields(string suffix)
    {
        using var client = Factory.CreateAuthenticatedClient(1);
        using var response = await client.GetAsync($"/api/contests/%20/2/results{suffix}");

        await HttpResponseAssert.IsValidationProblemAsync(response, "contestName", "stage");
    }

    [TestCaseSource(nameof(InvalidGrpcRequests))]
    public async Task InvalidGrpcRequest_ThrowsInvalidArgumentWithFieldErrors(object request, string[] properties)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(async () => await InvokeGrpcAsync(request));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        foreach (var property in properties)
            Assert.That(exception.Status.Detail, Does.Contain($"{property}:"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task NonPositiveStageId_ThrowsInvalidArgument(long stageId)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await Factory.CreateResultServiceClient().AddContestAsync(new()
            {
                ContestName = "test", Stage = ContestStage.Preliminary, StageId = stageId
            }));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Does.Contain("StageId:"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task NonPositiveParticipantId_ThrowsInvalidArgument(int participantId)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await Factory.CreateResultQueryingServiceClient().GetResultsByParticipantAsync(new()
            {
                ContestName = "test", Stage = ContestStage.Preliminary, ParticipantId = participantId
            }));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Does.Contain("ParticipantId:"));
    }

    [TestCase("0")]
    [TestCase("-1.25")]
    public async Task ZeroAndNegativeScoresAndAdjustments_AreAccepted(string value)
    {
        var score = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage.AddProblem("A", "Problem A"))
            .BuildAsync();

        await client.AddResultAsync(new()
        {
            ContestName = "test", Stage = ContestStage.Preliminary, Alias = "A", ParticipantId = 1, BaseScore = score
        });
        var response = await client.AddResultAdjustmentAsync(new()
        {
            ContestName = "test", Stage = ContestStage.Preliminary, Alias = "A", ParticipantId = 1, Adjustment = score
        });

        Assert.That(response.Id, Is.GreaterThan(0));
    }

    private static IEnumerable<TestCaseData> InvalidGrpcRequests()
    {
        foreach (var stage in new[] { (ContestStage)(-1), (ContestStage)2 })
        {
            yield return new(new GetContestRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage" });
            yield return new(new AddContestRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage", "StageId" });
            yield return new(new AddProblemRequest { ContestName = " ", Stage = stage, Alias = " ", Name = " " },
                             new[] { "ContestName", "Stage", "Alias", "Name" });
            yield return new(new AddResultRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage", "Alias", "ParticipantId", "BaseScore" });
            yield return new(new AddResultAdjustmentRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage", "Alias", "ParticipantId", "Adjustment" });
            yield return new(new GetResultsRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage" });
            yield return new(new GetResultsByParticipantRequest { ContestName = " ", Stage = stage },
                             new[] { "ContestName", "Stage", "ParticipantId" });
        }
    }

    private static Task InvokeGrpcAsync(object request)
    {
        var client = Factory.CreateResultServiceClient();
        var queryingClient = Factory.CreateResultQueryingServiceClient();
        return request switch
        {
            GetContestRequest value => client.GetContestAsync(value).ResponseAsync,
            AddContestRequest value => client.AddContestAsync(value).ResponseAsync,
            AddProblemRequest value => client.AddProblemAsync(value).ResponseAsync,
            AddResultRequest value => client.AddResultAsync(value).ResponseAsync,
            AddResultAdjustmentRequest value => client.AddResultAdjustmentAsync(value).ResponseAsync,
            GetResultsRequest value => queryingClient.GetResultsAsync(value).ResponseAsync,
            GetResultsByParticipantRequest value => queryingClient.GetResultsByParticipantAsync(value).ResponseAsync,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
    }
}
