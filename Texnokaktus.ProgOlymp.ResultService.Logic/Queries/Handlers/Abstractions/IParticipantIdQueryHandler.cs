using Texnokaktus.ProgOlymp.Cqrs;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

public interface IParticipantIdQueryHandler : IQueryHandler<ParticipantIdQuery, int?>;
