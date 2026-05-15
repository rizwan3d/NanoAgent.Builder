using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Saas;

public sealed class SubscriptionPlan : Entity
{
    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(
        string code,
        string name,
        string? description,
        SubscriptionTier tier,
        decimal monthlyPrice,
        string currency,
        int projectLimit,
        int displayOrder,
        string? stripePriceId = null)
    {
        SetCode(code);
        Rename(name);
        UpdateDescription(description);
        SetPricing(tier, monthlyPrice, currency);
        SetProjectLimit(projectLimit);
        ConfigureStripePrice(stripePriceId);
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public SubscriptionTier Tier { get; private set; }

    public decimal MonthlyPrice { get; private set; }

    public string Currency { get; private set; } = "USD";

    public int ProjectLimit { get; private set; }

    public int DisplayOrder { get; private set; }

    public string? StripePriceId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Plan name is required.");
        }

        if (name.Length > 100)
        {
            throw new DomainException("Plan name cannot be longer than 100 characters.");
        }

        Name = name.Trim();
    }

    public void UpdateDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new DomainException("Plan description cannot be longer than 500 characters.");
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void ConfigureStripePrice(string? stripePriceId)
    {
        if (stripePriceId is { Length: > 200 })
        {
            throw new DomainException("Stripe price id cannot be longer than 200 characters.");
        }

        if (Tier == SubscriptionTier.Free && !string.IsNullOrWhiteSpace(stripePriceId))
        {
            throw new DomainException("Free plans should not have a Stripe price id.");
        }

        StripePriceId = string.IsNullOrWhiteSpace(stripePriceId) ? null : stripePriceId.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Plan code is required.");
        }

        if (code.Length > 50)
        {
            throw new DomainException("Plan code cannot be longer than 50 characters.");
        }

        Code = code.Trim().ToLowerInvariant();
    }

    private void SetPricing(SubscriptionTier tier, decimal monthlyPrice, string currency)
    {
        if (monthlyPrice < 0)
        {
            throw new DomainException("Monthly price cannot be negative.");
        }

        if (tier == SubscriptionTier.Free && monthlyPrice != 0)
        {
            throw new DomainException("Free plans must have a zero monthly price.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        Tier = tier;
        MonthlyPrice = monthlyPrice;
        Currency = currency.Trim().ToUpperInvariant();
    }

    private void SetProjectLimit(int projectLimit)
    {
        if (projectLimit == 0 || projectLimit < -1)
        {
            throw new DomainException("Project limit must be positive, or -1 for unlimited.");
        }

        ProjectLimit = projectLimit;
    }
}
