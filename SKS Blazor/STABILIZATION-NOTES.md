# SKS Blazor stabilization notes

Running log of non-trivial fixes. The legacy WinForms app in `SKS/SKSWinForms-master/Upgraded` is the behavioral spec.
Each entry: **Error** (what was wrong), **Fix** (what changed), **Why** (legacy evidence or reasoning).

## Phase 0 — Repo hygiene

- **Error:** 102 build-output files (bin/, obj/, .vs/) were tracked; the app pointed at a stale absolute path to the legacy `Orders.db`.
- **Fix:** .NET `.gitignore` at repo root; build output untracked; app now owns a tracked working copy at `SKS Blazor/SKS Blazor/App_Data/Orders.db` with a relative connection string.
- **Why:** reviewable diffs, and a database that can be reset after manual testing:
  `git checkout -- "SKS Blazor/SKS Blazor/App_Data/Orders.db"` (stop the app first; SQLite connections are pooled).
