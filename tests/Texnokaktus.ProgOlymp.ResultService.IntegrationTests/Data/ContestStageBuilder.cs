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
    private readonly Dictionary<int, string?> _disqualifications = [];

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

    public IContestStageBuilder DisqualifyParticipant(int participantId, string? reason = null)
    {
        _disqualifications[participantId] = reason;
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

        if (_isPublished || _disqualifications.Count > 0)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stage = _contestStage switch
            {
                ContestStage.Preliminary => DataAccess.Entities.ContestStage.Preliminary,
                ContestStage.Final => DataAccess.Entities.ContestStage.Final,
                _ => throw new ArgumentOutOfRangeException(nameof(_contestStage))
            };
            var contestResult = await context.ContestResults.SingleAsync(result => result.ContestName == _contestName
                                                                               && result.Stage == stage);
            contestResult.Published = _isPublished;
            foreach (var (participantId, reason) in _disqualifications)
                context.Set<DataAccess.Entities.DisqualificationNote>().Add(new()
                {
                    ContestResultId = contestResult.Id,
                    ParticipantId = participantId,
                    Reason = reason
                });
            await context.SaveChangesAsync();
        }
    }
}
