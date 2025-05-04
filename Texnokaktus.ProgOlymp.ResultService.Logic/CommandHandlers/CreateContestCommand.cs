using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.CommandHandlers;

public record CreateContestCommand(int ContestId, ContestStage Stage, long StageId);
