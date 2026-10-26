using System.Net;
using System.Text.Json.Nodes;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;

internal static class HttpResponseAssert
{
    public static async Task IsValidationProblemAsync(HttpResponseMessage response, params string[] parameterNames)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.That(problem["status"]!.GetValue<int>(), Is.EqualTo(400));
        var errors = problem["errors"]!.AsObject();
        Assert.That(errors.Select(error => error.Key), Is.EquivalentTo(parameterNames));
        foreach (var name in parameterNames)
            Assert.That(errors[name]!.AsArray(), Is.Not.Empty);
    }

    public static async Task MatchesJsonAsync(HttpResponseMessage response, string expectedJson)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(body), JsonNode.Parse(expectedJson)), Is.True,
            $"Expected JSON:\n{expectedJson}\nActual JSON:\n{body}");
    }
}
