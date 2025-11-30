using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Admin
{
    public class LoanApprovalsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoanApprovalsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LoanRequest> LoanRequests { get; set; } = new();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string Filter { get; set; } = "Pending";
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int TotalCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string filter = "Pending")
        {
            // Check if user is Admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            Filter = filter;

            // Get counts for tabs
            PendingCount = await _context.LoanRequests
                .Where(l => l.Status == "Pending")
                .CountAsync();

            ApprovedCount = await _context.LoanRequests
                .Where(l => l.Status == "Approved")
                .CountAsync();

            RejectedCount = await _context.LoanRequests
                .Where(l => l.Status == "Rejected")
                .CountAsync();

            TotalCount = await _context.LoanRequests.CountAsync();

            // Get loan requests based on filter
            var query = _context.LoanRequests
                .Include(l => l.Borrower)
                .AsQueryable();

            if (Filter == "Pending")
            {
                query = query.Where(l => l.Status == "Pending");
            }
            else if (Filter == "Approved")
            {
                query = query.Where(l => l.Status == "Approved");
            }
            else if (Filter == "Rejected")
            {
                query = query.Where(l => l.Status == "Rejected");
            }
            // "All" shows everything (no filter)

            LoanRequests = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid loanId, string action)
        {
            // Check if user is Admin
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            var adminId = Guid.Parse(userIdString);

            // Get the loan request
            var loanRequest = await _context.LoanRequests
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loanRequest == null)
            {
                ErrorMessage = "Loan request not found.";
                return RedirectToPage();
            }

            // Check if already processed
            if (loanRequest.Status != "Pending")
            {
                ErrorMessage = "This loan request has already been processed.";
                return RedirectToPage();
            }

            // Update loan status
            if (action == "approve")
            {
                loanRequest.Status = "Approved";
                loanRequest.ApprovedAt = DateTime.UtcNow;
                loanRequest.ApproverId = adminId;
                SuccessMessage = $"Loan request for {loanRequest.Borrower.Email} ({loanRequest.AmountRequested:N0} RWF) has been approved!";
            }
            else if (action == "reject")
            {
                loanRequest.Status = "Rejected";
                loanRequest.ApprovedAt = DateTime.UtcNow;
                loanRequest.ApproverId = adminId;
                SuccessMessage = $"Loan request for {loanRequest.Borrower.Email} has been rejected.";
            }

            // Log the action
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = adminId,
                Action = $"Loan Request {action}d",
                Details = $"{action}d loan request of {loanRequest.AmountRequested:N0} RWF for {loanRequest.Borrower.Email} at {loanRequest.InterestRate}% interest",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}