using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Pandora.Core.Features.Pagination;

public static class Paginator
{
    public static Page<T> Paginate<T>(IEnumerable<T> source, int? pageNumber = null, int? pageSize = null, PaginationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var request = new PageRequest(pageNumber, pageSize, options);
        return Paginate(source, request);
    }

    public static Page<T> Paginate<T>(IEnumerable<T> source, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        var materialized = source as IReadOnlyList<T> ?? source.ToList();
        return CreatePage(materialized, request);
    }

    public static async Task<Page<T>> PaginateAsync<T>(IAsyncEnumerable<T> source, int? pageNumber = null, int? pageSize = null, PaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var request = new PageRequest(pageNumber, pageSize, options);
        return await PaginateAsync(source, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Page<T>> PaginateAsync<T>(IAsyncEnumerable<T> source, PageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        var items = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        return CreatePage(items, request);
    }

    private static Page<T> CreatePage<T>(IReadOnlyList<T> items, PageRequest request)
    {
        var offset = request.Offset;
        var pageItems = items.Skip(offset).Take(request.PageSize).ToList();
        return new Page<T>(pageItems, request.PageNumber, request.PageSize, items.Count);
    }
}
