# Phase 349: ManageAssessment + Monitoring LOW Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-05
**Phase:** 349-manageassessment-monitoring-low-polish
**Areas discussed:** Reset filter (MAP-06), Empty/feedback (MAP-05/07/08), Monitoring summary cards (MAP-10/12), Pre-Post token action (MAP-17)

---

## Reset filter (MAP-06)

| Option | Description | Selected |
|--------|-------------|----------|
| Reset Semua Filter | Relabel tombol → hapus search+kategori+status sekaligus. Jujur & simpel. | ✓ |
| Hapus pencarian saja | Clear-search-only, preserve kategori/status (HTMX). Lebih rumit. | |

**User's choice:** Reset Semua Filter (rekomendasi)
**Notes:** User non-teknis — pilih yang tak bikin bingung.

---

## Pesan saat data kosong (MAP-05/07/08)

| Option | Description | Selected |
|--------|-------------|----------|
| Pesan + hitungan | Filter-aware empty msg + "Menampilkan X dari Y" + aria-live polite. | ✓ |
| Pesan minimal | Cuma ganti teks empty-state filter-aware, tanpa baris hitungan. | |

**User's choice:** Pesan + hitungan (rekomendasi)

---

## Kartu ringkasan Monitoring Detail (MAP-10/12)

| Option | Description | Selected |
|--------|-------------|----------|
| Tambah Abandoned + tombol pintar | Kartu Abandoned (Total pas) + "Akhiri Semua Ujian" kondisional. | ✓ |
| Biarkan apa adanya | Tanpa Abandoned card + tombol selalu muncul. | |

**User's choice:** Tambah Abandoned + tombol pintar (rekomendasi)

---

## Aksi token grup Pre-Post (MAP-17)

| Option | Description | Selected |
|--------|-------------|----------|
| Tambah Regenerate Token | Pre-Post token group → regenerate (target LinkedGroupId, koord MAM-01). | ✓ |
| Cukup View Detail | Minimal, tanpa regenerate dari list. | |

**User's choice:** Tambah Regenerate Token (rekomendasi)
**Notes:** Sejalan fix MAM-01 Phase 348 (route-by-LinkedGroupId sudah benar).

## Claude's Discretion

Sisa 19 MAP item (MAP-01/02/03/04/09/11/13/14/15/16/18/19/20/21/22/23) = mekanis / spec-decided. Untuk item dengan pilihan "atau" minor: MAP-11 drop dead var, MAP-19 selalu render CompletionDisplayText, MAP-22 drop dead param (dipilih Claude, dicatat di CONTEXT §Claude's Discretion).

## Deferred Ideas

- Search Monitoring list ke Nama/NIP per-user (list aggregate — out of scope).
- Resource-file i18n framework (codebase pakai inline Indonesia).
