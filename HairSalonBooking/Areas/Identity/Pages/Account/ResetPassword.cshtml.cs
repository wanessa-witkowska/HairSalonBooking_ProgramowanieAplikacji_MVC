using System.ComponentModel.DataAnnotations;
using System.Text;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HairSalonBooking.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public ResetPasswordModel(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Pole adres e-mail jest wymagane.")]
        [EmailAddress(ErrorMessage = "Wpisz poprawny adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole hasło jest wymagane.")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} i maksymalnie {1} znaków.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pole potwierdzenie hasła jest wymagane.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasła nie są takie same.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? code = null)
    {
        if (code is null)
        {
            return BadRequest("Brak kodu resetowania hasła.");
        }

        Input = new InputModel
        {
            Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
