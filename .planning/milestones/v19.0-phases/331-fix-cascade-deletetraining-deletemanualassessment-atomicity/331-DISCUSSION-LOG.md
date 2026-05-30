# Phase 331: Fix Cascade DeleteTraining + DeleteManualAssessment Atomicity - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-28
**Phase:** 331-fix-cascade-deletetraining-deletemanualassessment-atomicity
**Areas discussed:** Auto-mode — all gray areas auto-selected, all recommended defaults applied
**Mode:** --auto (single-pass, no interactive questions)

---

## Transaction Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Whole method in tx | Wrap pre-check + Remove + SaveChanges + AuditLog dalam tx | |
| Tx only Remove+SaveChanges+AuditLog (pre-check di luar) | Pre-check read-only di luar tx, mutation operations + audit log inside | ✓ |

**Auto-selected:** Pre-check di luar tx
**Reason:** Pre-check = read-only count query, tidak butuh tx wrap. Early return TempData friendly tanpa tx overhead. Pre-check pindah masuk tx = tx scope membesar tanpa benefit.

---

## File.Delete Failure Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Rollback DB kalau file delete fail | Re-insert record + roll back audit log | |
| Log warning, accept orphan file | DB commit sukses, file fail = warn only, orphan file cleanup manual | ✓ |

**Auto-selected:** Log warning, accept orphan file
**Reason:** Tx sudah committed, tidak bisa rollback. Re-insert record = race condition + audit log duplicate. Orphan file di disk ACCEPTABLE (cleanup manual via janitor cron later). DB perspective sukses adalah benar.

---

## Audit Log Position

| Option | Description | Selected |
|--------|-------------|----------|
| Inside tx (sebelum CommitAsync) | Audit log atomic dengan Remove | ✓ |
| Outside tx (post-commit fire-and-forget) | Audit log non-blocking | |

**Auto-selected:** Inside tx
**Reason:** Konsisten dengan Phase 323 D-04 pattern. Audit log fail = rollback DB juga, prevent partial state (record removed tapi audit gone).

---

## File Path Capture Timing

| Option | Description | Selected |
|--------|-------------|----------|
| Capture after Remove | Akses record.SertifikatUrl setelah Remove panggil | |
| Capture before Remove (string variable) | Assign ke string var sebelum Remove, gunakan post-commit | ✓ |

**Auto-selected:** Capture before Remove
**Reason:** Record/Session object akan detached/dispose post-Remove. String variable value-typed sudah captured value, safe untuk dipakai post-commit.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | Push Phase 331 segera setelah ship | |
| Bundle v19.0 | Bundle dengan 325+326+327+329+330+331 batch push | ✓ |

**Auto-selected:** Bundle v19.0
**Reason:** Phase 329 D-07 + Phase 330 D-07 locked: hold push per Phase 327 option-b hold, tunggu IT availability. Phase 331 bundle sama.

---

## Claude's Discretion

- Exact error message strings — preserve existing "Gagal hapus: ada constraint database yang dilanggar." (Phase 325 P05 verbatim)
- Logger warning format — `_logger.LogWarning(ex, "File.Delete post-commit failed: {Path}", path)` standard struct logging
- File.Delete try/catch inner block — wrap dengan Exception (bukan IOException specific) — file system bisa fail beragam jenis

## Deferred Ideas

- Integration test xUnit untuk Delete endpoint — out of scope MED-mechanical fix
- Cron janitor cleanup orphan file — Phase 999.x backlog
- Phase 332-335 (Bagian + CoachingSession + Kompetensi + Worker) — separate phase per roadmap
