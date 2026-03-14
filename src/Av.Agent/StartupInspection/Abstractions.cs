using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Agent.StartupInspection;

public interface IStartupInspectionService
{
    Task<StartupInspectionReport> ScanAsync(CancellationToken cancellationToken = default);
    Task<RemediationResult> DisableEntryAsync(string entryId, CancellationToken cancellationToken = default);
    Task<RemediationResult> QuarantineTargetAsync(string entryId, CancellationToken cancellationToken = default);
    Task<RemediationResult> RestoreBackupAsync(string entryId, CancellationToken cancellationToken = default);
}

public interface IStartupEntriesSource
{
    Task<IReadOnlyList<StartupEntry>> EnumerateAsync(CancellationToken cancellationToken = default);
    Task<bool> DisableAsync(string entryId, CancellationToken cancellationToken = default);
}

public interface IExecutableResolver
{
    (string? path, string? arguments) Resolve(string command);
}

public interface IHashingService
{
    Task<string?> ComputeSha256Async(string path, CancellationToken cancellationToken = default);
}

public sealed record SignerReputation(string? SignerName, bool IsKnownSigner, int Reputation);

public interface ISignerReputationService
{
    Task<SignerReputation> CheckAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IQuarantineService
{
    Task<RemediationResult> QuarantineAsync(string entryId, string filePath, CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task<RemediationResult> RestoreAsync(string entryId, CancellationToken cancellationToken = default);
}
