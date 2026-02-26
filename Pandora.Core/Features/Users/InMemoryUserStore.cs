using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Pandora.Core.Features.Users;

public sealed class InMemoryUserStore : IUserStore
{
    private readonly UsersOptions _options;
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<string, Guid> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<Guid> _order = new();

    public InMemoryUserStore(UsersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public ValueTask<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var emailKey = NormalizeEmail(user.Email);

        if (_emailIndex.TryGetValue(emailKey, out var existingId) && _users.ContainsKey(existingId))
        {
            throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");
        }

        _users[user.Id] = user;
        _emailIndex[emailKey] = user.Id;
        _order.Enqueue(user.Id);
        Trim();

        return ValueTask.FromResult(user);
    }

    public ValueTask<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_users.TryGetValue(id, out var user) ? user : null);
    }

    public ValueTask<User?> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_users.TryGetValue(user.Id, out var existing))
        {
            return ValueTask.FromResult<User?>(null);
        }

        var emailKey = NormalizeEmail(user.Email);
        if (_emailIndex.TryGetValue(emailKey, out var mappedId) && mappedId != user.Id)
        {
            throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");
        }

        _users[user.Id] = user;
        _emailIndex[emailKey] = user.Id;

        var oldKey = NormalizeEmail(existing.Email);
        if (!string.Equals(oldKey, emailKey, StringComparison.OrdinalIgnoreCase))
        {
            _emailIndex.TryRemove(oldKey, out _);
        }

        return ValueTask.FromResult<User?>(user);
    }

    public ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_users.TryRemove(id, out var removed))
        {
            return ValueTask.FromResult(false);
        }

        var emailKey = NormalizeEmail(removed.Email);
        _emailIndex.TryRemove(emailKey, out _);
        return ValueTask.FromResult(true);
    }

    public async IAsyncEnumerable<User> QueryAsync(UserQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var snapshot = _users.Values.ToArray();
        foreach (var user in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Matches(user, query))
            {
                continue;
            }

            yield return user;
            await Task.Yield();
        }
    }

    private bool Matches(User user, UserQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Email) && !string.Equals(user.Email, query.Email, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.IsActive.HasValue && user.IsActive != query.IsActive.Value)
        {
            return false;
        }

        if (query.CreatedFrom is { } from && user.CreatedAt < from)
        {
            return false;
        }

        if (query.CreatedTo is { } to && user.CreatedAt > to)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var hasRole = user.Roles.Any(role => string.Equals(role, query.Role, StringComparison.OrdinalIgnoreCase));
            if (!hasRole)
            {
                return false;
            }
        }

        return true;
    }

    private void Trim()
    {
        var max = _options.MaxInMemoryUsers;
        while (_users.Count > max && _order.TryDequeue(out var id))
        {
            if (_users.TryRemove(id, out var removed))
            {
                var emailKey = NormalizeEmail(removed.Email);
                _emailIndex.TryRemove(emailKey, out _);
            }
        }
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
