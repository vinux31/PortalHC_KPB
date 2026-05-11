---
phase: 314
plan: 01
status: complete-shortcut
completed: 2026-05-08
---

# Plan 01 Summary — Repro + Root Cause Documentation

**One-liner:** Bug TKN-01 reproduced cross-env (Dev `10.55.3.3` + lokal `localhost:5277`); root cause identified sebagai routing mismatch dari Phase 287 controller split (Hipotesis 6 BARU, bukan dari 4 hipotesis pre-repro).

## What Was Done

| Task | Status | Output |
|------|--------|--------|
| Pre-checkpoint scaffold | ✅ | `repro-evidence/05-d39-queries.sql` + folder structure (commit `dd56926b`) |
| Task 1: Repro Dev UI | ✅ | `01-after-regen-attempt.png`, `02-network-requests.txt`, `03-response-body.txt` (commit `0eb5cb0f`) |
| Task 2: Capture stacktrace | ⚠ N/A | Server return 404 (no exception), captured via Network response. Stacktrace section not applicable. |
| Task 3: SQL D-39 queries | ⚠ Skipped | Root cause found via routing analysis di Step 1 — data shape baseline tidak kritikal untuk fix scope. |
| Task 4: Finalize RESEARCH.md hipotesis tabel | ⚠ Replaced | Hipotesis 6 (BARU) finalized via `08-root-cause-finalized.md` (commit `0eb5cb0f`) — replaces RESEARCH.md `## Stacktrace Evidence` section yang awalnya untuk H1/H2/H3. |
| Task 5: Revisi D-23 wording CONTEXT.md | ⚠ Deferred | D-23 (worker session invalidation) deferred ke v16.0+ enhancement — root cause Phase 314 = routing, bukan session lifecycle. |

## Findings

- **Hipotesis 1/2/3 (NRE / DbUpdateException / SqlException) RULED OUT** — method body never executed (404 di routing layer).
- **Hipotesis 4 (frontend handler hide message) PARTIALLY confirmed** — alert "Periksa koneksi jaringan" misleading untuk 404 case. D-07 frontend propagate deferred (non-blocker).
- **Hipotesis 5 (Dev binary stale) RULED OUT** — lokal juga repro.
- **Hipotesis 6 (BARU): Routing mismatch dari Phase 287 controller split** — confirmed root cause.

## Why Shortcut

Plan 01 originally scoped 5 sequential investigative tasks (mostly checkpoint:human-verify). Setelah Task 1 capture network response menunjukkan 404 (bukan exception), root cause analysis pivot dari data-shape investigation ke routing analysis. Tasks 2-3 SQL queries + stacktrace capture jadi non-kritikal — defer atau skip.

## Files Modified

- `.planning/phases/314-fix-regenerate-token-untuk-status-upcoming/repro-evidence/` (new folder, 11 files captured)

## Commits

- `dd56926b` chore(314-01): scaffold repro-evidence folder
- `0eb5cb0f` docs(314): repro evidence + root cause finalized
- `9884cdf9` docs(314): audit secondary clean

## Next

Plan 02 = backend patch (1-line route attribute fix).
