using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ContestResult> ContestResults { get; set; }
    public DbSet<Problem> Problems { get; set; }
    public DbSet<ProblemResult> ProblemResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContestResult>(builder =>
        {
            builder.HasKey(contestResult => contestResult.Id);

            builder.HasAlternateKey(result => new { result.ContestName, result.Stage });
            builder.HasAlternateKey(result => result.StageId);

            builder.HasMany(contestResult => contestResult.Problems)
                   .WithOne()
                   .HasForeignKey(problem => problem.ContestResultId);
        });

        modelBuilder.Entity<Problem>(builder =>
        {
            builder.HasKey(problem => problem.Id);

            builder.HasAlternateKey(problem => new { problem.ContestResultId, problem.Alias });

            builder.HasMany(problem => problem.Results)
                   .WithOne()
                   .HasForeignKey(problemResult => problemResult.ProblemId);
        });

        modelBuilder.Entity<ProblemResult>(builder =>
        {
            builder.HasKey(problemResult => problemResult.Id);

            builder.HasAlternateKey(problemResult => new { problemResult.ProblemId, problemResult.ParticipantId });

            builder.Property(result => result.BaseScore).HasScorePrecision();

            builder.OwnsMany<ScoreAdjustment>(result => result.Adjustments,
                                              navigationBuilder =>
                                              {
                                                  navigationBuilder.HasKey(adjustment => adjustment.Id);

                                                  navigationBuilder.Property(adjustment => adjustment.Adjustment)
                                                                   .HasScorePrecision();
                                              });
        });

        base.OnModelCreating(modelBuilder);
    }
}

file static class EfExtensions
{
    public static PropertyBuilder<decimal> HasScorePrecision(this PropertyBuilder<decimal> propertyBuilder) =>
        propertyBuilder.HasPrecision(5, 2);
}
