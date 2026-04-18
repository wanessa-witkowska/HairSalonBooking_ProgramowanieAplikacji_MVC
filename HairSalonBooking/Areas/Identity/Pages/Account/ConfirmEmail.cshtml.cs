using System.Text;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HairSalonBooking.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public ConfirmEmailModel(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public bool IsSuccess { get; private set; }
    public string StatusMessage { get; private set; } = "Przetwarzamy potwierdzenie adresu e-mail.";

    public async Task OnGetAsync(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            StatusMessage = "Link potwierdzający jest nieprawidłowy lub niekompletny.";
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            StatusMessage = "Nie znaleziono użytkownika powiązanego z tym linkiem.";
            return;
        }

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

        IsSuccess = result.Succeeded;
        StatusMessage = result.Succeeded
            ? "Adres e-mail został poprawnie potwierdzony."
            : "Nie udało się potwierdzić adresu e-mail.";
    }
}
