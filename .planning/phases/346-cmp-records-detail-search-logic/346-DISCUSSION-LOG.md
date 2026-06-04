# Phase 346: cmp-records-detail-search-logic - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 346-cmp-records-detail-search-logic
**Areas discussed:** REC-06 query strategy, REC-09 badge clarification, REC-04 old role-check, REC-04 test depth

**Konteks:** Spec `2026-06-04-cmp-records-enhancement-design.md` sudah APPROVED dgn 6 keputusan terkunci (D-01..D-06). Diskusi hanya menutup 4 gray area sisa yang spec biarkan terbuka. User pilih diskusikan keempatnya.

---

## REC-06 — Strategi query search Training

| Option | Description | Selected |
|--------|-------------|----------|
| Post-load filter | Filter worker-list setelah TrainingRecords load per-user (pola category-narrow L370-378); 'Keduanya' load section dulu lalu filter in-memory | ✓ |
| Pre-filter subquery user-ids | Subquery TrainingRecords.Judul narrow user-ids di SQL sebelum load; lebih efisien section besar tapi kompleks | |

**User's choice:** Post-load filter (rekomendasi)
**Notes:** Data per-section realistis kecil, konsisten pola existing. Pre-filter subquery di-defer sbg optimasi opsional bila profiling lambat.

---

## REC-09 — Perjelas badge "Assessment"

| Option | Description | Selected |
|--------|-------------|----------|
| Relabel header 'Assessment Lulus' | Ganti teks header kolom (view-only), tak sentuh field CompletedAssessments | ✓ |
| Tooltip 'Jumlah assessment lulus' | Header tetap + title attr; butuh hover (tak jalan mobile) | |
| Keduanya (relabel + tooltip) | Paling eksplisit, sedikit redundan | |

**User's choice:** Relabel header 'Assessment Lulus' (rekomendasi)
**Notes:** Eksplisit tanpa hover, jelas di mobile. Field `CompletedAssessments` tetap tak di-rename (spec larang cross-3-file).

---

## REC-04 — Cek role-string lama (defense-in-depth)

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus cek lama | roleLevel≤3 sudah cover Admin(1)+HC(2); hapus `roles.Contains` redundan → authz single-source | ✓ |
| Pertahankan (defense-in-depth) | Biarkan cek string sbg lapis ekstra kalau mapping roleLevel berubah | |

**User's choice:** Hapus cek lama (rekomendasi)
**Notes:** Authz tunggal-sumber lebih bersih + mudah di-test matrix.

---

## REC-04 — Kedalaman test authz (security-sensitive)

| Option | Description | Selected |
|--------|-------------|----------|
| Matrix penuh 8 kasus | owner/Admin/HC/L3/L4-same/L4-other/L5/L6 × (Results+Certificate+CertificatePdf) + guard Section-null | ✓ |
| Ringkas kasus kunci | Hanya boundary kritis (L4-same OK, L4-other Forbid, Section-null Forbid, L5 owner) | |

**User's choice:** Matrix penuh 8 kasus (rekomendasi)
**Notes:** REC-04 melonggarkan akses hasil assessment tim → security-sensitive, butuh regression-proof penuh.

---

## Claude's Discretion

- Plan split / wave structure (ROADMAP belum lock; reko isolasi REC-04 jadi plan sendiri).
- Mekanisme akses konstanta PendingGrading di Razor vs C# (ikut pola 345 D-02).
- Pre-filter subquery REC-06 sbg optimasi opsional (default post-load).

## Deferred Ideas

- REC-10 (category filter server-side Worker Detail) — DROP (over-eng).
- POL-01..10 (i18n/a11y/DRY) — Phase 347.
- AssessmentMonitoringDetail.cshtml:1409 SignalR — out of scope.
- MAM/MAP ManageAssessment+Monitoring — Phase 348/349; Tab3 History PendingGrading (MAP-20) dicakup REC-07 → tambah ke UAT 346.
