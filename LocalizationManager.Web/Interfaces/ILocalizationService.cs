using LocalizationManager.Web.DataLayer.Models;
using LocalizationManager.Web.DataLayer.ViewModels;

namespace LocalizationManager.Web.Interfaces;

public interface ILocalizationService
{
    Task<IReadOnlyCollection<CultureVm>> GetCulturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetGroupsAsync(string env, string? search = null,
        CancellationToken cancellationToken = default);
    
    Task<PagedResult<LocalizationRecordVm>> GetRecordsAsync(
        string env,
        string culture,
        string? group = null,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<LocalizationRecordVm> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<LocalizationRecordVm> UpsertAsync(
        string env,
        LocalizationRecordUpsertVm vm,
        string? group = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CultureLocalizationView> GetCultureViewAsync(
    string env,
    string culture,
    string? group = null,
    string? search = null,
    CancellationToken cancellationToken = default
        );
    
    void InvalidateCache(string cultureCode);
    
    public record PagedResult<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount);


    Task<int> CopyEnvAsync(
        string fromEnv,
        string toEnv,
        string? group = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default);
    
    Task<string> ExportJsonAsync(
        string env,
        IEnumerable<string> cultures,
        string? group = null,
        CancellationToken cancellationToken = default);
    
    Task<int> ImportJsonAsync(
        string env,
        string json,
        bool overwrite = false,
        string? group = null,
        CancellationToken cancellationToken = default);


    Task<Dictionary<string, Dictionary<string, string>>> GetPivotAsync(
        string env,
        IEnumerable<string> cultures,
        string? group = null,
        string? search = null,
        CancellationToken cancellationToken = default);
    
    Task<string> SanitizeHtmlAsync(string rawHtml, CancellationToken cancellationToken = default);


}