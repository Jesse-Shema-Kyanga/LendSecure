using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LendSecure.Models
{
    // ========================================
    // USER MODEL
    // ========================================
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [StringLength(255)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        [Column("role")]
        public string Role { get; set; }

        [Column("mfa_enabled")]
        public bool MfaEnabled { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
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
    [Table("UserProfiles")]
    public class UserProfile
    {
        [Key]
        [Column("profile_id")]
        public Guid ProfileId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [StringLength(100)]
        [Column("first_name")]
        public string FirstName { get; set; }

        [StringLength(100)]
        [Column("last_name")]
        public string LastName { get; set; }

        [StringLength(50)]
        [Column("phone")]
        public string Phone { get; set; }

        [Column("dob")]
        public DateTime? Dob { get; set; }

        [Column("address")]
        public string Address { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    // ========================================
    // KYC DOCUMENT MODEL
    // ========================================
    [Table("KYCDocuments")]
    public class KYCDocument
    {
        [Key]
        [Column("doc_id")]
        public Guid DocId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [StringLength(100)]
        [Column("doc_type")]
        public string DocType { get; set; }

        [StringLength(255)]
        [Column("file_path")]
        public string FilePath { get; set; }

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("reviewer_id")]
        public Guid? ReviewerId { get; set; }

        [Column("reviewed_at")]
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
    [Table("LoanRequests")]
    public class LoanRequest
    {
        [Key]
        [Column("loan_id")]
        public Guid LoanId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("borrower_id")]
        public Guid BorrowerId { get; set; }

        [Required]
        [Column("amount_requested", TypeName = "decimal(12,2)")]
        public decimal AmountRequested { get; set; }

        [StringLength(3)]
        [Column("currency")]
        public string Currency { get; set; } = "RWF";

        [Column("purpose")]
        public string Purpose { get; set; }

        [Column("term_months")]
        public short TermMonths { get; set; }

        [Column("interest_rate", TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("approver_id")]
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
    [Table("LoanFundings")]
    public class LoanFunding
    {
        [Key]
        [Column("funding_id")]
        public Guid FundingId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("loan_id")]
        public Guid LoanId { get; set; }

        [Required]
        [Column("lender_id")]
        public Guid LenderId { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column("funded_at")]
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
    [Table("Repayments")]
    public class Repayment
    {
        [Key]
        [Column("repayment_id")]
        public Guid RepaymentId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("loan_id")]
        public Guid LoanId { get; set; }

        [Required]
        [Column("scheduled_date")]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [Column("principal_amount", TypeName = "decimal(12,2)")]
        public decimal PrincipalAmount { get; set; }

        [Required]
        [Column("interest_amount", TypeName = "decimal(12,2)")]
        public decimal InterestAmount { get; set; }

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        // Navigation Property
        [ForeignKey("LoanId")]
        public LoanRequest Loan { get; set; }
    }

    // ========================================
    // WALLET MODEL
    // ========================================
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        [Column("wallet_id")]
        public Guid WalletId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("balance", TypeName = "decimal(12,2)")]
        public decimal Balance { get; set; } = 10000.00m;

        [StringLength(3)]
        [Column("currency")]
        public string Currency { get; set; } = "RWF";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<WalletTransaction> Transactions { get; set; }
    }

    // ========================================
    // WALLET TRANSACTION MODEL
    // ========================================
    [Table("WalletTransactions")]
    public class WalletTransaction
    {
        [Key]
        [Column("txn_id")]
        public Guid TxnId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("wallet_id")]
        public Guid WalletId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("txn_type")]
        public string TxnType { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(3)]
        [Column("currency")]
        public string Currency { get; set; } = "RWF";

        [Column("related_loan_id")]
        public Guid? RelatedLoanId { get; set; }

        [Column("created_at")]
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
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [Column("log_id")]
        public Guid LogId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("action")]
        public string Action { get; set; }

        [Column("details")]
        public string Details { get; set; }

        [StringLength(50)]
        [Column("ip_address")]
        public string IpAddress { get; set; }

        [Column("user_agent")]
        public string UserAgent { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}