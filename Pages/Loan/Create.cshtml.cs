using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LendSecure.Pages.Loan
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }
        public string KYCStatus { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Loan amount is required")]
            [Range(1000, 10000000, ErrorMessage = "Amount must be between 1,000 and 10,000,000 RWF")]
            [Display(Name = "Amount Requested (RWF)")]
            public decimal AmountRequested { get; set; }

            [Required(ErrorMessage = "Please describe the purpose of this loan")]
            [StringLength(500, MinimumLength = 10, ErrorMessage = "Purpose must be between 10 and 500 characters")]
            [Display(Name = "Loan Purpose")]
            public string Purpose { get; set; }

            [Required(ErrorMessage = "Loan term is required")]
            [Range(4, 52, ErrorMessage = "Loan term must be between 4 and 52 weeks")]
            [Display(Name = "Loan Term (Weeks)")]
            public int TermWeeks { get; set; }

            [Required(ErrorMessage = "Interest rate is required")]
            [Range(1, 20, ErrorMessage = "Interest rate must be between 1% and 20%")]
            [Display(Name = "Interest Rate (%)")]
            public decimal InterestRate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            var user = await _context.Users
                .Include(u => u.KYCDocuments)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user != null)
            {
                var kycDoc = user.KYCDocuments
                    .OrderByDescending(k => k.ReviewedAt ?? DateTime.MinValue)
                    .ThenByDescending(k => k.DocId)
                    .FirstOrDefault();

                KYCStatus = kycDoc?.Status ?? "Not Submitted";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                await LoadKYCStatusAsync(userIdString);
                return Page();
            }

            var userId = Guid.Parse(userIdString);

            try
            {
                short termMonths = (short)Math.Ceiling(Input.TermWeeks / 4.0m);

                var loanRequest = new LoanRequest
                {
                    LoanId = Guid.NewGuid(),
                    BorrowerId = userId,
                    AmountRequested = Input.AmountRequested,
                    Currency = "RWF",
                    Purpose = Input.Purpose,
                    TermMonths = termMonths,
                    InterestRate = Input.InterestRate,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.LoanRequests.Add(loanRequest);

                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    UserId = userId,
                    Action = "Loan Request Created",
                    Details = $"Requested loan of {Input.AmountRequested:N0} RWF for {Input.Purpose}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Loan request submitted successfully! It will be reviewed by an admin.";
                return RedirectToPage("/Loan/MyLoans");
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while creating your loan request. Please try again.";
                await LoadKYCStatusAsync(userIdString);
                return Page();
            }
        }

        private async Task LoadKYCStatusAsync(string userIdString)
        {
            var userId = Guid.Parse(userIdString);
            var user = await _context.Users
                .Include(u => u.KYCDocuments)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user != null)
            {
                var kycDoc = user.KYCDocuments
                    .OrderByDescending(k => k.ReviewedAt ?? DateTime.MinValue)
                    .ThenByDescending(k => k.DocId)
                    .FirstOrDefault();

                KYCStatus = kycDoc?.Status ?? "Not Submitted";
            }
        }
    }
}

