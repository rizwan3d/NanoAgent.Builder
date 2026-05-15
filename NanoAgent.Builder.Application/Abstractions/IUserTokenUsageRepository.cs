using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IUserTokenUsageRepository
{
    Task<MonthlyTokenUsage?> GetForUserPeriodAsync(
        string userId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTokenUsage>> ListForOpenPeriodsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(MonthlyTokenUsage usage, CancellationToken cancellationToken = default);
}
