---
title: "PreTest OJT GAST Cilacap — investigate migration loss + decide restore strategy"
status: pending
priority: P2
source: "promoted from /gsd-note (2026-05-29-pretest-ojt-gast-cilacap-lost.md)"
created: 2026-05-29
theme: incident-postmortem
---

## Goal

Investigasi root cause loss PreTest OJT GAST Cilacap (30 Mar 2026) yang hilang dari Dev DB pasca update code antara Mar–May 2026, lalu putuskan strategi restore dan pasang guardrail cegah recurrence.

## Context

Source note: `.planning/notes/2026-05-29-pretest-ojt-gast-cilacap-lost.md`

State 29 Mei 2026:
- Dev DB 10.55.3.3 → 0 row `AssessmentSessions` Title mengandung "Pre Test" / "OJT GAST"
- AuditLog 0 entry DeleteAssessment / DeleteAssessmentGroup untuk PreTest (silent loss via migration, bukan manual delete)
- Backup user: `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`
- Judul aktual Excel: `OJT GAST - GTO & SRU RU IV` — TIDAK mengandung kata "Pre Test"/"Cilacap" (penyebab search di Dev 0 hasil; naming convention beda dari PostTest counterpart `Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`)
- 13 peserta identik dengan PostTest Cilacap 20 May (sessionId Dev 9-21)
- PostTest counterpart aman di Dev DB (dibuat 19 May di skema baru post-update)

Numerical:
- Pre avg 53.92 → Post 79.38 (gain +25.46)
- Pass count 1/13 → 13/13 (12 peserta naik Fail→Pass)
- CSV comparison: `downloads/Post Test OJT Cilacap/04-Pre-vs-Post-Comparison.csv`

Cross-link: lihat todo `001-gap-ux-assessment-monitoring.md` — Gap #5 (Excel summary tidak include Elemen Teknis) bikin restore PreTest tidak comprehensive. Kalau Gap #5 fixed dulu, future loss restore-able dari Excel backup.

## Acceptance Criteria

- [ ] Investigate: git log `Models/AssessmentSession.cs` + `Migrations/*` + `Data/SeedData.cs` antara 2026-03-30 dan 2026-05-19 — identifikasi commit yang introduce `AssessmentType` / `LinkedGroupId` / schema change yang hapus Pre data
- [ ] Confirm: apakah Pre loss disebabkan (a) migration drop column without preserve, (b) `EnsureCreated` reset, (c) seed reset, atau (d) manual cleanup. Dokumentasikan di `.planning/notes/2026-05-29-pretest-ojt-gast-cilacap-lost.md`
- [ ] Decide restore strategy — pilih satu:
  - Option A: re-import via `Admin/AddManualAssessment` endpoint (1 entry per peserta, score total only, no elemen teknis). Hilang spider data tapi metadata + score tersimpan permanen di Dev DB
  - Option B: skip restore, treat Pre sebagai data archive di Excel saja. Bandingan Pre/Post via CSV manual `04-Pre-vs-Post-Comparison.csv`
  - Option C: tunggu Gap #5 (Excel breakdown Elemen Teknis) shipped, lalu re-export PostTest dgn elemen teknis utuh — Pre tetap hilang spider tapi PostTest jadi comprehensive baseline future
- [ ] Implement chosen option
- [ ] Guardrail recurrence: tambah `pre-deploy` hook backup `AssessmentSessions` + `AssessmentAttemptHistory` + `PackageUserResponses` ke `.bak` SQL Server dump SEBELUM jalan migration. Document di `docs/DEV_WORKFLOW.md`
- [ ] Naming convention standardization: dokumentasikan format judul "{Pre|Post} Test {Track} {Lokasi}" + enforce `LinkedGroupId` auto-pair (Pre/Post di create form admin) supaya `ExportGainScoreExcel` future aktif

## Risk

- Migration culprit mungkin sudah merged main puluhan commit yang lalu — rebase kontekstual perlu
- Option A re-import = nambah row "fake" historis (CompletedAt 30 Mar) — pastikan AuditLog tag entry sebagai `ManualImport-Backfill` bukan natural completion
- Guardrail backup pre-deploy butuh koordinasi tim IT (server akses)
