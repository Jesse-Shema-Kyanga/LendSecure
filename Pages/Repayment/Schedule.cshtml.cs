using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LendSecure.Data;
using LendSecure.Models;

namespace LendSecure.Pages.Repayment
{
    public class ScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ScheduleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LoanRequest Loan { get; set; }
        public List<Models.Repayment> Repayments { get; set; }
        public decimal WalletBalance { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal WeeklyPayment { get; set; }
        public int PaidCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid loanId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);

            // Get loan with repayments
            Loan = await _context.LoanRequests
                .Include(l => l.Repayments)
                .FirstOrDefaultAsync(l => l.LoanId == loanId && l.BorrowerId == userId);

            if (Loan == null)
            {
                return Page();
            }

            Repayments = Loan.Repayments.OrderBy(r => r.ScheduledDate).ToList();

            // Calculate totals
            TotalInterest = Loan.AmountRequested * (Loan.InterestRate / 100m);
            TotalRepayment = Loan.AmountRequested + TotalInterest;
            WeeklyPayment = Repayments.FirstOrDefault()?.PrincipalAmount + Repayments.FirstOrDefault()?.InterestAmount ?? 0;

            PaidCount = Repayments.Count(r => r.Status == "Paid");
            TotalPaid = Repayments.Where(r => r.Status == "Paid").Sum(r => r.PrincipalAmount + r.InterestAmount);
            TotalRemaining = TotalRepayment - TotalPaid;

            // Get wallet balance
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            WalletBalance = wallet?.Balance ?? 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid repaymentId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);

            // Get repayment with loan and fundings
            var repayment = await _context.Repayments
                .Include(r => r.Loan)
                .ThenInclude(l => l.Fundings)
                .FirstOrDefaultAsync(r => r.RepaymentId == repaymentId);

            if (repayment == null || repayment.Loan.BorrowerId != userId)
            {
                ErrorMessage = "Repayment not found or access denied.";
                return RedirectToPage(new { loanId = repayment?.LoanId });
            }

            if (repayment.Status == "Paid")
            {
                ErrorMessage = "This repayment has already been paid.";
                return RedirectToPage(new { loanId = repayment.LoanId });
            }

            // Get borrower wallet
            var borrowerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (borrowerWallet == null)
            {
                ErrorMessage = "Wallet not found.";
                return RedirectToPage(new { loanId = repayment.LoanId });
            }

            decimal totalPayment = repayment.PrincipalAmount + repayment.InterestAmount;

            // Check sufficient balance
            if (borrowerWallet.Balance < totalPayment)
            {
                ErrorMessage = $"Insufficient balance. You need {totalPayment:N0} RWF but only have {borrowerWallet.Balance:N0} RWF.";
                return RedirectToPage(new { loanId = repayment.LoanId });
            }

            // 1. Deduct from borrower wallet
            borrowerWallet.Balance -= totalPayment;
            borrowerWallet.UpdatedAt = DateTime.UtcNow;

            // 2. Create borrower transaction
            var borrowerTxn = new WalletTransaction
            {
                TxnId = Guid.NewGuid(),
                WalletId = borrowerWallet.WalletId,
                TxnType = "LoanRepayment",
                Amount = totalPayment,
                Currency = "RWF",
                RelatedLoanId = repayment.LoanId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(borrowerTxn);

            // 3. Distribute to lenders proportionally
            var totalFunded = repayment.Loan.Fundings.Sum(f => f.Amount);

            foreach (var funding in repayment.Loan.Fundings)
            {
                // Calculate lender's share based on proportion funded
                decimal proportion = funding.Amount / totalFunded;
                decimal lenderShare = totalPayment * proportion;

                // Add to lender wallet
                var lenderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == funding.LenderId);
                if (lenderWallet != null)
                {
                    lenderWallet.Balance += lenderShare;
                    lenderWallet.UpdatedAt = DateTime.UtcNow;

                    // Create lender transaction
                    var lenderTxn = new WalletTransaction
                    {
                        TxnId = Guid.NewGuid(),
                        WalletId = lenderWallet.WalletId,
                        TxnType = "LoanRepayment",
                        Amount = lenderShare,
                        Currency = "RWF",
                        RelatedLoanId = repayment.LoanId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.WalletTransactions.Add(lenderTxn);
                }
            }

            // 4. Mark repayment as paid
            repayment.Status = "Paid";
            repayment.PaidAt = DateTime.UtcNow;

            // 5. Check if all repayments are paid
            var allRepayments = await _context.Repayments
                .Where(r => r.LoanId == repayment.LoanId)
                .ToListAsync();

            if (allRepayments.All(r => r.Status == "Paid"))
            {
                repayment.Loan.Status = "Completed";
            }
            else
            {
                repayment.Loan.Status = "Repaying";
            }

            // 6. Audit log
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = userId,
                Action = "Loan Repayment",
                Details = $"Paid {totalPayment:N0} RWF for loan {repayment.LoanId}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Payment of {totalPayment:N0} RWF successful! Distributed to lenders.";
            return RedirectToPage(new { loanId = repayment.LoanId });
        }
    }
}