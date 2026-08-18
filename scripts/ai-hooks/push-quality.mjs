#!/usr/bin/env node
// Push quality gate results to the AI Control Center.
// Usage: node scripts/ai-hooks/push-quality.mjs <gate> <status> [options]
//
// Examples:
//   node push-quality.mjs backend-build PASS
//   node push-quality.mjs unit-tests PASS --total 42 --passed 42 --failed 0 --command "dotnet test"
//   node push-quality.mjs e2e-tests FAIL --total 10 --passed 7 --failed 3
//   node push-quality.mjs integration-tests RUNNING

import http from 'http';
import { randomUUID } from 'crypto';

const API_URL = process.env.ACC_API_URL || 'http://127.0.0.1:4300/api/quality';
const args = process.argv.slice(2);

if (args.length < 2) {
  console.log('Usage: push-quality.mjs <gate> <PASS|FAIL|RUNNING|NO_DATA> [--total N] [--passed N] [--failed N] [--command "..."] [--commit hash]');
  process.exit(1);
}

const gate = args[0];
const status = args[1].toUpperCase();

function getArg(name) {
  const idx = args.indexOf(`--${name}`);
  return idx >= 0 && idx + 1 < args.length ? args[idx + 1] : undefined;
}

const payload = {
  id: randomUUID(),
  gate,
  status,
  total: getArg('total') ? parseInt(getArg('total')) : undefined,
  passed: getArg('passed') ? parseInt(getArg('passed')) : undefined,
  failed: getArg('failed') ? parseInt(getArg('failed')) : undefined,
  command: getArg('command'),
  commit: getArg('commit'),
  timestamp: new Date().toISOString(),
};

const body = JSON.stringify(payload);
const url = new URL(API_URL);

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
    res.on('end', () => {
      if (res.statusCode >= 200 && res.statusCode < 300) {
        console.log(`Quality gate "${gate}" → ${status}`);
      } else {
        console.error(`Failed (HTTP ${res.statusCode}): ${data}`);
        process.exit(1);
      }
    });
  }
);

req.on('error', (err) => {
  console.error(`Cannot reach AI Control Center: ${err.message}`);
  process.exit(1);
});

req.write(body);
req.end();
