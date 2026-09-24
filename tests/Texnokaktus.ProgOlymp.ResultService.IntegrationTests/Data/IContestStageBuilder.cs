namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

internal interface IContestStageBuilder
{
    IContestStageBuilder AddProblem(string alias, string name, Action<IProblemBuilder>? builderAction = null);
    IContestStageBuilder MarkPublished(bool value = true);
}
