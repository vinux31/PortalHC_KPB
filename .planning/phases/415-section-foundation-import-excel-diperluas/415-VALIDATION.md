---
phase: 415
slug: section-foundation-import-excel-diperluas
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-22
---

# Phase 415 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail lengkap (REQ→test map, sampling, Wave-0 gaps, invariant kompatibel-mundur) ada di `415-RESEARCH.md` §Validation Architecture — difinalkan oleh `/gsd-validate-phase 415`.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (+ EF Core InMemory 8.0.0; real-SQL fixture `localhost\SQLEXPRESS`/`HcPortalDB_Dev` untuk path ExecuteUpdate) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Section"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Estimated runtime** | ~30–90 detik (full) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + quick filter run.
- **After every plan wave:** full suite hijau (baseline existing TIDAK regresi — invariant kompatibel-mundur: Section kosong = perilaku lama).
- **Before `/gsd-verify-work`:** full suite green + `dotnet run` @5277 boot 200 + (UI) Playwright UAT.
- **Max feedback latency:** ~90 detik.

---

## Per-Task Verification Map

Diturunkan saat planning + difinalkan `/gsd-validate-phase`. Sumber: `415-RESEARCH.md` §Validation Architecture (REQ→test map untuk SEC-01..06 + IMP-01..03, 4 file test baru + invariant keystone backward-compat). Tabel lengkap diisi oleh validate-phase.

| Task ID | Plan | Requirement | Test Type | Automated Command | Status |
|---------|------|-------------|-----------|-------------------|--------|
| TBD | TBD | SEC-01..06 / IMP-01..03 | unit + integration (+ Playwright UAT 419) | `dotnet test ... ~Section` | pending |

---

## Wave 0 (test infra gaps)

Per RESEARCH: 4 file test baru (entity/migration, import dual-format + validasi mismatch D-13, fingerprint, Section CRUD) + 1 invariant kompatibel-mundur (assessment tanpa Section = output identik baseline). Detail di `415-RESEARCH.md`.

---

## Backward-Compat Invariant (KEYSTONE)

Assessment/paket **tanpa Section** (`SectionId` semua null) HARUS menghasilkan urutan soal + grading + import yang **identik** dengan baseline pra-415. Ini gerbang regresi utama milestone — tes lama harus tetap hijau tanpa modifikasi.
