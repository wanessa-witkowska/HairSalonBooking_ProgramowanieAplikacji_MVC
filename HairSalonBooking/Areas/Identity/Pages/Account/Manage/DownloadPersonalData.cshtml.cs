using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HairSalonBooking.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class DownloadPersonalDataModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("./PersonalData");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("./PersonalData");
    }
}
