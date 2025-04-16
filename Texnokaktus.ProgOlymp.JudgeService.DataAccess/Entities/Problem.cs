namespace Texnokaktus.ProgOlymp.JudgeService.DataAccess.Entities;

public record Problem(int Id, int ContestResultId, string Alias, string Name)
{
    public ICollection<ProblemResult> Results { get; set; }
}
