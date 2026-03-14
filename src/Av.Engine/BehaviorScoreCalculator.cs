using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Av.Engine;

public sealed class BehaviorScoreCalculator
{
    private readonly BehaviorRulesOptions _options;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAlertByFingerprint = new(StringComparer.OrdinalIgnoreCase);

    public BehaviorScoreCalculator(BehaviorRulesOptions options)
    {
        _options = options;
    }

    public BehaviorAssessment Evaluate(IEnumerable<BehaviorSignal> candidateSignals)
    {
        var ordered = candidateSignals.OrderBy(s => s.Timestamp).ToArray();
        if (ordered.Length == 0)
        {
            throw new ArgumentException("No behavior signals provided.", nameof(candidateSignals));
        }

        var hostId = ordered[0].HostId;
        var processName = ordered[0].ProcessName;
        var processId = ordered[0].ProcessId;

        var signalTypes = ordered.Select(x => x.SignalType).Distinct().ToArray();
        var windowStart = ordered[0].Timestamp;
        var windowEnd = ordered[^1].Timestamp;

        var weightedScore = ordered.Sum(signal =>
        {
            var weight = _options.SignalWeights.GetValueOrDefault(signal.SignalType, 0.10d);
            return weight * signal.Severity;
        });

        // Clamp to [0, 1] range to keep thresholds predictable.
        var normalizedScore = Math.Clamp(weightedScore, 0.0d, 1.0d);

        var reasons = new List<string>();
        foreach (var type in signalTypes)
        {
            var count = ordered.Count(s => s.SignalType == type);
            reasons.Add($"{type}: {count} event(s) in correlation window");
        }

        if (signalTypes.Length >= _options.MinimumDistinctSignalTypesForChain)
        {
            reasons.Add($"Multi-step chain detected with {signalTypes.Length} distinct behavior stages.");
            normalizedScore = Math.Clamp(normalizedScore + 0.10d, 0.0d, 1.0d);
        }

        if (ordered.Any(s => _options.TrustedProcessAllowList.Contains(s.ProcessName)))
        {
            reasons.Add("Trusted process allow-list matched; reducing confidence.");
            normalizedScore = Math.Clamp(normalizedScore - 0.20d, 0.0d, 1.0d);
        }

        var fingerprint = BuildFingerprint(hostId, processId, signalTypes);
        var now = DateTimeOffset.UtcNow;
        var isSuppressed = false;
        if (_lastAlertByFingerprint.TryGetValue(fingerprint, out var lastRaisedAt) &&
            now - lastRaisedAt < _options.DuplicateSuppressionWindow)
        {
            isSuppressed = true;
            reasons.Add($"Suppressed duplicate alert in {_options.DuplicateSuppressionWindow.TotalMinutes:N0}-minute window.");
        }
        else
        {
            _lastAlertByFingerprint[fingerprint] = now;
        }

        var meetsAlertThreshold = normalizedScore >= _options.AlertThreshold;
        var meetsQuarantineThreshold = normalizedScore >= _options.QuarantineThreshold;

        return new BehaviorAssessment(
            hostId,
            processName,
            processId,
            windowStart,
            windowEnd,
            normalizedScore,
            meetsAlertThreshold,
            meetsQuarantineThreshold,
            isSuppressed,
            reasons,
            ordered);
    }

    private static string BuildFingerprint(string hostId, int processId, IEnumerable<BehaviorSignalType> signalTypes)
    {
        var signature = string.Join("|", signalTypes.OrderBy(type => type).Select(type => type.ToString()));
        return $"{hostId}:{processId}:{signature}";
    }
}
