# Phase 420 — Deferred Items (out-of-scope discoveries)

> Ditemukan selama eksekusi, TIDAK diperbaiki (scope boundary: hanya auto-fix issue yang
> langsung disebabkan perubahan task ini).

## 420-03 (Plan 03)

- **Pre-existing `tsc --noEmit` errors di spec e2e tak-terkait** (ditemukan saat typecheck Task 3).
  File: `tests/e2e/manage-org-label.spec.ts` (TS2552/TS2304: `doToggle`, `openAddModal`,
  `bootstrap`, `openEditModal`) + `tests/e2e/proton-bypass.spec.ts` (TS7006: param `page` implicit any).
  Bukan disebabkan Plan 420-03 (spec baru `form-persistence-420`/`form-prepost-ux-420` 0 error;
  `--list` parse OK). Playwright transpiler (esbuild) tetap menjalankan spec ini (tidak strict-typecheck
  saat run), jadi non-blocking untuk UAT. Tidak diperbaiki di fase ini — di luar scope FORM-*.
