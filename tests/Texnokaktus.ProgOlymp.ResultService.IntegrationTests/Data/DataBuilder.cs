using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using GRPC = Texnokaktus.ProgOlymp.Common.Contracts.Grpc;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal class DataBuilder : IDataBuilder
{
    private readonly GRPC.Results.ResultService.ResultServiceClient _client;
    private readonly Dictionary<ContestStageKey, ContestStageBuilder> _childBuilders;

    private DataBuilder(GRPC.Results.ResultService.ResultServiceClient client)
    {
        _client = client;
        _childBuilders = [];
    }

    public static DataBuilder ForClient(GRPC.Results.ResultService.ResultServiceClient client) => new(client);

    public DataBuilder AddContestStage(
        string contestName,
        ContestStage stage,
        long stageId,
        Action<IContestStageBuilder>? builderAction = null
    )
    {
        var contestStageKey = new ContestStageKey(contestName, stage);
        if (!_childBuilders.TryGetValue(contestStageKey, out var builder))
        {
            builder = new(_client, contestName, stage, stageId);
            _childBuilders.Add(contestStageKey, builder);
        }
        
        builderAction?.Invoke(builder);

        return this;
    }

    public async Task BuildAsync()
    {
        foreach (var (_, contestStageBuilder) in _childBuilders)
            await contestStageBuilder.BuildAsync();
    }

    private readonly record struct ContestStageKey(string Name, ContestStage Stage);
}
