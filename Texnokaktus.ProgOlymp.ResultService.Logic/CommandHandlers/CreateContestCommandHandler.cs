using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.CommandHandlers;

internal class CreateContestCommandHandler(AppDbContext dbContext) : ICommandHandler<CreateContestCommand>
{
    public async Task HandleAsync(CreateContestCommand command, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ContestResults.AnyAsync(contestResult => contestResult.ContestId == command.ContestId
                                                                  && contestResult.Stage == command.Stage,
                                                    cancellationToken))
            throw new ContestAlreadyExistsException(command.ContestId, command.Stage);

        if (await dbContext.ContestResults.AnyAsync(result => result.StageId == command.StageId,
                                                    cancellationToken))
            throw new ContestAlreadyExistsException(command.StageId);

        dbContext.ContestResults.Add(new()
        {
            ContestId = command.ContestId,
            Stage = command.Stage,
            StageId = command.StageId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
