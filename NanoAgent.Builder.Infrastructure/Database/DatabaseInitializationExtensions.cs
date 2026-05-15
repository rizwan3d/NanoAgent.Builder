using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Application.Security;
using NanoAgent.Builder.Domain.Saas;
using NanoAgent.Builder.Infrastructure.Data;
using NanoAgent.Builder.Infrastructure.Identity;
using NanoAgent.Builder.Infrastructure.Payments;

namespace NanoAgent.Builder.Infrastructure.Database;

public static class DatabaseInitializationExtensions
{
    public static async Task InitialiseDatabaseAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var context = scope.ServiceProvider.GetRequiredService<BuilderDbContext>();

        if (databaseOptions.EnsureCreated)
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        var stripeOptions = configuration
            .GetSection(StripeOptions.SectionName)
            .Get<StripeOptions>() ?? new StripeOptions();

        await SeedSaasPlansAsync(context, stripeOptions, cancellationToken);
        await SeedIdentityAsync(scope.ServiceProvider, configuration, context, cancellationToken);
    }

    private static async Task SeedSaasPlansAsync(
        BuilderDbContext context,
        StripeOptions stripeOptions,
        CancellationToken cancellationToken)
    {
        var existingPlans = await context.SubscriptionPlans.ToListAsync(cancellationToken);
        if (existingPlans.Count == 0)
        {
            var seedPlans = new[]
            {
                new SubscriptionPlan(
                    SaasPlanCodes.Free,
                    "Free",
                    "For trying NanoAgent Builder with a small project quota.",
                    SubscriptionTier.Free,
                    0,
                    "USD",
                    3,
                    10000,
                    "gpt-4o-mini",
                    1),
                new SubscriptionPlan(
                    SaasPlanCodes.Starter,
                    "Starter",
                    "Paid tier for growing users and small teams.",
                    SubscriptionTier.Paid,
                    19,
                    "USD",
                    25,
                    250000,
                    "gpt-4o-mini,gpt-4.1-mini",
                    2,
                    stripeOptions.GetPriceId(SaasPlanCodes.Starter)),
                new SubscriptionPlan(
                    SaasPlanCodes.Pro,
                    "Pro",
                    "Paid tier for production users with a high project quota.",
                    SubscriptionTier.Paid,
                    49,
                    "USD",
                    100,
                    1000000,
                    "gpt-4o-mini,gpt-4.1-mini,gpt-4.1",
                    3,
                    stripeOptions.GetPriceId(SaasPlanCodes.Pro))
            };

            await context.SubscriptionPlans.AddRangeAsync(seedPlans, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var free = existingPlans.FirstOrDefault(plan => plan.Code == SaasPlanCodes.Free);
        free?.UpdateEntitlements(3, 10000, "gpt-4o-mini");

        var starterExisting = existingPlans.FirstOrDefault(plan => plan.Code == SaasPlanCodes.Starter);
        starterExisting?.UpdateEntitlements(25, 250000, "gpt-4o-mini,gpt-4.1-mini");

        var proExisting = existingPlans.FirstOrDefault(plan => plan.Code == SaasPlanCodes.Pro);
        proExisting?.UpdateEntitlements(100, 1000000, "gpt-4o-mini,gpt-4.1-mini,gpt-4.1");

        var starterPriceId = stripeOptions.GetPriceId(SaasPlanCodes.Starter);
        var starter = existingPlans.FirstOrDefault(plan => plan.Code == SaasPlanCodes.Starter);
        if (starter is not null && !string.IsNullOrWhiteSpace(starterPriceId))
        {
            starter.ConfigureStripePrice(starterPriceId);
        }

        var proPriceId = stripeOptions.GetPriceId(SaasPlanCodes.Pro);
        var pro = existingPlans.FirstOrDefault(plan => plan.Code == SaasPlanCodes.Pro);
        if (pro is not null && !string.IsNullOrWhiteSpace(proPriceId))
        {
            pro.ConfigureStripePrice(proPriceId);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedIdentityAsync(
        IServiceProvider services,
        IConfiguration configuration,
        BuilderDbContext context,
        CancellationToken cancellationToken)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { ApplicationRoles.Admin, ApplicationRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminOptions = configuration
            .GetSection(SeedAdminOptions.SectionName)
            .Get<SeedAdminOptions>() ?? new SeedAdminOptions();

        if (string.IsNullOrWhiteSpace(adminOptions.Email) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminOptions.Email,
                Email = adminOptions.Email,
                EmailConfirmed = true,
                DisplayName = adminOptions.DisplayName,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, adminOptions.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not create seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.User))
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoles.User);
        }

        var hasSubscription = await context.UserSubscriptions.AnyAsync(
            subscription => subscription.UserId == admin.Id &&
                            subscription.Status == SubscriptionStatus.Active &&
                            subscription.EndsAtUtc == null,
            cancellationToken);

        if (!hasSubscription)
        {
            var proPlan = await context.SubscriptionPlans
                .FirstAsync(plan => plan.Code == SaasPlanCodes.Pro, cancellationToken);

            await context.UserSubscriptions.AddAsync(new UserSubscription(admin.Id, proPlan.Id), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
