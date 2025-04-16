namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ProblemResult(int Id, int ProblemId, int ParticipantId, decimal BaseScore)
{
    public ICollection<ScoreAdjustment> Adjustments { get; set; }
}
