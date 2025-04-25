using Grpc.Core;
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

    public async Task<int?> GetParticipantIdAsync(int contestId, int userId)
    {
        try
        {
            var request = new GetParticipantIdRequest
            {
                ContestId = contestId,
                UserId = userId
            };

            var response = await client.GetParticipantIdAsync(request);

            return response.ParticipantId;
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
