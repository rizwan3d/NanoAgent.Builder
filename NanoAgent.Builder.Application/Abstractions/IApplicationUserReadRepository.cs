using NanoAgent.Builder.Application.Admin;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IApplicationUserReadRepository
{
    Task<IReadOnlyList<ApplicationUserSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
}
