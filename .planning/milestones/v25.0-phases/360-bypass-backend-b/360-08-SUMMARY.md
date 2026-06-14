---
phase: 360-bypass-backend-b
plan: 08
subsystem: proton-bypass
tags: [proton, bypass, integration-test, uat, gate-exempt]
requires: [360-07]
provides:
  - "Integration test exempt gate Origin=Bypass (cross-year skip + gate 100% tetap + regresi normal)"
  - "360-UAT.md 6 skenario PASS live @5277"
affects: [361]
tech-stack:
  added: []
  patterns: []
key-files:
  created:
    - .planning/phases/360-bypass-backend-b/360-UAT.md
  modified:
    - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
    - Services/ProtonBypassService.cs
    - Controllers/ProtonDataController.cs
    - docs/SEED_JOURNAL.md
key-decisions:
  - "Gate exempt diuji tingkat predikat (SkippedByCrossYearGateAsync replikasi persis :1372-1379) — gate embedded di controller, opsi yang diizinkan plan"
  - "UAT via HTTP endpoint + SQL cross-check (Chrome profile Playwright ke-lock sesi paralel) — setara devtools manual per plan"
  - "SC3 'hapus' dipenuhi sebagai soft-cancel Status='Dibatalkan' (D-14/I-03) — BUKAN row terhapus, dinyatakan eksplisit di UAT"
requirements-completed: [PBYP-02, PBYP-07]
duration: 29 min
completed: 2026-06-10
---

# Phase 360 Plan 08: Verifikasi E2E + UAT Summary

Fase 360 terverifikasi end-to-end: 3 integration test exempt gate (Bypass lolos cross-year, gate 100% TETAP D-05, normal tetap keblok) hijau, dan **6 skenario UAT PASS live @5277** — termasuk hook GradingService terbukti E2E dua jalur (AkhiriUjian→Siap+notif; SubmitEditAnswers re-grade→revert D-15). Human checkpoint **approved**.

## Hitungan test final

- Full suite **206/206** (build 0 error) — naik dari 203 (plan 07) dengan 3 test exempt baru.
- Bypass-related: 19 ProtonBypassServiceTests + 8 ProtonBypassEndpointTests + 4 ProtonYearGateIntegrationTests + 13 unit BypassValidator/ProtonYearGate.

## Hasil UAT 6 skenario (semua PASS — detail di 360-UAT.md)

U1 CL-A (pindah instan + penanda tak dobel) · U2 CL-B(a) (force-approve + history + penanda Bypass) · U3 CL-B(b) full lifecycle (pending→exam lulus→Siap+notif HC+penanda Exam→Confirm→pindah+bootstrap W-02+Selesai) · U4 CL-C (turun + B-06 anti-dobel) · U5 batal 3 cabang (Dibatalkan/pertahankan-hasil/W-03 Completed-gagal) · U6 re-grade fail (D-15 revert + penanda Exam dihapus).

**SC3 (I-03):** 'belum-dikerjakan→hapus' DIPENUHI sebagai soft-cancel `Status='Dibatalkan'` (keputusan D-14, reversible) — bukan row terhapus.

Seed lifecycle bersih: snapshot → seed uat360-u1..u7 → UAT → RESTORE (0 sisa) → journal `cleaned`.

## Deviations from Plan

**[Rule 1 - Bug] TahunKe bare session pakai Urutan global** — Found during: Task 3 UAT sanity | Issue: `ProtonTracks.Urutan` global 1-6 lintas TrackType (Operator Tahun 1 = Urutan 4) → bare session CL-B(b) tertulis `TahunKe='Tahun 4'` (plan 04 interfaces salah asumsi Urutan==tahun) | Fix: `TahunKe`/`Title` pakai `sourceTrack.TahunKe`; `BypassDetail` + field `sourceTahunKe` | Files: Services/ProtonBypassService.cs, Controllers/ProtonDataController.cs | Verification: 44 bypass test hijau + live session 163 `TahunKe='Tahun 1'` | Commit: a628b082

**[Tool fallback] Playwright MCP → HTTP murni** — Chrome profile mcp-chrome ke-lock sesi paralel; UAT dijalankan via PowerShell WebSession (login + antiforgery header `RequestVerificationToken`) — setara "request manual devtools" yang plan izinkan.

**Total deviations:** 1 bug auto-fixed + 1 tool fallback. **Impact:** TahunKe fix mencegah data salah-kategori exam di produksi.

## ⚠ IT Notify Reminder (handoff)

- **Migration#2 WAJIB di-apply Dev:** `AddPendingProtonBypassAndAssignmentOrigin` — tabel `PendingProtonBypasses` (12 kolom) + kolom `ProtonTrackAssignments.Origin`.
- **Schema drift B-05:** `AssessmentSessions.AssessmentType` NOT NULL di DB nyata — kode bare session sudah set eksplisit 'Standard'.
- Sertakan commit hash + flag migration saat notifikasi IT (DEV_WORKFLOW).

## Observasi non-blocking (untuk Phase 361 / verifier)

`BypassValidator` D-B pakai `Urutan` global untuk |Δ|≤1 — dalam satu TrackType benar (consecutive), tapi lintas TrackType jarak Urutan ≠ jarak tahun (mis. Operator T1=4 vs Panelman T3=3 → diff 1 = lolos; Operator T1→Panelman T1 → diff 3 = ditolak padahal "lateral"). UI Phase 361 sebaiknya batasi pilihan target ke TrackType yang sama, atau validator di-upgrade pakai (TrackType, TahunKe).

## Self-Check: PASSED

## Next

Phase complete — 8/8 plan. Lanjut verifikasi fase (gsd-verifier) + code review.
