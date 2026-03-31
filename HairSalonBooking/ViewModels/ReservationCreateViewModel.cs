using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.ViewModels;

public class ReservationCreateViewModel
{
    [Required(ErrorMessage = "Wybierz usługę.")]
    [Display(Name = "Usługa")]
    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "Wybierz termin.")]
    [Display(Name = "Termin")]
    public int? AvailableSlotId { get; set; }

    [StringLength(500, ErrorMessage = "Uwagi mogą mieć maksymalnie 500 znaków.")]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    public List<SelectListItem> ServiceOptions { get; set; } = new();
}