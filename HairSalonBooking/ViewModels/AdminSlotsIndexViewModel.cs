using Microsoft.AspNetCore.Mvc.Rendering;

namespace HairSalonBooking.ViewModels;

public class AdminSlotsIndexViewModel
{
    public IReadOnlyList<AdminSlotsIndexRowViewModel> Slots { get; init; } = [];
    public IReadOnlyList<SelectListItem> ServiceOptions { get; init; } = [];
}

public class AdminSlotsIndexRowViewModel
{
    public int Id { get; init; }
    public int ServiceId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public bool IsBooked { get; init; }
    public string ServiceName { get; init; } = string.Empty;
}
