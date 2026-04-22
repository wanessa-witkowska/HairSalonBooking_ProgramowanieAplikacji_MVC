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

    public async Task<IActionResult> Create(int? serviceId)
    {
        var activeServices = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (!activeServices.Any())
        {
            TempData["Success"] = "Brak aktywnych usług do rezerwacji.";
            return RedirectToAction("Index", "Services");
        }

        var selectedServiceId = serviceId.HasValue && activeServices.Any(s => s.Id == serviceId.Value)
            ? serviceId.Value
            : activeServices.First().Id;

        var model = new ReservationCreateViewModel
        {
            ServiceId = selectedServiceId,
            ServiceOptions = await BuildServiceOptionsAsync(selectedServiceId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(int serviceId, int? selectedSlotId = null)
    {
        var serviceExists = await _context.Services
            .AnyAsync(s => s.Id == serviceId && s.IsActive);

        if (!serviceExists)
        {
            return Json(new List<object>());
        }

        var candidateSlots = await _context.AvailableSlots
            .Where(s =>
                s.ServiceId == serviceId &&
                s.StartTime > DateTime.Now &&
                (!s.IsBooked || s.Id == selectedSlotId))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (!candidateSlots.Any())
        {
            return Json(new List<object>());
        }

        var rangeStart = candidateSlots.Min(s => s.StartTime);
        var rangeEnd = candidateSlots.Max(s => s.EndTime);

        var blockedRanges = await GetBlockedRangesAsync(rangeStart, rangeEnd);

        var availableSlots = candidateSlots
            .Where(slot =>
                selectedSlotId.HasValue && slot.Id == selectedSlotId.Value
                || !blockedRanges.Any(block =>
                    Overlaps(slot.StartTime, slot.EndTime, block.Start, block.End)))
            .Select(slot => new
            {
                id = slot.Id,
                text = $"{slot.StartTime:dd.MM.yyyy HH:mm} - {slot.EndTime:HH:mm}"
            })
            .ToList();

        return Json(availableSlots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model)
    {
        if (!model.ServiceId.HasValue)
        {
            ModelState.AddModelError(nameof(model.ServiceId), "Wybierz usługę.");
        }

        if (!model.AvailableSlotId.HasValue)
        {
            ModelState.AddModelError(nameof(model.AvailableSlotId), "Wybierz termin.");
        }

        Service? service = null;

        if (model.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == model.ServiceId.Value && s.IsActive);

            if (service is null)
            {
                ModelState.AddModelError(nameof(model.ServiceId), "Wybrana usługa nie istnieje.");
            }
        }

        AvailableSlot? slot = null;

        if (model.ServiceId.HasValue && model.AvailableSlotId.HasValue)
        {
            slot = await _context.AvailableSlots
                .FirstOrDefaultAsync(s =>
                    s.Id == model.AvailableSlotId.Value &&
                    s.ServiceId == model.ServiceId.Value &&
                    !s.IsBooked &&
                    s.StartTime > DateTime.Now);

            if (slot is null)
            {
                ModelState.AddModelError(nameof(model.AvailableSlotId), "Wybrany termin nie jest już dostępny.");
            }
            else
            {
                var hasConflict = await HasConflictingReservationAsync(slot.StartTime, slot.EndTime);

                if (hasConflict)
                {
                    ModelState.AddModelError(nameof(model.AvailableSlotId),
                        "W wybranych godzinach istnieje już aktywna rezerwacja dla innej usługi.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            model.ServiceOptions = await BuildServiceOptionsAsync(model.ServiceId);
            return View(model);
        }

        var reservation = new Reservation
        {
            ReservationDate = slot!.StartTime,
            Status = ReservationStatus.Pending,
            Notes = model.Notes,
            UserId = _userManager.GetUserId(User)!,
            ServiceId = service!.Id,
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
            SlotOptions = await BuildSlotOptionsAsync(reservation.ServiceId, reservation.AvailableSlotId, reservation.Id)
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
        else
        {
            var hasConflict = await HasConflictingReservationAsync(
                selectedSlot.StartTime,
                selectedSlot.EndTime,
                reservation.Id);

            if (hasConflict)
            {
                ModelState.AddModelError(nameof(model.AvailableSlotId),
                    "W wybranych godzinach istnieje już aktywna rezerwacja dla innej usługi.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.ServiceName = reservation.Service.Name;
            model.SlotOptions = await BuildSlotOptionsAsync(reservation.ServiceId, reservation.AvailableSlotId, reservation.Id);
            return View(model);
        }

        if (reservation.AvailableSlotId != model.AvailableSlotId)
        {
            reservation.AvailableSlot.IsBooked = false;
            reservation.AvailableSlotId = selectedSlot!.Id;
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

    public async Task<IActionResult> Calendar()
    {
        ViewBag.ServiceOptions = await BuildServiceOptionsAsync();
        return View();
    }

    public async Task<IActionResult> Events(DateTime start, DateTime end)
    {
        var events = await _context.AvailableSlots
            .Include(s => s.Service)
            .Where(s =>
                s.IsBooked &&
                s.StartTime >= start &&
                s.EndTime <= end)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                title = $"{s.StartTime:HH:mm} {s.Service.Name}",
                start = s.StartTime,
                end = s.EndTime,
                className = "calendar-booked-only"
            })
            .ToListAsync();

        return Json(events);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableSlotsForDay(int serviceId, DateTime date)
    {
        var dayStart = date.Date.AddHours(9);
        var dayEnd = date.Date.AddHours(21);

        var minStart = date.Date == DateTime.Today
            ? DateTime.Now
            : dayStart;

        var serviceExists = await _context.Services
            .AnyAsync(s => s.Id == serviceId && s.IsActive);

        if (!serviceExists)
        {
            return Json(new List<object>());
        }

        var candidateSlots = await _context.AvailableSlots
            .Where(s =>
                s.ServiceId == serviceId &&
                !s.IsBooked &&
                s.StartTime >= minStart &&
                s.EndTime <= dayEnd)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (!candidateSlots.Any())
        {
            return Json(new List<object>());
        }

        var blockedRanges = await GetBlockedRangesAsync(dayStart, dayEnd);

        var availableSlots = candidateSlots
            .Where(slot => !blockedRanges.Any(block =>
                Overlaps(slot.StartTime, slot.EndTime, block.Start, block.End)))
            .Select(slot => new
            {
                id = slot.Id,
                text = $"{slot.StartTime:HH:mm} - {slot.EndTime:HH:mm}"
            })
            .ToList();

        return Json(availableSlots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromCalendar(int serviceId, int availableSlotId, string? notes)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);

        if (service is null)
        {
            TempData["CalendarError"] = "Wybrana usługa nie istnieje.";
            return RedirectToAction(nameof(Calendar));
        }

        var slot = await _context.AvailableSlots
            .FirstOrDefaultAsync(s =>
                s.Id == availableSlotId &&
                s.ServiceId == serviceId &&
                !s.IsBooked &&
                s.StartTime > DateTime.Now);

        if (slot is null)
        {
            TempData["CalendarError"] = "Wybrany termin nie jest już dostępny.";
            return RedirectToAction(nameof(Calendar));
        }

        var hasConflict = await HasConflictingReservationAsync(slot.StartTime, slot.EndTime);

        if (hasConflict)
        {
            TempData["CalendarError"] = "W wybranych godzinach istnieje już aktywna rezerwacja dla innej usługi.";
            return RedirectToAction(nameof(Calendar));
        }

        var reservation = new Reservation
        {
            ReservationDate = slot!.StartTime,
            Status = ReservationStatus.Pending,
            Notes = notes,
            UserId = _userManager.GetUserId(User)!,
            ServiceId = service.Id,
            AvailableSlotId = slot.Id
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Rezerwacja została utworzona i oczekuje na zatwierdzenie.";
        return RedirectToAction(nameof(MyReservations));
    }

    private async Task<List<SelectListItem>> BuildServiceOptionsAsync(int? selectedServiceId = null)
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return services.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.Name} - {s.Price:0.00} zł ({s.DurationMinutes} min)",
            Selected = selectedServiceId.HasValue && s.Id == selectedServiceId.Value
        }).ToList();
    }

    private async Task<List<SelectListItem>> BuildSlotOptionsAsync(
    int serviceId,
    int? selectedSlotId = null,
    int? excludeReservationId = null)
    {
        var slots = await _context.AvailableSlots
            .Where(s =>
                s.ServiceId == serviceId &&
                s.StartTime > DateTime.Now &&
                (!s.IsBooked || s.Id == selectedSlotId))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (!slots.Any())
        {
            return new List<SelectListItem>();
        }

        var rangeStart = slots.Min(s => s.StartTime);
        var rangeEnd = slots.Max(s => s.EndTime);

        var blockedRanges = await GetBlockedRangesAsync(rangeStart, rangeEnd, excludeReservationId);

        var filteredSlots = slots
            .Where(slot =>
                selectedSlotId.HasValue && slot.Id == selectedSlotId.Value
                || !blockedRanges.Any(block =>
                    Overlaps(slot.StartTime, slot.EndTime, block.Start, block.End)))
            .ToList();

        return filteredSlots.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.StartTime:dd.MM.yyyy HH:mm} - {s.EndTime:HH:mm}",
            Selected = selectedSlotId.HasValue && s.Id == selectedSlotId.Value
        }).ToList();
    }

    private static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
    {
        return startA < endB && endA > startB;
    }

    private async Task<List<(DateTime Start, DateTime End)>> GetBlockedRangesAsync(
        DateTime rangeStart,
        DateTime rangeEnd,
        int? excludeReservationId = null)
    {
        var query = _context.Reservations
            .Include(r => r.AvailableSlot)
            .Where(r =>
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved) &&
                r.ReservationDate < rangeEnd &&
                r.AvailableSlot.EndTime > rangeStart);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        var reservations = await query
            .Select(r => new
            {
                Start = r.ReservationDate,
                End = r.AvailableSlot.EndTime
            })
            .ToListAsync();

        return reservations
            .Select(r => (r.Start, r.End))
            .ToList();
    }

    private async Task<bool> HasConflictingReservationAsync(
        DateTime slotStart,
        DateTime slotEnd,
        int? excludeReservationId = null)
    {
        var blockedRanges = await GetBlockedRangesAsync(slotStart, slotEnd, excludeReservationId);

        return blockedRanges.Any(r => Overlaps(slotStart, slotEnd, r.Start, r.End));
    }
}
