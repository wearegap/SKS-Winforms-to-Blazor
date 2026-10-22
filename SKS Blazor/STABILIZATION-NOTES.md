# SKS Blazor stabilization notes

Running log of non-trivial fixes. The legacy WinForms app in `SKS/SKSWinForms-master/Upgraded` is the behavioral spec.
Each entry: **Error** (what was wrong), **Fix** (what changed), **Why** (legacy evidence or reasoning).

## Phase 0 — Repo hygiene

- **Error:** 102 build-output files (bin/, obj/, .vs/) were tracked; the app pointed at a stale absolute path to the legacy `Orders.db`.
- **Fix:** .NET `.gitignore` at repo root; build output untracked; app now owns a tracked working copy at `SKS Blazor/SKS Blazor/App_Data/Orders.db` with a relative connection string.
- **Why:** reviewable diffs, and a database that can be reset after manual testing:
  `git checkout -- "SKS Blazor/SKS Blazor/App_Data/Orders.db"` (stop the app first; SQLite connections are pooled).

## Phase 1 — Compiles

Starting point: 11 declaration-level errors hiding a second layer (pages calling service methods that
interfaces never declared, services using DTO members that were never generated). Ended at 0 errors, 2/2 tests.

- **UsersManage outside the project.** The page sat in `SKS Blazor/Pages/Upgraded/`, outside the csproj folder, so it was
  never compiled. Moved to `SKS Blazor/SKS Blazor/Pages/Users/`, namespace `SKS_Blazor.Pages.Users`. Route unchanged.
- **ProvidersService did not exist.** DI registered it and the page called six methods on an interface that declared two
  (with `async` on interface members). New interface matches the page; new service ports `frmProviders` Recordset operations.
  Search is `ProviderName LIKE 'value%'` (legacy `SearchCriteria`). Delete removes `ProductsByProvider` rows and refuses when
  `OrderReceptions` reference the provider, because `Orders.db` has no FK constraints.
- **Customers page called five missing methods.** Added `GetAll/Search/Create/Update/DeleteCustomerAsync` as wrappers over
  the existing `SaveAsync`/`DeleteAsync`. The legacy field whitelist is now one helper (`FilterByField`) shared with
  `SearchCriteriaProductsAsync`.
- **Approval filter DTOs missing; pages re-filtered in memory.** Created `ReceptionApprovalFilterDto` and
  `RequestApprovalFilterDto`. The pages now build the filter and use the service result as the single search path; the
  page-side copies left Supplier/Contact blank and skipped the name filters. Grid `Price` and `Rows` fixed from `string`.
- **ActionOrderReception page called four methods that did not exist.** The service's `LoadDataAsync/ApproveAsync/CancelAsync`
  already held the legacy logic, so the page now calls them. Added the 21 state members the service writes.
  `ApproveAsync` gets `AppState.UserId` (was `AppState.CurrentUser ?? "system"`, which never fell back).
- **ActionOrderRequest always loaded order 0.** The service read the order id from `CurrentIdCustomer`, which nobody set.
  Added `OrderID` to the state; the page sets it before loading.
- **Dialog-hosted pages.** `ActionOrderReception.OrderId/ActionCode` had only `[SupplyParameterFromQuery]`, so
  `DialogParameters` could not set them. Added `[Parameter]` (allowed alongside it on .NET 8+ per ASP.NET Core docs).
  `AddProductToDialog` now declares and forwards `Id/ObjectReferred/Table/ColumnName`; `AddProductTo` builds the service
  context from them (the service bailed out with "called without Context" before).
- **AddProductToService bad renames.** `context.Categories`/`context.InvoiceDetails` were generator renames of
  `context.Table`/`context.Id`.
- **MudBlazor 8 API drift.** `IsEditable` → `Editable` on data-grid columns (16; unknown column attributes throw at render);
  `CellClick`/`DataGridCellClickEventArgs` → `RowClick`/`DataGridRowClickEventArgs`; `MudDialogContent/MudDialogActions` →
  `MudDialog` + `DialogContent/DialogActions`; `IMudDialogInstance.CloseAsync` → `Close`; `OnFocus` on `MudTextField` is not a
  parameter (analyzer MUD0002) → `@onfocus`, which MudBlazor forwards to the input; `using MudBlazor.Services` for `AddMudServices`.
- **`goto retry` over a using declaration** (CS8648) in `OrderReceptionService.SaveAsync` → `for` retry loop.
- **LINQ join key types** `int?` vs `int` (CS1941) in four joins → cast the non-nullable side to `int?`.
- **`optionsBuilder.SetCommandTimeout`** does not exist on `DbContextOptionsBuilder`; removed from `OnConfiguring`. The
  120 s timeout moves to the provider options in Phase 2.

## Phase 2 — Runs on SQLite with real data

- **Wrong provider.** EF Core SqlServer was wired against a SQLite file. Now `Microsoft.EntityFrameworkCore.Sqlite`
  10.0.12, with a relative `Data Source` resolved against the content root and `Mode=ReadWrite`, so a missing file fails
  at startup instead of silently creating an empty database. Command timeout 120 s is a provider option.
- **Security patch.** 10.0.0 pulls `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (GHSA-2m69-gcr7-jv3q, high). 10.0.12 resolves 2.1.12;
  `dotnet list package --vulnerable` is clean.
- **decimal on SQLite.** The legacy schema declares money/quantity columns REAL; the entities use `decimal?`. Measured on
  EF Core 10 without a converter: the store type is TEXT and comparisons/ordering go through `ef_compare()` and the
  `EF_DECIMAL` collation (the published limitations page, which says they need client evaluation, is out of date).
  A model-wide `decimal → double` convention gives native `> 0.0` / `ORDER BY` SQL and REAL parameters. Pinned by
  `UpgradedDbContextSqliteTests`.
- **Dates.** TEXT columns hold epoch milliseconds (seed rows), `MM/dd/yyyy` and `M/d/yyyy`; the port also wrote
  `yyyy-MM-dd`. `Infrastructure/Helpers/LegacyDate` is now the single boundary: all writers use `MM/dd/yyyy` invariant
  (what the legacy app wrote last), and filters parse in memory. The legacy app compared these strings in SQL, which
  cannot order mixed shapes; the approval searches now implement the evident intent. The DB data is left as-is.
- **Case-insensitive search.** `.Contains()` translates to `instr()` (case-sensitive); the legacy SQL used `LIKE '%x%'`.
  The approval services now use `EF.Functions.Like`. Other screens: Phase 4.
- **Health check** now also requires rows in `Users`, so a wrong file reports Unhealthy.
- **Verified:** `/health` Healthy; Customers (5), Providers (3), Request approval (17, dates normalised),
  Reception approval (11) render from SQLite; tests 24/24; `Orders.db` integrity ok and unchanged.

## Phase 3 — Login and user context

- **Login could never succeed.** `ILoginService.OkAsync` always returned failure; the working lookup sat on a method the
  interface did not expose, the dialog was never opened, and nothing populated `AppState`. `OkAsync` now ports
  `frmLogin.cmdOk_Click` into `LoginStateDto` (UserId, Fullname, Level, admin flag, legacy status and error text).
  The duplicate, unused `Infrastructure/Security/SecurityService` is deleted.
- **Decision: real login screen.** The legacy build hard-codes auto-login as `acantillo` in `modMain.Main()`;
  `.modernizer/decisions.md` chose a login entry point, so this port has one. New `/login` page hosts `LoginDialog` as an
  undismissable modal. `MainLayout` redirects any other URL to `/login?returnUrl=…` while `AppState.IsLoggedIn` is false.
  The scoped `AppState` is the session; a new circuit (first load, F5, deep link) starts logged out.
- **Prerendering off** (`render-mode="Server"`). Prerender ran every page's init twice in a throw-away scope.
- **User id reaches the writes.** Pages passed `this` where services read `eventSender as string` for the user
  (ActionOrderRequest approve/cancel, Add/Adjust stock), so writes stored "" or "SYSTEM". Order request/reception headers
  now store `EmployeeID`/`ReceivedBy` from `AppState.UserId`. UsersManage checked `CurrentUser == "admin"` (never true);
  it now uses `CurrentUserAdmin`.
- **Layout.** Status bar repaints on `AppState.OnChange` and shows the full name. `Main` and `Customers` assigned
  `AppState.OnCommand` with `=`, clobbering other subscribers; now `+=`/`-=`.
- **Legacy approve is buggy; the port keeps the evident intent.** `frmActionOrderReception.cmdApprove_Click` inserts each
  stock row twice (loop + bulk insert) and writes the product id into StockLog's DocID/DocType/Quantity/StockID (the two
  stray legacy rows with DocType `HRS9`/`SH22` came from it). The port inserts one Stocks + one StockLog row per line with
  `DocType='OrderReception'`, `DocID=<order>`. Review in Phase 4.
- **Verified in headless Edge (Playwright):** deep link → login with returnUrl; wrong password → legacy error; login →
  status "User : Allan Cantillo logged at …" and back to the requested page; F5 → login again; approving RECEIVED
  reception 7 through the grid + dialog wrote `APPROVED / acantillo / 09/29/2026` plus 2 Stocks and 2 StockLog rows;
  seller `cbastos` logs in. Tests 30/30. DB reset afterwards.
- **Run it in Development.** MudBlazor and `blazor.server.js` are static web assets, served only in Development or from
  published output. `dotnet run` uses the launch profile (Development). On .NET 10, `dotnet run --environment X` sets an
  environment *variable* named X, not the host environment; use `ASPNETCORE_ENVIRONMENT=Development` instead.
  In any other mode the pages render but never become interactive.

## Phase 4 — Silent write loss and case-sensitive search

- **Products saved and deleted nothing.** `Products.razor.cs` called the parameterless `SaveAsync()`/`DeleteAsync()`,
  whose bodies were a `LogWarning` plus `Task.CompletedTask`; the real `SaveProductAsync(dto)`/`DeleteProductAsync(id)`
  had no callers. The page then reported `"Product saved successfully."` and requeried, so the edit visibly reverted
  and nothing said why. The page now builds a `ProductSaveDto` (`IsNew` from `_state.NewMode`) and passes the id to
  delete. **Both no-op overloads are deleted from `IProductsService` and `ProductsService`** rather than left in place —
  an empty method that satisfies a call site is how this survived three phases. Products was the only screen affected;
  Customers, Providers and UsersManage already called persisting overloads.
- **Case-sensitive search on four screens.** `.Contains()` translates to SQLite `instr()`, which is case-sensitive, so
  typing `ikura` found nothing. Measured on EF Core 10: `.StartsWith()` emits `LIKE @p ESCAPE '\'` (case-insensitive for
  ASCII) but `.Contains()` emits `instr(col, @p) > 0`. Now `EF.Functions.Like(col, "%" + term + "%")`, matching the
  idiom `ProvidersService` already used, in `AdjustStockManualService` (code, name), `AddProductToService` (code, name),
  `OrderReceptionService` (provider, contact first/last) and `OrderRequestService` (company, contact first/last).
  Collection-membership `.Contains()` calls are untouched. Customers was never affected — it uses `StartsWith`.
  Side effect, as on the legacy screens that used SQL LIKE: a typed `%` or `_` now acts as a wildcard.
- **Regression tests.** `ProductsPersistenceTests` asserts update/insert/duplicate-reject/delete against the database,
  so a no-op implementation fails instead of passing silently; `CaseInsensitiveSearchTests` drives each search with
  mixed case and a mid-string term. Tests 43/44.
- **Known failure, not a code defect:** `RealDatabase_HasExpectedRowCounts` expects 50 `Stocks` and finds 52. The
  working copy of `Orders.db` still carries the Phase 3 manual approval of reception 7 (APPROVED / acantillo, +2 Stocks,
  +2 StockLog). Reset with `git checkout -- "SKS Blazor/SKS Blazor/App_Data/Orders.db"` (stop the app first).

### Still open

- **No authentication or authorization framework.** No `[Authorize]`, no `UseAuthentication`/`UseAuthorization`, no
  `AuthenticationState`. The gate is a `MainLayout` redirect on `AppState.IsLoggedIn` plus one admin check in
  `UsersManage.razor.cs`. Passwords are plaintext in `Users`, compared literally. Fine for a reference port, not for
  anything exposed.
- **Approval date filters materialise the whole joined set** before filtering in memory — correct given three date
  shapes in a TEXT column, but it will not scale past the sample data.
- **`unconsumed-plan-data.txt` is stale.** `ProviderCompany`, `ChangedByLabel`, `ChangedDateLabel` and
  `ShowChangedFields` are wired up now. `ApproveEnabled`/`CancelEnabled` remain unused, which is harmless: the legacy
  hardcodes `cmdApprove.Enabled = true` and `ActionOrderReceptionService` rejects re-approval server-side.

## Phase 4b — Create Order layout matched to the legacy form

- **Error:** `OrderRequest.razor` laid the screen out as two 50% columns (customer search left, products
  and totals right). `frmOrderRequest` is a single 523x703 column: Search customer → Customer → dates →
  product grid → totals → Save/Close. Nothing lined up with the form it ports.
- **Fix:** single column in legacy order, with the legacy geometry driving the CSS grid tracks — labels in
  the gutter left of each field, fields at their legacy widths, totals in the legacy two-column arrangement
  (Line quantity / Sales Tax / Freight Charge / Total on the left at x=8, Total Tax / Sub Total on the right
  at x=272). Label gutters are fixed at the legacy offsets scaled ~1.15, because Roboto needs more room than
  the legacy 8.25pt font; field tracks flex so the form fills the page width, keeping the legacy width ratios
  as `fr` weights (Contact 209 against Company 145 in the Customer frame). Grid headers use the legacy
  captions; above ~1500px both grids show all 7 and 8 columns, and below that they scroll horizontally
  rather than squeezing, which is what the legacy did.
  Added the "Line quantity:" field (`txtEntry`, legacy 8,544) that the port had dropped — legacy keeps it
  disabled except while editing a quantity cell, and the port edits quantity in the grid, so it mirrors
  `TxtEntryText` read-only. `Save`/`Close` keep their legacy casing instead of MudBlazor's uppercase.
- **Verified** in headless Edge, logged in, measuring `scrollWidth` vs `clientWidth` on every label and on
  both grid containers: no truncated labels at 1920px or 1000px; at 1920px both grids fit entirely
  (`scrollWidth == clientWidth`), at 1000px they fall back to horizontal scroll.
- **Not matched, deliberately:** vertical pitch. Legacy rows are 24px apart with 20px-tall controls;
  MudBlazor dense fields are ~40px, so rows stack in legacy order at their natural height. The legacy
  per-form status bar is not duplicated — `MainLayout` already renders one. The empty date pickers are the
  one visible difference from the screenshot: the legacy never assigns `dtRequired`/`dtPromised`, so what it
  shows is the WinForms `DateTimePicker` default of today, not data.

## Phase 4c — Navigation matched to the frmMain menu

- **Error:** the shell carried two navigation surfaces the legacy app does not have — a top menu bar
  (File / Stock / Master Data / Approvals / Security / Help) and an icon toolbar (Home, Refresh, Search,
  Logout) — plus a left nav with invented groupings ("Master Data", "Administration"). None of the three
  matched `frmMain`, which has exactly one menu and a status bar.
- **Fix:** both top bars removed; `NavMenu` now mirrors the `frmMain` menu one-to-one — File (Manage
  Customers, Manage Suppliers, Exit), Orders (Create Order, Create Invoice, separator, Add Stock Order,
  Add Stock to Inventory), Inventory (Inventory Update, Inventory Adjust), Maintenance (Manage Products,
  Manage Users), Help (About). The sidebar gets the `frmMain` window caption, "Sales Agent".
  `MainLayout` keeps only the login guard, sidebar and status bar; its now-dead `NavigateTo`/`Refresh`/
  `ShowAbout`/`Exit` and the `IDialogService` injection are gone, with Exit and About moving to `NavMenu`.
- **Captions do not describe their targets, and that is faithful.** In `frmMain.cs` the handler names
  diverge from the menu text: `mnuOrderRequestsApproval` is captioned "Create Invoice" and opens
  `frmRequestApproval`; `mnuOrderReceptionsApproval` is "Add Stock to Inventory" and opens
  `frmReceptionApproval`; `mnuAddStockManually` is "Inventory Update". **Map menu items by their
  `mnu*_Click` handler, never by the caption** — matching on caption is how an earlier pass in this repo
  concluded, wrongly, that "Create Invoice" and "Inventory Update" were dead items.
- **Dropped, having no `frmMain` equivalent:** Home, Refresh and Search. `/upgraded/search` still routes,
  but nothing links to it — the legacy opens `frmSearch` from other forms, not from the main menu. The
  `AppState.SendCommandAsync("Refresh")` broadcast now has no sender.
- **Verified** in headless Edge: all 10 `Href` items land on the expected route, About opens its dialog,
  Exit returns to `/login`. Without an `Href`, `MudNavLink` renders a bare `div`; `tabindex="0"` makes
  Exit and About keyboard-focusable, but MudBlazor strips a `role` attribute, so they remain divs to
  assistive tech.

## Phase 4d — Manage Products: legacy field order, and a grid view

- **Error (layout):** the detail form was a responsive `MudGrid` of 14 equal cells in generator order
  (Product ID, Name, Description, Category, Unit Price, Units In Stock, Units On Order, Qty Per Unit,
  Unit, Reorder Level, Serial Number, Lead Time, Supplier ID, Discontinued). `frmProducts` Frame1
  "Product information" is a fixed single column: Product, Name, Desc, Category, Serial number,
  Discontinued, Unit price, then Qty per unit with Unit beside it.
- **Fix:** labels and order now follow the designer, in a label-gutter grid (labels at x=16, fields at
  x=104) with the legacy field widths kept as ratios — Product 113, Name 193, Desc 305, Category 121,
  Serial 121, Unit price 105, Qty per unit 105, Unit 105, scaled for Roboto. The five columns the legacy
  form never shows (Units in stock, Units on order, Reorder level, Lead time, Supplier ID) are **not
  rendered at all** — the screen now shows exactly the legacy fields. They are still carried: the page
  builds `ProductSaveDto` from `_currentProduct`, which `CloneProduct` fills from the loaded row, so a
  save round-trips them untouched. Verified end to end — renamed IK1 through the UI, saved, and read the
  database back: `UnitsInStock` 99 and `UnitsOnOrder` 59 were preserved. They are no longer editable
  anywhere in the app, which is the trade for matching the legacy form.
- **Error (pre-existing):** `MudDivider` renders `<hr>` with `flex: 1 0 auto`. Inside this page's
  `display:flex; flex-direction:column` paper each divider *grew*, taking 271px, which is why the
  toolbar, navigator and form were separated by two large empty bands. Fixed with `Style="flex:0 0 auto"`
  on both. **Any MudDivider placed directly in a flex column needs this** — worth checking other pages.
- **New: "View as Grid" toolbar button** (no legacy equivalent — `frmProducts` only ever shows one record).
  The Product information panel stays put; the grid appears **below it**, in the slot the additional-fields
  panel used to occupy, and the `dcProducts` navigator is hidden (the grid is how you pick a record there).
  Same columns in the same order, Category resolved to its name. Clicking a row loads that record into the
  panel above. The toggle refuses to leave the form while `EditMode`/`NewMode` is set, because the grid has
  no Save and the pending edit would be lost.
- **Verified** in headless Edge: the form renders exactly nine labels — Product / Name / Desc / Category /
  Serial number / Discontinued / Unit price / Qty per unit / Unit; form view shows the navigator and no
  grid, grid view keeps the panel, shows 4 rows and hides the navigator, and back again; clicking row 3
  then switching to the form shows SH22; the edit guard blocks the switch after Edit; grid header cells
  are a uniform 39px (no wrap); and an edit saved through the UI preserved the five hidden columns.
- **Note:** Cancel still ports `this.Close()` and navigates to "/", so it leaves the page entirely.

## Phase 4e — Manage Customers and Manage Suppliers: same treatment as Products

- **Error:** both screens were generator layouts — a permanent side-by-side (Customers: 45% grid / 55%
  form) or a grid stacked above the form (Suppliers), with fields in DTO order and no record navigator.
  `frmCustomers` and `frmProviders` are single-record forms with a `dc*` data control at the bottom and a
  nested "Contact" frame at (224,14) inside the main frame.
- **Fix, both pages:** fields reordered to the designer, in a label-gutter grid (labels x=16, fields x=104)
  with the legacy widths as ratios, and the "Contact" frame rendered beside the first rows as in the legacy.
  - Customers, Frame1 "Customer information": Company / Department / Email (Contact frame alongside),
    Address (313 wide), Zip, City+Phone, State/Prov+Ext, Country+Fax, Notes (401x52).
  - Suppliers, Frame1 "Supplier information": Name / Email / Zip code (Contact frame alongside),
    City+Phone, State/Prov+Ext, Country+Fax, Notes (401x52), Payment Terms (401x52).
  Both gained the `dc*` record navigator (First/Prev/Next/Last plus "Record n of m") the port had dropped.
- **"View as Grid"** added to both toolbars, same contract as Products: the information panel stays, the
  grid appears below it, the navigator is hidden, clicking a row loads that record into the panel, and the
  toggle refuses to switch while `EditMode`/`NewMode` is set.
- **Suppliers no longer renders Address.** `frmProviders` has no Address control — the port invented one.
  It is still carried: the page passes `_currentProvider` straight to the service, which maps
  `entity.Address = dto.Address` from a DTO loaded out of the row. Verified by seeding
  `Providers.Address = 'RT-PROBE-123'` on provider 2, renaming through the UI, saving, and reading back —
  the address survived. Seeded value and name restored afterwards.
- **Layout bug caught in review:** in the Customers two-column grid, Address spans columns 2-5, so the
  three filler `<span>`s after it wrapped onto the next row and shifted every row below by three cells —
  Zip and City rendered in the right-hand column and Phone overlapped State/Prov. A row that spans the
  full width takes no fillers.
- **Verified** in headless Edge for both pages: label order matches the designer; form view shows the
  navigator and no grid; grid view hides the navigator and lists all records (5 customers, 3 suppliers);
  clicking a row loads it into the panel.
