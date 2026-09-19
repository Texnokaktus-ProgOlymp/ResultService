using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class ResultNotFoundException(string contestName,
                                     ContestStage contestStage,
                                     string alias,
                                     int participantId,
                                     Exception? innerException = null)
    : NotFoundException($"The problem {alias} result for participant {participantId} in the contest {contestName} {contestStage} stage was not found", innerException);
