# windows-7-av

Prototype multi-project Windows antivirus solution organized as a .NET solution.

## Solution layout

- `src/Av.Core`: shared domain models, interfaces, and logging/telemetry abstractions.
- `src/Av.Engine`: behavioral and signature-driven analysis pipeline.
- `src/Av.Agent`: service-oriented process monitor that orchestrates scan requests.
- `src/Av.UI`: Windows Defender-style desktop interface (WPF) for local operator workflows.
- `src/Av.Quarantine`: quarantine storage, tracking, and controlled restore workflow.
- `src/Av.Signatures`: signature update client and verification logic.

## Architecture overview

1. **Collection and orchestration**: `Av.Agent` watches processes/events and forwards normalized context to `Av.Engine`.
2. **Detection**: `Av.Engine` evaluates process indicators and emits `Threat` records from `Av.Core`.
3. **Response**: confirmed detections are sent to `Av.Quarantine`, which creates restorable quarantine records.
4. **Signature trust**: `Av.Signatures` periodically downloads and verifies update bundles before activation.
5. **Operator visibility**: `Av.UI` presents health, threat, and update status.
6. **Cross-cutting telemetry**: all projects use `Av.Core` logging/telemetry interfaces for uniform observability.

## Trust boundaries

### User mode boundary

The current solution scope is intentionally **user mode**:

- `Av.UI`, `Av.Agent`, `Av.Engine`, `Av.Quarantine`, and `Av.Signatures` are user-space components.
- User-mode features include process observation, file hashing, quarantine storage, and UI workflows.
- User-mode code is treated as untrusted input facing (process metadata, files, network payloads) and must validate all external data.

### Kernel mode boundary

Kernel-mode protections (file system mini-filter, kernel callbacks, tamper-resistant self-protection) are **out of scope** in this repository, but the architecture reserves extension points:

- A future signed kernel driver can publish validated events into `Av.Agent`.
- Policy and detection decisions should remain in user mode where possible; kernel mode should provide constrained enforcement hooks.
- Any kernel-mode integration must assume strict least privilege, signed binaries, and defensive failure defaults.

See `docs/threat-model.md` for explicit assumptions and fail-safe behavior.
