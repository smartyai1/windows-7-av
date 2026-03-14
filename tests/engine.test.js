import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { Sha256Hasher } from '../src/Av.Engine/sha256Hasher.js';
import { SignatureStore } from '../src/Av.Engine/signatureStore.js';
import { ScannerWorkflow } from '../src/Av.Engine/scannerWorkflow.js';

const hasher = new Sha256Hasher();

test('Sha256Hasher hashes known value', async () => {
  const hash = hasher.hashText('abc');
  assert.equal(hash, 'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad');
});

test('SignatureStore persists signatures', async () => {
  const root = await mkdtemp(join(tmpdir(), 'av-signatures-'));
  try {
    const storePath = join(root, 'signatures.json');
    const store = new SignatureStore(storePath);
    await store.load();

    store.addSignature('hash1', 'Demo.Test.Threat', 'critical');
    await store.save();

    const secondStore = new SignatureStore(storePath);
    await secondStore.load();

    assert.equal(secondStore.hasSignature('hash1'), true);
    assert.equal(secondStore.getSignature('hash1').name, 'Demo.Test.Threat');
    assert.equal(secondStore.getSignature('hash1').severity, 'critical');
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('ScannerWorkflow detects infected and clean files', async () => {
  const root = await mkdtemp(join(tmpdir(), 'av-scan-'));

  try {
    const badFile = join(root, 'bad.bin');
    const goodFile = join(root, 'good.bin');
    await writeFile(badFile, 'evil payload', 'utf8');
    await writeFile(goodFile, 'benign payload', 'utf8');

    const storePath = join(root, 'sigs.json');
    const store = new SignatureStore(storePath);
    await store.load();
    const badHash = await hasher.hashFile(badFile);
    store.addSignature(badHash, 'EICAR.DEMO', 'high');
    await store.save();

    const workflow = new ScannerWorkflow(store);
    const events = [];
    const report = await workflow.scanPath(root, (event) => events.push(event));

    assert.equal(report.filesScanned, 3); // includes sigs.json after save.
    assert.equal(report.detections.length, 1);
    assert.equal(report.detections[0].signature.name, 'EICAR.DEMO');
    assert.equal(report.errors.length, 0);
    assert.equal(events.some((x) => x.type === 'threat-detected'), true);
    assert.equal(events.at(-1).type, 'scan-finished');
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});
