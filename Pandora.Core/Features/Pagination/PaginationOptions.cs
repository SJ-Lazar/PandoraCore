using System;

namespace Pandora.Core.Features.Pagination;

public sealed class PaginationOptions
{
    private int _defaultPageSize = 25;
    private int _maxPageSize = 100;

    public int DefaultPageSize
    {
        get => _defaultPageSize;
        set => _defaultPageSize = value <= 0 ? 25 : value;
    }

    public int MaxPageSize
    {
        get => _maxPageSize;
        set => _maxPageSize = value <= 0 ? 100 : value;
    }

    internal int NormalizePageSize(int? requested)
    {
        var size = requested.GetValueOrDefault(_defaultPageSize);
        if (size <= 0)
        {
            size = _defaultPageSize;
        }

        return Math.Min(size, _maxPageSize);
    }
}
