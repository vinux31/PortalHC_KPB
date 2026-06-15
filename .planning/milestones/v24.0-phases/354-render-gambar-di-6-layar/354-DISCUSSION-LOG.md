# Phase 354: Render Gambar di 6 Layar - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 354-render-gambar-di-6-layar
**Areas discussed:** Ukuran gambar per layar, Lightbox, Penempatan gambar opsi, Pola render (DRY)

---

## Ukuran Gambar per Layar

| Option | Description | Selected |
|--------|-------------|----------|
| Seragam 240/120 | Semua layar cap sama (soal 240px, opsi 120px), konsisten _PreviewQuestion 353 | ✓ |
| Peserta lebih besar | StartExam 360/200, lainnya 240/120 | |
| Per-surface kustom | Cap beda tiap layar | |

**User's choice:** Seragam 240/120 (D-01)
**Notes:** img-fluid responsif tetap; file full-res (no-resize 352), cap hanya CSS.

---

## Klik Perbesar (Lightbox)

| Option | Description | Selected |
|--------|-------------|----------|
| Static (no zoom) | Inline capped saja, no JS | |
| Lightbox semua layar | Klik → modal full-res, 6 layar, Bootstrap modal | ✓ |
| Lightbox peserta saja | Zoom 3 layar peserta saja | |

**User's choice:** Lightbox semua layar (D-02)
**Notes:** User tanya best-practice + apakah ukuran = upload. Dijelaskan: LMS (Moodle/Canvas) cap responsif + click-to-expand = best practice untuk diagram teknis; display capped (bukan raw upload), lightbox akses full-res; file full-res krn no-resize Phase 352 D-04. User pilih lightbox semua layar setelah konteks itu.

---

## Penempatan Gambar Opsi

| Option | Description | Selected |
|--------|-------------|----------|
| Bawah teks, block | Gambar di bawah teks opsi, full block — mirror _PreviewQuestion 353 | ✓ |
| Samping teks, inline | Flex row kanan teks | |
| Atas teks | Gambar dominan, teks caption | |

**User's choice:** Bawah teks, block (D-03)
**Notes:** Konsisten 353, aman di mobile.

---

## Pola Render (DRY)

| Option | Description | Selected |
|--------|-------------|----------|
| 1 partial reusable | _QuestionImage/_OptionImage dipakai 6 layar + lightbox trigger | ✓ |
| Inline per view | <img> langsung tiap view (6×), risiko drift | |
| Tag helper / HtmlHelper | C# TagHelper reusable, overhead baru | |

**User's choice:** 1 partial reusable (D-04)
**Notes:** Anti-drift, sejalan lightbox-semua-layar (1 implementasi modal).

## Claude's Discretion

- Bentuk partial (1 parametrik vs 2 terpisah), nama file.
- Mekanisme lightbox persis (1 modal global vs per-image).
- Cap sebagai param partial vs hardcode.

## Deferred Ideas

- Server-side resize (ditolak 352 D-04).
- xUnit + Playwright UAT end-to-end → Phase 355.
- Gambar Excel import → out of scope (353 D-09).
