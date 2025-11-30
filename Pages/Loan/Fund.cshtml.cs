using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LendSecure.Models;
using LendSecure.Data;

namespace LendSecure.Pages.Loan
{
    public class FundModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FundModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public decimal AmountToFund { get; set; }

        public LoanRequest Loan { get; set; }
        public decimal FundedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal LenderBalance { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // FIXED: Check authentication and role
            if (string.IsNullOrEmpty(userIdStr) || userRole != "Lender")
            {
                return RedirectToPage("/Account/Login");
            }

            Loan = await _context.LoanRequests
                .Include(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .Include(l => l.Fundings)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (Loan == null || Loan.Status != "Approved")
            {
                return RedirectToPage("/Loan/Browse");
            }

            FundedAmount = Loan.Fundings.Sum(f => f.Amount);
            RemainingAmount = Loan.AmountRequested - FundedAmount;

            // Check if already fully funded
            if (RemainingAmount <= 0)
            {
                TempData["ErrorMessage"] = "This loan is already fully funded.";
                return RedirectToPage("/Loan/Browse");
            }

            var lenderId = Guid.Parse(userIdStr);
            var lenderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == lenderId);
            LenderBalance = lenderWallet?.Balance ?? 0;

            // Pre-fill with remaining amount or max balance
            AmountToFund = Math.Min(RemainingAmount, LenderBalance);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // FIXED: Check authentication and role
            if (string.IsNullOrEmpty(userIdStr) || userRole != "Lender")
            {
                return RedirectToPage("/Account/Login");
            }

            var lenderId = Guid.Parse(userIdStr);

            // Re-fetch data to ensure consistency
            Loan = await _context.LoanRequests
                .Include(l => l.Fundings)
                .Include(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (Loan == null) return NotFound();

            FundedAmount = Loan.Fundings.Sum(f => f.Amount);
            RemainingAmount = Loan.AmountRequested - FundedAmount;

            var lenderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == lenderId);
            if (lenderWallet == null) return NotFound("Lender wallet not found.");

            LenderBalance = lenderWallet.Balance;

            // Validations
            if (AmountToFund <= 0)
            {
                ErrorMessage = "Amount must be greater than 0.";
                return Page();
            }
            if (AmountToFund > LenderBalance)
            {
                ErrorMessage = "Insufficient wallet balance.";
                return Page();
            }
            if (AmountToFund > RemainingAmount)
            {
                ErrorMessage = $"You cannot fund more than the remaining amount ({RemainingAmount:N0}).";
                return Page();
            }

            // 1. Create Funding Record
            var funding = new LoanFunding
            {
                FundingId = Guid.NewGuid(),
                LoanId = Loan.LoanId,
                LenderId = lenderId,
                Amount = AmountToFund,
                FundedAt = DateTime.UtcNow
            };
            _context.LoanFundings.Add(funding);

            // 2. Update Wallets
            // Deduct from Lender
            lenderWallet.Balance -= AmountToFund;
            lenderWallet.UpdatedAt = DateTime.UtcNow;

            // Add to Borrower
            var borrowerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == Loan.BorrowerId);
            if (borrowerWallet == null)
            {
                borrowerWallet = new LendSecure.Models.Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = Loan.BorrowerId,
                    Balance = 0,
                    Currency = "RWF",
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(borrowerWallet);
            }
            borrowerWallet.Balance += AmountToFund;
            borrowerWallet.UpdatedAt = DateTime.UtcNow;

            // 3. Create Wallet Transactions (FIXED: Added Currency)
            var lenderTxn = new WalletTransaction
            {
                TxnId = Guid.NewGuid(),
                WalletId = lenderWallet.WalletId,
                TxnType = "LoanFunding",
                Amount = AmountToFund,
                Currency = "RWF",
                RelatedLoanId = Loan.LoanId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(lenderTxn);

            var borrowerTxn = new WalletTransaction
            {
                TxnId = Guid.NewGuid(),
                WalletId = borrowerWallet.WalletId,
                TxnType = "LoanDisbursement",
                Amount = AmountToFund,
                Currency = "RWF",
                RelatedLoanId = Loan.LoanId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(borrowerTxn);

            // 4. Check for Full Funding
            var newFundedAmount = FundedAmount + AmountToFund;
            if (newFundedAmount >= Loan.AmountRequested)
            {
                Loan.Status = "Funded";

                // 5. Generate Repayment Schedule (4 weekly payments)
                decimal totalPrincipal = Loan.AmountRequested;
                decimal totalInterest = totalPrincipal * (Loan.InterestRate / 100m);

                decimal weeklyPrincipal = totalPrincipal / 4m;
                decimal weeklyInterest = totalInterest / 4m;

                for (int i = 1; i <= 4; i++)
                {
                    var repayment = new Repayment
                    {
                        RepaymentId = Guid.NewGuid(),
                        LoanId = Loan.LoanId,
                        ScheduledDate = DateTime.UtcNow.Date.AddDays(i * 7), // FIXED: Use .Date
                        PrincipalAmount = weeklyPrincipal,
                        InterestAmount = weeklyInterest,
                        Status = "Pending",
                        PaidAt = null
                    };
                    _context.Repayments.Add(repayment);
                }
            }

            // FIXED: Add Audit Log
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = lenderId,
                Action = "Loan Funded",
                Details = $"Funded {AmountToFund:N0} RWF to loan {Loan.LoanId} for {Loan.Borrower.Email}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully funded {AmountToFund:N0} RWF!";
            return RedirectToPage("/Loan/MyFundings");
        }
    }
}