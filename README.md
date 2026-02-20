# SecureBank - Modern Banking Management System

A comprehensive online banking platform built with ASP.NET Core and MVC, designed to deliver secure, fast, and convenient banking services for modern customers.

```
   _____                          ____              _    
  / ____|                        |  _ \            | |   
 | (___   ___  ___ _   _ _ __ ___| |_) | __ _ _ __ | | __
  \___ \ / _ \/ __| | | | '__/ _ \  _ < / _` | '_ \| |/ /
  ____) |  __/ (__| |_| | | |  __/ |_) | (_| | | | |   < 
 |_____/ \___|\___|\__,_|_|  \___|____/ \__,_|_| |_|_|\_\
                                                          
                     Modern Online Banking Platform
    ASP.NET Core MVC • SQL Server • Secure Authentication • RESTful API
```

## 🚀 Quick Start

### 1. Prerequisites

* .NET 8.0 SDK or later
* SQL Server (Express or higher)
* Visual Studio 2022 or VS Code
* Git

### 2. Clone & Configure

```bash
# Clone the repository
git clone https://github.com/EugeneMilford/ASP_SecureBank.git
cd ASP_SecureBank

# Update connection string in appsettings.json (API project)
# Edit "DefaultConnection" to point to your SQL Server
```

### 3. Initialize Database

```bash
# Navigate to API project
cd SecureBank.API

# Apply database migrations
dotnet ef database update

# This creates the database schema and seeds:
# - Demo user accounts
# - Sample bank accounts
# - Initial credit cards and loans
# - Transaction history
```

### 4. Run the Application

```bash
# Terminal 1 - Run the API
cd SecureBank.API
dotnet run
# API runs at: https://localhost:7xxx

# Terminal 2 - Run the MVC UI
cd SecureBank.UI
dotnet run
# UI runs at: https://localhost:5xxx
```

**Access the application:**
- **SecureBank UI:** https://localhost:5xxx
- **API Swagger:** https://localhost:7xxx/swagger

---

## 📋 Overview

### Core Features

* ✅ **Account Management** — Open, view, and manage multiple bank accounts (Savings, Checking, Business)
* ✅ **Fund Transfers** — Transfer money between accounts instantly and securely
* ✅ **Bill Payments** — Pay bills and manage recurring payments with ease
* ✅ **Credit Cards** — Apply for, manage, and track credit card transactions
* ✅ **Loan Services** — Apply for personal and business loans with transparent terms
* ✅ **Transaction History** — Complete audit trail of all account activities
* ✅ **Secure Authentication** — JWT-based authentication with password encryption
* ✅ **Responsive Design** — Mobile-ready interface with modern UI/UX
* ✅ **24/7 Banking** — Access your accounts anytime, anywhere
* ✅ **Customer Support** — Built-in support system with FAQ and contact features

---

## 👥 User Roles & Permissions

### 👤 Bank Customer — Standard User Access

**Account Management:**
* ✅ View personal account balances and details
* ✅ Open new accounts (Savings, Checking, Business)
* ✅ Close accounts (subject to zero balance)
* ✅ View complete transaction history
* ❌ Cannot access other customers' accounts

**Transactions:**
* ✅ Transfer funds between own accounts
* ✅ Make external transfers to other banks
* ✅ Pay bills to registered payees
* ✅ Schedule recurring payments
* ✅ Download transaction statements

**Credit & Loans:**
* ✅ Apply for credit cards
* ✅ View credit card balances and transactions
* ✅ Make credit card payments
* ✅ Apply for personal and business loans
* ✅ View loan details and payment schedules
* ✅ Make loan payments

**Profile & Settings:**
* ✅ Update personal information
* ✅ Change password
* ✅ Manage notification preferences
* ✅ Set up security questions
* ❌ Cannot modify interest rates or fees

**Demo User Credentials:**
- Username: `demo.user@securebank.com`
- Password: `Demo@2024`

---

### 👑 Bank Administrator — Full System Access

**Customer Management:**
* ✅ View all customer accounts
* ✅ Manage customer profiles
* ✅ Verify customer identities
* ✅ Suspend/activate accounts
* ✅ Access customer support tickets

**Account Operations:**
* ✅ Open accounts on behalf of customers
* ✅ Adjust account balances (with authorization)
* ✅ Reverse transactions (error corrections)
* ✅ Set overdraft limits
* ✅ Waive fees when appropriate

**Credit & Loan Management:**
* ✅ Approve/reject credit card applications
* ✅ Set credit limits
* ✅ Approve/reject loan applications
* ✅ Adjust interest rates
* ✅ Manage loan payment schedules

**Financial Operations:**
* ✅ Process deposits and withdrawals
* ✅ Generate financial reports
* ✅ Audit transaction logs
* ✅ Monitor fraud alerts
* ✅ Reconcile accounts

**System Administration:**
* ✅ Configure system settings
* ✅ Manage user roles and permissions
* ✅ Update interest rates and fees
* ✅ Access system logs
* ✅ Backup and restore data

**Default Admin Credentials:**
- Username: `admin@securebank.com`
- Password: `Admin@2024`

---

## 💼 Banking Features

### Account Types

| Account Type | Features | Interest Rate | Minimum Balance |
|--------------|----------|---------------|-----------------|
| **Savings Account** | High interest, limited withdrawals | 4.5% APY | R1,000 |
| **Checking Account** | Unlimited transactions, debit card | 0.5% APY | R500 |
| **Business Account** | Business features, merchant services | 2.0% APY | R5,000 |

### Transaction Types

```
1. Deposits
   └─> Cash, Check, Transfer

2. Withdrawals
   └─> ATM, Branch, Online Transfer

3. Transfers
   ├─> Internal (Between SecureBank accounts)
   └─> External (To other banks)

4. Bill Payments
   ├─> Utility bills
   ├─> Credit card payments
   └─> Loan payments

5. Card Transactions
   ├─> Debit card purchases
   └─> Credit card charges
```

### Credit Card Features

```
✓ Competitive Interest Rates
✓ Rewards Program (Cashback & Points)
✓ Fraud Protection
✓ Zero Liability for Unauthorized Charges
✓ Virtual Cards for Online Shopping
✓ Instant Notifications
✓ Travel Insurance
✓ Purchase Protection
```

### Loan Products

```
Personal Loans
├─> Loan Amount: R5,000 - R500,000
├─> Interest Rate: 8.5% - 15%
├─> Repayment Period: 12 - 60 months
└─> No Hidden Fees

Business Loans
├─> Loan Amount: R50,000 - R5,000,000
├─> Interest Rate: 6.5% - 12%
├─> Repayment Period: 24 - 120 months
└─> Flexible Terms
```

---

## 🛠️ Technology Stack

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| **ASP.NET MVC** | .NET 8.0 | Web UI framework |
| **Razor Views** | Latest | Server-side rendering |
| **Bootstrap** | 5.3.2 | Responsive design & components |
| **jQuery** | 3.7.1 | DOM manipulation & AJAX |
| **Font Awesome** | 6.0 | Icon library |
| **Bootstrap Icons** | 1.11.0 | Additional icons |
| **AOS** | 2.3.4 | Animation on scroll |

### Backend (API)

| Technology | Version | Purpose |
|------------|---------|---------|
| **ASP.NET Core Web API** | 8.0 | RESTful API framework |
| **Entity Framework Core** | 8.0 | ORM for database operations |
| **SQL Server** | Latest | Primary database |
| **JWT Authentication** | 8.0 | Token-based authentication |
| **AutoMapper** | Latest | Object-to-object mapping |
| **Swashbuckle** | Latest | API documentation (Swagger) |

### Testing

| Tool | Version | Purpose |
|------|---------|---------|
| **xUnit** | Latest | Unit testing framework |
| **Moq** | Latest | Mocking library |

---

## 📁 Project Structure

```
ASP_SecureBank/
├── SecureBank.API/                  # Backend REST API
│   ├── Controllers/
│   │   ├── AccountsController.cs    # Account CRUD operations
│   │   ├── TransactionsController.cs # Transaction management
│   │   ├── CreditCardsController.cs  # Credit card operations
│   │   ├── LoansController.cs       # Loan management
│   │   ├── UsersController.cs       # Authentication & user management
│   │   └── BillPaymentsController.cs # Bill payment processing
│   ├── Data/
│   │   ├── BankingContext.cs        # EF Core database context
│   │   ├── User.cs                  # User entity
│   │   ├── Account.cs               # Bank account entity
│   │   ├── Transaction.cs           # Transaction records
│   │   ├── CreditCard.cs            # Credit card entity
│   │   ├── Loan.cs                  # Loan entity
│   │   └── BillPayment.cs           # Bill payment entity
│   ├── Models/
│   │   ├── DTOs/                    # Data transfer objects
│   │   ├── Request/                 # API request models
│   │   └── Response/                # API response models
│   ├── Services/
│   │   ├── IAccountService.cs
│   │   ├── AccountService.cs
│   │   ├── ITransactionService.cs
│   │   ├── TransactionService.cs
│   │   └── AuthenticationService.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── JwtMiddleware.cs
│   ├── Migrations/                  # EF Core migrations
│   ├── Program.cs                   # API entry point
│   └── appsettings.json             # API configuration
│
├── SecureBank.UI/                   # ASP.NET MVC UI
│   ├── Controllers/
│   │   ├── HomeController.cs        # Home page & about
│   │   ├── AuthUserController.cs    # Login & registration
│   │   ├── AccountsController.cs    # Account management views
│   │   ├── TransactionsController.cs # Transaction views
│   │   ├── CreditCardsController.cs  # Credit card views
│   │   ├── LoansController.cs       # Loan views
│   │   └── BillPaymentsController.cs # Bill payment views
│   ├���─ Views/
│   │   ├── Home/
│   │   │   ├── Index.cshtml         # Landing page
│   │   │   └── About.cshtml         # About SecureBank
│   │   ├── AuthUser/
│   │   │   ├── Login.cshtml         # User login
│   │   │   └── Register.cshtml      # User registration
│   │   ├── Accounts/
│   │   │   ├── Index.cshtml         # Account dashboard
│   │   │   ├── Create.cshtml        # Open new account
│   │   │   └── Details.cshtml       # Account details
│   │   ├── Transactions/
│   │   │   ├── Index.cshtml         # Transaction history
│   │   │   ├── Transfer.cshtml      # Fund transfer
│   │   │   └── History.cshtml       # Transaction log
│   │   ├── CreditCards/
│   │   │   ├── Index.cshtml         # Credit card list
│   │   │   ├── Apply.cshtml         # Card application
│   │   │   └── Details.cshtml       # Card details
│   │   ├── Loans/
│   │   │   ├── Index.cshtml         # Loan list
│   │   │   ├── Apply.cshtml         # Loan application
│   │   │   └── Details.cshtml       # Loan details
│   │   ├── Welcome/
│   │   │   └── Index.cshtml         # Welcome/splash page
│   │   └── Shared/
│   │       ├── _Layout.cshtml       # Main layout
��   │       └── _LoginLayout.cshtml  # Login layout
│   ├── Services/
│   │   ├── ApiClient.cs             # HTTP client for API calls
│   │   ├── IAuthenticationService.cs
│   │   ├── AuthenticationService.cs
│   │   └── SessionHelper.cs         # Session management
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── Bank.css             # Custom styles
│   │   │   └── site.css             # Site-wide styles
│   │   ├── js/
│   │   │   └── site.js              # JavaScript utilities
│   │   └── img/                     # Images and assets
│   ├── Program.cs                   # MVC entry point
│   └── appsettings.json             # UI configuration
│
├── SecureBank.Tests/                # Unit & Integration Tests
│   ├── AccountTests.cs
│   ├── TransactionTests.cs
│   └── LoanTests.cs
│
└── SecureBank.sln                   # Solution file
```

---

## 🗄️ Database Schema

### Core Tables

```sql
-- Authentication
Users                -- User accounts and credentials
UserRoles            -- User role assignments

-- Banking
Accounts             -- Bank accounts (Savings, Checking, Business)
Transactions         -- All account transactions
CreditCards          -- Credit card records
Loans                -- Loan applications and details
BillPayments         -- Bill payment history

-- Supporting
Payees               -- Registered payees for bill payments
TransferRecipients   -- Saved transfer recipients
Notifications        -- User notifications
AuditLogs            -- System audit trail
```

### Entity Relationships

```
User (1) ────────< (N) Accounts
                         │
                         │ (1)
                         │
                         ↓
                        (N) Transactions
                               │
                               ├── Sender Account (FK)
                               └── Receiver Account (FK)

User (1) ────────< (N) CreditCards
                         │
                         │ (1)
                         │
                         ↓
                        (N) CreditCard Transactions

User (1) ────────< (N) Loans
                         │
                         │ (1)
                         │
                         ↓
                        (N) Loan Payments

User (1) ─────��──< (N) BillPayments
                         │
                         │ (N)
                         │
                         ↓
                        (1) Payee
```

---

## 🔐 Authentication & Security

### JWT Token-Based Authentication

SecureBank uses **JWT (JSON Web Token)** for secure, stateless authentication:

```csharp
// Token contains encrypted claims
{
  "sub": "user-id-guid",
  "email": "customer@securebank.com",
  "role": "Customer",
  "firstName": "John",
  "lastName": "Doe",
  "accountNumber": "1234567890",
  "exp": 1234567890  // Expiration timestamp
}
```

**Security Features:**
* 🔒 **Password Hashing** — PBKDF2 with salt
* 🎫 **JWT Tokens** — Secure API authentication
* ⏰ **Session Management** — Configurable token expiration
* 🚫 **Authorization** — Role-based access control
* 🔐 **HTTPS Enforced** — All traffic encrypted
* 🛡️ **CSRF Protection** — Anti-forgery tokens
* 📝 **Audit Logging** — Complete activity tracking
* 🚨 **Fraud Detection** — Real-time transaction monitoring

---

## 🌐 API Endpoints

### Authentication (`/api/users`)

```
POST   /api/users/register         - Register new user
POST   /api/users/login            - Login and receive JWT token
POST   /api/users/logout           - Logout and invalidate token
GET    /api/users/profile          - Get user profile
PUT    /api/users/profile          - Update user profile
POST   /api/users/change-password  - Change password
```

### Accounts (`/api/accounts`)

```
GET    /api/accounts               - Get all user accounts
GET    /api/accounts/{id}          - Get account by ID
POST   /api/accounts               - Create new account
PUT    /api/accounts/{id}          - Update account
DELETE /api/accounts/{id}          - Close account
GET    /api/accounts/{id}/balance  - Get account balance
```

### Transactions (`/api/transactions`)

```
GET    /api/transactions              - Get all transactions
GET    /api/transactions/{id}         - Get transaction by ID
POST   /api/transactions/transfer     - Transfer funds
POST   /api/transactions/deposit      - Deposit money
POST   /api/transactions/withdrawal   - Withdraw money
GET    /api/transactions/history      - Get transaction history
```

### Credit Cards (`/api/creditcards`)

```
GET    /api/creditcards            - Get all user credit cards
GET    /api/creditcards/{id}       - Get credit card by ID
POST   /api/creditcards/apply      - Apply for credit card
POST   /api/creditcards/{id}/payment - Make credit card payment
GET    /api/creditcards/{id}/transactions - Get card transactions
```

### Loans (`/api/loans`)

```
GET    /api/loans                  - Get all user loans
GET    /api/loans/{id}             - Get loan by ID
POST   /api/loans/apply            - Apply for loan
POST   /api/loans/{id}/payment     - Make loan payment
GET    /api/loans/{id}/schedule    - Get payment schedule
```

### Bill Payments (`/api/billpayments`)

```
GET    /api/billpayments           - Get all bill payments
POST   /api/billpayments           - Make bill payment
POST   /api/billpayments/schedule  - Schedule recurring payment
DELETE /api/billpayments/{id}      - Cancel scheduled payment
```
---

## 🚀 Deployment

### API Deployment

```bash
cd SecureBank.API

# Publish for production
dotnet publish -c Release -o ./publish

# Update appsettings.Production.json
# - Set production connection string
# - Configure JWT secret key
# - Enable HTTPS redirection

# Deploy to:
# - Azure App Service
# - IIS
# - Docker
# - AWS Elastic Beanstalk
```

### UI Deployment

```bash
cd SecureBank.UI

# Publish for production
dotnet publish -c Release -o ./publish

# Update appsettings.Production.json
# - Set ApiSettings:BaseUrl to production API

# Deploy to:
# - Azure App Service
# - IIS
# - Docker
```

### Database Migration

```bash
# Generate SQL script for production
cd SecureBank.API
dotnet ef migrations script -o ./migration.sql

# Apply to production database
# - Use SQL Server Management Studio
# - Or Azure SQL Database portal
```

---

## 🧪 Development Commands

### API Commands

```bash
# Build project
dotnet build

# Run API
dotnet run
# API: https://localhost:7xxx
# Swagger: https://localhost:7xxx/swagger

# Watch mode (auto-reload)
dotnet watch run

# Database migrations
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop --force
dotnet ef migrations list
dotnet ef migrations remove

# Run tests
cd SecureBank.Tests
dotnet test
```

### UI Commands

```bash
# Build project
dotnet build

# Run MVC app
dotnet run
# UI: https://localhost:5xxx

# Watch mode (hot reload)
dotnet watch run
```

---

## 🐛 Troubleshooting

### Issue: API Connection Failed

**Symptoms:** UI shows "Unable to connect to API"

**Solutions:**
1. Verify API is running: https://localhost:7xxx/swagger
2. Check `ApiSettings:BaseUrl` in UI's `appsettings.json`
3. Ensure firewall allows localhost connections
4. Check browser console for CORS errors

### Issue: Database Connection Failed

**Symptoms:** API crashes with `SqlException`

**Solutions:**
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database exists: `dotnet ef database update`
4. Test connection with SQL Server Management Studio

### Issue: JWT Token Invalid

**Symptoms:** API returns 401 Unauthorized

**Solutions:**
1. Clear browser cookies and re-login
2. Check token expiration (`JwtSettings:ExpiresInHours`)
3. Verify JWT secret key matches between API and UI
4. Inspect JWT token at https://jwt.io

### Issue: Login Failed

**Symptoms:** Cannot login with demo credentials

**Solutions:**
1. Ensure database is seeded: `dotnet ef database update`
2. Check User table in database
3. Verify password hashing is working
4. Check API logs for authentication errors
---

## 📄 License

This project is licensed under the **MIT License** - see the LICENSE file for details.

## 👏 Acknowledgments

- Built using ASP.NET Core and MVC
- Bootstrap for responsive UI
- Entity Framework Core for data access
- JWT for secure authentication
- Font Awesome & Bootstrap Icons for iconography
