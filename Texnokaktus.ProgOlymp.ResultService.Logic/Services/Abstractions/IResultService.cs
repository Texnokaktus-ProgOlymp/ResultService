using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;
using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

public interface IResultService
{
    Task<ContestResults?> GetResultsAsync(int contestId, ContestStage stage);
}
