using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class ParticipantIdQueryHandler(IParticipantServiceClient client) : IQueryHandler<ParticipantIdQuery, int?>
{
    public Task<int?> HandleAsync(ParticipantIdQuery query, CancellationToken cancellationToken = default) =>
        client.GetParticipantIdAsync(query.ContestId, query.UserId);
}
