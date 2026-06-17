---
phase: 388
slug: label-hasil-coachworkload-polish-lbl-03-dsn-04-dsn-05
status: draft
nyquist_compliant: false
wave_0_complete: false
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
| 388-LBL | 0x | 1 | LBL-03 | — | N/A (string statis, Razor auto-encode) | manual/build | `dotnet build` + UAT browser `/CMP/Results/{id}` | ✅ | ⬜ pending |
| 388-DSN04 | 0x | 1 | DSN-04 | — | parity approve/skip/threshold tetap jalan | manual/Playwright | `dotnet build` + Playwright CoachWorkload | ❌ W0 (spec baru) | ⬜ pending |
| 388-DSN05 | 0x | 1 | DSN-05 | — | N/A (CSS only) | manual/build | `dotnet build` + visual UAT | ✅ | ⬜ pending |

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

- [ ] Semua task punya verify (build + manual/Playwright) atau Wave 0 dep
- [ ] Sampling continuity: tak ada 3 task berturut tanpa verify otomatis (build = gate tiap task)
- [ ] Wave 0 menutup referensi MISSING (spec Playwright opsional)
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` di-set saat plan final

**Approval:** pending
