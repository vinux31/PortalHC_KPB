# Step 2 — Runtime Analysis Discussion
**Tanggal:** 17 April 2026
**Status:** Discussion (belum dieksekusi)

Dokumen ini mencatat keputusan desain untuk Step 2 sebelum dieksekusi di sesi terpisah.

## Environment
| Environment | URL | Tujuan |
|-------------|-----|--------|
| Localhost | `http://localhost:5277` | Verifikasi fungsional + kontrol penuh + EF logging |
| Dev server | `http://10.55.3.3/KPB-PortalHC` | Metrik scale dengan dataset realistis |

## Keputusan Umum

- **Format output:** isi matrix ke `API_RESPONSE_TIMES.md`. Untuk network waterfall (item 7) → screenshot + export HAR
- **Threshold severity:** `>500ms = Kuning (perhatian)`, `>1000ms = Merah (bottleneck)`
- **Compare mode:** satu tabel yang menyandingkan Localhost vs Dev server per endpoint
- **Read-only:** dev server dilarang CRUD, hindari jam sibuk
- **Metrik wajib per request:** TTFB, total duration, transfer size (Content-Length header), status code
- **Metrik yang TIDAK tersedia:** `X-Response-Time` header — aplikasi tidak expose header ini (tidak ada middleware di `Program.cs`). Skip dari metrik
- **Tujuan per fase:**
  - Fase 2A (Localhost) = **baseline fungsional** (verifikasi cache behavior, alur request, correctness). Tidak memvalidasi issue scale
  - Fase 2B (Dev) = **validasi scale impact** — compare vs Fase 2A untuk membuktikan issue C-03/C-04/H-03/H-04/M-08 hanya kelihatan di dataset besar
- **Estimasi durasi:** Fase 2A ~30-45 menit, Fase 2B ~60-90 menit

---

## Item 1 — Analytics Dashboard (11 endpoints + tab cache)

**Target file controller:** `CMPController.cs`
**Konteks:** Sudah `AsNoTracking()` + async di Step 1D. Cascade filter Bagian/Unit/Status di memory masuk kategori non-blocking tapi boros alokasi.

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Scope endpoint | Catat **seluruh 11 data endpoint** (baseline). Endpoint dengan pattern identik ditandai di kolom catatan |
| Skenario pemicu | **Cold load** (page refresh → semua fetch) + **Tab switch** (verifikasi `_tabCache` hit/miss) |
| Kondisi filter | **Default (no filter)** + **1 skenario filter aktif** (contoh: Bagian=X, Unit=Y) untuk menguji overhead cascade |
| Metrik | TTFB + total duration + transfer size + status code (X-Response-Time tidak tersedia — skip) |
| Threshold | >500ms kuning, >1000ms merah |
| Compare mode | Satu tabel berdampingan Localhost vs Dev |

### Verifikasi khusus
- Tab switch ke tab yang sudah pernah dibuka: response time harus ~0ms (cache hit). Jika masih fetch, `_tabCache` bermasalah
- Filter aktif cascade: ukur selisih response time dengan no-filter — validasi apakah cascade filter in-memory mempengaruhi user-perceived latency

---

## Item 2 — Tab Switching (`_tabCache` verification)

**Target file:** `wwwroot/js/analyticsDashboard.js` (`_tabCache` object)
**Konteks:** Ditemukan saat Step 1 awal — lazy load per tab, first visit fetch, subsequent visit restore dari cache.

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Scope environment | **Localhost + Dev server** (verifikasi konsisten di 2 env) |
| Skenario cache test | (1) Tab A → B → A basic hit/miss, (2) Filter change reset cache (cek data stale), (3) Page reload reset cache (F5 sanity check). **Skip** long-idle TTL |
| Metrik | TTFB + total duration (per tab, per skenario) |

### Verifikasi khusus
- Tab switch ke tab yang sudah pernah dibuka: Network tab harus menunjukkan **0 request baru** (cache hit) — meski tidak dihitung sebagai metrik formal, ini bukti visual
- Filter change → kembali ke tab yang sebelumnya cached: harus trigger fetch baru (cache invalidate). Jika tidak → bug data stale
- F5 reload: cache harus hilang (ekspektasi normal untuk in-memory JS state)

---

## Item 3 — ManageAssessment (H-05, M-04, M-09, C-04)

**Target:** `AssessmentAdminController.ManageAssessment` + tab History/Training
**Konteks:** Halaman dengan grouping in-memory (H-05), missing AsNoTracking (M-04), dropdown full-scan (M-09). Tab History trigger `GetAllWorkersHistory()` yang kena C-04.

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Skenario yang direkam | (1) Cold load tab Assessment, (2) Switch tab History, (3) Switch tab Training, (4) Apply filter + search |
| Scope environment | **Localhost + Dev server** |
| Tab History khusus | **Ukur 3x + catat rata-rata** (karena C-04 paling berat, variansi penting) |

### Verifikasi khusus
- Cold load: bandingkan ukuran response Localhost vs Dev (validasi dampak dataset pada H-05 grouping)
- Tab History 3x repetition: hitung mean + max. Jika max >> mean → indikasi cache warming atau lock contention
- Filter + search: validasi apakah response time naik proporsional (indikasi cascade filter di-memory memakan CPU)

---

## Item 4 — ManageWorker (C-02, C-03, H-06, M-08)

**Target:** `WorkerController.Index` + `ManageWorkers.cshtml`
**Konteks:** Halaman dengan 4x CountAsync, fetch-all users + UserRoles unfiltered, client-side pagination DOM. Issue paling sensitif ke scale dataset.

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Skenario yang direkam | (1) Cold load no-filter (baseline), (2) Filter section, (3) Filter role + showInactive, (+) Test pagination next/prev karena threshold ditentukan |
| Scope environment | **Dev server WAJIB** (issue scale hanya kelihatan di sini) + Localhost opsional (baseline perbandingan) |
| Metrik M-08 | (1) Initial DOMContentLoaded, (2) Time-to-next-page setelah klik pagination, (3) Initial HTML response size (validasi M-01 compression impact) |
| Threshold pagination lag | **>300ms = issue** (standar UX good) |

### Verifikasi khusus
- H-06: cek apakah stats card (TotalUsers/Admin/HC/Worker) berubah saat filter diaktifkan. Kalau tetap = bug terkonfirmasi di runtime
- Response size: bandingkan uncompressed (current) vs hasil uji setelah M-01 diterapkan (nanti di Step implementation)
- DOMContentLoaded di Dev: jika >3s pada 5K users → konfirmasi M-08 butuh server-side pagination
- Catat jumlah users actual di dev (berapa ribu) untuk konteks metrik

---

## Item 5 — AssessmentMonitoring (H-03, H-04, M-05, M-06, L-02)

**Target:** `AssessmentAdminController.AssessmentMonitoring` + `AssessmentMonitoringDetail` + `GetActivityLog`
**Konteks:** Halaman dengan unbounded 7-day fetch (H-03), N+1 essay grading (H-04), 4-query AJAX GetActivityLog (M-06).

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Skenario yang direkam | (1) Cold load main list, (2) MonitoringDetail Pre-Post 50 essay, (3) MonitoringDetail Standard no-essay (baseline), (4) GetActivityLog AJAX |
| Scope environment | **Dev server WAJIB + Localhost baseline** |
| H-04 repeat | **3x, catat mean + max** |
| GetActivityLog | **5-10 session berbeda berturut-turut** (simulasi HC browsing realistis) |

### Verifikasi khusus
- H-03: catat ukuran response JSON + HTML. Validasi apakah 7-day window real di dev berapa ratus/ribu sessions
- H-04 essay vs standard detail: selisih response time = overhead N+1 loop. Hitung rasio (standard / pre-post-essay) untuk membuktikan impact
- H-04 scaling: kalau di dev ada grup dengan N peserta, cek apakah wall-time scales linear dengan N (bukti N+1)
- M-06 5-10 calls: hitung rata-rata per call. Rekomendasi optimasi (hilangkan Query 4) = hemat ~25% dari rata-rata ini
- Catat versi browser + network condition (LAN/VPN) untuk konteks

---

## Item 6 — GetOrganizationTree (M-03)

**Target:** `OrganizationController.GetOrganizationTree` (AJAX)
**Konteks:** Endpoint AJAX tanpa cache/AsNoTracking. Dipanggil oleh beberapa halaman (tree editor, dropdown unit).

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Skenario trigger | (1) ManageOrganization tree editor, (2) Dropdown unit di picker Worker/Assessment, (3) F5 refresh berulang 5x, (4) Concurrency test: 2 BrowserContext paralel (simulasi 2 session akses bersamaan) |
| Scope environment | **Localhost + Dev server** (konteks jumlah unit berbeda) |
| Metrik | TTFB + total duration, transfer size JSON (validasi M-01), jumlah units returned |

### Verifikasi khusus
- F5 5x: ekspektasi response time konstan (no cache). Jika ada variance besar → indikasi DB cache SQL Server/warming
- 2 user concurrency: ekspektasi DB hit paralel, response time sama per user. Jika salah satu melambat → lock contention
- Ukuran JSON: bandingkan dengan estimasi di Step 1D (30-150 KB uncompressed). Validasi prediksi
- Catat jumlah unit di dev vs localhost untuk konteks

---

## Item 7 — Network Waterfall (ManageOrganization + expansion)

**Target utama:** `/Admin/ManageOrganization`
**Target tambahan:** `/Admin/ManageAssessment`, `/Admin/ManageWorkers`
**Konteks:** Analisis flow request — identifikasi critical path dan blocking resource.

### Keputusan

| Pertanyaan | Keputusan |
|-----------|-----------|
| Cara capture | (1) Screenshot DevTools Network full waterfall, (2) Export HAR file (offline analysis) |
| Fokus observasi | (1) Urutan request + critical path blocking, (2) TTFB per request |
| Scope environment | **Dev server saja** (network realistis) |
| Halaman tambahan | ManageAssessment + ManageWorker (selain ManageOrganization plan awal) |

### Verifikasi khusus
- Critical path: identifikasi request paling lama yang memblokir DOMContentLoaded
- Waterfall ManageOrganization: ekspektasi lihat `GetOrganizationTree` call + static assets
- Waterfall ManageAssessment: lihat sequence tab loader (History + Training) — apakah paralel atau sequential
- Waterfall ManageWorker: validasi sequence 4 CountAsync + users fetch (apakah browser atau server yang men-serialize)
- Simpan HAR file di `docs/test 16-april/har/` (buat subfolder) dengan nama: `<halaman>_<env>_<timestamp>.har`
- Screenshot di `docs/test 16-april/screenshots/` dengan nama serupa

---

## Ringkasan Keputusan Step 2

| Item | Endpoint/Halaman | Skenario | Env | Metrik Utama |
|------|------------------|---------|-----|-------------|
| 1 | Analytics Dashboard 11 endpoints | Cold + tab switch + filter aktif | Local + Dev | TTFB + duration + size + status |
| 2 | `_tabCache` verifikasi | A→B→A + filter change + F5 | Local + Dev | TTFB + duration |
| 3 | ManageAssessment + tab | Cold + History (3x) + Training + filter | Local + Dev | TTFB + duration |
| 4 | ManageWorker | Cold + filter section + filter role+inactive + pagination | Dev wajib + Local opsional | DOMContentLoaded + page-switch + HTML size (threshold >300ms) |
| 5 | AssessmentMonitoring + Detail + GetActivityLog | Main list + 50-essay (3x) + standard + GetActivityLog 5-10× | Dev wajib + Local baseline | TTFB + duration, mean + max |
| 6 | GetOrganizationTree | Tree editor + dropdown + F5 5x + 2-user concurrent | Local + Dev | TTFB + duration + JSON size + unit count |
| 7 | Waterfall | ManageOrganization + ManageAssessment + ManageWorker | Dev saja | Screenshot + HAR export |

### Output Artifacts
- `docs/test 16-april/API_RESPONSE_TIMES.md` — matrix utama metrik
- `docs/test 16-april/har/` — HAR files per halaman/env
- `docs/test 16-april/screenshots/` — screenshot waterfall + bukti cache behavior

### Eksekusi — Dipisah 2 Fase

**Eksekutor:** Saya (Claude) via Playwright MCP untuk kedua environment.

---

#### Fase 2A — Localhost (`http://localhost:5277`)

**Prasyarat yang perlu disiapkan user sebelum fase ini:**
- [ ] App running di localhost (pastikan `dotnet run` aktif di port 5277)
- [ ] Database localhost terisi data minimum (seed jalan)
- [ ] Akun test tersedia (admin@pertamina.com / password default)
- [ ] Tidak ada breakpoint debugger aktif yang memblokir request
- [ ] Playwright MCP terhubung; browser Chromium akan di-download otomatis jika belum tersedia (butuh akses internet first-run)
- [ ] Clear browser state (cookies/cache) tidak diperlukan — Playwright pakai context isolated

**Tujuan Fase 2A:** Baseline fungsional (verifikasi cache behavior, alur request, correctness). BUKAN untuk validasi issue scale.

**Item yang dijalankan:**
- Item 1 (Analytics 11 endpoints — cold + tab switch + filter)
- Item 2 (`_tabCache` — A→B→A + filter change + F5)
- Item 3 (ManageAssessment — cold + History 3x + Training + filter) — tanpa ekspektasi bottleneck karena dataset kecil
- Item 4 **baseline only** (ManageWorker — cold load + filter section/role). **Skip** pagination next/prev test (dataset kecil tidak bisa buktikan threshold 300ms)
- Item 5 **baseline only** (main list + 1 MonitoringDetail standard + GetActivityLog 5-10×). **Skip** MonitoringDetail Pre-Post 50-essay — localhost tidak punya grup realistis. H-04 validation di Fase 2B
- Item 6 (GetOrganizationTree — tree editor + dropdown + F5 5x + 2 BrowserContext concurrent)

**Item yang di-skip di fase ini:**
- Item 7 (waterfall — dev only)
- Item 4 pagination interactive test (dataset kecil tidak representatif)
- Item 5 MonitoringDetail Pre-Post 50-essay (tidak ada data realistis)

**Output Fase 2A:**
- Entry di `API_RESPONSE_TIMES.md` kolom "Localhost"
- HAR files (opsional, jika ada anomali) di `docs/test 16-april/har/localhost/`
- Screenshots bukti `_tabCache` behavior (Item 2) di `docs/test 16-april/screenshots/localhost/`

---

#### Fase 2B — Dev Server (`http://10.55.3.3/KPB-PortalHC`)

**Prasyarat yang perlu disiapkan user sebelum fase ini:**
- [ ] Koneksi VPN / jaringan internal aktif
- [ ] Kredensial AD valid (username + password domain)
- [ ] Konfirmasi LDAP auth aktif di dev (lihat `appsettings.json` di dev, atau tanya admin)
- [ ] Akses user test memiliki role Admin/HC (untuk buka Monitoring/Worker/Organization)
- [ ] Konfirmasi jam akses (hindari jam sibuk / jam ujian massal)
- [ ] Daftar target session/grup untuk Item 5:
  - Grup Pre-Post dengan ≥50 peserta yang punya essay (untuk H-04)
  - Grup Standard tanpa essay (baseline H-04)
- [ ] Konfirmasi **read-only** — tidak ada CRUD yang akan dilakukan Playwright

**Tujuan Fase 2B:** Validasi scale impact. Hasil dibandingkan dengan Fase 2A untuk membuktikan issue C-03/C-04/H-03/H-04/M-08 hanya muncul di dataset besar.

**Item yang dijalankan:**
- Semua item (1-7) — fase ini menjadi fase utama untuk metrik scale
- Item 4 WAJIB dengan pagination interactive test (C-03, M-08 baru kelihatan di sini)
- Item 5 WAJIB dengan grup 50-essay yang ditentukan user (validasi H-04 N+1)
- Item 7 waterfall — ManageOrganization + ManageAssessment + ManageWorker

**Output Fase 2B:**
- Entry di `API_RESPONSE_TIMES.md` kolom "Dev Server"
- HAR files di `docs/test 16-april/har/dev/`
- Screenshots di `docs/test 16-april/screenshots/dev/`

---

### Sebelum Fase 2A Dimulai
User konfirmasi checklist prasyarat → saya mulai Playwright.

### Sebelum Fase 2B Dimulai
User konfirmasi:
1. Checklist prasyarat Fase 2B
2. Target grup session 50-essay (title + schedule date) untuk Item 5
3. Kredensial AD (bisa input langsung via Playwright saat login prompt)
