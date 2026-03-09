namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ProblemResult
{
    public required int ProblemId { get; init; }
    public required string Alias { get; init; }
    public required ResultScore? Score { get; init; }
}
