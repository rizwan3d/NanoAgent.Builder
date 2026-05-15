using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Domain.Projects;
using NanoAgent.Builder.Domain.Saas;
using NanoAgent.Builder.Infrastructure.Identity;

namespace NanoAgent.Builder.Infrastructure.Data;

public sealed class BuilderDbContext : IdentityDbContext<ApplicationUser>
{
    public BuilderDbContext(DbContextOptions<BuilderDbContext> options)
        : base(options)
    {
    }

    public DbSet<AgentProject> AgentProjects => Set<AgentProject>();

    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

    public DbSet<ProjectMessage> ProjectMessages => Set<ProjectMessage>();

    public DbSet<ProjectRun> ProjectRuns => Set<ProjectRun>();

    public DbSet<GeneratedArtifact> GeneratedArtifacts => Set<GeneratedArtifact>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    public DbSet<MonthlyTokenUsage> MonthlyTokenUsages => Set<MonthlyTokenUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(200);

            entity.Property(user => user.StripeCustomerId)
                .HasMaxLength(200);

            entity.HasIndex(user => user.StripeCustomerId);
        });

        modelBuilder.Entity<AgentProject>(entity =>
        {
            entity.ToTable("AgentProjects");
            entity.HasKey(project => project.Id);

            entity.Property(project => project.OwnerUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(project => project.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(project => project.Description)
                .HasMaxLength(1000);

            entity.Property(project => project.LlmModel)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(project => project.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(project => project.OwnerUserId);
        });


        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.ToTable("ProjectFiles");
            entity.HasKey(file => file.Id);

            entity.Property(file => file.ProjectId)
                .IsRequired();

            entity.Property(file => file.Path)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(file => file.Language)
                .HasMaxLength(100);

            entity.Property(file => file.Content)
                .IsRequired();

            entity.Property(file => file.CreatedAtUtc)
                .IsRequired();

            entity.Property(file => file.UpdatedAtUtc)
                .IsRequired();

            entity.HasOne(file => file.Project)
                .WithMany()
                .HasForeignKey(file => file.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(file => new { file.ProjectId, file.Path })
                .IsUnique();
        });

        modelBuilder.Entity<ProjectMessage>(entity =>
        {
            entity.ToTable("ProjectMessages");
            entity.HasKey(message => message.Id);

            entity.Property(message => message.ProjectId)
                .IsRequired();

            entity.Property(message => message.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(message => message.Content)
                .IsRequired();

            entity.Property(message => message.LlmModel)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(message => message.InputTokens)
                .IsRequired();

            entity.Property(message => message.OutputTokens)
                .IsRequired();

            entity.Property(message => message.CreatedAtUtc)
                .IsRequired();

            entity.HasOne(message => message.Project)
                .WithMany()
                .HasForeignKey(message => message.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(message => new { message.ProjectId, message.CreatedAtUtc });
        });

        modelBuilder.Entity<ProjectRun>(entity =>
        {
            entity.ToTable("ProjectRuns");
            entity.HasKey(run => run.Id);

            entity.Property(run => run.ProjectId)
                .IsRequired();

            entity.Property(run => run.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(run => run.RequestedModel)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(run => run.Prompt)
                .HasMaxLength(8000);

            entity.Property(run => run.InputTokens)
                .IsRequired();

            entity.Property(run => run.OutputTokens)
                .IsRequired();

            entity.Property(run => run.StartedAtUtc)
                .IsRequired();

            entity.Property(run => run.CompletedAtUtc);

            entity.Property(run => run.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasOne(run => run.Project)
                .WithMany()
                .HasForeignKey(run => run.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(run => new { run.ProjectId, run.StartedAtUtc });
            entity.HasIndex(run => run.Status);
        });

        modelBuilder.Entity<GeneratedArtifact>(entity =>
        {
            entity.ToTable("GeneratedArtifacts");
            entity.HasKey(artifact => artifact.Id);

            entity.Property(artifact => artifact.ProjectId)
                .IsRequired();

            entity.Property(artifact => artifact.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(artifact => artifact.ArtifactType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(artifact => artifact.Path)
                .HasMaxLength(500);

            entity.Property(artifact => artifact.Content);

            entity.Property(artifact => artifact.CreatedAtUtc)
                .IsRequired();

            entity.HasOne(artifact => artifact.Project)
                .WithMany()
                .HasForeignKey(artifact => artifact.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(artifact => artifact.ProjectRun)
                .WithMany()
                .HasForeignKey(artifact => artifact.ProjectRunId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(artifact => new { artifact.ProjectId, artifact.CreatedAtUtc });
            entity.HasIndex(artifact => artifact.ProjectRunId);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("SubscriptionPlans");
            entity.HasKey(plan => plan.Id);

            entity.Property(plan => plan.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(plan => plan.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(plan => plan.Description)
                .HasMaxLength(500);

            entity.Property(plan => plan.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(plan => plan.MonthlyPrice)
                .HasPrecision(18, 2);

            entity.Property(plan => plan.MonthlyTokenLimit)
                .IsRequired();

            entity.Property(plan => plan.AllowedLlmModels)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(plan => plan.StripePriceId)
                .HasMaxLength(200);

            entity.Property(plan => plan.IsActive)
                .IsRequired();

            entity.Property(plan => plan.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(plan => plan.Code)
                .IsUnique();

            entity.HasIndex(plan => plan.StripePriceId);
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("UserSubscriptions");
            entity.HasKey(subscription => subscription.Id);

            entity.Property(subscription => subscription.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(subscription => subscription.StripeCustomerId)
                .HasMaxLength(200);

            entity.Property(subscription => subscription.StripeSubscriptionId)
                .HasMaxLength(200);

            entity.Property(subscription => subscription.StripePriceId)
                .HasMaxLength(200);

            entity.Property(subscription => subscription.CurrentPeriodStartsAtUtc);

            entity.Property(subscription => subscription.CurrentPeriodEndsAtUtc);

            entity.Property(subscription => subscription.StartedAtUtc)
                .IsRequired();

            entity.HasOne(subscription => subscription.Plan)
                .WithMany()
                .HasForeignKey(subscription => subscription.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(subscription => subscription.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(subscription => subscription.UserId);
            entity.HasIndex(subscription => subscription.StripeSubscriptionId);
            entity.HasIndex(subscription => subscription.StripeCustomerId);
        });

        modelBuilder.Entity<MonthlyTokenUsage>(entity =>
        {
            entity.ToTable("MonthlyTokenUsages");
            entity.HasKey(usage => usage.Id);

            entity.Property(usage => usage.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(usage => usage.PeriodStartUtc)
                .IsRequired();

            entity.Property(usage => usage.PeriodEndUtc)
                .IsRequired();

            entity.Property(usage => usage.UsedTokens)
                .IsRequired();

            entity.Property(usage => usage.CreatedAtUtc)
                .IsRequired();

            entity.Property(usage => usage.UpdatedAtUtc)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(usage => usage.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(usage => new { usage.UserId, usage.PeriodStartUtc, usage.PeriodEndUtc })
                .IsUnique();
        });

    }
}
