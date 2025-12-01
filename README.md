# LendSecure

**A modern micro-lending platform built with ASP.NET Core, Razor Pages, SQL Server, and integrated loan workflows.**

---

## 🚀 Overview

**LendSecure** is a digital lending platform designed to automate and streamline the processes of:

* Borrower onboarding
* KYC submission and verification
* Loan request and approval
* Repayment scheduling
* Transaction tracking
* Admin oversight

It provides a secure, scalable, and user-friendly system for both borrowers and staff.

---

## 🧩 Core Features

### **👤 Borrower Module**

* Register/login using session-based authentication
* Upload KYC documents (ID, proof of address…)
* Request loans with validated loan forms
* View loan history and statuses
* View repayment schedule and transaction breakdown
* Upload repayment proof (optional)

### **🏦 Manager/Admin Module** *(if needed)*

* Approve or decline loan requests
* Verify borrower KYC documents
* Manage repayment confirmations
* Manage borrowers and monitor analytics

---

## 🛠️ Tech Stack

| Category          | Technology Used              |
| ----------------- | ---------------------------- |
| Backend Framework | ASP.NET Core 8 (Razor Pages) |
| Frontend          | Razor Pages + Bootstrap      |
| Database          | Microsoft SQL Server         |
| Authentication    | Session-based auth (custom)  |
| File Storage      | Local `wwwroot/uploads`      |
| Version Control   | Git + GitHub                 |

---

## 📌 Project Structure

```
LendSecure/
│
├── Pages/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Logout.cshtml
│   │   ├── Register.cshtml
│   │
│   ├── Admin/
│   │   ├── AuditLogs.cshtml
│   │   ├── Dashboard.cshtml
│   │   ├── KYCReview.cshtml
│   │   ├── LoanApprovals.cshtml
│   │   ├── Users.cshtml
│   │
│   ├── Borrower/
│   │   ├── Dashboard.cshtml 
│   │   
│   ├── Lender/
│   │   ├── Dashboard.cshtml 
│   │
│   ├── Repayment/
│   │   ├── Schedule.cshtml
│   │   
│   ├── Loan/
│   │   ├── Browse.cshtml
│   │   ├── Create.cshtml
│   │   ├── MyFoundings.cshtml
│   │   ├── MyLoans.cshtml
│   │
│   ├── Wallet/
│   │   ├── ViewImports.cshtml
│   │   ├── ViewStart.cshtml
│   │   
│   │
│   ├── Shared/
│   │   ├── Layout.cshtml
│   │   ├── validationScriptPartial.cshtml
│   │
│   ├── KYC/
│   │   ├── Upload.cshtml
│   │    
│   │
│   └── 
│
├── wwwroot/
│   └── uploads/     
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Program.cs
├── appsettings.json
└── README.md
```

---

## 🧪 Getting Started (Local Setup)

### **1. Clone the Repository**

```
git clone https://github.com/Jesse-Shema-Kyanga/LendSecure.git
cd LendSecure
```

### **2. Restore Dependencies**

Visual Studio will automatically restore NuGet packages, or run:

```
dotnet restore
```

### **3. Update Database Connection**

Modify your connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=LendSecureDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### **4. Create the Database**

Run the SQL scripts for:

* Borrowers
* KYC table
* Loans
* Repayments



### **5. Run the App**

```
dotnet run
```

Or click **Run** in Visual Studio.

---

## 🧵 Git Workflow (For Contributors)

We follow a **feature-branch workflow**:

1. Pull latest main branch:

   ```bash
   git pull origin main
   ```
2. Create a feature branch:

   ```bash
   git checkout -b feature/loan-request
   ```
3. Commit changes:

   ```bash
   git commit -m "Implement loan request feature"
   ```
4. Push the branch:

   ```bash
   git push origin feature/loan-request
   ```
5. Open a Pull Request (PR) on GitHub.


## 🧩 Future Enhancements

* SMS/Email notifications
* Automated credit scoring
* Admin dashboard analytics
* OTP/2FA login
* Payment gateway integration

---

## 👥 Contributors



---

## 📄 License

This project is for academic/collaborative purposes and not licensed for commercial use.

---

## 💬 Feedback

If you have ideas or improvements, open an **Issue** or submit a **Pull Request**.
Your contributions are welcome!
