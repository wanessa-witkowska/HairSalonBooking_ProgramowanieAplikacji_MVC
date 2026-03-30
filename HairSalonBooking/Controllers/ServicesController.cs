using HairSalonBooking.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Controllers;

public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? category)
    {
        var query = _context.Services
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s =>
                s.Name.Contains(searchTerm) ||
                s.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Category = category;
        ViewBag.Categories = await _context.Services
            .Where(s => s.IsActive)
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var services = await query.OrderBy(s => s.Name).ToListAsync();
        return View(services);
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (service is null)
        {
            return NotFound();
        }

        return View(service);
    }
}