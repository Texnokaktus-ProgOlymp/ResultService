using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.CommandHandlers;

public record CreateProblemCommand(int ContestId, ContestStage Stage, string Alias, string Name);
