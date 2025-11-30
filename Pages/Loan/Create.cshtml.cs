using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

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

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public bool CanRequestLoan { get; set; }
        public decimal WalletBalance { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Loan amount is required")]
            [Range(1000, 1000000, ErrorMessage = "Amount must be between 1,000 and 1,000,000 RWF")]
            public decimal AmountRequested { get; set; }

            [Required(ErrorMessage = "Please select a loan term")]
            [Range(1, 12, ErrorMessage = "Term must be between 1 and 12 months")]
            public short TermMonths { get; set; }

            [Required(ErrorMessage = "Interest rate is required")]
            [Range(0.1, 50, ErrorMessage = "Interest rate must be between 0.1% and 50%")]
            public decimal InterestRate { get; set; }

            [Required(ErrorMessage = "Please explain the purpose of this loan")]
            [StringLength(1000, MinimumLength = 20, ErrorMessage = "Purpose must be between 20 and 1000 characters")]
            public string Purpose { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in as Borrower
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            // Check KYC status - MUST BE APPROVED
            var hasApprovedKYC = await _context.KYCDocuments
                .AnyAsync(k => k.UserId == userId && k.Status == "Approved");

            CanRequestLoan = hasApprovedKYC;

            // Get wallet balance
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            WalletBalance = wallet?.Balance ?? 0;

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

            var userId = Guid.Parse(userIdString);

            // Check KYC status again - STRICT REQUIREMENT
            var hasApprovedKYC = await _context.KYCDocuments
                .AnyAsync(k => k.UserId == userId && k.Status == "Approved");

            if (!hasApprovedKYC)
            {
                ErrorMessage = "You must have an approved KYC document before requesting a loan.";
                CanRequestLoan = false;

                // Get wallet balance for display
                var walletCheck = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                WalletBalance = walletCheck?.Balance ?? 0;

                return Page();
            }

            CanRequestLoan = true;

            if (!ModelState.IsValid)
            {
                // Get wallet balance for display
                var walletCheck = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                WalletBalance = walletCheck?.Balance ?? 0;
                return Page();
            }

            try
            {
                // Create loan request
                var loanRequest = new LoanRequest
                {
                    LoanId = Guid.NewGuid(),
                    BorrowerId = userId,
                    AmountRequested = Input.AmountRequested,
                    Currency = "RWF",
                    Purpose = Input.Purpose,
                    TermMonths = Input.TermMonths,
                    InterestRate = Input.InterestRate,
                    Status = "Pending", // Needs admin approval
                    CreatedAt = DateTime.UtcNow,
                    ApprovedAt = null,
                    ApproverId = null
                };

                _context.LoanRequests.Add(loanRequest);

                // Log the action
                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    UserId = userId,
                    Action = "Loan Request Created",
                    Details = $"Requested {Input.AmountRequested:N0} RWF for {Input.TermMonths} months at {Input.InterestRate}% interest",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                // Set success message and redirect to MyLoans
                TempData["SuccessMessage"] = "Loan request submitted successfully! Admin will review it soon.";
                return RedirectToPage("/Loan/MyLoans");
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while submitting your loan request. Please try again.";

                // Get wallet balance for display
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                WalletBalance = wallet?.Balance ?? 0;

                return Page();
            }
        }
    }
}