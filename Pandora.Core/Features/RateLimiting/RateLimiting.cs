using System;
using System.Collections.Concurrent;

namespace Pandora.Core.Features.RateLimiting;

public sealed class RateLimitingOptions
{
    private int _permitLimit = 100;
    private TimeSpan _window = TimeSpan.FromMinutes(1);
    private Func<DateTimeOffset> _nowProvider = static () => DateTimeOffset.UtcNow;

    public int PermitLimit
    {
        get => _permitLimit;
        set => _permitLimit = value <= 0 ? 100 : value;
    }

    public TimeSpan Window
    {
        get => _window;
        set => _window = value <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : value;
    }

    public Func<DateTimeOffset> NowProvider
    {
        get => _nowProvider;
        set => _nowProvider = value ?? (static () => DateTimeOffset.UtcNow);
    }
}

public readonly record struct RateLimitResult(bool IsAllowed, int Remaining, DateTimeOffset ResetAt);

public sealed class RateLimiter<TKey>
{
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<TKey, Bucket> _buckets;

    public RateLimiter(RateLimitingOptions? options = null, IEqualityComparer<TKey>? comparer = null)
    {
        _options = options ?? new RateLimitingOptions();
        _buckets = new ConcurrentDictionary<TKey, Bucket>(comparer ?? EqualityComparer<TKey>.Default);
    }

    public ValueTask<RateLimitResult> CheckAsync(TKey key, int permits = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var bucket = _buckets.GetOrAdd(key, static (_, options) => new Bucket(options.NowProvider()), _options);

        RateLimitResult result;
        lock (bucket)
        {
            result = bucket.TryConsume(permits, _options);
        }

        return ValueTask.FromResult(result);
    }

    private sealed class Bucket
    {
        private int _count;
        private DateTimeOffset _windowStart;

        public Bucket(DateTimeOffset windowStart)
        {
            _windowStart = windowStart;
            _count = 0;
        }

        public RateLimitResult TryConsume(int permits, RateLimitingOptions options)
        {
            var normalizedPermits = permits <= 0 ? 1 : permits;
            var now = options.NowProvider();
            var windowEnd = _windowStart + options.Window;

            if (now >= windowEnd)
            {
                _windowStart = now;
                _count = 0;
                windowEnd = _windowStart + options.Window;
            }

            var limit = options.PermitLimit;
            if (_count + normalizedPermits <= limit)
            {
                _count += normalizedPermits;
                var remaining = Math.Max(0, limit - _count);
                return new RateLimitResult(true, remaining, windowEnd);
            }

            var available = Math.Max(0, limit - _count);
            return new RateLimitResult(false, available, windowEnd);
        }
    }
}
