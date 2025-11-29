using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Admin
{
    public class KYCReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KYCReviewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<KYCDocument> Documents { get; set; } = new();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string Filter { get; set; } = "Pending";
        public int PendingCount { get; set; }
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

            // Get counts
            PendingCount = await _context.KYCDocuments
                .Where(k => k.Status == "Pending")
                .CountAsync();

            TotalCount = await _context.KYCDocuments.CountAsync();

            // Get documents based on filter
            var query = _context.KYCDocuments.Include(k => k.User).AsQueryable();

            if (Filter == "Pending")
            {
                query = query.Where(k => k.Status == "Pending");
            }

            Documents = await query
                .OrderByDescending(k => k.DocId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid docId, string action)
        {
            // Check if user is Admin
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            var adminId = Guid.Parse(userIdString);

            // Get the document
            var document = await _context.KYCDocuments
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.DocId == docId);

            if (document == null)
            {
                ErrorMessage = "Document not found.";
                return RedirectToPage();
            }

            // Update status
            if (action == "approve")
            {
                document.Status = "Approved";
                SuccessMessage = $"KYC document for {document.User.Email} has been approved.";
            }
            else if (action == "reject")
            {
                document.Status = "Rejected";
                SuccessMessage = $"KYC document for {document.User.Email} has been rejected.";
            }

            document.ReviewerId = adminId;
            document.ReviewedAt = DateTime.UtcNow;

            // Log the action
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = adminId,
                Action = $"KYC Document {action}d",
                Details = $"{action}d {document.DocType} for user {document.User.Email}",
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