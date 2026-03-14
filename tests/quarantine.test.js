import test from 'node:test';
import assert from 'node:assert/strict';
import { access, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { constants } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { QuarantineVault } from '../src/Av.Quarantine/quarantineVault.js';

const exists = async (path) => {
  try {
    await access(path, constants.F_OK);
    return true;
  } catch {
    return false;
  }
};

test('QuarantineVault lifecycle: quarantine -> restore', async () => {
  const root = await mkdtemp(join(tmpdir(), 'av-quarantine-'));

  try {
    const vault = new QuarantineVault(join(root, 'vault'));
    await vault.load();

    const source = join(root, 'sample.exe');
    await writeFile(source, 'payload', 'utf8');

    const quarantined = await vault.quarantineFile(source, {
      threatName: 'Demo.Trojan',
      severity: 'critical',
      hash: 'abc123',
    });

    assert.equal(await exists(source), false);
    assert.equal(await exists(quarantined.quarantinedPath), true);

    const restoredPath = join(root, 'restored', 'sample.exe');
    const restored = await vault.restoreFile(quarantined.id, restoredPath);

    assert.equal(restored.state, 'restored');
    assert.equal(await exists(restoredPath), true);
    assert.equal(await exists(quarantined.quarantinedPath), false);
    assert.equal(await readFile(restoredPath, 'utf8'), 'payload');
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('QuarantineVault lifecycle: quarantine -> delete -> purge', async () => {
  const root = await mkdtemp(join(tmpdir(), 'av-quarantine-'));

  try {
    const vault = new QuarantineVault(join(root, 'vault'));
    await vault.load();

    const source = join(root, 'bad.dll');
    await writeFile(source, 'danger', 'utf8');

    const quarantined = await vault.quarantineFile(source, {
      threatName: 'Bad.DLL',
      severity: 'high',
      hash: 'def456',
    });

    const deleted = await vault.deleteFile(quarantined.id);
    assert.equal(deleted.state, 'deleted');
    assert.equal(vault.list().length, 1);

    await vault.purgeDeleted();
    assert.equal(vault.list().length, 0);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});
