# Phase 396: Import Excel + retire BulkBackfill - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input planning/research/execution.
> Keputusan ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-18
**Phase:** 396-import-excel-retire-bulkbackfill
**Areas discussed:** Penempatan + sumber NIP · Format & sel template · Lingkup + preview · Cara retire BulkBackfill
**Mode:** discuss interaktif (advisor-off, no USER-PROFILE)

---

## Area 1 — Penempatan + sumber NIP

### Q1: Di mana jalur Import Excel diletakkan dalam wizard?
| Option | Description | Selected |
|--------|-------------|----------|
| Toggle dalam Step-5 | Radio 'Isi via Form'/'Import Excel', reuse seam #step5Placeholder, tak refactor pills/nav | ✓ |
| Langkah/tab terpisah | Excel jadi langkah/pill sendiri (Step-5b) | |

### Q2: Relasi NIP Excel dengan worker picker (Step-2)?
| Option | Description | Selected |
|--------|-------------|----------|
| Wajib subset picker | Picker=audience; baris Excel wajib NIP yang dipilih; NIP di luar picker → ditolak | ✓ |
| Excel sumber NIP bebas | Picker di-skip saat mode Excel; audience = baris Excel (NIP valid di AspNetUsers) | |

### Q3: Boleh campur form + Excel dalam satu room?
| Option | Description | Selected |
|--------|-------------|----------|
| Mutually exclusive | 1 room = 1 metode (semua form ATAU semua Excel) | ✓ |
| Boleh campur | Sebagian form, sebagian Excel; perlu merge state + aturan siapa-menang | |

**Notes:** D-01/D-02/D-03. Picker tetap satu sumber audience; Excel hanya isi jawaban mereka.

---

## Area 2 — Format & sel template

### Q1: Bagaimana legend soal/opsi disertakan?
| Option | Description | Selected |
|--------|-------------|----------|
| Multi-sheet | Sheet-1 matrix isian + Sheet-2 legend (soal→teks+tipe+ScoreValue+huruf opsi→teks) | ✓ |
| Single-sheet inline | Header kolom berisi teks soal+opsi inline | |

### Q2: Kolom essay — skor saja atau skor+teks?
| Option | Description | Selected |
|--------|-------------|----------|
| Skor + teks opsional | 2 kolom: skor (0..ScoreValue) + teks (opsional); D-04/395 teks-wajib di-scope mode form saja | ✓ |
| Skor saja | 1 kolom skor; teks dikosongkan | |
| Skor + teks wajib | Teks WAJIB bila skor>0 (selaras ketat D-04) | |

### Q3: Sel jawaban kosong (blank) artinya apa?
| Option | Description | Selected |
|--------|-------------|----------|
| Skip → grade 0 | Omit answer spec → grade 0 (konsisten warn-but-allow D-05/395); preview tunjukkan dampak | ✓ |
| Ditolak (wajib penuh) | Sel kosong = baris invalid → rollback; paksa semua sel terisi | |

**Notes:** D-04/D-05/D-06. Sel MC/MA = huruf opsi (A / A,C, pola spec §7.1).

---

## Area 3 — Lingkup + preview

### Q1: Excel dukung auto-generate (kolom skor target) atau eksplisit saja?
| Option | Description | Selected |
|--------|-------------|----------|
| Jawaban eksplisit saja | Auto-gen tetap di jalur form (395); BuildAutoGenAnswers tetap reusable bila perlu | ✓ |
| Dukung kolom skor target | Kolom opsional trigger BuildAutoGenAnswers per-baris; perlu aturan prioritas sel | |

### Q2: Preview sebelum commit?
| Option | Description | Selected |
|--------|-------------|----------|
| Wajib preview dry-run | Tabel NIP+skor final+lulus via PreviewInjectScore (reuse 395); preview==commit | ✓ |
| Commit langsung | Upload → langsung inject (pola BulkBackfill lama), hasil di TempData | |

### Q3: Granularitas error report?
| Option | Description | Selected |
|--------|-------------|----------|
| Daftar lengkap per-baris | Kumpulkan semua masalah sekaligus; atomic (tak commit bila ≥1 error) | ✓ |
| Error pertama saja | Stop di error pertama (pola BulkBackfill lama) | |

**Notes:** D-07/D-08/D-09.

---

## Area 4 — Cara retire BulkBackfill

### Q1: Mekanisme pensiun route lama (GET :787 + POST :836)?
| Option | Description | Selected |
|--------|-------------|----------|
| Redirect 302 ke inject | Route lama bounce ke /Admin/InjectAssessment; non-destruktif; view jadi dead | |
| Hard-remove total | Hapus action GET+POST + view BulkBackfill.cshtml; route → 404 | ✓ |

### Q2: Nasib kartu Section D (Index.cshtml:309)?
| Option | Description | Selected |
|--------|-------------|----------|
| Hapus kartu | Satu pintu = kartu Inject Section C | ✓ |
| Ganti jadi pointer | Kartu tetap, arahkan ke page inject baru | |

### Q3: Kasus 'skor-saja tanpa soal' ditangani bagaimana?
| Option | Description | Selected |
|--------|-------------|----------|
| Ditutup auto-generate | Mode auto-gen inject (395) sintesis jawaban+rincian; retire aman | ✓ |
| Pertahankan jalur skor-saja | Redirect entry-point tapi simpan fungsionalitas skor-saja terpisah | |

**Notes:** D-10/D-11/D-12. **Temuan scout:** entry-point KEDUA `_AssessmentGroupsTab.cshtml:319` (dropdown) — wajib ikut hapus (D-11). `ManualDuplicatePredicate` shared TETAP (dipakai AddManual/Import). `DuplicateGuardTests.cs` #14 uji predikat (bukan action) — verifikasi tak break.

## Claude's Discretion
- Lokasi generator/parser Excel (service vs helper), bentuk endpoint preview (terpisah vs 2-fase), header kolom, styling legend, nasib komentar-only refs BulkBackfill.

## Deferred Ideas
- Auto-gen via Excel · redirect 302 · jalur skor-saja · import gambar via Excel · campur form+Excel · essay teks wajib di Excel.
