# Overview Awam Sertifikat — Design Spec

**Tanggal:** 2026-05-27
**Output file:** `docs/sertifikat-ecosystem/overview-awam.html`
**Audience:** Staff HC admin Portal HC KPB (awam non-IT)
**Tujuan:** Satu halaman ringkas — struktur sistem sertifikat sekarang + gap utama — tanpa harus baca 3 file lengkap (`ekosistem-sertifikat.html` 479 baris + `analisa-gap-benchmark.html` 605 baris + `index.html` 1355 baris).

---

## 1. Latar Belakang

User (Staff HC admin) butuh tahu dua hal:

1. **Struktur sistem sertifikat sekarang seperti apa?** (potret kondisi sekarang)
2. **Apa gap / fitur yang seharusnya ada tapi belum ada?**

File existing sudah cover dua hal ini, tapi:
- Total ~2440 baris cross 3 file → overwhelming buat awam
- Banyak istilah teknis (audit trail, RBAC, JSON-LD, Hangfire, dsb)
- Tidak ada single entry point ringkas untuk audience awam HC

Solusi: bikin **1 halaman pengantar awam** yang tarik intisari dari 2 file companion, format singkat, link kembali ke file lengkap untuk dive deeper.

---

## 2. Scope

### IN SCOPE

- 1 file HTML standalone (~250 baris target, maks 350 baris)
- 2 section utama: Struktur Sekarang + Gap Utama (top-5 saja)
- Bootstrap 5.3 + Bootstrap Icons (CDN, konsisten file lain)
- Light/dark theme toggle (konsisten file lain)
- Mini-nav 2 link
- Cross-reference link ke `ekosistem-sertifikat.html` (untuk struktur lengkap) + `analisa-gap-benchmark.html` (untuk 50 gap lengkap)
- Bahasa Indonesia plain, jargon minimal — kalau pakai istilah teknis (mis. "RBAC"), define inline 1 kalimat
- Print stylesheet sederhana (konsisten file lain)

### OUT OF SCOPE

- Tidak ada diagram Mermaid (drop CDN heavy, konsisten dengan keputusan analisa-gap-benchmark v2.0)
- Tidak ada Highlight.js (tidak butuh syntax highlight)
- Tidak ulang 50 gap matrix lengkap — hanya top-5
- Tidak ulang detail teknis ER diagram, RBAC table 6-row, glosarium 15 istilah, dsb — itu di file companion
- Tidak ada interactive search/filter (overkill untuk 1 halaman)
- Tidak ada dynamic data dari DB — pure static HTML

---

## 3. Struktur Konten

### Header

- `<h1>` "Overview Sertifikat untuk Tim HC"
- Subtitle: "Pengantar singkat: struktur sekarang + gap utama"
- Audience banner: jelaskan ini ringkasan awam, tunjuk file lengkap kalau butuh detail
- Versi + tanggal

### Mini-nav (sticky)

- `#sec-struktur` → §1 Struktur Sekarang
- `#sec-gap` → §2 Gap Utama
- Theme toggle button

### §1 Struktur Sistem Sertifikat Sekarang (~½ halaman)

Sub-konten:

**1.1 Ekosistem 4-Komponen**
4 mini-card grid (text-only, no Mermaid):
- **Sumber** — dari mana sertifikat lahir (online dari assessment, atau manual upload HC)
- **Penyimpanan** — apa yang disimpan database (tanggal terbit, kadaluarsa, nomor, link PDF)
- **Status** — dihitung real-time (Aktif / Akan Expired / Expired / Permanent)
- **Notifikasi** — auto 30 hari sebelum expired ke pekerja + HC

**1.2 Perjalanan 1 Sertifikat (Happy Path)**
Narasi alur singkat dalam list ordered (no diagram):
1. Pekerja ikut assessment / training
2. Lulus → sertifikat terbit (auto kalau online, manual kalau external)
3. Status Aktif → mendekati expired (30 hari sebelum) → notif keluar
4. Renewal: HC trigger training/assessment baru → sertifikat baru terhubung ke yang lama (renewal chain)

**1.3 4 Status Sertifikat**
Compact card row 4 kolom dengan icon + warna:
- ✅ Aktif (hijau)
- ⚠️ Akan Expired (kuning)
- ❌ Expired (merah)
- ♾️ Permanent (biru)
Tiap card: 1 kalimat "Kapan" + 1 kalimat "Arti".

**1.4 Catatan kunci untuk Staff HC** (alert box)
- Status sertifikat **bukan kolom database** — sistem hitung real-time, jangan coba update manual
- Renewal cuma bisa di-trigger **Admin (L1) atau HC (L2)** — peran lain tidak punya akses
- Renewal chain otomatis filter sertifikat lama dari dashboard outstanding

**Link**: "Detail lengkap (4-kotak Mermaid + RBAC 6-row + glosarium) → `ekosistem-sertifikat.html`"

### §2 Gap Utama (~½ halaman)

Sub-konten:

**2.1 Apa itu Gap?**
1 paragraf definisi awam: "Gap = fitur yang seharusnya ada (berdasarkan benchmark 9 platform external) tapi belum ada di Portal HC KPB."

**2.2 Top-5 Gap Kritis**
Accordion atau card stack — 5 item, tiap item:
- Nama gap (bold)
- Badge severity 🔴 Kritis + kategori (🏗️ Sistem / 🔄 Flow / ✨ Fitur)
- **Kondisi sekarang** (1-2 kalimat awam — tarik dari `analisa-gap-benchmark.html` §4)
- **Yang seharusnya ada** (1-2 kalimat — tarik dari best practice external)
- **Effort estimate badge** (Quick Win / Medium / Long-term)

5 gap (sesuai file source §4):
1. **Self-Service Renewal Portal** — Medium (3-6 bulan)
2. **Public Verification (QR / URL)** — Quick Win (1-3 bulan)
3. **Bulk Action Suite** — Medium (3-6 bulan)
4. **HCM Integration (Employee Master Sync)** — Long-term (>9 bulan)
5. **Audit Trail Defensible** — Medium (3-6 bulan)

**2.3 Ringkasan: ada berapa gap total?**
1 paragraf + stat box:
- 50 gap unique total (dari konsolidasi 60 raw + dedup)
- 5 kategori: 🏗️ Sistem / 🔄 Flow / ✨ Fitur / 🔒 Compliance / ⚡ Performa
- 3 bucket roadmap: Quick Win (1-3 bln) / Medium (3-9 bln) / Long-term (>9 bln)

**Link**: "50 gap lengkap + 9 platform benchmark + roadmap detail → `analisa-gap-benchmark.html`"

### Footer

- Tanggal update + versi
- Cross-ref ringkas ke 3 file companion (ekosistem + analisa-gap + index teknis)

---

## 4. Style Decisions

| Aspek | Pilihan | Alasan |
|-------|---------|--------|
| CSS framework | Bootstrap 5.3 (CDN) | Konsisten file lain di folder |
| Icons | Bootstrap Icons (CDN) | Konsisten file lain |
| Diagram | Tidak ada (text/card only) | Drop CDN heavy, konsisten dengan analisa-gap v2.0 |
| Dark mode | Toggle button (localStorage persist) | Konsisten file lain |
| Print | `@media print` sederhana | Konsisten file lain |
| Mini-nav | Sticky top, 2 link | Section sedikit, no overflow |
| Width | `max-width: 900px` | Lebih sempit dari 1100px file lain (konten lebih sedikit, lebih nyaman dibaca) |
| Heading | h1 + h2 per section + h5 sub | Konsisten file lain |

---

## 5. Cross-References

File yang link masuk → keluar:

- **In-link**: tambah link di `ekosistem-sertifikat.html` footer + `analisa-gap-benchmark.html` audience-banner: "Mau ringkasan 1 halaman? Lihat `overview-awam.html`"
- **Out-link**: setiap "lihat detail" di overview-awam → arahkan ke section ID spesifik file lain (deep link)

---

## 6. Success Criteria

User (Staff HC admin) bisa:

1. Buka `overview-awam.html` di browser, scroll satu kali, baca tuntas dalam < 10 menit
2. Setelah baca, bisa jawab pertanyaan:
   - Apa 4 komponen ekosistem sertifikat?
   - Apa 4 status sertifikat + kapan masing-masing?
   - Apa 5 gap paling kritis?
   - Mau detail lanjut ke mana?
3. Punya entry point jelas kalau butuh dive deeper (link ke companion)

---

## 7. Implementation Notes

- File standalone — tidak butuh build step
- CDN external: Bootstrap 5.3 + Bootstrap Icons 1.11 (sama versi file lain)
- Test: buka di browser lokal, cek responsiveness mobile + dark mode + print preview
- Tidak ada test automation Playwright (doc static, konsisten file lain di folder)
- Commit setelah file selesai + verify visual di browser
