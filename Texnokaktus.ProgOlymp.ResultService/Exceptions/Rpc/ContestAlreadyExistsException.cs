using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ContestAlreadyExistsException : AlreadyExistsException
{
    public ContestAlreadyExistsException(string contestName, ContestStage contestStage, Exception? innerException = null) : base($"The contest {contestName} {contestStage} stage already exists", innerException)
    {
    }

    public ContestAlreadyExistsException(long contestStageId, Exception? innerException = null) : base($"The contest stage {contestStageId} already exists", innerException)
    {
    }
}
