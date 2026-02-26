using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pandora.Core.Features.Pagination;

namespace Pandora.Tests.Features.Pagination;

public sealed class PaginatorTests
{
    [Test]
    public void Paginate_ReturnsRequestedPage()
    {
        var data = Enumerable.Range(1, 50);

        var page = Paginator.Paginate(data, pageNumber: 2, pageSize: 10);

        Assert.That(page.PageNumber, Is.EqualTo(2));
        Assert.That(page.PageSize, Is.EqualTo(10));
        Assert.That(page.TotalCount, Is.EqualTo(50));
        Assert.That(page.TotalPages, Is.EqualTo(5));
        Assert.That(page.HasPrevious, Is.True);
        Assert.That(page.HasNext, Is.True);
        Assert.That(page.Items, Is.EqualTo(Enumerable.Range(11, 10)));
    }

    [Test]
    public void Paginate_UsesDefaultsAndClamps()
    {
        var options = new PaginationOptions
        {
            DefaultPageSize = 5,
            MaxPageSize = 10
        };

        var page = Paginator.Paginate(Enumerable.Range(1, 25), pageNumber: 0, pageSize: 50, options);

        Assert.That(page.PageNumber, Is.EqualTo(1));
        Assert.That(page.PageSize, Is.EqualTo(10));
        Assert.That(page.TotalPages, Is.EqualTo(3));
        Assert.That(page.Items.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task PaginateAsync_MaterializesAsyncEnumerable()
    {
        var page = await Paginator.PaginateAsync(GenerateAsync(12), pageNumber: 3, pageSize: 4);

        Assert.That(page.Items, Is.EqualTo(new[] { 9, 10, 11, 12 }));
        Assert.That(page.TotalCount, Is.EqualTo(12));
        Assert.That(page.HasNext, Is.False);
        Assert.That(page.HasPrevious, Is.True);
    }

    private static async IAsyncEnumerable<int> GenerateAsync(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            yield return i;
            await Task.Yield();
        }
    }
}
