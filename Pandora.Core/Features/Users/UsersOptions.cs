namespace Pandora.Core.Features.Users;

public sealed class UsersOptions
{
    private int _maxInMemoryUsers = 1000;

    public int MaxInMemoryUsers
    {
        get => _maxInMemoryUsers;
        set => _maxInMemoryUsers = value <= 0 ? 1000 : value;
    }
}
