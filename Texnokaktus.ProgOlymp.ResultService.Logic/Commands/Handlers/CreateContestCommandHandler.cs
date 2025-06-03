using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

internal class CreateContestCommandHandler(AppDbContext dbContext) : ICreateContestCommandHandler
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
