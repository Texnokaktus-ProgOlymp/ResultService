using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.QueryHandlers;

internal class ContestParticipantsQueryHandler(Common.Contracts.Grpc.Participants.ParticipantService.ParticipantServiceClient participantServiceClient) : IQueryHandler<ContestParticipantsQuery, IEnumerable<ParticipantGroup>>
{
    public async Task<IEnumerable<ParticipantGroup>> HandleAsync(ContestParticipantsQuery query, CancellationToken cancellationToken = default)
    {
        var response = await participantServiceClient.GetContestParticipantsAsync(new()
                                                                                  {
                                                                                      ContestId = query.ContestId
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

    private static string MapName(this Common.Contracts.Grpc.Participants.Name name) =>
        string.Join(" ", new[] { name.LastName, name.FirstName, name.Patronym }.Where(namePart => namePart is not null));
}
