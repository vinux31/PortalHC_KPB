---
phase: 367-delete-records-cascade-overhaul
plan: 08
subsystem: views
tags: [htmx, cascade-delete, honest-ui, online-delete, preview-modal, e2e-uat, tab2]
requires:
  - "GET DeletePreview + DeleteTraining/DeleteManualAssessment honest (367-06)"
  - "CompletionDisplayText badge recompute (367-03)"
  - "RecordCascadeDeleteService cascade (367-01/02)"
provides:
  - "Tab Input Records UI: tombol hapus online + modal cascade preview + flash jujur S3"
  - "e2e UAT delete-records-cascade.spec.ts (SC1/SC2/SC4 hijau)"
affects:
  - Views/Admin/Shared/_TrainingRecordsTab.cshtml
  - Controllers/TrainingAdminController.cs
  - tests/e2e/delete-records-cascade.spec.ts
tech-stack:
  added: []
  patterns: ["honest HTMX flash via event DOM dispatch (recordDeleted/recordDeleteFailed)", "modal preview reuse deleteAssessmentModal", "Seed Workflow e2e (backup/restore)"]
key-files:
  created:
    - tests/e2e/delete-records-cascade.spec.ts
  modified:
    - Views/Admin/Shared/_TrainingRecordsTab.cshtml
    - Controllers/TrainingAdminController.cs
key-decisions:
  - "Flash jujur via EVENT DOM HTMX (recordDeleted/recordDeleteFailed), BUKAN xhr.getResponseHeader('HX-Trigger') — header baca NULL di app ini walau HTMX proses trigger (root cause ditemukan UAT browser)"
  - "Sukses: window flag + render afterSettle (persist lintas re-fetch tab; HTMX TAK selalu re-run swapped <script>)"
  - "DeleteTabFailure respons 200 (revisi 06 yg 400) — HTMX skip dispatch event kustom utk 4xx; sinyal jujur = event + flash MERAH (bukan HTTP status). Honest-UI #1 utuh"
  - "Online row dapat tombol hapus (IC-3) + 3 tombol → GET DeletePreview modal (IC-1, hx-confirm native dihapus D-03)"
  - "Pitfall 6: anon 10-prop dijaga; runtime render diverifikasi Playwright (build/test tak cukup)"
requirements-completed: ["#3", "L-06", "L-03", "SC1", "SC2", "SC3", "SC4"]
duration: "~3h (incl debug HTMX flash + UAT iterasi)"
completed: 2026-06-13
---

# Phase 367 Plan 08: Tab Input Records UI + UAT End-to-End Summary

Lapisan yang admin LIHAT — menutup kasus Rino end-to-end: online terlihat + bisa dihapus + UI jujur. Task 1 rewire `_TrainingRecordsTab.cshtml` (tombol online + modal preview + flash S3). Task 2 e2e UAT @5277 (Playwright + Seed Workflow) — **3/3 PASS** setelah root-cause flash ditemukan + diperbaiki di browser.

**Tasks:** 2/2 | **Files:** 1 created + 2 modified | **UAT:** 3/3 Playwright hijau (Seed Workflow auto backup/restore)

## What was built

- **IC-3 online deletable:** tombol `btn-outline-danger` + `bi-trash` ke baris online (NO Edit online).
- **IC-1 preview-not-direct:** 3 tombol (online+training+manual) → `hx-get DeletePreview` → modal `#cascadePreviewModal`; `hx-confirm` native DIHAPUS (D-03).
- **S1 modal shell:** reuse pola `deleteAssessmentModal` (bg-danger header, scrollable, spinner aria-live), body `#cascade-preview-body` diisi `_CascadePreviewModal` (06).
- **S3/IC-2 flash JUJUR:** listener EVENT DOM HTMX — `recordDeleted` (sukses) → flag + render hijau afterSettle; `recordDeleteFailed` (gagal) → flash MERAH segera (`event.detail.pesan`). NEVER hijau saat gagal (#1).
- **DeleteTabFailure → 200** (revisi 06 yg 400) supaya HTMX dispatch event recordDeleteFailed.
- **e2e spec** Seed Workflow (backup→seed renewal-chain+online>7hari→restore) + UAT dual-path.

## Verification (UAT 3/3 PASS @localhost:5277)

- **SC1** cascade hapus sukses → DB BERSIH per record (queryScalar `induk=0 child=0`) + flash HIJAU + list re-fetch bersih. ✓
- **SC2** modal preview tampil korban PERSIS (Induk + "Turunan renewal" UAT367 Renewal Anak) — no blokir. ✓
- **SC4** online >7hari (kasus Rino) TAMPIL + terhapus tuntas dari DB. ✓
- **SC3** flash MERAH saat gagal = kontrak honest-split (recordDeleteFailed→alert-danger) diuji `RecordCascadeUiTests` (06) + listener event DOM (UAT-fixed reliable).
- **SC5** badge↔list konsisten ("13 record (11 assessment + 2 training)" = 13 baris) live-verified + `BadgeRecomputeTests` (03); dup guard `DuplicateGuardTests` (07).
- **Pitfall 6:** render runtime CLEAN (0 console error, no RuntimeBinderException, anon 10-prop utuh) — verified browser.
- Seed Workflow: auto backup→seed→restore (teardown Layer 4 OK, journal cleaned). `dotnet build` 0 err; 209 quick.

## Deviations from Plan

**[Rule 1 - Bug, UAT-found] Flash via event DOM, bukan baca HX-Trigger header** — UAT browser membuktikan `xhr.getResponseHeader('HX-Trigger')` baca NULL di app ini (walau HTMX proses trigger → re-fetch jalan). Ganti ke listener event DOM HTMX (`recordDeleted`/`recordDeleteFailed`) — andal. Sukses render afterSettle (HTMX tak re-run swapped `<script>`).

**[Rule 1 - Bug, UAT-found] DeleteTabFailure 400→200** — HTMX TAK dispatch event kustom utk respons 4xx → flash merah tak render. Revisi 06 (yg set 400) jadi 200; sinyal jujur = event + flash merah, bukan HTTP status. Honest-UI #1 tetap utuh + lebih andal.

**[Rule 1 - Test infra] e2e seed/selector fixes** — Users (bukan AspNetUsers), BannerColor required col, nested-table selector scope, pressSequentially (trigger HTMX debounce). Ditemukan saat menjalankan spec live.

**Total deviations:** 3 (semua UAT-driven bug fix — root cause hanya ketahuan runtime browser, Pitfall 6 confirmed). **Impact:** Positif — honest-UI flash kini ANDAL (bukan cuma build-pass); UAT membuktikan E2E.

## Issues Encountered / Known Limitations

- **HTMX header-read unreliable** di app ini → semua sinyal HTMX→JS WAJIB via event DOM dispatch (bukan getResponseHeader). Lesson untuk flash/listener berikutnya.
- SC3 red flash tak di-automate di spec (trigger gagal deterministik sulit) → unit + listener; manual/visual saat ada constraint nyata.

## Self-Check: PASSED

- Online deletable (IC-3) + 3 tombol→DeletePreview (IC-1, hx-confirm dihapus) + modal shell (S1) ✓.
- Flash jujur event DOM (sukses hijau/gagal merah) ✓; DeleteTabFailure 200 ✓.
- UAT 3/3 Playwright hijau (SC1 DB-verified + SC2 preview + SC4 Rino) ✓; Seed Workflow restore clean ✓.
- Pitfall 6 render clean ✓; anon 10-prop utuh ✓; build 0 err; 209 quick ✓; Migration=FALSE ✓.
- Commits `7ef3e69c` (Task 1 view+spec) + `90b7f138` (UAT honest-flash fix) ✓.

**Phase 367 (8/8 plan) COMPLETE — kasus Rino ditutup end-to-end: cascade 100% sampai akar, online deletable, UI jujur, preview konfirmasi, guard duplikat.**
