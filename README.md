# AcxiomCRM

A role-based CRM built with ASP.NET Core 8 MVC. It covers customers, leads, opportunities, follow-ups, activities, a dashboard, an audit log and a REST API.

**Stack:** ASP.NET Core 8 MVC + Razor, ASP.NET Core Identity, Entity Framework Core (SQLite), Bootstrap 5, Chart.js, jQuery unobtrusive validation.

## Run it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd AcxiomCRM
dotnet dev-certs https --trust     # once, so the browser trusts https://localhost
dotnet run --launch-profile https
```

Open https://localhost:7064. On first start the app applies the EF Core migration, creates `app.db`, seeds the roles and demo users, and adds a little sample data.

### Demo logins (Development only)

| Role | Email | Password |
|---|---|---|
| Admin | admin@acxiom.local | Admin@123 |
| Manager | manager@acxiom.local | Manager@123 |
| SalesExecutive | sales1@acxiom.local | Sales@123 |
| SalesExecutive | sales2@acxiom.local | Sales@123 |

The demo passwords are in `appsettings.Development.json`, not in code. New users who self-register get the **SalesExecutive** role. An Admin can change it on the Users page.

## Project structure

```
AcxiomCRM/
  Controllers/        MVC controllers (one per module) + CrmControllerBase (shared assignment/scope checks)
  Controllers/Api/    REST API controllers (customers, leads, opportunities)
  Models/             EF entities, enums, roles
  Models/Dtos/        API request/response DTOs (entities are never returned directly)
  Models/Validation/  Shared regex rules + NotInPast attribute (server + client)
  Data/               DbContext (with audit capture), seed data, migrations
  Services/           Record scoping, lead status workflow, Identity sign-in/user manager overrides
  ViewModels/         Dashboard, users, pipeline report
  Views/              Razor views
  wwwroot/js/validation.js   Client-side adapter for NotInPast
```

## How the requirements are met

**Authentication and security**
- Uses ASP.NET Core Identity throughout: Register, Login and Logout, password hashing, cookies. There is no custom password table.
- Password policy: at least 8 characters, with upper case, lower case, a digit and a symbol.
- Lockout after 5 failed logins, for 5 minutes. `AppSignInManager` forces `lockoutOnFailure: true`, because the default UI passes `false`.
- Cookie settings: HttpOnly, Secure, SameSite=Strict.
- Every MVC POST has `[ValidateAntiForgeryToken]`.
- A global `AuthorizeFilter` means every page and API needs a logged-in user.
- API requests get 401/403 status codes instead of a redirect to the login page.
- EF Core LINQ only, so all SQL is parameterized. No SQL is built from strings.
- Password hashes never appear in views, DTOs or logs.

**Role-based authorization**
| | Admin | Manager | SalesExecutive |
|---|---|---|---|
| CRM records | All | All | Only records assigned to them |
| Delete records | Yes | Yes | No |
| Pipeline report | Yes | Yes | No |
| Users and Audit Log | Yes | No | No |

Record-level scope is enforced on the server by `ScopeExtensions.VisibleTo()` on every query. If a Sales Executive changes the id in a URL to someone else's record, they get a 404. Anything a Sales Executive creates is always assigned to them.

**Validation**
- Client-side validation comes from the same data annotations as the server: Required, email, phone, length, date and numeric rules.
- The server always validates again. Crafted requests are rejected.
- Business rules:
  - Opportunity amount must be greater than 0.
  - Probability must be from 0 to 100.
  - Expected close date can't be in the past while the opportunity is open.
  - A follow-up date can't be before today while the follow-up is Planned.
  - Customer email and phone must be unique; this is also enforced by unique indexes in the database.
  - Lead status values must be valid, and only the transitions in `LeadWorkflow` are allowed.
- Phone rule: a 10-digit Indian mobile number (`^[6-9]\d{9}$`).

**Lead workflow:** New → Contacted → Qualified → **Convert**, with Unqualified and Lost along the way. Converting creates a customer, or reuses one with the same email or phone. It also creates an opportunity if the lead has an expected value.

**Audit log:** `ApplicationDbContext.SaveChangesAsync` records every Create, Update and Delete on CRM records. Each entry has old and new values, user, time and IP address. The sign-in manager records Login, LoginFailed, LockedOut and Logout. The Users page records RoleChange, Activate, Deactivate and Unlock. The log can only be added to: there is no edit or delete. It is viewable by Admin only, with filters.

**Dashboard:** 8 KPI cards plus 3 Chart.js charts (lead status, opportunity pipeline, monthly won sales). All figures are limited to the records the user is allowed to see.

## REST API

All endpoints need a logged-in session (cookie) and return JSON. Enums are sent as strings.

| Method | Endpoint | Notes |
|---|---|---|
| GET | /api/customers?q= | Scoped to the caller |
| GET | /api/customers/{id} | 404 if missing or out of scope |
| POST | /api/customers | 201; 400 if invalid; 409 if the email or phone already exists |
| PUT | /api/customers/{id} | 200; 400, 404 or 409 |
| DELETE | /api/customers/{id} | Admin/Manager only (403 otherwise); 409 if the customer has opportunities |
| GET | /api/leads?status= | |
| GET | /api/leads/{id} | |
| POST | /api/leads | 201 / 400 |
| GET | /api/opportunities?stage= | |
| GET | /api/opportunities/{id} | |
| POST | /api/opportunities | 201 / 400; the customer must be one the caller can see |

The API uses the same cookie as the website. CSRF is mitigated by the `SameSite=Strict` cookie.

## Simplifications (time-boxed build)

- **Managers' teams:** there is no team model, so a Manager sees all CRM records.
- **Reports:** only the **Pipeline report** (by stage and by owner, with weighted amount) is a dedicated page. The other reports are covered by the filtered list pages and the dashboard.
- **Not included:** pagination, exports, dashboard date filters, Swagger and automated unit tests.
- **Database:** SQLite, so there's nothing to install. Switching to SQL Server means changing the EF provider and connection string.
