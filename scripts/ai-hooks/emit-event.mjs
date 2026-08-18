#!/usr/bin/env node
// Hook bridge: reads Claude Code hook JSON from stdin, POSTs to AI Control Center API.
// Falls back to JSONL file if the API is unreachable. Never blocks Claude Code.

import { randomUUID } from 'crypto';
import { appendFileSync, mkdirSync, existsSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';
import http from 'http';

const __dirname = dirname(fileURLToPath(import.meta.url));
const API_URL = process.env.ACC_API_URL || 'http://127.0.0.1:4300/api/events';
const TIMEOUT_MS = parseInt(process.env.ACC_TIMEOUT_MS || '3000', 10);
const FALLBACK_DIR = join(__dirname, 'fallback');

const eventType = process.argv[2] || 'Unknown';

const REQ_PATTERN = /\bREQ-(\d{2,3})\b/gi;

function extractReqId(text) {
  if (!text || typeof text !== 'string') return undefined;
  const match = text.match(REQ_PATTERN);
  if (!match) return undefined;
  const num = match[0].replace(/REQ-/i, '');
  return `REQ-${num.padStart(3, '0')}`;
}

function extractReqFromData(data) {
  if (!data || typeof data !== 'object') return undefined;
  const fields = [
    data.task_subject, data.taskSubject, data.subject,
    data.agent_name, data.agentName, data.label,
    data.description, data.message, data.error,
    data.last_assistant_message,
  ];
  for (const f of fields) {
    const id = extractReqId(f);
    if (id) return id;
  }
  return undefined;
}

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }
  return Buffer.concat(chunks).toString('utf-8').trim();
}

function sanitize(data) {
  if (!data || typeof data !== 'object') return {};
  const clean = { ...data };
  const sensitiveKeys = [
    'api_key', 'apiKey', 'password', 'secret', 'token', 'credential',
    'ANTHROPIC_API_KEY', 'AWS_SECRET', 'OPENAI_API_KEY',
  ];
  for (const key of Object.keys(clean)) {
    if (sensitiveKeys.some(s => key.toLowerCase().includes(s.toLowerCase()))) {
      delete clean[key];
    }
    if (typeof clean[key] === 'string' && clean[key].length > 2000) {
      clean[key] = clean[key].substring(0, 500) + '...[truncated]';
    }
  }
  return clean;
}

function postEvent(payload) {
  return new Promise((resolve, reject) => {
    const url = new URL(API_URL);
    const body = JSON.stringify(payload);
    const req = http.request(
      {
        hostname: url.hostname,
        port: url.port,
        path: url.pathname,
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) },
        timeout: TIMEOUT_MS,
      },
      (res) => {
        let data = '';
        res.on('data', (chunk) => (data += chunk));
        res.on('end', () => resolve({ status: res.statusCode, body: data }));
      }
    );
    req.on('error', reject);
    req.on('timeout', () => { req.destroy(); reject(new Error('timeout')); });
    req.write(body);
    req.end();
  });
}

function writeFallback(payload) {
  try {
    if (!existsSync(FALLBACK_DIR)) mkdirSync(FALLBACK_DIR, { recursive: true });
    const line = JSON.stringify(payload) + '\n';
    const file = join(FALLBACK_DIR, `events-${new Date().toISOString().slice(0, 10)}.jsonl`);
    appendFileSync(file, line, 'utf-8');
  } catch {
    // silently fail — dashboard is optional tooling
  }
}

async function main() {
  try {
    const raw = await readStdin();
    let hookData = {};
    if (raw) {
      try {
        hookData = JSON.parse(raw);
      } catch {
        hookData = { rawText: raw.substring(0, 500) };
      }
    }

    // Extract REQ ID from raw data BEFORE sanitization (longer strings available)
    const requirementId = extractReqFromData(hookData);

    const safe = sanitize(hookData);

    const event = {
      id: randomUUID(),
      timestamp: new Date().toISOString(),
      eventType,
      sessionId: safe.session_id ?? safe.sessionId ?? undefined,
      agentId: safe.agent_id ?? safe.agentId ?? undefined,
      agentType: safe.agent_type ?? safe.agentType ?? undefined,
      agentName: safe.agent_name ?? safe.agentName ?? safe.label ?? safe.description ?? undefined,
      taskId: safe.task_id ?? safe.taskId ?? undefined,
      taskSubject: safe.task_subject ?? safe.taskSubject ?? safe.subject ?? undefined,
      requirementId: requirementId ?? undefined,
      worktree: safe.worktree ?? safe.worktree_path ?? undefined,
      cwd: safe.cwd ?? undefined,
      status: safe.status ?? safe.reason ?? undefined,
      message: safe.message ?? safe.error ?? undefined,
      metadata: safe,
    };

    try {
      await postEvent(event);
    } catch {
      writeFallback(event);
    }
  } catch {
    // never block Claude Code
  }

  process.exit(0);
}

main();
