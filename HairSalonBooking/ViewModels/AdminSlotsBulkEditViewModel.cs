using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HairSalonBooking.ViewModels;

public class AdminSlotsBulkEditViewModel
{
    public List<AdminSlotsBulkEditRowViewModel> Slots { get; set; } = [];
    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = [];
}

public class AdminSlotsBulkEditRowViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Początek")]
    public DateTime StartTime { get; set; }

    [Required]
    [Display(Name = "Koniec")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Zajęty")]
    public bool IsBooked { get; set; }

    [Required]
    [Display(Name = "Usługa")]
    public int ServiceId { get; set; }
}
