using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HairSalonBooking.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class PersonalDataModel : PageModel
{
    public void OnGet()
    {
    }
}
