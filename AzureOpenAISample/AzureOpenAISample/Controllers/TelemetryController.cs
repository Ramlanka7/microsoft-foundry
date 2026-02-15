using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// Controller demonstrating Application Insights custom telemetry
/// 
/// Interview Preparation Topics:
/// 1. Built-in telemetry: Request, dependency, exception tracking
/// 2. Custom telemetry: Events, metrics, traces
/// 3. Performance monitoring and distributed tracing
/// 4. Live Metrics for real-time monitoring
/// 5. Query with Kusto Query Language (KQL)
/// 6. Alerts and dashboards
/// 7. Integration with Azure Monitor
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(
        TelemetryClient telemetryClient,
        ILogger<TelemetryController> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/Telemetry/demo
    /// Demonstrates various Application Insights features
    /// 
    /// Interview Talking Points:
    /// - TrackEvent: Custom business events (user actions, business milestones)
    /// - TrackMetric: Numeric measurements (queue length, items processed)
    /// - TrackTrace: Diagnostic logs
    /// - TrackDependency: External service calls
    /// - TrackException: Errors and exceptions
    /// - TrackRequest: HTTP requests (automatic in ASP.NET Core)
    /// </summary>
    [HttpGet("demo")]
    public IActionResult DemoTelemetry()
    {
        try
        {
            // 1. Track a custom event
            // Use: Business events, user actions, feature usage
            _telemetryClient.TrackEvent("TelemetryDemo_Accessed",
                properties: new Dictionary<string, string>
                {
                    ["User"] = "DemoUser",
                    ["Feature"] = "TelemetryDemo",
                    ["Environment"] = "Development"
                },
                metrics: new Dictionary<string, double>
                {
                    ["ResponseTime"] = 123.45
                });

            // 2. Track a custom metric
            // Use: Performance counters, business metrics, KPIs
            _telemetryClient.TrackMetric("DemoMetric", 42.5, 
                properties: new Dictionary<string, string>
                {
                    ["MetricType"] = "Demo",
                    ["Unit"] = "Count"
                });

            // 3. Track a trace (log message)
            // Use: Diagnostic information
            _telemetryClient.TrackTrace("Telemetry demo executed successfully", 
                SeverityLevel.Information,
                properties: new Dictionary<string, string>
                {
                    ["Component"] = "TelemetryController",
                    ["Action"] = "Demo"
                });

            // 4. Track a dependency (simulated external call)
            // Use: Database calls, HTTP calls, Azure service calls
            var dependencyStart = DateTime.UtcNow;
            System.Threading.Thread.Sleep(50); // Simulate external call
            var dependencyDuration = DateTime.UtcNow - dependencyStart;

            _telemetryClient.TrackDependency(
                "HTTP",
                "api.example.com",
                "GET /demo",
                dependencyStart,
                dependencyDuration,
                success: true);

            // ILogger also sends to Application Insights automatically
            _logger.LogInformation("Telemetry demo executed at {Time}", DateTime.UtcNow);

            return Ok(new
            {
                message = "Telemetry demo executed successfully",
                telemetryTypes = new[]
                {
                    "Event: TelemetryDemo_Accessed",
                    "Metric: DemoMetric = 42.5",
                    "Trace: Information log",
                    "Dependency: External API call",
                    "Log: ILogger integration"
                },
                tip = "Check Application Insights portal to see these telemetry items"
            });
        }
        catch (Exception ex)
        {
            // Track exception
            _telemetryClient.TrackException(ex,
                properties: new Dictionary<string, string>
                {
                    ["Controller"] = "TelemetryController",
                    ["Action"] = "DemoTelemetry"
                });

            _logger.LogError(ex, "Error in telemetry demo");
            throw;
        }
    }

    /// <summary>
    /// POST: api/Telemetry/custom-event
    /// Track a custom event
    /// 
    /// Example Request:
    /// {
    ///   "eventName": "UserLogin",
    ///   "properties": {
    ///     "userId": "123",
    ///     "loginMethod": "OAuth"
    ///   }
    /// }
    /// </summary>
    [HttpPost("custom-event")]
    public IActionResult TrackCustomEvent([FromBody] CustomEventRequest request)
    {
        if (string.IsNullOrEmpty(request.EventName))
        {
            return BadRequest("EventName is required");
        }

        _telemetryClient.TrackEvent(request.EventName, request.Properties);
        
        return Ok(new
        {
            message = $"Event '{request.EventName}' tracked successfully",
            properties = request.Properties
        });
    }

    /// <summary>
    /// POST: api/Telemetry/custom-metric
    /// Track a custom metric
    /// 
    /// Example Request:
    /// {
    ///   "metricName": "OrdersProcessed",
    ///   "value": 150.5
    /// }
    /// </summary>
    [HttpPost("custom-metric")]
    public IActionResult TrackCustomMetric([FromBody] CustomMetricRequest request)
    {
        if (string.IsNullOrEmpty(request.MetricName))
        {
            return BadRequest("MetricName is required");
        }

        var cal = Calculate(3,4);

       // _telemetryClient.TrackMetric(request.MetricName, request.Value, request.Properties);
        
        return Ok(new
        {
            message = $"Metric '{request.MetricName}' = {request.Value} tracked successfully"
        });
    }

    private int Calculate(int a, int b)
    {
        var asf = a + b;
         return asf;
    }

    /// <summary>
    /// GET: api/Telemetry/performance-test
    /// Simulate different performance scenarios
    /// Interview Tip: Shows how App Insights tracks slow requests and dependencies
    /// </summary>
    [HttpGet("performance-test")]
    public async Task<IActionResult> PerformanceTest([FromQuery] int delayMs = 100)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Track operation
        using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("PerformanceTest"))
        {
            operation.Telemetry.Properties["DelayMs"] = delayMs.ToString();

            // Simulate work
            await Task.Delay(delayMs);

            // Track a dependency within the operation
            var dependencyStart = DateTime.UtcNow;
            await Task.Delay(50);
            _telemetryClient.TrackDependency(
                "Database",
                "SQL Azure",
                "SELECT * FROM Users",
                dependencyStart,
                DateTime.UtcNow - dependencyStart,
                success: true);

            operation.Telemetry.Success = true;
        }

        sw.Stop();

        return Ok(new
        {
            message = "Performance test completed",
            duration = $"{sw.ElapsedMilliseconds}ms",
            simulatedDelay = $"{delayMs}ms",
            tip = "Check Application Insights Performance blade for detailed timing"
        });
    }

    /// <summary>
    /// GET: api/Telemetry/error-test
    /// Trigger an error to demonstrate exception tracking
    /// Interview Tip: Shows automatic exception tracking and correlation
    /// </summary>
    [HttpGet("error-test")]
    public IActionResult ErrorTest([FromQuery] string? errorType = "handled")
    {
        try
        {
            if (errorType == "unhandled")
            {
                throw new InvalidOperationException("This is a simulated unhandled exception");
            }

            // Simulated handled exception
            try
            {
                var result = 10 / int.Parse("0"); // Division by zero
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex,
                    properties: new Dictionary<string, string>
                    {
                        ["ErrorType"] = "Handled",
                        ["TestScenario"] = "DivisionByZero"
                    });

                _logger.LogWarning(ex, "Handled exception in error test");
            }

            return Ok(new
            {
                message = "Handled exception tracked",
                tip = "Check Application Insights Failures blade to see exception details"
            });
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            _logger.LogError(ex, "Unhandled exception in error test");
            throw; // Let ASP.NET Core handle it
        }
    }
}

public class CustomEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public Dictionary<string, string>? Properties { get; set; }
}

public class CustomMetricRequest
{
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}
