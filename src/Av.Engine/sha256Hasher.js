import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';

export class Sha256Hasher {
  hashBuffer(buffer) {
    return createHash('sha256').update(buffer).digest('hex');
  }

  hashText(text) {
    return this.hashBuffer(Buffer.from(text, 'utf8'));
  }

  async hashFile(filePath) {
    return new Promise((resolve, reject) => {
      const hash = createHash('sha256');
      const stream = createReadStream(filePath);
      stream.on('error', reject);
      stream.on('data', (chunk) => hash.update(chunk));
      stream.on('end', () => resolve(hash.digest('hex')));
    });
  }
}

export const sha256Hasher = new Sha256Hasher();
