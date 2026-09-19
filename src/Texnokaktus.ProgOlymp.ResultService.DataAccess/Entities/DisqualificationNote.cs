namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record DisqualificationNote
{
    public int Id { get; init; }
    public int ContestResultId { get; init; }
    public int ParticipantId { get; init; }
    public string? Reason { get; set; }
}
