using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Signatures;

public interface IUpdateTelemetry
{
    void EmitAttempt(int attempt, TimeSpan delay);
    void EmitSuccess(string packageVersion, TimeSpan duration);
    void EmitFailure(string reason, Exception exception);
    void EmitRollback(string packageVersion, string reason);
    void EmitHealth(bool healthy, string details);
}

public sealed class UpdateScheduler
{
    private readonly TimeSpan _interval;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseBackoff;

    public UpdateScheduler(TimeSpan interval, int maxRetries, TimeSpan baseBackoff)
    {
        _interval = interval;
        _maxRetries = maxRetries;
        _baseBackoff = baseBackoff;
    }

    public async Task RunAsync(Func<CancellationToken, Task> updateWork, IUpdateTelemetry telemetry, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await ExecuteWithRetryAsync(updateWork, telemetry, ct).ConfigureAwait(false);
            await Task.Delay(_interval, ct).ConfigureAwait(false);
        }
    }

    private async Task ExecuteWithRetryAsync(Func<CancellationToken, Task> updateWork, IUpdateTelemetry telemetry, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            var delay = TimeSpan.FromMilliseconds(_baseBackoff.TotalMilliseconds * Math.Pow(2, attempt - 1));
            telemetry.EmitAttempt(attempt, delay);

            try
            {
                await updateWork(ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                telemetry.EmitFailure($"attempt-{attempt}", ex);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }
}

public sealed class UpdateOrchestrator
{
    private readonly UpdaterClient _client;
    private readonly PackageSignatureValidator _signatureValidator;
    private readonly SnapshotManager _snapshotManager;
    private readonly IUpdateTelemetry _telemetry;

    public UpdateOrchestrator(
        UpdaterClient client,
        PackageSignatureValidator signatureValidator,
        SnapshotManager snapshotManager,
        IUpdateTelemetry telemetry)
    {
        _client = client;
        _signatureValidator = signatureValidator;
        _snapshotManager = snapshotManager;
        _telemetry = telemetry;
    }

    public async Task ApplyUpdateAsync(string manifestPath, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;
        var package = await _client.GetUpdatePackageAsync(manifestPath, ct).ConfigureAwait(false);

        _signatureValidator.Validate(package);

        _snapshotManager.PromoteCurrentToLastKnownGood();

        try
        {
            foreach (var artifact in package.Manifest.Artifacts)
            {
                var targetPath = Path.Combine(_snapshotManager.CurrentPath, artifact.Path);
                await _client.DownloadArtifactAsync(artifact.Path, targetPath, ct).ConfigureAwait(false);
            }

            var corrupted = _snapshotManager.IsCorrupted(package.Manifest);
            if (corrupted)
            {
                _telemetry.EmitHealth(false, "Corruption detected during integrity verification.");
                _snapshotManager.RollbackToLastKnownGood();
                _telemetry.EmitRollback(package.Manifest.PackageVersion, "corruption-detected");
                throw new InvalidDataException("Update artifacts failed integrity checks.");
            }

            _telemetry.EmitHealth(true, "Update applied and verified.");
            _telemetry.EmitSuccess(package.Manifest.PackageVersion, DateTimeOffset.UtcNow - started);
        }
        catch (Exception ex)
        {
            _snapshotManager.RollbackToLastKnownGood();
            _telemetry.EmitRollback(package.Manifest.PackageVersion, "apply-exception");
            _telemetry.EmitFailure("apply-update", ex);
            throw;
        }
    }
}
