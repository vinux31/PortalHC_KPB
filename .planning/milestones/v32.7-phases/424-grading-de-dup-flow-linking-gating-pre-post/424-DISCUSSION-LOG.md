# Phase 424: Grading De-dup + Flow/Linking + Gating Pre→Post - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 424-grading-de-dup-flow-linking-gating-pre-post
**Areas discussed:** Gating Pre→Post, Manajemen peserta (GRDF-06), Validasi essay server-side, Dedupe scoring & link semu

---

## Area selection

| Option | Selected |
|--------|----------|
| Gating Pre→Post (GRDF-01) | ✓ |
| Manajemen peserta simetris (GRDF-06) | ✓ |
| Validasi essay server-side (GRDF-07) | ✓ |
| Dedupe scoring & link semu (GRDF-02+04) | ✓ |

---

## A. Gating Pre→Post (GRDF-01, HIGH)

| Pertanyaan | Pilihan | User pick |
|---|---|---|
| Definisi "Pre selesai" | Completed saja / Completed+Lulus | **Completed saja** |
| Orphan/Standard Post | Izinkan (gate hanya bila Pre ADA) / Blok semua | **Izinkan (gate hanya bila Pre ADA)** |
| UX terblok | Redirect+pesan (pola existing) / Info lobby+disable | **Redirect+pesan (pola existing)** |

**Notes:** Gate disisipkan di `StartExam` mengikuti rantai gate existing. Pasangan Pre ditentukan via pairing GRDF-03 (link eksplisit), bukan pola judul.

---

## B. Manajemen peserta simetris (GRDF-06) — DROPPED

| Pertanyaan | Pilihan | User pick |
|---|---|---|
| Pendekatan hapus peserta | Minimal+konsisten / Bikin sekarang / Tunda total | (user minta penjelasan dulu, lalu cek v32.5) |
| Dedup tambah-peserta (PA-08) | Helper bersama / diskresi | Helper bersama (kemudian ikut di-defer) |
| Keputusan akhir GRDF-06 | **Buang dari 424 — tunda ke merge v32.5** / Tetap garap | **Buang dari 424 — tunda ke merge v32.5** |

**Notes:** User mempertanyakan kenapa garap kalau v32.5 sudah ada. Verifikasi branch `main`: `AssessmentAdminController.cs:2358-2880` sudah implement `AddParticipantsLive`/`RemoveParticipantCoreAsync`/`RemoveParticipantLive`/`RestoreParticipantLive`/`DeleteAssessmentPeserta` (komentar `:2719` "D-04 fix stub mati EditAssessment.cshtml:666" = persis dead-ref audit; `:2660` PRMV-04 "simetri Pre/Post"). GRDF-06 dikonfirmasi sudah selesai di v32.5 → dibuang dari 424 untuk hindari duplikasi + konflik merge. PA-08 ikut di-defer (alur add di-rewrite v32.5).

---

## C. Validasi essay kosong server-side (GRDF-07 / VAL-03)

| Pertanyaan | Pilihan | User pick |
|---|---|---|
| Timeout vs on-time | Tolak hanya on-time (timeout finalize) / Tolak semua jalur | **Tolak hanya on-time; timeout tetap finalize** |
| Cakupan tolak | Blokir seluruh submit (semua wajib) / Peringatan saja | **Blokir seluruh submit; semua essay wajib terisi** |

**Notes:** Timeout/auto-submit pertahankan perilaku Phase 386 PXF-04. Server jadi otoritas (client flushEssay bisa gagal — lesson Phase 413).

---

## D. Dedupe scoring & link semu (GRDF-02 + GRDF-04)

| Pertanyaan | Pilihan | User pick |
|---|---|---|
| Jawaban dobel pakai mana | Jawaban terakhir (last-write-wins) / Jawaban pertama | **Jawaban terakhir (last-write-wins)** |
| Nilai sesi lama boleh berubah? | Tidak boleh (paritas) / Recompute semua | **Tidak boleh berubah (paritas, characterization test)** |
| Pasangan Pre/Post palsu | Mulai sekarang saja (data lama dibiarkan) / + bersihkan data lama | **Mulai sekarang saja; data lama dibiarkan** |

**Notes:** User minta penjelasan sederhana dulu (jawaban dobel, paritas, link semu) → dijelaskan dgn contoh → semua pilih opsi rekomendasi (forward-only non-destruktif).

---

## Claude's Discretion

- GRDF-03 (pairing satu sumber) — mekanik penyatuan tiga jalur, terfilter UserId; kanonik = link eksplisit bukan pola judul.
- GRDF-05 (ElapsedSeconds + ExtraTimeMinutes) — ekstrak helper durasi aktif, fix export under-report.
- Penempatan/nama kelas-fungsi murni baru; bentuk pesan ramah.

## Deferred Ideas

- GRDF-06 (manajemen peserta) → v32.5 merge (rekonsiliasi REQUIREMENTS/ROADMAP).
- Cleanup retroaktif link semu lama → backlog (bila perlu).
- Tech-debt timing/token/write-on-GET → Phase 425.
