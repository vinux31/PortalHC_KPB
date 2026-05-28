# Phase 332: Fix Cascade DeleteBagian File Atomicity - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-05-28
**Phase:** 332-fix-cascade-deletebagian-file-atomicity
**Mode:** --auto (single-pass, all recommended defaults)

---

## Transaction Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Whole method in tx | Wrap pre-check + confirm + file collection + Remove + Save + Audit | |
| Tx around RemoveRange+Remove+SaveChanges+AuditLog (pre-check + collection di luar) | Phase 331 pattern reuse | ✓ |

**Reason:** Pre-check + confirm = read-only Json early return, tidak butuh tx. File path collection = string list, value-typed safe pre-tx.

---

## Multi-File Loop Failure Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Break on first File.Delete fail | First fail = stop loop | |
| Continue all + warn per file | Iterate full, log warn per fail, no early break | ✓ |

**Reason:** DB sudah committed = all files SHOULD be deleted. Skip subsequent file karena 1 fail = orphan disk bertambah, bukan berkurang.

---

## AJAX Error Response

| Option | Description | Selected |
|--------|-------------|----------|
| TempData["Error"] + Redirect | Non-AJAX path | |
| Json success=false + message | Endpoint AJAX-only contract | ✓ |

**Reason:** Endpoint sudah Json-pure (5 existing Json returns). Konsistensi response shape.

---

## Audit Log Position

| Option | Description | Selected |
|--------|-------------|----------|
| Inside tx | Atomic dengan delete (Phase 331 pattern) | ✓ |
| Outside tx (post-commit) | Fire-and-forget | |

**Reason:** Konsisten Phase 323 D-04 + Phase 331 D-02. Existing audit log L353-364 sudah punya inner try/catch — preserve verbatim.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | Push Phase 332 segera | |
| Bundle v19.0 | Bundle dengan 325+...+332 | ✓ |

**Reason:** Phase 327 option-b hold persisted. Konsisten Phase 329-331.

---

## Claude's Discretion

- File.Delete inner try/catch wrap `Exception` (bukan IOException specific) — file system fail beragam jenis
- Logger warning format struct: `_logger.LogWarning(ex, "File.Delete post-commit failed (KKJ/CPDP): {Path}", path)` — distinguish KKJ vs CPDP via label di message
- Path.Combine pattern verbatim L326 + L342 (TrimStart('/') existing)

## Deferred Ideas

- Integration test xUnit — out of scope
- Cron janitor cleanup orphan file — Phase 999.x backlog
- Refactor pre-check ke shared helper — scope creep
