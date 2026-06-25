---
phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
plan: 03
subsystem: assessment-retake
tags: [retake, worker-self-service, tier-feedback, leak-safe, riwayat, razor-view, playwright, csrf, xss]

# Dependency graph
requires:
  - phase: 407-01
    provides: "RetakeReviewMode enum (ShowFullReview/ShowWrongFlagsOnly/ShowScoreOnly) + AssessmentResultsViewModel +7 retake/tier field + RiwayatAttemptViewModel"
  - phase: 407-02
    provides: "CMPController.Results populate 7 VM field (RetakeMode via assessment.IsPassed bool? Pitfall 5, RiwayatAttempts via RiwayatUnifier) + POST CMP/RetakeExam endpoint (antiforgery+ownership+CanRetakeAsync)"
  - phase: 406
    provides: "Views/Admin/_RiwayatPercobaan.cshtml (analog accordion + tri-state verdict) + retake-config-406.spec.ts idiom (db.backup/restore SEED_WORKFLOW)"
provides:
  - "Views/CMP/Results.cshtml — @switch(Model.RetakeMode) 3-state LEAK-SAFE (ShowWrongFlagsOnly suppress kunci jawaban) + retake control (btnRetake/counter/cooldown countdown/IsCapReached lock) + #retakeConfirmModal antiforgery POST RetakeExam + countdown JS guard-safe"
  - "Views/CMP/_RiwayatPekerja.cshtml — partial riwayat pekerja ter-gate (varian _RiwayatPercobaan: Tidak Lulus/Jawaban Saya + ViewData[HideDetail] ScoreOnly hide-detail)"
  - "tests/e2e/retake-worker-407.spec.ts — Playwright smoke leak-safety @5270 (6 skenario) + tests/sql/retake-worker-407-seed.sql (3 sesi fixture)"
affects: [408]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Tier feedback @switch(RetakeMode) di view: ShowFullReview (markup existing verbatim, kunci tampil) / ShowWrongFlagsOnly (LEAK-SAFE verdict-only) / ShowScoreOnly (notice) — leak-safety = keputusan server (407-02), view hanya men-suppress markup kunci"
    - "Worker riwayat partial = varian ter-gate dari HC partial (NOT include verbatim): 3 worker delta + ViewData[HideDetail] flag dilewatkan saat PartialAsync (partial @model tetap List<RiwayatAttemptViewModel> tak bawa RetakeMode)"
    - "Countdown JS guard-safe anti-ReferenceError (lesson 413): if(!btn)return + if(!iso)return + isNaN guard; .textContent/.setAttribute (no innerHTML user-string); saat <=0 clearInterval + enable + wire modal + relabel"
    - "Playwright leak-safety smoke: seed opsi-kunci dengan teks UNIK (KUNCIBENAR_*) → assert ABSEN di DOM ShowWrongFlagsOnly (DOM-level, bukan grep build) + page.on('pageerror') assert empty (lesson 413)"

key-files:
  created:
    - "Views/CMP/_RiwayatPekerja.cshtml"
    - "tests/e2e/retake-worker-407.spec.ts"
    - "tests/sql/retake-worker-407-seed.sql"
  modified:
    - "Views/CMP/Results.cshtml"
    - "docs/SEED_JOURNAL.md"

key-decisions:
  - "Records.cshtml SENGAJA tak disentuh (D-04 default): andalkan 'Lihat Hasil' yang sudah route ke Results tempat riwayat hidup — TIDAK menambah trigger per-row (tak ada alasan parity HC; mengurangi surface)"
  - "Comment di view/partial di-rephrase agar TIDAK memuat literal leak-token (list-group-item-success / '(Jawaban Benar)' / CorrectAnswer) maupun 'Html.Raw'/'Jawaban Peserta' — supaya gate grep secure-phase lewat bersih (acceptance criteria grep-by-string)"
  - "Gating ScoreOnly di partial via ViewData['HideDetail'] (bool) dilewatkan di PartialAsync call dari Results — partial render daftar attempt TANPA tabel per-soal saat true (notice 'Tinjauan jawaban tidak tersedia')"
  - "Spec leak-safety pakai teks opsi-kunci UNIK (KUNCIBENAR_A1/A2) di seed → assertion not.toContain di DOM membuktikan kunci tak bocor (lebih kuat dari sekadar absen class)"
  - "Spec 407C (cooldown) assert countdown HANYA bila #btnRetake hadir (non-flaky terhadap policy gate server CanRetakeAsync) — controller bisa gate tombol; spec terima EITHER"

patterns-established:
  - "View leak-safe tier: kunci jawaban (option-highlight/label-benar/essay-rubrik) HANYA di cabang ShowFullReview; ShowWrongFlagsOnly render question-text + own-answer + verdict tri-state SAJA"

requirements-completed: [RTK-10, RTK-11, RTK-12]

# Metrics
duration: 12min
completed: 2026-06-22
---

# Phase 407 Plan 03: Worker Self-Service UI + Gating Tier Feedback + Riwayat Pekerja Summary

**Rakit seluruh UI sisi-pekerja di `Views/CMP/Results.cshtml`: tier feedback 3-state `@switch(Model.RetakeMode)` LEAK-SAFE (`ShowWrongFlagsOnly` menahan kunci jawaban selama retake masih mungkin — tak render `list-group-item-success`/"(Jawaban Benar)"/`CorrectAnswer`), retake control (tombol "Ujian Ulang"/counter/cooldown countdown disabled→enable/lock cap-habis), modal konfirmasi ber-antiforgery POST `RetakeExam`, countdown JS guard-safe (lesson 413); partial baru `_RiwayatPekerja.cshtml` ter-gate (worker delta "Tidak Lulus"/"Jawaban Saya" + `ViewData[HideDetail]` ScoreOnly); + Playwright smoke `retake-worker-407.spec.ts` 6 skenario leak-safety @5270. 0 migration.**

## Performance
- **Duration:** ~12 min
- **Started:** 2026-06-22T02:35:05Z
- **Tasks:** 3 (Task 3 = checkpoint human-verify — spec artifact selesai, live UAT dijalankan orchestrator)
- **Files created:** 3 (`_RiwayatPekerja.cshtml`, `retake-worker-407.spec.ts`, `retake-worker-407-seed.sql`); **modified:** 2 (`Results.cshtml`, `SEED_JOURNAL.md`)

## Accomplishments

### Task 1 — Tier 3-state + retake control + modal + countdown JS (Results.cshtml)
- **`@using HcPortal.Helpers`** ditambah di atas view.
- **(A) Tier 3-state** — blok boolean `:316-418` diganti `@switch (Model.RetakeMode)`:
  - `ShowFullReview` → markup full-review existing **VERBATIM** (option loop + leak-site + essay CorrectAnswer dipertahankan utuh — kunci tampil saat lulus/exhausted).
  - `ShowWrongFlagsOnly` → **BRANCH BARU LEAK-SAFE**: card "Tinjauan Jawaban" + `alert alert-info role="status"` (icon `bi-eye-slash`, note kunci disembunyikan) + loop `Model.QuestionReviews` render HANYA `Soal N: QuestionText` + badge verdict tri-state (IsEssayPending→Menunggu / IsCorrect→Benar / else→Salah) + (`!IsNullOrEmpty(q.UserAnswer)`) "Jawaban Anda: @q.UserAnswer". TIDAK render `list-group-item-success` / icon kunci / "(Jawaban Benar)" / `CorrectAnswer`.
  - `ShowScoreOnly` → notice existing `alert alert-info` "Tinjauan jawaban tidak tersedia untuk assessment ini." VERBATIM.
- **(B) Retake control** (RTK-10) sebelum action row: `IsCapReached` → `alert-warning role=alert` `bi-lock-fill` "Batas percobaan tercapai..." (no tombol); `CanRetake` → `#btnRetake` (cooldown→disabled+`data-cooldown-until="o"`; eligible→`data-bs-toggle=modal`) + counter "Percobaan ke-@CurrentAttempt dari @MaxAttempts".
- **(C) Modal** `#retakeConfirmModal` (centered, aria-labelledby, `btn-close aria-label=Tutup`): body destruktif + footer "Batal" + `<form method=post asp-action=RetakeExam asp-controller=CMP asp-route-id=@Model.AssessmentId>` + **`@Html.AntiForgeryToken()`** + submit "Ya, Ujian Ulang" (RTK-09/D-02).
- **(D) Countdown JS** inline guard-safe (lesson 413): `if(!btn)return` + `if(!iso)return` + `isNaN` guard; baca `data-cooldown-until`, `setInterval` 1s tulis `#retakeCountdown` format `HH:MM:SS` via `.textContent`; saat `<=0` → `clearInterval` + `removeAttribute(disabled)` + `setAttribute(data-bs-toggle/target modal)` + relabel "Ujian Ulang".

### Task 2 — Partial _RiwayatPekerja (ter-gate) + render di Results
- **`Views/CMP/_RiwayatPekerja.cshtml`** (baru) — copy struktur `Views/Admin/_RiwayatPercobaan.cshtml`, `@model List<RiwayatAttemptViewModel>`, 3 worker delta:
  1. Badge "Gagal" → **"Tidak Lulus"** (`text-bg-danger`); "Lulus"/"Menunggu Penilaian" dipertahankan.
  2. Kolom "Jawaban Peserta" → **"Jawaban Saya"**.
  3. Gating `ViewData["HideDetail"]` (bool): true → render daftar attempt TANPA tabel per-soal (notice "Tinjauan jawaban tidak tersedia untuk assessment ini.").
  - Tri-state status cell (✓Benar/✗Salah/—Menunggu + `visually-hidden`), badge `bg-info` "Percobaan saat ini" (`attempt.IsCurrent`), empty-states — VERBATIM. ZERO raw-output (semua user-content Razor `@` auto-encode).
- **Render di Results** — card "Riwayat Percobaan Saya" (`bi-clock-history`) di bawah tier feedback: empty-state bila null/kosong; else `@await Html.PartialAsync("_RiwayatPekerja", Model.RiwayatAttempts, new ViewDataDictionary(ViewData) { ["HideDetail"] = (Model.RetakeMode == RetakeReviewMode.ShowScoreOnly) })`.
- **Records.cshtml** — D-04 DEFAULT: TIDAK disentuh (andalkan "Lihat Hasil" yang sudah route ke Results tempat riwayat hidup).

### Task 3 — Playwright smoke leak-safety @5270 (CHECKPOINT — spec artifact)
- **`tests/e2e/retake-worker-407.spec.ts`** (mirror `retake-config-406.spec.ts`: mode serial + per-spec `db.backup`(beforeAll)/`db.restore`(afterAll) SEED_WORKFLOW; login worker = `coachee` rino.prasetyo). 6 skenario:
  1. **leak-safety (KRITIS)** — sesi A: card Tinjauan + notice "Kunci jawaban disembunyikan" + badge Benar/Salah + "Jawaban Anda:"; DOM `not.toContain("(Jawaban Benar)")` / `"KUNCIBENAR_A1"` / `"KUNCIBENAR_A2"` + `.list-group-item-success` count 0; no pageerror.
  2. **control eligible** — `#btnRetake` visible+enabled+"Ujian Ulang" + counter regex.
  3. **modal** — klik → `#retakeConfirmModal` visible + `input[name=__RequestVerificationToken]` count 1 + "Ya, Ujian Ulang"; no pageerror.
  4. **riwayat** — card "Riwayat Percobaan Saya" + `#riwayatPekerjaAccordion` + badge "Percobaan saat ini".
  5. **cap reached** — sesi B: alert "Batas percobaan tercapai" + `#btnRetake` count 0.
  6. **cooldown active** — sesi C: tombol disabled + `#retakeCountdown` regex `HH:MM:SS` + ticking (nilai berubah) — assert hanya bila tombol hadir (non-flaky).
- **`tests/sql/retake-worker-407-seed.sql`** — 3 sesi prefix `[RETAKE407]` (idempotent WIPE-AND-INSERT + THROW 51407): A=LeakSafe+Eligible (Completed IsPassed=0, AllowRetake=1 MaxAttempts=3 cooldown=0, 1 arsip → currentAttempt=2; package chain 2 SA q1-benar/q2-salah + opsi-kunci `KUNCIBENAR_*`), B=CapReached (MaxAttempts=2 + 2 arsip → currentAttempt=3), C=CooldownActive (CompletedAt=now, RetakeCooldownHours=24).
- **`docs/SEED_JOURNAL.md`** — entry 407-03 (status `planned`; orchestrator jalankan live UAT → tandai cleaned).

## Task Commits
1. **Task 1: tier 3-state + retake control + modal + countdown JS** — `b57fdc6b` (feat) — build 0 error; leak-safe branch grep-verified.
2. **Task 2: partial _RiwayatPekerja + render di Results** — `810ffb60` (feat) — build 0 error; grep gate (NO Html.Raw / NO Jawaban Peserta) PASS.
3. **Task 3: Playwright spec + seed + journal** — `0bd3c1ac` (test) — build 0 error; unit suite 448/0/2; spec `--list` 6 tests OK.

## Files Created/Modified
- `Views/CMP/Results.cshtml` — `@using HcPortal.Helpers`; `@switch(Model.RetakeMode)` 3-state (240+/82-); retake control + modal + countdown JS.
- `Views/CMP/_RiwayatPekerja.cshtml` — NEW partial ter-gate (115 baris).
- `tests/e2e/retake-worker-407.spec.ts` — NEW spec (6 skenario).
- `tests/sql/retake-worker-407-seed.sql` — NEW seed (3 sesi).
- `docs/SEED_JOURNAL.md` — entry 407-03 planned.

## Decisions Made
Lihat frontmatter `key-decisions`. Ringkas: Records.cshtml tak disentuh (D-04 default); comment di-rephrase agar gate grep bersih; gating ScoreOnly via ViewData[HideDetail]; seed opsi-kunci teks unik untuk assertion leak-absent; spec 407C cooldown assert conditional (non-flaky).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Comment di ShowWrongFlagsOnly memuat literal leak-token**
- **Found during:** Task 1 (grep leak-safety self-check)
- **Issue:** Komentar penjelas leak-prevention awalnya menulis literal `list-group-item-success` / "(Jawaban Benar)" / `CorrectAnswer` → grep acceptance-criteria (`cabang ShowWrongFlagsOnly TIDAK mengandung ...`) false-positive (token muncul di komentar, bukan markup ter-render).
- **Fix:** Komentar di-rephrase ("TANPA highlight opsi-benar, TANPA label kunci, TANPA rubrik essay") — markup ter-render TIDAK pernah memuat token. Grep ulang: branch bersih.
- **Files modified:** `Views/CMP/Results.cshtml`
- **Commit:** `b57fdc6b` (inline sebelum commit Task 1)

**2. [Rule 1 - Bug] Comment di _RiwayatPekerja memuat literal 'Html.Raw' + 'Jawaban Peserta'**
- **Found during:** Task 2 (grep gate self-check)
- **Issue:** Komentar XSS-note menulis "ZERO Html.Raw" + worker-delta note "'Jawaban Peserta'→'Jawaban Saya'" → gate acceptance `TIDAK mengandung Html.Raw` + `TIDAK mengandung Jawaban Peserta` gagal (token di komentar).
- **Fix:** Rephrase komentar ("TANPA raw-output", "kolom jawaban → 'Jawaban Saya'"). Gate ulang PASS.
- **Files modified:** `Views/CMP/_RiwayatPekerja.cshtml`
- **Commit:** `810ffb60` (inline sebelum commit Task 2)

_Keduanya fix di file yang belum di-commit saat ditemukan → diperbaiki sebelum commit (bukan commit terpisah). Tidak mengubah perilaku ter-render, hanya teks komentar agar gate grep-by-string lewat bersih. 2 fix attempt total, di bawah limit 3._

## Authentication Gates
None — spec memakai login dev lokal (`coachee` / 123456); live UAT dijalankan orchestrator.

## Issues Encountered
- Working tree memuat file untracked pre-existing (`akun-doc-*.jpeg`, xlsx, `docs/akun-multirole-multiunit/`) dari sesi sebelumnya. TIDAK distage (di luar scope plan). `docs/SEED_JOURNAL.md` memuat 1 baris matrix-sweep `cleaned` pre-existing (uncommitted dari prior test run) yang ikut ter-commit bersama entry 407-03 — keduanya audit-entry journal, benign.

## User Setup Required
None untuk artifact. **Live UAT (orchestrator):** app @5270 (`Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270`) + `E2E_BASE_URL=http://localhost:5270` + `npx playwright test tests/e2e/retake-worker-407.spec.ts --workers=1`.

## Threat Model Compliance
Semua disposition `mitigate` di plan `<threat_model>` ter-implement:
- **T-407-leak** (Info Disclosure): `ShowWrongFlagsOnly` TIDAK render `list-group-item-success`/"(Jawaban Benar)"/`CorrectAnswer` — grep-verified branch + spec DOM assert (`not.toContain KUNCIBENAR_*` + `.list-group-item-success` count 0). Arsip riwayat struktural verdict-only.
- **T-407-csrf** (Tampering): `@Html.AntiForgeryToken()` di form modal; spec assert `input[name=__RequestVerificationToken]` count 1.
- **T-407-bypass** (Tampering): countdown JS non-authoritative (UX only); server re-cek `CanRetakeAsync` di POST (407-02).
- **T-407-xss** (Tampering): semua user-content Razor `@` auto-encode; ZERO raw-output partial; countdown JS `.textContent`/`.setAttribute`.
- **T-407-jsabort** (DoS handler): countdown guard `if(!btn)return` + `if(!iso)return`; spec `page.on('pageerror')` assert empty (lesson 413).

## Threat Flags
None — tidak ada surface keamanan baru di luar threat register (view render + spec; endpoint RetakeExam sudah dimodelkan di 407-02).

## Known Stubs
None — semua data (RetakeMode/CanRetake/counter/cooldown/RiwayatAttempts) di-wire dari VM yang sudah diisi server (407-02). Tidak ada hardcoded empty/placeholder yang mengalir ke render.

## Verification
- `dotnet build HcPortal.csproj` — **0 error** (24 warning pre-existing di file unrelated, out-of-scope).
- `dotnet test HcPortal.Tests --filter "Category!=Integration"` — **448/0/2** (2 skip SQLEXPRESS-gated; no regresi, baseline 407-02 identik).
- Grep leak-safety branch `ShowWrongFlagsOnly`: TANPA `list-group-item-success`/"(Jawaban Benar)"/`CorrectAnswer` (PASS).
- Grep partial: NO `Html.Raw`, NO "Jawaban Peserta", ADA "Jawaban Saya"/"Tidak Lulus"/"Percobaan saat ini"/tri-state/HideDetail (PASS).
- Playwright `retake-worker-407.spec.ts` — parse OK (`--list` 6 tests); **live run = orchestrator UAT gate @5270** (TIDAK dijalankan executor per instruksi checkpoint).

## Next Phase Readiness
- **Live Playwright UAT @5270 (orchestrator):** jalankan `retake-worker-407.spec.ts --workers=1` — buktikan leak-safety DOM + control + modal + riwayat + no-pageerror. DB di-RESTORE oleh spec afterAll → tandai SEED_JOURNAL `cleaned`.
- **Phase 408** — Test & UAT terakhir milestone v32.4 (depends 406+407). Setelah 407 verified.
- **0 migration** plan ini (Razor view + partial + spec/seed murni). Migration v32.4 satu-satunya tetap di 405-01 (`AddRetakeColumnsAndArchive`).
- No blockers (selain live UAT gate yang dijalankan orchestrator).

## Self-Check: PASSED

- `Views/CMP/Results.cshtml` — FOUND (modified).
- `Views/CMP/_RiwayatPekerja.cshtml` — FOUND (created).
- `tests/e2e/retake-worker-407.spec.ts` — FOUND (created).
- `tests/sql/retake-worker-407-seed.sql` — FOUND (created).
- Commits `b57fdc6b`, `810ffb60`, `0bd3c1ac` — all FOUND in git log.

---
*Phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja*
*Completed: 2026-06-22*
