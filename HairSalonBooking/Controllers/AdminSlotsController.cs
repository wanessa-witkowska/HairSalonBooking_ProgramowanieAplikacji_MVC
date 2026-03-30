using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HairSalonBooking.Data;
using HairSalonBooking.Models;

namespace HairSalonBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminSlotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminSlots
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.AvailableSlots
                .Include(a => a.Service)
                .OrderBy(a => a.StartTime);

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AdminSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (availableSlot == null)
            {
                return NotFound();
            }

            return View(availableSlot);
        }

        // GET: AdminSlots/Create
        public IActionResult Create()
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Category");
            return View();
        }

        // POST: AdminSlots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartTime,EndTime,IsBooked,ServiceId")] AvailableSlot availableSlot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(availableSlot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Category", availableSlot.ServiceId);
            return View(availableSlot);
        }

        // GET: AdminSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots.FindAsync(id);
            if (availableSlot == null)
            {
                return NotFound();
            }
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Category", availableSlot.ServiceId);
            return View(availableSlot);
        }

        // POST: AdminSlots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime,IsBooked,ServiceId")] AvailableSlot availableSlot)
        {
            if (id != availableSlot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(availableSlot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AvailableSlotExists(availableSlot.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Category", availableSlot.ServiceId);
            return View(availableSlot);
        }

        // GET: AdminSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (availableSlot == null)
            {
                return NotFound();
            }

            return View(availableSlot);
        }

        // POST: AdminSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var availableSlot = await _context.AvailableSlots.FindAsync(id);
            if (availableSlot != null)
            {
                _context.AvailableSlots.Remove(availableSlot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AvailableSlotExists(int id)
        {
            return _context.AvailableSlots.Any(e => e.Id == id);
        }
    }
}
