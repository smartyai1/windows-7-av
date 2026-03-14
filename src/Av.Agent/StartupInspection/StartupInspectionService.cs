using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Agent.StartupInspection;

public sealed class StartupInspectionService(
    IStartupEntriesSource source,
    IExecutableResolver executableResolver,
    IHashingService hashingService,
    ISignerReputationService signerReputationService,
    IQuarantineService quarantineService,
    IBackupService backupService) : IStartupInspectionService
{
    private readonly Dictionary<string, StartupFinding> _lastScan = new(StringComparer.OrdinalIgnoreCase);

    public async Task<StartupInspectionReport> ScanAsync(CancellationToken cancellationToken = default)
    {
        var entries = await source.EnumerateAsync(cancellationToken);
        var findings = new List<StartupFinding>(entries.Count);

        foreach (var rawEntry in entries)
        {
            var (resolvedPath, args) = executableResolver.Resolve(rawEntry.Command);
            var entry = rawEntry with { ResolvedExecutablePath = resolvedPath, Arguments = args };

            string? sha256 = null;
            SignerReputation signer = new(null, false, 0);

            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
            {
                sha256 = await hashingService.ComputeSha256Async(resolvedPath, cancellationToken);
                signer = await signerReputationService.CheckAsync(resolvedPath, cancellationToken);
            }

            var (factors, score, rationale) = StartupRiskScorer.Score(entry, signer);
            var finding = new StartupFinding(entry, sha256, signer.SignerName, signer.IsKnownSigner, signer.Reputation, factors, score, rationale);

            findings.Add(finding);
            _lastScan[entry.Id] = finding;
        }

        return new StartupInspectionReport(DateTimeOffset.UtcNow, findings.OrderByDescending(static f => f.RiskScore).ToList());
    }

    public async Task<RemediationResult> DisableEntryAsync(string entryId, CancellationToken cancellationToken = default)
    {
        var disabled = await source.DisableAsync(entryId, cancellationToken);
        return disabled
            ? new RemediationResult(true, $"Startup entry '{entryId}' disabled.")
            : new RemediationResult(false, $"Unable to disable startup entry '{entryId}'.");
    }

    public Task<RemediationResult> QuarantineTargetAsync(string entryId, CancellationToken cancellationToken = default)
    {
        if (!_lastScan.TryGetValue(entryId, out var finding) || string.IsNullOrWhiteSpace(finding.Entry.ResolvedExecutablePath))
        {
            return Task.FromResult(new RemediationResult(false, $"No resolved target found for '{entryId}'. Scan first."));
        }

        return quarantineService.QuarantineAsync(entryId, finding.Entry.ResolvedExecutablePath, cancellationToken);
    }

    public Task<RemediationResult> RestoreBackupAsync(string entryId, CancellationToken cancellationToken = default)
    {
        return backupService.RestoreAsync(entryId, cancellationToken);
    }
}
