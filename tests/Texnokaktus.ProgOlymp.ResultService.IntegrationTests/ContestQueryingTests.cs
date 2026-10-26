using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ContestQueryingTests : SetupBase
{
    [TestCase("missing", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task MissingContestOrStage_ReturnsNotFound(string contestName, ContestStage stage)
    {
        var client = Factory.CreateResultServiceClient();
        await DataBuilder.ForClient(client)
            .AddContestStage("test", ContestStage.Preliminary, 1000)
            .BuildAsync();

        var action = async () => await client.GetContestAsync(new() { ContestName = contestName, Stage = stage });

        await Assert.ThatAsync(action, Throws.TypeOf<RpcException>()
            .With.Property(nameof(RpcException.StatusCode)).EqualTo(StatusCode.NotFound)
            .And.Property(nameof(RpcException.Status)).Property(nameof(Status.Detail))
            .EqualTo($"The contest {contestName} {stage} stage was not found"));
    }
}
