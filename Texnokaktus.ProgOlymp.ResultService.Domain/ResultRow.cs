namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultRow(Participant Participant, IReadOnlyCollection<ProblemResult> ProblemResults)
{
    public int Place { get; private set; }
    public decimal TotalScore => ProblemResults.Sum(result => result.Score?.TotalScore) ?? 0m;

    public ResultRow WithPlace(int place)
    {
        Place = place;
        return this;
    }
}
