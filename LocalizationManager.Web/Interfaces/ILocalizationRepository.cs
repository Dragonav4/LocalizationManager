using LocalizationManager.Web.DataLayer.Models;

namespace LocalizationManager.Web.Utils;

public interface ILocalizationRepository
{
    Task<List<CultureEntity>> GetCulturesAsync(CancellationToken cancellationToken);
    Task<List<LocalizationRecordEntity>> GetRecordsAsync(string env, string culture, string? search, int skip, int take, CancellationToken cancellationToken);
    Task<LocalizationRecordEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LocalizationRecordEntity> UpsertAsync(string env, string culture, string key, string? value, CancellationToken cancellationToken);
    Task<LocalizationRecordEntity> DeleteAsync(Guid id, CancellationToken cancellationToken);
    
    
}