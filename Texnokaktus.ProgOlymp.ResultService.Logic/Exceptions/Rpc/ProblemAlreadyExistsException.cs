using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ProblemAlreadyExistsException(int contestId,
                                           ContestStage contestStage,
                                           string alias,
                                           Exception? innerException = null)
    : AlreadyExistsException($"The contest {contestId} {contestStage} stage already has problem {alias}", innerException);
