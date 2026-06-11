---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 02
subsystem: proton-approval
tags: [cdp, proton, modal, from-progress, drift-kill]
requires:
  - "363-01: ApproveDeliverableCoreAsync/RejectDeliverableCoreAsync/DispatchApproveNotificationsAsync"
provides:
  - "Tepat SATU jalur approve dan SATU jalur reject di kode — kedua surface (Deliverable page + CoachingProton modal) lewat core sama"
  - "T1 fixed: modal approve terakhir → COACH_ALL_COMPLETE ke HC"
  - "T2 fixed: modal reject → full chain reset termasuk HCApprovalStatus"
  - "T7 fixed: race-guard aktif di jalur modal"
  - "D-03: HC reset di kedua jalur resubmit"
affects: [363-06, 363-07]
tech-stack:
  added: []
  patterns: ["predikat-replikasi untuk blok reset controller-embedded"]
key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - HcPortal.Tests/ProtonApproveRejectParityTests.cs
key-decisions:
  - "JSON reject pakai field newStatus='Rejected' (overall) — JS CoachingProton.cshtml:1243-1244 baca data.newStatus untuk badge; nilai 'Rejected' → bg-danger, tidak perlu ubah JS"
  - "JSON approve tetap newStatus='Approved' + field allApproved baru (additive, JS abaikan)"
  - "isSrSpv/isSH lokal di RejectFromProgress dihapus (dead setelah rewire — core derive dari actorRole)"
requirements-completed: [T1, T2, T7]
duration: 12 min
completed: 2026-06-11
---

# Phase 363 Plan 02: Rewire FromProgress Modal Endpoints Summary

`ApproveFromProgress`/`RejectFromProgress` kini delegasi ke core Plan 01 — drift T1/T2/T7 mati struktural: race-guard, allApproved+notif HC, dan full-chain-reset (termasuk HC) otomatis berlaku di jalur modal; 6/6 parity test hijau.

- Duration: 12 min | Tasks: 3/3 | Files: 2

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1+2+3a | `b2b67ee6` | fix(363-02): route FromProgress modal endpoints through shared cores |
| 3b | `238ffc2d` | test(363-02): extend parity tests resubmit HC-reset + FromProgress parity |

## What Was Built

- **ApproveFromProgress** (:2001): guard `canApprove` + section-check (TANPA Admin exempt — divergensi dipertahankan, Pitfall 1) tetap; blok role-set/save inline diganti `ApproveDeliverableCoreAsync` + `DispatchApproveNotificationsAsync`. Modal pertama kali dapat: race-guard (T7), allApproved + COACH_ALL_COMPLETE (T1), notif coach/coachee (D-02).
- **RejectFromProgress** (:2065): blok set inline (termasuk anomali `SrSpvApprovedById/At` saat reject) diganti `RejectDeliverableCoreAsync`. JSON return `newStatus = "Rejected"` (overall) — **JS consumer `CoachingProton.cshtml:1243-1244` baca `data.newStatus`**: `'Rejected'` → badge `bg-danger` text "Rejected". View TIDAK diubah (kompatibel).
- **D-03 belt-and-braces**: `HCApprovalStatus="Pending"` + `HCReviewedById/At=null` ditambah di blok reset `UploadEvidence` wasRejected (:1368) dan `SubmitEvidenceWithCoaching` isResubmit (:2310).
- **Tests +2**: `Reject_ThenResubmit_HCStatusBackToPending` (predikat-replikasi blok resubmit) + `FromProgress_And_Deliverable_RejectCore_ProduceIdenticalEndState`.

## Deviations from Plan

**[Minor] Task 1+2+3a digabung satu commit** — ketiga task menyentuh blok berbeda di CDPController.cs; di-commit sekali setelah semua AC lolos. Impact: none (test commit terpisah).

**Total deviations:** 1 (commit granularity). **Impact:** none.

## Verification

- Grep AC: core dipanggil dari 2 endpoint approve + 2 endpoint reject; `SrSpvApprovalStatus = "Approved"` hanya di core (:1001); anomali `SrSpvApprovedById = user.Id` hilang; HC reset 3 lokasi (:1089/:1368/:2310).
- `dotnet test --filter ProtonApproveRejectParity` → 6/6 PASS.
- UAT perilaku live (modal badge, HC bell) → Plan 07.

## Self-Check: PASSED

## Next

Ready for 363-03 (T4 surface miss penanda — ProtonCompletionService).
