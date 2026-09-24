using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal class ProblemBuilder : IDataBuilder, IProblemBuilder
{
    private readonly Common.Contracts.Grpc.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly string _problemAlias;
    private readonly string _problemName;
    private readonly Dictionary<int, ResultBuilder> _childBuilders;

    public ProblemBuilder(
        Common.Contracts.Grpc.Results.ResultService.ResultServiceClient client,
        string contestName,
        ContestStage contestStage,
        string problemAlias,
        string problemName
    )
    {
        _client = client;
        _contestName = contestName;
        _contestStage = contestStage;
        _problemAlias = problemAlias;
        _problemName = problemName;
        _childBuilders = [];
    }

    public IProblemBuilder AddResult(int participantId, decimal baseScore, Action<IResultBuilder>? builderAction = null)
    {
        if (!_childBuilders.TryGetValue(participantId, out var builder))
        {
            builder = new(_client, _contestName, _contestStage, _problemAlias, participantId, baseScore);
            _childBuilders.Add(participantId, builder);
        }

        builderAction?.Invoke(builder);
        return this;
    }

    public async Task BuildAsync()
    {
        await _client.AddProblemAsync(
            new()
            {
                ContestName = _contestName,
                Stage = _contestStage,
                Alias = _problemAlias,
                Name = _problemName
            }
        );

        foreach (var (_, resultBuilder) in _childBuilders)
            await resultBuilder.BuildAsync();
    }
}
