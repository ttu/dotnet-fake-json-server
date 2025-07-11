using System.Reflection;
using System.Collections.Generic;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Mvc;

namespace FakeServer.Controllers;

/// <summary>
/// Controller that provides health check functionality for the Fake JSON Server.
/// Monitors critical dependencies and reports service status, uptime, and version information.
/// </summary>
[ApiController]
[Route("health")]
public class HealthCheckController : ControllerBase
{
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private readonly string _version;
    private readonly IDataStore _ds;

    public HealthCheckController(IDataStore ds)
    {
        // Get the version from the executing assembly or set a default
        _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        _ds = ds;
    }

    /// <summary>
    /// Retrieves health status of the application
    /// </summary>
    /// <remarks>
    /// Provides current health information about the system, including:
    /// - Status: "Healthy" when all services are operational, "Unhealthy" otherwise
    /// - Uptime: Duration since application startup in days, hours, and minutes
    /// - Version: Application version number
    ///
    /// Example: GET /health
    /// </remarks>
    /// <returns>Health status object</returns>
    /// <response code="200">Application is healthy</response>
    /// <response code="503">Application is unhealthy, data store is inaccessible</response>
    [HttpGet]
    public IActionResult Get()
    {
        // Calculate uptime once
        TimeSpan uptime = DateTime.UtcNow - _startTime;
        string formattedUptime = FormatUptime(uptime);

        try
        {
            // Check if data store is accessible
            _ds.Reload();

            var result = new Dictionary<string, string>
                {
                    { "status", "Healthy" },
                    { "uptime", formattedUptime },
                    { "version", _version }
                };
            return Ok(result);
        }
        catch (Exception ex)
        {
            var result = new Dictionary<string, string>
                {
                    { "status", "Unhealthy" },
                    { "version", _version }
                };
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
    }

    private string FormatUptime(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            int days = (int)timeSpan.TotalDays;
            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;
            return $"{days} day{(days == 1 ? "" : "s")}, {hours} hour{(hours == 1 ? "" : "s")}, {minutes} minute{(minutes == 1 ? "" : "s")}";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            int hours = (int)timeSpan.TotalHours;
            int minutes = timeSpan.Minutes;
            return $"{hours} hour{(hours == 1 ? "" : "s")}, {minutes} minute{(minutes == 1 ? "" : "s")}";
        }
        else
        {
            int minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}";
        }
    }
}
