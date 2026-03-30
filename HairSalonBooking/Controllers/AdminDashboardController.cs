using HairSalonBooking.Data;
using HairSalonBooking.Models;
using HairSalonBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            TotalReservations = await _context.Reservations.CountAsync(),
            PendingReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Pending),
            ApprovedReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Approved),
            RejectedReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Rejected),
            CancelledReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled),
            ActiveServices = await _context.Services.CountAsync(s => s.IsActive),
            FreeSlots = await _context.AvailableSlots.CountAsync(s => !s.IsBooked)
        };

        return View(model);
    }
}