# Phase 369: Sync H1 Search-Drop Fix main → ITHandoff - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 369-sync-h1-search-drop-fix-main-ithandoff
**Areas discussed:** Metode sync, Jejak commit, Kedalaman UAT

---

## Metode sync

| Option | Description | Selected |
|--------|-------------|----------|
| Cherry-pick saja | Ambil 1 commit 14e7adc5 (2 file, verified clean); full merge main tetap event terpisah pre-handoff IT | ✓ |
| Full merge main sekarang | Lunasi 13 commit sekaligus; risiko: 1 konflik docs, F1 recompute org Level di startup, regression surface lebar saat 363 paralel | |
| Cherry-pick + jadwalkan merge | Sama opsi 1 + entri eksplisit ROADMAP/backlog untuk full merge | |

**User's choice:** Cherry-pick saja (rekomendasi)
**Notes:** Hutang full merge sudah tercatat di STATE.md "Push pending IT" + memory — entri tambahan tidak diperlukan.

---

## Jejak commit

| Option | Description | Selected |
|--------|-------------|----------|
| Pakai -x | Pesan asli + "(cherry picked from commit ...)" — audit trail, dedup-friendly saat merge | ✓ |
| Pesan asli polos | Histori bersih tanpa jejak asal | |

**User's choice:** Pakai -x (rekomendasi)

---

## Kedalaman UAT

| Option | Description | Selected |
|--------|-------------|----------|
| Unit + live browser | Suite penuh + 1 skenario Playwright @5277 search nama Tab Input Records (konvensi CLAUDE.md) | ✓ |
| Unit test saja | Test H1 ter-pick + suite hijau dianggap cukup | |

**User's choice:** Unit + live browser (rekomendasi)

## Claude's Discretion

- Penanganan konflik cherry-pick tak terduga (target = guard identik main)
- Urutan langkah verifikasi

## Deferred Ideas

- Full merge main→ITHandoff (13 commit + 1 konflik docs) — event terencana pre-handoff IT
