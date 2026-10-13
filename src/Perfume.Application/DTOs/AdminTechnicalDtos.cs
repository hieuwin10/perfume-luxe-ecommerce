using System;
using System.Collections.Generic;
using Perfume.Domain.Entities;

namespace Perfume.Application.DTOs;

public class SystemHealthViewModel
{
    public bool SqlConnected { get; set; }
    public long SqlLatencyMs { get; set; }
    public int SqlActiveConnections { get; set; }
    public string SqlVersion { get; set; } = "Microsoft SQL Server";
    public bool RedisConfigured { get; set; }
    public bool RedisConnected { get; set; }
    public long ProcessMemoryMb { get; set; }
    public long GcTotalMemoryMb { get; set; }
    public string FreeDiskSpaceGb { get; set; } = "N/A";
    public TimeSpan ServerUptime { get; set; }
    public DateTime ServerStartTimeUtc { get; set; }
    public DateTime CurrentTimeUtc { get; set; } = DateTime.UtcNow;
    public string DotNetVersion { get; set; } = Environment.Version.ToString();
    public string OsDescription { get; set; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public int ProcessorCount { get; set; } = Environment.ProcessorCount;
    public int TotalAuditLogsCount { get; set; }
    public int TotalErrorLogsCount { get; set; }
}

public class SystemErrorLogsViewModel
{
    public List<SystemErrorLog> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int? StatusCodeFilter { get; set; }
}
