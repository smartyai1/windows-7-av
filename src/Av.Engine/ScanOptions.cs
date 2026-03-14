namespace Av.Engine;

public sealed class ScanOptions
{
    public ISet<string> AllowedHashes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public ISet<string> ExcludedPaths { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsPathExcluded(string path)
    {
        foreach (var excludedPath in ExcludedPaths)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsHashAllowed(string hash) => AllowedHashes.Contains(hash);
}
