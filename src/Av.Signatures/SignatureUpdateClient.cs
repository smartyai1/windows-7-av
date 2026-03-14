using Av.Core;

namespace Av.Signatures;

public sealed class SignatureUpdateClient
{
    private readonly IComponentLogger _logger;
    private readonly ITelemetryCollector _telemetry;

    public SignatureUpdateClient(IComponentLogger logger, ITelemetryCollector telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public bool VerifySignatureBundle(string bundleHash, string trustedHash)
    {
        var isTrusted = string.Equals(bundleHash, trustedHash, StringComparison.OrdinalIgnoreCase);

        _logger.Log(isTrusted ? LogLevel.Information : LogLevel.Error, "Signature bundle verification finished", new Dictionary<string, object?>
        {
            ["isTrusted"] = isTrusted
        });

        _telemetry.TrackEvent("signatures.bundle_verified", new Dictionary<string, object?>
        {
            ["isTrusted"] = isTrusted
        });

        return isTrusted;
    }
}
