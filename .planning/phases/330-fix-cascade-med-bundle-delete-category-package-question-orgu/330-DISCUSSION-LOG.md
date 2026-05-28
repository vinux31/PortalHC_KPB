# Phase 330: Fix Cascade MED Bundle - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-28
**Phase:** 330-fix-cascade-med-bundle-delete-category-package-question-orgu
**Areas discussed:** Auto-mode — all gray areas auto-selected, all recommended defaults applied
**Mode:** --auto (single-pass, no interactive questions)

---

## NotificationService D3 Audit Log

| Option | Description | Selected |
|--------|-------------|----------|
| Add audit log | Inject AuditLogService ke NotificationService, log tiap notification delete | |
| Skip audit log, refactor catch only | Ganti catch(Exception) → catch(DbUpdateException). No injection change. | ✓ |

**Auto-selected:** Skip audit log, refactor catch only
**Reason:** Phase 328 §5 row 5 menandai D3 sebagai optional ("mungkin overkill"). NotificationService tidak punya _auditLog, inject baru = scope creep dari mechanical S fix. Refactor catch ke DbUpdateException cukup close D6-equivalent.

---

## DeletePackage Restructure Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Full restructure | Move seluruh try/catch block agar SaveChangesAsync ter-cover | |
| Minimal wrap | Tambah try/catch hanya sekitar SaveChangesAsync baris L5122 | ✓ |

**Auto-selected:** Minimal wrap
**Reason:** Scope minimal, tidak mengganggu existing audit log try block. Phase 328 prescription: "Wrap SaveChangesAsync di try/catch DbUpdateException" — tidak minta full restructure.

---

## DeleteOrganizationUnit AJAX Error Path

| Option | Description | Selected |
|--------|-------------|----------|
| Ignore AJAX in catch | Hanya TempData path di catch, AJAX callers dapat 500 | |
| Handle dual path in catch | Catch handle IsAjaxRequest → JSON error + non-AJAX → TempData | ✓ |

**Auto-selected:** Handle dual path in catch
**Reason:** Konsistensi dengan existing IsAjaxRequest pattern di method ini. AJAX caller (tree drag-drop) harus dapat JSON error message, bukan 500.

---

## DeleteQuestion Audit Log Parameters

| Option | Description | Selected |
|--------|-------------|----------|
| Log at session level | entityId = assessmentId, entityType = "AssessmentSession" | |
| Log at package level | entityId = packageId, entityType = "PackageQuestion" | ✓ |

**Auto-selected:** Log at package level
**Reason:** Question termasuk dalam Package scope. DeletePackage logs ke assessmentId/AssessmentPackage, DeleteQuestion logs ke packageId/PackageQuestion — hirarki konsisten.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | Push Phase 330 segera setelah ship | |
| Bundle v19.0 | Bundle dengan 325+326+327+328+329+330 batch push | ✓ |

**Auto-selected:** Bundle v19.0
**Reason:** Phase 329 D-07 locked: hold push per Phase 327 option-b hold, tunggu IT availability. Phase 330 bundle sama.

---

## Claude's Discretion

- Exact error message strings (format "Tidak bisa hapus {entity}: masih ada data yang berelasi.") — auto-standardized untuk konsistensi
- _logger availability check di OrganizationController — delegated ke executor (verify via grep AdminBaseController saat Task 2)
- Audit log wrap pattern (inner try/catch(Exception)) — follow DeletePackage verbatim

## Deferred Ideas

- DeleteWorker HIGH fix (D2+D5+D7) — terpisah Phase 331+
- NotificationService audit log injection — overkill, deferred unless stakeholder request
- Soft delete proper — Phase 325 D-04 deferred v20.0+
