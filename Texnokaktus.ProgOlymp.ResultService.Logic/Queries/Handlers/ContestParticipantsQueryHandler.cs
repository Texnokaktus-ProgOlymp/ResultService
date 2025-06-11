using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;
using Participant = Texnokaktus.ProgOlymp.ResultService.Domain.Participant;
using ParticipantGroup = Texnokaktus.ProgOlymp.ResultService.Logic.Models.ParticipantGroup;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class ContestParticipantsQueryHandler(ParticipantService.ParticipantServiceClient participantServiceClient) : IContestParticipantsQueryHandler
{
    public async Task<IEnumerable<ParticipantGroup>> HandleAsync(ContestParticipantsQuery query, CancellationToken cancellationToken = default)
    {
        var response = await participantServiceClient.GetContestParticipantsAsync(new()
                                                                                  {
                                                                                      ContestName = query.ContestName
                                                                                  },
                                                                                  cancellationToken: cancellationToken);
        return response.ParticipantGroups.Select(group => group.MapParticipantGroup());
    }
}

file static class MappingExtensions
{
    public static ParticipantGroup MapParticipantGroup(this Common.Contracts.Grpc.Participants.ParticipantGroup participantGroup) =>
        new(participantGroup.Name, participantGroup.Participants.Select(participant => participant.MapParticipant()).ToArray());

    private static Participant MapParticipant(this Common.Contracts.Grpc.Participants.Participant participant) =>
        new(participant.Id, participant.Name.MapName(), participant.Grade);

    private static string MapName(this Name name) =>
        string.Join(" ", new[] { name.LastName, name.FirstName, name.Patronym }.Where(namePart => namePart is not null));
}
