using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Grpc.Core;
using NSubstitute;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class RestParticipantResultQueryingTests : SetupBase
{
    [Test]
    public async Task CancelledPersonalRequest_CancelsParticipantLookup()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient(42);
        using var cancellation = new CancellationTokenSource();
        var lookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupResponse = new TaskCompletionSource<GetParticipantIdResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeout = TimeSpan.FromSeconds(10);
        CancellationTokenRegistration registration = default;
        factory.ParticipantServiceClientMock
            .GetParticipantIdAsync(Arg.Any<GetParticipantIdRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                var token = info.Arg<CancellationToken>();
                registration = token.Register(() =>
                {
                    lookupCancelled.TrySetResult();
                    lookupResponse.TrySetCanceled(token);
                });
                lookupStarted.TrySetResult(token);
                return new AsyncUnaryCall<GetParticipantIdResponse>(lookupResponse.Task,
                    Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            });

        Task<HttpResponseMessage>? request = null;
        try
        {
            request = client.GetAsync("/api/contests/test/Preliminary/results/personal", cancellation.Token);
            var token = await lookupStarted.Task.WaitAsync(timeout);
            Assert.That(token.CanBeCanceled, Is.True, "The participant lookup must receive the request cancellation signal.");

            cancellation.Cancel();
            await lookupCancelled.Task.WaitAsync(timeout);
            await Assert.CatchAsync<OperationCanceledException>(async () =>
            {
                using var response = await request.WaitAsync(timeout);
            });
        }
        finally
        {
            cancellation.Cancel();
            lookupResponse.TrySetCanceled();
            registration.Dispose();
            if (request is not null)
            {
                try { using var response = await request.WaitAsync(timeout); }
                catch (OperationCanceledException) { }
            }
        }
    }

    [TestCase("missing")]
    [TestCase("malformed")]
    [TestCase("expired")]
    [TestCase("invalid-signature")]
    public async Task MissingOrInvalidToken_ReturnsBearerChallenge(string tokenKind)
    {
        using var client = Factory.CreateClient();
        var token = tokenKind switch
        {
            "missing" => null,
            "malformed" => "not-a-jwt",
            "expired" => TestJwtTokens.Create(42, expired: true),
            _ => TestJwtTokens.Create(42, invalidSignature: true)
        };
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/contests/test/Preliminary/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.Select(challenge => challenge.Scheme), Does.Contain("Bearer"));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AuthenticatedParticipant_ReturnsOwnGroupRankAndDetailedScores(bool disqualified)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage =>
            {
                stage.AddProblem("C", "Problem C")
                    .AddProblem("A", "Problem A", problem => problem
                        .AddResult(1, 200m)
                        .AddResult(2, 50.25m, result => result.AddAdjustment(10.50m, "Appeal").AddAdjustment(-0.75m))
                        .AddResult(3, 80m)
                        .AddResult(4, 60m))
                    .AddProblem("B", "Problem B", problem => problem.AddResult(2, 0m))
                    .MarkPublished();
                if (disqualified)
                    stage.DisqualifyParticipant(2, "Private reason");
            })
            .BuildAsync();
        SubstituteParticipant(1, "Other group");
        SubstituteParticipant(3, "Selected group");
        SubstituteParticipant(4, "Selected group");
        SubstituteParticipant(2, "Selected group");
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 2);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results/personal");

        var expected = JsonNode.Parse("""
            {
              "place":2,"group":"Selected group",
              "problems":[{"alias":"A","name":"Problem A"},{"alias":"B","name":"Problem B"},{"alias":"C","name":"Problem C"}],
              "problemResults":{
                "A":{"score":{
                  "baseScore":50.25,
                  "adjustments":[{"value":10.50,"comment":"Appeal"},{"value":-0.75,"comment":null}],
                  "adjustmentsSum":9.75,"totalScore":60
                }},
                "B":{"score":{"baseScore":0,"adjustments":[],"adjustmentsSum":null,"totalScore":0}},
                "C":{"score":null}
              },
              "totalScore":60,"isDisqualified":false
            }
            """)!;
        if (disqualified)
        {
            expected["place"] = null;
            expected["totalScore"] = null;
            expected["isDisqualified"] = true;
        }

        await HttpResponseAssert.MatchesJsonAsync(response, expected.ToJsonString());
    }

    [Test]
    public async Task UserNotRegisteredForSelectedContest_ReturnsNotFound()
    {
        await BuildPublishedStageAsync();
        SubstituteExtensions.Participants.SubstituteParticipantId("other", 42, 1);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task ParticipantWithoutVisibleResults_ReturnsNotFound(bool registered, bool hasResults)
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem =>
                {
                    problem.AddResult(1, 100m);
                    if (hasResults)
                        problem.AddResult(2, 50m);
                }).MarkPublished())
            .BuildAsync();
        SubstituteParticipant(1, "Default");
        if (registered)
            SubstituteParticipant(2, "Default");
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 2);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [TestCase("missing", "Preliminary")]
    [TestCase("test", "Final")]
    public async Task MissingContestOrStage_ReturnsNotFound(string contestName, string stage)
    {
        await BuildPublishedStageAsync();
        SubstituteExtensions.Participants.SubstituteParticipantId(contestName, 42, 1);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync($"/api/contests/{contestName}/{stage}/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [Test]
    public async Task UnpublishedResults_ReturnNotFoundEvenForTheirOwner()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Unpublished problem", problem => problem.AddResult(1, 100m)))
            .BuildAsync();
        SubstituteParticipant(1, "Default");
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 1);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync("/api/contests/test/Preliminary/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
    }

    [TestCase("not-a-stage")]
    [TestCase("preliminary")]
    [TestCase("final")]
    [TestCase("2")]
    [TestCase("-1")]
    public async Task InvalidStage_ReturnsBadRequest(string stage)
    {
        SubstituteExtensions.Participants.SubstituteParticipantId("test", 42, 1);
        using var client = Factory.CreateAuthenticatedClient(42);
        using var response = await client.GetAsync($"/api/contests/test/{stage}/results/personal");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        if (int.TryParse(stage, out _))
            await HttpResponseAssert.IsValidationProblemAsync(response, "stage");
    }

    private static async Task BuildPublishedStageAsync()
    {
        await DataBuilder.ForClient(Factory.CreateResultServiceClient())
            .AddContestStage("test", ContestStage.Preliminary, 1000, stage => stage
                .AddProblem("A", "Problem A", problem => problem.AddResult(1, 100m)).MarkPublished())
            .BuildAsync();
        SubstituteParticipant(1, "Default");
    }

    private static void SubstituteParticipant(int id, string group) =>
        SubstituteExtensions.Participants.SubstituteParticipant("test", group, new()
        {
            Id = id,
            Grade = 11,
            Name = new() { LastName = "Test", FirstName = $"Participant {id}" }
        });
}
