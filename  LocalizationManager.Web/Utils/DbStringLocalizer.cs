using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using LocalizationManager.Web.DataLayer;

namespace LocalizationManager.Web.Utils;

public class DbStringLocalizer : IStringLocalizer, IAsyncStringLocalizerr
{
    private readonly LocalizationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DbStringLocalizer>? _log;

    //Locks by key of culture just to not bombing DB by a parallel requests
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public DbStringLocalizer(LocalizationDbContext dbContext, IMemoryCache cache, ILogger<DbStringLocalizer>? log = null)
    {
        _db = dbContext;
        _cache = cache;
        _log = log;
    }

    //1.Firstly we check cache
    //2. Caught Semaphore
    //3. Load data in DB
    //4. Put result on cache
    //5. Release Semaphore
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(bool includeParentCultures = true, CancellationToken ct = default)
    {
        var ui = CultureInfo.CurrentUICulture;
        var cacheKey = BuildCacheKey(ui, includeParentCultures);

        //pre-checking cache which wouldn't create a request for DB and waiting for Semaphore if there is an already exist 
        // cache from the first stream
        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached))
            return cached;

        //Initially only one stream can load data for this key's, those who came after should wait
        var sem = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached))
                return cached; // avoiding sitiation double request from 2 streams, the second will see cache

            var codes = GetCultureChain(ui, includeParentCultures);

            var rows = await _db.LocalizationRecords
                .AsNoTracking()
                .Where(r => codes.Contains(r.CultureCode))
                .Select(r => new { r.CultureCode, r.Key, r.Value })
                .ToListAsync(ct)
                .ConfigureAwait(false); 

            var dict = new Dictionary<string, string>(StringComparer.Ordinal); // parent -> child like en, en-ES
            foreach (var code in codes)
            {
                foreach (var r in rows.Where(x => x.CultureCode == code))
                {
                    dict[r.Key] = r.Value ?? r.Key;
                }
            }

            var result = new Dictionary<string, string>(dict); 

            _cache.Set(cacheKey, result,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                    Priority = CacheItemPriority.High,
                    Size = result.Count
                });

            return result;
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<LocalizedString> GetAsync(string key, bool includeParentCultures = true, CancellationToken ct = default)
    {
        var dict = await GetAllAsync(includeParentCultures, ct).ConfigureAwait(false);
        if (dict.TryGetValue(key, out var value))
            return new LocalizedString(key, value, resourceNotFound: false);
        return new LocalizedString(key, key, resourceNotFound: true);
    }

    //update(invalidation) for cache if there is a new one
    public void Invalidate(string cultureCode, bool includeParentCultures = true)
    {
        var ui = new CultureInfo(cultureCode);
        _cache.Remove(BuildCacheKey(ui, true));
        _cache.Remove(BuildCacheKey(ui, false));
        _log?.LogInformation("Localization cache invalidated for {Culture}", cultureCode);
    }

    // ======================= SYNC IStringLocalizer =======================
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var dict = GetAllAsync(includeParentCultures).GetAwaiter().GetResult();
        return dict.Select(kv => new LocalizedString(kv.Key, kv.Value, resourceNotFound: false));
    }

    public LocalizedString this[string name]
    {
        get
        {
            var dict = GetAllAsync(includeParentCultures: true).GetAwaiter().GetResult();
            return dict.TryGetValue(name, out var val)
                ? new LocalizedString(name, val, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var ls = this[name];
            if (!ls.ResourceNotFound && arguments is { Length: > 0 })
            {
                var formatted = string.Format(CultureInfo.CurrentCulture, ls.Value, arguments);
                return new LocalizedString(name, formatted, resourceNotFound: false);
            }
            return ls;
        }
    }

    private static string BuildCacheKey(CultureInfo ui, bool includeParent)
        => $"loc:{ui.Name}:{includeParent}";

    private static string[] GetCultureChain(CultureInfo ui, bool includeParent)
    {
        if (includeParent && !string.IsNullOrEmpty(ui.Parent?.Name))
            return new[] { ui.Parent!.Name, ui.Name }; // parent -> current (order is important)
        return new[] { ui.Name };
    }
}