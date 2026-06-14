# Phase 366: Cascade Image File Cleanup - Context

**Gathered:** 2026-06-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Hapus **file gambar fisik orphan** saat cascade delete besar di `AssessmentAdminController.cs`:
`DeleteAssessment` (:2189), `DeleteAssessmentGroup` (:2377), `DeletePrePostGroup` (:2563).
Ketiganya saat ini `RemoveRange` Questions/Options dari DB tapi membiarkan file di
`wwwroot/uploads/questions/{packageId}` menjadi sampah disk.

Caranya: **ekstrak helper ref-count** dari 3 call-site inline existing Phase 353
(`DeletePackage` :5764, `EditQuestion` POST :6834, `DeleteQuestion` :6834) — 3 call-site
lama beralih ke helper (perilaku tak berubah), lalu pasang helper di 3 method `Delete*` target.

**Backend murni — no UI.** Migration=false. Severity rendah (sampah disk, BUKAN corruption / bukan gambar rusak di UI).

**DI LUAR scope (scope-creep — bukan fase ini):**
- Cleanup folder kosong `{packageId}` (di-defer, lihat Deferred Ideas).
- Cascade DB logic / pre-check renewal / file sertifikat → teritori Phase 367.
- Edit-replace file atomik → Phase 368 (#21).
</domain>

<decisions>
## Implementation Decisions

### Lokasi & bentuk helper (Area 1)
- **D-01:** Helper **static di `Helpers/`** (mis. `Helpers/ImageFileCleanup.cs` →
  `DeleteUnreferencedAsync(AppDbContext ctx, string webRootPath, ILogger logger, IEnumerable<string> paths)`).
  Bukan private method controller. Alasan: mudah di-unit/integration-test langsung (SC#4),
  1 sumber kebenaran, menggantikan mirror palsu `DeleteIfUnreferenced` di test project jadi
  helper produksi nyata. Nama method/param final = diskresi planner; yang terkunci = **static + di `Helpers/`**.
- **D-02:** 3 call-site inline lama (`DeletePackage`, `EditQuestion` POST, `DeleteQuestion`)
  **beralih panggil helper** — perilaku identik (ref-count `AnyAsync` PackageQuestions+PackageOptions
  → `File.Delete` warn-only). Per SC#1: perilaku tak boleh berubah. **Catatan ordering:** 3 call-site
  ini punya langkah `SyncPackagesToPost` (auto-sync Pre→Post) SEBELUM loop hapus — helper dipanggil
  di posisi yang sama (setelah sync), urutan tak berubah.

### Kedalaman test SC#4 (Area 2)
- **D-03:** **Integration test real-SQL** (pola Phase 344 TEST-05 disposable —
  lihat `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` / `ProtonYearGateIntegrationTests.cs`):
  assert (a) file fisik terhapus saat cascade delete session bergambar, (b) **shared-path Pre/Post
  SELAMAT** saat salah satu sisi dihapus (kasus paling rawan, SC#3). **Plus UAT manual @5277.**
- **D-04:** Test existing `PackageImageDeleteTests.cs` (private mirror `DeleteIfUnreferenced`)
  diarahkan memanggil helper produksi baru bila praktis — hindari 2 logika divergen. Detail
  refactor test = diskresi planner.

### Mekanisme batch-aware ref-count (Area 3)
- **D-05:** **Post-commit `AnyAsync` ke DB** — reuse persis pola inline. Eksekusi loop ref-count
  + `File.Delete` SETELAH `tx.CommitAsync`. Karena baris batch sudah terhapus dari DB, `AnyAsync`
  otomatis `false` untuk path khusus-batch → file dihapus; path masih dipakai Post/luar-batch →
  `AnyAsync` `true` → file selamat. **BUKAN** explicit exclusion-set (hindari duplikasi logika & drift).
  Ini sekaligus memenuhi SC#2 (sadar batch) + SC#3 (shared selamat) tanpa kode tambahan.

### Pola eksekusi di 3 method Delete* (terkunci, turunan SC#2)
- **D-06:** Per method: kumpul `ImagePath` (`Distinct()`) dari Questions+Options **SEBELUM**
  `RemoveRange` (di `DeleteAssessment` data sudah ter-`Include(p=>p.Questions).ThenInclude(q=>q.Options)`
  di :2314 — tinggal panggil paths sebelum loop RemoveRange :2320-2326). Panggil helper **SETELAH**
  `tx.CommitAsync`. Pola atomic Phase 333. `DeletePrePostGroup` hapus Pre+Post sekaligus → 2 paket
  dalam 1 batch, dijaga otomatis oleh D-05 (post-commit AnyAsync).

### Claude's Discretion
- Nama persis file/class/method helper + signature param exact (selama static + di `Helpers/`).
- Apakah `PackageImageDeleteTests.cs` di-refactor penuh ke helper produksi atau ditinggal + tambah
  integration test baru — planner putuskan asal D-03 terpenuhi.
- Pesan log warn-only per call-site (boleh bawa label sumber: "DeleteAssessment image" dst).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Roadmap
- `.planning/ROADMAP.md` (Phase 366, :267-278) — Goal + SC#1-4 terkunci + line-ref Delete*.
- `.planning/phases/353-admin-backend-gambar-crud-sync-atomic-delete/353-CONTEXT.md` — bagian
  `<deferred>` D-12 (asal fase ini) + `<decisions>` C-03/C-04/D-10 (shared-file, pola 333, ref-count).

### Kode target (file tunggal — semua di sini)
- `Controllers/AssessmentAdminController.cs`:
  - **Sumber ekstraksi helper (3 inline):** `DeletePackage` loop :5764-5778, `EditQuestion` POST loop :6834-6848 (dan declare `imagePathsToDelete` :6611-6663), `DeleteQuestion` loop :6833-6848 (collect :6782-6784).
  - **Target pasang helper (3 Delete*):** `DeleteAssessment` :2189 (tx :2235, packages Include :2314, RemoveRange :2320-2326, commit :2334), `DeleteAssessmentGroup` :2377 (tx :2427, RemoveRange :2509, commit :2519), `DeletePrePostGroup` :2563 (tx :2604, RemoveRange :2648).

### Pola test (acuan D-03)
- `HcPortal.Tests/PackageImageDeleteTests.cs` — mirror `DeleteIfUnreferenced` :34 (jadikan helper produksi nyata).
- `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs`, `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — pola integration real-SQL disposable (Phase 344 TEST-05).

### Helper existing (acuan bentuk static)
- `Helpers/FileUploadHelper.cs` — contoh helper static format-agnostic (`SaveFileAsync`/`ValidateImageFile`).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 3 blok inline identik (`AnyAsync` ref-count + `File.Delete` warn-only post-commit) — siap diangkat jadi 1 helper static.
- Pola atomic delete Phase 333 (collect-before-tx, delete-after-commit, inner try/catch warn-only) sudah diterapkan di 3 call-site inline → tinggal direplikasi ke 3 Delete*.
- `Helpers/FileUploadHelper.cs` sebagai template gaya helper static.

### Established Patterns
- Path fisik: `Path.Combine(_env.WebRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))`.
- Ref-count: `PackageQuestions.AnyAsync(x=>x.ImagePath==relUrl) || PackageOptions.AnyAsync(...)`.
- `DeleteAssessment` sudah `Include` Questions.Options (:2314) → ImagePath siap dikumpul sebelum RemoveRange. Cek apakah `DeleteAssessmentGroup`/`DeletePrePostGroup` juga Include nested (kalau belum, planner tambah Include untuk akses ImagePath).

### Integration Points
- Semua perubahan di 1 file controller + 1 file helper baru di `Helpers/` + test di `HcPortal.Tests/`.
- **File-overlap warning:** 3 Delete* method = endpoint yang sama dirombak Phase 367. Plan 367 WAJIB preserve helper image 366 (anotasi sudah di ROADMAP :282). 366 ship DULU.
</code_context>

<specifics>
## Specific Ideas

- Helper menggantikan mirror palsu di test (`PackageImageDeleteTests.cs` private `DeleteIfUnreferenced`) — user ingin 1 sumber kebenaran, bukan logika ganda.
- Kasus uji wajib: hapus Pre yang share gambar dengan Post → file HARUS selamat (regresi SYN-01 paling mahal).
</specifics>

<deferred>
## Deferred Ideas

- **Cleanup folder kosong `wwwroot/uploads/questions/{packageId}`** (Area 4 — defer). ROADMAP scope file saja. Folder kosong = sampah minor low-risk; hapus folder tambah edge-case (race / folder dipakai paket lain ber-packageId sama). Kandidat backlog hygiene bila kelak relevan.

### Reviewed Todos (not folded)
- 1 todo ter-match tapi 0 relevan (skor di bawah ambang) — tidak ada yang di-fold.
</deferred>

---

*Phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr*
*Context gathered: 2026-06-12*
