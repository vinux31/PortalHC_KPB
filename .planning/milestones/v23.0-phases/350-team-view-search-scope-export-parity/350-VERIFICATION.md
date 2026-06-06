---
phase: 350-team-view-search-scope-export-parity
verified: 2026-06-05T00:00:00Z
status: human_needed
score: 5/5
overrides_applied: 0
human_verification:
  - test: "Buka http://localhost:5277, login admin/manager, buka CMP/Records → Team View. (1) Dropdown Lingkup opsi tengah bertulisan 'Judul Kegiatan'. (2) Placeholder input bertulisan 'Cari nama/NIP, judul training, atau judul assessment...'. (3) Ketik 'ojt v14.2', set Lingkup='Keduanya' → worker pemilik assessment itu muncul (counter != 0). (4) Badge count Assessment Lulus per-worker tidak berubah vs tampilan unfiltered. (5) Aktifkan filter Kategori='OJT' → klik Export Assessment → buka .xlsx → baris assessment ter-narrow per-Kategori + archived rows (tanpa kolom Category) tidak muncul. (6) Clear Kategori → Export Assessment lagi → archived rows muncul kembali (behavior normal). (7) Semua 7 behavior di plan 03 Task 3 how-to-verify terkonfirmasi."
    expected: "Semua 7 behavior hold. Worker pemilik assessment 'OJT v14.2 Migas' tampil saat search 'ojt v14.2' scope Keduanya. Badge count tidak berubah. Export WYSIWYG."
    why_human: "Playwright e2e cmp-records-350.spec.ts telah pass 2 test (real browser), namun UAT poin (4) badge invariant on-screen, (5)-(7) export XLSX content (baris archived vs current per Category) tidak dapat diverifikasi secara programatik dari kode saja — membutuhkan visual eyeball + file .xlsx download pada localhost:5277 dengan seed aktif."
---

# Phase 350: Team View Search Scope + Export Parity — Verification Report

**Phase Goal:** HC/admin dapat menemukan worker pemilik assessment (bukan hanya Training) saat search di Team View CMP/Records, dengan dropdown "Lingkup" + placeholder jujur, dan Export menghasilkan data identik tabel on-screen (WYSIWYG). Preserve REC-06 D-07 (post-load worker-level filter, badge count utuh). NO migration.
**Verified:** 2026-06-05
**Status:** human_needed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (dari ROADMAP.md Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | User cari "ojt v14.2" di Team View scope "Keduanya" → worker pemilik assessment tampil (sebelumnya 0 worker); Training-match tidak ter-regresi | VERIFIED | `WorkerDataService.cs:413-415` — `assessmentMatch = w.AssessmentSessions.Any(a => a.Title.ToLower().Contains(searchLower))` di-OR ke path Training + Keduanya. 4/4 xUnit Fact GREEN (109/109 total). Playwright cmp-records-350.spec.ts 2 passed: `#workerCount` tidak 0 setelah search "OJT v14.2" scope Keduanya. |
| 2 | Dropdown "Lingkup" punya opsi eksplisit mencakup pencarian Assessment; placeholder jujur tidak menyesatkan | VERIFIED | `RecordsTeam.cshtml:102` — `<option value="Training">Judul Kegiatan</option>` (value="Training" frozen, label baru). `:96` — placeholder `"Cari nama/NIP, judul training, atau judul assessment..."`. Playwright asserts `#searchScope option[value="Training"]` text "Judul Kegiatan" + placeholder regex `/judul assessment/` — PASS. |
| 3 | Export Team View Assessment menghasilkan baris identik on-screen (WYSIWYG); Export Assessment tidak kosong saat search judul assessment | VERIFIED (code) / human_needed (XLSX content) | `CMPController.cs:680-689` — `if (!string.IsNullOrEmpty(category))` narrow `r.Kategori` (case-insensitive, archived Kategori==null auto-drop). `GetWorkersInSection` dipanggil dengan searchScope yang sama → `filteredIds` sudah include assessment-title workers (SF-01). Playwright assert export href contains `searchScope=Keduanya` + `search=OJT` — PASS. XLSX content (baris archived vs current per Category) memerlukan human-verify. |
| 4 | Badge count Assessment Lulus / Training per worker tidak berubah akibat search (REC-06 D-07 invariant) | VERIFIED | Badge ditetapkan di `WorkerDataService.cs:327-353` SEBELUM search block di `:403-423`. Search block tidak menyentuh `CompletedAssessments` / `TotalTrainings`. Dikonfirmasi oleh Fact `Search_DoesNotMutate_BadgeCounts_D07` GREEN: 2 passed sessions → `CompletedAssessments == 2` meski search hanya cocok 1. |
| 5 | `dotnet build` 0 error + `dotnet test` hijau termasuk test predikat baru `GetWorkersInSection` | VERIFIED | Summary Plan 03 mencatat 109/109 GREEN (105 baseline + 4 baru). `WorkerDataServiceSearchTests.cs:128-180` berisi semua 4 Fact. Build 0 error dikonfirmasi di Summary Plan 02 + 03. |

**Score:** 5/5 truths verified (1 dengan catatan human_needed untuk XLSX content)

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Services/WorkerDataService.cs` | SF-01 assessmentMatch predicate; SF-06 a.Category projection | VERIFIED | `:413-415` — `bool assessmentMatch = w.AssessmentSessions != null && w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Title) && a.Title.ToLower().Contains(searchLower))`. `:156` — `a.Category` in anon projection. `:199` — `Kategori = a.Category`. |
| `Views/CMP/RecordsTeam.cshtml` | SF-02 honest copy: placeholder + "Judul Kegiatan" label | VERIFIED | `:96` placeholder OK. `:102` `<option value="Training">Judul Kegiatan</option>`. `:107` hint span verbatim. value="Judul Kegiatan" tidak ada (grep bersih). |
| `Controllers/CMPController.cs` | SF-06 Category narrow + archived-drop + auth guard preserved | VERIFIED | `:657` `if (roleLevel >= 5) return Forbid()` ada. `:660-664` L4 section-lock ada. `:677` `category: null` unchanged. `:681-689` Category narrow dengan `string.Equals(r.Kategori, category, StringComparison.OrdinalIgnoreCase)`. |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | 4 new [Fact] tests | VERIFIED | Semua 4 method ada: `Scope_Training_FiltersByAssessmentTitle`, `Scope_Keduanya_Union_IncludesAssessment`, `Search_DoesNotMutate_BadgeCounts_D07`, `Keduanya_AssessmentTitle_ReturnsWorker_ForExport`. |
| `tests/sql/cmp350-seed.sql` | Temporary seed OJT v14.2, Completed, THROW guard, idempotent DELETE | VERIFIED | `[PENDING350] OJT v14.2 Migas`, `Status='Completed'`, `Category='OJT'`, `THROW 51350`, `DELETE FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%'`. Email reuse `rino.prasetyo@pertamina.com` sama dengan cmp346-seed.sql. |
| `tests/e2e/cmp-records-350.spec.ts` | Playwright UAT: beforeAll backup+seed, afterAll restore+Layer4, assertions SF-01/02/06 | VERIFIED | `db.execScript(path.resolve(__dirname, '../sql/cmp350-seed.sql'))` di beforeAll. Layer 1 + Layer 4 assert ada. `loginAny(page, 'manager')`. Asserts: `#searchScope option[value="Training"]` text "Judul Kegiatan", placeholder regex, `#workerCount` not '0', export href `searchScope=Keduanya` + `search=OJT`. |
| `docs/SEED_JOURNAL.md` | Entry Phase 350 temporary+local-only, status cleaned | VERIFIED | Baris `2026-06-05 | 350 | temporary + local-only | UAT Phase 350 SF-01/SF-06...` dengan status `cleaned` (Playwright e2e 2 passed, restore + Layer 4 = 0 verified). |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Views/CMP/RecordsTeam.cshtml` | `WorkerDataService.GetWorkersInSection` | `searchScope value='Training'/'Keduanya'` → server switch `:404` | VERIFIED | `value="Training"` preserved di dropdown. Controller meneruskan `searchScope` ke service. Switch `:404` — `searchScope == "Training" || searchScope == "Keduanya"`. |
| `Services/WorkerDataService.cs` | `w.AssessmentSessions (date-filtered)` | `w.AssessmentSessions.Any(a => a.Title...)` | VERIFIED | `:413-415` `assessmentMatch` menggunakan `w.AssessmentSessions` yang sudah date-filtered (`:283-293` sebelum `:314`). Tidak ada re-filter by date. |
| `Controllers/CMPController.cs (ExportRecordsTeamAssessment)` | `AllWorkersHistoryRow.Kategori` | `filtered = assessmentRows.Where(r => r.Kategori == category)` | VERIFIED | `:685-688` — `!string.IsNullOrEmpty(r.Kategori) && string.Equals(r.Kategori, category, ...)`. |
| `Services/WorkerDataService.cs (GetAllWorkersHistory current projection)` | `AllWorkersHistoryRow.Kategori` | `Kategori = a.Category` | VERIFIED | `:199` — `Kategori = a.Category`. Archived rows tidak memiliki `Category` column → tetap null → auto-drop saat Category active. |
| `tests/e2e/cmp-records-350.spec.ts` | `tests/sql/cmp350-seed.sql` | `db.execScript in beforeAll` | VERIFIED | `:48` — `await db.execScript(path.resolve(__dirname, '../sql/cmp350-seed.sql'))`. |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | `WorkerDataService.GetWorkersInSection` | `svc.GetWorkersInSection(...searchScope)` | VERIFIED | Semua 4 Fact memanggil `svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Training"/"Keduanya")`. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `CMPController.ExportRecordsTeamAssessment` | `filtered` (assessmentRows setelah narrow) | `GetWorkersInSection` → `filteredIds` → `GetAllWorkersHistory` → controller narrow | Ya — DB query + in-memory Category filter | FLOWING |
| `WorkerDataService.GetWorkersInSection` | `workerList` (post-predicate) | DB query `AssessmentSessions` loaded per worker → in-memory `assessmentMatch` | Ya — real LINQ query, `a.Title.ToLower().Contains(searchLower)` | FLOWING |
| `Views/CMP/RecordsTeam.cshtml` | Worker count display (`#workerCount`) | Server partial response dari `GetWorkersInSection` via fetch | Ya — 109/109 test GREEN + Playwright `#workerCount` != 0 | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Artifact | Result | Status |
|----------|----------|--------|--------|
| assessmentMatch predicate benar (Fact: Scope_Training_FiltersByAssessmentTitle) | WorkerDataServiceSearchTests.cs | 109/109 dotnet test GREEN | PASS |
| D-07 badge invariant (Fact: Search_DoesNotMutate_BadgeCounts_D07) | WorkerDataService.cs badge block sebelum search block | `CompletedAssessments == 2` meski search match 1 — Fact GREEN | PASS |
| SF-02 copy: dropdown option text + placeholder | RecordsTeam.cshtml | Grep confirm `Judul Kegiatan` + placeholder "judul assessment" | PASS |
| SF-06 export auth guard roleLevel >= 5 Forbid | CMPController.cs:657 | Grep confirm `if (roleLevel >= 5) return Forbid();` | PASS |
| Pitfall 3: GetAllWorkersHistory tidak di-filter assessment category in-service | WorkerDataService.cs | Tidak ada `category` Where-clause pada `currentQuery` | PASS |
| value="Training" tidak diubah menjadi "Judul Kegiatan" | RecordsTeam.cshtml | `value="Judul Kegiatan"` — grep returns no match | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| SF-01 | Plan 01 (scaffold), Plan 02 (impl) | Search Team View cocok assessment title — worker pemilik muncul | SATISFIED | `assessmentMatch` predicate di `:413-415`; 4 xUnit Fact GREEN; Playwright counter != 0 |
| SF-02 | Plan 02 | Dropdown Lingkup + placeholder jujur | SATISFIED | `<option value="Training">Judul Kegiatan</option>` + placeholder updated; Playwright assert PASS |
| SF-06 | Plan 01 (scaffold), Plan 03 (impl) | Export WYSIWYG — assessment rows narrow per Category; not empty for assessment search | SATISFIED (code) / human_needed (XLSX) | Controller narrow `:681-689`; `Kategori = a.Category` projected; `category: null` service call preserved; XLSX content human-verify pending |
| SF-03 | — | NOT in Phase 350 scope | DEFERRED to Phase 351 | REQUIREMENTS.md Traceability table: SF-03 → Phase 351 |
| SF-04 | — | NOT in Phase 350 scope | DEFERRED to Phase 351 | REQUIREMENTS.md Traceability table: SF-04 → Phase 351 |
| SF-05 | — | NOT in Phase 350 scope | DEFERRED to Phase 351 | REQUIREMENTS.md Traceability table: SF-05 → Phase 351 |
| SF-07 | — | NOT in Phase 350 scope | DEFERRED to Phase 351 | REQUIREMENTS.md Traceability table: SF-07 → Phase 351 |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| (tidak ada) | — | — | — | — |

Scan pada `Services/WorkerDataService.cs`, `Controllers/CMPController.cs`, `Views/CMP/RecordsTeam.cshtml`: tidak ditemukan TODO/FIXME/placeholder/return null yang blocker. Badge-count grep bersih dari assignment di dalam search block. `value="Judul Kegiatan"` tidak ada (Pitfall 2 clean).

---

### Human Verification Required

#### 1. Export XLSX Content + Full UAT 7-Behavior

**Test:** Jalankan `dotnet run` di localhost:5277. Apply `cmp350-seed.sql` (atau gunakan Playwright spec untuk seed). Login sebagai admin/manager. Buka CMP/Records → Team View:
1. Cek dropdown "Lingkup" tengah bertulisan **"Judul Kegiatan"**
2. Cek placeholder **"Cari nama/NIP, judul training, atau judul assessment..."**
3. Ketik **"ojt v14.2"**, set Lingkup = **"Keduanya"** → worker (pemilik "[PENDING350] OJT v14.2 Migas") **muncul** (counter != 0)
4. Verifikasi badge count Assessment Lulus per-worker **tidak berubah** vs tampilan unfiltered
5. Set filter Kategori = "OJT" → klik **Export Assessment** → buka .xlsx → baris assessment ter-narrow per-Kategori; archived rows (legacy, no-Category) **tidak muncul**
6. Clear filter Kategori → Export Assessment lagi → archived rows **muncul kembali**
7. Restore DB setelah selesai; tandai SEED_JOURNAL entry cleaned (sudah cleaned per Summary Plan 03)

**Expected:** Semua 7 behavior hold. Worker pemilik assessment "OJT v14.2 Migas" tampil. Badge utuh. Export WYSIWYG per Category.

**Why human:** Playwright e2e 2 passed hanya memvalidasi counter != 0 + export href (SF-01/02/06 plumbing). XLSX content — baris archived vs current per Category, dan badge count visual comparison — tidak dapat diverifikasi secara programatik dari grep kode.

> **Catatan:** Summary Plan 03 mencatat "user approved" dan "Playwright e2e 2 passed" dengan DB restore + Layer 4 = 0. Jika UAT tersebut sudah dianggap cukup oleh developer, human verification ini bisa di-close dengan konfirmasi eksplisit.

---

### Gaps Summary

Tidak ada gap yang memblokir goal achievement. Semua 5 success criteria roadmap terverifikasi di level kode. Human verification yang tersisa bersifat konfirmasi visual XLSX content dan full 7-behavior UAT checklist — bukan gap implementasi.

**Deferred (bukan gap Phase 350):** SF-03, SF-04, SF-05, SF-07 → Phase 351 (per REQUIREMENTS.md Traceability table).

---

_Verified: 2026-06-05_
_Verifier: Claude (gsd-verifier)_
