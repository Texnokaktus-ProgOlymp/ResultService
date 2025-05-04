using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic;

public static class DiExtensions
{
    public static IServiceCollection AddLogicServices(this IServiceCollection services) =>
        services.AddScoped<IParticipantService, ParticipantService>()
                .AddCommandHandler<CreateContestCommandHandler, CreateContestCommand>(ServiceLifetime.Scoped)
                .AddCommandHandler<CreateProblemCommandHandler, CreateProblemCommand>(ServiceLifetime.Scoped)
                .AddCommandHandler<CreateResultCommandHandler, CreateResultCommand>(ServiceLifetime.Scoped)
                .AddQueryHandler<ContestParticipantsQueryHandler, ContestParticipantsQuery, IEnumerable<ParticipantGroup>>(ServiceLifetime.Scoped)
                .AddQueryHandler<FullResultQueryHandler, FullResultQuery, ContestResults?>(ServiceLifetime.Scoped);
}
