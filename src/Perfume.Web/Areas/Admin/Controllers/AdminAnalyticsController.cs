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
    private readonly IAdminDashboardAnalyticsService _dashboardAnalyticsService;
    private readonly IAdminFinancialReportService _financialReportService;

    public AdminAnalyticsController(
        IAdminDashboardAnalyticsService dashboardAnalyticsService,
        IAdminFinancialReportService financialReportService)
    {
        this._dashboardAnalyticsService = dashboardAnalyticsService;
        this._financialReportService = financialReportService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FinancialReport(DateTime? fromDate, DateTime? toDate, string? preset = "30days")
    {
        var model = await _financialReportService.BuildFinancialReportAsync(fromDate, toDate, preset);
        return View("~/Areas/Admin/Views/AdminDashboard/FinancialReport.cshtml", model);
    }




    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportFinancialOrdersCsv(DateTime? fromDate, DateTime? toDate)
    {
        var export = await _financialReportService.CreateFinancialOrdersCsvAsync(fromDate, toDate);
        return File(export.Content, "text/csv; charset=utf-8", export.FileName);
    }




    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportInventoryValuationCsv()
    {
        var export = await _financialReportService.CreateInventoryValuationCsvAsync();
        return File(export.Content, "text/csv; charset=utf-8", export.FileName);
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
