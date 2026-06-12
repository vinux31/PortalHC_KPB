# Phase 367: Delete Records Cascade Overhaul - Context

**Gathered:** 2026-06-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin bisa hapus record worker (training / assessment manual / assessment ONLINE) dari tab Input Records sampai **100% bersih** — cascade rekursif seluruh turunan renewal lintas `TrainingRecords`↔`AssessmentSessions` + semua artefak per node (EditLogs, PackageUserResponses, AttemptHistory, UserPackageAssignments, Packages+Q+O, notifikasi lonceng, penanda Proton `Origin='Exam'`, PendingProtonBypass, `LinkedSessionId` pasangan, file sertifikat) — dengan **preview konfirmasi (bukan blokir)** dan **UI HTMX jujur (gagal ≠ sukses)**.

**Scope = Spec C temuan #1-12, #14-20** (§3.3b). Temuan #21-27 = Phase 368 (hygiene lanjutan, fase terpisah, JANGAN ditarik ke 367). Impersonate #13 = backlog 999.6.

Migration: **false** (semua perubahan kode, tidak ada kolom baru).
</domain>

<decisions>
## Implementation Decisions

### Locked dari Spec §2 (kebijakan user final — JANGAN dibahas/diubah)
- **L-01:** Cascade penuh, **no-blocker**. Induk dihapus → SEMUA turunan renewal ikut terhapus rekursif, lintas tabel via `RenewsTrainingId`/`RenewsSessionId`, guard cycle (HashSet visited).
- **L-02:** Anak renewal turunan **IKUT DIHAPUS, bukan detach** (user tolak opsi detach eksplisit §2.6).
- **L-03:** Konfirmasi = **preview tree** (judul+tanggal+jenis+pemilik, termasuk turunan renewal & kandidat mirror), bukan blokir. Pre-check renewal lama (tab 1 fase 325/329 + tab 2) DIUBAH jadi preview cascade yang sama.
- **L-04:** `PendingProtonBypasses` ber-`LinkedAssessmentSessionId` == Id → **soft-cancel** (`Status='Dibatalkan'` + `ResolvedAt`, konsisten bypass spec §8.1) — **BUKAN hard-delete row** (jejak audit dipertahankan). Phase 361 (UI panel pending) sudah ship — sinkron saat planning.
- **L-05:** Notif lonceng cleanup **konservatif** — hanya hapus `UserNotifications` yang `ActionUrl`-nya match rute entitas terhapus EKSAK (min `/CMP/Results/{id}`, `/CMP/Certificate/{id}`; pola TrainingRecord diinventarisir saat planning). Ragu = biarkan.
- **L-06:** UI jujur — sukses → `recordDeleted`, gagal → `recordDeleteFailed` (payload pesan, render merah DI DALAM partial). Tidak ada lagi respons gagal identik sukses.
- **L-07:** Online session dihapus via **refactor `DeleteManualAssessment` jadi endpoint per-session generik** (gate `IsManualEntry` dihapus; 1 endpoint layani manual + online), cascade sama, guard `Admin, HC` + antiforgery. BUKAN endpoint baru terpisah.
- **L-08:** File.Delete **POST-commit**, inner try/catch warn-only (pola fase 331-334). AuditLog 1 entri/operasi (aktor, node akar, jumlah + daftar Id turunan).

### Gray area yang diputuskan sesi ini
- **D-01 — Badge count fix (#16/#17): RECOMPUTE = baris tampil.** Count per jenis di tab Input Records = jumlah baris yang benar-benar tampil per jenis (online+manual+training sesuai list). Badge selalu cocok list, tak bikin admin bingung. (Bukan opsi relabel.)
- **D-02 — Duplicate-guard match (#12/#14): EXACT `user+judul+tanggal`.** Guard 3 pintu (AddManualAssessment, ImportTraining, BulkBackfill) tolak/skip hanya bila tanggal PERSIS sama. False-positive minim (re-entry tanggal beda lolos). **Penting:** ini BEDA dari heuristik mirror #15 yang pakai ±1 hari — toleransi ±1 hari HANYA untuk kandidat mirror di PREVIEW, BUKAN untuk guard pencegahan create. Perilaku per pintu (locked §3.3): single (AddManual) = **reject** dgn pesan; import/backfill = **skip-with-report** (kolom status "duplikat — dilewati").
- **D-03 — Preview modal friction: TOMBOL "Hapus Semua" SAJA.** Preview sudah tampilkan daftar korban persis → 1 klik konfirmasi, tanpa ketik-konfirmasi. Andalkan admin baca preview (mitigasi risiko = preview eksplisit + audit log + snapshot DB saat UAT). TIDAK ada gating ketik-kata.
- **D-04 — Dep 366 sequencing: ASUMSI 366 LAND DULU.** Plan 367 referensi image-cleanup helper 366 sebagai precondition; **eksekusi 367 gated setelah 366 ship**. Plan 367 WAJIB preserve helper image 366 di `DeleteAssessment`/`DeleteAssessmentGroup`/`DeletePrePostGroup` (3 endpoint overlap). 367 TIDAK mengabsorb scope image 366 (jaga separasi, no dobel logika). Planning 367 boleh jalan sekarang (doc-only, aman paralel sesi 364/371).

### Claude's Discretion (planner/researcher putuskan)
- Inventaris pola `ActionUrl` notif TrainingRecord (L-05) — researcher petakan saat planning.
- Struktur internal `RecordCascadeDeleteService` (BFS, signature preview vs execute) — §3.1 sudah beri blueprint, detail implementasi bebas.
- Ambang/threshold apa pun untuk telemetry — bukan keputusan user.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum planning/implementasi.**

### Spec utama (primary)
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` — Spec C FINAL. §1 = 28 temuan (26 baris, in-scope 27; 367=#1-12,#14-20). §2 = 6 kebijakan user final. §3.1 cascade engine blueprint. §3.2 endpoint+UI. §3.3 fix tetangga ber-tag [367]/[368]. §3.3b pembagian fase (kontrak planner). §3.4 testing strategy.

### Spec terkait (dependency)
- `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` §8.1 — lifecycle `PendingProtonBypasses` soft-cancel (`Status='Dibatalkan'` + `ResolvedAt`). Acuan L-04. (DRAFT-PAUSED varian = abaikan.)
- `.planning/phases/371-sesi-online-tampil-di-tab-input-records-visibility-only/371-01-SUMMARY.md` — seam UI online di `_TrainingRecordsTab.cshtml` (`@if(row.IsOnline)`, badge "Assessment Online", proyeksi anon `.Concat`). 367 bangun aksi hapus di atas seam ini (L-07).

### Pattern preseden (codebase)
- Gold standard delete artefak per-session: `Controllers/AssessmentAdminController.cs` DeleteAssessment (blueprint cascade §3.1, spec sebut ~:2270-2329 — verifikasi line, 363 mungkin geser).
- File-atomicity post-commit warn-only: pattern Phase 331-334 (L-08).
- Integration test real-SQL: pattern Phase 360 (assert per-tabel).
- File sertifikat [Fact] post-commit: preseden Phase 355 `Replace_NewFileWins_DeletesOldFileOnDisk`.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **DeleteAssessment gold standard** (`AssessmentAdminController.cs`) — blueprint RemoveRange artefak (EditLogs, PackageUserResponses, AttemptHistory, UserPackageAssignments, Packages+Q+O). Cascade engine execute-mode = parity dgn ini per node AssessmentSession.
- **371 `_TrainingRecordsTab.cshtml`** — sudah tampil online+manual+training, badge 3-way, proyeksi anon `.Concat(assessmentRows).Concat(onlineRows)`, kolom Aksi cabang `@if(row.IsOnline)`. 367 tambah tombol hapus per baris + flash error/sukses di partial.
- **`RemoveExamOriginAsync`** (`ProtonCompletionService.cs:70-86`) — cabut penanda Proton `Origin='Exam'`. Saat ini hanya dipanggil GradingService re-grade; cascade engine panggil saat hapus sesi Proton (#9).

### Established Patterns
- Pre-check renewal sbg BLOKIR (`ApplicationDbContext.cs:167-180, 235-243`; fase 325/329) → DIBALIK jadi preview cascade (L-03).
- HTMX `DeleteTabResult()` selalu 200 (`TrainingAdminController.cs:561-569`) = akar sukses-palsu #1 → dibedakan `recordDeleted`/`recordDeleteFailed` (L-06).
- Transaction wrap + File.Delete post-commit (fase 331-334).

### Integration Points
- **Service baru** `RecordCascadeDeleteService` (§3.1) — dipanggil dari `DeleteTraining`, `DeleteManualAssessment` (refactor generik L-07), + 3 endpoint tab 1 (DeleteAssessment/Group/PrePost).
- **Overlap 366:** 3 endpoint tab 1 = tempat 366 pasang image-cleanup helper → 367 preserve (D-04).
- **Overlap 368:** `TrainingAdminController.cs` dikelola via depends 368→367; planner 367 TIDAK sentuh item [368] (#21-27).

### Drift risk
- Spec C line numbers dari 2026-06-10; Phase 363 (shipped 2026-06-11) sentuh `AssessmentAdminController.cs` + `CMPController.cs` → researcher/planner WAJIB re-verify line sebelum edit.
</code_context>

<specifics>
## Specific Ideas

- Kasus lapangan acuan: Rino Adi Prasetyo (audit log Dev 2026-06-10 10:21) — hapus "berhasil" tapi worker masih lihat 2 sesi online (#3). Repro lokal via seed renewal-chain.
- Preview WAJIB tampilkan kandidat mirror legacy (#15) sbg checkbox opt-out, **default tercentang**, heuristik judul (`Judul == session.Title` ATAU `"Assessment: " + Title`) + tanggal ±1 hari. Heuristik TIDAK auto-hapus tanpa tampil di preview.
- UI hint = **yes** (modal preview, badge, flash di partial) → pertimbangkan `/gsd-ui-phase 367` sebelum plan untuk kontrak desain modal.
</specifics>

<deferred>
## Deferred Ideas

- **Phase 368 (#21-27):** edit atomic file, reset ET scores, one-time AttemptHistory legacy cleanup, import audit log, CertificationManagement dedup, EditTraining renewal validation, BulkBackfill kosmetik. Fase terpisah, depends 368→367.
- **Backlog 999.6:** Impersonate identity (#13) — tidak dipakai query worker surfaces.
- **Ditolak (out of scope, §3.5):** soft-delete/undo (opsi C ditolak user); tab 1 filter 7-hari tetap (kebutuhan hapus sesi lama dipenuhi via tab 2).
- **Reviewed todo (tidak folded):** `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` — bukan scope 367; eksekusi SETELAH 367 ship pakai cascade engine yang dibangun fase ini. Tetap di pending todos.
</deferred>

---

*Phase: 367-delete-records-cascade-overhaul*
*Context gathered: 2026-06-12*
