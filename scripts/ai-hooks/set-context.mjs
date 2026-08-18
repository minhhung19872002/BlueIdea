#!/usr/bin/env node
// Register active work context in AI Control Center before spawning subagents.
// The server correlates subsequent SubagentStart events with this context.
//
// Usage:
//   node set-context.mjs --session <id> --req REQ-001 --subject "Audit catalog" --phase AUDIT
//   node set-context.mjs --session <id> --clear

import http from 'http';
import { randomUUID } from 'crypto';

const API_BASE = process.env.ACC_API_URL?.replace('/api/events', '') || 'http://127.0.0.1:4300';
const args = process.argv.slice(2);

function getArg(name) {
  const idx = args.indexOf(`--${name}`);
  return idx >= 0 && idx + 1 < args.length ? args[idx + 1] : undefined;
}

const sessionId = getArg('session');
const clear = args.includes('--clear');

if (!sessionId) {
  console.log('Usage: set-context.mjs --session <id> --req REQ-001 --subject "..." --phase AUDIT [--agent-type ...]');
  console.log('       set-context.mjs --session <id> --clear');
  process.exit(1);
}

function httpRequest(method, path, body) {
  return new Promise((resolve, reject) => {
    const url = new URL(`${API_BASE}${path}`);
    const data = body ? JSON.stringify(body) : undefined;
    const req = http.request(
      {
        hostname: url.hostname,
        port: url.port,
        path: url.pathname + url.search,
        method,
        headers: data
          ? { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) }
          : {},
        timeout: 5000,
      },
      (res) => {
        let result = '';
        res.on('data', (chunk) => (result += chunk));
        res.on('end', () => {
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve(JSON.parse(result));
          } else {
            reject(new Error(`HTTP ${res.statusCode}: ${result}`));
          }
        });
      }
    );
    req.on('error', reject);
    req.on('timeout', () => { req.destroy(); reject(new Error('timeout')); });
    if (data) req.write(data);
    req.end();
  });
}

async function main() {
  if (clear) {
    await httpRequest('DELETE', `/api/work-context?sessionId=${encodeURIComponent(sessionId)}`);
    console.log(`Work context cleared for session ${sessionId.slice(0, 8)}`);
    return;
  }

  const requirementId = getArg('req') || getArg('requirement');
  const taskSubject = getArg('subject');
  const phase = getArg('phase');
  const agentType = getArg('agent-type');

  const payload = {
    id: randomUUID(),
    sessionId,
    requirementId,
    taskSubject,
    phase,
    agentType,
  };

  const result = await httpRequest('POST', '/api/work-context', payload);
  console.log(`Work context set: REQ=${requirementId || '-'} phase=${phase || '-'} (${result.id?.slice(0, 8)})`);
}

main().catch(err => {
  console.error(`Cannot reach AI Control Center: ${err.message}`);
  process.exit(1);
});
