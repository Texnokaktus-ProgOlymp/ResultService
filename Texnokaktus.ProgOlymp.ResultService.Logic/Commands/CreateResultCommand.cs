using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands;

public record CreateResultCommand(int ContestId, ContestStage Stage, string Alias, int ParticipantId, decimal BaseScore);
