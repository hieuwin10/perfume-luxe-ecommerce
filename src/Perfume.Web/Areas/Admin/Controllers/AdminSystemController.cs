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

[Area("Admin")]
[Route("Admin/AdminDashboard/[action]")]
[Authorize(Roles = "Admin,StoreManager")]
public class AdminSystemController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly IDatabaseConsoleService _dbConsole;
    private readonly UserManager<AppUser> _userManager;
    private readonly ISystemSettingService _settingService;
    private readonly IBlogService _blogService;
    private readonly IConfiguration _configuration;

    public AdminSystemController(
        PerfumeDbContext _db,
        IDatabaseConsoleService _dbConsole,
        UserManager<AppUser> _userManager,
        ISystemSettingService _settingService,
        IBlogService _blogService,
        IConfiguration _configuration)
    {
        this._db = _db;
        this._dbConsole = _dbConsole;
        this._userManager = _userManager;
        this._settingService = _settingService;
        this._blogService = _blogService;
        this._configuration = _configuration;
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTastingStatus(int id, string status)
    {
        var booking = await _db.PrivateTastingBookings.FindAsync(id);
        if (booking == null) return NotFound();

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái lịch hẹn VIP-{booking.Id:D5} thành: {status}";
        return RedirectToAction(nameof(Tastings));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ErrorLogs(string? search, int? statusCode, int page = 1)
    {
        var query = _db.SystemErrorLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(l => l.ErrorCode.Contains(s) || l.Message.Contains(s) || (l.RequestPath != null && l.RequestPath.Contains(s)));
        }

        if (statusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode == statusCode.Value);
        }

        var totalCount = await query.CountAsync();
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new SystemErrorLogsViewModel
        {
            Logs = logs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Search = search,
            StatusCodeFilter = statusCode
        };

        return View("~/Areas/Admin/Views/AdminDashboard/ErrorLogs.cshtml", vm);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Erd()
    {
        var tables = await _dbConsole.GetAllTablesAsync();
        return View("~/Areas/Admin/Views/AdminDashboard/Erd.cshtml", tables);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Tastings(string? status)
    {
        var query = _db.PrivateTastingBookings
            .AsNoTracking()
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(b => b.Status == status);
        }

        var list = await query.ToListAsync();
        ViewBag.CurrentStatus = status ?? "all";
        return View("~/Areas/Admin/Views/AdminDashboard/Tastings.cshtml", list);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Blogs()
    {
        var posts = await _blogService.GetAllPostsForAdminAsync();
        return View("~/Areas/Admin/Views/AdminDashboard/Blogs.cshtml", posts);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDatabaseBackup()
    {
        var backupDirectory = _configuration["Database:BackupPath"];
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        }

        try
        {
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            var fileName = $"PerfumeDb-Manual-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
            var backupPath = Path.Combine(backupDirectory, fileName);

            var sql = $"BACKUP DATABASE [PerfumeDb] TO DISK = N'{backupPath.Replace("'", "''")}' WITH COPY_ONLY, INIT";
            await _db.Database.ExecuteSqlRawAsync(sql);

            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User),
                Action = "DATABASE_BACKUP",
                Target = fileName,
                Succeeded = true,
                ElapsedMilliseconds = 0
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo bản sao lưu thành công: {fileName}";
        }
        catch (Exception ex)
        {
            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User),
                Action = "DATABASE_BACKUP_FAILED",
                Target = ex.Message[..Math.Min(120, ex.Message.Length)],
                Succeeded = false,
                ElapsedMilliseconds = 0
            });
            await _db.SaveChangesAsync();

            TempData["Error"] = $"Không thể sao lưu cơ sở dữ liệu: {ex.Message}";
        }

        return RedirectToAction(nameof(DatabaseBackup));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
    {
        if (newRole is not ("Admin" or "StoreManager" or "WarehouseStaff" or "Concierge" or "Customer"))
        {
            TempData["Error"] = "Role được chọn không hợp lệ.";
            return RedirectToAction(nameof(Users));
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal) && newRole != "Admin")
        {
            TempData["Error"] = "Không thể tự hạ quyền Admin của tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Users));
        }
        if (currentRoles.Contains("Admin") && newRole != "Admin" &&
            (await _userManager.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            TempData["Error"] = "Hệ thống cần giữ lại ít nhất một tài khoản Admin.";
            return RedirectToAction(nameof(Users));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var add = remove.Succeeded ? await _userManager.AddToRoleAsync(user, newRole) : remove;
        if (!add.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = string.Join(" ", add.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã đổi role của {user.FullName} sang {newRole}.";
        return RedirectToAction(nameof(Users));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal) ||
            (roles.Contains("Admin") && (await _userManager.GetUsersInRoleAsync("Admin")).Count <= 1))
        {
            TempData["Error"] = "Không thể tự xóa tài khoản đang đăng nhập hoặc xóa Admin cuối cùng.";
            return RedirectToAction(nameof(Users));
        }
        var name = user.FullName;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }
        TempData["Success"] = $"Đã xóa tài khoản {name}.";
        return RedirectToAction(nameof(Users));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearErrorLogs(string mode)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            int deletedCount;
            if (mode == "all")
            {
                var allLogs = await _db.SystemErrorLogs.ToListAsync();
                deletedCount = allLogs.Count;
                _db.SystemErrorLogs.RemoveRange(allLogs);
            }
            else
            {
                var cutoff = DateTime.UtcNow.AddDays(-30);
                var oldLogs = await _db.SystemErrorLogs.Where(l => l.CreatedAt < cutoff).ToListAsync();
                deletedCount = oldLogs.Count;
                _db.SystemErrorLogs.RemoveRange(oldLogs);
            }

            await _db.SaveChangesAsync();
            sw.Stop();

            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User) ?? User.Identity?.Name,
                Action = mode == "all" ? "CLEAR_ALL_ERROR_LOGS" : "PURGE_OLD_ERROR_LOGS",
                Target = $"{deletedCount} logs removed",
                Succeeded = true,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã dọn dẹp thành công {deletedCount} bản ghi nhật ký lỗi hệ thống!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Không thể dọn dẹp nhật ký lỗi: {ex.Message}";
        }

        return RedirectToAction(nameof(ErrorLogs));
    }


    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CustomerProfile(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == user.Id || o.CustomerEmail == user.Email)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var loyalty = await _db.UserLoyalties.FirstOrDefaultAsync(l => l.UserId == user.Id);
        var isLocked = await _userManager.IsLockedOutAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        ViewBag.Orders = orders;
        ViewBag.Loyalty = loyalty;
        ViewBag.IsLocked = isLocked;
        ViewBag.Roles = roles;
        ViewBag.TotalSpent = orders.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        ViewBag.DeliveredOrdersCount = orders.Count(o => o.OrderStatus == OrderStatus.Delivered);

        return View("~/Areas/Admin/Views/AdminDashboard/CustomerProfile.cshtml", user);
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult DatabaseBackup()
    {
        var backupDirectory = _configuration["Database:BackupPath"];
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        }

        var files = new List<BackupFileInfo>();
        if (Directory.Exists(backupDirectory))
        {
            var dirInfo = new DirectoryInfo(backupDirectory);
            foreach (var fi in dirInfo.GetFiles("*.bak").OrderByDescending(f => f.CreationTimeUtc))
            {
                files.Add(new BackupFileInfo
                {
                    FileName = fi.Name,
                    FullPath = fi.FullName,
                    SizeBytes = fi.Length,
                    CreatedAt = fi.CreationTimeUtc
                });
            }
        }

        var vm = new DatabaseBackupViewModel
        {
            BackupDirectory = backupDirectory,
            DirectoryExists = Directory.Exists(backupDirectory),
            BackupFiles = files
        };

        return View("~/Areas/Admin/Views/AdminDashboard/DatabaseBackup.cshtml", vm);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSmtp(string recipientEmail)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData["Error"] = "Vui lòng nhập địa chỉ email người nhận thử nghiệm.";
            return RedirectToAction(nameof(Settings));
        }

        var result = await _settingService.TestSmtpConnectionAsync(recipientEmail.Trim());
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditLogs(string? actionFilter, bool? succeededFilter, string? actorFilter, int page = 1)
    {
        var query = _db.DatabaseAuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (succeededFilter.HasValue)
        {
            query = query.Where(a => a.Succeeded == succeededFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(actorFilter))
        {
            query = query.Where(a => a.ActorUserId != null && a.ActorUserId.Contains(actorFilter));
        }

        var totalCount = await query.CountAsync();
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new AuditLogsViewModel
        {
            Logs = logs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            ActionFilter = actionFilter,
            SucceededFilter = succeededFilter,
            ActorFilter = actorFilter
        };

        return View("~/Areas/Admin/Views/AdminDashboard/AuditLogs.cshtml", vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SystemHealth()
    {
        var vm = new SystemHealthViewModel();

        // 1. SQL Database Health Check & Latency
        var sw = Stopwatch.StartNew();
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            sw.Stop();
            vm.SqlConnected = canConnect;
            vm.SqlLatencyMs = sw.ElapsedMilliseconds;

            if (canConnect)
            {
                var conn = _db.Database.GetDbConnection();
                vm.SqlVersion = $"{conn.Database} ({conn.ServerVersion ?? "Microsoft SQL Server"})";
            }
        }
        catch (Exception)
        {
            sw.Stop();
            vm.SqlConnected = false;
            vm.SqlLatencyMs = sw.ElapsedMilliseconds;
            vm.SqlVersion = "Lỗi kết nối cơ sở dữ liệu";
        }

        // 2. Redis Cache Check
        var redisConn = _configuration["Redis:ConnectionString"] ?? _configuration["ConnectionStrings:Redis"];
        vm.RedisConfigured = !string.IsNullOrWhiteSpace(redisConn);
        vm.RedisConnected = false;

        // 3. Process & Memory Metrics
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            vm.ProcessMemoryMb = currentProcess.WorkingSet64 / (1024 * 1024);
            vm.ServerStartTimeUtc = currentProcess.StartTime.ToUniversalTime();
            vm.ServerUptime = DateTime.UtcNow - vm.ServerStartTimeUtc;
        }
        catch
        {
            vm.ProcessMemoryMb = 0;
            vm.ServerStartTimeUtc = DateTime.UtcNow;
            vm.ServerUptime = TimeSpan.Zero;
        }

        vm.GcTotalMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024);

        // 4. Free Disk Space
        try
        {
            var appDrive = Path.GetPathRoot(AppContext.BaseDirectory);
            if (!string.IsNullOrWhiteSpace(appDrive))
            {
                var driveInfo = new DriveInfo(appDrive);
                var freeGb = driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                var totalGb = driveInfo.TotalSize / (1024.0 * 1024 * 1024);
                vm.FreeDiskSpaceGb = $"{freeGb:N1} GB / {totalGb:N1} GB ({driveInfo.Name})";
            }
        }
        catch
        {
            vm.FreeDiskSpaceGb = "Không xác định";
        }

        // 5. Counts
        try
        {
            vm.TotalAuditLogsCount = await _db.DatabaseAuditLogs.CountAsync();
            vm.TotalErrorLogsCount = await _db.SystemErrorLogs.CountAsync();
        }
        catch
        {
            // fallback
        }

        return View("~/Areas/Admin/Views/AdminDashboard/SystemHealth.cshtml", vm);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBlog(int id)
    {
        var success = await _blogService.DeletePostAsync(id);
        if (!success) return NotFound();

        TempData["Success"] = "Đã xóa bài viết thành công!";
        return RedirectToAction(nameof(Blogs));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlushCache()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var distCache = HttpContext?.RequestServices?.GetService<IDistributedCache>();
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
            sw.Stop();

            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User) ?? User.Identity?.Name,
                Action = "PURGE_SYSTEM_CACHE",
                Target = "ApplicationMemoryAndRuntimeCache",
                Succeeded = true,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã giải phóng bộ nhớ đệm cache và tối ưu hóa GC hoàn tất ({sw.ElapsedMilliseconds}ms)!";
        }
        catch (Exception ex)
        {
            sw.Stop();
            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User) ?? User.Identity?.Name,
                Action = "PURGE_SYSTEM_CACHE_FAILED",
                Target = ex.Message[..Math.Min(120, ex.Message.Length)],
                Succeeded = false,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            });
            await _db.SaveChangesAsync();

            TempData["Error"] = $"Lỗi khi xóa bộ nhớ đệm: {ex.Message}";
        }

        return RedirectToAction(nameof(SystemHealth));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SystemSettingsDto model)
    {
        if (!ModelState.IsValid) return View("~/Areas/Admin/Views/AdminDashboard/Settings.cshtml", model);
        await _settingService.SaveSettingsAsync(model);
        TempData["Success"] = "Đã lưu cài đặt hệ thống thành công!";
        return RedirectToAction(nameof(Settings));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Users));
        }

        var isLocked = await _userManager.IsLockedOutAsync(user);
        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Đã mở khóa tài khoản của {user.FullName}.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(50));
            TempData["Success"] = $"Đã tạm khóa tài khoản của {user.FullName}.";
        }

        return RedirectToAction(nameof(Users));
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBlog(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
        {
            ModelState.AddModelError("", "Tiêu đề và nội dung bài viết không được để trống.");
            ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
            return View("~/Areas/Admin/Views/AdminDashboard/CreateBlog.cshtml", post);
        }

        await _blogService.CreatePostAsync(post);
        TempData["Success"] = $"Đã xuất bản bài viết \"{post.Title}\" thành công!";
        return RedirectToAction(nameof(Blogs));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var usersWithRoles = new List<(AppUser User, IList<string> Roles)>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            usersWithRoles.Add((u, roles));
        }
        return View("~/Areas/Admin/Views/AdminDashboard/Users.cshtml", usersWithRoles);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBlog(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
        {
            ModelState.AddModelError("", "Tiêu đề và nội dung bài viết không được để trống.");
            ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
            return View("~/Areas/Admin/Views/AdminDashboard/CreateBlog.cshtml", post);
        }

        var success = await _blogService.UpdatePostAsync(post);
        if (!success) return NotFound();

        TempData["Success"] = $"Đã cập nhật bài viết \"{post.Title}\" thành công!";
        return RedirectToAction(nameof(Blogs));
    }
}
