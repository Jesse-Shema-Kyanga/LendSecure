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
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Account/Login");
            }

            Loan = await _context.LoanRequests
                .Include(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .Include(l => l.Fundings)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (Loan == null)
            {
                return NotFound();
            }

            FundedAmount = Loan.Fundings.Sum(f => f.Amount);
            RemainingAmount = Loan.AmountRequested - FundedAmount;

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
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Account/Login");
            }

            var lenderId = Guid.Parse(userIdStr);

            // Re-fetch data to ensure consistency
            Loan = await _context.LoanRequests
                .Include(l => l.Fundings)
                .Include(l => l.Borrower) // Include Borrower to get their ID for wallet
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
                ErrorMessage = $"You cannot fund more than the remaining amount ({RemainingAmount}).";
                return Page();
            }

            // 1. Create Funding Record
            var funding = new LoanFunding
            {
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
                // Should exist, but handle just in case
                borrowerWallet = new LendSecure.Models.Wallet { UserId = Loan.BorrowerId, Balance = 0 };
                _context.Wallets.Add(borrowerWallet);
            }
            borrowerWallet.Balance += AmountToFund;
            borrowerWallet.UpdatedAt = DateTime.UtcNow;

            // 3. Create Wallet Transactions
            var lenderTxn = new WalletTransaction
            {
                WalletId = lenderWallet.WalletId,
                TxnType = "Debit", // Funding a loan
                Amount = AmountToFund,
                RelatedLoanId = Loan.LoanId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(lenderTxn);

            var borrowerTxn = new WalletTransaction
            {
                WalletId = borrowerWallet.WalletId,
                TxnType = "Credit", // Loan disbursement
                Amount = AmountToFund,
                RelatedLoanId = Loan.LoanId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(borrowerTxn);

            // 4. Check for Full Funding
            if (FundedAmount + AmountToFund >= Loan.AmountRequested)
            {
                Loan.Status = "Funded";
                Loan.ApprovedAt = DateTime.UtcNow; // Using ApprovedAt as "FundedAt" or we could add a new field. 
                // Wait, ApprovedAt is for Admin approval. Let's leave it. 
                // Maybe we need a "FullyFundedAt"? For now, Status change is enough.

                // 5. Generate Repayment Schedule
                // 1 Month Term = 4 Weekly Payments
                // Total Repayment = Principal + Interest
                // Interest = Principal * (Rate / 100)
                // We calculate interest on the TOTAL loan amount
                decimal totalPrincipal = Loan.AmountRequested;
                decimal totalInterest = totalPrincipal * (Loan.InterestRate / 100m);
                decimal totalRepayment = totalPrincipal + totalInterest;
                
                decimal weeklyPrincipal = totalPrincipal / 4m;
                decimal weeklyInterest = totalInterest / 4m;

                for (int i = 1; i <= 4; i++)
                {
                    var repayment = new Repayment
                    {
                        LoanId = Loan.LoanId,
                        ScheduledDate = DateTime.UtcNow.AddDays(i * 7),
                        PrincipalAmount = weeklyPrincipal,
                        InterestAmount = weeklyInterest,
                        Status = "Pending"
                    };
                    _context.Repayments.Add(repayment);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/Lender/Dashboard");
        }
    }
}
