using Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;

public class ProblemNotFoundException(string contestName, ContestStage contestStage, string alias, Exception? innerException = null)
    : NotFoundException($"The problem {alias} was not found in the contest {contestName} {contestStage} stage", innerException);
