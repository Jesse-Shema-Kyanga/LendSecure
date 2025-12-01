using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LendSecure.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var role = HttpContext.Session.GetString("UserRole");

                if (role == "Admin")
                {
                    return RedirectToPage("/Admin/Dashboard");
                }
                else if (role == "Borrower")
                {
                    return RedirectToPage("/Borrower/Dashboard");
                }
                else if (role == "Lender")
                {
                    return RedirectToPage("/Lender/Dashboard");
                }
            }

            return Page();
        }
    }
}