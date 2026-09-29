#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Products table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Products
{
    public int? CategoryID { get; set; }
    public int Discontinued { get; set; }
    public string LeadTime { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? QuantityPerUnit { get; set; }
    public int? ReorderLevel { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int? SupplierID { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }
    public int? UnitsInStock { get; set; }
    public int? UnitsOnOrder { get; set; }

    public virtual SKS_Blazor.Domain.Entities.Core.Categories Category { get; set; }
    public virtual ICollection<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails> OrderReceptionDetails { get; set; } = new List<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails> OrderRequestDetails { get; set; } = new List<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Products.ProductsByCustomer> ProductsByCustomers { get; set; } = new List<SKS_Blazor.Domain.Entities.Products.ProductsByCustomer>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Products.ProductsByProvider> ProductsByProviders { get; set; } = new List<SKS_Blazor.Domain.Entities.Products.ProductsByProvider>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Stock.StockLog> StockLogs { get; set; } = new List<SKS_Blazor.Domain.Entities.Stock.StockLog>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Core.Stocks> Stocks { get; set; } = new List<SKS_Blazor.Domain.Entities.Core.Stocks>();
}
