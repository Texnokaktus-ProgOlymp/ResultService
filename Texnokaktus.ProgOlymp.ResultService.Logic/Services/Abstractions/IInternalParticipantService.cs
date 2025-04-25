using Texnokaktus.ProgOlymp.ResultService.Logic.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

internal interface IInternalParticipantService
{
    Task<ContestData> GerParticipantGroups(int contestId);
}
