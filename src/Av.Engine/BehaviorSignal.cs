using System;
using System.Collections.Generic;

namespace Av.Engine;

public enum BehaviorSignalType
{
    ProcessCreation,
    ModuleInjectionIndicator,
    SuspiciousChildProcessTree,
    ScriptInterpreterAbuse,
    PrivilegeEscalationHint,
    DefenseEvasionHint,
    LateralMovementHint,
    PersistenceHint
}

public sealed record BehaviorSignal(
    Guid EventId,
    DateTimeOffset Timestamp,
    string HostId,
    string ProcessName,
    int ProcessId,
    int? ParentProcessId,
    BehaviorSignalType SignalType,
    double Severity,
    string Summary,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static BehaviorSignal Create(
        string hostId,
        string processName,
        int processId,
        int? parentProcessId,
        BehaviorSignalType signalType,
        double severity,
        string summary,
        IReadOnlyDictionary<string, string>? metadata = null,
        DateTimeOffset? timestamp = null)
    {
        return new BehaviorSignal(
            Guid.NewGuid(),
            timestamp ?? DateTimeOffset.UtcNow,
            hostId,
            processName,
            processId,
            parentProcessId,
            signalType,
            Math.Clamp(severity, 0.0d, 1.0d),
            summary,
            metadata ?? new Dictionary<string, string>());
    }
}
