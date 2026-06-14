# Phase 360: Bypass Backend (B) - Context

**Gathered:** 2026-06-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Backend fitur **Bypass Tahun** — admin/HC pindahin coachee antar tahun/track/unit dengan alasan + audit. Deliverable (PBYP-01..07):
- Migration #2 tabel `PendingProtonBypass` (lifecycle Menunggu→Siap→Selesai/Dibatalkan) **+ kolom baru `Origin` di `ProtonTrackAssignment`** (lihat D-04).
- 4 closure mode CL-A (lulus instan), CL-B(a) (input manual instan), CL-B(b) (buat assessment, tunggu lulus), CL-C (tinggalkan) + validasi |Δtahun|≤1 + 1-assignment-aktif.
- Notif `PROTON_BYPASS_READY` via hook GradingService (flip flag, BUKAN auto-pindah).
- Coach handling E15 (deactivate mapping aktif lama → create baru).
- Bootstrap deliverable target pakai Unit dari form.
- 6 endpoint (`BypassList`, `BypassPendingList`, `BypassDetail`, `BypassSave`, `BypassConfirm`, `BypassCancelPending`) `[Authorize(Admin,HC)]` + AntiForgery + audit.

**TIDAK termasuk (→ Phase 361):** semua UI (Tab2 redesign, wizard 3-langkah, panel pending, notif deep-link, e2e UAT). Phase 360 = backend murni (migration + logic + service + 6 endpoint). Spec B §3-§11 sudah terkunci; phase ini implementasi + 4 keputusan implementasi di bawah.
</domain>

<decisions>
## Implementation Decisions

### CL-B(b) konfigurasi exam (gray area 1)
- **D-01:** CL-B(b) = **2-step (Opsi B)**. `BypassSave` bikin `AssessmentSession` source-year **"bare"** di dalam transaksi (`Category="Assessment Proton"`, `ProtonTrackId`=source, `TahunKe`=S, jadwal/durasi/KKM default atau dari form) **TANPA paket soal**. HC lampirkan paket soal lewat alur **ManagePackages / Kelola Assessment existing** setelah bypass. `LinkedAssessmentSessionId` nunjuk sesi bare ini. Worker baru bisa ujian setelah HC pasang paket (step 2).
- **D-02:** **WAJIB pengingat TempData** setelah `BypassSave` CL-B(b): "Sesi exam dibuat — lampirkan paket soal di Kelola Assessment sebelum worker bisa ujian." (Cegah pending nyangkut "Menunggu" karena worker gak punya soal.)
- **D-03 (klarifikasi/MISS-2):** CL-B(b) source exam **selalu online (Tahun 1/2)** — naik cuma 1→2 / 2→3, jadi tahun-asal yang diuji = Tahun 1/2 = exam online → hook GradingService §7 pasti kena. Tidak ada kasus source-Tahun-3-interview. Klausa "Tahun 3=interview" di spec §4 tidak relevan untuk CL-B(b).

### Exempt gate antar-tahun (gray area 2)
- **D-04:** **Stempel permanen (Opsi A).** `BypassSave` set kolom baru `Origin="Bypass"` di `ProtonTrackAssignment` target saat create. Kolom `string? Origin` nullable; baris lama = null (= "Normal", tidak exempt) → **tidak perlu backfill**. Nilai assignment.Origin = {null, "Bypass"} saja. **Gabung ke migration #2** (`PendingProtonBypass`) → tetap 1 operasi migration, sentuh 2 tabel. ⚠️ **Melebar dari spec** (spec §6 cuma sebut 1 tabel baru) — notify IT migration#2 = tabel `PendingProtonBypass` + kolom `ProtonTrackAssignment.Origin`.
- **D-05:** Exempt **CUMA cross-year prereq, BUKAN gate deliverable 100%.** Stempel cuma lewatkan cek "Tahun N-1 lulus" (`AssessmentAdminController.cs:1368`). Gate deliverable 100% target-year (`:1373-1389`) **TETAP berlaku** — worker bypass tetap wajib selesaikan deliverable tahun tujuan sebelum final tahun tujuan. (Bypass force-approve deliverable tahun ASAL/source, bukan tujuan.)
- **D-06:** Isi **2 titik exempt**, dua-duanya cek `assignment aktif worker punya Origin=="Bypass"` untuk `ProtonTrackId` itu:
  - (a) `CreateAssessment` gate `AssessmentAdminController.cs:1368` — tambah kondisi `|| isBypassAssignment` sebelum skip cross-year.
  - (b) Placeholder `CoachMappingController.cs:533` (`isExemptFromCrossYear`) — isi `worker active assignment Origin=="Bypass"` (untuk re-assign normal pasca-bypass).
- **D-07:** **Renewal exempt tetap session-based** (`RenewsSessionId`/`RenewsTrainingId`, sudah dari 359 — `:1362`/`:1368`). TERPISAH dari stempel assignment. Jangan campur jalur renewal ke `Origin` assignment.

### Penempatan logic + transaksi (gray area 3)
- **D-08:** **`ProtonBypassService` (Opsi A)** — komponen sendiri, scoped DI di `Program.cs`, dipakai bareng `ProtonDataController` (6 endpoint) DAN hook notif `GradingService` §7 (method mis. `MarkPendingReadyIfAnyAsync(sessionId)` + reverse re-grade). Pola sama `ProtonCompletionService` (358). Unit/integration testable (xUnit). Inject `ApplicationDbContext` + `ProtonCompletionService` (EnsureAsync) + `NotificationService` + `IAuditLogService`.
- **D-09:** **Transaksi all-or-nothing** per operasi (§5.1 instan, §5.2 buat-pending, §5.3 konfirmasi) pakai pola Phase 333/334 (`BeginTransactionAsync` wrap semua step + `CommitAsync` + catch `DbUpdateException` friendly). Tidak ada file op (E10 KEEP orphan evidence) → tidak ada langkah post-commit File.Delete.

### Guard rail pending (gray area 4) — ketiganya dipasang
- **D-10:** **Blok dobel pending — SEMUA mode.** Di `BypassSave`, kalau worker sudah punya `PendingProtonBypass` Status ∈ {Menunggu, Siap}, tolak bypass baru **apapun modenya** (instan maupun B(b)) + pesan jelas. Cegah dua rencana pindah tabrakan + ambigu LinkedAssessmentSession.
- **D-11:** **Cek ulang saat konfirmasi.** Di `BypassConfirm`, sebelum eksekusi pindah: validasi (a) assignment asal worker masih sama dengan rencana, (b) exam beneran lulus (`LinkedAssessmentSession.IsPassed` + penanda Origin="Exam" ada), (c) pending masih Status="Siap". Tolak + pesan kalau kondisi basi.
- **D-12:** **Konfirmasi anti-dobel (atomik).** Transisi `Status` Siap→Selesai dikunci atomik (conditional update `WHERE Status="Siap"` / rowsAffected guard pola Phase 310 `:3729`). Klik 2x / 2 HC barengan → eksekusi pindah cuma jalan sekali.

### Detail spec konkret yang di-baking (locked di spec, ditegaskan)
- **D-13:** Force-approve deliverable (CL-B(a)/B(b)/konfirmasi) tulis `DeliverableStatusHistory` dengan `StatusType="Bypassed-AutoApprove"` (nilai string baru, tanpa schema change) + `ActorId/Name/Role`=HC + Timestamp (spec §3.1 Fork1=B).
- **D-14:** Step `cancel exam aktif S` (E5) di §5.1 instan — cancel exam in-progress source-year, **kecuali** AssessmentSession CL-B(b) yang baru dibuat.
- **D-15:** Re-grade Pass→Fail exam yang sudah "Siap" → set pending balik `Status="Menunggu"` (penanda Origin="Exam" juga dihapus per A-M1, sudah di `RegradeAfterEditAsync` 358). Worker belum pindah (Opsi B) → aman, gak ada rollback assignment.
- **D-16:** Coach `TargetCoachId == null` = **pertahankan** mapping coach existing (jangan deactivate+recreate); kalau diisi = deactivate mapping aktif lama DULU → create baru (E15 filtered-unique `ApplicationDbContext.cs:326`).

### Claude's Discretion
- Default nilai jadwal/durasi/KKM sesi bare CL-B(b) (wizard 361 kumpulin minimal vs default murni) — planner pilih, sejalan D-01 (paket terpisah).
- Penamaan method `ProtonBypassService` + pemecahan per closure mode (strategy/switch) — planner.
- Mekanisme extract `AutoCreateProgressForAssignment` + create-coach-mapping dari `CoachMappingController` agar reusable oleh service (lihat code_context risiko) — planner pilih (extract ke helper/service vs internal-visible).
- Strategi test (xUnit `ProtonBypassService` real-SQL disposable fixture pola Phase 344 TEST-05 + integration gate exempt).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (otoritas)
- `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` — **Spec final Diskusi B** (sumber otoritas). §3 keputusan terkunci, §4 closure mode, §5 pohon keputusan (5.1/5.2/5.3), §6 schema `PendingProtonBypass`, §7 pemicu notif, §8.1 batal pending, §10 6 endpoint, §11 edge case (E5/E7/E8/E10/E14/E15), §12 dependency ke A.
- `.planning/REQUIREMENTS.md` §PBYP — PBYP-01..07 (scope 360); PBYP-08..10 (Phase 361, konteks dependency).

### Dependency Diskusi A (fondasi, sudah shipped 358/359)
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` — helper `EnsureProtonFinalAssessment` (A-4) dipakai CL-B(a) & konfirmasi B(b) (`Origin="Bypass"`/"Exam"); `Origin` marker (A-M9); bypass exempt gate antar-tahun (A-M4); level dimatikan (A-3) → form CL-B(a) tanpa input level.
- `.planning/phases/358-penanda-kelulusan-fondasi-a/358-CONTEXT.md` — `ProtonCompletionService.EnsureAsync(coacheeId, protonTrackId, createdById, origin, notes)` signature + guard A-M11 + Origin selektif re-grade.
- `.planning/phases/359-gate-berurutan-cleanup-a/359-CONTEXT.md` — gate cross-year D-03 (`IsPrevYearPassedAsync`), exempt placeholder D-06, gate enforce 2 titik D-04.

### Workflow & ops
- `docs/DEV_WORKFLOW.md` — SOP migration + notify IT flag migration (migration#2 = `PendingProtonBypass` + `ProtonTrackAssignment.Origin`).
- `docs/SEED_WORKFLOW.md` — snapshot/restore DB lokal sebelum apply migration + sebelum UAT seed (4 closure mode butuh skenario beragam).
- `CLAUDE.md` — Develop Workflow (lokal→Dev→Prod; verifikasi `dotnet build` + `dotnet ef database update` + `dotnet run` localhost:5277 + xUnit sebelum commit; AD lokal `Authentication__UseActiveDirectory=false dotnet run`).

### Pattern (reuse)
- `docs/superpowers/specs/2026-06-06-coach-coachee-assign-audit-fix.md` — Phase 356 `CoacheeEligibilityCalculator.IsEligiblePerUnit` (gate 100% target-year tetap berlaku D-05).
- Phase 333/334 atomicity pattern (transaksi wrap, catch DbUpdateException) — D-09.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/ProtonCompletionService.cs` — `EnsureAsync(...)` (penanda, dipakai CL-B(a)+konfirmasi B(b)), `RemoveExamOriginAsync`, `GetPassedYearsAsync`, `IsPrevYearPassedAsync` (`:107`), `ProtonYearGate.IsAllowed` (`:124`). Dari 358/359.
- `Services/GradingService.cs` — `GradeAndCompleteAsync` (~`:49`) + `RegradeAfterEditAsync` (`:420`) — hook §7 ditambah DI SINI (panggil method ProtonBypassService setelah EnsureProtonFinalAssessment, guard `Category=="Assessment Proton" && IsPassed && ProtonTrackId.HasValue`).
- `Helpers/CoacheeEligibilityCalculator.IsEligiblePerUnit` — gate 100% (D-05 tetap berlaku).

### Established Patterns
- DI scoped di `Program.cs` (`AddScoped<ProtonBypassService>`).
- Skip-with-summary TempData (Phase 359 D-01) + rowsAffected atomic guard (`AssessmentAdminController.cs:3729`, Phase 310) — pola D-12.
- Notif template di `NotificationService._templates` — tambah `PROTON_BYPASS_READY` (Title "Bypass Siap Diselesaikan", `ActionUrlTemplate="/ProtonData/Override?tab=bypass&pending={PendingId}"`).

### Integration Points
- `Models/ProtonModels.cs:71` (`ProtonTrackAssignment`) — tambah `string? Origin`. `:207-226` (`ProtonFinalAssessment`) sudah punya `Origin` (358 — beda tabel, jangan bingung).
- `Controllers/AssessmentAdminController.cs:1368` — gate cross-year, tambah exempt `Origin=="Bypass"` (D-06a). `:1373-1389` gate 100% — JANGAN exempt (D-05).
- `Controllers/CoachMappingController.cs:533` — isi placeholder `isExemptFromCrossYear` (D-06b). `:1424` `AutoCreateProgressForAssignment` (private) + logic create CoachCoacheeMapping — **perlu extract/share untuk ProtonBypassService** (D-08 risiko).
- `Controllers/ProtonDataController.cs:210` (`Override`) + `:1400` (`OverrideSave`) — 6 endpoint bypass baru ikut pola Authorize/AntiForgery/audit di sini.
- `ApplicationDbContext.cs:326` — filtered-unique `CoachCoacheeMapping.CoacheeId WHERE IsActive=1` (E15 wajib deactivate dulu) + register DbSet `PendingProtonBypass`.

### Risiko (planner harus selesaikan)
- `AutoCreateProgressForAssignment` (bootstrap deliverable unit-filtered) + create-coach-mapping = **private di `CoachMappingController`**. ProtonBypassService butuh logic ini. JANGAN duplikat filter unit — extract ke helper/service atau buat internal-visible. Inkonsistensi filter = bug bootstrap.
</code_context>

<specifics>
## Specific Ideas

- **Coupling ringan GradingService (§7):** hook cuma flip pending→Siap + kirim notif, BUKAN eksekusi pindah. Pindah tetap di tangan HC via `BypassConfirm` (Opsi B HC-konfirmasi). Hindari naro eksekusi pindah di hot-path grading.
- **Notif ke HC inisiator** (`InitiatedById`), bukan worker. Worker tetap dapat `ASMT_RESULTS_READY` biasa.
- **Sesi bare CL-B(b) tanpa paket** = trade-off sadar: worker gak bisa ujian sampai HC pasang paket (D-02 reminder kritikal). MISS-2: pending tetap "Menunggu", bisa retry, atau HC batal pending (§8.1).
- **Migration #2 melebar (D-04):** `PendingProtonBypass` + `ProtonTrackAssignment.Origin` dalam satu migration file. Snapshot DB lokal sebelum `ef database update` (SEED_WORKFLOW).
</specifics>

<deferred>
## Deferred Ideas

- **Semua UI bypass** (Tab2 redesign, wizard 3-langkah, panel "Menunggu Konfirmasi", notif deep-link, e2e UAT) → **Phase 361** (PBYP-08..10).
- **Audit/improve Tab1 Override Deliverable** (DeliverableStatusHistory di Tab1, warning un-approve penanda-Lulus, RejectedById) → backlog 999.x (spec B §13 out of scope).
- **Undo bypass executed** (tombol undo) → tidak ada (spec §8.2 Opsi C — koreksi via bypass lagi). Butuh kolom `PreviousStatus` di `DeliverableStatusHistory` kalau dibangun nanti.
- **Menghidupkan level kompetensi** → dibuang (A-3, dormant).

### Reviewed Todos (not folded)
None — `todo match-phase 360` tidak dijalankan (tidak ada backlog todo relevan terdeteksi).

None lain — diskusi tetap dalam scope phase.
</deferred>

---

*Phase: 360-bypass-backend-b*
*Context gathered: 2026-06-10*
