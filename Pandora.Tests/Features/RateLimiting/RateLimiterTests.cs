using System;
using System.Threading.Tasks;
using Pandora.Core.Features.RateLimiting;

namespace Pandora.Tests.Features.RateLimiting;

public sealed class RateLimiterTests
{
    [Test]
    public async Task CheckAsync_AllowsWithinLimit()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new RateLimitingOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            NowProvider = () => now
        };

        var limiter = new RateLimiter<string>(options);

        var first = await limiter.CheckAsync("client", permits: 1);
        var second = await limiter.CheckAsync("client", permits: 1);
        var third = await limiter.CheckAsync("client", permits: 1);
        var fourth = await limiter.CheckAsync("client", permits: 1);

        Assert.That(first.IsAllowed, Is.True);
        Assert.That(second.IsAllowed, Is.True);
        Assert.That(third.IsAllowed, Is.True);
        Assert.That(third.Remaining, Is.EqualTo(0));
        Assert.That(fourth.IsAllowed, Is.False);
    }

    [Test]
    public async Task CheckAsync_ResetsAfterWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new RateLimitingOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(10),
            NowProvider = () => now
        };

        var limiter = new RateLimiter<string>(options);

        var first = await limiter.CheckAsync("client", permits: 2);
        Assert.That(first.IsAllowed, Is.True);
        Assert.That(first.Remaining, Is.EqualTo(0));

        var blocked = await limiter.CheckAsync("client");
        Assert.That(blocked.IsAllowed, Is.False);

        now = now.AddSeconds(11);
        var afterWindow = await limiter.CheckAsync("client");

        Assert.That(afterWindow.IsAllowed, Is.True);
        Assert.That(afterWindow.Remaining, Is.EqualTo(1));
    }

    [Test]
    public async Task CheckAsync_SeparatesBucketsByKey()
    {
        var options = new RateLimitingOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            NowProvider = () => DateTimeOffset.UtcNow
        };

        var limiter = new RateLimiter<string>(options);

        var clientA = await limiter.CheckAsync("a");
        var clientB = await limiter.CheckAsync("b");
        var clientASecond = await limiter.CheckAsync("a");

        Assert.That(clientA.IsAllowed, Is.True);
        Assert.That(clientB.IsAllowed, Is.True);
        Assert.That(clientASecond.IsAllowed, Is.False);
    }
}
