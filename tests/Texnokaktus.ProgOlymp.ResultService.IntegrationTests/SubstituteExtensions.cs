using NSubstitute;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public static class SubstituteExtensions
{
    public static readonly ParticipantSubstitution Participants = new();

    public sealed class ParticipantSubstitution
    {
        private readonly Dictionary<ContestStageKey, Dictionary<string, List<Participant>>> _participants = new();

        public void Init(ParticipantService.ParticipantServiceClient client)
        {
            client.GetContestParticipantsAsync(
                       Arg.Any<GetContestParticipantsRequest>(),
                       cancellationToken: Arg.Any<CancellationToken>()
                   )
                  .Returns(info =>
                       {
                           var request = info.Arg<GetContestParticipantsRequest>();

                           var participantGroups = _participants.GetValueOrDefault(new(request.ContestName), []);

                           return new GetContestParticipantsResponse
                           {
                               ParticipantGroups =
                               {
                                   participantGroups.Select(x => new ParticipantGroup
                                       {
                                           Name = x.Key,
                                           Participants =
                                           {
                                               x.Value
                                           }
                                       }
                                   )
                               }
                           }.ToAsyncUnaryCall();
                       }
                   );
        }

        public void SubstituteParticipant(string contestName, string participantGroupName, Participant participant)
        {
            SubstituteGroup(contestName, participantGroupName);
            _participants[new(contestName)][participantGroupName].Add(participant);
        }

        public void SubstituteGroup(string contestName, string participantGroupName)
        {
            if (!_participants.TryGetValue(new(contestName), out var contestParticipants))
            {
                contestParticipants = new();
                _participants.Add(new(contestName), contestParticipants);
            }

            contestParticipants.TryAdd(participantGroupName, []);
        }

        public void Clear() => _participants.Clear();

        private readonly record struct ContestStageKey(string ContestName);
    }
}
