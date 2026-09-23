using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using GRPC = Texnokaktus.ProgOlymp.Common.Contracts.Grpc;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

internal interface IDataBuilder
{
    Task BuildAsync();
}

public class DataBuilder : IDataBuilder
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
        Action<ContestStageBuilder>? builderAction = null
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

public class ContestStageBuilder : IDataBuilder
{
    private readonly GRPC.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly long _stageId;
    private readonly Dictionary<string, ProblemBuilder> _childBuilders;

    private bool _isPublished = false;

    public ContestStageBuilder(
        GRPC.Results.ResultService.ResultServiceClient client,
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

    public ContestStageBuilder AddProblem(string alias, string name, Action<ProblemBuilder>? builderAction = null)
    {
        if (!_childBuilders.TryGetValue(alias, out var builder))
        {
            builder = new(_client, _contestName, _contestStage, alias, name);
            _childBuilders.Add(alias, builder);
        }

        builderAction?.Invoke(builder);
        return this;
    }

    public ContestStageBuilder MarkPublished(bool value = true)
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

public class ProblemBuilder : IDataBuilder
{
    private readonly GRPC.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly string _problemAlias;
    private readonly string _problemName;
    private readonly Dictionary<int, ResultBuilder> _childBuilders;

    public ProblemBuilder(
        GRPC.Results.ResultService.ResultServiceClient client,
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

    public ProblemBuilder AddResult(int participantId, decimal baseScore, Action<ResultBuilder>? builderAction = null)
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

public class ResultBuilder : IDataBuilder
{
    private readonly GRPC.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly string _problemAlias;
    private readonly int _participantId;
    private readonly decimal _baseScore;
    private readonly List<ResultAdjustment> _adjustments;
    
    public ResultBuilder(
        GRPC.Results.ResultService.ResultServiceClient client,
        string contestName,
        ContestStage contestStage,
        string problemAlias,
        int participantId,
        decimal baseScore
    )
    {
        _client = client;
        _contestName = contestName;
        _contestStage = contestStage;
        _problemAlias = problemAlias;
        _participantId = participantId;
        _baseScore = baseScore;
        _adjustments = [];
    }

    public ResultBuilder AddAdjustment(decimal adjustment, string? comment = null)
    {
        _adjustments.Add(new(adjustment, comment));
        return this;
    }

    public async Task BuildAsync()
    {
        await _client.AddResultAsync(
            new()
            {
                ContestName = _contestName,
                Stage = _contestStage,
                Alias = _problemAlias,
                BaseScore = _baseScore,
                ParticipantId = _participantId
            }
        );

        foreach (var (adjustment, comment) in _adjustments)
        {
            await _client.AddResultAdjustmentAsync(
                new()
                {
                    ContestName = _contestName,
                    Stage = _contestStage,
                    Alias = _problemAlias,
                    ParticipantId = _participantId,
                    Adjustment = adjustment,
                    Comment = comment
                }
            );
        }
    }

    private readonly record struct ResultAdjustment(decimal Adjustment, string? Comment);
}
