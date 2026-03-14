namespace Av.Core;

public sealed record Threat(
    string Id,
    string Name,
    ThreatSeverity Severity,
    string SourcePath,
    DateTimeOffset DetectedAtUtc,
    string DetectionEngine);

public enum ThreatSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical
}
