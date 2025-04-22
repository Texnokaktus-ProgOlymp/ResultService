using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Models;

internal record ParticipantGroup(string Name, IReadOnlyCollection<Participant> Participants);
