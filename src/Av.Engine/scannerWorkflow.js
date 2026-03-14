import { readdir, stat } from 'node:fs/promises';
import { basename, join } from 'node:path';
import { sha256Hasher } from './sha256Hasher.js';

export class ScannerWorkflow {
  constructor(signatureStore) {
    this.signatureStore = signatureStore;
  }

  async scanPath(scanPath, onEvent = () => {}) {
    const startedUtc = new Date().toISOString();
    const files = await this.#collectFiles(scanPath);
    const detections = [];
    const errors = [];

    onEvent({ type: 'scan-started', message: `Scan started: ${scanPath}`, timestamp: startedUtc });

    for (const filePath of files) {
      try {
        const hash = await sha256Hasher.hashFile(filePath);
        const signature = this.signatureStore.getSignature(hash);

        if (signature) {
          const detection = {
            status: 'infected',
            filePath,
            fileName: basename(filePath),
            hash,
            signature,
            scannedUtc: new Date().toISOString(),
          };
          detections.push(detection);
          onEvent({
            type: 'threat-detected',
            severity: signature.severity,
            filePath,
            hash,
            threatName: signature.name,
            timestamp: detection.scannedUtc,
            message: `${signature.name} matched in ${filePath}`,
          });
        } else {
          onEvent({
            type: 'file-clean',
            severity: 'low',
            filePath,
            hash,
            timestamp: new Date().toISOString(),
            message: `No signature matched for ${filePath}`,
          });
        }
      } catch (error) {
        const failure = {
          filePath,
          error: error.message,
          timestamp: new Date().toISOString(),
        };
        errors.push(failure);
        onEvent({
          type: 'scan-error',
          severity: 'medium',
          filePath,
          timestamp: failure.timestamp,
          message: `Could not scan ${filePath}: ${failure.error}`,
        });
      }
    }

    const finishedUtc = new Date().toISOString();
    onEvent({
      type: 'scan-finished',
      severity: detections.length > 0 ? 'high' : 'low',
      message: `Scan complete. ${files.length} files scanned, ${detections.length} threats detected, ${errors.length} errors.`,
      timestamp: finishedUtc,
    });

    return {
      startedUtc,
      finishedUtc,
      filesScanned: files.length,
      detections,
      errors,
    };
  }

  async #collectFiles(scanPath) {
    const nodeStat = await stat(scanPath);
    if (nodeStat.isFile()) {
      return [scanPath];
    }

    const entries = await readdir(scanPath, { withFileTypes: true });
    const nested = await Promise.all(
      entries.map(async (entry) => {
        const fullPath = join(scanPath, entry.name);
        if (entry.isDirectory()) {
          return this.#collectFiles(fullPath);
        }

        if (entry.isFile()) {
          return [fullPath];
        }

        return [];
      }),
    );

    return nested.flat();
  }
}
