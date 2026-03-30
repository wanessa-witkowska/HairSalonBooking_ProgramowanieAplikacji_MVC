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

    public async Task<IActionResult> Index()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

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