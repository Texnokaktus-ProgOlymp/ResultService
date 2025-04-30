using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;
using Texnokaktus.ProgOlymp.ResultService.Logic.QueryHandlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic;

public static class DiExtensions
{
    public static IServiceCollection AddLogicServices(this IServiceCollection services) =>
        services.AddScoped<IParticipantService, ParticipantService>()
                .AddQueryHandler<ContestParticipantsQueryHandler, ContestParticipantsQuery, IEnumerable<ParticipantGroup>>(ServiceLifetime.Scoped)
                .AddQueryHandler<FullResultQueryHandler, FullResultQuery, ContestResults?>(ServiceLifetime.Scoped);
}
