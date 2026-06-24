# Phase 423: Certificate Issuance Consistency - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Konsolidasi penerbitan sertifikat ke **satu helper bersama** `ShouldIssueCertificate` (belum ada — dibuat di fase ini) sehingga aturan kelayakan cert konsisten di seluruh jalur. Cakupan: kelayakan terbit (Pre-Test selalu ditolak), jaminan ValidUntil saat issue non-Pre, penomoran seq atomik tanpa race, pemisahan namespace manual vs auto, guard anti double-cert yang tak bisa di-bypass, dan penanda umur sesi PendingGrading. **migration=FALSE** (refactor helper + guard; tidak ada schema/write DB baru).

Bukan cakupan fase ini: dedupe scoring & gating Pre→Post (Phase 424), penamaan/dead-field cosmetic (Phase 425).
</domain>

<decisions>
## Implementation Decisions

### Cakupan helper (CERT-01)
- **D-01:** `ShouldIssueCertificate` jadi **SATU gate kelayakan cert untuk SEMUA jalur**: `GradingService.GradeAndCompleteAsync` (:287) + `GradingService.RecomputeAfterEssayGradingAsync` (:520) + jalur manual `AddManualAssessment`. Manual **berhenti hardcode** `GenerateCertificate=true` (FLD-5.2-02) dan tunduk aturan helper yang sama. Helper konsisten menolak `AssessmentType == "PreTest"` di semua kasus (FLD-5.2-10). Saat ini hanya jalur :520 yang cek PreTest; :287 tidak — divergensi ini ditutup.

### Penomoran cert (CERT-03 atomik + CERT-04 namespace)
- **D-02 (namespace):** Nomor cert manual tetap **free-text** tapi **divalidasi TIDAK boleh menyerupai format auto** `KPB/{seq}/{ROMAN}/{YEAR}` (regex/format check); insert manual dibungkus `try/catch DbUpdateException` (pakai `CertNumberHelper.IsDuplicateKeyException`) → **pesan error ramah** saat kolisi (bukan 500). TIDAK mengubah tampilan nomor cert yang sudah tercetak (tanpa prefix baru).
- **D-03 (seq atomik):** Harden generator seq (`CertNumberHelper.GetNextSeqAsync` MAX+1 race-prone, GRD-08) — perbanyak retry + jitter di atas pola existing (retry-3x + filtered `WHERE NomorSertifikat == null` + unique index `IX_AssessmentSessions_NomorSertifikat`). **Tanpa tabel SEQUENCE baru** (migration=FALSE). Perilaku saat seq tetap gagal terbit setelah retry (finalize burst): **sesi tetap selesai TANPA cert (non-destruktif, worker tak terblok) + ditandai agar HC bisa terbitkan/retry cert manual**. (Ganti perilaku sekarang yang log-saja diam-diam.)

### Type x ValidUntil (CERT-02 + CERT-06)
- **D-04:** Saat cert diaktifkan untuk sesi non-Pre, **ValidUntil wajib ditangani eksplisit** (tak terbit tanpa masa berlaku secara tak sengaja). `CertificateType` "Permanent" → **tolak ValidUntil** (harus null); "Annual" → **+1 tahun**; "3-Year" → **+3 tahun**.
- **D-05 (tanggal dasar):** Turunan ValidUntil dihitung dari **`CompletedAt` (tanggal selesai ujian)**, konsisten dgn paritas grading existing. Deterministik & adil bagi peserta (bukan tanggal terbit/hari-ini).
- **D-06 (retroaktif):** **Hanya berlaku ke depan** — baris lama dengan mismatch `CertificateType`×`ValidUntil` (mis. Permanent tapi ada ValidUntil) **TIDAK disentuh** (migration=FALSE; hindari mengubah cert yang sudah tercetak). Aturan baru hanya untuk penerbitan baru.

### Anti double-cert (CERT-05)
- **D-07:** Guard server-side cegah **dua cert AKTIF** untuk (peserta, judul) sama. Definisi **"aktif" = `ValidUntil == null` (permanen) ATAU `ValidUntil >= hari ini`** (belum kedaluwarsa). **Pengecualian renewal**: sesi dengan `RenewsSessionId` terisi (perpanjangan resmi) **lolos** guard. Cert kedaluwarsa boleh terbit lagi. Guard **tidak bisa di-bypass** via `ConfirmDuplicateTitle` (VAL-04 — `ConfirmDuplicateTitle` hanya override soft-block judul, BUKAN guard cert-aktif domain).

### PendingGrading age (CERT-07)
- **D-08 (tampil di):** Umur sesi "Menunggu Penilaian" ditampilkan di **halaman EssayGrading DAN daftar ManageAssessment** (dua tempat HC bekerja). **TANPA auto-finalize** — hanya penanda visibilitas.
- **D-09 (ambang):** Penanda umur: **>3 hari = kuning, >7 hari = merah**. Teks umur + badge warna.

### Claude's Discretion
- Penempatan persis & nama kelas helper baru (`ShouldIssueCertificate`) — kemungkinan kelas pure `Helpers/CertIssuanceRules.cs` (analog `SessionEditLockRules` dari 422) agar testable real-SQL-free; planner/researcher tentukan.
- Bentuk penanda "cert gagal terbit utk HC manual" (kolom flag existing vs TempData/log + tampilan) — selama non-destruktif & terlihat HC.
- Jumlah retry & strategi jitter konkret (selama lebih tahan burst dari retry-3x sekarang).
- Format teks umur PendingGrading (mis. "Menunggu N hari").
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit & requirements
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` — sumber temuan: GRD-01 (gate cert tak konsisten cek PreTest), GRD-05 (PendingGrading menggantung), GRD-06 (cert tanpa expiry), GRD-08 (seq non-atomik race), GRD-10/VAL-04 (anti double-cert + bypass ConfirmDuplicateTitle), FLD-5.2-02 (manual hardcode cert=true), FLD-5.2-07 (dua sumber penomoran), FLD-5.2-09 (CertificateType overlap ValidUntil), FLD-5.2-10 (Pre-non-cert hanya default+warning JS)
- `.planning/REQUIREMENTS.md` §CERT — CERT-01..07 (definisi acceptance)
- `.planning/ROADMAP.md` §"Phase 423" — goal + 5 Success Criteria

### Prior phase (pola yang diikuti)
- `.planning/phases/422-samepackage-shuffle-integrity/422-CONTEXT.md` — pola pure-helper + guard server-side + test real-SQL (carry-forward)

[Tidak ada ADR eksternal — aturan bisnis tercakup di audit + decisions di atas.]
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/CertNumberHelper.cs` — `Build(seq, date)` (format `KPB/{seq:D3}/{ROMAN}/{YEAR}`), `GetNextSeqAsync` (MAX+1, perlu di-harden), `IsDuplicateKeyException(DbUpdateException)` (sudah ada utk collision-safe), `ToRomanMonth`.
- Pola dari 422: pure helper (`SessionEditLockRules`, `ShuffleToggleRules`) + guard server-side di endpoint POST + test real-SQL `IClassFixture<RetakeServiceFixture>` (`NoOpHubContext`, ctor recipe `GradingService` dari `SubmitResurrectionTests`).

### Established Patterns
- Anti double-cert existing: `GradingService.cs:287-316` retry-3x + filtered `WHERE NomorSertifikat == null` di bawah unique index `IX_AssessmentSessions_NomorSertifikat`.
- ValidUntil = `DateOnly?` (TZ-01 v19.0 DateOnly refactor) — bukan DateTime.
- `DeriveCertificateStatus` (`Models/CertificationManagementViewModel.cs:54-66`) short-circuit "Permanent" sebelum cek ValidUntil (FLD-5.2-09).

### Integration Points
- `Services/GradingService.cs` — `GradeAndCompleteAsync` (gate :287 TANPA cek PreTest), `RecomputeAfterEssayGradingAsync` (gate :520 cek PreTest); seq via `CertNumberHelper`.
- `Controllers/AssessmentAdminController.cs` — `ConfirmDuplicateTitle` (:871, :997 soft-block judul), `FinalizeEssayGrading` (:3677-3911 cert block paritas), manual entry.
- `Controllers/TrainingAdminController.cs` — `AddManualAssessment` (~:733-765 manual `NomorSertifikat` free-text tanpa try/catch, hardcode cert=true).
- `Models/AssessmentConstants.cs:24` `CertificateType` (Permanent/Annual/3-Year); `Models/AssessmentSession.cs:166` `CertificateType`, `ValidUntil`, `RenewsSessionId`, `NomorSertifikat`, `GenerateCertificate`, `Status`, `CompletedAt`.
- UI: halaman EssayGrading (`Views/Admin/EssayGrading.cshtml` / page) + daftar ManageAssessment — render umur PendingGrading.

### New
- Helper pure `ShouldIssueCertificate` (single source-of-truth kelayakan cert) — gate di 3 jalur.
</code_context>

<specifics>
## Specific Ideas

- Manual namespace: validasi-tolak format menyerupai auto **lebih disukai daripada prefix baru** karena tak mengubah nomor cert yang sudah/akan tercetak.
- Seq-failure: non-destruktif (worker tak terblok) adalah prioritas — selaras filosofi v32.x (lifecycle non-destruktif, lihat RTH-01/RTK-LOGIC-02 di Phase 421).
</specifics>

<deferred>
## Deferred Ideas

- Cert/analytics atribusi per-unit akurat (kolom unit-at-issue + backfill) — sudah di backlog v2 REQUIREMENTS v32.3 (butuh migration ke-2). Tidak masuk 423.
- Sinkronisasi `DeriveCertificateStatus` agar konsumen status membaca Annual/3-Year — bila tak terselesaikan oleh CERT-06, masuk Phase 425 (cosmetic/tech-debt) atau backlog.

None lain — diskusi tetap dalam scope fase.
</deferred>

---

*Phase: 423-certificate-issuance-consistency*
*Context gathered: 2026-06-24*
