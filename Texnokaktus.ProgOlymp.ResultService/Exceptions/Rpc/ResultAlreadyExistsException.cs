using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ResultAlreadyExistsException(int contestId,
                                          ContestStage contestStage,
                                          string alias,
                                          int participantId,
                                          Exception? innerException = null)
    : AlreadyExistsException($"The problem {alias} result for participant {participantId} in the contest {contestId} {contestStage} stage already exists", innerException);
