---
phase: 411-remove-restore-backend-live
milestone: v32.5
secured_at: 2026-06-21
asvs_level: 1
block_on: HIGH
threats_total: 14
threats_closed: 14
threats_open: 0
status: SECURED
---

# Phase 411: Security Verification — Remove + Restore Backend Live

**Milestone:** v32.5 Flexible Add/Remove Peserta
**ASVS Level:** 1 (block on HIGH)
**Verifikasi:** 2026-06-21
**Hasil:** SECURED — 14/14 threat mitigated, threats_open: 0

Auditor memverifikasi setiap threat di `<threat_model>` PLAN (411-01 produksi + 411-02 test) terhadap kode terimplementasi NYATA (file:line), bukan scan buta. Implementasi READ-ONLY. Tak ada perubahan kode.

---

## Ringkasan

Phase 411 menambah backend remove/restore peserta live: satu core privat `RemoveParticipantCoreAsync` (hybrid hard/soft + Pre/Post pair-as-unit via `LinkedSessionId`) dibungkus 3 endpoint POST (`RemoveParticipantLive` JSON, `RestoreParticipantLive` JSON, `DeleteAssessmentPeserta` redirect), plus un-hide form delete di `EditAssessment.cshtml`.

**Postur keamanan: SOLID.** Ketiga endpoint membawa `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` — dan ini **load-bearing**: `Program.cs:13` memakai `AddControllersWithViews()` polos TANPA `FallbackPolicy`/`AutoValidateAntiforgeryToken` global, jadi atribut per-endpoint adalah SATU-SATUNYA proteksi auth+CSRF. Proton ditolak sebelum write. Bind hanya skalar primitif (no mass-assignment). Keputusan hard/soft 100% server-side dari kolom DB. Soft-remove TIDAK menyentuh `Score`/`IsPassed`/`NomorSertifikat`/`ManualSertifikatUrl`/`Status`/`response` (terverifikasi via test B1/B2 reload DB nyata). Audit soft kini IN-transaction (fix WR-01). Cascade D-01 (UPA) terverifikasi `RecordCascadeDeleteService:221-222`.

**0 Critical, 0 High terbuka.** Review 0C/3W/3I; WR-01/WR-02/WR-03 + IN-03 FIXED (`da2d2b8a`, `e51dc99b`). Build 0 error; suite 597/597 + remove-filter 16/16.

---

## Threat Verification — Produksi (411-01)

| Threat ID | Category | Disposition | Evidence (file:line) | Status |
|-----------|----------|-------------|----------------------|--------|
| T-411-01 | Spoofing/Tampering (CSRF) | mitigate | `[ValidateAntiForgeryToken]` di ketiga endpoint — `AssessmentAdminController.cs:2555` (Remove), `:2584` (Restore), `:2620` (DeletePeserta). Form delete `@Html.AntiForgeryToken()` `EditAssessment.cshtml:667`. **Load-bearing:** `Program.cs:13` `AddControllersWithViews()` polos, tak ada antiforgery global. | CLOSED |
| T-411-02 | Elevation (akses non-Admin/HC) | mitigate | `[Authorize(Roles = "Admin, HC")]` ketiga endpoint — `:2554`, `:2583`, `:2619`. | CLOSED |
| T-411-03 | Tampering (IDOR via sessionId tebakan) | accept | Admin/HC berwenang lintas-peserta (by-design RBAC). `sessionId` di-load + 404 bila tak ada (`:2559`, `:2588`, `:2624`). Partner Pre/Post di-resolve SERVER-side via `LinkedSessionId` (`:2671-2672`, `:2597-2599`), bukan id dari client → no IDOR ke partner. Tak ada eskalasi di bawah Admin/HC. | CLOSED |
| T-411-04 | Tampering (korup ProtonTrack) | mitigate | Reject `Category=="Assessment Proton"` → 400 sebelum write: `RemoveParticipantLive:2562`, `DeleteAssessmentPeserta:2625`. Restore IMPLICIT (IN-01): sesi Proton tak pernah bisa soft-removed (RemoveLive reject duluan) → `RestoreParticipantLive` selalu kena guard `RemovedAt==null` 400 (`:2589`). Outcome (no Proton mutation) terpenuhi. | CLOSED |
| T-411-05 | Tampering (client paksa hard vs soft) | mitigate | Keputusan jalur 100% server-side dari kolom DB via `SessionHasDataAsync:2657-2663` (`StartedAt != null` ATAU `PackageUserResponse` count). Client TIDAK punya input ke jalur (hanya `sessionId`+`reason`). | CLOSED |
| T-411-06 | Repudiation (non-repudiation) | mitigate | Audit `AuditLogService.LogAsync` tiap operasi: Remove soft `:2708` (mode + reason), Remove hard `:2750`, Restore `:2608` — actorId/actorName/timestamp. **WR-01 fix:** audit soft kini DALAM `BeginTransactionAsync`→`SaveChangesAsync`→`LogAsync`→`CommitAsync` (`:2694-2712`) dengan `try/catch`→`RollbackAsync`→re-throw: tak ada committed soft-remove tanpa audit row. PLIV-03 terpenuhi atomik. | CLOSED |
| T-411-07 | Tampering (soft-remove bocor mutasi cert/score) | mitigate | Soft HANYA set 3 kolom removal (`:2702-2704` RemovedAt/RemovedBy/RemovalReason). `Score`/`IsPassed`/`NomorSertifikat`/`ManualSertifikatUrl`/`Status`/`response` UNCHANGED (Pitfall 2). Diverifikasi test NYATA reload DB: B1 (`Score==80`, `Status==InProgress`, `IsPassed`) + B2 (`NomorSertifikat`, `ManualSertifikatUrl`, `IsPassed`, `Status`, `Score` semua UNCHANGED) — `FlexibleParticipantRemoveTests.cs:418-420, 458-462`. Cert utuh + reversibel. | CLOSED |
| T-411-08 | Elevation (HC hapus cert tanpa jejak, EnsureCanDeleteAsync longgar) | mitigate | Keputusan #5 (spec §H): TIDAK panggil `EnsureCanDeleteAsync` di core (`:2724`) — mitigasi bukan block: (a) soft-remove → cert UTUH (T-411-07), (b) audit WAJIB `RemovedBy`/`RemovalReason`, (c) reason-gate D-02 `:2679-2680` → 400 "Alasan penghapusan wajib diisi." bila soft tanpa reason. Hard hanya not-started (`anyHasData==false`) → guard no-op natural. | CLOSED |
| T-411-09 | Mass-assignment / over-post | mitigate | Endpoint bind hanya skalar primitif: `RemoveParticipantLive(int sessionId, string? reason)`, `RestoreParticipantLive(int sessionId)`, `DeleteAssessmentPeserta(int sessionId, int returnToId)`. Bukan entity-bind → tak ada surface over-post. | CLOSED |
| T-411-10 | Information Disclosure (error leak) | mitigate | Pesan generik ke client "Gagal menghapus peserta." (`:2744`); detail teknis (PAIR-HALF-DELETED, ErrorMessage) hanya ke `_logger.LogError` (`:2740-2743`). | CLOSED |
| T-411-11 | DoS (mass-delete) | accept | Single-session per call (`sessionId` tunggal skalar), bukan batch → risiko rendah, tak perlu cap. | CLOSED |

## Threat Verification — Test (411-02)

| Threat ID | Category | Disposition | Evidence | Status |
|-----------|----------|-------------|----------|--------|
| T-411T-01 | Tampering (test tautologis) | mitigate | De-tautology 999.12: test drive action ASLI + assert kolom DB NYATA. `grep SessionHasDataAsync == 0` + `grep ExecuteAsync == 0` (tak replica/bypass core) — diverifikasi review. Suite remove-filter 16/16 PASS. | CLOSED |
| T-411T-02 | Tampering (test hapus DB Dev) | mitigate | REUSE `FlexibleParticipantAddFixture` → `HcPortalDB_Test_{guid}` disposable (`EnsureDeletedAsync` di `DisposeAsync`); HcPortalDB_Dev TIDAK disentuh. | CLOSED |
| T-411T-03 | Info Disclosure (false-green tanpa SQLEXPRESS) | accept | SQLEXPRESS tersedia (dipakai 409/410, VERIFIED); absent → Integration trait gagal nyata (bukan silent-skip). Read-path InMemory tetap jalan. | CLOSED |

---

## Unregistered Flags

Tidak ada `## Threat Flags` di SUMMARY (411-01-SUMMARY / 411-02-SUMMARY) yang tak terpetakan ke threat ID di atas. Tidak ada unregistered flag.

---

## Residual Risk (accepted, NON-HIGH)

### WR-03 — Hard-delete pasangan Pre/Post tidak atomik lintas dua sesi
**Klasifikasi:** Data-integrity residual, BUKAN celah keamanan HIGH. **Disposition: accept.**
Jalur hard me-loop `cascade.ExecuteAsync` per sesi; tiap cascade atomik 1-tx individual, tapi loop dua-cascade tak atomik sebagai unit (desain `RecordCascadeDeleteService`: nested-tx pada ctx berbagi akan konflik → tak bisa dibungkus satu tx tanpa refactor besar). Mitigasi terpasang (`:2725-2746`):
- Pra-validasi both-clean (`anyHasData==false`) → kegagalan di-tengah-pair sangat langka.
- Bila tetap terjadi, `_logger.LogError("PAIR-HALF-DELETED ...")` eksplisit dgn deletedIds + failedId + title + alasan → terdeteksi ops untuk rekonsiliasi manual (bukan silent), lalu return `Fail` jelas.

**Penilaian keamanan:** Bukan privilege escalation, bukan disclosure, bukan tamper-cert (hard hanya not-started + 0 response = tanpa cert/score). Konsekuensi = inkonsistensi data langka yang ter-log untuk ops. ASVS L1 tak mensyaratkan distributed-tx untuk skenario ini. **Acceptable residual — tidak memblokir.**

### IN-02 — Peserta soft-removed masih tampil di query `EditAssessment`
**Klasifikasi:** Tampilan/konsistensi, BUKAN keamanan. **Disposition: defer ke Phase 412.**
Query `assignedUsers` GET `EditAssessment` (`:1687-1690`, `:1729-1734`) tak filter `RemovedAt==null`. Idempotency endpoint (`RemovedAt!=null` → no-op "Peserta sudah dikeluarkan.") menangkap klik ulang → tak berbahaya. Scope panel "Peserta Dikeluarkan" = Phase 412. Tak ada celah keamanan.

---

## Verification Gates

- [x] Semua `<required_reading>` dimuat sebelum analisis (PLAN 01/02, CONTEXT, REVIEW, REVIEW-FIX, spec §H, controller, test).
- [x] Threat register diekstrak dari kedua `<threat_model>` (11 produksi + 3 test = 14).
- [x] Tiap threat diverifikasi per disposition (mitigate → grep pattern di file dikutip; accept → terdokumentasi).
- [x] RBAC `[Authorize(Roles="Admin, HC")]` NYATA di 3 endpoint (`:2554/:2583/:2619`).
- [x] CSRF `[ValidateAntiForgeryToken]` NYATA di 3 endpoint (`:2555/:2584/:2620`) + form token (`EditAssessment.cshtml:667`); load-bearing (no global antiforgery `Program.cs:13`).
- [x] Proton-reject NYATA (`:2562`, `:2625`; restore implicit).
- [x] Mass-assignment: bind skalar saja (verified signatures).
- [x] Non-repudiation: audit soft IN-tx (WR-01 fix `:2694-2712`), restore + hard audit.
- [x] Soft-remove invariant: hanya 3 kolom, cert/score/status UNCHANGED (test B1/B2 reload DB nyata).
- [x] WR-03 half-delete residual dinilai NON-HIGH → accepted.
- [x] Implementasi TIDAK dimodifikasi (READ-ONLY).
- [x] SECURITY.md ditulis ke path benar.

**threats_open: 0** — tak ada HIGH/Critical terbuka. Gate block_on=HIGH: PASS.
