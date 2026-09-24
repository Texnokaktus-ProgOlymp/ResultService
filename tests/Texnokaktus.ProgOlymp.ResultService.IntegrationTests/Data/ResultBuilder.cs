using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal class ResultBuilder : IDataBuilder, IResultBuilder
{
    private readonly Common.Contracts.Grpc.Results.ResultService.ResultServiceClient _client;
    private readonly string _contestName;
    private readonly ContestStage _contestStage;
    private readonly string _problemAlias;
    private readonly int _participantId;
    private readonly decimal _baseScore;
    private readonly List<ResultAdjustment> _adjustments;
    
    public ResultBuilder(
        Common.Contracts.Grpc.Results.ResultService.ResultServiceClient client,
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

    public IResultBuilder AddAdjustment(decimal adjustment, string? comment = null)
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
