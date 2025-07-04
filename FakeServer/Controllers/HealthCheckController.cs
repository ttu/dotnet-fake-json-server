
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Reflection;
using static FakeServer.Controllers.HealthCheckController;

namespace FakeServer.Controllers
{
    /// <summary>
    /// Provides health check endpoints for the application.
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthCheckController(HealthChecker healthChecker) : ControllerBase
    {
        /// <summary>
        /// Gets the health status of the application.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> representing the health status.</returns>
        /// <response code="200">If the application is healthy.</response>
        /// <response code="503">If the application is unhealthy.</response>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 503)]
        public IActionResult Get()
        {
            var result = healthChecker.Check();

            if (result.IsHealthy)
            {
                return Ok(new
                {
                    status = "Healthy",
                    uptime = result.Duration,
                    version = result.Version
                });
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    version = result.Version
                });
            }
        }

        /// <summary>
        /// Performs health checks on the application's dependencies.
        /// </summary>        
        /// <param name="ds">The data store to check.</param>
        public class HealthChecker(IDataStore ds)
        {
            private static readonly DateTime _startTime = DateTime.UtcNow;
            private static readonly string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            /// <summary>
            /// Checks the health of the application.
            /// </summary>
            /// <returns>A <see cref="HealthCheckResult"/> indicating the health status.</returns>
            public HealthCheckResult Check()
            {
                try
                {
                    // Check if data store is accessible
                    ds.Reload();
                    return new HealthCheckResult(true, _version, CalculateUptime());
                }
                catch (Exception)
                {
                    return new HealthCheckResult(false, _version);
                }
            }

            private string CalculateUptime()
            {
                var uptime = DateTime.UtcNow - _startTime;
                Func<int, string, string> plural = (v, u) => $"{v} {u}{(v == 1 ? "" : "s")}";

                return string.Join(", ", new (bool condition, string value)[]
                {
                    ((int)uptime.TotalDays >= 1, plural((int)uptime.TotalDays, "day")),
                    (uptime.TotalHours >= 1, plural(uptime.Hours, "hour")),
                    (uptime.TotalMinutes >= 1 || uptime.TotalHours < 1, plural(uptime.Minutes, "minute"))
                }.Where(x => x.condition).Select(x => x.value));
            }

            /// <summary>
            /// Represents the result of a health check.
            /// </summary>
            /// <param name="IsHealthy">A value indicating whether the application is healthy.</param>
            /// <param name="Version">The version of the application.</param>
            /// <param name="Duration">The uptime of the application.</param>
            public record HealthCheckResult(bool IsHealthy, string Version, string? Duration = null);
        }
    }
}
