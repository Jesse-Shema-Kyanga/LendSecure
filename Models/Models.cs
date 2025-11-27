using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LendSecure.Models
{
    // ========================================
    // USER MODEL
    // ========================================
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        public string Role { get; set; } // "Borrower", "Lender", "Admin"

        public bool MfaEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public UserProfile Profile { get; set; }
        public ICollection<KYCDocument> KYCDocuments { get; set; }
        public ICollection<LoanRequest> LoanRequests { get; set; }
        public ICollection<LoanFunding> Fundings { get; set; }
        public ICollection<Wallet> Wallets { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }

    // ========================================
    // USER PROFILE MODEL
    // ========================================
    public class UserProfile
    {
        [Key]
        public Guid ProfileId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        public DateTime? Dob { get; set; }

        public string Address { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    // ========================================
    // KYC DOCUMENT MODEL
    // ========================================
    public class KYCDocument
    {
        [Key]
        public Guid DocId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [StringLength(100)]
        public string DocType { get; set; } // "ID", "Passport", "StudentID"

        [StringLength(255)]
        public string FilePath { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        public Guid? ReviewerId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("ReviewerId")]
        public User Reviewer { get; set; }
    }

    // ========================================
    // LOAN REQUEST MODEL
    // ========================================
    public class LoanRequest
    {
        [Key]
        public Guid LoanId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BorrowerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal AmountRequested { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "RWF";

        public string Purpose { get; set; }

        public short TermMonths { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Funded", "Repaying", "Completed", "Rejected"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public Guid? ApproverId { get; set; }

        // Navigation Properties
        [ForeignKey("BorrowerId")]
        public User Borrower { get; set; }

        [ForeignKey("ApproverId")]
        public User Approver { get; set; }

        public ICollection<LoanFunding> Fundings { get; set; }
        public ICollection<Repayment> Repayments { get; set; }
        public ICollection<WalletTransaction> Transactions { get; set; }
    }

    // ========================================
    // LOAN FUNDING MODEL
    // ========================================
    public class LoanFunding
    {
        [Key]
        public Guid FundingId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LoanId { get; set; }

        [Required]
        public Guid LenderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        public DateTime FundedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("LoanId")]
        public LoanRequest Loan { get; set; }

        [ForeignKey("LenderId")]
        public User Lender { get; set; }
    }

    // ========================================
    // REPAYMENT MODEL
    // ========================================
    public class Repayment
    {
        [Key]
        public Guid RepaymentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LoanId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal PrincipalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal InterestAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Paid", "Overdue"

        public DateTime? PaidAt { get; set; }

        // Navigation Property
        [ForeignKey("LoanId")]
        public LoanRequest Loan { get; set; }
    }

    // ========================================
    // WALLET MODEL
    // ========================================
    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Balance { get; set; } = 10000.00m; // Starting demo balance

        [StringLength(3)]
        public string Currency { get; set; } = "RWF";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<WalletTransaction> Transactions { get; set; }
    }

    // ========================================
    // WALLET TRANSACTION MODEL
    // ========================================
    public class WalletTransaction
    {
        [Key]
        public Guid TxnId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WalletId { get; set; }

        [Required]
        [StringLength(50)]
        public string TxnType { get; set; } // "Deposit", "Withdrawal", "LoanFunding", "LoanRepayment"

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "RWF";

        public Guid? RelatedLoanId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("WalletId")]
        public Wallet Wallet { get; set; }

        [ForeignKey("RelatedLoanId")]
        public LoanRequest RelatedLoan { get; set; }
    }

    // ========================================
    // AUDIT LOG MODEL
    // ========================================
    public class AuditLog
    {
        [Key]
        public Guid LogId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Action { get; set; }

        public string Details { get; set; } // JSON stored as string

        [StringLength(50)]
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}