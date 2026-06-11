# Phase 369: Sync H1 Search-Drop Fix main → ITHandoff - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Membawa fix H1 (`14e7adc5` di main) ke branch ITHandoff via cherry-pick: `WorkerDataService.GetWorkersInSection` treat `searchScope` null/kosong sebagai "Nama" sehingga SQL name pre-narrow jalan untuk caller lama (`ManageAssessmentTab_Training` Tab Input Records) — search nama tidak lagi diabaikan diam-diam. Termasuk test regresi `Scope_Null_WithSearch_FiltersByName_H1`. HANYA 1 commit ini — bukan full merge main.

</domain>

<decisions>
## Implementation Decisions

### Metode sync
- **D-01:** Cherry-pick `14e7adc5` SAJA (2 file: `Services/WorkerDataService.cs` +6/-2, `HcPortal.Tests/WorkerDataServiceSearchTests.cs` +13). BUKAN full merge main→ITHandoff — merge penuh (13 commit, termasuk self-heal seed F1/F2 + 1 konflik docs `DB_HANDOFF_IT_2026-06-06.html`) tetap event terpisah terencana sebelum handoff IT. Git dedup commit ter-pick saat merge nanti.

### Jejak commit
- **D-02:** `git cherry-pick -x 14e7adc5` — pesan asli + baris "(cherry picked from commit ...)" untuk audit trail; memudahkan deteksi duplikat saat full merge.

### Verifikasi
- **D-03:** Suite penuh `dotnet test` hijau + UAT live 1 skenario Playwright @5277: login admin → `/Admin/ManageAssessment` Tab Input Records → search nama/NIP → list TERFILTER (bukan balikin semua row). Konvensi CLAUDE.md: `Authentication__UseActiveDirectory=false dotnet run`.

### Claude's Discretion
- Penanganan kalau cherry-pick ternyata konflik (sudah diverifikasi clean via merge-tree 2026-06-11, tapi kalau ITHandoff berubah): resolve manual dengan hasil akhir = guard identik main.
- Urutan langkah verifikasi (build/test/UAT).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Fix asal
- Commit `14e7adc5` di branch `main` — diff lengkap = spesifikasi perubahan (lihat `git show 14e7adc5`).
- `.planning/REQUIREMENTS.md` §v26.0 — URG-01 definisi.
- `.planning/ROADMAP.md` §Phase 369 — SC 1-3.

### Konteks regresi
- Regresi asal: REC-06 D-07 scope-gating (v23.0 Phase 350, locked decision di STATE.md Accumulated Context: "search assessment-title filter di level worker (post-load)").

</canonical_refs>

<code_context>
## Existing Code Insights

### Target perubahan
- `Services/WorkerDataService.cs:257-266` — kondisi saat ini `if (searchScope == "Nama" && ...)` = persis pre-image commit; cherry-pick apply bersih (verified merge-tree 2026-06-11).
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — file test sudah ada di ITHandoff (Phase 346); fix menambah 1 [Fact] `Scope_Null_WithSearch_FiltersByName_H1`.

### Caller terdampak
- `Controllers/AssessmentAdminController.cs:279` (`ManageAssessmentTab_Training`) — panggil `GetWorkersInSection(section, unit, category, search, statusFilter)` TANPA searchScope → saat ini search di-drop diam-diam di ITHandoff.
- `Controllers/CMPController.cs:676,737` — pass searchScope dari form; ikut benefit saat scope kosong.

### Zero konflik phase lain
- File target TIDAK disentuh phase 363-368 (verified 2026-06-11). Aman jalan paralel dengan eksekusi 363.

</code_context>

<specifics>
## Specific Ideas

Hasil akhir guard HARUS identik main: `if ((string.IsNullOrEmpty(searchScope) || searchScope == "Nama") && !string.IsNullOrEmpty(search))` + komentar H1 di atasnya.

</specifics>

<deferred>
## Deferred Ideas

- **Full merge main→ITHandoff** (13 commit: self-heal seed F1/F2, docs audit, dll + resolve 1 konflik docs) — event terencana pre-handoff IT, BUKAN scope 369. Sudah tercatat di STATE.md "Push pending IT" + memory.

</deferred>

---

*Phase: 369-sync-h1-search-drop-fix-main-ithandoff*
*Context gathered: 2026-06-11*
