#!/usr/bin/env node
// Run a command and report its result as a quality gate to the AI Control Center.
// The underlying command's exit code is preserved — dashboard downtime never alters it.
//
// Usage:
//   node run-quality.mjs --gate backend-build --req REQ-001 -- dotnet build
//   node run-quality.mjs --gate unit-tests -- dotnet test --no-build
//   node run-quality.mjs --gate frontend-lint -- npm run lint

import { spawn } from 'child_process';
import http from 'http';
import { randomUUID } from 'crypto';
import { execSync } from 'child_process';

const API_URL = process.env.ACC_API_URL || 'http://127.0.0.1:4300/api/quality';

const args = process.argv.slice(2);
const separatorIdx = args.indexOf('--');

if (separatorIdx < 0 || separatorIdx === args.length - 1) {
  console.log('Usage: run-quality.mjs --gate <name> [--req REQ-XXX] -- <command> [args...]');
  console.log('');
  console.log('Examples:');
  console.log('  run-quality.mjs --gate backend-build -- dotnet build');
  console.log('  run-quality.mjs --gate unit-tests --req REQ-001 -- dotnet test');
  process.exit(1);
}

const flags = args.slice(0, separatorIdx);
const command = args.slice(separatorIdx + 1);

function getFlag(name) {
  const idx = flags.indexOf(`--${name}`);
  return idx >= 0 && idx + 1 < flags.length ? flags[idx + 1] : undefined;
}

const gate = getFlag('gate');
const requirementId = getFlag('req') || getFlag('requirement');

if (!gate) {
  console.error('Error: --gate is required');
  process.exit(1);
}

function getCommitHash() {
  try {
    return execSync('git rev-parse --short HEAD', { encoding: 'utf-8' }).trim();
  } catch {
    return undefined;
  }
}

function postQuality(payload) {
  return new Promise((resolve) => {
    try {
      const url = new URL(API_URL);
      const body = JSON.stringify(payload);
      const req = http.request(
        {
          hostname: url.hostname,
          port: url.port,
          path: url.pathname,
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) },
          timeout: 5000,
        },
        (res) => {
          let data = '';
          res.on('data', (chunk) => (data += chunk));
          res.on('end', () => resolve({ ok: res.statusCode < 300 }));
        }
      );
      req.on('error', () => resolve({ ok: false }));
      req.on('timeout', () => { req.destroy(); resolve({ ok: false }); });
      req.write(body);
      req.end();
    } catch {
      resolve({ ok: false });
    }
  });
}

async function main() {
  const runId = randomUUID();
  const startedAt = new Date().toISOString();
  const commit = getCommitHash();
  const cmdString = command.join(' ');

  // Report RUNNING
  await postQuality({
    id: runId,
    gate,
    status: 'RUNNING',
    command: cmdString,
    startedAt,
    commit,
    requirementId,
    source: 'script',
  });

  console.log(`[quality] ${gate}: RUNNING — ${cmdString}`);

  const startMs = Date.now();

  const child = spawn(command[0], command.slice(1), {
    stdio: 'inherit',
    shell: true,
    cwd: process.cwd(),
  });

  child.on('close', async (code) => {
    const finishedAt = new Date().toISOString();
    const durationMs = Date.now() - startMs;
    const status = code === 0 ? 'PASS' : 'FAIL';

    console.log(`[quality] ${gate}: ${status} (exit ${code}, ${(durationMs / 1000).toFixed(1)}s)`);

    // Report final status
    await postQuality({
      id: runId,
      gate,
      status,
      command: cmdString,
      durationMs,
      startedAt,
      finishedAt,
      commit,
      requirementId,
      source: 'script',
    });

    process.exit(code ?? 1);
  });

  child.on('error', async (err) => {
    const finishedAt = new Date().toISOString();
    const durationMs = Date.now() - startMs;
    console.error(`[quality] ${gate}: FAIL — ${err.message}`);

    await postQuality({
      id: runId,
      gate,
      status: 'FAIL',
      command: cmdString,
      durationMs,
      startedAt,
      finishedAt,
      commit,
      requirementId,
      source: 'script',
    });

    process.exit(1);
  });
}

main();
