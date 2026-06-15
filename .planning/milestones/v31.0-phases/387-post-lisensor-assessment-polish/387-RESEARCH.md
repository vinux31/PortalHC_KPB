# Phase 387: Post-Lisensor Assessment Polish - Research

**Researched:** 2026-06-15
**Domain:** ASP.NET Core MVC + EF Core + SignalR (Portal HC KPB assessment lifecycle)
**Confidence:** HIGH (all claims VERIFIED against live HEAD code this session)

## Summary

Phase 387 = 7 bug-fix/polish temuan (1 MED + 6 LOW) dengan lokasi file:line + pola acuan yang sudah diketahui. Riset ini = **konfirmasi shape kode persis** (enum status, signature method, blok yang ditiru), bukan eksplorasi domain. Semua 7 REQ telah diverifikasi langsung terhadap kode HEAD saat ini.

**Primary recommendation:** Setiap fix punya pola acuan yang sudah ada di codebase yang sama — tiru verbatim. Tidak ada library baru, **0 migration**, 0 entity/schema change. Catatan kritis: tidak ada status `"Failed"` di sistem — satu-satunya status terminal pasca-finalize adalah `Completed`; `PendingGrading` (nilai string = **"Menunggu Penilaian"**, BUKAN "PendingGrading") adalah window grading normal.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (PXF-06):** BLOCK + pesan jelas. Bila HC panggil `SubmitEssayScore` saat sesi **terminal/finalized** (post-`FinalizeEssayGrading`, yaitu `Status == Completed`), tolak dengan pesan Bahasa Indonesia jelas (mis. "Sesi sudah final, skor essay tidak dapat diubah"). TIDAK recompute, TIDAK re-issue cert.
- **D-01a (KRITIS):** Guard HANYA aktif saat status terminal pasca-finalize. Saat window grading normal (`PendingGrading`, sebelum finalize) `SubmitEssayScore` HARUS tetap jalan. Salah taruh guard = membekukan grading normal HC.
- **D-02 (PXF-12 + PXF-13):** Strict — mirror `SaveMultipleAnswer`.
  - PXF-13: `Hub.SaveTextAnswer` tambah guard timer-expired persis `SaveMultipleAnswer:205-215` (tolak tulis essay bila timer lewat, log warning).
  - PXF-12: `SubmitExam` upsert MC HANYA update key yang ADA di form `answers`; jangan timpa jawaban tersimpan jadi null untuk soal absent.
- **D-03 (PXF-08):** Retry 3× + log + surface ke HC. Match pola `GradingService` (retry-loop 3× + unique-index). Bila tetap gagal → log error + kembalikan pesan ke HC.
- **D-06 (PXF-09):** Excel BulkExport "Detail Jawaban" tampilkan skor/teks essay yang sudah dinilai (bukan selalu "—"). Sheet "Detail Jawaban" (`~4828`) BEDA dari "Detail Per Soal" (scope 386).
- **D-07 (PXF-10):** `FinalizeEssayGrading` broadcast ke monitor group — ikuti pola SignalR broadcast yang sudah ada (Claude pilih event/group konsisten).
- **D-08 (PXF-11):** Gambar opsi Results/ExamSummary sertakan label huruf A/B/C/D pada `AriaContext` — derive huruf dari index opsi.
- **D-09:** Verifikasi proporsional pasca-acara. Unit test untuk yang ada logika (PXF-06, PXF-09, PXF-12). `dotnet build` 0 error + `dotnet run` localhost:5277. Playwright HANYA PXF-11. LOW lain (PXF-08/10/13) cukup unit/manual + build.

### Claude's Discretion
- Wording pesan Bahasa Indonesia (PXF-06, PXF-08).
- Pilihan event/group SignalR spesifik (PXF-10) — selama konsisten dengan pola existing.
- Detail implementasi derivasi huruf (PXF-11).

### Deferred Ideas (OUT OF SCOPE)
- **F-01** (UX MA-warn "jawaban sebagian = 0") — mitigasi briefing peserta.
- **F-18** (export by-paket bukan ShuffledQuestionIds) — kondisional, relevan hanya bila >1 paket.
- **PXF-07 (F-02) + PXF-14 (F-DEV-02)** — DIPINDAH ke Phase 386. `Helpers/ExcelExportHelper.cs` JANGAN disentuh dari 387.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PXF-06 | `SubmitEssayScore` guard status pasca-finalize (block edit) | §PXF-06 — exact loc `:3525`, guard pakai `Status == Completed`; terminal hanya `Completed` |
| PXF-08 | Cert nomor essay finalize retry 3× + log + surface | §PXF-08 — bug `:3697-3711`, pola tiru `GradingService.cs:287-318` |
| PXF-09 | Excel "Detail Jawaban" tampilkan skor/teks essay (bukan "—") | §PXF-09 — exact loc `:4828-4838`, data tersedia di `sessionResp` (EssayScore/TextAnswer) |
| PXF-10 | `FinalizeEssayGrading` broadcast monitor group | §PXF-10 — pola tiru `:3204-3211` (monitor-{batchKey}) |
| PXF-11 | Gambar opsi Results/ExamSummary aria huruf A/B/C/D | §PXF-11 — pola tiru `StartExam.cshtml:125/134/148`; 2 surface |
| PXF-12 | `SubmitExam` MC no null-overwrite jawaban absent | §PXF-12 — exact bug `CMPController.cs:1714` |
| PXF-13 | `Hub.SaveTextAnswer` guard timer-expired | §PXF-13 — pola tiru `AssessmentHub.cs:205-215` verbatim |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Develop Workflow:** verifikasi lokal WAJIB sebelum commit — `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal (+ Playwright bila ada). Jangan push tanpa verifikasi lokal.
- **Jangan** edit kode/DB langsung di Dev (10.55.3.3) / Prod. Promosi ke Dev = tanggung jawab Team IT (notify dengan commit hash + flag migration).
- **0 migration** untuk fase ini → notify IT: migration=FALSE.
- **AD lokal:** `dotnet run` set `Authentication__UseActiveDirectory=false` (lesson Phase 355).
- **Admin login lokal:** `admin@pertamina.com` (reference_dev_credentials).
- **Playwright combined:** WAJIB `--workers=1`; start SQLBrowser + `lpc:` shared-memory override (reference_local_e2e_sql_env_fix).
- **Seed test:** klasifikasi `temporary + local-only`, snapshot DB, catat `docs/SEED_JOURNAL.md`, restore setelah test.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Guard edit skor essay terminal (PXF-06) | API/Controller | DB (read Status) | Otoritas server-side; jangan client-trust |
| Cert nomor retry (PXF-08) | API/Controller | DB (unique index) | Concurrency/collision handling = server |
| Excel export cell (PXF-09) | API/Controller | — | Server-side render export |
| Broadcast monitor (PXF-10) | API/Controller → SignalR Hub | Browser (monitor tab) | Real-time push via IHubContext |
| Aria label opsi (PXF-11) | Frontend Server (Razor) | Browser (a11y) | Render-time markup |
| MC upsert no-null (PXF-12) | API/Controller | DB | Data-integrity di write path |
| Timer guard essay (PXF-13) | SignalR Hub | DB (read session) | Server-authoritative timer |

## Standard Stack

Tidak ada dependency baru. Semua perubahan pakai API yang sudah dipakai di codebase:

| Komponen | Dipakai Untuk | Status |
|----------|---------------|--------|
| EF Core `ExecuteUpdateAsync` / `SaveChangesAsync` | upsert + cert update | [VERIFIED: kode HEAD] |
| `IHubContext<AssessmentHub>` (field `_hubContext`, `:27`) | broadcast monitor | [VERIFIED: AssessmentAdminController.cs:27] |
| `CertNumberHelper.GetNextSeqAsync/Build/IsDuplicateKeyException` | cert nomor + retry | [VERIFIED: GradingService.cs:300-308] |
| `AssessmentConstants.AssessmentStatus.*` | status enum | [VERIFIED: Models/AssessmentConstants.cs:15-20] |
| `Html.PartialAsync("_QuestionImage", ...)` reflection-based | aria opsi | [VERIFIED: _QuestionImage.cshtml] |
| ClosedXML `ws.Cell(...).Value` | Excel cell | [VERIFIED: AssessmentAdminController.cs:4807+] |

**Installation:** Tidak ada. `dotnet build` cukup.

## Status Enum — Fakta Kritis (PXF-06)

`Models/AssessmentConstants.cs:15-20` [VERIFIED]:

```csharp
public const string Open = "Open";
public const string Upcoming = "Upcoming";
public const string Completed = "Completed";
public const string PendingGrading = "Menunggu Penilaian"; // ← nilai STRING != nama konstanta
public const string InProgress = "InProgress";
public const string Cancelled = "Cancelled";
```

**Tidak ada `"Failed"`.** CONTEXT D-01 menyebut "Completed / Failed" — di kode **hanya `Completed`** yang ada. Lulus/gagal direpresentasikan oleh `IsPassed` (bool) DALAM status `Completed`, bukan status terpisah. Jadi **terminal/finalized = `Status == AssessmentConstants.AssessmentStatus.Completed`**.

Bukti tambahan dari `FinalizeEssayGrading` (`:3573`): early-return `alreadyFinalized` saat `Status == Completed`. Dan `:3661` ExecuteUpdate guard `WHERE Status == PendingGrading` → set `Status = Completed`. Jadi lifecycle essay: `PendingGrading` (window grading, SubmitEssayScore boleh) → `Completed` (terminal, SubmitEssayScore harus block).

**[ASSUMED→VERIFIED]** Guard PXF-06 yang benar: block bila `session.Status == Completed`; izinkan selainnya (`PendingGrading` = grading normal). GUNAKAN konstanta, BUKAN literal "Completed".

---

## PXF-06 (F-03, MED) — Guard `SubmitEssayScore` pasca-finalize

**File:line:** `Controllers/AssessmentAdminController.cs:3525` (method), gap guard di antara step 2/3 dan step 4 save (`:3542`).

**Current code [VERIFIED] `:3525-3544`:**
```csharp
public async Task<IActionResult> SubmitEssayScore(int sessionId, int questionId, int score)
{
    // 1. Load response
    var response = await _context.PackageUserResponses
        .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
    if (response == null)
        return Json(new { success = false, message = "Jawaban tidak ditemukan" });
    // 2. Load question untuk validasi ScoreValue
    var question = await _context.PackageQuestions.FindAsync(questionId);
    if (question == null)
        return Json(new { success = false, message = "Soal tidak ditemukan" });
    // 3. Validasi skor range
    if (score < 0 || score > question.ScoreValue)
        return Json(new { success = false, message = $"Skor harus antara 0 dan {question.ScoreValue}" });
    // 4. Save EssayScore  ← BUG: tak ada cek status sesi
    response.EssayScore = score;
    await _context.SaveChangesAsync();
    ...
```

**Fix shape (D-01/D-01a):** Tambah load `AssessmentSession` + guard SEBELUM step 4 save:
```csharp
var session = await _context.AssessmentSessions.FindAsync(sessionId);
if (session == null)
    return Json(new { success = false, message = "Sesi tidak ditemukan" });
if (session.Status == AssessmentConstants.AssessmentStatus.Completed)
    return Json(new { success = false, message = "Sesi sudah final, skor essay tidak dapat diubah." });
```

**Reference pattern (analog guard sudah ada):** `FinalizeEssayGrading:3573` & `:3591` — pola membandingkan `session.Status` dengan `AssessmentConstants.AssessmentStatus.*` dan return Json `{ success = false, message = ... }`.

**Gotcha:**
- JANGAN guard pada `PendingGrading` atau `InProgress` — itu jalur grading sah (D-01a). Hanya `Completed`.
- Konstanta `AssessmentConstants` sudah di-import (dipakai di method tetangga `:3573`). Namespace `HcPortal.Models`.
- Jangan recompute/re-issue cert (D-01: block murni).

---

## PXF-08 (F-06, LOW) — Cert nomor essay finalize: retry 3× + log + surface

**File:line (bug):** `Controllers/AssessmentAdminController.cs:3697-3711` (di dalam `FinalizeEssayGrading`).

**Current code [VERIFIED] `:3697-3711`:**
```csharp
// 5. Generate sertifikat jika applicable (same pattern as GradingService)  ← komentar BOHONG (single-attempt)
if (session.GenerateCertificate && isPassed)
{
    var certNow = DateTime.Now;
    int certYear = certNow.Year;
    try
    {
        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
        await _context.AssessmentSessions
            .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
    }
    catch (DbUpdateException) { /* race-condition: cert number sudah diambil thread lain — skip */ }
}
```
Masalah: single-attempt, `catch (DbUpdateException)` **telan diam-diam** → lulus tanpa nomor cert, tak ada log, HC tak tahu.

**Reference pattern (TIRU VERBATIM) — `Services/GradingService.cs:287-318` [VERIFIED]:**
```csharp
if (session.GenerateCertificate && isPassed)
{
    var certNow = DateTime.Now;
    int certYear = certNow.Year;
    int certAttempts = 0;
    const int maxCertAttempts = 3;
    bool certSaved = false;
    while (!certSaved && certAttempts < maxCertAttempts)
    {
        certAttempts++;
        try
        {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
            certSaved = true;
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
        {
            // Retry dengan sequence baru
        }
    }
    if (!certSaved)
    {
        _logger.LogError("Failed to generate certificate number for SessionId={SessionId} after {MaxAttempts} attempts due to repeated collisions.",
            session.Id, maxCertAttempts);
    }
}
```
Catatan: di GradingService variabel = `session.Id`; di FinalizeEssayGrading method gunakan `sessionId` (parameter). Cek var lokal mana yang dipakai di blok target.

**Surface error ke HC (D-03):** Return JSON `FinalizeEssayGrading` (`:3753-3759`) saat ini:
```csharp
return Json(new { success = true, score = finalPercentage, isPassed, nomorSertifikat = updatedSession?.NomorSertifikat });
```
Bila cert gagal (`!certSaved`), `updatedSession.NomorSertifikat` akan `null` walau `isPassed`. Untuk "surface", planner bisa: tambahkan flag `certWarning` ke payload JSON (mis. `certError = "Nomor sertifikat gagal dibuat, coba lagi."` ketika `isPassed && GenerateCertificate && string.IsNullOrEmpty(NomorSertifikat)`), agar UI HC (JS handler FinalizeEssayGrading) bisa tampilkan. Cek JS pemanggil di `Views/Admin/EssayGrading*.cshtml` untuk wiring pesan (LOW — boleh manual verify).

**Gotcha:** `_logger` & `CertNumberHelper` sudah ter-pakai di controller (lihat `:3637`, `:3704`). Tidak perlu import baru. `IsDuplicateKeyException` ada di `CertNumberHelper` [VERIFIED: GradingService.cs:308].

---

## PXF-09 (F-19, LOW) — Excel "Detail Jawaban" essay selalu "—"

**File:line:** `Controllers/AssessmentAdminController.cs:4828-4838` (cabang `if (tipe == "Essay")` di loop "Detail Jawaban" BulkExport).

**Confirm BEDA surface (D-06):** Ini sheet **"Detail Jawaban"** (`:4807` header, REQ EXP-03, MC/MA per soal per-session di BulkExport). BUKAN "Detail Per Soal" (`Helpers/ExcelExportHelper.cs:83` `AddDetailPerSoalSheet`, scope Phase 386). Verified terpisah — file & method beda. [VERIFIED]

**Current code [VERIFIED] `:4828-4838`:**
```csharp
if (tipe == "Essay")
{
    ws.Cell(currentRow, 1).Value = no++;
    ws.Cell(currentRow, 2).Value = q.QuestionText;
    ws.Cell(currentRow, 3).Value = "Essay";
    ws.Cell(currentRow, 4).Value = "Essay – manual grading (lihat Penilaian Essay)";
    ws.Cell(currentRow, 5).Value = "—";
    ws.Cell(currentRow, 6).Value = "—";
    currentRow++;
    continue;
}
```
Kolom: 1=No, 2=Soal, 3=Tipe, 4=Jawaban Peserta, 5=Jawaban Benar, 6=Status (`:4813-4818`).

**Data tersedia [VERIFIED]:** `sessionResp` (`:4805`) = `allResponses.Where(r => r.AssessmentSessionId == session.Id)`. Tiap `PackageUserResponse` punya `TextAnswer` (jawaban essay) dan `EssayScore` (int? skor manual HC) — keduanya dikonfirmasi ada di model (dipakai di EssayGradingPage `:3492-3493`: `TextAnswer = resp.TextAnswer`, `EssayScore = resp2.EssayScore`).

**Fix shape (D-06):** ganti hard-coded "—" jadi nilai sesungguhnya:
```csharp
var essayResp = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
ws.Cell(currentRow, 4).Value = string.IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp.TextAnswer;
ws.Cell(currentRow, 5).Value = "—"; // essay tak punya "jawaban benar" deterministik
ws.Cell(currentRow, 6).Value = essayResp?.EssayScore.HasValue == true
    ? $"Skor: {essayResp.EssayScore}/{q.ScoreValue}"
    : "Belum dinilai";
```
(Wording final = discretion; intinya tampilkan TextAnswer + EssayScore, bukan "—".)

**Gotcha:**
- Essay tidak punya `PackageOptionId` → `responses` di `:4840` (filter `r.PackageOptionId.HasValue`) tidak relevan untuk essay; tetap pakai `sessionResp` mentah.
- Kolom 5 "Jawaban Benar" untuk essay tak punya makna deterministik — biarkan "—" atau "(manual)". Yang penting kolom 4 (teks) + 6 (skor/status).
- "Detail Per Soal" sheet (386) — JANGAN sentuh. Jika menemukan call ke `ExcelExportHelper`, berhenti.

---

## PXF-10 (F-13, LOW) — `FinalizeEssayGrading` broadcast monitor group

**File:line:** `Controllers/AssessmentAdminController.cs:3753` (sebelum/berdekatan `return Json` akhir method) — tambah broadcast.

**Reference pattern (TIRU) — `:3204-3211` [VERIFIED]** (di method edit-jawaban, sama-sama monitor flow):
```csharp
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new
{
    sessionId = session.Id,
    workerName = session.User?.FullName ?? "Unknown",
    oldScore, newScore, oldIsPassed, newIsPassed,
    actorName, actorRole
});
```

**Pola batchKey [VERIFIED konsisten di 5 call-site]:** `$"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"` lalu group `$"monitor-{batchKey}"`. Group ini di-join di `AssessmentHub.cs:57` (`AddToGroupAsync($"monitor-{batchKey}")`).

**Event analog terdekat — `workerSubmitted` (`CMPController.cs:1786`) [VERIFIED]** (saat sesi selesai/grade):
```csharp
await _hubContext.Clients.Group($"monitor-{submitBatchKey}").SendAsync("workerSubmitted",
    new { sessionId = id, workerName = user.FullName, score = finalPercentage, result, status = pushStatus, totalQuestions = totalQuestionsSubmit });
```

**Fix shape (D-07):** Setelah `NotifyIfGroupCompleted` (`:3751`), sebelum `return Json` (`:3753`):
```csharp
var fbatchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", new {
    sessionId, workerName = session.User?.FullName ?? "Unknown",
    score = finalPercentage, result = isPassed ? "Pass" : "Fail",
    status = AssessmentConstants.AssessmentStatus.Completed, nomorSertifikat = updatedSession?.NomorSertifikat
});
```
(Event name = discretion. `workerSubmitted` paling konsisten karena monitor JS sudah handle event itu untuk transisi ke "Completed". Alternatif `workerAnswerEdited`.)

**Gotcha:**
- `session` di-load via `FindAsync` (`:3568`) → `session.User` / `session.Schedule` mungkin **tidak ter-include** (lazy nav null). Pola `:3204` ada di method yang load session dengan Include. CEK: planner harus pastikan `session.Title`/`Category` ter-load (scalar — aman, ada di tabel) tapi `session.User?.FullName` & `session.Schedule.Date` butuh navigation. Pakai `updatedSession` (`:3749`, juga `FindAsync` — sama masalahnya) atau load ulang dengan `.Include(s => s.User).Include(s => s.Schedule)`. **Title & Category = scalar di AssessmentSession (aman); Schedule = navigation (perlu Include atau ambil dari field schedule date scalar bila ada).** Verify field Schedule di model sebelum implement.
- `_hubContext` field sudah ada (`:27`). Event = fire-and-forget, jangan break primary flow (1-operator ≈ nihil dampak, LOW).

---

## PXF-11 (F-11, LOW) — Aria huruf A/B/C/D gambar opsi (Results + ExamSummary)

**File:line:** `Views/CMP/Results.cshtml:388` DAN `Views/CMP/ExamSummary.cshtml:59` (DUA surface — keduanya `AriaContext = "opsi"` statis). [VERIFIED]

**Current — Results.cshtml:356,388 [VERIFIED]:**
```cshtml
@foreach (var option in question.Options)   // :356 — TIDAK ada index var
{
    ...
    @await Html.PartialAsync("_QuestionImage", new { ImagePath = option.ImagePath, ImageAlt = option.ImageAlt, Cap = 120, AriaContext = "opsi" })  // :388
}
```
**Current — ExamSummary.cshtml:57,59 [VERIFIED]:**
```cshtml
@foreach (var optImg in item.OptionImages)   // :57 — TIDAK ada index var
{
    @await Html.PartialAsync("_QuestionImage", new { ImagePath = optImg.ImagePath, ImageAlt = optImg.ImageAlt, Cap = 120, AriaContext = "opsi" })  // :59
}
```

**Reference pattern (TIRU) — `StartExam.cshtml:125,134,148` [VERIFIED]:**
```cshtml
string[] letters = { "A", "B", "C", "D" };
...
var letter = oi < letters.Length ? letters[oi] : (oi + 1).ToString();
...
@await Html.PartialAsync("_QuestionImage", new { ..., AriaContext = "opsi " + letter })
```
Juga dipakai di `_PreviewQuestion.cshtml:67`. `oi` = loop index opsi.

**Fix shape (D-08):** Kedua loop **tidak punya index variable** → planner harus tambahkan. Dua opsi:
1. Ubah `foreach` jadi indexed: `@for (int oi = 0; oi < question.Options.Count; oi++) { var option = question.Options[oi]; ... }` lalu `var letter = oi < letters.Length ? letters[oi] : (oi+1).ToString();`
2. Atau `foreach (var (option, oi) in question.Options.Select((o, i) => (o, i)))`.

Lalu `AriaContext = "opsi " + letter`.

**Gotcha:**
- `_QuestionImage` baca `AriaContext` via reflection; aman bila absent — tapi di sini sudah dikirim, jadi cuma ganti nilai string.
- `_QuestionImage` render NOTHING bila `ImagePath` null → huruf hanya muncul di aria-label gambar yang ada. Tidak mempengaruhi opsi tanpa gambar.
- **D-09:** PXF-11 WAJIB Playwright (a11y render butuh runtime Razor — lesson Phase 354: grep+build tak cukup). Verify `aria-label` mengandung huruf di runtime.
- `question.Options` di Results = ViewModel collection; konfirmasi tipe (List vs IEnumerable) untuk indexing `[oi]` — bila bukan List, pakai pola Select.

---

## PXF-12 (F-20, LOW) — `SubmitExam` MC null-overwrite

**File:line:** `Controllers/CMPController.cs:1712-1716` (cabang upsert MC dalam `SubmitExam`).

**Current code [VERIFIED] `:1703-1726`:**
```csharp
int? selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : (int?)null;
if (selectedOptId.HasValue) { ... totalScore += q.ScoreValue; }

// Upsert MC answer only
if (existingResponses.TryGetValue(q.Id, out var existingResponse))
{
    existingResponse.PackageOptionId = selectedOptId;   // :1714 ← BUG: timpa jadi null bila soal absent di form
    existingResponse.SubmittedAt = DateTime.UtcNow;
}
else if (selectedOptId.HasValue)
{
    _context.PackageUserResponses.Add(new PackageUserResponse { ... PackageOptionId = selectedOptId, ... });
}
```

**Latent failure path [VERIFIED]:** Bila soal MC TIDAK ada di `answers` dict (mis. form partial / JS gagal kirim field), `selectedOptId = null`, lalu `:1714` menimpa `existingResponse.PackageOptionId = null` → **jawaban tersimpan (via autosave SignalR) hilang**. Happy-path (form lengkap) terlindungi karena `answers` punya semua key.

**Fix shape (D-02):** HANYA update bila key ADA di `answers`:
```csharp
if (existingResponses.TryGetValue(q.Id, out var existingResponse))
{
    if (answers.ContainsKey(q.Id))   // ← guard: jangan null-overwrite soal absent
    {
        existingResponse.PackageOptionId = selectedOptId;
        existingResponse.SubmittedAt = DateTime.UtcNow;
    }
}
else if (selectedOptId.HasValue) { ... add ... }
```

**Gotcha:**
- MA (`:1728`) dan Essay sudah TIDAK pakai jalur upsert ini (MA di-skip, score dari DB; Essay manual) — fix HANYA cabang `MultipleChoice`. Tidak ada efek samping ke MA/Essay.
- `answers` = `Dictionary<int,int>` (key=questionId). `selectedOptId` derive dari `answers.ContainsKey(q.Id)` di `:1703` — reuse cek itu.
- Unit-test-able (D-09): MC soal absent dari form tidak boleh nullify saved answer.

---

## PXF-13 (F-22, LOW) — `Hub.SaveTextAnswer` guard timer-expired

**File:line:** `Hubs/AssessmentHub.cs:134` (method `SaveTextAnswer`), guard hilang di antara validasi session (`:145`) dan truncate (`:151`).

**Reference pattern (TIRU VERBATIM) — `SaveMultipleAnswer:205-215` [VERIFIED]:**
```csharp
// T-298-08: Validasi timer belum expired (server-side check, memperhitungkan ExtraTimeMinutes)
if (session.StartedAt.HasValue && session.DurationMinutes > 0)
{
    var elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
    var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60;
    if (elapsed > allowed)
    {
        _logger.LogWarning("SaveMultipleAnswer: timer expired for session {SessionId}", sessionId);
        return;
    }
}
```

**Fix shape (D-02):** Paste blok identik ke `SaveTextAnswer` setelah `:149` (setelah `if (session == null) return;`), ubah string log jadi `"SaveTextAnswer: timer expired for session {SessionId}"`.

**Current `SaveTextAnswer:134-149` [VERIFIED]:** load session `Status == "InProgress"` lalu langsung truncate + upsert — TIDAK ada cek timer. `session` object punya `StartedAt`, `DurationMinutes`, `ExtraTimeMinutes` (sama entity AssessmentSession yang dipakai SaveMultipleAnswer) → tiru langsung bisa.

**Gotcha:**
- `session` di `SaveTextAnswer:143` di-query dengan `FirstOrDefaultAsync(...)` (full entity, bukan projection) → field timer tersedia. Aman.
- `_logger` sudah ada di Hub (dipakai `:147`).
- Catatan: `SaveTextAnswer` query session pakai literal `"InProgress"` (`:144`) bukan konstanta — konsisten dengan `SaveMultipleAnswer:198` (juga literal). JANGAN refactor itu (out of scope); cuma tambah guard timer.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cert nomor retry (PXF-08) | Loop retry custom | `GradingService.cs:287-318` verbatim + `CertNumberHelper` | Sudah handle unique-index collision + IsDuplicateKeyException |
| Timer expiry (PXF-13) | Hitung sendiri | `SaveMultipleAnswer:205-215` verbatim | Sudah perhitungkan ExtraTimeMinutes |
| Huruf opsi (PXF-11) | Mapping baru | `StartExam.cshtml:125/134` `letters[]` + index | Konsisten lintas surface |
| Broadcast group (PXF-10) | Group name baru | `monitor-{Title|Category|date}` `:3204` | Group ini yang di-join hub `:57` |

## Common Pitfalls

### Pitfall 1: Guard PXF-06 di status salah → bekukan grading normal
**Avoid:** Guard HANYA `Status == Completed`. JANGAN sertakan `PendingGrading`/`InProgress`. (D-01a — risiko tertinggi fase ini.)

### Pitfall 2: Pakai status "Failed" yang tidak ada
**Avoid:** Tidak ada konstanta `Failed`. Terminal = `Completed`. Lulus/gagal = `IsPassed` bool.

### Pitfall 3: PXF-09 salah surface (sentuh "Detail Per Soal" = 386)
**Avoid:** Edit HANYA `AssessmentAdminController.cs:4828` ("Detail Jawaban"). JANGAN `ExcelExportHelper.cs`.

### Pitfall 4: PXF-10 navigation null (session.User/Schedule via FindAsync)
**Avoid:** `FindAsync` tidak Include nav. Title/Category scalar aman; User.FullName & Schedule.Date butuh Include atau load ulang. Verify field Schedule sebelum implement.

### Pitfall 5: PXF-11 loop tanpa index var
**Avoid:** Results & ExamSummary `foreach` tak punya `oi`. Tambahkan index (for-loop atau Select). Verify `Options` indexable.

### Pitfall 6: PXF-08 nama variabel sessionId vs session.Id
**Avoid:** GradingService pakai `session.Id`; di FinalizeEssayGrading method = `sessionId` parameter. Sesuaikan.

## Runtime State Inventory

> Bukan rename/refactor murni, tapi diisi untuk kelengkapan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — 0 migration, tidak ada perubahan skema/key. Verified: semua fix = controller/hub/view logic | none |
| Live service config | None — tidak menyentuh konfigurasi service eksternal | none |
| OS-registered state | None | none |
| Secrets/env vars | None | none |
| Build artifacts | None — tidak ada rename package/binary | none |

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / dotnet | build + run verifikasi | ✓ (project aktif) | — | — |
| SQL Server lokal | DB lokal verify + e2e | ✓ (workflow existing) | — | SQLBrowser + lpc: override (ref) |
| Playwright | PXF-11 a11y e2e | ✓ (tests/e2e ada 23 spec) | — | — |

**Missing dependencies with no fallback:** None.

## Validation Architecture

`nyquist_validation: true` di `.planning/config.json` [VERIFIED] → section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Unit framework | xUnit — project `HcPortal.Tests/` [VERIFIED: AssessmentScoreAggregatorTests.cs ada] |
| Unit run | `dotnet test HcPortal.Tests` |
| E2E framework | Playwright (`tests/e2e/*.spec.ts`, 23 spec existing) |
| E2E run | `npx playwright test <spec> --workers=1` (combined WAJIB --workers=1) |
| Build gate | `dotnet build` (0 error) + `dotnet run` (cek localhost:5277) |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Command | File Exists? |
|-----|----------|-----------|---------|--------------|
| PXF-06 | guard block saat Completed, allow saat PendingGrading | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 (new test) |
| PXF-09 | essay cell tampil TextAnswer/EssayScore bukan "—" | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 |
| PXF-12 | MC absent dari form tidak nullify saved answer | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 |
| PXF-08 | cert retry 3× + log | manual/build | `dotnet build` + manual | manual (D-09 LOW) |
| PXF-10 | broadcast monitor | manual/build | `dotnet build` + manual | manual (D-09 LOW) |
| PXF-11 | aria-label opsi mengandung huruf A/B/C/D | e2e | `npx playwright test <new spec> --workers=1` | ❌ Wave 0 (new spec, mis. `aria-opsi-387.spec.ts`) |
| PXF-13 | SaveTextAnswer tolak tulis pasca timer | manual/build | `dotnet build` + manual | manual (D-09 LOW) |

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error) + unit test terkait.
- **Per wave merge:** `dotnet test HcPortal.Tests` full.
- **Phase gate:** build hijau + unit hijau + Playwright PXF-11 hijau + `dotnet run` localhost:5277 manual smoke.

### Wave 0 Gaps
- [ ] Unit test PXF-06 (guard status) — controller-level; cek pola test existing di `HcPortal.Tests/` (apakah ada controller test infra atau hanya pure-helper test seperti AssessmentScoreAggregatorTests). Jika hanya helper-test, PXF-06/09/12 logic mungkin perlu di-extract ke helper testable ATAU diverifikasi manual+build (D-09 proporsional — boleh manual jika controller test infra belum ada).
- [ ] Playwright spec baru untuk PXF-11 (aria huruf opsi Results + ExamSummary).

> **Catatan D-09 (proporsional):** Unit test diminta untuk PXF-06/09/12 "yang ada logika". Jika `HcPortal.Tests` hanya punya pure-helper tests (tidak ada WebApplicationFactory/controller harness), planner boleh: (a) extract logika ke helper murni testable, atau (b) turunkan ke manual+build untuk LOW. Verify infra test saat planning.

## Security Domain

`security_enforcement` tidak di-set di config → default enabled. Fase = bug-fix internal authenticated endpoint.

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | yes | `[Authorize(Roles="Admin, HC")]` sudah ada di SubmitEssayScore/FinalizeEssayGrading [VERIFIED] |
| V5 Input Validation | yes | PXF-06 score range guard sudah ada `:3539`; PXF-13 truncate MaxChars sudah ada |
| V6 Cryptography | no | tidak ada kripto |

**Threat relevan:**
| Pattern | STRIDE | Mitigation |
|---------|--------|-----------|
| Edit skor pasca-final (PXF-06) | Tampering | guard `Status == Completed` block (fix utama fase) |
| Tulis jawaban pasca-timer (PXF-13) | Tampering | guard timer-expiry mirror SaveMultipleAnswer (fix fase) |
| Data-loss null-overwrite (PXF-12) | (Integrity) | guard `answers.ContainsKey` (fix fase) |

CSRF: semua POST sudah `[ValidateAntiForgeryToken]` [VERIFIED `:3524`, `:3565`]. SignalR auth via `Context.UserIdentifier` [VERIFIED `:136`].

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Terminal/finalized = HANYA `Status == Completed` (tidak ada "Failed") | PXF-06 | LOW — diverifikasi: AssessmentConstants tak punya Failed; FinalizeEssayGrading early-return di Completed. Jika ada status terminal lain di luar enum (tidak ditemukan), guard kurang lengkap |
| A2 | `PackageUserResponse` punya `TextAnswer` + `EssayScore` accessible di `sessionResp` | PXF-09 | LOW — diverifikasi dipakai `:3492-3493`. Risiko nihil |
| A3 | `session.Schedule` adalah navigation property (perlu Include untuk PXF-10) | PXF-10 | MED — belum baca AssessmentSession model lengkap; planner WAJIB verify field Schedule (scalar date vs nav) sebelum implement broadcast |
| A4 | `question.Options` (Results VM) & `item.OptionImages` (ExamSummary) indexable untuk derive huruf | PXF-11 | LOW — pola Select((o,i)) bekerja untuk IEnumerable apa pun; risiko nihil |
| A5 | `HcPortal.Tests` punya infra untuk test controller logic PXF-06/09/12 | Validation | MED — hanya AssessmentScoreAggregatorTests (pure helper) yang dikonfirmasi. Jika tak ada controller harness, D-09 izinkan manual+build untuk LOW |

## Open Questions

1. **PXF-10 broadcast — field `Schedule` di AssessmentSession scalar atau navigation?**
   - Known: pola batchKey pakai `session.Schedule.Date` (`:3204`) — implies navigation `Schedule` dengan `.Date`. Tapi `:3204` di method yang load session via query ber-Include; FinalizeEssayGrading pakai `FindAsync` (no Include).
   - Recommendation: planner baca `Models/AssessmentSession.cs` untuk konfirmasi `Schedule` shape, lalu Include atau load ulang sebelum broadcast.

2. **Infra test controller (PXF-06/09/12).**
   - Known: `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` = pure helper test (14 test PASS).
   - Unclear: apakah ada WebApplicationFactory / controller harness.
   - Recommendation: planner cek `HcPortal.Tests/` saat plan; jika tak ada, terapkan D-09 proporsional (extract helper ATAU manual+build untuk LOW).

## Sources

### Primary (HIGH confidence — verified live HEAD this session)
- `Models/AssessmentConstants.cs:15-20` — status enum values
- `Controllers/AssessmentAdminController.cs:3525,3556-3760,4797-4838,3204-3211` — SubmitEssayScore, FinalizeEssayGrading, cert, BulkExport, broadcast pattern
- `Services/GradingService.cs:287-318` — cert retry-loop pattern
- `Hubs/AssessmentHub.cs:134-182,188-215,57` — SaveTextAnswer, SaveMultipleAnswer guard, monitor group join
- `Controllers/CMPController.cs:1703-1788` — SubmitExam MC upsert, workerSubmitted broadcast
- `Views/CMP/Results.cshtml:356,388`, `Views/CMP/ExamSummary.cshtml:57,59`, `Views/CMP/StartExam.cshtml:125,134,148`, `Views/Shared/_QuestionImage.cshtml` — aria opsi
- `.planning/config.json` — nyquist_validation: true
- `HcPortal.Tests/AssessmentScoreAggregatorTests.cs`, `tests/e2e/*.spec.ts` — test infra

### Tertiary (needs validation)
- Schedule field shape (A3/OQ1), controller test infra (A5/OQ2) — flagged for planner verify.

## Metadata

**Confidence breakdown:**
- Exact loc + current code (all 7 REQ): HIGH — read live HEAD
- Reference patterns to mirror: HIGH — read live HEAD
- PXF-10 nav-include detail: MEDIUM — needs AssessmentSession model read at plan time
- Test infra adequacy: MEDIUM — only pure-helper test confirmed

**Research date:** 2026-06-15
**Valid until:** 2026-06-22 (codebase aktif; Phase 386 belum dieksekusi — 387 ada file-overlap di AssessmentAdminController.cs, kerjakan setelah 386 untuk hindari konflik write)
