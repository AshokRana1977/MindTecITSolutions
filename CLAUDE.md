# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

MindtecFinance: an ASP.NET Core 8 Razor Pages app (single project, `MindtecFinance.csproj` / `.sln`) backed by SQL Server. It is an internal finance/operations dashboard covering clients, vendors, financials, tickets, users and configuration. There are no tests and no linter configured.

## Commands

- Run: `dotnet run` (profiles in `Properties/launchSettings.json`)
- Build: `dotnet build`
- EF tooling is pinned as a local tool (`.config/dotnet-tools.json`, dotnet-ef 10.0.5): `dotnet tool restore`. There are no migrations; see below.

## Architecture

**Data access is mixed and mostly bypasses EF.** `Data/AppDbContext.cs` only declares `AppUsers` and `Transactions`. Nearly every page model (`Pages/Dashboard/*.cshtml.cs`, `Pages/Login.cshtml.cs`) injects `IConfiguration` and runs raw ADO.NET (`SqlConnection`/`SqlCommand`) against the `DefaultConnection` string. Most tables (vendors, clients, tickets, config types and so on) are not in the EF model. When adding a feature, follow the surrounding page's ADO.NET pattern. Models in `Models/` (`VendorModels.cs`, `ClientModels.cs`, `Ticket.cs`, ...) are plain DTOs.

**Schema is not managed by migrations.** `Program.cs` calls `Database.EnsureCreated()` at startup, which only creates the EF-modelled tables when the database is empty. Other tables are created by hand; `VendorTables.sql` is one example (`VendorMaster`, `VendorContact`, `VendorWebsite`). Schema changes to non-EF tables must be applied to SQL Server manually.

**Known schema drift.** `Login.cshtml.cs` authenticates against `AppUsers.PasswordHash` (SHA-256 `VARBINARY(32)`) joined to a `UserTypes` table (`TypeName` becomes the role claim). `Models/AppUser.cs` and the `AppDbContext` seed use a plaintext `Password` column and `UserTypeId`. The live DB schema therefore differs from the EF model, so check the real table before touching user or login code.

**Auth.** Cookie authentication (`/Login`). The user's `TypeName` is stored as the `ClaimTypes.Role` claim. Dashboard pages carry `[Authorize]`; `ConfigTypeManagement` and `UserTypeManagement` require `Roles = "SA, Admin"`.

**Pages.** `Pages/Dashboard/*` uses `_DashboardLayout.cshtml`; the public pages (`Index`, `Login`) use `_Layout.cshtml`. Pages commonly expose AJAX handlers as named handlers (e.g. `OnGetDetailsAsync` in `Vendors`) returning JSON/partials.

## Configuration

`appsettings.json` holds the `DefaultConnection` string, with several environments (local, hosted demo, live) swapped by commenting lines in and out. The active line points at a local SQL Express instance. The file contains credentials for hosted databases. Do not copy them elsewhere, and do not commit changes that switch the active connection to a live DB.
