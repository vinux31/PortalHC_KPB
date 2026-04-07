# Phase 299: Worker Pre-Post Test + Comparison - Research

**Researched:** 2026-04-07
**Domain:** ASP.NET Core MVC — worker-facing assessment UI, controller extension, Razor view modification
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Tampilan Card Pre-Post di My Assessments**
- D-01: Pre-Test dan Post-Test ditampilkan sebagai 2 card terpisah (reuse layout card existing) yang secara visual terhubung
- D-02: Badge 'Pre-Test' / 'Post-Test' ditampilkan di samping badge kategori (OJT, IHT, dll) pada masing-masing card
- D-03: Visual linking: kedua card punya left-border warna sama dan ada ikon panah kecil dari Pre ke Post

**Blocking & Sequencing**
- D-04: Saat Pre-Test belum Completed, card Post-Test tampil tapi disabled (grayed out) dengan tombol disabled dan teks 'Selesaikan Pre-Test terlebih dahulu'
- D-05: Jika Pre-Test expired (ExamWindowCloseDate lewat) tanpa Completed, Post-Test otomatis blocked. Card Post menampilkan 'Pre-Test tidak diselesaikan'
- D-06: Saat Pre-Test Completed tapi jadwal Post belum tiba, card Post tampil normal (tidak grayed) dengan tombol disabled 'Opens [tanggal]' seperti assessment Upcoming biasa
- D-07: Setelah worker submit Pre-Test, tidak ada info tambahan tentang Post-Test. Flow submit Pre sama seperti assessment biasa

**Riwayat Ujian**
- D-09: Di tabel Riwayat Ujian, Pre dan Post ditampilkan sebagai 2 baris terpisah dengan badge 'Pre-Test' / 'Post-Test' di kolom Judul
- D-10: Post-Test row di Riwayat punya tombol 'Detail' yang mengarah ke halaman Results (yang sudah include section perbandingan)

**Halaman Perbandingan**
- D-11: Perbandingan Pre vs Post ditampilkan di dalam halaman Results existing (CMPController.Results) saat worker buka detail Post-Test. Bukan halaman baru terpisah
- D-12: Results action diextend — jika session adalah PostTest + ada linked Pre via LinkedSessionId, query skor Pre dan kirim comparison data ke ViewBag
- D-13: Section perbandingan ditampilkan di atas (sebelum detail soal) sebagai tabel: Elemen Kompetensi | Skor Pre | Skor Post | Gain Score
- D-14: Section perbandingan hanya muncul di Results Post-Test. Results Pre-Test tampil seperti assessment biasa tanpa section comparison

**Gain Score Display**
- D-15: Gain score ditampilkan sebagai angka persentase dengan warna: hijau jika positif, merah jika negatif, abu-abu jika 0. Format: '+67%' (hijau), '-10%' (merah)
- D-16: Formula gain: (PostScore - PreScore) / (100 - PreScore) × 100. Edge case PreScore = 100 → Gain = 100
- D-17: Jika Post-Test punya soal Essay belum dinilai (HasManualGrading = true, IsPassed = null), gain score menampilkan '—' dengan pesan 'Menunggu penilaian Essay'. Gain baru muncul setelah semua Essay dinilai

### Claude's Discretion
- Exact visual design left-border + arrow icon untuk card linking
- Tab filtering strategy untuk Pre-Post cards (D-08)
- Loading state dan empty state untuk section perbandingan
- Responsive layout section perbandingan di mobile

### Deferred Ideas (OUT OF SCOPE)
- Real-time update saat HC reset — butuh SignalR, bukan scope Phase 299
- AssessmentPhase multi-tahap — kolom sudah ada, use case belum ada
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WKPPT-01 | Daftar assessment menampilkan Pre dan Post sebagai 2 card terhubung | Assessment.cshtml card loop extend dengan AssessmentType detection + wrapper group |
| WKPPT-02 | Post-Test tidak bisa dimulai sebelum Pre-Test Completed | Card rendering logic: cek status Pre via LinkedSessionId; render tombol disabled dengan pesan |
| WKPPT-03 | Post-Test dapat dimulai setelah Pre-Test Completed dan jadwal tiba | Tab filtering dan status check existing sudah cover ini; hanya card rendering yang perlu extend |
| WKPPT-04 | Halaman perbandingan Pre vs Post dengan skor side-by-side | CMPController.Results extend + ViewBag.ComparisonData + Results.cshtml section baru |
| WKPPT-05 | Gain score formula: (Post - Pre) / (100 - Pre) x 100 | Computed di controller — tidak butuh library matematika tambahan |
| WKPPT-06 | PreScore = 100 → Gain = 100 | Edge case sederhana: if/else di controller sebelum formula |
| WKPPT-07 | Gain score per elemen kompetensi | SessionElemenTeknisScore tersedia — query linked pre-session dan join by ElemenTeknis name |
</phase_requirements>

---

## Summary

Phase ini adalah pure UI/UX extension pada worker-facing assessment flow. Tidak ada model baru, tidak ada migrasi database, tidak ada service baru. Semua kolom yang dibutuhkan (`AssessmentType`, `LinkedSessionId`, `HasManualGrading`, `IsPassed`) sudah ada di `AssessmentSession` sejak Phase 296 [VERIFIED: Models/AssessmentSession.cs baris 131-154]. Skor per elemen teknis tersimpan di `SessionElemenTeknisScore` dan sudah diisi oleh `GradingService.GradeAndCompleteAsync` [VERIFIED: Services/GradingService.cs baris 122-145].

Perubahan utama mencakup tiga area: (1) `CMPController.Assessment()` — query extend untuk include `AssessmentType` dan pre-post pair data, kemudian pass ke view; (2) `Views/CMP/Assessment.cshtml` — render card pair dengan linking visual, blocking logic, dan tab filtering; (3) `CMPController.Results()` + `Views/CMP/Results.cshtml` — extend untuk comparison section saat session adalah PostTest.

**Primary recommendation:** Mulai dari controller (Assessment + Results), kemudian view. Urutan ini memastikan data tersedia sebelum UI dikembangkan. Tidak ada library eksternal yang dibutuhkan.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | existing (project) | Controller action + ViewBag | Framework project |
| Bootstrap 5 | via CDN (existing) | Card, badge, table, btn | Sudah digunakan di seluruh project |
| Bootstrap Icons | via CDN (existing) | Arrow icon, bar-chart-line | Sudah digunakan di Assessment.cshtml |
| Entity Framework Core | existing | Query SessionElemenTeknisScore | ORM project |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Chart.js | via CDN (existing) | Radar chart di Results | Sudah ada di Results.cshtml — TIDAK perlu untuk comparison section |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ViewBag untuk comparison data | ViewModel baru | ViewModel lebih type-safe tapi butuh class baru; ViewBag lebih cepat dan cukup untuk section satu halaman |
| GroupBy di view | GroupBy di controller | Controller lebih testable, view lebih bersih — gunakan controller |

**Installation:** Tidak ada paket baru diperlukan. [VERIFIED: codebase scan]

---

## Architecture Patterns

### Recommended Project Structure

Tidak ada file baru yang mutlak diperlukan. Perubahan pada file existing:

```
Controllers/
└── CMPController.cs          ← extend Assessment() dan Results()

Views/CMP/
├── Assessment.cshtml         ← extend card loop + Riwayat Ujian tabel
└── Results.cshtml            ← tambah comparison section di atas detail soal
```

### Pattern 1: Assessment() — Query Extend untuk Pre-Post Pair

**Apa:** Query existing hanya mengambil sessions `Open | Upcoming | InProgress`. Perlu diperluas untuk:
1. Sertakan `AssessmentType` dan `LinkedSessionId` dalam result (sudah ada di model, tidak perlu `.Include()` tambahan)
2. Setelah query, kelompokkan sessions menjadi "pair" (Pre+Post dengan `LinkedGroupId` sama) vs "standalone"
3. Pass grouped data ke view via ViewBag

**Penting:** Query TIDAK mengubah filter `Open | Upcoming | InProgress`. Pre-Post pair mungkin memiliki Post-Test yang masih Upcoming — itu tetap ikut karena filter sudah include Upcoming. [VERIFIED: CMPController.cs baris 203]

**Pattern implementasi di controller:**
```csharp
// Source: CMPController.cs baris 197-265 (existing Assessment action)

// Setelah query exams (baris 225-229), kelompokkan:
var prePairs = exams
    .Where(e => e.AssessmentType == "PreTest" && e.LinkedGroupId.HasValue)
    .ToList();

var postPairs = exams
    .Where(e => e.AssessmentType == "PostTest" && e.LinkedGroupId.HasValue)
    .ToList();

// Build pair list: (PreSession, PostSession or null)
var pairedGroups = prePairs
    .Select(pre => new {
        Pre = pre,
        Post = postPairs.FirstOrDefault(p => p.LinkedGroupId == pre.LinkedGroupId)
    })
    .ToList();

// Standalone (tidak ada AssessmentType atau LinkedGroupId)
var standaloneExams = exams
    .Where(e => string.IsNullOrEmpty(e.AssessmentType) || !e.LinkedGroupId.HasValue)
    .ToList();

ViewBag.PairedGroups = pairedGroups;
ViewBag.StandaloneExams = standaloneExams;
```

**Catatan khusus edge case:** Post-Test yang belum Open (Upcoming) TIDAK ada dalam query exams karena filter sudah include Upcoming. Yang perlu dihandle: jika Pre sudah Completed (masuk Riwayat), Post-Test Upcoming masih muncul di exams sebagai standalone card tanpa pair — ini normal behavior. [ASSUMED: berdasarkan analisis filter query existing]

### Pattern 2: Blocking Detection di View

**Apa:** Card Post-Test harus tahu status Pre-Test untuk render blocking UI.

Detection logic (di Razor view):
```razor
@* Source: CONTEXT.md D-04, D-05, D-06 *@

@{
    var pre = pair.Pre;
    var post = pair.Post;  // nullable jika Post belum assigned ke worker ini

    bool preCompleted = pre.Status == "Completed";
    bool preExpired = (pre.Status == "Open" || pre.Status == "Upcoming")
        && pre.ExamWindowCloseDate.HasValue
        && pre.ExamWindowCloseDate.Value < DateTime.UtcNow.AddHours(7);
    bool postBlocked = !preCompleted;
    bool postBlockedByExpiry = preExpired && !preCompleted;
}
```

**Kasus-kasus yang harus dicakup:**
- Pre Open/Upcoming, Post belum bisa dimulai → Post card `opacity-50`, tombol disabled "Selesaikan Pre-Test terlebih dahulu"
- Pre expired (ExamWindowCloseDate lewat, belum Completed) → Post card menampilkan badge danger "Pre-Test tidak diselesaikan"
- Pre Completed, Post Upcoming → Post card normal (tidak grayed), tombol disabled "Opens [tanggal]"
- Pre Completed, Post Open → Post card normal, tombol "Mulai Post-Test" aktif

### Pattern 3: Tab Filtering untuk Pre-Post Pair (D-08 — Claude's Discretion)

**Keputusan:** Pair card mengikuti status Post-Test untuk tab placement.

Implementasi: wrapper `<div>` untuk pair group mendapat `data-status` dari status Post-Test. JS filter existing tidak diubah — cukup extend dengan wrapper group. [VERIFIED: filterCards() function di Assessment.cshtml baris 548-571]

```javascript
// JS filter existing sudah check `data-status` pada `.assessment-card`
// Untuk pair: wrapper div mendapat class `assessment-card` dan `data-status` dari status Post
// Kedua card (Pre + Post) berada di dalam satu wrapper — ikut hidden/visible bersama
```

**Reasoning:** Jika Post masih Upcoming, worker belum perlu melihat pair di tab Open. Jika Post sudah Open (berarti Pre juga Completed), worker perlu akses segera.

### Pattern 4: Results() — Extend untuk Comparison Data (D-12)

**Apa:** Setelah build viewModel existing, tambahkan comparison data ke ViewBag jika session adalah PostTest.

```csharp
// Source: CMPController.cs baris 1891-2076 (Results action)
// Ditempatkan sebelum `return View(viewModel);`

if (assessment.AssessmentType == "PostTest" && assessment.LinkedSessionId.HasValue)
{
    var preSessionId = assessment.LinkedSessionId.Value;

    // Cek apakah Essay pending (D-17)
    var preSession = await _context.AssessmentSessions
        .FirstOrDefaultAsync(s => s.Id == preSessionId);

    bool gainPending = assessment.HasManualGrading && assessment.IsPassed == null;

    // Query ET scores untuk kedua session
    var preEtScores = await _context.SessionElemenTeknisScores
        .Where(s => s.AssessmentSessionId == preSessionId)
        .ToListAsync();

    var postEtScores = await _context.SessionElemenTeknisScores
        .Where(s => s.AssessmentSessionId == assessment.Id)
        .ToListAsync();

    // Build comparison rows (join by ElemenTeknis name)
    var comparisonRows = postEtScores
        .Select(post => {
            var pre = preEtScores.FirstOrDefault(p => p.ElemenTeknis == post.ElemenTeknis);
            double preScore = pre != null && pre.QuestionCount > 0
                ? Math.Round((double)pre.CorrectCount / pre.QuestionCount * 100, 1) : 0;
            double postScore = post.QuestionCount > 0
                ? Math.Round((double)post.CorrectCount / post.QuestionCount * 100, 1) : 0;

            double? gainScore = null;
            if (!gainPending)
            {
                gainScore = preScore >= 100
                    ? 100
                    : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);
            }

            return new {
                ElemenTeknis = post.ElemenTeknis,
                PreScore = preScore,
                PostScore = postScore,
                GainScore = gainScore  // null = pending
            };
        })
        .OrderBy(r => r.ElemenTeknis)
        .ToList();

    ViewBag.ComparisonData = comparisonRows;
    ViewBag.GainScorePending = gainPending;
    ViewBag.HasComparisonSection = comparisonRows.Any();
}
```

### Pattern 5: Gain Score Formula

Formula: `(PostScore - PreScore) / (100 - PreScore) × 100`

Edge cases yang harus ditangani:
1. **PreScore = 100:** Gain = 100 (denominator nol, hindari DivisionByZero) [VERIFIED: CONTEXT.md D-16]
2. **Gain pending (Essay belum dinilai):** Tampilkan "—" dengan teks "Menunggu penilaian Essay" [VERIFIED: CONTEXT.md D-17]
3. **Gain negatif:** Formula bisa menghasilkan negatif — tampilkan merah dengan prefix "-" [VERIFIED: CONTEXT.md D-15]
4. **PreScore = 0:** (PostScore - 0) / (100 - 0) × 100 = PostScore — ini valid, tidak ada edge case

### Anti-Patterns to Avoid
- **Jangan filter Completed dari query exams:** Query existing sudah exclude Completed. Jika Post sudah Completed, ia tidak muncul di cards — masuk Riwayat. [VERIFIED: CMPController.cs baris 203]
- **Jangan buat halaman perbandingan terpisah:** D-11 locked — extend Results yang ada. [VERIFIED: CONTEXT.md]
- **Jangan query SessionElemenTeknisScore dari PackageQuestion secara real-time di Results:** ET scores sudah dihitung dan disimpan oleh GradingService saat submit — query dari tabel, jangan recompute. [VERIFIED: GradingService.cs baris 122-145]
- **Jangan hidden Post-Test card:** D-04 eksplisit — Post card harus visible tapi disabled, bukan hidden. [VERIFIED: CONTEXT.md]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Skor per elemen kompetensi | Recompute dari PackageQuestion | Query `SessionElemenTeknisScore` | GradingService sudah hitung dan persist saat submit — duplikasi logic |
| Gain score rendering | Custom JS | Razor inline dengan Bootstrap color classes | Data sudah tersedia di server-side, tidak perlu JS untuk ini |
| Tab filtering | Rewrite JS | Extend `data-status` attribute pada wrapper div | filterCards() existing sudah generic enough |
| Date formatting | Custom helper | `CultureInfo.GetCultureInfo("id-ID")` (sudah dipakai) | Pattern existing di Assessment.cshtml baris 158 |

---

## Common Pitfalls

### Pitfall 1: Post-Test Tanpa Pre ET Scores (LinkedSessionId ada tapi ET scores kosong)
**Apa yang salah:** Query `SessionElemenTeknisScores` untuk Pre session mengembalikan empty — menyebabkan semua PreScore = 0 dalam comparison table, dan gain score tampak artifisial tinggi.
**Kenapa terjadi:** Pre-Test mungkin dibuat sebelum Phase 296 (tidak ada ET tracking), atau paket soal tidak memiliki `ElemenTeknis` tag.
**Cara hindari:** Cek `preEtScores.Any()` sebelum build comparison. Jika kosong, set `ViewBag.HasComparisonSection = false` — jangan tampilkan tabel yang misleading.
**Warning signs:** Semua baris Skor Pre = 0% padahal assessment sudah dikerjakan.

### Pitfall 2: LinkedSessionId Menunjuk ke Session yang Sudah Dihapus
**Apa yang salah:** `assessment.LinkedSessionId.HasValue` = true tapi query `preSession` returns null.
**Kenapa terjadi:** ON DELETE SET NULL sudah ada di model, artinya jika Pre session dihapus, `LinkedSessionId` di Post menjadi null. Tapi jika ada bug dan tidak null, null-ref exception bisa terjadi.
**Cara hindari:** Selalu null-check `preSession` setelah query. Jika null, skip comparison section.

### Pitfall 3: Tab Filtering Broken untuk Pair Wrapper
**Apa yang salah:** Wrapper div pair mendapat `assessment-card` class dan `data-status`, tapi card children di dalamnya JUGA punya `assessment-card` class — JS `querySelectorAll('.assessment-card')` mendapat keduanya.
**Kenapa terjadi:** Jika implementor reuse class `assessment-card` pada inner cards dalam pair wrapper.
**Cara hindari:** Inner cards dalam pair wrapper TIDAK boleh punya class `assessment-card`. Hanya outer wrapper yang punya class `assessment-card` dan `data-status`.

### Pitfall 4: Gain Score DivisionByZero saat PreScore = 100
**Apa yang salah:** Formula `(Post - 100) / (100 - 100) * 100` → divide by zero → NaN atau exception.
**Kenapa terjadi:** Tidak ada guard untuk denominator = 0.
**Cara hindari:** `if (preScore >= 100) gainScore = 100; else { /* formula normal */ }` [VERIFIED: CONTEXT.md D-16]

### Pitfall 5: Post-Test Card Muncul di Riwayat Tanpa Linked Pre
**Apa yang salah:** Riwayat Ujian query mengambil semua Completed sessions. Post-Test row akan muncul tapi `LinkedSessionId` null — tidak bisa link ke comparison detail.
**Kenapa terjadi:** Post-Test Completed memang harus muncul di Riwayat (D-09 dan D-10).
**Cara hindari:** Tombol "Lihat Detail" harus selalu link ke Results (yang sudah handle null LinkedSessionId secara graceful) — bukan ke halaman comparison terpisah. Tidak perlu guard di Riwayat.

---

## Code Examples

### Existing Card HTML Pattern (Standalone Assessment)
```html
<!-- Source: Assessment.cshtml baris 138-291 -->
<div class="col-12 col-md-6 col-lg-4 assessment-card" data-status="open">
    <div class="card h-100 border-0 shadow-sm assessment-card-item">
        <div class="card-body d-flex flex-column p-4">
            <div class="mb-3">
                <span class="badge bg-primary">OJT</span>
            </div>
            <h5 class="card-title fw-bold mb-3">Judul Assessment</h5>
            <div class="assessment-meta mb-3">
                <!-- meta items -->
            </div>
            <div class="mt-auto">
                <button class="btn btn-primary w-100 btn-start-standard" data-id="123">
                    Start Assessment <i class="bi bi-arrow-right ms-1"></i>
                </button>
            </div>
        </div>
    </div>
</div>
```

### Target Pre-Post Pair Card HTML (Phase 299)
```html
<!-- Source: UI-SPEC.md + CONTEXT.md D-01, D-02, D-03, D-04 -->

<!-- Wrapper pair — satu entry untuk tab filtering -->
<div class="col-12 assessment-card" data-status="open">
    <div class="row g-3 align-items-stretch">

        <!-- Pre-Test Card -->
        <div class="col-12 col-md-5">
            <div class="card h-100 border-0 shadow-sm assessment-card-item border-start border-4 border-primary">
                <div class="card-body d-flex flex-column p-4">
                    <div class="mb-3">
                        <span class="badge bg-primary">OJT</span>
                        <span class="badge bg-info text-dark ms-1">Pre-Test</span>
                    </div>
                    <h5 class="card-title fw-bold mb-3">Nama Assessment</h5>
                    <!-- assessment-meta -->
                    <div class="mt-auto">
                        <button class="btn btn-primary w-100 btn-start-standard" data-id="@pre.Id">
                            Mulai Pre-Test <i class="bi bi-arrow-right ms-1"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Arrow Connector -->
        <div class="col-12 col-md-2 d-flex align-items-center justify-content-center">
            <i class="bi bi-arrow-right-circle-fill text-primary fs-4 d-none d-md-block" aria-hidden="true"></i>
            <i class="bi bi-arrow-down-circle-fill text-primary fs-4 d-md-none" aria-hidden="true"></i>
        </div>

        <!-- Post-Test Card (Pre belum Completed — D-04) -->
        <div class="col-12 col-md-5">
            <div class="card h-100 border-0 shadow-sm assessment-card-item border-start border-4 border-primary opacity-50">
                <div class="card-body d-flex flex-column p-4">
                    <div class="mb-3">
                        <span class="badge bg-primary">OJT</span>
                        <span class="badge bg-primary ms-1">Post-Test</span>
                    </div>
                    <h5 class="card-title fw-bold mb-3">Nama Assessment</h5>
                    <!-- assessment-meta -->
                    <div class="mt-auto">
                        <button class="btn btn-secondary w-100" disabled>
                            Selesaikan Pre-Test terlebih dahulu
                        </button>
                    </div>
                </div>
            </div>
        </div>

    </div>
</div>
```

### Gain Score Computation (Controller)
```csharp
// Source: CONTEXT.md D-15, D-16, D-17
double? ComputeGainScore(double preScore, double postScore, bool pending)
{
    if (pending) return null; // Essay belum dinilai
    if (preScore >= 100) return 100;
    return Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);
}
```

### Gain Score Rendering (Razor)
```razor
<!-- Source: UI-SPEC.md + CONTEXT.md D-15 -->
@{
    var gain = row.GainScore;
    string gainText, gainClass;
    if (gain == null)
    {
        gainText = "—";
        gainClass = "text-secondary";
    }
    else if (gain > 0)
    {
        gainText = $"+{gain:0.#}%";
        gainClass = "text-success";
    }
    else if (gain < 0)
    {
        gainText = $"{gain:0.#}%";
        gainClass = "text-danger";
    }
    else
    {
        gainText = "0%";
        gainClass = "text-secondary";
    }
}
<span class="fw-bold @gainClass">@gainText</span>
@if (gain == null)
{
    <small class="d-block text-muted">Menunggu penilaian Essay</small>
}
```

### Riwayat Ujian Badge (Razor)
```razor
<!-- Source: CONTEXT.md D-09, D-10 -->
<td class="fw-semibold">
    @if (!string.IsNullOrEmpty((string?)item.AssessmentType))
    {
        if (item.AssessmentType == "PreTest")
        {
            <span class="badge bg-info text-dark me-1">Pre-Test</span>
        }
        else if (item.AssessmentType == "PostTest")
        {
            <span class="badge bg-primary me-1">Post-Test</span>
        }
    }
    @item.Title
</td>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| GradeFromSavedAnswers() di controller | GradingService.GradeAndCompleteAsync() | Phase 296 | ET scores sekarang tersimpan di SessionElemenTeknisScores — tersedia untuk comparison |
| Assessment standalone only | AssessmentType + LinkedGroupId/LinkedSessionId di model | Phase 296 | Foundation untuk Pre-Post linking sudah ada |

**Deprecated/outdated:**
- Query langsung `PackageUserResponses` untuk hitung skor di Results: tidak perlu — skor tersimpan di `AssessmentSession.Score` dan `SessionElemenTeknisScore`

---

## Open Questions

1. **Post-Test Upcoming muncul sementara Pre Completed (tapi Post belum dalam query exams)**
   - Yang diketahui: filter query exams include `Upcoming` — Post-Test Upcoming sudah masuk query
   - Yang tidak jelas: apakah Post-Test bisa dalam status berbeda dari Pre-Test secara bersamaan di query result? Misal Pre = Open (baru saja assigned), Post = Upcoming (jadwal lebih lambat)?
   - Rekomendasi: Implementor harus test case: Pre Open + Post Upcoming dalam satu query result → pair harus render dengan benar

2. **Pre-Test Completed tapi tidak ada dalam query exams (filter exclude Completed)**
   - Yang diketahui: query hanya `Open | Upcoming | InProgress` — Pre yang Completed tidak ikut
   - Yang tidak jelas: bagaimana pair card Post-Test muncul jika Pre sudah Completed dan tidak ada dalam `exams` list?
   - Rekomendasi: Query terpisah untuk Pre sessions yang Completed dan LinkedGroupId-nya matching Post sessions yang ada. Atau: include `Completed` Pre sessions dalam ViewBag terpisah khusus untuk pair detection.

3. **`AssessmentType` null di sessions yang dibuat sebelum Phase 296**
   - Yang diketahui: kolom `AssessmentType` nullable — backward compatible [VERIFIED: AssessmentSession.cs baris 131]
   - Yang tidak jelas: apakah ada sessions lama dengan AssessmentType null yang secara tidak sengaja punya `LinkedGroupId` terisi?
   - Rekomendasi: Guard dengan `!string.IsNullOrEmpty(e.AssessmentType)` saat grouping pair

---

## Environment Availability

Step 2.6: SKIPPED (phase ini pure code/view modification, tidak ada external dependencies baru)

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada unit test framework terdeteksi di project) |
| Config file | none |
| Quick run command | `dotnet run` + navigasi ke /CMP/Assessment |
| Full suite command | Manual UAT checklist |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command / Steps |
|--------|----------|-----------|-----------------|
| WKPPT-01 | Pre dan Post muncul sebagai 2 card terhubung dengan badge dan arrow | Manual browser | Login sebagai worker yang punya Pre-Post session → lihat /CMP/Assessment |
| WKPPT-02 | Post-Test blocked sebelum Pre-Test Completed | Manual browser | Pre = Open, Post = Upcoming → cek card Post grayed out + tombol disabled |
| WKPPT-03 | Post-Test bisa dimulai setelah Pre Completed + jadwal tiba | Manual browser | Set Pre = Completed, Post = Open → cek tombol "Mulai Post-Test" aktif |
| WKPPT-04 | Halaman perbandingan muncul di Results Post-Test | Manual browser | Complete Post-Test → buka Results → cek section "Perbandingan Pre-Post Test" |
| WKPPT-05 | Gain score formula benar | Manual calculation | Compare nilai di UI dengan formula manual (Post-Pre)/(100-Pre)*100 |
| WKPPT-06 | PreScore = 100 → Gain = 100 | Manual browser | Setup Pre dengan 100% score → lihat gain score |
| WKPPT-07 | Gain score per elemen kompetensi | Manual browser | Cek tabel comparison memiliki baris per ET element + gain per baris |

### Wave 0 Gaps
Tidak ada test file infrastructure yang perlu dibuat. Project menggunakan manual UAT.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | N/A — existing auth middleware covers this |
| V4 Access Control | yes | Results action sudah memiliki ownership check (`assessment.UserId == user.Id || Admin || HC`) [VERIFIED: CMPController.cs baris 1903-1906] |
| V5 Input Validation | yes | `id` parameter di Results — sudah ada null check + NotFound guard |
| V6 Cryptography | no | N/A |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — worker akses Results milik worker lain | Spoofing | Ownership check sudah ada di Results() — tidak perlu perubahan |
| Manipulasi LinkedSessionId untuk akses comparison data worker lain | Tampering | Query `preSession` harus divalidasi: `preSession.UserId == assessment.UserId` — pastikan ini dicek |

**Catatan keamanan penting:** Saat Results() query Pre session via LinkedSessionId, WAJIB validasi bahwa Pre session milik user yang sama dengan Post session. Tanpa validasi ini, worker bisa melihat comparison data milik orang lain jika LinkedSessionId dimanipulasi. [ASSUMED: validasi ini belum ada karena Results() belum pernah handle cross-session query sebelumnya]

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` — dibaca langsung: Assessment() baris 185-266, Results() baris 1891-2076
- `Views/CMP/Assessment.cshtml` — dibaca langsung: card loop, tab filtering JS, Riwayat Ujian table
- `Views/CMP/Results.cshtml` — dibaca langsung: ElemenTeknis section
- `Models/AssessmentSession.cs` — dibaca langsung: semua field Pre-Post (AssessmentType, LinkedSessionId, HasManualGrading)
- `Models/SessionElemenTeknisScore.cs` — dibaca langsung: struktur tabel ET scores
- `Services/GradingService.cs` — dibaca langsung: ET score computation dan persistence
- `.planning/phases/299-worker-pre-post-test-comparison/299-CONTEXT.md` — locked decisions D-01 s/d D-17
- `.planning/phases/299-worker-pre-post-test-comparison/299-UI-SPEC.md` — component HTML patterns

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` — WKPPT-01 s/d WKPPT-07 requirement definitions

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Post-Test Upcoming masih ikut query exams karena filter include Upcoming | Architecture Pattern 1 | Jika tidak, pair dengan Pre Open + Post Upcoming tidak akan render — harus tambah separate query |
| A2 | Sessions lama (pre-Phase 296) tidak akan punya AssessmentType terisi + LinkedGroupId terisi secara tidak sengaja | Open Questions | Jika ada, pair detection bisa salah kelompokkan sessions lama |
| A3 | Validasi `preSession.UserId == assessment.UserId` belum ada di Results() untuk cross-session query | Security Domain | Security vulnerability jika tidak ditambahkan |

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library existing, tidak ada yang baru
- Architecture: HIGH — pola controller/view sudah sangat jelas dari kode existing
- Gain score formula: HIGH — verified dari CONTEXT.md dan sederhana secara matematis
- ET score retrieval: HIGH — tabel dan data tersedia, pattern query jelas
- Pair detection query: MEDIUM — edge case Pre-Completed-not-in-exams memerlukan keputusan implementasi

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stable stack, tidak ada moving parts eksternal)
