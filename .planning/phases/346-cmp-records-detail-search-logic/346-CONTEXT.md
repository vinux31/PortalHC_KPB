# Phase 346: cmp-records-detail-search-logic - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Halaman **CMP/Records** (tab My Records + Team View + drill-down Worker Detail) ditambah fasilitas detail/hasil + search adaptif + tutup bug logic. Cakupan = REC-01..REC-09 (REC-10 DROP). **No migration, no schema change.** Sequential setelah Phase 345, sebelum Phase 347.

Yang dikerjakan:
1. **My Records** — kolom "Aksi" (tombol `Lihat Hasil`→Results utk assessment, `Detail`→modal training) + row tetap clickable (REC-01/02).
2. **Worker Detail** — tombol `Lihat Hasil`→Results di row assessment + modal training tambah Kategori/SubKategori (REC-03/05).
3. **Authz 🔐** — `Results`+`Certificate`+`CertificatePdf` dilonggarkan: owner ∥ roleLevel≤3 ∥ (L4 section-scoped). Sekalian fix AUTHZ-01 tombol Sertifikat dead L3/L4 (REC-04).
4. **Team View** — search box + selektor scope Nama/Training/Keduanya server-side, export ikut filter (REC-06).
5. **Logic** — include PendingGrading di My Records + export (REC-07); validasi date-range (REC-08); perjelas badge "Assessment" (REC-09).

Phase ini menambah fitur detail/search + tutup bug — bukan redesign halaman. Polish i18n/a11y + DRY CSS = Phase 347 (terpisah, jangan dikerjakan di sini).
</domain>

<decisions>
## Implementation Decisions

### Keputusan terkunci dari spec (user APPROVED 2026-06-04)
Spec `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` — JANGAN dilitigasi ulang:
- **D-01 Privasi:** Atasan L1–L4 lihat hasil assessment **penuh** (skor + review per-soal) anggota tim. L4 dibatasi section sendiri (`assessment.User.Section == user.Section`, guard Section non-null). Coach/Coachee (L5/L6) tetap hanya owner.
- **D-02 Struktur:** Split 2 phase — 346 (fitur+logic) & 347 (polish). WAJIB sequential 345→346→347 (edit baris berdekatan di `Records.cshtml`+`RecordsWorkerDetail.cshtml`).
- **D-03 Discoverability:** Kolom "Aksi" dgn tombol eksplisit **DAN** row tetap clickable.
- **D-04 Detail training:** Modal (bukan page baru) — ~13 field flat + 1 PDF, tak ada konten bertingkat. (Assessment butuh page `Results` krn ada review per-soal.)
- **D-05 Team search:** 1 input + selektor scope (Nama/Training/Keduanya, default Keduanya), server-side.
- **D-06 Authz:** Mirror aturan `RecordsWorkerDetail` (L1–3 full, L4 section-scoped), owner/Admin/HC dipertahankan via roleLevel≤3.

### Keputusan diskusi 2026-06-04 (gray area sisa)
- **D-07 (REC-06) Strategi query search Training = POST-LOAD filter.** Filter worker-list **setelah** `TrainingRecords` di-load per-user (ikut pola category-narrow `GetWorkersInSection` L370-378). `searchScope=="Nama"` boleh pakai SQL filter existing (FullName/NIP L255-262); `searchScope=="Training"`/`"Keduanya"` filter in-memory. **Catatan union "Keduanya":** name-filter SQL TAK BISA pre-narrow (akan buang training-only match) → utk "Keduanya" load worker section dulu lalu filter in-memory `(Nama/NIP match OR TrainingRecords.Any(Judul.Contains))`. Alasan pilih post-load: data per-section realistis kecil, konsisten pola, hindari kompleksitas subquery. *(User: reko.)*
- **D-08 (REC-09) Perjelas badge = RELABEL header.** Ganti teks header kolom Team View (`RecordsTeam.cshtml:137`) "Assessment" → **"Assessment Lulus"** (view-only string, TAK sentuh field `CompletedAssessments`/property). Eksplisit tanpa hover, jelas di mobile. Tooltip opsional drop. *(User: reko.)*
- **D-09 (REC-04) Cek role-string lama = HAPUS.** Setelah authz jadi `owner ∥ roleLevel≤3 ∥ (L4 section-scoped)`, hapus `roles.Contains("Admin") || roles.Contains("HC")` lama — `roleLevel≤3` sudah cover Admin(1)+HC(2), redundan. Authz single-source lebih bersih + mudah di-test. *(User: reko.)*
- **D-10 (REC-04 test) Kedalaman = MATRIX PENUH 8 kasus.** xUnit authz: owner / Admin / HC / L3 / L4-same-section / L4-other-section / L5 / L6 × (`Results` + `Certificate` + `CertificatePdf`) → Forbid vs OK. Plus guard Section-null L4 (Forbid). Security-sensitive (melonggarkan akses hasil tim) → regression-proof penuh. *(User: reko.)*

### REC yang sudah locked di spec (tinggal eksekusi, bukan gray area)
- **REC-08 date-range:** Pilih **warning** (bukan auto-swap) — hint "Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang." (extend `updateDateHint`).
- **REC-02 modal field:** Port `trainingDetailModal` + tambah Kategori/SubKategori/Status/ValidUntil/CertificateType. Tampilkan field yg ada (ValidUntil **dan** CertificateType bila tersedia, bukan pilih salah satu).
- **REC-07:** WHERE include `Status==PendingGrading` (konstanta, BUKAN literal). Label sudah benar (lihat code_context).

### Claude's Discretion (planner refine)
- Plan split / wave structure — ROADMAP belum lock (beda dari 345 yg pre-locked). Reko isolasi REC-04 (security) jadi plan sendiri agar review fokus. Planner putuskan.
- Mekanisme akses konstanta `PendingGrading` di Razor vs C# (ikut pola 345 D-02).
- Pre-filter subquery REC-06 sbg optimasi opsional bila profiling tunjukkan lambat (default post-load).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements & Roadmap
- `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` — **spec utama** Phase 346/347. §"Phase 346" (baris 38-131): REC-01..10 detail + pitfall per-REQ + D-01..06. §"Testing Strategy" (185-194). §"Rejected findings" (169-170, 14 finding jangan dikerjakan).
- `.planning/REQUIREMENTS.md` — REC-01..09 (baris 40-48), REC-10 DROP (49), coverage matrix (121-127).
- `.planning/ROADMAP.md` — §Phase 346 (baris 755-772): 7 SC, files affected, pitfalls (colspan 6→7, konstanta PendingGrading, Include User, sequential).

### Dependency (Phase 345 — predecessor, baru ship)
- `.planning/phases/345-assessment-pending-grade-display-fix/345-CONTEXT.md` — label "Menunggu Penilaian" amber + konstanta `AssessmentConstants.AssessmentStatus.PendingGrading`. REC-07 depends label ini (sudah ada di service, lihat code_context).

### Kode wajib (line ref TERVERIFIKASI pasca-345, 2026-06-04)
- `Controllers/CMPController.cs` — `Certificate` L1815 (isAuthorized L1830, Forbid L1834), `CertificatePdf` L1926 (isAuthorized L1938, Forbid L1941), `Results` L2169 (isAuthorized L2181, Forbid L2184, sudah `.Include(a=>a.User)` L2172). Helper `GetCurrentUserRoleLevelAsync` L2485 → `(user, roleLevel)`. Pola authz roleLevel ada di `Records` action L502-506 / Export L661/L709/L763. Excel `ExportRecords` L694.
- `Services/WorkerDataService.cs` — `GetUnifiedRecords` L28 (label switch L51-56), `GetAllWorkersHistory` L92, `GetWorkersInSection` L242 (signature: section, unitFilter, category, search, statusFilter, dateFrom, dateTo, subCategory — BELUM ada searchScope; category-narrow pola L370-378; name-filter L255-262).
- `Models/AssessmentConstants.cs:18` — `AssessmentStatus.PendingGrading = "Menunggu Penilaian"` (WAJIB konstanta).
- `Models/WorkerTrainingStatus.cs` (59 baris) — `CompletedAssessments` (REC-09 JANGAN rename); `UnifiedTrainingRecord` punya semua field modal REC-02 (Penyelenggara/Kota/Tgl/NomorSertifikat/Kategori/SubKategori/Status/ValidUntil/CertificateType/SertifikatUrl).
- Views: `Records.cshtml` (458), `RecordsWorkerDetail.cshtml` (450), `RecordsTeam.cshtml` (494), `_RecordsTeamBody.cshtml` (42).

**⚠ STALE-LINE WARNING:** Spec ditulis SEBELUM Phase 345 ship. 345 mengedit `Records.cshtml` + `RecordsWorkerDetail.cshtml` + `GetUnifiedRecords`. Anchor level-method (1815/1926/2169/L28/L242) terverifikasi utuh, TAPI line-ref INTERNAL view di spec (thead L150-157, empty-state L227, JS L381, modal L288-307, badge L226-231) mungkin geser ±. **Planner/executor WAJIB re-grep sebelum edit — jangan percaya line number spec buta.**
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetCurrentUserRoleLevelAsync` (CMPController L2485) → `(user, roleLevel)`. REC-04: ketiga action (`Results`/`Certificate`/`CertificatePdf`) saat ini pakai `GetUserAsync`+`GetRolesAsync` string-check — ganti panggil helper ini utk resolve roleLevel (pola sama Records L481, Export L654/706).
- Pola authz roleLevel+section-scope sudah ada di `RecordsWorkerDetail` action (L549-553 per spec) & `Records` L502-506 → REC-04 tinggal mirror.
- `trainingDetailModal` di `RecordsWorkerDetail.cshtml` (~L288-307) → port ke `Records.cshtml` (REC-02); pola data-* attr + JS `show.bs.modal` handler reusable.
- `UnifiedTrainingRecord` (Models) sudah ekspos semua field modal REC-02/05 → **no controller change** utk data modal.

### Established Patterns
- **REC-07 label SUDAH BENAR:** Phase 345 menambah switch `null => PendingGrading` di `GetUnifiedRecords` L51-56. Jadi REC-07 = HANYA extend WHERE filter (`Status=="Completed" || Status==PendingGrading`) di `GetUnifiedRecords` (~L31-34) + `GetAllWorkersHistory` (~L134-136). Sesi pending punya `IsPassed==null` → label otomatis benar. Verifikasi tampil "Menunggu Penilaian".
- Ketiga REC-04 action saat ini: `owner || roles.Contains("Admin") || roles.Contains("HC")` via `GetRolesAsync` (Results L2181 verified). `Certificate`+`CertificatePdf` BELUM `.Include(a=>a.User)` → WAJIB tambah sebelum cek `assessment.User.Section` (kalau tidak, `assessment.User` null → L4 selalu Forbid).
- Team filter JS pola: `getFilterState`/`doFetch`/`updateExportLinks`/`saveFilterState`/`restoreFilterState`/`resetTeamFilters` + debounce (REC-06 wire semua); `updateDateHint` (REC-08 extend).

### Integration Points
- `WorkerDataService.GetUnifiedRecords` → feed `Records.cshtml` (My Records) + (via Worker Detail path) `RecordsWorkerDetail.cshtml`.
- `WorkerDataService.GetWorkersInSection` → `RecordsTeamPartial` (CMPController L753) + Export `ExportRecordsTeamAssessment`/`ExportRecordsTeamTraining` (L652/L704). REC-06: tambah param `searchScope` ke ketiganya (optional default null = backward-compat caller lain).
- `Results`/`Certificate`/`CertificatePdf` (CMPController) — entry tombol REC-01/03; authz REC-04.
</code_context>

<specifics>
## Specific Ideas

- REC-01 PITFALL colspan: empty-state `<td colspan="6">` + JS inject empty-state harus 6→**7** (kolom Aksi baru). Re-grep lokasi exact di `Records.cshtml` (spec sebut L227+L381, mungkin geser pasca-345).
- REC-04 efek samping diinginkan: fix AUTHZ-01 (tombol Sertifikat di Worker Detail dead utk L3/L4) ikut beres.
- REC-06 semantik: search **menyaring worker mana yang muncul**, badge count assessment/training per-worker tetap utuh (bukan per-training row). Dokumentasikan di tooltip/hint.
- REC-09: relabel header "Assessment Lulus" cukup; field `CompletedAssessments` = `IsPassed==true count` (komentar `WorkerTrainingStatus.cs` sudah benar, jangan ubah logika).
- Test: ikut SEED_WORKFLOW Phase 345 (snapshot DB lokal sebelum seed sesi PendingGrading, restore sesudah, journal `cleaned`). Dev creds: admin+coach pwd `123456`, DB `HcPortalDB_Dev` (SQLEXPRESS).
</specifics>

<deferred>
## Deferred Ideas

- **REC-10** (Worker Detail category filter server-side) — **DROP** (over-eng, data 1 pekerja tak paginated, impact LOW). Spec §165 rekomendasi drop.
- **Polish i18n/a11y + DRY CSS** (POL-01..10) — Phase 347 (sequential, file sama). Termasuk badge `Passed/Failed`→`Lulus/Tidak Lulus`, `Score`→`Nilai`, modal aria, ekstrak CSS `records.css`, mobile grid. JANGAN dikerjakan di 346.
- **`AssessmentMonitoringDetail.cshtml:1409`** JS SignalR result-cell — di luar halaman Records (spec Out of Scope).
- **ManageAssessment + Monitoring** (MAM/MAP) — Phase 348/349. M4 (Tab3 History PendingGrading) dicakup REC-07/346 → tambah Tab3 History ke UAT 346 (MAP-20 depends REC-07).
- Pre-filter subquery REC-06 (optimasi) — opsional, hanya bila profiling lambat.

### Reviewed Todos (not folded)
Tidak ada — `todo match-phase 346` = 0 match.
</deferred>

---

*Phase: 346-cmp-records-detail-search-logic*
*Context gathered: 2026-06-04*
