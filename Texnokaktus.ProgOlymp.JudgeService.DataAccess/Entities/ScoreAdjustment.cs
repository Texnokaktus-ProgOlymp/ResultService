namespace Texnokaktus.ProgOlymp.JudgeService.DataAccess.Entities;

public record ScoreAdjustment(int Id, int ProblemResultId, decimal Adjustment, string? Comment);
