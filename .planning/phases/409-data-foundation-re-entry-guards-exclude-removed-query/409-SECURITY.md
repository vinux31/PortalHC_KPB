---
phase: 409
slug: 409-data-foundation-re-entry-guards-exclude-removed-query
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-21
---

# Phase 409 — Security

> Kontrak keamanan per-fase: register ancaman, risiko yang diterima, dan jejak audit.
> Phase ini membangun fondasi soft-remove peserta: migration skema + guard re-entry server-authoritative + exclude-removed dari query monitoring aktif.

---

## Trust Boundaries

| Boundary | Deskripsi | Data yang Melintas |
|----------|-----------|--------------------|
| Pekerja (browser) → CMPController | Tidak terpercaya: pekerja yang sudah soft-removed dapat reload StartExam atau POST SubmitExam langsung (bypass UI) | sessionId, jawaban ujian |
| Pekerja (SignalR) → AssessmentHub | Tidak terpercaya: koneksi SignalR yang sudah terbuka sebelum removal dapat invoke JoinBatch / SaveTextAnswer / SaveMultipleAnswer langsung | sessionId, jawaban essay/pilihan ganda |
| Admin/HC (browser) → AssessmentAdminController | Terpercaya (RBAC Admin+HC); monitoring batch-aktif harus mengecualikan peserta soft-removed dari hitungan dan daftar | data monitoring, InProgressCount, TotalCount |
| Developer → DB lokal (migration) | Semi-terpercaya: migrasi DDL apply ke HcPortalDB_Dev; env salah (Pitfall 6) bisa connect ke server yang salah | DDL schema AssessmentSessions |
| EF tool → snapshot | Semi-terpercaya: tool 10.0.3 vs project EF 8.0.0 — mismatch dapat menyebabkan annotation runtime break | Migration snapshot ProductVersion |

---

## Threat Register

| Threat ID | Kategori STRIDE | Komponen | Disposisi | Mitigasi | Status |
|-----------|-----------------|----------|-----------|----------|--------|
| T-409-01 | Elevation of Privilege | CMPController.StartExam — pekerja soft-removed me-resume ujian via reload | mitigate | Guard server-side `IsParticipantRemoved(assessment)` dipanggil SEBELUM mark-InProgress (:924); sesi removed di-redirect + TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."; test `StartExam_Blocks_RemovedSession` GREEN | closed |
| T-409-02 | Tampering | CMPController.SubmitExam — pekerja soft-removed re-submit via direct POST | mitigate | Guard server-side `IsParticipantRemoved(assessment)` dipanggil SEBELUM grading (:1611); jawaban di-discard, Score tidak berubah; test `SubmitExam_Blocks_RemovedSession` GREEN | closed |
| T-409-03 | Information Disclosure | AssessmentHub.JoinBatch — pekerja soft-removed re-join grup monitoring/batch | mitigate | Predikat `AnyAsync` += `&& s.RemovedAt == null` (:31); silent `return` tanpa throw; test `JoinBatch_Predicate_Rejects_RemovedSession` GREEN | closed |
| T-409-04 | Tampering | Migration chain — risiko ALTER hand-rolled memutus chain EF | mitigate | Migration di-scaffold via `dotnet ef migrations add` (bukan hand-roll); Down() simetris 3 DropColumn; `Migrations/20260621011101_AddParticipantRemovalColumns.cs` verified | closed |
| T-409-05 | Denial of Service | Snapshot version mismatch — tool 10.x menstempel EF10 di snapshot | mitigate | Local tool manifest `.config/dotnet-tools.json` pin `dotnet-ef` 8.0.0 (rollForward:false); snapshot `ProductVersion` = "8.0.0" terverifikasi (:20 ApplicationDbContextModelSnapshot.cs) | closed |
| T-409-06 | Tampering | Provider/connstring leak — SQLite tertarik saat migrasi tanpa env | mitigate | `ASPNETCORE_ENVIRONMENT=Development` wajib untuk semua `dotnet ef`/`dotnet run`; sqlcmd verify post-apply ke HcPortalDB_Dev lokal (60 baris, 3 kolom hadir) | closed |
| T-409-07 | Information Disclosure | Backfill destruktif pada baris existing — defaultValue dapat merusak data lama | accept | 3 kolom nullable additif (`nullable: true`, tanpa `defaultValue`); baris existing otomatis NULL; sqlcmd `COUNT(RemovedAt IS NOT NULL) = 0` dikonfirmasi | closed |
| T-409-08 | Tampering | AssessmentHub.SaveTextAnswer / SaveMultipleAnswer — pekerja soft-removed menulis jawaban via koneksi Hub hidup (bypass JoinBatch guard) | mitigate | Predikat `FirstOrDefaultAsync` += `&& s.RemovedAt == null` pada kedua method (:146 dan :213); silent return-if-null dipertahankan; defense-in-depth menutup gap PRMV-03 "jawaban setelah penghapusan tidak terhitung" | closed |
| T-409-09 | Repudiation / Availability | Over-exclude menyembunyikan sertifikat/riwayat peserta yang sah (UserAssessmentHistory) | mitigate | Exclude hanya pada 3 query monitoring batch-aktif (managementQuery :121, AssessmentMonitoring.query :2825, AssessmentMonitoringDetail.query :3335); NO global `HasQueryFilter`; `UserAssessmentHistory` (:5263) tidak disentuh; test boundary `UserAssessmentHistory_StillShows_RemovedSession` GREEN | closed |
| T-409-10 | Tampering | RemovalReason free-text — XSS saat dirender di panel | accept (out of 409) | Penulisan `RemovalReason` = Phase 411; render di panel "Peserta Dikeluarkan" = Phase 412. Phase 409 read-path only; EF parameterized mencegah injection di tulis; XSS-at-render ditangani Phase 412 (encode/escape wajib di sana) | closed |

*Status: open · closed*
*Disposisi: mitigate (implementasi diperlukan) · accept (risiko terdokumentasi) · transfer (pihak ketiga)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rasional | Diterima Oleh | Tanggal |
|---------|------------|----------|---------------|---------|
| AR-409-01 | T-409-07 | 3 kolom baru nullable additif tanpa defaultValue — baris existing AssessmentSessions otomatis NULL. Dikonfirmasi via sqlcmd: 60 baris existing, COUNT(RemovedAt IS NOT NULL) = 0. Tidak ada perubahan data, non-destruktif. | Orchestrator (Phase 409 design) | 2026-06-21 |
| AR-409-02 | T-409-10 | RemovalReason free-text XSS-at-render di-defer ke Phase 412 karena Phase 409 hanya mendefinisikan skema (penulisan = 411, render panel = 412). EF parameterized menjamin no SQL injection. Flag dilacak di 409-02-SUMMARY.md §Deferred. | Orchestrator (Phase 409 design) | 2026-06-21 |

---

## Unregistered Threat Flags dari SUMMARY.md

SUMMARY 409-02 `## Threat Flags` menyatakan: "Tidak ada threat surface baru di luar `<threat_model>` plan."

Tidak ada unregistered flag yang perlu dicatat.

---

## Catatan Review Code (WR-01 / WR-02 — SUDAH DITUTUP)

Review 409-REVIEW.md mengidentifikasi 2 warnings yang terlewat dari scope awal:

- **WR-01** (`SaveAnswer` MC path di CMPController tidak mem-filter `RemovedAt`): **SUDAH DIPERBAIKI** — guard `IsParticipantRemoved(session)` ditambahkan di :373 CMPController.cs SEBELUM upsert response. Bukti: `Controllers/CMPController.cs:372-374`.
- **WR-02** (daftar ujian aktif pekerja `Assessment` masih menampilkan sesi soft-removed): **SUDAH DIPERBAIKI** — `query = query.Where(a => a.RemovedAt == null)` ditambahkan di :218 CMPController.cs. Bukti: `Controllers/CMPController.cs:216-218`.

Kedua fix ter-merge ke dalam phase sebelum audit ini dijalankan. Pesan error WR-01 = "Anda telah dikeluarkan dari ujian ini." (konsisten dengan StartExam/SubmitExam — total **3 occurrence** di CMPController).

Warnings info non-blocking dari review (IN-01 / IN-02 / IN-03) tidak mempengaruhi status keamanan Phase 409 dan di-defer ke fase berikutnya:
- IN-01: Test JoinBatch mereplikasi predikat (bukan panggil Hub asli) — tech-debt testability, bukan security gap.
- IN-02: StartExam/SubmitExam wiring guard tidak diuji end-to-end via action (dependency berat controller) — diverifikasi manual; helper seam `IsParticipantRemoved` sendiri ter-cover test.
- IN-03: Warning "ada peserta mengerjakan" menghitung sesi soft-removed — nudge informasional non-blocking, defer ke Phase 411.

---

## Bukti Verifikasi Implementasi

| Threat ID | File | Baris | Bukti |
|-----------|------|-------|-------|
| T-409-01 | Controllers/CMPController.cs | :924-927 | `if (IsParticipantRemoved(assessment)) { TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."; return RedirectToAction("Assessment"); }` SEBELUM mark-InProgress |
| T-409-02 | Controllers/CMPController.cs | :1611-1614 | Guard identik SEBELUM grading; jawaban di-discard |
| T-409-03 | Hubs/AssessmentHub.cs | :31 | `AnyAsync(s => ... && s.RemovedAt == null)` |
| T-409-04 | Migrations/20260621011101_AddParticipantRemovalColumns.cs | :12-48 | 3 `AddColumn` nullable:true + 3 `DropColumn` simetris; tidak ada `defaultValue` |
| T-409-05 | Migrations/ApplicationDbContextModelSnapshot.cs | :20 | `.HasAnnotation("ProductVersion", "8.0.0")` |
| T-409-06 | .planning/phases/409-.../409-01-SUMMARY.md | §Migration Notes | sqlcmd verify post-apply; 3 kolom hadir; `ASPNETCORE_ENVIRONMENT=Development` wajib |
| T-409-07 | Migrations/20260621011101_AddParticipantRemovalColumns.cs | :14-31 | `nullable: true`, tanpa `defaultValue` pada ketiga AddColumn |
| T-409-08 | Hubs/AssessmentHub.cs | :146, :213 | `FirstOrDefaultAsync(s => ... && s.RemovedAt == null)` pada SaveTextAnswer dan SaveMultipleAnswer |
| T-409-09 | Controllers/AssessmentAdminController.cs | :121, :2825, :3335 | `.Where(a => a.RemovedAt == null)` — 3 occurrence eksplisit; `HasQueryFilter` = 0 occurrence di ApplicationDbContext; UserAssessmentHistory :5263 tidak disentuh |
| T-409-10 | (accepted) | — | EF parameterized pada write-path; XSS-at-render di-defer ke Phase 412 |
| WR-01 fix | Controllers/CMPController.cs | :372-374 | `IsParticipantRemoved(session)` pada SaveAnswer MC path |
| WR-02 fix | Controllers/CMPController.cs | :216-218 | `query = query.Where(a => a.RemovedAt == null)` pada daftar ujian aktif pekerja |

**Seam tunggal:** `CMPController.IsParticipantRemoved(AssessmentSession) => session.RemovedAt != null` (:2540) adalah single-source-of-truth deteksi removed — dipakai StartExam, SubmitExam, SaveAnswer (3 path).

**Suite test:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ParticipantRemoval"` → **6/6 GREEN** (3 guard + 2 exclude + 1 boundary). Full suite 569/569 GREEN, 0 regresi.

---

## Security Audit Trail

| Tanggal Audit | Total Ancaman | Closed | Open | Dijalankan Oleh |
|---------------|---------------|--------|------|-----------------|
| 2026-06-21 | 10 | 10 | 0 | gsd-security-auditor (Claude Sonnet 4.6) |

---

## Sign-Off

- [x] Semua ancaman memiliki disposisi (mitigate / accept)
- [x] Risiko yang diterima terdokumentasi di Accepted Risks Log (AR-409-01, AR-409-02)
- [x] `threats_open: 0` dikonfirmasi
- [x] `status: verified` ditetapkan di frontmatter

**Approval:** verified 2026-06-21
