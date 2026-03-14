using Av.Core;

namespace Av.Quarantine;

public sealed class QuarantineWorkflow
{
    private readonly IComponentLogger _logger;
    private readonly ITelemetryCollector _telemetry;

    public QuarantineWorkflow(IComponentLogger logger, ITelemetryCollector telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public string MoveToQuarantine(Threat threat)
    {
        var quarantineId = $"q-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{threat.Id[..8]}";
        _logger.Log(LogLevel.Warning, "Quarantining threat", new Dictionary<string, object?>
        {
            ["threatId"] = threat.Id,
            ["quarantineId"] = quarantineId
        });

        _telemetry.TrackEvent("quarantine.item_created", new Dictionary<string, object?>
        {
            ["quarantineId"] = quarantineId,
            ["source"] = threat.SourcePath
        });

        return quarantineId;
    }

    public void RestoreFromQuarantine(string quarantineId, string destinationPath)
    {
        _logger.Log(LogLevel.Information, "Restoring quarantined item", new Dictionary<string, object?>
        {
            ["quarantineId"] = quarantineId,
            ["destinationPath"] = destinationPath
        });

        _telemetry.TrackEvent("quarantine.item_restored", new Dictionary<string, object?>
        {
            ["quarantineId"] = quarantineId
        });
    }
}
