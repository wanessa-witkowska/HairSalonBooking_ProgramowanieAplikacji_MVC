using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.ViewModels;

public class ReservationCreateViewModel
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }

    [Required]
    [Display(Name = "Termin")]
    public int AvailableSlotId { get; set; }

    [StringLength(500)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    public List<SelectListItem> SlotOptions { get; set; } = new();
}