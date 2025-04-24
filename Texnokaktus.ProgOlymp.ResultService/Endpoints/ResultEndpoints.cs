using Texnokaktus.ProgOlymp.ResultService.Models;
using Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Endpoints;

public static class ResultEndpoints
{
    public static IEndpointRouteBuilder MapResultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contests/{contestId:int}/{stage}/results");

        group.MapGet("/",
                     (int contestId, ContestStage stage, IResultService resultService) =>
                         resultService.GetContestResultsAsync(contestId, stage));
        
        return app;
    }
}
