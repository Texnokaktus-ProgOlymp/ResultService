using Texnokaktus.ProgOlymp.ResultService.Logic.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

internal interface IParticipantService
{
    Task<ContestData> GerParticipantGroups(int contestId);
}
