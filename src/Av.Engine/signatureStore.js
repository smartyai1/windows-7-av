import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';

/**
 * Disk-backed signature store keyed by SHA-256 hash.
 */
export class SignatureStore {
  constructor(storePath) {
    this.storePath = storePath;
    this.signatures = new Map();
    this.loaded = false;
  }

  async load() {
    await mkdir(dirname(this.storePath), { recursive: true });

    try {
      const content = await readFile(this.storePath, 'utf8');
      const parsed = JSON.parse(content);
      this.signatures = new Map(parsed.signatures?.map((entry) => [entry.hash, entry]));
    } catch (error) {
      if (error.code === 'ENOENT') {
        this.signatures = new Map();
      } else {
        throw error;
      }
    }

    this.loaded = true;
  }

  async save() {
    this.#assertLoaded();
    const data = {
      signatures: this.list(),
      updatedUtc: new Date().toISOString(),
    };

    await writeFile(this.storePath, JSON.stringify(data, null, 2), 'utf8');
  }

  addSignature(hash, name, severity = 'high') {
    this.#assertLoaded();
    this.signatures.set(hash, { hash, name, severity });
  }

  removeSignature(hash) {
    this.#assertLoaded();
    return this.signatures.delete(hash);
  }

  hasSignature(hash) {
    this.#assertLoaded();
    return this.signatures.has(hash);
  }

  getSignature(hash) {
    this.#assertLoaded();
    return this.signatures.get(hash);
  }

  list() {
    this.#assertLoaded();
    return [...this.signatures.values()].sort((a, b) => a.name.localeCompare(b.name));
  }

  #assertLoaded() {
    if (!this.loaded) {
      throw new Error('SignatureStore must be loaded before use. Call load().');
    }
  }
}
