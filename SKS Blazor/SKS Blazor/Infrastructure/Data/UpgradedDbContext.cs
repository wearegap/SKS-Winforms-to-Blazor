using Microsoft.EntityFrameworkCore;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Invoice;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Stock;

namespace SKS_Blazor.Infrastructure.Data;

/// <summary>
/// Port of legacy `main` database (Upgraded module). EF Core database-first DbContext
/// mapping to the existing SQLite database (Orders.db) used by the WinForms application.
/// </summary>
public class UpgradedDbContext : DbContext
{
    public UpgradedDbContext(DbContextOptions<UpgradedDbContext> options) : base(options)
    {
    }

    public DbSet<Categories> Categories { get; set; } = null!;
    public DbSet<Customers> Customers { get; set; } = null!;
    public DbSet<InvoiceDetails> InvoiceDetails { get; set; } = null!;
    public DbSet<Invoices> Invoices { get; set; } = null!;
    public DbSet<Levels> Levels { get; set; } = null!;
    public DbSet<ManualStocks> ManualStocks { get; set; } = null!;
    public DbSet<OrderReceptionDetails> OrderReceptionDetails { get; set; } = null!;
    public DbSet<OrderReceptions> OrderReceptions { get; set; } = null!;
    public DbSet<OrderRequestDetails> OrderRequestDetails { get; set; } = null!;
    public DbSet<OrderRequests> OrderRequests { get; set; } = null!;
    public DbSet<Products> Products { get; set; } = null!;
    public DbSet<ProductsByCustomer> ProductsByCustomers { get; set; } = null!;
    public DbSet<ProductsByProvider> ProductsByProviders { get; set; } = null!;
    public DbSet<Providers> Providers { get; set; } = null!;
    public DbSet<StockLog> StockLogs { get; set; } = null!;
    public DbSet<Stocks> Stocks { get; set; } = null!;
    public DbSet<Users> Users { get; set; } = null!;
    public DbSet<Warehouses> Warehouses { get; set; } = null!;

    /// <summary>
    /// Orders.db stores every money/quantity column as REAL. EF Core's SQLite provider supports only
    /// equality on decimal (ordering, comparison, Min/Max need client evaluation), so map decimal and
    /// decimal? properties to double model-wide while the entities keep decimal.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categories>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryID).HasColumnName("CategoryID").ValueGeneratedOnAdd();
            e.Property(x => x.CategoryName).HasColumnName("CategoryName");
        });

        modelBuilder.Entity<Customers>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.CustomerID);
            e.Property(x => x.CustomerID).HasColumnName("CustomerID").ValueGeneratedOnAdd();
            e.Property(x => x.BillingAddress).HasColumnName("BillingAddress");
            e.Property(x => x.City).HasColumnName("City");
            e.Property(x => x.CompanyName).HasColumnName("CompanyName");
            e.Property(x => x.CompanyOrDepartment).HasColumnName("CompanyOrDepartment");
            e.Property(x => x.ContactFirstName).HasColumnName("ContactFirstName");
            e.Property(x => x.ContactLastName).HasColumnName("ContactLastName");
            e.Property(x => x.ContactTitle).HasColumnName("ContactTitle");
            e.Property(x => x.CountryRegion).HasColumnName("Country/Region");
            e.Property(x => x.EmailAddress).HasColumnName("EmailAddress");
            e.Property(x => x.Extension).HasColumnName("Extension");
            e.Property(x => x.FaxNumber).HasColumnName("FaxNumber");
            e.Property(x => x.Notes).HasColumnName("Notes");
            e.Property(x => x.PhoneNumber).HasColumnName("PhoneNumber");
            e.Property(x => x.PostalCode).HasColumnName("PostalCode");
            e.Property(x => x.StateOrProvince).HasColumnName("StateOrProvince");
        });

        modelBuilder.Entity<InvoiceDetails>(e =>
        {
            e.ToTable("InvoiceDetails");
            e.HasKey(x => x.InvoiceDetailID);
            e.Property(x => x.InvoiceDetailID).HasColumnName("InvoiceDetailID").ValueGeneratedOnAdd();
            e.Property(x => x.Discount).HasColumnName("Discount");
            e.Property(x => x.InvoiceID).HasColumnName("InvoiceID");
            e.Property(x => x.PaymentTerms).HasColumnName("PaymentTerms");
            e.Property(x => x.PricePerUnit).HasColumnName("PricePerUnit");
            e.Property(x => x.Quantity).HasColumnName("Quantity");
            e.Property(x => x.StockID).HasColumnName("StockID");
        });

        modelBuilder.Entity<Invoices>(e =>
        {
            e.ToTable("Invoices");
            e.HasKey(x => x.InvoiceID);
            e.Property(x => x.InvoiceID).HasColumnName("InvoiceID").ValueGeneratedOnAdd();
            e.Property(x => x.CustomerID).HasColumnName("CustomerID");
            e.Property(x => x.DeliveryID).HasColumnName("DeliveryID");
            e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
            e.Property(x => x.InvoiceDate).HasColumnName("InvoiceDate");
            e.Property(x => x.Notes).HasColumnName("Notes");
            e.Property(x => x.OrderID).HasColumnName("OrderID");
            e.Property(x => x.SalespersonName).HasColumnName("SalespersonName");
            e.Property(x => x.ShipDate).HasColumnName("ShipDate");
            e.Property(x => x.ShippedTo).HasColumnName("ShippedTo");
            e.Property(x => x.ShippedVia).HasColumnName("ShippedVia");
            e.Property(x => x.ShippingCost).HasColumnName("ShippingCost");
            e.Property(x => x.Status).HasColumnName("Status");
        });

        modelBuilder.Entity<Levels>(e =>
        {
            e.ToTable("Levels");
            e.HasKey(x => x.Level);
            e.Property(x => x.Level).HasColumnName("Level").ValueGeneratedNever();
        });

        modelBuilder.Entity<ManualStocks>(e =>
        {
            e.ToTable("ManualStocks");
            e.HasKey(x => x.ManualID);
            e.Property(x => x.ManualID).HasColumnName("ManualID").ValueGeneratedOnAdd();
            e.Property(x => x.Action).HasColumnName("Action");
            e.Property(x => x.Date).HasColumnName("Date");
            e.Property(x => x.Price).HasColumnName("Price");
            e.Property(x => x.Quantity).HasColumnName("Quantity");
            e.Property(x => x.StockID).HasColumnName("StockID");
            e.Property(x => x.User).HasColumnName("User");
        });

        modelBuilder.Entity<OrderReceptionDetails>(e =>
        {
            e.ToTable("OrderReceptionDetails");
            e.HasKey(x => x.OrderDetailID);
            e.Property(x => x.OrderDetailID).HasColumnName("OrderDetailID").ValueGeneratedOnAdd();
            e.Property(x => x.DateSold).HasColumnName("DateSold");
            e.Property(x => x.Discount).HasColumnName("Discount");
            e.Property(x => x.LineTotal).HasColumnName("LineTotal");
            e.Property(x => x.OrderID).HasColumnName("OrderID");
            e.Property(x => x.ProductID).HasColumnName("ProductID");
            e.Property(x => x.Quantity).HasColumnName("Quantity");
            e.Property(x => x.SalePrice).HasColumnName("SalePrice");
            e.Property(x => x.SalesTax).HasColumnName("SalesTax");
            e.Property(x => x.UnitPrice).HasColumnName("UnitPrice");
        });

        modelBuilder.Entity<OrderReceptions>(e =>
        {
            e.ToTable("OrderReceptions");
            e.HasKey(x => x.OrderID);
            e.Property(x => x.OrderID).HasColumnName("OrderID").ValueGeneratedOnAdd();
            e.Property(x => x.ChangedBy).HasColumnName("ChangedBy");
            e.Property(x => x.ChangedDate).HasColumnName("ChangedDate");
            e.Property(x => x.FreightCharge).HasColumnName("FreightCharge");
            e.Property(x => x.Notes).HasColumnName("Notes");
            e.Property(x => x.OrderDate).HasColumnName("OrderDate");
            e.Property(x => x.PromisedByDate).HasColumnName("PromisedByDate");
            e.Property(x => x.ProviderID).HasColumnName("ProviderID");
            e.Property(x => x.PurchaseOrderNumber).HasColumnName("PurchaseOrderNumber");
            e.Property(x => x.ReceivedBy).HasColumnName("ReceivedBy");
            e.Property(x => x.RequiredByDate).HasColumnName("RequiredByDate");
            e.Property(x => x.SalesTaxRate).HasColumnName("SalesTaxRate");
            e.Property(x => x.Status).HasColumnName("Status");
        });

        modelBuilder.Entity<OrderRequestDetails>(e =>
        {
            e.ToTable("OrderRequestDetails");
            e.HasKey(x => x.OrderDetailID);
            e.Property(x => x.OrderDetailID).HasColumnName("OrderDetailID").ValueGeneratedOnAdd();
            e.Property(x => x.DateSold).HasColumnName("DateSold");
            e.Property(x => x.Discount).HasColumnName("Discount");
            e.Property(x => x.LineTotal).HasColumnName("LineTotal");
            e.Property(x => x.OrderID).HasColumnName("OrderID");
            e.Property(x => x.ProductID).HasColumnName("ProductID");
            e.Property(x => x.Quantity).HasColumnName("Quantity");
            e.Property(x => x.SalePrice).HasColumnName("SalePrice");
            e.Property(x => x.SalesTax).HasColumnName("SalesTax");
            e.Property(x => x.UnitPrice).HasColumnName("UnitPrice");
        });

        modelBuilder.Entity<OrderRequests>(e =>
        {
            e.ToTable("OrderRequests");
            e.HasKey(x => x.OrderID);
            e.Property(x => x.OrderID).HasColumnName("OrderID").ValueGeneratedOnAdd();
            e.Property(x => x.ChangedBy).HasColumnName("ChangedBy");
            e.Property(x => x.ChangedDate).HasColumnName("ChangedDate");
            e.Property(x => x.CustomerID).HasColumnName("CustomerID");
            e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
            e.Property(x => x.FreightCharge).HasColumnName("FreightCharge");
            e.Property(x => x.Notes).HasColumnName("Notes");
            e.Property(x => x.OrderDate).HasColumnName("OrderDate");
            e.Property(x => x.PromisedByDate).HasColumnName("PromisedByDate");
            e.Property(x => x.PurchaseOrderNumber).HasColumnName("PurchaseOrderNumber");
            e.Property(x => x.RequiredByDate).HasColumnName("RequiredByDate");
            e.Property(x => x.SalesTaxRate).HasColumnName("SalesTaxRate");
            e.Property(x => x.ShipAddress).HasColumnName("ShipAddress");
            e.Property(x => x.ShipCity).HasColumnName("ShipCity");
            e.Property(x => x.ShipCountry).HasColumnName("ShipCountry");
            e.Property(x => x.ShipDate).HasColumnName("ShipDate");
            e.Property(x => x.ShipName).HasColumnName("ShipName");
            e.Property(x => x.ShipPhoneNumber).HasColumnName("ShipPhoneNumber");
            e.Property(x => x.ShippingMethodID).HasColumnName("ShippingMethodID");
            e.Property(x => x.ShipPostalCode).HasColumnName("ShipPostalCode");
            e.Property(x => x.ShipState).HasColumnName("ShipState");
            e.Property(x => x.ShipStateOrProvince).HasColumnName("ShipStateOrProvince");
            e.Property(x => x.Status).HasColumnName("Status");
        });

        modelBuilder.Entity<Products>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.ProductID);
            e.Property(x => x.ProductID).HasColumnName("ProductID").ValueGeneratedNever();
            e.Property(x => x.CategoryID).HasColumnName("CategoryID");
            e.Property(x => x.Discontinued).HasColumnName("Discontinued");
            e.Property(x => x.LeadTime).HasColumnName("LeadTime");
            e.Property(x => x.ProductDescription).HasColumnName("ProductDescription");
            e.Property(x => x.ProductName).HasColumnName("ProductName");
            e.Property(x => x.QuantityPerUnit).HasColumnName("QuantityPerUnit");
            e.Property(x => x.ReorderLevel).HasColumnName("ReorderLevel");
            e.Property(x => x.SerialNumber).HasColumnName("SerialNumber");
            e.Property(x => x.SupplierID).HasColumnName("SupplierID");
            e.Property(x => x.Unit).HasColumnName("Unit");
            e.Property(x => x.UnitPrice).HasColumnName("UnitPrice");
            e.Property(x => x.UnitsInStock).HasColumnName("UnitsInStock");
            e.Property(x => x.UnitsOnOrder).HasColumnName("UnitsOnOrder");
        });

        modelBuilder.Entity<ProductsByCustomer>(e =>
        {
            e.ToTable("ProductsByCustomer");
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).HasColumnName("ID").ValueGeneratedOnAdd();
            e.Property(x => x.CustomerID).HasColumnName("CustomerID");
            e.Property(x => x.ProductID).HasColumnName("ProductID");
        });

        modelBuilder.Entity<ProductsByProvider>(e =>
        {
            e.ToTable("ProductsByProvider");
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).HasColumnName("ID").ValueGeneratedOnAdd();
            e.Property(x => x.ProductID).HasColumnName("ProductID");
            e.Property(x => x.ProviderID).HasColumnName("ProviderID");
        });

        modelBuilder.Entity<Providers>(e =>
        {
            e.ToTable("Providers");
            e.HasKey(x => x.ProviderID);
            e.Property(x => x.ProviderID).HasColumnName("ProviderID").ValueGeneratedOnAdd();
            e.Property(x => x.Address).HasColumnName("Address");
            e.Property(x => x.City).HasColumnName("City");
            e.Property(x => x.ContactFirstName).HasColumnName("ContactFirstName");
            e.Property(x => x.ContactLastName).HasColumnName("ContactLastName");
            e.Property(x => x.ContactTitle).HasColumnName("ContactTitle");
            e.Property(x => x.CountryRegion).HasColumnName("Country/Region");
            e.Property(x => x.EmailAddress).HasColumnName("EmailAddress");
            e.Property(x => x.Extension).HasColumnName("Extension");
            e.Property(x => x.FaxNumber).HasColumnName("FaxNumber");
            e.Property(x => x.Notes).HasColumnName("Notes");
            e.Property(x => x.PaymentTerms).HasColumnName("PaymentTerms");
            e.Property(x => x.PhoneNumber).HasColumnName("PhoneNumber");
            e.Property(x => x.PostalCode).HasColumnName("PostalCode");
            e.Property(x => x.ProviderName).HasColumnName("ProviderName");
            e.Property(x => x.StateOrProvince).HasColumnName("StateOrProvince");
        });

        modelBuilder.Entity<StockLog>(e =>
        {
            e.ToTable("StockLog");
            e.HasKey(x => x.ID);
            e.Property(x => x.ID).HasColumnName("ID").ValueGeneratedOnAdd();
            e.Property(x => x.Date).HasColumnName("Date");
            e.Property(x => x.DocID).HasColumnName("DocID");
            e.Property(x => x.DocType).HasColumnName("DocType");
            e.Property(x => x.ProductID).HasColumnName("ProductID");
            e.Property(x => x.Quantity).HasColumnName("Quantity");
            e.Property(x => x.StockID).HasColumnName("StockID");
            e.Property(x => x.StockPrice).HasColumnName("StockPrice");
            e.Property(x => x.User).HasColumnName("User");
        });

        modelBuilder.Entity<Stocks>(e =>
        {
            e.ToTable("Stocks");
            e.HasKey(x => x.StockID);
            e.Property(x => x.StockID).HasColumnName("StockID").ValueGeneratedOnAdd();
            e.Property(x => x.DateModified).HasColumnName("DateModified");
            e.Property(x => x.DateStarted).HasColumnName("DateStarted");
            e.Property(x => x.InitialStock).HasColumnName("InitialStock");
            e.Property(x => x.ProductID).HasColumnName("ProductID");
            e.Property(x => x.Stock).HasColumnName("Stock");
            e.Property(x => x.StockPrice).HasColumnName("StockPrice");
            e.Property(x => x.UnitPrice).HasColumnName("UnitPrice");
            e.Property(x => x.User).HasColumnName("User");
        });

        modelBuilder.Entity<Users>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Username);
            e.Property(x => x.Username).HasColumnName("Username").ValueGeneratedNever();
            e.Property(x => x.Fullname).HasColumnName("Fullname");
            e.Property(x => x.Level).HasColumnName("Level");
            e.Property(x => x.Password).HasColumnName("Password");
        });

        modelBuilder.Entity<Warehouses>(e =>
        {
            e.ToTable("Warehouses");
            e.HasKey(x => x.WarehouseID);
            e.Property(x => x.WarehouseID).HasColumnName("WarehouseID").ValueGeneratedOnAdd();
            e.Property(x => x.Address).HasColumnName("Address");
            e.Property(x => x.City).HasColumnName("City");
            e.Property(x => x.CountryRegion).HasColumnName("Country/Region");
            e.Property(x => x.Description).HasColumnName("Description");
            e.Property(x => x.Name).HasColumnName("Name");
            e.Property(x => x.PostalCode).HasColumnName("PostalCode");
            e.Property(x => x.Region).HasColumnName("Region");
            e.Property(x => x.StateOrProvince).HasColumnName("StateOrProvince");
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpgradedDbContext).Assembly);
    }
}
