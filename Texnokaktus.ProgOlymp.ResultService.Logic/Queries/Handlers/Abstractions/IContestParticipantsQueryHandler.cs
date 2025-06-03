using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

internal interface IContestParticipantsQueryHandler : IQueryHandler<ContestParticipantsQuery, IEnumerable<ParticipantGroup>>;
