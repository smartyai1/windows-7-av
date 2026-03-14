using Av.Core;
using Av.Engine;

namespace Av.Agent;

public sealed class MonitorService
{
    private readonly BehaviorScanner _scanner;
    private readonly IComponentLogger _logger;

    public MonitorService(BehaviorScanner scanner, IComponentLogger logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public Threat? InspectProcess(string processName, IReadOnlyList<string> indicators)
    {
        _logger.Log(LogLevel.Information, "Inspecting process", new Dictionary<string, object?>
        {
            ["processName"] = processName
        });

        var threat = _scanner.AnalyzeProcess(processName, indicators);
        if (threat is not null)
        {
            _logger.Log(LogLevel.Warning, "Threat detected by monitor service", new Dictionary<string, object?>
            {
                ["threatId"] = threat.Id,
                ["severity"] = threat.Severity.ToString()
            });
        }

        return threat;
    }
}
