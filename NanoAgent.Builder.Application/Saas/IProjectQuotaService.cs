namespace NanoAgent.Builder.Application.Saas;

internal interface IProjectQuotaService
{
    Task EnsureCanCreateProjectAsync(string userId, CancellationToken cancellationToken = default);
}
