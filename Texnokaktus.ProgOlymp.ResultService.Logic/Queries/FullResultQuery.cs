using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries;

public record FullResultQuery(int ContestId, ContestStage Stage);
