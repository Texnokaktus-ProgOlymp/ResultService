namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

[SetUpFixture]
public class SetupGlobal
{
    public static CustomWebApplicationFactory Factory { get; private set; }

    [OneTimeSetUp]
    public void Setup()
    {
        Factory = new();
        Factory.StartServer();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await Factory.DisposeAsync();
    }
}
