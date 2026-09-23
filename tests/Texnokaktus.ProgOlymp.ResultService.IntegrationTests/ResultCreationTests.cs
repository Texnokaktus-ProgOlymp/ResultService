using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultCreationTests : SetupBase
{
    [Test]
    public async Task CreateResult_MissingContest_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        var action = async () => await client.AddResultAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = "A",
                                         ParticipantId = 1,
                                         BaseScore = 100m
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
    public async Task CreateResult_MissingProblem_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string problemAlias = "A";

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(contestName, contestStage, 1000)
             .BuildAsync();

        var action = async () => await client.AddResultAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = 1,
                                         BaseScore = 100m
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
    public async Task CreateResult_PublishedContest_ThrowsError()
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

        var action = async () => await client.AddResultAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = 1,
                                         BaseScore = 100m
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
    public async Task CreateResult_AlreadyCreated_ThrowsError()
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

        var action = async () => await client.AddResultAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = problemAlias,
                                         ParticipantId = participantId,
                                         BaseScore = 200m
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.AlreadyExists)
                  .And.Message.Matches($"The problem {problemAlias} result for participant {participantId} in the contest {contestName} {contestStage} stage already exists")
        );
    }

    [Test]
    public async Task CreateResult_Success()
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
                  contestStageBuilder => contestStageBuilder.AddProblem(problemAlias, "Test Problem")
              )
             .BuildAsync();

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
                                TotalScore = 100m,
                                IsDisqualified = false,
                                Results =
                                {
                                    new ProblemResult
                                    {
                                        ProblemId = 1,
                                        Score = new()
                                        {
                                            BaseScore = 100m,
                                            TotalScore = 100m
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
