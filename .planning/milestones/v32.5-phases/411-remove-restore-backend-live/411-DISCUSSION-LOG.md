# Phase 411: Remove + Restore Backend Live - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-06-21
**Phase:** 411-remove-restore-backend-live
**Areas discussed:** Hard-delete vs eager-UPA, RemovalReason wajib/opsional, SignalR, Fix stub DeleteAssessmentPeserta

---

## Hard-delete vs eager-UPA (410)

| Option | Selected |
|--------|----------|
| UPA bukan data → hard-delete (cascade bersihkan UPA) | ✓ |
| Ada UPA → soft-remove (konservatif) | |

**Choice:** UPA bukan data → hard-delete. Threshold = StartedAt==null & 0 PackageUserResponse. RecordCascadeDeleteService cascade UPA.

---

## RemovalReason wajib/opsional

| Option | Selected |
|--------|----------|
| Wajib saat soft-remove, opsional hard-delete | ✓ |
| Selalu wajib | |
| Selalu opsional | |

**Choice:** Wajib saat soft-remove (audit PLIV-03), opsional hard-delete.

---

## SignalR examRemoved/participantRemoved

| Option | Selected |
|--------|----------|
| Defer ke 412 | ✓ |
| Emit di 411 | |

**Choice:** Defer ke 412 (konsisten 410 D-04). 411 backend+audit only.

---

## Fix stub DeleteAssessmentPeserta

| Option | Selected |
|--------|----------|
| Delegasi ke RemoveParticipantLive | ✓ |
| Hapus tombol mati | |

**Choice:** Delegasi (spec §B5) — tombol existing hidup, service bersama.

## Claude's Discretion
- Bentuk service bersama, verifikasi/extend cascade UPA, bentuk JSON outcome, penempatan validasi reason, cakupan test.

## Deferred Ideas
- SignalR + force-kick + modal + panel → 412; Playwright/suite → 413; AssessmentAttemptHistory FK-User cascade caution.
