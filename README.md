# MindTec IT Solutions

Source code for the **MindTec IT Solutions** web site (<https://mindtec.ca>), a managed IT services company based in Mississauga, Ontario, Canada.

The application has two parts in a single ASP.NET Core project:

1. **Public marketing site**: home, about, services and a contact form that emails enquiries to the team.
2. **Internal dashboard** (login required): an operations and finance back office for managing clients, vendors, tickets, financials, users and configuration.

## Tech stack

| Area | Technology |
|------|------------|
| Framework | ASP.NET Core 8, Razor Pages (C#) |
| Database | SQL Server (ADO.NET for most pages, Entity Framework Core 8 for the user and transaction tables) |
| Auth | Cookie authentication with role-based authorization |
| Front end | Bootstrap 5, Bootstrap Icons, AOS scroll animations |
| Email | `System.Net.Mail.SmtpClient` |
| Hosting | IIS on SmarterASP.NET (Web Deploy / FTP publish from Visual Studio) |

## Features

### Public site (`/`)
- Responsive single-page layout with Home, About, Services and Contact sections.
- Mobile-friendly navigation: the menu closes after a selection or an outside tap, and section links land below the sticky header.
- Click-to-call and click-to-email links for the company phone number and addresses.
- **"Submit Request" contact form**: sends the enquiry by SMTP, with the visitor's address as Reply-To.

### Dashboard (`/Dashboard`, requires login)
| Page | Purpose |
|------|---------|
| Clients | Client records and details |
| Vendors | Vendor master, contacts and websites |
| Financials | Financial transactions |
| Tickets (New / Update / Logs) | Create, update and track support tickets |
| Operations | Operations overview |
| CoConfiguration | Company configuration records |
| Users | User administration |
| User Types, Config Types | Admin-only lookup management (roles `SA`, `Admin`) |

## Project structure

```
Pages/                 Razor Pages
  Index, Login, ...    Public pages (use Shared/_Layout.cshtml)
  Dashboard/           Authenticated pages (use Shared/_DashboardLayout.cshtml)
  Shared/              Layouts and partials
Models/                Plain DTO classes
Data/AppDbContext.cs   EF Core context (AppUsers and Transactions only)
Properties/            launchSettings.json (local run profiles)
wwwroot/               Static files (css, js, img, client libraries)
VendorTables.sql       SQL script for the vendor tables
Program.cs             Startup: Razor Pages, cookie auth, EF Core
```

Notes on how the code is organised:

- **Data access is mixed.** Most page models inject `IConfiguration` and run raw ADO.NET (`SqlConnection` / `SqlCommand`) against the `DefaultConnection` string. Only `AppUsers` and `Transactions` are in the EF model.
- **There are no EF migrations.** `Program.cs` calls `Database.EnsureCreated()`, which creates only the EF-modelled tables, and only on an empty database. The other tables (vendors, clients, tickets, config types and so on) must be created in SQL Server by hand. `VendorTables.sql` is one example.
- **Auth.** Users sign in at `/Login`. The user's type name becomes the role claim. Dashboard pages require login, and the type-management pages require the `SA` or `Admin` role.

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Express is enough for local development)
- Visual Studio 2022 or VS Code (optional)

### 1. Clone
```bash
git clone https://github.com/AshokRana1977/MindTecITSolutions.git
cd MindTecITSolutions
```

### 2. Configure settings (not stored in git)
The tracked `appsettings.json` contains only a placeholder connection string. Create `appsettings.Development.json` next to it (it is git-ignored) with your own values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS;Database=MPS_FM;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "User": "sender@example.com",
    "From": "sender@example.com",
    "To": "enquiries@example.com",
    "Cc": ""
  }
}
```

Set the SMTP password outside the file:
```bash
dotnet user-secrets set "Smtp:Password" "<password>"
```

### 3. Prepare the database
Create the database, then run the app once so `EnsureCreated()` builds the user and transaction tables. Create the remaining tables manually, for example by running `VendorTables.sql`. The login page reads `AppUsers` and `UserTypes`, so check the real table layout before touching user or login code.

### 4. Run
```bash
dotnet run
```
The default local URLs are `http://localhost:7171` and `https://localhost:7172` (see `Properties/launchSettings.json`).

## Configuration reference

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `Smtp:Host`, `Port`, `EnableSsl` | Mail server |
| `Smtp:User`, `Smtp:Password` | Mail login (password via user-secrets or the `Smtp__Password` environment variable) |
| `Smtp:From` | Sender address (defaults to `Smtp:User`); must be accepted by the mail server |
| `Smtp:To`, `Smtp:Cc` | Enquiry recipients, semicolon-separated |
| `AllowedHosts` | Allowed host names, separated by **semicolons** (for example `mindtec.ca;www.mindtec.ca`) |

## Deployment

The site is published from Visual Studio to SmarterASP.NET with a Web Deploy or FTP profile. Publish profiles and production settings are git-ignored. Keep a copy of `appsettings.Production.json` (live connection string, SMTP, `AllowedHosts`) and put it in the project before publishing.

If the site returns HTTP 500.34 or 502.5 after publishing, check these first:
- The hosting model in the csproj (`AspNetCoreHostingModel`) matches the app pool.
- The production connection string has real credentials.
- `stdoutLogEnabled="true"` in the server's `web.config` shows the startup error.

## Security notes

- Never commit connection strings, SMTP passwords or publish profiles. They are git-ignored on purpose.
- `AppDbContext` seeds a default admin account for first-time setup. Change that password immediately in any real environment.

## License

Released under the [MIT License](LICENSE).
