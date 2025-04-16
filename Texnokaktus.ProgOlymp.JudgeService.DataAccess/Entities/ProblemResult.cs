namespace Texnokaktus.ProgOlymp.JudgeService.DataAccess.Entities;

public record ProblemResult(int Id, int ProblemId, int ParticipantId, decimal BaseScore)
{
    public ICollection<ScoreAdjustment> Adjustments { get; set; }
}
