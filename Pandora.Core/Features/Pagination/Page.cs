using System;
using System.Collections.Generic;

namespace Pandora.Core.Features.Pagination;

public sealed class Page<T>
{
    public Page(IReadOnlyList<T>? items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items ?? Array.Empty<T>();
        PageNumber = pageNumber > 0 ? pageNumber : 1;
        PageSize = pageSize > 0 ? pageSize : 1;
        TotalCount = totalCount < 0 ? 0 : totalCount;
        TotalPages = PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;
}
