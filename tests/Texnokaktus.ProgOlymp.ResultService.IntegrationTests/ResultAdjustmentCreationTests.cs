using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultAdjustmentCreationTests : SetupBase
{
    [Test]
    public async Task SubsequentAdjustments_ChangeLeaderCreateTieAndBreakTie()
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem => problem.AddResult(1, 100m).AddResult(2, 90m)))
            .BuildAsync();
        foreach (var id in new[] { 1, 2 })
            SubstituteExtensions.Participants.SubstituteParticipant("test", "Default", new()
            {
                Id = id,
                Grade = 11,
                Name = new() { FirstName = $"Participant {id}", LastName = "Test" }
            });

        var queryingClient = Factory.CreateResultQueryingServiceClient();
        await AssertRowsAsync([new(1, 1, [new(1, 100m)]), new(2, 2, [new(1, 90m)])]);

        var first = await AddAdjustmentAsync(2, 20m, "Appeal");
        Assert.That(first.Id, Is.EqualTo(1));
        await AssertRowsAsync([
            new(2, 1, [new(1, 90m, [new(1, 20m, "Appeal")])]), new(1, 2, [new(1, 100m)])]);

        var second = await AddAdjustmentAsync(1, 10m);
        Assert.That(second.Id, Is.EqualTo(2));
        await AssertRowsAsync([
            new(1, 1, [new(1, 100m, [new(2, 10m)])]),
            new(2, 1, [new(1, 90m, [new(1, 20m, "Appeal")])])]);

        var third = await AddAdjustmentAsync(1, -20m, "Correction");
        Assert.That(third.Id, Is.EqualTo(3));
        await AssertRowsAsync([
            new(2, 1, [new(1, 90m, [new(1, 20m, "Appeal")])]),
            new(1, 2, [new(1, 100m, new(2, 10m), new(3, -20m, "Correction"))])]);

        async Task AssertRowsAsync(ExpectedResultRow[] rows)
        {
            var results = await queryingClient.GetResultsAsync(new() { ContestName = "test", Stage = ContestStage.Preliminary });
            ContestResultsAssert.Matches(results, new([new(1, "A", "Problem A")], [new("Default", rows)]));
        }

        async Task<AddResultAdjustmentResponse> AddAdjustmentAsync(int participantId, decimal adjustment, string? comment = null) =>
            await client.AddResultAdjustmentAsync(new()
            {
                ContestName = "test",
                Stage = ContestStage.Preliminary,
                Alias = "A",
                ParticipantId = participantId,
                Adjustment = adjustment,
                Comment = comment
            });
    }

    [Test]
    public async Task CreateResultAdjustment_MissingContest_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        var action = async () => await client.AddResultAdjustmentAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = "A",
                                         ParticipantId = 1,
                                         Adjustment = 100m
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.NotFound)
                  .And.Message.Matches($"The contest {contestName} {contestStage} stage was not found")
        );
    }

    [Test]
    public async Task CreateResultAdjustment_MissingProblem_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string problemAlias = "A";

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client).AddContestStage(contestName, contestStage, 1000,
                  stage => stage.AddProblem("B", "Other problem", problem => problem.AddResult(1, 50m)))
             .BuildAsync();

        var action = async () => await client.AddResultAdjustmentAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = 1,
                                         Adjustment = 100m
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.NotFound)
                  .And.Message.Matches($"The problem {problemAlias} was not found in the contest {contestName} {contestStage} stage")
        );
    }

    [Test]
    public async Task CreateResultAdjustment_PublishedContest_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string problemAlias = "A";

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder.MarkPublished()
              )
             .BuildAsync();

        var action = async () => await client.AddResultAdjustmentAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = 1,
                                         Adjustment = 100m
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.FailedPrecondition)
                  .And.Message.Matches($"The contest {contestName} {contestStage} stage is readonly")
        );
    }

    [Test]
    public async Task CreateResultAdjustment_MissingResult_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string problemAlias = "A";
        const int participantId = 1;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder.AddProblem(problemAlias, "Test Problem",
                      problem => problem.AddResult(2, 50m))
              )
             .BuildAsync();

        var action = async () => await client.AddResultAdjustmentAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = participantId,
                                         Adjustment = 100m
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.NotFound)
                  .And.Message.Matches($"The problem {problemAlias} result for participant {participantId} in the contest {contestName} {contestStage} stage was not found")
        );
    }

    [Test]
    public async Task CreateResultAdjustment_Success()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string problemAlias = "A";
        const int participantId = 1;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder.AddProblem(
                      problemAlias,
                      "Test Problem",
                      problemBuilder => problemBuilder.AddResult(participantId, 100m)
                  )
              )
             .BuildAsync();

        var response = await client.AddResultAdjustmentAsync(
                           new()
                           {
                               ContestName = contestName,
                               Stage = contestStage,
                               Alias = problemAlias,
                               ParticipantId = participantId,
                               Adjustment = 100m
                           }
                       );

        SubstituteExtensions.Participants.SubstituteParticipant(
            contestName,
            "Default",
            new()
            {
                Id = participantId,
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Id, Is.EqualTo(1));

            Assert.That(
                results.ResultGroups,
                Is.EquivalentTo(
                    [
                        new ResultGroup
                        {
                            Name = "Default",
                            Rows =
                            {
                                new ResultRow
                                {
                                    Place = 1,
                                    ParticipantId = participantId,
                                    TotalScore = 200m,
                                    IsDisqualified = false,
                                    Results =
                                    {
                                        new ProblemResult
                                        {
                                            ProblemId = 1,
                                            Score = new()
                                            {
                                                BaseScore = 100m,
                                                Adjustments =
                                                {
                                                    new ScoreAdjustment
                                                    {
                                                        Id = 1,
                                                        Adjustment = 100m
                                                    }
                                                },
                                                AdjustmentsSum = 100m,
                                                TotalScore = 200m
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ]
                )
            );
        }
    }
}
