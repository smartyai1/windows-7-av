using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Av.Quarantine;

public sealed class QuarantineService
{
    private const string VaultDirectoryName = "vault";

    private readonly string _root;
    private readonly string _vaultDirectory;
    private readonly ManifestRepository _manifest;
    private readonly AuditLogger _auditLogger;
    private readonly byte[] _encryptionKey;

    public QuarantineService(string rootDirectory, byte[] encryptionKey)
    {
        if (encryptionKey is null || encryptionKey.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException("Encryption key must be 16, 24, or 32 bytes.", nameof(encryptionKey));
        }

        _root = rootDirectory;
        _vaultDirectory = Path.Combine(_root, VaultDirectoryName);
        _encryptionKey = encryptionKey;
        Directory.CreateDirectory(_root);

        var manifestPath = Path.Combine(_root, "manifest.db");
        _manifest = new ManifestRepository(manifestPath);
        _auditLogger = new AuditLogger(Path.Combine(_root, "audit.log"));

        EnsureVaultDirectoryWithStrictAcls();
    }

    public string QuarantineFile(string sourcePath, string detectionName)
    {
        if (!File.Exists(sourcePath))
        {
            _auditLogger.Write("quarantine", "failed", null, $"Source file missing: {sourcePath}");
            throw new FileNotFoundException("File to quarantine was not found.", sourcePath);
        }

        var fileInfo = new FileInfo(sourcePath);
        var quarantineId = Guid.NewGuid().ToString("N");
        var quarantinedAt = DateTimeOffset.UtcNow;
        var vaultPath = Path.Combine(_vaultDirectory, quarantineId + ".qf");

        using var sourceStream = File.OpenRead(sourcePath);
        var plainHash = ComputeSha256(sourceStream);
        sourceStream.Position = 0;

        var iv = RandomNumberGenerator.GetBytes(16);
        EncryptToVault(sourceStream, vaultPath, iv);

        using var encryptedStream = File.OpenRead(vaultPath);
        var encryptedHash = ComputeSha256(encryptedStream);
        File.Delete(sourcePath);

        var record = new QuarantineRecord(
            Id: quarantineId,
            OriginalPath: sourcePath,
            VaultPath: vaultPath,
            OriginalSize: fileInfo.Length,
            OriginalCreatedUtc: fileInfo.CreationTimeUtc,
            OriginalModifiedUtc: fileInfo.LastWriteTimeUtc,
            QuarantinedAtUtc: quarantinedAt,
            DetectionName: detectionName,
            OriginalSha256: plainHash,
            EncryptedSha256: encryptedHash,
            EncryptionIvBase64: Convert.ToBase64String(iv),
            IsDeleted: false,
            RestoredAtUtc: null,
            DeletedAtUtc: null);

        _manifest.Upsert(record);
        _auditLogger.Write("quarantine", "success", quarantineId, $"Quarantined {sourcePath} as {vaultPath}");
        return quarantineId;
    }

    public string RestoreFile(string quarantineId, IUserConfirmation confirmation, string? restorePath = null)
    {
        var record = _manifest.Get(quarantineId)
            ?? throw new InvalidOperationException($"No quarantine record found for ID '{quarantineId}'.");

        if (record.IsDeleted)
        {
            _auditLogger.Write("restore", "failed", quarantineId, "Record already permanently deleted.");
            throw new InvalidOperationException("Cannot restore an entry that has been permanently deleted.");
        }

        var destinationPath = restorePath ?? record.OriginalPath;
        if (!confirmation.ConfirmRestore(record, destinationPath))
        {
            _auditLogger.Write("restore", "cancelled", quarantineId, "User rejected restore confirmation.");
            throw new OperationCanceledException("Restore cancelled by user confirmation policy.");
        }

        using var encryptedStream = File.OpenRead(record.VaultPath);
        var currentEncryptedHash = ComputeSha256(encryptedStream);
        if (!string.Equals(currentEncryptedHash, record.EncryptedSha256, StringComparison.OrdinalIgnoreCase))
        {
            _auditLogger.Write("restore", "failed", quarantineId, "Vault file integrity check failed.");
            throw new CryptographicException("Encrypted payload integrity validation failed.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");
        DecryptToDestination(record.VaultPath, destinationPath, Convert.FromBase64String(record.EncryptionIvBase64));

        using var restoredStream = File.OpenRead(destinationPath);
        var restoredHash = ComputeSha256(restoredStream);
        if (!string.Equals(restoredHash, record.OriginalSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destinationPath);
            _auditLogger.Write("restore", "failed", quarantineId, "Decrypted payload hash mismatch.");
            throw new CryptographicException("Restored file integrity validation failed.");
        }

        File.SetCreationTimeUtc(destinationPath, record.OriginalCreatedUtc.UtcDateTime);
        File.SetLastWriteTimeUtc(destinationPath, record.OriginalModifiedUtc.UtcDateTime);

        var updated = record with { RestoredAtUtc = DateTimeOffset.UtcNow };
        _manifest.Upsert(updated);

        _auditLogger.Write("restore", "success", quarantineId, $"Restored to {destinationPath}");
        return destinationPath;
    }

    public void PermanentlyDelete(string quarantineId)
    {
        var record = _manifest.Get(quarantineId)
            ?? throw new InvalidOperationException($"No quarantine record found for ID '{quarantineId}'.");

        if (File.Exists(record.VaultPath))
        {
            SecureDelete(record.VaultPath);
        }

        var updated = record with { IsDeleted = true, DeletedAtUtc = DateTimeOffset.UtcNow };
        _manifest.Upsert(updated);
        _auditLogger.Write("delete", "success", quarantineId, "Permanently deleted quarantined item.");
    }

    public int CleanupByRetention(TimeSpan retention)
    {
        var threshold = DateTimeOffset.UtcNow.Subtract(retention);
        var expired = _manifest.GetExpired(threshold);

        var deletedCount = 0;
        foreach (var record in expired)
        {
            try
            {
                PermanentlyDelete(record.Id);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _auditLogger.Write("retention_cleanup", "failed", record.Id, ex.Message);
            }
        }

        _auditLogger.Write("retention_cleanup", "success", null, $"Deleted {deletedCount} expired quarantine items.");
        return deletedCount;
    }

    private void EnsureVaultDirectoryWithStrictAcls()
    {
        if (!Directory.Exists(_vaultDirectory))
        {
            Directory.CreateDirectory(_vaultDirectory);
        }

        try
        {
            var security = new DirectorySecurity();
            var currentUser = WindowsIdentity.GetCurrent().User
                ?? throw new InvalidOperationException("Unable to identify current user SID.");
            security.SetOwner(currentUser);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            Directory.SetAccessControl(_vaultDirectory, security);

            _auditLogger.Write("vault_acl", "success", null, "Applied strict ACLs to quarantine vault.");
        }
        catch (PlatformNotSupportedException)
        {
            _auditLogger.Write("vault_acl", "warning", null, "ACL enforcement unavailable on current platform.");
        }
    }

    private void EncryptToVault(Stream plain, string vaultPath, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var destination = File.Create(vaultPath);
        using var crypto = new CryptoStream(destination, aes.CreateEncryptor(), CryptoStreamMode.Write);
        plain.CopyTo(crypto);
        crypto.FlushFinalBlock();
    }

    private void DecryptToDestination(string vaultPath, string destinationPath, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var source = File.OpenRead(vaultPath);
        using var crypto = new CryptoStream(source, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var destination = File.Create(destinationPath);
        crypto.CopyTo(destination);
    }

    private static string ComputeSha256(Stream stream)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static void SecureDelete(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            return;
        }

        info.IsReadOnly = false;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[8192];
            long remaining = stream.Length;
            while (remaining > 0)
            {
                var blockSize = (int)Math.Min(buffer.Length, remaining);
                RandomNumberGenerator.Fill(buffer.AsSpan(0, blockSize));
                stream.Write(buffer, 0, blockSize);
                remaining -= blockSize;
            }

            stream.Flush(flushToDisk: true);
        }

        File.Delete(path);
    }
}
