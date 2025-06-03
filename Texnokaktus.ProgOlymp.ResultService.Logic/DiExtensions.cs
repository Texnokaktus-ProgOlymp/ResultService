using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers.Abstractions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic;

public static class DiExtensions
{
    public static IServiceCollection AddLogicServices(this IServiceCollection services) =>
        services.AddScoped<ICreateContestCommandHandler, CreateContestCommandHandler>()
                .AddScoped<ICreateProblemCommandHandler, CreateProblemCommandHandler>()
                .AddScoped<ICreateResultCommandHandler, CreateResultCommandHandler>()
                .AddScoped<ICreateResultAdjustmentCommandHandler, CreateResultAdjustmentCommandHandler>()
                .AddScoped<IParticipantIdQueryHandler, ParticipantIdQueryHandler>()
                .AddScoped<IContestQueryHandler, ContestQueryHandler>()
                .AddScoped<IContestParticipantsQueryHandler, ContestParticipantsQueryHandler>()
                .AddScoped<IFullResultQueryHandler, FullResultQueryHandler>();
}
