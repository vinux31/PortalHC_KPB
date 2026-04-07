# Phase 299: Worker Pre-Post Test + Comparison - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 299-worker-pre-post-test-comparison
**Areas discussed:** Tampilan card Pre-Post, Blocking & sequencing, Halaman perbandingan, Gain score display, Tab filtering, Riwayat Ujian, Controller/backend scope

---

## Tampilan Card Pre-Post

| Option | Description | Selected |
|--------|-------------|----------|
| 2 card terpisah terhubung | 2 card biasa (reuse layout existing) dengan badge dan garis penghubung visual | ✓ |
| 1 card gabungan 2 section | 1 card lebih besar dengan 2 section (Pre/Post) | |
| Claude decides | Claude pilih pendekatan terbaik | |

**User's choice:** 2 card terpisah terhubung
**Notes:** Familiar untuk worker, reuse layout existing

| Option | Description | Selected |
|--------|-------------|----------|
| Di samping category badge | Badge tambahan 'Pre-Test'/'Post-Test' di sebelah badge kategori | ✓ |
| Ganti status badge | Badge Pre/Post menggantikan posisi status | |
| Claude decides | | |

**User's choice:** Di samping category badge

| Option | Description | Selected |
|--------|-------------|----------|
| Border warna sama + ikon panah | Kedua card punya left-border warna sama dan ikon panah kecil | ✓ |
| Grouped dalam container | Container tipis dashed border dengan label | |
| Claude decides | | |

**User's choice:** Border warna sama + ikon panah

---

## Blocking & Sequencing

| Option | Description | Selected |
|--------|-------------|----------|
| Card disabled + pesan | Card Post tampil tapi disabled & grayed out, teks 'Selesaikan Pre-Test terlebih dahulu' | ✓ |
| Card tersembunyi | Post card tidak muncul sampai Pre Completed | |
| Card normal, tombol locked | Card normal, tombol Start jadi lock icon | |

**User's choice:** Card disabled + pesan
**Notes:** User asked for clarification about which page — confirmed My Assessments worker page

| Option | Description | Selected |
|--------|-------------|----------|
| Post juga otomatis blocked | Post tidak bisa dimulai, card menampilkan 'Pre-Test tidak diselesaikan' | ✓ |
| HC decides via reset | Post tetap blocked, HC harus reset Pre | |
| Claude decides | | |

**User's choice:** Post juga otomatis blocked (when Pre expired)

| Option | Description | Selected |
|--------|-------------|----------|
| Card aktif tapi tombol Upcoming | Card Post normal, tombol disabled 'Opens [tanggal]' | ✓ |
| Card + countdown | Card Post aktif dengan countdown timer | |
| Claude decides | | |

**User's choice:** Card aktif tapi tombol Upcoming (when Pre Completed but Post not yet scheduled)

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak ada info tambahan | Flow submit Pre sama seperti assessment biasa | ✓ |
| Info di ExamSummary | Halaman ringkasan menampilkan info Post-Test | |
| Claude decides | | |

**User's choice:** Tidak ada info tambahan (after Pre submit)

---

## Halaman Perbandingan

| Option | Description | Selected |
|--------|-------------|----------|
| Dari Riwayat Ujian | Post-Test punya tombol tambahan 'Bandingkan' di Riwayat | ✓ |
| Dari card Post-Test | Card Post punya tombol 'Lihat Perbandingan' | |
| Kedua akses | Bisa dari card dan Riwayat | |

**User's choice:** Dari Riwayat Ujian

| Option | Description | Selected |
|--------|-------------|----------|
| Tabel side-by-side | Elemen Kompetensi, Skor Pre, Skor Post, Gain Score | ✓ |
| Card summary + tabel detail | 3 card summary di atas + tabel per elemen | |
| Claude decides | | |

**User's choice:** Tabel side-by-side

**Detail level discussion:** User clarified that AllowAnswerReview setting controls per-soal detail visibility. Decided comparison lives inside Results page, not as separate page.

| Option | Description | Selected |
|--------|-------------|----------|
| Di halaman Results Post | Section perbandingan di dalam Results existing saat buka detail Post-Test | ✓ |
| Halaman baru terpisah | /CMP/ComparePrePost/{id} khusus perbandingan | |
| Claude decides | | |

**User's choice:** Di halaman Results Post

| Option | Description | Selected |
|--------|-------------|----------|
| Di atas, sebelum detail soal | Section comparison di atas, detail soal Post di bawah | ✓ |
| Di bawah, setelah detail soal | Detail soal dulu, comparison di bawah | |
| Claude decides | | |

**User's choice:** Di atas, sebelum detail soal

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya dari Post | Section comparison hanya di Results Post | ✓ |
| Dari keduanya | Results Pre juga tampilkan comparison jika Post Completed | |
| Claude decides | | |

**User's choice:** Hanya dari Post

---

## Gain Score Display

| Option | Description | Selected |
|--------|-------------|----------|
| Angka + warna | Hijau positif, merah negatif, abu 0. Format '+67%' | ✓ |
| Angka saja | Angka persentase tanpa warna | |
| Claude decides | | |

**User's choice:** Angka + warna

| Option | Description | Selected |
|--------|-------------|----------|
| Sembunyikan gain score | Gain menampilkan '—' + pesan menunggu penilaian | ✓ |
| Tampilkan partial + warning | Gain dihitung dari skor sementara + warning | |
| Claude decides | | |

**User's choice:** Sembunyikan gain score (when Essay pending)

---

## Tab Filtering Pre-Post

| Option | Description | Selected |
|--------|-------------|----------|
| Claude decides | Claude pilih pendekatan terbaik | ✓ |
| Ikuti status masing-masing | Pre dan Post masuk tab sesuai statusnya | |
| Selalu berpasangan | Pre-Post selalu muncul berdua | |

**User's choice:** Claude decides

---

## Riwayat Ujian Pre-Post

| Option | Description | Selected |
|--------|-------------|----------|
| 2 baris terpisah + badge | Pre dan Post sebagai 2 baris terpisah dengan badge | ✓ |
| 1 baris gabungan | 1 baris dengan skor Pre/Post inline | |
| Claude decides | | |

**User's choice:** 2 baris terpisah + badge

---

## Controller/Backend Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Extend Results action | Results() diextend untuk include comparison data | ✓ |
| Action baru ComparePrePost | Buat action baru khusus perbandingan | |
| Claude decides | | |

**User's choice:** Extend Results action
**Notes:** User asked for analysis. Claude recommended extend Results as more consistent with UX (worker klik 'Detail' → Results). User agreed.

## Claude's Discretion

- Tab filtering strategy for Pre-Post cards
- Exact visual design for card linking (left-border + arrow)
- Loading/empty states for comparison section
- Responsive layout comparison section on mobile

## Deferred Ideas

- Real-time SignalR updates (realtime-assessment.md todo) — independent phase
- AssessmentPhase multi-tahap — no use case yet
