# Phase 419: Export Label Section + Polish + Test/UAT Milestone - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 419-export-label-section-polish-test-uat-milestone
**Areas discussed:** Format label export (PAG-04), Guard LinkPrePost × Section, Kedalaman fix ET-warning (DEF-416-01), Scope UAT + audit milestone, Folded todo

---

## Format Label Export (PAG-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Excel band-header + PDF heading | Reorder kolom per (SectionNumber, Order) + baris header merged "Section {n}: {Nama}"; PDF heading antar-blok. Paling jelas, konsisten dua format. | ✓ |
| Excel prefix kolom + PDF heading | Tetap urut q.Order, prefix header "S{n}·Soal i"; tak reorder (lebih aman test lama) tapi padat. | |
| PDF heading saja | Minimal; Excel matrix tak disentuh. Risiko PAG-04 minta Excel juga. | |

**User's choice:** Excel band-header + PDF heading
**Notes:** Matrix `AddDetailPerSoalSheet` di-reorder per Section + merged header; PDF heading antar-grup. Backward-compat: no-Section → grup "Lainnya" pertahankan q.Order.

---

## Guard LinkPrePost × Section

| Option | Description | Selected |
|--------|-------------|----------|
| Hard-block bila Section beda | LinkPrePost tolak link bila struktur Section Pre≠Post, pesan jelas. Perilaku BARU, integritas terjaga. | ✓ |
| Audit-only | Verifikasi SEC-06 sync sudah clone Section; tak tambah guard. | |
| Soft-warning | Izinkan link + peringatan bila beda. | |

**User's choice:** Hard-block bila Section beda
**Notes:** Reuse fingerprint identitas per-Section SEC-04. LinkPrePost menaut room existing → bisa divergen tanpa guard.

---

## Kedalaman Fix ET-warning (DEF-416-01 + IN-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Full re-spec predikat | DistinctEt = pool ET lintas paket-saudara vs K=min(count Section antar sibling) + grouping SectionNumber + test positif. | ✓ |
| Minimal align grouping | Hanya SectionId→SectionNumber; biarkan predikat (tetap nyaris dead). | |

**User's choice:** Full re-spec predikat
**Notes:** Lokasi `AssessmentAdminController.cs:7673-7680`. Tetap NON-BLOCKING (D-416-03).

---

## Scope UAT + Audit Milestone

| Option | Description | Selected |
|--------|-------------|----------|
| Lifecycle Section inti | Section+shuffle+pagination+opsi 2–6 end-to-end real-browser. | ✓ |
| Inject v32.2 × Section | Inject hasil manual saat paket ber-Section + opsi 5–6. | ✓ |
| LinkPrePost 397 × Section | Link Pre↔Post struktur sama vs beda (uji guard). | ✓ |
| Add/Remove v32.5 × Section | Tambah/hapus peserta live saat ujian ber-Section + pagination. | ✓ |

**User's choice:** Keempat dipilih (semua interaksi lintas-milestone di-UAT live)
**Notes:** Audit milestone PASSED 20/20 REQ sebelum ship; migration=FALSE.

---

## Folded Todo

| Option | Description | Selected |
|--------|-------------|----------|
| Fold ke 419 | Cleanup data test lokal pasca-UAT/pra-ship masuk scope 419. | ✓ |
| Tetap di backlog todo | Item terpisah, manual. | |

**User's choice:** Fold ke 419
**Notes:** `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` — langkah pasca-UAT (snapshot→restore, journal `cleaned`).

## Claude's Discretion

- Penyembunyian header "Lainnya" saat 1 grup tanpa Section (backward-compat visual).
- Styling Excel band-header (merge/warna) & layout heading PDF.
- Struktur/nama file test + spec Playwright.

## Deferred Ideas

- SREP-01 (breakdown skor per-Section) — D-12 tidak untuk v32.6.
- SAMP-01 (sampling N dari M) — v2.
- Excel zero-config — milestone quick-win terpisah.
