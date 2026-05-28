# Kickoff-PROTON Slide 25/26/30 Koreksi — Design Spec

**Tanggal:** 2026-05-28
**File target:** `docs/Kickoff-PROTON.html`
**Total slide:** 32 → 30 (drop 2 slide)
**Scope:** Edit slide 25 (3 sub-koreksi) + drop slide 26 + drop slide 30 + renumber + total update

---

## 1. Konteks

File `Kickoff-PROTON.html` saat ini versi 4.0 (Part 1 + Part 2 Assessment, 32 slide, tag `kickoff-proton-v4.0-part2` @ commit `e16ea41e`). Part 2 Assessment (slide 22–31) berisi flow ujian end-to-end. Review materi mengidentifikasi:

- Slide 25 (Alur Assessment Tahun 1-2 vs Tahun 3): aktor + urutan approval salah; outcome Mahir di step alur duplicate dengan slide 31.
- Slide 26 (Anatomi Soal ER 4-tabel): terlalu teknis untuk audience kickoff.
- Slide 30 (Cara Grading HC POV): tidak perlu coachee tahu workflow internal HC.

## 2. Perubahan Slide 25

**Lokasi:** `docs/Kickoff-PROTON.html` line 3114–3150.

### 2.1 Step 1 (kedua kolom Tahun 1-2 dan Tahun 3)

- **Sebelum (Tahun 1-2 line 3128):** `Coachee submit <strong>Deliverables</strong> (ProtonDeliverableProgress)`
- **Sebelum (Tahun 3 line 3140):** `Coachee submit <strong>Deliverables</strong>`
- **Sesudah (kedua kolom):** `<strong>Coach Submit Evidence Coaching</strong>`

Alasan: aktor sebenarnya Coach (bukan Coachee); deliverables = evidence coaching yang dikumpulkan Coach.

### 2.2 Step 2 (kedua kolom)

- **Sebelum (line 3129 dan 3141):** `<strong>3-Role Approval:</strong> HC + SrSpv + SectionHead`
- **Sesudah:** `<strong>3-Role Approval:</strong> Sr Spv &rarr; Section Head &rarr; HC (Final Review)`

Alasan: urutan approval sebenarnya sequential bukan paralel; HC posisi terakhir sebagai Final Review.

### 2.3 Step 6 Tahun 3 — DROP

- **Sebelum (line 3145):** `🏅 <strong>Sertifikasi Mahir</strong> granted`
- **Sesudah:** baris dihapus.

Tahun 3 jadi 5 step (1. Coach Submit Evidence Coaching, 2. 3-Role Approval, 3. Interview offline, 4. SubmitInterviewResults, 5. HC create ProtonFinalAssessment + CompetencyLevel 0-5).

Tahun 1-2 tetap 6 step.

Alasan: outcome Mahir sudah dijelaskan slide 31 (Outcome — Sertifikat & CompetencyLevel) card kanan; di alur step terminal hanya membingungkan (Mahir = outcome, bukan action).

## 3. Drop Slide 26

**Lokasi:** line 3152–3211 (block `data-slide="26"` Sistem Ujian — Anatomi Soal).

Hapus full block termasuk comment marker `<!-- ================= SLIDE 26: ... ================= -->`.

## 4. Drop Slide 30

**Lokasi:** line 3384–3422 (block `data-slide="30"` Cara Grading — HC POV).

Hapus full block termasuk comment marker.

## 5. Renumber

Mapping data-slide attribute setelah drop:

| Lama | Baru | Title |
|------|------|-------|
| 1–25 | 1–25 | unchanged |
| 26   | —    | DROP (Anatomi Soal) |
| 27   | 26   | Grading Engine |
| 28   | 27   | Status Lifecycle & Timer |
| 29   | 28   | Cara Ujian — Coachee POV |
| 30   | —    | DROP (Cara Grading HC POV) |
| 31   | 29   | Outcome — Sertifikat & CompetencyLevel |
| 32   | 30   | Penutup / Terima Kasih |

Edit per slide affected:
- `data-slide="27"` → `data-slide="26"`, badge `27 / 32` → `26 / 30`
- `data-slide="28"` → `data-slide="27"`, badge `28 / 32` → `27 / 30`
- `data-slide="29"` → `data-slide="28"`, badge `29 / 32` → `28 / 30`
- `data-slide="31"` → `data-slide="29"`, badge `31 / 32` → `29 / 30`
- `data-slide="32"` → `data-slide="30"`, (slide 32 tidak punya badge; cek)

Update badge denominator `/ 32` → `/ 30` di slide 1–25 yang tidak terkena renumber data-slide:
- `1 / 32` → `1 / 30`
- `2 / 32` → `2 / 30`
- ...
- `25 / 32` → `25 / 30`

## 6. JS + Counter Update

**Line 3500:** `<span class="slide-counter" id="slideCounter">1 / 32</span>` → `1 / 30`

**Line 3506:** `const TOTAL = 32;` → `const TOTAL = 30;`

## 7. Slide 31 (jadi 29) — Tie-back Reference

Line 3476: `Lihat <strong>Slide 19 (Outcome — Sertifikasi Mahir & IDP)</strong> di Part 1` — slide 19 di Part 1 unaffected, tidak perlu diubah.

Card kanan "🏅 Sertifikasi Mahir" tetap dipertahankan — outcome konseptual, bukan step alur.

## 8. Cross-Reference Check

Grep `Slide 26|slide 26|Sl26|Slide 30|slide 30|Sl30` hasilnya hanya match di H1 title kedua slide tersebut. Tidak ada cross-ref tekstual di slide lain → safe drop tanpa broken reference.

## 9. Verifikasi Manual

Setelah edit, buka file di browser dan verifikasi:

1. Total slide counter menampilkan `1 / 30` (footer kanan bawah).
2. Navigate ke slide 25 → konfirmasi 3 sub-koreksi visible.
3. Navigate ke slide 26 (baru) → tampil "Grading Engine" (mantan slide 27).
4. Navigate ke slide 28 (baru) → tampil "Cara Ujian — Coachee POV" (mantan slide 29).
5. Navigate ke slide 29 (baru) → tampil "Outcome — Sertifikat & CompetencyLevel" (mantan slide 31).
6. Navigate ke slide 30 (baru, terakhir) → tampil "Terima Kasih" (mantan slide 32).
7. Tombol next di slide 30 disabled.
8. Tidak ada slide kosong / data-slide attribute skip.

## 10. Versioning

- Tag baru: `kickoff-proton-v4.1-koreksi-slide25-26-30` (parent dari v4.0-part2 e16ea41e).
- Commit message format: `feat(kickoff-proton-v4.1): koreksi slide 25 alur + drop slide 26 & 30 + renumber 32→30`.
- Update footer line 3490 jika ada label versi: `Kickoff PROTON · Versi 4.0 (Part 1 + Part 2 Assessment) · Mei 2026` → `Versi 4.1`.

## 11. Out of Scope

- Slide 1–24 tidak diubah selain badge `/ 32` → `/ 30`.
- Slide 31 (jadi 29) content tidak diubah selain data-slide attribute + badge.
- Tidak ada perubahan CSS, layout, atau script logic selain `TOTAL` constant.
- Tidak ada perubahan file lain di `docs/`.
