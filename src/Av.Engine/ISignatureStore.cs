namespace Av.Engine;

public interface ISignatureStore
{
    bool TryGet(string hash, out SignatureMetadata metadata);
}
