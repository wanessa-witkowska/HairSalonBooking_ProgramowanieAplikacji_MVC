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

    public async Task<IActionResult> MyReservations(ReservationStatus? status)
    {
        var userId = _userManager.GetUserId(User)!;

        var query = _context.Reservations
            .Include(r => r.Service)
            .Include(r => r.AvailableSlot)
            .Where(r => r.UserId == userId)
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

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var reservation = await _context.Reservations
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            TempData["Success"] = "Edytować można tylko rezerwacje oczekujące.";
            return RedirectToAction(nameof(MyReservations));
        }

        var model = new ReservationEditViewModel
        {
            ReservationId = reservation.Id,
            ServiceId = reservation.ServiceId,
            ServiceName = reservation.Service.Name,
            AvailableSlotId = reservation.AvailableSlotId,
            Notes = reservation.Notes,
            SlotOptions = await BuildSlotOptionsAsync(reservation.ServiceId, reservation.AvailableSlotId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReservationEditViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        var reservation = await _context.Reservations
            .Include(r => r.Service)
            .Include(r => r.AvailableSlot)
            .FirstOrDefaultAsync(r => r.Id == model.ReservationId && r.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return RedirectToAction(nameof(MyReservations));
        }

        var selectedSlot = await _context.AvailableSlots
            .FirstOrDefaultAsync(s =>
                s.Id == model.AvailableSlotId &&
                s.ServiceId == reservation.ServiceId &&
                (!s.IsBooked || s.Id == reservation.AvailableSlotId) &&
                s.StartTime > DateTime.Now);

        if (selectedSlot is null)
        {
            ModelState.AddModelError(nameof(model.AvailableSlotId), "Wybrany termin nie jest dostępny.");
        }

        if (!ModelState.IsValid)
        {
            model.ServiceName = reservation.Service.Name;
            model.SlotOptions = await BuildSlotOptionsAsync(reservation.ServiceId, reservation.AvailableSlotId);
            return View(model);
        }

        if (reservation.AvailableSlotId != model.AvailableSlotId)
        {
            reservation.AvailableSlot.IsBooked = false;
            selectedSlot!.IsBooked = true;
            reservation.AvailableSlotId = selectedSlot.Id;
            reservation.ReservationDate = selectedSlot.StartTime;
        }

        reservation.Notes = model.Notes;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Rezerwacja została zaktualizowana.";
        return RedirectToAction(nameof(MyReservations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var reservation = await _context.Reservations
            .Include(r => r.AvailableSlot)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Rejected)
        {
            return RedirectToAction(nameof(MyReservations));
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.AvailableSlot.IsBooked = false;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Rezerwacja została anulowana.";
        return RedirectToAction(nameof(MyReservations));
    }

    public IActionResult Calendar()
    {
        return View();
    }

    public async Task<IActionResult> Events(DateTime start, DateTime end)
    {
        var events = await _context.AvailableSlots
            .Include(s => s.Service)
            .Where(s => s.StartTime >= start && s.EndTime <= end)
            .Select(s => new
            {
                title = s.Service.Name + (s.IsBooked ? " (zajęty)" : " (wolny)"),
                start = s.StartTime,
                end = s.EndTime,
                className = s.IsBooked ? "calendar-booked" : "calendar-free"
            })
            .ToListAsync();

        return Json(events);
    }
}