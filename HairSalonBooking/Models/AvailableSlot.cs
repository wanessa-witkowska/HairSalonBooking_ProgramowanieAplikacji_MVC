using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.Models;

public class AvailableSlot
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

    public Service Service { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}