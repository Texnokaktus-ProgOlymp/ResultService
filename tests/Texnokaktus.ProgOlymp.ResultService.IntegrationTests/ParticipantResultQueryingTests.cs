using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ParticipantResultQueryingTests : SetupBase
{
    [TestCase("missing", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task MissingContestOrStage_ReturnsNotFound(string contestName, ContestStage stage)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000)
            .BuildAsync();

        var client = Factory.CreateResultQueryingServiceClient();
        var action = async () => await client.GetResultsByParticipantAsync(new()
        {
            ContestName = contestName,
            Stage = stage,
            ParticipantId = 1
        });

        await Assert.ThatAsync(action, Throws.TypeOf<RpcException>()
            .With.Property(nameof(RpcException.StatusCode)).EqualTo(StatusCode.NotFound)
            .And.Property(nameof(RpcException.Status)).Property(nameof(Status.Detail))
            .EqualTo($"The contest {contestName} {stage} stage was not found"));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task ParticipantWithoutVisibleResults_ReturnsNotFound(bool registered, bool hasResults)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage =>
                stage.AddProblem("A", "Problem A", problem =>
                {
                    problem.AddResult(1, 100m);
                    if (hasResults)
                        problem.AddResult(2, 50m);
                }))
            .BuildAsync();
        SubstituteParticipant("Default", 1);
        if (registered)
            SubstituteParticipant("Default", 2);

        var client = Factory.CreateResultQueryingServiceClient();
        var action = async () => await client.GetResultsByParticipantAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            ParticipantId = 2
        });

        await Assert.ThatAsync(action, Throws.TypeOf<RpcException>()
            .With.Property(nameof(RpcException.StatusCode)).EqualTo(StatusCode.NotFound)
            .And.Property(nameof(RpcException.Status)).Property(nameof(Status.Detail))
            .EqualTo("The participant 2 results were not found in the contest test Preliminary stage"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExistingParticipant_ReturnsGroupRankMissingScoresAndAdjustments(bool published)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("C", "Problem C")
                .AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 200m)
                    .AddResult(2, 50.25m, result => result.AddAdjustment(10.50m, "Appeal").AddAdjustment(-0.75m))
                    .AddResult(3, 80m)
                    .AddResult(4, 60m))
                .AddProblem("B", "Problem B", problem => problem.AddResult(2, 0m))
                .MarkPublished(published))
            .BuildAsync();
        SubstituteParticipant("Other group", 1);
        SubstituteParticipant("Selected group", 3);
        SubstituteParticipant("Selected group", 4);
        SubstituteParticipant("Selected group", 2);

        var results = await Factory.CreateResultQueryingServiceClient().GetResultsByParticipantAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            ParticipantId = 2
        });

        ContestResultsAssert.Matches(results,
            [new(2, "A", "Problem A"), new(3, "B", "Problem B"), new(1, "C", "Problem C")],
            "Selected group", new(2, 2,
                [new(2, 50.25m, new(1, 10.50m, "Appeal"), new(2, -0.75m)), new(3, 0m), new(1, null)]));
        Assert.That(results.Problems.Select(problem => problem.Alias), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public async Task DisqualifiedParticipant_ReturnsFlagWithoutPlaceOrTotalAndKeepsScores()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 200m, result => result.AddAdjustment(-10m, "Correction"))
                    .AddResult(2, 100m))
                .AddProblem("B", "Problem B")
                .DisqualifyParticipant(1, "Rule violation"))
            .BuildAsync();
        SubstituteParticipant("Default", 2);
        SubstituteParticipant("Default", 1);

        var results = await Factory.CreateResultQueryingServiceClient().GetResultsByParticipantAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            ParticipantId = 1
        });

        ContestResultsAssert.Matches(results, [new(1, "A", "Problem A"), new(2, "B", "Problem B")],
            "Default", new(1, null, [new(1, 200m, [new(1, -10m, "Correction")]), new(2, null)], true));
    }

    private static void SubstituteParticipant(string groupName, int id) =>
        SubstituteExtensions.Participants.SubstituteParticipant("test", groupName, new()
        {
            Id = id,
            Grade = 11,
            Name = new() { FirstName = $"Participant {id}", LastName = "Test" }
        });
}
