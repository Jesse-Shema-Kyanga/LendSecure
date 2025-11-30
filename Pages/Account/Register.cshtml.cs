using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
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
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required(ErrorMessage = "Please confirm your password")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Please select a role")]
            public string Role { get; set; }
        }

        public void OnGet()
        {
            // Just display the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (existingUser != null)
            {
                ErrorMessage = "An account with this email already exists.";
                return Page();
            }

            // Hash the password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password);

            // Create new user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = Input.Email,
                PasswordHash = passwordHash,
                Role = Input.Role,
                MfaEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create default wallet for user with demo balance
            var wallet = new LendSecure.Models.Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = user.UserId,
                Balance = 10000.00m, // Demo money - 10,000 RWF
                Currency = "RWF",
                UpdatedAt = DateTime.UtcNow
            };

            // Create empty user profile
            var profile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = user.UserId
            };

            // Save to database
            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.UserProfiles.Add(profile);

            try
            {
                await _context.SaveChangesAsync();

                // Log the registration action
                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Action = "User Registration",
                    Details = $"New {Input.Role} account created",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Redirect to login page
                TempData["SuccessMessage"] = "Account created successfully! Please login.";
                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while creating your account. Please try again.";
                return Page();
            }
        }
    }
}