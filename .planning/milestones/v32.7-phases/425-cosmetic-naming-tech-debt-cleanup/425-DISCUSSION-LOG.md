# Phase 425: Cosmetic / Naming / Tech-Debt Cleanup - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-06-24
**Phase:** 425-cosmetic-naming-tech-debt-cleanup
**Areas discussed:** CLN-03 dead-field, CLN-04 timing scope, CLN-05 ModelState scope, CLN-02 manual cross-validate

---

## Area selection
All 4 decision-areas selected (CLN-01 left as cosmetic discretion).

## CLN-03 — AssessmentPhase dead-field
| Pilihan | User pick |
|---|---|
| RESERVED XML-doc (migration=FALSE) / DROP via migration | **RESERVED XML-doc (migration=FALSE)** |
Notes: 0 referensi app (cuma AssessmentSession.cs:180 + migration snapshots). RESERVED = nol-risiko skema di fase cleanup.

## CLN-04 — tech-debt timing scope
| Pilihan | User pick |
|---|---|
| Aman-saja (konsolidasi timer; defer token+write-on-GET) / Penuh | **Aman-saja** |
Notes: konsolidasi 4 situs formula timer (CMPController :1191/:1564/:1642/:4661) ke ExamTimeRules (424). FLOW-08 token + FLOW-10 write-on-GET → backlog (by-design+mitigated).

## CLN-05 — ModelState konvensi scope
| Pilihan | User pick |
|---|---|
| Minimal guard-helper / Penuh DTO refactor / Diskresi | **Minimal guard-helper bersama** |
Notes: tanpa ubah signature action; DTO refactor penuh ditolak (risiko regresi luas).

## CLN-02 — manual entry cross-validate
| Pilihan | User pick |
|---|---|
| Peringatan server-side non-blocking / Blokir simpan | **Peringatan server-side non-blocking** |
Notes: di AddManualAssessment POST; IsPassed vs Score>=PassPercentage mismatch → warn + tetap simpan (tidak auto-override).

## Claude's Discretion
- CLN-01 label/doc (ValidUntil, Status 7-nilai, AssessmentPackageId sentinel, LinkedSessionId FK doc).
- Nama/lokasi guard-helper; teks peringatan; teks XML-doc RESERVED.

## Deferred
- DROP AssessmentPhase; TokenVerifiedAt (FLOW-08); write-on-GET refactor (FLOW-10); DTO refactor penuh.

## Outcome
**migration=FALSE.** Fase low-risk, semua keputusan konservatif (sesuai ethos fase terakhir).
