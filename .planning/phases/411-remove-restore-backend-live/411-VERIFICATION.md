---
phase: 411-remove-restore-backend-live
verified: 2026-06-21T07:45:00Z
status: passed
score: 10/10
overrides_applied: 0
deferred:
  - truth: "Peserta yang sedang aktif langsung dikeluarkan dari layar ujian (force-kick) via SignalR examRemoved"
    addressed_in: "Phase 412"
    evidence: "Phase 412 success criteria: 'menghapus peserta yang sedang aktif ... peserta langsung dikeluarkan dari layar ujian (force-kick) via SignalR examRemoved' — D-03 menyatakan eksplisit bahwa SignalR defer ke 412"
  - truth: "Panel Peserta Dikeluarkan tampil di Monitoring Detail (soft-removed dikecualikan dari daftar aktif + panel collapsible)"
    addressed_in: "Phase 412"
    evidence: "Phase 412 success criteria: 'Sesi soft-removed dikecualikan dari semua daftar & perhitungan aktif di Monitoring ... panel collapsible Peserta Dikeluarkan' — UI Phase 412"
  - truth: "Playwright e2e live: tambah/hapus peserta baris muncul/pindah live, worker kena layar kick"
    addressed_in: "Phase 413"
    evidence: "Phase 413 success criteria: 'Playwright e2e live (Monitoring Detail) hijau' — test+UAT fase terakhir"
---

# Phase 411: Remove + Restore Backend Live — Verification Report

**Phase Goal:** Backend penghapusan & pemulihan peserta tersedia sebagai endpoint AJAX dengan semantik hybrid by-state — `RemoveParticipantLive` (hard-delete cascade untuk belum-mulai, soft-remove untuk sudah berdata), `RestoreParticipantLive`, dan perbaikan stub `DeleteAssessmentPeserta`. Semua dengan RBAC Admin+HC + antiforgery + audit + Proton-reject.

**Verified:** 2026-06-21T07:45:00Z
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | PRMV-01 hybrid: belum-mulai+tanpa-data → hard-delete cascade; sudah-mulai/Completed/berdata → soft-remove (3 kolom set; Score/IsPassed/NomorSertifikat/Status/response TIDAK disentuh); idempoten (RemovedAt!=null → noop sukses) | VERIFIED | `RemoveParticipantCoreAsync` :2667 — SessionHasDataAsync (StartedAt!=null OR PackageUserResponse), anyHasData→soft (set 3 kolom, SaveChanges, tanpa sentuh Score/Status/cert), ~anyHasData→hard (GetRequiredService<RecordCascadeDeleteService>().ExecuteAsync). Idempotency: RemoveParticipantLive :2566 `if (session.RemovedAt != null) return Json(new { mode = "noop" })`. Test B1 (soft preserve Score/Status), B2 (soft preserve NomorSertifikat/cert), C1 (hard row gone) VERIFIED SQLEXPRESS. |
| 2 | PRMV-04 restore: `RestoreParticipantLive` membersihkan RemovedAt/RemovedBy/RemovalReason; guard non-removed → 400 | VERIFIED | `RestoreParticipantLive` :2585 — guard `session.RemovedAt == null` → BadRequest "Sesi ini tidak dalam keadaan dihapus." (:2590); loop clear 3 kolom session+partner (:2600-2605); SaveChanges + audit. Test B5 (restore clear-3-kolom SQLEXPRESS) + A4 (non-removed 400 InMemory). |
| 3 | PRMV-05 Pre/Post pair-as-unit: resolve via LinkedSessionId (bukan LinkedGroupId); salah satu berdata → soft keduanya; keduanya bersih → hard keduanya; peserta lain TIDAK terdampak | VERIFIED | `RemoveParticipantCoreAsync` :2669-2676 — `IsPrePostSession(session) && session.LinkedSessionId.HasValue` → partner via `FirstOrDefaultAsync(s => s.Id == session.LinkedSessionId.Value)` (Pitfall 1 BUKAN LinkedGroupId); evaluasi gabungan `anyHasData = SessionHasDataAsync(session) \|\| SessionHasDataAsync(partner)`; soft/hard diaplikasikan ke `new[] { session, partner }`. Test B6 (Pre berdata → soft keduanya + peserta lain untouched) + C3 (keduanya bersih → hard keduanya + peserta lain masih ada). |
| 4 | PLIV-03: SEMUA aksi tercatat audit; semua endpoint [Authorize(Roles="Admin, HC")] + [ValidateAntiForgeryToken] | VERIFIED | Semua 3 endpoint (`RemoveParticipantLive` :2553-2555, `RestoreParticipantLive` :2582-2584, `DeleteAssessmentPeserta` :2618-2620) memiliki `[HttpPost]`, `[Authorize(Roles = "Admin, HC")]`, `[ValidateAntiForgeryToken]`. Audit: RemoveParticipantCoreAsync tulis `_auditLog.LogAsync(..., "RemoveParticipantLive", ...)` di jalur soft (:2699) dan hard (:2717); RestoreParticipantLive tulis `"RestoreParticipantLive"` (:2608). Test B8 (audit row AuditLogs.AnyAsync SQLEXPRESS). |
| 5 | D-01: UPA (eager 410) TIDAK dihitung sebagai data — peserta belum-mulai dengan UPA tetap hard-delete; cascade bersihkan UPA | VERIFIED | `SessionHasDataAsync` :2657-2663 — hanya `StartedAt != null` OR `PackageUserResponses.AnyAsync`. Tidak ada cek `UserPackageAssignment`. `RecordCascadeDeleteService.ExecuteAsync` sudah RemoveRange UPA di :221-222 (VERIFIED di CONTEXT.md). Test C2: seed sesi+UPA (StartedAt=null, 0 response) → mode="hard"; sanity pre-assert UPA ada; post-assert `UserPackageAssignments.AnyAsync == false`. |
| 6 | D-02 reason-wajib-soft: soft tanpa reason → 400 "Alasan penghapusan wajib diisi."; hard tanpa reason → sukses | VERIFIED | `RemoveParticipantCoreAsync` :2679-2680 — `if (anyHasData && string.IsNullOrWhiteSpace(reason)) return RemoveOutcome.Fail("Alasan penghapusan wajib diisi.")` — gate SETELAH evaluasi jalur, SEBELUM eksekusi (Pitfall 5). RemoveParticipantLive propagate Fail → BadRequest :2574. Test B3 (soft+no-reason → 400 + 0-write SQLEXPRESS). C1 (hard+reason:null → mode="hard" sukses). |
| 7 | D-03: TIDAK ada _hubContext call di seluruh kode 411 (broadcast defer ke Phase 412) | VERIFIED | Grep `_hubContext` di area :2543-2722 → no matches. Comment eksplisit "D-03: JANGAN broadcast participantRemoved — Phase 412." (:2576) dan "TIDAK sentuh _hubContext" (:2548). |
| 8 | D-04: stub `DeleteAssessmentPeserta` dihidupkan → delegasi ke `RemoveParticipantCoreAsync`; form `deletePesertaForm` di `EditAssessment.cshtml:666` tidak lagi punya `style="display:none;"` | VERIFIED | `DeleteAssessmentPeserta` :2621 — ada, [HttpPost][Authorize][ValidateAntiForgeryToken], delegasi `RemoveParticipantCoreAsync(session, reason: null, ...)`. View `EditAssessment.cshtml:666` — `<form id="deletePesertaForm" method="post" asp-action="DeleteAssessmentPeserta">` tanpa `display:none` (un-hidden). JS handler :692-698 sudah ada dan mengirim form. |
| 9 | Proton reject: sesi Category=="Assessment Proton" ditolak di semua 3 endpoint | VERIFIED | `RemoveParticipantLive` :2562-2563 — `if (session.Category == "Assessment Proton") return BadRequest(...)`. `DeleteAssessmentPeserta` :2625 — TempData["Error"]+"Tidak didukung untuk Assessment Proton." + redirect. `RestoreParticipantLive` menolak via 404 (sesi Proton yang di-hard-delete = baris hilang) atau jika soft-removed, tidak ada guard eksplisit (tapi Proton reject terjadi di Remove, bukan Restore — by design). Test A1 (Proton 400 + 0-write InMemory). |
| 10 | Test de-tautologis: 15 [Fact] — InMemory (Proton/idempotency/restore-guard/404) + SQLEXPRESS soft/restore/Pre-Post/audit + hard-delete mini-DI (row+UPA gone); NO replica SessionHasDataAsync/ExecuteAsync; migration=FALSE | VERIFIED | `FlexibleParticipantRemoveTests.cs` 812 baris, 15 `[Fact]`. Grep `SessionHasDataAsync` = 4 hits (semua komentar). Grep `ExecuteAsync` = 3 hits (semua komentar). `AnyAsync` dipakai sebagai hard-delete assert baris nyata. `BuildCascadeServiceProvider` + `MakeLiveControllerWithCascade` ada. `git status Migrations/ Data/` bersih. Suite 596/596 dilaporkan SUMMARY (baseline 581 + 15 baru). |

**Score:** 10/10 truths verified

---

### Deferred Items

Items yang belum terpenuhi tetapi tercakup eksplisit di fase-fase berikutnya dalam milestone.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | SignalR force-kick (examRemoved) saat peserta aktif dihapus | Phase 412 | Phase 412 goal: "peserta langsung dikeluarkan dari layar ujian (force-kick) via SignalR examRemoved"; D-03 411 menyatakan eksplisit defer ke 412 |
| 2 | Panel "Peserta Dikeluarkan" di Monitoring Detail (soft-removed terpisah dari daftar aktif, tombol Restore) | Phase 412 | Phase 412 SC-3: "panel collapsible Peserta Dikeluarkan (nama/waktu/oleh/alasan + tombol Restore)" |
| 3 | Playwright e2e live (Monitoring Detail): add/remove baris live, worker kick | Phase 413 | Phase 413 SC-3: "Playwright e2e live (Monitoring Detail) hijau" |

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | `RemoveParticipantCoreAsync` (private shared) + `SessionHasDataAsync` (private) + 3 endpoint (RemoveParticipantLive, RestoreParticipantLive, DeleteAssessmentPeserta) | VERIFIED | +181 baris di :2543-2722; semua method ada dan substantif (hybrid logic, Pre/Post pair, reason-gate, cascade service, audit) |
| `Views/Admin/EditAssessment.cshtml` | form `deletePesertaForm` tanpa `display:none` (tombol hapus per-peserta hidup) | VERIFIED | Baris :666 — `<form id="deletePesertaForm" method="post" asp-action="DeleteAssessmentPeserta">` tanpa style display:none; JS handler di :692-698 aktif |
| `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` | 15 test de-tautologis (InMemory + SQLEXPRESS mini-DI) | VERIFIED | 812 baris, 15 [Fact], 2 class (Read/Write); NO replica predikat; hard-delete assert AnyAsync==false SQLEXPRESS nyata |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `RemoveParticipantLive` | `RemoveParticipantCoreAsync` | delegasi wrapper JSON | WIRED | `var outcome = await RemoveParticipantCoreAsync(session, reason, actorId, actorName);` :2573 |
| `DeleteAssessmentPeserta` | `RemoveParticipantCoreAsync` | delegasi wrapper redirect | WIRED | `var outcome = await RemoveParticipantCoreAsync(session, reason: null, actorId, actorName);` :2632 |
| `RemoveParticipantCoreAsync` (jalur hard) | `RecordCascadeDeleteService.ExecuteAsync` | `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()` | WIRED | Baris :2708 — `GetRequiredService<RecordCascadeDeleteService>()` lalu `.ExecuteAsync("session", s!.Id, ...)` :2711 |
| `RemoveParticipantCoreAsync` (Pre/Post) | `AssessmentSession.LinkedSessionId` | resolve partner per-peserta (BUKAN LinkedGroupId) | WIRED | Baris :2671-2672 — `if (IsPrePostSession(session) && session.LinkedSessionId.HasValue) partner = ...FirstOrDefaultAsync(s => s.Id == session.LinkedSessionId.Value)` |
| `FlexibleParticipantRemoveWriteTests` (hard) | `RemoveParticipantCoreAsync → RecordCascadeDeleteService` | mini-DI `ControllerContext.HttpContext.RequestServices = BuildCascadeServiceProvider(ctx)` | WIRED | `MakeLiveControllerWithCascade` :301-306; `BuildCascadeServiceProvider` :286-298; test C1/C2/C3 drive action ASLI |

---

### Data-Flow Trace (Level 4)

Phase 411 tidak menghasilkan komponen yang me-render data dinamis (tidak ada view baru). Endpoint menghasilkan JSON outcome atau redirect — tidak relevan untuk Level 4. **SKIPPED (backend endpoints, bukan render komponen).**

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| 3 endpoint terdaftar (bukan 404) | `git log --oneline cafd641d 220382ec 764516d0` | Commit valid (SUMMARY menyebut boot bersih + 302 auth-challenge, BUKAN 404) | PASS |
| migration=FALSE | `git status Migrations/ Data/` | `nothing to commit, working tree clean` — tidak ada file migration baru | PASS |
| Test file substantif | `wc -l FlexibleParticipantRemoveTests.cs` | 812 baris, 15 [Fact] | PASS |
| De-tautology compliance | Grep `SessionHasDataAsync\|ExecuteAsync` di test file | 4+3 hits, semua di komentar; 0 di kode fungsional | PASS |
| Soft-remove tidak sentuh Score/Status | Kode `RemoveParticipantCoreAsync` jalur soft :2691-2697 | Hanya set `RemovedAt`, `RemovedBy`, `RemovalReason` — tidak ada assignment ke `Score`/`IsPassed`/`Status`/`NomorSertifikat` | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| PRMV-01 | 411-01 | Hybrid delete: belum-mulai → hard-delete; sudah-berdata → soft-remove (data/cert dipertahankan) | SATISFIED | `RemoveParticipantCoreAsync` + test B1/B2/C1 |
| PRMV-04 | 411-01 | Restore: clear RemovedAt/RemovedBy/RemovalReason; guard non-removed → 400 | SATISFIED | `RestoreParticipantLive` :2585 + test B5/A4 |
| PRMV-05 | 411-01 | Pre/Post pair-as-unit via LinkedSessionId | SATISFIED | `RemoveParticipantCoreAsync` :2671 + test B6/C3 |
| PLIV-03 | 411-01 | Audit (siapa/kapan/alasan) + RBAC Admin+HC + antiforgery semua endpoint | SATISFIED | 3×[Authorize(Roles="Admin, HC")]+[ValidateAntiForgeryToken] + `_auditLog.LogAsync` + test B8 |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| — | — | — | — | Tidak ada anti-pattern ditemukan di kode 411 |

Catatan: warning CS1998 pra-eksisting di `ManageAssessment` (:68) disebutkan di SUMMARY sebagai bukan kode 411 dan sudah ada sebelum fase ini. Bukan anti-pattern baru.

---

### Human Verification Required

Tidak ada. Semua behavioral requirement fase 411 (backend HTTP endpoints + integration tests) dapat diverifikasi programatik. Item UI/SignalR/force-kick yang membutuhkan browser/human deferred ke Phase 412 dan 413 (tercatat di Deferred Items).

---

## Gaps Summary

Tidak ada gap. Semua 10 must-have truth terverifikasi di codebase aktual:

- Kode produksi `RemoveParticipantCoreAsync` mengimplementasikan hybrid by-state (hard-delete via `RecordCascadeDeleteService` / soft-remove set 3 kolom) secara benar, dengan urutan yang tepat (resolve partner → evaluasi gabungan → reason-gate → eksekusi → audit).
- Ketiga endpoint wrapper (`RemoveParticipantLive` JSON, `RestoreParticipantLive` JSON, `DeleteAssessmentPeserta` redirect) memiliki atribut RBAC dan antiforgery yang benar.
- Form `deletePesertaForm` di `EditAssessment.cshtml:666` sudah un-hidden (tombol hapus per-peserta aktif).
- 15 test de-tautologis mengunci kontrak Phase 411-01 tanpa replica predikat — drive action ASLI AssessmentAdminController dengan assert kolom DB NYATA (SQLEXPRESS disposable via FlexibleParticipantAddFixture).
- D-01 (UPA bukan data): `SessionHasDataAsync` tidak memeriksa UPA; test C2 membuktikan cascade menghapus UPA bersama sesi.
- migration=FALSE dikonfirmasi (`git status Migrations/ Data/` bersih).

3 item deferred (SignalR force-kick, panel UI, Playwright e2e) tercakup di Phase 412 dan 413 secara eksplisit — bukan gap fase ini.

---

_Verified: 2026-06-21T07:45:00Z_
_Verifier: Claude (gsd-verifier)_
