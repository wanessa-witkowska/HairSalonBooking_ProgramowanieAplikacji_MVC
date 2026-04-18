using System.ComponentModel.DataAnnotations;
using HairSalonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HairSalonBooking.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(SignInManager<AppUser> signInManager, ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required(ErrorMessage = "Pole kod uwierzytelniający jest wymagane.")]
        [StringLength(7, ErrorMessage = "Kod powinien mieć od {2} do {1} znaków.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Zapamiętaj to urządzenie")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Nie można załadować użytkownika oczekującego na 2FA.");
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
        RememberMe = rememberMe;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe;
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Nie można załadować użytkownika oczekującego na 2FA.");
        }

        var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode,
            rememberMe,
            Input.RememberMachine);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with 2fa.");
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Nieprawidłowy kod uwierzytelniający.");
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }
}
