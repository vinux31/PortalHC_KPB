# Phase 303: Rasio Coach-Coachee dan Balanced Mapping - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-10
**Phase:** 303-rasio-coach-coachee-dan-balanced-mapping
**Areas discussed:** Visualisasi rasio, Definisi beban, Balancing tools, Threshold & aturan, Halaman analitik layout, Mekanisme setting threshold, Akses role, Notifikasi overload, Nama menu

---

## Visualisasi Rasio — Lokasi

| Option | Description | Selected |
|--------|-------------|----------|
| Di halaman mapping yang sudah ada | Tambahkan section baru di atas tabel CoachCoacheeMapping | |
| Halaman baru terpisah | Buat menu baru khusus 'Analitik Coach' | ✓ |

**User's choice:** Halaman baru terpisah

---

## Visualisasi Rasio — Bentuk Chart

| Option | Description | Selected |
|--------|-------------|----------|
| Bar chart horizontal | Setiap coach = 1 bar, panjang bar = jumlah coachee | ✓ |
| Summary cards + ranking table | Cards di atas + tabel ranking | |
| Donut/pie chart | Proporsi distribusi coachee per coach | |

**User's choice:** Bar chart horizontal

---

## Definisi Beban

| Option | Description | Selected |
|--------|-------------|----------|
| Jumlah coachee aktif saja | Beban = count coachee aktif & belum graduated | ✓ |
| Weighted by tahun coaching | Bobot berbeda per tahun coaching | |
| Jumlah aktif + breakdown detail | Beban utama + breakdown per tahun | |

**User's choice:** Jumlah coachee aktif saja

---

## Graduated Coachee

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak dihitung | Hanya coachee aktif yang belum graduated | ✓ |
| Tampilkan terpisah | Tidak masuk beban, tapi tampil sebagai info historis | |

**User's choice:** Tidak dihitung

---

## Balancing Tools

| Option | Description | Selected |
|--------|-------------|----------|
| Visual warning + manual action | Warna indikator + admin reassign manual | |
| Saran otomatis reassign | Sistem menyarankan pindah coachee, admin approve/reject | ✓ |
| Visual warning saja | Hanya tampilkan info rasio dan warning | |

**User's choice:** Saran reassign saja (tanpa visual warning terpisah, tapi bar chart tetap pakai warna threshold)

---

## Auto-Suggest saat Assign Baru

| Option | Description | Selected |
|--------|-------------|----------|
| Ya — suggest coach beban terendah | Otomatis sarankan coach dengan coachee paling sedikit | ✓ |
| Tidak perlu | Admin pilih manual | |

**User's choice:** Ya — suggest coach dengan beban terendah

---

## Threshold

| Option | Description | Selected |
|--------|-------------|----------|
| Configurable oleh Admin | Admin set batas dari UI, tersimpan di DB | ✓ |
| Hardcoded default | Batas tetap di kode | |
| Rata-rata otomatis | Berdasarkan rata-rata distribusi | |

**User's choice:** Configurable oleh Admin, via tombol + modal popup
**Notes:** "ada seperti fitur untuk set berapa beban threshold"

---

## Section Consideration

| Option | Description | Selected |
|--------|-------------|----------|
| Prioritaskan section yang sama | Saran utamakan coach di section sama | ✓ |
| Lintas section boleh | Murni berdasarkan beban | |

**User's choice:** Prioritaskan section yang sama

---

## Halaman Analitik — Konten

| Option | Description | Selected |
|--------|-------------|----------|
| Summary cards | Total Coach, Coachee, Rata-rata, Overloaded | ✓ |
| Tabel detail per coach | Nama, Section, Jumlah Coachee, Status | ✓ |
| Filter by section | Dropdown filter section | ✓ |
| Export Excel | Download data rasio ke Excel | ✓ |

**User's choice:** Semua opsi dipilih

---

## Saran Reassign UI

| Option | Description | Selected |
|--------|-------------|----------|
| Section terpisah di bawah chart | List saran + tombol Approve/Skip | ✓ |
| Modal popup | Tombol 'Lihat Saran' buka modal | |
| Inline di tabel coach | Expand saran di bawah baris coach overloaded | |

**User's choice:** Section terpisah di bawah chart

---

## Threshold UI

**User's choice:** Tombol button yang membuka modal popup untuk set threshold
**Notes:** "ada tombol button, dan modal popup untuk set"

---

## Akses Role

| Option | Description | Selected |
|--------|-------------|----------|
| Admin | Full access | ✓ |
| HC | Read-only | ✓ |
| Coach | Lihat beban diri sendiri | |

**User's choice:** Admin (full) + HC (read-only)

---

## Notifikasi Overload

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak perlu | Cukup lihat dari halaman analitik | ✓ |
| Notif ke Admin | Notifikasi saat coach melebihi threshold | |
| Notif ke Admin dan HC | Notifikasi ke semua Admin dan HC | |

**User's choice:** Tidak perlu

---

## Nama Menu

**User's choice:** "Coach Workload"
**Penempatan:** Sebelum "Deliverable Progress Override" di sidebar CMP

---

## Claude's Discretion

- Default value threshold awal
- Algoritma prioritas saran reassign
- Chart library (Chart.js atau native)
- Styling dan spacing detail
- Empty state

## Deferred Ideas

None — discussion stayed within phase scope
