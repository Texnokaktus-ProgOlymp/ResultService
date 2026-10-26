using Microsoft.AspNetCore.Mvc;

namespace Texnokaktus.ProgOlymp.ResultService.Models.Requests;

public record GetResultsRequest(
    [property: FromRoute] string ContestName,
    [property: FromRoute] ContestStage Stage
);
