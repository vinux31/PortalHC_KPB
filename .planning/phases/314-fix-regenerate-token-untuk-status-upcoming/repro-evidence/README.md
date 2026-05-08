# Phase 314 Plan 01 — Reproduction Evidence

Folder ini menampung artifacts dari investigation Plan 01 (repro Task 1 + stacktrace Task 2 + DB queries Task 3).

## Expected files (filled by operator + executor)

| File | Source | Owner |
|------|--------|-------|
| `01-alert.png` | Browser alert text screenshot saat klik Regenerate Token | Operator |
| `02-network.png` | DevTools Network panel screenshot (request `RegenerateToken` — status, headers, response body) | Operator |
| `03-response-body.txt` | Verbatim text response body (JSON atau HTML 5xx page) | Operator |
| `04-stacktrace.txt` | Server log Dev exception text (full stacktrace) atau "NONE — server tidak throw exception" note | Operator (via koordinasi IT) |
| `05-d39-queries.sql` | 5 SQL queries D-39 verbatim | Executor (akan generate kalau diperlukan) |
| `06-d39-results.txt` | Tabular output 5 queries dari DB Dev | Operator (eksekusi SSMS/DBeaver read-only) |

## Catatan

- Per CLAUDE.md: investigation ini **read-only** terhadap Dev environment. JANGAN edit kode Dev. JANGAN UPDATE/DELETE/INSERT di DB Dev.
- Akses DB Dev + log Dev = koordinasi Team IT (read-only credential).
- Setelah artifacts siap, executor akan finalize §Reproduction/§Stacktrace/§Data Shape Baseline/§Root Cause sections di RESEARCH.md (Tasks 4-5 auto).
