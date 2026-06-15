# Phase 356: Audit Fix Assign Coach×Coachee - Context

**Gathered:** 2026-06-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Pastikan fitur HC/Admin **Assign Coach×Coachee (PROTON)** berfungsi benar dengan memperbaiki 7 temuan audit 2026-06-06 di `Controllers/CoachMappingController.cs`. Audit-driven fix — **bukan fitur baru**. Off-theme dari v24.0 image-work (jalur file berbeda, independen 352-355).

Headline: AF-1 (HIGH) eligibility salah untuk track multi-unit → coachee tak pernah eligible Assessment Proton. Sisanya MED/LOW/INFO.

**Out-of-scope (JANGAN sentuh):** arsitektur transaksi/audit/file-delete atomic existing (sudah benar), unique-index invariant 1-coach-aktif/coachee, fitur image v24.0.
</domain>

<decisions>
## Implementation Decisions

### AF-1 (HIGH) — Eligibility per-unit [LOCKED, headline]
- **D-01:** `GetEligibleCoachees` (L1277-1334) hitung **expected deliverable per-unit coachee**, bukan total deliverable semua-unit track. Resolve unit tiap coachee (mirror `AutoCreateProgressForAssignment` L1342-1355: `AssignmentUnit` mapping aktif → fallback `User.Unit`), `expectedCount = deliverable track WHERE Unit==coacheeUnit`. Eligible bila `mine.Count == expectedCount && expectedCount > 0 && mine.All(Approved)`.
- **D-02:** Tahun 3 (track tanpa deliverable, L1298-1307) tetap **semua assigned-coachee eligible** by-design — dipertahankan verbatim.

### AF-3 (MED) — Graduated per-unit [LOCKED by user 2026-06-06, opsi ii]
- **D-03:** `MarkMappingCompleted` (L1075-1109) set `IsCompleted=true` **DAN `IsActive=false`** (+ `CompletedAt`, + `EndDate` = waktu completion). Membebaskan unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` agar coachee graduated bisa di-assign lagi untuk unit lain.
- **D-04:** Cascade — deactivate `ProtonTrackAssignment` aktif coachee saat graduate (ikuti pola `CoachCoacheeMappingDeactivate` L931-939, stamp `DeactivatedAt`). **Pertahankan histori progress unit lama (jangan hapus).**
- **D-05:** Bungkus dalam **transaksi** (saat ini single `SaveChanges`).
- **D-06:** Mapping graduated muncul hanya saat `showAll=true` dengan badge "Graduated"; list default (`IsActive` only) tak menampilkannya.
- Semantik per-unit konsisten lintas AF-1/AF-2: assignment, progress, eligibility, graduation semua scoped per unit coachee.

### AF-2 (MED) — Batch lintas-unit = UI guard (Opsi A)
- **D-07:** Fix via **UI guard**: batasi pemilihan coachee dalam 1 batch ke **satu unit** (disable/cegah cross-unit select di modal `CoachCoacheeMapping.cshtml` L408-449 `coacheeChecklist`). Pertahankan semantik `AssignmentUnit` eksplisit. **Backend `AutoCreateProgressForAssignment` tidak diubah** (Opsi B ditolak — bikin makna `AssignmentUnit` batch kabur).

### AF-5 (LOW) — Notifikasi reassign
- **D-08:** `ApproveReassignSuggestion` (L1614-1638) tambah `_notificationService` ke coach lama (dilepas), coach baru (ditunjuk), coachee — selaras pola COACH-02 / konsisten dengan Assign/Edit/Deactivate.

### AF-6 (LOW) — Pesan error duplikat spesifik
- **D-09:** `CoachCoacheeMappingAssign` (L474-490) tangkap `DbUpdateException` yang melanggar `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` → kembalikan pesan ramah spesifik "coachee sudah punya coach aktif", bukan generic "Gagal menyimpan assignment".

### AF-7 (INFO) — N+1 progression-warning [USER PILIH MASUK SCOPE]
- **D-10:** Refactor loop progression-warning (`CoachCoacheeMappingAssign` L497-546, ~4 query/coachee) jadi **batch query**. Catatan: user eksplisit minta masuk scope meski INFO/volume rendah — jaga agar tak ubah perilaku warning, hanya kurangi query count.

### AF-4 (LOW-MED) — DEFER ke backlog
- **D-11:** Reactivate window ±5s (L1012-1033) **TIDAK difix di phase ini**. FIX-01 sudah mitigasi salah-restore. **Dokumentasikan asumsi window di kode** (komentar). Catat sebagai backlog item. Alasan defer: severity LOW-MED + hindari nambah migration ke v24 leg yang 0-migration.

### Migration
- **D-12:** **Migration = false.** Verified: kolom `IsActive`/`EndDate`/`CompletedAt`/`IsCompleted`/`AssignmentUnit` SEMUA sudah ada di `Models/CoachCoacheeMapping.cs` (L24/34/44/47/50). D-03 cuma set nilai kolom existing. AF-4 (satu-satunya kandidat kolom baru) di-defer. Tidak ada schema change.

### Verifikasi & Seed
- **D-13:** **xUnit** logic-bearing untuk eligibility per-unit (AF-1): track multi-unit — coachee unit A 3/3 Approved → eligible; coachee unit B 0/1 → tidak. Ekstrak logic eligibility ke helper testable.
- **D-14:** **Playwright UAT** localhost:5277 (CLAUDE.md Develop Workflow): track id=4 (4 deliverable / 2 unit) — assign coachee → approve deliverable unit-nya → buka CreateAssessment kategori Assessment Proton track 4 → coachee muncul di `GetEligibleCoachees`. Plus smoke AF-2 batch cross-unit guard + AF-5 notif reassign.
- **D-15:** **Seed fixture** track id=4 via SEED_WORKFLOW: snapshot DB lokal sebelum seed, catat SEED_JOURNAL, restore sesudah (klasifikasi temporary+local-only). `dotnet build` 0 error + `dotnet test` hijau + tanpa regresi assign/deactivate/reactivate.

### Claude's Discretion
- Bentuk persis ekstraksi helper eligibility per-unit (signature, lokasi) — asal testable & mirror filter `AutoCreateProgressForAssignment`.
- Wording persis pesan notif AF-5 & pesan error spesifik AF-6.
- Mekanisme UI guard AF-2 (disable checkbox vs validasi submit vs filter dropdown) — asal hasil = 1 unit/batch.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Audit (primary)
- `docs/superpowers/specs/2026-06-06-coach-coachee-assign-audit-fix.md` — AF-1..7 code+data-verified, D-1..D-5 spec-level, bukti DB track id=4 (4 deliverable/2 unit). **Sumber kebenaran utama.**

### Target Code
- `Controllers/CoachMappingController.cs` (1694 baris) — semua fix di sini. Anchor: `GetEligibleCoachees` L1277, `AutoCreateProgressForAssignment` L1338, `MarkMappingCompleted` L1075, `CoachCoacheeMappingDeactivate` L931 (pola cascade), `CoachCoacheeMappingAssign` L474/497, `ApproveReassignSuggestion` L1614, `CoachCoacheeMappingReactivate` L1012.
- `Models/CoachCoacheeMapping.cs` — kolom IsActive/EndDate/CompletedAt/IsCompleted/AssignmentUnit (bukti no-migration).
- `Views/Admin/CoachCoacheeMapping.cshtml` L408-449 — modal `coacheeChecklist` (AF-2 UI guard).

### Workflow & Standard
- `docs/DEV_WORKFLOW.md` — Develop Workflow (Lokal→Dev→Prod, verifikasi lokal wajib, IT handoff commit hash + flag migration).
- `docs/SEED_WORKFLOW.md` — seed temporary+local-only, snapshot/restore SQL Server, SEED_JOURNAL.

### Pola Referensi (jangan duplikasi, ikuti gaya)
- Phase 333 (`CDPController.DeleteCoachingSession`) — pola transaksi + post-commit cleanup; dipakai sebagai gaya untuk D-05 transaksi.
- Notif pattern COACH-02 di `CoachMappingController` (Assign/Edit/Deactivate existing) — contoh untuk AF-5.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AutoCreateProgressForAssignment` (L1338-1407): filter `Unit==resolvedUnit` (L1363-1365) = sumber kebenaran untuk "deliverable yang diharapkan per-unit". AF-1 eligibility harus mirror filter ini.
- `CoachCoacheeMappingDeactivate` (L931-939): pola deactivate ProtonTrackAssignment + stamp `DeactivatedAt` (FIX-01) — reuse untuk cascade AF-3 (D-04).
- `_notificationService` + `_auditLog.LogAsync` — sudah dipakai konsisten; AF-5 tinggal ikut pola.

### Established Patterns
- Transaksi: `CoachCoacheeMappingAssign` sudah pakai transaksi untuk side-effect ProtonTrackAssignment — D-05 MarkCompleted ikuti pola yang sama.
- `catch (DbUpdateException)` spesifik — AF-6 ikuti pola error-handling existing.
- Kualitas controller TINGGI (CRIT-02, FIX-01/02, MED-01, HIGH-04, D-08..D-17 tercatat) — audit ini sisa bug logic, bukan kelemahan arsitektur.

### Integration Points
- `GetEligibleCoachees` dikonsumsi form **CreateAssessment** (kategori Assessment Proton) — AF-1 fix langsung mempengaruhi dropdown pilih coachee di sana.
- Unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (filtered `WHERE IsActive=1`) — AF-3 (D-03) sengaja melepas dengan set IsActive=false; AF-6 (D-09) backstop-nya.
</code_context>

<specifics>
## Specific Ideas

- AF-1 test WAJIB pakai track id=4 (data nyata DB lokal: Alkylation Unit 065 = 3 deliverable, RFCC NHT 053 = 1 deliverable). Coachee Alkylation 3/3 Approved → harus muncul setelah fix.
- AF-7 masuk scope atas permintaan eksplisit user (override default "skip INFO") — tapi jaga zero behavior change pada warning.
- Bundle v19-v24 masih pending push IT; phase ini harus tetap 0-migration agar tak nambah beban handoff.
</specifics>

<deferred>
## Deferred Ideas

- **AF-4 (Reactivate ±5s window refactor)** — di-defer ke backlog. Fix proper = kolom korelasi eksplisit (`DeactivatedByMappingEventId`) atau match `EndDate==DeactivatedAt` exact (butuh migration). Phase 356 cukup dokumentasikan asumsi window di komentar kode. Promote kembali via `/gsd-add-backlog` bila track multi-unit / volume reactivate naik.
</deferred>

---

*Phase: 356-audit-fix-assign-coach-coachee*
*Context gathered: 2026-06-09*
