using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NanoAgent.Builder.Infrastructure.Data;

#nullable disable

namespace NanoAgent.Builder.Infrastructure.Migrations;

[DbContext(typeof(BuilderDbContext))]
partial class BuilderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
        {
            b.Property<string>("Id");
            b.Property<string>("ConcurrencyStamp").IsConcurrencyToken();
            b.Property<string>("Name").HasMaxLength(256);
            b.Property<string>("NormalizedName").HasMaxLength(256);
            b.HasKey("Id");
            b.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex");
            b.ToTable("AspNetRoles", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd();
            b.Property<string>("ClaimType");
            b.Property<string>("ClaimValue");
            b.Property<string>("RoleId").IsRequired();
            b.HasKey("Id");
            b.HasIndex("RoleId");
            b.ToTable("AspNetRoleClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd();
            b.Property<string>("ClaimType");
            b.Property<string>("ClaimValue");
            b.Property<string>("UserId").IsRequired();
            b.HasKey("Id");
            b.HasIndex("UserId");
            b.ToTable("AspNetUserClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
        {
            b.Property<string>("LoginProvider").HasMaxLength(128);
            b.Property<string>("ProviderKey").HasMaxLength(128);
            b.Property<string>("ProviderDisplayName");
            b.Property<string>("UserId").IsRequired();
            b.HasKey("LoginProvider", "ProviderKey");
            b.HasIndex("UserId");
            b.ToTable("AspNetUserLogins", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
        {
            b.Property<string>("UserId");
            b.Property<string>("RoleId");
            b.HasKey("UserId", "RoleId");
            b.HasIndex("RoleId");
            b.ToTable("AspNetUserRoles", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
        {
            b.Property<string>("UserId");
            b.Property<string>("LoginProvider").HasMaxLength(128);
            b.Property<string>("Name").HasMaxLength(128);
            b.Property<string>("Value");
            b.HasKey("UserId", "LoginProvider", "Name");
            b.ToTable("AspNetUserTokens", (string)null);
        });

        modelBuilder.Entity("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", b =>
        {
            b.Property<string>("Id");
            b.Property<int>("AccessFailedCount");
            b.Property<string>("ConcurrencyStamp").IsConcurrencyToken();
            b.Property<DateTimeOffset>("CreatedAtUtc");
            b.Property<string>("DisplayName").HasMaxLength(200);
            b.Property<string>("Email").HasMaxLength(256);
            b.Property<bool>("EmailConfirmed");
            b.Property<bool>("LockoutEnabled");
            b.Property<DateTimeOffset?>("LockoutEnd");
            b.Property<string>("NormalizedEmail").HasMaxLength(256);
            b.Property<string>("NormalizedUserName").HasMaxLength(256);
            b.Property<string>("PasswordHash");
            b.Property<string>("PhoneNumber");
            b.Property<bool>("PhoneNumberConfirmed");
            b.Property<string>("SecurityStamp");
            b.Property<string>("StripeCustomerId").HasMaxLength(200);
            b.Property<bool>("TwoFactorEnabled");
            b.Property<string>("UserName").HasMaxLength(256);
            b.HasKey("Id");
            b.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex");
            b.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex");
            b.HasIndex("StripeCustomerId");
            b.ToTable("AspNetUsers", (string)null);
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Projects.AgentProject", b =>
        {
            b.Property<Guid>("Id");
            b.Property<DateTimeOffset>("CreatedAtUtc");
            b.Property<string>("Description").HasMaxLength(1000);
            b.Property<string>("LlmModel").IsRequired().HasMaxLength(100);
            b.Property<string>("Name").IsRequired().HasMaxLength(200);
            b.Property<string>("OwnerUserId").IsRequired().HasMaxLength(450);
            b.HasKey("Id");
            b.HasIndex("OwnerUserId");
            b.ToTable("AgentProjects", (string)null);
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Saas.MonthlyTokenUsage", b =>
        {
            b.Property<Guid>("Id");
            b.Property<DateTimeOffset>("CreatedAtUtc");
            b.Property<DateTimeOffset>("PeriodEndUtc");
            b.Property<DateTimeOffset>("PeriodStartUtc");
            b.Property<DateTimeOffset>("UpdatedAtUtc");
            b.Property<int>("UsedTokens");
            b.Property<string>("UserId").IsRequired().HasMaxLength(450);
            b.HasKey("Id");
            b.HasIndex("UserId", "PeriodStartUtc", "PeriodEndUtc").IsUnique();
            b.ToTable("MonthlyTokenUsages", (string)null);
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Saas.SubscriptionPlan", b =>
        {
            b.Property<Guid>("Id");
            b.Property<string>("AllowedLlmModels").IsRequired().HasMaxLength(500);
            b.Property<string>("Code").IsRequired().HasMaxLength(50);
            b.Property<DateTimeOffset>("CreatedAtUtc");
            b.Property<string>("Currency").IsRequired().HasMaxLength(3);
            b.Property<string>("Description").HasMaxLength(500);
            b.Property<int>("DisplayOrder");
            b.Property<bool>("IsActive");
            b.Property<decimal>("MonthlyPrice").HasPrecision(18, 2);
            b.Property<int>("MonthlyTokenLimit");
            b.Property<string>("Name").IsRequired().HasMaxLength(100);
            b.Property<int>("ProjectLimit");
            b.Property<string>("StripePriceId").HasMaxLength(200);
            b.Property<int>("Tier");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique();
            b.HasIndex("StripePriceId");
            b.ToTable("SubscriptionPlans", (string)null);
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Saas.UserSubscription", b =>
        {
            b.Property<Guid>("Id");
            b.Property<DateTimeOffset?>("CurrentPeriodEndsAtUtc");
            b.Property<DateTimeOffset?>("CurrentPeriodStartsAtUtc");
            b.Property<DateTimeOffset?>("EndsAtUtc");
            b.Property<DateTimeOffset>("StartedAtUtc");
            b.Property<int>("Status");
            b.Property<string>("StripeCustomerId").HasMaxLength(200);
            b.Property<string>("StripePriceId").HasMaxLength(200);
            b.Property<string>("StripeSubscriptionId").HasMaxLength(200);
            b.Property<Guid>("SubscriptionPlanId");
            b.Property<string>("UserId").IsRequired().HasMaxLength(450);
            b.HasKey("Id");
            b.HasIndex("StripeCustomerId");
            b.HasIndex("StripeSubscriptionId");
            b.HasIndex("SubscriptionPlanId");
            b.HasIndex("UserId");
            b.ToTable("UserSubscriptions", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
        {
            b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
        {
            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
        {
            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
        {
            b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
        {
            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Saas.MonthlyTokenUsage", b =>
        {
            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("NanoAgent.Builder.Domain.Saas.UserSubscription", b =>
        {
            b.HasOne("NanoAgent.Builder.Domain.Saas.SubscriptionPlan", "Plan")
                .WithMany()
                .HasForeignKey("SubscriptionPlanId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("NanoAgent.Builder.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Plan");
        });
    }
}
