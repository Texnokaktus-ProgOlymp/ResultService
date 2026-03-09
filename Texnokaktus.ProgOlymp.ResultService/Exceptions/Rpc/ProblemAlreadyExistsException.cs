using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ProblemAlreadyExistsException(string contestName,
                                           ContestStage contestStage,
                                           string alias,
                                           Exception? innerException = null)
    : AlreadyExistsException($"The contest {contestName} {contestStage} stage already has problem {alias}", innerException);
