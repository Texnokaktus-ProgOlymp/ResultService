using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services;

internal class ParticipantService(IParticipantServiceClient client) : IParticipantService
{
    public Task<int?> GetParticipantIdAsync(int contestId, int userId) =>
        client.GetParticipantIdAsync(contestId, userId);
}
