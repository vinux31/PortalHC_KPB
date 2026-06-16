# Phase 386: AssessmentAdminController Hardening - Research

**Researched:** 2026-06-15
**Domain:** ASP.NET Core 8 MVC controller hardening (validasi soal + lifecycle essay + display-path PDF) di codebase Portal HC KPB
**Confidence:** HIGH (semua klaim diverifikasi langsung dari source/test file repo ini, file:line tercantum)

> Catatan bahasa: prosa Bahasa Indonesia; identifier/path/kode English. CONTEXT.md sudah research-grade (adversarial-verified 4-agen) — riset ini TIDAK mengulang derivasi bug/keputusan, hanya mengisi gap IMPLEMENTASI + TEST dengan preseden konkret repo ini.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (ground truth — JANGAN re-derive)
- **PXF-02 D-01:** `MultipleChoice` & `MultipleAnswer` WAJIB ≥2 opsi ber-isi. Server-side, tolak via `TempData["Error"]` + `RedirectToAction("ManagePackageQuestions")`. Essay tak terkena.
- **PXF-02 D-02:** "opsi ber-isi" = `!string.IsNullOrWhiteSpace(optionX)` (ber-teks), selaras loop persist existing. Opsi gambar-tanpa-teks = deferred.
- **PXF-02 D-03:** Setiap opsi yang dicentang benar WAJIB ber-teks + total opsi ber-teks ≥2. **JANGAN ubah `correctCount` existing** (MC `==1`, MA `≥2`) — tambahan murni perketat, jangan longgarkan.
- **PXF-02 D-04:** Server-side WAJIB; client-side opsional (Claude discretion).
- **PXF-04 D-05:** "essay kosong" = no response row ATAU `TextAnswer` whitespace → 0 poin otomatis, TIDAK dihitung pending.
- **PXF-04 D-06:** SATU predikat byte-identik "essay pending" = `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` di **4 TITIK** (Monitoring 3308, page 3500, SubmitEssayScore 3547, finalize-gate 3620).
- **PXF-04 D-07:** auto-0 via `AssessmentScoreAggregator.Compute` existing — TIDAK buat row saat GET.
- **PXF-04 D-08:** `SubmitEssayScore` defensive upsert (create row bila belum ada) + **status-guard WAJIB** (tolak bila `Status != PendingGrading`).
- **PXF-05 D-09:** Ganti single-option logic di `GeneratePerPesertaPdf` dengan `AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ)` untuk SEMUA tipe.
- **PXF-05 D-10:** Kolom "Jawaban" MA = gabung SEMUA OptionText terpilih. MC tetap 1. Essay tetap TextAnswer.
- **PXF-05 D-11:** JANGAN sentuh scoring engine (`Compute`). Hanya display-path PDF.

### Claude's Discretion
- Struktur predikat "essay pending" (extension method / helper / inline) — selama byte-identik di 4 titik.
- Bentuk pesan validasi PXF-02 (jelas + Bahasa Indonesia + pola `QuestionTypeLabels.Short`).
- Validasi client-side PXF-02 (opsional).
- Format gabung opsi MA di PDF (koma vs newline).
- Struktur unit/Playwright test.

### Deferred Ideas (OUT OF SCOPE)
- Dukungan opsi gambar-tanpa-teks → Future (sentuh save-loop).
- F-03 (sisa, recompute/desync pasca-finalize penuh) → Future; HANYA status-guard minimal di-fold ke D-08.
- **F-DEV-02 / `Helpers/ExcelExportHelper.cs:83` `AddDetailPerSoalSheet`** (MA mislabel surface KEDUA) → beda file, OUT single-file phase ini. **Keputusan owner perlu:** fix bareng atau Future.
- F-18 (export by-paket ≥2 paket) → OUT/kondisional; mitigasi runbook 1 paket.
- F-02/F-01/F-06/F-11/F-13/F-19/F-20/F-22 → Future pasca-acara.
- `SyncPackagesToPost` (~5689) — JANGAN tambah validasi opsi (terlindungi transitif; akan tolak clone sah).
- `ImportPackageQuestions` (~6162) — wajibkan 4 opsi (aman dari F-DEV-01). Deklarasikan OUT eksplisit ATAU samakan rule MA import (≥2) — hindari split-brain.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **PXF-02** | Admin tak bisa simpan soal Single/Multiple Answer tanpa opsi jawaban | Validasi blok `CreateQuestion` L6440-6456 + `EditQuestion` L6647-6663 identik → sisipkan gate "≥2 opsi ber-teks + opsi-benar harus ber-teks" tepat setelah `correctCount` gate. Pola `TempData["Error"]` + `RedirectToAction` + `QuestionTypeLabels.Short` sudah ada. |
| **PXF-04** | HC bisa selesaikan penilaian walau peserta kosongkan essay; pending konsisten 4 surface | 4 predikat terverifikasi (lihat tabel B5). Predikat tunggal `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` EF-translatable di SQL Server (verified). Upsert idiom dari `AssessmentHub.SaveTextAnswer` L161-181. Status-guard pola dari finalize L3591. |
| **PXF-05** | PDF bukti per-peserta tandai MA benar/salah akurat (SetEquals, baca semua opsi) | `GeneratePerPesertaPdf` L4955-5112; blok mislabel L5070-5092 (FirstOrDefault 1-row). `IsQuestionCorrect` helper sudah dipakai essay L5085 → extend ke MC/MA. Preseden join MA: Excel per-session L4857-4863 (`SetEquals` + `string.Join`). |
</phase_requirements>

---

## Summary

Phase 386 adalah hotfix tiga-titik di **satu file** `Controllers/AssessmentAdminController.cs` (4099 LOC) untuk ujian lisensor real ~2026-06-17. Riset ini mengonfirmasi semua klaim CONTEXT.md di source nyata dan menemukan bahwa **codebase ini sudah punya semua preseden yang dibutuhkan** — tidak perlu library baru, tidak perlu pola baru. Tiga mekanisme kunci sudah teruji di repo: (1) helper murni `AssessmentScoreAggregator.IsQuestionCorrect/Compute` (EF-free, unit-testable, 14+ test hijau); (2) pola test "mirror-data-level real-SQL" di `EssayFinalizeRecomputeTests.cs` yang membuktikan behavior controller tanpa meng-instantiate controller 12-dependency; (3) idiom upsert `PackageUserResponse` di `AssessmentHub.SaveTextAnswer`.

Temuan riset paling penting (menghilangkan satu risiko utama): **`string.IsNullOrWhiteSpace` EF-translatable di SQL Server EF Core 8** (translate ke `[col] IS NULL OR [col] = N''`). Maka predikat pending tunggal `!string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null` boleh diterapkan langsung di query EF (site 3 `SubmitEssayScore` & site 4 `Monitoring`) TANPA paksa client-side evaluation. Dua site lain (page count L3500, finalize-gate L3620) sudah in-memory (data sudah `.ToListAsync()`-ed) sehingga predikat C# penuh aman di sana.

Untuk PXF-05 (PDF tak unit-testable karena `private byte[]` QuestPDF), rekomendasi LOW-RISK untuk hotfix = **extract helper murni** `ResolveQuestionCorrectness(q, responses)` + `BuildAnswerCell(q, responses)` di samping `AssessmentScoreAggregator`, lalu unit-test helper itu (label + join). Ini menghindari kebrittlean integration-test parsing PDF bytes dan selaras kill-drift pattern repo.

**Primary recommendation:** Ekstrak predikat pending tunggal + helper PDF-display ke `Helpers/` (pure, EF-free), terapkan byte-identik di 4 titik + PDF, kunci dengan unit test pola `IsQuestionCorrectTests.cs` + satu test "count-parity 4 fixture" pola `EssayFinalizeRecomputeTests.cs` (mirror-data real-SQL), tutup dengan Playwright PXF-02 (reject) + PXF-04 (finalize round-trip). 0 migration.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Validasi opsi soal (PXF-02) | API/Backend (controller action) | Browser (client-side opsional) | Sumber kebenaran server-side (D-04); form POST `CreateQuestion`/`EditQuestion` |
| Hitung pending essay (PXF-04) | API/Backend (controller + EF query) | — | Predikat dievaluasi 2x di EF query (Monitoring/SubmitEssayScore) + 2x in-memory (page/finalize) |
| Upsert essay score (PXF-04 D-08) | API/Backend + Database | — | INSERT/UPDATE `PackageUserResponse` (entity existing, no migration) |
| Visibility tombol "Selesaikan" | Browser (data-driven view + JS) | API/Backend (count source) | View L98 `@Model.EssayPendingCount==0` + JS L63-65 `data.allGraded` — TAK perlu edit view utk visibility (data-driven) |
| Label benar/salah PDF (PXF-05) | API/Backend (display-path) | — | `GeneratePerPesertaPdf` private method server-side; bukti resmi → akurasi prioritas |

---

## Standard Stack

Tidak ada library baru. Semua sudah terpasang & teruji di repo.

### Core (sudah ada — REUSE, jangan ubah logika)
| Aset | Lokasi | Purpose | Status |
|------|--------|---------|--------|
| `AssessmentScoreAggregator.IsQuestionCorrect` | `Helpers/AssessmentScoreAggregator.cs:73-98` | bool? correctness MC/MA/Essay (display-path, MA `SetEquals` non-empty guard L82) | 14+ unit test hijau (`IsQuestionCorrectTests.cs`) |
| `AssessmentScoreAggregator.Compute` | `Helpers/AssessmentScoreAggregator.cs:26-60` | Skor agregat (essay kosong = kontribusi 0 → auto-0, D-07) | LOCKED (D-04 formula), JANGAN ubah |
| `QuestionTypeLabels.Short` | `Models/QuestionTypeLabels.cs:13-19` | Label tipe utk pesan validasi ("Single Answer"/"Multiple Answer") | dipakai existing L6444/6449 |
| `PackageUserResponse` entity | `Models/PackageUserResponse.cs` | `PackageOptionId int?` (L19), `TextAnswer string?` (L29), `EssayScore int?` (L32), `SubmittedAt` default (L23) → upsert INSERT tanpa migration | — |

### Supporting (test infra — sudah ada)
| Tool | Versi | Purpose |
|------|-------|---------|
| xUnit | 2.9.3 | Unit + integration test (`HcPortal.Tests.csproj`) |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQL fixture (`HcPortalDB_Test_{guid}` disposable) |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Tersedia (tapi pola repo pilih real-SQL utk integration) |
| @playwright/test | ^1.58.2 | E2E `tests/` (baseURL `localhost:5277`, `--workers=1`) |
| exceljs ^4.4.0 / unzipper / jszip (node_modules) | — | parse Excel/ZIP bila integration-test PDF dipilih (tersedia di `tests/node_modules`) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extract helper PDF-display (rekomendasi) | Integration test parse PDF bytes via QuestPDF | Integration lebih brittle (parsing teks PDF), lebih berat, lebih lambat. Untuk hotfix → extract helper menang (testable + kill-drift). |
| Predikat di EF query (site 3/4) | Force `.AsEnumerable()` lalu predikat C# | TAK perlu — `IsNullOrWhiteSpace` EF-translatable (verified). Force-client = regresi performa tak perlu. |

**Installation:** Tidak ada. `dotnet restore` sudah cukup (semua paket terpasang).

**Version verification:** SDK `dotnet --version` = **8.0.418** [VERIFIED: Bash dotnet --version]. Target framework `net8.0` [VERIFIED: HcPortal.Tests.csproj:4]. EF Core 8.0.0 [VERIFIED: HcPortal.Tests.csproj:12-13].

---

## Architecture Patterns

### System Architecture Diagram (alur 4 predikat PXF-04)

```
                              ┌─────────────────────────────────────────────┐
                              │  SUMBER DATA: PackageUserResponse (per soal) │
                              │  row dgn TextAnswer + EssayScore (nullable)  │
                              └───────────────────┬─────────────────────────┘
                                                  │
        ┌─────────────────────┬───────────────────┼────────────────────┬──────────────────────┐
        │ (EF query)          │ (in-memory)       │ (EF query)         │ (in-memory)          │
        ▼                     ▼                   ▼                    ▼
  SITE 4 Monitoring     SITE 1 Page count   SITE 3 SubmitEssay   SITE 2 Finalize-gate
  L3308-3314            L3500               L3547-3551           L3620
  .Where(EssayScore     items.Count(        .CountAsync(         essayResponses.Any(
   ==null) ← BUG:        EssayScore==null)    EssayScore==null)    r.EssayScore==null)
   tak cek TextAnswer
        │                     │                   │                    │
        └─────────────────────┴───────────────────┴────────────────────┘
                              │  GANTI SEMUA dengan PREDIKAT TUNGGAL:
                              │  !string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null
                              ▼
              ┌───────────────────────────────────────────────────┐
              │ Konsistensi → tombol "Selesaikan Penilaian" muncul │
              │  - page-load: view L98 @Model.EssayPendingCount==0 │
              │  - pasca-save: JS L63-65 data.allGraded            │
              │  - finalize lolos: gate L3620 tak blokir essay kosong │
              │  → Compute (L3652) skor essay kosong = 0 (D-07)    │
              └───────────────────────────────────────────────────┘
```

> ⚠️ **CAVEAT D-06a (domain iterasi beda):** Page (site 1) enumerate **questions** (`items` = semua essayQs, ada/tidak ada row). Monitoring (site 4) enumerate **response rows**. Predikat sama TAPI COUNT bisa beda di kasus no-row (page: row absen → `EssayScore==null` → terhitung kalau cuma cek EssayScore; setelah tambah cek TextAnswer, page item no-row punya `TextAnswer==null` → TIDAK pending → COUNT turun, benar). Inilah kenapa test count-parity 4 fixture WAJIB.

### Pattern 1: Sisip validasi setelah `correctCount` gate (PXF-02)
**What:** Tambahkan blok validasi opsi tepat setelah gate `correctCount` existing (jangan ubah `correctCount`).
**When to use:** `CreateQuestion` (setelah L6451) DAN `EditQuestion` (setelah L6658) — identik.
**Example (mirror struktur existing L6441-6451):**
```csharp
// Source: AssessmentAdminController.cs:6440-6451 (struktur existing yang di-mirror)
// EXISTING gate (JANGAN UBAH):
var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
if (questionType == "MultipleChoice" && correctCount != 1) { /* TempData Error + Redirect */ }
if (questionType == "MultipleAnswer" && correctCount < 2)  { /* TempData Error + Redirect */ }

// === TAMBAHAN PXF-02 (sisip SETELAH gate di atas, SEBELUM Essay rubrik gate) ===
if (questionType == "MultipleChoice" || questionType == "MultipleAnswer")
{
    // D-02 — "ber-teks" = !IsNullOrWhiteSpace, selaras loop persist (L6488)
    var optionTexts = new[] { optionA, optionB, optionC, optionD };
    var optionCorrects = new[] { correctA, correctB, correctC, correctD };
    int filledCount = optionTexts.Count(t => !string.IsNullOrWhiteSpace(t));

    // D-01 — total opsi ber-teks ≥2
    if (filledCount < 2)
    {
        TempData["Error"] = $"{QuestionTypeLabels.Short(questionType)} membutuhkan minimal 2 opsi jawaban yang berisi teks.";
        return RedirectToAction("ManagePackageQuestions", new { packageId });
    }
    // D-03 — setiap opsi yang dicentang benar HARUS ber-teks
    for (int i = 0; i < 4; i++)
    {
        if (optionCorrects[i] && string.IsNullOrWhiteSpace(optionTexts[i]))
        {
            TempData["Error"] = $"Opsi yang ditandai sebagai jawaban benar harus berisi teks ({QuestionTypeLabels.Short(questionType)}).";
            return RedirectToAction("ManagePackageQuestions", new { packageId });
        }
    }
}
```
> Catatan: parameter order `optionA..D` (string?) + `correctA..D` (bool) identik di kedua method (Create L-signature ~6395, Edit L6609-6610). `packageId` tersedia di scope kedua method.

### Pattern 2: Predikat pending tunggal (PXF-04) — Claude discretion bentuknya
**What:** Satu definisi byte-identik. Rekomendasi: static helper di `Helpers/` agar bisa dipakai EF-query DAN in-memory.
**When to use:** 4 titik (lihat tabel B5).
**Tantangan:** EF query (site 3/4) butuh ekspresi yang EF-translatable; in-memory (site 1/2) bisa lambda biasa. `string.IsNullOrWhiteSpace` translatable (verified) → ekspresi SAMA bisa dipakai keduanya secara literal inline. Bila ingin helper terpusat untuk EF query, pakai `Expression<Func<PackageUserResponse,bool>>` atau cukup inline literal byte-identik (paling sederhana, paling aman untuk hotfix).
**Example:**
```csharp
// In-memory (site 1 page L3500, site 2 finalize L3620):
//   site 1: items.Count(i => !string.IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore == null)
//           (EssayGradingItemViewModel.TextAnswer ada — Models/AssessmentMonitoringViewModel.cs:79)
//   site 2: essayResponses.Any(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null)

// EF query (site 3 SubmitEssayScore L3547-3551, site 4 Monitoring L3308-3314):
//   .CountAsync(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null)
//   → SQL: WHERE (TextAnswer IS NULL OR TextAnswer = N'') = 0 AND EssayScore IS NULL
//   [VERIFIED: IsNullOrWhiteSpace EF-translatable SQL Server EF Core 6+/8 — github.com/dotnet/efcore#22916]
```

### Pattern 3: Defensive upsert + status-guard (PXF-04 D-08)
**What:** `SubmitEssayScore` bila row tak ada → create (bukan error); + guard `Status != PendingGrading`.
**Example (idiom upsert dari `AssessmentHub.SaveTextAnswer:161-181`, status-guard dari `FinalizeEssayGrading:3591`):**
```csharp
// Source: Hubs/AssessmentHub.cs:161-181 (upsert idiom) + AssessmentAdminController.cs:3591 (status-guard)
public async Task<IActionResult> SubmitEssayScore(int sessionId, int questionId, int score)
{
    // === TAMBAHAN D-08 status-guard (WAJIB — tutup lubang F-03 yang diperlebar upsert) ===
    var session = await _context.AssessmentSessions.FindAsync(sessionId);
    if (session == null)
        return Json(new { success = false, message = "Session tidak ditemukan" });
    if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
        return Json(new { success = false, message = "Penilaian hanya bisa dilakukan saat status Menunggu Penilaian." });

    // Load question dulu (validasi ScoreValue) — ada existing L3534
    var question = await _context.PackageQuestions.FindAsync(questionId);
    if (question == null)
        return Json(new { success = false, message = "Soal tidak ditemukan" });
    if (score < 0 || score > question.ScoreValue)
        return Json(new { success = false, message = $"Skor harus antara 0 dan {question.ScoreValue}" });

    // === D-08 upsert (ganti dead-end "Jawaban tidak ditemukan" L3530-3531) ===
    var response = await _context.PackageUserResponses
        .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
    if (response == null)
    {
        response = new PackageUserResponse
        {
            AssessmentSessionId = sessionId,
            PackageQuestionId = questionId,
            PackageOptionId = null,   // essay tak punya opsi
            TextAnswer = null,        // peserta kosongkan essay
            EssayScore = score
        };
        _context.PackageUserResponses.Add(response);
    }
    else
    {
        response.EssayScore = score;
    }
    await _context.SaveChangesAsync();

    // pendingCount — PAKAI PREDIKAT TUNGGAL (site 3)
    var pendingCount = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == sessionId)
        .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
            r => r.PackageQuestionId, q => q.Id, (r, q) => r)
        .CountAsync(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null);
    return Json(new { success = true, pendingCount, allGraded = pendingCount == 0 });
}
```
> ⚠️ Validasi urutan: `FindAsync(questionId)` untuk ScoreValue guard HARUS sebelum upsert agar `score > ScoreValue` tetap ditolak (jangan create row dgn skor invalid).

### Pattern 4: PDF display via helper (PXF-05 D-09/D-10)
**What:** Ganti blok L5074-5092 (FirstOrDefault 1-row) dengan helper murni yang resolve correctness + answer cell untuk semua tipe.
**Rekomendasi (extract helper, lower-risk):** Tambah dua static method di `AssessmentScoreAggregator` (atau helper baru sebelahnya) — `IsQuestionCorrect` SUDAH ada; tambah `BuildAnswerCell`:
```csharp
// Source: pola dari AssessmentScoreAggregator.IsQuestionCorrect:73-98 + Excel join L4860-4861
// Helper baru (pure, EF-free, unit-testable) — TARUH di Helpers/AssessmentScoreAggregator.cs
public static string BuildAnswerCell(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
{
    var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "Essay":
            var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            var txt = essay?.TextAnswer;
            if (string.IsNullOrEmpty(txt)) return "—";
            return txt.Length > 300 ? txt.Substring(0, 300) + "..." : txt;   // mirror L5083
        case "MultipleAnswer":
            // D-10 — gabung SEMUA opsi terpilih (mirror Excel L4860-4861)
            var selectedIds = list.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                                  .Select(r => r.PackageOptionId!.Value).ToHashSet();
            var joined = string.Join(", ",
                q.Options.Where(o => selectedIds.Contains(o.Id)).Select(o => o.OptionText));
            return string.IsNullOrEmpty(joined) ? "—" : joined;
        default: // MultipleChoice — 1 opsi
            var resp = list.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
            if (resp == null) return "—";
            var opt = q.Options.FirstOrDefault(o => o.Id == resp.PackageOptionId!.Value);
            return opt?.OptionText ?? "—";
    }
}
```
Lalu di `GeneratePerPesertaPdf` (ganti L5070-5092):
```csharp
var responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList();
bool? correct = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ);  // D-09 SEMUA tipe
string jawaban = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ);   // D-10
// statusColor / statusText TETAP (L5089-5092) — correct==true/false/null sudah benar utk MA/MC/Essay
```
> Verifier konfirmasi: MC default-branch `IsQuestionCorrect` byte-identik dgn PDF MC existing → no regresi MC. Essay sudah pakai helper L5085 → jangan double.

### Anti-Patterns to Avoid
- **Longgarkan `correctCount` jadi `≥1`** (PXF-02) — D-03 eksplisit JANGAN; MA tetap `≥2`. Tambahan murni perketat.
- **Tambah validasi di `SyncPackagesToPost` (~5689) atau `ImportPackageQuestions` (~6162)** — copy-path/importer; akan tolak clone sah. OUT.
- **Buat row `PackageUserResponse` saat GET** (PXF-04) — D-07 hindari side-effect on read; auto-0 lewat `Compute` saja.
- **Edit `Compute` / scoring engine** (PXF-05 D-11) — hanya display-path.
- **Edit view `EssayGrading.cshtml` untuk visibility tombol** — data-driven (`@Model.EssayPendingCount==0` L98 + JS `data.allGraded` L63), TAK perlu.
- **Force client-side eval (`.AsEnumerable()`) di site 3/4** — tak perlu, `IsNullOrWhiteSpace` translatable.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Correctness MC/MA/Essay di PDF | Logika inline FirstOrDefault per tipe | `AssessmentScoreAggregator.IsQuestionCorrect` | Sudah handle MA `SetEquals` non-empty guard + essay `>0`; 14 test hijau; kill-drift single source |
| Gabung opsi MA terpilih | Loop manual ambil OptionText | `string.Join(", ", Options.Where(selected).Select(OptionText))` | Preseden persis ada Excel L4860-4861 |
| Upsert PackageUserResponse | Insert ad-hoc | Idiom `SaveTextAnswer:161-181` (find-or-add, PackageOptionId=null) | Nullable field aman, SubmittedAt default — pola teruji |
| Auto-0 essay kosong | Materialisasi row skor 0 | `Compute` (kontribusi null=0) | D-07; no side-effect, maxScore tetap benar |
| Status-guard finalize | Cek ad-hoc | Pola `session.Status != PendingGrading` (L3591) | Sudah ada switch-message di finalize; mirror |

**Key insight:** Semua "solusi" sudah ada di repo ini. Phase ini = WIRING + KONSISTENSI, bukan algoritma baru. Setiap deviasi dari preseden repo = risiko drift di hari-H.

---

## Common Pitfalls

### Pitfall 1: COUNT divergen walau predikat sama (D-06a domain iterasi)
**What goes wrong:** Page enumerate questions, Monitoring enumerate rows → COUNT bisa beda di kasus no-row meski predikat byte-identik.
**Why it happens:** Page item no-row punya `TextAnswer==null` (dari `essayRespMap.TryGetValue` gagal → null); Monitoring no-row = tak ada row sama sekali (tak masuk query). Keduanya HARUS hasilkan "tidak pending".
**How to avoid:** Test count-parity 4 fixture (no-row, row-whitespace, row-filled-ungraded, graded) — assert keempat surface hasilkan COUNT pending identik.
**Warning signs:** Tombol "Selesaikan" muncul di page tapi Monitoring tampil pending>0 (atau sebaliknya).

### Pitfall 2: Upsert tanpa status-guard memperlebar F-03
**What goes wrong:** D-08 upsert tanpa guard → HC bisa create+score row pasca-Completed → `Score`/`IsPassed` desync, cacat PDF resmi.
**Why it happens:** `SubmitEssayScore` saat ini (L3525) TANPA cek status apa pun.
**How to avoid:** Guard `Status != PendingGrading` WAJIB bagian D-08 (bukan opsional). Verifier C-02.
**Warning signs:** Skor essay berubah setelah sesi Completed; re-finalize no-op tapi count berubah.

### Pitfall 3: ScoreValue guard dilewati saat upsert
**What goes wrong:** Bila `FindAsync(questionId)` untuk ScoreValue dipindah setelah upsert, row tercipta dengan skor invalid sebelum ditolak.
**How to avoid:** Urutan: status-guard → load question → range-guard → upsert. (Lihat Pattern 3.)

### Pitfall 4: MA mislabel surface KEDUA terlupakan
**What goes wrong:** `Helpers/ExcelExportHelper.cs:83` `AddDetailPerSoalSheet` (F-DEV-02) bug-class SAMA (FirstOrDefault 1-row) tapi BEDA file → OUT phase ini, TAPI ekspor resmi lisensor juga.
**How to avoid:** Planner deklarasikan eksplisit: keputusan owner (fix bareng = perluas scope ke Helpers/, atau Future). JANGAN diam-diam abaikan — bukti resmi.
**Warning signs:** Excel Summary "Detail Per Soal" tampil MA salah-label walau PDF sudah benar.

### Pitfall 5: Playwright e2e butuh app + DB lokal hidup + AD off
**What goes wrong:** Test login 500 / non-admin gagal login.
**How to avoid:** `Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5277` + `--workers=1` + SQLBrowser/shared-memory fix (reference_local_e2e_sql_env_fix). Admin `admin@pertamina.com`.

---

## Code Examples

Lihat **Architecture Patterns** (Pattern 1-4) untuk contoh verified per-REQ. Ringkas lokasi predikat existing (PXF-04 4 titik) di tabel B5 di bawah.

### Excel SetEquals + join precedent (referensi PXF-05, JANGAN diubah)
```csharp
// Source: AssessmentAdminController.cs:4857-4863 (Excel per-session — SUDAH benar)
else // MultipleAnswer
{
    var selectedIds = responses.Select(r => r.PackageOptionId!.Value).ToHashSet();
    jawabanText = string.Join(", ",
        q.Options.Where(o => selectedIds.Contains(o.Id)).Select(o => o.OptionText));
    var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
    correct = selectedIds.SetEquals(correctIds);
}
```

---

## B5. PXF-04 — 4 Titik Predikat (EXACT current expression + query shape)

| # | Titik | File:Line | Ekspresi SAAT INI | Query shape | Predikat baru | EF-translate? |
|---|-------|-----------|-------------------|-------------|---------------|---------------|
| 1 | Page count `EssayGrading` | `AssessmentAdminController.cs:3500` | `items.Count(i => i.EssayScore == null)` | **in-memory** (`items` sudah materialized; `EssayGradingItemViewModel.TextAnswer` tersedia, AssessmentMonitoringViewModel.cs:79) | `items.Count(i => !string.IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore == null)` | n/a (C#) |
| 2 | Finalize-gate | `AssessmentAdminController.cs:3620` | `essayResponses.Any(r => r.EssayScore == null)` | **in-memory** (`essayResponses` sudah `.ToListAsync()` L3615-3618) | `essayResponses.Any(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null)` | n/a (C#) |
| 3 | SubmitEssayScore pendingCount | `AssessmentAdminController.cs:3547-3551` | `.CountAsync(r => r.EssayScore == null)` (setelah Join QuestionType=="Essay") | **EF query** (server-side) | `.CountAsync(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null)` | ✅ YES (verified) |
| 4 | Monitoring count | `AssessmentAdminController.cs:3308-3314` | `.Where(r => ... && r.EssayScore == null)` (Join QuestionType=="Essay", GroupBy SessionId) | **EF query** (server-side) | tambah `&& !string.IsNullOrWhiteSpace(r.TextAnswer)` di `.Where` | ✅ YES (verified) |

> **KEY RISK RESOLVED:** `string.IsNullOrWhiteSpace` EF Core 8 SQL Server → `[TextAnswer] IS NULL OR [TextAnswer] = N''` (LTRIM/RTRIM dihapus sejak EF Core 6). Site 3 & 4 boleh inline langsung di query, TIDAK perlu client-side. [VERIFIED: github.com/dotnet/efcore#22916, learn.microsoft.com EF Core 6 whatsnew]
>
> **Nuance SQL Server:** Perbandingan `[col] = N''` di SQL Server menganggap string whitespace-only sama dengan empty (trailing whitespace di-ignore oleh `=`) → match semantik C# `IsNullOrWhiteSpace` untuk kasus pending. Leading-whitespace tab/newline murni di-handle EF translation. Cukup untuk D-05 (worker ketik lalu hapus → autosave row whitespace).

---

## Runtime State Inventory

> Bukan rename/refactor murni, tapi PXF-04 menyentuh perilaku data-write (upsert). Inventory ringkas:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `PackageUserResponse` rows whitespace-TextAnswer dari autosave (`SaveTextAnswer` L161-181 simpan walau kosong) | Tak ada migrasi data — predikat baru perlakukan whitespace = bukan pending. Row existing kompatibel. |
| Live service config | None — verified (tak ada config eksternal; semua di kode + DB existing) | none |
| OS-registered state | None — verified | none |
| Secrets/env vars | `Authentication__UseActiveDirectory` (env override lokal saja, tak diubah) | none |
| Build artifacts | None baru — 0 migration, 0 schema change, INSERT entity existing | `dotnet build` cukup |

**0 migration** terkonfirmasi: `PackageUserResponse` entity sudah punya semua field (Id auto, nullable FK/text/score, SubmittedAt default) → upsert = INSERT row entity existing, bukan schema change.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/run | ✓ | 8.0.418 | — |
| SQL Server LocalDB/Express (`localhost\SQLEXPRESS`) | Integration test fixture (real-SQL mirror) | ✓ (pola repo asumsikan ada; `EssayFinalizeRecomputeFixture` connect) | — | Pure unit test (`IsQuestionCorrectTests` style) jalan tanpa SQL bila Express absent |
| SQL Server (DB lokal `HcPortalDB_Dev`) | `dotnet run` lokal + manual verify | ✓ (asumsi dev env) | — | — |
| Node + Playwright + chromium | E2E `tests/` | ✓ (node_modules ada) | @playwright/test ^1.58.2 | `npx playwright install chromium` bila browser absent |
| SQLBrowser service + shared-memory conn | E2E login (NTLM loopback fix) | manual start | — | reference_local_e2e_sql_env_fix |

**Missing dependencies with no fallback:** Tidak ada yang memblokir (unit test pure jalan tanpa SQL; integration butuh SQLEXPRESS — skip via `--filter "Category!=Integration"` bila absent).

**Missing dependencies with fallback:** chromium (install on demand); SQLEXPRESS (integration test di-skip, unit test tetap jalan).

---

## Validation Architecture

> nyquist_validation = **true** (config.json:15) → section WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework (unit/integration) | xUnit 2.9.3 (`HcPortal.Tests/`) |
| Framework (e2e) | Playwright @playwright/test ^1.58.2 (`tests/`) |
| Config file (unit) | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Config file (e2e) | `tests/playwright.config.ts` (baseURL `localhost:5277`, `fullyParallel:false`) |
| Quick run (unit, pure only, no SQL) | `dotnet test --filter "Category!=Integration"` |
| Full suite (unit + integration real-SQL) | `dotnet test` |
| E2E run | `cd tests; npx playwright test <spec> --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PXF-02 | MC/MA 0-opsi ditolak; opsi-benar-tanpa-teks ditolak; ≥2 ber-teks diterima | unit (pure, controller-validasi mirror ATAU helper) | `dotnet test --filter "FullyQualifiedName~OptionValidation"` | ❌ Wave 0 — `OptionValidationTests.cs` |
| PXF-02 | Admin simpan Single 0-opsi → ditolak pesan jelas, soal tak tersimpan | e2e | `cd tests; npx playwright test option-validation-386 --workers=1` | ❌ Wave 0 — `option-validation-386.spec.ts` |
| PXF-04 | COUNT pending identik 4 surface × 4 fixture (no-row, whitespace, filled-ungraded, graded) | integration (mirror-data real-SQL) | `dotnet test --filter "FullyQualifiedName~EssayEmptyPendingParity"` | ❌ Wave 0 — `EssayEmptyPendingParityTests.cs` |
| PXF-04 | SubmitEssayScore upsert (no row → create) + status-guard (reject non-PendingGrading) | integration (mirror-data real-SQL) | (sama file di atas) | ❌ Wave 0 |
| PXF-04 | HC finalize sesi ≥1 essay kosong → tombol "Selesaikan" muncul → round-trip, essay kosong=0 | e2e | `cd tests; npx playwright test essay-empty-finalize-386 --workers=1` | ❌ Wave 0 — `essay-empty-finalize-386.spec.ts` |
| PXF-05 | MA benar={A,C,D} ⇒ "Benar" + Jawaban=semua opsi; partial/superset ⇒ "Salah" | unit (helper `BuildAnswerCell` + `IsQuestionCorrect`) | `dotnet test --filter "FullyQualifiedName~BuildAnswerCell"` | ❌ Wave 0 — `PdfAnswerCellTests.cs` |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (pure unit, <30s) + `dotnet build` 0 error.
- **Per wave merge:** `dotnet test` (full, termasuk integration real-SQL).
- **Phase gate:** `dotnet test` full hijau + `dotnet run` (localhost:5277) manual golden+edge + Playwright spec hijau `--workers=1` sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/OptionValidationTests.cs` — PXF-02 (pola `IsQuestionCorrectTests.cs` pure ATAU `EssayFinalizeRecomputeTests.cs` mirror untuk validasi controller-level). Rekomendasi: pure helper-level bila validasi diekstrak, atau mirror-data bila inline.
- [ ] `HcPortal.Tests/EssayEmptyPendingParityTests.cs` — PXF-04 count-parity 4 fixture + upsert + status-guard (pola `EssayFinalizeRecomputeTests.cs` real-SQL `[Trait("Category","Integration")]`; reuse `SeedEssayOnlyAsync` helper extended utk MC/MA + multi-fixture).
- [ ] `HcPortal.Tests/PdfAnswerCellTests.cs` — PXF-05 (pola `IsQuestionCorrectTests.cs` pure; test `BuildAnswerCell` MA join + `IsQuestionCorrect` MA label).
- [ ] `tests/e2e/option-validation-386.spec.ts` — PXF-02 reject (selectors: `#QuestionType`, `#option_A..D`, `#correct_A..D`, `#submitBtn`, assert `.alert-danger`, assert soal tak muncul di list).
- [ ] `tests/e2e/essay-empty-finalize-386.spec.ts` — PXF-04 finalize round-trip (reuse helper `gradeSingleEssaySession` / `fillEssayAnswer` dari `tests/e2e/helpers/examTypes.ts`).
- [ ] Framework install: sudah ada — `dotnet restore` + (e2e) `cd tests; npm install; npx playwright install chromium` bila perlu.

**Catatan reuse:** `EssayFinalizeRecomputeTests.cs:62-99` punya `SeedUserAsync` + `SeedEssayOnlyAsync` (seed AssessmentSession + AssessmentPackage + PackageQuestion(Essay) + PackageUserResponse + UserPackageAssignment) — PERSIS fixture yang dibutuhkan PXF-04 count-parity. Extend untuk: (a) tanpa response row, (b) row TextAnswer="" / "  ", (c) row TextAnswer terisi EssayScore=null, (d) graded. `MirrorSubmitEssayScoreAsync:155-171` & `MirrorFinalizeWriteAsync:295-311` = template mirror untuk site 2/3.

---

## Test Harness Conventions (jawaban research question A — detail)

**Base class / fixture pattern:**
- **Pure unit (no DB):** plain class, in-memory builders `Q(...)` + `Resp(...)` (verbatim di `IsQuestionCorrectTests.cs:23-33` & `AssessmentScoreAggregatorTests.cs:20-30`). NO `[Trait]`, selalu jalan. **Pakai ini untuk PXF-05 helper + PXF-02 bila validasi diekstrak.**
- **Integration real-SQL:** `IAsyncLifetime` fixture buat DB disposable `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync`, `EnsureDeletedAsync` on dispose (`EssayFinalizeRecomputeFixture:19-52`). Test class `IClassFixture<...>`, tag `[Trait("Category","Integration")]` → skip via `--filter "Category!=Integration"`. **Pakai ini untuk PXF-04 count-parity (butuh AssessmentSession+Package+Question+Response+Assignment relasi nyata).**
- **Mirror-data-level:** controller behavior diuji TANPA instantiate controller (12-dep) — logika di-replikasi di-level data PERSIS controller, reuse helper `AssessmentScoreAggregator` (single source). Lihat `MirrorSubmitEssayScoreAsync` (L155-171), `MirrorFinalizeWriteAsync` (L295-311), `ForwardAggregateAsync` (L102-114). Drift-guard komentar WAJIB ("bila body controller berubah, test ini harus diperbarui").
- **Reflection authz lock:** `EssaySubmitFinalizeAuthzTests` (L367-383) — assert `[Authorize(Roles=...)]` via reflection, pure, no DB. **Pakai untuk kunci status-guard D-08 punya `[Authorize(Roles="Admin, HC")]` tetap.**

**Bagaimana controller action diuji:** TIDAK instantiate controller. Mirror-data-level (replikasi query+write di test, assert DB state). Ctor controller berat (12-dep) → dihindari sengaja (komentar `EssayFinalizeRecomputeTests.cs:4-5,151-154`).

**Naming:** `{Subject}Tests.cs`, method `{Scenario}_{Condition}_{Expected}` (mis. `SubmitEssayScore_Rejects_WhenOutOfRange`).

**`AssessmentScoreAggregator` diuji:** pure, in-memory builder, no fixture (`AssessmentScoreAggregatorTests.cs` 14 test; `IsQuestionCorrectTests.cs` matrix MC/MA/Essay).

### PXF-05 testability (research question A2) — REKOMENDASI
`GeneratePerPesertaPdf` = `private byte[]` QuestPDF (L4955) → **tak tertembus unit test langsung**. Dua opsi CONTEXT C-03:
1. **Extract helper murni (REKOMENDASI hotfix, lower-risk):** `BuildAnswerCell(q, responses)` (baru) + `IsQuestionCorrect` (sudah ada) di `Helpers/AssessmentScoreAggregator.cs`. Unit-test pola `IsQuestionCorrectTests.cs`. Helper dipanggil dari PDF (ganti L5070-5092). **Lokasi helper: `Helpers/AssessmentScoreAggregator.cs`** (sebelah `IsQuestionCorrect` — konsisten kill-drift, sudah tempat correctness terpusat).
2. **Integration parse PDF bytes:** panggil `BulkExportPdf` (public, L4880-an) → unzip ZIP (`unzipper`/`jszip` ada di node_modules) → parse PDF text. **Lebih brittle + berat** → hindari untuk hotfix.

**Pilih opsi 1.** Test assert: MA benar={A,C,D} terpilih semua ⇒ `IsQuestionCorrect==true` ("Benar") + `BuildAnswerCell` = "Avtur, Solar, Bensin" (semua terpilih); partial {A,C} ⇒ `false` ("Salah"); superset {A,C,D,X} ⇒ `false`. MC 1-opsi ⇒ byte-identik existing. Essay ⇒ TextAnswer truncate 300 (D unchanged).

### PXF-04 fixture (research question A3)
Seed pattern ADA: `EssayFinalizeRecomputeTests.cs:71-99` `SeedEssayOnlyAsync` → `AssessmentSession` + `AssessmentPackage` + `PackageQuestion{QuestionType="Essay"}` + `PackageUserResponse` + `UserPackageAssignment{ShuffledQuestionIds}`. Untuk 4 fixture count-parity: parametrize response (skip add untuk no-row; `TextAnswer=""`/`"  "` untuk whitespace; `TextAnswer="jawab"` `EssayScore=null` untuk ungraded; `EssayScore=80` untuk graded). Assert 4 mirror-builder (Monitoring-style group query, page-style items, SubmitEssayScore-style Join+Count, finalize-gate Any) hasilkan COUNT pending identik.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IsNullOrWhiteSpace` → `LTRIM(RTRIM(col))=''` di SQL | `col IS NULL OR col = N''` | EF Core 6.0 | Predikat pending bisa inline di EF query (site 3/4) tanpa client-eval |
| Correctness inline per-surface (drift) | Helper terpusat `IsQuestionCorrect` | v30.0 ECG-01 (Phase 383) | PDF tinggal extend pemakaian; tak ada logika baru |
| Pending essay = `EssayScore==null` saja | + `!IsNullOrWhiteSpace(TextAnswer)` (D-06) | Phase 386 (ini) | Essay kosong tak lagi blokir finalize |

**Deprecated/outdated:** Tidak ada. Pola repo (xUnit + mirror-data + Playwright) konsisten lintas v25-v30.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `localhost\SQLEXPRESS` tersedia di dev env untuk integration test (pola `EssayFinalizeRecomputeFixture`) | Validation Architecture | Integration test PXF-04 skip; fallback pure unit + manual DB verify. Risiko rendah (pola sudah jalan v28-v30). |
| A2 | SQL Server `=N''` comparison memperlakukan whitespace-only (spasi/tab/newline) = empty untuk semua kasus autosave | B5 Nuance | Bila ada TextAnswer berisi HANYA newline yang TIDAK di-trim oleh `=`, predikat EF beda dari C# `IsNullOrWhiteSpace`. Mitigasi: integration test fixture row `TextAnswer="  "` + `"\t\n"` assert kedua hasilkan "tidak pending". |
| A3 | Opsi gambar-tanpa-teks TIDAK dipakai ujian lisensor (Avtur/Bensin/Solar ber-teks) | PXF-02 D-02 | Bila ada opsi gambar-saja, validasi text-based menolaknya. CONTEXT konfirmasi opsi ber-teks → risiko rendah. |
| A4 | `correct_A..D` checkbox + `option_A..D` text input selectors masih akurat di `ManagePackageQuestions.cshtml` | Validation (e2e) | E2E PXF-02 selector break. Verified via `wizardSelectors.ts:125-126` (existing e2e pakai) → risiko rendah. |

**Catatan:** Mayoritas klaim VERIFIED dari source. Assumptions di atas adalah env/data-shape, bukan logika — mudah dikonfirmasi saat Wave 0 / manual verify.

---

## Open Questions

1. **F-DEV-02 (ExcelExportHelper.cs:83) — fix bareng atau Future?**
   - What we know: bug-class SAMA dengan PXF-05 (FirstOrDefault 1-row MA mislabel), ekspor resmi lisensor juga, BEDA file (Helpers/).
   - What's unclear: apakah owner mau perluas scope single-file phase ini.
   - Recommendation: Planner ajukan ke owner sebagai keputusan eksplisit. Bila fix bareng → helper `BuildAnswerCell` + `IsQuestionCorrect` yang sama bisa dipakai di `AddDetailPerSoalSheet` (1 helper, 2 surface). Bila Future → catat di register, JANGAN diam.

2. **ImportPackageQuestions MA rule split-brain (≥1 import vs ≥2 form).**
   - What we know: importer wajibkan 4 opsi (aman 0-opsi) tapi MA-benar rule `≥1` (L6179) beda dari form `≥2`.
   - Recommendation: Planner deklarasikan importer OUT eksplisit (tak disentuh) ATAU samakan ke `≥2`. Pilih OUT untuk hotfix (importer tak hasilkan 0-opsi → tak blokir ujian); samakan = Future.

3. **PXF-02 unit test: helper-level vs controller-mirror?**
   - What we know: validasi PXF-02 = logika sederhana di controller (bukan helper terpisah saat ini).
   - Recommendation: Bila planner ekstrak validasi ke helper murni `ValidateQuestionOptions(type, texts, corrects)` → pure unit test (pola `IsQuestionCorrectTests`). Bila inline di controller → mirror-data test atau cukup Playwright e2e (reject path). Untuk konsistensi kill-drift + testability, ekstrak helper direkomendasikan (dipakai Create + Edit, sekali test).

---

## Sources

### Primary (HIGH confidence — source repo, file:line)
- `Controllers/AssessmentAdminController.cs` — L3308-3314 (Monitoring count), L3500 (page count), L3525-3554 (SubmitEssayScore), L3591 (status-guard pola), L3611-3666 (finalize gate+Compute), L4840-4873 (Excel SetEquals precedent), L4900-4953 (BulkExportPdf), L4955-5112 (GeneratePerPesertaPdf), L5070-5092 (PDF mislabel block), L6420-6504 (CreateQuestion validasi+persist), L6600-6747 (EditQuestion validasi+persist)
- `Helpers/AssessmentScoreAggregator.cs` — L26-60 (Compute), L73-98 (IsQuestionCorrect)
- `Hubs/AssessmentHub.cs` — L134-182 (SaveTextAnswer upsert), L188-252 (SaveMultipleAnswer multi-row)
- `Models/PackageUserResponse.cs` — nullable fields confirm upsert no-migration
- `Models/QuestionTypeLabels.cs` — Short() labels
- `Models/AssessmentMonitoringViewModel.cs:73-86` — EssayGradingItemViewModel.TextAnswer ada
- `HcPortal.Tests/IsQuestionCorrectTests.cs`, `AssessmentScoreAggregatorTests.cs`, `EssayFinalizeRecomputeTests.cs` — test pattern (pure + mirror-data real-SQL)
- `Views/Admin/EssayGrading.cshtml:98,109-120` + `wwwroot/js/essay-grading.js:63-65` — button data-driven
- `tests/playwright.config.ts`, `tests/package.json`, `tests/e2e/helpers/examTypes.ts:240-316`, `tests/e2e/helpers/wizardSelectors.ts:109-126`, `tests/e2e/export-per-peserta.spec.ts` — e2e harness
- `docs/DEV_WORKFLOW.md:64-94` — verify commands
- `.planning/config.json:15` — nyquist_validation true

### Secondary (MEDIUM confidence — verified web)
- EF Core 8 `string.IsNullOrWhiteSpace` SQL translation — [VERIFIED via 2 sources]:
  - https://github.com/dotnet/efcore/issues/22916 (SqlServer translation simplification)
  - https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-6.0/whatsnew (LTRIM/RTRIM removal EF Core 6+)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua aset existing diverifikasi file:line, 0 library baru.
- Architecture (4 predikat + upsert + PDF helper): HIGH — ekspresi current + query shape dibaca langsung; preseden persis (Excel SetEquals, SaveTextAnswer upsert).
- EF-translatability (key risk): MEDIUM-HIGH — 2 sumber resmi konfirmasi; nuance whitespace di-flag (A2) dengan mitigasi test.
- Test harness: HIGH — pola pure + mirror-data + Playwright dibaca dari file test nyata; fixture reuse teridentifikasi.
- Pitfalls: HIGH — bersumber CONTEXT adversarial-verified + konfirmasi source.

**Research date:** 2026-06-15
**Valid until:** 2026-06-22 (stable codebase; hotfix urgent — riset spesifik file, tahan selama controller tak refactor besar)

## RESEARCH COMPLETE
