using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ParticipantResultsNotFound(int contestId, ContestStage contestStage, int participantId, Exception? innerException = null)
    : NotFoundException($"The participant {participantId} results were not found in the contest {contestId} {contestStage} stage", innerException);
