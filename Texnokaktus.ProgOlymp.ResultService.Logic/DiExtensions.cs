using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic;

public static class DiExtensions
{
    public static IServiceCollection AddLogicServices(this IServiceCollection services) =>
        services.AddScoped<IContestParticipantsQueryHandler, ContestParticipantsQueryHandler>()
                .AddScoped<IFullResultQueryHandler, FullResultQueryHandler>();
}
