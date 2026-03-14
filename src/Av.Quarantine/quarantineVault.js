import { mkdir, readFile, rename, rm, stat, writeFile } from 'node:fs/promises';
import { basename, dirname, join } from 'node:path';
import { randomUUID } from 'node:crypto';

export class QuarantineVault {
  constructor(vaultRoot) {
    this.vaultRoot = vaultRoot;
    this.itemsDir = join(vaultRoot, 'items');
    this.indexPath = join(vaultRoot, 'index.json');
    this.index = new Map();
    this.loaded = false;
  }

  async load() {
    await mkdir(this.itemsDir, { recursive: true });

    try {
      const content = await readFile(this.indexPath, 'utf8');
      const parsed = JSON.parse(content);
      this.index = new Map(parsed.items?.map((x) => [x.id, x]));
    } catch (error) {
      if (error.code === 'ENOENT') {
        this.index = new Map();
      } else {
        throw error;
      }
    }

    this.loaded = true;
  }

  list() {
    this.#assertLoaded();
    return [...this.index.values()].sort((a, b) => b.quarantinedUtc.localeCompare(a.quarantinedUtc));
  }

  async quarantineFile(sourcePath, reason) {
    this.#assertLoaded();
    const sourceInfo = await stat(sourcePath);
    if (!sourceInfo.isFile()) {
      throw new Error(`Path is not a file: ${sourcePath}`);
    }

    const id = randomUUID();
    const record = {
      id,
      threatName: reason.threatName,
      severity: reason.severity ?? 'high',
      sourcePath,
      sourceName: basename(sourcePath),
      quarantinedPath: join(this.itemsDir, `${id}.bin`),
      quarantinedUtc: new Date().toISOString(),
      byteLength: sourceInfo.size,
      hash: reason.hash,
      restoredUtc: null,
      deletedUtc: null,
      state: 'quarantined',
    };

    await rename(sourcePath, record.quarantinedPath);
    this.index.set(id, record);
    await this.#save();
    return record;
  }

  async restoreFile(id, restorePath = null) {
    this.#assertLoaded();
    const record = this.#getActive(id, 'quarantined');
    const destination = restorePath ?? record.sourcePath;

    await mkdir(dirname(destination), { recursive: true });
    await rename(record.quarantinedPath, destination);

    record.state = 'restored';
    record.restoredUtc = new Date().toISOString();
    record.restorePath = destination;
    this.index.set(id, record);
    await this.#save();

    return record;
  }

  async deleteFile(id) {
    this.#assertLoaded();
    const record = this.#getActive(id, 'quarantined');

    await rm(record.quarantinedPath, { force: true });
    record.state = 'deleted';
    record.deletedUtc = new Date().toISOString();
    this.index.set(id, record);
    await this.#save();

    return record;
  }

  async purgeDeleted() {
    this.#assertLoaded();
    for (const [id, item] of this.index.entries()) {
      if (item.state === 'deleted') {
        this.index.delete(id);
      }
    }

    await this.#save();
  }

  #getActive(id, requiredState) {
    const record = this.index.get(id);
    if (!record) {
      throw new Error(`Quarantine item not found: ${id}`);
    }

    if (record.state !== requiredState) {
      throw new Error(`Quarantine item ${id} is in state ${record.state}, expected ${requiredState}`);
    }

    return record;
  }

  async #save() {
    await writeFile(
      this.indexPath,
      JSON.stringify({ items: this.list(), updatedUtc: new Date().toISOString() }, null, 2),
      'utf8',
    );
  }

  #assertLoaded() {
    if (!this.loaded) {
      throw new Error('QuarantineVault must be loaded before use. Call load().');
    }
  }
}
