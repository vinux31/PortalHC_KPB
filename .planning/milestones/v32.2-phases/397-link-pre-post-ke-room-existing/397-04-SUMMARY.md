---
phase: 397-link-pre-post-ke-room-existing
plan: 04
subsystem: ui
tags: [auth, assessment, pre-post-link, view, razor, bootstrap-modal, playwright, e2e, xss]

# Dependency graph
requires:
  - phase: 397-03
    provides: "SearchLinkTargets JSON picker + MapToRequest LinkTargetRepId + PreviewPairing endpoint (D-07) + UnlinkInjectGroup POST (D-12)"
  - phase: 397-02
    provides: "per-pekerja bidirectional linking + Kasus A/B resolution + Kasus B write-to-online atomic + audit LinkPrePost + PreviewPairingAsync + UnlinkInjectGroupAsync"
  - phase: 396-04
    provides: "Step-5 Form/Excel toggle pada view yang sama (InjectAssessment.cshtml) — di-commit lebih dulu, menggeser nomor baris"
provides:
  - "UI runtime INJ-12: room-picker modal + chip Step-1 (N1/N2) wired ke SearchLinkTargets"
  - "Pratinjau ringkasan pairing (ter-pair/tanpa-pasangan) + banner Kasus B 'data online akan disentuh' + date warn (N3) wired ke PreviewPairing"
  - "Anti-double-link entries di error/warn list (N4) — surface pre-commit dari DoubleLinkErrors"
  - "Unlink confirm modal Bootstrap (N5) di success surface post-commit wizard (BUKAN monitoring, D-12) wired ke UnlinkInjectGroup"
  - "Playwright e2e inject-assessment-397.spec.ts (6/6) — bukti runtime modal/picker/chip/preview/unlink + cross inject↔online grouping §13"
affects: [398-test-uat, monitoring, cmp-records, gain-score]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "XSS-safe render user-authored room/worker text via .textContent (NEVER innerHTML) — T-397-14"
    - "Anti-forgery token (RequestVerificationToken header) pada POST fetch PreviewPairing/UnlinkInjectGroup — T-397-15"
    - "Bootstrap confirm modal (bukan native confirm()) untuk aksi destruktif unlink"
    - "Debounced (~300ms) fetch search picker GET SearchLinkTargets"
    - "Unlink host LOCKED di success surface post-commit wizard saja (D-12 jaga minimal) — bukan monitoring page"

key-files:
  created:
    - "tests/e2e/inject-assessment-397.spec.ts"
  modified:
    - "Views/Admin/InjectAssessment.cshtml"
    - "docs/SEED_JOURNAL.md"
    - "Controllers/InjectAssessmentController.cs"
    - "Dtos/InjectResult.cs"
    - "Services/InjectAssessmentService.cs"

key-decisions:
  - "Unlink control hanya di success surface post-commit wizard (D-12 jaga minimal); monitoring page OUT OF SCOPE fase ini"
  - "resolvedGroupId di-surface post-commit via InjectResult.LinkedGroupId + TempData['InjectedGroupId'] (Rule-3) agar host unlink punya group id yang tepat"
  - "Semua teks room/pekerja di-render via .textContent (XSS-safe), bukan innerHTML"
  - "0-migration diverifikasi via git diff Migrations/ Data/ kosong (sejak 396 b465a5ab)"

patterns-established:
  - "Cari Room conditional by tipe (Pre/Post tampil, Standard sembunyi) + tipe-lawan hint dinamis"
  - "Kasus A 'Sudah ber-grup' (bg-success) vs Kasus B 'Standalone' (bg-warning) badge di picker + chip"
  - "Pratinjau pairing summary role=status aria-live=polite (D-07)"

requirements-completed: [INJ-12]

# Metrics
duration: continuation-finalize
completed: 2026-06-18
---

# Phase 397 Plan 04: Link Pre/Post ke Room Existing — UI + e2e Summary

**UI runtime INJ-12: room-picker modal + chip di Step-1, ringkasan pairing + banner Kasus B + date-warn di Pratinjau, anti-double entries di error list, dan unlink confirm modal Bootstrap — dikunci Playwright e2e (6/6) yang membuktikan cross inject↔online grouping utuh dengan data online TAK disentuh (KRITIS §13). UAT live browser 9/9 PASS.**

## Performance

- **Duration:** Continuation (finalisasi pasca-approval; Tasks 1-3 sudah di-commit di sesi sebelumnya)
- **Started:** 2026-06-18
- **Completed:** 2026-06-18
- **Tasks:** 4 (3 auto + 1 checkpoint human-verify)
- **Files modified:** 5 (1 view + 1 e2e baru + SEED_JOURNAL + 3 file dari deviasi Rule-3)

## Accomplishments

- **N1/N2 (Task 1, `47b0e875`):** Tombol "Cari Room Pasangan" kondisional (tampil untuk Pre/Post, sembunyi untuk Standard) + hint tipe-lawan dinamis ("Cari room Post-Test pasangannya." / "Cari room Pre-Test pasangannya."), chip `#selectedRoomChip` yang bisa dihapus dengan badge Kasus A/B, hidden field `#LinkedTargetRepId` (asp-for bound ke VM), dan `#roomPickerModal` Bootstrap (daftar tipe-lawan inject+online, badge Kasus A "Sudah ber-grup"/Kasus B "Standalone"/"Inject", debounced search ~300ms ke `SearchLinkTargets`). Placeholder note "tersedia pada fase berikutnya" dihapus.
- **N3/N4 (Task 2, bagian `47b0e875`):** `#previewPairingSummary` (role=status, aria-live=polite) di Pratinjau — fetch `PreviewPairing` saat room ter-link → render "{p} ter-pair" + "{u} tanpa pasangan", banner Kasus B "Data online akan disentuh." (skor/jawaban/status TIDAK diubah), date-warn (Pre lebih baru dari Post). Anti-double entries dari `DoubleLinkErrors[]` masuk ke error/warn list yang SAMA (bukan panel baru) dengan copy "sudah memiliki … tidak dapat ditautkan dua kali".
- **N5 (Task 2, bagian `47b0e875`):** `#unlinkConfirmModal` Bootstrap (BUKAN native `confirm()`) + trigger `#btnUnlinkRoom` + `#btnConfirmUnlink` → POST `UnlinkInjectGroup` (anti-forgery). Host unlink LOCKED di success surface post-commit wizard saja (D-12 "jaga minimal") — monitoring page OUT OF SCOPE fase ini.
- **e2e (Task 3, `2d19bf07`):** `tests/e2e/inject-assessment-397.spec.ts` (serial, loginAdmin, db.snapshot/restore, --workers=1, MAIN tree, AD-off) — 6/6 PASS, menutup Interaction Contracts UI-SPEC termasuk Contract 8 cross-grouping §13 (load-bearing). SEED_JOURNAL.md dicatat + CLEANED.

## Task Commits

Tasks 1-3 di-commit atomik di sesi eksekusi sebelumnya:

1. **Task 1: Step-1 trigger + chip + room-picker modal (N1/N2)** — `47b0e875` (feat) — Views/Admin/InjectAssessment.cshtml
2. **Task 2: pairing summary (N3) + anti-double (N4) + unlink confirm modal (N5)** — bagian dari `47b0e875` (feat) + `7ecd0ef3` (feat, deviasi Rule-3 surface resolvedGroupId post-commit untuk host unlink N5) — Views/Admin/InjectAssessment.cshtml + Controllers/InjectAssessmentController.cs + Dtos/InjectResult.cs + Services/InjectAssessmentService.cs
3. **Task 3: Playwright e2e + 0-migration gate** — `2d19bf07` (test) — tests/e2e/inject-assessment-397.spec.ts + docs/SEED_JOURNAL.md
4. **Task 4: checkpoint human-verify** — APPROVED (orchestrator-driven live browser UAT 9/9 PASS; tidak ada perubahan kode)

**Plan metadata:** commit finalisasi ini (docs: complete plan — SUMMARY + STATE + ROADMAP)

## Files Created/Modified

- `Views/Admin/InjectAssessment.cshtml` — UI link Pre/Post: prePostLinkBlock + btnCariRoom + hint tipe-lawan + selectedRoomChip + LinkedTargetRepId (hidden, asp-for) + roomPickerModal + previewPairingSummary + pairingTouchOnlineBanner + pairingDateWarn + pairingDoubleLinkPanel + unlinkConfirmModal + postCommitLinkSurface + btnUnlinkRoom + btnConfirmUnlink
- `tests/e2e/inject-assessment-397.spec.ts` — Playwright e2e (6/6) modal/picker/chip/preview/unlink + cross-grouping §13
- `docs/SEED_JOURNAL.md` — entry Phase 397 (temporary + local-only; entitas AssessmentSession, AuditLog) status CLEANED
- `Controllers/InjectAssessmentController.cs` — (Rule-3) set TempData["InjectedGroupId"] dari resolvedGroupId post-commit untuk host unlink
- `Dtos/InjectResult.cs` — (Rule-3) tambah `LinkedGroupId` agar group id terpilih ter-surface ke view
- `Services/InjectAssessmentService.cs` — (Rule-3) isi `InjectResult.LinkedGroupId` dengan resolvedGroupId hasil commit

## View IDs yang Ditambahkan

`prePostLinkBlock`, `btnCariRoom`, `selectedRoomChip`, `LinkedTargetRepId` (hidden, asp-for VM), `roomPickerModal`, `previewPairingSummary` (aria-live=polite), `pairingTouchOnlineBanner` (Kasus B), `pairingDateWarn`, `pairingDoubleLinkPanel` (N4), `unlinkConfirmModal`, `postCommitLinkSurface`, `btnUnlinkRoom`, `btnConfirmUnlink`.

## e2e Contracts yang Dicakup (inject-assessment-397.spec.ts, 6/6)

1. **Cari Room conditional** — Standard → btnCariRoom hidden; Pre/Post → visible + tipe-lawan hint; placeholder note absen.
2. **Open modal + opposite-type filter** — modal listing tipe-lawan; debounced search filter; inject rooms ber-badge "Inject".
3. **Kasus A/B badge** — grouped → "Sudah ber-grup" (success); standalone → "Standalone" (warning).
4. **Pick room → chip + skippable** — row klik → chip (title/date/type/group), #LinkedTargetRepId set; remove → cleared (skip standalone).
5. **Pairing summary Pratinjau** — "{p} ter-pair" + "{u} tanpa pasangan"; Kasus B banner; date-warn saat Pre > Post.
6. **Anti-double entry** — pekerja dgn sibling same-type di target → muncul di error list + commit blocked.
7. **Skip penautan** — Pre/Post tanpa pilih room → inject standalone (LinkedGroupId null), tanpa pairing summary.
8. **Commit cross-grouping intact (KRITIS §13)** — commit linked inject (Pre, Id 174) + online Post (Id 173) → keduanya share `LinkedGroupId=173` + same UserId; online Score/Status UNCHANGED; audit "LinkPrePost" present. **Pasangan silang inject↔online tampil sebagai satu grup Pre/Post — assertion load-bearing fase ini.**
9. **Unlink + confirm modal** — Bootstrap confirm modal (bukan native) → "Ya, Lepaskan" → success notice; DB link reverted (LinkedGroupId/LinkedSessionId NULL), Score utuh, audit "LinkPrePostUndo".

(Kontrak UI-SPEC 1-10 dikompresi ke 6 test serial; Contract 8 = inti cross-grouping §13.)

## Unlink Host Location (D-12)

Kontrol "Lepaskan Tautan" (#btnUnlinkRoom + #unlinkConfirmModal) **HANYA** di success surface post-commit wizard inject (`#postCommitLinkSurface`, muncul setelah commit yang ter-link). Monitoring page **OUT OF SCOPE** fase ini per D-12 "jaga minimal" — tidak ada trigger/modal unlink yang ditambahkan ke view monitoring mana pun. Group id yang dipakai unlink di-carry dari respons commit via `InjectResult.LinkedGroupId` + `TempData["InjectedGroupId"]`.

## 0-Migration Check

Dipakai: `git diff --name-only b465a5ab..HEAD -- Migrations/ Data/` → **kosong** (0 perubahan schema/seed sejak Phase 396). Probe migration `_verify397` tidak meninggalkan file tertinggal di `Migrations/`. **0 migration confirmed.** (CLAUDE.md: notify IT migration=FALSE saat push.)

## Decisions Made

- **Unlink host = success surface post-commit wizard saja** (D-12 jaga minimal) — monitoring tidak disentuh fase ini.
- **resolvedGroupId di-surface ke view** via InjectResult.LinkedGroupId + TempData agar host unlink punya group id yang akurat (deviasi Rule-3, lihat di bawah).
- **Semua teks user-authored via .textContent** (XSS-safe) konsisten lintas modal/chip/preview/error list.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Surface resolvedGroupId post-commit untuk host unlink (N5)**
- **Found during:** Task 2 (unlink confirm modal N5)
- **Issue:** Kontrol unlink di success surface butuh group id batch yang baru di-commit, tetapi respons commit (`InjectResult`) belum membawa `resolvedGroupId` — tanpa ini tombol "Lepaskan Tautan" tidak punya `injectGroupId` untuk dikirim ke `UnlinkInjectGroup`, sehingga N5 tak bisa berfungsi runtime.
- **Fix:** Tambah `InjectResult.LinkedGroupId` (di-isi service dengan resolvedGroupId hasil commit) + set `TempData["InjectedGroupId"]` di controller sehingga view post-commit dapat menampilkan kontrol unlink ber-group-id benar.
- **Files modified:** Controllers/InjectAssessmentController.cs, Dtos/InjectResult.cs, Services/InjectAssessmentService.cs
- **Verification:** e2e Contract 9 (unlink) lulus — DB link reverted setelah "Ya, Lepaskan"; UAT live §9 unlink 173/174 link columns NULL + audit LinkPrePostUndo ×2.
- **Committed in:** `7ecd0ef3` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking, Rule-3).
**Impact on plan:** Diperlukan agar N5 (unlink) berfungsi runtime; signature `InjectBatchAsync` tak berubah, skor/status/jawaban online tak disentuh. Tidak ada scope creep.

## Issues Encountered

None — Tasks 1-3 dieksekusi sesuai plan (build green, e2e 6/6, 0 migration). Checkpoint Task 4 di-drive orchestrator dan APPROVED.

## Human-Verify Outcome (Task 4 — checkpoint:human-verify, APPROVED)

Orchestrator menjalankan UAT live di browser nyata (admin@pertamina.com, AD-off, localhost:5277), DB snapshot/restore per CLAUDE.md Seed Workflow (entry SEED_JOURNAL Phase 397 status CLEANED). **9/9 kontrak PASS:**

1. Tipe Pre-Test → "Cari Room Pasangan" + hint "Cari room Post-Test pasangannya"; Standard → tersembunyi.
2. Modal hanya menampilkan room Post-Test; debounced search memfilter.
3. Badge Kasus A "Sudah ber-grup" / Kasus B "Standalone".
4. Pick → chip + hidden #LinkedTargetRepId=173 + "Ganti Room"/remove.
5. Pratinjau pairing "1 ter-pair / 0 tanpa pasangan".
6. Kasus B banner "Data online akan disentuh… skor/jawaban/status TIDAK diubah".
7. Date-warn (Pre > Post).
8. **KRITIS §13 commit:** inject Pre (Id 174) + online Post (Id 173) share LinkedGroupId=173 + same UserId; online Score=85 + Status=Completed UNCHANGED; audit LinkPrePost ×1. Cross inject↔online grouping utuh.
9. Unlink via Bootstrap confirm modal (bukan native) → 173/174 link columns NULL, Score utuh, audit LinkPrePostUndo ×2.

DB di-restore ke baseline (sessions 60→60, 0 link audit, 0 baris UAT).

## Next Phase Readiness

- **INJ-12 COMPLETE** lintas plan 01-04 (TDD lock → service wiring → controller wiring → view + e2e). Cross inject↔online grouping §13 terbukti runtime + DB + UAT live, data online tak disentuh.
- **Phase 397 siap ke Phase 398 (Test + UAT "seakan online", INJ-13)** — E2E full lifecycle inject → /CMP/Records + /CMP/Results per-soal + sertifikat + regression suite + audit milestone v32.2.
- **0 migration** sepanjang fase. Branch main. Saat push: notify IT (commit hash + migration=FALSE). ❌ tidak ada edit Dev/Prod.
- Phase-level completion (transition 397→398) dimiliki orchestrator setelah verifikasi — TIDAK dijalankan di sini.

## Self-Check: PASSED

- FOUND: .planning/phases/397-link-pre-post-ke-room-existing/397-04-SUMMARY.md
- FOUND: tests/e2e/inject-assessment-397.spec.ts
- FOUND: Views/Admin/InjectAssessment.cshtml
- FOUND commit: 47b0e875 (Task 1 — modal/chip/pairing/unlink UI N1-N5)
- FOUND commit: 7ecd0ef3 (Task 2 — surface resolvedGroupId post-commit, Rule-3)
- FOUND commit: 2d19bf07 (Task 3 — Playwright e2e + cross-grouping §13)

---
*Phase: 397-link-pre-post-ke-room-existing*
*Completed: 2026-06-18*
