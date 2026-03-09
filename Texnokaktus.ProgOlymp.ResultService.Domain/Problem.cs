namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record Problem
{
    public required int Id { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
}
