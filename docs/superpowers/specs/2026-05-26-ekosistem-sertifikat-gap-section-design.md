# Ekosistem Sertifikat — §9 Gap & Best Practice External

**Tanggal**: 2026-05-26
**Author**: Rino (via Claude Opus 4.7, brainstorming skill)
**Target file**: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (existing, append §9)
**Audience**: Manager / HC non-IT
**Companion docs**:
- `docs/sertifikat-ecosystem/index.html` — versi teknis developer
- `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — versi developer-level gap analysis (10 R-rec dengan ICE score)

---

## 1. Tujuan

Tambah section §9 ke `ekosistem-sertifikat.html` yang membahas gap sistem/flow/fitur Portal HC KPB dibandingkan 5 platform HRIS+LMS enterprise terkemuka (Workday, SAP SuccessFactors, Cornerstone, Docebo, TalentLMS). Output: peta gap awam-friendly + roadmap rekomendasi 3-bucket untuk Manager/HC sehingga punya dasar pengembangan sistem ke depan.

## 2. Non-Goals

- Tidak ubah section §1-§8 existing (kecuali tambah link §9 ke mini-nav)
- Tidak duplikasi konten developer-level dari `analisa-gap-benchmark.html` (10 R-rec dengan ICE score) — section §9 ini awam-friendly, tanpa ICE
- Tidak benchmark platform Migas/Energy-specific (per Q3 user pilih enterprise mainstream)
- Tidak Mermaid baru (matrix + accordion sudah cukup visual)
- Tidak bikin file baru terpisah (append in-place)

## 3. Locked Decisions (Q1-Q6 + 8 Fix)

| Item | Locked |
|---|---|
| Lokasi | Append §9 ke `ekosistem-sertifikat.html`, abaikan overlap `analisa-gap-benchmark.html` |
| Dimensi gap | Full 3-angle: Sistem + Flow + Fitur |
| Benchmark platform | 5 enterprise HRIS+LMS: Workday Learning, SAP SuccessFactors Learning, Cornerstone Learning, Docebo, TalentLMS |
| Research method | Live WebSearch + WebFetch (vendor docs + 1 G2/Capterra review per platform) |
| Format §9 | Hybrid: konteks benchmark → matrix → top-5 deep-dive → roadmap |
| Jumlah gap | 20-25 (target 23 = 8 Sistem + 8 Flow + 7 Fitur) |
| Cross-link | §9 intro link ke `analisa-gap-benchmark.html` (developer version) |
| Severity kriteria | 🔴 Kritis: compliance/audit/security atau blocking workflow utama. 🟡 Penting: efficiency/UX hit signifikan, workaround mahal. 🟢 Nice: enhancement, value-add |
| Top-5 layout | Bootstrap accordion (collapsed default) |
| Citation format | Inline `<a target="_blank" rel="noopener">` saat cite + section "Sumber Referensi" mini list di akhir §9 |
| Mobile matrix | Wrap di `<div class="table-responsive">` |
| Date freshness | "(per riset 2026-05)" disclaimer di footer §9.1 + §9.3 |
| Roadmap kriteria | Quick Win: config/UI tweak existing kode. Medium: new feature 1-2 controller. Long-term: arsitektur ulang atau integrasi external |
| Severity badges | Bootstrap class `bg-danger`/`bg-warning text-dark`/`bg-success` (auto-handle light+dark theme) |

## 4. Tech Stack

| Item | Pilihan |
|---|---|
| File format | Modify existing `ekosistem-sertifikat.html` (append) |
| CSS framework | Reuse Bootstrap 5.3.0 + Bootstrap Icons existing (tidak tambah CDN baru) |
| New components | Bootstrap accordion (sudah include di bundle.min.js) |
| Lang attr | `id` |
| Theme compat | Inherit `data-bs-theme` toggle existing |
| Layout | Konsisten dengan section §1-§8: max-width 900px, single-column scroll |

## 5. Konten §9 — Sub-Section Breakdown

### §9.0 Intro (~100 kata)

Pembuka section: konteks kenapa benchmark + cross-link.

> "Sistem sertifikat Portal HC KPB sudah jalan dan memenuhi kebutuhan dasar — tapi ada beberapa gap dibanding platform HRIS+LMS enterprise modern. Bagian ini bandingkan Portal HC dengan 5 platform terkemuka dunia (Workday, SAP SuccessFactors, Cornerstone, Docebo, TalentLMS) untuk identifikasi gap di 3 dimensi: Sistem, Flow, dan Fitur — plus roadmap rekomendasi yang actionable untuk tim HC."
>
> Catatan: untuk versi developer-level gap analysis dengan ICE score (10 R-rec), lihat [`analisa-gap-benchmark.html`](./analisa-gap-benchmark.html).

### §9.1 Konteks Benchmark — 5 Platform (~400 kata)

Layout: **Bootstrap card grid** (col-md-6 col-lg-4, total 5 card + 1 placeholder kosong di col ke-6 atau center 5 card).

Per card:
- **Header**: nama platform + badge kategori (HRIS / LMS / Hybrid)
- **Body**:
  - 1 paragraf 3-4 kalimat: apa unggulan certification management mereka (awam)
  - 1 baris **Relevansi ke Portal HC KPB**: kenapa platform ini relevan dibandingkan
- **Footer**: link inline `<a target="_blank" rel="noopener">` ke vendor docs (1 link per card)

5 platform:

| Platform | Kategori | Anchor unggulan |
|---|---|---|
| Workday Learning | HRIS Enterprise | Certification terintegrasi performance review + skill cloud |
| SAP SuccessFactors Learning | HRIS Enterprise | Compliance training + skill graph + global enterprise |
| Cornerstone Learning | LMS Dedicated | Strong compliance audit trail + dedicated cert management |
| Docebo | Cloud LMS Modern | AI skill mapping + automasi renewal notification |
| TalentLMS | Mid-Market LMS | Simplicity + gamification + branded certificate |

Footer: "Sumber: vendor docs + G2/Capterra reviews, per riset 2026-05."

### §9.2 Overview Matrix Gap (~500 kata + tabel)

**Tabel responsive** 23 row × 6 kolom:

| # | Gap | Kategori | Severity | Best Practice (Platform) | Status Portal HC |
|---|---|---|---|---|---|
| 1 | ... | 🏗️ Sistem | 🔴 Kritis | "..." (Workday) | belum ada |

**Wrapper**: `<div class="table-responsive">` → horizontal scroll di mobile.

**Sebaran target 23 gap** (final list locked saat implementation post-research):
- **🏗️ Sistem (~8 gap)**: Storage scaling, RBAC granularity, notif engine (push/email/SMS), audit trail compliance, SSO integration, multi-tenant, backup/DR, observability/logging
- **🔄 Flow (~8 gap)**: Self-service renewal trigger, bulk action (renewal/export/re-issue), approval workflow, escalation policy, calendar integration, mobile responsive flow, offline mode, deep linking
- **✨ Fitur (~7 gap)**: QR code/public verify URL, digital signature, skill graph & mapping, analytics dashboard, AI-recommendation training, gamification (badge/leaderboard), external API (LinkedIn/HRIS sync)

**Sebaran severity**: ~6 kritis, ~10 penting, ~7 nice.

Header tabel pakai `<thead class="table-dark">`. Severity column pakai Bootstrap badge (`bg-danger`/`bg-warning text-dark`/`bg-success`).

### §9.3 Deep-Dive Top-5 Gap Kritis (~600 kata)

Layout: **Bootstrap accordion** (collapsed default, expand on click). 5 item.

Kandidat top-5 (lock saat implementation post-research):
1. **Self-Service Renewal Portal** (Coachee trigger renewal sendiri, tidak nunggu HC)
2. **Public Verification (QR/URL)** (pihak external verify sertifikat scan QR)
3. **Skill Graph & Competency Mapping** (sertifikat → skill tree → gap analysis pekerja)
4. **Bulk Action Suite** (HC bulk renewal, bulk re-issue, bulk export with filter)
5. **External Integration / API** (LinkedIn import, HRIS sync, webhook event)

Per accordion item (struktur konsisten):
- **Judul**: nama gap + 2 badge (severity + kategori)
- **Body** (saat expand):
  - 📍 **Current State Portal HC** (2-3 kalimat — apa yang ada sekarang, kekurangannya)
  - 🌐 **Best Practice External** (cite 1-2 platform + fitur konkret 2-3 kalimat, inline link vendor doc)
  - 💡 **Rekomendasi untuk Portal HC** (3-5 bullet actionable)
  - ⚙️ **Effort Estimate**: badge Quick Win / Medium / Long-term

Footer §9.3: disclaimer "Top-5 dipilih berdasarkan impact + frekuensi keluhan HC + benchmark gap. Per riset 2026-05."

### §9.4 Roadmap Rekomendasi (~250 kata)

Layout: **3 Bootstrap card vertikal** (col-md-4), warna theme berbeda:

**Card 1 — Quick Win (1-3 bulan)** — `border-success`
- Kriteria: config/UI tweak existing kode, no new arsitektur
- Konten: 3-5 item dari matrix dengan justifikasi 1-baris (target: low-effort high-impact)
- Contoh kandidat: QR verify dasar, email template improvement, dashboard polish, bulk export filter

**Card 2 — Medium (3-9 bulan)** — `border-warning`
- Kriteria: new feature, 1-2 controller baru, kemungkinan migration DB
- Konten: 3-5 item
- Contoh kandidat: self-service renewal, skill graph foundation, bulk action UI, analytics dashboard

**Card 3 — Long-term (>9 bulan)** — `border-danger`
- Kriteria: arsitektur baru atau integrasi external system
- Konten: 3-5 item
- Contoh kandidat: HRIS sync, AI skill recommendation, SSO/SAML, blockchain attestation

Tiap card pakai `<ul class="small">` list item dengan icon Bootstrap (`bi-rocket-takeoff`, `bi-arrow-up-right-circle`, `bi-flag`).

### §9.5 Sumber Referensi (~80 kata)

Mini list 5-7 link vendor docs + sumber utama yang dicite di §9. Format:

```html
<ul class="small">
  <li><a href="..." target="_blank" rel="noopener">Workday Learning — Certification Tracking</a> (vendor docs)</li>
  <li><a href="..." target="_blank" rel="noopener">SAP SuccessFactors — Learning Suite Overview</a> (vendor docs)</li>
  ...
</ul>
```

Final source list di-lock post-research.

## 6. Research Plan (Live Web Research)

Pre-implementasi, jalankan WebSearch + WebFetch untuk 5 platform. Tiap platform:

| Step | Tool | Query/URL |
|---|---|---|
| 1 | WebSearch | `"{platform} certification management features 2025"` — top 3 result |
| 2 | WebFetch | vendor doc URL teratas dari hasil search |
| 3 | WebFetch | 1 G2/Capterra review URL (validasi independen) |

5 platform × ~3 fetch = ~15 total web call (estimate 3-5 menit). Cite hasil:
- Inline link saat sebut feature spesifik
- Section §9.5 untuk top-5 sumber utama
- Disclaimer "per riset 2026-05" di footer §9.1 + §9.3

## 7. Mini-Nav Update

Edit existing mini-nav (line ~38-46 di `ekosistem-sertifikat.html`), tambah:

```html
<a href="#sec-9">§9 Gap & Best Practice</a>
```

Setelah `<a href="#sec-8">§8 Glosarium</a>`. Existing `<div class="d-flex flex-wrap gap-1">` sudah wrap-capable — 9 link tetap muat (atau wrap baris kedua di layar md).

## 8. File Size & Acceptance Criteria

**Estimasi penambahan**:
- §9.0 intro: ~10 baris
- §9.1 benchmark 5 card: ~60 baris
- §9.2 matrix 23 row + tabel header: ~80 baris
- §9.3 top-5 accordion: ~120 baris
- §9.4 roadmap 3 card: ~50 baris
- §9.5 sumber referensi: ~15 baris
- **Total tambahan: ~335 baris**
- File final: ~800 baris (dari 467 sekarang)

**Acceptance criteria**:
1. ✅ §9 ter-append setelah §8 di `ekosistem-sertifikat.html`
2. ✅ Mini-nav punya link §9 (tetap responsive wrap)
3. ✅ §9.1: 5 card benchmark dengan nama platform + cite vendor docs + disclaimer date
4. ✅ §9.2: tabel matrix 20-25 row dengan kolom Gap/Kategori/Severity/Best Practice/Status; wrap `table-responsive`
5. ✅ §9.3: 5 accordion item top-5 gap kritis, struktur konsisten (Current/Best Practice/Rekomendasi/Effort)
6. ✅ §9.4: 3 card roadmap (Quick Win green / Medium yellow / Long-term red), 3-5 item per card
7. ✅ §9.5: 5-7 link sumber referensi target=_blank
8. ✅ Severity pakai Bootstrap badge bg-danger/warning/success (auto-handle light+dark theme)
9. ✅ Semua external link `target="_blank" rel="noopener"`
10. ✅ Bahasa awam — no endpoint/file:line/SQL/ICE-score
11. ✅ Live research done (WebSearch + WebFetch 5 platform) sebelum tulis konten — cite verified
12. ✅ Render OK Chrome+Edge, theme toggle tetap work (verify §9 di light+dark)

## 9. Risk & Mitigation

| Risk | Mitigation |
|---|---|
| Vendor docs berubah/4xx setelah cite | Disclaimer "per riset 2026-05" + revisi tahunan ditugaskan ke HC |
| Best practice cite outdated dalam 1-2 tahun | Footer §9.1 + §9.3 sebut date freshness |
| 23 gap subjektif (siapa decide?) | Lock spec sumber: gap dari (a) benchmark vs Portal HC current, (b) keluhan HC operasional, (c) celah audit `analisa-gap-benchmark.html`. Final list di-doc di komit message |
| Roadmap bucket estimate ngawur tanpa engineering input | Disclaimer di footer §9.4: "Estimate awam — verify dengan IT sebelum execute" |
| Accordion §9.3 broken di older browser | Bootstrap 5.3 accordion = standard, fallback OK |
| Mobile matrix tetap horizontal scroll bisa kena UX issue | Acceptable per locked decision; mitigation = consider card-view fallback di v2 |

## 10. Implementation Order Sketsa

1. Research phase: WebSearch + WebFetch 5 platform → kumpulkan feature claim + source URL
2. Lock final 23 gap + top-5 + roadmap items berdasarkan research
3. Edit `ekosistem-sertifikat.html`: tambah link mini-nav §9
4. Tambah skeleton §9.0..§9.5 (placeholder)
5. Isi §9.0 + §9.1 (benchmark 5 card)
6. Isi §9.2 (matrix 23 gap)
7. Isi §9.3 (top-5 accordion)
8. Isi §9.4 (roadmap 3 card)
9. Isi §9.5 (sumber referensi)
10. QA: line count, accordion expand/collapse, theme toggle, external link work
11. Final commit + report
