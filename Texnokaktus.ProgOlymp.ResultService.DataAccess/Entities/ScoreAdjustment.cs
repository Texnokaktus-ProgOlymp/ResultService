namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ScoreAdjustment(int Id, int ProblemResultId, decimal Adjustment, string? Comment);
