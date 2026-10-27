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
using Perfume.Web.Services;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/AdminDashboard/[action]")]
[Authorize(Roles = "Admin,StoreManager")]
public class AdminAnalyticsController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly IAdminDashboardAnalyticsService _dashboardAnalyticsService;

    public AdminAnalyticsController(
        PerfumeDbContext _db,
        IAdminDashboardAnalyticsService dashboardAnalyticsService)
    {
        this._db = _db;
        this._dashboardAnalyticsService = dashboardAnalyticsService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FinancialReport(DateTime? fromDate, DateTime? toDate, string? preset = "30days")
    {
        var now = DateTime.UtcNow;
        DateTime start;
        DateTime end = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;

        switch (preset?.ToLowerInvariant())
        {
            case "today":
                start = now.Date;
                break;
            case "7days":
                start = now.Date.AddDays(-7);
                break;
            case "thismonth":
                start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
            case "thisyear":
                start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
            case "all":
                start = DateTime.MinValue;
                break;
            default: // 30days
                start = fromDate ?? now.Date.AddDays(-30);
                if (fromDate.HasValue) start = fromDate.Value.Date;
                break;
        }

        // 1. Lấy danh sách giá vốn gần nhất từ StockAdjustments
        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        // 2. Query Orders trong kỳ (chỉ tính đơn hoàn tất hoặc đã thanh toán)
        var completedOrders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .Where(o => o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new FinancialReportViewModel
        {
            FromDate = start == DateTime.MinValue ? null : start,
            ToDate = end,
            Preset = preset ?? "30days",
            TotalOrdersCount = completedOrders.Count,
            GrossSales = completedOrders.Sum(o => o.SubTotal),
            VoucherDiscounts = completedOrders.Sum(o => o.DiscountAmount),
            ShippingFeesCollected = completedOrders.Sum(o => o.ShippingFee),
            GiftWrappingFeesCollected = completedOrders.Sum(o => o.GiftWrappingFee),
            NetRevenue = completedOrders.Sum(o => o.TotalAmount)
        };

        // Tính COGS và Lợi nhuận của từng đơn
        decimal totalCogs = 0;
        foreach (var order in completedOrders)
        {
            decimal orderCogs = 0;
            foreach (var item in order.Items)
            {
                decimal itemUnitCost = 0;
                if (item.VariantId.HasValue && recentCosts.TryGetValue(item.VariantId.Value, out var cost))
                {
                    itemUnitCost = cost;
                }
                else
                {
                    // Tỷ lệ giá vốn giả định 42% cho nước hoa Haute Parfumerie
                    itemUnitCost = Math.Round(item.UnitPrice * 0.42m, 0);
                }
                orderCogs += itemUnitCost * item.Quantity;
            }

            totalCogs += orderCogs;
            var orderProfit = order.TotalAmount - orderCogs;
            var marginPct = order.TotalAmount > 0 ? Math.Round((double)orderProfit / (double)order.TotalAmount * 100, 1) : 0;

            vm.OrderEntries.Add(new FinancialOrderEntry
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerName,
                PaymentMethod = order.PaymentMethod.ToString(),
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                GiftWrappingFee = order.GiftWrappingFee,
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                EstimatedCogs = orderCogs,
                GrossProfit = orderProfit,
                MarginPercent = marginPct
            });
        }

        vm.TotalCogs = totalCogs;
        vm.GrossProfit = vm.NetRevenue - vm.TotalCogs;
        vm.GrossMarginPercent = vm.NetRevenue > 0 ? Math.Round((double)vm.GrossProfit / (double)vm.NetRevenue * 100, 1) : 0;
        vm.AverageOrderValue = vm.TotalOrdersCount > 0 ? Math.Round(vm.NetRevenue / vm.TotalOrdersCount, 0) : 0;

        // 3. Định giá kho hiện tại
        var allVariants = await _db.ProductVariants
            .Include(v => v.Product!)
                .ThenInclude(p => p.Brand)
            .Include(v => v.Product!)
                .ThenInclude(p => p.FragranceFamily)
            .Where(v => !v.IsDeleted)
            .ToListAsync();

        vm.TotalStockQuantity = allVariants.Sum(v => v.StockQuantity);
        decimal inventoryCost = 0;
        decimal inventoryRetail = 0;

        var brandValuations = new Dictionary<string, (int Qty, decimal CostVal, decimal RetailVal)>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in allVariants)
        {
            var retailPrice = v.SalePrice > 0 ? v.SalePrice : (v.OriginalPrice > 0 ? v.OriginalPrice : (v.Product?.BasePrice ?? 0));
            decimal costPrice = 0;
            if (recentCosts.TryGetValue(v.Id, out var recordedCost))
            {
                costPrice = recordedCost;
            }
            else
            {
                costPrice = Math.Round(retailPrice * 0.42m, 0);
            }

            var itemCostVal = v.StockQuantity * costPrice;
            var itemRetailVal = v.StockQuantity * retailPrice;

            inventoryCost += itemCostVal;
            inventoryRetail += itemRetailVal;

            var brandName = v.Product?.Brand?.Name ?? "Khác";
            if (!brandValuations.ContainsKey(brandName))
            {
                brandValuations[brandName] = (0, 0, 0);
            }
            var cur = brandValuations[brandName];
            brandValuations[brandName] = (cur.Qty + v.StockQuantity, cur.CostVal + itemCostVal, cur.RetailVal + itemRetailVal);
        }

        vm.TotalInventoryCostValue = inventoryCost;
        vm.TotalInventoryRetailValue = inventoryRetail;
        vm.UnrealizedGrossProfit = Math.Max(0, inventoryRetail - inventoryCost);

        foreach (var kvp in brandValuations.OrderByDescending(b => b.Value.CostVal).Take(10))
        {
            vm.BrandValuations.Add(new BrandInventoryValuation
            {
                BrandName = kvp.Key,
                TotalUnits = kvp.Value.Qty,
                TotalCostValue = kvp.Value.CostVal,
                TotalRetailValue = kvp.Value.RetailVal,
                PotentialProfit = Math.Max(0, kvp.Value.RetailVal - kvp.Value.CostVal)
            });
        }

        // 4. Biểu đồ Chart.js: Doanh thu & Lợi nhuận theo các ngày gần nhất (hoặc 7 ngày qua)
        var chartDays = Enumerable.Range(0, 14)
            .Select(i => now.Date.AddDays(-13 + i))
            .ToList();

        foreach (var day in chartDays)
        {
            var dayOrders = completedOrders.Where(o => o.CreatedAt.Date == day).ToList();
            var dayRevenue = dayOrders.Sum(o => o.TotalAmount);
            var dayCogs = dayOrders.Sum(o => vm.OrderEntries.FirstOrDefault(e => e.OrderId == o.Id)?.EstimatedCogs ?? 0);
            var dayProfit = dayRevenue - dayCogs;

            vm.DailyChartLabels.Add(day.ToString("dd/MM"));
            vm.DailyRevenueData.Add(dayRevenue);
            vm.DailyProfitData.Add(dayProfit);
        }

        // 5. Biểu đồ cơ cấu nhóm hương
        var familyRevenues = completedOrders.SelectMany(o => o.Items)
            .GroupBy(i => i.Product?.FragranceFamily?.Name ?? "Woody / Khác")
            .Select(g => new { FamilyName = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        foreach (var fam in familyRevenues)
        {
            vm.FamilyChartLabels.Add(fam.FamilyName);
            vm.FamilyChartData.Add(fam.Revenue);
        }

        return View("~/Areas/Admin/Views/AdminDashboard/FinancialReport.cshtml", vm);
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportFinancialOrdersCsv(DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.UtcNow;
        var start = fromDate ?? now.AddDays(-30);
        var end = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .Where(o => o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM
        sb.Append('\uFEFF');
        sb.AppendLine("Mã Đơn,Ngày Đặt,Khách Hàng,Phương Thức,Tiền Hàng,Giảm Giá,Gói Quà,Phí Ship,Doanh Thu Thuần,Giá Vốn Ước Tính,Lợi Nhuận Gộp,Biên Lãi %");

        foreach (var o in orders)
        {
            decimal cogs = 0;
            foreach (var item in o.Items)
            {
                decimal uCost = item.VariantId.HasValue && recentCosts.TryGetValue(item.VariantId.Value, out var c)
                    ? c : Math.Round(item.UnitPrice * 0.42m, 0);
                cogs += uCost * item.Quantity;
            }
            var profit = o.TotalAmount - cogs;
            var margin = o.TotalAmount > 0 ? Math.Round((double)profit / (double)o.TotalAmount * 100, 1) : 0;

            sb.AppendLine($"\"{o.OrderCode}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm}\",\"{o.CustomerName}\",\"{o.PaymentMethod}\",{o.SubTotal},{o.DiscountAmount},{o.GiftWrappingFee},{o.ShippingFee},{o.TotalAmount},{cogs},{profit},{margin}%");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"PerfumeLuxe_Financial_Orders_{now:yyyyMMdd_HHmm}.csv");
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportInventoryValuationCsv()
    {
        var now = DateTime.UtcNow;
        var variants = await _db.ProductVariants
            .Include(v => v.Product!)
                .ThenInclude(p => p.Brand)
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.Product != null ? v.Product.Name : string.Empty)
            .ToListAsync();

        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("ID Variant,Mã SKU,Tên Nước Hoa,Thương Hiệu,Dung Tích,Tồn Kho,Đơn Giá Vốn,Đơn Giá Niêm Yết,Tổng Giá Trị Vốn,Tổng Giá Trị Bán Lẻ,Lợi Nhuận Tiềm Năng");

        foreach (var v in variants)
        {
            var retailPrice = v.SalePrice > 0 ? v.SalePrice : (v.OriginalPrice > 0 ? v.OriginalPrice : (v.Product?.BasePrice ?? 0));
            decimal costPrice = recentCosts.TryGetValue(v.Id, out var c) ? c : Math.Round(retailPrice * 0.42m, 0);
            var totalCost = v.StockQuantity * costPrice;
            var totalRetail = v.StockQuantity * retailPrice;
            var potentialProfit = Math.Max(0, totalRetail - totalCost);

            sb.AppendLine($"{v.Id},\"{v.VariantSku}\",\"{v.Product?.Name}\",\"{v.Product?.Brand?.Name}\",\"{v.VolumeLabel}\",{v.StockQuantity},{costPrice},{retailPrice},{totalCost},{totalRetail},{potentialProfit}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"PerfumeLuxe_Inventory_Valuation_{now:yyyyMMdd_HHmm}.csv");
    }


    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole("Admin") && User.IsInRole("StoreManager"))
        {
            return RedirectToAction("Index", "StoreManager");
        }

        var model = await _dashboardAnalyticsService.BuildDashboardAsync();
        return View("~/Areas/Admin/Views/AdminDashboard/Index.cshtml", model);
    }
}
