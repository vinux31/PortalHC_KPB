# 268-UAT-ANALYSIS.md
**Phase 268 — Monitoring Dashboard Assessment**
**Dibuat:** 2026-03-28
**Status:** Siap untuk UAT browser

---

## Ringkasan Analisa Kode

Monitoring dashboard assessment sudah sepenuhnya diimplementasi. Ada **1 bug CONFIRMED** dan **1 bug SUSPECTED** yang perlu diverifikasi via UAT. Semua fitur dasar (SignalR push, handler JS, route hub) sudah ada dan secara teori berfungsi untuk package mode (non-package path sudah dihapus di Phase 227).

---

## MON-01: Progress Real-Time (x/total soal terjawab)

### Alur Kode
```
Worker klik radio answer
  → JS saveAnswerAsync() → POST CMPController.SaveAnswer(sessionId, questionId, optionId)
  → Hitung answeredCount dari PackageUserResponses
  → Ambil assignment.GetShuffledQuestionIds().Count → totalQuestions
  → _hubContext.Clients.Group("monitor-{batchKey}").SendAsync("progressUpdate", { sessionId, progress, totalQuestions })
  → AssessmentMonitoringDetail.cshtml — handler "progressUpdate"
      → update .progress-cell dengan "x/total"
      → flashRow(tr, 'flash-update')
```

### File:Line Kunci
- **CMPController.cs:309-319** — `answeredCount`, `totalQuestions`, `SendAsync("progressUpdate")`
- **AssessmentMonitoringDetail.cshtml:1142-1154** — handler progressUpdate JS
- **Batchkey format:** `"{title}|{category}|{schedule.Date:yyyy-MM-dd}"` (harus identik antara SaveAnswer dan JoinMonitor)

### Verifikasi Kode
- `progressUpdate` dikirim ke group `monitor-{batchKey}` DAN ke `User(session.UserId)` — benar
- `totalQuestions` = `assignment?.GetShuffledQuestionIds().Count ?? 0` — jika `assignment` null (artinya session tidak punya package), totalQuestions akan 0 dan tampil "—". Tapi karena non-package path sudah dihapus (line 1517: error jika tidak ada package), semua session aktif pasti punya assignment
- JS handler: update `progressCell.textContent = data.totalQuestions > 0 ? (data.progress + '/' + data.totalQuestions) : '—'` — benar

### Status: INFO
**Catatan:** Progress awal di halaman monitor akan tampil "—/N" (karena progress tidak dikirim saat halaman pertama load, hanya saat SaveAnswer dipanggil). Setelah worker jawab soal pertama, akan update menjadi "1/N". Ini bukan bug — perilaku yang diharapkan.

---

## MON-02: Status Lifecycle (Open → InProgress → Completed)

### Alur Kode — InProgress
```
Worker buka StartExam pertama kali
  → CMPController.StartExam() → justStarted = (assessment.StartedAt == null)
  → Jika justStarted: _hubContext.Clients.Group("monitor-{batchKey}").SendAsync("workerStarted", { sessionId, workerName, status: "InProgress" })
  → AssessmentMonitoringDetail.cshtml — handler "workerStarted"
      → update .status-cell → '<span class="badge bg-warning text-dark">InProgress</span>'
      → flashRow, toast notification, updateSummaryFromDOM()
```

### Alur Kode — Completed
```
Worker submit ExamSummary POST → CMPController.ExamSummary(id, assignmentId, answers)
  → package path (packageAssignment != null)
  → Grading, update AssessmentSession.Status = "Completed"
  → _hubContext.Clients.Group("monitor-{batchKey}").SendAsync("workerSubmitted",
      { sessionId, workerName, score, result, status: "Completed", totalQuestions })
  → AssessmentMonitoringDetail.cshtml — handler "workerSubmitted"
      → update Status, Score, Result, CompletedAt di row
      → flashRow(tr, 'flash-complete')
      → hide Akhiri Semua button jika semua selesai
```

### File:Line Kunci
- **CMPController.cs:775-779** — `workerStarted` push di StartExam
- **CMPController.cs:1493-1500** — `workerSubmitted` push di ExamSummary (package path saja)
- **CMPController.cs:1515-1519** — non-package path mengembalikan error (dihapus)
- **AssessmentMonitoringDetail.cshtml:1157-1171** — handler workerStarted
- **AssessmentMonitoringDetail.cshtml:1174-1231** — handler workerSubmitted

### Status: SUSPECTED BUG di workerStarted
**Bug:** `workerStarted` handler TIDAK mengupdate kolom "Time Remaining" — lihat MON-03 untuk detail.

**Status lifecycle sendiri:** Secara teori OK untuk package mode.

---

## MON-03: Timer/Elapsed (tampil, bergerak, tidak nol/negatif)

### Mekanisme Timer — Dua Lapisan

**Lapisan 1: Load-time countdown (saat halaman monitoring dimuat)**
- Kode: `AssessmentMonitoringDetail.cshtml:867-895` — fungsi `updateTimeRemaining()`
- Membaca `data-started-at` (ISO string) dari `<tr>` attribute
- `data-started-at` diisi saat Razor render: `@(session.UserStatus == "InProgress" ? session.StartedAt?.ToString("o") ?? "" : "")` (line 241)
- Hitung: `timeLeft = (startedAt + durationMs) - Date.now()`
- Update setiap 1 detik via `setInterval(updateTimeRemaining, 1000)`

**Lapisan 2: countdownMap (dari updateRow — hanya dipanggil saat ada data server polling)**
- `countdownMap[sessionId] = session.remainingSeconds` saat `updateRow()` dipanggil
- Tapi `updateRow()` hanya dipanggil dari polling endpoint — dan tidak ada polling endpoint! Monitoring ini murni push-based.
- `tickCountdowns()` mengurangi 1 detik setiap detik, tapi hanya jika `countdownMap` sudah terisi

### BUG CONFIRMED (MON-03-BUG-1): Timer tidak muncul untuk worker yang mulai SETELAH halaman monitoring dibuka

**Masalah:**
1. Admin buka halaman monitoring → server render `<tr>` dengan `data-started-at=""` (kosong) karena session.StartedAt == null (worker belum mulai)
2. Worker mulai ujian → `workerStarted` push diterima admin
3. `workerStarted` handler hanya update status badge, TIDAK update `data-started-at` attribute
4. `updateTimeRemaining()` cek `data-started-at` — masih kosong → return early → timer tetap "—"
5. `countdownMap` juga tidak terisi karena `workerStarted` payload tidak berisi `remainingSeconds`

**Hasil:** Kolom "Time Remaining" tetap "—" meskipun worker sudah InProgress dan timer berjalan di sisi worker.

**Diperkuat oleh:** `workerStarted` payload hanya berisi `{ sessionId, workerName, status }` (CMPController.cs:778-779) — tidak ada `startedAt` atau `durationMinutes`.

**File:Line:**
- **CMPController.cs:778-779** — `workerStarted` payload (tidak berisi startedAt/durationMinutes)
- **AssessmentMonitoringDetail.cshtml:241** — `data-started-at` hanya diisi saat server render jika InProgress
- **AssessmentMonitoringDetail.cshtml:1157-1171** — `workerStarted` handler (tidak update data-started-at)
- **AssessmentMonitoringDetail.cshtml:867-895** — `updateTimeRemaining()` (bergantung pada data-started-at)

**Kondisi yang mana timer AKAN berjalan:**
- Admin buka halaman monitoring SETELAH worker sudah mulai ujian → `data-started-at` sudah terisi di server render → timer berjalan normal

**Kondisi yang mana timer TIDAK berjalan:**
- Admin buka halaman monitoring SEBELUM worker mulai → timer "—" sampai halaman di-refresh

**Workaround sementara:** Refresh halaman monitoring setelah worker mulai ujian.

**Rencana fix:**
- Server push `workerStarted` harus menyertakan `startedAt` (ISO) dan `durationMinutes`
- JS handler `workerStarted` harus update `data-started-at` dan `data-duration` di `<tr>`

---

## MON-04: Skor dan Pass/Fail Setelah Submit

### Alur Kode
```
Worker submit → CMPController.ExamSummary (package path)
  → Grading: finalPercentage = Math.Round(correctScore / totalScore * 100, 2)
  → result = finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail"
  → SendAsync("workerSubmitted", { sessionId, workerName, score: finalPercentage, result, status: "Completed", totalQuestions })
  → AssessmentMonitoringDetail.cshtml — handler workerSubmitted
      → tds[3] = data.score + '%' (Score column)
      → tds[4] = data.result (Result column) dengan class text-success/text-danger
```

### File:Line Kunci
- **CMPController.cs:1493-1500** — push workerSubmitted dengan score, result
- **AssessmentMonitoringDetail.cshtml:1174-1231** — handler workerSubmitted (update all columns)

### Verifikasi Kode
- Score dikirim sebagai `finalPercentage` (desimal, misal 85.0)
- Jika `assessment.PassPercentage` adalah 0 dan score 0 → result = "Pass" (edge case, tidak relevan untuk UAT normal)
- `score` bisa null di payload hanya jika `finalPercentage` null — tapi `finalPercentage` dihitung dari decimal arithmetic, tidak mungkin null

### Status: OK (untuk package mode)
**Catatan:** `workerSubmitted` hanya dikirim di package path, tapi non-package path sudah dihapus di Phase 227 — jadi semua assessment aktif pasti package mode. MON-04 seharusnya berfungsi.

**PERHATIAN:** Perlu verifikasi apakah assessment di server dev memang package mode (ada `UserPackageAssignments` untuk session rino.prasetyo).

---

## Bug List

### CONFIRMED Bugs (pasti ada, perlu fix setelah UAT)

| # | Bug | File | Baris | Impact |
|---|-----|------|-------|--------|
| BUG-01 | `workerStarted` tidak update `data-started-at` → timer tidak tampil untuk worker yang mulai setelah halaman dibuka | CMPController.cs (payload) + AssessmentMonitoringDetail.cshtml (handler) | 778-779 + 1157-1171 | MON-03: timer "—" sampai refresh |

### SUSPECTED Bugs (perlu verifikasi UAT)

| # | Bug | Kondisi Trigger | Impact |
|---|-----|-----------------|--------|
| BUG-02 | Assessment di server dev mungkin non-package mode → `workerSubmitted` tidak dikirim → MON-04 FAIL | Jika session rino tidak punya `UserPackageAssignments` | MON-04: score tidak update real-time |

### INFO (bukan bug, perilaku expected)

| # | Catatan |
|---|---------|
| INFO-01 | Progress awal tampil "—/N" bukan "0/N" — update hanya terjadi saat SaveAnswer pertama dipanggil |
| INFO-02 | Timer berjalan jika admin buka monitoring SETELAH worker mulai — ini adalah perilaku normal untuk data existing |
| INFO-03 | Clock skew antara browser admin dan worker mungkin menyebabkan selisih beberapa detik — acceptable per D-05 |

---

## Skenario UAT

### Persiapan Sebelum Test

1. Login admin@pertamina.com di http://10.55.3.3/KPB-PortalHC/
2. Buka Kelola Data > Assessment Monitoring
3. Verifikasi ada assessment group yang masih "Open" dengan worker yang belum mulai
4. **Jika tidak ada assessment Open yang bisa dipakai:** Minta Admin reset salah satu session (Kelola Data > Assessment Monitoring > pilih group > klik Reset untuk worker rino.prasetyo) sehingga status kembali ke Open/Not started

---

### Skenario A — MON-04: Verifikasi Data Existing (Completed)

**Tujuan:** Pastikan monitoring menampilkan Score% dan Pass/Fail untuk session yang sudah Completed sebelumnya.

**Langkah:**
1. Login admin@pertamina.com
2. Buka Kelola Data > Assessment Monitoring
3. Verifikasi: assessment group dari Phase 264-267 muncul di list (dengan status Closed/Completed)
4. Klik nama salah satu assessment group yang punya worker Completed
5. Di halaman detail, verifikasi:
   - Kolom "Score" menampilkan angka + "%" (bukan "—")
   - Kolom "Result" menampilkan "Pass" atau "Fail" (bukan "—")
   - Kolom "Completed At" menampilkan tanggal/waktu (bukan "—")
6. Klik tombol "View Results" untuk satu worker Completed → verifikasi halaman Results terbuka di tab baru dengan data lengkap

**Hasil yang diharapkan:** Score%, Pass/Fail, dan Completed At terisi untuk semua worker dengan status Completed.

**Laporkan:** MON-04 (data existing): PASS/FAIL + apa yang dilihat

---

### Skenario B — MON-01 + MON-02 + MON-03: Live Two-Browser Test

**Persiapan:** Dua browser/tab berbeda (admin dan worker berbeda akun).

**Step B1 — Setup admin (MON-02 baseline)**
1. Browser 1: Login admin@pertamina.com
2. Buka detail assessment group yang punya worker rino.prasetyo@pertamina.com dengan status "Not started" / "Open"
3. Catat: status badge rino di tabel = "Not started" (atau "Open")
4. Biarkan halaman monitoring terbuka

**Step B2 — Worker mulai ujian (MON-02 — InProgress)**
5. Browser 2: Login rino.prasetyo@pertamina.com
6. Buka Assessment > Cari assessment yang sama
7. Klik "Mulai Ujian" (masukkan token jika diminta)
8. Verifikasi ujian dimulai (soal muncul)

**Step B3 — Verifikasi di Admin (MON-02 + MON-03)**
9. Browser 1 (admin): Tanpa refresh, amati baris rino
   - Apakah status badge berubah dari "Not started" ke "InProgress"? → **MON-02**
   - Apakah ada toast notification "rino memulai ujian"?
   - Apakah kolom "Time Remaining" menampilkan timer yang bergerak (bukan "—")? → **MON-03**
   - **CATATAN:** Berdasarkan analisa kode, timer kemungkinan tetap "—" (BUG-01 CONFIRMED)

**Step B4 — Worker jawab soal (MON-01)**
10. Browser 2 (worker): Klik salah satu jawaban radio
11. Browser 1 (admin): Tanpa refresh, amati kolom "Progress" baris rino
    - Apakah progress berubah dari "—" ke "1/N"? → **MON-01**
    - Apakah baris flash (highlight kuning sebentar)?
12. Jawab 2-3 soal lagi, verifikasi progress update ke "2/N", "3/N", dst.

**Step B5 — Worker submit ujian (MON-02 + MON-04)**
13. Browser 2 (worker): Selesaikan semua soal dan klik "Submit"
14. Browser 1 (admin): Tanpa refresh, amati baris rino
    - Apakah status badge berubah ke "Completed"? → **MON-02**
    - Apakah kolom "Score" menampilkan angka%? → **MON-04**
    - Apakah kolom "Result" menampilkan "Pass" atau "Fail"? → **MON-04**
    - Apakah ada toast notification "rino menyelesaikan ujian"?

---

### Skenario C — Fallback jika SignalR tidak bekerja

Jika ada update yang tidak muncul real-time di Skenario B:

1. Buka browser console (F12 > Console) di admin browser
2. Cek apakah ada error JavaScript
3. Cek apakah `window.assessmentBatchKey` terdefinisi: ketik `window.assessmentBatchKey` di console → harus ada nilai seperti "OJT Assessment|OJT|2026-01-15"
4. Cek apakah hub badge menampilkan "Live" atau "Disconnected"
5. Catat error yang ada
6. Refresh halaman monitoring → verifikasi data tetap benar (Score, Status, Progress via server render)

---

### Checklist Laporan UAT

Setelah menjalankan semua skenario, laporkan:

```
MON-01 (Progress real-time): [PASS/FAIL]
Detail: [progress counter berubah tanpa refresh / tidak berubah / dll]

MON-02 (Status lifecycle): [PASS/FAIL]
Detail: [InProgress muncul / tidak muncul] [Completed muncul / tidak muncul]

MON-03 (Timer/elapsed): [PASS/FAIL]
Detail: [timer tampil / "—"] [bergerak / diam] [nilai wajar / nol/negatif]
CATATAN: Berdasarkan analisa kode, MON-03 kemungkinan FAIL (BUG-01 confirmed)
Jika refresh halaman setelah worker mulai → apakah timer muncul? [Ya/Tidak]

MON-04 (Skor setelah submit): [PASS/FAIL]
Detail: [skor muncul real-time / perlu refresh / tidak muncul sama sekali]

Bug tambahan ditemukan: [deskripsi + cara reproduksi]

Tipe assessment yang ditest: [Package mode / tidak tahu]
```

---

## Prediksi Hasil UAT (berdasarkan analisa kode)

| Requirement | Prediksi | Alasan |
|-------------|----------|--------|
| MON-01 (Progress) | PASS | SignalR push ada, handler ada, payload lengkap |
| MON-02 (Lifecycle) | PASS | workerStarted dan workerSubmitted ada, handler ada |
| MON-03 (Timer) | FAIL (jika admin buka sebelum worker mulai) | BUG-01 CONFIRMED: workerStarted tidak update data-started-at |
| MON-04 (Skor) | PASS (jika package mode) | Handler workerSubmitted lengkap, payload berisi score + result |

**Fix yang sudah dipersiapkan untuk BUG-01 (jika terkonfirmasi):**
1. CMPController.StartExam: tambahkan `startedAt` dan `durationMinutes` ke `workerStarted` payload
2. AssessmentMonitoringDetail.cshtml: update `workerStarted` handler untuk set `data-started-at` dan `data-duration` di `<tr>` element

---

*Dokumen ini dibuat dari analisa statis kode. Hasil aktual mungkin berbeda — UAT browser diperlukan untuk konfirmasi.*
