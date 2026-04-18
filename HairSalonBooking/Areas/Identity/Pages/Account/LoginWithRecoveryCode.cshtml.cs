using System.ComponentModel.DataAnnotations;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HairSalonBooking.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(
        SignInManager<AppUser> signInManager,
        ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required(ErrorMessage = "Pole kod odzyskiwania jest wymagane.")]
        [DataType(DataType.Text)]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Nie można załadować użytkownika oczekującego na 2FA.");
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Nie można załadować użytkownika oczekującego na 2FA.");
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with a recovery code.");
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Nieprawidłowy kod odzyskiwania.");
        ReturnUrl = returnUrl;
        return Page();
    }
}
