# Phase 320: Assessment Export Per-Peserta Excel - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-21
**Phase:** 320-assessment-export-per-peserta-excel
**Areas discussed:** Sheet ordering, Chart visual style, PLAN sub-numbering strategy, Playwright regression test

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Urutan sheet per-peserta | OrderBy FullName/NIP/Status/Score | ✓ |
| Chart visual style | Color, embed size | ✓ |
| PLAN sub-numbering strategy | 1 / 2 / 3 / 5 file split | ✓ |
| Playwright regression test | Manual only vs hybrid vs parse xlsx | ✓ |

**User's choice:** All 4 areas selected (multi-select).

---

## Sheet Ordering

| Option | Description | Selected |
|--------|-------------|----------|
| FullName asc | Research default, konsisten dgn monitoring UI | ✓ |
| NIP asc | Konsisten dgn sheet name prefix | |
| Status grouping + name | Completed dulu lalu Abandoned | |
| Score desc | Top performer dulu | |

**User's choice:** FullName asc (Recommended)
**Notes:** Sesuai research existing + UI ManageAssessment pattern.

---

## Chart Visual Style — Color

| Option | Description | Selected |
|--------|-------------|----------|
| Biru research RGB(54,162,235) | Chart.js default, kontras tinggi, ready di research code | ✓ |
| Pertamina merah | Brand primary identity | |
| Pertamina biru tua | Brand secondary navy | |

**User's choice:** Biru research (Recommended)
**Notes:** Chart konteks data internal, bukan brand-facing artifact.

## Chart Visual Style — Embed Size

| Option | Description | Selected |
|--------|-------------|----------|
| 400×400 (render 500 → scale down) | Sharp + kompak ~22 row | ✓ |
| 500×500 native | No scaling, ~28 row | |
| 300×300 mini | Kompak tapi label bisa kecil | |

**User's choice:** 400×400 (Recommended)
**Notes:** Sesuai research code (`WithSize(400, 400)`).

---

## PLAN Sub-Numbering Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| 3 grouping (helpers / controller / perf-uat) | T1-3 + T4-10 + T11-12, paralel-able antar PLAN | ✓ |
| 1 monolitik | Semua 12 task di 320-01-PLAN.md | |
| 5 fine-grained | T1 + T2-3 + T4-5 + T6-10 + T11-12 | |
| 2 split by feature | T1-5 foundation + T6-12 per-peserta+perf | |

**User's choice:** 3 grouping (Recommended)
**Notes:** Atomic per layer, mudah review pisah, paralel-able execution wave.

---

## Playwright Regression Test

| Option | Description | Selected |
|--------|-------------|----------|
| Manual UAT only | Sesuai research + pattern v16.0 | |
| Tambah Playwright minimal | Auth+download flow regression | |
| Tambah Playwright + xlsx parse | Cover sheet content via openpyxl/ClosedXML | |
| **Other (hybrid)** | via playwright, dan manual yang gak bisa playwright | ✓ |

**User's choice:** Hybrid — "via playwright, dan manual yang gak bisa playwright"

### Hybrid Split Confirmation

| Option | Description | Selected |
|--------|-------------|----------|
| Playwright: auth+download flow | Admin login + HC login + Worker 403 + benchmark | ✓ |
| Playwright + xlsx parse | + sheet count, "Summary" name, cell sample | |
| Playwright minimal smoke | 1 test cuma Admin download | |

**User's choice:** Playwright auth+download flow (4 test)
**Notes:**
- Playwright cover: Admin login + export download, HC login + export download, Worker → 403, response time <30s 50 peserta benchmark
- Manual cover: sheet content (Summary tab, {NIP}_{FullName} format, ET tabel isi, chart visual, Detail Jawaban ✓/✗, Manual Entry hyperlink, buka di Excel + LibreOffice, sheet name 31-char truncate + no collision)

---

## Done Gate

| Option | Description | Selected |
|--------|-------------|----------|
| Ready bikin CONTEXT.md | Write + commit + advance | ✓ |
| Bahas area tambahan | More gray areas (breaking change comms, commit granularity, dll) | |

**User's choice:** Ready bikin CONTEXT.md

---

## Claude's Discretion

- PNG cache lock strategy (`Dictionary` + `lock` per research, atau `ConcurrentDictionary`) — Claude pick efficient
- Field name verification `Penyelenggara/Kota/SubKategori/CertificateType/ManualSertifikatUrl` (already verified Models/AssessmentSession.cs:130-147)
- `ws.Columns().AdjustToContents()` placement (per research Task 9 step 2)
- Worker EF pre-load query strategy (Claude execute persis research code blocks)

## Deferred Ideas

- Brand color chart variant (parameterize stroke/fill kalau future request)
- xlsx content assert di Playwright (upgrade kalau test infra eventually setup)
- Test project setup (xUnit/NUnit) — future hygiene improvement
- PDF/JSON export variant — future phase
- Todo `realtime-assessment.md` reviewed but not folded — kemungkinan scope Phase 321 SignalR
