# Semantic findings — SKS Blazor

Code that compiles, reads correctly, and is wrong about something the source states.
None of these is a compile error; each fails at runtime, usually by showing nothing.

**9 finding(s)** in 2 of 169 file(s) scanned.

> No seed data was read, so the identifier checks did not run. Pass the legacy
> source folder to enable them.

## By rule

| Rule | Findings | Files |
|---|---|---|
| `discarded-result` | 2 | 2 |

## By file

### SKS Blazor\Application\Services\Login\LoginService.cs

- **[discarded-result]** 'db' from _dbFactory.CreateDbContextAsync(…) is assigned and never read.
  - *Evidence:* The call runs and its answer is thrown away, so whatever depended on it is working from nothing — typically an empty collection put in its place.
  - *Fix:* Use the result, or remove the call. If the value it returns is what the screen should show, assign that rather than a new empty collection.

### SKS Blazor\Pages\Request\RequestApproval.razor.cs

- **[discarded-result]** 'found' from CustomersService.SearchCriteriaProductsAsync(…) is assigned and never read.
  - *Evidence:* The call runs and its answer is thrown away, so whatever depended on it is working from nothing — typically an empty collection put in its place.
  - *Fix:* Use the result, or remove the call. If the value it returns is what the screen should show, assign that rather than a new empty collection.

## Whole project

Findings that no single file can show, so they are known only once every slice has run.

- `SaveOrderReceptionResultDto.ErrorMessage` is written by generated code and read by nothing (written in 9 place(s), read in none) — the work that produces it never reaches anyone.
- `SaveOrderRequestResultDto.ErrorMessage` is written by generated code and read by nothing (written in 9 place(s), read in none) — the work that produces it never reaches anyone.
- `ActionOrderReceptionHeaderDto.ChangedDateLabel` is written by generated code and read by nothing (written in 4 place(s), read in none) — the work that produces it never reaches anyone.
- `ActionOrderReceptionHeaderDto.ChangedByLabel` is written by generated code and read by nothing (written in 4 place(s), read in none) — the work that produces it never reaches anyone.
- `ActionOrderReceptionHeaderDto.IsReceived` is written by generated code and read by nothing (written in 3 place(s), read in none) — the work that produces it never reaches anyone.
- `ActionOrderReceptionHeaderDto.ShowChangedFields` is written by generated code and read by nothing (written in 3 place(s), read in none) — the work that produces it never reaches anyone.
- `UsersManageStateDto.CboLevelSelectedIndex` is written by generated code and read by nothing (written in 3 place(s), read in none) — the work that produces it never reaches anyone.

