# Phase 414: Fix Visibilitas History Jawaban Admin/HC saat AllowAnswerReview OFF - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-22
**Phase:** 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie
**Areas discussed:** VM flag, Bypass scope, Seam+test, Admin message

---

## Bentuk flag efektif (ViewModel)

| Option | Description | Selected |
|--------|-------------|----------|
| Field baru CanReviewAnswers | VM property efektif baru; AllowAnswerReview tetap raw toggle; controller+view pakai flag baru | ✓ |
| Overwrite AllowAnswerReview | Set VM.AllowAnswerReview = nilai efektif; makna field ambigu | |

**User's choice:** Field baru CanReviewAnswers
**Notes:** Sesuai arah ROADMAP "expose field efektif di VM, view gate pakai flag baru".

---

## Cakupan bypass (siapa selalu lihat history)

| Option | Description | Selected |
|--------|-------------|----------|
| Semua non-owner | viewer ≠ owner selalu lihat (Admin/HC/L3/L4 section-scoped); cek tunggal user.Id != assessment.UserId | ✓ |
| Hanya Admin/HC (L1-L2) | bypass hanya roleLevel<=2; L3/L4 tetap kena gate | |

**User's choice:** Semua non-owner
**Notes:** User menambahkan permintaan kapabilitas baru: fasilitas HC/Admin memberi akses / kirim URL ke non-owner (atasan) untuk lihat & review, kemungkinan terkait tinjau jawaban essay ("perlu analisa ulang"). Di-flag sebagai scope creep → dicatat DEF-01 (fase sendiri). Phase 414 tetap sempit.

---

## Pola seam + test

| Option | Description | Selected |
|--------|-------------|----------|
| Pure helper + unit test | static CanReviewAnswers(allowReview, isOwner); unit test admin-bypass + owner-gated; no DB | ✓ |
| Inline + integration test | ekspresi inline; integration test via WebApplicationFactory + DB seed | |

**User's choice:** Pure helper + unit test
**Notes:** Konsisten pola IsResultsAuthorized / IsParticipantRemoved. Sejalan lesson 999.12 (hindari replica tautologis).

---

## Pesan untuk Admin saat toggle OFF

| Option | Description | Selected |
|--------|-------------|----------|
| Tampil review diam | Admin lihat history tanpa banner ekstra | |
| Tampil + nota admin | Admin lihat history + badge "Peserta tak bisa lihat (review OFF)" | ✓ |

**User's choice:** Tampil + nota admin
**Notes:** Kondisi nota diturunkan di view dari CanReviewAnswers && !AllowAnswerReview — tak butuh field VM ke-3.

## Claude's Discretion
- Teks/ikon/warna/penempatan badge nota admin; nama persis helper.

## Deferred Ideas
- DEF-01: Fasilitas grant akses / share URL hasil ke atasan (non-owner) + keterkaitan tinjau jawaban essay — kapabilitas baru, fase sendiri.
