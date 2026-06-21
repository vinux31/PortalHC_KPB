# Phase 409: Data Foundation + Re-entry Guards + Exclude-Removed Query - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Fondasi data + invarian soft-remove untuk milestone v32.5. Deliverable Phase 409:
1. **Migration `AddParticipantRemovalColumns`** — 3 kolom nullable additif di `AssessmentSession`: `RemovedAt DateTime?`, `RemovedBy string?`, `RemovalReason string?`. **migration=TRUE.**
2. **Definisi tunggal** sumber-kebenaran: sesi **soft-removed ⇔ `RemovedAt != null`** / **aktif ⇔ `RemovedAt == null`**.
3. **Guard re-entry** anti-resubmit di `CMPController.StartExam`/`SubmitExam` + `AssessmentHub.JoinBatch` — peserta yang dihapus tak bisa lanjut/submit/re-join.
4. **Exclude-removed query** — daftar & perhitungan peserta **aktif** mengecualikan `RemovedAt != null`.

Fondasi ini dikonsumsi Phase 410 (idempotency add cek sesi aktif), 411 (set/clear kolom removal), 412 (panel "Peserta Dikeluarkan" + exclude-from-count UI). **TIDAK** termasuk endpoint add/remove/restore (410/411), UI/SignalR (412), atau test+UAT (413).

</domain>

<decisions>
## Implementation Decisions

### Cakupan exclude-removed (D-01) — area dibahas
- **D-01:** Exclude `RemovedAt != null` **HANYA di surface admin batch-aggregate** yang terdaftar di spec §D — `AssessmentMonitoring` (`:2815`), `AssessmentMonitoringDetail` (`:3273`, termasuk `InProgressCount`), grouping `ManageAssessmentTab_Assessment` (`:179`, group status & count), serta jalur hasil/grading-list + cert-count + pass-rate **dalam konteks hasil-batch**.
- **D-01a:** **JANGAN** sentuh `/CMP/Records` pekerja / `WorkerDataService.GetUnifiedRecords` / sertifikat pekerja di Phase 409. Sesi soft-removed yang bersertifikat **tetap** jadi record historis utuh & tetap tampil di riwayat pekerja — selaras prinsip "sertifikat utuh & reversibel". Blast-radius Phase 409 sengaja minimal (hanya surface admin aktif).
- **Rasional:** spec §D hanya menyebut surface monitoring/hasil-batch; worker-facing records di luar daftar. Membatasi exclude ke admin-aktif menghindari risiko over-exclude (tercatat di STATE Open Concern (c)) sekaligus menjaga visibilitas sertifikat pekerja.

### UX guard re-entry (D-02) — area dibahas
- **D-02:** Saat sesi `RemovedAt != null` mencoba `StartExam`/`SubmitExam` → **ikut konvensi block existing**: `TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."; return RedirectToAction("Assessment");` (pola identik dengan block window-close / Abandoned / durasi-0 yang sudah ada di `StartExam`). **Tidak** ada view/halaman dedicated baru di Phase 409.
- **D-02a:** `SubmitExam` — guard ditempatkan **sebelum grading** (setelah load sesi, dekat `if (assessment == null) return NotFound();` `:1583`): jawaban yang dikirim setelah penghapusan **di-discard**, redirect + pesan. Pesan locked.
- **Catatan lintas-fase:** halaman dedicated "Anda dikeluarkan" untuk force-kick live (`examRemoved`) adalah scope **Phase 412** (SignalR client redirect), bukan 409. 409 = guard server-side saja.

### Detail kolom removal (D-03) — area dibahas
- **D-03:** `RemovedBy` = **userId** Admin/HC pelaku (cermin kolom existing `CreatedBy` `string?` `AssessmentSession.cs:95`). `RemovalReason` = **`nvarchar(500)`** (alasan modal pendek; hindari `nvarchar(max)` tak perlu) — set via Fluent `HasMaxLength(500)` di `ApplicationDbContext.cs` (~L461-552) atau `[MaxLength(500)]` di model. `RemovedAt` = **UTC** (`DateTime?`).
- **D-03a:** 3 kolom **nullable additif tanpa default destruktif** — semua baris existing dapat NULL (data lama tak berubah). Verifikasi `dotnet ef migrations add AddParticipantRemovalColumns` + apply DB lokal + cek sqlcmd kolom hadir.

### Invarian JoinBatch (D-04) — temuan scout, penting untuk planner
- **D-04:** `AssessmentHub.JoinBatch` (`:21`) saat ini cek `s.Status == "InProgress"` lalu **silent `return`** bila tak ada sesi. Soft-remove **TIDAK mengubah `Status`** (spec §B2: jangan sentuh Score/IsPassed/dst — Status InProgress tetap). Maka guard WAJIB **eksplisit** tambah `&& s.RemovedAt == null` ke predikat `AnyAsync`, mempertahankan pola **silent skip** existing (bukan throw). Tanpa ini, peserta InProgress yang di-soft-remove masih bisa re-join grup.

### Carry-forward dari spec (LOCKED, bukan dibahas ulang)
- Sumber-kebenaran removed = `RemovedAt != null`. `AssessmentSession` per-peserta; batch = `Title+Category+Schedule.Date`; InProgress = turunan (`StartedAt!=null && CompletedAt==null`, `DeriveUserStatus :2715`).
- RBAC/antiforgery untuk endpoint add/remove/restore = scope 410/411 (Phase 409 hanya guard read-path + migration, tak ada endpoint mutasi baru).
- Proton reject = scope endpoint 410/411 (409 tak buat endpoint mutasi).

### Claude's Discretion
- Penempatan presisi guard line di `StartExam` (sebelum/sesudah mark-InProgress — rekomendasi: sangat awal, sebelum `justStarted`/StartedAt ditulis, agar sesi removed tak pernah ter-mark).
- Pilih Fluent API vs Data Annotation untuk `HasMaxLength(500)` — ikut pola dominan `ApplicationDbContext`.
- Bentuk exact query exclude (tambah `.Where(s => s.RemovedAt == null)` ke chain existing vs predikat gabungan) — selama semua surface §D ter-cover.
- Cakupan unit/integration test guard + exclude (minimal: guard block 3 jalur + exclude count/list).

### Folded Todos
Tidak ada. (Todo match `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` skor 0.6 = cleanup data test pasca-367, **bukan** scope 409 — lihat Deferred.)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — design spec milestone. **§A** Data Model (3 kolom migration), **§D** Filter exclude-removed + daftar surface, **§E** Guard re-entry (KRITIS), **§H** Security. 6 keputusan LOCKED.
- `.planning/REQUIREMENTS.md` — **PRMV-03** (guard re-entry, mapped 409) + **PLIV-01** (exclude-removed + panel, fondasi di 409).
- `.planning/ROADMAP.md` — Phase 409 §"Phase Details" (Goal + 5 Success Criteria + Files map).

### Production code (file:line, code-verified dari audit 4-agen)
- `Models/AssessmentSession.cs` — tambah 3 properti removal (cermin `CreatedBy :95`).
- `Data/ApplicationDbContext.cs` (~L461-552) — Fluent config (HasMaxLength 500 untuk RemovalReason bila perlu).
- `Migrations/{TIMESTAMP}_AddParticipantRemovalColumns.cs` — NEW.
- `Controllers/CMPController.cs` — `StartExam` (block pattern existing `:953-995`, guard `RemovedAt!=null` paling awal) + `SubmitExam` (`:1573`, guard setelah load `:1583` sebelum grading).
- `Hubs/AssessmentHub.cs` — `JoinBatch` (`:21`, tambah `&& RemovedAt==null` ke `AnyAsync`, silent skip; **catatan D-04**).
- `Controllers/AssessmentAdminController.cs` — `AssessmentMonitoring` (`:2815`), `AssessmentMonitoringDetail` (`:3273` + `InProgressCount`), `ManageAssessmentTab_Assessment` (`:179`), `DeriveUserStatus` (`:2715`).
- `Models/AssessmentConstants.cs` (`:13-22`) — enum status (Status TIDAK berubah saat soft-remove).

### Lingkungan / workflow
- `CLAUDE.md` — Develop Workflow (migration=TRUE → `dotnet ef` + apply DB lokal + verify + notify IT commit hash + flag) + Seed Workflow.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola block `StartExam`** (`CMPController.cs:953-995`): `TempData["Error"]=...; return RedirectToAction("Assessment");` dipakai untuk window-close, Abandoned, durasi-0 → guard removed reuse identik (D-02).
- **Kolom `CreatedBy`** (`AssessmentSession.cs:95`, `string?`, userId) — template langsung untuk `RemovedBy` (D-03).
- **`DeriveUserStatus`** (`:2715`) — turunkan InProgress; tak perlu kolom baru.

### Established Patterns
- **JoinBatch silent-skip** (`AssessmentHub.cs:21`): `if (!hasSession) return;` — guard removed pertahankan silent return, tambah predikat `RemovedAt==null` (D-04). Soft-remove TIDAK mutasi `Status` → guard harus eksplisit, bukan andalkan Status.
- **Exclude via `.Where`** pada query monitoring existing — tambahkan `s.RemovedAt == null` ke chain LINQ surface §D.
- **Migration additif nullable** (pola Phase 372 ShuffleToggles, 352 image cols) — `dotnet ef migrations add` + apply lokal.

### Integration Points
- 3 kolom baru dikonsumsi: Phase 410 (idempotency add: query sesi aktif `RemovedAt==null`), Phase 411 (write `RemovedAt/RemovedBy/RemovalReason` + clear saat restore), Phase 412 (panel "Peserta Dikeluarkan" baca soft-removed + exclude count).
- Guard re-entry melindungi jalur worker existing tanpa ubah signature endpoint.

</code_context>

<specifics>
## Specific Ideas

- Pesan guard locked verbatim: **"Anda telah dikeluarkan dari ujian ini."**
- Nama migration locked: **`AddParticipantRemovalColumns`**.
- Prinsip pemandu exclude: "sertifikat utuh & reversibel" → soft-removed bersertifikat tetap utuh di riwayat pekerja, hanya hilang dari hitungan **aktif** admin.

</specifics>

<deferred>
## Deferred Ideas

- **Endpoint `AddParticipantsLive` / `RemoveParticipantLive` / `RestoreParticipantLive` + RBAC/antiforgery/Proton-reject** — Phase 410/411 (409 hanya read-path guard + migration, tak ada endpoint mutasi).
- **Panel UI "Peserta Dikeluarkan" + SignalR `participantAdded/Removed/examRemoved` + halaman dedicated force-kick** — Phase 412.
- **Test+UAT (xUnit integration + Playwright e2e)** — Phase 413 (Phase 409 cukup unit/integration minimal untuk guard + exclude).
- **Exclude di `/CMP/Records` pekerja / `WorkerDataService`** — sengaja TIDAK dilakukan (D-01a); buka hanya bila muncul kebutuhan nyata "removed harus hilang dari riwayat pekerja".

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` (skor 0.6) — cleanup data test/audit lokal pasca-Phase 367; **bukan** scope v32.5/409. Tetap di backlog todo.

</deferred>

---

*Phase: 409-data-foundation-re-entry-guards-exclude-removed-query*
*Context gathered: 2026-06-21*
