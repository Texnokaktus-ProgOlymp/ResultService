using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ContestAlreadyExistsException : AlreadyExistsException
{
    public ContestAlreadyExistsException(int contestId, ContestStage contestStage, Exception? innerException = null) : base($"The contest {contestId} {contestStage} stage already exists", innerException)
    {
    }

    public ContestAlreadyExistsException(long contestStageId, Exception? innerException = null) : base($"The contest stage {contestStageId} already exists", innerException)
    {
    }
}
