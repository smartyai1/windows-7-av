namespace Av.Engine;

public sealed record DetectionEvent(string FilePath, string Hash, SignatureMetadata Signature);
