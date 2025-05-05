using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands;

public record CreateResultAdjustmentCommand(int ContestId, ContestStage Stage, string Alias, int ParticipantId, decimal Adjustment, string? Comment);
