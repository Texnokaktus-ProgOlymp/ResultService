using Texnokaktus.ProgOlymp.ResultService.Infrastructure.Clients.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class ParticipantIdQueryHandler(IParticipantServiceClient client) : IParticipantIdQueryHandler
{
    public Task<int?> HandleAsync(ParticipantIdQuery query, CancellationToken cancellationToken = default) =>
        client.GetParticipantIdAsync(query.ContestName, query.UserId);
}
