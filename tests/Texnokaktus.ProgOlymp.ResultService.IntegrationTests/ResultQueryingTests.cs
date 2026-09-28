using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultQueryingTests : SetupBase
{
    [Test]
    public async Task EqualTotals_SharePlacesAndSkipFollowingPlaces()
    {
        await AssertResultsAsync(
            stage => stage
                .AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 100m).AddResult(2, 40m).AddResult(3, 80m)
                    .AddResult(4, 60m).AddResult(5, 20m).AddResult(6, 40m))
                .AddProblem("B", "Problem B", problem => problem.AddResult(2, 60m).AddResult(5, 40m)),
            [("Default", [6, 4, 2, 3, 1, 5])],
            [new(1, "A", "Problem A"), new(2, "B", "Problem B")],
            [new("Default", [
                new(2, 1, [new(1, 40m), new(2, 60m)]),
                new(1, 1, [new(1, 100m), new(2, null)]),
                new(3, 3, [new(1, 80m), new(2, null)]),
                new(4, 4, [new(1, 60m), new(2, null)]),
                new(5, 4, [new(1, 20m), new(2, 40m)]),
                new(6, 6, [new(1, 40m), new(2, null)])])]);
    }

    [Test]
    public async Task ZeroScores_SharePlaceAndPreserveMissingProblemResults()
    {
        await AssertResultsAsync(
            stage => stage
                .AddProblem("A", "Problem A", problem => problem.AddResult(1, 50m).AddResult(2, 0m).AddResult(3, 0m))
                .AddProblem("B", "Problem B", problem => problem.AddResult(2, 0m)),
            [("Default", [2, 1, 3])],
            [new(1, "A", "Problem A"), new(2, "B", "Problem B")],
            [new("Default", [
                new(1, 1, [new(1, 50m), new(2, null)]),
                new(2, 2, [new(1, 0m), new(2, 0m)]),
                new(3, 2, [new(1, 0m), new(2, null)])])]);
    }

    [Test]
    public async Task ParticipantWithoutResults_IsAbsentFromRows()
    {
        await AssertResultsAsync(
            stage => stage.AddProblem("A", "Problem A", problem => problem.AddResult(1, 50m)),
            [("Default", [2, 1])],
            [new(1, "A", "Problem A")],
            [new("Default", [new(1, 1, [new(1, 50m)])])]);
    }

    [Test]
    public async Task DisqualifiedParticipants_AreUnrankedAndKeepProblemScores()
    {
        await AssertResultsAsync(
            stage => stage.AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 100m).AddResult(2, 100m).AddResult(3, 50m)
                    .AddResult(4, 0m).AddResult(5, 200m).AddResult(6, 10m))
                .DisqualifyParticipant(5, "Rule violation").DisqualifyParticipant(6),
            [("Default", [5, 4, 2, 6, 3, 1])],
            [new(1, "A", "Problem A")],
            [new("Default", [
                new(2, 1, [new(1, 100m)]), new(1, 1, [new(1, 100m)]),
                new(3, 3, [new(1, 50m)]), new(4, 4, [new(1, 0m)]),
                new(5, null, [new(1, 200m)], true), new(6, null, [new(1, 10m)], true)])]);
    }

    [TestCase(1)]
    [TestCase(2)]
    public async Task AllParticipantsDisqualified_AllPlacesAreNull(int count)
    {
        var participantIds = Enumerable.Range(1, count).ToArray();
        await AssertResultsAsync(
            stage =>
            {
                stage.AddProblem("A", "Problem A", problem =>
                {
                    foreach (var id in participantIds)
                        problem.AddResult(id, id * 10m);
                });
                foreach (var id in participantIds)
                    stage.DisqualifyParticipant(id);
            },
            [("Default", participantIds)],
            [new(1, "A", "Problem A")],
            [new("Default", participantIds.Select(id =>
                new ExpectedResultRow(id, null, [new(1, id * 10m)], true)).ToArray())]);
    }

    [Test]
    public async Task MultipleGroups_RankParticipantsIndependently()
    {
        await AssertResultsAsync(
            stage => stage.AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 100m).AddResult(2, 100m).AddResult(3, 50m)
                    .AddResult(4, 80m).AddResult(5, 40m).AddResult(6, 40m)
                    .AddResult(7, 0m).AddResult(8, 200m))
                .DisqualifyParticipant(8),
            [("A", [3, 1, 2]), ("B", [8, 7, 5, 4, 6])],
            [new(1, "A", "Problem A")],
            [new("A", [new(1, 1, [new(1, 100m)]), new(2, 1, [new(1, 100m)]), new(3, 3, [new(1, 50m)])]),
             new("B", [new(4, 1, [new(1, 80m)]), new(5, 2, [new(1, 40m)]), new(6, 2, [new(1, 40m)]),
                 new(7, 4, [new(1, 0m)]), new(8, null, [new(1, 200m)], true)])]);
    }

    [Test]
    public async Task MultipleStages_ApplyDisqualificationAndPublicationToSelectedStage()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000,
                stage => stage.AddProblem("A", "Problem A", problem => problem.AddResult(1, 50m)))
            .AddContestStage("test", ContestStage.Final, 1001,
                stage => stage.AddProblem("A", "Problem A", problem => problem.AddResult(1, 50m))
                    .DisqualifyParticipant(1, "Old reason").DisqualifyParticipant(1, "Updated reason")
                    .DisqualifyParticipant(2).MarkPublished())
            .BuildAsync();
        SubstituteParticipant("Default", 1);
        SubstituteParticipant("Default", 2);

        var client = Factory.CreateResultQueryingServiceClient();
        var preliminary = await client.GetResultsAsync(new() { ContestName = "test", Stage = ContestStage.Preliminary });
        var final = await client.GetResultsAsync(new() { ContestName = "test", Stage = ContestStage.Final });

        ContestResultsAssert.Matches(preliminary, new([new(1, "A", "Problem A")],
            [new("Default", [new(1, 1, [new(1, 50m)])])]));
        ContestResultsAssert.Matches(final, new([new(2, "A", "Problem A")],
            [new("Default", [new(1, null, [new(2, 50m)], true)])]));

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stages = await context.ContestResults.Include(stage => stage.DisqualificationNotes).ToArrayAsync();
        var preliminaryStage = stages.Single(stage => stage.Stage == DataAccess.Entities.ContestStage.Preliminary);
        var finalStage = stages.Single(stage => stage.Stage == DataAccess.Entities.ContestStage.Final);
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(preliminaryStage.Published, Is.False);
        Assert.That(preliminaryStage.DisqualificationNotes, Is.Empty);
        Assert.That(finalStage.Published, Is.True);
        Assert.That(finalStage.DisqualificationNotes.Select(note => (note.ParticipantId, note.Reason)),
            Is.EquivalentTo(new (int, string?)[] { (1, "Updated reason"), (2, null) }));
    }

    private async Task AssertResultsAsync(
        Action<IContestStageBuilder> configure,
        (string Name, int[] ParticipantIds)[] groups,
        ExpectedProblem[] problems,
        ExpectedResultGroup[] expectedGroups)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, configure)
            .BuildAsync();
        foreach (var (name, participantIds) in groups)
            foreach (var id in participantIds)
                SubstituteParticipant(name, id);

        var results = await Factory.CreateResultQueryingServiceClient().GetResultsAsync(new()
        {
            ContestName = "test",
            Stage = ContestStage.Preliminary
        });

        ContestResultsAssert.Matches(results, new(problems, expectedGroups));
    }

    private static void SubstituteParticipant(string groupName, int id) =>
        SubstituteExtensions.Participants.SubstituteParticipant("test", groupName, new()
        {
            Id = id,
            Grade = 11,
            Name = new() { FirstName = $"Participant {id}", LastName = "Test" }
        });

    [Test]
    public async Task SingleParticipant()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder
                                        .AddProblem(
                                             "A",
                                             "Test Problem 1",
                                             problemBuilder => problemBuilder.AddResult(1, 20m)
                                         )
                                        .AddProblem(
                                             "B",
                                             "Test Problem 2",
                                             problemBuilder => problemBuilder.AddResult(
                                                 1,
                                                 40m,
                                                 resultBuilder => resultBuilder.AddAdjustment(10m, "Test comment")
                                                                               .AddAdjustment(-2m)
                                             )
                                         )
                                        .AddProblem(
                                             "C",
                                             "Test Problem 3",
                                             problemBuilder => problemBuilder.AddResult(1, 80m)
                                         )
              )
             .BuildAsync();

        SubstituteExtensions.Participants.SubstituteParticipant(
            contestName,
            "Default",
            new()
            {
                Id = 1,
                Grade = 11,
                Name = new()
                {
                    FirstName = "A",
                    LastName = "B",
                    Patronym = "C"
                }
            }
        );

        var queryingClient = Factory.CreateResultQueryingServiceClient();

        var results = await queryingClient.GetResultsAsync(
                          new()
                          {
                              ContestName = contestName,
                              Stage = contestStage
                          }
                      );

        ContestResultsAssert.Matches(
            results,
            new(
                [
                    new(1, "A", "Test Problem 1"),
                    new(2, "B", "Test Problem 2"),
                    new(3, "C", "Test Problem 3")
                ],
                [
                    new("Default",
                    [
                        new(ParticipantId: 1, Place: 1, Results:
                        [
                            new(1, 20m),
                            new(2, 40m, new(1, 10m, "Test comment"), new(2, -2m)),
                            new(3, 80m)
                        ])
                    ])
                ]
            )
        );
    }
}
