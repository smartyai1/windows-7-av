namespace Av.Core;

public interface IComponentLogger
{
    void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? fields = null);
}

public interface ITelemetryCollector
{
    void TrackEvent(string name, IReadOnlyDictionary<string, object?>? properties = null);
    void TrackMetric(string name, double value, IReadOnlyDictionary<string, object?>? properties = null);
}

public interface IExecutionContext
{
    string Component { get; }
    string CorrelationId { get; }
}

public sealed class NoOpTelemetryCollector : ITelemetryCollector
{
    public void TrackEvent(string name, IReadOnlyDictionary<string, object?>? properties = null)
    {
    }

    public void TrackMetric(string name, double value, IReadOnlyDictionary<string, object?>? properties = null)
    {
    }
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
