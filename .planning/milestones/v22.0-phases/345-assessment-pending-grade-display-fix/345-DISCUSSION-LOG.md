# Phase 345: assessment-pending-grade-display-fix - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input ke planning/research/execution agent.
> Keputusan ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-04
**Phase:** 345-assessment-pending-grade-display-fix
**Areas discussed:** Badge color, Stats edge/display, Minor fold-in scope

**Catatan:** Phase ini sangat pre-decided (label "Menunggu Penilaian" terkunci, passRate exclude pending terkunci, file:line + 4-plan split + out-of-scope terkunci di REQUIREMENTS/ROADMAP). Hanya 3 keputusan visual/edge yang masih terbuka — dibahas dalam 1 batch.

---

## Badge color — "Menunggu Penilaian"

| Option | Description | Selected |
|--------|-------------|----------|
| Amber (bg-warning text-dark / Colors.Orange.Darken2) | Sinyal "menunggu aksi penilaian"; beda jelas dari Failed merah & dash abu. Rekomendasi. | ✓ |
| Abu-abu (bg-secondary / Grey.Darken2) | Netral murni, konsisten dash "—"; kurang menonjol. | |
| Biru info (bg-info) | Informasional; risiko rancu dengan badge "Completed" lama. | |

**User's choice:** "sesuai reko kamu" → Amber.
**Notes:** Diterapkan konsisten 4 surface (web ×3 + PDF). → D-01.

---

## Stats edge & display (UserAssessmentHistory)

| Option | Description | Selected |
|--------|-------------|----------|
| Tambah indikator pending | Fix passRate (denominator graded) + "Menunggu Penilaian: N" + all-pending → "—". Rekomendasi. | (via discretion) |
| Minimal (angka saja) | Hanya passRate; all-pending → 0%; no UI baru. | |
| Kamu putuskan | Claude pilih sesuai pola kartu existing. | ✓ |

**User's choice:** "Kamu putuskan" → Claude's discretion.
**Notes:** Claude lock default = opsi rekomendasi (indikator pending + passRate graded-denominator + all-pending "—"); averageScore exclude-pending direkomendasi tapi planner konfirmasi. → D-04..D-07.

---

## Minor fold-in scope

| Option | Description | Selected |
|--------|-------------|----------|
| Ikutkan semua sekarang | Excel CMPController:694 + grup PassedCount 2759/2775/2789/2821. Rekomendasi. | ✓ |
| Hanya Excel | Skip grup PassedCount (surface Monitoring beda). | |
| Defer keduanya | Jaga scope 345 seketat 5 REQ inti. | |

**User's choice:** "Ikutkan semua sekarang".
**Notes:** Grup PassedCount = hanya konsistensi hitung; bila nambah MenungguPenilaianCount ke PrePost rows dianggap melebar, defer ke Phase 348. → D-09/D-10.

## Claude's Discretion

- Tampilan stats + averageScore (D-05/D-06/D-07).
- Mekanisme akses konstanta `PendingGrading` di Razor (D-02).

## Deferred Ideas

- JS SignalR `AssessmentMonitoringDetail.cshtml:1409` → follow-up.
- Inklusi sesi Status="Menunggu Penilaian" murni di GetUnifiedRecords → Phase 346 REC-07.
- MenungguPenilaianCount untuk PrePost sub-rows → potensi Phase 348.
