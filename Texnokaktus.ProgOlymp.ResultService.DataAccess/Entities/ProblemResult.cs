namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ProblemResult
{
    public int Id { get; init; }
    public int ProblemId { get; init; }
    public int ParticipantId { get; init; }
    public decimal BaseScore { get; init; }
    public ICollection<ScoreAdjustment> Adjustments { get; init; }
}
