namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ContestResult
{
    public int Id { get; init; }
    public required string ContestName { get; init; }
    public required ContestStage Stage { get; init; }
    public required long StageId { get; init; }
    public bool Published { get; set; }
    public ICollection<Problem> Problems { get; init; }
}
