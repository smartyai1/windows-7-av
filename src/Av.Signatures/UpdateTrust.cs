using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Av.Signatures;

public interface ITrustStore
{
    bool TryGetPublicKey(string keyId, out ECDsa? publicKey);
}

public sealed class InMemoryTrustStore : ITrustStore
{
    private readonly IReadOnlyDictionary<string, ECDsa> _keys;

    public InMemoryTrustStore(IReadOnlyDictionary<string, ECDsa> keys)
    {
        _keys = keys;
    }

    public bool TryGetPublicKey(string keyId, out ECDsa? publicKey)
    {
        return _keys.TryGetValue(keyId, out publicKey);
    }
}

public sealed class PackageSignatureValidator
{
    private readonly ITrustStore _trustStore;

    public PackageSignatureValidator(ITrustStore trustStore)
    {
        _trustStore = trustStore;
    }

    public void Validate(SignedUpdatePackage package)
    {
        package.Manifest.Validate();
        package.SignatureSet.Validate();

        var manifestPayload = JsonSerializer.SerializeToUtf8Bytes(package.Manifest);
        var validSignatures = 0;

        foreach (var signature in package.SignatureSet.Signatures)
        {
            if (!_trustStore.TryGetPublicKey(signature.KeyId, out var publicKey) || publicKey is null)
            {
                continue;
            }

            if (!string.Equals(signature.Algorithm, "ECDSA_P256_SHA256", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var signedBytes = Convert.FromBase64String(signature.ValueBase64);
            if (publicKey.VerifyData(manifestPayload, signedBytes, HashAlgorithmName.SHA256))
            {
                validSignatures++;
            }
        }

        if (validSignatures < package.SignatureSet.RequiredSignatures)
        {
            throw new CryptographicException("Signature threshold check failed for manifest.");
        }

        ValidateDetachedSignature(package, manifestPayload);
    }

    private static void ValidateDetachedSignature(SignedUpdatePackage package, byte[] manifestPayload)
    {
        var detachedSignature = Convert.FromBase64String(package.DetachedSignatureBase64);
        var expectedDigest = SHA256.HashData(manifestPayload);
        if (!CryptographicOperations.FixedTimeEquals(detachedSignature, expectedDigest))
        {
            throw new CryptographicException("Detached signature (manifest digest) mismatch.");
        }
    }
}

public sealed class TlsPinningValidator
{
    private readonly HashSet<string> _allowedSpkiPins;

    public TlsPinningValidator(IEnumerable<string> allowedSpkiPins)
    {
        _allowedSpkiPins = new HashSet<string>(allowedSpkiPins, StringComparer.OrdinalIgnoreCase);
    }

    public bool ValidateCertificate(X509Certificate2? certificate)
    {
        if (certificate is null)
        {
            return false;
        }

        using var sha256 = SHA256.Create();
        var spki = certificate.ExportSubjectPublicKeyInfo();
        var pin = Convert.ToBase64String(sha256.ComputeHash(spki));
        return _allowedSpkiPins.Contains(pin);
    }
}

public sealed class SnapshotManager
{
    private readonly string _root;

    public SnapshotManager(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public string CurrentPath => Path.Combine(_root, "current");
    public string LastKnownGoodPath => Path.Combine(_root, "lkg");

    public void PromoteCurrentToLastKnownGood()
    {
        if (!Directory.Exists(CurrentPath))
        {
            return;
        }

        if (Directory.Exists(LastKnownGoodPath))
        {
            Directory.Delete(LastKnownGoodPath, recursive: true);
        }

        DirectoryCopy(CurrentPath, LastKnownGoodPath);
    }

    public void RollbackToLastKnownGood()
    {
        if (!Directory.Exists(LastKnownGoodPath))
        {
            throw new InvalidOperationException("No last-known-good snapshot available.");
        }

        if (Directory.Exists(CurrentPath))
        {
            Directory.Delete(CurrentPath, recursive: true);
        }

        DirectoryCopy(LastKnownGoodPath, CurrentPath);
    }

    public bool IsCorrupted(UpdateManifest manifest)
    {
        foreach (var artifact in manifest.Artifacts)
        {
            var fullPath = Path.Combine(CurrentPath, artifact.Path);
            if (!File.Exists(fullPath))
            {
                return true;
            }

            using var stream = File.OpenRead(fullPath);
            var hash = SHA256.HashData(stream);
            var actual = Convert.ToHexString(hash).ToLowerInvariant();
            if (!string.Equals(actual, artifact.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName)
    {
        var dir = new DirectoryInfo(sourceDirName);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirName}");
        }

        Directory.CreateDirectory(destDirName);

        foreach (var file in dir.GetFiles())
        {
            file.CopyTo(Path.Combine(destDirName, file.Name), overwrite: true);
        }

        foreach (var subdir in dir.GetDirectories())
        {
            DirectoryCopy(subdir.FullName, Path.Combine(destDirName, subdir.Name));
        }
    }
}
