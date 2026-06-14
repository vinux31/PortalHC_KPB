# Phase 370: Hapus Window 7-Hari (Tampilan Default Tanpa Batas) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 370-hapus-window-7-hari-tampilan-default-tanpa-batas
**Areas discussed:** Nasib helper + 3 test, Badge counter Closed, Guard regresi pengganti, AsNoTracking Monitoring, Pagination Monitoring, Data UAT sesi lama

---

## Nasib helper + 3 test

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus total (Recommended) | Hapus helper + 2 call site + var sevenDaysAgo; komentar 260611-m9r dibersihkan | ✓ |
| Sisakan sebagai komentar sejarah | Hapus helper, tinggalkan komentar jejak keputusan | |
| Kamu yang putuskan | Claude's Discretion saat planning | |

**User's choice:** Hapus total

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus file test (Recommended) | AssessmentSearchWindowTests.cs dihapus utuh — helper hilang, tak compile | ✓ |
| Repurpose file | Isi diganti test perilaku baru | |

**User's choice:** Hapus file test

---

## Badge counter Closed

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan all-time (Recommended) | Konsisten "tanpa batas"; badge = row count saat filter dipilih; zero kode ekstra | ✓ |
| Badge Closed dibatasi | Dihitung dengan batas waktu — inkonsisten badge ≠ row count | |
| Kamu yang putuskan | Claude's Discretion | |

**User's choice:** Biarkan all-time

---

## Guard regresi pengganti

| Option | Description | Selected |
|--------|-------------|----------|
| Grep-guard + UAT (Recommended) | Grep zero sisa sevenDaysAgo/ApplySevenDayWindow + full suite + UAT @5277 | ✓ |
| Test integration baru | InMemory DbContext assert sesi >7 hari muncul default | |
| Playwright e2e committed | Spec e2e committed pola 361-04 | |

**User's choice:** Grep-guard + UAT

---

## AsNoTracking Monitoring (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Ikut fix (Recommended) | Tambah .AsNoTracking() 1 baris di query Monitoring — read-only, risiko nol | ✓ |
| Strict scope | Jangan sentuh, defer ke fase perf | |

**User's choice:** Ikut fix

---

## Pagination Monitoring (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Defer (Recommended) | Kapabilitas baru + ubah view — catat Deferred Ideas | ✓ |
| Masuk scope 370 | Tambah paging sekarang — scope membesar | |

**User's choice:** Defer

---

## Data UAT sesi lama (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Data legacy existing (Recommended) | 12 InProgress + 9 Open legacy + Post Test OJT >7 hari di DB lokal; zero seed | ✓ |
| Seed khusus | Seed temporary SEED_WORKFLOW + snapshot/restore | |

**User's choice:** Data legacy existing

---

## Claude's Discretion

- Wording komentar pengganti di 2 method (atau tanpa komentar — yang penting komentar stale hilang)
- Urutan langkah edit vs hapus test (kompilasi hijau tiap commit)

## Deferred Ideas

- Pagination AssessmentMonitoring — kandidat fase perf/UX nanti
