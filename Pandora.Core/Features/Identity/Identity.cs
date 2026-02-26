using System;
using System.Collections.Generic;

namespace Pandora.Core.Features.Identity;

public sealed class Identity<T>
{
    private readonly IdentityOptions _options;
    private readonly HashSet<string> _roles;
    private readonly Dictionary<string, string> _claims;

    public Identity(
        T id,
        string name,
        IEnumerable<string>? roles = null,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        IdentityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Id = id;
        _options = options ?? new IdentityOptions();

        _roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    continue;
                }

                _roles.Add(_options.RoleNormalizer(role));
            }
        }

        _claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (claims is not null)
        {
            foreach (var claim in claims)
            {
                if (string.IsNullOrWhiteSpace(claim.Key) || _claims.Count >= _options.MaxClaims)
                {
                    continue;
                }

                var key = _options.ClaimTypeNormalizer(claim.Key);
                _claims[key] = claim.Value ?? string.Empty;
            }
        }
    }

    public T Id { get; }

    public string Name { get; }

    public IReadOnlyCollection<string> Roles => _roles;

    public IReadOnlyDictionary<string, string> Claims => _claims;

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return _roles.Contains(_options.RoleNormalizer(role));
    }

    public bool HasClaim(string type, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        var normalized = _options.ClaimTypeNormalizer(type);
        if (!_claims.TryGetValue(normalized, out var claimValue))
        {
            return false;
        }

        return value is null || string.Equals(claimValue, value, StringComparison.Ordinal);
    }
}
