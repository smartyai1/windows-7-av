using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Av.Signatures;

/// <summary>
/// Canonical signed update package envelope.
/// </summary>
public sealed record SignedUpdatePackage(
    [property: JsonPropertyName("manifest")] UpdateManifest Manifest,
    [property: JsonPropertyName("signatureSet")] SignatureSet SignatureSet,
    [property: JsonPropertyName("detachedSignature")] string DetachedSignatureBase64);

/// <summary>
/// Versioned manifest describing all artifacts and integrity metadata.
/// </summary>
public sealed record UpdateManifest(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("packageVersion")] string PackageVersion,
    [property: JsonPropertyName("publishedAtUtc")] DateTimeOffset PublishedAtUtc,
    [property: JsonPropertyName("minimumClientVersion")] string MinimumClientVersion,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<UpdateArtifact> Artifacts,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata)
{
    public bool SupportsClientVersion(Version clientVersion)
    {
        var minimum = Version.Parse(MinimumClientVersion);
        return clientVersion >= minimum;
    }

    public void Validate()
    {
        if (SchemaVersion <= 0)
        {
            throw new InvalidOperationException("Manifest schemaVersion must be positive.");
        }

        if (string.IsNullOrWhiteSpace(PackageVersion))
        {
            throw new InvalidOperationException("Manifest packageVersion is required.");
        }

        if (Artifacts is null || Artifacts.Count == 0)
        {
            throw new InvalidOperationException("Manifest must contain at least one artifact.");
        }

        if (Artifacts.Any(a => string.IsNullOrWhiteSpace(a.Path) || string.IsNullOrWhiteSpace(a.Sha256)))
        {
            throw new InvalidOperationException("Each artifact must include path and sha256 hash.");
        }
    }
}

public sealed record UpdateArtifact(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes,
    [property: JsonPropertyName("sha256")] string Sha256);

/// <summary>
/// Signature bundle supporting key rotation and threshold verification.
/// </summary>
public sealed record SignatureSet(
    [property: JsonPropertyName("keysetVersion")] int KeysetVersion,
    [property: JsonPropertyName("requiredSignatures")] int RequiredSignatures,
    [property: JsonPropertyName("signatures")] IReadOnlyList<ManifestSignature> Signatures)
{
    public void Validate()
    {
        if (KeysetVersion <= 0)
        {
            throw new InvalidOperationException("Signature keysetVersion must be positive.");
        }

        if (RequiredSignatures <= 0)
        {
            throw new InvalidOperationException("Required signature threshold must be greater than zero.");
        }

        if (Signatures is null || Signatures.Count < RequiredSignatures)
        {
            throw new InvalidOperationException("Not enough signatures to satisfy the threshold.");
        }
    }
}

public sealed record ManifestSignature(
    [property: JsonPropertyName("keyId")] string KeyId,
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("value")] string ValueBase64);
