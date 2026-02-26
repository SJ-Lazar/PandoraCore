using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Pandora.Core.Features.Search;

public static class Search
{
    public static IEnumerable<T> In<T>(IEnumerable<T> source, string? term, params Func<T, string?>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(term) || selectors is null || selectors.Length == 0)
        {
            return source;
        }

        var normalized = term.Trim();
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        return Filter();

        IEnumerable<T> Filter()
        {
            foreach (var item in source)
            {
                if (Matches(item, selectors, normalized, comparison))
                {
                    yield return item;
                }
            }
        }
    }

    public static async IAsyncEnumerable<T> InAsync<T>(IAsyncEnumerable<T> source, string? term, Func<T, string?>[] selectors, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(term) || selectors is null || selectors.Length == 0)
        {
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        var normalized = term.Trim();
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Matches(item, selectors, normalized, comparison))
            {
                yield return item;
            }
        }
    }

    private static bool Matches<T>(T item, Func<T, string?>[] selectors, string term, StringComparison comparison)
    {
        foreach (var selector in selectors)
        {
            var value = selector?.Invoke(item);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (value.IndexOf(term, comparison) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
