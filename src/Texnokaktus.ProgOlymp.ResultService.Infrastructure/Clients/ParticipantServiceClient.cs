using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients;

public class ParticipantServiceClient(ParticipantService.ParticipantServiceClient client) : IParticipantServiceClient
{
    public async Task<GetContestParticipantsResponse> GetContestParticipantsAsync(string contestName)
    {
        var request = new GetContestParticipantsRequest
        {
            ContestName = contestName
        };

        return await client.GetContestParticipantsAsync(request);
    }

    public async Task<int?> GetParticipantIdAsync(string contestName, int userId)
    {
        try
        {
            var request = new GetParticipantIdRequest
            {
                ContestName = contestName,
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
