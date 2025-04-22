using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients;

public class ParticipantServiceClient(ParticipantService.ParticipantServiceClient client) : IParticipantServiceClient
{
    public async Task<GetContestParticipantsResponse> GetContestParticipantsAsync(int contestId)
    {
        var request = new GetContestParticipantsRequest
        {
            ContestId = contestId
        };

        return await client.GetContestParticipantsAsync(request);
    }
}
