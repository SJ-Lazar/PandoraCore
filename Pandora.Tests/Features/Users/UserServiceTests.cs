using Pandora.Core.Features.Users;

namespace Pandora.Tests.Features.Users;

public sealed class UserServiceTests
{
    [Test]
    public async Task CreateAsync_AssignsDefaultsAndNormalizes()
    {
        var options = new UsersOptions();
        var service = new UserService(new InMemoryUserStore(options));

        var input = new User
        {
            Email = "  USER@Example.com  ",
            Roles = new[] { "admin", "Admin" }
        };

        var created = await service.CreateAsync(input);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(created.Email, Is.EqualTo("user@example.com"));
        Assert.That(created.Roles.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_UpdatesExistingUser()
    {
        var options = new UsersOptions();
        var store = new InMemoryUserStore(options);
        var service = new UserService(store);

        var created = await service.CreateAsync(new User { Email = "one@example.com", DisplayName = "One" });

        var updated = await service.UpdateAsync(created with { DisplayName = "Updated" });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.DisplayName, Is.EqualTo("Updated"));
        Assert.That(updated.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public async Task QueryAsync_FiltersByRoleAndActive()
    {
        var options = new UsersOptions();
        var service = new UserService(new InMemoryUserStore(options));

        await service.CreateAsync(new User { Email = "a@example.com", Roles = new[] { "admin" } });
        await service.CreateAsync(new User { Email = "b@example.com", Roles = new[] { "user" }, IsActive = false });
        await service.CreateAsync(new User { Email = "c@example.com", Roles = new[] { "admin" } });

        var results = new List<User>();
        await foreach (var user in service.QueryAsync(new UserQuery { Role = "admin", IsActive = true }))
        {
            results.Add(user);
        }

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.All(u => u.Roles.Contains("admin", StringComparer.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task DeleteAsync_RemovesUser()
    {
        var options = new UsersOptions();
        var store = new InMemoryUserStore(options);
        var service = new UserService(store);

        var created = await service.CreateAsync(new User { Email = "delete@example.com" });
        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetAsync(created.Id);

        Assert.That(deleted, Is.True);
        Assert.That(fetched, Is.Null);
    }
}
