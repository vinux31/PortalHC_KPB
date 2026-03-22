# Domain Pitfalls: Assessment & Training Management System

**Domain:** Corporate HR/HC Assessment & Training Management (Internal Portal Pertamina)
**Researched:** 2026-03-21
**Confidence:** MEDIUM-HIGH — berdasarkan code inspection + industry best practices

---

## Critical Pitfalls

Kesalahan yang menyebabkan rewrite atau masalah serius.

### Pitfall 1: ElemenTeknis Score Tidak Dipersist — Analytics Impossible

**What goes wrong:**
ElemenTeknis scoring dihitung saat grading di `SubmitExam`, ditampilkan di results view, tapi tidak disimpan ke DB. Jika nanti HC minta "tampilkan rata-rata score ET 'Pengetahuan Proses' untuk semua peserta assessment X tahun ini", data tidak ada. Satu-satunya cara recover: re-run grading query untuk semua historical sessions, tapi ini hanya works jika PackageUserResponse masih ada (data tidak pernah dihapus).

**Why it happens:**
ET scoring diimplementasikan sebagai visual-only feature — cukup untuk tampilkan di halaman results, tidak didesain untuk analytics.

**Consequences:**
- Analytics per-ET tidak bisa dibuat tanpa schema change
- Setiap kali admin lihat results, grading di-recompute dari scratch (boros query)
- Jika ada bug di ET grouping logic, historical display juga berubah (tidak reproducible)

**Prevention:**
Buat `SessionElemenTeknisScore` table. Isi saat `SubmitExam`. Satu row per (SessionId, ElemenTeknis). Fields: SessionId, ElemenTeknis, CorrectCount, TotalCount, ScorePercentage.

**Detection:**
Coba query: "rata-rata score ET 'X' untuk semua peserta assessment Y" → tidak bisa dilakukan tanpa re-compute.

---

### Pitfall 2: Compliance Percentage Tidak Meaningful Tanpa Compliance Matrix

**What goes wrong:**
`WorkerTrainingStatus.CompletionPercentage` dihitung sebagai `CompletedTrainings / TotalTrainings`. Tapi `TotalTrainings` adalah jumlah training yang SUDAH ADA di DB untuk worker tersebut — bukan jumlah training yang WAJIB untuk jabatannya. Jika seorang Senior Operator wajib punya 8 sertifikasi tapi di DB hanya ada 3 record (2 completed, 1 pending), system report completion = 67% padahal sebenarnya 25% dari yang wajib.

**Why it happens:**
Compliance matrix (jabatan → training wajib) tidak pernah diimplementasikan. Sistem hanya bisa tracking "apa yang sudah dilakukan", bukan "apa yang seharusnya dilakukan".

**Consequences:**
- Laporan compliance ke manajemen tidak akurat
- Audit internal tidak bisa detect gap secara otomatis
- HC harus manually cross-check per individu vs daftar wajib di spreadsheet terpisah

**Prevention:**
Implementasi `RequiredTraining` model sebelum build compliance dashboard. Tanpa denominator yang benar, angka compliance tidak reliable.

**Detection:**
Tanya HC: "Dari mana angka compliance % itu dihitung?" — jika jawabannya tidak jelas, ini confirmed pitfall.

---

### Pitfall 3: Question-Per-Session Coupling — No Reuse, No Quality Control

**What goes wrong:**
Soal terikat ke session (`PackageQuestion.AssessmentPackageId` → `AssessmentPackage.AssessmentSessionId`). Untuk buat assessment serupa di tahun berikutnya, soal harus di-import ulang dari Excel. Tidak ada cara untuk tahu soal mana yang "sudah pernah dipakai" atau "punya discrimination index bagus".

**Why it happens:**
Desain awal fokus pada functionality (exam works), bukan question lifecycle.

**Consequences:**
- HC kerja double — re-import soal yang sama setiap assessment cycle
- Soal berkualitas rendah (P-value ekstrim, D negatif) tidak terdeteksi
- Tidak ada version control untuk soal — jika ada koreksi, harus cari di setiap assessment
- Item analysis impossible tanpa bank terpusat

**Prevention:**
Saat membangun question bank, gunakan "copy by value" strategy — soal dari bank di-copy ke PackageQuestion, bukan di-reference via FK. Ini preserve exam engine dan historical data.

**Detection:**
Hitung: berapa % soal di PackageQuestion yang identik (QuestionText sama) lintas sessions? Jika > 20%, ada potensi penghematan signifikan dengan bank.

---

## Moderate Pitfalls

### Pitfall 4: Email Notification Duplikat dari Background Service

**What goes wrong:**
Background service `CertificateExpiryReminderService` jalan daily. Jika tidak ada de-duplikasi, setiap hari service akan query "ValidUntil = today + 30 days" dan kirim email. Worker dengan sertifikat yang valid sampai 30 hari lagi akan menerima email SETIAP HARI selama 30 hari.

**Prevention:**
Buat `NotificationSentLog` table dengan (UserId, RecordId, RecordType, ThresholdDays). Sebelum kirim, cek apakah entry untuk kombinasi ini sudah ada. Jika sudah, skip. Tambah index pada (RecordId, RecordType, ThresholdDays) untuk query cepat.

---

### Pitfall 5: Analytics Dashboard N+1 Query

**What goes wrong:**
Developer build analytics dengan loop: untuk setiap section → query training records → compute stats. Dengan 10 sections × 20 workers × queries = ratusan DB round-trips per halaman load.

**Prevention:**
Aggregate di DB level. Contoh pattern yang benar:
```csharp
var passRateByCategory = await _context.AssessmentSessions
    .Where(s => s.IsPassed != null && s.CompletedAt != null)
    .GroupBy(s => s.Category)
    .Select(g => new {
        Category = g.Key,
        PassRate = (double)g.Count(s => s.IsPassed == true) / g.Count()
    })
    .ToListAsync();
```
Satu query, bukan N queries.

---

### Pitfall 6: RequiredTraining Position Matching Case-Sensitive

**What goes wrong:**
`RequiredTraining.PositionTitle` harus match dengan `ApplicationUser.Position`. Jika ada inconsistency case ("Senior Operator" vs "senior operator") atau trailing spaces, compliance query mengembalikan 0 required trainings untuk worker tersebut — false positive "100% compliant".

**Prevention:**
Saat query compliance, normalize comparison:
```csharp
.Where(r => r.PositionTitle.ToLower().Trim() == worker.Position.ToLower().Trim())
```
Atau normalize saat input (store `PositionTitle` selalu lowercase trimmed, tampilkan dengan TextInfo.ToTitleCase).

---

### Pitfall 7: Tab-Switch Detection Causes False Alarms for Legitimate Actions

**What goes wrong:**
Exam page deteksi `visibilitychange` → log "focus_lost". Tapi event ini juga trigger ketika worker:
- Alt+Tab ke calculator (legitimate)
- Minimize window untuk check physical manual (maybe legitimate)
- OS push notification appears

Jika HC auto-terminate exam saat focus_lost, akan ada legitimate complaints.

**Prevention:**
Tab-switch detection harus HANYA LOG, tidak auto-terminate. Tampilkan warning ke peserta ("Anda terdeteksi meninggalkan halaman ujian — hal ini tercatat"). HC yang decide berdasarkan monitoring view.

---

## Minor Pitfalls

### Pitfall 8: Chart.js Tidak Diinstall — Analytics Dashboard Butuh Library

**What goes wrong:**
Untuk build analytics dashboard dengan chart, butuh chart library. Codebase belum punya Chart.js atau ApexCharts.

**Prevention:**
Chart.js bisa diload via CDN (seperti Bootstrap dan jQuery). Tambah satu script tag. Tidak perlu npm build pipeline. Pilih Chart.js (lebih ringan dari ApexCharts untuk use case sederhana).

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
```

---

### Pitfall 9: AssessmentSession.Status Tidak Konsisten dengan Completion State

**What goes wrong:**
`AssessmentSession.Status` values: "Open", "Upcoming", "Completed". Tapi seorang worker yang sudah submit exam — apakah statusnya "Completed" untuk session mereka, atau "Completed" untuk semua sessions dengan title yang sama?

Dari code: status di-set per-session, bukan per-group. Ini benar. Tapi penamaan di beberapa view mungkin ambigu.

**Prevention:**
Ketika build analytics, pastikan query selalu filter `IsPassed IS NOT NULL` (sudah di-graded) atau `Status = "Completed"` secara konsisten — jangan campurkan.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Analytics Dashboard | N+1 query performance | Aggregate di DB, satu query per metric |
| Analytics Dashboard | Missing ElemenTeknis data | Accept limitation dulu; persist ET score dulu sebelum ET analytics |
| Training Compliance Matrix | Position string mismatch | Normalize strings; tampilkan preview "X workers akan di-cover" sebelum save |
| Question Bank | Exam engine breaks | Copy-by-value strategy; engine tidak perlu diubah |
| Question Bank | Legacy AssessmentQuestion orphaned | Bank hanya untuk path baru; legacy tetap read-only |
| Email Notification | Duplicate sends | NotificationSentLog de-duplikasi wajib |
| Email Notification | SMTP not configured | Cek appsettings.Production.json sebelum implement |
| Tab-switch detection | False alarms | Log only, NEVER auto-terminate |
| ElemenTeknis persist | Retroactive data | ET score hanya untuk sessions BARU; tidak perlu backfill |

---

## Design Smells yang Sudah Ada (Tidak Perlu Fix Sekarang, Catat Saja)

| Issue | Lokasi | Severity | Catatan |
|-------|--------|----------|---------|
| Dua question paths (legacy + package) coexist | AssessmentSession + CMPController | LOW | Sudah documented di code; tidak menyebabkan bug aktif |
| `TrainingRecord.Kategori` raw string tidak normalized | TrainingRecord.cs | LOW | Risk typo saat manual input |
| `AssessmentSession.Status` diset manual string | Multiple controllers | LOW | Bukan enum; tapi nilai sudah terbatas |
| Competency auto-update DISABLED | Comment di code | LOW | Orphaned tables; tidak aktif, tidak berbahaya |
| `WorkerTrainingStatus.CompletionPercentage` denominator salah | WorkerTrainingStatus.cs | MEDIUM | Perlu fix setelah compliance matrix ada |
| `UserResponse` tidak ada timestamp | UserResponse.cs | LOW | Aman untuk sekarang; butuh schema change untuk fix |

---

## Sources

- Direct code inspection: `Models/AssessmentSession.cs`, `PackageUserResponse.cs`, `UserResponse.cs`, `WorkerTrainingStatus.cs`
- Direct code inspection: `Controllers/CMPController.cs` — ElemenTeknis scoring L2088-2136 (confirms tidak dipersist)
- Web research: Anti-cheating measures — [Synap Blog](https://synap.ac/blog/anti-cheat-methods-for-online-exams)
- Web research: Expiry notification best practices — [ExpiryEdge](https://expiryedge.com/features/)
- Web research: Item analysis — [University of Washington](https://www.washington.edu/assessment/scanning-scoring/scoring/reports/item-analysis/)
- Web research: Training compliance — [ComplianceQuest](https://www.compliancequest.com/cq-guide/training-management-systems-regulatory-compliance/)

---
*Pitfalls research untuk: Portal HC KPB — Assessment & Training Management Gap Analysis*
*Researched: 2026-03-21*
