using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
            // Just display the login form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find user by email
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Verify password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Store user info in session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            // Log the login action
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = user.UserId,
                Action = "User Login",
                Details = $"{user.Role} logged in successfully",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Redirect based on role
            if (user.Role == "Admin")
            {
                return RedirectToPage("/Admin/Dashboard");
            }
            else if (user.Role == "Borrower")
            {
                return RedirectToPage("/Borrower/Dashboard");
            }
            else if (user.Role == "Lender")
            {
                return RedirectToPage("/Lender/Dashboard");
            }
            else
            {
                // Invalid role - clear session and show error
                HttpContext.Session.Clear();
                ErrorMessage = "Invalid account role. Please contact administrator.";
                return Page();
            }
        }
    }
}