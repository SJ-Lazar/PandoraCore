using System;

namespace Pandora.Core.Features.Pagination;

public sealed record PageRequest
{
    public PageRequest(int? pageNumber = null, int? pageSize = null, PaginationOptions? options = null)
    {
        options ??= new PaginationOptions();

        PageNumber = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
        PageSize = options.NormalizePageSize(pageSize);
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int Offset
    {
        get
        {
            var offset = (long)(PageNumber - 1) * PageSize;
            if (offset <= 0)
            {
                return 0;
            }

            return offset > int.MaxValue ? int.MaxValue : (int)offset;
        }
    }
}
