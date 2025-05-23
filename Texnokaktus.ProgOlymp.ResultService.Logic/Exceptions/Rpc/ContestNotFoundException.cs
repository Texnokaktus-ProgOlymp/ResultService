using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ContestNotFoundException(int contestId, ContestStage contestStage, Exception? innerException = null)
    : NotFoundException($"The contest {contestId} {contestStage} stage was not found", innerException);

