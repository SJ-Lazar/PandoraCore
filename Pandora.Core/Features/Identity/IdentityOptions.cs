using System;

namespace Pandora.Core.Features.Identity;

public sealed class IdentityOptions
{
    private int _maxClaims = 32;
    private Func<string, string> _claimTypeNormalizer = static type => type;
    private Func<string, string> _roleNormalizer = static role => role;

    public int MaxClaims
    {
        get => _maxClaims;
        set => _maxClaims = value <= 0 ? 32 : value;
    }

    public Func<string, string> ClaimTypeNormalizer
    {
        get => _claimTypeNormalizer;
        set => _claimTypeNormalizer = value ?? (static type => type);
    }

    public Func<string, string> RoleNormalizer
    {
        get => _roleNormalizer;
        set => _roleNormalizer = value ?? (static role => role);
    }
}
