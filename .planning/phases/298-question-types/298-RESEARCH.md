# Phase 298: Question Types - Research

**Researched:** 2026-04-07
**Domain:** ASP.NET Core MVC — Exam Question Types (Multiple Answer + Essay), ClosedXML Excel Import, SignalR Auto-Save, Manual Essay Grading
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

#### HC Question Management
- **D-01:** Dropdown QuestionType di form create/edit soal. Form berubah dinamis: MC/MA tampilkan 4 opsi (A-D), Essay sembunyikan opsi dan tampilkan textarea rubrik
- **D-02:** MA: HC menandai opsi benar via checkbox per opsi (bukan radio). Minimal 2 opsi harus dicentang untuk MA
- **D-03:** Essay: textarea rubrik/kunci jawaban sebagai referensi HC saat grading. Disimpan tapi tidak digunakan untuk auto-grading
- **D-04:** Essay tidak punya opsi A-D. Form sembunyikan bagian opsi saat tipe Essay dipilih
- **D-05:** MA tetap 4 opsi (A-D) seperti MC. Bedanya HC bisa centang >1 opsi sebagai benar
- **D-06:** Edit tipe soal boleh (MC↔MA↔Essay) dengan warning: jawaban peserta yang sudah ada bisa tidak valid
- **D-07:** Validasi: MC tepat 1 IsCorrect, MA minimal 2 IsCorrect, Essay harus ada rubrik dan tidak boleh punya opsi. Error jika dilanggar (block save)
- **D-08:** ScoreValue: MC/MA tetap default 10 (fixed). Essay HC bisa set bobot per soal (default 10, bisa diubah)
- **D-09:** Preview soal sederhana — modal di halaman manage questions yang menampilkan soal seperti tampilan pekerja

#### Excel Import
- **D-10:** Tambah kolom QuestionType dan Rubrik di template Excel. Format: Question | Opt A | Opt B | Opt C | Opt D | Correct | Elemen Teknis | QuestionType | Rubrik
- **D-11:** MA Correct bisa isi multi huruf: 'A,B' atau 'A,C,D'. Essay Correct dikosongkan
- **D-12:** Backward compatible: file lama tanpa kolom QuestionType default ke MC
- **D-13:** 4 tombol download template: MC, MA, Essay, Universal (campur semua tipe)
- **D-14:** Template diupdate dengan contoh baris per tipe dan instruksi yang diperbarui

#### Essay Grading UI
- **D-15:** HC menilai Essay inline di AssessmentMonitoringDetail. Soal Essay tampil dengan jawaban pekerja + rubrik + input skor (0 s/d ScoreValue)
- **D-16:** Skor parsial: HC bisa input angka bebas 0 s/d ScoreValue per soal Essay (bukan hanya 0 atau penuh)
- **D-17:** Setelah semua Essay dinilai: auto recalculate skor total (MC+MA auto + Essay manual), update IsPassed, status berubah dari "Menunggu Penilaian" → "Completed"
- **D-18:** Sertifikat + TrainingRecord HANYA digenerate setelah status "Completed" (setelah semua Essay dinilai). Bukan saat submit

#### Worker Exam UI
- **D-19:** MA di StartExam: checkbox list (layout sama dengan MC, ganti radio → checkbox). Label "Pilih semua yang benar" di atas opsi
- **D-20:** Essay di StartExam: textarea sederhana (plain text, bukan rich editor). Placeholder "Tulis jawaban Anda...". Counter karakter
- **D-21:** Auto-save: MA auto-save setiap checkbox berubah. Essay auto-save debounce 2 detik setelah berhenti mengetik
- **D-22:** Batas karakter Essay: default 2000 karakter. HC bisa set batas per soal saat create
- **D-23:** Badge tipe soal di setiap card soal: "Pilihan Ganda" / "Multi Jawaban" / "Essay" di samping nomor soal
- **D-24:** Panel navigasi soal (sidebar): TIDAK perlu badge tipe — hanya nomor + status terjawab/belum seperti sekarang

#### Status & Monitoring
- **D-25:** Status "Menunggu Penilaian" ditampilkan sebagai badge kuning/orange di AssessmentMonitoring + counter Essay belum dinilai (e.g., "2 Essay belum dinilai")
- **D-26:** Tidak ada notifikasi khusus ke HC — HC lihat dari monitoring page dengan filter status

#### Mixed Assessment
- **D-27:** Soal campur MC+MA+Essay tampil sesuai urutan import/create (tidak dikelompokkan per tipe). Shuffle tetap berlaku jika enabled

#### ExamSummary
- **D-28:** Halaman review sebelum submit: ringkas per tipe — MC: "Jawaban: A", MA: "Jawaban: A, C", Essay: "Jawaban: (50 karakter pertama...)". Belum dijawab ditandai merah

### Claude's Discretion
- Auto-save implementation detail (SignalR vs AJAX)
- Exact debounce timing untuk Essay auto-save
- CSS styling untuk badge tipe soal dan status "Menunggu Penilaian"
- Preview modal layout dan styling
- Error handling saat grading Essay (partial save, validasi skor range)

### Deferred Ideas (OUT OF SCOPE)
- Delegasi Essay ke Atasan — butuh role/permission baru, phase terpisah
- Notifikasi in-app untuk Essay pending — butuh extend notification system
- Rich text editor untuk Essay — TinyMCE/Quill lebih kompleks
- Badge tipe soal di panel navigasi sidebar
- QTYPE-01 (True/False): DROP — HC buat sebagai MC 2 opsi
- QTYPE-04 (Fill in the Blank): DROP
- QTYPE-12 (FillBlank grading): DROP
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Research Support |
|----|-----------|------------------|
| QTYPE-01 | HC dapat membuat soal True/False (2 opsi radio) | DROP per CONTEXT.md — dibuat sebagai MC 2 opsi, tidak perlu implementasi khusus |
| QTYPE-02 | HC dapat membuat soal Multiple Answer (checkbox multi-pilih) | D-01 s/d D-08: form create/edit dengan QuestionType dropdown + checkbox MA |
| QTYPE-03 | HC dapat membuat soal Essay (rich text editor) | D-01 s/d D-08: form Essay dengan textarea rubrik (plain text, bukan rich editor per D-20) |
| QTYPE-04 | HC dapat membuat soal Fill in the Blank (text input) | DROP per CONTEXT.md |
| QTYPE-05 | Template Excel impor soal memiliki kolom QuestionType | D-10: tambah kolom QuestionType + Rubrik di template |
| QTYPE-06 | Upload bulk berhasil dengan tipe soal beragam dalam satu file | D-11 + D-12: parser MA multi-correct + backward compat |
| QTYPE-07 | StartExam menampilkan UI yang sesuai per tipe soal | D-19 s/d D-23: checkbox MA, textarea Essay, badge tipe |
| QTYPE-08 | Multiple Answer scoring all-or-nothing | GradingService case "MultipleAnswer" — semua IsCorrect harus dipilih dan tidak boleh salah pilih |
| QTYPE-09 | Essay tidak ter-grading otomatis — status "Menunggu Penilaian" | GradeAndCompleteAsync: detect HasManualGrading, set status "Menunggu Penilaian" bukan "Completed" |
| QTYPE-10 | HC dapat input skor per soal Essay dari AssessmentMonitoringDetail | D-15 s/d D-16: inline grading UI + AJAX endpoint SubmitEssayScore |
| QTYPE-11 | Sistem menghitung ulang skor total setelah semua Essay dinilai | D-17: FinalizeEssayGrading endpoint → recalculate + update IsPassed + generate sertifikat |
| QTYPE-12 | Fill in the Blank auto-grade exact match case-insensitive | DROP per CONTEXT.md |
| QTYPE-13 | IsPassed tetap null sampai semua Essay dinilai | GradeAndCompleteAsync: jika HasManualGrading, set IsPassed = null, Status = "Menunggu Penilaian" |
</phase_requirements>

---

## Summary

Phase 298 mengimplementasikan dua tipe soal baru (MultipleAnswer dan Essay) di atas fondasi yang sudah dibangun Phase 296. Model data sudah siap (`QuestionType`, `TextAnswer`, `HasManualGrading`), dan GradingService sudah punya switch-case placeholder untuk kedua tipe. Yang perlu dibangun: (1) form create/edit soal dengan QuestionType dropdown, (2) parser Excel yang diperluas, (3) worker exam UI yang menampilkan checkbox/textarea sesuai tipe, dan (4) essay grading UI inline di AssessmentMonitoringDetail.

Tantangan terbesar adalah alur Essay: grading tidak boleh terjadi saat submit — session harus masuk status "Menunggu Penilaian" dengan IsPassed null, lalu HC nilai manual, lalu sistem recalculate dan generate sertifikat. Ini memerlukan perubahan di GradeAndCompleteAsync (deteksi HasManualGrading), dua endpoint AJAX baru (SubmitEssayScore dan FinalizeEssayGrading), dan modifikasi AssessmentMonitoringDetail untuk menampilkan soal essay per session.

QTYPE-01 (True/False) dan QTYPE-04 (Fill in the Blank) di-drop per keputusan user di CONTEXT.md — tidak perlu diimplementasikan.

**Primary recommendation:** Implementasi secara berurutan: (1) model/form soal, (2) Excel import, (3) GradingService MA + Essay, (4) StartExam UI, (5) Essay grading admin. Urutan ini memastikan fondasi data tersedia sebelum UI dibangun.

---

## Standard Stack

### Core (Sudah Ada di Proyek)
| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| ASP.NET Core MVC | .NET 8 | Web framework + controllers + views | Existing |
| Entity Framework Core | 8.x | ORM + migration | Existing |
| Bootstrap | 5.3.0 via CDN | UI framework, form controls, badges | Existing `[VERIFIED: _Layout.cshtml]` |
| Bootstrap Icons | 1.10.0 via CDN | Icon set | Existing `[VERIFIED: _Layout.cshtml]` |
| ClosedXML | Current | Excel read/write untuk import/download template | Existing `[VERIFIED: AssessmentAdminController.cs line 11]` |
| SignalR | Built-in ASP.NET Core | Auto-save jawaban soal via hub | Existing `[VERIFIED: AssessmentAdminController.cs + StartExam.cshtml]` |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Font Awesome | 6.5.1 via CDN | Icon tambahan | Sudah ada, gunakan sesuai konteks |
| jQuery | (via Bootstrap) | DOM manipulation, AJAX calls | Sudah ada, gunakan untuk AJAX grading |

Tidak ada library baru yang perlu ditambahkan. Semua komponen dibangun di atas stack yang sudah ada. `[VERIFIED: 298-UI-SPEC.md]`

---

## Architecture Patterns

### Recommended Project Structure (Perubahan Phase 298)

```
Controllers/
└── AssessmentAdminController.cs    # Tambah: SubmitEssayScore, FinalizeEssayGrading, DownloadQuestionTemplate (update 4 varian)

Services/
└── GradingService.cs               # Update: case "MultipleAnswer" + case "Essay" + HasManualGrading flow

Models/
├── AssessmentPackage.cs            # Update PackageQuestion: tambah Rubrik field + MaxCharacters field
├── PackageUserResponse.cs          # Update: tambah EssayScore field (int?, untuk manual grading)
└── AssessmentMonitoringViewModel.cs # Update MonitoringSessionViewModel: tambah HasManualGrading + EssayPendingCount

Views/
├── Admin/
│   ├── AssessmentMonitoringDetail.cshtml   # Update: tambah essay grading section per session
│   └── ImportPackageQuestions.cshtml        # Update: info template baru
└── CMP/
    ├── StartExam.cshtml            # Update: render per QuestionType (checkbox MA, textarea Essay, badge tipe)
    └── ExamSummary.cshtml          # Update: ringkasan per tipe (MA multi-letter, Essay truncated)

Migrations/
└── AddRubrikAndEssayScoreFields.cs # Migration: PackageQuestion.Rubrik, PackageQuestion.MaxCharacters, PackageUserResponse.EssayScore
```

### Pattern 1: QuestionType-Aware Form Rendering (HC Create/Edit Soal)

**What:** Form soal berubah dinamis via JavaScript saat dropdown QuestionType berubah.
**When to use:** Saat HC membuat atau mengedit soal.

```javascript
// Source: [ASSUMED] — pola standard, konsisten dengan Bootstrap 5
document.getElementById('QuestionType').addEventListener('change', function() {
    const qtype = this.value;
    document.getElementById('optionsSection').style.display =
        (qtype === 'Essay') ? 'none' : 'block';
    document.getElementById('rubrikSection').style.display =
        (qtype === 'Essay') ? 'block' : 'none';
    // Ganti radio → checkbox untuk MA
    document.querySelectorAll('.correct-input').forEach(inp => {
        inp.type = (qtype === 'MultipleAnswer') ? 'checkbox' : 'radio';
    });
});
```

### Pattern 2: MA All-or-Nothing Grading di GradingService

**What:** Untuk soal MA, pekerja harus memilih SEMUA opsi yang IsCorrect=true dan TIDAK boleh memilih opsi yang IsCorrect=false. Jika kondisi terpenuhi sempurna, dapat ScoreValue penuh. Jika tidak, skor 0.

**Storage MA responses:** Jawaban MA disimpan sebagai beberapa row PackageUserResponse dengan PackageOptionId berbeda per opsi yang dipilih (bukan satu row). Saat grading, query semua response untuk question_id tersebut.

```csharp
// Source: [ASSUMED] — berdasarkan model PackageUserResponse yang ada
case "MultipleAnswer":
    var selectedOptionIds = allResponses
        .Where(r => r.PackageQuestionId == q.Id)
        .Select(r => r.PackageOptionId)
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .ToHashSet();
    var correctOptionIds = q.Options
        .Where(o => o.IsCorrect)
        .Select(o => o.Id)
        .ToHashSet();
    var incorrectOptionIds = q.Options
        .Where(o => !o.IsCorrect)
        .Select(o => o.Id)
        .ToHashSet();
    // All-or-nothing: semua correct dipilih, tidak ada incorrect dipilih
    bool maCorrect = correctOptionIds.SetEquals(selectedOptionIds) &&
                     !selectedOptionIds.Overlaps(incorrectOptionIds);
    if (maCorrect) totalScore += q.ScoreValue;
    break;
```

**CATATAN KRITIS:** GradingService saat ini mengambil responses sebagai `Dictionary<int, int?>` (satu response per question). Untuk MA, perlu mengambil semua response rows, bukan dictionary. Lihat bagian Pitfall #1.

### Pattern 3: Essay — "Menunggu Penilaian" Flow

**What:** Saat submit ujian dengan Essay, GradeAndCompleteAsync mendeteksi HasManualGrading dan TIDAK langsung set Completed + tidak generate sertifikat. Status menjadi "Menunggu Penilaian".

```csharp
// Source: [ASSUMED] — berdasarkan HasManualGrading field yang sudah ada di AssessmentSession
// Di awal GradeAndCompleteAsync, cek apakah ada soal Essay
bool hasEssay = packageQuestions.Any(q => q.QuestionType == "Essay");

if (hasEssay) {
    // Grade hanya MC + MA, Essay skip (skor 0 untuk sementara)
    // Set status "Menunggu Penilaian", IsPassed = null
    await _context.AssessmentSessions
        .Where(s => s.Id == session.Id && s.Status != "Completed")
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.Status, "Menunggu Penilaian")
            .SetProperty(r => r.HasManualGrading, true)
            .SetProperty(r => r.IsPassed, (bool?)null)
            // Score interim disimpan (MC+MA saja)
            .SetProperty(r => r.Score, interimPercentage)
        );
    // TIDAK generate TrainingRecord/Sertifikat di sini
    return true;
}
// Jika tidak ada Essay, flow normal (langsung Completed)
```

### Pattern 4: MA Auto-Save via SignalR

**What:** Setiap checkbox berubah, langsung kirim ke SignalR hub. Berbeda dari Essay yang pakai debounce.

```javascript
// Source: [ASSUMED] — berdasarkan pola auto-save existing di StartExam.cshtml
document.querySelectorAll('.exam-checkbox').forEach(cb => {
    cb.addEventListener('change', function() {
        const qId = this.dataset.questionId;
        const optId = this.value;
        const checked = this.checked;
        // Kumpulkan semua checked options untuk question ini
        const selectedOpts = [...document.querySelectorAll(
            `input[data-question-id="${qId}"]:checked`
        )].map(el => el.value);
        connection.invoke("SaveAnswer", sessionId, qId, selectedOpts.join(','));
    });
});
```

### Pattern 5: Essay Grading AJAX (HC AssessmentMonitoringDetail)

**What:** HC input skor per soal Essay, klik "Simpan Skor" → AJAX POST → response JSON → update badge status soal. Setelah semua dinilai, muncul tombol "Selesaikan Penilaian".

```javascript
// Source: [ASSUMED] — jQuery AJAX pattern standard
$('.btn-save-essay-score').on('click', function() {
    const sessionId = $(this).data('session-id');
    const questionId = $(this).data('question-id');
    const score = $(this).closest('.essay-grading-card').find('.essay-score-input').val();
    $.post('/Admin/SubmitEssayScore', { sessionId, questionId, score, __RequestVerificationToken: token })
        .done(function(res) {
            if (res.success) {
                // Update badge: Belum Dinilai → Sudah Dinilai
                // Cek apakah semua essay sudah dinilai → tampilkan tombol Selesaikan
            }
        });
});
```

### Anti-Patterns to Avoid

- **Menyimpan jawaban MA sebagai satu string comma-separated di PackageOptionId:** Field `PackageOptionId` adalah `int?`, tidak bisa menampung multi-value. Jawaban MA harus disimpan sebagai multiple rows PackageUserResponse.
- **Memanggil GradeAndCompleteAsync secara penuh untuk Essay:** Jika ada Essay, flow harus bercabang — jangan set "Completed" dan jangan generate sertifikat sebelum semua Essay dinilai manual.
- **Grading dari form POST bukan dari DB:** Pola yang sudah ditetapkan Phase 296 — selalu grade dari data yang tersimpan di DB, bukan dari payload POST.
- **Cross-package count validation untuk Essay:** Validasi saat ini memeriksa jumlah soal antar paket harus sama. Perlu update agar Essay row (tanpa opsi) tetap dihitung valid.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel read/write | Custom parser | ClosedXML (sudah ada) | Sudah digunakan di DownloadQuestionTemplate + ImportPackageQuestions |
| Real-time auto-save | Custom WebSocket | SignalR hub (sudah ada) | AssessmentHub sudah terdaftar, StartExam.cshtml sudah punya connection logic |
| Form validation UI | Custom JS error | Bootstrap `is-invalid` + `invalid-feedback` (sudah ada) | Pattern konsisten di seluruh codebase |
| Badge styling | Custom CSS | Bootstrap badge classes (`bg-warning`, `bg-success`, dll.) | Per UI-SPEC yang sudah disetujui |
| Race condition guard | Custom lock | `ExecuteUpdateAsync` dengan WHERE status guard (sudah ada di GradingService) | Pattern Phase 296 sudah terbukti |

---

## Critical Model Changes Needed

### 1. PackageQuestion — Tambah Rubrik dan MaxCharacters

Model `PackageQuestion` perlu dua field baru:
- `Rubrik` (string?, nullable) — kunci jawaban/rubrik untuk Essay. Tidak digunakan auto-grading.
- `MaxCharacters` (int, default 2000) — batas karakter jawaban Essay per soal (D-22).

```csharp
// Source: [VERIFIED: Models/AssessmentPackage.cs + CONTEXT.md D-22]
public string? Rubrik { get; set; }
public int MaxCharacters { get; set; } = 2000;
```

### 2. PackageUserResponse — Tambah EssayScore

Field untuk menyimpan skor manual HC per soal Essay setelah grading:
- `EssayScore` (int?, nullable) — null = belum dinilai, ada nilai = sudah dinilai oleh HC.

```csharp
// Source: [ASSUMED] — kebutuhan dari D-16 (skor parsial per soal essay)
public int? EssayScore { get; set; }
```

### 3. GradingService — Perubahan responses Query

GradingService saat ini mengambil responses sebagai Dictionary (satu per question). Untuk MA, perlu mengambil **semua response rows per question** karena satu question MA bisa punya banyak rows:

```csharp
// Source: [VERIFIED: GradingService.cs line 71-73] — current code
var responses = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == session.Id)
    .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId);

// PERLU DIGANTI dengan:
var allResponses = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == session.Id)
    .ToListAsync();
// Dari allResponses, buat lookup:
// MC/MA: responses grouped by QuestionId
// Essay: filtered by QuestionType == "Essay" untuk TextAnswer
```

---

## Common Pitfalls

### Pitfall 1: GradingService Responses Dictionary Hanya Menyimpan Satu Value per Question
**What goes wrong:** `ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId)` akan throw exception atau silently drop data jika ada multiple rows untuk question yang sama (MA). Dictionary tidak bisa punya duplicate key.
**Why it happens:** Dictionary `<int, int?>` tidak dirancang untuk multi-value. MA menghasilkan N rows per question (satu per opsi yang dipilih).
**How to avoid:** Ubah query responses menjadi `ToListAsync()`, lalu buat lookup yang tepat: `ILookup<int, int?>` untuk MC/MA. Cek apakah ada duplicate key sebelum mengubah query.
**Warning signs:** `InvalidOperationException: An item with the same key has already been added` saat ada soal MA.

### Pitfall 2: Cross-Package Count Validation Blokir Import Essay
**What goes wrong:** Validasi saat ini mensyaratkan jumlah soal antar paket sama, dan memeriksa row valid dengan kondisi "semua opsi tidak kosong + Correct = A/B/C/D". Essay row tidak punya opsi dan Correct kosong — akan dianggap invalid dan di-skip.
**Why it happens:** `validRowCount` dihitung dengan kondisi MC-only. `[VERIFIED: AssessmentAdminController.cs line 3363-3380]`
**How to avoid:** Update `validRowCount` calculation agar Essay row (QuestionType=Essay) dihitung sebagai valid meski tidak punya opsi.

### Pitfall 3: Status "Menunggu Penilaian" Tidak Dikenali di Existing Status Logic
**What goes wrong:** Banyak switch-case dan if-else di codebase yang menganggap status valid hanya: "Open", "Upcoming", "Completed", "Cancelled", "Abandoned". Status baru "Menunggu Penilaian" bisa menyebabkan UI/logic menampilkan badge yang salah atau default.
**Why it happens:** Status adalah string field tanpa enum constraint.
**How to avoid:** Audit semua tempat yang switch/check `session.Status`. Prioritas: AssessmentMonitoringDetail view, AssessmentMonitoring list, GradeAndCompleteAsync guard (`s.Status != "Completed"`).
**Warning signs:** Worker melihat assessment seolah "Open" padahal sudah submit, atau HC tidak bisa membedakan session yang pending grading.

### Pitfall 4: Sertifikat Ter-generate Sebelum Essay Selesai Dinilai
**What goes wrong:** Jika GradeAndCompleteAsync tidak bercabang dengan benar untuk Essay, flow normal akan generate sertifikat meski Essay belum dinilai. Pekerja menerima sertifikat dengan IsPassed yang salah.
**Why it happens:** Logika sertifikat ada di bagian bawah GradeAndCompleteAsync — harus di-guard dengan `!hasEssay` atau status guard.
**How to avoid:** Pastikan branch "Menunggu Penilaian" return sebelum mencapai kode TrainingRecord dan CertNumberHelper. Sertifikat hanya digenerate dari FinalizeEssayGrading endpoint. `[VERIFIED: GradingService.cs line 186-235]`

### Pitfall 5: Essay Auto-Save Memerlukan Endpoint atau Hub Method Baru
**What goes wrong:** Auto-save MC/MA menggunakan SignalR hub dengan method `SaveAnswer(sessionId, questionId, optionId)`. Essay menyimpan teks — signature berbeda (TextAnswer bukan optionId).
**Why it happens:** Hub method existing hanya handle `PackageOptionId` (int), tidak bisa menerima text string panjang.
**How to avoid:** Tambah SignalR hub method baru `SaveTextAnswer(sessionId, questionId, textAnswer)` atau tambah optional parameter. Simpan ke `PackageUserResponse.TextAnswer`.

### Pitfall 6: MA Checkbox State Restoration saat Resume
**What goes wrong:** Saat pekerja resume ujian (refresh/reconnect), radio button MC di-restore dari `ans_N` hidden input. MA menggunakan multiple checkboxes — restore logic berbeda (harus parse comma-separated option IDs dan check masing-masing checkbox).
**Why it happens:** Restore logic di StartExam.cshtml dirancang untuk single-value radio.
**How to avoid:** Update JavaScript restore logic untuk MA: parse comma-separated string dari `ans_N`, iterate dan set `checked = true` untuk setiap matching checkbox.

---

## Code Examples

### Excel Import — Parse QuestionType dan Multi-Correct

```csharp
// Source: [ASSUMED] — berdasarkan existing parser di AssessmentAdminController.cs line 3286-3294
// Column mapping yang diupdate:
// Col 1: Question, Col 2: Opt A, Col 3: Opt B, Col 4: Opt C, Col 5: Opt D
// Col 6: Correct, Col 7: Elemen Teknis, Col 8: QuestionType, Col 9: Rubrik

var questionType = row.Cell(8).GetString()?.Trim() ?? "";
if (string.IsNullOrWhiteSpace(questionType)) questionType = "MultipleChoice"; // backward compat D-12
var rubrik = row.Cell(9).GetString()?.Trim();

// Parse multi-correct untuk MA: "A,B" → [0, 1] indices
var correctStr = row.Cell(6).GetString()?.Trim().ToUpper() ?? "";
var correctLetters = correctStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim())
    .Where(s => new[] { "A", "B", "C", "D" }.Contains(s))
    .ToList();
```

### StartExam.cshtml — Render per QuestionType

```html
<!-- Source: [ASSUMED] — berdasarkan existing radio pattern di StartExam.cshtml line 113-126 -->
@{
    var qtype = q.QuestionType ?? "MultipleChoice";
}
<p class="fw-bold mb-3">
    <span class="badge bg-primary me-2">@q.DisplayNumber</span>
    <!-- Badge tipe soal (D-23) -->
    @if (qtype == "MultipleAnswer") {
        <span class="badge bg-secondary ms-1 small">Multi Jawaban</span>
    } else if (qtype == "Essay") {
        <span class="badge bg-secondary ms-1 small">Essay</span>
    }
    @q.QuestionText
</p>

@if (qtype == "Essay") {
    <!-- Essay: textarea + counter karakter -->
    <textarea class="form-control exam-essay"
              id="essay_@q.QuestionId"
              data-question-id="@q.QuestionId"
              placeholder="Tulis jawaban Anda..."
              maxlength="@(q.MaxCharacters)"
              style="min-height: 120px;"></textarea>
    <div class="text-end text-muted small mt-1">
        <span id="charCount_@q.QuestionId">0</span>/@q.MaxCharacters
    </div>
} else {
    <!-- MC/MA: opsi list -->
    @if (qtype == "MultipleAnswer") {
        <p class="text-muted small mb-2">Pilih semua yang benar</p>
    }
    <div class="list-group">
        @foreach (var opt in qOptions) {
            <label class="list-group-item list-group-item-action d-flex align-items-center gap-3"
                   style="cursor: pointer;">
                <input class="form-check-input flex-shrink-0 @(qtype == "MultipleAnswer" ? "exam-checkbox" : "exam-radio")"
                       type="@(qtype == "MultipleAnswer" ? "checkbox" : "radio")"
                       name="@(qtype == "MultipleAnswer" ? $"check_{q.QuestionId}" : $"radio_{q.QuestionId}")"
                       value="@opt.OptionId"
                       data-question-id="@q.QuestionId"
                       style="transform: scale(1.2);">
                <!-- ... -->
            </label>
        }
    </div>
}
```

### ExamSummary — Ringkasan per Tipe

```csharp
// Source: [ASSUMED] — update ExamSummaryItem untuk support MA dan Essay
public class ExamSummaryItem
{
    public int DisplayNumber { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public string? QuestionType { get; set; }         // baru
    // MC: satu option
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionText { get; set; }
    // MA: multiple options
    public List<string> SelectedOptionTexts { get; set; } = new(); // baru
    // Essay: text answer
    public string? TextAnswer { get; set; }            // baru
    public bool IsAnswered => QuestionType == "Essay"
        ? !string.IsNullOrWhiteSpace(TextAnswer)
        : (QuestionType == "MultipleAnswer"
            ? SelectedOptionTexts.Any()
            : SelectedOptionId.HasValue);
}
```

### AssessmentMonitoringDetail — Essay Grading Section

```html
<!-- Source: [ASSUMED] — berdasarkan D-15, D-16, UI-SPEC komponen grading -->
<div class="card shadow-sm mb-3 essay-grading-card" id="essay_@questionId">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-start mb-2">
            <h6 class="fw-semibold">Soal @displayNum: @questionText</h6>
            <span class="badge bg-secondary essay-status-badge" id="badge_@questionId">
                Belum Dinilai
            </span>
        </div>
        <!-- Jawaban pekerja (read-only) -->
        <div class="border rounded p-3 bg-light mb-2">
            <small class="text-muted d-block mb-1">Jawaban Pekerja:</small>
            <p class="mb-0">@textAnswer</p>
        </div>
        <!-- Rubrik (collapsed by default) -->
        <div class="alert alert-info small mb-2 collapsed-rubrik">
            <a class="text-decoration-none" data-bs-toggle="collapse"
               href="#rubrik_@questionId">Lihat Rubrik</a>
            <div class="collapse" id="rubrik_@questionId">@rubrik</div>
        </div>
        <!-- Input skor -->
        <div class="d-flex align-items-center gap-2">
            <label class="form-label mb-0 small">Skor:</label>
            <input type="number" class="form-control essay-score-input"
                   min="0" max="@scoreValue" style="max-width:80px"
                   value="@existingScore"
                   data-question-id="@questionId"
                   data-session-id="@sessionId" />
            <span class="text-muted small">/ @scoreValue</span>
            <button class="btn btn-primary btn-sm btn-save-essay-score"
                    data-question-id="@questionId"
                    data-session-id="@sessionId">
                Simpan Skor
            </button>
        </div>
    </div>
</div>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| GradeFromSavedAnswers di controller | GradingService terpusat | Phase 296 | GradingService adalah satu-satunya source of truth grading |
| MultipleChoice only | MultipleChoice + MultipleAnswer + Essay | Phase 298 | Switch-case di GradingService perlu diisi |
| Satu response row per question | Multiple rows untuk MA | Phase 298 | GradingService responses query perlu diubah dari Dictionary ke ILookup |
| Auto-complete setelah submit | "Menunggu Penilaian" untuk Essay | Phase 298 | GradeAndCompleteAsync perlu bercabang |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Jawaban MA disimpan sebagai multiple rows PackageUserResponse (satu per opsi yang dipilih) | Architecture Pattern 2, Pitfall 1 | Jika ternyata menggunakan format lain, storage dan grading logic perlu diganti |
| A2 | SignalR hub punya method `SaveAnswer(sessionId, questionId, optionId)` yang perlu di-extend untuk text | Pitfall 5, Pattern 4 | Jika signature berbeda, update method berbeda |
| A3 | Field EssayScore perlu ditambahkan ke PackageUserResponse untuk menyimpan skor manual HC | Critical Model Changes | Jika ada cara lain menyimpan skor essay (misalnya field di tabel lain), migration berbeda |
| A4 | Status "Menunggu Penilaian" aman ditambahkan sebagai string baru — tidak ada constraint DB | Common Pitfalls 3 | Jika ada constraint enum di DB, perlu migration |
| A5 | Manage Questions halaman masih ada (tidak di-remove di Phase 227 CLEN-02) | Architecture | AssessmentAdminController.cs line 2995 mencatat "Question Management removed in Phase 227" — perlu konfirmasi apakah ada halaman manage per-package |

---

## Open Questions

1. **Apakah ada halaman Manage Questions per-package yang masih aktif?**
   - Yang kita ketahui: CONTEXT.md D-01 menyebutkan "form create/edit soal per tipe" dan D-09 menyebutkan "preview soal sederhana — modal di halaman manage questions". AssessmentAdminController.cs line 2995 mengatakan "Question Management removed in Phase 227 (CLEN-02)".
   - Yang tidak jelas: Apakah yang di-remove adalah ManageQuestions legacy (per assessment), sementara ManagePackageQuestions (per package) masih ada?
   - Rekomendasi: Cari view `ManagePackageQuestions.cshtml` atau endpoint serupa sebelum planning. Kalau tidak ada, perlu dibuat dari scratch.

2. **Bagaimana SignalR SaveAnswer method saat ini menyimpan jawaban MA?**
   - Yang kita ketahui: Auto-save menggunakan SignalR. Jawaban MC disimpan via hub.
   - Yang tidak jelas: Method signature hub untuk MA dan Essay — apakah sudah ada atau perlu ditambahkan.
   - Rekomendasi: Baca `Hubs/AssessmentHub.cs` saat planning untuk konfirmasi.

3. **Bagaimana ExamSummary mendapat jawaban saat ini?**
   - Yang kita ketahui: `ViewBag.Answers` adalah `Dictionary<int, int>` — option ID per question. Untuk MA dan Essay, format ini tidak cukup.
   - Yang tidak jelas: Apakah ExamSummary di-populate dari DB atau dari form POST answers.
   - Rekomendasi: Baca CMPController.ExamSummary action sebelum planning task untuk view update.

---

## Environment Availability

Step 2.6: SKIPPED — Phase ini murni perubahan kode/view/migration di dalam proyek yang sudah ada. Tidak ada external dependency baru.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual testing (tidak ada automated test framework terdeteksi) |
| Config file | none |
| Quick run command | `dotnet build` + manual browser test |
| Full suite command | `dotnet build` + manual acceptance test per requirement |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command | Notes |
|--------|----------|-----------|---------|-------|
| QTYPE-02 | Form MA: checkbox multi-pilih, minimal 2 centang | Manual UI | Browser: buka form soal, pilih MA | — |
| QTYPE-03 | Form Essay: textarea rubrik muncul, opsi tersembunyi | Manual UI | Browser: buka form soal, pilih Essay | — |
| QTYPE-05 | Template Excel baru punya kolom QuestionType + Rubrik | Manual | Download template, buka di Excel | — |
| QTYPE-06 | Import file campur MC+MA+Essay berhasil | Manual | Upload file universal template | — |
| QTYPE-07 | StartExam: checkbox MA, textarea Essay, badge tipe | Manual UI | Browser: mulai ujian dengan soal MA+Essay | — |
| QTYPE-08 | MA scoring all-or-nothing | Manual | Submit ujian MA, cek skor di monitoring | — |
| QTYPE-09 | Status "Menunggu Penilaian" setelah submit ujian Essay | Manual | Submit ujian Essay, cek status di monitoring | — |
| QTYPE-10 | HC input skor Essay dari MonitoringDetail | Manual UI | Buka monitoring detail, input skor essay | — |
| QTYPE-11 | Recalculate skor total setelah semua Essay dinilai | Manual | Nilai semua Essay, klik Selesaikan Penilaian | — |
| QTYPE-13 | IsPassed null sampai semua Essay dinilai | Manual | Cek DB/UI: IsPassed harus null setelah submit | — |

### Wave 0 Gaps
- Tidak ada automated test framework — semua pengujian manual via browser.
- Pastikan `dotnet build` bersih sebelum setiap task di-commit.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | Yes | `[Authorize(Roles = "Admin, HC")]` pada endpoint SubmitEssayScore dan FinalizeEssayGrading |
| V5 Input Validation | Yes | Validasi skor Essay: 0 ≤ EssayScore ≤ ScoreValue. Validasi MaxCharacters di server-side. |
| V2 Authentication | No | Sudah ditangani ASP.NET Core Identity |
| V6 Cryptography | No | Tidak ada kebutuhan kriptografi baru |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| HC input skor negatif atau > ScoreValue | Tampering | Server-side validation: `if (score < 0 || score > q.ScoreValue) return BadRequest()` |
| Worker submit jawaban setelah timer habis | Tampering | Guard existing di SubmitExam — tetap berlaku untuk MA/Essay |
| Replay attack pada FinalizeEssayGrading | Tampering | `[ValidateAntiForgeryToken]` + guard: cek status session sebelum execute |
| IDOR: HC nilai essay milik session orang lain | Elevation of Privilege | Validasi: session harus dimiliki user dengan Title+Category+Schedule yang sama dengan HC's scope |

---

## Sources

### Primary (HIGH confidence)
- `[VERIFIED: .planning/phases/298-question-types/298-CONTEXT.md]` — 28 keputusan implementasi locked
- `[VERIFIED: Services/GradingService.cs]` — switch-case existing, placeholder MA+Essay, responses Dictionary query
- `[VERIFIED: Models/AssessmentPackage.cs]` — PackageQuestion.QuestionType field confirmed nullable string
- `[VERIFIED: Models/AssessmentSession.cs]` — HasManualGrading field confirmed
- `[VERIFIED: Models/PackageUserResponse.cs]` — TextAnswer field confirmed, tidak ada EssayScore
- `[VERIFIED: Controllers/AssessmentAdminController.cs]` — DownloadQuestionTemplate (7 kolom), ImportPackageQuestions parser, cross-package validation logic
- `[VERIFIED: Views/CMP/StartExam.cshtml]` — radio button pattern MC, auto-save structure, SignalR hub usage
- `[VERIFIED: Views/CMP/ExamSummary.cshtml]` — ExamSummaryItem structure, ViewBag.Answers Dictionary<int,int>
- `[VERIFIED: .planning/phases/298-question-types/298-UI-SPEC.md]` — design system, komponen Bootstrap, copywriting

### Secondary (MEDIUM confidence)
- `[VERIFIED: Models/PackageExamViewModel.cs]` — ExamQuestionItem tidak punya QuestionType field (perlu ditambahkan)
- `[VERIFIED: Models/AssessmentMonitoringViewModel.cs]` — MonitoringSessionViewModel tidak punya HasManualGrading/EssayPendingCount

### Tertiary (LOW confidence — perlu verifikasi saat planning)
- `[ASSUMED]` — MA jawaban disimpan sebagai multiple PackageUserResponse rows
- `[ASSUMED]` — SignalR hub signature untuk text answer
- `[ASSUMED]` — Apakah ada halaman ManagePackageQuestions yang aktif

---

## Metadata

**Confidence breakdown:**
- Model changes needed: HIGH — field yang ada dan yang perlu ditambah sudah dikonfirmasi dari kode
- GradingService changes: HIGH — placeholder sudah ada, logika MA/Essay jelas dari keputusan CONTEXT.md
- Excel import changes: HIGH — parser existing sudah dipahami, perlu extend kolom 8 dan 9
- StartExam UI: HIGH — pattern radio existing jelas, tinggal extend ke checkbox dan textarea
- Essay grading admin: MEDIUM — perlu baca AssessmentMonitoringDetail view lebih detail untuk planning task
- Manage Questions page: LOW — kemungkinan tidak ada, perlu konfirmasi

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stable — tidak ada breaking change yang diantisipasi)
