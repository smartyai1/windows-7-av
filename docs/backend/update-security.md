# Secure AV Signature Update Pipeline

This document defines the backend + client contract for secure signature updates used by `Av.Signatures`.

## 1. Signed update package format

The update payload is a single JSON envelope with three mandatory parts:

1. `manifest`: versioned update manifest.
2. `signatureSet`: threshold signature set over the serialized manifest.
3. `detachedSignature`: detached digest signature to prevent envelope tampering.

### JSON schema (logical)

```json
{
  "manifest": {
    "schemaVersion": 1,
    "packageVersion": "2026.03.14.1",
    "publishedAtUtc": "2026-03-14T02:13:00Z",
    "minimumClientVersion": "7.4.0",
    "artifacts": [
      {
        "path": "defs/main.dat",
        "sizeBytes": 15522041,
        "sha256": "..."
      }
    ],
    "metadata": {
      "channel": "stable"
    }
  },
  "signatureSet": {
    "keysetVersion": 3,
    "requiredSignatures": 2,
    "signatures": [
      {
        "keyId": "release-key-a",
        "algorithm": "ECDSA_P256_SHA256",
        "value": "base64-signature"
      }
    ]
  },
  "detachedSignature": "base64-sha256(manifest-bytes)"
}
```

`manifest.schemaVersion` is required for compatibility-safe evolution. `signatureSet` supports key rotation (`keysetVersion`) and M-of-N trust (`requiredSignatures`).

## 2. Transport trust: TLS pinning

The updater performs strict certificate checks:

- Standard TLS validation must pass.
- SPKI SHA-256 pin of the leaf certificate must match one of the locally pinned values.

This prevents trust-on-first-use and mitigates CA compromise. Pin sets should contain at least one backup key to support certificate rotation.

## 3. Signature validation flow

Before any artifact is applied:

1. Validate manifest and signature set structure.
2. Verify each signature entry against trusted public keys.
3. Ensure signature threshold (`requiredSignatures`) is met.
4. Validate detached signature against canonical serialized manifest digest.

Any failure aborts the update.

## 4. Last-known-good snapshot and rollback

Client keeps two directories:

- `current/`: active runtime content.
- `lkg/`: last-known-good snapshot.

Apply procedure:

1. Promote `current` to `lkg`.
2. Download new artifacts into `current`.
3. Re-hash every downloaded artifact and compare to manifest hashes.
4. If corruption or apply error occurs, rollback by replacing `current` with `lkg` automatically.

## 5. Scheduling, retries/backoff, telemetry

Scheduler behavior:

- Periodic update polling (`interval`).
- Bounded retries (`maxRetries`).
- Exponential backoff (`baseBackoff * 2^(attempt-1)`).

Telemetry events emitted:

- Attempt start (`attempt`, `delay`).
- Success (`packageVersion`, duration).
- Failure (`reason`, exception).
- Rollback (`packageVersion`, reason).
- Health signal (`healthy`, details).

These events should feed backend observability and alerting for update safety, failure rates, and rollback frequency.
