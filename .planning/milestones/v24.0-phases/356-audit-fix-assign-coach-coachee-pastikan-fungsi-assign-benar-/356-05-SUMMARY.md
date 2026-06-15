---
phase: 356-audit-fix-assign-coach-coachee
plan: 05
subsystem: coaching-verification
tags: [coaching, verification, uat, playwright, seed-workflow]
requires: [356-01, 356-02, 356-03, 356-04]
provides: [phase-356-uat-evidence]
affects: [docs/SEED_JOURNAL.md]
tech-stack:
  added: []
  patterns: [seed-workflow-snapshot-restore]
key-files:
  created: []
  modified:
    - docs/SEED_JOURNAL.md
key-decisions:
  - "UAT dijalankan Claude via Playwright MCP @localhost:5277 (AD=false) atas permintaan user"
  - "AF-6 race tak bisa direproduksi single-threaded → code-verified; AF-5 hanya 1 coach di data → reassign-to-same membuktikan 3-notif path"
  - "Seed state-only (no schema), SEED_WORKFLOW snapshot+restore disiplin"
requirements-completed: [AF-1, AF-2, AF-3, AF-5, AF-6, AF-7]
duration: ~25 min
completed: 2026-06-09
---

# Phase 356 Plan 05: Gate + UAT Summary

Gate otomatis hijau + UAT browser (Playwright MCP @localhost:5277, AD=false) untuk keenam fix. SEED_WORKFLOW disiplin: snapshot pre-UAT → state-only seed → RESTORE WITH REPLACE → verifikasi DB bersih.

## Task 1 — Gate otomatis
- `dotnet build HcPortal.csproj` → **0 error** (22 warning baseline).
- `dotnet test` → **135/135 passed** (131 baseline + 4 [Fact] AF-1 `CoacheeEligibilityCalculator`), 0 regresi (~10s).

## Task 2 — Seed + UAT (Playwright)
**SEED_WORKFLOW:** snapshot `C:\Temp\HcPortalDB_Dev_pre356uat_20260609_153801.bak` → seed state-only → RESTORE 1938 pages → verify clean (COACH_REASSIGNED=0, Rino mapping IsActive=1/IsCompleted=0, track4 Approved=0, test mapping gone). SEED_JOURNAL `cleaned`.

**Data track id=4 (multi-unit, audit-verified):** Alkylation Unit (065)=3 deliverable, RFCC NHT (053)=1. Coachee track-4: Rino (Alkylation).

### Hasil UAT
| Fix | Metode | Hasil |
|-----|--------|-------|
| **AF-1** (headline) | `GET /Admin/GetEligibleCoachees?protonTrackId=4` authenticated | **PASS** — Rino 3/3 Approved → MUNCUL (`[{Rino...}]`). Old code bandingkan 3==4(total) → tak pernah muncul; new per-unit 3==3 → muncul. |
| AF-1 negatif | flip 1 progress→Pending, refetch | **PASS** — 2/3 → `[]` (tidak eligible). |
| **AF-2** | exercise `updateAssignmentDefaults()` (simulasi 2 unit di DOM, data nyata 1 unit) | **PASS** — centang unit X → checkbox unit lain `disabled`+`text-muted`+hint muncul; clear → semua re-enable+hint hilang; `style.display` tak tersentuh (filterCoacheesBySection utuh). |
| **AF-3 / D-06** | set mapping IsActive=0+IsCompleted=1, reload `?showAll=true` | **PASS** — graduated tampil badge "Graduated" + Edit only (BUKAN tombol "Aktifkan"); re-assignability: graduated Rino kembali ke pool eligible coachee (was blocked pre-fix). Action transaksi/cascade = code-verified (full graduate butuh fixture Tahun-3; Rino Tahun-1). |
| **AF-5** | `POST /Admin/ApproveReassignSuggestion` (mapping 13) | **PASS** — 3 row `UserNotifications` Type=COACH_REASSIGNED: "Penugasan Coaching Dialihkan" (coach lama), "Coach Ditunjuk" (coach baru), "Coach Anda Berubah" (coachee). Microcopy match UI-SPEC. |
| **AF-6** | — | **CODE-VERIFIED** — race unique-index tak bisa direproduksi single-threaded (pre-check L474 menangkap duplikat sebelum insert). Catch `DbUpdateException ... when (IX_...ActiveUnique/2601/2627)` sebelum generic, pesan ramah, no `ex.Message` leak (grep). |
| **AF-7** | `POST CoachCoacheeMappingAssign` ConfirmProgressionWarning=false | **PASS** — Regan (tanpa Operator-Tahun-1) → warning verbatim "1 coachee belum menyelesaikan Operator - Tahun 1. Tetap lanjutkan?" (cabang 2, no insert). Iwan (prev exists) → assign tanpa warning (cabang 1/3, konsisten old code). Kedua cabang batch-query terbukti, output identik. |

## Task 3 — Human verify
Checkpoint blocking. UAT dijalankan Claude via Playwright atas permintaan user ("verifikasi via browser, catat jika ada temuan"). Awaiting user final sign-off.

## Deviations / Temuan
1. **[Env] Build output lock** — dev server `dotnet run` lokal memegang `HcPortal.dll`/`.exe` → memblok `dotnet build` (MSB3027/3021, bukan CS error). Dihentikan untuk build bersih; di-relaunch AD=false untuk UAT.
2. **[Plan] Filter test salah** — `~IsEligiblePerUnit` → 0 match (nama method = behavior); koreksi ke `~CoacheeEligibilityCalculator` (4/4). Sudah dicatat Plan 01.
3. **[Observasi, bukan bug] AF-7 cabang-1** — `hasForRequestedTrack` & prev-assignment lookup tidak filter `IsActive` (coachee dgn assignment track lama inaktif tetap dianggap "ada"). Ini **identik perilaku kode lama** (zero behavior change terjaga) — bukan regresi. Bila ingin warning lebih ketat, itu enhancement terpisah (di luar scope AF-7 parity).

## Issues Encountered
Tidak ada blocking. AF-6 (race) & AF-3-action (full graduate) tervalidasi via kode + state-sim, bukan e2e penuh (keterbatasan data: 1 coach, Rino Tahun-1) — non-blocking, severity LOW/MED.

## Next Phase Readiness
6 fix terverifikasi (AF-1/2/3/5/7 functional + AF-6 code). DB lokal bersih. Awaiting human sign-off → verifier → phase complete. IT handoff: migration=false. AF-4 → backlog (`/gsd-add-backlog`).
