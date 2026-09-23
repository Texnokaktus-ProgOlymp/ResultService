using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultAdjustmentCreationTests : SetupBase
{
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

        await client.AddContestAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                StageId = 1000
            }
        );

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

        await client.AddContestAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                StageId = 1000
            }
        );

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var contestResult = await context.ContestResults.SingleAsync(result => result.Id == 1);
            contestResult.Published = true;
            await context.SaveChangesAsync();
        }

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

        await client.AddContestAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                StageId = 1000
            }
        );

        await client.AddProblemAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                Alias = problemAlias,
                Name = "Test Problem"
            }
        );

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

        await client.AddContestAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                StageId = 1000
            }
        );

        await client.AddProblemAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                Alias = problemAlias,
                Name = "Test Problem"
            }
        );

        await client.AddResultAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                Alias = problemAlias,
                ParticipantId = participantId,
                BaseScore = 100m
            }
        );

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
