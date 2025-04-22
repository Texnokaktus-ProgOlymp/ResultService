using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services;

internal class ParticipantService(IParticipantServiceClient client) : IParticipantService
{
    public async Task<ContestData> GerParticipantGroups(int contestId)
    {
        var response = await client.GetContestParticipantsAsync(contestId);

        return new(response.ContestName,
                   response.ParticipantGroups.Select(group => group.MapParticipantGroup()).ToArray());
    }
}

file static class MappingExtensions
{
    public static ParticipantGroup MapParticipantGroup(this Common.Contracts.Grpc.Participants.ParticipantGroup participantGroup) =>
        new(participantGroup.Name, participantGroup.Participants.Select(participant => participant.MapParticipant()).ToArray());

    private static Participant MapParticipant(this Common.Contracts.Grpc.Participants.Participant participant) =>
        new(participant.Id, participant.Name.MapName(), participant.Grade);

    private static string MapName(this Common.Contracts.Grpc.Participants.Name name) =>
        string.Join(" ", new[] { name.LastName, name.FirstName, name.Patronym }.Where(namePart => namePart is not null));
}
