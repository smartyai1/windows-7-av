namespace Av.Engine;

public interface IFileHasher
{
    ValueTask<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default);
}
