using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.Models;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Data wizyty")]
    public DateTime ReservationDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(500)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public AppUser User { get; set; } = null!;

    [Required]
    public int ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    [Required]
    public int AvailableSlotId { get; set; }

    public AvailableSlot AvailableSlot { get; set; } = null!;
}