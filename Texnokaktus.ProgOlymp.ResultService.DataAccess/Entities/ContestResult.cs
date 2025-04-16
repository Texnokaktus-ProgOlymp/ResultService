namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ContestResult
{
    public int Id { get; init; }
    public int ContestId { get; init; }
    public ContestStage Stage { get; init; }
    public long StageId { get; init; }
    public bool Published { get; set; }
    public ICollection<Problem> Problems { get; init; }
}
