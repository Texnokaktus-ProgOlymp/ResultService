using Microsoft.AspNetCore.Http.HttpResults;
using Texnokaktus.ProgOlymp.ResultService.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;

public interface IResultService
{
    Task<Results<Ok<ContestResults>, NotFound>> GetContestResultsAsync(int contestId, ContestStage stage);
}
