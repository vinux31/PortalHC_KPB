---
phase: 346-cmp-records-detail-search-logic
verified: 2026-06-04T00:00:00Z
status: passed
score: 7/7 success criteria verified
overrides_applied: 0
---

# Phase 346: cmp-records-detail-search-logic — Verification Report

**Phase Goal:** Pekerja & atasan bisa lihat detail assessment (hasil) + training (modal), Worker Detail buka hasil assessment, Team View search adaptif (Nama/Training/Keduanya), assessment PendingGrading tak hilang.
**Verified:** 2026-06-04
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (dari ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | My Records kolom "Aksi": Assessment→Lihat Hasil→/CMP/Results, Training→Detail→modal (11 field + PDF); row tetap clickable | VERIFIED | `<th>Aksi</th>` di thead Records.cshtml; tombol `Lihat Hasil` wired via `Url.Action("Results","CMP",...)`; `data-bs-target="#trainingDetailModal"` pada tombol Detail; `colspan="7"` di 2 lokasi (Razor L252 + JS L438); row `data-href` tidak disentuh |
| 2 | Worker Detail row Assessment punya tombol Lihat Hasil→/CMP/Results; modal training tambah Kategori/SubKategori | VERIFIED | `asp-action="Results"` + `bi-bar-chart-line` + teks "Lihat Hasil" di RecordsWorkerDetail.cshtml; tombol berada di luar blok `if (item.GenerateCertificate)` (hanya gate RecordType+AssessmentSessionId); `id="mdKategori"` + `id="mdSubKategori"` di modal `<dl>` + di data-* attrs tombol Detail + di JS handler |
| 3 | Results/Certificate/CertificatePdf authz: owner OR L<=3 full OR L4 section-scoped; guard Section non-null; L3/L4 same-section PASS; L4 beda section + L5/L6 non-owner Forbid | VERIFIED | `public static bool IsResultsAuthorized(...)` ada di CMPController.cs; pakai `roleLevel is >= 1 and <= 3` (guard roleLevel 0); `!string.IsNullOrEmpty(currentUserSection)` (guard null/empty); dipanggil TEPAT 3x (Certificate L1830, CertificatePdf L1936, Results L2176); `userRoles.Contains("Admin")` dan `userRoles.Contains("HC")` tidak ada di region L1815-2184 |
| 4 | Team View search box + selektor scope (Nama/Training/Keduanya, server-side); training-search via TrainingRecords.Judul; export links ikut filter | VERIFIED | IWorkerDataService.cs signature berakhir `string? searchScope = null`; WorkerDataService.cs: blok SQL dibungkus `searchScope == "Nama"`; blok post-load `searchScope == "Training" \|\| searchScope == "Keduanya"` membaca `t.Judul`; CMPController RecordsTeamPartial + ExportRecordsTeamAssessment + ExportRecordsTeamTraining ketiga-nya punya `string? searchScope = null` + forward ke service; RecordsTeam.cshtml: `id="teamSearch"` + `id="searchScope"` + Keduanya default; searchScope diambil di getFilterState + set di doFetch + updateExportLinks + saved/restored + reset ke Keduanya |
| 5 | Assessment esai PendingGrading tampil di My Records + export team dengan label "Menunggu Penilaian" (WHERE pakai AssessmentConstants.AssessmentStatus.PendingGrading) | VERIFIED | WorkerDataService.cs: `AssessmentConstants.AssessmentStatus.PendingGrading` muncul 2x di WHERE (GetUnifiedRecords L33 + GetAllWorkersHistory L136); label switch `null => AssessmentConstants.AssessmentStatus.PendingGrading` L56 tetap utuh dari Phase 345; UAT scope-extension: AssessmentAdminController.UserAssessmentHistory WHERE juga di-extend (T-346-UAT-01 fix, user-approved) |
| 6 | Date range dateFrom>dateTo → warning; badge "Assessment" → "Assessment Lulus" (BUKAN rename field CompletedAssessments) | VERIFIED | RecordsTeam.cshtml: `updateDateHint` berisi cabang `state.dateFrom > state.dateTo` dengan teks literal "Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang."; header baris 155 `<th>Assessment Lulus</th>`; `_RecordsTeamBody.cshtml` masih `@worker.CompletedAssessments` (field tidak di-rename) |
| 7 | dotnet build 0 error + dotnet test hijau (authz matrix + search scope + PendingGrading tests) + Playwright UAT PASS | VERIFIED | `ResultsAuthorizationTests.cs` ada dengan 11 [InlineData] memanggil `CMPController.IsResultsAuthorized` (owner/Admin/HC/L3/L4-same/L4-other/L4-null/L4-empty/L5/L6/roleLevel-0); `WorkerDataServiceSearchTests.cs` ada dengan InMemory tests (scope Nama/Training/Keduanya/Null + include-PendingGrading + exclude-other-status + konstanta `AssessmentConstants.AssessmentStatus.PendingGrading`); Summary dokumen 76/76 `dotnet test` PASS; `cmp-records-346.spec.ts` ada dengan coverage semua surface (My Records Aksi/modal, Worker Detail Lihat Hasil, L3/L4-section/L4-cross Forbid, Team search 3 scope + export param + date-warning, PendingGrading, Tab3 History); 9/9 REC browser-verified via Playwright MCP |

**Score:** 7/7 success criteria verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/CMP/Records.cshtml` | Kolom Aksi + tombol Lihat Hasil/Detail + trainingDetailModal + JS handler | VERIFIED | th "Aksi", tombol Lihat Hasil wired ke /CMP/Results, data-bs-target="#trainingDetailModal", modal 11 field + pdfw-wrap, JS addEventListener show.bs.modal, colspan="7" 2x |
| `Views/CMP/RecordsWorkerDetail.cshtml` | Tombol Lihat Hasil di action column + Kategori/SubKategori di trainingDetailModal | VERIFIED | asp-action="Results" un-gated dari GenerateCertificate, id="mdKategori"/"mdSubKategori" di modal + data-* attrs + JS handler |
| `Controllers/CMPController.cs` | Static helper IsResultsAuthorized + 3 action memakai helper | VERIFIED | `public static bool IsResultsAuthorized(` ada; dipanggil 3x; roleLevel is >= 1 and <= 3; IsNullOrEmpty guard; cek lama dihapus |
| `Services/IWorkerDataService.cs` | Interface searchScope param | VERIFIED | Signature berakhir `string? searchScope = null` |
| `Services/WorkerDataService.cs` | GetWorkersInSection + searchScope 3-cabang; GetUnifiedRecords + GetAllWorkersHistory WHERE include PendingGrading via konstanta | VERIFIED | searchScope param, SQL guard "Nama", post-load "Training"/"Keduanya", 2x AssessmentConstants.AssessmentStatus.PendingGrading di WHERE |
| `Views/CMP/RecordsTeam.cshtml` | input teamSearch + select searchScope + wiring 5 fungsi JS + warning date + header "Assessment Lulus" | VERIFIED | id="teamSearch"/"searchScope", Keduanya default, wired di getFilterState/doFetch/updateExportLinks/save/restore/reset, warning "Tanggal Awal...", header "Assessment Lulus" |
| `HcPortal.Tests/ResultsAuthorizationTests.cs` | Matrix authz pure-static IsResultsAuthorized (11 kasus) | VERIFIED | 11 InlineData: owner/Admin/HC/L3/L4-same/L4-other/L4-null/L4-empty/L5/L6/roleLevel-0 |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | InMemory tests searchScope + include-pending | VERIFIED | scope Nama/Training/Keduanya/Null + GetUnifiedRecords include/exclude, konstanta bukan literal |
| `tests/e2e/cmp-records-346.spec.ts` | Playwright UAT semua surface Phase 346 | VERIFIED | 6 test, coverage My Records/Worker Detail authz/Team search/date-warning/PendingGrading/Tab3 History; "Lihat Hasil" ada |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| Records.cshtml tombol Lihat Hasil | /CMP/Results | `Url.Action("Results","CMP", new { id = item.AssessmentSessionId.Value })` | WIRED | Pattern `Url.Action("Results"` ditemukan di sel Aksi Records.cshtml |
| Records.cshtml tombol Detail | #trainingDetailModal | `data-bs-target="#trainingDetailModal"` + show.bs.modal handler | WIRED | Attribute + addEventListener confirmed |
| RecordsWorkerDetail.cshtml tombol Lihat Hasil | /CMP/Results | `asp-action="Results"` | WIRED | `asp-action="Results"` + `asp-route-id="@item.AssessmentSessionId.Value"` confirmed |
| Worker Detail modal data-kategori/data-subcategory | #trainingDetailModal mdKategori/mdSubKategori | JS show.bs.modal handler | WIRED | `data-kategori` attr + `document.getElementById('mdKategori').textContent = btn.dataset.kategori` confirmed |
| Results/Certificate/CertificatePdf actions | CMPController.IsResultsAuthorized | panggil static helper | WIRED | `IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section)` muncul 3x confirmed |
| RecordsTeam.cshtml doFetch/updateExportLinks | RecordsTeamPartial + Export endpoints | URLSearchParams set searchScope | WIRED | `params.set('searchScope', s.searchScope)` di doFetch L418 + updateExportLinks L365 confirmed |
| CMPController RecordsTeamPartial/Export | WorkerDataService.GetWorkersInSection | forward search + searchScope arg | WIRED | Ketiga action forward `searchScope` ke `GetWorkersInSection(...)` confirmed |
| GetUnifiedRecords / GetAllWorkersHistory WHERE | AssessmentSessions PendingGrading rows | `\|\| a.Status == AssessmentConstants.AssessmentStatus.PendingGrading` | WIRED | Konstanta ditemukan 2x di WHERE (bukan literal) |
| RecordsTeam.cshtml updateDateHint | dateFilterHint banner | inverted-range branch dateFrom > dateTo | WIRED | `state.dateFrom > state.dateTo` → textContent "Tanggal Awal lebih besar..." confirmed |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Records.cshtml modal training | `dataset.*` dari data-* attrs tombol Detail | `UnifiedTrainingRecord` fields (Kategori, SubKategori, dll.) di model view | Ya — `UnifiedTrainingRecord` memiliki semua field (Kategori, SubKategori, CertificateType, ValidUntil, Penyelenggara, Judul, dll.) confirmed di `Models/UnifiedTrainingRecord.cs` | FLOWING |
| RecordsTeam.cshtml worker list | `workerList` dari RecordsTeamPartial | `WorkerDataService.GetWorkersInSection` → EF Core query + TrainingRecords join | Ya — SQL query + post-load filter real, bukan static | FLOWING |
| WorkerDataService.GetWorkersInSection searchScope | `workerList` setelah filter | `TrainingRecord.Judul` field (string?, confirmed di `Models/TrainingRecord.cs`) | Ya — `t.Judul.ToLower().Contains(searchLower)` membaca field DB nyata | FLOWING |
| WorkerDataService.GetUnifiedRecords | `assessments` list | EF Core WHERE `AssessmentSessions` status filter | Ya — query ke DB real, konstanta `AssessmentConstants.AssessmentStatus.PendingGrading == "Menunggu Penilaian"` di Models/AssessmentConstants.cs | FLOWING |

---

### Behavioral Spot-Checks

Step 7b: SKIPPED — server tidak dijalankan selama verifikasi. UAT sudah dieksekusi oleh Claude via Playwright MCP (9/9 REC PASS, browser-verified, didokumentasikan di 346-06-SUMMARY.md) sehingga behavioral verification sudah terpenuhi secara live.

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| REC-01 | 346-01 | My Records kolom Aksi + tombol Lihat Hasil + fix colspan | SATISFIED | thead "Aksi", Url.Action("Results"), colspan="7" 2x di Records.cshtml |
| REC-02 | 346-01 | My Records modal detail training (11 field + PDF, no controller change) | SATISFIED | id="trainingDetailModal" + 11 id="md*" elemen + mdPdfWrap + JS handler confirmed |
| REC-03 | 346-02 | Worker Detail tombol Lihat Hasil→/CMP/Results (un-gated GenerateCertificate) | SATISFIED | asp-action="Results" un-gated di RecordsWorkerDetail.cshtml |
| REC-04 | 346-03, 346-06 | Authz extend Results/Certificate/CertificatePdf via IsResultsAuthorized | SATISFIED | Helper exists, 3x called, old role-string removed, 11-case test matrix |
| REC-05 | 346-02 | Worker Detail modal training tambah Kategori/SubKategori | SATISFIED | id="mdKategori"/"mdSubKategori" di modal + data-* + JS handler |
| REC-06 | 346-04, 346-06 | Team View search + selektor scope (Nama/Training/Keduanya) server-side + export filter | SATISFIED | Full chain: IWorkerDataService → WorkerDataService → CMPController 3 actions → RecordsTeam.cshtml 5 JS fungsi wired |
| REC-07 | 346-05, 346-06 | Include PendingGrading di GetUnifiedRecords + GetAllWorkersHistory (konstanta) | SATISFIED | 2x `AssessmentConstants.AssessmentStatus.PendingGrading` di WHERE; literal "PendingGrading" tidak ada; UAT T-346-UAT-01 fix ikut extend AssessmentAdminController |
| REC-08 | 346-05 | Team View date range inverted warning | SATISFIED | updateDateHint cabang `state.dateFrom > state.dateTo` + teks "Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang." |
| REC-09 | 346-05 | Header "Assessment Lulus" (field tidak di-rename) | SATISFIED | `<th>Assessment Lulus</th>` di RecordsTeam.cshtml; `@worker.CompletedAssessments` tetap di _RecordsTeamBody.cshtml |
| REC-10 | — | DROPPED (over-eng, Worker Detail category filter server-side) | N/A — tidak diimplementasikan per keputusan terkunci | Benar tidak ada implementasi REC-10 |

**Orphaned requirements check:** REQUIREMENTS.md memetakan REC-01..09 ke Phase 346; REC-10 eksplisit DROP. Tidak ada requirement orphaned.

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| Views/CMP/Records.cshtml:58 | `placeholder="Cari berdasarkan..."` (HTML input placeholder attr) | Info | HTML form placeholder biasa — bukan code stub, tidak perlu aksi |

Tidak ditemukan anti-pattern blocker. Tidak ada `TODO/FIXME` di file yang dimodifikasi. Tidak ada stub implementasi (return null / return {}) di path kode baru. Literal `"PendingGrading"` tidak ada di WHERE clauses (pakai konstanta benar). `userRoles.Contains("Admin")` tidak ada di region authz (sudah dihapus sesuai D-09).

---

### Human Verification Required

Tidak ada. UAT browser-verify (Task 3 Plan 06) sudah dieksekusi oleh Claude via Playwright MCP atas permintaan user. 9/9 REC verified secara live di `localhost:5277`:

- REC-01: My Records kolom "Aksi" (7 kolom) + "Lihat Hasil"→/CMP/Results/157 PASS
- REC-02: #trainingDetailModal 11 field + PDF toggle PASS
- REC-03: Worker Detail tombol "Lihat Hasil" un-gated; baris cert = Lihat Hasil + Sertifikat (AUTHZ-01 fixed) PASS
- REC-04: L1 OK; L4 same-section (GAST→rino) OK; L4 cross-section (GAST→Section-NULL) → Akses Ditolak PASS
- REC-05: Modal Kategori "Mandatory HSSE" + Sub Kategori "Gas Tester" PASS
- REC-06: Search "rino"/Nama→1 worker; "k3"/Training→2 worker; export href berisi `?search=&searchScope=` PASS
- REC-07: Sesi [PENDING346] muncul "Menunggu Penilaian" di Worker Detail PASS
- REC-08: dateFrom>dateTo → warning "Tanggal Awal lebih besar..." PASS
- REC-09: Header Team View "Assessment Lulus" PASS

SEED_WORKFLOW dipatuhi: 2x snapshot→seed→verify→RESTORE, journal entries status `cleaned`.

---

### Gaps Summary

Tidak ada gap. Semua 7 success criteria ROADMAP terverifikasi di codebase aktual. Semua 9 requirement (REC-01..09) terpenuhi. REC-10 DROP benar tidak diimplementasikan. Test suite 76/76 pass. Human UAT 9/9 PASS via Playwright MCP.

---

_Verified: 2026-06-04_
_Verifier: Claude (gsd-verifier)_
