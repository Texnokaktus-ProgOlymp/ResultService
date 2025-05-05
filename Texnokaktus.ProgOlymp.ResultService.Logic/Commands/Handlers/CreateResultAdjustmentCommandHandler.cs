using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

public class CreateResultAdjustmentCommandHandler(AppDbContext dbContext) : ICommandHandler<CreateResultAdjustmentCommand, int>
{
    public async Task<int> HandleAsync(CreateResultAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .ThenInclude(problemResult => problemResult.Adjustments)
                                           .FirstOrDefaultAsync(contestResult => contestResult.ContestId == command.ContestId
                                                                              && contestResult.Stage == command.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(command.ContestId, command.Stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(command.ContestId, command.Stage);

        var problem = contestResult.Problems.FirstOrDefault(problem => problem.Alias == command.Alias)
                   ?? throw new ProblemNotFoundException(command.ContestId, command.Stage, command.Alias);

        var result = problem.Results.FirstOrDefault(result => result.ParticipantId == command.ParticipantId)
                  ?? throw new ResultNotFoundException(command.ContestId, command.Stage, command.Alias, command.ParticipantId);

        var entity = new ScoreAdjustment { Adjustment = command.Adjustment, Comment = command.Comment };

        result.Adjustments.Add(entity);

        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
