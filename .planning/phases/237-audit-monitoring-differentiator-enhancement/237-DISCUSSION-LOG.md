# Phase 237: Audit Monitoring & Differentiator Enhancement - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-23
**Phase:** 237-audit-monitoring-differentiator-enhancement
**Areas discussed:** Dashboard & Chart accuracy, Batch approval HC, Workload & bottleneck, Override & export audit

---

## Dashboard & Chart Accuracy

| Option | Description | Selected |
|--------|-------------|----------|
| Audit accuracy saja | Claude investigasi query stats dan Chart.js data, fix bugs tanpa tambah card baru | ✓ |
| Audit + tambah card baru | Audit accuracy + tambah stat cards baru (avg days pending, completion rate %) | |

**User's choice:** Audit accuracy saja
**Notes:** Tidak perlu card baru

| Option | Description | Selected |
|--------|-------------|----------|
| Audit saja, keep existing | Pastikan data chart akurat tanpa tambah chart baru | |
| Tambah chart bottleneck | Tambah chart baru untuk bottleneck analysis | ✓ |

**User's choice:** Tambah chart bottleneck

| Option | Description | Selected |
|--------|-------------|----------|
| Bar chart horizontal | Top 5-10 deliverable paling lama pending, horizontal bar | ✓ |
| Tabel ranked list | Tabel sortable dengan kolom detail | |
| Card + tabel combo | Summary card + expandable tabel detail | |

**User's choice:** Bar chart horizontal

---

## Batch Approval HC

| Option | Description | Selected |
|--------|-------------|----------|
| CoachingProton tracking | Tambah checkbox per row + "Approve Selected" button | ✓ |
| Override page | Extend OverrideSave endpoint | |
| Keduanya | Batch approval di kedua halaman | |

**User's choice:** CoachingProton tracking

| Option | Description | Selected |
|--------|-------------|----------|
| HC Review saja | Hanya deliverable "Pending HC Review" yang bisa di-batch approve | ✓ |
| Semua level approval | SrSpv, SH, dan HC masing-masing bisa batch-approve | |

**User's choice:** HC Review saja

| Option | Description | Selected |
|--------|-------------|----------|
| Modal konfirmasi | Tampilkan daftar item sebelum proses | ✓ |
| Langsung proses | Klik langsung proses tanpa konfirmasi | |

**User's choice:** Modal konfirmasi

---

## Workload & Bottleneck

| Option | Description | Selected |
|--------|-------------|----------|
| Mapping page | Kolom "Jumlah Coachee Aktif" per coach di CoachCoacheeMapping | ✓ |
| Dashboard card | Section di dashboard menampilkan top coaches | |
| Keduanya | Mapping page DAN dashboard | |

**User's choice:** Mapping page

| Option | Description | Selected |
|--------|-------------|----------|
| 30 hari | Standar umum untuk coaching cycle | ✓ |
| 14 hari | Lebih agresif — 2 minggu | |
| Claude decide | Claude tentukan berdasarkan data pattern | |

**User's choice:** 30 hari

---

## Override & Export Audit

| Option | Description | Selected |
|--------|-------------|----------|
| Audit trail + transition rules | Audit log completeness + validasi status transition | ✓ |
| Full rewrite | Redesign override flow dari awal | |

**User's choice:** Audit trail + transition rules

| Option | Description | Selected |
|--------|-------------|----------|
| Audit saja | N+1 elimination, projection, role attribute check | |
| Audit + tambah export baru | Audit existing + tambah export action baru | ✓ |

**User's choice:** Audit + tambah export baru

| Option | Description | Selected |
|--------|-------------|----------|
| Export bottleneck report | Excel: deliverable pending >30 hari | ✓ |
| Export coaching tracking | Excel: data CoachingProton tracking sesuai filter | ✓ |
| Export workload summary | Excel: coach workload summary | ✓ |

**User's choice:** Semua 3 export baru dipilih

---

## Claude's Discretion

- Chart.js config detail untuk horizontal bar
- Batch approve endpoint design
- Checkbox UI pattern
- Export file naming dan column layout
- Override audit trail mechanism
- Query optimization detail

## Deferred Ideas

None
