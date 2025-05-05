using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ProblemNotFoundException(int contestId, ContestStage contestStage, string alias, Exception? innerException = null)
    : NotFoundException($"The problem {alias} was not found in the contest {contestId} {contestStage} stage", innerException);
