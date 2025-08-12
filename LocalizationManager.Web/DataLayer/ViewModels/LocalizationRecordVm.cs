using System;

namespace LocalizationManager.Web.DataLayer.ViewModels;

public record LocalizationRecordVm(
    Guid Id,
    string CultureCode,
    string Key,
    string? Value,
    DateTime UpdatedUtc
);



