namespace Av.Engine;

public sealed class HashSetSignatureStore : ISignatureStore
{
    private readonly Dictionary<string, SignatureMetadata> _signatures;

    public HashSetSignatureStore(IEnumerable<KeyValuePair<string, SignatureMetadata>> signatures)
    {
        ArgumentNullException.ThrowIfNull(signatures);
        _signatures = new Dictionary<string, SignatureMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hash, metadata) in signatures)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                continue;
            }

            _signatures[hash] = metadata;
        }
    }

    public bool TryGet(string hash, out SignatureMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return _signatures.TryGetValue(hash, out metadata!);
    }
}
