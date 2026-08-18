# MISSION

Build a local developer-facing AI Control Center for the BLUEDEA repository.

This dashboard is NOT part of the customer-facing BLUEDEA application.

It is engineering infrastructure used to observe Claude Code agents, tasks, requirement progress, quality gates and AI development activity in real time.

Before implementation:

1. Read the current `CLAUDE.md`
2. Read `.claude/rules/`
3. Inspect `.claude/agents/`
4. Inspect existing `.claude/settings.json`
5. Inspect `docs/` requirement/audit/traceability artifacts
6. Inspect existing scripts
7. Inspect repository package/tooling configuration
8. Inspect git status

Do not overwrite other developers' work.

Do not modify customer-facing application architecture unless absolutely necessary.

---

# GOAL

I want to run:

```bash
npm run ai:dashboard
```

or an equivalent single command appropriate for the repository.

Then open a local URL such as:

```text
http://localhost:4300
```

and see the current state of the AI engineering team.

---

# ARCHITECTURE

Prefer implementing the dashboard as isolated developer tooling:

```text
tools/
  ai-control-center/
    server/
    web/
    data/

scripts/
  ai-hooks/
```

Do not put this inside the production BLUEDEA frontend unless there is a strong architectural reason.

Preferred stack unless repository constraints strongly suggest otherwise:

Backend:
NestJS

Frontend:
React + TypeScript

Local persistence:
SQLite

Realtime:
WebSocket / Socket.IO

Hook bridge:
portable Node.js script

If equivalent existing project infrastructure can be reused more cleanly, explain why before changing the design.

---

# EVENT FLOW

Implement:

```text
Claude Code
       |
       | hooks
       v
scripts/ai-hooks/emit-event.mjs
       |
       v
AI Control Center API
       |
       +--> SQLite
       |
       +--> WebSocket
                |
                v
             React UI
```

The hook emitter must be lightweight.

If the dashboard API is temporarily unavailable, events must not break Claude Code.

Use a safe fallback such as local JSONL event logging.

Claude development must continue even when the dashboard is offline.

---

# CLAUDE CODE EVENTS

Inspect the currently installed Claude Code version/documentation/configuration before editing settings.

Where supported, collect at least:

```text
SubagentStart
SubagentStop

TaskCreated
TaskCompleted

TeammateIdle

PostToolUseFailure

WorktreeCreate
WorktreeRemove

Stop
StopFailure
```

Also determine whether useful tool activity can be collected safely without excessive noise.

Do not capture secrets or complete source-file contents.

---

# EVENT MODEL

Normalize events approximately into:

```typescript
type AiEvent = {
  id: string;
  timestamp: string;

  sessionId?: string;

  eventType: string;

  agentId?: string;
  agentType?: string;
  agentName?: string;

  taskId?: string;
  taskSubject?: string;

  requirementId?: string;

  worktree?: string;

  cwd?: string;

  status?: string;

  message?: string;

  metadata?: Record<string, unknown>;
};
```

Adjust this model based on actual Claude Code hook payloads.

Do not invent values that Claude does not provide.

---

# AGENT STATE MODEL

Derive agent states from events.

At minimum:

```text
STARTING
RUNNING
WAITING
IDLE
COMPLETED
FAILED
```

Example:

```text
SubagentStart
→ RUNNING

TeammateIdle
→ IDLE

SubagentStop
→ COMPLETED

failure
→ FAILED
```

Preserve timestamps so the UI can display duration.

---

# TASK MODEL

Track:

```text
PENDING
IN_PROGRESS
BLOCKED
REVIEW
QA
COMPLETED
FAILED
```

Do not fake transitions when there is no evidence.

If Claude Code's native task events do not expose every state, distinguish:

```text
native state
derived state
```

in the data model.

---

# REQUIREMENT MODEL

Integrate with the canonical requirement traceability artifact already established in this repository.

Do NOT create a second conflicting source of truth.

Display statuses such as:

```text
NOT_ASSESSED
MISSING
PARTIAL
IMPLEMENTED_NOT_VERIFIED
VERIFIED
BLOCKED_EXTERNAL
NOT_APPLICABLE
```

The dashboard should be read-only with respect to requirement truth for V1.

Do not allow a UI button to manually mark a customer requirement VERIFIED.

Verification must continue to come from the audit process.

---

# DASHBOARD PAGE

Create a main dashboard showing:

```text
Total Requirements

Verified
Partial
Missing
Blocked

Overall verified percentage

Agents Running
Agents Idle
Agents Failed

Tasks Pending
Tasks Active
Tasks Failed

Quality Gates

Current active requirements

Critical blockers
```

---

# AGENTS PAGE

Display each agent as a card/table row.

Show where available:

```text
Agent name
Agent type
Status
Current task
Requirement
Start time
Duration
Working directory
Worktree
Last activity
```

Update in real time.

---

# REQUIREMENTS PAGE

Show all customer requirements.

Columns:

```text
ID
Customer requirement number
Title
Status
Progress
Current gap
Owner/agent
Last verified commit
```

Support filters:

```text
status
module
requirement type
agent
```

Clicking a requirement should show evidence:

```text
Frontend
Backend
Database
Authorization
Tests
Security
Integration
Known gaps
```

---

# TASK BOARD PAGE

Implement a clear task visualization.

For example:

```text
Pending

In Progress

Review

QA

Blocked

Completed
```

Realtime updates where evidence exists.

---

# ACTIVITY PAGE

Provide chronological engineering activity.

Examples:

```text
agent started

task created

task completed

tool failed

worktree created

test result received

agent stopped
```

Support filtering by:

```text
session
agent
requirement
event type
```

---

# QUALITY PAGE

Display engineering quality gates when data exists:

```text
backend build

frontend build

unit tests

integration tests

E2E

security

requirement audit
```

Do not fabricate test numbers.

If no data source exists, display:

```text
NO DATA
```

instead of PASS.

---

# UI REQUIREMENTS

The UI should look like a professional engineering control center.

Prioritize:

* information density
* readability
* operational clarity
* dark/light mode compatibility if straightforward
* responsive layout
* clear state badges

Avoid flashy marketing design.

Prefer:

```text
dashboard
tables
status chips
progress bars
timeline
kanban
```

---

# REALTIME

Use WebSocket or Socket.IO.

When a hook event arrives:

```text
POST event
    ↓
persist
    ↓
update state
    ↓
broadcast
    ↓
React UI updates
```

Do not require browser refresh.

---

# API

Create an internal local API approximately covering:

```text
POST /api/events

GET /api/overview

GET /api/agents

GET /api/tasks

GET /api/events

GET /api/requirements

GET /api/quality
```

Use DTO validation.

The event ingestion endpoint must reject malformed input safely.

---

# LOCAL STORAGE

Use SQLite for V1 unless the existing repository already provides a better isolated development database.

Tables may include:

```text
ai_sessions

ai_agents

ai_tasks

ai_events

quality_runs
```

Do NOT duplicate the canonical customer requirement registry unnecessarily.

Requirements may be parsed/read from the canonical repository artifact.

---

# HOOK BRIDGE

Create:

```text
scripts/ai-hooks/emit-event.mjs
```

It must:

1. Read hook JSON from stdin
2. Normalize safe metadata
3. POST it to the local dashboard API
4. Use a short timeout
5. Never block Claude development because dashboard is down
6. Fallback to JSONL if API is unreachable
7. Exit safely

Do not log secrets.

---

# CLAUDE SETTINGS

Inspect existing settings first.

Safely integrate hooks into the project Claude configuration.

Do NOT delete existing hooks.

Merge with current configuration.

Where supported configure events including:

```text
SubagentStart
SubagentStop
TaskCreated
TaskCompleted
TeammateIdle
PostToolUseFailure
WorktreeCreate
WorktreeRemove
Stop
StopFailure
```

Use the exact schema supported by the currently installed Claude Code version.

Do not guess the schema.

---

# SUBAGENT TERMINAL STATUS

Also configure or document a `subagentStatusLine` so Claude Code itself shows useful compact information even when the web dashboard is closed.

Suggested row:

```text
Backend Engineer | RUNNING | REQ-009 | 08:21
```

Use only data actually supplied by Claude Code.

---

# OPTIONAL OPENTELEMETRY

Do NOT make OpenTelemetry mandatory for V1.

However design the system so telemetry can be added later.

Create documentation for a Phase 2 integration supporting:

```text
tokens
cost
model
tool usage
session duration
errors
```

via Claude Code OpenTelemetry.

Do not enable sensitive prompt/tool-content telemetry by default.

---

# REQUIREMENT ASSOCIATION

We need a reliable way to associate AI work with a requirement.

Prefer convention-based task subjects such as:

```text
[REQ-009] Implement workflow versioning

[REQ-026] Add similarity regression tests
```

When possible, parse:

```text
REQ-\d+
```

from:

```text
task subject
description
agent activity
```

Never invent a requirement ID if none is present.

---

# QUALITY GATES

Provide infrastructure so future test runners can push quality results into the dashboard.

Suggested data:

```text
gate
status
command
total
passed
failed
duration
timestamp
commit
```

Possible statuses:

```text
PASS
FAIL
RUNNING
NO_DATA
```

Do not modify existing test semantics.

---

# FAILURE SAFETY

The AI Control Center is observability tooling.

It MUST NOT become a dependency required for normal application development.

If:

```text
dashboard stops
database unavailable
browser closed
API unavailable
```

Claude Code and the BLUEDEA project must continue working normally.

---

# SECURITY

The dashboard should default to localhost only.

Do not expose it publicly.

Do not ingest:

```text
API keys
passwords
full environment variables
complete source contents
customer document contents
```

unless specifically necessary.

No telemetry should leave the development machine for V1.

---

# DOCUMENTATION

Create a concise README explaining:

## Start

How to run the dashboard.

## Claude hooks

How events are captured.

## Status mapping

How agent/task states are derived.

## Requirements

Where requirement data comes from.

## Troubleshooting

What happens when hooks cannot reach the dashboard.

## Phase 2

OpenTelemetry / centralized multi-machine observability.

---

# VALIDATION

Actually validate the implementation.

Test at minimum:

1. server starts
2. frontend starts
3. database initializes
4. fake SubagentStart event is ingested
5. agent appears RUNNING
6. fake TaskCreated event appears
7. fake SubagentStop updates state
8. WebSocket update reaches frontend
9. API unavailable fallback works
10. existing project build/tests are not broken

Add automated tests for critical event normalization/state logic.

---

# IMPORTANT

Do NOT merely create mock UI.

The dashboard must receive and persist real event payloads through the hook bridge.

Do NOT claim realtime works unless you actually test event ingestion and WebSocket propagation.

Do NOT modify unrelated customer functionality.

---

# FINAL REPORT

After implementation provide:

## Files created

## Files modified

## Architecture

## Hook events configured

## How to start

## Local URL

## Tests executed

## Test results

## Example event tested

## Known limitations

## Phase 2 recommendations

## Git diff summary

Then STOP.

Do not start implementing unrelated BLUEDEA business requirements.
