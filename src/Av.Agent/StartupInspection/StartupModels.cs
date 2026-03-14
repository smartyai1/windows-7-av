using System;
using System.Collections.Generic;

namespace Av.Agent.StartupInspection;

public enum StartupEntryType
{
    Run,
    RunOnce,
    StartupFolder,
    ScheduledTask,
    Service
}

[Flags]
public enum StartupRiskFactors
{
    None = 0,
    UnknownSigner = 1,
    SuspiciousLocation = 2,
    TamperingPattern = 4,
    MissingTarget = 8,
    UnsignedBinary = 16
}

public sealed record StartupEntry(
    string Id,
    StartupEntryType Type,
    string Name,
    string Source,
    string Command,
    string? ResolvedExecutablePath,
    string? Arguments);

public sealed record StartupFinding(
    StartupEntry Entry,
    string? Sha256,
    string? Signer,
    bool IsSignerKnown,
    int Reputation,
    StartupRiskFactors RiskFactors,
    int RiskScore,
    string Rationale);

public sealed record StartupInspectionReport(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StartupFinding> Findings);

public sealed record RemediationResult(bool Succeeded, string Message);
