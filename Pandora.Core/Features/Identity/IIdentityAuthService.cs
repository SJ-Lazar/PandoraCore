namespace Pandora.Core.Features.Identity;

public interface IIdentityAuthService
{
    Task<AuthLoginResult?> LoginAsync(string email, CancellationToken cancellationToken = default);

    Task<Identity<Guid>?> GetIdentityAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default);
}

public sealed class AuthLoginResult
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public Identity<Guid> Identity { get; init; } = default!;
}
