using System;
using System.Collections.Generic;

namespace Av.Engine;

public sealed record BehaviorAssessment(
    string HostId,
    string ProcessName,
    int ProcessId,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    double Score,
    bool MeetsAlertThreshold,
    bool MeetsQuarantineThreshold,
    bool IsSuppressed,
    IReadOnlyCollection<string> ExplainableReasons,
    IReadOnlyCollection<BehaviorSignal> CorrelatedSignals);
