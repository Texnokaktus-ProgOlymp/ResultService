using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ContestNotFoundException(int contestId, ContestStage contestStage, Exception? innerException = null)
    : NotFoundException($"The contest {contestId} {contestStage} stage was not found", innerException);
