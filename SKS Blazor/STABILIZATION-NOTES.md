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
