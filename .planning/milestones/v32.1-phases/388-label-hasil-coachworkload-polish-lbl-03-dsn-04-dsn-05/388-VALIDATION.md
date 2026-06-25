---
phase: 388
slug: label-hasil-coachworkload-polish-lbl-03-dsn-04-dsn-05
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
---

# Phase 388 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> **Catatan phase:** ini phase PURE UI/teks (0 backend, 0 logic baru). Mayoritas verifikasi = compile (Razor) + Playwright/UAT browser runtime (lesson proyek: UI/Razor dinamis WAJIB runtime, grep+build TAK cukup). Tak ada unit/business-logic baru untuk di-TDD.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (regression suite, tak ada test baru) + Playwright (e2e/UAT, `tests/e2e/`) |
| **Config file** | `HcPortal.Tests/` (xUnit) · `playwright.config.ts` (baseURL `http://localhost:5277`, globalTeardown restore DB) |
| **Quick run command** | `dotnet build HcPortal.csproj` (Razor compile gate) |
| **Full suite command** | `dotnet test --filter Category!=Integration` (regression no-break) + `dotnet run` (AD-off) lalu Playwright `--workers=1` |
| **Estimated runtime** | build ~30s · fast suite ~ menit · Playwright per-spec ~detik |

---

## Sampling Rate

- **After every task commit:** `dotnet build HcPortal.csproj` (0 error Razor).
- **After every plan wave:** `dotnet test --filter Category!=Integration` (regression tetap hijau — tak ada kode logic disentuh, harus 0 perubahan jumlah pass) + manual/Playwright cek surface yang diubah.
- **Before `/gsd-verify-work`:** build hijau + Playwright/UAT browser semua aksi parity hijau (Phase 390 tuntaskan parity penuh; phase ini minimal jangan rusak).
- **Max feedback latency:** ~60 detik (build).

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 388-LBL | 01 | 1 | LBL-03 | — | N/A (string statis, Razor auto-encode) | build+grep+manual UAT | `dotnet build` + grep `Batas Nilai Kelulusan`=1 + UAT browser `/CMP/Results/166` | ✅ | ✅ green |
| 388-DSN04 | 02 | 1 | DSN-04 | — | parity approve/skip/threshold tetap jalan | Playwright | `npx playwright test coachworkload-388 --workers=1` (filter card + Saran list-group + filter submit) | ✅ `coachworkload-388.spec.ts` | ✅ green |
| 388-DSN05 | 02 | 1 | DSN-05 | — | N/A (CSS only) | Playwright+grep | Playwright legend-dot/chart + grep 0 magic-number font-size | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] (opsional) `tests/e2e/coachworkload-388.spec.ts` — assert parity runtime: filter section, export hadir, set threshold (Admin), approve & skip saran (fade-out via `#sug-{id}`), chart `#workloadChart` render. `--workers=1`.
- [ ] Tak ada framework install — xUnit + Playwright sudah ada.

*Jika spec Playwright dianggap berlebihan untuk perubahan kosmetik, manual UAT browser cukup — keputusan planner; parity penuh dikunci Phase 390.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Label kartu "Batas Nilai Kelulusan" muncul | LBL-03 | String statis non-JS; visual | Buka `/CMP/Results/{id}`, cek kartu tengah berlabel "Batas Nilai Kelulusan", persen tak berubah |
| Filter bar + "Saran Penyeimbangan" dalam card konsisten | DSN-04 | Visual hierarchy | Buka `/Admin/CoachWorkload`, cek keduanya ber-card + card-header seperti card chart/tabel |
| Spacing & font konsisten, tak ada magic-number | DSN-05 | Visual | Inspect: tak ada inline `font-size:11px/12px`; legend dot pakai `.legend-dot` |

---

## Validation Sign-Off

- [x] Semua task punya verify (build + Playwright + manual UAT) atau Wave 0 dep
- [x] Sampling continuity: tak ada 3 task berturut tanpa verify otomatis (build = gate tiap task)
- [x] Wave 0 menutup referensi MISSING (spec `coachworkload-388.spec.ts` dibuat + hijau)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` di-set

**Approval:** verified 2026-06-17

---

## Validation Audit 2026-06-17

| Metric | Count |
|--------|-------|
| Requirements (Phase 388) | 3 (LBL-03, DSN-04, DSN-05) |
| COVERED | 3 |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |
| Resolved | 0 (tak perlu generate test baru) |
| Escalated | 0 |

**State A audit (orchestrator):** Post-eksekusi, semua 3 requirement Phase 388 COVERED:
- **LBL-03** — `dotnet build` 0 error + grep `Batas Nilai Kelulusan`=1 / `>Nilai Kelulusan<`=0 + UAT browser live (`/CMP/Results/166` → "Batas Nilai Kelulusan 80%"). String statis → render-runtime via manual UAT (legit, non-JS).
- **DSN-04 + DSN-05** — `tests/e2e/coachworkload-388.spec.ts` (5 test, `--workers=1`): filter card, Saran list-group (no card-in-card), legend `.legend-dot`, chart, filter submit → **5 pass / 1 skip**. Skip = approve/skip parity (butuh data coach overload — DSN-06, requirement **Phase 390**, bukan 388).
- Regresi: fast xUnit `dotnet test --filter Category!=Integration` **347/347** (view-only, 0 logic break).

Tak ada gap MISSING → tak spawn gsd-nyquist-auditor (Step 3 "no gaps → compliant"). DSN-06 (approve/skip runtime + HC non-Admin negative) = scope Phase 390, di-track di sana.
