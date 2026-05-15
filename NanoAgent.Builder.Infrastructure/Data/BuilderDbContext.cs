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
