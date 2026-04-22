using System.ComponentModel.DataAnnotations;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HairSalonBooking.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public IndexModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [Display(Name = "Nazwa użytkownika")]
    public string Username { get; set; } = string.Empty;

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Phone(ErrorMessage = "Wpisz poprawny numer telefonu.")]
        [Display(Name = "Numer telefonu")]
        public string? PhoneNumber { get; set; }
    }

    private async Task LoadAsync(AppUser user)
    {
        Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
        Input = new InputModel
        {
            PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound("Nie udało się wczytać użytkownika.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound("Nie udało się wczytać użytkownika.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (Input.PhoneNumber != phoneNumber)
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = "Błąd: nie udało się zaktualizować numeru telefonu.";
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Profil został zaktualizowany.";
        return RedirectToPage();
    }
}
