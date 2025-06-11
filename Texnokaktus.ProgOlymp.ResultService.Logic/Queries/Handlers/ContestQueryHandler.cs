using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class ContestQueryHandler(AppDbContext dbContext) : IContestQueryHandler
{
    public async Task<Contest> HandleAsync(ContestQuery query, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .AsNoTracking()
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestName == query.ContestName
                                                                       && result.Stage == query.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(query.ContestName, query.Stage);

        return new(contestResult.Id,
                   contestResult.Stage,
                   contestResult.StageId,
                   contestResult.Problems.Select(problem => new Problem(problem.Id,
                                                                        problem.Alias,
                                                                        problem.Name)));
    }
}


