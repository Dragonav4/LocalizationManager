
namespace LocalizationManager.Web.DataLayer.ViewModels;

public class CultureLocalizationView
{
    public string CultureCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public RecordView[] Records { get; set; } = Array.Empty<RecordView>();

    public class RecordView
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    public static CultureLocalizationView From(
        CultureVm culture,
        IEnumerable<LocalizationRecordVm> items)
    {
        return new CultureLocalizationView
        {
            CultureCode = culture.Code,
            DisplayName = culture.DisplayName,
            Records = items
                .OrderBy(i => i.Key)
                .Select(i => new RecordView
                {
                    Id = i.Id,
                    Key = i.Key,
                    Value = i.Value,
                    UpdatedUtc = i.UpdatedUtc
                })
                .ToArray()
        };
    }
}