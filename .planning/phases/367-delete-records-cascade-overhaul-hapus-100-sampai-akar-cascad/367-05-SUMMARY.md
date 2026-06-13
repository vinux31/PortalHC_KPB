---
phase: 367-delete-records-cascade-overhaul
plan: 05
subsystem: controllers
tags: [cascade-delete, no-blocker, cert-file, role-guard, engine-route, tab1]
requires:
  - "RecordCascadeDeleteService.ExecuteAsync (367-02)"
  - "RecordCascadeDeleteService.CollectCascadeIds (367-01)"
  - "StandardGroupSiblingPredicate (367-04)"
  - "ImageFileCleanup.DeleteUnreferencedAsync (366)"
provides:
  - "3 endpoint tab 1 no-blocker (L-03): DeleteAssessment/Group/PrePost cascade turunan renewal via engine"
  - "#19 file sertifikat manual dihapus post-commit di tab 1"
  - "HC role-tier guard diperluas ke seluruh set cascade (cegah bypass via ancestor)"
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: ["endpoint thin-wrapper delegasi cascade ke engine (preview==execute)", "guard role-tier atas full cascade set", "partial-failure-safe cleanup (ref-checked image + committed-scoped cert)"]
key-files:
  created:
    - HcPortal.Tests/CertFileTab1Tests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Engine-route (user pick): 3 endpoint delegasi ke ExecuteAsync — zero drift, preview==execute. Single=1 call, grup=loop per-sibling+deletedSet. Trade-off: grup atomik per-sibling (bukan 1-tx semua)"
  - "Image SOAL (Opsi B) di-collect utk SEMUA cascade session node (root+turunan via CollectCascadeIds), bukan cuma root — engine tak sentuh image SOAL, root-only akan orphan image turunan"
  - "FIX KRITIS (adversarial verify): EnsureCanDeleteAsync diperluas ke full cascade set (cascadeSessions), bukan cuma root/siblings — engine tak punya role guard, HC bisa hapus turunan Completed/ber-jawaban via ancestor. Guard di-posisi LAST sebelum engine (window TOCTOU minimal). Admin override tetap (L-03 no-blocker utuh utk Admin)"
  - "Group partial-failure: break+flag, cleanup+audit SELALU jalan, cert scoped ke deletedSet (DeleteCertFiles tanpa ref-check ≠ ImageFileCleanup), partial-aware msg + *Partial audit"
  - "WR-01 in-tx re-check di-drop; dimitigasi guard-last-before-engine; residual TOCTOU diterima (no-blocker policy + Admin override)"
requirements-completed: ["#19", "L-03", "L-08"]
duration: "~3h (incl 2 adversarial verify workflow)"
completed: 2026-06-13
---

# Phase 367 Plan 05: Tab-1 Delete Engine-Route (No-Blocker L-03 + Cert #19 + HC-Guard) Summary

3 endpoint hapus tab 1 (`DeleteAssessment` :2189, `DeleteAssessmentGroup`, `DeletePrePostGroup`) di-rewire dari inline-cascade-yang-MEMBLOKIR-saat-ada-turunan-renewal menjadi **engine-route**: delegasi penuh ke `RecordCascadeDeleteService.ExecuteAsync`. Konsistensi no-blocker lintas tab 1↔tab 2 + bersihkan file sertifikat orphan.

**Tasks:** 2/2 | **Files:** 1 created + 1 modified | **Tests:** 4 [Fact] baru (CertFileTab1Tests) | **Migration:** FALSE

## What was built

- **L-03 no-blocker:** pre-check renewal BLOKIR (fase 325 `refTr+refAs>0→return`, fase 329 sibling/group variant) DIHAPUS dari 3 endpoint. Turunan renewal kini IKUT terhapus via engine (fix kasus Rino #3). Single = `ExecuteAsync("session", id, …)`; grup = loop `ExecuteAsync` per sibling/group-session + `deletedSet` (cegah dobel).
- **#19 cert:** `ManualSertifikatUrl` di-collect + `File.Delete` post-commit warn-only (helper `DeleteCertFiles`, confined webroot V12). Engine juga hapus cert per committed node (idempotent).
- **Image SOAL 366 (Opsi B) preserved:** helper `CollectQuestionImagePathsAsync` collect ImagePath utk SEMUA cascade session node (root+turunan) SEBELUM engine; `ImageFileCleanup.DeleteUnreferencedAsync` post-commit (D-05 AnyAsync). Engine tak sentuh image SOAL.
- **HC role-tier guard diperluas (FIX KRITIS):** `EnsureCanDeleteAsync` kini di-pass `cascadeSessions` (seluruh set cascade), bukan cuma root/siblings → HC diblok bila ADA node cascade Completed/ber-jawaban. Posisi LAST sebelum engine. Admin tetap bypass (no-blocker utuh).
- **Group partial-failure-safe:** `break`+`partialFailure` flag; image cleanup (ref-checked) + cert (scoped `deletedSet`) + audit SELALU jalan; pesan partial-aware + audit action `*Partial` + `FailedAt`.

## Verification

- `dotnet build` — 0 error.
- **199 quick + 72 integration = 271 pass** (incl 4 CertFileTab1Tests baru, no regression dari 270→271).
- **2× adversarial verify workflow (multi-agent, ultracode):**
  - Sweep 1 (15 agent): temukan **1 CRITICAL** (HC-guard bypass via renewal descendant, dikonfirmasi 3 lensa high-conf) + 4 medium/low + 2 refuted → SEMUA confirmed di-fix.
  - Sweep 2 re-verify fix (5 agent): **0 functional/security defect**, hanya 2 LOW dead-local (`siblingIds`/`groupIds`) → dihapus.
- Self-bug caught (di luar workflow): `DeleteCertFiles` tanpa ref-check akan hapus cert sesi surviving saat partial → di-scope ke `deletedSet`.

## Deviations from Plan

**[Rule 4 - Arsitektur, user-approved] Engine-route (bukan inline cascade)** — Plan tawarkan 2 jalur ("route ke engine" ATAU "inline traversal"). Fork di-eskalasi ke user (AskUserQuestion) → user pilih **engine-route** (anti-drift, preview==execute). 3 endpoint jadi thin-wrapper.

**[Rule 1 - Kelengkapan] Image SOAL turunan** — Plan fokus root cert/image. Engine-route hapus turunan renewal yg punya image SOAL → root-only collect akan orphan. Diperluas: collect image utk SEMUA cascade session node via `CollectCascadeIds`.

**[Rule 1 - Keamanan, dari adversarial verify] HC-guard atas full cascade set** — Plan asumsikan guard root-only cukup ("WR-01 subsumed by EnsureCanDeleteAsync"). Adversarial verify buktikan engine tak punya role guard → HC bisa hapus turunan Completed via ancestor (privilege bypass). Guard diperluas ke cascade set.

**[Rule 1 - Robustness, dari adversarial verify] Partial-failure cleanup + cert scoping + audit** — Grup per-sibling tx → partial commit mungkin. Cleanup/audit dibuat selalu-jalan + cert di-scope committed-only + audit partial-aware.

**Total deviations:** 4 (1 user-approved arsitektur, 3 hardening dari adversarial verify). **Impact:** Positif — menutup 1 privilege-escalation CRITICAL + 2 orphan-file MED yg tak terdeteksi build/test biasa.

## Issues Encountered / Known Limitations

- **LOW (deferred-documented):** single `DeleteAssessment` bisa hapus sesi Pre/Post secara TAK LANGSUNG bila ia turunan renewal (PostTest carry renewal FK), tinggalkan partner half-orphan (LinkedGroupId retained → phantom group). Uncommon data shape + recoverable (`DeletePrePostGroup` masih bisa bersihkan). Fix benar = engine-level (expand ke LinkedGroupId partner) → kandidat backlog, DI LUAR scope 05. Lihat [[project_delete_inputrecords_silentfail]].
- **Residual (accepted):** WR-01 in-tx responseCount re-check di-drop; window TOCTOU root menyempit (guard last-before-engine) tapi tak nol. Diterima: policy no-blocker + Admin override + low-prob.
- **Trade-off (by design):** grup atomik per-sibling, bukan 1-tx semua. Partial-failure kini bersih (cleanup+audit+pesan), tapi sebagian sesi bisa terhapus sebelum gagal.

## Self-Check: PASSED

- 3 endpoint route ke `cascade.ExecuteAsync` ✓; pre-check renewal BLOKIR hilang (grep "menggunakan record ini sebagai sumber renewal" = 0) ✓; `ManualSertifikatUrl` di 3 endpoint ✓; image 366 label ketiga endpoint match ✓.
- `EnsureCanDeleteAsync(…, cascadeSessions)` (full set) di 3 endpoint ✓; Admin override utuh ✓.
- build 0 err ✓; 271/271 test ✓; 2 verify workflow clean ✓; Migration=FALSE ✓.
- Commit code+test `d9f86dd6` (CertFileTab1Tests git add eksplisit — untracked lesson) ✓.

Ready for 367-06 (rewire hapus tab Input Records / TrainingAdminController).
