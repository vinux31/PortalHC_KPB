# Phase 350: Team View Search Scope + Export Parity - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-05
**Phase:** 350-team-view-search-scope-export-parity
**Areas discussed:** Dropdown Lingkup + scope, Paritas Export Category, Micro-copy Indonesia, Cakupan test SF-01/06

---

## Dropdown "Lingkup" + scope predikat (SF-01 + SF-02)

| Option | Description | Selected |
|--------|-------------|----------|
| 4 opsi +Assessment | Tambah opsi "Assessment" eksplisit; Keduanya = Nama+Training+Assessment; granular | |
| 3 opsi relabel jujur | Nama / "Judul Kegiatan" (value tetap "Training", cakup assessment) / Keduanya; minim ripple | ✓ |
| Minimal keep label | Diam-diam extend Training+Keduanya, fix placeholder saja; kurang jujur | |

**User's choice:** 3 opsi relabel jujur.
**Notes:** Value internal `"Training"` dipertahankan (label tampil "Judul Kegiatan") → server switch (`:402`) + `WorkerDataServiceSearchTests.cs` + sessionStorage tak break. Predikat assessment-title masuk blok post-load `:402-417` (mirror union Category `:373-381`), D-07 preserved.

---

## Paritas Export Category (SF-06)

| Option | Description | Selected |
|--------|-------------|----------|
| Simetris — narrow assessment per-Category | Sejajar training; cocok teks REQ SF-06 | ✓ |
| Keep category=null + dokumentasi by-design | Biarkan asimetris, dokumentasikan | |

**User's choice:** Simetris.
**Follow-up (constraint ditemukan saat verifikasi):** `GetAllWorkersHistory` mengabaikan `category` untuk assessment (`:217-218` hanya training); `AssessmentAttemptHistory` tak punya kolom Category (`:116`).

### Sub-keputusan: nasib baris archived assessment saat Kategori aktif

| Option | Description | Selected |
|--------|-------------|----------|
| Drop archived saat Kategori aktif | Narrow current sessions by Category + exclude archived (no Category col); konsisten worker-narrowing | ✓ |
| Keep archived unfiltered | Current di-narrow, archived tetap muncul; sedikit asimetris | |
| Batalkan simetris → keep category=null | Revisi balik ke by-design | |

**User's choice:** Drop archived saat Kategori aktif (Kategori kosong → archived normal).

---

## Micro-copy Bahasa Indonesia (SF-02)

### Label opsi tengah dropdown

| Option | Description | Selected |
|--------|-------------|----------|
| Judul Kegiatan | Ringkas, netral awam, cakup training+assessment | ✓ |
| Judul Training/Assessment | Eksplisit tapi panjang di col-md-3 | |
| Judul | Paling ringkas, kurang konteks | |

### Placeholder + hint

| Option | Description | Selected |
|--------|-------------|----------|
| Lengkap jujur | "Cari nama/NIP, judul training, atau judul assessment..."; hint :107 tetap | ✓ |
| Ringkas "kegiatan" | "Cari nama/NIP atau judul kegiatan..." | |
| Serahkan ke Claude | Discretion | |

**User's choice:** Label "Judul Kegiatan" + placeholder lengkap jujur; hint `:107` dipertahankan verbatim.

---

## Cakupan test SF-01/SF-06

| Option | Description | Selected |
|--------|-------------|----------|
| xUnit mirror + Playwright UAT Team View | Unit predicate-mirror + invariant D-07 + export-worker-list; Playwright search→worker muncul + export link param | ✓ |
| xUnit predicate-mirror saja | Skip Playwright; lebih ringan | |
| xUnit + Playwright + assert isi xlsx | Paling tebal; mahal/rapuh (parse xlsx) | |

**User's choice:** xUnit mirror + Playwright UAT Team View (sesuai pola v22 & STATE "tests folded per phase").

---

## Claude's Discretion

- Mekanisme tepat extend Category-narrow assessment di Export (`GetAllWorkersHistory` vs in-controller).
- Struktur assertion Playwright + seed/data approach.
- Penempatan test export-worker-list ([Fact] terpisah vs gabung).

## Deferred Ideas

- SF-03 / SF-04 / SF-05 / SF-07 → Phase 351 (Worker Detail + cross-surface consistency).
- Tidak ada scope-creep baru dari diskusi.
