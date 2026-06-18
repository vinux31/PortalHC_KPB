# Phase 395: Mode jawaban (input asli + auto-generate) - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC (Razor + vanilla JS wizard) · deterministic answer-pattern generation · subset-sum hit-target dengan integer-truncation grading · pre-persist dry-run preview
**Confidence:** HIGH (semua seam codebase-verified `file:line`; CONTEXT.md sudah diverifikasi via workflow wf_c2c181e3-87b)

> **Catatan untuk planner:** CONTEXT.md fase ini SANGAT lengkap & sudah codebase-verified. Riset ini **TIDAK** menurunkan ulang apa yang sudah dikunci di sana — fokusnya menutup 8 item teknis yang CONTEXT.md serahkan ke "Claude's Discretion" + bagian Validation Architecture (Nyquist). Baca CONTEXT.md sebagai sumber kebenaran; baca dokumen ini untuk spesifikasi algoritma + signature + test plan.

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Keputusan Terkunci (LOCKED)

- **D-01:** Pemilihan mode (input-asli vs auto-generate) = **DEFAULT per-room + OVERRIDE per-pekerja** (boleh campur 1 room). Resolusi mode = **controller-side**; kedua mode menghasilkan `InjectWorkerSpec.Answers` identik → nol perubahan service.
- **D-02:** Skor target auto-gen = **batch default + override per-pekerja**. Target adalah **input auto-gen saja**, TIDAK pernah masuk DTO/service. Controller hitung answer-set, emit `Answers` eksplisit.
- **D-03:** Tata letak input-asli = **satu pekerja per layar** (Prev/Next antar pekerja), BUKAN matrix/accordion. Sub-komponen self-contained menggantikan `#step5Placeholder` (`InjectAssessment.cshtml:404`), independen dari `goToStep` luar (:503-549). Daftar pekerja dari `#userCheckboxContainer .user-checkbox:checked`; soal dari client-state `injQuestions[]` (:498) — **nol round-trip server** untuk render.
- **D-04:** Essay (mode input-asli) = **teks WAJIB + skor wajib**, berlaku **per-essay-yang-DIISI (engaged)**. Essay di-skip = omit spec → grade 0 tanpa teks. **Butuh rule validasi BARU** di-scope `mode==input-asli && essay engaged` (JANGAN global — akan blokir essay auto-gen yang sah ber-teks kosong per D-08). Rekomendasi: di service, reuse 396, ber-guard mode.
- **D-05:** **Warn-but-allow** — soal di-skip dihitung salah/0, HC konfirmasi (tak diblok). **"Skip" = OMIT `InjectAnswerSpec` (BUKAN kirim spec kosong).** Kirim MC/MA kosong → reject-all (`MC Count!=1` :398, `MA Count<1` :400).
- **D-06:** Pembulatan auto-gen = **jamin capaian ≥ target**. Mekanis = pilih subset soal MC/MA jadi benar sehingga `floor(Σpoin_benar/maxScore×100)` = nilai **terkecil yang ≥ target**. **WAJIB cek persentase SETELAH truncation int** (boundary off-by-one).
- **D-07:** Variasi pola = **acak per-pekerja dgn SEED TETAP** (reproducible). Seed = hash stabil **(NIP + identitas room [Title+Category+CompletedAt] + target)** + **urutan soal stabil** agar preview == commit. **NIP-saja DITOLAK** (sintetis lintas room).
- **D-08:** Essay dalam auto-gen = **EssayScore di-set 0 / HC isi manual**; auto-gen **hanya** sentuh MC/MA. 3 interaksi locked:
  1. **HYBRID:** room ber-essay → mode auto-gen TETAP render input skor essay (+teks D-04). Tak pernah full-auto bila ada essay.
  2. **Tak ada state "isi nanti":** HC ketik skor essay di form yang sama sebelum commit (essay efektif input-asli walau worker mode=auto).
  3. **Ceiling target → BLOCKING:** bila `target > ceiling MC/MA-only` → **warning blocking per-worker** + arahkan switch input-asli. **JANGAN diam-diam cap di ceiling** (integritas sertifikasi).
- **D-09:** Preview = **skor final + status Lulus/Tidak**, TANPA preview nomor sertifikat. Engine = **`AssessmentScoreAggregator.Compute`** (pure, EF-free) atas answer-pattern usulan → preview == commit. **JANGAN panggil `CertNumberHelper` di preview.** Trigger = **tombol "Pratinjau" on-demand** (bukan per-keystroke).
- **D-10:** Auto-gen tak capai target persis → **tawarkan beralih ke input-asli** untuk pekerja itu. Unifikasi dgn D-01: "switch to manual" = set mode worker = input-asli (mekanisme override per-pekerja SAMA). **PRE-FILL** answer-grid dari pola auto-gen terakhir (HC tweak).

### Open Questions — RESOLVED (locked user 2026-06-18)
1. Auto-gen room BER-ESSAY → **HYBRID** (HC ketik skor essay manual; auto-gen MC/MA saja).
2. `target > ceiling MC/MA-only` → **BLOCKING + switch input-asli** (jangan cap diam-diam).
3. Resep seed → **NIP + identitas room + target** (NIP-saja ditolak).

### Claude's Discretion (teknis — diturunkan di dokumen ini)
- Lokasi lapisan auto-gen (rekomendasi server-side `BuildAutoGenAnswers`).
- Resep seed eksplisit (komposisi & hashing).
- Konstruksi MA "salah" untuk option-set degenerate.
- Model state per-worker + roster ringkas.
- Serialisasi `#AnswersJson` + `ParseAnswerVms`.
- Lokasi rule TextAnswer-wajib (D-04).

### Deferred Ideas (OUT OF SCOPE)
- Full-auto essay (skor essay auto-proporsional) — ditolak (Q-08=c).
- Preview nomor sertifikat pra-commit — ditolak (D-09=b).
- Matrix/accordion layout — ditolak (D-03=a).
- Blok commit sampai target persis — ditolak (D-10=c).
- Import Excel = Phase 396 (reuse `BuildAutoGenAnswers`); Link Pre/Post = Phase 397.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Deskripsi (REQUIREMENTS.md) | Dukungan Riset |
|----|-----------------------------|----------------|
| **INJ-08** | HC dapat **menginput jawaban asli** tiap pekerja per soal via form — MC/MA pilih opsi, Essay isi teks+skor manual — lalu sistem hitung skor lewat pipeline grading. | Sub-komponen 1-pekerja-per-layar (D-03) → emit `InjectAnswerSpec[]` per worker → serialize `#AnswersJson` → `ParseAnswerVms` → `MapToRequest` :116 → `InjectBatchAsync`. Rule TextAnswer-wajib BARU (D-04) di service. Skip=omit (D-05). Lihat §Architecture Pattern 1 & 2. |
| **INJ-09** | HC dapat **auto-generate pola jawaban dari skor target** (MC/MA pola konsisten dgn skor; Essay skor langsung), dengan **skor final aktual ditampilkan sebelum commit** (perhitungkan pembulatan). | `InjectAssessmentService.BuildAutoGenAnswers(...)` (subset-sum hit-target, §Don't Hand-Roll + Pattern 3). Ceiling-with-essay BLOCKING (D-08.3). Seed deterministik (§Pattern 4). Preview dry-run via `AssessmentScoreAggregator.Compute` (§Pattern 5). |
</phase_requirements>

## Summary

Fase ini mengisi **Langkah 5** wizard `/Admin/InjectAssessment` (placeholder `#step5Placeholder` dari 394), menambah mode **auto-generate dari skor target** + **preview skor final aktual pra-commit**, dan **mewire `#btnInject` → `InjectBatchAsync`** (commit pertama di milestone — 394 berhenti sebelum commit per D-07). Backend grading/persistence sudah ada & teruji dari 393 (`InjectAnswerSpec` + `InjectAssessmentService.cs:189-218`); yang baru adalah **lapisan translasi target→pola** + **validasi TextAnswer** + **dry-run preview** + **serialisasi answers** — bukan engine baru. "Seakan online" terjaga karena auto-gen hanya menghasilkan `SelectedOptionTempIds`/`EssayScore` eksplisit, lalu service yang sama menilai (byte-identik online).

Empat keputusan teknis paling load-bearing: (1) **Subset-sum hit-target** harus deterministik & re-cek `floor()` setelah seleksi (boundary off-by-one) — equal-weight memakai closed-form `k = ceil(target×N/100)`, mixed-weight memakai greedy+verify; (2) **Ceiling dengan essay** — denominator selalu termasuk `ScoreValue` essay tapi numerator auto-gen=0, jadi maksimum MC/MA-only bisa `< target` → BLOCKING per-worker, **jangan cap diam-diam**; (3) **Seed deterministik** harus pakai hash stabil lintas-proses (BUKAN `string.GetHashCode()` yang di-randomize per-proses di .NET modern) — rekomendasi SHA-256 atas string kanonik → seed `int`; (4) **Preview == commit** dijamin dengan memakai `AssessmentScoreAggregator.Compute` yang sama untuk dry-run dan untuk finalize.

**Primary recommendation:** Bangun lapisan auto-gen **server-side** sebagai `InjectAssessmentService.BuildAutoGenAnswers(questions, targetPercent, seed)` + dry-run preview endpoint berbasis `AssessmentScoreAggregator.Compute` di atas in-memory `PackageQuestion`/`PackageUserResponse` yang dipetakan dari pola usulan. Pertahankan service `InjectBatchAsync` sebagai "dumb executor" tanpa field `Mode`/`Target`. UI 1-pekerja-per-layar mengelola state `{nip, mode, targetScore, answers[]}` per worker, serialize ke `#AnswersJson` (paralel `#QuestionsJson`), commit via `MapToRequest` :116.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Input jawaban asli per soal (form 1-pekerja-per-layar) | Browser/Client (JS state) | — | D-03: render dari `injQuestions[]` + checked workers; nol round-trip render |
| Pemilihan mode + target (default+override) | Browser/Client | API/Controller (resolve saat POST) | D-01/D-02: mode/target tak pernah ke service; controller emit `Answers` eksplisit |
| **Auto-gen pola dari target (subset-sum)** | **API/Service** (`BuildAutoGenAnswers`) | — | Server-side agar Phase 396 (Excel) reuse; cegah duplikasi (CONTEXT.md discretion) |
| **Preview skor final pra-commit (dry-run)** | **API/Service** (`AssessmentScoreAggregator.Compute`) | Browser (trigger tombol) | D-09: preview==commit hanya bila engine identik; aggregator pure/EF-free |
| Validasi TextAnswer-wajib (essay engaged) | API/Service (`PreflightValidateAsync` ber-guard mode) | Browser (UX pre-check) | D-04: reuse 396; server-authoritative |
| Grading + persistence + cert | API/Service (`InjectBatchAsync`) | — | Sudah ada dari 393; nol perubahan happy-path |
| Serialisasi answers (client→server) | Browser (hidden `#AnswersJson`) | API (`ParseAnswerVms`) | Pola sama `#QuestionsJson`; submit-listener |

## Standard Stack

### Core (semua SUDAH ada di repo — fase ini menambah konsumen, bukan dependency)
| Komponen | Versi | Tujuan | Kenapa standar |
|----------|-------|--------|----------------|
| .NET / ASP.NET Core | net8.0 `[VERIFIED: HcPortal.csproj]` | Runtime MVC + Razor | Stack proyek |
| `AssessmentScoreAggregator.Compute` | existing `[VERIFIED: Helpers/AssessmentScoreAggregator.cs:26-60]` | **Engine preview D-09** — pure, EF-free, sinkron | Formula identik commit → preview==commit; single source of truth (kill-drift 363/365/376) |
| `InjectAssessmentService.InjectBatchAsync` | existing `[VERIFIED: Services/InjectAssessmentService.cs:42-334]` | Commit batch (grade+persist+cert+audit) | Teruji 393; konsumsi `Answers` identik untuk semua mode |
| `InjectAnswerSpec` / `InjectWorkerSpec` | existing `[VERIFIED: Models/InjectAssessmentDtos.cs:27-43]` | Kontrak jawaban worker | TAK ADA field Mode/Target → mode/target = lapisan baru di controller/view |
| `System.Security.Cryptography.SHA256` | BCL net8.0 `[VERIFIED: digunakan di Controllers/AssessmentAdminController.cs:2774]` | Hash stabil untuk seed D-07 | Deterministik lintas-proses (beda dari `string.GetHashCode()`) |
| `System.Text.Json` | BCL net8.0 `[VERIFIED: Controllers/InjectAssessmentController.cs:5,132]` | Serialize/deserialize `#AnswersJson` | Pola sama `ParseQuestionVms` :126-139 |

### Supporting
| Komponen | Versi | Tujuan | Kapan dipakai |
|----------|-------|--------|---------------|
| xUnit | 2.9.3 `[VERIFIED: HcPortal.Tests/HcPortal.Tests.csproj]` | Unit test pure (subset-sum, seed, ceiling) | Test math deterministik tanpa DB (pola `AssessmentScoreAggregatorTests.cs`) |
| Playwright | `tests/playwright.config.ts` `[VERIFIED]` | E2e runtime wizard (Razor+JS) | Wizard/sub-nav/serialize WAJIB runtime-check (lesson 354/392) |
| `System.Random(int seed)` | BCL net8.0 | RNG **deterministik** dengan seed tetap | Acak urutan pemilihan subset (D-07) — seeded, bukan crypto-RNG |

### Alternatives Considered
| Alih-alih | Bisa pakai | Tradeoff |
|-----------|------------|----------|
| Auto-gen server-side (`BuildAutoGenAnswers`) | Auto-gen client-side (JS) | Client lebih responsif TAPI duplikat ke Phase 396 (Excel) → drift. **Ditolak** demi reuse (CONTEXT.md discretion). |
| Preview via `Aggregator.Compute` dry-run | `GradingService.PreviewScoreAsync` | `PreviewScoreAsync` (`GradingService.cs:578-585`) beroperasi atas **session ter-persist**, bukan `InjectRequest` pra-persist → tak cocok. **Aggregator** bekerja atas POCO in-memory. |
| Seed SHA-256(canonical) | `string.GetHashCode()` | `GetHashCode()` **di-randomize per-proses** di .NET Core+ → preview & commit di request berbeda akan beda seed → pola beda → preview≠commit. **Wajib hash stabil.** |

**Installation:** Tidak ada paket baru. `[VERIFIED: semua komponen sudah di csproj]`

## Architecture Patterns

### System Architecture Diagram

```
                  ┌─────────────────────── BROWSER (InjectAssessment.cshtml + JS) ───────────────────────┐
                  │                                                                                       │
  Step 3 authoring│  injQuestions[]  (client-state, D-07, :498)                                           │
        │         │       │                                                                               │
        ▼         │       ▼                                                                               │
  Step 5 (BARU) ──┼──> Sub-komponen 1-pekerja-per-layar (IIFE, state per worker)                          │
  ganti           │       state[nip] = { mode: 'manual'|'auto', targetScore, answers[], essayScores{} }   │
  #step5Placeholder       │                                                                               │
                  │       ├── mode=manual → HC pilih opsi/ketik essay per soal                            │
                  │       ├── mode=auto   → input target → [tombol Pratinjau] ─┐                          │
                  │       │                                                    │ POST dry-run             │
                  │       │                          ┌─────────────────────────┘                         │
                  │       ▼                          ▼                                                    │
                  │   roster ringkas       ╔═══════════════════════╗  GET/POST /Admin/PreviewInjectScore │
                  │   (auto vs manual)     ║  PreviewRequest DTO    ║───────────┐                         │
                  │                        ╚═══════════════════════╝           │                         │
                  └──────────────────────────────────┬──────────────────────────┼─────────────────────────┘
                                                      │ submit (#btnInject)       │ JSON
                       serialize: #AnswersJson ◄──────┘                           ▼
                       (paralel #QuestionsJson :868-875)         ┌──────────────────────────────────────┐
                                  │                              │ CONTROLLER (InjectAssessmentController)│
                                  ▼                              │  PreviewInjectScore action (BARU)      │
              POST /Admin/InjectAssessment                       │   → BuildAutoGenAnswers (bila auto)    │
                                  │                              │   → map ke PackageQuestion/Response     │
                                  ▼                              │   → AssessmentScoreAggregator.Compute   │
   ┌────────────────────────────────────────────┐               │   → {percentage, isPassed, ceiling,     │
   │ CONTROLLER                                  │               │      overshoot, blocked}  (NO cert#)    │
   │  ParseAnswerVms (BARU, paralel              │               └──────────────────────────────────────┘
   │   ParseQuestionVms :126-139)                │
   │  MapToRequest :116  Answers = ParseAnswerVms│  ◄── (auto worker: BuildAutoGenAnswers dipanggil di sini ATAU
   │   → InjectWorkerSpec.Answers                │       client kirim pola final hasil preview — lihat Pattern 3)
   └──────────────────────┬──────────────────────┘
                          ▼
   ┌──────────────────────────────────────────────────────────────────┐
   │ SERVICE  InjectAssessmentService.InjectBatchAsync (existing 393)   │
   │   PreflightValidateAsync :341  (+ rule TextAnswer-wajib BARU D-04) │
   │   write responses :189-218 → GradeAndCompleteAsync → finalize      │
   │   → cert backdate (D-12) → AuditLog ManualInject                   │
   └──────────────────────────────────────────────────────────────────┘
                          ▼
                    DB (AssessmentSession + PackageUserResponse + cert)  — "seakan online"
```

### Recommended Project Structure (file yang disentuh — 0 file baru wajib)
```
Views/Admin/InjectAssessment.cshtml   # Step-5 sub-komponen + #AnswersJson hidden + preview JS + fix injTypeLabel
Controllers/InjectAssessmentController.cs   # ParseAnswerVms + MapToRequest:116 isi Answers + PreviewInjectScore action + wire commit
Services/InjectAssessmentService.cs   # BuildAutoGenAnswers + rule TextAnswer-wajib di PreflightValidateAsync
Models/InjectAssessmentDtos.cs        # (opsional) DTO preview: InjectPreviewRequest/Result — boleh taruh di sini
tests/e2e/inject-assessment-395.spec.ts (BARU)  # e2e mode jawaban + preview + commit
HcPortal.Tests/BuildAutoGenAnswersTests.cs (BARU)  # unit pure subset-sum/seed/ceiling
```

### Pattern 1: Sub-komponen 1-pekerja-per-layar (IIFE ber-state) — D-03
**What:** Step-5 menggantikan `#step5Placeholder` dengan komponen yang punya state sendiri (`currentWorkerIdx` + Prev/Next + render per-worker), independen dari `goToStep` luar (yang hanya toggle `.step-panel .d-none`, tak sentuh DOM dalam).
**When to use:** Render daftar jawaban per worker tanpa round-trip.
**Example (pola yang harus diikuti — bukan kode final):**
```javascript
// Source: pola existing InjectAssessment.cshtml (worker picker IIFE :650-719, authoring :721-876)
(function Step5AnswersController() {
    // state per worker; key = user.Id (checkbox value), simpan NIP untuk display
    var workerState = {};   // { [userId]: { mode:'manual'|'auto', targetScore:int, answers:{[qTempId]: {...}}, essay:{[qTempId]:{score,text}} } }
    var currentIdx = 0;
    function selectedWorkers() {
        return Array.from(document.querySelectorAll('#userCheckboxContainer .user-checkbox:checked'));
    }
    function renderCurrent() { /* render injQuestions[] form untuk workerState[selectedWorkers()[currentIdx].value] */ }
    // Prev/Next antar pekerja + roster ringkas (auto vs manual)
    // serialize → #AnswersJson di submit-listener (lihat Pattern 6)
})();
```
**Catatan kritis:** Daftar pekerja BISA berubah setelah HC kembali ke Step-2 (D-10 switch). Komponen harus **rebuild state** saat Step-5 ditampilkan (event saat `goToStep(5)`), prune worker yang sudah unchecked, prune answer dengan `QuestionTempId` yang sudah dihapus dari `injQuestions[]` (Pitfall TempId dangling).

### Pattern 2: Skip = OMIT answer-spec (BUKAN spec kosong) — D-05
**What:** Soal di-skip → JANGAN tambahkan `InjectAnswerSpec` untuk soal itu ke `answers[]`. Service akan menilai soal tanpa response = 0 (`AssessmentScoreAggregator` baris-hilang).
**Anti-pattern:** Kirim `{QuestionTempId: X, SelectedOptionTempIds: []}` untuk MC/MA → `PreflightValidateAsync` :398/:400 reject-all batch.
```javascript
// benar: skip = jangan push
if (!isSkipped(q)) answers.push({ QuestionTempId: q.TempId, SelectedOptionTempIds: selectedIds });
// SALAH: answers.push({ QuestionTempId: q.TempId, SelectedOptionTempIds: [] });  // → reject-all
```

### Pattern 3: `BuildAutoGenAnswers` server-side (subset-sum hit-target) — D-06/INJ-09
**What:** Lapisan translasi target → subset soal MC/MA benar → `List<InjectAnswerSpec>` eksplisit. Service tetap dumb executor.
**Signature rekomendasi:**
```csharp
// Services/InjectAssessmentService.cs — static, pure (testable tanpa DB), reuse Phase 396.
// Mengembalikan jawaban untuk SEMUA soal MC/MA (benar atau salah eksplisit). Essay TIDAK disentuh
// (D-08 — HC isi manual; auto-gen MC/MA only). Caller (controller) gabung dgn essay-answers manual.
public static AutoGenResult BuildAutoGenAnswers(
    IReadOnlyList<InjectQuestionSpec> questions,   // urutan stabil di-handle internal
    int targetPercent,
    int seed);                                      // dari ComputeAutoGenSeed (Pattern 4)

public sealed record AutoGenResult(
    List<InjectAnswerSpec> Answers,   // MC/MA only; benar=opsi-benar, salah=opsi-salah (lihat Pattern 7)
    int CeilingPercent,               // floor(Σ(MC+MA ScoreValue)/maxScore×100); maks dapat dicapai auto-gen
    int MaxScoreIncludingEssay,       // denominator (termasuk essay ScoreValue)
    bool TargetReachable);            // false bila targetPercent > CeilingPercent (D-08.3 BLOCKING)
```
**Algoritma (deterministik):**
1. Hitung `maxScore = Σ ScoreValue SEMUA soal` (termasuk essay — denominator selalu termasuk essay, `Aggregator.cs:35`).
2. Hitung `mcMaPoints = Σ ScoreValue soal MC/MA`. `CeilingPercent = floor(mcMaPoints / maxScore × 100)`.
3. Bila `targetPercent > CeilingPercent` → `TargetReachable=false`; tetap kembalikan pola "best effort = semua MC/MA benar" agar caller dapat tampilkan ceiling, TAPI controller WAJIB emit BLOCKING (D-08.3) dan tak commit worker itu.
4. **Equal-weight closed-form** (kasus umum — `ScoreValue=10` default semua, `AssessmentPackage.cs:41`): bila semua soal MC/MA punya `ScoreValue` sama & tak ada essay (atau essay diabaikan untuk target MC-only), `k = ceil(targetPercent × N / 100)` soal benar di antara `N` soal MC/MA. Verifikasi `floor(k×SV / maxScore × 100) >= targetPercent`; bila belum, `k++` (boundary off-by-one). Bila `k > N` → unreachable.
5. **Mixed-weight fallback** (ScoreValue berbeda / ada essay di denominator): greedy — urutkan soal MC/MA `OrderByDescending(ScoreValue)`, akumulasi sampai `floor(acc/maxScore×100) >= targetPercent`, lalu **verifikasi ulang `floor()`** dan coba kurangi soal terkecil bila masih ≥target (cari subset terkecil yang ≥target = smallest-such). Karena N soal kecil (puluhan), greedy+verify cukup; tak perlu DP penuh.
6. **Seleksi WHICH soal benar** (variasi pola D-07): dari himpunan soal MC/MA, pilih `k` soal menggunakan `new Random(seed)` untuk mengacak urutan kandidat sebelum memilih → pola berbeda antar pekerja tapi reproducible. Untuk mixed-weight, acak dalam grup ScoreValue-sama agar tetap hit target.
7. Untuk tiap soal: bila "benar" → `SelectedOptionTempIds` = opsi `IsCorrect` (MC: 1; MA: semua benar). Bila "salah" → konstruksi salah (Pattern 7).
**WAJIB:** re-cek `floor((Σpoin_benar)/maxScore×100)` SETELAH seleksi final; ini sumber kebenaran target (jangan percaya `k` saja di mixed-weight).

### Pattern 4: Seed deterministik lintas-proses — D-07
**What:** Seed `int` stabil dari `(NIP + identitas room + target)` + urutan soal stabil, agar preview (request A) == commit (request B).
**Signature rekomendasi:**
```csharp
// Source: pola hash via System.Security.Cryptography (sudah dipakai AssessmentAdminController.cs:2774)
public static int ComputeAutoGenSeed(string nip, string title, string category, DateTime completedAt, int targetPercent)
{
    // string kanonik — pisahkan dgn '' (unit separator) agar tak ada tabrakan concat.
    var canonical = string.Join('', nip.Trim(), title.Trim(), category.Trim(),
                                 completedAt.ToString("yyyy-MM-dd"), targetPercent.ToString());
    using var sha = System.Security.Cryptography.SHA256.Create();
    var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical));
    return BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;   // non-negatif untuk new Random(seed)
}
```
**Kenapa SHA-256, bukan `string.GetHashCode()`:** `[CITED: learn.microsoft.com/dotnet/api/system.string.gethashcode]` — `GetHashCode()` TIDAK dijamin stabil antar run aplikasi dan **di-randomize per-proses** di .NET Core/.NET 5+. Preview dan commit terjadi di request HTTP berbeda (mungkin proses berbeda di multi-instance) → seed beda → pola beda → preview≠commit. SHA-256 deterministik mutlak. `[VERIFIED]`
**Urutan soal stabil:** internal `BuildAutoGenAnswers` HARUS `questions.OrderBy(q => q.Order).ThenBy(q => q.TempId)` sebelum seleksi (sama dengan urutan persist `InjectAssessmentService.cs:146` `OrderBy(q => q.Order)`). `CompletedAt` pakai komponen tanggal saja (`yyyy-MM-dd`) — bukan ticks — agar tahan terhadap perbedaan jam preview vs commit.
**Konsekuensi (konfirmasi OK di CONTEXT.md):** Elemen Teknis breakdown per worker bervariasi (natural).

### Pattern 5: Dry-run preview pra-persist (preview == commit) — D-09
**What:** Endpoint baru menghitung skor final dari pola usulan TANPA menulis DB, memakai engine yang sama dengan commit.
**Mekanik:** Petakan `InjectQuestionSpec[]` + `InjectAnswerSpec[]` (pola usulan untuk satu worker) → in-memory `PackageQuestion`/`PackageOption`/`PackageUserResponse` (pakai TempId sebagai Id sintetis — aman karena Aggregator pakai Id untuk match, bukan persist) → `AssessmentScoreAggregator.Compute(questions, responses, passPercentage)`. Formula identik dengan finalize (`InjectAssessmentService.cs:236`) → **preview == commit dijamin**.
**Endpoint shape rekomendasi:**
```
POST /Admin/PreviewInjectScore   [Authorize(Roles="Admin,HC")] [ValidateAntiForgeryToken]
Request  (per-worker, JSON body): { passPercentage, questions:[{tempId,type,scoreValue,options:[{tempId,isCorrect}]}],
                                    mode, targetScore, answers:[{questionTempId, selectedOptionTempIds, essayScore}] }
Response (JSON): { percentage, isPassed, totalScore, maxScore,
                   ceilingPercent, targetReachable, overshoot (actual-target),
                   blockingMessage?: "target X% tak tercapai: bobot essay dikecualikan, maks Y%" }
```
**Aturan:**
- Bila `mode==auto`: server panggil `BuildAutoGenAnswers` lalu gabung essay manual dari request, lalu Compute. **Penting:** server harus mengembalikan POLA yang dipakai (agar client pre-fill saat switch manual, D-10) ATAU client memanggil preview dan commit dengan pola identik. Rekomendasi: **server adalah otoritas pola** — client kirim {mode,target,essayScores}, server hitung pola+skor, kembalikan keduanya; saat commit, client kirim pola final (hasil preview) sebagai answers eksplisit → `MapToRequest` tinggal pasang.
- **JANGAN** panggil `CertNumberHelper` di preview (D-09): `GetNextSeqAsync` read-only (Max+1) tapi nomor tak ter-reserve sampai commit (unique-index 3-retry `:258-283`) → nomor pra-commit menyesatkan.
- Trigger = tombol **"Pratinjau"** on-demand (bukan per-keystroke).

### Pattern 6: Serialisasi `#AnswersJson` (paralel `#QuestionsJson`) — INJ-08
**What:** Hidden field + `JSON.stringify` di submit-listener + `ParseAnswerVms` server.
```html
<!-- View: tambah di samping #QuestionsJson (InjectAssessment.cshtml:312) -->
<input type="hidden" asp-for="AnswersJson" id="AnswersJson" />
```
```javascript
// View: di submit-listener yang SAMA dgn #QuestionsJson (:871-874) — JANGAN buat listener kedua.
injForm.addEventListener('submit', function () {
    document.getElementById('QuestionsJson').value = JSON.stringify(injQuestions);
    document.getElementById('AnswersJson').value = JSON.stringify(buildWorkerAnswersPayload()); // [{userId/nip, answers:[...]}]
});
```
```csharp
// Controller: ParseAnswerVms paralel ParseQuestionVms:126-139; isi MapToRequest:116.
// VM butuh field baru: public string? AnswersJson { get; set; }
// Bentuk per-worker: { Nip (atau UserId→map), Answers: List<InjectAnswerSpec-shape> }
```
**Pitfall kritis (CONTEXT.md):** lupa serialize answers di submit = POST `Answers` kosong → semua worker grade 0 (silent). E2e WAJIB cek skor pasca-commit, bukan hanya "redirect sukses".

### Pattern 7: Konstruksi MA/MC "salah" + degenerate option-set (rule deterministik) — Claude-discretion
**What:** Untuk soal yang dipilih "salah" oleh auto-gen, hasilkan `SelectedOptionTempIds` yang dijamin di-grade salah, deterministik.
**Pre-scan per soal (WAJIB):** hitung `correctCount` & `optionCount`.
| Tipe | Kondisi opsi | Rule "salah" deterministik |
|------|--------------|----------------------------|
| MC | ≥2 opsi, ≥1 benar, ≥1 salah | pilih 1 opsi `!IsCorrect` (deterministik: `OrderBy(TempId).First(!IsCorrect)`) |
| MC | semua opsi `IsCorrect` (degenerate) | **MUSTAHIL salah** — opsi apa pun benar. Flag: soal ini selalu menyumbang poin. `BuildAutoGenAnswers` perlakukan sebagai "forced-correct" (kurangi dari pool yang bisa dibuat salah). |
| MC | 1 opsi saja | forced-correct (pilih opsi itu). Flag. |
| MA | ≥2 opsi | salah via **proper subset** kunci atau himpunan ≠ kunci. Deterministik: bila ada opsi salah, pilih {1 opsi salah}; else (semua benar) pilih proper-subset kunci (mis. buang 1 → `SetEquals` gagal → salah). |
| MA | semua opsi benar & hanya 1 opsi | forced-correct. Flag. |
**Konsekuensi untuk subset-sum:** soal forced-correct mengurangi N yang fleksibel. `BuildAutoGenAnswers` HARUS: (a) tetapkan forced-correct dulu, (b) hitung sisa target dari soal yang fleksibel, (c) bila forced-correct saja sudah melewati target → semua sisa dibuat salah; bila forced-correct + semua fleksibel benar masih < target → unreachable (jalur sama dengan ceiling essay D-08.3 → BLOCKING).

### Anti-Patterns to Avoid
- **Hitung skor di JS untuk commit** — biarkan service grade. JS hanya untuk preview (dan itu pun via endpoint server, bukan rumus JS lokal) → cegah drift preview≠commit.
- **`string.GetHashCode()` untuk seed** — randomized per-proses → preview≠commit.
- **Cap diam-diam di ceiling** saat target unreachable — pelanggaran integritas sertifikasi (D-08.3). Harus BLOCKING + arahkan switch.
- **Skip = kirim spec kosong** — reject-all batch (D-05). Skip = omit.
- **Validasi TextAnswer global** — blokir essay auto-gen yang sah teks-kosong (D-04). Scope ke `mode==input-asli && essay engaged`.
- **Listener submit kedua untuk answers** — pakai listener yang sama dengan `#QuestionsJson`.

## Don't Hand-Roll

| Masalah | Jangan bangun | Pakai | Kenapa |
|---------|---------------|-------|--------|
| Hitung skor preview | Rumus persentase di JS | `AssessmentScoreAggregator.Compute` via endpoint dry-run | Drift preview≠commit; aggregator = single source of truth (kill-drift 363/365/376) |
| Grading/persist/cert commit | Tulis ulang loop response/cert | `InjectAssessmentService.InjectBatchAsync` (existing) | Teruji 393; "seakan online" terjaga |
| Hash seed | XOR string char manual / `GetHashCode` | `SHA256` atas string kanonik | Deterministik lintas-proses |
| Korelasi benar/salah grading | Re-implement IsCorrect | `AssessmentScoreAggregator.IsQuestionCorrect` (`:73`) bila perlu display | Sudah ditest |
| Subset-sum optimal | DP/ILP penuh | Closed-form equal-weight + greedy+verify mixed | N soal kecil; equal-weight (default) closed-form cukup; greedy+verify menjamin ≥target |

**Key insight:** Seluruh "kebenaran skor" sudah dimiliki `AssessmentScoreAggregator` + `InjectBatchAsync`. Fase ini **hanya** menambah lapisan translasi (target→pola) dan jalur preview yang **memanggil engine yang sama**. Satu-satunya algoritma baru yang genuine adalah subset-sum hit-target + seleksi seeded — dan itu pun harus selalu di-verifikasi ulang lewat `floor()` yang sama.

## Common Pitfalls

### Pitfall 1: Boundary off-by-one truncation (D-06)
**What goes wrong:** Asumsi `k = ceil(target×N/100)` soal benar pasti menghasilkan `floor(skor) >= target`. Di mixed-weight (ScoreValue berbeda) atau saat essay menambah denominator, `floor()` bisa jatuh 1% di bawah target.
**Why:** Integer truncation membuang pecahan; `ceil` di pemilihan ≠ `floor` di grading.
**How to avoid:** Selalu hitung `floor(Σpoin_benar/maxScore×100)` SETELAH seleksi; bila `< target` tambah 1 soal benar dan ulang. Unit test boundary (lihat Validation Architecture).
**Warning signs:** Preview menampilkan target-1% (mis. target 80 → tampil 79).

### Pitfall 2: Ceiling essay membuat target mustahil (D-08.3)
**What goes wrong:** Room dengan essay berbobot besar; auto-gen tak bisa capai target karena numerator essay=0.
**Why:** Denominator selalu termasuk `ScoreValue` essay (`Aggregator.cs:35`), numerator auto-gen=0 → maks = `floor(mcMaPoints/maxScore×100) < 100`.
**How to avoid:** `BuildAutoGenAnswers` kembalikan `CeilingPercent` + `TargetReachable=false`; controller emit BLOCKING per-worker + arahkan switch input-asli (HC naikkan skor essay manual). Jangan commit worker itu.
**Warning signs:** target diminta > ceiling; jangan tampil "tercapai".

### Pitfall 3: TempId dangling pasca-edit soal (D-04/D-05)
**What goes wrong:** HC isi jawaban, lalu kembali ke Step-3 hapus/edit soal → `answers[]` punya `QuestionTempId` basi. Service skip unmatched silent (`:191 continue`) → soal itu grade 0 tanpa peringatan.
**Why:** State answers terpisah dari `injQuestions[]`.
**How to avoid:** Saat masuk Step-5 (atau saat `injQuestions[]` berubah), prune answer dengan TempId yang tak ada lagi + re-validate. Roster ringkas bantu HC lihat kelengkapan.
**Warning signs:** Skor lebih rendah dari ekspektasi; soal "hilang" dari grid.

### Pitfall 4: Serialize answers terlewat di submit (silent grade 0)
**What goes wrong:** Lupa isi `#AnswersJson` di submit-listener → POST `Answers` kosong → semua worker 0.
**How to avoid:** Isi di listener yang sama dengan `#QuestionsJson` (:871). E2e cek skor pasca-commit di DB/`/CMP/Results`, bukan sekadar redirect.

### Pitfall 5: Seed tak stabil → preview≠commit (D-07/D-09)
**What goes wrong:** Seed dari `GetHashCode()` atau dari timestamp/`new Random()` tanpa seed → pola preview ≠ pola commit.
**How to avoid:** `ComputeAutoGenSeed` (SHA-256, Pattern 4); `CompletedAt` pakai tanggal saja; server otoritas pola (commit pakai pola hasil preview).

### Pitfall 6: Razor+JS runtime tak ter-cover grep/build (lesson 354/392)
**What goes wrong:** Wizard sub-nav/serialize "kelihatan benar" di kode tapi gagal runtime (event tak ter-bind, IIFE scope, override View path).
**How to avoid:** Playwright WAJIB. Jalankan app dari MAIN tree (`Authentication__UseActiveDirectory=false`) pasca-edit view (Razor di-embed saat build, tanpa RuntimeCompilation). `[VERIFIED: inject-assessment-394.spec.ts:1-6]`

## Code Examples

### Map pola usulan → in-memory untuk preview (preview==commit)
```csharp
// Source: AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:26-60) — formula identik commit.
// TempId dipakai sebagai Id sintetis in-memory (Aggregator match by Id, EF-free, tak persist).
var qInMem = questions.Select(q => new PackageQuestion {
    Id = q.TempId, QuestionType = q.QuestionType, ScoreValue = q.ScoreValue,
    Options = q.Options.Select(o => new PackageOption { Id = o.TempId, IsCorrect = o.IsCorrect }).ToList()
}).ToList();
var respInMem = new List<PackageUserResponse>();
foreach (var a in answers) {
    var q = questions.First(x => x.TempId == a.QuestionTempId);
    if ((q.QuestionType ?? "MultipleChoice") == "Essay")
        respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, EssayScore = a.EssayScore, TextAnswer = a.TextAnswer });
    else
        foreach (var optTemp in a.SelectedOptionTempIds)
            respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, PackageOptionId = optTemp });
}
var agg = AssessmentScoreAggregator.Compute(qInMem, respInMem, passPercentage);
// agg.Percentage == skor commit (jaminan D-09); agg.IsPassed == status Lulus/Tidak.
```

### Rule TextAnswer-wajib (D-04) — sisip di `PreflightValidateAsync` essay branch (:382-391)
```csharp
// Source: Services/InjectAssessmentService.cs:382-391 (essay branch existing).
// Tambah SETELAH cek EssayScore. Guard: hanya bila essay "engaged" (ada spec essay & EssayScore terisi).
// Mode tak ada di DTO (D-02) → "engaged" = essay spec dikirim DAN EssayScore.HasValue.
// Auto-gen essay (D-08) yang TAK dikirim sebagai answer = omit → tak masuk loop ini → tak terblokir. ✔ konsisten D-05.
if (qType == "Essay" && ans.EssayScore.HasValue && string.IsNullOrWhiteSpace(ans.TextAnswer))
    errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Teks jawaban essay NIP {w.Nip} wajib diisi (mode input asli)." });
```
> **Verifikasi planner:** pastikan auto-gen TIDAK pernah emit `InjectAnswerSpec` essay dengan `EssayScore` tapi `TextAnswer` kosong KECUALI memang HC isi teks (HYBRID D-08.1). Bila HYBRID essay selalu butuh teks (D-04), maka rule ini benar untuk semua jalur. Karena D-08.2 menyatakan "HC ketik skor essay (+teks) di form yang sama" → teks essay SELALU wajib bila skor diisi. Rule di atas tepat.

### Fix carry-in cosmetic 394 (LBL-02) — ~4 baris
```javascript
// InjectAssessment.cshtml:735-738 — injTypeLabel
function injTypeLabel(t) {
    if (t === 'MultipleChoice') return 'Single Answer';   // dari 'Pilihan Ganda'
    if (t === 'MultipleAnswer') return 'Multiple Answer';  // dari 'Pilihan Majemuk'
    if (t === 'Essay') return 'Essay';
    return t;
}
// InjectAssessment.cshtml:832-833 — pesan validasi authoring
if (qType === 'MultipleChoice' && correctCount !== 1) problems.push('Single Answer harus tepat 1 jawaban benar.');
if (qType === 'MultipleAnswer' && correctCount < 2) problems.push('Multiple Answer butuh minimal 2 jawaban benar.');
```

## State of the Art

| Pendekatan lama | Pendekatan sekarang | Kapan berubah | Dampak |
|-----------------|---------------------|---------------|--------|
| Spec §13 "ambil terdekat & laporkan" | D-06 "jamin ≥target" (subset-sum, re-verify floor) | discuss 395 | Auto-gen bias jamin-lulus, bukan terdekat; overshoot di-toleransi (D-10) |
| `string.GetHashCode()` stabil (asumsi lama) | Hash randomized per-proses di .NET Core+ → wajib SHA-256 untuk determinisme | .NET Core 1.0+ | Seed D-07 wajib hash kriptografis stabil |
| Preview via session ter-persist (`PreviewScoreAsync`) | Dry-run pra-persist via `Aggregator.Compute` | fase ini | Preview tanpa write DB; preview==commit |

**Deprecated/outdated:**
- Label "Pilihan Ganda"/"Pilihan Majemuk" di `injTypeLabel`/validasi → "Single Answer"/"Multiple Answer" (LBL-02, carry-in 394).

## Assumptions Log

| # | Klaim | Section | Risiko bila salah |
|---|-------|---------|-------------------|
| A1 | "Server adalah otoritas pola" (client kirim mode/target/essay, server hitung pola+skor, commit pakai pola hasil preview) menjamin preview==commit termudah | Pattern 5 | Bila planner pilih client-kirim-pola-final, harus pastikan client tak re-randomize antara preview & submit. Keduanya valid; rekomendasi server-otoritas. |
| A2 | Greedy+verify cukup untuk mixed-weight subset-sum (tak perlu DP) karena N soal kecil (puluhan) | Pattern 3 | Bila ada room dengan ratusan soal ScoreValue sangat bervariasi, greedy bisa tak menemukan subset terkecil — tapi tetap menemukan subset ≥target (cukup untuk D-06). "Smallest-such" jadi best-effort. |
| A3 | Essay HYBRID selalu butuh `TextAnswer` bila `EssayScore` diisi (D-04 berlaku ke essay auto-gen HYBRID juga) | Code Examples (rule D-04) | Bila HC boleh isi skor essay tanpa teks di mode auto-gen, rule memblokir keliru. CONTEXT D-08.2 mengindikasikan teks wajib — perlu konfirmasi di plan-check. |
| A4 | `CompletedAt` granularity tanggal (`yyyy-MM-dd`) cukup unik untuk identitas room dalam seed | Pattern 4 | Dua room beda dengan Title+Category+tanggal identik → seed kolisi (pola sama). Sangat jarang; bila perlu, tambah PassPercentage/DurationMinutes ke kanonik. |

## Open Questions

1. **Client-kirim-pola vs server-otoritas-pola untuk commit (A1)**
   - Yang kita tahu: preview==commit terjamin bila pola yang dinilai preview = pola yang di-commit.
   - Yang belum jelas: apakah client menyimpan pola hasil preview lalu mengirimnya saat submit, atau server me-rekomputasi pola dari seed saat commit.
   - Rekomendasi: server-otoritas — saat commit, controller panggil `BuildAutoGenAnswers(seed)` ulang untuk worker auto (seed deterministik → pola identik dengan preview). Lebih sederhana & tahan-tamper. Planner putuskan; keduanya benar bila seed stabil.

2. **Roster ringkas auto-vs-manual — sejauh apa detail (Claude-discretion)**
   - Yang kita tahu: HC perlu jejak di nav 1-pekerja-per-layar.
   - Rekomendasi: tabel ringkas (NIP/nama, mode badge, skor preview bila sudah di-Pratinjau, flag BLOCKING). Cukup untuk UX; tak butuh persist.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/run | ✓ (asumsi dev box) | net8.0 | — |
| SQL Server (SQLEXPRESS) | e2e + integration test (DB lokal HcPortalDB_Dev) | ✓ `[VERIFIED: InjectAssessmentServiceTests.cs:33]` | — | unit pure (Aggregator/BuildAutoGen) tak butuh DB |
| Node + Playwright | e2e wizard runtime | ✓ `[VERIFIED: tests/node_modules + playwright.config.ts]` | — | — |

**Missing dependencies with no fallback:** Tidak ada. Semua tooling sudah ada.

## Validation Architecture

> Nyquist validation **ENABLED** (`workflow.nyquist_validation: true` `[VERIFIED: .planning/config.json]`).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (unit/integration) + Playwright (e2e) `[VERIFIED: csproj + playwright.config.ts]` |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/playwright.config.ts` |
| Quick run command | `dotnet test --filter "Category!=Integration"` (unit pure, no DB) |
| Full suite command | `dotnet test` (incl. Integration real-SQL) + `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INJ-09 | Subset-sum hit-target: hasil ≥target, smallest-such (equal-weight closed-form) | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers&Category!=Integration"` | ❌ Wave 0 |
| INJ-09 | Truncation boundary off-by-one (mixed-weight: floor ulang setelah seleksi) | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers_Boundary"` | ❌ Wave 0 |
| INJ-09 | Ceiling-with-essay → `TargetReachable=false` saat target>ceiling (BLOCKING) | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers_EssayCeiling"` | ❌ Wave 0 |
| INJ-09 | Seed reproducibility: input sama → pola sama; room beda → pola beda | unit | `dotnet test --filter "FullyQualifiedName~ComputeAutoGenSeed"` | ❌ Wave 0 |
| INJ-09 | MA/MC degenerate (semua benar / 1-opsi) → rule deterministik (forced-correct) | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers_Degenerate"` | ❌ Wave 0 |
| INJ-09 | Preview == commit: `Aggregator.Compute` atas pola = skor finalize | unit + integration | `dotnet test --filter "FullyQualifiedName~PreviewEqualsCommit"` | ❌ Wave 0 |
| INJ-08 | Skip = omit → soal tak terjawab grade 0 (bukan reject-all) | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~SkipOmit"` | ❌ Wave 0 |
| INJ-08 | Rule TextAnswer-wajib (essay engaged) → reject; auto-gen essay teks-kosong tak terblokir | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~TextAnswerRequired"` | ❌ Wave 0 |
| INJ-08/09 | E2e: input-asli + auto-gen + Pratinjau + commit → skor muncul di /CMP/Results | e2e | `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` | ❌ Wave 0 |
| INJ-08 | E2e: serialize `#AnswersJson` terisi (anti silent-grade-0) | e2e | (bagian spec 395) | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (unit pure — subset-sum/seed/ceiling/degenerate; cepat, no DB).
- **Per wave merge:** `dotnet test` (incl. Integration real-SQL preview==commit + skip-omit + TextAnswer) + `npx playwright test e2e/inject-assessment-395.spec.ts --workers=1`.
- **Phase gate:** Full suite hijau + e2e hijau sebelum `/gsd-verify-work`. Cek manual: skor di `/CMP/Results` pasca-commit (UAT 398).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/BuildAutoGenAnswersTests.cs` — unit pure: hit-target ≥target & smallest-such (equal-weight), boundary off-by-one (mixed-weight), ceiling-essay `TargetReachable=false`, seed reproducible (sama→sama, beda-room→beda), degenerate (all-correct/1-opsi forced-correct). Pola: `AssessmentScoreAggregatorTests.cs` (in-memory builder, no fixture). Covers REQ-INJ-09.
- [ ] `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` (atau tambah ke fixture existing `[Trait Category=Integration]`) — preview `Aggregator.Compute` == skor `InjectBatchAsync` finalize; skip=omit grade 0; TextAnswer-wajib reject. Pola: `InjectAssessmentServiceTests.cs` (disposable real-SQL fixture). Covers REQ-INJ-08/09.
- [ ] `tests/e2e/inject-assessment-395.spec.ts` — Playwright: mode input-asli (pilih opsi/essay), mode auto-gen (target→Pratinjau→skor tampil), commit `#btnInject`→skor di /CMP/Results, `#AnswersJson` terisi. Pola: `inject-assessment-394.spec.ts` (login, fillWizardToConfirm). Covers REQ-INJ-08/09.
- [ ] Tak ada framework install — xUnit + Playwright sudah ada.

## Security Domain

> `security_enforcement` tak diset di config → diperlakukan ENABLED. Fase ini menambah 1 endpoint POST (`PreviewInjectScore`) + jalur commit baru.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin,HC")]` (pola `InjectAssessmentController` :40/:50) — endpoint preview WAJIB sama |
| V3 Session Management | yes | ASP.NET Core Identity cookie (existing) |
| V4 Access Control | yes | RBAC Admin,HC server-authoritative; preview & commit keduanya guard |
| V5 Input Validation | yes | `PreflightValidateAsync` (existing) + rule TextAnswer baru; target 0..100; EssayScore 0..ScoreValue (:389); deserialize `#AnswersJson` try/catch (pola `ParseQuestionVms` :136) |
| V6 Cryptography | yes (terbatas) | SHA-256 untuk seed = **non-secret** (hanya determinisme, bukan keamanan) — JANGAN klaim sebagai kontrol keamanan |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Forge skor via POST manipulasi (client kirim pola/skor palsu) | Tampering | Server-authoritative: skor selalu di-hitung service/aggregator dari pola, bukan diterima dari client. Preview read-only (no write). Commit tetap lewat `PreflightValidateAsync` + `GradeAndCompleteAsync`. |
| Cap diam-diam di ceiling → sertifikat tak sah | Repudiation/Integrity | D-08.3 BLOCKING — tak commit worker unreachable; AuditLog ManualInject (:301) lacak penuh. |
| CSRF pada endpoint preview/commit | Tampering | `[ValidateAntiForgeryToken]` (existing pola :51) — preview WAJIB juga. |
| Akses non-Admin/HC ke preview | Elevation | `[Authorize(Roles="Admin,HC")]`. |
| XSS via QuestionText/answers di render JS | Tampering | `.textContent` (existing pola :763 XSS-safe) — sub-komponen 395 WAJIB ikut. |
| `#AnswersJson` malformed → exception | DoS | try/catch JsonException → fallback (pola :136). |

## Sources

### Primary (HIGH confidence)
- `Helpers/AssessmentScoreAggregator.cs:26-60,73` — engine preview/grading (formula D-04 LOCKED, EF-free) `[VERIFIED]`
- `Services/InjectAssessmentService.cs:42-334` (write :189-218, validate :341-402, finalize :230-243, cert :258-283), `:146` urutan soal `[VERIFIED]`
- `Models/InjectAssessmentDtos.cs:27-62` — DTO (tak ada Mode/Target) `[VERIFIED]`
- `Controllers/InjectAssessmentController.cs:71-139` — MapToRequest :116, ParseQuestionVms :126 `[VERIFIED]`
- `Views/Admin/InjectAssessment.cshtml` — step5 :398-417, btnInject :479, QuestionsJson :312/:868-875, injQuestions :498, injTypeLabel :735-738, validasi :832-833, picker :577/605 `[VERIFIED]`
- `Helpers/CertNumberHelper.cs:23-35` — read-only no-reserve (D-09) `[VERIFIED]`
- `Services/GradingService.cs:92-145` — formula commit identik aggregator `[VERIFIED]`
- `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` (pola unit pure) + `InjectAssessmentServiceTests.cs:1-90` (pola fixture real-SQL) `[VERIFIED]`
- `tests/e2e/inject-assessment-394.spec.ts:1-60` + `tests/playwright.config.ts` (pola e2e) `[VERIFIED]`
- `.planning/config.json` — nyquist_validation:true `[VERIFIED]`
- `.planning/phases/395-.../395-CONTEXT.md` — keputusan LOCKED (workflow wf_c2c181e3-87b) `[VERIFIED]`

### Secondary (MEDIUM confidence)
- `string.GetHashCode()` di-randomize per-proses di .NET Core+ `[CITED: learn.microsoft.com/dotnet/api/system.string.gethashcode — "hash code itself is not guaranteed to be stable" + per-app-domain randomization]`

### Tertiary (LOW confidence)
- (tidak ada — semua klaim load-bearing diverifikasi di codebase atau dokumentasi resmi)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua komponen existing, file:line verified
- Architecture (seam wire-up): HIGH — CONTEXT.md + kode verified
- Subset-sum/seed/ceiling algoritma: HIGH (logika) — re-verify floor wajib di test; A1/A2 di Assumptions
- Pitfalls: HIGH — diturunkan dari kode + CONTEXT.md + lesson fase lalu

**Research date:** 2026-06-18
**Valid until:** ~30 hari (stack stabil; tak ada dependency fast-moving)
