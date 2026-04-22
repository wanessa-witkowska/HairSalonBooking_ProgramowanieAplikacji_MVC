using System.Globalization;
using HairSalonBooking.Data;
using HairSalonBooking.Models;
using HairSalonBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Controllers;

[Authorize(Roles = "Admin")]
public class AdminSlotsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminSlotsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var slots = await _context.AvailableSlots
            .Include(slot => slot.Service)
            .OrderBy(slot => slot.StartTime)
            .Select(slot => new AdminSlotsIndexRowViewModel
            {
                Id = slot.Id,
                ServiceId = slot.ServiceId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsBooked = slot.IsBooked,
                ServiceName = slot.Service.Name
            })
            .ToListAsync();

        var model = new AdminSlotsIndexViewModel
        {
            Slots = slots,
            ServiceOptions = await BuildServiceOptionsAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var availableSlot = await _context.AvailableSlots
            .Include(slot => slot.Service)
            .FirstOrDefaultAsync(slot => slot.Id == id);

        if (availableSlot is null)
        {
            return NotFound();
        }

        await PopulateServiceSelectListAsync(availableSlot.ServiceId);
        return View(availableSlot);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateServiceSelectListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,StartTime,EndTime,IsBooked,ServiceId")] AvailableSlot availableSlot)
    {
        ValidateSlotTimes(availableSlot.StartTime, availableSlot.EndTime, nameof(availableSlot.EndTime));

        if (ModelState.IsValid)
        {
            _context.Add(availableSlot);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Termin został dodany.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateServiceSelectListAsync(availableSlot.ServiceId);
        return View(availableSlot);
    }

    public async Task<IActionResult> BulkCreate()
    {
        var model = new AdminSlotsBulkCreateViewModel
        {
            DateFrom = DateTime.Today,
            DateTo = DateTime.Today.AddDays(6),
            DayStartTime = new TimeSpan(9, 0, 0),
            DayEndTime = new TimeSpan(21, 0, 0),
            GapMinutes = 0,
            SkipDuplicates = true
        };

        model.ServiceOptions = await BuildServiceOptionsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkCreate(AdminSlotsBulkCreateViewModel model)
    {
        ValidateBulkCreateModel(model);

        if (!ModelState.IsValid)
        {
            model.ServiceOptions = await BuildServiceOptionsAsync(model.ServiceId);
            return View(model);
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(item => item.Id == model.ServiceId);

        if (service is null)
        {
            ModelState.AddModelError(nameof(model.ServiceId), "Wybrana usługa nie istnieje.");
            model.ServiceOptions = await BuildServiceOptionsAsync(model.ServiceId);
            return View(model);
        }

        var startDate = model.DateFrom!.Value.Date;
        var endDate = model.DateTo!.Value.Date;
        var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var gapDuration = TimeSpan.FromMinutes(model.GapMinutes);

        var existingSlots = await _context.AvailableSlots
            .Where(slot =>
                slot.ServiceId == service.Id &&
                slot.StartTime >= startDate &&
                slot.StartTime < endDate.AddDays(1))
            .Select(slot => new { slot.StartTime, slot.EndTime })
            .ToListAsync();

        var existingKeys = existingSlots
            .Select(slot => BuildSlotKey(service.Id, slot.StartTime, slot.EndTime))
            .ToHashSet(StringComparer.Ordinal);

        var slotsToAdd = new List<AvailableSlot>();
        var skippedDuplicates = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!IsSelectedDay(date.DayOfWeek, model))
            {
                continue;
            }

            var currentStart = date + model.DayStartTime!.Value;
            var dayEnd = date + model.DayEndTime!.Value;

            while (currentStart + slotDuration <= dayEnd)
            {
                var currentEnd = currentStart + slotDuration;
                var slotKey = BuildSlotKey(service.Id, currentStart, currentEnd);

                if (model.SkipDuplicates && !existingKeys.Add(slotKey))
                {
                    skippedDuplicates++;
                    currentStart = currentEnd + gapDuration;
                    continue;
                }

                slotsToAdd.Add(new AvailableSlot
                {
                    ServiceId = service.Id,
                    StartTime = currentStart,
                    EndTime = currentEnd,
                    IsBooked = model.IsBooked
                });

                currentStart = currentEnd + gapDuration;
            }
        }

        if (slotsToAdd.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Nie wygenerowano żadnych nowych terminów dla wybranego zakresu.");
            model.ServiceOptions = await BuildServiceOptionsAsync(model.ServiceId);
            return View(model);
        }

        _context.AvailableSlots.AddRange(slotsToAdd);
        await _context.SaveChangesAsync();

        TempData["Success"] = skippedDuplicates > 0
            ? $"Dodano {slotsToAdd.Count} terminów. Pominięto {skippedDuplicates} duplikatów."
            : $"Dodano {slotsToAdd.Count} terminów.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkEditSelection(int[] selectedIds)
    {
        return await BuildBulkEditViewAsync(selectedIds);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteSelected(int[] selectedIds)
    {
        var ids = selectedIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            TempData["Error"] = "Zaznacz co najmniej jeden termin do usunięcia.";
            return RedirectToAction(nameof(Index));
        }

        var slots = await _context.AvailableSlots
            .Include(slot => slot.Reservations)
            .Where(slot => ids.Contains(slot.Id))
            .ToListAsync();

        if (slots.Count == 0)
        {
            TempData["Error"] = "Nie znaleziono zaznaczonych terminów.";
            return RedirectToAction(nameof(Index));
        }

        var blockedSlots = slots
            .Where(slot => slot.Reservations.Any())
            .ToList();

        var removableSlots = slots
            .Where(slot => slot.Reservations.Count == 0)
            .ToList();

        if (removableSlots.Count > 0)
        {
            _context.AvailableSlots.RemoveRange(removableSlots);
            await _context.SaveChangesAsync();
        }

        if (blockedSlots.Count > 0 && removableSlots.Count > 0)
        {
            TempData["Success"] = $"Usunięto {removableSlots.Count} terminów. Pominięto {blockedSlots.Count} terminów z istniejącymi rezerwacjami.";
            return RedirectToAction(nameof(Index));
        }

        if (blockedSlots.Count > 0)
        {
            TempData["Error"] = "Nie można usunąć zaznaczonych terminów, ponieważ mają powiązane rezerwacje.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Usunięto {removableSlots.Count} terminów.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var availableSlot = await _context.AvailableSlots.FindAsync(id);
        if (availableSlot is null)
        {
            return NotFound();
        }

        await PopulateServiceSelectListAsync(availableSlot.ServiceId);
        return View(availableSlot);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime,IsBooked,ServiceId")] AvailableSlot availableSlot)
    {
        if (id != availableSlot.Id)
        {
            return NotFound();
        }

        ValidateSlotTimes(availableSlot.StartTime, availableSlot.EndTime, nameof(availableSlot.EndTime));

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(availableSlot);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Termin został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AvailableSlotExists(availableSlot.Id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        await PopulateServiceSelectListAsync(availableSlot.ServiceId);
        return View(availableSlot);
    }

    public async Task<IActionResult> BulkEdit(int[] selectedIds)
    {
        return await BuildBulkEditViewAsync(selectedIds);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkEdit(AdminSlotsBulkEditViewModel model)
    {
        if (model.Slots.Count == 0)
        {
            TempData["Error"] = "Brak zaznaczonych terminów do zapisania.";
            return RedirectToAction(nameof(Index));
        }

        var validServiceIds = await _context.Services
            .Select(service => service.Id)
            .ToHashSetAsync();

        ValidateBulkEditModel(model, validServiceIds);

        var slotIds = model.Slots
            .Select(slot => slot.Id)
            .Distinct()
            .ToList();

        var slots = await _context.AvailableSlots
            .Where(slot => slotIds.Contains(slot.Id))
            .ToDictionaryAsync(slot => slot.Id);

        if (slots.Count != slotIds.Count)
        {
            ModelState.AddModelError(string.Empty, "Część terminów została usunięta lub zmieniona w międzyczasie.");
        }

        if (!ModelState.IsValid)
        {
            model.ServiceOptions = await BuildServiceOptionsAsync();
            return View(model);
        }

        foreach (var row in model.Slots)
        {
            if (!slots.TryGetValue(row.Id, out var slot))
            {
                continue;
            }

            slot.StartTime = row.StartTime;
            slot.EndTime = row.EndTime;
            slot.ServiceId = row.ServiceId;
            slot.IsBooked = row.IsBooked;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Zaktualizowano {model.Slots.Count} terminów.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var availableSlot = await _context.AvailableSlots
            .Include(slot => slot.Service)
            .FirstOrDefaultAsync(slot => slot.Id == id);

        if (availableSlot is null)
        {
            return NotFound();
        }

        return View(availableSlot);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var availableSlot = await _context.AvailableSlots.FindAsync(id);
        if (availableSlot is not null)
        {
            _context.AvailableSlots.Remove(availableSlot);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Termin został usunięty.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> BuildBulkEditViewAsync(IEnumerable<int> selectedIds)
    {
        var ids = selectedIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            TempData["Error"] = "Zaznacz co najmniej jeden termin do masowej edycji.";
            return RedirectToAction(nameof(Index));
        }

        var slots = await _context.AvailableSlots
            .Where(slot => ids.Contains(slot.Id))
            .OrderBy(slot => slot.StartTime)
            .ToListAsync();

        if (slots.Count == 0)
        {
            TempData["Error"] = "Nie znaleziono zaznaczonych terminów.";
            return RedirectToAction(nameof(Index));
        }

        var model = new AdminSlotsBulkEditViewModel
        {
            Slots = slots
                .Select(slot => new AdminSlotsBulkEditRowViewModel
                {
                    Id = slot.Id,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    IsBooked = slot.IsBooked,
                    ServiceId = slot.ServiceId
                })
                .ToList(),
            ServiceOptions = await BuildServiceOptionsAsync()
        };

        return View(model);
    }

    private void ValidateBulkCreateModel(AdminSlotsBulkCreateViewModel model)
    {
        if (model.DateFrom.HasValue && model.DateTo.HasValue && model.DateTo.Value.Date < model.DateFrom.Value.Date)
        {
            ModelState.AddModelError(nameof(model.DateTo), "Data końcowa nie może być wcześniejsza niż początkowa.");
        }

        if (model.DayStartTime.HasValue && model.DayEndTime.HasValue && model.DayEndTime <= model.DayStartTime)
        {
            ModelState.AddModelError(nameof(model.DayEndTime), "Godzina końcowa musi być późniejsza niż początkowa.");
        }

        if (!model.HasAnySelectedDay())
        {
            ModelState.AddModelError(string.Empty, "Wybierz przynajmniej jeden dzień tygodnia.");
        }
    }

    private void ValidateBulkEditModel(AdminSlotsBulkEditViewModel model, ISet<int> validServiceIds)
    {
        for (var index = 0; index < model.Slots.Count; index++)
        {
            var row = model.Slots[index];
            var prefix = $"Slots[{index}]";

            if (row.EndTime <= row.StartTime)
            {
                ModelState.AddModelError($"{prefix}.{nameof(row.EndTime)}", "Koniec musi być późniejszy niż początek.");
            }

            if (!validServiceIds.Contains(row.ServiceId))
            {
                ModelState.AddModelError($"{prefix}.{nameof(row.ServiceId)}", "Wybrana usługa nie istnieje.");
            }
        }
    }

    private void ValidateSlotTimes(DateTime startTime, DateTime endTime, string modelKey)
    {
        if (endTime <= startTime)
        {
            ModelState.AddModelError(modelKey, "Koniec musi być późniejszy niż początek.");
        }
    }

    private bool IsSelectedDay(DayOfWeek dayOfWeek, AdminSlotsBulkCreateViewModel model) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => model.Monday,
            DayOfWeek.Tuesday => model.Tuesday,
            DayOfWeek.Wednesday => model.Wednesday,
            DayOfWeek.Thursday => model.Thursday,
            DayOfWeek.Friday => model.Friday,
            DayOfWeek.Saturday => model.Saturday,
            DayOfWeek.Sunday => model.Sunday,
            _ => false
        };

    private static string BuildSlotKey(int serviceId, DateTime startTime, DateTime endTime) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{serviceId}|{startTime:O}|{endTime:O}");

    private async Task PopulateServiceSelectListAsync(int? selectedServiceId = null)
    {
        ViewData["ServiceId"] = await BuildServiceOptionsAsync(selectedServiceId);
    }

    private async Task<List<SelectListItem>> BuildServiceOptionsAsync(int? selectedServiceId = null)
    {
        return await _context.Services
            .OrderBy(service => service.Name)
            .ThenBy(service => service.DurationMinutes)
            .Select(service => new SelectListItem
            {
                Value = service.Id.ToString(CultureInfo.InvariantCulture),
                Text = service.IsActive
                    ? $"{service.Name} ({service.DurationMinutes} min)"
                    : $"{service.Name} ({service.DurationMinutes} min, nieaktywna)",
                Selected = selectedServiceId.HasValue && selectedServiceId.Value == service.Id
            })
            .ToListAsync();
    }

    private bool AvailableSlotExists(int id)
    {
        return _context.AvailableSlots.Any(slot => slot.Id == id);
    }
}
