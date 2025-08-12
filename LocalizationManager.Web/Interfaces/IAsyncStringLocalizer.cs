using Microsoft.Extensions.Localization;

namespace LocalizationManager.Web.Utils;

public interface IAsyncStringLocalizer
{
    Task<LocalizedString> GetAsync(string key, bool includeParentCultures = true, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(bool includeParentCultures = true, CancellationToken ct = default);
    void Invalidate(string cultureCode, bool includeParentCultures = true);
}