#!/usr/bin/env node
// Replay fallback JSONL events into the AI Control Center API.
// Dedup: events with existing IDs are skipped (INSERT OR REPLACE in DB).
// Corrupted lines are logged and skipped, not retried.
//
// Usage: node scripts/ai-hooks/replay-events.mjs [file.jsonl]
// Without arguments, replays all .jsonl files in the fallback directory.

import { readFileSync, readdirSync, renameSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import http from 'http';

const __dirname = dirname(fileURLToPath(import.meta.url));
const API_URL = process.env.ACC_API_URL || 'http://127.0.0.1:4300/api/events/bulk';
const FALLBACK_DIR = join(__dirname, 'fallback');

function postBulk(events) {
  return new Promise((resolve, reject) => {
    const url = new URL(API_URL);
    const body = JSON.stringify(events);
    const req = http.request(
      {
        hostname: url.hostname,
        port: url.port,
        path: url.pathname,
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) },
        timeout: 30000,
      },
      (res) => {
        let data = '';
        res.on('data', (chunk) => (data += chunk));
        res.on('end', () => {
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve(JSON.parse(data));
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${data}`));
          }
        });
      }
    );
    req.on('error', reject);
    req.on('timeout', () => { req.destroy(); reject(new Error('timeout')); });
    req.write(body);
    req.end();
  });
}

async function replayFile(filePath) {
  const content = readFileSync(filePath, 'utf-8').trim();
  if (!content) {
    console.log(`  [skip] ${filePath} — empty`);
    return { accepted: 0, skipped: 0, corrupted: 0 };
  }

  const lines = content.split('\n');
  const events = [];
  let corrupted = 0;

  for (const line of lines) {
    try {
      const parsed = JSON.parse(line);
      if (parsed && typeof parsed === 'object') {
        parsed._replayed = true;
        events.push(parsed);
      } else {
        corrupted++;
      }
    } catch {
      corrupted++;
    }
  }

  if (events.length === 0) {
    console.log(`  [skip] ${filePath} — no valid events (${corrupted} corrupted)`);
    return { accepted: 0, skipped: 0, corrupted };
  }

  let totalAccepted = 0;
  let totalSkipped = 0;
  const batchSize = 50;

  for (let i = 0; i < events.length; i += batchSize) {
    const batch = events.slice(i, i + batchSize);
    const result = await postBulk(batch);
    totalAccepted += result.accepted || 0;
    totalSkipped += result.skipped || 0;
  }

  console.log(`  [done] ${filePath} — ${totalAccepted} accepted, ${totalSkipped} duplicates skipped, ${corrupted} corrupted`);

  const replayedPath = filePath.replace('.jsonl', '.replayed.jsonl');
  renameSync(filePath, replayedPath);

  return { accepted: totalAccepted, skipped: totalSkipped, corrupted };
}

async function main() {
  const specificFile = process.argv[2];

  if (specificFile) {
    console.log('Replaying specific file...');
    await replayFile(specificFile);
    return;
  }

  let files;
  try {
    files = readdirSync(FALLBACK_DIR)
      .filter(f => f.endsWith('.jsonl') && !f.includes('.replayed.'))
      .map(f => join(FALLBACK_DIR, f))
      .sort();
  } catch {
    console.log('No fallback directory found. Nothing to replay.');
    return;
  }

  if (files.length === 0) {
    console.log('No pending fallback files to replay.');
    return;
  }

  console.log(`Found ${files.length} fallback file(s) to replay:\n`);
  let totalAccepted = 0;
  let totalSkipped = 0;
  let totalCorrupted = 0;

  for (const file of files) {
    try {
      const result = await replayFile(file);
      totalAccepted += result.accepted;
      totalSkipped += result.skipped;
      totalCorrupted += result.corrupted;
    } catch (err) {
      console.error(`  [error] ${file} — ${err.message}`);
      console.error('  Is the AI Control Center running? (cd tools/ai-control-center && npm start)');
      break;
    }
  }

  console.log(`\nTotal: ${totalAccepted} accepted, ${totalSkipped} duplicates, ${totalCorrupted} corrupted.`);
}

main().catch(err => {
  console.error('Replay failed:', err.message);
  process.exit(1);
});
