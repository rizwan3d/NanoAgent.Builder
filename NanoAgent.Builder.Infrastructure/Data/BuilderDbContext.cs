using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Infrastructure.Data;

public sealed class BuilderDbContext : DbContext
{
    public BuilderDbContext(DbContextOptions<BuilderDbContext> options)
        : base(options)
    {
    }

    public DbSet<AgentProject> AgentProjects => Set<AgentProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AgentProject>(entity =>
        {
            entity.ToTable("AgentProjects");
            entity.HasKey(project => project.Id);

            entity.Property(project => project.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(project => project.Description)
                .HasMaxLength(1000);

            entity.Property(project => project.CreatedAtUtc)
                .IsRequired();
        });
    }
}
