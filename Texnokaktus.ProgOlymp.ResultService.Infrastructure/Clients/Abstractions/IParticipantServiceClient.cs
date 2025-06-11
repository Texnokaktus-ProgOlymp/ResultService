using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;

namespace Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;

public interface IParticipantServiceClient
{
    Task<GetContestParticipantsResponse> GetContestParticipantsAsync(string contestName);
    Task<int?> GetParticipantIdAsync(string contestName, int userId);
}
