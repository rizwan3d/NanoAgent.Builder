using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Saas;

public sealed class MonthlyTokenUsage : Entity
{
    private MonthlyTokenUsage()
    {
    }

    public MonthlyTokenUsage(string userId, DateTimeOffset periodStartUtc, DateTimeOffset periodEndUtc)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User id is required for token usage.");
        }

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException("Token usage period end must be after the start.");
        }

        UserId = userId;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        UsedTokens = 0;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public string UserId { get; private set; } = string.Empty;

    public DateTimeOffset PeriodStartUtc { get; private set; }

    public DateTimeOffset PeriodEndUtc { get; private set; }

    public int UsedTokens { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void AddTokens(int tokenCount)
    {
        if (tokenCount <= 0)
        {
            throw new DomainException("Token count must be greater than zero.");
        }

        UsedTokens = checked(UsedTokens + tokenCount);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
