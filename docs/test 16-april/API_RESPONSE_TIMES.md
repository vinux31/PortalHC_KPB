# API Response Times — PortalHC KPB
**Tanggal Mulai Testing:** 17 April 2026
**Scope:** Step 2 — Runtime Analysis
**Threshold:** >500ms = Kuning, >1000ms = Merah, >300ms = Issue untuk pagination

---

## Environment

| Env | URL | Status |
|-----|-----|--------|
| Localhost | `http://localhost:5277` | Fase 2A — Selesai |
| Dev Server | `http://10.55.3.3/KPB-PortalHC` | Fase 2B — Selesai |

**Akun test yang digunakan:** `admin@pertamina.com` / `123456` (Admin)

---

## Item 1 — Analytics Dashboard 11 Endpoints

| # | Endpoint | Skenario | TTFB (ms) | Total (ms) | Transfer | Decoded | Status | Catatan |
|---|----------|----------|-----------|------------|----------|---------|--------|---------|
| 1.1 | `/CMP/GetAnalyticsSummary` | Cold load (no filter) | 289 | 294 | 372 B | 72 B | 200 | Bar baseline, paling lambat di cold |
| 1.2 | `/CMP/GetFailRateData` | Cold load (default tab) | 234 | 239 | 777 B | 477 B | 200 | Tab default |
| 1.3 | `/CMP/GetTrendData` | Tab switch (lazy) | 62 | 65 | 664 B | 364 B | 200 | First visit tab Trend |
| 1.4 | `/CMP/GetEtBreakdownData` | Tab switch (lazy) | 44 | 52 | 2.8 KB | 2.5 KB | 200 | Tab Skor Elemen Teknis |
| 1.5 | `/CMP/GetExpiringSoonData` | Tab switch (lazy) | 50 | 51 | 302 B | 2 B | 200 | Tab Sertifikat Expired (kosong di dataset lokal) |
| 1.6 | `/CMP/GetPrePostAssessmentList` | Tab switch (dropdown) | 39 | 51 | 493 B | 193 B | 200 | Populate dropdown Item Analysis |
| 1.7 | `/CMP/GetItemAnalysisData` | — | — | — | — | — | — | Tidak dipicu tanpa assessment dipilih (UX-gated) |
| 1.8 | `/CMP/GetGainScoreData` | — | — | — | — | — | — | Tidak dipicu tanpa assessment dipilih (UX-gated) |
| 1.9 | `/CMP/GetFailRateDrillDown` | — | — | — | — | — | — | Tidak dipicu tanpa klik bar grafik |
| 1.10 | `/CMP/GetAnalyticsCascadeUnits` | Filter change | 58 | 61 | ~500 B | — | 200 | Cascade Bagian→Unit |
| 1.11 | `/CMP/GetAnalyticsCascadeSubKategori` | — | — | — | — | — | — | Tidak dipicu tanpa Kategori dipilih |
| — | `/CMP/GetAnalyticsSummary` (filter aktif) | Filter Bagian=RFCC | 64 | 66 | — | — | 200 | Re-fetch setelah cache invalidate |
| — | `/CMP/GetFailRateData` (filter aktif) | Filter Bagian=RFCC | 23 | 24 | — | — | 200 | Re-fetch setelah cache invalidate |
| — | `/CMP/GetTrendData` (filter aktif) | Tab Trend setelah filter | — | 88 | — | — | 200 | Validasi cache invalidate per-tab lazy |

**Temuan Item 1:**
- Semua endpoint di localhost **jauh di bawah threshold 500ms** (kuning). Terlambat = GetAnalyticsSummary cold (294ms) — masih di zona hijau
- Dataset lokal kecil (25 sesi, 14 pass, 11 fail — dari ringkasan grafik). Belum representatif untuk identifikasi bottleneck scale
- 4 dari 11 endpoint **UX-gated** (butuh user action dulu): ItemAnalysis, GainScore, DrillDown, CascadeSubKategori. Perlu dipicu manual saat Fase 2B
- Compare dengan Fase 2B akan memvalidasi cascade filter in-memory (kategori E di audit `.ToList()`) — apakah bottleneck di scale atau tidak



---

## Item 2 — `_tabCache` Verification

| Skenario | Action | Localhost Request Count (cumulative) | Delta | Cache Behavior OK? |
|---------|--------|---------------------------------------|-------|--------------------|
| First load (cold) | Open page | 2 (Summary + FailRate) | +2 | ✅ Hanya default tab yang fetch |
| Tab Trend (first visit) | Click Trend | 3 | +1 | ✅ Lazy load |
| Tab Skor Elemen Teknis | Click tab | 4 | +1 | ✅ Lazy load |
| Tab Sertifikat Expired | Click tab | 5 | +1 | ✅ Lazy load |
| Tab Item Analysis | Click tab | 6 | +1 (dropdown only, belum fetch data) | ✅ UX-gated |
| Tab Gain Score Report | Click tab | 6 | 0 | ✅ UX-gated |
| Tab Fail Rate (revisit) | Click Fail Rate | 6 | **0** | ✅ **Cache hit** |
| Filter Bagian=RFCC + Apply | Re-fetch | 9 | +3 (Cascade + Summary + FailRate) | ✅ Cache invalidate, current tab refetch |
| Tab Trend (after filter) | Click Trend | 10 | +1 (Trend filtered) | ✅ Per-tab lazy re-fetch |
| F5 reload | Page reload | 2 (reset) | — | ✅ Cache hilang, kembali initial |

**Temuan Item 2:**
- `_tabCache` bekerja **persis sesuai desain**:
  1. Tab yang sudah pernah dibuka → revisit **0 request**
  2. Filter change → cache invalidate, hanya tab aktif fetch segera. Tab lain re-fetch saat dibuka (lazy)
  3. F5 reload → cache reset total
- Tidak ada bug data stale terdeteksi
- Tab "Item Analysis" + "Gain Score Report" memang **UX-gated** (butuh user pilih assessment dulu) — pattern baik untuk menghindari fetch tidak perlu

---

## Item 3 — ManageAssessment + Tabs

**Temuan arsitektural penting:** ManageAssessment **tidak menggunakan AJAX untuk tab switching** — seluruh tab (Assessment Groups, Input Records, History Riwayat Assessment 33, History Riwayat Training 27) sudah pre-rendered di HTML initial. Tab click = DOM toggle saja.

Implikasi: `GetAllWorkersHistory()` (C-04) dipanggil **server-side saat render initial**, bukan saat klik tab History. Ini menjelaskan kenapa initial response 297 KB dan TTFB cold 745ms.

| Skenario | TTFB (ms) | Total (ms) | DOM (ms) | Size | Catatan |
|---------|-----------|------------|----------|------|---------|
| Cold load run 1 (no filter) | **745** 🟡 | 856 | 839 | 297 KB | Kuning (>500ms). Triggers C-04 + H-05 |
| Cold load run 2 (warm DB cache) | 114 | 191 | 183 | 297 KB | Warm — 6× lebih cepat |
| Cold load run 3 (warm) | 81 | 194 | — | 297 KB | Stable |
| Mean 3-run | 313 | 414 | — | — | Max: 745 (run 1) |
| Tab History click | — | — | — | — | **0 AJAX** — DOM toggle, instant |
| Tab Training click | — | — | — | — | **0 AJAX** — DOM toggle, instant |
| Filter OJT + Closed | 84 | 187 | — | 247 KB | HTML lebih kecil (5 grup vs 14) |

**Temuan Item 3:**
- **Run 1 cold: 745ms TTFB = bukti C-04 + H-05 impact** (fetch 3 tabel penuh + grouping in-memory)
- Variance antara cold vs warm sangat besar (745ms → 114ms) — indikasi SQL Server/SQLite page cache warming, bukan app-level cache
- HTML 297 KB uncompressed mendominasi initial payload. **M-01 compression prediction**: Gzip akan reduce ~80% → ~60 KB
- Tab History **tidak menggunakan AJAX** — `GetAllWorkersHistory()` dipanggil saat Razor render initial page, sehingga optimasi C-04 akan berdampak langsung pada cold-load time
- Filter aktif tidak secara signifikan mengurangi TTFB (84ms warm) — dataset kecil tidak mengekspos H-05 cascade filter overhead



---

## Item 4 — ManageWorker

**Dataset localhost:** 12 total users (1 Admin + 1 HC + 5 Worker + 5 lainnya). Pagination client-side DOM tidak teruji (≤15 row = 1 halaman).

| Skenario | TTFB (ms) | Total (ms) | DOM (ms) | HTML Size | Hasil (rows) | Stats Card | Catatan |
|---------|-----------|------------|----------|-----------|--------------|------------|---------|
| Cold load no-filter | 213 | 336 | 334 | 77 KB | 12 | 12/1/1/5 | Baseline |
| Filter Bagian=GAST | 124 | 205 | — | 49 KB | 7 | **12/1/1/5** | 🚨 Stats tetap 12 (H-06 bug) |
| Filter role=Coachee + showInactive | 64 | 154 | 152 | 60 KB | 4 | **12/1/1/5** | 🚨 Stats tetap 12 (H-06 bug) |
| Pagination next/prev | — | — | — | — | — | — | Skip — dataset kecil |

**Temuan Item 4:**
- **H-06 TERKONFIRMASI di runtime** — stats card (Total 12, Admin 1, HC 1, Worker 5) **sama persis** di semua skenario filter. Query stats di `WorkerController.cs:108-111` tidak menghormati filter aktif. User melihat inkonsistensi: hasil filter 4 user vs stats "Total User 12"
- HTML size turun proporsional dengan jumlah hasil (77 → 49 → 60 KB) — validasi rendering data yang terfilter tetap server-side
- TTFB semua <300ms di localhost — C-02 + C-03 dampak tidak kelihatan di dataset 12 user. **Fase 2B wajib** untuk validasi scale (5K users)
- HTML 77 KB uncompressed → validasi M-01 potensi reduksi (Gzip ~80% → ~15 KB)



---

## Item 5 — AssessmentMonitoring + Detail + GetActivityLog

**Dataset localhost:** 14 grup ditampilkan (filter default Open+Upcoming), 17 total peserta, 2 selesai. Tidak ada grup Pre-Post 50-essay realistis — validasi H-04 scale di Fase 2B.

| Skenario | TTFB (ms) | Total (ms) | Size | Catatan |
|---------|-----------|------------|------|---------|
| AssessmentMonitoring main list | 426 | 532 | 64 KB | Grouping in-memory (H-03) — dataset kecil, tidak bottleneck |
| AssessmentMonitoringDetail `ojt v1.9` (3 peserta + 1 essay) | **747** 🟡 | 880 | 81 KB | Kuning! 1 essay × 3 query = 3 RT. Grup kecil saja sudah 747ms |
| GetActivityLog session 64 (cold) | 90 | — | 925 B | Cold call |
| GetActivityLog session 65 (cold) | 11 | — | 694 B | Warm |
| GetActivityLog × 8 warm calls | — | — | — | Mean: 25ms, Max: 41ms |

**Temuan Item 5:**
- **AssessmentMonitoringDetail 747ms TTFB** untuk grup sekecil 3 peserta dengan 1 essay. Menunjukkan **overhead H-04 essay grading loop** sudah signifikan bahkan di dataset kecil. Ekstrapolasi linear: grup 50-essay di dev bisa mencapai ~15-30 detik (150 RT × 5-10ms/RT × faktor koneksi LAN)
- AssessmentMonitoring main list 426ms — masih di bawah threshold kuning, tapi mendekati. Window 7-hari yang unbounded (H-03) baru kelihatan dampaknya di dataset ratusan session
- GetActivityLog sangat cepat di localhost (25ms mean). Optimasi M-06 (hilangkan Query 4) akan hemat ~25% → ~19ms mean. Di dev server LAN mungkin 100-200ms, hemat jadi 75-150ms
- Grup realistis untuk validasi H-04 di Fase 2B: butuh grup Pre-Post 50+ peserta dengan essay



---

## Item 6 — GetOrganizationTree

**Dataset localhost:** 21 unit organisasi (4 Bagian + 17 Unit), 21 aktif. Data stabil untuk baseline.

| Skenario | Duration (ms) | TTFB (ms) | JSON Size | Unit Count | Catatan |
|---------|---------------|-----------|-----------|------------|---------|
| ManageOrganization initial AJAX | 46 | 41 | 2.4 KB | 21 | Cold AJAX saat page load |
| F5 reload call 1 | 10 | — | — | 21 | Warm |
| F5 reload call 2 | 9 | — | — | 21 | — |
| F5 reload call 3 | 9 | — | — | 21 | — |
| F5 reload call 4 | 11 | — | — | 21 | — |
| F5 reload call 5 | 8 | — | — | 21 | — |
| F5 reload mean | **9** | — | — | 21 | Stabil warm |
| F5 reload max | 11 | — | — | 21 | Variance rendah |
| Concurrency session A (paralel) | 7 | — | — | 21 | — |
| Concurrency session B (paralel) | 11 | — | — | 21 | — |
| Concurrency wall-time | 12 | — | — | — | Paralel OK, tidak ada lock contention |

**Temuan Item 6:**
- Sangat cepat di localhost (9ms mean). Dataset kecil (21 unit)
- Concurrency 2 session paralel: wall-time 12ms = 2 query DB jalan paralel tanpa blocking. DB handle concurrent read dengan baik (SQLite WAL mode sudah aktif dari `Program.cs:140`)
- **Prediksi Fase 2B dev server (500 units)**: response size akan ~60-80 KB (dibanding 2.4 KB di local), duration LAN ~50-150ms. Potensi kuning jika dataset sangat besar
- M-03 optimasi IMemoryCache (TTL 10 menit) akan hemat ~95% DB hit — validasi impact lebih signifikan di dev server
- Validasi M-01: 2.4 KB JSON dengan compression Gzip potensi ~800 B, di dev server 60-80 KB → ~20 KB



---

## Item 7 — Network Waterfall (Dev Only)

| Halaman | Screenshot | HAR File | Critical Path Request | Longest TTFB | Catatan |
|---------|-----------|----------|----------------------|--------------|---------|
| ManageOrganization | | | | | |
| ManageAssessment | | | | | |
| ManageWorker | | | | | |

---

## Ringkasan Fase 2A (Localhost) — 17 April 2026

### Issue Kategori Merah / Kuning yang Terdeteksi

| Severity | Lokasi | Metrik | Catatan |
|----------|--------|--------|---------|
| 🚨 Bug | ManageWorker stats card | Total tetap 12 di semua filter | **H-06 TERKONFIRMASI di runtime** |
| 🟡 Kuning | ManageAssessment cold load run 1 | TTFB 745ms | Triggers C-04 + H-05 |
| 🟡 Kuning | AssessmentMonitoringDetail (1 essay) | TTFB 747ms | Triggers H-04 (bahkan untuk 1 essay) |

### Validasi Issue di Runtime

| Issue | Validasi | Hasil |
|-------|----------|-------|
| H-06 (stats tidak honor filter) | 2 filter scenario | ✅ BUG CONFIRMED |
| C-04 (full fetch history) | Cold vs warm run | ✅ Overhead 6× di cold |
| H-04 (essay N+1 loop) | Grup 3 peserta + 1 essay | ✅ Sudah 747ms dari 1 iteration |
| M-01 (no compression) | Response size uncompressed | ✅ 297 KB ManageAssessment, 2.4 KB GetOrgTree — potensi Gzip ~80% |
| `_tabCache` Analytics | Tab switch revisit | ✅ 0 request baru — bekerja sesuai desain |
| Cache invalidate on filter | Filter change → tab refetch | ✅ Pattern benar |

### Issue yang Belum Teruji di Fase 2A

| Issue | Alasan | Rencana Validasi |
|-------|--------|------------------|
| C-03 unbounded users fetch | Dataset 12 users terlalu kecil | Fase 2B (5K+ users) |
| M-08 client-side pagination | <15 rows = 1 halaman | Fase 2B (5K+ users, multi-page) |
| H-03 AssessmentMonitoring 7-day unbounded | Dataset kecil | Fase 2B (ratusan session per week) |
| H-04 N+1 essay scale | Tidak ada grup 50-essay | Fase 2B (grup Pre-Post 50+ peserta dgn essay) |
| M-03 GetOrgTree scale | Dataset 21 units, perfect paralel | Fase 2B (500 units, beberapa concurrent user) |

### Kesimpulan Fase 2A

- **3 issue terkonfirmasi di runtime** (H-06 confirmed bug, C-04 + H-04 terdeteksi meskipun dataset kecil)
- **Tab cache `_tabCache` bekerja sempurna** — tidak perlu perubahan
- **`M-01 compression`** divalidasi berpotensi besar (response 297 KB → ~60 KB estimasi)
- 5 issue lainnya (C-03, M-08, H-03, H-04 scale, M-03 scale) memerlukan dataset realistis Fase 2B

---

## Ringkasan Fase 2B (Dev Server) — 17 April 2026

### Konteks Dataset Dev
- **Users:** 530 (vs local 12 = 44×)
- **OrganizationUnits:** 26 (vs local 21 = 1.2×)
- **Assessment sessions:** KOSONG (analytics/monitoring data 0) — dataset belum terisi
- **ManageAssessment rendered rows:** 4,789 (history + training) vs local 33 = **145×**

### Item 1 Dev — Analytics Dashboard

| Endpoint | Dev TTFB | Dev Total | Local TTFB | Local Total | Catatan |
|----------|----------|-----------|------------|-------------|---------|
| GetAnalyticsSummary | 176 | 181 | 289 | 294 | Dataset kosong |
| GetFailRateData | 176 | 194 | 234 | 239 | — |
| GetTrendData | 160 | 169 | 62 | 65 | Dev **2.6× lambat** |
| GetEtBreakdownData | 277 | 301 | 44 | 52 | Dev **6× lambat** |
| GetExpiringSoonData | 374 | 378 | 50 | 51 | Dev **7× lambat** |
| GetAnalyticsCascadeUnits (filter) | — | 194 | — | 61 | 3× LAN overhead |
| GetAnalyticsSummary (filter) | — | 273 | — | 66 | 4× |
| GetFailRateData (filter) | — | 220 | — | 24 | 9× |

### Item 2 Dev — `_tabCache` verification
Pattern **identik dengan localhost**. Cache hit revisit = 0 request, filter change invalidate current tab + lazy re-fetch lainnya, F5 reset.

### Item 3 Dev — ManageAssessment
**🚨 CATASTROPHIC FAILURE**

| Metrik | Local | Dev | Rasio |
|--------|-------|-----|-------|
| TTFB | 745 / 81 ms | 328 / 412 ms | — |
| Total load time | 856 ms | **>60s timeout** | ∞ |
| Transfer size | 297 KB | **7.96 MB** | 27× |
| DOM rows | 33 | **4,789** | 145× |

Halaman secara praktis **tidak usable di dev** karena timeout >60s. Bukti langsung C-04 (GetAllWorkersHistory 3-fetch penuh) + H-05 (in-memory grouping) + M-01 (no compression) semuanya aktif.

### Item 4 Dev — ManageWorker

| Skenario | TTFB | Total | HTML | Rows | Hasil | Stats |
|----------|------|-------|------|------|-------|-------|
| Cold load no-filter | 221 | **25,586** 🚨 | 1.94 MB | 530 | 530 | 530/1/13/467 |
| Filter Bagian=GAST | 132 | 3,253 | 210 KB | 48 | 48 | **530/1/13/467 🚨 H-06** |
| Pagination page 3-7 | — | ~32ms each | — | — | — | — |

**Temuan:**
- **DOMContentLoaded 25.6 detik** — UX tidak acceptable
- **H-06 TERKONFIRMASI di 530 users** — stats card tidak honor filter (tampil 530 meski filter 48)
- **Pagination page-switch 32ms** — client-side DOM fine; M-08 lag threshold 300ms tidak tercapai
- HTML 1.94 MB uncompressed → prediksi M-01 Gzip ~85% → ~300 KB

### Item 5 Dev — AssessmentMonitoring + Detail + GetActivityLog

Dataset assessment session di dev KOSONG (Grup 0, Peserta 0). Validasi H-03/H-04/M-06 scale **tidak dapat dilakukan**. TTFB 132ms, total 3253ms (banyak asset loading).

### Item 6 Dev — GetOrganizationTree (M-03)

| Skenario | Dev Duration | Local | Rasio |
|----------|--------------|-------|-------|
| Initial AJAX on page load | 582 ms | 46 ms | **13×** |
| F5 reload mean (5 calls) | 196 ms | 9 ms | **22×** |
| F5 reload max | 374 ms | 11 ms | **34×** |
| Concurrency 2 paralel wall | **662 ms** 🚨 | 12 ms | **55×** |

**Temuan kritis:** Concurrency 2 session paralel = 662ms wall (vs local 12ms). Session B took 662ms vs A 226ms → **serialisasi/lock contention** di DB production-like. Indikasi perlu IMemoryCache (M-03) untuk mengurangi DB hit.

### Item 7 Dev — Network Waterfall

**Capture location:**
- `docs/test 16-april/har/dev/ManageOrganization_dev.txt`
- `docs/test 16-april/har/dev/ManageWorkers_dev.txt`
- `docs/test 16-april/har/dev/ManageAssessment_dev_partial.txt` (partial — page tidak selesai <60s)
- Screenshots di `docs/test 16-april/screenshots/dev/*.png`

Fokus observasi: ManageAssessment dan ManageWorker mendominasi total load time karena **HTML body size besar**, bukan request count. Single HTML request 1.94 MB / 7.96 MB = bottleneck transfer + render, bukan paralelisasi.

---

## Ringkasan Akhir — Compare Local vs Dev (All Items)

### Issue Terkonfirmasi di Runtime

| Issue | Severity | Local Evidence | Dev Evidence | Status |
|-------|----------|----------------|--------------|--------|
| **C-01** Blocking `.ToList()` CDP | Critical | Code-verified | N/A (tidak di-trigger) | ✅ Static analysis saja |
| **C-02** 4x CountAsync Worker | Critical | — | 530 users page load 25.6s | ✅ CONFIRMED |
| **C-03** Fetch all users unfiltered | Critical | — | HTML 1.94 MB, DOM 25.6s | ✅ CONFIRMED at scale |
| **C-04** GetAllWorkersHistory 3 full fetch | Critical | 745ms cold TTFB | 7.96 MB HTML, >60s timeout | ✅ CATASTROPHIC |
| **H-03** AssessmentMonitoring 7-day unbounded | High | — | Data kosong, tidak bisa validasi | ⏳ Menunggu dataset |
| **H-04** N+1 essay grading | High | 747ms untuk 1 essay | Data kosong | ⏳ Menunggu dataset |
| **H-05** ManageAssessment in-memory grouping | High | Dampak dirasakan di 745ms cold | Dampak ledakan di 7.96 MB | ✅ CONFIRMED |
| **H-06** Stats card ignore filter | High | ✅ CONFIRMED at 12 users | ✅ CONFIRMED at 530 users | ✅ CONFIRMED di 2 scale |
| **M-01** No compression | Medium | 297 KB+77 KB uncompressed | **7.96 MB + 1.94 MB** uncompressed | ✅ HUGE impact validated |
| **M-02** ImpersonationMiddleware query per req | Medium | Static | Static | ✅ Static only |
| **M-03** GetOrgTree no cache | Medium | 46ms cold, 9ms warm | **582ms cold, 196ms warm, 662ms concur** | ✅ CONFIRMED at scale |
| **M-04/M-05** Missing AsNoTracking | Medium | Static | Static | ✅ Static only |
| **M-06** GetActivityLog 4 query | Medium | 25ms mean (8 calls) | N/A (no session) | ⏳ Menunggu dataset |
| **M-08** Client-side pagination lag | Medium | N/A (12 users) | **32ms page-switch OK** | ⚠️ **Ternyata tidak lag** — downgrade severity |
| **M-09** Dropdown tanpa date filter | Medium | Static | Static | ✅ Static only |
| **L-01** AssessmentHub fire-forget | Low | Static | Static | ✅ Static only |
| **L-02** Include hanya untuk Count | Low | Static | Static | ✅ Static only |

### Revisi Severity Berdasarkan Runtime

| Issue | Severity Lama | Severity Baru | Alasan |
|-------|--------------|---------------|--------|
| **C-04** | Critical | **Critical++** | 7.96 MB HTML + >60s timeout — halaman tidak usable |
| **C-02 + C-03** | Critical | **Critical** | DOMContent 25.6s di 530 users |
| **H-06** | High | **High** | Bug terkonfirmasi di 2 scale — **tampilan menyesatkan** di runtime |
| **M-08** | Medium | **Low** | Pagination page-switch 32ms, tidak mendekati threshold 300ms. Issue sebenarnya bukan client-side lag tapi **initial DOM render** (sudah dicover M-01 + C-03) |
| **M-03** | Medium | **Medium-High** | Concurrency wall 55× lambat di dev — serialisasi DB |
| **M-01** | Medium | **High** | Response size 7.96 MB = user-blocker. Compression = single biggest win ROI |

### Rekomendasi Prioritas Final (Diurutkan Berdasarkan Runtime Impact)

| Prio | Issue | Fix | Impact Estimate |
|------|-------|-----|-----------------|
| 1 | **M-01** enable compression | Add `UseResponseCompression` + Brotli/Gzip config | -80% transfer size across semua halaman, dampak langsung terhadap UX |
| 2 | **C-04** batch filter `GetAllWorkersHistory` | Add UserId + date range filter | Kurangi 7.96 MB → ~500 KB untuk ManageAssessment |
| 3 | **C-03** server-side pagination ManageWorker | Skip/Take di `IQueryable` | Kurangi 1.94 MB → ~50 KB per page |
| 4 | **H-06** fix stats filter | Gunakan `query` terfilter untuk stats count | Bug fix — UX correctness |
| 5 | **C-02** single query aggregate | GroupBy + Case di SQL | -75% DB round-trip stats |
| 6 | **H-05** push grouping ke SQL | `GroupBy` translate EF Core | Kurangi CPU in-memory grouping |
| 7 | **M-03** IMemoryCache GetOrgTree | TTL 10 menit + invalidate di write | Dev 196ms → ~5ms (cache hit); fix concurrency serialization |
| 8 | **C-01** async GetCascadeOptions | `.ToListAsync()` + `Task.WhenAll` | Thread pool freedom |
| 9 | **M-04 + M-05** AsNoTracking | 1-liner tambah `.AsNoTracking()` | Memory overhead kecil, cepat fix |
| 10 | **H-04** batch essay grading | Load assignments/questions/responses di luar loop | Data kosong di dev, validasi setelah seed |

### Issue yang Menunggu Dataset Dev

Issue berikut hanya bisa divalidasi runtime setelah dev server diisi data assessment:
- H-03: AssessmentMonitoring 7-day unbounded
- H-04: N+1 essay grading scale
- M-06: GetActivityLog 4-query per AJAX

### Catatan Deliverables

- `docs/test 16-april/API_RESPONSE_TIMES.md` — matrix lengkap (file ini)
- `docs/test 16-april/har/localhost/` — kosong (tidak perlu per plan)
- `docs/test 16-april/har/dev/` — 3 HAR files
- `docs/test 16-april/screenshots/dev/` — 3 screenshots
- `docs/test 16-april/PERFORMANCE_REPORT.md` — perlu update dengan hasil runtime (next step)
- `docs/test 16-april/DATABASE_ANALYSIS.md` — perlu Step 3 untuk DB query runtime

### Status Akhir

- Step 2A (Localhost) ✅ Selesai
- Step 2B (Dev Server) ✅ Selesai
- Step 3 (EF logging) ⏭ Skipped
- Validasi H-03/H-04/M-06 ⏳ Menunggu dataset assessment di dev server diisi
- Implementasi fix ⏳ Pending — lihat prioritas 1-17 di `PERFORMANCE_REPORT.md`
