using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.KYC
{
    public class UploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UploadModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string UserRole { get; set; }
        public List<KYCDocument> ExistingDocuments { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Please select a document type")]
            public string DocType { get; set; }

            [Required(ErrorMessage = "Please select a file to upload")]
            public IFormFile DocumentFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            UserRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            // Get existing KYC documents
            ExistingDocuments = await _context.KYCDocuments
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.DocId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            UserRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                // Reload existing documents
                var userId = Guid.Parse(userIdString);
                ExistingDocuments = await _context.KYCDocuments
                    .Where(k => k.UserId == userId)
                    .OrderByDescending(k => k.DocId)
                    .ToListAsync();
                return Page();
            }

            var currentUserId = Guid.Parse(userIdString);

            // Validate file
            if (Input.DocumentFile == null || Input.DocumentFile.Length == 0)
            {
                ErrorMessage = "Please select a file to upload.";
                return Page();
            }

            // Validate file size (max 5MB)
            if (Input.DocumentFile.Length > 5 * 1024 * 1024)
            {
                ErrorMessage = "File size must be less than 5MB.";
                return Page();
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(Input.DocumentFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ErrorMessage = "Only JPG, JPEG, and PNG files are allowed.";
                return Page();
            }

            try
            {
                // Create Uploads folder if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "kyc");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{currentUserId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.DocumentFile.CopyToAsync(fileStream);
                }

                // Save to database
                var kycDocument = new KYCDocument
                {
                    DocId = Guid.NewGuid(),
                    UserId = currentUserId,
                    DocType = Input.DocType,
                    FilePath = $"/uploads/kyc/{uniqueFileName}",
                    Status = "Pending",
                    ReviewerId = null,
                    ReviewedAt = null
                };

                _context.KYCDocuments.Add(kycDocument);

                // Log the action
                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    UserId = currentUserId,
                    Action = "KYC Document Upload",
                    Details = $"Uploaded {Input.DocType} document",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                SuccessMessage = "Document uploaded successfully! Admin will review it soon.";

                // Reload documents
                ExistingDocuments = await _context.KYCDocuments
                    .Where(k => k.UserId == currentUserId)
                    .OrderByDescending(k => k.DocId)
                    .ToListAsync();

                // Clear form
                ModelState.Clear();
                Input = new InputModel();

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while uploading the document. Please try again.";
                return Page();
            }
        }
    }
}