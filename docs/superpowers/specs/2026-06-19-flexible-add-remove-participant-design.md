# Flexible Add/Remove Participant Saat Ujian Berjalan — Design Spec

- **Tanggal:** 2026-06-19
- **Branch target:** `main` (v32.2 base; punya semua guard Phase 391/398.1)
- **Migration:** TRUE (`AddParticipantRemovalColumns` — 3 kolom nullable, additif)
- **Surface:** Live di halaman Monitoring Detail (AJAX + SignalR)

## Latar Belakang & Tujuan

Admin/HC butuh fleksibilitas menambah **dan** menghapus peserta assessment **walaupun ujian sedang berlangsung** (ada sesi InProgress di batch yang sama), langsung dari layar Monitoring tempat mereka mengawasi ujian live.

### Model data (existing)
- Tiap peserta = **satu baris** `AssessmentSession` (FK `UserId`).
- "Batch" ujian = sesi yang berbagi `Title + Category + Schedule.Date`.
- Status `InProgress` = **turunan**, bukan kolom: `StartedAt != null && CompletedAt == null` (`DeriveUserStatus` `AssessmentAdminController.cs:2715`).
- Enum status: Open, Upcoming, Completed, PendingGrading (`Menunggu Penilaian`), InProgress, Cancelled (`Dibatalkan`), Abandoned (`AssessmentConstants.cs:13-22`).

### State saat ini (hasil audit kode)
- **ADD** sudah berfungsi parsial via `EditAssessment` POST param `NewUserIds` (`AssessmentAdminController.cs:1794`). Phase 391 izinkan tambah meski ada sesi Completed/PendingGrading (carve-out `hasAddition` `:2006`), dan lindungi sesi berjalan dari overwrite field volatil (D-03 skip `:2092`). InProgress hanya munculkan notif Info, tidak blokir.
- **DELETE 1 peserta = TIDAK ADA backend.** View `EditAssessment.cshtml:666` POST ke `DeleteAssessmentPeserta` yang **tidak diimplementasi** di controller (grep = 0). Tombol hapus per-peserta = mati. Proteksi InProgress cuma di UI (`CanDelete` `:1710`); server-side `EnsureCanDeleteAsync` `:7203` hanya blok HC kalau Completed/ada-response — **tidak cek InProgress**.
- Hapus yang ada cuma level sesi/grup: `DeleteAssessment` `:2285`, `DeleteAssessmentGroup` `:2420`, `DeletePrePostGroup` `:2566`. Cascade berat via `RecordCascadeDeleteService.ExecuteAsync` `:175-314` (9 tabel + file sertifikat, 1 tx). Komplikasi: `AssessmentAttemptHistory` FK ke **User**, bukan Session (`ApplicationDbContext.cs:552`).
- Monitoring Detail (`:3273`) render baris per-sesi (`data-session-id`) + dropdown Aksi. SignalR `AssessmentHub` push ke grup `monitor-{batchKey}` (`workerStarted/workerSubmitted/progressUpdate`, handler `AssessmentMonitoringDetail.cshtml:1199-1400`). **Belum ada** event `participantAdded/Removed`, endpoint AJAX add/remove, atau force-disconnect (cuma `LeaveMonitor` no-op).

## Keputusan Desain (LOCKED)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| 1 | Surface UX | **Live di Monitoring Detail** (AJAX + SignalR), bukan reload EditAssessment |
| 2 | Semantik hapus | **Hybrid by-state**: belum-mulai → hard-delete; sudah-ada-data → soft-remove + arsip |
| 3 | Hapus saat aktif | **Konfirmasi keras + force-kick live** (event SignalR `examRemoved`) |
| 4 | Eligibility tambah | **Longgar + guard wajar** (cek window, idempotent, auto PackageAssignment, ready status) |
| 5 | RBAC | **Admin+HC penuh** (termasuk hapus peserta Completed/bersertifikat) |
| 6 | Model soft-remove | **3 kolom removal** (`RemovedAt/RemovedBy/RemovalReason`), migration=TRUE |

## Arsitektur & Komponen

### A. Data Model (migration=TRUE)
Tambah ke `Models/AssessmentSession.cs`:
- `public DateTime? RemovedAt { get; set; }` — UTC. null = aktif; non-null = soft-removed (**sumber kebenaran "removed"**).
- `public string? RemovedBy { get; set; }` — userId Admin/HC pelaku.
- `public string? RemovalReason { get; set; }` — alasan opsional dari modal.

Migration `AddParticipantRemovalColumns`: 3 kolom nullable, additif, tanpa default destruktif. **migration=TRUE → notify IT** dengan commit hash + flag.

Definisi: sesi **soft-removed** ⇔ `RemovedAt != null`. Sesi **aktif** ⇔ `RemovedAt == null`.

### B. Endpoint baru — `AssessmentAdminController` (RBAC `[Authorize(Roles="Admin, HC")]`, `[ValidateAntiForgeryToken]`)

**B1. `AddParticipantsLive` (HttpPost)** — param: representative `sessionId` (atau batchKey) + `userIds[]`.
1. Resolve batch (`Title+Category+Schedule.Date`) dari sesi representatif.
2. **Guard window:** kalau `ExamWindowCloseDate` di-set dan `DateTime.UtcNow.AddHours(7) > ExamWindowCloseDate` → tolak (400, "Window ujian sudah tutup, tidak bisa tambah peserta."). Window null = bebas.
3. **Idempotency:** skip `userId` yang sudah punya sesi **aktif** (`RemovedAt==null`) di batch. Report jumlah di-skip.
4. Per user baru (dalam satu transaksi):
   - Buat `AssessmentSession`: warisi `Title/Category/Schedule/DurationMinutes/PassPercentage/Shuffle*/GenerateCertificate/AllowAnswerReview/AssessmentType/LinkedGroupId` dari representatif. `Status = DeriveReadyStatus(schedule, window)` (Open/Upcoming, **bukan** InProgress). `StartedAt/CompletedAt = null`. `RemovedAt = null`.
   - Buat `UserPackageAssignment` cermin paket batch. **Refactor helper bersama** dari jalur create batch existing (hindari duplikasi; pola atomic mirip `InjectAssessmentService`).
   - Kalau batch Pre/Post → buat pasangan Pre+Post (reuse cabang `:1926`).
5. Notif `ASMT_ASSIGNED` (existing) + audit `AddParticipantLive`.
6. Return JSON baris baru (`id, fullName, nip, status`) untuk DOM inject.
7. **Setelah commit**, broadcast SignalR `participantAdded` ke grup `monitor-{batchKey}`.

**B2. `RemoveParticipantLive` (HttpPost)** — param: `sessionId` + `reason`.
1. Load sesi (404 kalau tak ada). Idempotent: kalau `RemovedAt != null` → no-op sukses.
2. **Tentukan jalur (hybrid):**
   - **Belum mulai + tanpa data** (`StartedAt == null` & 0 `PackageUserResponse`) → **hard-delete** sesi tunggal via `RecordCascadeDeleteService.ExecuteAsync` (single root). Bersih, tanpa jejak.
   - **Sudah mulai / Completed / ada data** → **soft-remove**: set `RemovedAt=UtcNow`, `RemovedBy`, `RemovalReason`. **JANGAN sentuh** `Score`, `IsPassed`, `NomorSertifikat`, file sertifikat, response.
3. Kalau peserta InProgress saat dihapus → push SignalR `examRemoved` (payload `reason`) ke worker (target user/grup batch).
4. **Pre/Post:** kalau sesi linked Pre/Post → perlakukan **pasangan sebagai satu unit** (jaga konsistensi, jangan tinggalkan setengah pasangan). Evaluasi gabungan: kalau **salah satu** dari Pre/Post sudah punya data (`StartedAt != null` atau ada response) → **soft-remove keduanya**; kalau **kedua-duanya** belum-mulai + tanpa data → **hard-delete keduanya** (mirror partner-handling `DeletePrePostGroup`).
5. Audit `RemoveParticipantLive` (flag hard/soft + reason). Return JSON outcome.
6. **Setelah commit**, broadcast SignalR `participantRemoved` (payload `sessionId`) ke `monitor-{batchKey}`.

**B3. `RestoreParticipantLive` (HttpPost)** — param: `sessionId`. RBAC Admin,HC. Set `RemovedAt=null` (+ clear `RemovedBy/RemovalReason`), audit `RestoreParticipantLive`, broadcast `participantAdded`. Hanya berlaku untuk sesi soft-removed (hard-deleted tak bisa restore).

**B4. `GetEligibleParticipantsToAdd` (HttpGet)** — return user yang belum punya sesi aktif di batch, untuk picker tambah. Reuse query eligible existing (unit/section scope) yang dipakai assign awal.

**B5. Perbaiki `DeleteAssessmentPeserta`** (tombol mati `EditAssessment.cshtml:666`) → delegasi ke service yang sama dengan `RemoveParticipantLive` (biar tak broken; full-page redirect varian).

### C. SignalR — `Hubs/AssessmentHub.cs` + `Views/Admin/AssessmentMonitoringDetail.cshtml`
- **Server→monitor:** `participantAdded` (data baris), `participantRemoved` (sessionId). Dikirim dari controller setelah commit ke grup `monitor-{batchKey}`.
- **Server→worker:** `examRemoved` (reason) → handler client kunci UI ujian + redirect ke halaman "Anda dikeluarkan". Reuse pola `examClosed` (`AkhiriUjian` `:4430`).
- **JS Monitoring Detail:** handler inject baris `<tr data-session-id>` baru, pindahkan baris removed ke section "Peserta Dikeluarkan", update summary count (mirror DOM logic `:1199-1400`).
- Tidak perlu force-disconnect koneksi fisik; cukup `examRemoved` + redirect client + guard server (lihat E).

### D. Filter query (exclude removed) + panel Restore
Semua list/count peserta **exclude `RemovedAt != null`**:
- `AssessmentMonitoring` `:2815`, `AssessmentMonitoringDetail` `:3273` (+ `InProgressCount`).
- Tab grouping `ManageAssessmentTab_Assessment` `:179` (group status & count).
- Hasil/grading list, cert count, pass-rate.

UI: panel collapsible **"Peserta Dikeluarkan"** di Monitoring Detail menampilkan sesi soft-removed (nama, waktu, oleh siapa, alasan) + tombol **Restore** → payoff reversibilitas.

### E. Guard re-entry (KRITIS — anti resubmit peserta yang dihapus)
- `CMPController.StartExam` (`:974`): `if (session.RemovedAt != null)` → block ("Anda telah dikeluarkan dari ujian ini.").
- `CMPController.SubmitExam` (`:1573`): guard sama sebelum grading (cegah submit sesi removed).
- `AssessmentHub.JoinBatch` (`:21`): tolak join kalau `RemovedAt != null`.

### F. Scope
- **IN:** batch standar + Pre/Post.
- **OUT:** Proton (`Category == "Assessment Proton"`) — punya state `ProtonTrack`; add/remove live dikecualikan agar tak korup track. Endpoint tolak sesi Proton dengan pesan jelas.

### G. Error Handling
- Add: window tutup → 400 + pesan; duplikat → skip diam + report count; gagal buat assignment → rollback transaksi (atomic per request).
- Remove: not found → 404; sudah removed → idempotent no-op sukses; cascade fail → rollback (`RecordCascadeDeleteService` sudah tx).
- **SignalR broadcast HANYA setelah `CommitAsync` sukses** (hindari notif untuk tx yang rollback).

### H. Security
- Semua endpoint `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`.
- Keputusan #5 **melonggarkan** `EnsureCanDeleteAsync` untuk kasus HC menghapus peserta Completed/bersertifikat. Mitigasi: (a) modal konfirmasi keras di UI, (b) audit wajib `RemovedBy` + `RemovalReason` di tiap remove, (c) soft-remove (bukan hard-delete) untuk peserta bersertifikat → sertifikat tetap utuh & reversibel.
- Re-entry guard (E) mencegah peserta yang dihapus melanjutkan/submit ujian.

## Testing

### xUnit Integration (pola `FlexibleParticipantAddTests`, DB disposable `HcPortalDB_Test_{guid}`)
- Add-live: sesi baru ready-status (Open/Upcoming, bukan InProgress) + PackageAssignment tercipta.
- Add-live idempotent: user yang sudah aktif di batch → di-skip.
- Add-live window tutup → ditolak.
- Remove not-started → hard-delete (baris hilang dari DB).
- Remove in-progress → soft-remove (`RemovedAt` set; response & `Score` utuh).
- Remove completed-certified → soft-remove; `NomorSertifikat` + file preserved; excluded dari list aktif.
- Removed worker `StartExam`/`SubmitExam` → blocked.
- Restore → `RemovedAt=null`, muncul lagi di list aktif.
- Pre/Post: add buat pasangan; remove soft-remove kedua sesi pasangan.
- Proton sesi → endpoint tolak.

### Playwright e2e (Monitoring Detail live)
- Tambah peserta dari Monitoring → baris muncul live tanpa reload.
- Hapus peserta (modal konfirmasi keras) → baris pindah ke section "Peserta Dikeluarkan".
- Worker yang sedang ujian → kena layar kick (`examRemoved`) saat dihapus.

## Out of Scope (YAGNI)
- Self-service enrolment (peserta daftar sendiri) — Admin/HC only.
- Bulk import peserta live (hanya picker manual).
- Proton add/remove live.
- Notifikasi email ke peserta yang dihapus.

## Branch & Deploy
- Bangun di `main`. Semua guard Phase 391/398.1 sudah ada di main.
- migration=TRUE → setelah merge, **notify IT** dengan commit hash + flag migration.
- Jangan tarik ITHandoff→main tanpa cherry-pick guard (ITHandoff kehilangan guard Phase 391/398.1).

## File Rujukan
`Controllers/AssessmentAdminController.cs` (EditAssessment `:1794`, guard `:2006`/`:2092`, Delete* `:2285`/`:2420`/`:2566`, `EnsureCanDeleteAsync` `:7203`, `DeriveUserStatus` `:2715`, Monitoring `:2815`/`:3273`, AkhiriUjian `:4379`), `Views/Admin/EditAssessment.cshtml:666`, `Views/Admin/AssessmentMonitoringDetail.cshtml:1199-1400`, `Services/RecordCascadeDeleteService.cs:175-314`, `Hubs/AssessmentHub.cs`, `Controllers/CMPController.cs:953,974,1573`, `Services/GradingService.cs:224,263`, `Models/AssessmentConstants.cs:13-22`, `Models/AssessmentSession.cs`, `Data/ApplicationDbContext.cs:461-552`.
