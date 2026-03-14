using System;
using System.Collections.Generic;

namespace Av.Engine;

public sealed class BehaviorRulesOptions
{
    public Dictionary<BehaviorSignalType, double> SignalWeights { get; } = new()
    {
        [BehaviorSignalType.ProcessCreation] = 0.05d,
        [BehaviorSignalType.ModuleInjectionIndicator] = 0.30d,
        [BehaviorSignalType.SuspiciousChildProcessTree] = 0.25d,
        [BehaviorSignalType.ScriptInterpreterAbuse] = 0.20d,
        [BehaviorSignalType.PrivilegeEscalationHint] = 0.20d,
        [BehaviorSignalType.DefenseEvasionHint] = 0.20d,
        [BehaviorSignalType.LateralMovementHint] = 0.20d,
        [BehaviorSignalType.PersistenceHint] = 0.15d,
    };

    public TimeSpan CorrelationWindow { get; set; } = TimeSpan.FromMinutes(7);

    public double AlertThreshold { get; set; } = 0.65d;

    public double QuarantineThreshold { get; set; } = 0.80d;

    public int MinimumDistinctSignalTypesForChain { get; set; } = 3;

    public TimeSpan DuplicateSuppressionWindow { get; set; } = TimeSpan.FromMinutes(10);

    public HashSet<string> TrustedProcessAllowList { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "msiexec.exe",
        "trustedinstaller.exe"
    };
}
