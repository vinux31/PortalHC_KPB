# Phase 244: UAT Monitoring & Analytics - Research

**Researched:** 2026-03-24
**Domain:** UAT manual verification — SignalR monitoring, token management, Excel export, analytics dashboard
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 (MON-01):** Pendekatan dual browser — worker mengerjakan ujian di browser A, HC membuka AssessmentMonitoringDetail di browser B. Verifikasi stat cards dan status per-user diperbarui secara live tanpa refresh halaman.
- **D-02 (MON-02):** Tes dengan linear sequence satu flow panjang berurutan: copy token → regenerate token → verifikasi token lama invalid → force close ujian peserta → reset peserta → peserta bisa ujian ulang. Satu skenario realistis sesuai workflow HC.
- **D-04 (MON-04):** Test semua kombinasi cascading filter: Bagian saja, Bagian+Unit, Bagian+Unit+Kategori, plus reset filter. Verifikasi fail rate, trend skor, ET breakdown, dan expiring soon tampil dengan benar di setiap kombinasi.

### Claude's Discretion

- **D-03 (MON-03):** Level validasi export Excel — Claude memilih antara structural check (cek header/sheet name) atau full data match (bandingkan nilai sel dengan data DB).

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — diskusi tetap dalam scope fase ini.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MON-01 | HC dapat memonitor ujian real-time via SignalR dengan stat cards dan status per-user | AssessmentMonitoringDetail + AssessmentHub sudah ada; SignalR group `monitor-{batchKey}` digunakan; JS di view sudah listen update dari hub |
| MON-02 | HC dapat manage token (copy, regenerate) dan force close/reset ujian | RegenerateToken (AdminController:2155), AkhiriUjian, ResetAssessment actions sudah implemented; token card di view sudah ada |
| MON-03 | HC dapat export hasil ujian ke Excel | ExportAssessmentResults (AdminController:3019) via ClosedXML sudah implemented; GET form di view sudah ada |
| MON-04 | Analytics dashboard menampilkan fail rate, trend, ET breakdown, dan expiring soon dengan cascading filter | AnalyticsDashboard + GetAnalyticsData (CMPController:2051) sudah implemented; cascading filter via AJAX sudah ada |

</phase_requirements>

---

## Summary

Phase 244 adalah fase UAT murni. Seluruh implementasi fitur monitoring, token management, export Excel, dan analytics dashboard sudah shipped di milestone sebelumnya. Tidak ada kode baru yang perlu ditulis — semua task adalah skenario verifikasi manual via browser.

Penelitian kode konfirmasi: (1) `AssessmentHub` sudah handle group `monitor-{batchKey}`, `OnConnected/OnDisconnected` events, dan `LogPageNav`; (2) `AssessmentMonitoringDetail` view sudah punya stat cards dengan `id="count-total"`, `id="count-completed"`, dll. yang siap diupdate via SignalR JS; (3) `RegenerateToken` mengupdate semua sibling sessions dengan token baru via batch update; (4) `ExportAssessmentResults` menggunakan ClosedXML dan menyertakan header multi-row plus data per peserta; (5) `GetAnalyticsData` di CMPController sudah mendukung filter Bagian/Unit/Kategori dengan query DB terpisah per chart (failRate, trend, etBreakdown, expiringSoon).

**Primary recommendation:** Buat satu plan per requirement (4 plans total), masing-masing berisi langkah-langkah UAT manual yang terstruktur. Tidak ada wave implementasi — hanya task verifikasi berurutan sesuai keputusan D-01 s/d D-04.

---

## Standard Stack

### Core (sudah terinstall di proyek)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.SignalR | built-in .NET 8 | Real-time hub komunikasi | Sudah dipakai di proyek, mapped di Program.cs |
| ClosedXML | existing | Excel generation | Sudah dipakai ExportAssessmentResults |
| Chart.js | existing (CDN) | Analytics charts | Sudah dipakai AnalyticsDashboard.cshtml |

Tidak ada instalasi paket baru yang diperlukan untuk fase ini.

---

## Architecture Patterns

### Pattern: UAT Sequential Flow (D-02)

Skenario token management mengikuti alur linear berurutan:

```
1. HC buka AssessmentMonitoring → pilih assessment "OJT Proses Alkylation Q1-2026"
2. HC copy token dari token card → verifikasi clipboard
3. HC klik Regenerate Token → konfirmasi modal → token baru tampil di UI
4. Worker coba input token lama → sistem menolak (invalid token)
5. HC klik "Akhiri Ujian" pada row peserta yang sedang InProgress → status berubah Completed/graded
6. HC klik "Reset" pada row peserta → status kembali ke Not started / session reset
7. Worker input token baru → mulai ujian ulang → berhasil masuk exam
```

Alur ini cover MON-02 + EDGE-02 + EDGE-03 dalam satu flow realistis.

### Pattern: Dual Browser (D-01)

```
Browser A (Worker — Rino):  Login sebagai worker → buka exam → kerjakan soal
Browser B (HC — Admin):     Login sebagai HC/Admin → buka AssessmentMonitoringDetail
                             → amati stat cards berubah real-time saat worker progres
```

SignalR group yang digunakan:
- Worker join `batch-{batchKey}` saat exam dimulai
- HC join `monitor-{batchKey}` saat buka detail page
- Server push update ke `monitor-{batchKey}` saat ada perubahan status peserta

### Pattern: Analytics Filter Kombinasi (D-04)

Urutan test yang harus dicakup:

| Test | Bagian | Unit | Kategori | Expected |
|------|--------|------|----------|----------|
| 1 | Alkylation | (kosong) | (kosong) | Data filtered by section |
| 2 | Alkylation | RFCC NHT | (kosong) | Data filtered by section+unit |
| 3 | Alkylation | RFCC NHT | OJT | Data filtered by all 3 |
| 4 | (reset) | (kosong) | (kosong) | Semua data kembali tampil |

### Anti-Patterns to Avoid

- **Jangan skip verifikasi SignalR connection badge:** UI punya `id="hubStatusBadge"` yang menampilkan status koneksi. Pastikan badge berubah dari "Connecting..." ke "Connected" sebelum mulai test D-01.
- **Jangan assume token lama sudah invalid tanpa verifikasi:** D-02 mengharuskan worker aktif mencoba token lama di browser A setelah regenerate dilakukan di browser B.
- **Jangan export saat 0 sessions:** Export diakses via GET dengan query params title/category/scheduleDate — pastikan assessment group dengan data yang benar digunakan.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Real-time push ke monitor | Custom polling endpoint | SignalR `monitor-{batchKey}` group | Sudah ada, hanya perlu diverifikasi |
| Excel dengan header multi-row | Custom stream | ClosedXML (sudah impl) | ExportAssessmentResults line 3019 |
| Analytics chart render | Custom D3 | Chart.js (sudah CDN) | Sudah dipakai di AnalyticsDashboard.cshtml |

---

## Common Pitfalls

### Pitfall 1: SignalR Update Tidak Muncul di Monitor Browser
**What goes wrong:** Stat cards tidak berubah real-time walaupun worker progres di browser A.
**Why it happens:** HC belum join group `monitor-{batchKey}`, atau batchKey yang di-pass ke `JoinMonitor` tidak sesuai format `{title}|{category}|{yyyy-MM-dd}` yang di-set di `ViewBag.AssessmentBatchKey`.
**How to avoid:** Verifikasi badge hubStatusBadge bertuliskan "Connected" di browser B sebelum worker mulai exam. Jika tidak ada update, cek browser console untuk error SignalR.
**Warning signs:** Badge tetap "Connecting..." atau "Disconnected"; no update di stat cards.

### Pitfall 2: Token Lama Masih Diterima Setelah Regenerate
**What goes wrong:** Worker bisa masuk dengan token lama setelah HC melakukan regenerate.
**Why it happens:** `RegenerateToken` mengupdate semua sibling sessions (batch update). Jika ada cache browser atau client-side state yang menyimpan token lama, worker bisa bypass.
**How to avoid:** Verifikasi dengan mencoba submit token lama di halaman exam — harus muncul pesan error "Token tidak valid".
**Warning signs:** Worker berhasil mulai exam dengan token lama.

### Pitfall 3: Export Excel Membuka File Rusak
**What goes wrong:** File .xlsx yang diunduh tidak bisa dibuka di Excel.
**Why it happens:** Stream tidak di-flush dengan benar, atau `Content-Disposition` header salah.
**How to avoid:** Buka file hasil download di Excel — verifikasi sheet "Results" ada, header row ada, data row ada minimal 1 (untuk Rino yang completed).
**Warning signs:** Excel error "file format not supported" atau file corrupt.

### Pitfall 4: Analytics Chart Kosong Setelah Filter
**What goes wrong:** Chart tidak menampilkan data setelah filter diterapkan.
**Why it happens:** Seed data SEED-07 (completed assessment untuk Rino) mungkin tidak match filter yang dipilih (misalnya Bagian Rino tidak "Alkylation").
**How to avoid:** Sebelum test filter, verifikasi data apa yang di-seed dengan melihat analytics tanpa filter terlebih dahulu. Pastikan ada data yang muncul di state "no filter" sebelum tes kombinasi filter.
**Warning signs:** Semua chart kosong bahkan tanpa filter.

---

## Excel Export Validation Approach (D-03 — Claude's Discretion)

**Keputusan:** Gunakan **structural + spot-check data** approach.

Alasan: Full data match memerlukan akses DB langsung yang tidak praktis dalam UAT manual. Pure structural check (hanya cek header) terlalu lemah dan bisa melewatkan bug data mapping. Spot-check adalah middle ground yang realistis untuk verifikasi manual.

**Langkah validasi:**
1. Buka file Excel yang diunduh
2. Verifikasi sheet bernama "Results" ada
3. Verifikasi row 1: "Laporan Assessment"
4. Verifikasi row 2: "Judul" | nama assessment
5. Verifikasi headers kolom: No, Nama, NIP, Jumlah Soal, Status, Skor, Hasil, Waktu Selesai
6. Verifikasi minimal 1 data row untuk peserta Rino dengan Status "Completed", Skor terisi, Hasil "Pass" atau "Fail"
7. Verifikasi file dapat disimpan ulang tanpa error di Excel

---

## Seed Data yang Tersedia (dari Phase 241)

| Seed | Deskripsi | Relevansi untuk Phase 244 |
|------|-----------|--------------------------|
| SEED-03 | Assessment "OJT Proses Alkylation Q1-2026" untuk Rino + Iwan, token required | MON-01, MON-02, MON-03 — gunakan assessment ini |
| SEED-07 | 1 assessment completed dengan skor + sertifikat untuk Rino | MON-03 (export punya data), MON-04 (analytics punya data) |

Untuk MON-01 dan MON-02, assessment SEED-03 harus dalam status "Open" dan Rino harus mengerjakan ujian secara live (tidak menggunakan SEED-07 yang sudah completed).

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual UAT (browser-based) |
| Config file | Tidak ada — UAT manual sesuai Out of Scope di REQUIREMENTS.md |
| Quick run command | — |
| Full suite command | — |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MON-01 | Stat cards dan status per-user update real-time via SignalR | Manual (dual browser) | — (manual-only: requires live browser interaction) | N/A |
| MON-02 | Copy token, regenerate, verifikasi token lama invalid, force close, reset | Manual (sequential flow) | — (manual-only: requires live browser interaction) | N/A |
| MON-03 | Export Excel dapat dibuka dan berisi data peserta | Manual (spot-check) | — (manual-only: requires file download verification) | N/A |
| MON-04 | Analytics dashboard dengan cascading filter menampilkan data benar | Manual (filter kombinasi) | — (manual-only: requires visual chart verification) | N/A |

**Justifikasi manual-only:** REQUIREMENTS.md §Out of Scope secara eksplisit menyatakan "Automated browser testing: UAT dilakukan manual via browser, bukan Playwright/Selenium" dan "Performance/load testing: Scope terbatas pada functional correctness."

### Sampling Rate

- Per task: Verifikasi manual langsung di browser
- Per wave: Semua langkah dalam satu plan diselesaikan sebelum lanjut ke plan berikutnya
- Phase gate: Semua 4 requirements verified sebelum phase dinyatakan complete

### Wave 0 Gaps

Tidak ada — tidak ada test infrastructure yang perlu dibuat untuk fase UAT manual ini.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| ASP.NET Core app (running) | Semua MON-* | Harus running | .NET 8 | Start dengan `dotnet run` |
| Browser (2 instances) | MON-01, MON-02 | Tersedia | — | Gunakan 2 tab/window berbeda dengan session berbeda |
| Microsoft Excel / LibreOffice | MON-03 | Diasumsikan tersedia | — | Jika tidak ada, verifikasi file bisa dibuka di Google Sheets |

**Catatan penting:** Untuk dual-browser test (D-01, D-02), dua sesi login yang berbeda diperlukan. Bisa menggunakan: (1) dua browser berbeda (Chrome + Firefox), atau (2) satu browser normal + satu incognito/private window.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Polling manual (page refresh) | SignalR push | Phase sebelumnya | HC tidak perlu refresh halaman untuk melihat update status |
| Export manual (screenshot) | ClosedXML Excel download | Phase sebelumnya | Data terstruktur dan dapat dianalisis |

---

## Open Questions

1. **Apakah SEED-03 masih dalam status "Open" saat phase 244 dieksekusi?**
   - What we know: SEED-03 dibuat dengan status "Open" dan ExamWindowCloseDate yang belum expired (seed di phase 241)
   - What's unclear: Apakah window sudah expired tergantung kapan phase 244 dieksekusi
   - Recommendation: Verifikasi status assessment di halaman AssessmentMonitoring sebelum memulai MON-01. Jika Closed, mungkin perlu update ExamWindowCloseDate via AdminController atau reset status manually.

2. **Apakah ada data analytics selain SEED-07 untuk test MON-04?**
   - What we know: SEED-07 seed 1 completed assessment untuk Rino
   - What's unclear: Apakah 1 data point cukup untuk visualisasi chart (terutama trend yang butuh multi-month data)
   - Recommendation: Test MON-04 dengan "no filter" dulu — jika chart fail rate dan trend terlihat meski 1 data point, lanjutkan. Jika chart membutuhkan minimum 2+ data points untuk render, catat sebagai temuan UAT di FIX-01.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` lines 2151-2220 — RegenerateToken implementation verified
- `Controllers/AdminController.cs` lines 2222-2439 — AssessmentMonitoring + AssessmentMonitoringDetail verified
- `Controllers/AdminController.cs` lines 3016-3090+ — ExportAssessmentResults via ClosedXML verified
- `Controllers/CMPController.cs` lines 2049-2147 — AnalyticsDashboard + GetAnalyticsData verified
- `Hubs/AssessmentHub.cs` — JoinMonitor/LeaveMonitor groups verified
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — stat cards, token card, per-user table, SignalR JS integration verified
- `Views/CMP/AnalyticsDashboard.cshtml` — cascading filter UI verified
- `.planning/phases/244-uat-monitoring-analytics/244-CONTEXT.md` — locked decisions D-01 s/d D-04
- `.planning/REQUIREMENTS.md` — MON-01 s/d MON-04, Out of Scope (no automated testing)

### Secondary (MEDIUM confidence)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` JS section tidak dibaca lengkap — diasumsikan SignalR connection handler ada berdasarkan badge `id="hubStatusBadge"` yang tampil di view

### Tertiary (LOW confidence)
- Status "Open/Closed" dari SEED-03 saat eksekusi fase — belum dicek runtime, bergantung pada ExamWindowCloseDate seed

---

## Metadata

**Confidence breakdown:**
- Implementation inventory: HIGH — kode dibaca langsung dari source files
- UAT scenarios: HIGH — berdasarkan locked decisions D-01 s/d D-04 dari CONTEXT.md
- Seed data availability: MEDIUM — seed shipped di phase 241, status saat ini bergantung pada waktu eksekusi
- Analytics data sufficiency: LOW — hanya 1 completed record (SEED-07), belum diverifikasi apakah cukup untuk semua chart

**Research date:** 2026-03-24
**Valid until:** 2026-04-07 (14 hari — stable, tidak ada perubahan kode yang expected)
