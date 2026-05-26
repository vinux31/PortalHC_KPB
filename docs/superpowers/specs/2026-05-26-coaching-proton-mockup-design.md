# Mockup Presentasi Coaching Proton — Design Spec

**Tanggal:** 2026-05-26
**Tujuan:** Mockup HTML interaktif untuk presentasi ke atasan menampilkan fitur Page Coaching Proton + sub-page real Portal HC KPB
**Output:** `docs/mockup-presentasi/coaching-proton-mockup.html` + `vendor/`
**Effort estimate:** 2–3 hari kerja (16–24 jam)

---

## 1. Tujuan & Audience

**Tujuan:** Menunjukkan ke atasan replika 80–100% UI fitur Coaching Proton agar atasan paham alur kerja, role-based view, dan governance approval chain dari Coach → Sr Supervisor → Section Head → HC.

**Audience:** Atasan (manajemen senior PT Pertamina KPB), presentasi formal via laptop/proyektor desktop FHD. Tidak ada mobile, tidak ada akses publik.

**Bukan tujuan:**
- Bukan dokumentasi teknis untuk developer
- Bukan training material untuk pekerja
- Bukan replacement Portal real

---

## 2. Keputusan Desain (Snapshot)

| # | Topik | Keputusan |
|---|---|---|
| 1 | Format | Single-page interactive HTML walkthrough |
| 2 | Scope page | 1 page: CoachingProton + sub-page + 3 modal |
| 3 | Total layar | **6 layar** (drop SH, merge dengan SrSpv) |
| 4 | Sumber replika | Handcraft port dari `.cshtml` (bukan screenshot) |
| 5 | Navigasi | Linear next/prev (footer button) + dot indicator |
| 6 | Data | Mix: nama coachee/coach anonim + kompetensi PROTON real |
| 7 | Chrome | Full chrome (navbar topbar Pertamina + breadcrumb) |
| 8 | Annotation | Bersih, no overlay |
| 9 | Storyline | Kronologis Coach → SrSpv → HC |
| 10 | Modal feedback | Toast "Mock: ..." kanan-atas, autohide 4 detik |
| 11 | CDN | Embed lokal (offline-proof) |
| 12 | Edit Session form | Read-only pre-filled |
| 13 | Search box | Hidup (filter rows real-time) |
| 14 | Filter dropdown | Mati (alert "demo only") |
| 15 | Pagination | Hidup 2 halaman dummy |
| 16 | Navbar items | Uniform (tidak berubah per role) |
| 17 | POV switch | Via user dropdown nama+role |
| 18 | "Lihat Detail" link | Aktif (shortcut ke Layar 2/4) |
| 19 | Edit pencil session | Aktif (shortcut ke Layar 6) |
| 20 | localStorage persist | Tidak ada — selalu mulai Layar 1 |
| 21 | Coachee info bar Layar 1 | Drop (ikut real cshtml) |

---

## 3. File Structure

```
docs/mockup-presentasi/
├── coaching-proton-mockup.html    (file utama, ±1500–2000 baris)
└── vendor/                         (CDN embed lokal)
    ├── bootstrap.min.css           (~230 KB, versi exact dari _Layout.cshtml)
    ├── bootstrap.bundle.min.js     (~80 KB)
    ├── bootstrap-icons.css         (~120 KB)
    └── fonts/
        ├── bootstrap-icons.woff2   (~100 KB)
        └── bootstrap-icons.woff    (~120 KB)
```

**Total payload:** ~1.5 MB. Berfungsi 100% offline via `file://` di Chrome.

**Catatan execute:** versi Bootstrap & Bootstrap Icons di-verify dulu dari `Views/Shared/_Layout.cshtml` `<link>` head, akuisisi via `curl`/`Invoke-WebRequest` ke `vendor/`.

---

## 4. Storyline Linear 6 Layar

```
[1] Coach View
    ↓ klik "Submit Evidence" pada row → modal C1 buka → isi → submit → toast
    ↓ (next)
[2] Detail Pending B1 (POV Coach)
    ↓ (next)
[3] Sr Supervisor View
    ↓ klik "Tinjau" pada row → modal C2 buka → Approve → toast
    ↓ (next)
[4] Detail Approved B1 (POV Coach, chain lengkap 3 hijau)
    ↓ (next)
[5] HC View + HC Pending Review Panel
    ↓ centang checkbox → klik "Approve Selected" → modal C3 → toast
    ↓ (next)
[6] Edit Session B2 (POV Coach, read-only pre-filled)
```

**Shortcut alternatif (tidak menggantikan footer Next):**
- Klik "Lihat Detail" tabel Layar 1 → Layar 2
- Klik "Lihat Detail" tabel Layar 3 → Layar 4
- Klik "Lihat Detail" tabel Layar 5 → Layar 4
- Klik pencil "Edit" di session card Layar 2/4 → Layar 6

---

## 5. Komponen Chrome (Sticky Header + Footer)

### 5.1 Header — Navbar Pertamina (Uniform)
- Port dari `Views/Shared/_Layout.cshtml` baris 92–180 (navbar topbar)
- Items konstan: Home / CMP / CDP / Guide / Admin / [user dropdown]
- **User dropdown berubah per layar** mengikuti POV:
  - Layar 1, 2, 4, 6: "Eko Wibowo (Coach)"
  - Layar 3: "Fajar Hidayat (Sr Supervisor)"
  - Layar 5: "Hadi Nugroho (HC)"
- Avatar bulatan initial sesuai nama (EW / FH / HN)
- Dropdown menu: My Profile / Settings / Logout — semua mati (alert demo)

### 5.2 Footer — Navigation Bar (Sticky)
```
[← Sebelumnya]   ● ● ● ● ● ●   Layar 3 dari 6   [Berikutnya →]
                 ^ active (warna primary)
```
- Tombol Prev disabled di Layar 1
- Tombol Next disabled di Layar 6
- 6 dot clickable (loncat langsung)
- Indikator teks "Layar X dari 6"
- Keyboard: ← prev, → next, ESC tutup modal
- Posisi: `fixed bottom`, z-index < modal

### 5.3 Breadcrumb (Per Layar)
- Layar 1, 3, 5: `CDP > Coaching Proton`
- Layar 2, 4: `CDP > Coaching Proton > Deliverable`
- Layar 6: `CDP > Coaching Proton > Deliverable > Edit Sesi Coaching`

### 5.4 Footer Brand (Optional)
- Verify `_Layout.cshtml` untuk footer brand area saat execute
- Port apa adanya kalau ada

---

## 6. Komponen Per Layar (Detail)

### Layar [1] — Coach View CoachingProton
**POV:** Eko Wibowo (Coach, level 5) mengasuh 3 coachee

**Filter bar:**
- Coachee dropdown ✓ (level ≤5 muncul)
- Track dropdown ✓
- Tahun dropdown ✓
- Search box ✓ **HIDUP**
- Tombol Reset ✓ mati
- Bagian/Unit dropdown HIDDEN (level ≤3/4 only)

**3 Stat card:** Progress 65% (progress bar) · Pending Actions 2 · Pending Approvals 0

**Tabel deliverable** (group `Coachee → Kompetensi → SubKompetensi → Deliverable`):
- Ahmad Budiman → Kompetensi 1 (PROTON real) → 2 sub → 4 deliverable
- Citra Lestari → Kompetensi 1 → 1 sub → 2 deliverable
- Dimas Pratama → Kompetensi 2 → 1 sub → 2 deliverable
- 8 baris halaman 1, 4 baris halaman 2 (data dummy berbeda)
- Kolom Evidence: 2 baris tombol "Submit Evidence" AKTIF → modal C1, sisanya badge "Sudah Upload"/"Approved"
- Kolom Approval SrSpv/SH/HC: mix badge Pending/Approved
- Kolom Detail: link "Lihat Detail" AKTIF → Layar 2

**Pagination:** 2 halaman dummy hidup, klik halaman 2 = swap rows
**HC Pending Panel:** HIDDEN (bukan HC)

---

### Layar [2] — Detail Pending (B1) POV Coach
**Card 1 Detail Coachee:**
- Ahmad Budiman · Track "PROTON Maintenance 2026" Tahun 1
- Kompetensi (PROTON real)
- Sub-Kompetensi (PROTON real)
- Deliverable (PROTON real)
- Badge "Coach"

**Card 2 Approval Chain:**
- Status badge top: "Submitted" (biru)
- 3 step vertical timeline: SrSpv Pending · SH Pending · HC Pending (semua icon abu)
- **Tidak ada tombol approve** (POV Coach, `CanApprove == false`)
- Tidak ada section "Tindakan Approval"

**Card 3 Evidence Coach:**
- File "Dokumen-IDP-Ahmad.pdf" + tombol Download mati
- 1 coaching session card: tabel acuan (Pedoman/TKO/BestPractice/Dokumen) + Catatan Coach + Kesimpulan + Result badge "Good"
- Tombol Edit (pencil) AKTIF → Layar 6
- Tombol Hapus session mati
- Tombol "PDF Evidence Report" mati

**Card 4 Riwayat Status:**
- Timeline 3 event: "Deliverable dibuat" (15 Mei) · "Evidence diupload oleh Eko Wibowo (Coach)" (15 Mei) · "Coaching session oleh Eko Wibowo" (15 Mei)

**Tombol Kembali:** mati

---

### Layar [3] — Sr Supervisor View CoachingProton
**POV:** Fajar Hidayat (Sr Supervisor, level 4) lingkup Unit Maintenance

**Filter bar:**
- Unit dropdown ✓ (level ≤4)
- Coachee dropdown ✓ (level ≤5)
- Track + Tahun + search ✓
- Bagian dropdown HIDDEN (level ≤3 only)

**Tabel deliverable:**
- 5 coachee × ~3 deliverable = ~15 baris (pagination 2 halaman: 8 + 7)
- Kolom Approval SrSpv: 3 baris status Submitted → tombol **"Tinjau" AKTIF** → modal C2
- Kolom Detail: link "Lihat Detail" AKTIF → Layar 4

**HC Pending Panel:** HIDDEN

---

### Layar [4] — Detail Approved (B1) POV Coach
**Sama struktur Layar [2]** dengan beda:

**Card 2 Approval Chain:**
- Status badge top: "Approved" (hijau)
- 3 step semua badge hijau:
  - Sr Supervisor — Approved by Fajar Hidayat · 16 Mei 2026 10:30
  - Section Head — Approved by Gita Sari · 17 Mei 2026 09:15
  - HC Review — Reviewed by Hadi Nugroho · 18 Mei 2026 14:00
- Tidak ada tombol approve (POV Coach)

**Card 4 Riwayat Status:**
- Timeline 6 event lengkap: dibuat → upload → SrSpv approve → SH approve → HC review → session

**Card 3 Evidence:** sama dengan Layar 2 (file + 1 session card, badge Result tetap "Good")

---

### Layar [5] — HC View CoachingProton + HC Pending Review Panel
**POV:** Hadi Nugroho (HC, level 2)

**Filter bar lengkap:**
- Bagian + Unit + Coachee + Track + Tahun + search semua tampak

**Tombol export HC tambahan:** Bottleneck Report + Coaching Tracking + Workload Summary (semua mati)

**Tabel deliverable:**
- Kolom paling kiri: checkbox (HC only)
- Tombol "Approve Selected (N)" muncul saat ada centang → modal C3
- Kolom Approval HC: 3 baris status Submitted + Pending → tombol "Review" inline
- Kolom Detail: link "Lihat Detail" AKTIF → Layar 4

**HC Pending Review Panel:**
- Card collapse expanded (terbuka by default)
- Header badge "3 pending"
- Tabel sub: 3 baris coachee dengan tombol "Review" AKTIF → toast "Mock: di-review"

---

### Layar [6] — Edit Session (B2) POV Coach
**Header card biru:** "Edit Sesi Coaching"

**Read-only block:**
- Tanggal Sesi: "15 Mei 2026"
- Kompetensi (PROTON real)
- Sub-Kompetensi (PROTON real)
- Deliverable (PROTON real)

**Form pre-filled (DISPLAY ONLY — bukan editable per keputusan #12):**
- Catatan Coach textarea: paragraf realistis 3-4 kalimat observasi coaching
- Kesimpulan dropdown: "Kompeten" terpilih (read-only)
- Result dropdown: "Good" terpilih (read-only)

**Tombol:**
- "Simpan Perubahan" mati (tooltip "demo only")
- "Batal" mati

---

## 7. Modal Interactive (3 Modal)

### 7.1 Modal C1 — Submit Evidence & Coaching Report
**Trigger:** klik "Submit Evidence" tabel Layar 1
**Size:** modal-lg (800px)
**Port dari:** `CoachingProton.cshtml` baris 938+

**Konten:**
- Deliverable selector — 2 checkbox deliverable Ahmad Budiman (1 pre-checked, toggle aktif)
- Tanggal input date pre-filled "2026-05-15"
- Card Acuan — 4 textarea pre-filled: Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen
- Catatan Coach textarea pre-filled 3-4 kalimat
- Kesimpulan dropdown: "Kompeten secara mandiri" / "Masih perlu dikembangkan"
- Result dropdown: "Need Improvement / Suitable / Good / Excellence"
- Upload file `<input type="file">` aktif, tidak benar-benar upload (tampilkan nama file saja)

**Footer:** Batal (tutup) + Submit Evidence (primary)

**State submit:** modal tutup → toast hijau kanan-atas "Mock: Evidence terkirim — di production akan masuk antrian approval Sr Supervisor" (4 detik autohide)

---

### 7.2 Modal C2 — Tinjau Deliverable
**Trigger:** klik "Tinjau" tabel Layar 3
**Size:** modal default (500px)
**Port dari:** `CoachingProton.cshtml` baris 902+

**Konten:**
- Info read-only: Kompetensi / Sub-Kompetensi / Deliverable
- Link "Lihat Evidence" mati (alert demo)
- Aksi dropdown: "Pilih Aksi" / Approve / Reject
- Komentar textarea muncul kondisional kalau Reject dipilih (Bootstrap collapse asli)
- Required indicator merah saat Reject

**Footer:** Batal + Submit (disabled sampai Aksi dipilih)

**State submit:**
- Approve → toast "Mock: Deliverable disetujui Sr Supervisor — selanjutnya menunggu Section Head"
- Reject tanpa komentar → form validation inline error
- Reject + komentar → toast "Mock: Deliverable ditolak — Coach perlu re-submit"

---

### 7.3 Modal C3 — Batch HC Approve
**Trigger:** centang ≥1 checkbox tabel Layar 5 + klik "Approve Selected (N)"
**Size:** modal default
**Catatan execute:** port konten asli dari `CoachingProton.cshtml` (cari `#batchApproveModal`, di area 1000–1700 yang belum di-baseline)

**Konten (perkiraan, verify saat execute):**
- Header: "Konfirmasi Batch HC Review"
- Body: "Anda akan menandai N deliverable sebagai sudah di-review HC:"
- List bullet 3 deliverable yang dicentang (nama coachee + deliverable)
- Note: "Status approval tidak berubah, hanya menandai bahwa HC sudah memeriksa."

**Footer:** Batal + Konfirmasi Review (success)

**State submit:** modal tutup → toast "Mock: 3 deliverable ditandai sudah di-review HC" + reset checkbox + hide tombol "Approve Selected"

---

### 7.4 Pola Toast Bersama
- Container: `<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3">`
- Komponen Bootstrap 5 `toast` standar
- Warna: success hijau untuk semua mock submit
- Autohide 4 detik + close manual
- Single instance — toast baru replace toast lama
- Helper: `showToast(message)` global function

---

## 8. Data Inline Structure

Semua data hardcoded di `<script>` footer. Tidak ada fetch/API.

### 8.1 Master Data (struktur)
```js
const KOMPETENSI = [
  { id, nama: '<PROTON real>', subKompetensi: [
    { id, nama, deliverable: [{ id, nama }] }
  ]}
  // 3 kompetensi × ~6 sub × ~2 deliverable
];

const USERS = {
  coach: { id, nama: 'Eko Wibowo', role: 'Coach', level: 5 },
  srspv: { id, nama: 'Fajar Hidayat', role: 'Sr Supervisor', level: 4, unit: 'Maintenance' },
  sh:    { id, nama: 'Gita Sari', role: 'Section Head', level: 4, seksi: 'Mekanikal' },
  hc:    { id, nama: 'Hadi Nugroho', role: 'HC', level: 2 }
};

const COACHEES = [
  { id: 'c1', nama: 'Ahmad Budiman', track: 'PROTON Maintenance 2026', tahunKe: 'Tahun 1', unit, seksi },
  { id: 'c2', nama: 'Citra Lestari', track: 'PROTON Operasi 2026', tahunKe: 'Tahun 2', unit, seksi },
  { id: 'c3', nama: 'Dimas Pratama', ... },
  { id: 'c4', nama: 'Bayu Saputra', ... },
  { id: 'c5', nama: 'Rini Astuti', ... }
];

const PROGRESS = [
  // ~20 entry total cover semua state Layar 1/3/5
  {
    id, coacheeId, deliverableId,
    status: 'Pending|Submitted|Approved|Rejected',
    evidenceFile, submittedAt,
    srSpvApproval: { status, approvedById, approvedAt },
    shApproval: { ... },
    hcReview: { ... },
    coachingSessions: [{ coachId, date, acuanPedoman, acuanTko, acuanBestPractice, acuanDokumen, catatanCoach, kesimpulan, result }],
    statusHistory: [...]
  }
];
```

### 8.2 Sumber Kompetensi PROTON Real
Saat execute, jalankan urutan fallback:
1. `grep -A 5 ProtonKompetensi Data/SeedData*.cs` — kalau ada, ambil 3 nama representatif
2. Fallback query DB lokal: `sqlcmd -S . -d HcPortal -Q "SELECT TOP 3 NamaKompetensi FROM ProtonKompetensi"`
3. Last resort: placeholder generik dengan note "(sample data)"

---

## 9. Tech Stack

| Item | Pilihan | Alasan |
|---|---|---|
| HTML | Plain HTML5 single file | Sederhana, portable |
| CSS framework | Bootstrap 5.x embed lokal (versi exact match Layout) | Match Portal real |
| Icon | Bootstrap Icons embed lokal | Match Portal real |
| JS framework | Vanilla JS | Tidak butuh framework |
| Build tool | Tidak ada | Direct edit |
| Server | Tidak butuh, `file://` di Chrome | Offline-first |

**Custom CSS:** ~50–100 baris inline `<style>` untuk footer sticky nav + dot indicator + hover hotspot + minor override

**Custom JS:** ~200–300 baris inline `<script>` untuk:
- Navigasi 6 layar (show/hide section + footer state + keyboard listener)
- Search box client-side (filter rows)
- Pagination 2 halaman dummy (swap rows)
- Modal C1/C2/C3 form handler + toast trigger
- Filter dropdown alert "demo only"
- Pre-fill modal field saat dibuka

**TIDAK pakai localStorage** — selalu mulai Layar 1 saat load.

---

## 10. State Logic & Interactivity

### Navigation
- `activeScreen` integer 1–6 di memory (bukan localStorage)
- Semua 6 layar render ke DOM, hidden via `display:none` + class `.screen-active`
- Switch: klik prev/next/dot/keyboard → toggle class + scroll-to-top
- Footer indicator: 6 dot, dot active warna primary
- Prev disabled di Layar 1, Next disabled di Layar 6

### Modal
- Bootstrap modal default behavior
- Form value tidak reset antar buka-tutup dalam session
- Native HTML5 validation + custom JS untuk C2 (komentar wajib saat reject)

### Search Box (Layar 1, 3, 5)
- Filter `<tbody>` rows dengan attribute `data-search`
- Case-insensitive `includes()` keyword
- Clear button muncul kalau input ada isi
- No match → tampil "Tidak ditemukan untuk '<keyword>'"

### Pagination (Layar 1, 3, 5)
- 2 halaman dummy hidup
- State per layar: `paginationState = { 1: 1, 3: 1, 5: 1 }`
- Switch: hide rows halaman 1, show rows halaman 2 (dummy data berbeda)

### Tombol Hotspot
- **Aktif:** Submit Evidence (C1) · Tinjau (C2) · Approve Selected (C3) · Review HC Panel · Lihat Detail · Edit pencil session · Search box · Pagination · Footer Prev/Next · Dot indicator · User dropdown (visual only)
- **Mati (toast "Demo mode — fitur aktif di Portal produksi"):** Reset filter · Kembali · Download Evidence · Export Excel/PDF · Bottleneck/Tracking/Workload Report · Hapus Session · PDF Evidence Report · Simpan Perubahan (Edit) · Batal (Edit) · Tombol Logout dropdown · Tombol Profile/Settings dropdown

### Filter Dropdown
- onChange → alert "Filter aktif di Portal produksi — demo ini menampilkan data tetap"
- Reset ke "Semua [X]" setelah alert

---

## 11. Verification & Testing

### Local Browser Test (Wajib)
1. Buka `file:///.../docs/mockup-presentasi/coaching-proton-mockup.html` di Chrome
2. Validasi navigasi 6 layar (footer button + keyboard ←/→ + dot click)
3. Validasi 3 modal: buka, isi, submit, toast muncul, autohide
4. Validasi search filter rows real-time
5. Validasi pagination swap rows
6. Validasi hotspot "Lihat Detail" + Edit pencil shortcut bekerja
7. Validasi offline: matikan wifi → reload → semua tetap render
8. Cek console: no errors, no 404 untuk vendor

### Fidelity Check (Manual)
1. Render Portal real lokal: `dotnet run` → buka `http://localhost:5277/CDP/CoachingProton`
2. Login berbagai role (Coach, SrSpv, HC) — pakai dev credentials
3. Side-by-side compare per layar dengan mockup
4. Target: 80–100% replika visual (layout, warna, spacing)
5. Acceptable drift: data dummy beda (sengaja), filter dropdown beda (mockup mati), modal feedback toast vs redirect

### Tidak Perlu
- Playwright regression test
- Mobile responsive test
- Unit test JavaScript
- Lighthouse audit

---

## 12. Out-of-Scope (Explicit)

- ❌ Mobile responsive — desktop FHD 1920×1080 only
- ❌ Page lain (BudgetTraining, AnalyticsDashboard, CDP Dashboard) — saved for future mockup
- ❌ Backend integration — no fetch, no API, no DB call
- ❌ Authentication flow — POV via user dropdown name only
- ❌ Print/PDF export dari mockup
- ❌ i18n / bilingual — Indonesia only
- ❌ Animasi transition antar layar — instant swap
- ❌ Dark mode
- ❌ Persist state via localStorage
- ❌ README terpisah (informasi cukup di file HTML inline atau via spec ini)

---

## 13. Risiko & Mitigasi

| Risiko | Mitigasi |
|---|---|
| Page Dev real berubah selama mockup dibuat → drift | Baseline `.cshtml` SHA dicatat di plan saat execute |
| Kompetensi PROTON real tidak ada di seed | Fallback query DB lokal `sqlcmd`, last resort placeholder generik |
| Atasan iseng klik tombol mati | Toast "Demo mode — fitur aktif di Portal produksi" feedback |
| Font Bootstrap Icons gagal load offline | Embed WOFF2 di `vendor/fonts/` + override `@font-face` path |
| Browser cache vendor stale | Cache-busting `?v=1` di link CSS |
| Versi Bootstrap salah → visual mismatch | Verify versi exact dari `_Layout.cshtml` saat execute |
| Modal `#batchApproveModal` konten asli berbeda dari perkiraan | Port apa adanya saat execute (cari di area 1000–1700) |

---

## 14. Success Criteria

- [ ] Atasan buka 1 file HTML, tanpa internet, semua jalan
- [ ] 6 layar bisa diakses linear via footer Next/Prev + keyboard ←/→ + dot click
- [ ] 3 modal interactive: buka, isi, submit, toast muncul
- [ ] Search box filter rows real-time di Layar 1/3/5
- [ ] Pagination 2 halaman dummy bisa di-swap
- [ ] Shortcut "Lihat Detail" Layar 1/3/5 → Layar 2/4 bekerja
- [ ] Shortcut Edit pencil session Layar 2/4 → Layar 6 bekerja
- [ ] Toast muncul kanan-atas, autohide 4 detik
- [ ] Tombol mati (Reset/Export/Logout/etc) trigger toast "Demo mode"
- [ ] Filter dropdown trigger alert "demo only"
- [ ] Fidelity 80–100% vs Portal real (visual side-by-side acceptable)
- [ ] Total payload < 2 MB
- [ ] No console errors saat load offline
- [ ] User dropdown nama+role berubah per layar sesuai POV

---

## 15. Catatan Eksekusi

**Yang harus di-verify saat execute (tidak butuh decision user lagi):**
1. Versi exact Bootstrap & Bootstrap Icons dari `Views/Shared/_Layout.cshtml` `<link>` head
2. Konten asli modal `#batchApproveModal` di `CoachingProton.cshtml` baris 1000–1700
3. Footer brand area `_Layout.cshtml` (kalau ada, port apa adanya)
4. Kompetensi PROTON real dari `Data/SeedData*.cs` atau DB lokal (3 nama representatif)
5. Custom color brand Pertamina kalau ada override di Layout

**Workflow next step:** invoke `superpowers:writing-plans` untuk decompose ke implementation plan setelah spec ini di-approve user.
