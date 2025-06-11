using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands;

public record CreateResultAdjustmentCommand(string ContestName, ContestStage Stage, string Alias, int ParticipantId, decimal Adjustment, string? Comment);
