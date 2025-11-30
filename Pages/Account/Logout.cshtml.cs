using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LendSecure.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Redirect to login
            return RedirectToPage("/Account/Login");
        }
    }
}