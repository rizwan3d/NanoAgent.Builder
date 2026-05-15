using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Admin;

namespace NanoAgent.Builder.Infrastructure.Identity;

internal sealed class IdentityUserReadRepository : IApplicationUserReadRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserReadRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ApplicationUserSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var results = new List<ApplicationUserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            results.Add(new ApplicationUserSummaryDto(
                user.Id,
                user.Email ?? user.UserName ?? user.Id,
                user.DisplayName,
                roles.ToArray(),
                user.CreatedAtUtc));
        }

        return results;
    }
}
