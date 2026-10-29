# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Solution file is `Bookshelf.slnx` (two projects under `Bookshelf/`). There is no test project yet.

- Build: `dotnet build Bookshelf.slnx`
- Run (from repo root): `dotnet run --project Bookshelf/Bookshelf --launch-profile https` → https://localhost:7287 (http profile: http://localhost:5002)
- Add migration: `dotnet ef migrations add <Name> --project Bookshelf/Bookshelf`
- Apply migrations: `dotnet ef database update --project Bookshelf/Bookshelf`

The app needs a PostgreSQL connection string named `Bookshelf`. It isn't in appsettings, so set it in user secrets (the server project has a `UserSecretsId`):
`dotnet user-secrets set "ConnectionStrings:Bookshelf" "<conn>" --project Bookshelf/Bookshelf`

## Architecture

Blazor Web App on .NET 10, using both Interactive Server and Interactive WebAssembly render modes.

- **`Bookshelf/Bookshelf`** (server host): `Program.cs` wires everything up: Razor components, ASP.NET Core Identity (`AddIdentityCore<AppUser>` plus cookies, not the full Identity UI), EF Core with Npgsql, MudBlazor, and the minimal API endpoints. `Components/` contains the root `App.razor`, `Routes.razor` (`AuthorizeRouteView`, which also discovers routes in the client assembly) and `MainLayout.razor` (MudBlazor layout, dark mode).
- **`Bookshelf/Bookshelf.Client`** (WASM): pages that run in the browser (e.g. `Pages/Login.razor`, `Pages/Register.razor`, set to `InteractiveWebAssemblyRenderMode(prerender: false)`). `Authentication/` holds the request DTOs (`RegisterUserRequest`, `LoginUserRequest`) with DataAnnotations validation. The server references these too, so the DTOs shared by client and server live here.

### Auth flow
- Client pages call JSON endpoints under `/api/auth/*` (`register`, `login`, `logout`), defined in `Bookshelf/Bookshelf/Endpoints/AuthenticationEndpoints.cs` and mapped through `app.MapAuthEndpoints()`. After a successful login the client does a full page reload (`forceLoad: true`) so the new cookie takes effect.
- The server serializes auth state to the WASM client: `AddAuthenticationStateSerialization(SerializeAllClaims = true)` on the server, `AddAuthenticationStateDeserialization()` on the client.
- `AppClaimsPrincipalFactory` adds a `GivenName` claim from `AppUser.FirstName`. Note: this file is in the `Authentication/` folder but its namespace is `Bookshelf.Auth`.
- For `/api` paths, the cookie's redirect-to-login returns a 401 instead of redirecting, and the API skips the HTML error and status-code pages so it returns ProblemDetails. Keep new API endpoints under `/api` to get this behaviour.
- Identity rules: password length ≥ 10, unique email, restricted username characters. The client DTO attributes repeat these rules, so change both sides together.

### Data
`Data/AppDbContext.cs` defines `AppUser : IdentityUser` (adds `FirstName`) and `AppDbContext : IdentityDbContext<AppUser>`. Migrations are in `Bookshelf/Bookshelf/Migrations`.

### UI
The UI uses MudBlazor (registered with `AddMudServices()` in both projects; CSS/JS included in `App.razor`). Bootstrap and the template's NavMenu have been removed, so use MudBlazor components rather than Bootstrap classes.
