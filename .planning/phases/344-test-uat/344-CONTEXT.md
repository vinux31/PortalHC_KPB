# Phase 344: Test + UAT - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Verifikasi quality + tidak ada regresi untuk milestone v21.0 (ManageOrganization Overhaul + Level Label CRUD, Phase 340-343 sudah SHIPPED LOCAL). Phase ini **menutup test gap** dan menjalankan UAT — bukan menambah fitur baru.

Cakupan requirement: **TEST-01..06 + ORG-INTEG-03**.

Coverage existing yang sudah ada dari 340-343 (hasil scout codebase):
- `HcPortal.Tests/OrgLabelServiceTests.cs` — 13 [Fact] (GetLabel happy+fallback ✅, Update/Add/Delete, GetMax* helpers)
- `HcPortal.Tests/OrgLabelControllerTests.cs` — 10 [Fact] (validation: empty/whitespace/toolong/duplicate-across-levels ✅; add non-next-level; delete non-highest/in-use)
- `HcPortal.Tests/OrganizationControllerTests.cs` — ada (cek apakah DFS / dup-name per-parent sudah tercover)
- `tests/e2e/` — 9+ Playwright spec dengan `global.setup.ts` + `global.teardown.ts` + `helpers/` (BELUM ada spec ManageOrg/OrgLabel)
- xUnit pakai EF InMemory (tidak exercise migration SQL asli)

</domain>

<decisions>
## Implementation Decisions

### Unit Test Scope (TEST-01..04)
- **D-01:** **Isi gap saja** — pertahankan 23 [Fact] existing (OrgLabelService 13 + OrgLabelController 10), JANGAN rewrite/duplikat. Tambah hanya yang bolong:
  - TEST-02: test permission denial non-Admin/non-HC (403) di `OrgLabelController` — belum ada di test names existing (validation sudah ✅).
  - TEST-03: pre-order DFS sort correctness untuk tree 3 level + multi-root — verifikasi dulu apakah sudah ada di `OrganizationControllerTests`; kalau belum, tambah.
  - TEST-04: dup-name per-parent (same name OK di parent beda, ditolak di parent sama) — verifikasi dulu di `OrganizationControllerTests` (342 ORG-TREE-02 + 6 [Fact] preview==actual); tambah hanya jika belum.
- Planner WAJIB cek isi `OrganizationControllerTests.cs` sebelum nulis test baru, supaya tidak duplikat.

### Integration Test (TEST-05)
- **D-02:** **SQL Server LocalDB / DB test terpisah disposable.** EF InMemory existing TIDAK memvalidasi migration SQL asli, jadi TEST-05 butuh DB betulan.
  - Target: DB test disposable (`HcPortalDB_Test` atau LocalDB instance) — migrate fresh + seed default + service first read + assert, lalu **drop per run**.
  - JANGAN sentuh `HcPortalDB_Dev` → tidak perlu snapshot/restore SEED_WORKFLOW (isolasi bersih via DB terpisah).
  - Test memvalidasi: migration apply sukses + row seed default OrgLabel ada + `OrgLabelService` first read mengembalikan label terkonfigurasi (bukan fallback).

### Playwright E2E (TEST-06)
- **D-03:** **File spec baru** `tests/e2e/manage-org-label.spec.ts`, pakai pola `global.setup.ts`/`global.teardown.ts` + `helpers/` existing (konsisten dengan 9 spec lain). JANGAN campur ke spec domain lain.
- 5 scenario per TEST-06: tree load + legend visible; dropdown pre-order + inactive parent shown; cascade warning modal count akurat; label CRUD live rename → tree updated; label baru kelihatan di 2+ page integrasi (CMP + Worker form).

### Manual UAT vs Automation (success criteria #4, ORG-INTEG-03)
- **D-04:** **Maksimalkan Playwright (otomasi 4), manual tipis (1 visual).**
  - Otomasi ke Playwright: (1) HC rename label → cek 7 area, (2) Admin add Bagian baru → title dinamis, (3) "Operations" di 2 Bagian beda OK, (4) nonaktif parent → edit anak pindah parent OK.
  - Manual tipis: (5) Edit Bagian besar → warning cascade muncul dengan **count benar** (visual judgment akurasi count) + **5 regresi smoke** (tree drag-reorder, toggle active, delete unit, add unit existing flow) per ORG-INTEG-03.
  - Doc manual = `344-HUMAN-UAT.md` ringkas (checklist item yang gak diotomasi saja), eksekusi user di `http://localhost:5277`.

### TEST-03 DFS pre-order (decided post-research)
- **D-05:** Pre-order DFS logic ada di **client-side JS** (`wwwroot/js/orgTree.js` `flattenTreePreOrder` L308-317 + `buildTree` L62-75), bukan C#. Endpoint `GetOrganizationTree` (OrganizationController.cs:61-67) return flat list (Level, DisplayOrder, Name) — BUKAN pre-order.
- Pilihan user: **extract pure C# helper `BuildPreOrder`** (port logic JS) di production code → unit-test xUnit deterministik (penuhi literal "unit test xUnit") + Playwright assert urutan `<option>` DOM (TEST-06 scenario 2).
- Konsekuensi disetujui: **boleh sentuh production code** untuk 1 helper kecil C# (mis. `Helpers/OrgTreePreOrder.cs` atau static method di OrganizationController) yang me-mirror algoritma JS. Logic JS UI tetap; helper = sumber kebenaran terverifikasi.
- JANGAN refactor `GetOrganizationTree` jadi return pre-order (ditolak — scope creep, risiko regresi dropdown). Helper berdiri sendiri, di-test, tidak wajib dipanggil endpoint existing.
- Test TEST-03: assert helper terhadap tree 3 level + multi-root menghasilkan urutan pre-order yang benar (deterministik).

### Claude's Discretion
- Penamaan method test, struktur arrange/act/assert, fixture/helper internal.
- Lokasi & signature exact C# helper BuildPreOrder (selama pure + testable + tidak ubah endpoint existing).
- Mekanisme provisioning DB test disposable (xUnit fixture / `IClassFixture` / connection string test config) — selama drop per run dan tidak sentuh DB dev.
- Detail seed SQL untuk Playwright (via global.setup pattern existing).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Roadmap v21.0
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §7 — daftar 5 skenario manual UAT (sumber success criteria #4)
- `.planning/milestones/v21.0-ROADMAP.md` — Phase 344 goal + 5 success criteria
- `.planning/milestones/v21.0-REQUIREMENTS.md` — TEST-01..06 + ORG-INTEG-03 wording lengkap (baris 39, 43-48)

### Test Infra Existing (analog wajib dibaca sebelum nulis test)
- `HcPortal.Tests/OrgLabelServiceTests.cs` — 13 [Fact] existing (jangan duplikat)
- `HcPortal.Tests/OrgLabelControllerTests.cs` — 10 [Fact] existing (validation sudah ada)
- `HcPortal.Tests/OrganizationControllerTests.cs` — cek DFS + dup-name coverage di sini
- `tests/e2e/global.setup.ts`, `tests/e2e/global.teardown.ts`, `tests/e2e/helpers/` — pola Playwright + seed
- `tests/e2e/manage-assessment-filter.spec.ts` — contoh spec admin-page Playwright

### Workflow Project
- `docs/DEV_WORKFLOW.md` — verifikasi lokal (dotnet build + run + localhost:5277)
- `docs/SEED_WORKFLOW.md` — alasan DB test terpisah dipilih (hindari snapshot/restore DB dev)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HcPortal.Tests` (xUnit + EF InMemory) — project test sudah ada, tinggal tambah file/Fact.
- Playwright `global.setup.ts`/`teardown.ts` + `helpers/` — login + seed + cleanup pattern reusable.
- `OrgLabelService` / `OrgLabelController` / `OrganizationController` — SUT sudah live (340-343).

### Established Patterns
- Unit test pakai EF InMemory → cocok untuk TEST-01..04, TIDAK cocok untuk TEST-05 (migration) → D-02 pakai DB asli terpisah.
- Playwright: 1 spec per domain, seed via global.setup, RESTORE/cleanup via teardown.
- Dev creds: admin@pertamina.com + coach, pwd `123456`, DB `HcPortalDB_Dev` SQLEXPRESS.

### Integration Points
- Spec baru `manage-org-label.spec.ts` masuk daftar Playwright project existing (config tests/e2e).
- DB test disposable = connection terpisah, tidak masuk `HcPortalDB_Dev`.

</code_context>

<specifics>
## Specific Ideas

- TEST-06 scenario #5 ("label baru kelihatan di 2+ page integrasi") harus assert di CMP page + Worker form — dua page yang di-integrasi Phase 343.
- Cascade warning count (manual UAT) butuh akurasi angka — Phase 342 ORG-TREE-07 PreviewEditCascade A1 full-accuracy + 6 [Fact] preview==actual sudah ada; manual UAT cuma konfirmasi visual.

</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope Phase 344 (test + UAT v21.0).

</deferred>

---

*Phase: 344-test-uat*
*Context gathered: 2026-06-04*
