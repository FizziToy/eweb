using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eweb.Web.Areas.Identity.Pages.Account;

public class LoginWith2faModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }
}
