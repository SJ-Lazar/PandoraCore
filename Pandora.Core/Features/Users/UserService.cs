namespace Pandora.Core.Features.Users;

public sealed class UserService : IUserService
{
    private readonly IUserStore _store;

    public UserService(IUserStore store)
    {
        _store = store;
    }

    public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var normalized = Normalize(user);
        if (normalized.Id == Guid.Empty)
        {
            normalized = normalized with { Id = Guid.NewGuid() };
        }

        if (normalized.CreatedAt == default)
        {
            normalized = normalized with { CreatedAt = DateTimeOffset.UtcNow };
        }

        return _store.CreateAsync(normalized, cancellationToken).AsTask();
    }

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(id, cancellationToken).AsTask();
    }

    public Task<User?> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException("User Id must be provided for updates.", nameof(user));
        }

        var normalized = Normalize(user);

        if (normalized.CreatedAt == default)
        {
            normalized = normalized with { CreatedAt = DateTimeOffset.UtcNow };
        }

        normalized = normalized with { UpdatedAt = DateTimeOffset.UtcNow };

        return _store.UpdateAsync(normalized, cancellationToken).AsTask();
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _store.DeleteAsync(id, cancellationToken).AsTask();
    }

    public IAsyncEnumerable<User> QueryAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _store.QueryAsync(query, cancellationToken);
    }

    private static User Normalize(User user)
    {
        var email = (user.Email ?? string.Empty).Trim().ToLowerInvariant();
        var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? null : user.DisplayName.Trim();
        var roles = user.Roles?.Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var metadata = user.Metadata is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(user.Metadata, StringComparer.OrdinalIgnoreCase);

        return user with
        {
            Email = email,
            DisplayName = displayName,
            Roles = roles,
            Metadata = metadata,
            IsActive = user.IsActive
        };
    }
}
