using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Texnokaktus.ProgOlymp.ResultService.Endpoints.Filters;
using Texnokaktus.ProgOlymp.ResultService.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Tests;

public class EnumValidationFilterTests
{
    [Test]
    public async Task MultipleUndefinedEnums_ReturnErrorsForEachParameterWithoutInvokingHandler()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var invoked = false;
        var filter = EnumValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<ContestStage, DayOfWeek, ContestStage?, object>)EnumHandler).Method
        }, _ =>
        {
            invoked = true;
            return ValueTask.FromResult<object?>(null);
        });
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(),
            (ContestStage)2, (DayOfWeek)7, (ContestStage?)(-1));

        var result = await filter(context);

        Assert.That(result, Is.TypeOf<ValidationProblem>());
        var problem = ((ValidationProblem)result!).ProblemDetails;
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(invoked, Is.False);
        Assert.That(problem.Status, Is.EqualTo(400));
        Assert.That(problem.Errors.Keys, Is.EquivalentTo(new[] { "stage", "day", "optionalStage" }));
        Assert.That(problem.Errors.Values, Is.All.Not.Empty);
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(1)]
    public async Task DefinedEnumsAndNullableValues_InvokeHandler(int? optionalStage)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var expected = new object();
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(),
            ContestStage.Final, DayOfWeek.Monday, (ContestStage?)optionalStage);
        EndpointFilterInvocationContext? receivedContext = null;
        var filter = EnumValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<ContestStage, DayOfWeek, ContestStage?, object>)EnumHandler).Method
        }, invocationContext =>
        {
            receivedContext = invocationContext;
            return ValueTask.FromResult<object?>(expected);
        });

        var result = await filter(context);

        Assert.That(result, Is.SameAs(expected));
        Assert.That(receivedContext, Is.SameAs(context));
    }

    [Test]
    public async Task NonEnumArguments_ArePassedToHandler()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var expected = new object();
        var filter = EnumValidationFilter.Create(new()
        {
            ApplicationServices = services,
            MethodInfo = ((Func<int, string, object>)NonEnumHandler).Method
        }, _ => ValueTask.FromResult<object?>(expected));

        var result = await filter(EndpointFilterInvocationContext.Create(new DefaultHttpContext(), -1, "999"));

        Assert.That(result, Is.SameAs(expected));
    }

    private static object EnumHandler(ContestStage stage, DayOfWeek day, ContestStage? optionalStage) => new();

    private static object NonEnumHandler(int number, string text) => new();
}
