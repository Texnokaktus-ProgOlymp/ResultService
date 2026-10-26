using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultMutationIsolationTests : SetupBase
{
    [TestCase("other", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task AddProblem_AliasInAnotherContestOrStage_DoesNotConflict(string otherContest, ContestStage otherStage)
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000)
            .AddContestStage(otherContest, otherStage, 1001, stage => stage
                .AddProblem("A", "Other problem").MarkPublished())
            .BuildAsync();

        await client.AddProblemAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            Alias = "A",
            Name = "Selected problem"
        });

        var selected = await client.GetContestAsync(new() { ContestName = "test", Stage = ContestStage.Preliminary });
        var other = await client.GetContestAsync(new() { ContestName = otherContest, Stage = otherStage });

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(selected.StageId, Is.EqualTo(1000));
        Assert.That(selected.Stage, Is.EqualTo(ContestStage.Preliminary));
        Assert.That(selected.Problems, Is.EqualTo(new[] { new Problem { Id = 2, Alias = "A", Name = "Selected problem" } }));
        Assert.That(other.StageId, Is.EqualTo(1001));
        Assert.That(other.Stage, Is.EqualTo(otherStage));
        Assert.That(other.Problems, Is.EqualTo(new[] { new Problem { Id = 1, Alias = "A", Name = "Other problem" } }));
    }

    [TestCase("other", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task AddResult_SameParticipantAndAliasInAnotherScope_DoesNotConflict(string otherContest, ContestStage otherStage)
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Selected A", problem => problem.AddResult(2, 80m))
                .AddProblem("B", "Selected B", problem => problem.AddResult(1, 40m)))
            .AddContestStage(otherContest, otherStage, 1001, stage => stage
                .AddProblem("B", "Other B", problem => problem.AddResult(2, 200m))
                .MarkPublished())
            .BuildAsync();
        SubstituteParticipants("test");
        if (otherContest != "test")
            SubstituteParticipants(otherContest);

        await client.AddResultAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            Alias = "B",
            ParticipantId = 2,
            BaseScore = 100m
        });

        await AssertResultsAsync("test", ContestStage.Preliminary,
            [new(1, "A", "Selected A"), new(2, "B", "Selected B")],
            [new(2, 1, [new(1, 80m), new(2, 100m)]), new(1, 2, [new(1, null), new(2, 40m)])]);
        await AssertResultsAsync(otherContest, otherStage, [new(3, "B", "Other B")],
            [new(2, 1, [new(3, 200m)])]);
    }

    [TestCase("other", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task AddAdjustment_ChangesOnlySelectedContestStageProblemAndParticipant(string otherContest, ContestStage otherStage)
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Selected A", problem => problem.AddResult(1, 10m).AddResult(2, 20m))
                .AddProblem("B", "Selected B", problem => problem.AddResult(1, 30m).AddResult(2, 40m)))
            .AddContestStage(otherContest, otherStage, 1001, stage => stage
                .AddProblem("B", "Other B", problem => problem.AddResult(1, 100m).AddResult(2, 200m))
                .MarkPublished())
            .BuildAsync();
        SubstituteParticipants("test");
        if (otherContest != "test")
            SubstituteParticipants(otherContest);

        var adjustment = await client.AddResultAdjustmentAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary,
            Alias = "B",
            ParticipantId = 2,
            Adjustment = 5.25m,
            Comment = "Selected result only"
        });

        Assert.That(adjustment.Id, Is.EqualTo(1));
        await AssertResultsAsync("test", ContestStage.Preliminary,
            [new(1, "A", "Selected A"), new(2, "B", "Selected B")],
            [new(2, 1, [new(1, 20m), new(2, 40m, [new(1, 5.25m, "Selected result only")])]),
                new(1, 2, [new(1, 10m), new(2, 30m)])]);
        await AssertResultsAsync(otherContest, otherStage, [new(3, "B", "Other B")],
            [new(2, 1, [new(3, 200m)]), new(1, 2, [new(3, 100m)])]);
    }

    [TestCase("problem")]
    [TestCase("result")]
    [TestCase("adjustment")]
    public async Task PublishedStage_RejectsMutationAndPreservesStoredData(string mutation)
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 100m, result => result.AddAdjustment(5m, "Existing")))
                .MarkPublished())
            .BuildAsync();
        SubstituteParticipants("test");
        var before = await client.GetContestAsync(new() { ContestName = "test", Stage = ContestStage.Preliminary });

        await Assert.ThatAsync(MutateAsync, Throws.TypeOf<RpcException>()
            .With.Property(nameof(RpcException.StatusCode)).EqualTo(StatusCode.FailedPrecondition)
            .And.Property(nameof(RpcException.Status)).Property(nameof(Status.Detail))
            .EqualTo("The contest test Preliminary stage is readonly"));

        var after = await client.GetContestAsync(new() { ContestName = "test", Stage = ContestStage.Preliminary });
        Assert.That(after, Is.EqualTo(before));
        await AssertResultsAsync("test", ContestStage.Preliminary, [new(1, "A", "Problem A")],
            [new(1, 1, [new(1, 100m, [new(1, 5m, "Existing")])])]);

        async Task MutateAsync()
        {
            switch (mutation)
            {
                case "problem":
                    await client.AddProblemAsync(new()
                    {
                        ContestName = "test", Stage = ContestStage.Preliminary, Alias = "B", Name = "New problem"
                    });
                    break;
                case "result":
                    await client.AddResultAsync(new()
                    {
                        ContestName = "test", Stage = ContestStage.Preliminary, Alias = "A", ParticipantId = 2, BaseScore = 50m
                    });
                    break;
                case "adjustment":
                    await client.AddResultAdjustmentAsync(new()
                    {
                        ContestName = "test", Stage = ContestStage.Preliminary, Alias = "A", ParticipantId = 1, Adjustment = 10m
                    });
                    break;
            }
        }
    }

    private static void SubstituteParticipants(string contestName)
    {
        foreach (var id in new[] { 1, 2 })
            SubstituteExtensions.Participants.SubstituteParticipant(contestName, "Default", new()
            {
                Id = id,
                Grade = 11,
                Name = new() { FirstName = $"Participant {id}", LastName = "Test" }
            });
    }

    private static async Task AssertResultsAsync(string contestName, ContestStage stage,
        ExpectedProblem[] problems, ExpectedResultRow[] rows)
    {
        var client = Factory.CreateResultQueryingServiceClient();
        var results = await client.GetResultsAsync(new() { ContestName = contestName, Stage = stage });
        ContestResultsAssert.Matches(results, new(problems, [new("Default", rows)]));

        foreach (var row in rows)
        {
            var personal = await client.GetResultsByParticipantAsync(new()
            {
                ContestName = contestName, Stage = stage, ParticipantId = row.ParticipantId
            });
            ContestResultsAssert.Matches(personal, problems, "Default", row);
        }
    }
}
