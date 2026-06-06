# Phase 347: cmp-records-i18n-a11y-polish - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-04
**Phase:** 347-cmp-records-i18n-a11y-polish
**Areas discussed:** POL-08 records.css (inclusion+scope), verification depth, i18n terms

**Konteks:** Spec §347 sudah APPROVED (POL-01..10 dgn teks exact). User pilih diskusikan ke-3 gray area + catatan "semua check dulu, dan sesuai reko" → lock ke rekomendasi.

---

## POL-08 records.css — inclusion + extraction scope

| Option (inclusion) | Description | Selected |
|--------|-------------|----------|
| (a) `<link>` inline per-view | No _Layout change; link di body (works, kurang ideal) | |
| (b) `@section Styles` + _Layout RenderSection | Tambah RenderSectionAsync('Styles') ke _Layout head + @section per full-page view; RecordsTeam(partial) styling via parent link | ✓ |
| (c) Global _Layout link | records.css load tiap page (wasteful) | |

| Option (scope) | Selected |
|--------|----------|
| Hanya .sticky-header + @keyframes (common 3 view) | |
| + .stat-card/.stat-icon (semua common, 2-3 view) | ✓ |

**User's choice:** (b) inclusion + ekstrak semua common (sesuai reko)
**Notes:** RecordsTeam = PARTIAL → tak bisa @section; CSS-nya di-cover link parent Records page. Ekstrak HARUS verbatim (hindari visual regression).

---

## Kedalaman verifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Build + grep saja | Cepat, tapi miss visual regression | |
| Build + grep + Playwright spot-check ringan | Cek no visual regression 1-2 surface | ✓ |
| Full re-UAT | Overkill utk polish LOW | |

**User's choice:** Build+grep + Playwright spot-check (sesuai reko)
**Notes:** Risk LOW; regresi utama dari POL-08 CSS extraction. No xUnit baru (no logic change). Reuse cmp346-seed.sql utk POL-01 null-case.

---

## Konfirmasi istilah i18n + label tombol

| Option | Selected |
|--------|----------|
| Default spec verbatim (Lulus/Tidak Lulus/Nilai/Jabatan/Semua.../Tutup) | ✓ |
| Override istilah tertentu | |

**User's choice:** Default spec verbatim (sesuai reko)

---

## Claude's Discretion
- Plan split (i18n / a11y / POL-08 DRY isolasi). Planner putuskan.
- Fallback inline `<link>` bila _Layout RenderSection berisiko (default @section).

## Deferred Ideas
- MAM/MAP → Phase 348/349. MAP-20 SUDAH COVERED Phase 346 → drop di 349.
- Behavior/logic/xUnit → out of scope (pure polish).
