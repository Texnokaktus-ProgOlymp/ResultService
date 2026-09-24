using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal class ContestStageBuilder : IDataBuilder, IContestStageBuilder
{
    private readonly Common.Contracts.Grpc.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly long _stageId;
    private readonly Dictionary<string, ProblemBuilder> _childBuilders;

    private bool _isPublished;

    public ContestStageBuilder(
        Common.Contracts.Grpc.Results.ResultService.ResultServiceClient client,
        string contestName,
        ContestStage contestStage,
        long stageId
    )
    {
        _client = client;
        _contestName = contestName;
        _contestStage = contestStage;
        _stageId = stageId;
        _childBuilders = [];
    }

    public IContestStageBuilder AddProblem(string alias, string name, Action<IProblemBuilder>? builderAction = null)
    {
        if (!_childBuilders.TryGetValue(alias, out var builder))
        {
            builder = new(_client, _contestName, _contestStage, alias, name);
            _childBuilders.Add(alias, builder);
        }

        builderAction?.Invoke(builder);
        return this;
    }

    public IContestStageBuilder MarkPublished(bool value = true)
    {
        _isPublished = value;
        return this;
    }

    public async Task BuildAsync()
    {
        await _client.AddContestAsync(
            new()
            {
                ContestName = _contestName,
                Stage = _contestStage,
                StageId = _stageId
            }
        );

        foreach (var (_, problemBuilder) in _childBuilders)
            await problemBuilder.BuildAsync();

        if (_isPublished)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var contestResult = await context.ContestResults.SingleAsync(result => result.Id == 1);
            contestResult.Published = true;
            await context.SaveChangesAsync();
        }
    }
}
