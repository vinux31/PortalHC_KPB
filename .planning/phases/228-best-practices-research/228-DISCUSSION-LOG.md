# Phase 228: Best Practices Research - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 228-best-practices-research
**Areas discussed:** Kedalaman riset, Format output, Prioritas rekomendasi, Scope platform

---

## Kedalaman Riset

| Option | Description | Selected |
|--------|-------------|----------|
| Fitur-level | Bandingkan fitur apa saja — tabel checklist | |
| UX flow detail | Step-by-step UX flow per platform | |
| Dengan screenshot | UX flow detail + deskripsi visual UI referensi | ✓ |

**User's choice:** Dengan screenshot (deskripsi teks detail, tanpa link)
**Notes:** Semua 4 aspek UX didokumentasikan: renewal flow, exam/assessment flow, real-time monitoring, admin management

---

## Format Output

| Option | Description | Selected |
|--------|-------------|----------|
| Per-topik | 4 dokumen + 1 ringkasan perbandingan | ✓ |
| Satu dokumen besar | Semua topik dalam 1 dokumen | |
| Per-platform | Dokumen per platform | |

**User's choice:** Per-topik — 5 dokumen total

| Option | Description | Selected |
|--------|-------------|----------|
| Tabel + narasi | Tabel perbandingan + narasi analisis | ✓ |
| Tabel saja | Fokus tabel checklist | |
| Narasi saja | Penjelasan mendalam tanpa tabel | |

**User's choice:** Tabel + narasi

| Option | Description | Selected |
|--------|-------------|----------|
| docs/ (HTML) | Konsisten dengan dokumen lain di project | ✓ |
| .planning/phases/228/ | Di phase directory sebagai Markdown | |
| .planning/research/ | Folder research khusus | |

**User's choice:** docs/ sebagai HTML

---

## Prioritas Rekomendasi

| Option | Description | Selected |
|--------|-------------|----------|
| 3-tier | Must-fix, Should-improve, Nice-to-have | ✓ |
| Tanpa ranking | Flat list | |
| Impact-effort matrix | Impact vs effort ranking | |

**User's choice:** 3-tier ranking

| Option | Description | Selected |
|--------|-------------|----------|
| Map ke phase | Tiap rekomendasi di-tag phase targetnya | ✓ |
| Per-halaman saja | Dikelompokkan per halaman portal | |

**User's choice:** Map ke phase (229/230/231/232)

---

## Scope Platform

| Option | Description | Selected |
|--------|-------------|----------|
| Pakai list ROADMAP | Coursera, LinkedIn Learning, HR portals, Moodle, Google Forms Quiz, Examly | ✓ |
| Tambah platform | Tambah platform lain | |
| Kurangi platform | Skip beberapa | |

**User's choice:** Pakai list ROADMAP

| Option | Description | Selected |
|--------|-------------|----------|
| Claude's discretion | Claude riset dan pilih 1-2 HR portal relevan | ✓ |
| SAP SuccessFactors | Fokus SAP | |
| Workday | Fokus Workday | |

**User's choice:** Claude's discretion untuk HR portals

---

## Claude's Discretion

- Pemilihan HR portal spesifik
- Styling/layout HTML dokumen
- Kedalaman narasi per aspek

## Deferred Ideas

None — discussion stayed within phase scope
