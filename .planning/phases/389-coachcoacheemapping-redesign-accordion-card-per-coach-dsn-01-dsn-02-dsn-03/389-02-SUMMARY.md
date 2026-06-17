---
phase: 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
plan: 02
status: complete
completed: 2026-06-17
requirements: [DSN-01, DSN-02, DSN-03]
---

# Phase 389-02 Summary — CoachCoacheeMapping Redesign (Accordion Card per Coach)

## What was built

Rewrote `Views/Admin/CoachCoacheeMapping.cshtml` (view-only, 0 backend, migration=FALSE):
- **DSN-03 (Task 1):** Toolbar normalized — Download Template / Import Excel / Export Excel grouped in a `btn-group` (`btn-outline-secondary`, all `btn-sm`); "Tambah Mapping" solo `btn-primary` CTA; dead `onclick="...querySelector('[data-bs-dismiss]')..."` removed (modal still opens via `data-bs-toggle="modal"`).
- **DSN-01/02 (Task 2):** Grouped `<table>` (`else`-block) replaced with **accordion card per coach** — each coach = `card border-0 shadow-sm mb-3`; card-header is a real `<button>` collapse toggle (`role`/`aria-expanded`/`aria-controls`, keyboard) containing avatar-initial (36px `bg-primary`, reuse `ManageWorkers.cshtml:251`) + coach name + section + load badge (threshold `>=8 bg-danger / >=5 bg-warning text-dark / else bg-info text-dark`, verbatim) + chevron. Body = `collapse` (default closed, NO `data-bs-parent` → independent). Mini-table = `table-responsive`, **9 columns** (dropped "Coachee Aktif"). Aksi block + all parity hooks copied verbatim. Scoped `@section Styles` chevron rotation.
- **Spec finalize (Task 3):** Adjusted `tests/e2e/coachcoacheemapping-389.spec.ts` selectors to actual markup (V-04/V-07 wait for `.show` stable to avoid `.collapsing` race; V-10 `#editCoacheeName` is `<p>` → `not.toHaveText('')`).

## Commits
- `7548c6d0` feat(389-02): normalisasi toolbar + hapus dead onclick (DSN-03)
- `aa377982` feat(389-02): rewrite grouped table → accordion card per coach (DSN-01/02)
- `2ca83c2a` test(389-02): finalize spec selectors to actual accordion markup (V-04/V-07/V-10 green)

## Verification
- `dotnet build` → **0 errors** (25 pre-existing nullable warnings, unrelated).
- Playwright `coachcoacheemapping-389 --workers=1` @ localhost:5277 → **11 passed / 4 skipped / 0 FAILED**.
  - Green (structural): V-01 card, V-02 badge threshold, V-03 default-closed, V-04 collapse toggle, V-06 9-kolom, V-07 a11y keyboard, V-08 toolbar, V-09 tambah-modal, V-10 edit-modal, V-14 filter.
  - Skipped (data-guard, full mutation parity = Phase 390): V-05 independent (needs ≥2 coaches), V-11 delete-row, V-12 aksi-branch, V-13 ajax-appUrl.

## Parity hooks verified intact (grep)
data-mapping-id ✓ · openEditModal 7-arg ✓ · MarkMappingCompleted form ✓ · reactivateMapping/confirmDeactivate/confirmDelete ✓ · badge threshold verbatim ✓ · `@section Scripts` count=1 (frozen) ✓ · 5 modal IDs untouched ✓ · collapse-@idx id↔target pair ✓ · var idx / idx++ ✓ · 0 `data-bs-parent` ✓ · 0 "collapse show" ✓ · "Coachee Aktif" dropped (0) ✓ · **0 new @Html.Raw** (3 = all pre-existing: statusBadge + sectionUnits + coachWorkloads) → no XSS regression.

## Self-Check: PASSED (Tasks 1-3)

## Deviations / incidents
- **Executor drift + crash:** The first 389-02 executor finished Tasks 1-2 (committed) but then went OUT OF SCOPE — it pre-created a full phase-390 discuss+research+ui-spec+plan cycle and triggered the Playwright assessment-matrix seed fixture, then died on an API 529. The orchestrator recovered via filesystem/git spot-check: verified Tasks 1-2 markup correct, kept the high-quality 390 docs (user decision; flagged as needing plan-check before 390 exec), corrected STATE.md, and completed Task 3 inline.
- SEED_JOURNAL "matrix" rows are the normal `global.setup.ts` test fixture (BACKUP→seed→RESTORE→cleaned per run), not pollution — DB restored clean.

## Task 4 — APPROVED (checkpoint:human-verify, blocking) — browser UAT 2026-06-17
Browser UAT driven via Playwright MCP @ http://localhost:5277/Admin/CoachCoacheeMapping (admin@pertamina.com, app run AD-off + lpc:Lenovo shared-mem). DB lokal = 1 coach group ("Rustam Santiko — GAST", 1 coachee aktif "Rino").

Results — ALL PASS:
- Card per coach: avatar inisial "R" bulat biru + nama + section "— GAST" muted + badge beban "1" warna **bg-info (biru)** (activeCount=1 <5 ✓ threshold), chevron-down. Default **TERTUTUP** saat load (V-01/02/03).
- Klik header → mini-table muncul, chevron berputar ke atas, aria-expanded=true (V-04/V-07). Kolom = **9** (Nama/NIP/Bagian Penugasan/Unit Penugasan/Jabatan/Proton Track/Status/Mulai/Aksi), **TANPA "Coachee Aktif"** (V-06 ✓). Badge Status "Aktif" hijau + Proton Track "Operator - Tahun 1" biru render benar.
- Aksi branch parity: row aktif → Edit / Nonaktifkan / Graduated (urutan H-7 ✓).
- Modal **Edit** (openEditModal) terisi benar: Coachee=Rino, Coach=Rustam Santiko(GAST), Bagian=GAST, Unit=Alkylation Unit (065), Tanggal Mulai=2026-04-10 (V-10 ✓).
- Modal **Tambah Mapping** (#assignModal) buka via data-bs-toggle, form lengkap + coachee checkbox (V-09 ✓). Dead onclick hilang.
- Modal **Import Excel** (#importMappingModal) buka, file input + Upload disabled-until-file (H-10 ✓).
- Toolbar: 3 tombol Excel dalam btn-group outline rapi + "Tambah Mapping" btn-primary biru solo (V-08 ✓).
- Console error level=error: **0** (V-13, no 404/500).
- Spacing antar-card + legibilitas avatar + animasi chevron: rapi/subjektif-OK.

Data-guard (TIDAK bisa diuji, DB lokal 1 coach saja → Phase 390 full mutation parity):
- V-05 independent multi-open (butuh ≥2 coach), branch inaktif "Aktifkan/Hapus" (butuh mapping non-aktif), mutasi nyata assign/import/export/row-removal.

Verdict: tampilan & semua aksi existing **PARITY OK** → checkpoint approved. Phase 389 siap verifikasi.
