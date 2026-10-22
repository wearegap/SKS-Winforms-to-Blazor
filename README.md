# SKS — WinForms to Blazor modernization

Salmon King Seafood (SKS) is a small order and inventory application. This repository holds **two
versions of it side by side**: the legacy Windows desktop app, and a modernized web app ported from it.

The point of keeping both is that the legacy app is the *specification*. When the web app's behaviour is
in question, the answer is whatever the WinForms code does — including its quirks.

| | Legacy | Modernized |
|---|---|---|
| Folder | [`SKS/`](SKS/) | [`SKS Blazor/`](SKS%20Blazor/) |
| Stack | WinForms, .NET Framework 4.8, x86 | Blazor Server, .NET 10, MudBlazor 8 |
| Data access | ADO via the SQLite3 ODBC driver | EF Core 10 (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Database | `Orders.db` (SQLite) | its own copy of the same `Orders.db` |
| Entry point | `SKS.exe` | `https://localhost:64938` |

SKS started as a VB6 application. It was converted to C# WinForms with Mobilize.Net's Visual Basic
Upgrade Companion (VBUC) — that conversion is what lives in `SKS/`, and it keeps its original
[README](SKS/SKSWinForms-master/README.md). The Blazor app is the next step: a port of those forms to the
web, screen by screen.

## The two apps

### Legacy — `SKS/SKSWinForms-master/Upgraded/`

The VBUC output, essentially untouched. One form per screen (`frmProducts`, `frmCustomers`,
`frmOrderRequest`, …), each with a toolbar and a record-at-a-time data control. Worth knowing before you
read it:

- **It never shows the login form.** `frmLogin` is compiled but called from nowhere;
  [`modMain.Main()`](SKS/SKSWinForms-master/Upgraded/modMain.cs) hardcodes the session as
  `acantillo` / Administrator and opens the main window directly.
- **Menu captions do not match what they open.** "Create Invoice" opens the order *request approval*
  form; "Add Stock to Inventory" opens *reception approval*; "Inventory Update" opens *add stock*. Map
  menu items by their `mnu*_Click` handler, never by the caption.
- The connection string is hardcoded in `modMain.Main()`, and the SQL is built by string concatenation.

### Modernized — `SKS Blazor/SKS Blazor/`

Blazor Server. Layered as `Pages/` → `Application/Services/` → `Infrastructure/Data/`, with one service
per legacy form and XML comments pointing back at the handler each method ports. It reads the legacy
schema as-is: no migrations, no reshaping of the database.

Differences from the legacy that are deliberate:

- **There is a real login screen.** `MainLayout` redirects to `/login` until you sign in. Session state
  is per-circuit, so a refresh signs you out.
- **Screens that only showed one record at a time now also offer "View as Grid"** — Products, Customers
  and Suppliers keep the legacy form and add a toggle that lists every record beneath it.
- Searches are case-insensitive, and dates are normalised on the way in and out (the legacy stores three
  different date shapes in the same TEXT columns).

[`SKS Blazor/STABILIZATION-NOTES.md`](SKS%20Blazor/STABILIZATION-NOTES.md) is the running log of what was
fixed and why, each entry tied to the legacy evidence. Read it before changing behaviour — several
oddities in the port are faithful ports of legacy bugs, and it says which.

## Running them

### Blazor

```bash
cd "SKS Blazor/SKS Blazor"
dotnet run
```

Then open **https://localhost:64938**. The dev certificate warning is expected; `dotnet dev-certs https
--trust` clears it.

It must run in the **Development** environment — MudBlazor and `blazor.server.js` are static web assets,
served only in Development or from published output. In any other mode the pages render but never become
interactive. `dotnet run` picks up the launch profile, which sets this. Note that on .NET 10
`dotnet run --environment X` sets an environment *variable* named `X`, not the host environment; use
`ASPNETCORE_ENVIRONMENT=Development` if you need to set it explicitly.

`GET /health` reports `Healthy` only when the database resolves *and* has rows in `Users`, so it catches
a wrong or empty file at startup.

### WinForms

Two prerequisites, both easy to miss:

1. **The 32-bit SQLite3 ODBC driver.** Download `sqliteodbc.exe` from
   [ch-werner.de/sqliteodbc](http://www.ch-werner.de/sqliteodbc/) and install it. The app targets x86, so
   the 32-bit driver is the one that matters — the 64-bit build is not used. Without it the app dies on
   startup with `ERROR [IM002] Data source name not found and no default driver specified`.
2. **Clear the mark of the web** if you downloaded this repo as a ZIP. Windows flags every extracted file,
   and MSBuild refuses to compile `.resX` files in the Internet zone (`error MSB3821`). The checked-in
   `UpgradeSupport\*.dll` references are flagged too, which breaks assembly loading even if compilation
   succeeds:

   ```powershell
   Get-ChildItem -Path . -Recurse -File |
     Where-Object { Get-Item $_.FullName -Stream Zone.Identifier -ErrorAction SilentlyContinue } |
     Unblock-File
   ```

Then build with **full MSBuild** — this is a non-SDK `net48` project, so `dotnet build` cannot build it:

```
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" ^
  "SKS\SKSWinForms-master\Upgraded\SKS.sln" -t:Rebuild -p:Configuration=Debug -p:Platform=x86
```

The output is `SKS\SKSWinForms-master\Upgraded\bin\SKS.exe`. The `MSB3284` "type library not registered"
warnings are harmless: no source file references those COM controls any more, and the build falls back to
the checked-in interop assemblies.

## Signing in

The Blazor app authenticates against the `Users` table. The legacy app skips login entirely and always
runs as Allan Cantillo.

| Username | Password | Level |
|---|---|---|
| `acantillo` | `123` | Administrator |
| `mrojas` | `mrojas` | Administrator |
| `jwelsh` | `jwelsh` | Administrator |
| `jbrown` | `jbrown` | Administrator |
| `cbastos` | `cbastos` | Seller |

Use `cbastos` to exercise the non-administrator path.

> **These are sample credentials in a reference application, stored and compared in plain text, and the
> database file is committed to this repository.** That is inherited from the original sample and is fine
> for a demo. There is also no authentication or authorization framework in the Blazor app — access
> control is a layout redirect plus one admin check. Do not put this on a network anyone else can reach
> without replacing both.

## Tests

```bash
cd "SKS Blazor"
dotnet test "SKS Blazor.sln"
```

The suite covers the database contract (column affinities, the legacy date shapes), login, approval
searches, product persistence and case-insensitive search.

Two things to know. The fixture copies `Orders.db` per test, so **stop the app before running tests** —
otherwise the copy races the running app and a test can fail spuriously. And `RealDatabase_HasExpectedRowCounts`
asserts exact row counts, so it fails once you have clicked around and changed data. Reset with:

```bash
git checkout -- "SKS Blazor/SKS Blazor/App_Data/Orders.db"
```

## Layout

```
SKS/                            Legacy WinForms app
  SKSWinForms-master/
    Upgraded/                   VBUC output — the forms and their code-behind
    UpgradeSupport/             UpgradeHelpers runtime assemblies
    Requirements/               Reference copies of Orders.db and the app configs

SKS Blazor/                     Modernized web app
  SKS Blazor/
    Pages/                      One folder per ported screen
    Application/Services/       One service per legacy form
    Infrastructure/Data/        EF Core context and entity configuration
    App_Data/Orders.db          The database the app runs against
  SKS Blazor.Tests/
  STABILIZATION-NOTES.md        Running log of fixes, with legacy evidence
```
