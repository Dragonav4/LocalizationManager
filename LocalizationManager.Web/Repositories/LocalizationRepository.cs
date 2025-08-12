using LocalizationManager.Web.DataLayer;
using LocalizationManager.Web.DataLayer.Models;
using LocalizationManager.Web.Utils;
using Microsoft.EntityFrameworkCore;

namespace LocalizationManager.Web.Repositories;

public class LocalizationRepository : ILocalizationRepository
{
    private readonly LocalizationDbContext _db;
    public LocalizationRepository(LocalizationDbContext db) => _db = db;

    public Task<List<CultureEntity>> GetCulturesAsync(CancellationToken cancellationToken)
        => _db.Cultures.AsNoTracking().OrderBy(c => c.Code).ToListAsync(cancellationToken);


    public async Task<List<LocalizationRecordEntity>> GetRecordsAsync(string env, string culture, string? search, int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.LocalizationRecords.AsNoTracking()
            .Where(r => r.Env == env && r.CultureCode == culture);
        if(!string.IsNullOrWhiteSpace(search)) query=query
            .Where(r => 
                r.Key.Contains(search) || (r.Value != null && r.Value.Contains(search)));
        return await query.OrderBy(r => r.Key).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public Task<LocalizationRecordEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.LocalizationRecords.FindAsync(id, cancellationToken).AsTask(); 
    

    public async Task<LocalizationRecordEntity> UpsertAsync(string env, string culture, string key, string? value,
        CancellationToken cancellationToken)
    {
        var entity = await _db.LocalizationRecords
            .FirstOrDefaultAsync(r => r.Env == env 
                                      && r.CultureCode == culture
                                      && r.Key == key,
                cancellationToken);

        if (entity is null)
        {
            entity = new LocalizationRecordEntity
            {
                Id = Guid.NewGuid(),
                Env = env,
                CultureCode = culture,
                Key = key,
                Value = value,
                UpdatedUtc = DateTime.UtcNow
            };
            await _db.LocalizationRecords.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Value = value;
            entity.UpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<LocalizationRecordEntity> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.LocalizationRecords.FindAsync(id, cancellationToken);
        if (entity is null)
            throw new KeyNotFoundException($"Localization record {id} not found.");

        _db.LocalizationRecords.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }
}