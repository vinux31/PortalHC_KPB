# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- ✅ **v7.1 Export & Import Data** - Phases 176–180 (shipped 2026-03-16)
- ✅ **v7.2 PDF Evidence Report Enhancement** - Phase 181 (shipped 2026-03-17)
- 📋 **v7.2 (loose)** - Phase 182 (unplanned, pending)
- 🚧 **v7.3 Elemen Teknis Shuffle & Rename** - Phases 183–184 (in progress)

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

<details>
<summary>✅ v7.2 PDF Evidence Report Enhancement (Phase 181) — SHIPPED 2026-03-17</summary>

#### Phase 181: PDF Header Coachee Info
**Goal**: The PDF Evidence Report header displays coachee identity (Nama, Unit, Track) above Tanggal Coaching
**Requirements**: PDF-01, PDF-02, PDF-03
**Plans**: 1 plan (complete)

</details>

### Phase 182: CDP/CoachingProton Evidence Column Clarification (Loose Phase)

**Goal**: Fix Evidence column to derive display from Status field instead of EvidencePath
**Requirements**: None (loose phase)
**Depends on**: Phase 181
**Plans**: 1 plan

Plans:
- [ ] 182-01-PLAN.md — Fix EvidenceStatus mapping and view badges

---

### 🚧 v7.3 Elemen Teknis Shuffle & Rename (In Progress)

**Milestone Goal:** Fix cross-package shuffle algorithm to guarantee all Elemen Teknis groups are represented in exam questions, and rename internal code from SubCompetency to ElemenTeknis.

## Phase Details

### Phase 183: Internal Rename SubCompetency → ElemenTeknis
**Goal**: All internal C# code, DB column, and ViewModels use ElemenTeknis instead of SubCompetency
**Depends on**: Phase 182
**Requirements**: RENAME-01, RENAME-02, RENAME-03
**Success Criteria** (what must be TRUE):
  1. EF Core migration runs without error and the PackageQuestion table column is named ElemenTeknis in the database
  2. No C# code references SubCompetency as a property name, variable name, or method name — all references use ElemenTeknis
  3. The ViewModel previously named SubCompetencyScore is named ElemenTeknisScore and all its usages compile and function correctly
  4. The application builds with zero compilation errors after the rename
**Plans**: 1 plan

Plans:
- [x] 183-01-PLAN.md — Rename SubCompetency to ElemenTeknis across models, controllers, views, and DB column

### Phase 184: Shuffle Algorithm — Guaranteed Elemen Teknis Distribution
**Goal**: Cross-package and single-package shuffle guarantees at least one question per Elemen Teknis group, and reshuffles preserve that distribution
**Depends on**: Phase 183
**Requirements**: SHUF-01, SHUF-02, SHUF-03
**Success Criteria** (what must be TRUE):
  1. When a worker starts an exam using a cross-package assessment, the shuffled question set contains at least one question from every Elemen Teknis group present in the packages
  2. When a worker starts an exam using a single-package assessment, the shuffled question set contains at least one question from every Elemen Teknis group present in the package
  3. When HC triggers a reshuffle (single or bulk), the reshuffled question set still contains at least one question per Elemen Teknis group — the distribution guarantee is not lost
  4. The spider web (radar chart) on Results page renders whenever the worker's session has Elemen Teknis score data — it does not silently skip
**Plans**: 2 plans

Plans:
- [ ] 184-01-PLAN.md — ET-aware shuffle algorithm + legacy Results spider web fix
- [ ] 184-02-PLAN.md — ManagePackages ET coverage table + upload warning enhancement

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 181. PDF Header Coachee Info | v7.2 | 1/1 | Complete | 2026-03-17 |
| 182. CDP Evidence Column Clarification | 1/1 | Complete    | 2026-03-17 | - |
| 183. Internal Rename SubCompetency → ElemenTeknis | 1/1 | Complete    | 2026-03-17 | - |
| 184. Shuffle Algorithm Elemen Teknis Distribution | v7.3 | 0/2 | Not started | - |
