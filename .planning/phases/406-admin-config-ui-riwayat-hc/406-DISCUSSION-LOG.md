# Phase 406: Admin Config UI + Riwayat HC - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-06-21
**Phase:** 406-admin-config-ui-riwayat-hc
**Areas discussed:** Presentasi Riwayat HC, Kedalaman per-soal, Lock & warning card, Layout field card

---

## Presentasi Riwayat HC (RTK-08)

| Option | Selected |
|--------|----------|
| Modal per-pekerja (Bootstrap, accordion attempt + expand per-soal) | ✓ |
| Page terpisah (mirror EssayGrading) | |
| Baris expand inline | |

**Choice:** Modal per-pekerja. **Notes:** read-only view → modal ringan + konsisten modal existing; EssayGrading page krn editing, riwayat cuma view.

---

## Kedalaman per-soal

| Option | Selected |
|--------|----------|
| Penuh (teks soal + jawaban + ✓/✗ + skor) | ✓ |
| Ringkas (✓/✗ + skor) | |

**Choice:** Penuh. **Notes:** arsip simpan QuestionText+AnswerText+IsCorrect+AwardedScore; HC perlu lihat kenapa gagal (tujuan snapshot D-10). XSS-encode.

---

## Lock & warning card

| Option | Selected |
|--------|----------|
| No-lock + warning inline (alert kuning dekat MaxAttempts) | ✓ |
| Lock saat ujian mulai (seperti shuffle) | |

**Choice:** No-lock + warning inline. **Notes:** D-02 retroaktif butuh config changeable anytime; warning non-blocking saat MaxAttempts < RetakeMaxAttemptsUsedInGroup.

---

## Layout field card

| Option | Selected |
|--------|----------|
| Number input + helper + progressive disclosure | ✓ |
| Dropdown MaxAttempts + number cooldown | |

**Choice:** Number input + helper + progressive disclosure. **Notes:** toggle reveal 2 number input (1-5 / 0-168) + helper ('0=tanpa jeda','jam'), stacked, card setelah shuffle card, mirror styling.

## Claude's Discretion
- Markup detail, id elemen, struktur accordion, penempatan binding Step 3, modal pre-render vs AJAX.

## Deferred Ideas
None — riwayat pekerja + gating + endpoint worker = 407; test+Playwright+security = 408.
