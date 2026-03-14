using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Av.Engine;

namespace Av.Agent;

public sealed class ProcessEventCollector
{
    private static readonly string[] ScriptInterpreters =
    [
        "powershell.exe", "pwsh.exe", "wscript.exe", "cscript.exe", "mshta.exe", "rundll32.exe"
    ];

    private readonly BehaviorRulesOptions _rules;
    private readonly BehaviorScoreCalculator _scoreCalculator;
    private readonly IUiAlertSink _uiAlertSink;
    private readonly IQuarantineSink _quarantineSink;

    private readonly ConcurrentDictionary<string, List<BehaviorSignal>> _signalBuffer = new(StringComparer.OrdinalIgnoreCase);

    public ProcessEventCollector(
        BehaviorRulesOptions rules,
        BehaviorScoreCalculator scoreCalculator,
        IUiAlertSink uiAlertSink,
        IQuarantineSink quarantineSink)
    {
        _rules = rules;
        _scoreCalculator = scoreCalculator;
        _uiAlertSink = uiAlertSink;
        _quarantineSink = quarantineSink;
    }

    public async Task IngestAsync(ProcessTelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
    {
        var derivedSignals = DeriveSignals(telemetryEvent);
        if (derivedSignals.Count == 0)
        {
            return;
        }

        var processKey = BuildProcessKey(telemetryEvent.HostId, telemetryEvent.ProcessId);
        var allSignals = _signalBuffer.GetOrAdd(processKey, _ => new List<BehaviorSignal>());

        lock (allSignals)
        {
            allSignals.AddRange(derivedSignals);
            PruneOldSignals(allSignals, telemetryEvent.Timestamp);
        }

        BehaviorAssessment? assessment = null;
        lock (allSignals)
        {
            var correlated = allSignals
                .Where(s => telemetryEvent.Timestamp - s.Timestamp <= _rules.CorrelationWindow)
                .OrderBy(s => s.Timestamp)
                .ToList();

            if (correlated.Count > 0)
            {
                assessment = _scoreCalculator.Evaluate(correlated);
            }
        }

        if (assessment is null || assessment.IsSuppressed || !assessment.MeetsAlertThreshold)
        {
            return;
        }

        await _uiAlertSink.PublishBehaviorAlertAsync(assessment, cancellationToken).ConfigureAwait(false);

        if (assessment.MeetsQuarantineThreshold)
        {
            await _quarantineSink.RequestQuarantineAsync(assessment, cancellationToken).ConfigureAwait(false);
        }
    }

    private List<BehaviorSignal> DeriveSignals(ProcessTelemetryEvent processEvent)
    {
        var signals = new List<BehaviorSignal>();

        if (processEvent.EventType == ProcessTelemetryEventType.ProcessCreated)
        {
            signals.Add(BehaviorSignal.Create(
                processEvent.HostId,
                processEvent.ProcessName,
                processEvent.ProcessId,
                processEvent.ParentProcessId,
                BehaviorSignalType.ProcessCreation,
                0.30d,
                "New process created."));
        }

        if (processEvent.EventType == ProcessTelemetryEventType.ModuleLoaded &&
            processEvent.Metadata.TryGetValue("modulePath", out var modulePath) &&
            IsSuspiciousInjectionModule(modulePath))
        {
            signals.Add(BehaviorSignal.Create(
                processEvent.HostId,
                processEvent.ProcessName,
                processEvent.ProcessId,
                processEvent.ParentProcessId,
                BehaviorSignalType.ModuleInjectionIndicator,
                0.95d,
                $"Potential module injection indicator: {modulePath}",
                processEvent.Metadata));
        }

        if (processEvent.EventType == ProcessTelemetryEventType.ChildProcessSpawned &&
            LooksLikeSuspiciousChildTree(processEvent))
        {
            signals.Add(BehaviorSignal.Create(
                processEvent.HostId,
                processEvent.ProcessName,
                processEvent.ProcessId,
                processEvent.ParentProcessId,
                BehaviorSignalType.SuspiciousChildProcessTree,
                0.85d,
                "Unusual parent-child process chain observed.",
                processEvent.Metadata));
        }

        if (IsScriptInterpreterAbuse(processEvent))
        {
            signals.Add(BehaviorSignal.Create(
                processEvent.HostId,
                processEvent.ProcessName,
                processEvent.ProcessId,
                processEvent.ParentProcessId,
                BehaviorSignalType.ScriptInterpreterAbuse,
                0.90d,
                "Script interpreter abuse pattern matched.",
                processEvent.Metadata));
        }

        return signals;
    }

    private static bool IsSuspiciousInjectionModule(string modulePath)
    {
        return modulePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
               || modulePath.Contains("\\appdata\\", StringComparison.OrdinalIgnoreCase)
               || modulePath.Contains("reflective", StringComparison.OrdinalIgnoreCase)
               || modulePath.Contains("manualmap", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeSuspiciousChildTree(ProcessTelemetryEvent processEvent)
    {
        if (!processEvent.Metadata.TryGetValue("parentName", out var parentName) ||
            !processEvent.Metadata.TryGetValue("childName", out var childName))
        {
            return false;
        }

        return (parentName.Equals("winword.exe", StringComparison.OrdinalIgnoreCase) &&
                (childName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase) || childName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)))
               || (parentName.Equals("excel.exe", StringComparison.OrdinalIgnoreCase) && childName.Equals("wscript.exe", StringComparison.OrdinalIgnoreCase))
               || (parentName.Equals("outlook.exe", StringComparison.OrdinalIgnoreCase) && childName.Equals("mshta.exe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsScriptInterpreterAbuse(ProcessTelemetryEvent processEvent)
    {
        var isInterpreter = ScriptInterpreters.Any(x => x.Equals(processEvent.ProcessName, StringComparison.OrdinalIgnoreCase));
        if (!isInterpreter)
        {
            return false;
        }

        var commandLine = processEvent.CommandLine ?? string.Empty;
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        return commandLine.Contains("-enc", StringComparison.OrdinalIgnoreCase)
               || commandLine.Contains("frombase64string", StringComparison.OrdinalIgnoreCase)
               || commandLine.Contains("invoke-expression", StringComparison.OrdinalIgnoreCase)
               || commandLine.Contains("downloadstring", StringComparison.OrdinalIgnoreCase)
               || commandLine.Contains("http://", StringComparison.OrdinalIgnoreCase)
               || commandLine.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    private void PruneOldSignals(List<BehaviorSignal> allSignals, DateTimeOffset now)
    {
        allSignals.RemoveAll(signal => now - signal.Timestamp > _rules.CorrelationWindow);
    }

    private static string BuildProcessKey(string hostId, int processId)
    {
        return $"{hostId}:{processId}";
    }
}
