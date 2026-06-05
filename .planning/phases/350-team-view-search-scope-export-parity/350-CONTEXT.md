# Phase 350: Team View Server-Side Search Scope + Export Parity - Context

**Gathered:** 2026-06-05
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/admin dapat menemukan worker pemilik **assessment** (bukan hanya Training) saat search di
**Team View** CMP/Records, dengan dropdown "Lingkup" + placeholder yang **jujur** mencerminkan apa
yang dicari, dan tombol **Export** menghasilkan data **identik dengan tabel on-screen** (WYSIWYG).

Cakupan = surface **Team View** saja (`RecordsTeam.cshtml` + `RecordsTeamPartial` +
`ExportRecordsTeam*`) di-back oleh `WorkerDataService.GetWorkersInSection` / `GetAllWorkersHistory`.
REQ: **SF-01 (HIGH), SF-02 (MED), SF-06 (MED)**.

**Invariant terkunci (warisan REC-06 D-07 / Phase 346):** predikat search baru WAJIB memfilter
*worker mana yang muncul* di **level worker (post-load in-memory)** — TIDAK boleh mengubah angka
badge/count Assessment-Lulus / Training per worker. **No migration** (search/filter predicate + view
+ export saja).

Worker Detail + My Records + cross-surface consistency (SF-03/04/05/07) = **Phase 351**, di luar
boundary ini.
</domain>

<decisions>
## Implementation Decisions

### Dropdown "Lingkup" + scope predikat search (SF-01 + SF-02)
- **D-01:** Pakai **3 opsi relabel jujur** (BUKAN tambah opsi "Assessment" baru, BUKAN minimal-keep-label).
  Struktur dropdown `#searchScope`:
  - `Nama` (value `"Nama"`) — Nama/NIP, via SQL pre-narrow (`WorkerDataService.cs:257-264`), **tidak diubah**.
  - `Judul Kegiatan` (value internal **tetap `"Training"`**) — kini match `TrainingRecords.Judul` **OR** `AssessmentSessions.Title`.
  - `Keduanya` (value `"Keduanya"`, **default**) — Nama/NIP **∪** Judul Kegiatan (training+assessment).
  - **Alasan value `"Training"` dipertahankan:** hindari ripple di server switch (`:402`) + `WorkerDataServiceSearchTests.cs` + sessionStorage `cmp-records-team-filter` (value lama tetap valid). Hanya **label tampil** yang berubah jadi "Judul Kegiatan".
- **D-02:** Predikat assessment-title ditambah di blok **post-load** `WorkerDataService.cs:402-417`
  (mirror pola union Category yang sudah ada di `:373-381`):
  - Tambah `assessmentMatch = w.AssessmentSessions != null && w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Title) && a.Title.ToLower().Contains(searchLower))`.
  - `searchScope == "Training"` → `trainingMatch || assessmentMatch`.
  - `searchScope == "Keduanya"` → `nameMatch || trainingMatch || assessmentMatch`.
  - **Otomatis date-aware:** `w.AssessmentSessions` per worker sudah di-date-filter di `sessionsByUser` (`:283-293`), jadi search title hanya menyentuh sesi dalam rentang tanggal aktif — tanpa kode tambahan.
  - **D-07 preserved:** filter tetap di level worker (post-load), badge/count per-worker tidak disentuh.

### Micro-copy Bahasa Indonesia (SF-02)
- **D-03:** Label opsi tengah dropdown = **"Judul Kegiatan"** (ringkas, netral untuk awam HC, mencakup training & assessment sebagai "kegiatan").
- **D-04:** Placeholder `#teamSearch` (`RecordsTeam.cshtml:96`) = **"Cari nama/NIP, judul training, atau judul assessment..."** (eksplisit jujur mencakup assessment).
- **D-05:** Hint `:107` ("Menyaring worker yang muncul; jumlah badge per worker tetap utuh.") **DIPERTAHANKAN verbatim** — transparansi D-07.

### Export parity + Category symmetry (SF-06)
- **D-06:** SF-01 **otomatis** menutup sebagian SF-06: `ExportRecordsTeamAssessment`/`Training` keduanya
  pre-filter worker-list via `GetWorkersInSection(...searchScope)` (`CMPController.cs:670`, `:722`) yang
  sama — begitu predikat assessment-title masuk, search judul-assessment akan mengembalikan worker yang
  benar → Export Assessment **tidak lagi kosong**. Tidak perlu perubahan terpisah untuk bagian ini.
- **D-07:** **Simetris Category narrowing untuk baris assessment** (sejajar baris training). Saat filter
  Kategori aktif:
  - **Current** `AssessmentSessions` (punya `.Category`) → di-narrow by Category.
  - **Archived** `AssessmentAttemptHistory` (**TIDAK punya kolom Category**, lihat `WorkerDataService.cs:116`)
    → **di-DROP** saat Kategori aktif (tak bisa di-tag kategori).
  - Saat filter Kategori **kosong** → archived assessment rows muncul normal (perilaku sekarang).
  - **Rasional:** worker-narrowing on-screen sendiri keys ke current `AssessmentSessions.Category`
    (`:378`) — drop archived saat Kategori aktif membuat export konsisten dengan worker-visibility on-screen.
  - **Constraint kritis untuk planner:** `GetAllWorkersHistory` (`:93-220`) saat ini **mengabaikan
    `category` untuk assessment** — param `category` hanya diterapkan ke training rows (`:217-218`).
    Implementasi simetris harus extend (current-session Category filter + archived drop) entah di dalam
    `GetAllWorkersHistory` atau di controller `ExportRecordsTeamAssessment`. **Mekanisme tepat = Claude's
    Discretion** (researcher/planner), tapi **perilaku** terkunci seperti di atas.

### Test coverage (folded — bukan fase verify terpisah)
- **D-08:** **xUnit predicate-mirror + Playwright UAT Team View.**
  - xUnit di `HcPortal.Tests/WorkerDataServiceSearchTests.cs` (mirror gaya `Scope_Training_FiltersByJudul`
    / `Scope_Keduanya_Union_NameOrTraining`):
    1. Assessment-title match di scope `"Training"`.
    2. Assessment-title match (union) di scope `"Keduanya"`.
    3. **Invariant D-07:** badge/count per-worker (mis. `CompletedAssessments`, `TotalTrainings`)
       **tidak berubah** akibat search predikat.
    4. (SF-06) export worker-list: `GetWorkersInSection(... searchScope:"Keduanya", search: assessment-title)`
       mengembalikan worker yang benar (sebelumnya 0).
  - Playwright UAT Team View: search judul assessment ("ojt v14.2"-style) → worker pemilik **muncul** +
    verifikasi export link (`updateExportLinks` `:329-346`) membawa param `searchScope`/`search`.

### Claude's Discretion
- Mekanisme tepat extend Category-narrow assessment di Export (dalam `GetAllWorkersHistory` vs filter
  in-controller).
- Struktur assertion Playwright + seed/data approach (ikut SEED_WORKFLOW bila perlu seed).
- Apakah test xUnit export-worker-list digabung ke test predikat existing atau [Fact] terpisah.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit & requirements (sumber kebenaran fase ini)
- `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` — audit 3-surface; **SF-01** (`§2` tabel + `§4` bukti ganda), **SF-02** (`§2`), **SF-06** (`§2`), by-design list `§3` (A=D-07 invariant, F=export WYSIWYG snapshot). Semua finding code-verified `file:line`.
- `.planning/REQUIREMENTS.md` — SF-01/SF-02/SF-06 (definisi REQ + "narrow assessment per-Category setara training") + "Out of Scope (by-design)".
- `.planning/ROADMAP.md` §"Phase 350" (baris 869-881) — Success Criteria 1-5 + UI hint.

### Prior-art constraints (WAJIB dipatuhi)
- Phase 346 **REC-06 D-07** — Team search = post-load worker-level filter, badge count per-worker utuh.
  Tercermin di `WorkerDataService.cs:401` comment + `RecordsTeam.cshtml:107` hint. (Spec `prior_art_constraints`.)
- v22.0 **MAP-23** — pola "broaden search scope + label jujur" (referensi gaya, bukan file fase ini).

### Source files (titik sentuh utama)
- `Services/WorkerDataService.cs` — `GetWorkersInSection:242` (predikat `:402-417`, pola union Category `:373-381`, date-filter sessions `:283-293`); `GetAllWorkersHistory:93-220` (category→training-only `:217-218`, archived no-Category `:116`).
- `Views/CMP/RecordsTeam.cshtml` — search box `:96`, dropdown Lingkup `:100-104`, hint `:107`, `updateExportLinks` `:329-346`, `resetTeamFilters` default "Keduanya" `:448`.
- `Controllers/CMPController.cs` — `ExportRecordsTeamAssessment:652-700` (assessment `category:null` `:677`), `ExportRecordsTeamTraining:704-750` (training `category:category` `:729`).
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — pola test scope (`Scope_Training`/`Scope_Keduanya`/`Scope_Null`), helper `Session(...)` punya `.Title`.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola union Assessment** sudah ada di Category filter `WorkerDataService.cs:373-381`
  (`w.AssessmentSessions.Any(a => ... a.Category ...)`) → tinggal mirror untuk `a.Title` di blok search `:402-417`.
- **`updateExportLinks` JS** (`RecordsTeam.cshtml:329-346`) **sudah** menyertakan `searchScope` + `search`
  ke querystring export → export otomatis WYSIWYG terhadap search (by-design F). Tak perlu ubah untuk SF-06 bagian (a).
- **`WorkerDataServiceSearchTests.cs`** — InMemory DB harness + helper `User/Training/Session` siap; `Session.Title = "Asm " + id` → tambah test tinggal set Title bermakna.

### Established Patterns
- **Asimetri mekanisme Nama-vs-Training/Keduanya disengaja** (D-07): "Nama" = SQL pre-narrow (`:257-264`),
  "Training"/"Keduanya" = post-load (`:402-417`). Predikat assessment-title masuk ke jalur **post-load**, JANGAN dipindah ke SQL.
- **`.ToLower().Contains(...)`** di kedua sisi (InMemory case-sensitive) — ikuti pola existing.
- **Export pre-filter worker via `GetWorkersInSection` lalu `GetAllWorkersHistory(workerIds)`** — perubahan predikat di service satu tempat otomatis merembet ke partial + 2 export.

### Integration Points
- Predikat search: `WorkerDataService.GetWorkersInSection` `:402-417` (satu titik, dipakai partial + 2 export).
- Dropdown + copy: `Views/CMP/RecordsTeam.cshtml` `:96`, `:100-104`, `:107`.
- Export Category symmetry: `CMPController.ExportRecordsTeamAssessment` `:669-680` (+ kemungkinan `GetAllWorkersHistory` `:134-157`/`:201-204`).

### Constraints
- `AssessmentAttemptHistory` **tak punya kolom Category** (`:116`) — archived assessment tak bisa di-narrow Category (basis D-07 drop-archived decision).
- Value `searchScope` internal **tidak boleh** diganti dari "Training"/"Keduanya"/"Nama" (server switch + test + sessionStorage backward-compat).
</code_context>

<specifics>
## Specific Ideas

- Repro target bug (SF-01): search **"ojt v14.2"** (judul assessment) di Team View Lingkup "Keduanya"
  → sebelum fix "Showing 0 workers"; sesudah fix worker pemilik assessment itu muncul. (Origin backlog Phase 999.2, UAT Phase 349 2026-06-05.)
- "Keduanya" tetap **default** dropdown (sekarang benar-benar = semua: Nama/NIP + training + assessment).
- Label jujur tapi ringkas: dropdown "Judul Kegiatan" (ringkas) sementara placeholder eksplisit menyebut "judul training, atau judul assessment" (jelas) — sengaja beda granularitas (label sempit col-md-3, placeholder lega).
</specifics>

<deferred>
## Deferred Ideas

- **SF-03** (Worker Detail 0-match feedback + counter) → Phase 351.
- **SF-04** (Worker Detail filter Kategori actual-records match) → Phase 351.
- **SF-05** (paritas filter My Records ↔ Worker Detail) → Phase 351.
- **SF-07** (back-nav Worker Detail → Team View preserve `subCategory`/`dateFrom`/`dateTo`/`searchScope`) → Phase 351.
- Tidak ada ide scope-creep baru dari diskusi ini — semua tetap dalam boundary SF-01/02/06.

</deferred>

---

*Phase: 350-team-view-search-scope-export-parity*
*Context gathered: 2026-06-05*
