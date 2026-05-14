using HairSalonBooking.Data;
using HairSalonBooking.Models;
using HairSalonBooking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Controllers;

[Authorize(Roles = "Admin")]
public class AdminReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;


    public AdminReservationsController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index(ReservationStatus? status)
    {
        var query = _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Service)
            .Include(r => r.AvailableSlot)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        ViewBag.Status = status;

        var reservations = await query
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        return View(reservations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.AvailableSlot)
            .Include(r => r.User)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            TempData["Success"] = "Tylko rezerwacje oczekujące mogą zostać zatwierdzone.";
            return RedirectToAction(nameof(Index));
        }

        reservation.Status = ReservationStatus.Approved;
        reservation.AvailableSlot.IsBooked = true;

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(reservation.User?.Email))
        {
            await _emailService.SendAsync(
                reservation.User.Email,
                "Twoja rezerwacja została zatwierdzona",
                $"<h2>Rezerwacja zatwierdzona</h2><p>Usługa: <strong>{reservation.Service.Name}</strong></p><p>Termin: <strong>{reservation.ReservationDate:dd.MM.yyyy HH:mm}</strong></p>");
        }

        TempData["Success"] = "Rezerwacja została zatwierdzona.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.AvailableSlot)
            .Include(r => r.User)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.Approved)
        {
            TempData["Success"] = "Tylko rezerwacje oczekujące lub zatwierdzone mogą zostać odrzucone.";
            return RedirectToAction(nameof(Index));
        }

        reservation.Status = ReservationStatus.Rejected;
        reservation.AvailableSlot.IsBooked = false;

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(reservation.User?.Email))
        {
            await _emailService.SendAsync(
                reservation.User.Email,
                "Twoja rezerwacja została odrzucona",
                $"<h2>Rezerwacja odrzucona</h2><p>Usługa: <strong>{reservation.Service.Name}</strong></p><p>Termin: <strong>{reservation.ReservationDate:dd.MM.yyyy HH:mm}</strong></p><p>Skontaktuj się z salonem, jeśli chcesz ustalić inny termin.</p>");
        }

        TempData["Success"] = "Rezerwacja została odrzucona.";
        return RedirectToAction(nameof(Index));
    }
}
