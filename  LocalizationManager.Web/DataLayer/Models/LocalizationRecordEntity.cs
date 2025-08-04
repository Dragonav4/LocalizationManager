using System.ComponentModel.DataAnnotations;

namespace LocalizationManager.Web.DataLayer.Models;

public class LocalizationRecordEntity
{
    [Key] public Guid Id { get; set; }
    [Required] public string CultureCode { get; set; } = null!;
    [Required] public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public CultureEntity? Culture { get; set; }
}