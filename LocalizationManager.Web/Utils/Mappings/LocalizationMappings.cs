using LocalizationManager.Web.DataLayer.Models;
using LocalizationManager.Web.DataLayer.ViewModels;

namespace LocalizationManager.Web.Utils.Mappings;

public static class LocalizationMappings
{
    public static CultureVm ToVm(this CultureEntity entity) => new (entity.Code, entity.DisplayName);

    public static LocalizationRecordVm ToVm(this LocalizationRecordEntity entity)
        => new LocalizationRecordVm(
            entity.Id,
            entity.CultureCode,
            entity.Key,
            entity.Value,
            entity.UpdatedUtc);
}