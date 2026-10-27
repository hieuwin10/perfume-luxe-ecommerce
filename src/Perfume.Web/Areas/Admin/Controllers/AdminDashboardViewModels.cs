using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Areas.Admin.Controllers;
public class AuditLogsViewModel
{
    public List<DatabaseAuditLog> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? ActionFilter { get; set; }
    public bool? SucceededFilter { get; set; }
    public string? ActorFilter { get; set; }
}

public class DatabaseBackupViewModel
{
    public string BackupDirectory { get; set; } = string.Empty;
    public bool DirectoryExists { get; set; }
    public List<BackupFileInfo> BackupFiles { get; set; } = new();
}

public class BackupFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FinancialReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Preset { get; set; } = "30days";

    // P&L Metrics
    public int TotalOrdersCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal VoucherDiscounts { get; set; }
    public decimal ShippingFeesCollected { get; set; }
    public decimal GiftWrappingFeesCollected { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public double GrossMarginPercent { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Inventory Valuation
    public int TotalStockQuantity { get; set; }
    public decimal TotalInventoryCostValue { get; set; }
    public decimal TotalInventoryRetailValue { get; set; }
    public decimal UnrealizedGrossProfit { get; set; }

    // Breakdown Lists
    public List<FinancialOrderEntry> OrderEntries { get; set; } = new();
    public List<BrandInventoryValuation> BrandValuations { get; set; } = new();

    // Chart Data
    public List<string> DailyChartLabels { get; set; } = new();
    public List<decimal> DailyRevenueData { get; set; } = new();
    public List<decimal> DailyProfitData { get; set; } = new();

    public List<string> FamilyChartLabels { get; set; } = new();
    public List<decimal> FamilyChartData { get; set; } = new();
}

public class FinancialOrderEntry
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GiftWrappingFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal EstimatedCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public double MarginPercent { get; set; }
}

public class BrandInventoryValuation
{
    public string BrandName { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialProfit { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int TotalBrands { get; set; }
    public int TotalCategories { get; set; }
    public int TotalBlogPosts { get; set; }
    public int TotalReviews { get; set; }
    public int TotalVouchers { get; set; }

    // Thống kê tài chính & vận hành
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ConfirmedOrdersCount { get; set; }
    public int ShippingOrdersCount { get; set; }
    public int DeliveredOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }
    public double FulfillmentRate { get; set; }

    // Biểu đồ
    public List<string> RevenueChartLabels { get; set; } = new();
    public List<decimal> RevenueChartData { get; set; } = new();
    public Dictionary<string, int> OrderStatusChart { get; set; } = new();
    public Dictionary<string, int> PaymentMethodChart { get; set; } = new();

    // Top sản phẩm & Nhóm hương
    public List<DashboardTopProductStat> TopProducts { get; set; } = new();
    public List<ScentFamilyStat> ScentFamilyStats { get; set; } = new();

    public IList<AppUser> RecentUsers { get; set; } = new List<AppUser>();
    public IList<Perfume.Domain.Entities.Order> RecentOrders { get; set; } = new List<Perfume.Domain.Entities.Order>();
    public IList<TableInfo> Tables { get; set; } = new List<TableInfo>();
}

public class DashboardTopProductStat
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ScentFamilyStat
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#D4AF37";
    public int ProductCount { get; set; }
}

public class InventoryViewModel
{
    public int TotalProducts { get; set; }
    public int TotalStockUnits { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public string CurrentFilter { get; set; } = "all";
    public string? SearchQuery { get; set; }
    public List<InventoryProductItem> Items { get; set; } = new();
    public List<StockAdjustment> RecentAdjustments { get; set; } = new();
}

public class InventoryProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalStock { get; set; }
    public bool IsInStock { get; set; }
    public List<InventoryVariantItem> Variants { get; set; } = new();
}

public class InventoryVariantItem
{
    public int VariantId { get; set; }
    public string VolumeLabel { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
}
