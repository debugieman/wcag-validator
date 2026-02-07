using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Domain.Entities;

namespace WcagAnalyzer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasMany(e => e.Results)
                  .WithOne(e => e.AnalysisRequest)
                  .HasForeignKey(e => e.AnalysisRequestId);
        });

        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleId).IsRequired();
            entity.Property(e => e.Impact).IsRequired();
            entity.Property(e => e.Description).IsRequired();
        });
    }
}
