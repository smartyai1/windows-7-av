using System.Text.Json;

namespace Av.Quarantine;

public sealed class AuditLogger
{
    private readonly string _logPath;
    private readonly object _sync = new();

    public AuditLogger(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath) ?? ".");
    }

    public void Write(string operation, string outcome, string? quarantineId, string details)
    {
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            operation,
            outcome,
            quarantineId,
            details
        };

        var line = JsonSerializer.Serialize(entry);
        lock (_sync)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }
}
