using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eweb.Web.Areas.Identity.Pages.Account.Manage;

public class EnableAuthenticatorModel : PageModel
{
    public IActionResult OnGet()
    {
        TempData["StatusMessage"] = "Двофакторну авторизацію вимкнено для цього сайту.";
        return RedirectToPage("./Index");
    }
}
