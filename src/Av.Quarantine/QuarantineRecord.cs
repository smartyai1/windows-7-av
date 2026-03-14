namespace Av.Quarantine;

public sealed record QuarantineRecord(
    string Id,
    string OriginalPath,
    string VaultPath,
    long OriginalSize,
    DateTimeOffset OriginalCreatedUtc,
    DateTimeOffset OriginalModifiedUtc,
    DateTimeOffset QuarantinedAtUtc,
    string DetectionName,
    string OriginalSha256,
    string EncryptedSha256,
    string EncryptionIvBase64,
    bool IsDeleted,
    DateTimeOffset? RestoredAtUtc,
    DateTimeOffset? DeletedAtUtc);
