namespace Pandora.Core.Features.Users;

public interface IUserService
{
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<User> QueryAsync(UserQuery query, CancellationToken cancellationToken = default);
}
