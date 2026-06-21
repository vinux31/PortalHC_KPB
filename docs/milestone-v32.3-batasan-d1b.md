# Batasan v32.3 — Atribusi Sertifikat/Analitik ke Unit Primary (Keputusan D1=b)

**Milestone:** v32.3 — Akun Multi-Unit (1 Bagian)
**Tanggal:** 2026-06-21
**Status:** keterbatasan sadar-desain (diterima saat keputusan milestone)

## Keputusan

**D1=b** — Laporan **agregat** sertifikat/analitik mengatribusikan hasil ke **Unit Primary** pekerja
(`ApplicationUser.Unit`, cermin dari baris `UserUnits` ber-`IsPrimary=1`), **bukan** per-unit penuh.

Konsekuensi memilih `b` (atribusi primary) alih-alih `a` (kolom unit-saat-terbit penuh): **NO migration ke-2** —
fase 400 menjadi "membership listing set-aware" saja, dan `AssessmentSession`/`TrainingRecord` **tidak** menambah
kolom `Unit-at-issue`. Ini menjaga milestone tetap MEDIUM (1 migration: `AddUserUnits` di Fase 399).

## Penting — sertifikat per-track TIDAK hilang

Sertifikat / hasil PROTON per-tahun **tetap tersimpan utuh sebagai histori**:

- Histori per-track disimpan via `ProtonFinalAssessment` (1:1 per `ProtonTrackAssignment`, unique index DB).
- PROTON sekuensial Tahun 1 @ Unit X → Tahun 2 @ Unit Y menyisakan **2 assignment** yang co-exist
  (Tahun 1 inactive + Tahun 2 active) — penugasan/penanda Tahun 1 **tidak dihancurkan** oleh progres ke Tahun 2.
- Yang menggunakan Unit Primary **hanya laporan AGREGAT** (mis. rekap/renewal/analitik tingkat unit), bukan
  penyimpanan sertifikat itu sendiri.

Singkatnya: **cert/laporan agregat atribusi ke unit primary; cert per-track tetap tersimpan utuh sebagai histori
(tidak hilang).**

## Konsekuensi untuk IT/HC

- Sebuah sertifikat yang **diperoleh di Unit Y** (unit non-primary) bisa **muncul di laporan agregat Unit X**
  (unit primary pekerja). Ini perilaku yang diharapkan pada paket ini, bukan bug.
- Bila kelak compliance **per-unit penuh** dibutuhkan (mis. audit per-unit yang ketat), itu pekerjaan lanjutan
  "to v2": menambah kolom **unit-saat-terbit** (`Unit-at-issue`) di `AssessmentSession`/`TrainingRecord` + backfill
  — **ditangguhkan** secara eksplisit di `REQUIREMENTS.md` (di luar scope v32.3).

## Rujukan

- Spesifikasi desain: `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md`
- Roadmap & requirements: `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md` (kategori MU/PSU/CXU/ORG/QA)
- Bukti invariant SQL-riil: `HcPortal.Tests/SingleActiveInvariantSqlTests.cs`,
  `HcPortal.Tests/UnitMembershipInvariantSqlTests.cs`, `HcPortal.Tests/CrossUnitAssignTests.cs`
