# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- ✅ **v7.1 Export & Import Data** - Phases 176–180 (shipped 2026-03-16)
- 🚧 **v7.2 PDF Evidence Report Enhancement** - Phase 181 (in progress)

## Phases

<details>
<summary>✅ v1.0–v5.0 (Phases 1–172) - SHIPPED 2026-03-16</summary>

Phases 1–172 shipped across milestones v1.0–v5.0. See MILESTONES.md for details.

</details>

<details>
<summary>⚠️ v6.0 Deployment Preparation (Phases 173–174) - CLOSED 2026-03-16, no work executed</summary>

Phases 173–174 defined but never executed. Deferred indefinitely.

</details>

<details>
<summary>✅ v7.0 Assessment Terminology Fix (Phase 175) - SHIPPED 2026-03-16</summary>

#### Phase 175: Terminology Rename

**Goal**: All user-facing assessment UI shows "Elemen Teknis" instead of "Sub Kompetensi"
**Requirements**: TERM-01, TERM-02, TERM-03, TERM-04, TERM-05, TERM-06, TERM-07
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.1 Export & Import Data (Phases 176–180) — SHIPPED 2026-03-16</summary>

- [x] Phase 176: Export Records & RecordsTeam (1/1 plans) — completed 2026-03-16
- [x] Phase 177: Import CoachCoacheeMapping (1/1 plans) — completed 2026-03-16
- [x] Phase 178: Export AuditLog (1/1 plans) — completed 2026-03-16
- [x] Phase 179: Export & Import Silabus Proton (1/1 plans) — completed 2026-03-16
- [x] Phase 180: Import Training & Export HistoriProton (1/1 plans) — completed 2026-03-16

</details>

### 🚧 v7.2 PDF Evidence Report Enhancement (In Progress)

**Milestone Goal:** Add coachee identity info (Nama, Unit, Track) to the PDF Evidence Report header on the CDP Deliverable detail page, positioned above Tanggal Coaching in the top-left corner.

## Phase Details

### Phase 181: PDF Header Coachee Info
**Goal**: The PDF Evidence Report header displays coachee identity (Nama, Unit, Track) above Tanggal Coaching
**Depends on**: Phase 180
**Requirements**: PDF-01, PDF-02, PDF-03
**Success Criteria** (what must be TRUE):
  1. When a user generates the PDF Evidence Report, "Nama Coachee" appears in the header above "Tanggal Coaching"
  2. When a user generates the PDF Evidence Report, "Unit Coachee" appears in the header above "Tanggal Coaching"
  3. When a user generates the PDF Evidence Report, "Track (Operator/Panelman Tahun X)" appears in the header above "Tanggal Coaching"
  4. All three coachee fields are positioned in the top-left corner of the header, consistent with existing layout conventions
**Plans**: 1 plan

Plans:
- [ ] 181-01-PLAN.md — Add coachee identity fields to PDF header with side-by-side layout

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 181. PDF Header Coachee Info | 1/1 | Complete    | 2026-03-17 | - |
