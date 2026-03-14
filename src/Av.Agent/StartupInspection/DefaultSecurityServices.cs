using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Agent.StartupInspection;

public sealed class Sha256HashingService : IHashingService
{
    public async Task<string?> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

public sealed class AuthenticodeSignerReputationService : ISignerReputationService
{
    public Task<SignerReputation> CheckAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
            var signerName = cert.GetNameInfo(X509NameType.SimpleName, false);
            var isKnown = !string.IsNullOrWhiteSpace(signerName) &&
                          (signerName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                           signerName.Contains("Google", StringComparison.OrdinalIgnoreCase) ||
                           signerName.Contains("Adobe", StringComparison.OrdinalIgnoreCase));
            var reputation = isKnown ? 90 : 35;
            return Task.FromResult(new SignerReputation(signerName, isKnown, reputation));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(new SignerReputation(null, false, 0));
        }
    }
}

public sealed class FileQuarantineService(string quarantineRoot) : IQuarantineService
{
    public Task<RemediationResult> QuarantineAsync(string entryId, string filePath, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(quarantineRoot);
        var fileName = Path.GetFileName(filePath);
        var quarantinePath = Path.Combine(quarantineRoot, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}");
        File.Move(filePath, quarantinePath, overwrite: true);
        return Task.FromResult(new RemediationResult(true, $"Entry '{entryId}' target quarantined to '{quarantinePath}'."));
    }
}

public sealed class FileBackupRestoreService(string backupRoot) : IBackupService
{
    public Task<RemediationResult> RestoreAsync(string entryId, CancellationToken cancellationToken = default)
    {
        var backupFile = Path.Combine(backupRoot, Sanitize(entryId) + ".bak");
        if (!File.Exists(backupFile))
        {
            return Task.FromResult(new RemediationResult(false, $"No backup available for '{entryId}'."));
        }

        var restoreDestination = backupFile[..^4];
        File.Copy(backupFile, restoreDestination, overwrite: true);
        return Task.FromResult(new RemediationResult(true, $"Backup restored for '{entryId}' to '{restoreDestination}'."));
    }

    private static string Sanitize(string input) => input.Replace(':', '_').Replace('\\', '_').Replace('/', '_');
}
