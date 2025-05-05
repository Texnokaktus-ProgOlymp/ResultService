using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

public class CreateProblemCommandHandler(AppDbContext dbContext) : ICommandHandler<CreateProblemCommand>
{
    public async Task HandleAsync(CreateProblemCommand command, CancellationToken cancellationToken = default)
    {
        var contestResult = await dbContext.ContestResults
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestId == command.ContestId
                                                                       && result.Stage == command.Stage,
                                                                cancellationToken)
                         ?? throw new ContestNotFoundException(command.ContestId, command.Stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(command.ContestId, command.Stage);

        if (contestResult.Problems.Any(problem => problem.Alias == command.Alias))
            throw new ProblemAlreadyExistsException(command.ContestId, command.Stage, command.Alias);

        contestResult.Problems.Add(new()
        {
            Alias = command.Alias,
            Name = command.Name
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
