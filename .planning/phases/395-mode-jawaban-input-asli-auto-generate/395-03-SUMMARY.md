---
phase: 395-mode-jawaban-input-asli-auto-generate
plan: 03
subsystem: inject-assessment / view-step5-answers-preview
tags: [inject, step5, input-asli, auto-generate, preview, answers-serialize, lbl-02, e2e, razor-js]
requires:
  - "InjectAssessmentController.PreviewInjectScore (Plan 02, POST /Admin/PreviewInjectScore, [FromBody] InjectPreviewRequest → InjectPreviewResult)"
  - "InjectAssessmentController.InjectAssessment POST commit aktual (Plan 02, InjectBatchAsync + FindBlockedAutoGenNips guard)"
  - "InjectAssessmentViewModel.AnswersJson + InjectAnswerVM + InjectWorkerAnswersVM (Plan 02)"
  - "InjectAssessmentService.BuildAutoGenAnswers / ComputeAutoGenSeed (Plan 01, server-otoritas auto-gen)"
  - "Views/Admin/InjectAssessment.cshtml wizard 6-langkah + #QuestionsJson + injQuestions[] client-state (Phase 394)"
provides:
  - "Langkah 5 sub-komponen IIFE 1-pekerja-per-layar (roster + Prev/Next + toggle mode + form MC/MA/Essay + Pratinjau + BLOCKING)"
  - "Hidden #AnswersJson + serialize JSON.stringify(buildWorkerAnswersPayload()) di submit-listener yang SAMA dgn #QuestionsJson (anti silent-grade-0)"
  - "Permukaan Pratinjau (POST PreviewInjectScore + CSRF) — skor final % + Lulus/Tidak + overshoot, NO cert#"
  - "State BLOCKING (target>ceiling) + 'Beralih ke input asli' (D-08.3/D-10)"
  - "Carry-in LBL-02: injTypeLabel + pesan validasi 'Single Answer'/'Multiple Answer'"
  - "tests/e2e/inject-assessment-395.spec.ts (e2e: input-asli + auto-gen + commit + #AnswersJson terisi)"
affects:
  - "Phase 396 (Import Excel) — reuse permukaan #AnswersJson + jalur commit + PreviewInjectScore"
  - "Phase 397 (link Pre/Post) — wizard surface yang sama"
tech-stack:
  added: []
  patterns:
    - "Sub-nav 1-pekerja-per-layar = IIFE/closure ber-state (step5State + step5Idx) di dalam WizardController, independen goToStep luar (toggle .step-panel .d-none)"
    - "Render data user via .textContent (XSS-safe T-395-12) — teks soal/opsi/nama; innerHTML hanya markup statis"
    - "Hidden-JSON serialize per-worker di submit-listener SAMA dgn #QuestionsJson (anti silent-grade-0)"
    - "Pratinjau on-demand: fetch POST PreviewInjectScore + RequestVerificationToken (T-395-13 CSRF); server-otoritas (no JS scoring)"
    - "Rebuild on goToStep(5) + prune answer ber-qTempId dangling (Pitfall TempId)"
    - "Skip=OMIT (D-05): soal di-skip TIDAK di-push ke answers payload (bukan spec kosong → reject-all)"
key-files:
  created:
    - "tests/e2e/inject-assessment-395.spec.ts (e2e 3 test: INJ-08 input-asli commit, INJ-09 auto-gen commit, LBL-02 label)"
  modified:
    - "Views/Admin/InjectAssessment.cshtml (+#AnswersJson hidden, +Step-5 markup K1-K6, +Step-5 IIFE controller, +Pratinjau fetch, +serialize #AnswersJson di submit-listener, LBL-02 4 baris)"
    - "docs/SEED_JOURNAL.md (+1 entry e2e 395-03, status cleaned)"
decisions:
  - "Step-5 = IIFE ber-state di dalam WizardController (closure akses injQuestions/injTypeLabel); render via .textContent; btnPrev5/btnNext5/pills TIDAK disentuh (D-03)"
  - "Auto-gen MC/MA TIDAK dirender di form (server hitung via BuildAutoGenAnswers); hanya essay yang dirender (HYBRID D-08.1)"
  - "Pratinjau seed pakai cb.value (user.Id) sebagai nip field — advisory; commit server pakai NIP sebenarnya (preview seed≈commit seed cukup untuk skor MC/MA; essay manual identik)"
  - "Switch BLOCKING→input-asli set mode=manual; pre-fill grid dari pola auto-gen terakhir TIDAK diimplementasi klien (auto-gen MC/MA dihitung server, tak ter-expose ke klien) — HC isi manual (deviasi minor dari D-10 pre-fill, dicatat)"
metrics:
  duration: "~30 menit"
  tasks: "2 of 3 (Task 3 = checkpoint human-verify, PENDING)"
  files_created: 1
  files_modified: 2
  tests_added: 3
  completed: 2026-06-18
---

# Phase 395 Plan 03: Langkah 5 jawaban per-pekerja (input-asli/auto-gen) + Pratinjau + #AnswersJson + LBL-02 Summary

Permukaan UI yang HC lihat untuk mengisi jawaban + jalur serialize jawaban ke commit aktual "seakan online". Mengganti `#step5Placeholder` (Phase 394) dengan sub-komponen IIFE 1-pekerja-per-layar (D-03): roster ringkas, navigasi Prev/Next antar pekerja, toggle mode input-asli/auto-generate per-pekerja, form MC radio / MA checkbox / Essay textarea+skor, tombol "Pratinjau Skor" (POST `PreviewInjectScore` Plan 02 → skor final aktual + Lulus/Tidak, no cert#), state BLOCKING (target>ceiling → "Beralih ke input asli"), hidden `#AnswersJson` di-serialize di submit-listener yang SAMA dengan `#QuestionsJson` (anti silent-grade-0), dan carry-in LBL-02. Dikunci Playwright e2e runtime (input-asli + auto-gen + commit + `#AnswersJson` terisi). **Task 3 (checkpoint human-verify) PENDING** — verifikasi browser live oleh user.

## What Was Built

**Task 1 — Step-5 sub-komponen + #AnswersJson + LBL-02** (`feat(395-03)` @`929a6c2e`):
- **(A) Hidden `#AnswersJson`** di samping `#QuestionsJson` (:~313) — `<input type="hidden" asp-for="AnswersJson" id="AnswersJson" />`.
- **(B) LBL-02 carry-in** (K7): `injTypeLabel()` return "Single Answer"/"Multiple Answer" (drop "Pilihan Ganda"/"Pilihan Majemuk"); 2 pesan validasi authoring di-update. Grep "Pilihan Ganda"/"Pilihan Majemuk" = **0 match**.
- **(C) Markup Step-5**: ganti `#step5Placeholder` dgn `#step5Root` — empty states (no-workers/no-questions), toggle default-room (K2), roster ringkas `.card`+tabel `role=status aria-live=polite` `max-height:280px` (K1: #/NIP·Nama/Mode/Skor Pratinjau/Status), indikator pekerja aktif `h6.fw-bold`, kontrol Prev/Next `.btn-sm.btn-outline-secondary` (K1), toggle mode per-pekerja (K2), body auto-gen (target number + banner HYBRID `.alert-info`, K4), `#step5AnswerForm` (di-render JS, K3), permukaan Pratinjau (tombol `.btn-outline-primary` + blok hasil `display:none`, K5), state BLOCKING `.alert-warning` + "Beralih ke input asli" (K6). Copy Bahasa Indonesia verbatim UI-SPEC. `btnPrev5`/`btnNext5`/pills TIDAK disentuh.
- **(D) Sub-komponen IIFE** ber-state (`step5State`/`step5Idx`) di dalam `WizardController`: `step5Rebuild()` (prune worker unchecked + answer qTempId dangling, default mode dari toggle), `step5RenderWorker()`/`step5RenderAnswerForm()` (MC radio / MA checkbox `aria-label="opsi A"` / Essay textarea+skor, skip-checkbox), `step5RenderRoster()` (mode badge + skor + status icon, klik baris=lompat). Render data user via `.textContent` (XSS-safe). Hook ke `goToStep` saat `n===5`.
- **(E) Pratinjau** (K5): tombol "Pratinjau Skor" → `fetch('/Admin/PreviewInjectScore', POST, RequestVerificationToken)` → render `percentage`/`isPassed`/overshoot/blocking + perbarui roster; spinner + disable saat fetch; `blocked` → tampilkan K6. Server-otoritas (no JS scoring).
- **(F) Serialize `#AnswersJson`** (Pitfall 4 KRITIS): di submit-listener yang SAMA dgn `#QuestionsJson` (:~993) — `document.getElementById('AnswersJson').value = JSON.stringify(buildWorkerAnswersPayload())`. `buildWorkerAnswersPayload()` map worker terpilih → `{userId, mode, targetScore, answers}`; skip=OMIT, auto MC/MA tak dikirim (server hitung), essay manual tetap dikirim (HYBRID).

**Task 2 — Playwright e2e** (`test(395-03)` @`d165f218`): `tests/e2e/inject-assessment-395.spec.ts` (TIRU pola 394, `mode:serial`, beforeAll BACKUP / afterAll RESTORE default backup dir). 3 test:
- **INJ-08 input-asli commit**: 2 pekerja ber-NIP (rino.prasetyo+iwan3); worker-1 pilih opsi A (benar) 2 soal → Pratinjau **100% Lulus** (no cert#); worker-2 pilih B (salah) → 0%. `#AnswersJson` via `page.evaluate(injBuildWorkerAnswers())` = array 2 worker × 2 answers. Submit → flash "Inject berhasil" + 2 sesi; DB: **1 sesi Score=100/IsPassed=1** (== preview, BUKAN 0) + 1 sesi Score=0.
- **INJ-09 auto-gen commit**: mode auto, target 50 → Pratinjau skor aktual **≥50** + badge (no cert#) → commit → `Score` DB **== previewPct** (seed deterministik preview==commit).
- **LBL-02**: badge tipe soal form jawaban = "Single Answer".

Run `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` → **3/3 GREEN** (+ setup project) dari main tree, AD-off. DB restored clean (60 sesi, 0 leftover; SEED_JOURNAL CLEANED).

## Verification

- `dotnet build HcPortal.csproj` → **Build succeeded, 0 Error(s)**.
- `npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` → **3/3 passed** (INJ-08 input-asli commit skor 100/0 anti-silent-grade-0, INJ-09 auto-gen commit Score==preview, LBL-02). Lifecycle BACKUP/RESTORE OK + global teardown RESTORE (Layer 4 0 rows).
- grep `AnswersJson` + `PreviewInjectScore` + `textContent` di `InjectAssessment.cshtml` ✓ (47 occurrences gabungan).
- grep "Pilihan Ganda"/"Pilihan Majemuk" di `InjectAssessment.cshtml` = **0 match** (LBL-02 selesai).
- grep `JSON.stringify(buildWorkerAnswersPayload())` di submit-listener yang SAMA dgn `#QuestionsJson` (:~993) ✓; `btnPrev5`/`btnNext5` utuh (:524/:527) ✓.
- DB post-test: 60 sesi, 0 sesi 'ZZ Inject%' (restored clean).
- **0 migration**: hanya file view + e2e + journal disentuh; tak ada `Migrations/`/`ApplicationDbContext`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test logic] E2e pilih worker ber-NIP (bukan N-pertama)**
- **Found during:** Task 2 (e2e run pertama gagal: "Inject berhasil: 0 sesi ter-commit").
- **Issue:** `fillToStep5` semula pilih N pekerja PERTAMA (ordered FullName). Pekerja pertama aktif ("Admin KPB", "Ahmad Yani", dst.) ber-**NIP NULL**. Controller `MapToRequest` (Plan 02) **skip user null/empty NIP** (`continue`) → `req.Workers` kosong → `InjectBatchAsync` commit 0 sesi tapi `Success=true` ("berhasil: 0 sesi"). Bukan bug implementasi 395-03 — perilaku controller Plan 02 (catatan STATE "null-NIP user diabaikan, surfaced commit 395") + data lokal (hanya 2 user aktif ber-NIP).
- **Fix:** `fillToStep5` pilih pekerja spesifik ber-NIP via `data-email` (rino.prasetyo@pertamina.com + iwan3@pertamina.com).
- **Files modified:** `tests/e2e/inject-assessment-395.spec.ts`.
- **Commit:** `d165f218`.
- **Catatan untuk verifier/UAT:** "berhasil: 0 sesi" saat semua pekerja terpilih ber-NIP NULL = surfacing lemah (UX). Bukan blocking 395; di-flag sebagai potensi polish future (controller bisa beri warning bila ada pekerja null-NIP yang di-skip).

### Catatan non-deviasi
- **Pre-fill grid saat switch BLOCKING→input-asli (D-10):** tidak diimplementasi penuh — auto-gen MC/MA dihitung **server-side** (BuildAutoGenAnswers) dan TIDAK ter-expose ke klien, jadi pola terakhir tak tersedia untuk pre-fill grid. Switch hanya set `mode=manual` + reset preview; HC isi jawaban manual. Sesuai prinsip server-otoritas (jangan duplikasi auto-gen di JS). Esensi D-10 (tawarkan switch ke input-asli) terpenuhi; pre-fill = nice-to-have yang bertentangan dgn server-otoritas → dilewati sengaja.
- **Untracked** `docs/395-QUESTIONS.json`, `docs/*.xlsx`, `docs/Soal/` = artefak discuss/authoring (di luar scope), dibiarkan.

## Known Stubs
None — semua jalur (render form per-pekerja, toggle mode, skip=omit, Pratinjau fetch, BLOCKING, serialize #AnswersJson, commit) terimplementasi penuh + terkunci e2e runtime.

## TDD Gate Compliance
Plan type=execute (bukan tdd). Task 2 e2e = verifikasi runtime (anti silent-grade-0), bukan TDD RED/GREEN. Gate phase keseluruhan: `test(395-01)` RED → `feat(395-01)` GREEN (algoritma); Plan 02/03 = lapisan controller/view di atas algoritma teruji.

## Self-Check: PASSED
- FOUND: `Views/Admin/InjectAssessment.cshtml` (berisi `AnswersJson`, `PreviewInjectScore`, `buildWorkerAnswersPayload`, `step5State`, `Single Answer`)
- FOUND: `tests/e2e/inject-assessment-395.spec.ts` (berisi `inject-assessment-395`, `injBuildWorkerAnswers`)
- FOUND commit: `929a6c2e` (feat Task 1)
- FOUND commit: `d165f218` (test Task 2)
- MISSING: none

## Checkpoint Pending
**Task 3 (checkpoint:human-verify)** — verifikasi browser live Langkah 5 + commit "seakan online" (preview==commit, BLOCKING benar, LBL-02). App berjalan di http://localhost:5277 (main tree, AD-off). Menunggu approval user atau laporan masalah.
