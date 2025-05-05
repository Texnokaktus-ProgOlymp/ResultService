using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record Contest(int Id, ContestStage Stage, long StageId, IEnumerable<Problem> Problems);
