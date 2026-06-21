# Phase 411: Remove + Restore Backend Live - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Backend penghapusan + pemulihan peserta live untuk milestone v32.5. Deliverable Phase 411:
1. **`RemoveParticipantLive` (HttpPost)** — hybrid by-state: belum-mulai+tanpa-data → hard-delete (`RecordCascadeDeleteService` single root); sudah-mulai/Completed/ada-data → soft-remove (`RemovedAt/RemovedBy/RemovalReason`, JANGAN sentuh `Score/IsPassed/NomorSertifikat`/file/response). Idempoten. Pre/Post pasangan-sebagai-satu-unit (PRMV-05).
2. **`RestoreParticipantLive` (HttpPost)** — set `RemovedAt=null` + clear `RemovedBy/RemovalReason`, hanya untuk soft-removed (PRMV-04).
3. **Perbaiki stub mati `DeleteAssessmentPeserta`** (`EditAssessment.cshtml:666`) → delegasi ke service RemoveParticipantLive.
4. **Audit** Add/Remove/Restore + **RBAC Admin+HC + antiforgery** di SEMUA endpoint (PLIV-03).
5. Tolak sesi Proton.

**TIDAK** termasuk: SignalR force-kick/broadcast + UI panel (412), test+UAT (413). File-overlap `AssessmentAdminController.cs` dengan 410 → **sequential** (410 sudah complete).

</domain>

<decisions>
## Implementation Decisions

### Hard-delete vs eager-UPA dari 410 (D-01) — area dibahas — PENTING
- **D-01:** Threshold hard-delete = `StartedAt == null` **&&** **0 `PackageUserResponse`**. **`UserPackageAssignment` (eager dari 410) TIDAK dihitung "data"** → peserta belum-mulai tetap **hard-delete**. `RecordCascadeDeleteService.ExecuteAsync` (single root sesi) **wajib cascade bersihkan eager-UPA + assignment** milik sesi itu.
- **Rasional:** 410 sekarang buat UPA eager saat add; tanpa pengecualian ini, hampir tak ada peserta yang bisa hard-delete (semua punya UPA). UPA = artefak siap-mulai, bukan jejak pengerjaan. Planner: verifikasi `RecordCascadeDeleteService` sudah/diperluas untuk hapus `UserPackageAssignment` milik sesi (cek 9-tabel cascade `:175-314`).

### RemovalReason wajib/opsional (D-02) — area dibahas
- **D-02:** `RemovalReason` **WAJIB** saat **soft-remove** (peserta sudah-mulai/Completed/bersertifikat — jejak audit PLIV-03). **Opsional** saat **hard-delete** (peserta belum-mulai, bersih). Validasi server: jika jalur = soft-remove dan `reason` kosong → 400 "Alasan penghapusan wajib diisi." (peserta berdata). Modal UI (412) cermin aturan ini.

### SignalR (D-03) — area dibahas
- **D-03:** **DEFER** SignalR `examRemoved` (force-kick worker) + `participantRemoved` (broadcast monitor) ke **Phase 412** (konsisten 410 D-04). 411 = backend `RemoveParticipantLive`/`RestoreParticipantLive` + audit + RBAC saja. Endpoint return JSON outcome (hard/soft + sessionId) untuk dikonsumsi UI 412. **JANGAN** tambah `_hubContext` call di 411.

### Fix stub DeleteAssessmentPeserta (D-04) — area dibahas
- **D-04:** Implement `DeleteAssessmentPeserta` (tombol mati `EditAssessment.cshtml:666`) sebagai **delegasi ke service bersama** yang sama dengan `RemoveParticipantLive` (varian full-page redirect, bukan JSON). Ekstrak logika remove jadi service/private method bersama → satu sumber kebenaran. Tombol existing jadi hidup.

### Carry-forward dari spec & fase lalu (LOCKED, bukan dibahas ulang)
- **Soft-remove JANGAN sentuh** `Score`, `IsPassed`, `NomorSertifikat`, file sertifikat, `PackageUserResponse`, `Status` (sesi InProgress tetap InProgress — guard 409 D-04 yang andalkan `RemovedAt`). (spec §B2.2)
- **Idempoten:** `RemovedAt != null` → no-op sukses. (spec §B2.1)
- **Pre/Post (PRMV-05):** salah satu Pre/Post ada-data → **soft-remove keduanya**; kedua-duanya belum-mulai+tanpa-data → **hard-delete keduanya** (mirror `DeletePrePostGroup`). (spec §B2.4)
- **Restore** hanya untuk soft-removed (hard-deleted tak bisa restore). (spec §B3)
- **RBAC `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`** semua endpoint; **audit** `RemoveParticipantLive`/`RestoreParticipantLive` (siapa/kapan/alasan). (PLIV-03, §H)
- **Proton reject** (`Category=="Assessment Proton"`). (spec §F)
- Keputusan #5 spec: lepas `EnsureCanDeleteAsync` untuk HC hapus Completed/bersertifikat — mitigasi: soft-remove (cert utuh) + audit + (412 modal keras).

### Claude's Discretion
- Bentuk service bersama (private method vs service class) untuk remove-logika yang dipakai RemoveParticipantLive + DeleteAssessmentPeserta.
- Apakah `RecordCascadeDeleteService` sudah hapus UPA atau perlu ditambah (verifikasi cascade list `:175-314`); jika perlu tambah, lakukan minimal.
- Bentuk JSON outcome RemoveParticipantLive (`{ sessionId, mode: "hard"|"soft", linkedSessionId? }`).
- Penempatan validasi reason-wajib (sebelum tentukan jalur vs sesudah).
- Cakupan integration test (hard not-started, soft in-progress/completed-cert preserved, idempotent, Pre/Post pair, restore, Proton reject, reason-wajib-soft).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — **§B2** RemoveParticipantLive (hybrid, idempotent, Pre/Post unit), **§B3** RestoreParticipantLive, **§B5** fix DeleteAssessmentPeserta, **§F** Proton out, **§G** errors, **§H** security (keputusan #5).
- `.planning/REQUIREMENTS.md` — PRMV-01 (hybrid delete), PRMV-04 (restore), PRMV-05 (Pre/Post unit), PLIV-03 (audit + RBAC).
- `.planning/ROADMAP.md` — Phase 411 §"Phase Details".
- `.planning/phases/410-add-participant-backend-live/410-CONTEXT.md` — D-04 SignalR-defer pattern (411 cermin); eager-UPA (D-01 411 berinteraksi).
- `.planning/phases/409-...409-CONTEXT.md` — definisi soft-removed `RemovedAt!=null`; guard re-entry andalkan `RemovedAt` (Status TIDAK berubah saat soft-remove).

### Production code (file:line)
- `Controllers/AssessmentAdminController.cs` — endpoint baru `RemoveParticipantLive` + `RestoreParticipantLive` + `DeleteAssessmentPeserta` (stub mati, implement); `EnsureCanDeleteAsync` `:7203` (longgar untuk soft-remove cert); `DeletePrePostGroup` `:2566` (mirror Pre/Post partner-handling); `DeriveUserStatus` `:2715`.
- `Services/RecordCascadeDeleteService.cs` `:175-314` — cascade 9-tabel + file sertifikat (single tx); **verifikasi cakupan UserPackageAssignment** (D-01).
- `Models/AssessmentSession.cs` — kolom `RemovedAt/RemovedBy/RemovalReason` (dari 409).
- `Views/Admin/EditAssessment.cshtml:666` — tombol mati POST `DeleteAssessmentPeserta` (D-04 hidupkan).
- Audit pattern `LogAsync(...)` (lihat AddParticipantLive 410 `:2456`).

### Lingkungan / workflow
- `CLAUDE.md` — migration=FALSE untuk 411 (set/clear kolom existing 409, cascade existing); verify lokal; no push; Seed Workflow untuk test.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`RecordCascadeDeleteService.ExecuteAsync`** (`:175-314`) — hard-delete cascade single-root (9 tabel + file cert, 1 tx). Pakai untuk jalur hard-delete (D-01); verifikasi/extend untuk UPA.
- **`DeletePrePostGroup`** (`:2566`) — partner-handling Pre/Post → mirror untuk PRMV-05 pair-as-unit.
- **`EnsureCanDeleteAsync`** (`:7203`) — guard delete existing; longgarkan untuk soft-remove cert (keputusan #5).
- **Audit `LogAsync`** — pola dari AddParticipantLive (410) untuk RemoveParticipantLive/RestoreParticipantLive.

### Established Patterns
- **Soft-remove = set 3 kolom, JANGAN mutasi Status** (409 D-04): guard re-entry andalkan `RemovedAt`, bukan Status.
- **Idempoten by RemovedAt** check di awal endpoint.
- **RBAC + antiforgery** pola endpoint mutasi admin (cermin AddParticipantsLive 410).

### Integration Points
- Konsumsi 409 (set/clear kolom removal + definisi soft-removed) + 410 (eager-UPA → cascade D-01).
- Output (JSON outcome) dikonsumsi 412 (UI pindah baris + panel "Peserta Dikeluarkan" + SignalR).
- Service bersama remove dipakai RemoveParticipantLive (JSON) + DeleteAssessmentPeserta (redirect).

</code_context>

<specifics>
## Specific Ideas

- Soft-remove peserta bersertifikat = sertifikat UTUH + reversibel (prinsip pemandu, 409).
- Reason-wajib HANYA jalur soft-remove (D-02) — hindari friksi hapus salah-tambah belum-mulai.
- UPA eager (410) BUKAN data → tetap hard-delete (D-01); cascade harus bersihkan UPA.
- SignalR + modal keras = 412 (D-03 defer).

</specifics>

<deferred>
## Deferred Ideas

- **SignalR `examRemoved`/`participantRemoved` + force-kick + modal konfirmasi keras + panel "Peserta Dikeluarkan" + handler client** — Phase 412.
- **Playwright e2e + xUnit suite lengkap remove/restore live** — Phase 413 (411 cukup integration minimal backend).
- **`AssessmentAttemptHistory` FK ke User (bukan Session)** — komplikasi cascade (spec §State); planner perhatikan saat hard-delete, jangan yatim-kan.

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` (skor) — cleanup data test lokal; bukan scope 411.

</deferred>

---

*Phase: 411-remove-restore-backend-live*
*Context gathered: 2026-06-21*
