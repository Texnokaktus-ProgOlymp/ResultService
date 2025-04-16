using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ProblemAlreadyExistsException(int contestId,
                                           ContestStage contestStage,
                                           string alias,
                                           Exception? innerException = null)
    : AlreadyExistsException($"The contest {contestId} {contestStage} stage already has problem {alias}", innerException);
