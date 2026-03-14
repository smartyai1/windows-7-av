using Av.Engine;

namespace Av.Engine.Tests;

public class FileScannerTests
{
    [Fact]
    public async Task ScanAsync_EmitsDetectionEvents_ForBlacklistedHashesOnly()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var infectedFile = Path.Combine(root.FullName, "infected.bin");
            var cleanFile = Path.Combine(root.FullName, "clean.bin");
            var excludedDir = Path.Combine(root.FullName, "excluded");
            Directory.CreateDirectory(excludedDir);
            var excludedFile = Path.Combine(excludedDir, "ignored.bin");

            await File.WriteAllTextAsync(infectedFile, "evil-payload");
            await File.WriteAllTextAsync(cleanFile, "safe-payload");
            await File.WriteAllTextAsync(excludedFile, "evil-payload");

            var fileHasher = new FileHasher();
            var infectedHash = await fileHasher.ComputeSha256Async(infectedFile);
            var excludedHash = await fileHasher.ComputeSha256Async(excludedFile);

            var signatureStore = new HashSetSignatureStore(new[]
            {
                KeyValuePair.Create(infectedHash, new SignatureMetadata("BadSig", "Trojan", "Known-bad sample")),
                KeyValuePair.Create(excludedHash, new SignatureMetadata("BadSig2", "Worm", "Should be excluded by path"))
            });

            var scanner = new FileScanner(fileHasher, signatureStore);
            var options = new ScanOptions
            {
                AllowedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { infectedHash },
                ExcludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { excludedDir }
            };

            var detections = new List<DetectionEvent>();
            await scanner.ScanAsync(root.FullName, detection =>
            {
                detections.Add(detection);
                return Task.CompletedTask;
            }, options);

            Assert.Empty(detections);

            options.AllowedHashes.Clear();
            await scanner.ScanAsync(root.FullName, detection =>
            {
                detections.Add(detection);
                return Task.CompletedTask;
            }, options);

            var detected = Assert.Single(detections);
            Assert.Equal(infectedFile, detected.FilePath);
            Assert.Equal(infectedHash, detected.Hash);
            Assert.Equal("BadSig", detected.Signature.Name);
        }
        finally
        {
            root.Delete(true);
        }
    }
}
