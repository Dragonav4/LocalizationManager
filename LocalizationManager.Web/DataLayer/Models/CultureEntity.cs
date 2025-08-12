using System.ComponentModel.DataAnnotations;
using LocalizationManager.Web.DataLayer.ViewModels;

namespace LocalizationManager.Web.DataLayer.Models;

public class CultureEntity
{
    [Key] public string Code { get; set; } = null!; // en-US
    [Required] public string DisplayName { get; set; } = null!;
    
}