using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

internal class CreateResultCommandHandler(AppDbContext dbContext) : ICreateResultCommandHandler
{
    public async Task HandleAsync(CreateResultCommand command, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .FirstOrDefaultAsync(contestResult => contestResult.ContestName == command.ContestName
                                                                              && contestResult.Stage == command.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(command.ContestName, command.Stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(command.ContestName, command.Stage);

        var problem = contestResult.Problems.FirstOrDefault(problem => problem.Alias == command.Alias)
                   ?? throw new ProblemNotFoundException(command.ContestName, command.Stage, command.Alias);

        if (problem.Results.Any(result => result.ParticipantId == command.ParticipantId))
            throw new ResultAlreadyExistsException(command.ContestName, command.Stage, command.Alias, command.ParticipantId);

        problem.Results.Add(new()
        {
            ParticipantId = command.ParticipantId,
            BaseScore = command.BaseScore
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
