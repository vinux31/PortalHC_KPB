# Phase 348: manageassessment-monitoring-med-fix - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 348-manageassessment-monitoring-med-fix
**Areas discussed:** MAM-02 (Pre-Post link), MAM-07 (Tab2 pagination), MAM-08 (delete filter), MAM-09 (Tab2 status filter)

---

## Gray Area Selection

Phase 348 = audit-driven MED fix (spec user-approved). 8/13 MAM punya fix tunggal terkunci. 4 MAM punya pilihan implementasi ("pilih" di spec) → diangkat sebagai gray area.

User memilih: **keempat** gray area + minta rekomendasi semua ("check ulang semua, dan reko semua").

---

## MAM-02 — Pre-Post Monitoring/Export link

| Option | Description | Selected |
|--------|-------------|----------|
| A: tambah param LinkedGroupId ke link tunggal | Ringkas, satu link tetap | |
| B: pecah link per-half (Pre/Post terpisah) | Reuse pola AssessmentMonitoring.cshtml:337-383, jujur 2 sesi | ✓ (Monitoring link) |
| Hybrid (reko Claude) | Monitoring link split per-half (B); Export/PDF LinkedGroupId-aware both-half | ✓ |

**User's choice:** Terima reko Claude — route-by-LinkedGroupId. Prinsip: JANGAN filter Pre-Post by single scheduleDate (3 endpoint: Detail:3165 / Export:4120 / PDF:4503).
**Notes:** Pola split sudah proven existing. Koord MAP-17/349.

---

## MAM-07 — Tab2 pagination

| Option | Description | Selected |
|--------|-------------|----------|
| A: pagination ASLI (Skip/Take + kontrol tiru Tab1) | Robust, section bisa ratusan | ✓ |
| B: drop param mati page/pageSize | Simpel, kalau roster kecil | |

**User's choice:** A — pagination asli. GetWorkersInSection (WorkerDataService.cs:242) saat ini tanpa Skip/Take; tambah + render kontrol tiru _AssessmentGroupsTab.cshtml:348-427.
**Notes:** MAM-06 sudah gate query (load pasca-filter) → pagination bukan over-eng.

---

## MAM-08 — Delete filter-preservation

| Option | Description | Selected |
|--------|-------------|----------|
| A: hx-post re-swap wrapper + hx-include filter | Fits HTMX, preserve filter penuh, no full reload | ✓ |
| B: RedirectToAction route-values | Partial restore (section+unit ala 322), full reload | |

**User's choice:** A — hx-post re-swap. Konversi _TrainingRecordsTab.cshtml:327-349 delete.
**Notes:** Koord MAM-06 — pasca-delete isInitialState tetap false (jangan empty-state). Handler DeleteTraining:586/619 + DeleteManualAssessment:985/1016.

---

## MAM-09 — Tab2 Status filter honesty

| Option | Description | Selected |
|--------|-------------|----------|
| A: relabel "Status Training" saja | Minimal jujur, risk rendah | ✓ |
| B: relabel + fold passed manual-assessment ke Sudah/Belum | Lebih akurat, ubah WorkerDataService logic, semantik ambigu | |

**User's choice:** A — relabel "Status Training". Fold-assessment (B) deferred.
**Notes:** Bug = labeling/kejujuran. Koord MAP-19/349.

---

## Claude's Discretion

- MAM-05 event shape (reuse workerSubmitted status-override vs event workerPendingGrading) — default minimal-risk reuse.
- MAM-02 button layout Tab1 row (hindari clutter) — planner putuskan, prinsip no-single-date wajib.
- Plan split — planner putuskan.

## Deferred Ideas

- MAM-09 combined-status semantics (training+assessment) — backlog/REQ baru.
- MAM-12 extend search ke Kota — MAP-23/349 opsional atau backlog.
- 29 LOW (MAP-01..23) — Phase 349.
