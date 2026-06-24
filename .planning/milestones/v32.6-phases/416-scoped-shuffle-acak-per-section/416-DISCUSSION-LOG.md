# Phase 416: Scoped Shuffle (Acak per-Section) - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in 416-CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-23
**Phase:** 416-scoped-shuffle-acak-per-section
**Areas discussed:** Toggle granularity per-Section, Semantik Reshuffle, ET-coverage saat Section sempit, Cakupan verifikasi backward-compat

---

## Toggle granularity per-Section

| Option | Description | Selected |
|--------|-------------|----------|
| 1 saklar per-Section (gate soal+opsi) | Pakai `ShuffleEnabled` existing (415); no migration; simpel | ✓ |
| 2 saklar terpisah (acak-soal vs acak-opsi per-Section) | Lebih fleksibel; butuh kolom DB baru → migration + UI nambah | |

**User's choice:** 1 saklar per-Section.
**Notes:** User awalnya pilih "2 saklar terpisah", lalu minta klarifikasi (bingung hubungan Section / Elemen Teknis / acak-soal / acak-opsi, ingin "fleksibel TAPI mudah input soal"). Setelah dijelaskan peta hierarki + ongkos 2-saklar (migration + UI, manfaat kecil), user **balik ke 1 saklar** dan setuju Section tetap **opsional (on/off)**. Induk assessment tetap 2 toggle terpisah.

## Semantik Reshuffle (>1 paket)

| Option | Description | Selected |
|--------|-------------|----------|
| Re-roll: SET soal bisa beda per-Section | Sampling baru lintas-paket dalam batas Section, deterministik workerIndex | ✓ |
| Hanya URUTAN berubah, set soal sama | Tak ganti soal; menyimpang dari engine existing | |

**User's choice:** Re-roll (default spec).

## ET-coverage saat Section sempit

| Option | Description | Selected |
|--------|-------------|----------|
| Best-effort (tanpa error) | Jamin sebisanya, sama existing | |
| Best-effort + peringatan ke HC saat setup | Tetap best-effort, tampilkan warning bila K < jumlah ET | ✓ |
| Blokir (anggap salah konfigurasi) | Tolak mulai ujian | |

**User's choice:** Best-effort + peringatan ke HC saat setup.
**Notes:** User minta penjelasan simpel ET dulu (Elemen Teknis = label sub-topik tersembunyi buat nyeimbangin sampling). Pilih warning, bukan blokir (Section kecil sah).

## Cakupan verifikasi backward-compat

| Option | Description | Selected |
|--------|-------------|----------|
| Golden-order + determinisme + reshuffle | Bukti kompatibel-mundur kuat | |
| Cukup smoke all-null = perilaku lama | Ringan, bukti lemah | |
| Penuh + Playwright UAT real-browser | Paling kuat, e2e acak per-Section | ✓ |

**User's choice:** Penuh + Playwright UAT real-browser.

## Claude's Discretion
- Bentuk refactor ShuffleEngine (signature, retain-vs-wrap jalur all-null) — planner/researcher.
- Tempat & wording peringatan ET-coverage di UI.

## Deferred Ideas
- 2 saklar terpisah per-Section (acak-soal vs acak-opsi independen) — ditolak (migration + UI cost, manfaat kecil).
- Todo "cleanup data test lokal pasca-367" — reviewed, not folded (ops, bukan 416 scope).
