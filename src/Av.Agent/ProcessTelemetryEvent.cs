using System;
using System.Collections.Generic;

namespace Av.Agent;

public enum ProcessTelemetryEventType
{
    ProcessCreated,
    ModuleLoaded,
    ChildProcessSpawned,
    CommandLineObserved
}

public sealed record ProcessTelemetryEvent(
    DateTimeOffset Timestamp,
    string HostId,
    int ProcessId,
    int? ParentProcessId,
    string ProcessName,
    string? CommandLine,
    ProcessTelemetryEventType EventType,
    IReadOnlyDictionary<string, string> Metadata);
