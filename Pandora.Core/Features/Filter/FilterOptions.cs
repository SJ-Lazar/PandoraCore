using System;

namespace Pandora.Core.Features.Filter;

public sealed class FilterOptions
{
    private int _maxFilters = 16;
    private Func<string, string> _propertyNameNormalizer = static name => name;

    public int MaxFilters
    {
        get => _maxFilters;
        set => _maxFilters = value <= 0 ? 16 : value;
    }

    public Func<string, string> PropertyNameNormalizer
    {
        get => _propertyNameNormalizer;
        set => _propertyNameNormalizer = value ?? (static name => name);
    }
}
 