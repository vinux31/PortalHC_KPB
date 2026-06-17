# Phase 393: Backend core inject - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Bangun `Services/InjectAssessmentService.cs` (BARU) — orchestrator backend yang menyusun set sesi assessment manual **lengkap** per pekerja (`AssessmentSession` + `UserPackageAssignment` ber-`ShuffledQuestionIds` [anchor paket sentinel] + `PackageUserResponses` + `SessionElemenTeknisScore` + sertifikat opsional), di mana skor / kelulusan / elemen-teknis / nomor-sertifikat **dihitung** lewat **reuse** pipeline grading existing (`GradingService.GradeAndCompleteAsync` + jalur essay-finalize + `AssessmentScoreAggregator` + `CertNumberHelper`) — **nol duplikasi logic** → hasil byte-identik jalur online. Atomic per-batch, `IsManualEntry=true` + AuditLog `"ManualInject"`.

**Scope-lock:** xUnit only, **tidak ada UI** (page/controller/view = Phase 394+), **0 migration** (semua tabel sudah ada), tidak membuat engine grading/authoring baru. Service menerima **jawaban eksplisit per-soal** (model "input asli"); auto-generate dari skor target = Phase 395 (di atas service yang sama). Cakupan REQ: INJ-01, INJ-02.

</domain>

<decisions>
## Implementation Decisions

### Guard duplikat / anti-double-cert
- **D-01:** Bila pekerja yang di-inject **sudah punya sesi** yang cocok → **skip pekerja itu + lapor** (jangan gagalkan seluruh batch). Bukan soft-block, bukan hard-block. Konsisten pola `BulkBackfillAssessment` existing.
- **D-02:** Kunci deteksi duplikat **per-pekerja** = `UserId + Judul(ter-normalisasi) + Kategori + Tanggal(date)`. **Cert-aware:** bila mode generate-cert ON, juga cegah double-cert (judul/cert + tanggal sama). Normalisasi judul reuse `NormalizeTitleForDup` (`AdminBaseController.cs`). Sediakan tombol "Cek" mirip form CreateAssessment (`GET /Admin/CheckTitleAvailability`) — **catatan: tombol = UI Phase 394**; di 393 cukup logic dedup di service + endpoint check bila perlu.

### Perilaku batch (validasi & atomicity)
- **D-03:** Baris **invalid** (NIP tak dikenal di `AspNetUsers` / opsi tak valid / skor di luar range / essay tanpa skor / tanggal tak masuk akal) → **pre-flight validasi seluruh batch dulu**; bila **ada** baris invalid → **tolak SELURUH submit** + kembalikan **daftar error per-baris** yang jelas, **nol tulisan** (HC perbaiki & ulang). Pola `BulkBackfillAssessment` (validasi NIP up-front sebelum tx).
- **D-04:** Tulisan sesi yang valid dibungkus **satu transaction**; bila terjadi **error tak terduga** di tengah write → **rollback-all** (tidak ada sesi parsial). Transaction = safety-net, bukan jalur utama penolakan (penolakan input = pre-flight D-03). **Beda kategori:** duplikat = skip+lapor (D-01, sebelum tx); invalid = pre-flight tolak-semua (D-03); error tak terduga = rollback (D-04). Tidak saling bentrok.

### Essay
- **D-05:** Sesi essay yang di-inject **wajib** ber-`EssayScore`; setelah skor di-set + jalur essay-finalize dipanggil → `Status=Completed` (BUKAN tertinggal "Menunggu Penilaian"). Essay tanpa skor saat submit = **invalid** → masuk pre-flight tolak (D-03). (Risiko spec §13 essay-finalize ditutup.)

### Validasi nilai & tanggal (policy)
- **D-06:** Backdate `CompletedAt` / `Schedule` sesi inject **wajib ≤ hari ini** (inject = data historis ujian luring). Tanggal di luar akal (mis. masa depan / tahun absurd) → invalid (pre-flight tolak).
- **D-07:** `PackageUserResponse.EssayScore` valid pada range **0..ScoreValue** (bobot poin soal), lalu `AssessmentScoreAggregator` hitung persen — identik grading online. Di luar range → invalid (pre-flight tolak). **Bukan** 0..100.

### Sertifikat
- **D-08:** Cert **di-suppress bila pekerja TIDAK lulus** (`Score < PassPercentage`) walau toggle sertifikat ON — hanya `isPassed=true` yang dapat nomor. Mirror online: `GradingService` sudah gate cert pada `isPassed` → reuse otomatis konsisten.
- **D-09:** Mode cert **input manual**: nomor (`NomorSertifikat`) **wajib unik** — collision dengan `IX_AssessmentSessions_NomorSertifikat` (UNIQUE index) → invalid (pre-flight tolak) dgn pesan jelas. Cegah error DB + double nomor.
- **D-10:** `ValidUntil` (masa berlaku cert, tipe `DateOnly?`) **fleksibel**: HC boleh isi tanggal **atau** set **permanent** (`null` = berlaku selamanya). Berlaku untuk mode manual **dan** auto-generate (pilihan per-room).

### Audit / transparansi
- **D-11:** AuditLog cakupan **sukses + skip + reject**: (a) `ActionType="ManualInject"` per sesi **sukses** (actor, NIP, sessionId, skor) — count = jumlah sesi sukses (INJ-02, dikunci xUnit); (b) entri **ActionType terpisah** untuk pekerja yang di-**SKIP** duplikat (siapa & alasan); (c) entri **ActionType terpisah** untuk submit yang **DITOLAK** pre-flight (jejak percobaan). Pakai ActionType berbeda agar assertion "count ManualInject = jumlah sesi" tetap bersih.

### Claude's Discretion (teknis — diserahkan ke researcher/planner)
- **Mekanisme reuse essay-finalize:** `FinalizeEssayGrading` saat ini = controller action `[HttpPost][ValidateAntiForgeryToken]` (`AssessmentAdminController.cs:3637`), **bukan** service bersih. Service inject perlu memanggil logic finalize tanpa lewat HTTP — opsi: extract shared helper / service, atau replikasi logic data-level (pola xUnit Phase 387). Pilihan mekanisme = ranah planner; **wajib nol-duplikasi semantik** (status-transition + score-recompute + cert + audit harus identik).
- **Kontrak `InjectAssessmentService`:** menerima `(packageSpec/authored questions, room settings, List<workerAnswers eksplisit>)`. Bentuk DTO/signature = discretion.
- **Nilai field sesi inject:** `AccessToken="INJECT"`, `IsTokenRequired=false`, `IsManualEntry=true`, `AssessmentType` ∈ {Standard/PreTest/PostTest} (BUKAN "Manual" — agar grouping & `/CMP/Results` render seperti online; **researcher verifikasi** `/CMP/Results` & `GetUnifiedRecords` tidak branch khusus `AssessmentType="Manual"`), `AllowAnswerReview=true` (agar rincian per-soal tampil).
- **Anchor paket sentinel:** anchor `AssessmentPackage` ke sesi representatif room inject; tiap pekerja `UserPackageAssignment(AssessmentPackageId=sentinelPackage.Id, ShuffledQuestionIds, ShuffledOptionIdsPerQuestion="{}", SavedQuestionCount, IsCompleted=true)`. Pola `CMPController.cs:1048-1101`.
- **Alokasi cert dalam transaction:** generate nomor di dalam tx (rollback → nomor belum commit → ter-reclaim, no gap); tarik dari sekuens resmi sama dgn online via `CertNumberHelper.GetNextSeqAsync` (retry 3× anti-collision, gap sengaja boleh).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — design spec penuh: keputusan D-1..D-10, alur end-to-end (§5), data yang ditulis per sesi (§6), mode jawaban (§7), sertifikat (§8), audit/keamanan (§10), risiko (§13: essay-finalize, anchor paket, cert-target), asumsi (§14).
- `.planning/REQUIREMENTS.md` — INJ-01 (build session set full via reuse + atomic), INJ-02 (`IsManualEntry`+AuditLog `ManualInject`).
- `.planning/ROADMAP.md` — Phase 393 details + 5 Success Criteria (byte-identik MC/MA/Essay, rollback atomic, essay→Completed, AuditLog count, build/test/0-migration).

### Mesin grading di-reuse (nol duplikasi)
- `Services/GradingService.cs:57-336` — `GradeAndCompleteAsync(AssessmentSession session)`: baca `UserPackageAssignment`+`PackageUserResponses`, hitung Score/IsPassed, set Status (Completed non-essay / PendingGrading essay), insert `SessionElemenTeknisScore`, generate cert (retry 3×, gate `isPassed`, guard `NomorSertifikat==null` + status). **Tidak buka transaction sendiri** → caller atur atomicity batch.
- `Controllers/AssessmentAdminController.cs:3637-3871` — `FinalizeEssayGrading(int sessionId)`: validasi semua essay ter-skor, transisi `PendingGrading→Completed` via `ExecuteUpdateAsync`, recompute via aggregator, generate cert, audit. (Controller action — lihat Claude's Discretion soal mekanisme reuse.)
- `Helpers/AssessmentScoreAggregator.cs` — `Compute(questions, responses, passPercentage)` hitung persen + IsPassed; `IsQuestionCorrect` (essay `>0`=benar).
- `Helpers/CertNumberHelper.cs` — `GetNextSeqAsync(ctx, year)` (MAX+1 dari `NomorSertifikat` ending `/year`), `Build(seq, date)` → `KPB/{seq:D3}/{ROMAN}/{year}`, `IsDuplicateKeyException(ex)`.

### Pola yang ditiru
- `Controllers/TrainingAdminController.cs:836-985` — `BulkBackfillAssessment`: pola atomic batch (`BeginTransactionAsync`/try-commit/catch-rollback), **pre-validate semua NIP up-front** (`:889-899`), dedup `(UserId,Title,CompletedAt)` skip+lapor (`:914-925`), audit in-tx via `_context.AuditLogs.Add` (`:960-969`). **Tool ini di-retire di Phase 396.**
- `Controllers/CMPController.cs:1048-1101` — anchor sentinel `UserPackageAssignment` (AssessmentPackageId=first package, ShuffledQuestionIds JSON, idempotency guard).
- `Controllers/AdminBaseController.cs:276-293` — `FindTitleDuplicatesAsync` + `NormalizeTitleForDup`; usage soft-block `AssessmentAdminController.cs:990-1004`; endpoint `GET /Admin/CheckTitleAvailability` (`:846`). (Dedup inject = per-pekerja D-02, tombol Cek = UI 394.)

### Model & infra
- `Models/AssessmentSession.cs` — field inject: `IsManualEntry`(bool), `AssessmentType`(string?), `AccessToken`(string), `IsTokenRequired`(bool), `Schedule`(DateTime), `StartedAt`/`CompletedAt`(DateTime?), `Status`(string), `Score`(int?), `IsPassed`(bool?), `PassPercentage`(int), `GenerateCertificate`(bool), `NomorSertifikat`(string?, UNIQUE), `ValidUntil`(DateOnly?), `AllowAnswerReview`(bool), `LinkedGroupId`/`LinkedSessionId`(int?), `DurationMinutes`(int), `HasManualGrading`(bool).
- `Models/AuditLog.cs` + `AuditLogService.LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` — fields `ActorUserId`/`ActorName`/`ActionType`(MaxLength 50)/`Description`/`TargetId`/`TargetType`/`CreatedAt`. In-tx: pakai `_context.AuditLogs.Add` (bukan `LogAsync` yang SaveChanges sendiri).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GradingService.GradeAndCompleteAsync` — engine skor/lulus/ET/cert. **Tidak transaksional** → caller (service inject) WAJIB bungkus batch dalam transaction sendiri.
- `CertNumberHelper` (GetNextSeqAsync/Build/IsDuplicateKeyException) — alokasi nomor cert resmi, retry anti-collision, gap sengaja boleh.
- `BulkBackfillAssessment` — blueprint atomic + pre-validate-NIP + dedup skip + audit-in-tx (tiru, lalu retire di 396).
- Anchor sentinel `UserPackageAssignment` (CMPController) — cara assign paket ke banyak pekerja tanpa schema baru.
- `FindTitleDuplicatesAsync` / `NormalizeTitleForDup` — normalisasi judul untuk dedup.

### Established Patterns
- Grading **by question ID** via `ShuffledQuestionIds` (bukan by session) → Results & GradingService load soal dari assignment, bukan dari sesi. Inject cukup set `ShuffledQuestionIds` benar.
- Cert generation: retry 3× + guard `WHERE NomorSertifikat==null AND Status NOT IN (terminal)` (race-safe, anti-double-cert).
- Essay: non-essay → `Completed`; ada essay → `PendingGrading` lalu finalize → `Completed`. Inject WAJIB panggil finalize (D-05).
- Audit in-tx via direct `_context.AuditLogs.Add` + SaveChanges di akhir (BulkBackfill); `LogAsync` SaveChanges sendiri (jangan dipakai di tengah tx batch).

### Integration Points
- `Services/WorkerDataService.cs:28` `GetUnifiedRecords` **tidak** filter `IsManualEntry` → sesi inject muncul di `/CMP/Records` label "Assessment Online" **gratis** (tak ada kode visibility baru di 393).
- `Controllers/CMPController.cs:2184` `Results` render rincian per-soal bila ada assignment + responses + paket + `AllowAnswerReview=true` → inject harus penuhi 4 syarat ini.
- Service inject akan dikonsumsi controller/page Phase 394 + diperluas auto-gen Phase 395 + Excel Phase 396 + link Pre/Post Phase 397 (semua share file ini → 395/396/397 sequential).

</code_context>

<specifics>
## Specific Ideas

- "Dibilang sama jika judul room / certificate (jika on generate certif) dan tanggal sama" → dedup cert-aware (D-02).
- "Ada tombol untuk check, seperti di form create assessment" → reuse pola `CheckTitleAvailability` (UI 394).
- ValidUntil: "HC bisa isi valid until atau set ke permanent" → `null` = permanent (D-10).
- Prinsip menyeluruh: hasil inject **byte-identik** jalur online (reuse mesin, nol duplikasi) — jangan menulis skor/lulus/cert dengan tangan.

</specifics>

<deferred>
## Deferred Ideas

- **Essay boleh "Menunggu Penilaian"** (grader manusia selesaikan nanti via `/Admin/EssayGrading`) — ditolak untuk 393 (D-05 wajib Completed). Bila muncul kebutuhan nyata → milestone berikut.
- **Partial-batch** (inject valid, skip+lapor invalid untuk baris non-duplikat) — ditolak; baris invalid = pre-flight tolak-semua (D-03). Skip+lapor HANYA untuk duplikat (D-01).
- **Auto-generate jawaban dari skor target** — Phase 395 (di atas `InjectAssessmentService` yang sama), bukan 393.
- **Import Excel batch** — Phase 396. **Link Pre/Post silang** — Phase 397. **Page/UI** (controller+view+kartu Section C+tombol Cek+worker picker+toggle cert) — Phase 394.
- **Multi-paket variasi per room** / **import gambar via Excel** / **edit massal sesi inject** — out of scope milestone (REQUIREMENTS Future).

</deferred>

---

*Phase: 393-backend-core-inject*
*Context gathered: 2026-06-17*
