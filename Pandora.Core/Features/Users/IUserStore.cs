namespace Pandora.Core.Features.Users;

public interface IUserStore
{
    ValueTask<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    ValueTask<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    ValueTask<User?> UpdateAsync(User user, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<User> QueryAsync(UserQuery query, CancellationToken cancellationToken = default);
}
