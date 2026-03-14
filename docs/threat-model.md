# Threat model

## Security goals

- Detect and contain common commodity malware behavior with low false-negative tolerance.
- Prevent signature downgrade and tampering during update flows.
- Maintain auditability of detections, quarantine actions, and restore operations.

## Attack assumptions

The system assumes adversaries may:

- Execute arbitrary user-mode code under standard user or local admin context.
- Drop malicious binaries/scripts into writable paths.
- Attempt process injection and persistence mechanisms.
- Attempt man-in-the-middle attacks against signature update transport.
- Attempt to trigger denial-of-service by generating high alert volume.

The system does **not** currently assume trusted kernel integrity in this repository; kernel-level rootkits are considered out of direct prevention scope.

## Privilege model

- `Av.UI`: standard user privileges for read-only status views; elevated actions should require explicit consent.
- `Av.Agent`: service account with minimal privileges required for process/event observation.
- `Av.Engine`: library invoked by agent; no direct privileged operations.
- `Av.Quarantine`: write access only to dedicated quarantine storage locations; restore requires authorization checks.
- `Av.Signatures`: network and file permissions limited to update cache paths; cryptographic verification required before activation.
- `Av.Core`: shared contracts only, no privileged side effects.

## Trust and data flow notes

- Inputs from OS events, process metadata, files, and network responses are untrusted until validated.
- Signature trust depends on immutable root-of-trust keys/certificates and strict verification.
- Quarantine metadata should be append-only from an audit perspective.

## Fail-safe behavior

- If signature verification fails, reject update and keep last known-good signatures.
- If detection engine state is degraded, default to conservative policy (e.g., raise telemetry and block high-confidence behaviors when policy allows).
- If quarantine write fails, preserve original evidence path metadata and emit high-severity telemetry for operator action.
- If telemetry sink is unavailable, components continue core protection behavior while buffering or dropping non-critical telemetry.
- If UI is unavailable, background protection and update flows continue without user interaction.
