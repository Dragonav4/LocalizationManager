using System.ComponentModel.DataAnnotations;

namespace LocalizationManager.Web.DataLayer.Models;

public class CultureEntity
{
    [Key] public string Code { get; set; } = null!; // en-US
    [Required] public string DisplayName { get; set; } = null!;
}