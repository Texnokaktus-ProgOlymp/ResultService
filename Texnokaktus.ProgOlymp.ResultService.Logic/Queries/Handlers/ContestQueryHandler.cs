using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

public class ContestQueryHandler(AppDbContext dbContext) : IQueryHandler<ContestQuery, Contest>
{
    public async Task<Contest> HandleAsync(ContestQuery query, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .AsNoTracking()
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestId == query.ContestId
                                                                       && result.Stage == query.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(query.ContestId, query.Stage);

        return new(contestResult.Id,
                   contestResult.Stage,
                   contestResult.StageId,
                   contestResult.Problems.Select(problem => new Problem(problem.Id,
                                                                        problem.Alias,
                                                                        problem.Name)));
    }
}


