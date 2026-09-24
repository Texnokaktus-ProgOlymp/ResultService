namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal interface IResultBuilder
{
    IResultBuilder AddAdjustment(decimal adjustment, string? comment = null);
}
