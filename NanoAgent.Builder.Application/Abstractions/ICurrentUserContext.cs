namespace NanoAgent.Builder.Application.Abstractions;

public interface ICurrentUserContext
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }
}
