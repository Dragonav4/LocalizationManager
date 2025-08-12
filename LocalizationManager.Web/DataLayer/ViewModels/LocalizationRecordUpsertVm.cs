using System.ComponentModel.DataAnnotations;

namespace LocalizationManager.Web.DataLayer.ViewModels;

public class LocalizationRecordUpsertVm
{
    [Required] public string CultureCode { get; init; } = null!;
    [Required] public string Key { get; init; } = null!;
    public string? Value { get; init; }
}