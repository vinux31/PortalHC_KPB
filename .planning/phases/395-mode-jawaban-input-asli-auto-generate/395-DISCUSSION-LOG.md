# Phase 395: Mode jawaban (input asli + auto-generate) - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Keputusan tercatat di CONTEXT.md. Q-01..Q-10 awal ada di 395-QUESTIONS.json/html (sesi `--power`).
> Log ini = sesi discuss interaktif lanjutan yang me-resolve 3 open product question.

**Date:** 2026-06-18
**Phase:** 395-mode-jawaban-input-asli-auto-generate
**Sesi:** Interactive follow-up (resolve open_questions dari sesi --power)
**Areas discussed:** Auto-gen essay-room · Target>ceiling · Seed recipe

---

## Q-OPEN-1: Auto-gen pada room ber-essay

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid | Auto-gen pilih MC/MA benar; HC ketik skor essay (+teks) manual sebelum commit | ✓ |
| Full-auto (essay proporsional) | Essay diberi skor otomatis proporsional; no manual (rekom Claude lama Q-08=b, ditolak) | |

**User's choice:** Hybrid
**Notes:** Konsisten dgn D-08=c. Full-auto essay tetap di Deferred Ideas.

---

## Q-OPEN-2: target > ceiling MC/MA-only (room essay berat)

| Option | Description | Selected |
|--------|-------------|----------|
| Blocking + switch input-asli | Warning blocking per-worker, arahkan HC switch input-asli + naikkan skor essay manual | ✓ |
| Non-blocking (commit di ceiling) | Izinkan commit pakai skor ceiling + warning info saja | |

**User's choice:** Blocking + switch input-asli
**Notes:** User minta penjelasan dulu (konsep "ceiling" tak paham) → dijelaskan dgn contoh (10 MC@5 + 1 essay@50 → ceiling 50%, target 80% mustahil). Setelah paham, pilih blocking. Integritas sertifikasi: jangan diam-diam cap di ceiling.

---

## Q-OPEN-3: Resep seed auto-gen (D-07)

| Option | Description | Selected |
|--------|-------------|----------|
| NIP + room + target | Hash stabil (NIP + Title+Category+CompletedAt + target); pola tak berulang lintas room | ✓ |
| NIP saja | Sederhana, tapi pola identik lintas room = sintetis | |

**User's choice:** NIP + room + target
**Notes:** Prinsip "pola tak berulang lintas room" (seakan online) disetujui.

## Claude's Discretion
(Tak berubah — lihat CONTEXT.md: lokasi lapisan auto-gen, resep seed eksplisit, MA degenerate, model state per-worker, serialisasi #AnswersJson, lokasi rule TextAnswer-wajib.)

## Deferred Ideas
(Tak berubah — full-auto essay, preview nomor cert, matrix/accordion, blok-sampai-target-persis, Excel=396, Pre/Post=397.)

## Carry-in
- Phase 394 UAT cosmetic (label "Pilihan Ganda"→"Single Answer") di-fold ke 395 (file sama disentuh). Lihat CONTEXT.md Folded Todos.
