using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Services;

public class ResultServiceImpl(AppDbContext dbContext) : Common.Contracts.Grpc.Results.ResultService.ResultServiceBase
{
    public override async Task<Contest> GetContest(GetContestRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestId == request.ContestId
                                                                       && result.Stage == contestStage)
                         ?? throw new ContestNotFoundException(request.ContestId, contestStage);

        return new()
        {
            Id = contestResult.ContestId,
            Stage = contestResult.Stage.MapContestStage(),
            StageId = contestResult.StageId,
            Problems =
            {
                contestResult.Problems.Select(problem => new Problem
                {
                    Id = problem.Id,
                    Alias = problem.Alias,
                    Name = problem.Name
                })
            }
        };
    }
}

file static class MappingExtensions
{
    public static DataAccess.Entities.ContestStage MapContestStage(this ContestStage contestStage) =>
        contestStage switch
        {
            ContestStage.Preliminary => DataAccess.Entities.ContestStage.Preliminary,
            ContestStage.Final       => DataAccess.Entities.ContestStage.Final,
            _                        => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
        };

    public static ContestStage MapContestStage(this DataAccess.Entities.ContestStage contestStage) =>
        contestStage switch
        {
            DataAccess.Entities.ContestStage.Preliminary => ContestStage.Preliminary,
            DataAccess.Entities.ContestStage.Final       => ContestStage.Final,
            _                                            => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
        };
}
