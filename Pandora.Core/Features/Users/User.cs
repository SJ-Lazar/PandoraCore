namespace Pandora.Core.Features.Users;

public sealed record User
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public Dictionary<string, string?> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
