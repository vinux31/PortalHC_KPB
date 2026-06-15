# Phase 352: Data Foundation + Image-Only Upload - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-06
**Phase:** 352-data-foundation-image-only-upload
**Areas discussed:** Format gambar, Batas ukuran file, Kompresi/resize otomatis

---

## Format Gambar

| Option | Description | Selected |
|--------|-------------|----------|
| JPG/PNG saja | Sesuai spec. Foto (JPG) + diagram/screenshot (PNG). Kompatibel, magic-byte sudah ada | ✓ |
| JPG/PNG + WebP | +WebP (~30% kecil). Perlu tools export + magic-byte baru | |
| JPG/PNG + WebP + GIF | +GIF animasi. Jarang perlu, nambah kompleksitas | |

**User's choice:** JPG/PNG saja
**Notes:** Screenshot HP WebP/HEIC harus dikonversi admin (accepted).

---

## Batas Ukuran File

| Option | Description | Selected |
|--------|-------------|----------|
| 5MB | Aman diagram hi-res / foto HP tanpa kompres | ✓ |
| 2MB (spec asli) | Hemat storage + load cepat, best-practice LMS | |
| 10MB (sama cert) | Paling longgar, risiko load lambat + boros | |

**User's choice:** 5MB
**Notes:** OVERRIDE spec §4/§6 + REQ IMG-01/02 (2MB→5MB). Konten = diagram teknis kilang hi-res.

---

## Kompresi/Resize Otomatis

| Option | Description | Selected |
|--------|-------------|----------|
| Simpan apa adanya | Simpel, reuse SaveFileAsync 100%, andalkan img-fluid+lazy | ✓ |
| Auto-resize dimensi (1600px) | File turun, load cepat. Perlu lib image processing | |
| Auto-kompres kualitas (JPG ~80%) | Ukuran kecil, risiko teks buram. Perlu lib + tuning | |

**User's choice:** Simpan apa adanya
**Notes:** SkiaSharp ada di proyek (Phase 320) tapi sengaja tak dipakai. Trade-off berat load 5MB diterima; mitigasi loading=lazy phase 354.

---

## Claude's Discretion

- Bentuk method image-only (method baru vs parameterize) — planner.
- Nama persis konstanta/method, urutan kolom migration, gaya fixture test.

## Deferred Ideas

- Thumbnail / auto-resize bila storage/load jadi masalah.
- Dukungan WebP/HEIC bila admin sering upload dari HP.
- Auto-kompres kualitas (ditolak — risiko buram).
