---
phase: 428
slug: startexam-write-on-get-idempotency
requirement: EXSEC-02
created: 2026-06-25
author: inline (subagent infra unstable — orchestrator authored from full codebase scout)
---

# Phase 428 Research — StartExam Write-on-GET Idempotency (EXSEC-02)

> Tujuan: GET `CMP/StartExam(id)` tidak lagi mem-persist transisi status `Upcoming→Open` ke DB. Transisi dihitung in-memory (effective-status by-schedule). Gate lain tetap jalan. migration=FALSE.

## 1. Lokus & Edit Minimal (R-1: surgical)

**File:** `Controllers/CMPController.cs`, method `StartExam(int id)` GET (mulai :909).

### Blok yang DIGANTI (saat ini :922-932)
```csharp
// Auto-transition: Upcoming → Open when scheduled date+time has arrived in WIB (persisted to DB)
if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7))
{
    if (!_impersonationService.IsImpersonating())   // write-on-GET guard
    {
        assessment.Status = "Open";
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();           // <-- PERSIST yang dilanggar (EXSEC-02)
    }
}

// Time gate: block access if assessment is still Upcoming (scheduled time not yet reached)   (:934-939)
if (assessment.Status == "Upcoming")
{
    TempData["Error"] = "Ujian belum dibuka. Silakan kembali setelah waktu ujian dimulai.";
    return RedirectToAction("Assessment");
}
```

### Bentuk SESUDAH (D-02 in-memory effective-status, NO persist)
```csharp
// EXSEC-02: effective-status by-schedule — GET idempoten, TIDAK persist transisi Upcoming→Open.
// Mirror pola lobby Assessment (:245-251, display-only). Transisi status aktual hanya saat
// worker mulai (justStarted → InProgress write di bawah).
var nowWib = DateTime.UtcNow.AddHours(7);

// Time gate: blok HANYA bila benar-benar belum waktunya (Upcoming & jadwal di masa depan).
// Upcoming yang waktunya SUDAH tiba → diperlakukan openable (tak diblok), tanpa menulis DB.
if (assessment.Status == "Upcoming" && assessment.Schedule > nowWib)
{
    TempData["Error"] = "Ujian belum dibuka. Silakan kembali setelah waktu ujian dimulai.";
    return RedirectToAction("Assessment");
}
```

**Catatan:** tak perlu variabel `isEffectivelyOpen` eksplisit — satu-satunya konsumen status pra-start adalah time-gate. Cukup ubah kondisi time-gate jadi `Status=="Upcoming" && Schedule > nowWib`. Lebih sedikit baris = lebih aman terhadap konflik merge (R-1). **Rekomendasi: INLINE, jangan ekstrak helper** (over-engineering untuk 1 pemakaian; lobby 245-251 punya kebutuhan beda — ia me-loop list & mengubah Status objek untuk display, bukan gating). Bila kelak ada konsumen ke-3, baru pertimbangkan `RetakeRules`-style pure helper.

### Verifikasi tak ada pembaca `Status` lain yang rusak
Antara blok yang dihapus (:932) dan justStarted write (:1021), pembaca `assessment.Status`:
- `:941` `Status == "Completed"` — tak terpengaruh (Upcoming ≠ Completed).
- `:989` `Status == "Abandoned"` — tak terpengaruh.
- justStarted (`:996`) keys off **`StartedAt == null`**, BUKAN Status → AMAN. ✔
- GRDF-01 (`:950-959`), token-gate EXSEC-01 (`:964-972`), exam-window (`:975`), duration (`:982`) — semua baca field lain (pairedPre.Status, TokenVerifiedAt, ExamWindowCloseDate, DurationMinutes), bukan `assessment.Status`. ✔
- Setelah justStarted: worker asli → `Status="InProgress"` (`:1023`). Impersonate → Status tetap `Upcoming` in-memory; view exam-skeleton render (read-only). Status hanya kosmetik di view skeleton; tak ada logika downstream yang gagal bila Upcoming. ✔

## 2. Konsekuensi perilaku (didokumentasikan, dapat diterima)
- **Worker asli, waktu tiba:** Upcoming → (langsung) InProgress di justStarted write. Tak ada lagi state `Open` perantara yang di-persist. End-state identik dgn sebelumnya (InProgress). SC#4 ✔
- **Impersonate admin/HC, waktu tiba, sesi Upcoming:** SEBELUMNYA diblok time-gate (transisi Open ada di dalam guard `!IsImpersonating`, jadi Status tetap Upcoming → redirect "belum dibuka"). SESUDAH: lolos sebagai effective-open → view render, TANPA tulis DB. Ini perbaikan kecil & konsisten dgn "Admin/HC may access StartExam for debugging/monitoring". Tetap read-only (justStarted write di-skip saat impersonate).
- **Badge admin monitoring** (AssessmentAdminController grouping baca Status persisted: Open||InProgress→Open): TIDAK regresi — worker asli tetap menulis InProgress (flip badge ke Open). Persisted-Open standalone tak pernah "diam" sebelumnya juga (langsung ditimpa InProgress di request sama). ✔

## 3. Strategi Test (real-SQL) — TITIK TERSULIT terpecahkan

**Masalah:** GET yang sama yang memperlakukan sesi effectively-open JUGA menjalankan justStarted InProgress write (utk worker asli). Jadi GET owner non-impersonate pada Upcoming-waktu-tiba → Status jadi InProgress (bukan Upcoming). Tak bisa langsung mengamati "tak ada persist Upcoming→Open" lewat jalur owner.

**Solusi — jalur impersonation = satu-satunya GET non-starting:** justStarted write di-guard `!IsImpersonating()`. Maka GET saat impersonate TIDAK menulis apa pun. Effective-status (perubahan kita) tetap melewatkan gate. Observasi: **Status DB tetap `Upcoming` setelah GET** = bukti tak ada write-on-GET untuk transisi status (SC#1/#2).

Simulasi impersonate di test factory (reuse `RetakeExamEndpointTests`/`TokenVerifiedAtTests` factory + `StubSession`):
```csharp
session.SetString(ImpersonationKeys.Mode, "user");           // "Impersonate_Mode" → IsImpersonating()=true
session.SetString(ImpersonationKeys.TargetUserId, owner.Id); // effective user = owner (TargetUser path)
session.SetString(ImpersonationKeys.TargetUserName, owner.FullName ?? "owner");
session.SetString(ImpersonationKeys.StartedAt, DateTime.UtcNow.Ticks.ToString());
```
`GetCurrentUserRoleLevelAsync` mode="user" → resolve effective user via `FindByIdAsync(TargetUserId)` (FakeUserStore) → owner. Owner-check (`UserId==user.Id`) lolos. (Tambahkan owner ke FakeUserStore.)

**Komplemen statik (untuk checker/verifier):** grep method StartExam → TIDAK ada `Status = "Open"` diikuti `SaveChangesAsync` di jalur GET. Pembuktian SC#1 di level kode.

## 4. ## Validation Architecture

Framework: xUnit 2.9.3 (.NET 8), real-SQL `RetakeServiceFixture` (disposable DB @ `localhost\SQLEXPRESS`, MigrateAsync full chain). Trait `[Category,"Integration"]`. Reuse CMPController factory dari `RetakeExamEndpointTests` (FakeUserStore/MakeUserManager/StubSession/ImpersonationService) — RetakeService boleh real (tak dipakai StartExam) atau null!-substitute bila StartExam tak deref. **WAJIB sertakan `_impersonationService` real** (StartExam deref di guard).

Test file baru: `HcPortal.Tests/StartExamIdempotencyTests.cs`.

| SC | Behavior | Test (real-SQL) | Assert |
|----|----------|-----------------|--------|
| SC#1, SC#2 | GET tak persist Upcoming→Open | `StartExam_Impersonate_TimeArrivedUpcoming_RendersWithoutPersisting` — impersonate owner, sesi Upcoming + Schedule di masa lalu (waktu tiba) + IsTokenRequired=false + Duration>0 | result = `ViewResult` (bukan Redirect); reload DB → `Status == "Upcoming"` (TIDAK berubah) ; `StartedAt == null` |
| SC#1 (idempoten) | GET berulang stabil | `StartExam_Impersonate_DoubleGet_StatusStaysUpcoming` — panggil GET 2× berturut | kedua kali ViewResult; DB Status tetap `Upcoming` setelah panggilan ke-2 |
| SC#3 (time-gate) | Upcoming belum waktunya tetap diblok | `StartExam_Upcoming_NotYetTime_BlocksAndNoWrite` — owner, Schedule masa depan | `RedirectToActionResult` → "Assessment"; DB Status tetap `Upcoming` |
| SC#3 (GRDF-01) | Post butuh Pre Completed | `StartExam_PostTest_PreNotCompleted_Blocks` — owner, Post linked ke Pre status≠Completed, waktu tiba | Redirect → "Assessment" (pesan "Selesaikan Pre-Test dulu"); Status tetap Upcoming |
| SC#4 | Worker mulai end-to-end | `StartExam_Owner_TimeArrived_StartsInProgress` — owner non-impersonate, Upcoming waktu tiba, token ok, paket+soal ada | ViewResult (exam skeleton); reload DB → `Status == "InProgress"` ; `StartedAt != null` ; assignment ter-create |
| Regresi 427 | token-gate tetap | `StartExam_TokenRequired_NotVerified_Blocks` — owner, IsTokenRequired=true, TokenVerifiedAt=null, StartedAt=null, waktu tiba | Redirect → "Assessment" ("token akses"); Status tetap Upcoming |

Sampling: `dotnet build` + `dotnet test --filter "FullyQualifiedName~StartExamIdempotencyTests"` (real-SQL). Full suite sebelum verify. Latency < 60s.

Manual-only: tidak ada (UI hint=no, tak ada view berubah; semua ter-cover integration).

## 5. Regression Risk
- **token-gate EXSEC-01 (427):** baca `IsTokenRequired/UserId/StartedAt/TokenVerifiedAt` — tak tersentuh. Urutan gate dipertahankan. ✔
- **GRDF-01 (424):** baca `pairedPre.Status` via `PrePostPairing.FindPairedPreAsync` — tak tersentuh. ✔
- **Merge R-1:** edit terkurung di 2 blok berdekatan (922-939). Pertahankan urutan: Completed → GRDF-01 → token → window → duration → abandoned → justStarted. Saat merge vs main, jaga KEDUA (GRDF-01 setelah cek-Completed sebelum token-gate; effective-status menggantikan blok persist+time-gate).

## 6. Plan shape (saran utk planner)
Satu plan, satu wave: **428-01** (EXSEC-02) — (T1) refactor StartExam blok 922-939 ke effective-status in-memory; (T2) tulis `StartExamIdempotencyTests.cs` 6 test real-SQL per tabel §4. autonomous:true. files_modified: `Controllers/CMPController.cs`, `HcPortal.Tests/StartExamIdempotencyTests.cs`. migration=FALSE. <threat_model> ringkas (lihat §5; STRIDE: Tampering=GET idempoten mengurangi side-effect; tak ada surface baru; accept).
