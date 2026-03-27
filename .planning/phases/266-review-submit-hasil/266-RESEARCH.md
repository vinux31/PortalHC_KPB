# Phase 266: Review, Submit & Hasil - Research

**Researched:** 2026-03-27
**Domain:** UAT — submit ujian, grading otomatis, halaman hasil, sertifikat (CMPController, server development)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

- **D-01:** rino.prasetyo — jawab **lengkap** semua soal → submit → **harus lulus** → test sertifikat preview + download PDF
- **D-02:** mohammad.arsyad — sengaja **skip beberapa soal** → review summary harus tampilkan warning soal belum dijawab → submit → **kemungkinan gagal** → pastikan tombol sertifikat **tidak muncul**
- **D-03:** moch.widyadhana sudah abandon di Phase 265, tidak dipakai di phase ini
- **D-04:** Verifikasi ExamSummary menampilkan daftar soal dengan status jawaban per soal
- **D-05:** Soal yang belum dijawab harus ada warning/indikator visual yang jelas
- **D-06:** Setelah submit, verifikasi skor dihitung benar: cross-check jumlah jawaban benar di database vs skor yang ditampilkan
- **D-07:** Verifikasi pass/fail sesuai passing grade assessment
- **D-08:** Jika soal punya data ElemenTeknis → verifikasi tabel ET + radar chart tampil dengan benar
- **D-09:** Jika soal tidak punya data ET → RESULT-03 dicatat "N/A — no ET data", tetap PASS
- **D-10:** Test **keduanya**: preview HTML (Certificate) + download PDF (CertificatePdf)
- **D-11:** Hanya ditest pada worker yang lulus (rino.prasetyo)
- **D-12:** Worker yang gagal (mohammad.arsyad) — pastikan tidak ada tombol/link sertifikat
- **D-13:** Alur sama seperti Phase 264-265: jalankan semua skenario → kumpulkan bug → fix batch di project lokal
- **D-14:** Verifikasi dual: visual check di browser + query database

### Claude's Discretion

- Urutan langkah-langkah test spesifik per worker
- Query database untuk verifikasi grading dan skor ET
- Detail verifikasi layout sertifikat (konten, format, watermark)

### Deferred Ideas (OUT OF SCOPE)

- Test koneksi putus saat submit → Phase 267
- Test timer habis saat di ExamSummary → Phase 267
- Admin monitoring progress real-time → Phase 268

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SUBMIT-01 | Summary jawaban ditampilkan per soal | ExamSummary GET action + ExamSummary.cshtml sudah ada — summaryItems dibangun dari ShuffledQuestionIds |
| SUBMIT-02 | Warning ditampilkan untuk soal belum dijawab | `unanswered > 0` → alert-warning di ExamSummary.cshtml; baris `table-warning` per soal yang belum dijawab |
| SUBMIT-03 | Submit berhasil, grading otomatis benar | SubmitExam POST: grading via `PackageOption.IsCorrect`, `finalPercentage = totalScore/maxScore * 100` |
| RESULT-01 | Skor dan status pass/fail ditampilkan | Results.cshtml: Score%, PassPercentage%, badge LULUS/TIDAK LULUS |
| RESULT-02 | Review jawaban per-soal (jawaban benar vs dipilih) | Results.cshtml: section "Tinjauan Jawaban" — hanya tampil jika `AllowAnswerReview == true` |
| RESULT-03 | Analisa Elemen Teknis ditampilkan (jika ada) | Results.cshtml: section "Analisis Elemen Teknis" — hanya tampil jika `ElemenTeknisScores != null && Any()` |
| CERT-01 | Sertifikat preview & download PDF (jika lulus) | Certificate action + CertificatePdf action — guard: `GenerateCertificate && IsPassed == true` |

</phase_requirements>

---

## Ringkasan Eksekutif

Phase 266 adalah UAT ketiga dari milestone v10.0. Seluruh infrastruktur kode sudah tersedia dan terbukti fungsional (Phase 265 verified zero bugs). Fokus phase ini adalah menjalankan 2 skenario di server development: rino.prasetyo (happy path lengkap — submit + sertifikat) dan mohammad.arsyad (partial answers — warning + kemungkinan gagal).

Kode controller dan view sudah well-tested secara desain: ExamSummary menangani TempData, SubmitExam menggunakan anti-race pattern (`ExecuteUpdateAsync` dengan filter `Status != "Completed"`), Results menampilkan ET analysis + radar chart secara kondisional, dan Certificate/CertificatePdf memiliki guard ganda (`GenerateCertificate && IsPassed`).

**Rekomendasi utama:** Jalankan skenario Rino terlebih dahulu (complete flow) lalu Arsyad. Verifikasi setiap tahap via browser + query database. Kumpulkan semua bug dulu, fix batch di lokal.

---

## State Awal dari Phase 265

Berdasarkan 265-01-SUMMARY.md:

| Worker | Assessment | Status Sesi di Server Dev |
|--------|-----------|--------------------------|
| rino.prasetyo | Assessment 7 (with token) | Session ID 9, **InProgress**, 3/5 soal terjawab |
| mohammad.arsyad | Assessment 10 (no token) | InProgress (belum diketahui berapa soal terjawab) |

**Implikasi untuk planning:**
- Rino perlu melanjutkan session yang sudah ada (session 9) — tidak perlu mulai ulang
- Rino perlu menjawab sisa soal yang belum dijawab (minimal yang cukup untuk lulus) sebelum klik "Selesai & Tinjau Jawaban"
- Arsyad perlu skip beberapa soal secara sengaja sebelum klik "Selesai & Tinjau Jawaban"

---

## Arsitektur Submit Flow (Kode yang Diverifikasi)

### Flow Lengkap

```
StartExam (POST "Selesai & Tinjau Jawaban")
    → ExamSummary POST: simpan answers ke TempData, redirect GET
    → ExamSummary GET: build summaryItems dari ShuffledQuestionIds, hitung unansweredCount
    → User klik "Kumpulkan Ujian" → confirm dialog
    → SubmitExam POST: grading, persist answers, ET scores, update status
    → Results GET: load PackageUserResponses + ElemenTeknisScores, build viewModel
    → (jika lulus + GenerateCertificate) → Certificate GET / CertificatePdf GET
```

### ExamSummary — Detail Kritis

- **TempData dependency:** Answers disimpan sebagai JSON di TempData. Jika user refresh/kembali ke halaman summary, TempData masih tersedia karena `TempData.Keep()` dipanggil.
- **AssignmentId flow:** ExamSummary POST menyimpan `PendingAssignmentId` ke TempData. GET action membacanya dengan cast int/long (CookieTempDataProvider serializes as long).
- **Warning logic:** `unansweredCount = summaryItems.Count(s => !s.SelectedOptionId.HasValue)` — baris di tabel dengan class `table-warning` untuk soal unanswered.
- **Submit form:** Hidden inputs untuk semua answers digenerate di view. POST ke SubmitExam dengan `id` + `Dictionary<int, int> answers`.

### SubmitExam — Detail Kritis

- **Grading formula:** `totalScore = sum(ScoreValue jika IsCorrect)`, `finalPercentage = (int)(totalScore / maxScore * 100.0)`
- **Upsert responses:** Jika SaveAnswer sudah menyimpan record sebelumnya, SubmitExam melakukan UPDATE (bukan INSERT) untuk menghindari duplikat.
- **ET scores:** Disimpan ke `SessionElemenTeknisScores` per ET group. Soal tanpa ET digroup ke "Lainnya".
- **Anti-race:** `ExecuteUpdateAsync` dengan filter `Status != "Completed"` — jika rowsAffected == 0, berarti AkhiriUjian sudah selesaikan sesi.
- **Certificate number:** Hanya di-generate jika `GenerateCertificate == true && isPassed == true`. Retry loop 3x untuk menghindari duplicate key.
- **Timer enforcement:** Server-side check — elapsed > DurationMinutes + 2 menit grace → reject submission. Karena session masih InProgress dari Phase 265, timer masih berjalan (wallclock).

**Potensi pitfall timer:** Session rino (ID 9) sudah InProgress sejak Phase 265. Jika DurationMinutes assessment pendek dan elapsed sudah melebihi batas + 2 menit grace, SubmitExam akan menolak dengan error "Waktu ujian Anda telah habis." Planner harus memeriksa DurationMinutes assessment dan elapsed time sebelum melanjutkan — atau jika perlu, perpanjang duration di admin.

### Results — Detail Kritis

- **AllowAnswerReview flag:** Jika flag ini `false` di assessment, section "Tinjauan Jawaban" **tidak tampil** meskipun worker sudah submit. Planner harus verifikasi setting ini di data Phase 264.
- **ET radar chart:** Hanya muncul jika `ElemenTeknisScores.Count >= 3`. Jika assessment punya < 3 ET unik, hanya tabel yang tampil (tidak ada radar chart).
- **Certificate button guard di view:** `@if (Model.IsPassed && Model.GenerateCertificate)` — tombol "Lihat Sertifikat" hanya muncul jika kedua kondisi true.
- **NomorSertifikat:** Ditampilkan sebagai alert-info di atas section ET jika `!string.IsNullOrEmpty(Model.NomorSertifikat)`.

### Certificate/CertificatePdf — Detail Kritis

- **Double guard:** Baik Certificate maupun CertificatePdf memiliki guard `GenerateCertificate` dan `IsPassed == true`. Jika salah satu false, redirect ke Results.
- **Certificate (HTML):** `Layout = null` — standalone page tanpa navbar. Ada tombol "Cetak" (print) dan "Kembali".
- **CertificatePdf:** Generate A4 landscape via QuestPDF. Filename: `Sertifikat_{NIP}_{SafeTitle}_{Year}.pdf` — di-serve sebagai file download.
- **Google Fonts dependency:** Certificate.cshtml load Playfair Display dari `fonts.googleapis.com`. Di server dev yang mungkin tidak ada akses internet, font akan fallback ke default browser — layout mungkin berbeda dari production.

---

## Query Database untuk Verifikasi

### Verifikasi Grading (SUBMIT-03)

```sql
-- Lihat skor yang tersimpan di AssessmentSessions
SELECT Id, UserId, Score, PassPercentage, IsPassed, Status, CompletedAt
FROM AssessmentSessions
WHERE Id = {session_id};

-- Hitung jawaban benar secara manual (cross-check)
SELECT COUNT(*) as CorrectCount
FROM PackageUserResponses pur
JOIN PackageQuestions pq ON pur.PackageQuestionId = pq.Id
JOIN PackageOptions po ON pur.PackageOptionId = po.Id
WHERE pur.AssessmentSessionId = {session_id}
  AND po.IsCorrect = 1;

-- Lihat total soal dan max score
SELECT COUNT(*) as TotalQuestions, SUM(ScoreValue) as MaxScore
FROM PackageQuestions
WHERE Id IN (
    SELECT value FROM UserPackageAssignments
    CROSS APPLY STRING_SPLIT(ShuffledQuestionIds, ',')  -- jika JSON array, gunakan JSON_VALUE
    WHERE AssessmentSessionId = {session_id}
);
```

*Catatan: ShuffledQuestionIds disimpan sebagai JSON array. Query di atas perlu disesuaikan tergantung format aktual. Gunakan pendekatan alternatif jika STRING_SPLIT tidak cocok.*

### Verifikasi ET Scores (RESULT-03)

```sql
-- Lihat ET scores yang tersimpan
SELECT ElemenTeknis, CorrectCount, QuestionCount
FROM SessionElemenTeknisScores
WHERE AssessmentSessionId = {session_id}
ORDER BY ElemenTeknis;
```

### Verifikasi NomorSertifikat (CERT-01)

```sql
-- Pastikan nomor sertifikat di-generate
SELECT NomorSertifikat, IsPassed, GenerateCertificate, CompletedAt
FROM AssessmentSessions
WHERE Id = {session_id};
```

---

## Pola UAT yang Sudah Terbukti (dari Phase 264-265)

- **Browser automation via Playwright** (file `uat-265-*.js` sudah ada di root project)
- **Alur UAT:** Jalankan semua skenario → kumpulkan semua bug → fix batch di lokal
- **Dual verification:** Visual browser + query database
- **Password rino di server dev:** `TotenhimFeb!26` (bukan 123456 dari seed)
- **Session ID rino:** 9 (InProgress dari Phase 265 — 3/5 soal terjawab)

---

## Potensi Bug yang Harus Diantisipasi

### Bug 1: Timer Expired pada Session Lama
**Apa yang bisa terjadi:** Session rino (ID 9) sudah InProgress sejak Phase 265. Jika DurationMinutes assessment 7 sudah terlewati + 2 menit grace, SubmitExam akan menolak dengan error.
**Cara deteksi:** Hitung elapsed = `DateTime.UtcNow - StartedAt`. Bandingkan dengan `DurationMinutes + 2`.
**Mitigasi:** Jika expired, admin perlu reset session (ubah `StartedAt` ke waktu sekarang, atau extend `DurationMinutes`).
**Confidence:** MEDIUM — tergantung berapa lama DurationMinutes assessment.

### Bug 2: AllowAnswerReview = false
**Apa yang bisa terjadi:** Section "Tinjauan Jawaban" tidak muncul meskipun requirement RESULT-02 mengharuskannya.
**Cara deteksi:** Cek `SELECT AllowAnswerReview FROM AssessmentSessions WHERE Id = {id}`.
**Mitigasi:** Jika false, admin perlu set ke true sebelum UAT.
**Confidence:** MEDIUM — tergantung setting di Phase 264.

### Bug 3: ET Chart Tidak Muncul (< 3 ET Groups)
**Apa yang bisa terjadi:** Assessment hanya punya 1-2 ET group → radar chart tidak tampil, hanya tabel.
**Cara deteksi:** Cek `SELECT DISTINCT ElemenTeknis FROM PackageQuestions WHERE PackageId IN (...)`.
**Behavior yang benar:** Tabel tampil tanpa radar chart (kode sudah handle ini — `Count >= 3` guard).
**Confidence:** HIGH — kode sudah defensive.

### Bug 4: Font Google Tidak Load di Server Dev
**Apa yang bisa terjadi:** Certificate.cshtml load font dari `fonts.googleapis.com`. Server dev mungkin tidak ada akses internet → font fallback.
**Impact:** Layout sertifikat HTML mungkin tampak berbeda dari PDF (QuestPDF embed font sendiri). Ini bukan bug fungsional.
**Confidence:** MEDIUM.

### Bug 5: Arsyad Session State
**Apa yang bisa terjadi:** Session arsyad di assessment 10 sudah InProgress dari Phase 265. Soal yang sudah dijawab tetap terjawab — plan perlu konfirmasi berapa soal yang sudah dijawab sebelum skenario "skip".
**Cara deteksi:** `SELECT COUNT(*) FROM PackageUserResponses WHERE AssessmentSessionId = {arsyad_session_id}`.
**Confidence:** HIGH — harus di-check sebelum UAT.

---

## Checklist Setting Assessment yang Harus Diverifikasi Sebelum UAT

| Setting | Assessment 7 (Rino) | Assessment 10 (Arsyad) | Diperlukan |
|---------|--------------------|-----------------------|-----------|
| `AllowAnswerReview` | ? | ? | true untuk RESULT-02 |
| `GenerateCertificate` | ? | ? | true untuk CERT-01 (Rino), tidak harus untuk Arsyad |
| `PassPercentage` | ? | ? | Harus diketahui untuk prediksi pass/fail |
| `DurationMinutes` | ? | ? | Untuk cek timer expiry |

---

## Struktur Skenario UAT

### Skenario 1: rino.prasetyo — Happy Path Lengkap

1. Login ke server dev → http://10.55.3.3/KPB-PortalHC/ (password: TotenhimFeb!26)
2. Masuk ke assessment 7 (session ID 9, sudah InProgress)
3. Jawab **semua sisa soal** yang belum dijawab
4. Klik "Selesai & Tinjau Jawaban"
5. **ExamSummary:** Verifikasi semua soal tampil, semua sudah dijawab, alert-success muncul (SUBMIT-01 ✓, SUBMIT-02 ✓)
6. Klik "Kumpulkan Ujian" → confirm dialog
7. **Results:** Verifikasi skor, pass/fail badge, NomorSertifikat (RESULT-01 ✓)
8. Verifikasi section "Tinjauan Jawaban" (jika AllowAnswerReview=true) (RESULT-02 ✓)
9. Verifikasi section "Analisis Elemen Teknis" (jika ada ET data) (RESULT-03 ✓ atau N/A)
10. Klik "Lihat Sertifikat" → verifikasi preview HTML (CERT-01 ✓)
11. Navigasi ke `/CMP/CertificatePdf/{id}` → verifikasi PDF download
12. Query database: cross-check skor (SUBMIT-03 ✓)

### Skenario 2: mohammad.arsyad — Partial Submit & Kemungkinan Gagal

1. Login ke server dev
2. Masuk ke assessment 10 (session arsyad, sudah InProgress)
3. **Sengaja skip beberapa soal** — jangan jawab semuanya
4. Klik "Selesai & Tinjau Jawaban"
5. **ExamSummary:** Verifikasi warning "X soal belum dijawab" muncul (SUBMIT-02 ✓), baris table-warning terlihat
6. Klik "Kumpulkan Ujian" → confirm dialog khusus ("X soal belum dijawab. Kumpulkan tetap?")
7. **Results:** Verifikasi skor dan pass/fail badge
8. Verifikasi **tombol "Lihat Sertifikat" TIDAK muncul** (D-12)
9. Query database: verifikasi IsPassed = 0, NomorSertifikat = NULL

---

## Validasi Arsitektur

### Framework Test
| Property | Value |
|----------|-------|
| Framework | Playwright (Node.js) — sudah digunakan di Phase 265 |
| Script referensi | `uat-265-test.js`, `uat-265-debug*.js` di root project |
| Quick run | `node uat-266-test.js` |
| Full suite | Manual browser verification + DB query |

### Mapping Requirement ke Test

| Req ID | Behavior | Tipe Test | Command | File Exists? |
|--------|----------|-----------|---------|--------------|
| SUBMIT-01 | Summary tampil per soal | Browser + visual | Playwright screenshot | ❌ Wave 0 |
| SUBMIT-02 | Warning soal belum dijawab | Browser + visual | Playwright check alert-warning | ❌ Wave 0 |
| SUBMIT-03 | Grading benar | DB query | SQL cross-check | Manual |
| RESULT-01 | Skor + pass/fail | Browser + visual | Playwright check badge | ❌ Wave 0 |
| RESULT-02 | Review jawaban per soal | Browser + visual | Playwright check list-group | ❌ Wave 0 |
| RESULT-03 | Analisa ET (jika ada) | Browser + visual | Playwright check table | ❌ Wave 0 |
| CERT-01 | Preview + PDF download | Browser + file check | Playwright + download check | ❌ Wave 0 |

### Wave 0 Gaps
- [ ] `uat-266-test.js` — script Playwright baru untuk phase 266
- [ ] SQL queries untuk verifikasi grading dan ET scores

---

## Environment Availability

| Dependency | Diperlukan Untuk | Tersedia | Versi | Fallback |
|------------|-----------------|----------|-------|----------|
| Server dev http://10.55.3.3/KPB-PortalHC/ | Semua UAT | Diasumsikan ✓ | — | — |
| Playwright (Node.js) | Browser automation | ✓ (dipakai Phase 265) | — | Manual browser |
| SQL Server (server dev) | DB verification | ✓ | — | — |
| QuestPDF | PDF generation | ✓ (sudah ada di CMPController) | — | — |
| Google Fonts | Certificate HTML font | Mungkin ✗ (server dev) | — | Font fallback |

---

## Project Constraints (dari CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` — ExamSummary GET/POST (line 1170-1276), SubmitExam POST (1278-1482), Results GET (1810-1996), Certificate GET (1512-1578), CertificatePdf GET (1581-1807) — dibaca langsung
- `Views/CMP/ExamSummary.cshtml` — UI summary, warning logic, hidden form — dibaca langsung
- `Views/CMP/Results.cshtml` — full results view termasuk ET chart + answer review — dibaca langsung
- `Views/CMP/Certificate.cshtml` — standalone certificate HTML — dibaca langsung
- `.planning/phases/265-worker-exam-flow/265-01-SUMMARY.md` — state session setelah Phase 265 — dibaca langsung

### Secondary (MEDIUM confidence)
- `.planning/phases/265-worker-exam-flow/uat-results.json` — raw UAT data (Playwright run pertama sebelum fix)
- `.planning/phases/265-worker-exam-flow/265-CONTEXT.md` — keputusan worker assignment

---

## Metadata

**Confidence breakdown:**
- Submit & grading flow: HIGH — kode dibaca langsung, pattern jelas
- Results & ET analysis: HIGH — kode dibaca langsung
- Certificate: HIGH — kode dibaca langsung, double guard terverifikasi
- State session server dev: MEDIUM — berdasarkan SUMMARY Phase 265, perlu dikonfirmasi via query DB
- Timer expiry risk: MEDIUM — tergantung DurationMinutes assessment yang perlu dicek

**Research date:** 2026-03-27
**Valid until:** 2026-04-27 (kode stabil, tidak ada perubahan aktif)
