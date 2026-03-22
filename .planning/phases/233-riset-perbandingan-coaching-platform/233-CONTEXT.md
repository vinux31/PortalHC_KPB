# Phase 233: Riset & Perbandingan Coaching Platform - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Riset perbandingan platform coaching industri (360Learning, BetterUp, CoachHub) vs portal KPB untuk 4 area Proton: Setup, Execution, Monitoring, Completion. Output berupa 1 dokumen HTML yang menjadi lens untuk audit Phases 234-237.

</domain>

<decisions>
## Implementation Decisions

### Scope Platform
- **D-01:** Riset 3 platform: 360Learning, BetterUp, CoachHub — sesuai requirements, tidak tambah platform lain
- **D-02:** Tidak boleh substitusi atau tambah platform di luar 3 ini, meskipun ada gap coverage

### Struktur Perbandingan
- **D-03:** Struktur per area Proton (Setup, Execution, Monitoring, Completion) — paralel dengan Phase 234-237
- **D-04:** Tiap area mulai dengan deskripsi as-is portal KPB (baseline) lalu perbandingan dengan platform luar — gap terlihat jelas
- **D-05:** Semua 4 area diriset dengan kedalaman yang sama, tidak ada prioritas area

### Format & Jumlah Dokumen
- **D-06:** 1 dokumen HTML lengkap di `docs/` — semua 4 area + ringkasan perbandingan + rekomendasi dalam satu file
- **D-07:** Format HTML konsisten dengan dokumen project lainnya (commit-log.html, audit-assessment-training-v8.html, dll)

### Kedalaman Rekomendasi
- **D-08:** Ranking 3-tier: Must-fix (bug/UX kritis), Should-improve (best practice gap), Nice-to-have (enhancement)
- **D-09:** Tiap rekomendasi di-map ke target phase (234/235/236/237)
- **D-10:** Rekomendasi juga mencakup validasi/pengayaan fitur differentiator (DIFF-01 workload indicator, DIFF-02 batch approval, DIFF-03 bottleneck analysis)

### Claude's Discretion
- Styling dan layout HTML dokumen riset
- Kedalaman narasi per aspek berdasarkan relevansi
- Cara mendeskripsikan flow platform luar (teks naratif vs step-by-step)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone & Requirements
- `.planning/ROADMAP.md` — Phase 233-237 details, success criteria, dependency chain
- `.planning/REQUIREMENTS.md` — RSCH-01, RSCH-02, RSCH-03 requirement definitions + DIFF-01/02/03

### Prior Research (pola riset sebelumnya)
- `.planning/phases/228-best-practices-research/228-CONTEXT.md` — Keputusan riset Phase 228 sebagai reference pola

### Existing Portal Pages (untuk baseline as-is)
- `Views/CDP/PlanIdp.cshtml` — Silabus display dan guidance tabs (Setup area)
- `Views/CDP/CoachingProton.cshtml` — Coaching tracking dan monitoring (Monitoring area)
- `Controllers/CDPController.cs` — Proton coaching business logic (Execution/Completion area)
- `Controllers/ProtonDataController.cs` — Silabus, Guidance, Override CRUD (Setup area)
- `Controllers/AdminController.cs` — Coach-Coachee Mapping, Track Assignment (Setup area)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- HTML docs di `docs/` folder — template styling reference untuk output dokumen
- Phase 228 research docs — pola format dan struktur yang sudah terbukti

### Established Patterns
- Dokumen riset menggunakan format HTML di `docs/`
- Struktur: tabel perbandingan fitur + narasi analisis per aspek
- Ranking 3-tier dengan phase mapping untuk traceability

### Integration Points
- Output dokumen riset menjadi referensi utama untuk Phase 234-237
- Rekomendasi di-map langsung ke phase number — researcher dan planner tiap phase audit baca seksi yang relevan

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 233-riset-perbandingan-coaching-platform*
*Context gathered: 2026-03-22*
