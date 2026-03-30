using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.ViewModels;

public class ReservationEditViewModel
{
    public int ReservationId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Termin")]
    public int AvailableSlotId { get; set; }

    [StringLength(500)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    public List<SelectListItem> SlotOptions { get; set; } = new();
}