using Av.Core;

namespace Av.Engine;

public sealed class BehaviorScanner
{
    private readonly IComponentLogger _logger;
    private readonly ITelemetryCollector _telemetry;

    public BehaviorScanner(IComponentLogger logger, ITelemetryCollector telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public Threat? AnalyzeProcess(string processName, IReadOnlyList<string> indicators)
    {
        _logger.Log(LogLevel.Debug, "Analyzing process behavior", new Dictionary<string, object?>
        {
            ["processName"] = processName,
            ["indicatorCount"] = indicators.Count
        });

        _telemetry.TrackMetric("engine.indicator_count", indicators.Count, new Dictionary<string, object?>
        {
            ["processName"] = processName
        });

        if (indicators.Contains("InjectRemoteThread"))
        {
            _telemetry.TrackEvent("engine.threat_detected", new Dictionary<string, object?>
            {
                ["processName"] = processName,
                ["signal"] = "InjectRemoteThread"
            });

            return new Threat(
                Id: Guid.NewGuid().ToString("N"),
                Name: "Behavioral process injection",
                Severity: ThreatSeverity.High,
                SourcePath: processName,
                DetectedAtUtc: DateTimeOffset.UtcNow,
                DetectionEngine: "BehavioralHeuristics");
        }

        return null;
    }
}
