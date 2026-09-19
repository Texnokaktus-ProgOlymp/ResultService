using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;
using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;

public interface IResultService
{
    Task<ContestResults?> GetResultsAsync(string contestName, ContestStage stage, CancellationToken cancellationToken);
}
