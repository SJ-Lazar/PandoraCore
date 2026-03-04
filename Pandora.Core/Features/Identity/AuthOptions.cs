namespace Pandora.Core.Features.Identity;

public sealed class AuthOptions
{
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);
}
