# Phase 410: Add-Participant Backend Live - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Backend penambahan peserta live (AJAX) untuk milestone v32.5. Deliverable Phase 410:
1. **Endpoint `AddParticipantsLive` (HttpPost)** — `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`. Resolve batch dari sesi representatif → guard window → idempotent skip → buat `AssessmentSession` + `UserPackageAssignment` ber-status siap-mulai (`DeriveReadyStatus`) dalam 1 transaksi atomic → Pre/Post pair → tolak Proton → return JSON baris baru.
2. **Endpoint `GetEligibleParticipantsToAdd` (HttpGet)** — picker user yang bisa ditambah.
3. **Helper bersama** pembuatan sesi+assignment (refactor dari jalur create batch existing; pola atomic mirip `InjectAssessmentService`).

**TIDAK** termasuk: remove/restore (411), SignalR wiring + UI panel (412), test+UAT (413). Mengonsumsi fondasi 409 (definisi sesi aktif `RemovedAt==null`, exclude-removed query).

</domain>

<decisions>
## Implementation Decisions

### Re-add peserta soft-removed (D-01) — area dibahas
- **D-01:** Picker **EXCLUDE** user yang punya sesi **APAPUN** di batch (aktif `RemovedAt==null` **maupun** removed `RemovedAt!=null`). User yang sudah di-soft-remove **hanya** bisa balik lewat **Restore** (panel 412 → endpoint `RestoreParticipantLive` 411), **bukan** lewat Add. → `AddParticipantsLive` tetap murni "tambah user baru"; tak ada dobel sesi; history utuh.
- **Implikasi query:** `GetEligibleParticipantsToAdd` filter = user yang **belum punya sesi sama sekali** di batch (cek existence by `UserId` + batchKey, tanpa pandang `RemovedAt`). Idempotency `AddParticipantsLive` (skip sesi aktif) tetap berlaku sebagai guard server kedua.

### Cakupan picker eligible (D-02) — area dibahas — ⚠️ OVERRIDE spec §B4
- **D-02:** Picker tampilkan **SEMUA pekerja eligible** yang belum punya sesi di batch — **TANPA** batasan unit/section. Admin bebas tambah pekerja mana saja.
- **⚠️ Deviasi sadar dari spec §B4** yang menulis "reuse query eligible existing (unit/section scope)". User secara eksplisit memilih scope lebih luas (opsi "Semua pekerja eligible — admin bebas tambah siapa saja"). Planner: **JANGAN** batasi ke unit/section assign-awal; eligible = seluruh pekerja aktif (mis. `User`/`AspNetUsers` role pekerja) minus yang sudah punya sesi di batch (per D-01).
- **Catatan multi-unit:** karena tak ada filter unit/section, isu multi-unit (UserUnits v32.3) jadi moot untuk eligibility — semua pekerja kandidat. (Verifikasi planner: pastikan daftar pekerja sumbernya benar + tidak ikutkan akun non-pekerja/admin bila tak relevan — Claude discretion.)

### Feedback duplikat di-skip (D-03) — area dibahas
- **D-03:** JSON response `AddParticipantsLive` kembalikan **`added[]` + `skipped[]`** dengan **nama + NIP** tiap entri (bukan sekadar count). UI 412 bisa toast "X ditambah, Y dilewati (sudah terdaftar)". Sertakan juga `addedCount`/`skippedCount` untuk ringkas.

### Broadcast SignalR (D-04) — area dibahas
- **D-04:** **DEFER** wiring SignalR `participantAdded` ke **Phase 412**. Endpoint 410 **cukup** return JSON baris baru (`id, fullName, nip, status`). Broadcast `participantAdded` ke `monitor-{batchKey}` + handler client ditambah bareng di 412. Batas fase bersih; 410 fokus backend.

### Carry-forward dari spec & 409 (LOCKED, bukan dibahas ulang)
- **Guard window:** `ExamWindowCloseDate` di-set & `DateTime.UtcNow.AddHours(7) > ExamWindowCloseDate` → tolak **400** "Window ujian sudah tutup, tidak bisa tambah peserta." Window null = bebas. (spec §B1.2)
- **Status sesi baru:** `DeriveReadyStatus(schedule, window)` → Open/Upcoming, **BUKAN** InProgress. `StartedAt/CompletedAt/RemovedAt = null`. (spec §B1.4, PART-06)
- **Inherit fields** dari representatif: `Title/Category/Schedule/DurationMinutes/PassPercentage/Shuffle*/GenerateCertificate/AllowAnswerReview/AssessmentType/LinkedGroupId`. (spec §B1.4)
- **`UserPackageAssignment`** cermin paket batch; **transaksi atomic** (gagal buat assignment → rollback seluruh request). (spec §B1.4, §G)
- **Pre/Post:** batch Pre/Post → buat **pasangan Pre+Post** (reuse cabang `:1926`). (PART-07)
- **Proton reject:** `Category == "Assessment Proton"` → tolak dengan pesan jelas. (spec §F)
- **Notif + audit:** notif `ASMT_ASSIGNED` existing + audit `AddParticipantLive`. (spec §B1.5)
- **Soft-removed ⇔ `RemovedAt != null`** (fondasi 409); batch = `Title+Category+Schedule.Date`.

### Claude's Discretion
- Bentuk param endpoint: representative `sessionId` vs `batchKey` (rekomendasi: `sessionId` representatif, resolve batch key darinya).
- Sumber daftar pekerja eligible (tabel/role query) + exclude akun non-pekerja — selama hasil = "semua pekerja minus yang sudah punya sesi di batch" (D-02).
- Ekstraksi helper bersama sesi+assignment dari jalur create batch existing (`EditAssessment` BULK ASSIGN / pola `InjectAssessmentService`) — bentuk signature & lokasi.
- Cakupan integration test (minimal: ready-status, idempotent, window-tolak, Pre/Post pair, Proton tolak, eligible exclude-by-batch).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — **§B1** AddParticipantsLive (langkah 1-7), **§B4** GetEligibleParticipantsToAdd (⚠️ D-02 override scope), **§F** Proton out, **§G** error handling, **§H** security. Keputusan LOCKED #4 eligibility longgar.
- `.planning/REQUIREMENTS.md` — **PART-06** (ready-status + window-reject + idempotent) + **PART-07** (Pre/Post pair).
- `.planning/ROADMAP.md` — Phase 410 §"Phase Details" (Goal + Success Criteria + Files).
- `.planning/phases/409-data-foundation-re-entry-guards-exclude-removed-query/409-CONTEXT.md` — definisi sesi aktif/removed (D-01/D-04 409), guard re-entry yang dikonsumsi.

### Production code (file:line, dari spec File Rujukan)
- `Controllers/AssessmentAdminController.cs` — `EditAssessment` POST `NewUserIds` (`:1794`, jalur add existing parsial), guard `:2006`/`:2092`, cabang Pre/Post `:1926`, `DeriveUserStatus` `:2715`, Monitoring `:2815`/`:3273`. **Endpoint baru `AddParticipantsLive` + `GetEligibleParticipantsToAdd` ditambah di sini.**
- `Services/InjectAssessmentService.cs` — pola transaksi atomic create sesi+assignment+cert (template helper bersama).
- `Models/AssessmentSession.cs` — entitas sesi (3 kolom removal dari 409 + field inherit).
- `Models/AssessmentConstants.cs` (`:13-22`) — enum status (Open/Upcoming target ready-status).
- `Data/ApplicationDbContext.cs` — `UserPackageAssignment` + relasi.
- `DeriveReadyStatus` — helper status siap-mulai (Phase 391 existing; cari di `AssessmentAdminController`).

### Lingkungan / workflow
- `CLAUDE.md` — Develop Workflow (migration=FALSE untuk 410; build+run+DB lokal sebelum commit) + Seed Workflow.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`EditAssessment` POST `NewUserIds`** (`:1794`) — jalur add peserta existing (parsial, full-page). Logika resolve user + buat sesi bisa jadi basis helper bersama `AddParticipantsLive`.
- **`InjectAssessmentService`** — pola transaksi atomic (buat sesi + assignment + finalize) byte-konsisten; template untuk helper atomic add.
- **`DeriveReadyStatus`** (Phase 391) — turunkan Open/Upcoming dari schedule+window; pakai langsung untuk status sesi baru.
- **Cabang Pre/Post** (`:1926`) — pasangan Pre+Post saat create; reuse untuk PART-07.

### Established Patterns
- **Atomic tx + rollback** (`InjectAssessmentService`, Delete cascade): bungkus create sesi+assignment dalam 1 transaksi; gagal → rollback (spec §G).
- **RBAC + antiforgery** (`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`) pada endpoint mutasi admin existing.
- **Idempotency by existence check** — query sesi aktif by `UserId`+batchKey sebelum insert.

### Integration Points
- Konsumsi fondasi 409: definisi sesi aktif `RemovedAt==null` (idempotency D-01) + eligible exclude-by-batch.
- Output (JSON baris baru) dikonsumsi **412** (DOM inject + SignalR wiring).
- `GetEligibleParticipantsToAdd` → picker UI 412.
- Helper bersama dipakai juga oleh `RemoveParticipantLive`/jalur create existing (hindari duplikasi).

</code_context>

<specifics>
## Specific Ideas

- Pesan window-tolak locked: **"Window ujian sudah tutup, tidak bisa tambah peserta."** (400)
- Status sesi baru WAJIB ready (Open/Upcoming), **NEVER** InProgress — peserta baru mulai fresh.
- ⚠️ **D-02 sengaja lebih longgar dari spec §B4**: picker = semua pekerja (tanpa unit/section). Planner catat sebagai keputusan user eksplisit, bukan bug.
- Re-add user removed = lewat **Restore** (411), bukan Add (D-01).

</specifics>

<deferred>
## Deferred Ideas

- **SignalR `participantAdded` broadcast + handler client** — Phase 412 (D-04; 410 cuma return JSON).
- **`RemoveParticipantLive` / `RestoreParticipantLive` / perbaiki `DeleteAssessmentPeserta`** — Phase 411.
- **Panel "Peserta Dikeluarkan" + picker UI + modal** — Phase 412.
- **Playwright e2e + xUnit suite lengkap** — Phase 413 (410 cukup integration minimal endpoint).
- **Filter eligible by unit/section** — sengaja TIDAK dilakukan (D-02 override); buka hanya bila muncul kebutuhan nyata batasi scope tambah.

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` (skor 0.6) — cleanup data test lokal pasca-367; bukan scope 410. Tetap di backlog.

</deferred>

---

*Phase: 410-add-participant-backend-live*
*Context gathered: 2026-06-21*
