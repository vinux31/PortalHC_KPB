---
phase: 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
verified: 2026-06-17T09:00:00Z
status: passed
score: 5/5
overrides_applied: 0
re_verification: false
---

# Phase 389: CoachCoacheeMapping Redesign — Accordion Card per Coach — Verification Report

**Phase Goal:** Daftar mapping coach-coachee tampil sebagai accordion card per coach yang rapi & dapat dibuka/tutup, dengan toolbar header seragam dan tanpa dead-code — semua aksi/modal/AJAX/threshold existing tetap berfungsi byte-for-byte (behavior parity).
**Verified:** 2026-06-17T09:00:00Z
**Status:** PASSED
**Re-verification:** Tidak — verifikasi awal (VERIFICATION.md belum pernah dibuat sebelumnya).
**Migration:** FALSE — murni view markup; 0 backend/controller/skema disentuh.

---

## Goal Achievement

### Observable Truths

| # | Truth (dari ROADMAP Success Criteria) | Status | Evidence |
|---|---------------------------------------|--------|----------|
| 1 | Admin/HC melihat accordion card per coach: avatar inisial + nama + section + badge beban warna ikut threshold existing (info `<5` / warning `>=5` / danger `>=8`) | VERIFIED | `card border-0 shadow-sm mb-3` hadir (1x); `avatar-initial` hadir (1x); ternary threshold verbatim `activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark"` hadir (1x). UAT: avatar "R", badge bg-info count=1 ✓ |
| 2 | Admin/HC klik header card buka/tutup daftar coachee; semua 9 kolom existing tampil tanpa "Coachee Aktif" | VERIFIED | Pair `data-bs-target="#collapse-@idx"` ↔ `id="collapse-@idx"` hadir (1x masing-masing); `collapse show` = 0 (default tertutup); `data-bs-parent` = 0 (independen); `Coachee Aktif` = 0 (kolom dibuang); 9 `<th>` tersurat di markup. Playwright V-03/V-04/V-06 PASS. UAT: mini-table 9 kolom confirmed ✓ |
| 3 | Toolbar seragam (Excel btn-group + "Tambah Mapping" btn-primary solo); dead onclick dihapus; #assignModal tetap buka | VERIFIED | `btn-group` hadir (1x); `btn-sm btn-primary` hadir (1x, "Tambah Mapping"); `querySelector('[data-bs-dismiss]')` di toolbar = 0 (semua 16 querySelector ada di `@section Scripts` baris 693+, bukan toolbar); `data-bs-target="#assignModal"` (1x), `#importMappingModal` (1x), `CoachCoacheeMappingExport` (1x), `DownloadMappingImportTemplate` (1x). Playwright V-08/V-09 PASS. UAT: toolbar rapi, Tambah Mapping buka #assignModal ✓ |
| 4 | Parity behavior terjaga: openEditModal, confirmDeactivate, MarkMappingCompleted, reactivateMapping, confirmDelete, badge Graduated, filter + pagination — semua berfungsi identik sebelum redesign | VERIFIED | Hooks: `openEditModal(@coachee.Id` (1x), `confirmDeactivate(@coachee.Id` (1x), `asp-action="MarkMappingCompleted"` (1x), `reactivateMapping(@coachee.Id)` (1x), `confirmDelete(@coachee.Id` (1x), `data-mapping-id="@coachee.Id"` (1x). Branch urutan H-7 (IsCompleted → else if IsActive → else) utuh baris 324-353. `@section Scripts` = 1 (frozen, tak disentuh). `@Html.Raw` = 3 semua pre-existing (statusBadge + sectionUnits + coachWorkloads) — 0 XSS sink baru. Playwright V-10/V-14 PASS. UAT: Edit modal terisi benar (Rino/Rustam/GAST/2026-04-10) ✓. Phase 390 VERIFICATION (11/11) cross-verify mutasi C1-C6 seluruhnya berjalan di markup baru. |
| 5 | `dotnet build` 0 error + Playwright runtime (lesson Phase 354): card collapse OK, modal assign/edit OK, AJAX appUrl + RequestVerificationToken tak hardcode + tak 404 | VERIFIED | Build: 0 error (25 pre-existing nullable warnings tidak terkait). Playwright `coachcoacheemapping-389 --workers=1` @ localhost:5277: **11 passed / 4 skipped / 0 FAILED**. 4 skip = data-guard sah (V-05 butuh ≥2 coach group, V-11/12/13 butuh mapping non-aktif — diakui design; full mutation parity dikunci Phase 390). `@Html.AntiForgeryToken()` = 3 (utuh). UAT Task 4 APPROVED. |

**Score:** 5/5 truths verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/Admin/CoachCoacheeMapping.cshtml` | Accordion card per coach (DSN-01/02) + toolbar seragam (DSN-03), parity hooks utuh | VERIFIED | File ada; `card border-0 shadow-sm mb-3` + `avatar-initial` + threshold ternary + collapse pair + 9 kolom + semua H-1..H-15 hooks hadir; `@section Scripts` frozen (1x); 3 `@Html.Raw` semua pre-existing |
| `tests/e2e/coachcoacheemapping-389.spec.ts` | Playwright parity spec V-01..V-14 (14 test minimum, grep-label cocok VALIDATION.md) | VERIFIED | File ada (371 baris); 18 `test(` call (14 V-label + 1 V-15 yang ditambah Plan 02 Task 3 sebagai bonus parity export); import `from '../helpers/accounts'` (1x); `beforeEach` nav `/Admin/CoachCoacheeMapping`; 19 `test.skip` data-guard; `CoachCoacheeMappingDeletePreview` (4x); `section=` (3x) |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `card-header <button>` (collapse trigger) | `<div class="collapse" id="collapse-@idx">` | `data-bs-toggle="collapse" data-bs-target="#collapse-@idx"` ↔ `id="collapse-@idx"` | WIRED | Pair hadir (masing-masing 1x di dalam foreach loop); `var idx = 0` (1x) + `idx++` (1x) menjamin ID unik per iterasi |
| `<tr data-mapping-id>` | `submitDelete() row.remove` (@section Scripts L991) | `querySelector('tr[data-mapping-id="' + id + '"]')` | WIRED | `data-mapping-id="@coachee.Id"` (1x di markup); JS hook L991 di `@section Scripts` (frozen) menggunakan selector ini — utuh |
| Tombol "Tambah Mapping" | `#assignModal` | `data-bs-toggle="modal" data-bs-target="#assignModal"` (dead onclick DIHAPUS) | WIRED | `data-bs-target="#assignModal"` (1x); `querySelector('[data-bs-dismiss]')` di toolbar = 0; Playwright V-09 PASS |
| `spec coachcoacheemapping-389.spec.ts` | `/Admin/CoachCoacheeMapping` | `page.goto('/Admin/CoachCoacheeMapping')` di `beforeEach` | WIRED | Hadir (1x) |
| `spec coachcoacheemapping-389.spec.ts` | `tests/helpers/accounts.ts` | `import { accounts, AccountKey } from '../helpers/accounts'` | WIRED | Import hadir (1x) |

---

## Data-Flow Trace (Level 4)

Fase ini adalah view-only markup rewrite. Tidak ada komponen dinamis baru yang membuat fetch/query baru — semua data mengalir dari controller yang sudah ada (ViewBag.GroupedCoaches, ViewBag.EligibleCoaches, dll.) via `@section Scripts` yang dibekukan. Level 4 tidak relevan: tidak ada state/hook baru yang perlu di-trace.

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `CoachCoacheeMapping.cshtml` (accordion) | `groupedCoaches` (ViewBag) | Controller `CoachCoacheeMappingController` — tidak disentuh fase ini | Ya (pre-existing, tidak berubah) | FLOWING — data source tidak dimodifikasi; markup baru hanya mengubah cara render |

---

## Behavioral Spot-Checks

Spot-check runtime dilakukan via Playwright dan browser UAT (tidak bisa dijalankan ulang tanpa server):

| Behavior | Basis Verifikasi | Hasil | Status |
|----------|-----------------|-------|--------|
| Card per coach render + default tertutup | Playwright V-01/V-03 PASS + UAT Task 4 | V-01 PASS, V-03 PASS; UAT "Semua card TERTUTUP saat load" | PASS |
| Klik header → collapse buka + chevron berputar | Playwright V-04/V-07 PASS + UAT Task 4 | V-04 PASS, V-07 PASS; UAT "chevron berputar ke atas" | PASS |
| 9 kolom mini-tabel, tanpa "Coachee Aktif" | Playwright V-06 PASS + UAT Task 4 | V-06 PASS; UAT "9 kolom...TANPA Coachee Aktif" | PASS |
| Toolbar seragam + Tambah Mapping buka modal | Playwright V-08/V-09 PASS + UAT Task 4 | V-08 PASS, V-09 PASS; UAT "toolbar rapi...Tambah Mapping buka #assignModal" | PASS |
| Edit modal terisi benar (openEditModal 7-arg) | Playwright V-10 PASS + UAT Task 4 | V-10 PASS; UAT "Modal Edit terisi: Rino, Rustam Santiko(GAST), GAST, 2026-04-10" | PASS |
| Filter seksi → URL `section=` | Playwright V-14 PASS | V-14 PASS | PASS |
| Console error = 0 | UAT Task 4 | "Console error level=error: 0" | PASS |
| Mutation parity (C1-C6: add/edit/deactivate/graduated/reactivate/delete) | Phase 390 VERIFICATION (11/11) cross-verify | Phase 390 passed 11/11; semua mutasi C1-C6 berjalan di markup baru | PASS (cross-ref) |

---

## Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|----------|
| DSN-01 | 389-01-PLAN, 389-02-PLAN | Accordion card per coach: avatar inisial + nama + section + badge beban warna ikut threshold `<5`/`>=5`/`>=8` | SATISFIED | `card border-0 shadow-sm mb-3` + `avatar-initial` + threshold ternary verbatim; Playwright V-01/V-02 PASS; UAT badge bg-info count=1 ✓ |
| DSN-02 | 389-01-PLAN, 389-02-PLAN | Klik header buka/tutup mini-tabel coachee; default tertutup; card independen (multi-open); 9 kolom tanpa "Coachee Aktif" | SATISFIED | Collapse pair + `class="collapse"` tanpa `show` + `data-bs-parent`=0 + `Coachee Aktif`=0 + 9 th tersurat; Playwright V-03/V-04/V-06/V-07 PASS |
| DSN-03 | 389-01-PLAN, 389-02-PLAN | Toolbar seragam: 3 tombol Excel dalam btn-group, "Tambah Mapping" btn-primary solo, dead onclick dihapus tanpa ubah fungsi modal | SATISFIED | `btn-group` (1x) + `btn-sm btn-primary` Tambah Mapping (1x) + dead onclick=0 di toolbar + `data-bs-target="#assignModal"` utuh; Playwright V-08/V-09 PASS; UAT toolbar rapi ✓ |

---

## Anti-Patterns Found

| File | Pola | Keparahan | Dampak |
|------|------|-----------|--------|
| `Views/Admin/CoachCoacheeMapping.cshtml` | `@Html.Raw` (3x) | Info | Semua 3 pre-existing: `statusBadge` (baris 174), `sectionUnitsMap` (baris 627), `coachWorkloads` (baris 630) — tidak ada yang baru. Baseline tidak bertambah. |

Tidak ada blocker, tidak ada warning. Tidak ada TODO/FIXME/placeholder. Tidak ada `return null` / empty implementation. Tidak ada path AJAX hardcode (semua di `@section Scripts` frozen memakai `appUrl()`).

---

## Human Verification Required

Tidak ada. Semua item visual/animasi telah di-verify via checkpoint UAT manusia (Task 4 — blocking gate, APPROVED 2026-06-17) dengan hasil:

- Ritme spacing antar-card: rapi/OK
- Animasi chevron buka/tutup: halus (CSS `transition: transform .2s ease`)
- Legibilitas avatar bulat biru 36px inisial satu huruf: OK
- Kerapian toolbar di layar normal: OK (flex-wrap tersedia untuk layar sempit)
- Semua aksi modal (Edit/Assign/Import): terisi + berfungsi benar
- 0 console error

---

## Gaps Summary

Tidak ada gap. Semua 5 success criteria ROADMAP terverifikasi.

4 test skip di Playwright (`V-05 independent multi-open`, `V-11 delete hapus row`, `V-12 aksi branch`, `V-13 ajax appUrl subpath`) adalah **data-guard yang sah** — DB lokal hanya memiliki 1 coach group dan 0 mapping non-aktif. Ini bukan kegagalan: (a) test skip guard dirancang sejak Plan 01 untuk kasus data terbatas, (b) full mutation parity dengan data lengkap dikunci di Phase 390 VERIFICATION (PASSED 11/11) yang cross-verify semua jalur H-1..H-15 di markup baru, (c) UAT Task 4 membuktikan semua aksi yang bisa diuji dengan data ada berfungsi benar.

**Verdict:** Phase 389 goal tercapai — CoachCoacheeMapping tampil sebagai accordion card per coach, semua existing behavior parity terjaga, toolbar seragam, view-only 0 backend, migration=FALSE.

---

_Verified: 2026-06-17T09:00:00Z_
_Verifier: Claude (gsd-verifier)_
