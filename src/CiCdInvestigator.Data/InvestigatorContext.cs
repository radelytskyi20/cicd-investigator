using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.Data;

public sealed class InvestigatorContext(DbContextOptions<InvestigatorContext> options) : DbContext(options)
{
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<TestResult> Tests => Set<TestResult>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<ChangeSet> Changes => Set<ChangeSet>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Pipeline>().HasKey(x => x.Id);
        model.Entity<Commit>().HasKey(x => x.Id);
        model.Entity<Run>().HasOne<Pipeline>().WithMany().HasForeignKey(x => x.PipelineId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Run>().HasOne<Commit>().WithMany().HasForeignKey(x => x.CommitId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Job>().HasOne<Run>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Job>().HasIndex(x => new { x.RunId, x.Name, x.Attempt }).IsUnique();
        model.Entity<LogEntry>().HasKey(x => new { x.JobId, x.Position });
        model.Entity<LogEntry>().HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TestResult>().HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ChangeSet>().HasKey(x => x.RunId);
        model.Entity<ChangeSet>().HasOne<Run>().WithOne().HasForeignKey<ChangeSet>(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ChangeSet>().HasOne<Commit>().WithMany().HasForeignKey(x => x.BaseCommitId).OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                if (property.IsKey() || property.IsForeignKey()) property.SetMaxLength(100);
            }
        }
        model.Entity<Job>().Property(x => x.Name).HasMaxLength(100);
    }
}
