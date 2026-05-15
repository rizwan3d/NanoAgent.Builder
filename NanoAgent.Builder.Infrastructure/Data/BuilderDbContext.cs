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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.Property(plan => plan.IsActive)
                .IsRequired();

            entity.Property(plan => plan.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(plan => plan.Code)
                .IsUnique();
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("UserSubscriptions");
            entity.HasKey(subscription => subscription.Id);

            entity.Property(subscription => subscription.UserId)
                .IsRequired()
                .HasMaxLength(450);

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
        });
    }
}
