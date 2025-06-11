using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands;

public record CreateContestCommand(string ContestName, ContestStage Stage, long StageId);
