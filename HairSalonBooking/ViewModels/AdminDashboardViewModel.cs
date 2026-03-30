namespace HairSalonBooking.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalReservations { get; set; }
    public int PendingReservations { get; set; }
    public int ApprovedReservations { get; set; }
    public int RejectedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public int ActiveServices { get; set; }
    public int FreeSlots { get; set; }
}