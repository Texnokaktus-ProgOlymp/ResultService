using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ContestNotFoundException : NotFoundException
{
    public ContestNotFoundException(int contestId, ContestStage contestStage, Exception? innerException = null) : base($"The contest {contestId} {contestStage} stage was not found", innerException)
    {
    }

    public ContestNotFoundException(long contestStageId, Exception? innerException = null) : base($"The contest stage {contestStageId} was not found", innerException)
    {
        
    }

}
