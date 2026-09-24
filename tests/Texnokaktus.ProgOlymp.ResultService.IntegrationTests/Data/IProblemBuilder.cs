namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal interface IProblemBuilder
{
    IProblemBuilder AddResult(int participantId, decimal baseScore, Action<IResultBuilder>? builderAction = null);
}
