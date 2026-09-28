using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Data;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

public class ResultQueryingTests : SetupBase
{
    [Test]
    public async Task SingleParticipant()
    {
        const string contestName = "test";
        const ContestStage contestStage = ContestStage.Preliminary;

        var client = Factory.CreateResultServiceClient();

        await DataBuilder
             .ForClient(client)
             .AddContestStage(
                  contestName,
                  contestStage,
                  1000,
                  contestStageBuilder => contestStageBuilder
                                        .AddProblem(
                                             "A",
                                             "Test Problem 1",
                                             problemBuilder => problemBuilder.AddResult(1, 20m)
                                         )
                                        .AddProblem(
                                             "B",
                                             "Test Problem 2",
                                             problemBuilder => problemBuilder.AddResult(
                                                 1,
                                                 40m,
                                                 resultBuilder => resultBuilder.AddAdjustment(10m, "Test comment")
                                                                               .AddAdjustment(-2m)
                                             )
                                         )
                                        .AddProblem(
                                             "C",
                                             "Test Problem 3",
                                             problemBuilder => problemBuilder.AddResult(1, 80m)
                                         )
              )
             .BuildAsync();

        SubstituteExtensions.Participants.SubstituteParticipant(
            contestName,
            "Default",
            new()
            {
                Id = 1,
                Grade = 11,
                Name = new()
                {
                    FirstName = "A",
                    LastName = "B",
                    Patronym = "C"
                }
            }
        );

        var queryingClient = Factory.CreateResultQueryingServiceClient();

        var results = await queryingClient.GetResultsAsync(
                          new()
                          {
                              ContestName = contestName,
                              Stage = contestStage
                          }
                      );

        ContestResultsAssert.Matches(
            results,
            new(
                [
                    new(1, "A", "Test Problem 1"),
                    new(2, "B", "Test Problem 2"),
                    new(3, "C", "Test Problem 3")
                ],
                [
                    new("Default",
                    [
                        new(ParticipantId: 1, Place: 1, Results:
                        [
                            new(1, 20m),
                            new(2, 40m, new(1, 10m, "Test comment"), new(2, -2m)),
                            new(3, 80m)
                        ])
                    ])
                ]
            )
        );
    }
}
