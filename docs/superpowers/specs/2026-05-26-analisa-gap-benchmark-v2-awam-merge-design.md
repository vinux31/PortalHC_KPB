# Analisa Gap + Benchmark v2.0 (Awam) — Merge Design

**Tanggal**: 2026-05-26
**Author**: Rino (via Claude Opus 4.7, brainstorming skill)
**Target file**: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` (existing v1.0 executive → refactor jadi v2.0 awam)
**Side-effect file**: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (hapus §9, ganti teaser link)
**Audience baru**: Manager / HC non-IT (sebelumnya: Executive / Board PT Pertamina KPB)
**Backup version**: v1.0 sudah PUSHED origin/main + tag `sertifikat-doc-gap-benchmark-v1.0` (preserved)

---

## 1. Tujuan

Merge konten §9 Gap & Best Practice External (yang sekarang ada di `ekosistem-sertifikat.html` baris ~432-936) ke `analisa-gap-benchmark.html`, dengan reformat audience dari Executive (v1.0) ke awam HC non-IT (v2.0). Hasil: satu dokumen gap analysis konsolidasi audience awam, tidak ada lagi duplikasi konten antara dua file.

Trigger user: "file analisa-gap-benchmark.html dan §9 digabung" + "rubah dulu formatnya analisa gap benchmark, lalu gabung" + "awam bisa memahami".

## 2. Non-Goals

- Tidak ubah `index.html` (developer technical reference, tetap as-is)
- Tidak ubah `bug-findings.html` (bug detail, tetap as-is)
- Tidak hapus tag/commit v1.0 — versi executive preserved di git history (cite `sertifikat-doc-gap-benchmark-v1.0`)
- Tidak rewrite §1-§8 di `ekosistem-sertifikat.html` — hanya hapus §9 + tambah teaser link
- Tidak tambah file baru — operasi murni merge ke existing file

## 3. Locked Decisions (Q-A..Q-D + 4 fix audit)

| Item | Locked |
|---|---|
| Moodle (existing §5.2 LMS reference) | **Drop** — replace dengan Docebo + TalentLMS (per §9.1 user lock) |
| Migas section (existing §5.1: Shell/Chevron/Exxon/PPSDM) | **Keep simplified** — ringkas 1 paragraf per platform, bahasa awam, jadi sub-section §2 |
| G-/N-/R- IDs traceability (existing) | **Keep sebagai badge kecil** disebelah judul gap/rec untuk cross-doc lookup |
| Top 5 Quick-Win + Top 3 Big-Bet (existing §1) | **Distribusi ke roadmap §5** — 5 QW → bucket Quick Win, 2 BB (R-09 +R-10) → bucket Medium/Long-term |
| Layout (sidebar TOC vs mini-nav top) | **Switch ke mini-nav top** — konsisten dengan ekosistem-sertifikat.html awam style |
| Print stylesheet existing (`@media print`) | **Keep** — fitur sudah ada, low-effort retain |
| Acknowledgment section (§10.3 existing) | **Keep simplified** di §6 |
| Cross-link ke ekosistem-sertifikat.html setelah merge | **Update** — replace anchor `#sec-9` (yang akan dihapus) ke link halaman utama ekosistem-sertifikat |
| Cross-link di ekosistem-sertifikat.html footer | **Tambah** link "Gap analysis lengkap: analisa-gap-benchmark.html" di samping link existing ke `index.html` |
| Version tag post-rewrite | `sertifikat-doc-gap-benchmark-v2.0-awam` (annotated, push setelah verify) |
| Audience banner di top file v2 | Pakai pattern `audience-banner` dari ekosistem-sertifikat.html (alert biru border-left info) |

## 4. Tech Stack

| Item | Pilihan |
|---|---|
| File format | Modify existing `analisa-gap-benchmark.html` (replace majority content, preserve `<head>` CSS pattern with adjustments) |
| CSS framework | Bootstrap 5.3.0 + Bootstrap Icons (existing CDN tetap) |
| Components | Bootstrap accordion (top-5 deep-dive), card (benchmark + roadmap), table-responsive (matrix), badge (severity + ID traceability) |
| JS | Theme toggle (port dari ekosistem-sertifikat.html, localStorage `analisa-gap-theme`) + Bootstrap bundle |
| Mermaid | Drop — v1.0 punya Mermaid Gantt di §9 Roadmap, v2.0 ganti dengan card 3-bucket (no Mermaid CDN load) |
| Highlight.js | Drop — v1.0 punya `<pre>` code samples di §8 R-rec, v2.0 simplify ke awam tanpa code |
| Lang attr | `id` |

## 5. Konten v2.0 — 6 Section Breakdown

### §1 Ringkasan Executive Awam (~200 kata)

**Konten**:
- 1-2 paragraf intro: apa itu analisa gap, kenapa penting, untuk siapa
- 4 mini card (h3 angka + caption):
  - **9** Platform Benchmark (5 HRIS+LMS + 4 Migas)
  - **40** Gap & Capability (konsolidasi, dedup)
  - **5** Top Gap Kritis Deep-Dive
  - **3** Bucket Roadmap (QW / Medium / Long)
- 1 paragraf "Cara baca dokumen ini" — section sequence guide

**Source**: Compose baru. Drop "5 Top Quick-Win + 3 Top Big-Bet" highlights existing (sudah distribusi ke §5 roadmap).

### §2 Konteks Benchmark — 9 Platform (~600 kata)

**Sub-section §2.1 — HRIS+LMS Enterprise (5 card)**
- Layout: Bootstrap grid 5 card (col-md-6 col-lg-4)
- Konten: copy verbatim dari §9.1 ekosistem-sertifikat.html — Workday/SAP/Cornerstone/Docebo/TalentLMS
- Disclaimer date per riset 2026-05

**Sub-section §2.2 — Industri Migas (4 card)**
- Layout: Bootstrap grid 4 card (col-md-6 col-lg-3). Konten tiap card max 3 kalimat, tidak akan overflow → tidak perlu accordion fallback.
- Konten: simplify dari existing §5.1 v1.0 (Shell Learning Academy + PetroSkills, Chevron OEMS, ExxonMobil OIMS, PPSDM Migas)
- Per card: nama platform + badge "Migas" + 2-3 kalimat awam (drop istilah teknis OEMS/OIMS detail, replace dengan bahasa "framework operational excellence" + "standar kompetensi domestik")
- Relevansi: 1 kalimat — Pertamina dapat adopsi pattern apa

**Drop**: Moodle (existing §5.2), Internationan Cert Standards (existing §5.3) — too technical untuk awam.

**Footer**: "Sumber: vendor docs + community/blog, per riset 2026-05."

### §3 Gap Matrix Konsolidasi (~800 kata + tabel)

**Source dedup**:
- 25 existing G-* (§3 v1.0) — semua gap existing dari review internal
- 12 existing N-* (§4 v1.0) — net-new capability dari benchmark
- 23 §9.2 dari ekosistem-sertifikat.html — gap awam vs 5 platform HRIS+LMS

**Dedup logic** (estimate ~20 duplikat, hasil ~40 unique gap):
- N-09 QR Verify = §9 #17 Public Verification → merge, gabung badge `N-09 / §9#17`
- N-07 Cert Revocation = §9 #1 Audit Trail (related concept) → keep terpisah, tapi cross-ref
- G-09 Notif scheduling = §9 #4 Multi-channel notif → merge
- G-13 DB Index ValidUntil = perf, no §9 equivalent → keep G-13 only
- (full dedup mapping lock saat implementation, tulis di plan)

**Tabel layout** (table-responsive):
- Kolom: # | Gap | Kategori | Severity | Best Practice (Platform) | Status Portal HC | ID Lookup
- Kategori: 🏗️ Sistem / 🔄 Flow / ✨ Fitur / 🔒 Compliance / ⚡ Performa (5 kategori, expanded dari 3 di §9.2 untuk handle G-13 Perf + G-10 Audit/Compliance)
- Severity: 🔴 Kritis / 🟡 Penting / 🟢 Nice (Bootstrap badge bg-danger/warning text-dark/success)
- ID Lookup column: small monospace badge (mis. `G-09 + §9#4` untuk merged item)

**Sebaran target ~40 gap** (lock saat plan):
- 🏗️ Sistem ~10 / 🔄 Flow ~10 / ✨ Fitur ~10 / 🔒 Compliance ~5 / ⚡ Performa ~5
- Severity ~10 🔴 / ~18 🟡 / ~12 🟢

### §4 Deep-Dive Top-5 Gap Kritis (~600 kata)

**Source**: copy verbatim §9.3 ekosistem-sertifikat.html (5 accordion item: Self-Service Renewal, QR Verify, Bulk Action, HCM Integration, Audit Trail Defensible).

**Modifikasi**: tambah ID badge di accordion header (mis. "1. Self-Service Renewal Portal · §9#9 + N-03").

**Layout**: Bootstrap accordion existing pattern.

### §5 Roadmap 3-Bucket (~400 kata)

**Source merge**:
- 5 R-* Quick-Win existing (R-01 DB Index, R-02 QR Verify, R-03 Email Reminder, R-04 Rate Limit, R-05 Revocation, R-06 Case-Insensitive, R-07 Validator, R-08 Cycle Detection) — total 8 R-* QW
- 2 R-* Big-Bet existing (R-09 Hangfire Pipeline, R-10 Open Badges JSON-LD)
- 5 Quick Win awam dari §9.4
- 5 Medium awam dari §9.4
- 5 Long-term awam dari §9.4

**Bucket logic**:
- **Quick Win (1-3 bulan)**: 8 R-* QW existing (simplify ke bahasa awam, drop code/library detail) + 5 QW §9.4. Hasil ~10-13 item (dedup overlap mis. R-02 QR = §9.4 QR verify).
- **Medium (3-9 bulan)**: R-09 Hangfire Pipeline simplified + 5 Medium §9.4. Hasil ~5-6 item.
- **Long-term (>9 bulan)**: R-10 Open Badges simplified + 5 LT §9.4. Hasil ~5-6 item.

**Layout**: 3 Bootstrap card vertikal (mirror §9.4 pattern: border-success / border-warning / border-danger).

**Tiap item format**: `<strong>Nama gap awam</strong> <small class="text-muted">(R-02 / §9.4)</small> — 1-baris justifikasi`. Badge ID inline untuk traceability.

### §6 Sumber Referensi + Cross-Reference (~150 kata)

**Source merge**:
- §10.1 v1.0 — external WebSearch sources (industri Migas + HRIS/LMS + Cert standards)
- §9.5 ekosistem-sertifikat — 7 sumber utama (Workday/SAP/Cornerstone/Docebo/TalentLMS docs)
- §10.2 v1.0 — internal cross-reference (index.html, bug-findings.html, ekosistem-sertifikat.html)
- §10.3 v1.0 — acknowledgment (Claude Opus 4.7 generated)

**Layout**:
- 6.1 Sumber External (~10 link consolidate, dedup overlap)
- 6.2 Cross-Reference Internal (link ke 3 file lain di folder)
- 6.3 Acknowledgment (simplified 2 baris)

**Format**: `<ul class="small">` dengan `target="_blank" rel="noopener"` untuk external link.

## 6. Modifikasi `ekosistem-sertifikat.html` (Side-Effect)

**Action**:
1. Hapus seluruh `<section id="sec-9">...</section>` (baris ~432-936, ~505 baris)
2. Hapus link `<a href="#sec-9">§9 Gap & Best Practice</a>` dari mini-nav (baris ~41)
3. Tambah teaser paragraf di lokasi yang sama (sebelum footer), gantikan section §9:

```html
<section id="sec-9-teaser">
  <h2><span class="badge bg-secondary">§9</span> Gap & Best Practice External</h2>
  <p>Untuk analisa gap sistem/flow/fitur lengkap (40+ gap) + benchmark 9 platform (5 HRIS+LMS + 4 Migas) + roadmap 3-bucket, lihat dokumen terpisah:</p>
  <div class="d-grid gap-2 col-md-6 mx-auto my-3">
    <a href="./analisa-gap-benchmark.html" class="btn btn-primary btn-lg">
      <i class="bi bi-graph-up-arrow"></i> Buka Analisa Gap + Benchmark v2.0
    </a>
  </div>
  <p class="small text-muted text-center">Dokumen tersebut sebelumnya audience executive (v1.0) — sekarang sudah di-reformat untuk audience awam HC non-IT yang konsisten dengan dokumen ini.</p>
</section>
```

4. Mini-nav link `§9` tetap arahkan ke `#sec-9-teaser` (rename ID atau keep `#sec-9` agar link tetap valid):
   - Pilihan **keep `#sec-9`** (teaser pakai id="sec-9") — simpler, tidak break navigasi anchor
   - Lock: pakai id="sec-9" untuk teaser
5. Footer: tambah link `<a href="./analisa-gap-benchmark.html">analisa-gap-benchmark.html</a>` di samping link existing ke `index.html`

**Estimate file size post-edit**: 972 → ~480-490 baris (kembali dekat ke 467 baseline + teaser ~20 baris).

## 7. File Size & Acceptance Criteria

### `analisa-gap-benchmark.html` v2.0
- **Estimate**: ~750-850 baris (dari 914 baris v1.0, sedikit lebih ringkas karena drop ICE table + Quadrant + R-rec code detail)
- **Acceptance criteria**:
  1. ✅ Audience banner top: "Dokumen untuk Manager/HC non-IT" + link ke index.html (developer) + link ke ekosistem-sertifikat.html (panduan awam)
  2. ✅ Mini-nav top (bukan sidebar), 6 link section + theme toggle
  3. ✅ §1: 4 mini card + intro awam (drop quick-win/big-bet count)
  4. ✅ §2.1: 5 card HRIS+LMS (Workday/SAP/Cornerstone/Docebo/TalentLMS) + cite vendor docs
  5. ✅ §2.2: 4 card Migas (Shell/Chevron/Exxon/PPSDM) simplified awam
  6. ✅ §3: tabel matrix konsolidasi ~40 gap, 5 kategori + 3 severity, badge ID `G-/N-/§9#` traceability
  7. ✅ §4: 5 accordion top-5 dengan badge ID
  8. ✅ §5: 3 card roadmap dengan item count realistic (QW ~10, Medium ~5, Long ~5), badge ID per item
  9. ✅ §6: consolidate sumber + cross-ref + acknowledgment
  10. ✅ Theme toggle work (port dari ekosistem-sertifikat.html)
  11. ✅ Print stylesheet retained
  12. ✅ Mermaid + Highlight.js CDN dropped (no longer used)
  13. ✅ Bahasa awam end-to-end (no ICE/Quadrant/library/code/file:line jargon)
  14. ✅ External link `target="_blank" rel="noopener"`
  15. ✅ Render OK Chrome+Edge (visual verify manual atau Playwright)

### `ekosistem-sertifikat.html` post-edit
- **Estimate**: 972 → ~480-490 baris
- **Acceptance criteria**:
  1. ✅ Section §9 isi dihapus, ganti teaser dengan link button ke analisa-gap-benchmark.html
  2. ✅ Mini-nav §9 link tetap valid (anchor `#sec-9`)
  3. ✅ Footer tambah link ke analisa-gap-benchmark.html
  4. ✅ Section §1-§8 tidak terpengaruh (verify line count + grep section ID)
  5. ✅ Theme toggle + Mermaid §2/§3/§5 tetap render

## 8. Implementation Order (untuk plan)

1. Read full existing `analisa-gap-benchmark.html` — ekstrak 25 G-*, 12 N-*, 10 R-*, 4 Migas detail, 10 sumber, untuk plan deep-dive
2. Read full §9 di ekosistem-sertifikat.html (baris ~432-936) — sudah ada di context, confirm
3. Dedup mapping table — list 60 raw gap (25 G + 12 N + 23 §9), tandai duplikat, hasil ~40 unique
4. Lock final 40 gap list + 5 top accordion + 3 roadmap bucket items dengan badge ID
5. Reformat `analisa-gap-benchmark.html`:
   - 5a. Replace `<head>` styles ke pattern ekosistem-sertifikat.html (mini-nav + theme toggle CSS) — preserve print stylesheet
   - 5b. Replace sidebar TOC layout dengan mini-nav top + max-width container
   - 5c. Drop Mermaid + Highlight.js CDN
   - 5d. Replace §1 executive ringkasan dengan §1 awam + 4 mini card
   - 5e. Replace §2 Methodology + §6 Quadrant + §7 ICE + §8 R-rec code → §2 Konteks Benchmark consolidated
   - 5f. Replace §3+§4 Gap + Capability detail → §3 Matrix Konsolidasi
   - 5g. Tambah §4 Top-5 accordion (port dari §9.3)
   - 5h. Replace §9 Roadmap Mermaid → §5 Roadmap 3-card
   - 5i. Replace §10 Sources → §6 consolidated
   - 5j. Tambah theme toggle JS di bawah
6. Edit `ekosistem-sertifikat.html`:
   - 6a. Hapus `<section id="sec-9">` content
   - 6b. Tambah teaser section dengan id="sec-9"
   - 6c. Update footer link
7. QA: line count, section count, placeholder scan, internal link verify, theme toggle, accordion expand
8. Commit + tag `sertifikat-doc-gap-benchmark-v2.0-awam`

**Estimate**: 8-10 commit (1 per logical chunk: scratchpad dedup → head/css reformat → §1 → §2.1 → §2.2 → §3 → §4 → §5 → §6 → ekosistem-sertifikat edit → QA).

## 9. Risk & Mitigation

| Risk | Mitigation |
|---|---|
| Dedup gap mapping subjective (sulit consistent) | Lock dedup table di Task 1 scratchpad post-research; user review sebelum commit final |
| Existing executive content (ICE/code) hilang permanent | Preserved di git tag `sertifikat-doc-gap-benchmark-v1.0` — accessible via `git checkout v1.0 -- file` |
| 40 gap rows panjang di matrix → user lelah baca | Mitigation: severity column sortable post-v2.0 (future enhancement), atau group by kategori dengan sticky header |
| Migas section istilah teknis sulit di-awam-kan | Replace OEMS/OIMS → "framework operational excellence", PetroSkills → "konsorsium training oil & gas". Confirm bahasa di review user |
| Switch sidebar → mini-nav = lose UX scroll-spy active highlight | Trade-off accepted; mini-nav konsisten + simpler. Future: add scroll-spy JS via Bootstrap |
| Audience pivot dari Executive ke awam = bingung stakeholder existing | Audience banner top + tag v1.0 vs v2.0 jelas + git log message link tag v1.0 |
| Mermaid Gantt §9 v1.0 ada value untuk visualisasi timeline | Trade-off: drop Mermaid simpler. Future v2.1 bisa re-add timeline visual via static SVG atau Bootstrap timeline component |

## 10. Out of Scope

- Tidak rewrite `index.html` (developer technical, audience beda)
- Tidak rewrite `bug-findings.html` (bug detail audit, audience developer + audit)
- Tidak tambah QR rendering library (R-02/§9 #17 implementation = future phase, not doc work)
- Tidak generate PDF version (HTML only, browser print stylesheet sudah cukup)
- Tidak i18n English version (Bahasa Indonesia per CLAUDE.md)
- Tidak tambah scroll-spy / active highlight di mini-nav v2.0 (future enhancement)
