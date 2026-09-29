# Architecture Decisions

## Database Strategy
- **Mode**: Existing Database
- Map to existing database — NO migrations, NO DataSeeder
- Use `SetCommandTimeout(120)` instead of `EnsureCreated`/`Migrate`
- Entity class names MUST match table names exactly
- Property names MUST match column names exactly

## Target Module
- Module: Upgraded (22 forms)
- Total lines: 15,405
- Source language: WinForms

## Library Choices
- System.Windows.Forms.Label → MudText
- System.Windows.Forms.TextBox → MudTextField
- System.Windows.Forms.ColumnHeader → MudDataGrid PropertyColumn
- System.Windows.Forms.Button → MudButton
- System.Windows.Forms.ToolTip → MudTooltip
- System.Windows.Forms.GroupBox → MudPaper
- System.Windows.Forms.ToolStripButton → MudButton
- System.Windows.Forms.ToolStripMenuItem → MudMenuItem
- System.Windows.Forms.ToolStripStatusLabel → MudText
- System.Windows.Forms.StatusStrip → MudAppBar (bottom)
- System.Windows.Forms.ListView → MudDataGrid / MudTable
- System.Windows.Forms.PictureBox → MudImage
- UpgradeHelpers.DataGridViewFlex → MudDataGrid
- System.Windows.Forms.CheckBox → MudCheckBox
- System.Windows.Forms.DateTimePicker → MudDatePicker
- System.Windows.Forms.ComboBox → MudSelect
- UpgradeHelpers.DB.ADO.ADODataControlHelper → MudToolBar
- System.Windows.Forms.ImageList → MudImage
- System.Windows.Forms.ToolStrip → MudToolBar
- System.Windows.Forms.MenuStrip → MudMenu
- System.Windows.Forms.ToolStripSeparator → MudDivider
- System.Windows.Forms.Timer → System.Threading.Timer / PeriodicTimer
- dep:MSChartLib → MudChart

## Architectural Answers
- **In frmAddProductTo, what data do the three OrderedDictionary fields (productsStored, productsToDelete, productsToAdd) actually contain—product IDs, full product objects, or product rows with quantities?**: Each holds complete Product objects with all properties (id, name, category, price, stock)
- **Is frmLogin meant to be the entry point to the application (shown before frmMain), or is it a modal dialog opened from within frmMain for re-authentication?**: It is the primary entry point; redirect unauthenticated users to /login before frmMain loads
- **In frmAddProductTo, where does lvProductsBy get populated? Is it loaded from a database query on form activation, passed in from a parent screen, or pre-populated by user action on another screen?**: It is populated only when the user clicks a button (e.g., cmdProducts_Click); it starts empty
- **In frmReceptionApproval and frmRequestApproval, where do the unfed grids (fgOrders) load their rows? Are they queried on date-filter change (dtFrom_ValueChanged, dtTo_ValueChanged), or should they be pre-loaded on page init?**: Load fgOrders once on page init with default dates; allow user to manually trigger a refresh
- **How should the MDI (Multiple Document Interface) pattern from frmMain be adapted for Blazor? Should all forms become separate routes with a persistent nav sidebar/tab bar, or should they be hosted as modal overlays/dialogs within a single dashboard page?**: Route-based: each form becomes a page (/products, /customers, /orders, etc.) with a persistent sidebar menu
- **The legacy app uses modConnection (ExecuteSql, ExecuteSql2) for raw SQL. Should the Blazor backend consolidate all database access into EF Core DbContext methods, or do certain screens require raw SQL that should be preserved via context.Database.ExecuteSqlInterpolated()?**: Convert all queries to EF Core/LINQ DbContext methods; no raw SQL in the Blazor backend
- **Do you have any reporting or export requirements (e.g., print orders, export grids to CSV/PDF)? The legacy app references MSChartLib, but no active charts were detected in the forms—was charting ever used, and should it be ported to Blazor?**: No charting or reporting needed; drop MSChartLib dependency entirely

## Conventions
- Entity class names: PascalCase of table name (singular)
- Property names: PascalCase of column name
- Page file names: PascalCase of legacy form name + .razor
- Routes: kebab-case under /upgraded/
- Service method names: verb+entity idiom with Async suffix
- DI scope: Scoped for all services (Blazor Server)
