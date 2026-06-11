---
phase: 371
slug: sesi-online-tampil-di-tab-input-records-visibility-only
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-11
---

# Phase 371 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) + Playwright (e2e TypeScript) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/e2e/` |
| **Quick run command** | `dotnet build` (Razor view — WAJIB build, lesson Phase 354) |
| **Full suite command** | `dotnet test` (suite ~226) |
| **Estimated runtime** | build ~55s · full test ~90s |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (view change → build wajib) + `dotnet test`
- **After every plan wave:** full suite hijau (~226)
- **Before `/gsd-verify-work`:** build 0 error + suite hijau + UAT Playwright @5277
- **Max feedback latency:** 150 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 371-01-?? | 01 | 1 | URG-03 (SC1/SC2) | — | online rows tampil tanpa melonggarkan authorize; tombol Lihat hasil hanya Completed/Pending (gate CMP/Results IsAssessmentSubmitted selaras) | build + grep | `dotnet build` 0 error; grep `Assessment Online` di `_TrainingRecordsTab.cshtml` | ✅ | ⬜ pending |
| 371-01-?? | 01 | 1 | URG-03 (SC4) | — | zero regresi suite | full suite | `dotnet test` → Failed: 0 | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers phase requirements — derivasi label inline view (tak unit-testable) diandalkan ke UAT Playwright; tidak ada test existing yang assert komposisi row `_TrainingRecordsTab` (verified grep — `WorkerDataServiceSearchTests.cs:135` assert `GetUnifiedRecords` surface /CMP/Records, TIDAK terdampak).

Opsional (planner putuskan): e2e spec committed `tests/e2e/input-records-online.spec.ts` (expand worker → assert badge + tombol).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Online rows tampil semua status + badge "Assessment Online" | URG-03 SC1 | Razor render runtime (lesson 354: build/grep tak cukup untuk dynamic view) | `Authentication__UseActiveDirectory=false dotnet run` → admin @5277 → `/Admin/ManageAssessment` Tab Input Records → Bagian GAST → expand Rino (NIP 29007720, punya online lama Lulus/Gagal/2025) → row online tampil dengan badge + status benar |
| Manual rows tak berubah + online tanpa hapus | URG-03 SC2 | idem | Expand worker ber-record manual → edit/hapus manual tetap; row online TANPA tombol hapus/edit; tombol Lihat hasil hanya di Completed/Menunggu Penilaian, link buka `CMP/Results?id=` |
| Empty-state copy baru | URG-03 SC3 | idem | Expand worker tanpa record apa pun → "Belum ada record untuk pekerja ini." |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 150s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
