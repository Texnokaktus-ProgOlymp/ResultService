using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ContestReadonlyException(int contestId, ContestStage contestStage, Exception? innerException = null)
    : FailedPreconditionException($"The contest {contestId} {contestStage} stage is readonly", innerException);
