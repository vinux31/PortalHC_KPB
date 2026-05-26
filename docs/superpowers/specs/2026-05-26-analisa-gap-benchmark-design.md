# Design Spec — `analisa-gap-benchmark.html`

**Date**: 2026-05-26
**Topic**: Sertifikat Ecosystem — Gap Analysis + Industry Benchmark + Engineering Recommendation
**Target file**: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`
**Version**: v1.0
**Audience**: Executive (manager/board PT Pertamina KPB)
**Style baseline**: Match `docs/sertifikat-ecosystem/index.html` v1.0 (Bootstrap 5 + Mermaid + sidebar TOC sticky)

---

## 1. Goal & Scope

Buat laporan HTML standalone yang:

1. **Inventarisasi gap** sistem sertifikat yang sudah ada (derive dari `index.html` §10 — 8 fungsi + 8 sistem + 9 logic).
2. **Riset kapabilitas baru** (sistem belum ada) — derive dari benchmark industri (Open Badges, blockchain credential, expiry pipeline, skill matrix, peer endorsement, badge wallet, revocation list, multi-language cert, dll.).
3. **Benchmark vs industri lain** — Migas (Shell/Chevron/Exxon) + HRIS/LMS (Workday/SuccessFactors/Cornerstone/Moodle) + Cert std internasional (ISO 17024/IACET/Open Badges 1EdTech/Accredible/Credly).
4. **Rekomendasi engineering** prioritas tinggi — top quick-win + big-bet, dengan integration sketch + library/standard.

### Out of Scope (YAGNI)

- Tidak ada interactive filter/search/datatable JS (executive scan only).
- Tidak ada hardcoded tanggal kalendar / owner / sprint number di roadmap.
- Tidak ada code snippet implementasi penuh (sketch architecture saja).
- Tidak ada cost estimasi Rupiah (effort person-week saja).
- Tidak ada Government/SOE Indonesia benchmark dedicated (skipped per Q2 — masuk callout regulatori migas saja).
- Tidak migrate konten existing dari `index.html` (referensi cross-link saja).

---

## 2. Audience & Output Profile

- **Reader**: Manager Pertamina KPB + board reviewer + senior dev/IT lead. Bukan junior dev.
- **Reading pattern**: Scan executive summary → drill quadrant chart → ke tabel ICE → ke rekomendasi engineering top 10.
- **Output panjang**: ~1800-2000 baris HTML (Bootstrap markup verbose ~30%).
- **Bahasa**: English section title (match `index.html` style) + Bahasa Indonesia konten body (CLAUDE.md compliance untuk content).

---

## 3. Research Mode

**Hybrid static + WebSearch spot-check**:

- **Static knowledge** (Jan 2026 cutoff) — Skeleton, best-practice generic, capability mapping, terminology.
- **WebSearch live** — Spot-check 8-15 URL untuk klaim faktual kritis:
  - ISO 17024 clause specifik (renewal cycle, surveillance audit)
  - Workday Learning capability snapshot
  - SAP SuccessFactors Learning module current name
  - Open Badges 1EdTech spec version + endorsement model
  - Accredible / Credly platform feature
  - Shell OpenAcademy / Chevron Learning publicly disclosed
  - Permen ESDM relevansi sertifikat operator kilang
  - BNSP cert validity period current

Citation per klaim: tampilkan URL + access date di §10.

---

## 4. Architecture

### File layout

```
docs/sertifikat-ecosystem/analisa-gap-benchmark.html  (single-file, no asset baru)
```

**Dependency** (CDN, sama dengan `index.html`):
- Bootstrap 5.3 CSS
- Bootstrap Icons
- Mermaid 10.x (quadrantChart, timeline)
- highlight.js (kalau ada code snippet inline)

**Tidak ada asset baru** (no png/svg/js file). Self-contained HTML.

### Layout pattern

Replikasi `index.html`:
- **Sidebar kiri** sticky TOC (collapsed by default mobile).
- **Konten kanan** scrollable, badge `§n` per section header.
- **Footer** dengan last-updated + version + author.

### Mermaid usage

- `quadrantChart` untuk §6 (Effort vs Impact matrix, top 20 item plotted).
- `timeline` untuk §9 (Now / Next / Later roadmap fase).

### Color palette (consistent dengan `index.html`)

| Token | Bootstrap class | Use |
|-------|-----------------|-----|
| Quick-win | `bg-success` / `text-success` | High impact, low effort |
| Big-bet | `bg-primary` / `text-primary` | High impact, high effort |
| Fill-in | `bg-warning` / `text-warning` | Low impact, low effort |
| Time-sink | `bg-secondary` / `text-secondary` | Low impact, high effort |

### Print stylesheet

`@media print { ... }` ~30 baris:
- Hide sidebar TOC
- Expand semua accordion (display all collapsibles)
- Page-break-before per `<section>` major
- Convert Mermaid SVG ke print-safe (default Mermaid sudah SVG, OK)

---

## 5. Section Breakdown (10 sections)

### §1 — Executive Summary

**Konten**:
- Ringkasan 1-page: total gap count (~37), top 5 quick-win + top 3 big-bet, total estimated effort (person-week).
- 3 metric badge besar: "Total Gap", "Quick-Win Identified", "Estimated Effort (PW)".
- 1-paragraph narrative motivasi laporan.

**Panjang**: ~80-100 baris HTML.

### §2 — Methodology

**Konten**:
- Sumber: derive dari `index.html` §10 + benchmark research.
- Framework: **ICE** (Impact × Confidence × Ease) — replace RICE karena Reach flat di context internal Pertamina KPB.
- 2x2 quadrant chart: Effort (X) × Impact (Y).
- Citation method: WebSearch URL + access date.
- Inline mini-glossary 6 term internal: KKJ, CMP, CDP, BP, PROTON, UPA. (Plus link ke `index.html#§12` full glossary.)

**Panjang**: ~120-150 baris HTML.

### §3 — Existing System Gap Inventory

**Konten**:
- Tabel ~25 row derive dari `index.html` §10 (8 fungsi + 8 sistem + 9 logic, dedup).
- Kolom: ID (G-01..G-25), Kategori, Description, Status (existing), Related Bug ID (link ke `bug-findings.html#bug-XX` kalau ada overlap), Source (link ke `index.html#§10`).
- Filter visual via Bootstrap badge color per kategori (Fungsi/Sistem/Logic).

**Panjang**: ~250-300 baris HTML.

### §4 — Net-New Capability Research (Missing Systems)

**Konten**:
- ~12 capability di luar §10, sourced dari benchmark:
  1. Open Badges 1EdTech standard issuer
  2. Blockchain credential verification (Accredible/Credly model)
  3. Automated expiry + renewal pipeline (cron + email + grace period)
  4. Skill matrix integration (link cert ke skill taxonomy)
  5. Peer-to-peer endorsement (Workday-style)
  6. Digital badge wallet / portfolio
  7. Public cert revocation list (CRL-style)
  8. Multi-language cert generation (EN/ID)
  9. QR-code dynamic verify (vs static PNG)
  10. SCORM/xAPI export untuk LMS portability
  11. CPD point accumulation tracker
  12. External assessor / proctor mode (ISO 17024-compliant)
- Per item: ID (N-01..N-12), Definisi, Sumber benchmark (vendor refer), Manfaat, Complexity estimate (S/M/L).

**Panjang**: ~280-320 baris HTML.

### §5 — Industry Benchmark

Sub-sections:

**§5.1 Migas / Energy**
- Shell OpenAcademy, Chevron Learning, ExxonMobil competency cert system.
- Per vendor: capability snapshot (3-5 fitur), gap Portal HC vs vendor, citation URL.
- **Callout box**: Konteks regulatori migas Indonesia — Permen ESDM 1/2018, BNSP cert validity 3 tahun, IADC WellCAP, sertifikasi operator pengilangan.

**§5.2 HRIS / LMS**
- Workday Learning, SAP SuccessFactors Learning, Cornerstone, Moodle, TalentLMS.
- Sama format: snapshot + gap + citation.

**§5.3 International Cert Standards**
- ISO 17024 (Personnel certification body)
- IACET (continuing education)
- Open Badges 1EdTech (digital credential)
- Blockchain credential — Accredible, Credly platform model.
- Per standar: scope, klausa relevan, current Portal HC compliance status (Yes/Partial/No), citation.

**Visual**: Accordion collapsible per vendor (avoid wall-of-text).

**Panjang**: ~450-500 baris HTML.

### §6 — 2x2 Prioritization Matrix

**Konten**:
- Mermaid `quadrantChart`: X-axis = Effort (Low → High), Y-axis = Impact (Low → High).
- Plot **top 20 item by Impact** (cherrypick dari §3 + §4 gabungan, sisanya tetap di tabel §7).
- Legend kotak warna: Quick-win (success/green), Big-bet (primary/blue), Fill-in (warning/yellow), Time-sink (secondary/grey).
- Brief paragraph interpretasi per quadrant.

**Panjang**: ~180-220 baris HTML.

### §7 — ICE Score Table

**Konten**:
- Tabel ~37 row (semua gap §3 + net-new §4).
- Kolom: ID, Item, Impact (1-10), Confidence (%), Ease (1-10), **ICE Score** (computed = I × C × E / 100), Quadrant.
- Top 10 highlighted dengan background tint.
- Default sort: ICE Score descending.
- Tidak ada JS sort (static — pre-sorted di HTML).

**Panjang**: ~250-300 baris HTML.

### §8 — Engineering Recommendation

**Konten**:
- ~8-10 rekomendasi detail untuk top quick-win + big-bet.
- Per rec card:
  - Problem statement (1-2 sentence)
  - Proposed solution (3-5 sentence)
  - Library / standard / vendor reference (CDN/NuGet/library name)
  - Integration sketch (text-based, e.g., "extend `SertifikatService.cs:Generate()` to emit Open Badges 2.0 JSON-LD, add `/api/badge/verify` endpoint, store `BadgeAssertion` table.")
  - Trade-off (ADR-lite: alternatif yang ditolak + alasan)
  - Effort estimate (person-week, range)
- Format: Bootstrap card per rec, color-coded badge per quadrant.

**Panjang**: ~400-450 baris HTML.

### §9 — Roadmap Tentative

**Konten**:
- Mermaid `timeline` chart 3 fase: **Now** (next 3 bulan), **Next** (3-9 bulan), **Later** (9-18 bulan).
- Tidak ada hardcoded date kalendar — kategorikal saja.
- Per fase: list item ID dari §8 yang masuk fase.
- Brief paragraph rationale grouping.

**Panjang**: ~150-180 baris HTML.

### §10 — Sources & References

**Konten**:
- Daftar 8-15 URL WebSearch (per claim citation).
- Format: Numbered list, URL + title + access date (2026-05-26).
- Internal cross-ref:
  - `index.html#§10` (Gap Analysis source)
  - `index.html#§11` (Spec Cross-Check)
  - `index.html#§12` (Full Glossary)
  - `bug-findings.html` (Bug detail untuk Related Bug ID di §3)

**Panjang**: ~80-100 baris HTML.

---

## 6. Total Length Estimate

| Section | Baris HTML estimasi |
|---------|---------------------|
| §1 Executive Summary | 80-100 |
| §2 Methodology | 120-150 |
| §3 Existing Gap | 250-300 |
| §4 Net-New Capability | 280-320 |
| §5 Industry Benchmark | 450-500 |
| §6 2x2 Matrix | 180-220 |
| §7 ICE Score Table | 250-300 |
| §8 Engineering Rec | 400-450 |
| §9 Roadmap | 150-180 |
| §10 Sources | 80-100 |
| **Sidebar + boilerplate** | ~150 |
| **TOTAL** | **~2400-2770 baris** |

Catatan: lebih besar dari estimasi awal 1800-2000 karena adjustment §3 dari ~25→25, §4 12 item, §5 3 sub-section vendor. Target final **~2500 baris**.

---

## 7. Data Sources

### Internal

- `docs/sertifikat-ecosystem/index.html` — §9 (12 finding static audit), §10 (25 gap item), §11 (13 row spec cross-check), §12 (16-term glossary), §13 (14-phase migration timeline).
- `docs/sertifikat-ecosystem/bug-findings.html` — 16 bug (12 Portal + 4 Doc) untuk cross-ref Related Bug ID di §3.
- Code: `Services/SertifikatService.cs`, `Controllers/SertifikatController.cs`, `Data/PortalHCContext.cs` Sertifikat entity — referenced via path:line di §8 integration sketch.

### External (WebSearch spot-check)

- shell.com/sustainability/skills.html (OpenAcademy)
- chevron.com/learning
- workday.com/products/talent-management/learning
- sap.com/products/hcm/successfactors-learning
- iso.org/standard/52993.html (ISO 17024)
- 1edtech.org/standards/open-badges
- accredible.com / credly.com platform docs
- esdm.go.id (Permen ESDM)
- bnsp.go.id (cert validity)

(Real URLs verified at write time, listed in §10.)

---

## 8. Quality Gates

Before commit, verify:

- [ ] All 10 sections present + populated (no TBD placeholder).
- [ ] Mermaid `quadrantChart` renders (≤20 dots, readable).
- [ ] Mermaid `timeline` renders (3 fase, ≤10 item per fase).
- [ ] ICE Score table sorted descending, top 10 highlighted.
- [ ] All Related Bug ID di §3 link valid ke `bug-findings.html#bug-XX`.
- [ ] All URL di §10 reachable (WebSearch confirmed) + access date 2026-05-26.
- [ ] Print stylesheet test: `Ctrl+P` preview menunjukkan sidebar hidden + accordion expanded.
- [ ] Konsistensi bahasa: title EN, body ID.
- [ ] Bootstrap valid HTML5 (no console error open di browser).
- [ ] Sidebar TOC scroll-spy aktif (highlight section active).

---

## 9. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Mermaid quadrantChart breaks dengan >20 dot | Limit plot top 20 by Impact di §6, sisanya hanya di tabel §7 |
| WebSearch citation URL drift/404 di masa depan | Snapshot title + access date 2026-05-26, archive.org link kalau ada finding kritis |
| ICE score subjektif | Tampilkan rubric I/C/E scoring di §2 metodologi + allow user override post-review |
| Confidential vendor info (Shell/Chevron internal) | Cite publicly disclosed sources only (corporate sustainability report, public press release) |
| File 2500+ baris berat load | Bootstrap collapse per vendor di §5, lazy Mermaid render via `mermaid.init` on-demand |

---

## 10. Open Questions (Resolved at Review Gate)

Tidak ada open question — semua keputusan locked via Q1-Q8 brainstorming sequence. User review spec ini di gate berikutnya untuk approve sebelum lanjut ke writing-plans.

---

**Status**: Spec ready for user review.
**Next step**: User approves → invoke `writing-plans` skill untuk turunkan spec ke detailed implementation plan.
