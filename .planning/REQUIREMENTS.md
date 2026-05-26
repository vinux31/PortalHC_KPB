# Requirements — v18.0 Cascade Delete Hardening

**Milestone:** v18.0
**Started:** 2026-05-26
**Status:** Active

## Goal

Tutup oversight Phase 321 (model `AssessmentEditLog` baru, FK Restrict) di Phase 312 cascade (`DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup`). Bukti repro di Dev: `AssessmentSession` Id 1 (0 edit log) sukses dihapus; Id 2+5 (ada edit log) gagal dengan exception "Gagal menghapus assessment".

## v18.0 Requirements

### Cascade Delete

- [ ] **CASCADE-01**: Admin/HC dapat menghapus `AssessmentSession` (single, group, atau Pre-Post group) yang sudah pernah di-edit soalnya — `AssessmentEditLogs` ikut ter-cascade tanpa FK Restrict exception.

  **Acceptance criteria:**
  1. `DeleteAssessment(id)`, `DeleteAssessmentGroup(id)`, `DeletePrePostGroup(linkedGroupId)` di `Controllers/AssessmentAdminController.cs` masing-masing tambah `RemoveRange(AssessmentEditLogs)` sebelum cascade existing
  2. Session belum pernah di-edit → tetap sukses (no regression)
  3. Session sudah di-edit ≥1 soal → sukses, `AssessmentEditLogs` ikut terhapus
  4. Audit log `DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup` tetap tercatat normal
  5. Transaction scope existing (line 2040, 2184, 2313) tetap membungkus delete + cascade — rollback bersih saat exception
  6. Smoke test 3 skenario: (a) session no-edits → delete OK, (b) session 1+ edits → delete OK, (c) group dengan campuran sibling no-edits + edits → delete OK
  7. Tidak ubah schema DB, model class, FK definition, atau migration

## Future Requirements (deferred)

Tidak ada — milestone hotfix-scope. Bisa expand via `/gsd-add-phase` bila ada bug cascade serupa ditemukan saat audit.

## Out of Scope (explicit)

- **Audit endpoint delete lain** (DeleteCategory, DeletePackage, DeleteQuestion, DeleteWorker, DeleteTraining, DeleteManualAssessment, DeleteOrganizationUnit, DeleteCoachingSession, BudgetTrainingDelete, dll.) — fokus milestone hanya 3 endpoint `DeleteAssessment*` yang directly affected oleh Phase 321 `AssessmentEditLog`. Audit luas masuk milestone berikutnya bila perlu.
- **Refactor cascade helper** (extract reusable `CascadeAssessmentSessionDependents(sessionIds)` helper) — tidak perlu untuk 3 endpoint; tunggu ada signal pattern reuse.
- **Migration ubah FK Restrict → Cascade DB-level** — endpoint cascade lebih eksplisit + audit-friendly daripada DB cascade silent.
- **UI surface old assessment (filter `>= 7 hari`)** di `ManageAssessmentTab_Assessment` line 115 — separate UX issue, masuk backlog tersendiri.

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| CASCADE-01 | 323 | Pending |

**Active mapped: 1/1 ✓ — Orphans: 0 — Duplicates: 0**

---

*Requirements created: 2026-05-26*
