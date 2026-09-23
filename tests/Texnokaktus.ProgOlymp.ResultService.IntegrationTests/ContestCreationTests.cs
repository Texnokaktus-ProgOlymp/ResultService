using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ContestCreationTests : SetupBase
{
    [Test]
    public async Task CreateContests_Success()
    {
        var client = Factory.CreateResultServiceClient();

        await client.AddContestAsync(
            new()
            {
                StageId = 1000,
                ContestName = "test",
                Stage = ContestStage.Preliminary
            }
        );

        await client.AddContestAsync(
            new()
            {
                StageId = 1001,
                ContestName = "test",
                Stage = ContestStage.Final
            }
        );

        var contest1 = await client.GetContestAsync(
                           new()
                           {
                               ContestName = "test",
                               Stage = ContestStage.Preliminary
                           }
                       );

        var contest2 = await client.GetContestAsync(
                           new()
                           {
                               ContestName = "test",
                               Stage = ContestStage.Final
                           }
                       );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contest1.Id, Is.EqualTo(1));
            Assert.That(contest1.StageId, Is.EqualTo(1000));
            Assert.That(contest1.Stage, Is.EqualTo(ContestStage.Preliminary));
            Assert.That(contest1.Problems, Is.Empty);

            Assert.That(contest2.Id, Is.EqualTo(2));
            Assert.That(contest2.StageId, Is.EqualTo(1001));
            Assert.That(contest2.Stage, Is.EqualTo(ContestStage.Final));
            Assert.That(contest2.Problems, Is.Empty);
        }
    }

    [Test]
    public async Task CreateContests_DuplicateNameStage_ThrowsError()
    {
        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage("test", ContestStage.Preliminary, 1000)
             .BuildAsync();

        var contest = await client.GetContestAsync(
                          new()
                          {
                              ContestName = "test",
                              Stage = ContestStage.Preliminary
                          }
                      );

        var action = async () => await client.AddContestAsync(
                                     new()
                                     {
                                         StageId = 1001,
                                         ContestName = "test",
                                         Stage = ContestStage.Preliminary
                                     }
                                 );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contest.Id, Is.EqualTo(1));
            Assert.That(contest.StageId, Is.EqualTo(1000));
            Assert.That(contest.Stage, Is.EqualTo(ContestStage.Preliminary));
            Assert.That(contest.Problems, Is.Empty);

            Assert.That(
                action,
                Throws.InstanceOf<RpcException>()
                      .With.Property("StatusCode").EqualTo(StatusCode.AlreadyExists)
                      .And.Message.Matches("The contest test Preliminary stage already exists")
            );
        }
    }

    [Test]
    public async Task CreateContests_DuplicateStageId_ThrowsError()
    {
        const string contestName = "test";
        const long stageId = 1000;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(contestName, ContestStage.Preliminary, stageId)
             .BuildAsync();

        var contest = await client.GetContestAsync(
                          new()
                          {
                              ContestName = contestName,
                              Stage = ContestStage.Preliminary
                          }
                      );

        var action = async () => await client.AddContestAsync(
                                     new()
                                     {
                                         StageId = stageId,
                                         ContestName = contestName,
                                         Stage = ContestStage.Final
                                     }
                                 );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contest.Id, Is.EqualTo(1));
            Assert.That(contest.StageId, Is.EqualTo(stageId));
            Assert.That(contest.Stage, Is.EqualTo(ContestStage.Preliminary));

            Assert.That(
                action,
                Throws.InstanceOf<RpcException>()
                      .With.Property("StatusCode").EqualTo(StatusCode.AlreadyExists)
                      .And.Message.Matches($"The contest stage {stageId} already exists")
            );
        }
    }
}
