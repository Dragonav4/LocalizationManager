using System.Linq;
using System.Text.Json;
using Ganss.Xss;
using LocalizationManager.Web.DataLayer;
using LocalizationManager.Web.DataLayer.Models;
using LocalizationManager.Web.DataLayer.ViewModels;
using LocalizationManager.Web.Interfaces;
using LocalizationManager.Web.Utils;
using LocalizationManager.Web.Utils.Mappings;
using Microsoft.EntityFrameworkCore;

namespace LocalizationManager.Web.Services;

public class LocalizationService : ILocalizationService
{
    private readonly LocalizationDbContext _db;
    private readonly IAsyncStringLocalizer _localizer;
    public LocalizationService(
        LocalizationDbContext db,
        IAsyncStringLocalizer localizer)
    {
        _db = db;
        _localizer = localizer;
    }

    public async Task<IReadOnlyCollection<CultureVm>> GetCulturesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Cultures
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => c.ToVm())
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task<IReadOnlyCollection<string>> GetGroupsAsync(string env, string? search = null, CancellationToken cancellationToken = default)
    {
        var q = _db.LocalizationRecords
            .AsNoTracking()
            .Where(r => r.Env == env && r.GroupName != null);
        if(!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.GroupName!.Contains(search));
        var groups = await q.Select(r => r.GroupName!)
            .Distinct()
            .OrderBy(g=> g)
            .ToListAsync(cancellationToken);
        return groups;
    }

    public async Task<ILocalizationService.PagedResult<LocalizationRecordVm>> GetRecordsAsync(string env, string culture, string? group = null, string? search = null, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize <=0) pageSize = 10;
        
        var q = _db.LocalizationRecords
            .AsNoTracking()
            .Where(r => r.Env == env && r.CultureCode == culture);
        if(!string.IsNullOrWhiteSpace(group))
            q = q.Where(r => r.GroupName == group);
        if(!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.Key.Contains(search)
                             || (r.GroupName != null && r.GroupName.Contains(search))
                             || (r.Value != null && r.Value.Contains(search)));
        var total = await q.CountAsync(cancellationToken);

        var items = await q.OrderBy(r => r.Key)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.ToVm())
            .ToListAsync(cancellationToken);

        return new ILocalizationService.PagedResult<LocalizationRecordVm>(items, page, pageSize, total);
    }

    public async Task<LocalizationRecordVm> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _db.LocalizationRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (e is null)
            throw new KeyNotFoundException($"Unable to find localization record with id: {id}");

        return e.ToVm();
    }

    public async Task<LocalizationRecordVm> UpsertAsync(string env, LocalizationRecordUpsertVm vm, string? group = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.LocalizationRecords
            .FirstOrDefaultAsync(r => r.Env == env &&
                                      r.CultureCode == vm.CultureCode
                                      && r.Key == vm.Key, cancellationToken);
        if (entity == null)
        {
            entity = new LocalizationRecordEntity()
            {
                Id = Guid.NewGuid(),
                Env = env,
                CultureCode = vm.CultureCode,
                Key = vm.Key,
                Value = vm.Value,
                GroupName = group,
                UpdatedUtc = DateTime.UtcNow
            };
            await _db.LocalizationRecords.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Value = vm.Value;
            if(group != null) entity.GroupName = group;
            entity.UpdatedUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        _localizer.Invalidate(vm.CultureCode);
        return entity.ToVm();
        
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LocalizationRecords.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;
        _db.LocalizationRecords.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _localizer.Invalidate(entity.CultureCode);
        return true;

    }

    public async Task<CultureLocalizationView> GetCultureViewAsync(string env, string culture, string? group = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var cultureVm = await _db.Cultures
                            .AsNoTracking()
                            .Where(c => c.Code == culture)
                            .Select(c => c.ToVm())
                            .FirstOrDefaultAsync(cancellationToken)
                        ?? new CultureVm(culture, culture); // fallback
        var q = _db.LocalizationRecords
            .AsNoTracking()
            .Where(r => r.Env == env && r.CultureCode == culture);
        
        if(!string.IsNullOrWhiteSpace(group))
            q = q.Where(r=> r.GroupName == group);

        if(!string.IsNullOrWhiteSpace(search))
            q = q.Where(r=> r.Key.Contains(search) || (r.Value !=null && r.Value.Contains(search)));
        
        var items = await q.OrderBy(r=> r.Key)
            .Select(r => r.ToVm())
            .ToListAsync(cancellationToken);
        return CultureLocalizationView.From(cultureVm, items);
    }

    public void InvalidateCache(string cultureCode)
    {
        _localizer.Invalidate(cultureCode);
    }

    public async Task<int> CopyEnvAsync(string fromEnv, string toEnv, string? group = null, bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var src = await _db.LocalizationRecords
            .AsNoTracking()
            .Where(r => r.Env == fromEnv && (group == null || r.GroupName == group))
            .ToListAsync(cancellationToken);

        if (src.Count == 0) return 0;

        var cultures = src.Select(s => s.CultureCode).Distinct().ToList();
        var keys = src.Select(s => s.Key).Distinct().ToList();

        var dst = await _db.LocalizationRecords
            .Where(r => r.Env == toEnv && cultures.Contains(r.CultureCode) && keys.Contains(r.Key))
            .ToListAsync(cancellationToken);

        var map = dst.ToDictionary(d => (d.CultureCode, d.Key));

        var affected = 0;
        foreach (var s in src)
        {
            if (!map.TryGetValue((s.CultureCode, s.Key), out var d))
            {
                await _db.LocalizationRecords.AddAsync(new LocalizationRecordEntity
                {
                    Id = Guid.NewGuid(),
                    Env = toEnv,
                    CultureCode = s.CultureCode,
                    Key = s.Key,
                    Value = s.Value,
                    GroupName = s.GroupName,
                    UpdatedUtc = DateTime.UtcNow
                }, cancellationToken);
                affected++;
            }
            else if (overwrite)
            {
                d.Value = s.Value;
                d.GroupName = s.GroupName;
                d.UpdatedUtc = DateTime.UtcNow;
                affected++;
            }
        }

        if (affected > 0)
            await _db.SaveChangesAsync(cancellationToken);

        foreach (var c in src.Select(s => s.CultureCode).Distinct())
            _localizer.Invalidate(c);

        return affected;
    }

    public Task<string> ExportJsonAsync(string env, IEnumerable<string> cultures, string? group = null,
        CancellationToken cancellationToken = default)
    {
        var set = new HashSet<string>(cultures);
        return ExportAsync();

        async Task<string> ExportAsync()
        {
            var q = _db.LocalizationRecords.AsNoTracking()
                .Where(r => r.Env == env && set.Contains(r.CultureCode));
            if (!string.IsNullOrWhiteSpace(group))
                q = q.Where(r => r.GroupName == group);

            var list = await q
                .OrderBy(r => r.CultureCode).ThenBy(r => r.Key)
                .ToListAsync(cancellationToken);

            var dto = list
                .GroupBy(r => r.CultureCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(x => x.Key, x => x.Value ?? string.Empty));

            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public Task<int> ImportJsonAsync(string env, string json, bool overwrite = false, string? group = null,
        CancellationToken cancellationToken = default)
    {
        return ImportAsync();

        async Task<int> ImportAsync()
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                       ?? new Dictionary<string, Dictionary<string, string>>();

            var affected = 0;
            foreach (var (culture, pairs) in data)
            {
                foreach (var (key, value) in pairs)
                {
                    var e = await _db.LocalizationRecords
                        .FirstOrDefaultAsync(r => r.Env == env && r.CultureCode == culture && r.Key == key, cancellationToken);

                    if (e == null)
                    {
                        await _db.LocalizationRecords.AddAsync(new LocalizationRecordEntity
                        {
                            Id = Guid.NewGuid(),
                            Env = env,
                            CultureCode = culture,
                            Key = key,
                            Value = value,
                            GroupName = group,
                            UpdatedUtc = DateTime.UtcNow
                        }, cancellationToken);
                        affected++;
                    }
                    else if (overwrite)
                    {
                        e.Value = value;
                        if (group != null) e.GroupName = group;
                        e.UpdatedUtc = DateTime.UtcNow;
                        affected++;
                    }
                }
            }

            if (affected > 0)
                await _db.SaveChangesAsync(cancellationToken);

            foreach (var c in data.Keys)
                _localizer.Invalidate(c);

            return affected;
        }
    }

    public Task<Dictionary<string, Dictionary<string, string>>> GetPivotAsync(string env, IEnumerable<string> cultures, string? group = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        return PivotAsync();

        async Task<Dictionary<string, Dictionary<string, string>>> PivotAsync()
        {
            var set = new HashSet<string>(cultures);
            var q = _db.LocalizationRecords.AsNoTracking()
                .Where(r => r.Env == env && set.Contains(r.CultureCode));

            if (!string.IsNullOrWhiteSpace(group))
                q = q.Where(r => r.GroupName == group);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r => r.Key.Contains(search) || (r.Value != null && r.Value.Contains(search)));

            var list = await q.ToListAsync(cancellationToken);

            var keys = list.Select(r => r.Key).Distinct();
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var inner = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var c in set)
                {
                    inner[c] = list.FirstOrDefault(r => r.Key == key && r.CultureCode == c)?.Value ?? string.Empty;
                }
                result[key] = inner;
            }

            return result;
        }
    }

    public Task<string> SanitizeHtmlAsync(string rawHtml, CancellationToken cancellationToken = default)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedSchemes.Remove("data"); 
        var safe = sanitizer.Sanitize(rawHtml ?? string.Empty);
        return Task.FromResult(safe);
    }
}