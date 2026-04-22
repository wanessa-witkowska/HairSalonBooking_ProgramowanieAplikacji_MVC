using System.ComponentModel.DataAnnotations;
using HairSalonBooking.Data;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class DeletePersonalDataModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<DeletePersonalDataModel> _logger;

    public DeletePersonalDataModel(
        ApplicationDbContext dbContext,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<DeletePersonalDataModel> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RequirePassword { get; set; }

    public class InputModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Haslo")]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound("Nie udalo sie wczytac uzytkownika.");
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound("Nie udalo sie wczytac uzytkownika.");
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        if (RequirePassword && !await _userManager.CheckPasswordAsync(user, Input.Password))
        {
            ModelState.AddModelError(string.Empty, "Nieprawidlowe haslo.");
            return Page();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var reservations = await _dbContext.Reservations
            .Where(r => r.UserId == user.Id)
            .ToListAsync();

        if (reservations.Count > 0)
        {
            _dbContext.Reservations.RemoveRange(reservations);
            await _dbContext.SaveChangesAsync();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await transaction.CommitAsync();
        await _signInManager.SignOutAsync();

        _logger.LogInformation("User deleted their account together with related reservations.");

        return Redirect("~/");
    }
}
