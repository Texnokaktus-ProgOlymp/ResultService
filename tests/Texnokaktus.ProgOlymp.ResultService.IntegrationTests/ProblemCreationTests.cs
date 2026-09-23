using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ProblemCreationTests : SetupBase
{
    [Test]
    public async Task CreateProblem_MissingContest_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        var action = async () => await client.AddProblemAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = "A",
                                         Name = "Test Problem"
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
    public async Task CreateProblem_Success()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(contestName, contestStage, 1000)
             .BuildAsync();

        await client.AddProblemAsync(
            new()
            {
                ContestName = contestName,
                Stage = contestStage,
                Alias = "A",
                Name = "Test Problem"
            }
        );

        var contest = await client.GetContestAsync(
                          new()
                          {
                              ContestName = contestName,
                              Stage = contestStage
                          }
                      );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contest.Id, Is.EqualTo(1));
            Assert.That(contest.StageId, Is.EqualTo(1000));
            Assert.That(contest.Stage, Is.EqualTo(contestStage));

            Assert.That(
                contest.Problems,
                Is.EquivalentTo(
                    [
                        new Problem
                        {
                            Id = 1,
                            Alias = "A",
                            Name = "Test Problem"
                        }
                    ]
                )
            );
        }
    }

    [Test]
    public async Task CreateProblem_PublishedContest_ThrowsError()
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
                  contestStageBuilder => contestStageBuilder.MarkPublished()
              )
             .BuildAsync();

        var action = async () => await client.AddProblemAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = "A",
                                         Name = "Test Problem"
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
    public async Task CreateProblem_DuplicateAlias_ThrowsError()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;
        const string alias = "A";

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder.AddProblem(alias, "Test Problem")
              )
             .BuildAsync();

        var action = async () => await client.AddProblemAsync(
                                     new()
                                     {
                                         ContestName = contestName,
                                         Stage = contestStage,
                                         Alias = alias,
                                         Name = "Test Problem 2"
                                     }
                                 );

        await Assert.ThatAsync(
            action,
            Throws.TypeOf<RpcException>()
                  .With.Property("StatusCode").EqualTo(StatusCode.AlreadyExists)
                  .And.Message.Matches($"The contest {contestName} {contestStage} stage already has problem {alias}")
        );
    }
}
