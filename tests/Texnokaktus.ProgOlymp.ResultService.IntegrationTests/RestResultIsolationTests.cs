using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class RestResultIsolationTests : SetupBase
{
    [TestCase("other", ContestStage.Preliminary)]
    [TestCase("test", ContestStage.Final)]
    public async Task CommonAndPersonalResults_SelectRequestedContestAndStage(string otherContest, ContestStage otherStage)
    {
        var otherParticipantId = otherContest == "test" ? 2 : 1;
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Selected problem", problem => problem.AddResult(2, 10m)).MarkPublished())
            .AddContestStage(otherContest, otherStage, 1001, stage => stage
                .AddProblem("A", "Other problem", problem => problem.AddResult(otherParticipantId, 100m))
                .DisqualifyParticipant(otherParticipantId).MarkPublished())
            .BuildAsync();
        SubstituteParticipant("test", 2);
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 2);
        if (otherContest != "test")
        {
            SubstituteParticipant(otherContest, otherParticipantId);
            SubstituteExtensions.Participants.SubstituteParticipantId(otherContest, 42, otherParticipantId);
        }

        using var commonClient = Factory.CreateClient();
        using var personalClient = Factory.CreateAuthenticatedClient(42);
        await AssertResultsAsync("test", ContestStage.Preliminary, "Selected problem", 10m, false);
        await AssertResultsAsync(otherContest, otherStage, "Other problem", 100m, true);

        async Task AssertResultsAsync(string contestName, ContestStage stage, string problemName, decimal baseScore, bool disqualified)
        {
            var url = $"/api/contests/{contestName}/{stage}/results";
            using var commonResponse = await commonClient.GetAsync(url);
            using var personalResponse = await personalClient.GetAsync($"{url}/personal");
            Assert.That(commonResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(personalResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var common = await commonResponse.Content.ReadFromJsonAsync<JsonElement>();
            var personal = await personalResponse.Content.ReadFromJsonAsync<JsonElement>();

            using var assertions = Assert.EnterMultipleScope();
            Assert.That(common.GetProperty("problems").EnumerateArray().Select(problem => problem.GetProperty("name").GetString()),
                Is.EqualTo(new[] { problemName }));
            Assert.That(personal.GetProperty("problems").EnumerateArray().Select(problem => problem.GetProperty("name").GetString()),
                Is.EqualTo(new[] { problemName }));
            Assert.That(personal.GetProperty("group").GetString(), Is.EqualTo("Default"));
            var group = common.GetProperty("resultGroups").EnumerateArray().Single();
            Assert.That(group.GetProperty("name").GetString(), Is.EqualTo("Default"));
            var row = group.GetProperty("rows").EnumerateArray().Single();
            foreach (var result in new[] { row, personal })
            {
                Assert.That(result.GetProperty("isDisqualified").GetBoolean(), Is.EqualTo(disqualified));
                var totalScore = result.GetProperty("totalScore");
                Assert.That(totalScore.ValueKind == JsonValueKind.Null ? (decimal?)null : totalScore.GetDecimal(),
                    Is.EqualTo(disqualified ? (decimal?)null : baseScore));
                var place = result.GetProperty("place");
                Assert.That(place.ValueKind == JsonValueKind.Null ? (int?)null : place.GetInt32(),
                    Is.EqualTo(disqualified ? (int?)null : 1));
                Assert.That(result.GetProperty("problemResults").GetProperty("A").GetProperty("score").GetProperty("baseScore").GetDecimal(),
                    Is.EqualTo(baseScore));
            }
        }
    }

    [TestCase("Preliminary", ContestStage.Preliminary)]
    [TestCase("0", ContestStage.Preliminary)]
    [TestCase("Final", ContestStage.Final)]
    [TestCase("1", ContestStage.Final)]
    public async Task DefinedStageNamesAndNumbers_AreAcceptedByBothRoutes(string routeStage, ContestStage stage)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", stage, 1000, builder => builder
                .AddProblem("A", "Problem A", problem => problem.AddResult(2, 50m)).MarkPublished())
            .BuildAsync();
        SubstituteParticipant("test", 2);
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 2);
        using var commonClient = Factory.CreateClient();
        using var personalClient = Factory.CreateAuthenticatedClient(42);
        using var commonResponse = await commonClient.GetAsync($"/api/contests/test/{routeStage}/results");
        using var personalResponse = await personalClient.GetAsync($"/api/contests/test/{routeStage}/results/personal");

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(commonResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(personalResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static void SubstituteParticipant(string contestName, int id) =>
        SubstituteExtensions.Participants.SubstituteParticipant(contestName, "Default", new()
        {
            Id = id,
            Grade = 11,
            Name = new() { LastName = "Test", FirstName = "Participant" }
        });
}
