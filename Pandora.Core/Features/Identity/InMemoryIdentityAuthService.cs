using System.Collections.Concurrent;
using Pandora.Core.Features.Users;

namespace Pandora.Core.Features.Identity;

public sealed class InMemoryIdentityAuthService : IIdentityAuthService
{
    private readonly IUserService _userService;
    private readonly AuthOptions _options;
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new(StringComparer.Ordinal);

    public InMemoryIdentityAuthService(IUserService userService, AuthOptions? options = null)
    {
        _userService = userService;
        _options = options ?? new AuthOptions();
    }

    public async Task<AuthLoginResult?> LoginAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        User? found = null;
        await foreach (var user in _userService.QueryAsync(new UserQuery { Email = email }, cancellationToken).ConfigureAwait(false))
        {
            found = user;
            break;
        }

        if (found is null || !found.IsActive)
        {
            return null;
        }

        var identity = new Identity<Guid>(
            found.Id,
            found.DisplayName ?? found.Email,
            found.Roles,
            new[] { new KeyValuePair<string, string>("email", found.Email) });

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.TokenLifetime);

        _sessions[token] = new AuthSession
        {
            AccessToken = token,
            UserId = found.Id,
            ExpiresAt = expiresAt,
            Identity = identity
        };

        return new AuthLoginResult
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            Identity = identity
        };
    }

    public Task<Identity<Guid>?> GetIdentityAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult<Identity<Guid>?>(null);
        }

        if (!_sessions.TryGetValue(accessToken, out var session))
        {
            return Task.FromResult<Identity<Guid>?>(null);
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(accessToken, out _);
            return Task.FromResult<Identity<Guid>?>(null);
        }

        return Task.FromResult<Identity<Guid>?>(session.Identity);
    }

    public Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_sessions.TryRemove(accessToken, out _));
    }

    private sealed class AuthSession
    {
        public string AccessToken { get; init; } = string.Empty;

        public Guid UserId { get; init; }

        public DateTimeOffset ExpiresAt { get; init; }

        public Identity<Guid> Identity { get; init; } = default!;
    }
}
