using HairSalonBooking.Data;
using HairSalonBooking.Models;
using HairSalonBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ReservationsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Create(int serviceId)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);

        if (service is null)
        {
            return NotFound();
        }

        var model = new ReservationCreateViewModel
        {
            ServiceId = service.Id,
            ServiceName = service.Name,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            SlotOptions = await BuildSlotOptionsAsync(service.Id)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == model.ServiceId && s.IsActive);

        if (service is null)
        {
            return NotFound();
        }

        var slot = await _context.AvailableSlots
            .FirstOrDefaultAsync(s =>
                s.Id == model.AvailableSlotId &&
                s.ServiceId == model.ServiceId &&
                !s.IsBooked &&
                s.StartTime > DateTime.Now);

        if (slot is null)
        {
            ModelState.AddModelError(nameof(model.AvailableSlotId), "Wybrany termin nie jest już dostępny.");
        }

        if (!ModelState.IsValid)
        {
            model.ServiceName = service.Name;
            model.Price = service.Price;
            model.DurationMinutes = service.DurationMinutes;
            model.SlotOptions = await BuildSlotOptionsAsync(service.Id);
            return View(model);
        }

        slot!.IsBooked = true;

        var reservation = new Reservation
        {
            ReservationDate = slot.StartTime,
            Status = ReservationStatus.Pending,
            Notes = model.Notes,
            UserId = _userManager.GetUserId(User)!,
            ServiceId = service.Id,
            AvailableSlotId = slot.Id
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Rezerwacja została utworzona i oczekuje na zatwierdzenie.";
        return RedirectToAction(nameof(MyReservations));
    }

    public async Task<IActionResult> MyReservations()
    {
        var userId = _userManager.GetUserId(User)!;

        var reservations = await _context.Reservations
            .Include(r => r.Service)
            .Include(r => r.AvailableSlot)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        return View(reservations);
    }

    private async Task<List<SelectListItem>> BuildSlotOptionsAsync(int serviceId, int? selectedSlotId = null)
    {
        var slots = await _context.AvailableSlots
            .Where(s =>
                s.ServiceId == serviceId &&
                s.StartTime > DateTime.Now &&
                (!s.IsBooked || s.Id == selectedSlotId))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        return slots.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.StartTime:dd.MM.yyyy HH:mm} - {s.EndTime:HH:mm}",
            Selected = s.Id == selectedSlotId
        }).ToList();
    }
}