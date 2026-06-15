# Phase 367: Delete Records Cascade Overhaul - Research

**Researched:** 2026-06-12
**Domain:** ASP.NET Core MVC + EF Core cascade-delete service, HTMX honest-UI, file atomicity, real-SQL integration testing
**Confidence:** HIGH (semua anchor di-verifikasi langsung terhadap kode aktual; line drift sudah dipetakan)
**Bahasa:** Indonesia (project rule CLAUDE.md)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (Spec §2 — JANGAN diubah/dibahas)
- **L-01:** Cascade penuh, **no-blocker**. Induk dihapus → SEMUA turunan renewal ikut terhapus rekursif lintas tabel via `RenewsTrainingId`/`RenewsSessionId`, guard cycle (HashSet visited).
- **L-02:** Anak renewal turunan **IKUT DIHAPUS, bukan detach** (user tolak detach eksplisit §2.6).
- **L-03:** Konfirmasi = **preview tree** (judul+tanggal+jenis+pemilik, termasuk turunan renewal & kandidat mirror), bukan blokir. Pre-check renewal lama (fase 325/329 + tab 2) DIUBAH jadi preview cascade yang sama.
- **L-04:** `PendingProtonBypasses` ber-`LinkedAssessmentSessionId == Id` → **soft-cancel** (`Status='Dibatalkan'` + `ResolvedAt`), BUKAN hard-delete row (jejak audit dipertahankan). Koordinasi Phase 361 UI pending.
- **L-05:** Notif lonceng cleanup **konservatif** — hanya hapus `UserNotifications` yang `ActionUrl`-nya match rute entitas terhapus EKSAK. Ragu = biarkan. (Inventaris pola aktual = bagian "L-05 Inventory" di bawah.)
- **L-06:** UI jujur — sukses → `recordDeleted`, gagal → `recordDeleteFailed` (payload pesan, render merah DI DALAM partial). Tidak ada lagi respons gagal identik sukses.
- **L-07:** Online session dihapus via **refactor `DeleteManualAssessment` jadi endpoint per-session generik** (gate `IsManualEntry` dihapus; 1 endpoint layani manual + online), cascade sama, guard `Admin, HC` + antiforgery. BUKAN endpoint baru terpisah.
- **L-08:** File.Delete **POST-commit**, inner try/catch warn-only (pola fase 331-334). AuditLog 1 entri/operasi (aktor, node akar, jumlah + daftar Id turunan).

### Gray area yang diputuskan sesi ini
- **D-01 — Badge count fix (#16/#17): RECOMPUTE = baris tampil.** Count per jenis = jumlah baris yang benar-benar tampil per jenis (online+manual+training). Bukan opsi relabel.
- **D-02 — Duplicate-guard match (#12/#14): EXACT `user+judul+tanggal`.** Guard 3 pintu (AddManualAssessment, ImportTraining, BulkBackfill). Single (AddManual) = **reject** dgn pesan; import/backfill = **skip-with-report**. BEDA dari heuristik mirror #15 (±1 hari hanya untuk PREVIEW mirror, bukan guard create).
- **D-03 — Preview modal: TOMBOL "Hapus Semua" SAJA.** 1 klik konfirmasi, tanpa ketik-konfirmasi.
- **D-04 — Dep 366 sequencing: ASUMSI 366 LAND DULU.** Plan 367 WAJIB preserve helper image 366 di 3 endpoint overlap. 367 TIDAK mengabsorb scope image 366.

### Claude's Discretion (researcher/planner putuskan)
- Inventaris pola `ActionUrl` notif TrainingRecord (L-05) — **DIPETAKAN di section "L-05 ActionUrl Notif Inventory" di bawah.**
- Struktur internal `RecordCascadeDeleteService` (BFS, signature preview vs execute) — §3.1 blueprint + section "Cascade Engine Architecture".
- Ambang/threshold telemetry — bukan keputusan user.

### Deferred Ideas (OUT OF SCOPE 367)
- **Phase 368 (#21-27):** edit atomic file, reset ET scores, one-time AttemptHistory legacy cleanup, import audit log, CertificationManagement dedup, EditTraining renewal validation, BulkBackfill kosmetik. Fase terpisah, depends 368→367. **Planner 367 TIDAK menyentuh item [368].**
- **Backlog 999.6:** Impersonate identity (#13).
- **Ditolak (§3.5):** soft-delete/undo (opsi C ditolak); tab 1 filter 7-hari tetap (kebutuhan hapus sesi lama dipenuhi via tab 2).
- **Reviewed todo (tidak folded):** one-time-cleanup-data-test-lokal — eksekusi SETELAH 367 ship pakai cascade engine yang dibangun fase ini.
</user_constraints>

---

<phase_requirements>
## Phase Requirements (Spec C temuan #1-12 + #14-20)

| ID | Deskripsi | Research Support |
|----|-----------|------------------|
| #1 | Sukses palsu HTMX `DeleteTabResult()` selalu 200 | DRIFT-FREE: `TrainingAdminController.cs:561-569` (`DeleteTabResult`). Fix L-06 = bedakan `recordDeleted`/`recordDeleteFailed` |
| #2 | Blokir renewal diam-diam (pre-check → TempData tak terrender) | `TrainingAdminController.cs:590-602` (DeleteTraining), `:986-1001` (DeleteManualAssessment). Fix L-03 = ganti jadi preview cascade |
| #3 | Mismatch baris — online tak terjangkau hapus | Seam 371 sudah tampilkan online (`_TrainingRecordsTab.cshtml:368-379`); 367 tambah tombol hapus + endpoint generik (L-07) |
| #4 | Online >7 hari tak bisa dihapus via UI | URG-02 (Phase 370) sudah hapus window default; endpoint POST tidak batasi umur — verifikasi tab 2 reach |
| #5 | AttemptHistory orphan saat DeleteManualAssessment | Gold-standard `AssessmentAdminController.cs:2291-2299` bersihkan; cascade engine adopsi |
| #6 | UserNotifications yatim (no FK) | L-05 inventory di bawah; cascade hapus eksak-match |
| #7 | FK renewal anak tak di-null-clear (terealisasi BLOKIR) | Pre-check controller; cascade = hapus anak (L-02), bukan null-clear |
| #8 | LinkedSessionId dangling → gain score Post-Test silent missing | `CMPController.cs:2461-2476, 2743-3008` (read-side); cascade null-clear pasangan SEBELUM Remove |
| #9 | Penanda Proton `Origin='Exam'` tak dicabut | `ProtonCompletionService.cs:113-129` `RemoveExamOriginAsync(coacheeId, protonTrackId)` |
| #10 | PendingProtonBypasses dangling | `ProtonModels.cs:235-249`; soft-cancel L-04 |
| #11 | EditLogs + PackageUserResponses FK Restrict → DbUpdateException | Gold-standard cleanup eksplisit `:2271-2289` |
| #12 | AddManualAssessment + ImportTraining tanpa guard duplikat | `:653-725` (AddManual insert :690-714), `:1152-1267` (Import insert :1255). D-02 EXACT |
| #14 | BulkBackfill tanpa guard duplikat | `:748-878` (insert loop :821-848). D-02 skip-with-report |
| #15 | Mirror TrainingRecord legacy (no FK) | `GradingService.cs:262-265` (auto-create REMOVED Phase 324; legacy residu di DB). Heuristik preview ±1 hari |
| #16 | Badge "X assessments" hitung SEMUA IsPassed (termasuk online) | `WorkerDataService.cs:303-313, 329`; `WorkerTrainingStatus.cs:55-57`. D-01 recompute |
| #17 | Badge "Y trainings" hanya Passed/Valid/Permanent | `WorkerDataService.cs:332-334`. D-01 recompute |
| #18 | DeleteAssessmentGroup sibling over-match | `AssessmentAdminController.cs:2409-2414`. Tambah filter LinkedGroupId/AssessmentType/IsManualEntry |
| #19 | Delete tab 1 tak hapus file fisik sertifikat manual | `:2189-2769` ketiga endpoint (0 file ops sertifikat manual; HANYA image 366). Tambah collect `ManualSertifikatUrl` |
| #20 | ResetAssessment tanpa guard IsManualEntry | `AssessmentAdminController.cs:~4013-4046` (verifikasi line saat plan). Tolak dengan pesan |

> **CATATAN [368] — JANGAN ditarik:** #21 (edit atomic file), #22 (reset ET scores), #23-orphan-legacy (one-time cleanup), #24 (import audit), #25 (cert dedup), #26 (EditTraining renewal validation), #27 (BulkBackfill kosmetik). File-overlap `TrainingAdminController.cs` dikelola via depends 368→367.
</phase_requirements>

---

## Summary

Phase 367 membangun satu service cascade-delete (`RecordCascadeDeleteService`) yang menelusuri rantai renewal lintas dua tabel (`TrainingRecords` ↔ `AssessmentSessions` via kolom `RenewsTrainingId`/`RenewsSessionId`), plus rewiring UI HTMX di tab Input Records supaya jujur (gagal ≠ sukses) dan tombol hapus tersedia untuk sesi online. Pola execute-mode per node `AssessmentSession` adalah parity dari **gold-standard `DeleteAssessment`** yang sudah ter-verifikasi (EditLogs → Responses → AttemptHistory → UserPackageAssignments → Packages+Q+O → session), ditambah 4 artefak baru per node (LinkedSessionId null-clear, PendingProtonBypass soft-cancel, RemoveExamOriginAsync, UserNotifications eksak-match) dan file fisik (`ManualSertifikatUrl`/`SertifikatUrl`) post-commit warn-only.

Drift verification hasilnya: **semua anchor line dari Spec C (era 2026-06-10) bergeser** karena Phase 363/366 menyentuh `AssessmentAdminController.cs` (sekarang 7137 baris) dan `TrainingAdminController.cs`. Anchor aktual sudah dipetakan di tabel "Drift-Risk Line Verification". Dua DRIFT besar yang HARUS dikoreksi planner: (1) `ApplicationDbContext.cs:167-180/235-243` BUKAN runtime pre-check — itu konfigurasi FK NoAction; pre-check BLOKIR sebenarnya ada di controller. (2) Pola `ActionUrl` notif yang diasumsikan spec (`/CMP/Results/{id}`, `/CMP/Certificate/{id}`) **tidak ada di kode aktif** — pola riil session-bound = `/CMP/StartExam/{sessionId}` (2 call-site ASMT_ASSIGNED), plus template `/CMP/AssessmentResults/{id}` & `/CMP/AssessmentDetails/{id}` (jika `SendByTemplateAsync` dipakai). Tidak ada pola ActionUrl TrainingRecord-bound.

Phase 366 (helper static `ImageFileCleanup.DeleteUnreferencedAsync`) sudah land di 3 endpoint tab 1 dengan pola identik (collect ImagePath sebelum RemoveRange → helper post-commit, pakai `logger` lokal). Planner 367 WAJIB preserve panggilan helper itu verbatim saat refactor 3 endpoint jadi cascade engine.

**Primary recommendation:** Bangun `RecordCascadeDeleteService` dengan 2 metode publik (`BuildPreviewAsync` → tree tanpa mutasi; `ExecuteAsync` → 1 transaction). Daftarkan via `AddScoped` di Program.cs (pola existing). Re-use gold-standard `DeleteAssessment` per-node, preserve image-cleanup 366, dan validasi via integration test real-SQL disposable (pola Phase 366/360) + Playwright dua-arah sukses/gagal.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Cascade traversal renewal (BFS lintas tabel) | API/Service (`RecordCascadeDeleteService`) | DB (FK NoAction = app-level null-clear) | Renewal chain = business rule app-level; FK sengaja NoAction (SQL Server tolak SET NULL cyclic) |
| Preview konfirmasi (kumpulkan korban tree) | API/Service (read-only) → MVC (`GET DeletePreview`) | Razor partial (modal) | Read-only query di service; rendering di partial |
| Hapus artefak per-node (Responses/EditLogs/dst) | API/Service (in-tx RemoveRange) | DB (constraint Restrict) | Parity gold-standard, dalam 1 transaction |
| File.Delete sertifikat | API/Service (post-commit) | Filesystem | L-08 atomicity — collect path pre-Remove, delete post-commit |
| Image-cleanup 366 (overlap) | Helper static `ImageFileCleanup` | — | Sudah ada; preserve, jangan duplikasi |
| Honest UI flash (sukses/gagal) | MVC controller (HX-Trigger) → Razor partial | HTMX | L-06; render flash DI DALAM partial yang ter-swap |
| Tombol hapus online per-baris | Razor `_TrainingRecordsTab.cshtml` (`@if row.IsOnline`) | HTMX → modal | Seam 371 = extension point |
| Badge count recompute | Service `WorkerDataService` (formula) | Razor (`CompletionDisplayText`) | D-01 = data/formula fix, bukan view |
| Soft-cancel PendingProtonBypass | API/Service (UPDATE Status) | — | L-04 jejak audit; koordinasi Phase 361 |
| Cabut penanda Proton | API/Service (`RemoveExamOriginAsync`) | — | Existing helper, panggil saat node Proton |

---

## Drift-Risk Line Verification

> Spec C + CONTEXT pakai line dari 2026-06-10. Phase 363 (shipped 2026-06-11) + 366 (shipped 2026-06-12) menggeser line. **Semua di-verifikasi langsung 2026-06-12.** [VERIFIED: codebase read]

| File | Anchor | Line spec-era | Line aktual | Catatan drift |
|------|--------|---------------|-------------|---------------|
| `AssessmentAdminController.cs` | `DeleteAssessment` method | ~2270-2329 | **2185-2383** | Method mulai :2189. Cascade artefak: EditLogs :2271-2279, Responses :2281-2289, AttemptHistory :2291-2299, UserPackageAssignments :2301-2310, Packages+Q+O :2312-2336, Remove session :2339, SaveChanges+Commit :2341-2342. Pre-check BLOKIR :2218-2230 |
| `AssessmentAdminController.cs` | 366 image collect (DeleteAssessment) | n/a | **2318-2325** (`var imagePaths`) + helper :2346 | PRESERVE D-04 |
| `AssessmentAdminController.cs` | `DeleteAssessmentGroup` sibling query (#18 over-match) | :2395-2400 | **2408-2414** | Query: `a.Title==rep.Title && a.Category==rep.Category && a.Schedule.Date==scheduleDate`. TANPA filter LinkedGroupId/AssessmentType/IsManualEntry — itu temuan #18. 366 image collect :2513-2520 + helper :2542 |
| `AssessmentAdminController.cs` | `DeletePrePostGroup` | n/a | **2582-2769** | Method :2586. 366 image collect :2704-2711 + helper :2732. Pre-check renewal :2612-2624 |
| `AssessmentAdminController.cs` | `ResetAssessment` guard IsManualEntry (#20) | :4013-4046 | **VERIFIKASI SAAT PLAN** (file 7137 baris, kemungkinan ~ baris itu masih akurat) | Bandingkan dgn `EditAssessment` guard yang ada |
| `TrainingAdminController.cs` | `DeleteTabResult()` (#1 sukses palsu) | :561-569 | **561-569 (DRIFT-FREE)** | `HX-Trigger="recordDeleted"` selalu; `EmptyResult()` |
| `TrainingAdminController.cs` | `DeleteTraining` pre-check renewal (#2/#7) | :590-602 | **590-602 (DRIFT-FREE)** | `referencingTr + referencingAs > 0` → TempData + `DeleteTabResult()`. Capture `sertifikatPath` :579-584, tx :605, File.Delete post-commit :619-623 |
| `TrainingAdminController.cs` | `DeleteManualAssessment` gate IsManualEntry (#3) | :978 | **976-1032**, gate :978 (`s.Id==id && s.IsManualEntry`) | L-07 = hapus gate `&& s.IsManualEntry`. Pre-check :989-1001. File delete :1017-1021 |
| `TrainingAdminController.cs` | `AddManualAssessment` insert (#12 guard) | :653-725 | **653-725**, insert loop :684-715, `Add(session)` :714 | Guard EXACT user+title+CompletedAt SEBELUM Add |
| `TrainingAdminController.cs` | `ImportTraining` insert (#12 guard) | :1152-1362 | **1152**, assessment-branch insert :1255 (`Add`), training-branch mulai :1268 | Guard per-row skip-with-report (`ImportTrainingResult.Status="Skip"`) |
| `TrainingAdminController.cs` | `BulkBackfillAssessment` insert (#14 guard) | :748-878, :816-849 | **748-878**, insert loop :821-848, `Add(session)` :845 | Guard per-row skip-with-report |
| `CMPController.cs` | LinkedSessionId Post-Test gain (#8 read-side) | :2463-2776 | **2461-2476** (Results gain), **2741-2748, 2767-2778** (trend), **2876-2901, 2980-3008** (analytics) | Read-only konsumen. Cascade null-clear pasangan SEBELUM Remove node |
| `ApplicationDbContext.cs` | "pre-check renewal blokir" | :167-180, :235-243 | **167-180 (TR FK config), 229-247 (AS FK config)** | ⚠️ **DRIFT/MISLABEL** — ini KONFIGURASI FK `OnDelete(NoAction)`, BUKAN runtime pre-check. Blokir runtime ada di controller (lihat baris DeleteTraining/DeleteAssessment). Komentar :167-168 "Null-clearing handled at application level" |
| `ProtonCompletionService.cs` | `RemoveExamOriginAsync` (#9) | :70-86 | **113-129** | ⚠️ DRIFT +43. Signature: `RemoveExamOriginAsync(string coacheeId, int protonTrackId)`. Filter `Origin=="Exam"` (Interview/Bypass kebal). `AssessmentSession.UserId` = coacheeId, `AssessmentSession.ProtonTrackId` (int? :96) = protonTrackId |
| `_TrainingRecordsTab.cshtml` | seam online Aksi (371) | n/a | **368-379** (`@if row.IsOnline`), proyeksi anon :296-303, badge tipe :345-356, HTMX delete existing :387-393/:402-408 | Online branch HANYA "Lihat hasil" (:372-378), NO hapus = extension point 367 (komentar :371) |
| `WorkerDataService.cs` | badge formula (#16/#17) | :306-311, :330-332 | **303-313** (passedAssessmentLookup, count ALL IsPassed), **332-334** (completedTrainings Passed/Valid/Permanent) | D-01 recompute. `CompletionDisplayText` di `WorkerTrainingStatus.cs:55-57` |

---

## Overlap 366 Preserve (D-04) — Inventaris Eksak

> Phase 366 SHIPPED 2026-06-12 (commit `f38f7120` helper + `d63fbf9f` install). Helper `Helpers/ImageFileCleanup.DeleteUnreferencedAsync`. [VERIFIED: codebase read + 366 SUMMARY]

**Signature helper:**
```csharp
// Helpers/ImageFileCleanup.cs (namespace HcPortal.Helpers) — static
public static async Task DeleteUnreferencedAsync(
    ApplicationDbContext ctx, string webRootPath, ILogger logger,
    IEnumerable<string> paths, string source = "")
```
Pola internal: guard `IsNullOrEmpty`, ref-count `PackageQuestions.AnyAsync(x => x.ImagePath==...)` + `PackageOptions.AnyAsync(...)` POST-commit (D-05 batch-aware, **TANPA exclusion-set**), `Path.Combine` confined webroot, try/catch warn-only.

**3 call-site di `AssessmentAdminController.cs` (overlap 367):**

| Endpoint | Collect `var imagePaths` (sebelum RemoveRange) | Helper call (setelah `tx.CommitAsync`) | Logger |
|----------|-----------------------------------------------|----------------------------------------|--------|
| `DeleteAssessment` | **:2318-2325** (`packages.SelectMany Q+O ImagePath, !empty, Distinct`) | **:2346** label `"DeleteAssessment image"` | `logger` LOKAL (`GetRequiredService<ILogger<...>>` :2191) |
| `DeleteAssessmentGroup` | **:2513-2520** (`allPackages.SelectMany...`) | **:2542** label `"DeleteAssessmentGroup image"` | `logger` LOKAL :2391 |
| `DeletePrePostGroup` | **:2704-2711** (`allPackages...` Pre+Post 1 batch) | **:2732** label `"DeletePrePostGroup image"` | `logger` LOKAL :2588 |

**Pola atomic yang HARUS dipertahankan (urutan per method, by construction):**
`collect imagePaths` < `RemoveRange Packages` < `SaveChangesAsync` < `tx.CommitAsync` < `ImageFileCleanup.DeleteUnreferencedAsync(...)`.

**GOTCHA logger (dari memory 366 + verifikasi):** Di 3 method Delete* ini, variabel `logger` adalah **lokal** (`var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();`), **BUKAN field `_logger`**. Kalau planner refactor jadi cascade engine, pastikan helper image tetap dapat `ILogger` yang valid. Di `TrainingAdminController.cs` sebaliknya pakai field `_logger` (kelas-level). Jangan tertukar.

**Kontrak preserve:** Saat 367 refactor 3 endpoint tab 1 supaya memanggil `RecordCascadeDeleteService.ExecuteAsync`, image-cleanup 366 HARUS tetap dijalankan — **sekali per operasi, post-commit, tanpa duplikasi**. Dua opsi arsitektur (planner pilih):
- (A) Cascade engine **menerima `webRootPath`/`ILogger`** dan memanggil `ImageFileCleanup.DeleteUnreferencedAsync` sendiri sebagai bagian post-commit cleanup-nya (image + sertifikat manual jadi 1 jalur). Risiko: dobel kalau endpoint juga panggil.
- (B) Cascade engine TIDAK sentuh image; endpoint tetap panggil `ImageFileCleanup` seperti sekarang setelah engine return korban + commit. Lebih aman separasi (sesuai "367 TIDAK absorb scope image 366").

**Rekomendasi:** Opsi (B) untuk 3 endpoint tab 1 yang sudah punya image-cleanup (preserve verbatim), DAN cascade engine yang mengumpulkan `ManualSertifikatUrl`/`SertifikatUrl` (sertifikat, bukan gambar soal) menangani file sertifikat sendiri post-commit. Image soal = ranah 366 (preserve), file sertifikat = ranah 367 (#19). Dua jenis file berbeda, dua jalur cleanup berbeda — tidak tumpang tindih.

---

## Seam 371 (L-07) — Peta UI

> `_TrainingRecordsTab.cshtml` (507 baris). Phase 371 SHIPPED 2026-06-12 (`d1d03e13`), visibility-only. [VERIFIED: codebase read + 371 SUMMARY]

**Proyeksi anon 10-property (:296-303):** `trainingRows.Concat(assessmentRows).Concat(onlineRows).OrderByDescending(r => r.Date)`.
Shape tiap row: `{ Type, Date, Title, Detail, Status, StatusClass, ValidUntil, Id, IsOnline, CanViewResult }`.
- `trainingRows` (:296): `Type="Training"`, `IsOnline=false`, `CanViewResult=false`
- `assessmentRows` (:297-299): `.Where(a => a.IsManualEntry)`, `Type="Assessment"`, `IsOnline=false`
- `onlineRows` (:300-302): `.Where(a => !a.IsManualEntry)`, `Type="AssessmentOnline"`, `IsOnline=true`, `CanViewResult=(PendingGrading || CompletedAt!=null)`

**Badge tipe 3-way (:345-356):** Training Manual (`bg-success`) / Assessment Online (`bg-secondary`) / Assessment Manual (`bg-info text-white`).

**Kolom Aksi (:366-410)** — 3 cabang:
1. `@if (row.IsOnline)` (:368-379) → **HANYA "Lihat hasil"** (eye button, gated `CanViewResult`, `Url.Action("Results","CMP")`). **NO Edit, NO Hapus** — komentar :371 eksplisit "placeholder Phase 367 — extension point cascade delete". **KONFIRMASI: tombol hapus online BELUM ADA.**
2. `else if (row.Type == "Training")` (:380-394) → Edit (`EditTraining`) + HTMX delete (`hx-post DeleteTraining` + `hx-confirm` + `hx-swap="none"`, token via `hx-vals`)
3. `else` (manual) (:395-409) → Edit (`EditManualAssessment`) + HTMX delete (`hx-post DeleteManualAssessment` + `hx-confirm` + `hx-swap="none"`)

**Re-fetch listener (:178-183):** hidden div `hx-get ManageAssessmentTab_Training`, `hx-trigger="recordDeleted from:body"`, `hx-include="#filterFormTraining"`, target `closest .htmx-tab-wrapper`. **Preserve filter.** Flash 367 (S3) di-render DI ATAS struktur ini (top of partial), bukan menggantikan re-fetch.

**Rewire 367 (per UI-SPEC IC-1..IC-3):**
- 3 tombol hapus (online baru + training existing + manual existing) → BUKAN lagi `hx-post` langsung. Trigger `GET DeletePreview(type, id)` → inject ke modal body → show modal. Hapus `hx-confirm` native (D-03 — modal pengganti).
- "Hapus Semua" di modal → POST endpoint refactored (`DeleteTraining` / generic `DeleteManualAssessment`) + antiforgery + mirror-candidate IDs ter-centang.
- Sukses → `HX-Trigger: recordDeleted` (re-fetch existing + flash sukses S3). Gagal → `HX-Trigger: recordDeleteFailed` + payload + HTTP status gagal → flash merah S3.

---

## L-05 ActionUrl Notif Inventory (Claude's Discretion — DIPETAKAN)

> ⚠️ **DRIFT BESAR vs Spec.** Spec asumsi `/CMP/Results/{id}` & `/CMP/Certificate/{id}`. **Tidak ada di kode aktif.** [VERIFIED: grep seluruh *.cs]

**`UserNotifications.ActionUrl` ditulis via 2 jalur:**
1. `SendAsync(userId, type, title, message, actionUrl)` — actionUrl literal dari call-site.
2. `SendByTemplateAsync(userId, type, context)` — substitusi `ActionUrlTemplate` dari `_templates` dict (`NotificationService.cs:34-101`).

**Pola session-bound RIIL (yang merujuk `AssessmentSession.Id`):**

| Pola ActionUrl | Sumber | Kapan dibuat | Aman dihapus saat session terhapus? |
|----------------|--------|--------------|--------------------------------------|
| `/CMP/StartExam/{session.Id}` | `AssessmentAdminController.cs:1514` + `:2148` (SendAsync literal, type `ASMT_ASSIGNED`) | CreateAssessment / assign worker baru | ✅ YA — eksak match `=/CMP/StartExam/{id}` |
| `/CMP/AssessmentResults/{AssessmentId}` | template `ASMT_RESULTS_READY` (`NotificationService.cs:47`) | Jika `SendByTemplateAsync("ASMT_RESULTS_READY", {AssessmentId})` dipanggil (cari call-site saat plan; grep tidak temukan call aktif — kemungkinan dormant) | ✅ YA jika ada — eksak match |
| `/CMP/AssessmentDetails/{AssessmentId}` | template `ASMT_ASSIGNED` (`:41`) | Template default ASMT_ASSIGNED — **TAPI call-site aktual override pakai `/CMP/StartExam/{id}` literal**, jadi template ini kemungkinan tak terpakai | ✅ YA jika ada |

**Pola TrainingRecord-bound:** **TIDAK ADA.** Tidak ada notif dengan ActionUrl yang merujuk `TrainingRecord.Id`. `CERT_EXPIRED` (`HomeController.cs:146-152`) pakai `/Admin/RenewalCertificate` (statis, bukan record-id-bound) → **JANGAN dihapus** (bukan entitas-spesifik).

**Pola NON-session yang HARUS dibiarkan (ragu = biarkan, L-05):** `/CDP/ProtonProgress`, `/CDP/CoachingProton`, `/ProtonData/Override?...`, `/Admin/RenewalCertificate`, `/Admin/ManageAssessment` — semua statis/non-record-bound.

**Query aman cascade (konservatif, eksak-match per node session):**
```csharp
// Untuk setiap AssessmentSession Id yang terhapus dalam cascade:
var notifUrls = new[] {
    $"/CMP/StartExam/{sessionId}",
    $"/CMP/AssessmentResults/{sessionId}",
    $"/CMP/AssessmentDetails/{sessionId}"
};
var orphanNotifs = await _context.UserNotifications
    .Where(n => n.ActionUrl != null && notifUrls.Contains(n.ActionUrl))
    .ToListAsync();
_context.UserNotifications.RemoveRange(orphanNotifs);
```
**TrainingRecord node:** tidak ada notif untuk dihapus (no pola). Catat eksplisit di engine: "TrainingRecord → 0 notif cleanup (no record-bound ActionUrl pattern)."

**OPEN QUESTION (L-05):** Spec sebut `/CMP/Certificate/{id}` — tidak ditemukan di kode notif. Jika user/HC pernah lihat pola itu, mungkin dari versi lama yang sudah hilang. Rekomendasi: jangan sertakan pola yang tidak terbukti ada (konservatif). Konfirmasi dengan user saat discuss/plan apakah `/CMP/AssessmentResults` & `/CMP/AssessmentDetails` perlu disertakan (mereka ada di template tapi call-site aktif tak ditemukan).

---

## Cascade Engine Architecture (§3.1)

> Service baru `RecordCascadeDeleteService`. Belum ada di kode (`grep RecordCascadeDeleteService` = 0). [VERIFIED]

### Relasi FK aktual (di-verifikasi `ApplicationDbContext.cs` + models)

| Tabel | Kolom renewal | Tipe | FK config |
|-------|---------------|------|-----------|
| `TrainingRecord` | `RenewsTrainingId` (`:62`), `RenewsSessionId` (`:68`) | `int?` | `OnDelete(NoAction)` (DbContext :172-180). Mutual-exclusive check constraint :183 |
| `AssessmentSession` | `RenewsSessionId` (`:116`), `RenewsTrainingId` (`:123`) | `int?` | `OnDelete(NoAction)` (DbContext :235-243). Check constraint :246 |

**Arah anak→induk:** `RenewsXId` di anak menunjuk ke Id induk. Untuk cari ANAK dari node induk `id`:
- Anak TrainingRecord: `TrainingRecords.Where(t => t.RenewsSessionId==id)` (jika induk session) ATAU `t.RenewsTrainingId==id` (jika induk training)
- Anak AssessmentSession: `AssessmentSessions.Where(a => a.RenewsSessionId==id || a.RenewsTrainingId==id)` sesuai tipe induk

### Signature yang direkomendasikan

```csharp
public class RecordCascadeDeleteService
{
    // Node identitas: (Type, Id) — Type ∈ {"training","session"}
    public record CascadeNode(string Type, int Id, string Title, DateTime Date, string OwnerName, bool IsRoot, bool IsMirrorCandidate);

    // PREVIEW: BFS kumpulkan korban + kandidat mirror, TANPA mutasi
    public async Task<List<CascadeNode>> BuildPreviewAsync(string rootType, int rootId);

    // EXECUTE: 1 transaction, hapus semua node + artefak; return ringkasan (count + Ids)
    public async Task<CascadeResult> ExecuteAsync(string rootType, int rootId, IEnumerable<int> mirrorTrainingIdsToInclude);
}
```

### Traversal (BFS, guard cycle)
- `HashSet<(string,int)> visited` cegah siklus (FK NoAction memungkinkan chain; check-constraint cegah self pada satu node tapi tidak cegah chain A→B→A lintas tabel secara teori).
- Queue mulai dari root. Per node: enqueue anak (2 query per node: TR anak + AS anak). Tandai visited sebelum enqueue.
- **Preview set == Execute set** (invariant uji): keduanya pakai traversal yang SAMA; execute hanya menambah mutasi.

### Execute per-node (parity gold-standard `DeleteAssessment` + tambahan)

**Node `AssessmentSession` (urutan, dalam tx):**
1. `LinkedSessionId` null-clear pasangan (#8): `AssessmentSessions.Where(a => a.LinkedSessionId==id).ExecuteUpdate(SetProperty(LinkedSessionId, null))` SEBELUM hapus node (kalau tidak, gain-score reads CMP putus). **Atau** kalau pasangan juga ikut terhapus dalam cascade, tidak perlu null-clear (sudah hilang). Hati-hati: pasangan Pre/Post mungkin BUKAN turunan renewal — null-clear lebih aman.
2. RemoveRange `AssessmentEditLogs` (by `AssessmentSessionId`) — Restrict FK
3. RemoveRange `PackageUserResponses` (by `AssessmentSessionId`) — Restrict FK
4. RemoveRange `AssessmentAttemptHistory` (by `SessionId`) — no FK (#5)
5. RemoveRange `UserPackageAssignments` (by `AssessmentSessionId`) — Restrict FK ke Package
6. RemoveRange `AssessmentPackages`+Questions+Options (collect `ImagePath` 366 + collect `ManualSertifikatUrl` #19 SEBELUM RemoveRange)
7. `PendingProtonBypasses.Where(p => p.LinkedAssessmentSessionId==id && p.Status != "Dibatalkan")` → **soft-cancel** `Status="Dibatalkan"`, `ResolvedAt=DateTime.UtcNow` (L-04, BUKAN Remove)
8. Jika `ProtonTrackId.HasValue` (node Proton, #9): `await _protonCompletion.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value)` — catat: helper ini SaveChanges internal (`:127`), pertimbangkan urutan dalam tx
9. `UserNotifications` eksak-match (L-05 inventory) — RemoveRange
10. `_context.AssessmentSessions.Remove(session)`

**Node `TrainingRecord` (urutan, dalam tx):**
1. Collect `SertifikatUrl` (#19) SEBELUM Remove
2. `UserNotifications`: 0 cleanup (no pola — catat eksplisit)
3. `_context.TrainingRecords.Remove(record)`

**Post-commit (L-08):** Setelah `tx.CommitAsync`, File.Delete semua `SertifikatUrl`/`ManualSertifikatUrl` ter-collect, inner try/catch warn-only per file. Image soal = jalur `ImageFileCleanup` 366 (preserve di 3 endpoint tab 1).

**AuditLog (L-08):** 1 entri per operasi: aktor, node akar (type+id+title), jumlah turunan + daftar Id. Pakai `_auditLog.LogAsync` (catat: SaveChanges internal — panggil POST commit, pola gold-standard :2349-2364).

### DI Registration
```csharp
// Program.cs (~line 60, pola existing AddScoped service)
builder.Services.AddScoped<HcPortal.Services.RecordCascadeDeleteService>();
```
Inject `ApplicationDbContext`, `ILogger<RecordCascadeDeleteService>`, `ProtonCompletionService`, `AuditLogService`, `IWebHostEnvironment` (untuk webRootPath file ops).

---

## Mirror Legacy (#15) — Heuristik Preview

> `GradingService.cs:262-265`: TrainingRecord auto-create **REMOVED Phase 324 (D-01)**. Regresi `766011b6` (re-add 2026-04-10), removal asli `79284609` (2026-03-18). [VERIFIED: codebase read]

**Status:** Mirror BARU tidak lagi dibuat (auto-create sudah dihapus). Tapi **legacy residu** dari window regresi mungkin masih di DB lokal/Dev — tanpa FK ke session, jadi cascade FK-traversal TIDAK menjangkaunya. Tampil terus di worker view pasca hapus session (#15).

**Heuristik kandidat (preview only, checkbox opt-out default CHECKED):**
```csharp
// Untuk session node yang terhapus, cari TrainingRecord mirror milik user yang sama:
var mirrorCandidates = await _context.TrainingRecords
    .Where(t => t.UserId == session.UserId
        && (t.Judul == session.Title || t.Judul == "Assessment: " + session.Title)
        && t.Tanggal >= session.Schedule.AddDays(-1)
        && t.Tanggal <= session.Schedule.AddDays(1))   // ±1 hari (BEDA dari guard duplikat #12 yang EXACT)
    .ToListAsync();
```
- **JANGAN auto-hapus** — tampil di preview sebagai checkbox "Ikut hapus: {judul} · {tanggal}" (default checked, admin bisa uncheck).
- ±1 hari toleransi HANYA untuk mirror preview. Guard create (#12/#14) pakai EXACT (D-02).
- Query aman: no FK, read-only. Field `TrainingRecord.Judul` (`:14`), `Tanggal` (DateTime `:17`), `UserId` (`:10`). Session `Title`, `Schedule` (DateTime), `UserId`.

---

## Guard Duplikat 3 Pintu (D-02, #12/#14)

> EXACT `UserId + Title/Judul + Tanggal`. [VERIFIED: line aktual]

| Pintu | Method:line | Insert point | Perilaku | Cara cek existing |
|-------|-------------|--------------|----------|-------------------|
| **AddManualAssessment** | `:653-725`, loop :684-715, `Add` :714 | Per worker (`wc`) | **REJECT** (single) — `ModelState.AddModelError` + `return View(model)` jika dup | `AssessmentSessions.AnyAsync(s => s.UserId==wc.UserId && s.Title==model.Title && s.CompletedAt==model.CompletedAt && s.IsManualEntry)` |
| **ImportTraining** (assessment branch) | `:1255` `Add` | Per row Excel | **SKIP-with-report** — `result.Status="Skip"`, `result.Message="duplikat — dilewati"`, `continue` | `AnyAsync(s => s.UserId==targetUser.Id && s.Title==judul && s.CompletedAt==parsedDate && s.IsManualEntry)`. (Training branch :1268+ analog: cek `TrainingRecords` by Judul+Tanggal) |
| **BulkBackfillAssessment** | `:821-848`, `Add` :845 | Per row Excel (in-tx) | **SKIP-with-report** — kolom status "duplikat — dilewati", jangan increment `success` | `AnyAsync(s => s.UserId==user.Id && s.Title==title && s.CompletedAt==completedAt && s.IsManualEntry)`. Hati-hati: cek dalam tx (sudah `BeginTransactionAsync` :817) — pakai query DB, atau track in-memory set untuk dup dalam batch yang sama |

**Catatan:** `ImportTrainingResult` (`Models/ImportTrainingResult.cs`) saat ini hanya `Status="Success"|"Error"` — tambah `"Skip"` (atau reuse `BudgetTrainingImportResult` yang sudah punya "Skip"). UI hasil import perlu render baris Skip (kolom status).

**Performa:** Untuk import/backfill batch, pertimbangkan pre-load existing keys sekali (`ToHashSet`) daripada `AnyAsync` per-row (N query). Tapi prioritas = correctness; optimize jika perlu.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Hapus artefak per-session | Loop manual ad-hoc | Parity **gold-standard `DeleteAssessment` :2271-2339** | Sudah handle ordering Restrict FK (EditLogs/Responses sebelum session), sudah teruji 363 |
| Hapus file gambar soal | Re-implement ref-count | `ImageFileCleanup.DeleteUnreferencedAsync` (366) | Sudah batch-aware post-commit AnyAsync, shared Pre/Post selamat, warn-only |
| Cabut penanda Proton | Query+Remove ProtonFinalAssessments manual | `ProtonCompletionService.RemoveExamOriginAsync` (:113) | Selektif Origin="Exam" (Interview/Bypass kebal) — salah hapus = bug |
| Preview impact endpoint | Pola baru | Pola `GET ...DeletePreview` existing (`CoachCoacheeMappingDeletePreview` :1199, `SilabusDeletePreview` ProtonDataController :588) | Konvensi GET-preview-partial sudah ada |
| File atomicity | File.Delete sebelum commit | Pola fase 331-334 (capture path pre-Remove, delete post-commit, warn-only) | DB-first; orphan file acceptable, data loss tidak |
| Integration test real-SQL | Mock DbContext | Disposable fixture `HcPortalDB_Test_{guid}` (`ImageCleanupIntegrationTests.cs`, `ProtonCompletionFixture`) | FK + cascade hanya teruji di SQL Server nyata |
| Honest HTMX flash | Custom JS toast lib | `HX-Trigger` event + in-partial Bootstrap alert | UI-SPEC out-of-scope toast lib; honest-UI = render di fragment ter-swap |

**Key insight:** Hampir semua building-block sudah ada di codebase (gold-standard delete, image helper, proton helper, preview pola, file atomicity, test fixture). 367 = **mengorkestrasi** mereka dalam 1 service rekursif + rewire UI jujur, BUKAN membangun primitif baru.

---

## Common Pitfalls

### Pitfall 1: Salah hapus penanda Proton non-Exam
**Apa:** Hapus semua `ProtonFinalAssessments` untuk track → hapus penanda Interview/Bypass yang sah.
**Hindari:** Pakai `RemoveExamOriginAsync` (sudah filter `Origin=="Exam"`). Jangan query manual.
**Warning sign:** Test `ProtonCompletionServiceTests` gagal pada kasus Bypass-kebal.

### Pitfall 2: `RenewsSessionId` vs `RenewsTrainingId` tertukar
**Apa:** Saat node = AssessmentSession, anak menunjuk via `RenewsSessionId`; saat node = TrainingRecord, via `RenewsTrainingId`. Memory phase 331 catat gotcha ini (komentar `:987` "FK column = RenewsSessionId BUKAN RenewsTrainingId").
**Hindari:** Per node type, query 2 kolom yang BENAR. Mutual-exclusive constraint berarti satu anak hanya isi salah satu.
**Warning sign:** Cascade lewatkan turunan (worker masih lihat sesi pasca hapus — kasus Rino #3).

### Pitfall 3: LinkedSessionId tidak di-null-clear → gain score CMP putus
**Apa:** Hapus PreTest tanpa null-clear `LinkedSessionId` di PostTest pasangan → `CMPController` gain-score read (`:2776 preScoreDict[LinkedSessionId]`) dapat session hilang → silent missing (#8).
**Hindari:** Null-clear pasangan SEBELUM Remove, ATAU pastikan pasangan ikut cascade. FK NoAction = tidak auto-null.
**Warning sign:** Post-Test gain score hilang di dashboard CMP setelah delete.

### Pitfall 4: Image-cleanup 366 ter-duplikasi atau hilang saat refactor
**Apa:** Refactor 3 endpoint tab 1 jadi cascade engine → lupa preserve `ImageFileCleanup` call, atau panggil 2×.
**Hindari:** Opsi (B) — endpoint tetap panggil image-cleanup; engine hanya urus sertifikat. Preserve `logger` lokal (bukan `_logger`) di method ini.
**Warning sign:** Integration test 366 (`ImageCleanupIntegrationTests`) gagal, atau file shared Pre/Post salah terhapus.

### Pitfall 5: HTMX masih sukses-palsu (#1 belum tuntas)
**Apa:** Endpoint return `recordDeleted` di jalur gagal (mis. catch DbUpdateException tetap `DeleteTabResult()`).
**Hindari:** Jalur gagal HARUS `recordDeleteFailed` + HTTP status gagal + payload. Bedakan dari sukses (L-06).
**Warning sign:** Playwright skenario-gagal lihat flash hijau, bukan merah.

### Pitfall 6: Razor dynamic anon-shape runtime error
**Apa:** Lesson Phase 354/371 — Razor `dynamic`/anon-type `.Concat()` shape mismatch → `RuntimeBinderException` runtime (build hijau, browser 500).
**Hindari:** Jaga 10-property shape identik saat tambah baris/kolom. Verifikasi via Playwright runtime, bukan cuma build.
**Warning sign:** HTTP 500 saat expand worker di tab.

### Pitfall 7: Guard duplikat false-positive blok re-entry sah
**Apa:** Guard terlalu longgar (±hari) blok input tanggal beda yang sah.
**Hindari:** Guard create = EXACT tanggal (D-02). ±1 hari HANYA mirror preview.
**Warning sign:** Admin tak bisa input record tanggal beda untuk judul sama.

---

## Code Examples

### Soft-cancel PendingProtonBypass (L-04)
```csharp
// Source: ProtonModels.cs:235-249 + bypass spec §8.1
var pendingToCancel = await _context.PendingProtonBypasses
    .Where(p => p.LinkedAssessmentSessionId == sessionId && p.Status != "Dibatalkan")
    .ToListAsync();
foreach (var p in pendingToCancel)
{
    p.Status = "Dibatalkan";        // BUKAN Remove — jejak audit
    p.ResolvedAt = DateTime.UtcNow;
}
```

### Cabut penanda Proton (#9)
```csharp
// Source: ProtonCompletionService.cs:113-129 (verified signature)
if (session.ProtonTrackId.HasValue)   // node Proton
{
    // session.UserId = coacheeId; ProtonTrackId = protonTrackId
    await _protonCompletion.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
}
```

### Honest HTMX trigger (L-06)
```csharp
// Sukses
Response.Headers["HX-Trigger"] = "recordDeleted";
// Gagal — payload pesan, status reflect failure
Response.StatusCode = StatusCodes.Status400BadRequest;
Response.Headers["HX-Trigger"] =
    System.Text.Json.JsonSerializer.Serialize(new { recordDeleteFailed = new { pesan = errorMsg } });
```

---

## Validation Architecture

> `nyquist_validation: true` di `.planning/config.json` → section ini WAJIB. [VERIFIED: config.json]

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests/`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate runsettings) |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (SQL-less, cepat) |
| Full suite command | `dotnet test HcPortal.Tests` (termasuk integration real-SQL @localhost\SQLEXPRESS) |
| Baseline saat ini | **229/229 passed** (post-366), 2 WIP failure di `AssessmentWindowRemovalTests.cs` = file untracked sesi paralel (independen) |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Automated Command | File Exists? |
|-----|----------|-----------|-------------------|-------------|
| #1/#2/L-06 | DeleteTabResult bedakan sukses/gagal HX-Trigger | unit | `dotnet test --filter RecordCascadeUiTests` | ❌ Wave 0 |
| §3.1 traversal | BFS multi-level lintas tabel, cycle guard, **preview set == execute set** | unit | `dotnet test --filter RecordCascadeServiceTests` | ❌ Wave 0 |
| #5/#6/#7/#8/#9/#10/#11 | Execute hapus semua artefak per-tabel (assert eksplisit) | integration real-SQL | `dotnet test --filter RecordCascadeIntegrationTests` | ❌ Wave 0 |
| #10/L-04 | PendingProtonBypass jadi `Status='Dibatalkan'` (BUKAN hilang) | integration real-SQL | (same fixture) | ❌ Wave 0 |
| #8 | LinkedSessionId pasangan jadi NULL | integration real-SQL | (same fixture) | ❌ Wave 0 |
| L-08 | AuditLog 1 entri berisi daftar turunan Id | integration real-SQL | (same fixture) | ❌ Wave 0 |
| #19 | File sertifikat manual terhapus post-commit | [Fact] file-on-disk | `dotnet test --filter RecordCascadeFileTests` | ❌ Wave 0 (preseden `ImageCleanupIntegrationTests` + Phase 355 `Replace_NewFileWins_DeletesOldFileOnDisk`) |
| #12/#14/D-02 | Guard duplikat 3 pintu (reject single / skip import+backfill) | unit | `dotnet test --filter DuplicateGuardTests` | ❌ Wave 0 |
| #15 | Heuristik mirror match/non-match ±1 hari | unit | `dotnet test --filter MirrorHeuristicTests` | ❌ Wave 0 |
| #16/#17/D-01 | Badge formula = baris tampil per jenis | unit | `dotnet test --filter BadgeRecomputeTests` | ❌ Wave 0 |
| #18 | DeleteAssessmentGroup sibling filter (no over-match) | unit | `dotnet test --filter SiblingFilterTests` | ❌ Wave 0 |
| #20 | ResetAssessment guard IsManualEntry tolak | unit | `dotnet test --filter ResetGuardTests` | ❌ Wave 0 |
| L-01/L-03 (e2e) | Preview→hapus→DB bersih→worker view bersih (sukses) + flash merah (gagal) + online >7hari | manual Playwright @5277 | (UAT, pola fase 359/362/371) | manual |

### Cara seed renewal-chain (integration test)
- Fixture disposable `HcPortalDB_Test_{Guid}` @`localhost\SQLEXPRESS`, `Database.MigrateAsync()` full chain, `[Trait("Category","Integration")]`, `IClassFixture`/`IAsyncLifetime` — **pola `ImageCleanupIntegrationTests.cs` / `ProtonCompletionFixture`**.
- Seed: `ApplicationUser` minimal DULU (FK `FK_AssessmentSessions_Users_UserId` — lesson 366 deviation), lalu chain: session induk → TrainingRecord anak (`RenewsSessionId=induk.Id`) → AssessmentSession cucu (`RenewsTrainingId=anak.Id`). Tambah artefak (EditLog/Response/AttemptHistory/UPA/Package+Q+O/PendingBypass/notif/ProtonFinalAssessment) untuk assert per-tabel.
- `FakeNotificationService` (`HcPortal.Tests/FakeNotificationService.cs`) untuk unit; integration pakai DB nyata.
- Assert per tabel: `ctx.<Table>.CountAsync(...) == 0` (hapus) ATAU `Status=="Dibatalkan"` (soft-cancel) ATAU `LinkedSessionId==null` (null-clear) ATAU AuditLog `.Description` contains Ids turunan.

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` (< 30s).
- **Per wave merge:** `dotnet test HcPortal.Tests` (full, real-SQL).
- **Phase gate:** Full suite green + Playwright UAT 2-arah + Seed Workflow (snapshot→seed→restore+journal) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/RecordCascadeServiceTests.cs` — unit traversal + preview==execute + cycle guard (#§3.1)
- [ ] `HcPortal.Tests/RecordCascadeIntegrationTests.cs` — real-SQL per-tabel assert (#5-11, L-04, L-08) — reuse fixture pola 366
- [ ] `HcPortal.Tests/RecordCascadeFileTests.cs` — [Fact] sertifikat manual post-commit (#19)
- [ ] `HcPortal.Tests/DuplicateGuardTests.cs` — 3 pintu (#12/#14, D-02)
- [ ] `HcPortal.Tests/MirrorHeuristicTests.cs` — ±1 hari (#15)
- [ ] `HcPortal.Tests/BadgeRecomputeTests.cs` — D-01 (#16/#17)
- [ ] Test online-delete endpoint generik (L-07) + sibling filter (#18) + reset guard (#20)
- Framework sudah ada (xUnit) — **no install needed.**

---

## Security Domain

> `security_enforcement` absent di config → default ENABLED. Stack: ASP.NET Core MVC + EF Core.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Cascade engine 1 service, transaction-bounded |
| V2 Authentication | no (reuse) | `[Authorize(Roles="Admin, HC")]` existing |
| V4 Access Control | yes | **Endpoint generik L-07 WAJIB `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`** (parity DeleteManualAssessment :973-975). IDOR: preview/execute terima id arbitrer — admin-only OK, tapi validasi entity exists |
| V5 Input Validation | yes | `type` param (`training`/`session`) whitelist; id int; mirror-IDs validasi milik user yang sama |
| V6 Cryptography | no | — |
| V7 Error Handling | yes | Gagal → pesan generik ke user (jangan leak `ex.Message` — pola D6 fix Phase 334), detail ke log |
| V12 File Resources | yes | `Path.Combine(webRoot, url.TrimStart('/'))` confined (pola existing), File.Delete warn-only |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada endpoint hapus baru | Tampering | `[ValidateAntiForgeryToken]` + `__RequestVerificationToken` di hx-vals (existing pola :389) |
| Cascade hapus data sah (admin tak baca preview) | Repudiation/DoS | Preview eksplisit + AuditLog daftar Id + snapshot DB UAT (risiko utama spec §4) |
| IDOR via mirror-IDs ter-inject di POST | Elevation | Validasi mirror-ID `UserId == session.UserId` server-side sebelum hapus (jangan percaya checkbox client) |
| Info leak via error message | Info disclosure | Pesan generik (pola Phase 334 D6: NO `+ ex.Message`) |
| Path traversal File.Delete | Tampering | Confined webroot `Path.Combine` (pola existing FileUploadHelper) |

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (dotnet) | build + test + run | ✓ (asumsi — proyek aktif) | — | — |
| SQL Server (localhost\SQLEXPRESS) | integration test real-SQL + DB lokal | ✓ (HcPortalDB_Dev aktif) | — | unit test InMemory (kurang akurat FK) |
| Playwright MCP | UAT 2-arah @5277 | ✓ (dipakai 366/371 UAT) | — | manual browser |
| ClosedXML | ImportTraining/BulkBackfill (existing) | ✓ (`XLWorkbook` dipakai) | — | — |

**Catatan lingkungan (CLAUDE.md Develop Workflow):** AD lokal WAJIB `Authentication__UseActiveDirectory=false dotnet run` untuk UAT @5277. Migration = **FALSE** (367 semua perubahan kode). SEED_WORKFLOW WAJIB: snapshot DB lokal → seed renewal-chain → restore + journal `cleaned`.

---

## Runtime State Inventory

> Phase 367 = cascade delete (mutasi data + kode). Bukan rename. Tapi ada implikasi state.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | DB lokal `HcPortalDB_Dev` punya legacy mirror TrainingRecord (#15) + kemungkinan renewal-chain test data | Seed Workflow snapshot/restore; mirror dibersihkan via cascade engine yang dibangun (one-time cleanup = SETELAH ship, deferred) |
| Live service config | None — verified (tidak ada konfigurasi eksternal tersentuh) | None |
| OS-registered state | None — verified | None |
| Secrets/env vars | None — `Authentication__UseActiveDirectory` env hanya untuk run lokal, tidak diubah | None |
| Build artifacts | None — no package rename | None |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| TrainingRecord auto-create saat grading (mirror) | Removed — AssessmentSession sole source | Phase 324 (D-01), `GradingService.cs:262-265` | Mirror baru tak dibuat; legacy residu butuh heuristik cleanup |
| DeleteManualAssessment gate `IsManualEntry` (manual-only) | Endpoint per-session generik (manual+online) | Phase 367 (L-07) | 1 endpoint, gate dihapus |
| Pre-check renewal = BLOKIR (fase 325/329) | Preview cascade no-blocker (L-03) | Phase 367 | Induk dihapus = turunan ikut, bukan ditolak |
| Inline image ref-count (3 blok) | `ImageFileCleanup` helper static | Phase 366 | Preserve; jangan re-inline |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `/CMP/AssessmentResults/{id}` & `/CMP/AssessmentDetails/{id}` template ada tapi call-site aktif tak ditemukan (grep) — mungkin dormant | L-05 Inventory | Rendah — jika dipakai, query eksak-match tetap aman; jika tidak, no-op |
| A2 | `ResetAssessment` masih di ~:4013-4046 (tak di-verifikasi langsung, file 7137 baris) | Drift table #20 | Rendah — planner verifikasi saat plan; method namanya stabil |
| A3 | Mirror legacy residu masih ada di DB Dev/lokal (komentar GradingService implies, tak query DB live) | Mirror #15 | Sedang — jika sudah bersih, heuristik no-op (aman); jika ada, perlu dibersihkan |
| A4 | `RemoveExamOriginAsync` SaveChanges internal (:127) aman dipanggil di tengah cascade tx | Cascade engine step 8 | Sedang — urutan SaveChanges dalam tx perlu hati-hati; test integration buktikan |
| A5 | Opsi (B) separasi image-vs-sertifikat tidak menyebabkan dobel/miss cleanup | Overlap 366 | Rendah — image (soal) & sertifikat (manual) field berbeda, jalur berbeda |

---

## Open Questions

1. **Pola ActionUrl `/CMP/Certificate/{id}` (spec) tidak ditemukan di kode.**
   - Diketahui: Hanya `/CMP/StartExam/{id}` aktif + 2 template dormant.
   - Tidak jelas: Apakah user pernah lihat pola Certificate (versi lama?).
   - Rekomendasi: Konservatif — sertakan hanya pola terbukti (`StartExam` + opsional 2 template). Konfirmasi user saat plan.

2. **RemoveExamOriginAsync SaveChanges internal di tengah cascade tx.**
   - Diketahui: Helper :127 `await _context.SaveChangesAsync()` — flush partial dalam tx caller.
   - Tidak jelas: Apakah aman flush sebelum cascade selesai (kemungkinan ya — masih dalam tx, belum commit).
   - Rekomendasi: Test integration assert rollback utuh saat exception setelah RemoveExamOrigin.

3. **DeleteAssessmentGroup sibling filter (#18) — definisi exact filter.**
   - Diketahui: Spec §3.3 sebut `LinkedGroupId == null && AssessmentType bukan PreTest/PostTest && !IsManualEntry` (samakan scope tab 1 `:155-156`).
   - Tidak jelas: Apakah filter ini benar semua kasus grup (verifikasi terhadap query tab-1 yang menampilkan grup).
   - Rekomendasi: Verifikasi tab-1 list query saat plan; test `SiblingFilterTests` cover over-match.

4. **ResetAssessment line aktual (#20)** — verifikasi saat plan (A2).

---

## Sources

### Primary (HIGH confidence)
- Codebase read (2026-06-12): `AssessmentAdminController.cs` (DeleteAssessment/Group/PrePost + 366 image), `TrainingAdminController.cs` (DeleteTabResult/Training/ManualAssessment/AddManual/Import/BulkBackfill), `CMPController.cs` (LinkedSessionId), `ProtonCompletionService.cs` (RemoveExamOriginAsync), `ApplicationDbContext.cs` (FK config), `WorkerDataService.cs` (badge), `NotificationService.cs` (templates), `Models/*` (AssessmentSession, TrainingRecord, ProtonModels, WorkerTrainingStatus), `_TrainingRecordsTab.cshtml` (seam), `Program.cs` (DI), `HcPortal.Tests/*` (fixtures).
- Spec C `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` — §1-3.5.
- CONTEXT.md + UI-SPEC.md (367).
- SUMMARY 366 (01/02/03) + SUMMARY 371 (01).

### Secondary (MEDIUM confidence)
- MEMORY.md entries (366 gotcha logger, 331 RenewsSessionId pitfall, 367 preserve helper).
- STATE.md / REQUIREMENTS.md (phase map, depends).

### Tertiary (LOW confidence)
- (none — semua claim di-verifikasi terhadap kode)

---

## Metadata

**Confidence breakdown:**
- Standard stack / reuse assets: HIGH — semua building-block ada + line di-verifikasi
- Drift verification: HIGH — anchor dibaca langsung 2026-06-12
- L-05 inventory: HIGH — grep eksaustif; DRIFT vs spec terdokumentasi
- Cascade engine arch: MEDIUM-HIGH — blueprint solid, urutan SaveChanges-in-tx perlu test (A4)
- Mirror legacy residu: MEDIUM — komentar implies, DB live tak di-query (A3)
- Validation: HIGH — pola test existing jelas

**Research date:** 2026-06-12
**Valid until:** 2026-06-19 (7 hari — file controller aktif disentuh fase paralel; re-verify line jika 364/368 land dulu)
