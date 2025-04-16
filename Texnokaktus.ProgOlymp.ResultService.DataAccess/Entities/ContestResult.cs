namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ContestResult(int Id, int ContestId, ContestStage Stage, long StageId, bool Published)
{
    public ICollection<Problem> Problems { get; set; }
}
