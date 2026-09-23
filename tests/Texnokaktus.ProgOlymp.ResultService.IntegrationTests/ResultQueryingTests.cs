using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

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
                                             problemBuilder => problemBuilder.AddResult(1, 40m)
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                results.Problems,
                Is.EquivalentTo(
                    [
                        new Problem()
                        {
                            Id = 1,
                            Alias = "A",
                            Name = "Test Problem 1"
                        },
                        new Problem()
                        {
                            Id = 2,
                            Alias = "B",
                            Name = "Test Problem 2"
                        },
                        new Problem()
                        {
                            Id = 3,
                            Alias = "C",
                            Name = "Test Problem 3"
                        }
                    ]
                )
            );

            Assert.That(
                results.ResultGroups,
                Is.EquivalentTo(
                    [
                        new ResultGroup
                        {
                            Name = "Default",
                            Rows =
                            {
                                new ResultRow
                                {
                                    Place = 1,
                                    ParticipantId = 1,
                                    TotalScore = 140m,
                                    IsDisqualified = false,
                                    Results =
                                    {
                                        new ProblemResult
                                        {
                                            ProblemId = 1,
                                            Score = new()
                                            {
                                                BaseScore = 20m,
                                                TotalScore = 20m
                                            }
                                        },
                                        new ProblemResult
                                        {
                                            ProblemId = 2,
                                            Score = new()
                                            {
                                                BaseScore = 40m,
                                                TotalScore = 40m
                                            }
                                        },
                                        new ProblemResult
                                        {
                                            ProblemId = 3,
                                            Score = new()
                                            {
                                                BaseScore = 80m,
                                                TotalScore = 80m
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ]
                )
            );
        }
    }
}
