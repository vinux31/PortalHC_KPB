# Phase 246: UAT Edge Cases & Records - Research

**Researched:** 2026-03-24
**Domain:** ASP.NET Core MVC — Assessment edge cases, renewal sertifikat, records view & export
**Confidence:** HIGH

## Summary

Phase ini adalah fase UAT murni dengan satu komponen kecil coding: seed data tambahan. Semua fitur yang akan diverifikasi (token validation, force close, reset, regenerate token, renewal certificate, records view, export Excel) sudah terimplementasi sepenuhnya di codebase existing. Tidak ada fitur baru yang perlu dibangun — hanya perlu seed data yang tepat agar skenario edge case dapat dijalankan via browser.

Seed data yang dibutuhkan: (1) AssessmentSession baru dengan `IsTokenRequired = true` untuk test EDGE-01 dan EDGE-03. (2) AssessmentSession completed dengan `ValidUntil` di masa lalu (expired) untuk test EDGE-04 — saat ini sertifikat existing di SeedData memiliki `ValidUntil = certDate.AddYears(1)` (1 tahun ke depan dari 30 hari lalu), sehingga belum expired dan alarm banner tidak muncul. Records (REC-01, REC-02) sudah memiliki data dari SEED-07 (completed pass session untuk Rino).

**Primary recommendation:** Plan 1 = extend SeedData.cs dengan seed token-required session + expired certificate. Plan 2 = browser UAT 6 requirement.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Browser UAT langsung tanpa code review ulang — code review sudah dilakukan di Phase 244 (MON-01/MON-02 semua 9 poin OK)
- **D-02:** Perlu seed data tambahan: minimal 1 assessment session dengan `IsTokenRequired = true` untuk test EDGE-01 (token salah) dan EDGE-03 (regenerate token). Data existing semua `IsTokenRequired = false`.
- **D-03:** Full flow test: Home/Index alarm banner → klik link → RenewalCertificate → proses renewal → sertifikat baru terbuat
- **D-04:** Perlu seed sertifikat expired — tidak ada di seed data existing. Seed harus membuat sertifikat dengan `ValidUntil` di masa lalu agar alarm banner muncul.
- **D-05:** Verifikasi fitur existing saja — halaman My Records (worker) dan UserAssessmentHistory (HC team view) sudah ada. Cukup verifikasi kolom lengkap + export Excel berfungsi.
- **D-06:** 2 plans — Plan 1: Seed data tambahan (assessment IsTokenRequired=true + sertifikat expired). Plan 2: Browser UAT semua 6 requirements (EDGE-01–04, REC-01–02).

### Claude's Discretion

- Detail implementasi seed data (nama assessment, token value, tanggal expired)
- Urutan test scenario dalam browser UAT

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — diskusi tetap dalam scope phase.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EDGE-01 | Token salah ditolak dengan pesan error, token expired/invalid tidak bisa digunakan | `CMPController.ValidateToken`: baris 693–695 menolak token salah dengan message "Token tidak valid. Silakan periksa dan coba lagi." Butuh seed session dengan `IsTokenRequired = true` |
| EDGE-02 | HC force close mengakhiri ujian worker secara real-time, HC reset memungkinkan ujian ulang | `AdminController.AkhiriUjian` (baris 2693) + `AdminController.ResetAssessment` (baris 2585) sudah ada. Data existing sudah cukup (session OJT Q1-2026 bisa masuk InProgress) |
| EDGE-03 | HC regenerate token menghasilkan token baru dan token lama invalid | `AdminController.RegenerateToken` (baris 2155) sudah ada. Butuh seed session dengan `IsTokenRequired = true` |
| EDGE-04 | Renewal sertifikat expired berfungsi end-to-end dari alarm hingga perpanjangan | `_CertAlertBanner.cshtml` + `AdminController.RenewalCertificate` sudah ada. Butuh seed sertifikat dengan `ValidUntil` di masa lalu |
| REC-01 | Worker dapat melihat riwayat assessment di My Records dengan kolom lengkap dan export Excel | `CMPController.ExportRecords` (baris 477) sudah ada. Data SEED-07 sudah ada (Rino lulus OJT Q4-2025) |
| REC-02 | HC dapat melihat data seluruh pekerja di Team View dengan date range filter dan export | `CMPController.ExportRecordsTeamAssessment` (baris 526) sudah ada. Data SEED-07 sudah cukup |

</phase_requirements>

---

## Standard Stack

### Core (existing — tidak ada instalasi baru)

| Library/Pattern | Purpose | Status |
|-----------------|---------|--------|
| ASP.NET Core MVC | Controller + View layer | Existing |
| Entity Framework Core | ORM untuk seed data | Existing |
| `CertNumberHelper.Build()` | Generate nomor sertifikat | Existing — `Helpers/CertNumberHelper.cs` |
| SignalR `/hubs/assessment` | Real-time push saat AkhiriUjian | Existing — verified Phase 244 |
| ClosedXML / EPPlus | Excel export | Existing — digunakan di ExportRecords |
| TempData `TokenVerified_{id}` | Guard token verification → StartExam | Existing pattern (Phase 244) |

**Installation:** Tidak diperlukan — semua dependency sudah ada.

---

## Architecture Patterns

### Pattern 1: Seed Data Idempotency Guard

Seed data existing menggunakan title-based guard di `SeedUatDataAsync`:

```csharp
// Source: Data/SeedData.cs baris 251
if (await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Proses Alkylation Q1-2026"))
{
    // inner guard untuk fallback Proton
    ...
    Console.WriteLine("UAT-SEED: Data UAT sudah ada, skip.");
    return;
}
```

Seed baru harus **ditempatkan di dalam blok yang sama** setelah seed existing, ATAU menggunakan inner guard by title baru seperti pola Proton fallback. Untuk seed EDGE (token-required + expired cert), paling aman menggunakan inner guard terpisah.

### Pattern 2: Seed Session dengan IsTokenRequired = true

Berdasarkan session existing (baris 400–414), field yang wajib:

```csharp
// Source: Data/SeedData.cs baris 400–414
var session = new AssessmentSession
{
    Title        = "OJT Token Test Q1-2026",    // judul unik untuk idempotency
    UserId       = rinoId,
    Category     = "Assessment OJT",
    Schedule     = now.AddDays(3),
    DurationMinutes = 30,
    Status       = "Open",
    PassPercentage  = 70,
    AllowAnswerReview = true,
    GenerateCertificate = true,
    AccessToken  = "EDGE-TOKEN-001",            // token yang diketahui untuk testing
    IsTokenRequired = true,                     // KUNCI — beda dari session existing
    CreatedAt    = now
};
```

Token ini harus di-assign ke Iwan juga (multi-user pattern), karena RegenerateToken update sibling sessions (`token rotation update sibling sessions` — Phase 244 decision).

### Pattern 3: Seed Sertifikat Expired

Sertifikat existing di SEED-07 memiliki `ValidUntil = certDate.AddYears(1)` (certDate = 30 hari lalu → valid sampai 11 bulan ke depan). Untuk EDGE-04, perlu session baru dengan ValidUntil di masa lalu:

```csharp
// Source: Data/SeedData.cs baris 531
session.ValidUntil = certDate.AddYears(1);  // POLA EXISTING — expired

// Pattern untuk EDGE-04: ValidUntil masa lalu
var expiredCertDate = now.AddDays(-400);    // ~13 bulan lalu → expired
session.ValidUntil = expiredCertDate.AddYears(1);  // = 60 hari lalu → expired
```

Alarm banner muncul di `_CertAlertBanner.cshtml` jika `Model.ExpiredCount > 0`. Query di HomeController/BuildRenewalRowsAsync harus membandingkan ValidUntil < DateTime.UtcNow untuk menghitung ExpiredCount.

### Pattern 4: AkhiriUjian (Force Close) Flow

```
HC klik "Akhiri Ujian" di AssessmentMonitoringDetail
  → POST /Admin/AkhiriUjian/{id}
  → GradeFromSavedAnswers(session)  // auto-grade dari jawaban tersimpan
  → ExecuteUpdateAsync: Status="Completed", CompletedAt=now, Score=..., Progress=100
  → SignalR push (via cache invalidation)
  → Worker yang sedang di /CMP/StartExam/{id} → redirect ke Results
```

Source: `AdminController.cs` baris 2693–2795. `_cache.Remove($"exam-status-{id}")` memicu SignalR push.

### Pattern 5: ResetAssessment Flow

```
HC klik "Reset" di AssessmentMonitoringDetail
  → POST /Admin/ResetAssessment/{id}
  → Jika Status="Completed": archive ke AssessmentAttemptHistory
  → Delete PackageUserResponses + UserPackageAssignment
  → ExecuteUpdateAsync: Status="Open", semua progress fields = null/0
```

Source: `AdminController.cs` baris 2585–2680. Setelah reset, worker dapat mulai ujian dari awal.

### Pattern 6: RegenerateToken Flow

```
HC klik "Regenerate Token" di AssessmentMonitoringDetail
  → POST /Admin/RegenerateToken/{id}
  → Cek: assessment.IsTokenRequired == true (jika false → error)
  → Generate newToken = Guid.NewGuid().ToString("N")[..8].ToUpper()
  → Update session + semua sibling sessions (same Title+Category+Schedule) dengan token baru
  → Return JSON: { success: true, token: newToken }
```

Source: `AdminController.cs` baris 2155–2195. Token lama otomatis tidak valid karena `ValidateToken` membandingkan `assessment.AccessToken != token.ToUpper()`.

### Pattern 7: Renewal Certificate E2E Flow

```
Home/Index → _CertAlertBanner → klik "Lihat Detail"
  → GET /Admin/RenewalCertificate
  → Tampil daftar sertifikat expired/akan expired
  → HC pilih session → klik "Perpanjang"
  → GET /Admin/CreateAssessment?renewSessionId={id}
  → isRenewalMode = true, pre-fill: Title, Category, ValidUntil+1yr, SelectedUserIds
  → HC submit → POST /Admin/CreateAssessment
  → Sertifikat baru terbuat dengan NomorSertifikat baru
```

Source: `AdminController.cs` baris 1025–1059, `_CertAlertBanner.cshtml`, `RenewalCertificate.cshtml`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Token generation | Custom random string | `Guid.NewGuid().ToString("N")[..8].ToUpper()` — already used in RegenerateToken |
| Certificate number | Custom counter | `CertNumberHelper.GetNextSeqAsync` + `CertNumberHelper.Build` |
| Excel export | Manual CSV | ClosedXML/EPPlus via existing `ExportRecords` pattern |
| Idempotency guard | Complex logic | Title-based `AnyAsync` check — established pattern |

---

## Common Pitfalls

### Pitfall 1: Seed Masuk ke Guard "Skip"

**What goes wrong:** Seed baru ditambahkan setelah `return` di idempotency guard, sehingga tidak pernah dieksekusi pada database existing.

**Why it happens:** Guard `if (AnyAsync(s.Title == "OJT Proses Alkylation Q1-2026")) return;` akan skip semua kode di bawahnya.

**How to avoid:** Untuk database existing yang sudah punya data Phase 241, perlu inner guard terpisah by title baru. Pattern: setelah `return` dari guard utama, TIDAK. Gunakan pattern fallback yang sama dengan Proton:

```csharp
// Di dalam blok guard yang sudah ada (setelah cek Proton):
if (!await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Token Test Q1-2026"))
{
    await SeedTokenRequiredAssessmentAsync(context, rinoId, iwanId, now);
}
if (!await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Expired Cert Q3-2024"))
{
    await SeedExpiredCertAssessmentAsync(context, rinoId, now);
}
```

**Warning signs:** Console.log tidak muncul untuk seed baru.

### Pitfall 2: IsTokenRequired = false Tidak Bisa Test RegenerateToken

**What goes wrong:** RegenerateToken endpoint menolak session dengan `IsTokenRequired = false`: "This assessment does not require a token."

**Why it happens:** Guard di baris 2163–2165 AdminController.

**How to avoid:** Seed session baru dengan `IsTokenRequired = true` — ini keputusan D-02.

### Pitfall 3: Sertifikat Belum Expired di Seed Existing

**What goes wrong:** SEED-07 membuat sertifikat dengan `ValidUntil = certDate.AddYears(1)` di mana certDate = 30 hari lalu → ValidUntil = 335 hari ke depan → TIDAK expired → alarm banner tidak muncul.

**Why it happens:** SEED-07 dirancang untuk data analytics/records, bukan untuk test renewal.

**How to avoid:** Seed session expired baru dengan `ValidUntil` jelas di masa lalu (misal: `now.AddDays(-10)` setelah menghitung bahwa `expiredCertDate.AddYears(1) < now`).

### Pitfall 4: Multi-User Session untuk Token Test

**What goes wrong:** RegenerateToken update semua sibling sessions (same Title+Category+Schedule). Jika hanya Rino yang di-assign, hanya 1 session yang diupdate — test valid. Namun untuk memverifikasi sibling update, perlu minimal 2 users.

**How to avoid:** Seed token-required session untuk Rino DAN Iwan (multi-user, seperti session OJT Q1-2026 existing yang sudah include keduanya).

### Pitfall 5: Renewal Mode Butuh `renewSessionId` Parameter

**What goes wrong:** Navigasi langsung ke `/Admin/CreateAssessment` tanpa parameter tidak trigger renewal mode.

**Why it happens:** Renewal mode diaktifkan hanya jika `renewSessionId != null && renewSessionId.Count > 0` (baris 1031).

**How to avoid:** UAT harus mengklik tombol "Perpanjang" dari halaman RenewalCertificate — tidak bisa navigasi manual ke URL. Ini sudah bagian dari flow D-03.

---

## Code Examples

### Seed Inner Guard Pattern (untuk database existing)

```csharp
// Source: Data/SeedData.cs baris 251–264 (pola fallback Proton)
// Di dalam blok guard utama, SEBELUM return:
if (!await context.AssessmentSessions.AnyAsync(s => s.Category == "Assessment Proton"))
{
    // seed Proton
}
// TAMBAHKAN pola serupa:
if (!await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Token Test Q1-2026"))
{
    await SeedTokenRequiredSessionAsync(context, rinoId, iwanId, now);
}
if (!await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Expired Cert Q3-2024"))
{
    await SeedExpiredCertSessionAsync(context, rinoId, now);
}
Console.WriteLine("UAT-SEED: Data UAT sudah ada, skip.");
return;
```

### Sertifikat Expired yang Benar

```csharp
// Pattern untuk ValidUntil di masa lalu:
var expiredDate = now.AddDays(-400);   // assessment selesai 400 hari lalu
session.ValidUntil = expiredDate.AddYears(1); // = ~35 hari lalu → expired
```

Nilai `ValidUntil` harus < `DateTime.UtcNow` agar alarm banner muncul.

---

## Environment Availability

Step 2.6: SKIPPED — fase ini adalah koding seed data + browser UAT tanpa external dependencies baru. Semua dependency (ASP.NET Core, EF Core, SignalR) sudah available dan verified di phases sebelumnya.

---

## Validation Architecture

Fase ini adalah UAT manual — tidak ada automated test baru. Verifikasi dilakukan via browser oleh human tester. Sesuai pattern semua UAT phases sebelumnya (242–245), Plan 2 adalah "Browser UAT" yang outputnya pass/fail per requirement ID.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated? |
|--------|----------|-----------|-----------|
| EDGE-01 | Token salah → pesan error | Manual browser | Tidak — requires live session + user interaction |
| EDGE-02 | Force close → worker di-redirect; Reset → ujian ulang | Manual browser | Tidak — requires SignalR + real-time |
| EDGE-03 | Regenerate → token baru, lama invalid | Manual browser | Tidak |
| EDGE-04 | Alarm → Renewal → sertifikat baru | Manual browser | Tidak |
| REC-01 | Worker My Records, kolom lengkap, export Excel | Manual browser | Tidak |
| REC-02 | HC Team View, filter date range, export Excel | Manual browser | Tidak |

**Quick run command:** N/A (UAT manual)
**Wave 0 Gaps:** Tidak ada — UAT manual tidak butuh test file baru.

---

## Sources

### Primary (HIGH confidence)

- `Controllers/AdminController.cs` — AkhiriUjian (baris 2693), ResetAssessment (baris 2585), RegenerateToken (baris 2155), RenewalCertificate (baris 7376), CreateAssessment renewal mode (baris 1025–1059)
- `Controllers/CMPController.cs` — ValidateToken (baris 681–700), StartExam token guard (baris 741–746), ExportRecords (baris 477), ExportRecordsTeamAssessment (baris 526)
- `Data/SeedData.cs` — SeedUatDataAsync (baris 248), idempotency guard (baris 251), SeedCompletedAssessmentPassAsync (baris 500), session model fields (baris 400–414)
- `Views/Home/_CertAlertBanner.cshtml` — alarm banner display logic
- `Models/RecordsWorkerListViewModel.cs` — worker records view model fields

### Secondary (MEDIUM confidence)

- `.planning/phases/244-uat-monitoring-analytics/244-VERIFICATION.md` — Phase 244 code review OK, 9 poin SignalR + token rotation verified

---

## Metadata

**Confidence breakdown:**
- Seed data patterns: HIGH — kode dibaca langsung dari SeedData.cs
- Edge case implementation: HIGH — controller actions ditemukan dan dibaca
- Renewal flow: HIGH — action dan view ditemukan
- Records/export: HIGH — controller actions ditemukan

**Research date:** 2026-03-24
**Valid until:** 2026-04-24 (stable codebase, tidak ada dependency eksternal berubah)
