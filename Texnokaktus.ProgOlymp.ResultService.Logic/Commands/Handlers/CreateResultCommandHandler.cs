using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

public class CreateResultCommandHandler(AppDbContext dbContext) : ICommandHandler<CreateResultCommand>
{
    public async Task HandleAsync(CreateResultCommand command, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .FirstOrDefaultAsync(contestResult => contestResult.ContestId == command.ContestId
                                                                              && contestResult.Stage == command.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(command.ContestId, command.Stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(command.ContestId, command.Stage);

        var problem = contestResult.Problems.FirstOrDefault(problem => problem.Alias == command.Alias)
                   ?? throw new ProblemNotFoundException(command.ContestId, command.Stage, command.Alias);

        if (problem.Results.Any(result => result.ParticipantId == command.ParticipantId))
            throw new ResultAlreadyExistsException(command.ContestId, command.Stage, command.Alias, command.ParticipantId);

        problem.Results.Add(new()
        {
            ParticipantId = command.ParticipantId,
            BaseScore = command.BaseScore
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
