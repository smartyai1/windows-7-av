namespace Av.Engine;

public sealed class FileScanner
{
    private readonly IFileHasher _fileHasher;
    private readonly ISignatureStore _signatureStore;

    public FileScanner(IFileHasher fileHasher, ISignatureStore signatureStore)
    {
        _fileHasher = fileHasher ?? throw new ArgumentNullException(nameof(fileHasher));
        _signatureStore = signatureStore ?? throw new ArgumentNullException(nameof(signatureStore));
    }

    public async Task ScanAsync(
        string rootPath,
        Func<DetectionEvent, Task> onDetection,
        ScanOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(onDetection);

        options ??= new ScanOptions();

        foreach (var filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (options.IsPathExcluded(filePath))
            {
                continue;
            }

            var hash = await _fileHasher.ComputeSha256Async(filePath, cancellationToken);
            if (options.IsHashAllowed(hash))
            {
                continue;
            }

            if (_signatureStore.TryGet(hash, out var signature))
            {
                await onDetection(new DetectionEvent(filePath, hash, signature));
            }
        }
    }
}
