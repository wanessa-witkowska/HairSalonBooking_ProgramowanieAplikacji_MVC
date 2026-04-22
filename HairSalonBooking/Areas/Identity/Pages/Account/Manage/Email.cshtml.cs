using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HairSalonBooking.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class EmailModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;

    public EmailModel(UserManager<AppUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [Display(Name = "Obecny adres e-mail")]
    public string Email { get; set; } = string.Empty;

    public bool IsEmailConfirmed { get; set; }

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public class InputModel
    {
        [Required(ErrorMessage = "Podaj nowy adres e-mail.")]
        [EmailAddress(ErrorMessage = "Wpisz poprawny adres e-mail.")]
        [Display(Name = "Nowy adres e-mail")]
        public string NewEmail { get; set; } = string.Empty;
    }

    private async Task LoadAsync(AppUser user)
    {
        Email = await _userManager.GetEmailAsync(user) ?? string.Empty;
        Input = new InputModel();
        IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
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

    public async Task<IActionResult> OnPostChangeEmailAsync()
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

        var email = await _userManager.GetEmailAsync(user);
        if (Input.NewEmail == email)
        {
            StatusMessage = "Nowy adres e-mail jest taki sam jak obecny.";
            return RedirectToPage();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmailChange",
            pageHandler: null,
            values: new { area = "Identity", userId, email = Input.NewEmail, code },
            protocol: Request.Scheme);

        await _emailSender.SendEmailAsync(
            Input.NewEmail,
            "Potwierdź zmianę adresu e-mail",
            $"Potwierdź zmianę adresu e-mail, klikając <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>ten link</a>.");

        StatusMessage = "Link potwierdzający zmianę adresu e-mail został wysłany.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
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

        var userId = await _userManager.GetUserIdAsync(user);
        var email = await _userManager.GetEmailAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId, code },
            protocol: Request.Scheme);

        await _emailSender.SendEmailAsync(
            email!,
            "Potwierdź swój adres e-mail",
            $"Potwierdź konto, klikając <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>ten link</a>.");

        StatusMessage = "Wiadomość z linkiem potwierdzającym została wysłana.";
        return RedirectToPage();
    }
}
