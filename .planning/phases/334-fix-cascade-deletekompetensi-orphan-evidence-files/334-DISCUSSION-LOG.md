# Phase 334: Fix Cascade DeleteKompetensi Orphan Evidence Files - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-05-28
**Phase:** 334-fix-cascade-deletekompetensi-orphan-evidence-files
**Mode:** --auto (single-pass, all recommended defaults)

---

## evidencePaths Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Outer tx scope (Phase 333 pattern) | Declare nullable, populate inside tx, access POST commit | ✓ |
| Inside tx scope only | File.Delete inside tx (status quo broken) | |

**Reason:** Phase 333 D-02 pattern verbatim reuse.

---

## File.Delete Position vs Audit Log

| Option | Description | Selected |
|--------|-------------|----------|
| File.Delete BEFORE audit log POST commit | Physical cleanup first | |
| File.Delete AFTER audit log POST commit | Audit-first order, then cleanup | ✓ |

**Reason:** Audit log records "Deleted Kompetensi" as system-of-record event FIRST. Then physical cleanup. Order: DB commit → audit → file delete → Json response. Konsisten dengan Phase 333.

---

## Audit Log Position (INSIDE vs POST tx)

| Option | Description | Selected |
|--------|-------------|----------|
| Move audit INSIDE tx (Phase 332/333 pattern) | D3 atomicity | |
| Preserve audit POST tx (existing position) | Out of scope D3 positioning per §4.8 | ✓ |

**Reason:** Phase 328 §4.8 scope = D2+D6 only (NOT D3 positioning). Minimal scope discipline. Existing audit POST commit acceptable trade-off.

---

## Catch Refactor

| Option | Description | Selected |
|--------|-------------|----------|
| Keep generic Exception + ex.Message + RollbackAsync (status quo) | D6 info leak preserved | |
| DbUpdateException + Exception fallback, no ex.Message, no explicit RollbackAsync | Phase 332/333 pattern + D6 fix | ✓ |

**Reason:** D6 polish — `ex.Message` leak DB constraint detail / table names ke client = info disclosure. Replace dengan generic friendly messages. Pattern Phase 332/333 reuse.

---

## RollbackAsync Explicit Call

| Option | Description | Selected |
|--------|-------------|----------|
| Keep explicit RollbackAsync di catch | Status quo | |
| Hapus — using var disposal handles rollback | Konsisten Phase 333 | ✓ |

**Reason:** `using var transaction` synchronous disposal auto-rollback. Konsisten Phase 333 D-03.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | | |
| Bundle v19.0 | Phase 327 option-b hold | ✓ |

**Reason:** Konsisten Phase 329-333.

---

## Claude's Discretion

- File.Delete inner try/catch `Exception` (bukan IOException) — konsisten Phase 331/332/333
- Logger format `"File.Delete post-commit failed (Kompetensi evidence): {Path}"` — distinguish phase context
- Generic friendly messages: "Gagal hapus kompetensi: ada constraint database yang dilanggar." + "Gagal hapus kompetensi: terjadi kesalahan internal. Hubungi admin."

## Deferred Ideas

- Move audit INSIDE tx — Phase 999.x backlog (D3 positioning out of scope §4.8)
- Authorization attribute — existing convention
- Integration test xUnit — out of scope
- Cron janitor — Phase 999.x backlog
- Phase 335 Worker — separate phase
