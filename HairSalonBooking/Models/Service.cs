using System.ComponentModel.DataAnnotations;

namespace HairSalonBooking.Models;

public class Service
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Nazwa usługi")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Opis")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10000)]
    [Display(Name = "Cena")]
    public decimal Price { get; set; }

    [Range(15, 480)]
    [Display(Name = "Czas trwania (min)")]
    public int DurationMinutes { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Kategoria")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Aktywna")]
    public bool IsActive { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<AvailableSlot> AvailableSlots { get; set; } = new List<AvailableSlot>();
}