namespace Pandora.Core.Features.Users;

public sealed class UserQuery
{
    public string? Email { get; init; }

    public string? Role { get; init; }

    public bool? IsActive { get; init; }

    public DateTimeOffset? CreatedFrom { get; init; }

    public DateTimeOffset? CreatedTo { get; init; }
}
