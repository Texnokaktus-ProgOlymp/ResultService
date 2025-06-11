using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands;

public record CreateProblemCommand(string ContestName, ContestStage Stage, string Alias, string Name);
