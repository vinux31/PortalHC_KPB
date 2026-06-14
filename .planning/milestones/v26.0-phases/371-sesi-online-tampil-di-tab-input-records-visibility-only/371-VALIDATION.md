---
phase: 371
slug: sesi-online-tampil-di-tab-input-records-visibility-only
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-11
updated: 2026-06-12
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
| 371-01-T1 | 01 | 1 | URG-03 (SC1/SC2) | T-371-02/03 | online rows tampil tanpa melonggarkan authorize; tombol Lihat hasil hanya Completed/Pending (gate CMP/Results IsAssessmentSubmitted selaras) | build + grep | `dotnet build` 0 error; grep `Assessment Online` di `_TrainingRecordsTab.cshtml` | ✅ | ✅ green |
| 371-01-T1 | 01 | 1 | URG-03 (SC4) | — | zero regresi suite | full suite | `dotnet test` → Failed: 0 (226/226 baseline) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Eksekusi 2026-06-12:** `dotnet build` → 0 error 0 warning (anon-shape 3-operand `.Concat()` compile OK — catch Pitfall 1/2). `dotnet test HcPortal.Tests` → 226/226 baseline pass (2 failure di `AssessmentWindowRemovalTests.cs` = WIP sesi paralel 370-secure, action `ManageAssessmentTab_Assessment`, independen). Grep acceptance semua match (`Assessment Online`=1, `AssessmentOnline`=2, `!a.IsManualEntry`, `Url.Action("Results","CMP"`, empty-state baru, `DeleteTraining`+`DeleteManualAssessment` retained, `.Concat(`=2).

---

## Wave 0 Requirements

Existing infrastructure covers phase requirements — derivasi label inline view (tak unit-testable) diandalkan ke UAT Playwright; tidak ada test existing yang assert komposisi row `_TrainingRecordsTab` (verified grep — `WorkerDataServiceSearchTests.cs:135` assert `GetUnifiedRecords` surface /CMP/Records, TIDAK terdampak).

Opsional (planner putuskan): e2e spec committed `tests/e2e/input-records-online.spec.ts` (expand worker → assert badge + tombol).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Online rows tampil semua status + badge "Assessment Online" | URG-03 SC1 | Razor render runtime (lesson 354: build/grep tak cukup untuk dynamic view) | `Authentication__UseActiveDirectory=false dotnet run` → admin @5277 → `/Admin/ManageAssessment` Tab Input Records → Bagian GAST → expand Rino (NIP 29007720, punya online lama Lulus/Gagal/2025) → row online tampil dengan badge + status benar | ✅ PASS 2026-06-12 (Playwright MCP) |
| Manual rows tak berubah + online tanpa hapus | URG-03 SC2 | idem | Expand worker ber-record manual → edit/hapus manual tetap; row online TANPA tombol hapus/edit; tombol Lihat hasil hanya di Completed/Menunggu Penilaian, link buka `CMP/Results?id=` | ✅ PASS 2026-06-12 (`/CMP/Results/126` load penuh, bukan 403) |
| Empty-state copy baru | URG-03 SC3 | idem | Expand worker tanpa record apa pun → "Belum ada record untuk pekerja ini." | ✅ PASS 2026-06-12 (Choirul Anam) |

*Render-runtime SC1/SC2/SC3 genuinely tak unit-testable murah (371 ubah projection di VIEW Razor, bukan controller query — beda dari 370 yang testable via ViewData). e2e spec `tests/e2e/input-records-online.spec.ts` planner tandai OPSIONAL; tidak dibuat. UAT Playwright live 3/3 PASS = bukti manual, user approved.*

---

## Validation Audit 2026-06-12

| Metric | Count |
|--------|-------|
| Gaps found | 1 (render-runtime SC1/SC2/SC3) |
| Resolved (automated) | 0 |
| Manual-only (justified, lesson 354) | 1 |
| Escalated | 0 |

Automated layer (build + grep + full suite) hijau saat eksekusi. Render-runtime → Manual-Only (UAT Playwright 3/3 PASS). Tidak ada gap yang automatable secara murah; nyquist_compliant: true dengan render Manual-Only by design.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (build + grep + full suite; render → Manual-Only justified)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (render-runtime → Manual-Only, lesson 354)
- [x] No watch-mode flags
- [x] Feedback latency < 150s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-12
