using System;
using System.IO;

namespace Av.Agent.StartupInspection;

public static class StartupRiskScorer
{
    public static (StartupRiskFactors factors, int score, string rationale) Score(StartupEntry entry, SignerReputation signer)
    {
        var factors = StartupRiskFactors.None;
        var score = 0;

        if (string.IsNullOrWhiteSpace(entry.ResolvedExecutablePath) || !File.Exists(entry.ResolvedExecutablePath))
        {
            factors |= StartupRiskFactors.MissingTarget;
            score += 30;
        }

        if (!signer.IsKnownSigner)
        {
            factors |= StartupRiskFactors.UnknownSigner;
            score += 25;
        }

        if (string.IsNullOrWhiteSpace(signer.SignerName))
        {
            factors |= StartupRiskFactors.UnsignedBinary;
            score += 20;
        }

        if (IsSuspiciousLocation(entry.ResolvedExecutablePath))
        {
            factors |= StartupRiskFactors.SuspiciousLocation;
            score += 30;
        }

        if (LooksTampered(entry.Command, entry.Arguments))
        {
            factors |= StartupRiskFactors.TamperingPattern;
            score += 35;
        }

        score += Math.Max(0, 30 - signer.Reputation);
        score = Math.Min(score, 100);

        var rationale = factors == StartupRiskFactors.None
            ? "No startup anomalies detected."
            : $"Flags: {factors}; signer reputation={signer.Reputation}.";

        return (factors, score, rationale);
    }

    private static bool IsSuspiciousLocation(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        return normalized.Contains("/appdata/local/temp/")
            || normalized.Contains("/windows/temp/")
            || normalized.Contains("/public/");
    }

    private static bool LooksTampered(string command, string? arguments)
    {
        var aggregate = $"{command} {arguments}".ToLowerInvariant();
        return aggregate.Contains("powershell -enc")
            || aggregate.Contains("cmd /c")
            || aggregate.Contains("wscript")
            || aggregate.Contains("mshta");
    }
}
