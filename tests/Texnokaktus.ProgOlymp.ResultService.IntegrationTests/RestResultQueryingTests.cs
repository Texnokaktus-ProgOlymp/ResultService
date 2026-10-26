using System.Net;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class RestResultQueryingTests : SetupBase
{
    [Test]
    public async Task PublishedResults_AllowAnonymousAccessAndReturnPublicJson()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("B", "Problem B", problem => problem.AddResult(1, 0m))
                .AddProblem("A", "Problem A", problem => problem
                    .AddResult(1, 10.25m, result => result.AddAdjustment(2.50m, "Private appeal comment").AddAdjustment(-0.75m))
                    .AddResult(2, 12m)
                    .AddResult(3, 200m)
                    .AddResult(4, 5m))
                .DisqualifyParticipant(3, "Private disqualification reason")
                .MarkPublished())
            .BuildAsync();
        SubstituteParticipant(3, "Default", "Disqualified", "Participant", null);
        SubstituteParticipant(2, "Default", "Second", "Participant", null);
        SubstituteParticipant(1, "Default", "First", "Participant", "Patronym");
        SubstituteParticipant(4, "Other", "Other", "Participant", null);

        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results");

        await HttpResponseAssert.MatchesJsonAsync(response, """
            {
              "problems": [{"alias":"A","name":"Problem A"},{"alias":"B","name":"Problem B"}],
              "resultGroups": [
                {
                  "name":"Default",
                  "rows":[
                    {
                      "place":1,"participant":{"name":"Second Participant","grade":11},
                      "problemResults":{"A":{"score":{"baseScore":12,"adjustmentsSum":null,"totalScore":12}},"B":{"score":null}},
                      "totalScore":12,"isDisqualified":false
                    },
                    {
                      "place":1,"participant":{"name":"First Participant Patronym","grade":11},
                      "problemResults":{"A":{"score":{"baseScore":10.25,"adjustmentsSum":1.75,"totalScore":12}},"B":{"score":{"baseScore":0,"adjustmentsSum":null,"totalScore":0}}},
                      "totalScore":12,"isDisqualified":false
                    },
                    {
                      "place":null,"participant":{"name":"Disqualified Participant","grade":11},
                      "problemResults":{"A":{"score":{"baseScore":200,"adjustmentsSum":null,"totalScore":200}},"B":{"score":null}},
                      "totalScore":null,"isDisqualified":true
                    }
                  ]
                },
                {
                  "name":"Other",
                  "rows":[{
                    "place":1,"participant":{"name":"Other Participant","grade":11},
                    "problemResults":{"A":{"score":{"baseScore":5,"adjustmentsSum":null,"totalScore":5}},"B":{"score":null}},
                    "totalScore":5,"isDisqualified":false
                  }]
                }
              ]
            }
            """);
    }

    [TestCase("missing", "Preliminary")]
    [TestCase("test", "Final")]
    public async Task MissingContestOrStage_ReturnsNotFound(string contestName, string stage)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, builder => builder.MarkPublished())
            .BuildAsync();
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync($"/api/contests/{contestName}/{stage}/results");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [Test]
    public async Task UnpublishedResults_ReturnNotFoundWithoutBody()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Unpublished problem", problem => problem.AddResult(1, 100m)))
            .BuildAsync();
        SubstituteParticipant(1, "Default", "First", "Participant", null);
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [Test]
    public async Task PublishedStageWithoutProblems_ReturnsEmptyProblemsAndGroupRows()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage.MarkPublished())
            .BuildAsync();
        SubstituteParticipant(1, "Default", "First", "Participant", null);
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results");

        await HttpResponseAssert.MatchesJsonAsync(response,
            """{"problems":[],"resultGroups":[{"name":"Default","rows":[]}]}""");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PublishedProblemsWithoutVisibleResults_ReturnEmptyRows(bool hasStoredResults)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem =>
                {
                    if (hasStoredResults)
                        problem.AddResult(2, 100m);
                }).MarkPublished())
            .BuildAsync();
        SubstituteParticipant(1, "Default", "First", "Participant", null);
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results");

        await HttpResponseAssert.MatchesJsonAsync(response,
            """{"problems":[{"alias":"A","name":"Problem A"}],"resultGroups":[{"name":"Default","rows":[]}]}""");
    }

    [Test]
    public async Task NoParticipantGroups_ReturnsProblemsAndEmptyGroups()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem => problem.AddResult(1, 100m)).MarkPublished())
            .BuildAsync();
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results");

        await HttpResponseAssert.MatchesJsonAsync(response,
            """{"problems":[{"alias":"A","name":"Problem A"}],"resultGroups":[]}""");
    }

    [TestCase("not-a-stage")]
    [TestCase("preliminary")]
    [TestCase("final")]
    [TestCase("2")]
    [TestCase("-1")]
    public async Task InvalidStage_ReturnsBadRequest(string stage)
    {
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync($"/api/contests/test/{stage}/results");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        if (int.TryParse(stage, out _))
            await HttpResponseAssert.IsValidationProblemAsync(response, "stage");
    }

    private static void SubstituteParticipant(int id, string group, string lastName, string firstName, string? patronym) =>
        SubstituteExtensions.Participants.SubstituteParticipant("test", group, new()
        {
            Id = id,
            Grade = 11,
            Name = new() { LastName = lastName, FirstName = firstName, Patronym = patronym }
        });
}
