using Microsoft.AspNetCore.Identity;

namespace HairSalonBooking.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    //public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}