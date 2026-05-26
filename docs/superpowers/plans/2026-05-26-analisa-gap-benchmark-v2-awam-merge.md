# Analisa Gap + Benchmark v2.0 Awam Merge — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` dari v1.0 executive (914 baris ICE/Quadrant/R-rec) → v2.0 awam HC non-IT (~750-850 baris), merge §9 dari ekosistem-sertifikat.html. Side-effect: ekosistem-sertifikat.html §9 dihapus, ganti teaser link.

**Architecture:** Rewrite existing HTML file dengan layout baru (sidebar TOC → mini-nav top), drop Mermaid+Highlight.js CDN, port semua konten dari §9 ekosistem-sertifikat + simplify konten v1.0 ke bahasa awam. Konsolidasi 60 raw gap (25 G-* + 12 N-* + 23 §9.2) → 50 unique via 7 merge groups. V1.0 preserved via existing tag `sertifikat-doc-gap-benchmark-v1.0`.

**Tech Stack:** HTML5, Bootstrap 5.3.0 (CDN existing), Bootstrap Icons (CDN existing), vanilla JS theme toggle (port dari ekosistem-sertifikat.html), localStorage `analisa-gap-theme`. NO Mermaid, NO Highlight.js (dropped).

**Spec:** `docs/superpowers/specs/2026-05-26-analisa-gap-benchmark-v2-awam-merge-design.md`

**Verification model:** Static HTML, verify via line count + section count + grep pattern check + accordion expand test + theme toggle + (kalau available) Playwright snapshot.

---

## File Structure

**File yang dimodifikasi (BIG refactor):**
- `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — 914 → ~750-850 baris (rewrite majority content)

**File yang dimodifikasi (small edit):**
- `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — 972 → ~480-490 baris (hapus §9, ganti teaser)

**File yang dibuat:**
- `.planning/research/2026-05-26-gap-merge-dedup-mapping.md` — scratchpad dedup mapping konkret

**File reference (read-only):**
- `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` v1.0 existing — sumber konten 25 G-* + 12 N-* + 5 R-* QW + 2 R-* BB + 4 Migas + sources
- `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` §9 baris ~432-936 — sumber konten 5 platform + 23 §9.2 + top-5 + roadmap §9.4 + 7 sumber

---

## Dedup Mapping Reference (untuk Task 6)

**7 merge groups** (10 items saved, 60 raw → 50 unique):

| Merged | Original Items | Final Badge |
|---|---|---|
| Audit Trail Defensible | G-10 + §9#1 | `G-10 / §9#1` |
| Multi-channel notif + scheduler | G-02 + G-09 + §9#4 | `G-02 + G-09 / §9#4` |
| Scalable file storage (CDN/Blob) | G-15 + §9#7 | `G-15 / §9#7` |
| Self-service renewal + auto-renewal pipeline | G-01 + N-03 + §9#9 | `G-01 + N-03 / §9#9` |
| Bulk action (renewal/export/re-issue/mass upload) | G-08 + §9#10 | `G-08 / §9#10` |
| QR / Public verification (CRL implementation) | G-04 + N-09 + §9#17 | `G-04 + N-09 / §9#17` |
| Skill graph & competency mapping | N-04 + §9#19 | `N-04 / §9#19` |

**Standalone (43 items):**
- G-03, G-05, G-06, G-07, G-11, G-12, G-13, G-14, G-16, G-17, G-18, G-19, G-20, G-21, G-22, G-23, G-24, G-25 (18 G-* standalone)
- N-01, N-02, N-05, N-06, N-07, N-08, N-10, N-11, N-12 (9 N-* standalone)
- §9#2, §9#3, §9#5, §9#6, §9#8, §9#11, §9#12, §9#13, §9#14, §9#15, §9#16, §9#18, §9#20, §9#21, §9#22, §9#23 (16 §9 standalone)

**Total**: 7 merged + 43 standalone = **50 unique gap** ✓

---

## Task 1: Read v1.0 sisa + tulis dedup mapping scratchpad

**Files:**
- Create: `.planning/research/2026-05-26-gap-merge-dedup-mapping.md`

- [ ] **Step 1: Read v1.0 §6 (Quadrant) + §7 (ICE) + §8 (R-rec detail) + §9 (Roadmap Mermaid) + §10 (Sources)**

```
Read tool: docs/sertifikat-ecosystem/analisa-gap-benchmark.html
offset 470, limit 460  # baris 470-930 cover §5.3 sampai §10
```

Catat di scratchpad: 10 R-* judul + ID + bucket label (5 QW + 2 BB di §8), 10 sumber external dari §10.1.

- [ ] **Step 2: Tulis scratchpad dedup mapping lengkap**

```bash
mkdir -p .planning/research
```

Tulis `.planning/research/2026-05-26-gap-merge-dedup-mapping.md`:

```markdown
# Gap Merge Dedup Mapping — 2026-05-26

Source: 60 raw gap = 25 G-* + 12 N-* + 23 §9.2
Result: 50 unique gap dedup via 7 merge groups + 43 standalone

## 7 Merge Groups
[copy 7-row tabel dari plan reference]

## 50 Final Gap List (untuk §3 Matrix)

### 🏗️ Sistem (~12)
1. Audit Trail Defensible (G-10 + §9#1) — 🔴 Kritis — Cornerstone CFR 21 Part 11 — belum ada
2. Multi-channel notif + scheduler (G-02 + G-09 + §9#4) — 🔴 Kritis — SAP Joule 4-week reminder + Hangfire — sebagian
3. HCM Integration (§9#2) — 🔴 Kritis — Workday native HRIS sync — belum ada
4. Scalable file storage CDN/Blob (G-15 + §9#7) — 🟡 Penting — Docebo cloud-native — sebagian (DB backup manual)
5. SSO/SAML enterprise (§9#3) — 🟡 Penting — Workday/SAP SSO — belum ada
6. Observability/logging (§9#5) — 🟡 Penting — Workday admin dashboard real-time — sebagian
7. RBAC granularity per-field (§9#8) — 🟢 Nice — SAP role-based field masking — sebagian
8. Backup/DR auto-replicate (§9#6) — 🟢 Nice — Enterprise SaaS auto-replicate region — sebagian
9. Caching Layer Redis/MemoryCache (G-11) — 🟡 Penting — performance — belum ada
10. Cert PDF Async Generation (G-16) — 🟡 Penting — QuestPDF synchronous block thread — sebagian (synchronous)
11. AssessmentSession.CertificateType schema (G-25) — 🟢 Nice — asymmetry vs TrainingRecord — belum ada
12. External Assessor / Proctor Mode role (N-12) — 🟡 Penting — ISO 17024 BNSP framework — belum ada

### 🔄 Flow (~12)
13. Self-service renewal + auto-renewal pipeline (G-01 + N-03 + §9#9) — 🔴 Kritis — Docebo user-initiated + Workday auto-pipeline — belum ada (HC only)
14. Bulk action (renewal/export/re-issue/mass upload) (G-08 + §9#10) — 🔴 Kritis — Cornerstone auto-reassign mass — belum ada
15. Approval workflow multi-tier (§9#11) — 🟡 Penting — SAP Joule guided action queue manager — belum ada
16. Escalation policy expired un-renewed (§9#12) — 🟡 Penting — Workday auto-escalate ke manager+HRBP — belum ada
17. Calendar integration Outlook/Google (§9#13) — 🟡 Penting — SuccessFactors deep calendar event — belum ada
18. Mobile responsive flow ujian online (§9#14) — 🟡 Penting — TalentLMS mobile-first — sebagian (responsive layout, UX limited)
19. Offline mode learning content (§9#15) — 🟢 Nice — Docebo mobile offline app — belum ada
20. Deep linking notif email → page (§9#16) — 🟢 Nice — Workday email→direct context — sebagian
21. Renewal History Timeline View (G-05) — 🟡 Penting — chain FK ada tapi UI tidak visualisasi — sebagian (chain FK exists)
22. Cert Revocation Mechanism (G-03) — 🟢 Nice — Accredible/Credly hard delete only — belum ada
23. Peer Endorsement (N-05) — 🟢 Nice — Workday Talent + LinkedIn — belum ada
24. Job rotation tracking (Shell-pattern) — 🟢 Nice — Shell Learning Academy 3-year graduate — belum ada

### ✨ Fitur (~12)
25. QR / Public verification (G-04 + N-09 + §9#17) — 🔴 Kritis — TalentLMS branded cert + Accredible — belum ada
26. Skill graph & competency mapping (N-04 + §9#19) — 🟡 Penting — Workday skill cloud + Docebo AI auto-tag — sebagian (KKJ tabel, no graph)
27. Digital signature pada PDF (§9#18) — 🟡 Penting — Cornerstone 21 CFR e-signature — belum ada
28. Analytics dashboard real-time drill-down (§9#20) — 🟡 Penting — SAP drill-down by role/location — sebagian (Excel export)
29. AI-recommendation training next-step (§9#21) — 🟢 Nice — Docebo AgentHub 2026 — belum ada
30. Gamification badge/level/leaderboard (§9#22) — 🟢 Nice — TalentLMS 5-mode leaderboard + 8-tier badge — belum ada
31. External API webhook + LinkedIn import (§9#23) — 🟢 Nice — Docebo webhook event lengkap — belum ada
32. Cert Template Customization admin (G-07) — 🟢 Nice — QuestPDF hardcode logo/wording — belum ada
33. Multi-Language Cert Generation EN+ID (N-08) — 🟢 Nice — Workday Learning + Moodle — belum ada
34. Digital Badge Wallet/Portfolio publik (N-06) — 🟡 Penting — Credly Acclaim — belum ada
35. CPD Point Accumulation (N-11) — 🟡 Penting — IACET CEU + BNSP CPD — belum ada
36. Open Badges 1EdTech Issuer JSON-LD (N-01) — 🟢 Nice — Moodle Open Badges 2.0/3.0 — belum ada

### 🔒 Compliance (~7)
37. Audit Log Generic (G-10) — covered di #1
38. Cert Revocation List CRL endpoint (N-07) — 🟡 Penting — ISO 17024 clause 9.5 + Accredible — belum ada
39. Blockchain Credential Verify (N-02) — 🟢 Nice — Accredible/Blockcerts — belum ada
40. Budget Multi-Year Trend (G-06) — 🟢 Nice — chart 3-5 tahun untuk audit — belum ada
41. External auditor verification model (LRQA-style Chevron) — 🟡 Penting — Chevron OEMS — belum ada
42. PSM/HSSE database linkage (ExxonMobil OIMS pattern) — 🟡 Penting — ExxonMobil OIMS — belum ada
43. LSP PPSDM Migas feeder integration — 🔴 Kritis — Permen ESDM SKKNI Migas wajib — belum ada

### ⚡ Performa (~7)
44. DB Index pada ValidUntil (G-13) — 🟡 Penting — filter WHERE ValidUntil tanpa index = full scan — belum ada
45. Rate-Limit Export endpoint (G-12) — 🟡 Penting — no throttle OOM risk — belum ada
46. Soft Delete IsDeleted Flag (G-14) — 🔴 Kritis — hard delete lenyapkan renewal chain history — sebagian
47. Cycle Detection renewal chain A→B→A (G-18) — 🟡 Penting — Union-Find algorithm — belum ada
48. Null ValidUntil Ambiguity (G-17) — 🟡 Penting — DeriveCertificateStatus return Expired untuk null ambigu Permanent — bug
49. Timezone WIB vs UTC (G-19) — 🟡 Penting — selisih 7 jam di boundary — belum fix
50. SEQ Reset Tahunan + boundary inclusive (G-20 + G-21) — 🟢 Nice — premature alert + persepsi duplikasi format — sebagian

## Severity Count
🔴 Kritis: 7 (Audit Trail, Multi-notif+sched, HCM Int, Self-service, Bulk, QR Verify, LSP PPSDM)
🟡 Penting: ~22
🟢 Nice: ~21

## Wait — recount: 7+22+21 = 50 ✓

## 5 Kategori Count
🏗️ Sistem: 12
🔄 Flow: 12
✨ Fitur: 12
🔒 Compliance: 7
⚡ Performa: 7

Total: 50 ✓

## R-* Mapping ke Roadmap (untuk Task 8)

### Quick Win existing (simplify):
- R-01 DB Index ValidUntil → #44 (Quick Win)
- R-02 QR Verify → #25 (Quick Win)
- R-03 Auto-Email Reminder → #2 (Quick Win baseline)
- R-04 Rate-Limit Export → #45 (Quick Win)
- R-05 Cert Revocation List → #38 (Quick Win)
- R-06 Permanent Case-Insensitive → bug fix simple (Quick Win)
- R-07 Permanent + ValidUntil Validator → bug fix (Quick Win)
- R-08 Renewal Cycle Detection → #47 (Quick Win)

### Big-Bet existing (simplify):
- R-09 Hangfire Pipeline → #2 + #13 (Medium-Long)
- R-10 Open Badges JSON-LD → #36 + #39 (Long-term)

## Sources Consolidated (untuk §6)
[from §9.5 + §10.1, dedup overlap Workday/SAP/Cornerstone]
```

- [ ] **Step 3: Commit scratchpad**

```bash
git add .planning/research/2026-05-26-gap-merge-dedup-mapping.md
git commit -m "research(sertifikat-ecosystem): dedup mapping 60→50 gap untuk merge v2.0 awam"
```

---

## Task 2: Reformat HTML structure — head + body skeleton + drop CDN

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Backup verify (existing v1.0 sudah di tag)**

```bash
git log --oneline --all | grep sertifikat-doc-gap-benchmark-v1.0
```
Expected: ada commit tag v1.0. Bila tidak ada (tag hilang), STOP dan tanya user.

- [ ] **Step 2: Rewrite full HTML — head + body skeleton dengan layout mini-nav awam style**

Total rewrite isi file. Tulis dengan struktur lengkap berikut (placeholder konten task selanjutnya):

```html
<!DOCTYPE html>
<html lang="id" data-bs-theme="light">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Analisa Gap + Benchmark v2.0 — Panduan Awam Portal HC KPB</title>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bs-body-bg); }
    main.content { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem 4rem; }
    section { scroll-margin-top: 5rem; padding-bottom: 2.5rem; margin-bottom: 2rem; border-bottom: 1px solid var(--bs-border-color); }
    section:last-of-type { border-bottom: none; }
    h2 { margin-top: 2rem; padding-top: 1rem; }
    h2 .badge { font-size: 0.7em; vertical-align: middle; }
    .mini-nav { position: sticky; top: 0; z-index: 1020; background: var(--bs-body-bg); border-bottom: 1px solid var(--bs-border-color); padding: 0.5rem 1rem; }
    .mini-nav a { color: var(--bs-body-color); text-decoration: none; padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-size: 0.875rem; }
    .mini-nav a:hover { background: var(--bs-secondary-bg); }
    .audience-banner { background: var(--bs-info-bg-subtle); border-left: 4px solid var(--bs-info); padding: 1rem; margin: 1.5rem 0; border-radius: 0.25rem; }
    .id-badge { font-family: 'SFMono-Regular', Consolas, monospace; font-size: 0.7rem; opacity: 0.7; }
    @media print {
      .mini-nav, #theme-toggle { display: none !important; }
      main { padding: 1rem !important; max-width: 100% !important; }
      section { page-break-before: always; border-bottom: none; padding-bottom: 1rem; margin-bottom: 1rem; }
      section#sec-1 { page-break-before: auto; }
      .accordion-collapse { display: block !important; }
      .accordion-button { display: none; }
      .accordion-body { padding: 0.5rem 0; border: none; }
      .card { page-break-inside: avoid; }
      table { page-break-inside: avoid; }
      a { color: black; text-decoration: underline; }
      h2 { page-break-after: avoid; }
    }
  </style>
</head>
<body>
  <nav class="mini-nav d-none d-md-flex justify-content-between align-items-center">
    <div class="d-flex flex-wrap gap-1">
      <a href="#sec-1">§1 Ringkasan</a>
      <a href="#sec-2">§2 Benchmark</a>
      <a href="#sec-3">§3 Matrix Gap</a>
      <a href="#sec-4">§4 Top-5 Kritis</a>
      <a href="#sec-5">§5 Roadmap</a>
      <a href="#sec-6">§6 Sumber</a>
    </div>
    <button id="theme-toggle" class="btn btn-sm btn-outline-secondary" title="Toggle dark mode">
      <i class="bi bi-moon-stars"></i>
    </button>
  </nav>

  <main class="content">
    <header class="mb-4">
      <h1 class="display-6">Analisa Gap + Benchmark</h1>
      <p class="lead text-muted">Panduan Awam untuk Manager &amp; Tim HC — Portal HC KPB</p>
      <div class="audience-banner">
        <strong><i class="bi bi-info-circle"></i> Dokumen v2.0 untuk Manager/HC non-IT.</strong>
        Versi v1.0 (audience executive dengan ICE/Quadrant detail) preserved di git tag <code>sertifikat-doc-gap-benchmark-v1.0</code>.
        Companion: <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a> (panduan awam), <a href="./index.html"><code>index.html</code></a> (versi teknis developer).
      </div>
      <p class="small text-muted">Versi 2.0 — 2026-05-26 — Hasil merge §9 ekosistem-sertifikat + reformat v1.0 ke audience awam.</p>
    </header>

    <section id="sec-1"><h2><span class="badge bg-secondary">§1</span> Ringkasan</h2><p><em>Konten Task 3</em></p></section>
    <section id="sec-2"><h2><span class="badge bg-secondary">§2</span> Konteks Benchmark — 9 Platform</h2><p><em>Konten Task 4 + 5</em></p></section>
    <section id="sec-3"><h2><span class="badge bg-secondary">§3</span> Gap Matrix Konsolidasi</h2><p><em>Konten Task 6</em></p></section>
    <section id="sec-4"><h2><span class="badge bg-secondary">§4</span> Deep-Dive Top-5 Gap Kritis</h2><p><em>Konten Task 7</em></p></section>
    <section id="sec-5"><h2><span class="badge bg-secondary">§5</span> Roadmap Rekomendasi 3-Bucket</h2><p><em>Konten Task 8</em></p></section>
    <section id="sec-6"><h2><span class="badge bg-secondary">§6</span> Sumber Referensi &amp; Cross-Reference</h2><p><em>Konten Task 9</em></p></section>

    <footer class="text-center text-muted small mt-5 pt-4 border-top">
      <p>Analisa Gap + Benchmark Portal HC KPB — Panduan Awam v2.0 — 2026-05-26<br>
      v1.0 executive tag: <code>sertifikat-doc-gap-benchmark-v1.0</code> | © PT Pertamina (Persero) — KPB</p>
    </footer>
  </main>

  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  <!-- Theme toggle script di Task 9 -->
</body>
</html>
```

- [ ] **Step 3: Verifikasi line count drastically reduced (skeleton only)**

```bash
wc -l docs/sertifikat-ecosystem/analisa-gap-benchmark.html
```
Expected: ~80-90 baris (skeleton, content akan di-fill Task 3-9).

- [ ] **Step 4: Commit skeleton**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): rewrite analisa-gap-benchmark v2.0 skeleton awam — mini-nav + 6 section placeholder, drop Mermaid+Highlight.js CDN"
```

---

## Task 3: §1 Ringkasan

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Replace placeholder §1**

Ganti `<p><em>Konten Task 3</em></p>` di bawah `<h2>§1 Ringkasan</h2>` dengan:

```html
      <p><strong>Dokumen ini memetakan gap dan rekomendasi pengembangan sistem sertifikat Portal HC KPB</strong> dengan membandingkan ke 9 platform benchmark (5 HRIS+LMS enterprise dunia + 4 platform Migas/Energy). Hasil: 50 gap unique (konsolidasi review internal + capability baru), 5 top kritis dengan deep-dive, dan roadmap 3-bucket actionable untuk tim HC.</p>
      <div class="row text-center my-4 g-3">
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">9</h3>
            <small class="text-muted">Platform Benchmark</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">50</h3>
            <small class="text-muted">Gap &amp; Capability</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">5</h3>
            <small class="text-muted">Top Kritis Deep-Dive</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">3</h3>
            <small class="text-muted">Bucket Roadmap</small>
          </div></div>
        </div>
      </div>
      <h5 class="mt-4">Cara baca dokumen ini</h5>
      <ol class="small">
        <li><strong>§2</strong> kenali dulu 9 platform benchmark (5 HRIS+LMS + 4 Migas) — konteks dari mana gap di-identifikasi</li>
        <li><strong>§3</strong> scan matrix 50 gap, filter by severity (🔴/🟡/🟢) atau kategori (🏗️🔄✨🔒⚡)</li>
        <li><strong>§4</strong> deep-dive 5 gap paling kritis — current state, best practice, rekomendasi, effort</li>
        <li><strong>§5</strong> lihat roadmap 3-bucket: Quick Win (1-3 bulan) / Medium (3-9 bulan) / Long-term (&gt;9 bulan)</li>
        <li><strong>§6</strong> sumber referensi external (vendor docs + community) + link ke dokumen sertifikat ecosystem lainnya</li>
      </ol>
      <p class="small text-muted"><i class="bi bi-info-circle"></i> Setiap gap punya badge ID kecil (mis. <code class="id-badge">G-10 / §9#1</code>) untuk traceability ke dokumen v1.0 executive dan §9 ekosistem-sertifikat sebelumnya.</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §1 Ringkasan v2.0 — 4 mini card + cara baca dokumen"
```

---

## Task 4: §2.1 — 5 Card HRIS+LMS (port dari §9.1)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Replace placeholder §2 dengan intro + 5 card HRIS+LMS**

Ganti `<p><em>Konten Task 4 + 5</em></p>` di bawah `<h2>§2 Konteks Benchmark</h2>` dengan:

```html
      <p>9 platform benchmark dibagi 2 grup: <strong>5 HRIS+LMS Enterprise</strong> (best practice digital cert mainstream) + <strong>4 platform Industri Migas</strong> (best practice sektor sama).</p>

      <h4 id="sec-2-1" class="mt-4">2.1 HRIS + LMS Enterprise (5 Platform)</h4>
      <div class="row g-3">

        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Workday Learning</strong>
              <span class="badge bg-primary">HRIS Enterprise</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Workday Learning terintegrasi langsung dengan modul HRIS, sehingga sertifikat, training, dan kompetensi pekerja terupdate otomatis saat pekerja pindah jabatan atau departemen. Punya skill cloud untuk mapping kompetensi ke jabatan + auto-assign remediation training saat ada gap. Compliance tracking real-time per departemen dengan reminder otomatis pre-expired.</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> Pertamina punya ekosistem HR kompleks dengan KKJ per jabatan — pattern HRIS-Learning native Workday ideal untuk integrasi data pekerja.</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.workday.com/en-us/services/training-certifications.html" target="_blank" rel="noopener">Workday Learning Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>SAP SuccessFactors Learning</strong>
              <span class="badge bg-primary">HRIS Enterprise</span>
            </div>
            <div class="card-body">
              <p class="card-text small">SAP SuccessFactors Learning fokus pada compliance enterprise dengan reporting drill-down by population/role/location. Joule AI agent (rilis 1H 2026) bertindak proaktif: identifikasi siapa at-risk, kirim reminder 4 minggu sebelum deadline, dan bundle pending approval jadi action queue siap-pakai untuk manager.</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> SAP dipakai luas di BUMN Indonesia termasuk Pertamina — pattern compliance + audit reporting SuccessFactors familiar untuk tim HC.</p>
            </div>
            <div class="card-footer small">
              <a href="https://news.sap.com/2026/04/sap-successfactors-1h-2026-release/" target="_blank" rel="noopener">SAP SuccessFactors 1H 2026 <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Cornerstone Learning</strong>
              <span class="badge bg-success">LMS Dedicated</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Cornerstone Learning dedicated LMS dengan fokus compliance regulated industry. Audit trail "defensible": setiap interaksi training tercatat dengan timestamp, IP address, dan score — siap untuk inspeksi regulator. Standar 21 CFR Part 11 (life sciences) didukung lewat konfigurasi e-signature + audit trail.</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> Industri Migas/Energy = regulated sector. Pattern rigorous audit trail Cornerstone cocok untuk compliance audit Pertamina (BPK, internal audit, regulator).</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.cornerstoneondemand.com/solutions/compliance-management/" target="_blank" rel="noopener">Cornerstone Compliance <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Docebo</strong>
              <span class="badge bg-info text-dark">Cloud LMS Modern</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Docebo cloud LMS modern dengan fokus AI + automation. AI otomatis tag skill ke konten saat upload, certification renewal bisa di-trigger user kapan saja (self-service), webhook event untuk skill changes terintegrasi sistem external. AgentHub 2026 satukan AI agent + skill intelligence dalam satu platform.</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> Pattern AI auto-tag skill + self-service renewal bisa diadopsi bertahap ke modul CDP/CMP, terutama mapping otomatis KKJ ↔ sertifikat.</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.docebo.com/learning-network/blog/inspire-2026-announcements/" target="_blank" rel="noopener">Docebo Inspire 2026 <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>TalentLMS</strong>
              <span class="badge bg-warning text-dark">Mid-Market LMS</span>
            </div>
            <div class="card-body">
              <p class="card-text small">TalentLMS mid-market LMS dengan fokus simplicity + engagement. Gamification mendalam (point/badge/leaderboard/reward), branded certificate yang bisa di-customize logo + design, automated re-certification flow. UX mobile-first dengan onboarding cepat.</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> Pattern gamification + branded certificate bisa naikkan engagement pekerja untuk mandatory training (K3, HSE), terutama kalau ada leaderboard per section.</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.talentlms.com/features/gamification-lms" target="_blank" rel="noopener">TalentLMS Gamification <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-calendar3"></i> Sumber: vendor docs + community/blog posts, per riset 2026-05.</p>

      <h4 id="sec-2-2" class="mt-4">2.2 Industri Migas (4 Platform)</h4>
      <p><em>Konten Task 5</em></p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §2.1 HRIS+LMS — 5 card Workday/SAP/Cornerstone/Docebo/TalentLMS (port dari §9.1)"
```

---

## Task 5: §2.2 — 4 Card Migas Simplified

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Replace placeholder §2.2 dengan 4 card Migas simplified**

Ganti `<p><em>Konten Task 5</em></p>` di bawah `<h4 id="sec-2-2">` dengan:

```html
      <p>4 platform Industri Migas/Energy yang jadi acuan operational excellence + competency framework:</p>
      <div class="row g-3">

        <div class="col-md-6 col-lg-3">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Shell Learning Academy</strong>
              <span class="badge bg-dark">Migas</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Shell Learning Academy gabungkan praktik competence-based via hands-on roles + job tasks + training formal. Kolaborasi dengan PetroSkills Alliance (bersama BP) untuk training oil &amp; gas industri-wide. Punya Graduate Programme 3-tahun dengan job rotation + coaching personal + leadership development.</p>
              <p class="small mb-0"><strong>Relevansi:</strong> Pola job rotation tracking + coaching pairing formal cocok untuk pengembangan pekerja Pertamina KPB.</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.shell.com/business-customers/aviation/aviation-consultancy-services/technical-products-and-services/operating-systems/learning-academy.html" target="_blank" rel="noopener">Shell Learning Academy <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-3">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Chevron OEMS</strong>
              <span class="badge bg-dark">Migas</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Operational Excellence Management System (launched 2004, updated 2018) — framework manajemen operasional yang mengaitkan kompetensi dengan risiko + safeguards. Pelatihan operator wajib selesai dalam 3 tahun pertama kerja. Verifikasi external oleh Lloyd's Register selaras dengan standar ISO 14001 + ISO 45001.</p>
              <p class="small mb-0"><strong>Relevansi:</strong> Pola external auditor verification + time-bound mandatory training (rule 3 tahun) layak adopsi.</p>
            </div>
            <div class="card-footer small">
              <a href="https://www.chevron.com/who-we-are/culture/operational-excellence/oems" target="_blank" rel="noopener">Chevron OEMS <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-3">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>ExxonMobil OIMS</strong>
              <span class="badge bg-dark">Migas</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Operations Integrity Management System — framework 11 elemen, elemen ke-6 khusus "Personnel and Training". Mengaitkan kompetensi pekerja dengan pencegahan insiden lewat Process Safety Management. Independent verification oleh auditor korporasi internal. Matriks training terdokumentasi per asset (refinery, platform, terminal).</p>
              <p class="small mb-0"><strong>Relevansi:</strong> Pola linkage kompetensi ↔ risiko proses + matriks training per-asset cocok untuk konteks pengilangan KPB.</p>
            </div>
            <div class="card-footer small">
              <a href="https://corporate.exxonmobil.com/operations" target="_blank" rel="noopener">ExxonMobil Operations <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <div class="col-md-6 col-lg-3">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>PPSDM Migas (LSP)</strong>
              <span class="badge bg-success">Domestik</span>
            </div>
            <div class="card-body">
              <p class="card-text small">Lembaga Sertifikasi Profesi (LSP) milik pemerintah Indonesia khusus migas — satu-satunya LSP yang sah untuk uji kompetensi migas. Ada 45 ruang lingkup sertifikasi; bidang hilir/pengilangan mencakup Crude Distilling Unit, Vacuum Distilling Unit, Operasi Boiler, Lab Pengujian. Mengacu SKKNI sesuai Permen ESDM (wajib).</p>
              <p class="small mb-0"><strong>Relevansi:</strong> Portal HC dapat berperan feeder data ke PPSDM Migas + tracker re-sertifikasi 3-tahun + repositori sertifikat LSP — kritis untuk compliance regulasi Indonesia.</p>
            </div>
            <div class="card-footer small">
              <a href="https://ppsdmmigas.esdm.go.id/" target="_blank" rel="noopener">PPSDM Migas ESDM <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-info-circle"></i> Detail framework OEMS/OIMS + Permen ESDM regulasi: lihat <code>sertifikat-doc-gap-benchmark-v1.0</code> tag di git history (versi executive).</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §2.2 Migas — 4 card Shell/Chevron/Exxon/PPSDM awam simplified"
```

---

## Task 6: §3 Gap Matrix Konsolidasi (50 row)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

**Source data**: Scratchpad `.planning/research/2026-05-26-gap-merge-dedup-mapping.md` section "50 Final Gap List".

- [ ] **Step 1: Replace placeholder §3 dengan tabel matrix 50 row**

Ganti `<p><em>Konten Task 6</em></p>` di bawah `<h2>§3 Gap Matrix</h2>` dengan tabel responsive lengkap. Generate 50 row dari scratchpad mapping. Format setiap row:

```html
<tr>
  <td>1</td>
  <td>Audit Trail Defensible <code class="id-badge">G-10 / §9#1</code></td>
  <td><span class="badge bg-secondary">🏗️ Sistem</span></td>
  <td><span class="badge bg-danger">🔴 Kritis</span></td>
  <td><small>Cornerstone: defensible audit trail dengan IP + e-signature (21 CFR Part 11)</small></td>
  <td><small>belum ada</small></td>
</tr>
```

Wrapper:
```html
      <p>Matrix 50 gap unique (hasil dedup 60 raw: 25 G-* existing + 12 N-* capability + 23 §9.2). 5 kategori: 🏗️ Sistem, 🔄 Flow, ✨ Fitur, 🔒 Compliance, ⚡ Performa. Severity 🔴 Kritis = blocking compliance/audit/workflow. 🟡 Penting = efficiency/UX hit. 🟢 Nice = enhancement.</p>
      <p class="small text-muted">Badge ID format: <code class="id-badge">G-XX</code> = existing review internal, <code class="id-badge">N-XX</code> = net-new capability dari benchmark, <code class="id-badge">§9#XX</code> = dari §9 ekosistem-sertifikat. Multiple ID = merged dedup.</p>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark">
            <tr>
              <th style="width: 3rem;">#</th>
              <th>Gap / Kapabilitas</th>
              <th>Kategori</th>
              <th>Severity</th>
              <th>Best Practice (Platform)</th>
              <th>Status Portal HC</th>
            </tr>
          </thead>
          <tbody>
            <!-- 50 row dari scratchpad mapping -->
          </tbody>
        </table>
      </div>
```

**Kategori badge classes**:
- 🏗️ Sistem → `bg-secondary`
- 🔄 Flow → `bg-info text-dark`
- ✨ Fitur → `bg-primary`
- 🔒 Compliance → `bg-warning text-dark`
- ⚡ Performa → `bg-success text-white` (atau `bg-dark`)

**Severity badge classes**:
- 🔴 Kritis → `bg-danger`
- 🟡 Penting → `bg-warning text-dark`
- 🟢 Nice → `bg-success`

Generate 50 row sesuai scratchpad. Order: §3.1 Sistem (12) → §3.2 Flow (12) → §3.3 Fitur (12) → §3.4 Compliance (7) → §3.5 Performa (7). Total 50. Atau order by # 1-50 langsung tanpa sub-section.

**CATATAN PENTING**: tidak boleh ada placeholder `{...}` tersisa.

- [ ] **Step 2: Verifikasi 50 row tercipta**

```bash
grep -c '<tr>' docs/sertifikat-ecosystem/analisa-gap-benchmark.html
```
Expected: increment by ~52 (1 thead + 50 tbody + closing wrapper).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §3 Matrix Konsolidasi — 50 gap dedup (7 merge groups + 43 standalone)"
```

---

## Task 7: §4 Top-5 Accordion (port §9.3 + ID badges)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Replace placeholder §4 dengan accordion 5 item port dari §9.3**

Ganti `<p><em>Konten Task 7</em></p>` di bawah `<h2>§4 Top-5 Deep-Dive</h2>` dengan accordion identik §9.3 di ekosistem-sertifikat.html (baris ~580-790 sebelum dihapus), TAMBAH badge ID di header tiap item.

Top-5 (dengan ID badge):
1. Self-Service Renewal Portal — `G-01 + N-03 / §9#9`
2. Public Verification (QR/URL) — `G-04 + N-09 / §9#17`
3. Bulk Action Suite — `G-08 / §9#10`
4. HCM Integration (Employee Master Sync) — `§9#2`
5. Audit Trail Defensible — `G-10 / §9#1`

Template per item (gunakan content dari §9.3 existing, tambah `<code class="id-badge">` di header):

```html
        <div class="accordion-item">
          <h2 class="accordion-header" id="gapHeading1">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gapCollapse1" aria-expanded="false" aria-controls="gapCollapse1">
              <span class="me-2"><strong>1. Self-Service Renewal Portal</strong></span>
              <code class="id-badge me-2">G-01 + N-03 / §9#9</code>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-info text-dark">🔄 Flow</span>
            </button>
          </h2>
          <div id="gapCollapse1" class="accordion-collapse collapse" aria-labelledby="gapHeading1" data-bs-parent="#topGapAccordion">
            <div class="accordion-body">
              <p><strong>📍 Current State Portal HC:</strong> Renewal sertifikat hanya bisa di-trigger HC/Admin via menu Renewal Certificate di Admin Panel...</p>
              <p><strong>🌐 Best Practice External:</strong> <a href="..." target="_blank" rel="noopener">Docebo</a> mengizinkan user trigger renewal <strong>kapan saja</strong>...</p>
              <p><strong>💡 Rekomendasi untuk Portal HC:</strong></p>
              <ul>
                <li>...</li>
              </ul>
              <p class="mb-0"><strong>⚙️ Effort Estimate:</strong> <span class="badge bg-warning text-dark">Medium (3-6 bulan)</span></p>
            </div>
          </div>
        </div>
```

Wrapper:
```html
      <p>5 gap paling kritis dengan deep-dive: kondisi saat ini, best practice external, rekomendasi spesifik, dan estimasi effort.</p>
      <div class="accordion" id="topGapAccordion">
        <!-- 5 accordion item dengan ID badge -->
      </div>
      <p class="small text-muted mt-3"><i class="bi bi-info-circle"></i> Top-5 dipilih berdasarkan impact operasional + frekuensi keluhan HC + benchmark gap. Per riset 2026-05.</p>
```

Port content lengkap (5 item × ~150 kata each) dari §9.3 ekosistem-sertifikat.html. Pastikan semua inline link `target="_blank" rel="noopener"`.

- [ ] **Step 2: Verifikasi 5 accordion item**

```bash
grep -c 'gapCollapse' docs/sertifikat-ecosystem/analisa-gap-benchmark.html
```
Expected: 10 (5 item × 2 attr: id + data-bs-target).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §4 Top-5 Deep-Dive Accordion — port §9.3 + ID badge traceability"
```

---

## Task 8: §5 Roadmap 3-Bucket (merge R-* + §9.4)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

**Source**: §9.4 ekosistem-sertifikat.html roadmap + R-01..R-10 mapping dari scratchpad.

- [ ] **Step 1: Replace placeholder §5 dengan 3 card roadmap (merged items)**

Ganti `<p><em>Konten Task 8</em></p>` di bawah `<h2>§5 Roadmap</h2>` dengan 3 Bootstrap card vertikal.

Layout (template):
```html
      <p>Rekomendasi roadmap pengembangan dibagi 3 bucket berdasarkan effort dan dependency:</p>
      <div class="row g-3">

        <div class="col-md-4">
          <div class="card h-100 border-success">
            <div class="card-header bg-success text-white">
              <strong><i class="bi bi-rocket-takeoff"></i> Quick Win (1-3 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">Config/UI tweak existing kode, no new arsitektur. Hasil cepat momentum.</p>
              <ul class="small mb-0">
                <li><strong>QR verify URL sertifikat</strong> <code class="id-badge">R-02 / §9.4</code> — endpoint public read-only + embed QR di PDF</li>
                <li><strong>DB Index pada ValidUntil</strong> <code class="id-badge">R-01 / G-13</code> — quick wins query performance ekspirasi check</li>
                <li><strong>Auto-Email Reminder Expiry</strong> <code class="id-badge">R-03 / G-02</code> — email template + dispatch baseline untuk Hangfire fase berikutnya</li>
                <li><strong>Rate-Limit Export endpoint</strong> <code class="id-badge">R-04 / G-12</code> — throttle untuk cegah OOM crash export besar</li>
                <li><strong>Cert Revocation List endpoint</strong> <code class="id-badge">R-05 / N-07</code> — soft revoke + JSON list public</li>
                <li><strong>Permanent Case-Insensitive Comparison</strong> <code class="id-badge">R-06 / G-24</code> — bug fix simple status derivation</li>
                <li><strong>Permanent + ValidUntil Validator</strong> <code class="id-badge">R-07 / G-22</code> — bug fix validator data invalid</li>
                <li><strong>Renewal Cycle Detection (Union-Find)</strong> <code class="id-badge">R-08 / G-18</code> — algoritma detect chain A→B→A</li>
                <li><strong>Email template polish + deep link</strong> <code class="id-badge">§9.4</code> — link langsung ke renewal/detail page</li>
                <li><strong>Bulk export filter advance CDP</strong> <code class="id-badge">§9.4</code> — extend Export Excel dengan filter status/section/range</li>
                <li><strong>Analytics dashboard widget</strong> <code class="id-badge">§9.4</code> — sum/group by status + section + tipe di homepage CDP</li>
                <li><strong>Branded certificate template improvement</strong> <code class="id-badge">§9.4</code> — logo Pertamina KPB + watermark + signature image</li>
              </ul>
            </div>
          </div>
        </div>

        <div class="col-md-4">
          <div class="card h-100 border-warning">
            <div class="card-header bg-warning text-dark">
              <strong><i class="bi bi-arrow-up-right-circle"></i> Medium (3-9 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">New feature, 1-2 controller baru, kemungkinan migration DB.</p>
              <ul class="small mb-0">
                <li><strong>Self-Service Renewal Portal pekerja</strong> <code class="id-badge">G-01 + N-03 / §9.4</code> — beban HC turun drastis</li>
                <li><strong>Hangfire Pipeline + Expiry Automation</strong> <code class="id-badge">R-09 / N-03</code> — daily 06:00 query + dispatch + auto-create assessment renewal</li>
                <li><strong>Bulk action UI HC</strong> <code class="id-badge">G-08 / §9.4</code> — mass renewal/re-issue + batch progress indicator</li>
                <li><strong>Skill graph foundation (KKJ ↔ sertifikat)</strong> <code class="id-badge">N-04 / §9.4</code> — table mapping + visualisasi tree</li>
                <li><strong>Multi-channel notification (push/SMS)</strong> <code class="id-badge">G-09 + §9.4</code> — Twilio/Firebase untuk SDM lapangan</li>
                <li><strong>Audit trail defensible (CFR-style)</strong> <code class="id-badge">G-10 / §9.4</code> — tabel dedicated + trigger CRUD + export PDF BPK-ready</li>
                <li><strong>Soft Delete IsDeleted Flag</strong> <code class="id-badge">G-14</code> — preserve renewal chain history</li>
                <li><strong>Cert PDF Async Generation</strong> <code class="id-badge">G-16</code> — background queue untuk batch PDF (no thread block)</li>
              </ul>
            </div>
          </div>
        </div>

        <div class="col-md-4">
          <div class="card h-100 border-danger">
            <div class="card-header bg-danger text-white">
              <strong><i class="bi bi-flag"></i> Long-term (&gt;9 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">Arsitektur baru atau integrasi external system.</p>
              <ul class="small mb-0">
                <li><strong>HCM Integration (SAP HCM Pertamina pusat)</strong> <code class="id-badge">§9.4</code> — single source of truth employee master sync</li>
                <li><strong>SSO/SAML enterprise (Pertamina AD)</strong> <code class="id-badge">§9.4</code> — eliminasi password lokal + audit otomatis</li>
                <li><strong>Open Badges 1EdTech JSON-LD Issuer</strong> <code class="id-badge">R-10 / N-01</code> — verifiable digital credential standar W3C VCDM 2.0</li>
                <li><strong>Blockchain Credential Verify</strong> <code class="id-badge">N-02</code> — anchor hash ke blockchain public (Ethereum/Polygon) anti-fraud</li>
                <li><strong>AI skill recommendation + auto-tag</strong> <code class="id-badge">§9.4</code> — Docebo-pattern ML pipeline atau external API</li>
                <li><strong>Object storage migration (Blob/S3)</strong> <code class="id-badge">G-15 / §9.4</code> — scalability + DR cloud-native</li>
                <li><strong>External API webhook + LinkedIn import</strong> <code class="id-badge">§9.4</code> — extensibility ekosistem external</li>
                <li><strong>LSP PPSDM Migas feeder integration</strong> <code class="id-badge">Permen ESDM</code> — compliance regulasi SKKNI nasional</li>
              </ul>
            </div>
          </div>
        </div>

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-exclamation-triangle"></i> Estimate awam — verify dengan tim IT sebelum execute. Sequencing dapat berubah berdasarkan resource availability + prioritas Pertamina KPB.</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §5 Roadmap 3-bucket — merge R-01..R-10 existing + §9.4 dengan badge ID"
```

---

## Task 9: §6 Sumber + Cross-Reference + Theme Toggle JS

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Replace placeholder §6 dengan consolidated sources + cross-ref + ack**

Ganti `<p><em>Konten Task 9</em></p>` di bawah `<h2>§6 Sumber Referensi</h2>` dengan:

```html
      <h4 id="sec-6-1" class="mt-3">6.1 Sumber External (Vendor Docs + Community)</h4>
      <p class="small">Sumber utama yang dicite di §2 Benchmark + §3 Matrix + §4 Top-5 + §5 Roadmap:</p>
      <ul class="small">
        <li><a href="https://www.workday.com/en-us/services/training-certifications.html" target="_blank" rel="noopener">Workday — Training &amp; Certifications</a> (vendor docs, HRIS)</li>
        <li><a href="https://news.sap.com/2026/04/sap-successfactors-1h-2026-release/" target="_blank" rel="noopener">SAP SuccessFactors 1H 2026 Release (Joule AI for Learning)</a> (vendor blog)</li>
        <li><a href="https://community.sap.com/t5/human-capital-management-blog-posts-by-members/from-tracking-to-acting-strategic-insights-from-the-sap-successfactors/ba-p/14377565" target="_blank" rel="noopener">SAP Community — From Tracking to Acting (Learning 1H 2026)</a> (community)</li>
        <li><a href="https://www.cornerstoneondemand.com/solutions/compliance-management/" target="_blank" rel="noopener">Cornerstone — Compliance Management Solution</a> (vendor docs)</li>
        <li><a href="https://www.cornerstoneondemand.com/industries/life-sciences/" target="_blank" rel="noopener">Cornerstone — Validated LMS Life Sciences (21 CFR Part 11)</a> (vendor docs)</li>
        <li><a href="https://www.docebo.com/learning-network/blog/inspire-2026-announcements/" target="_blank" rel="noopener">Docebo Inspire 2026 — AgentHub &amp; Skills Intelligence</a> (vendor blog)</li>
        <li><a href="https://www.talentlms.com/features/gamification-lms" target="_blank" rel="noopener">TalentLMS — Gamification Features</a> (vendor docs)</li>
        <li><a href="https://www.shell.com/business-customers/aviation/aviation-consultancy-services/technical-products-and-services/operating-systems/learning-academy.html" target="_blank" rel="noopener">Shell Learning Academy</a> (industri Migas)</li>
        <li><a href="https://www.chevron.com/who-we-are/culture/operational-excellence/oems" target="_blank" rel="noopener">Chevron OEMS</a> (industri Migas)</li>
        <li><a href="https://corporate.exxonmobil.com/operations" target="_blank" rel="noopener">ExxonMobil Operations (OIMS)</a> (industri Migas)</li>
        <li><a href="https://ppsdmmigas.esdm.go.id/" target="_blank" rel="noopener">PPSDM Migas ESDM (LSP Indonesia)</a> (regulator domestik)</li>
        <li><a href="https://migas.esdm.go.id/post/permen-esdm-tentang-pemberlakuan-skkni-di-bidang-migas-secara-wajib" target="_blank" rel="noopener">Permen ESDM SKKNI Migas Wajib</a> (regulator domestik)</li>
      </ul>
      <p class="small text-muted"><i class="bi bi-shield-check"></i> Semua URL valid per riset 2026-05. Bila vendor mengubah struktur web, link mungkin redirect.</p>

      <h4 id="sec-6-2" class="mt-4">6.2 Cross-Reference Internal</h4>
      <p class="small">Link ke dokumen lain di folder <code>docs/sertifikat-ecosystem/</code>:</p>
      <ul class="small">
        <li><a href="./ekosistem-sertifikat.html"><i class="bi bi-book"></i> ekosistem-sertifikat.html</a> — panduan awam ekosistem sertifikat (§1-§8 dasar struktur + alur)</li>
        <li><a href="./index.html"><i class="bi bi-code-square"></i> index.html</a> — versi teknis developer (13 endpoint + 9 tabel DB + audit + RBAC matrix)</li>
        <li><a href="./bug-findings.html"><i class="bi bi-bug"></i> bug-findings.html</a> — detail 16 bug Portal HC + Doc (path-traversal, validasi, edge case)</li>
      </ul>

      <h4 id="sec-6-3" class="mt-4">6.3 Versi &amp; Acknowledgment</h4>
      <ul class="small mb-0">
        <li><strong>Versi 2.0</strong> (2026-05-26) — audience awam HC non-IT, hasil merge §9 ekosistem-sertifikat + reformat v1.0</li>
        <li><strong>Versi 1.0</strong> (2026-05-26 earlier) — audience executive dengan ICE/Quadrant detail + 10 R-rec code-level. Preserved di git tag <code>sertifikat-doc-gap-benchmark-v1.0</code></li>
        <li>Generated by Claude Opus 4.7 (1M context) sesi brainstorming + executing-plans skill. Source verified per 2026-05.</li>
      </ul>
```

- [ ] **Step 2: Tambah theme toggle JS sebelum `</body>`**

Ganti `<!-- Theme toggle script di Task 9 -->` dengan:

```html
  <script>
    const html = document.documentElement;
    const toggleBtn = document.getElementById('theme-toggle');
    const icon = toggleBtn.querySelector('i');

    function applyTheme(theme) {
      html.setAttribute('data-bs-theme', theme);
      icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
    }

    const savedTheme = localStorage.getItem('analisa-gap-theme') || 'light';
    applyTheme(savedTheme);

    toggleBtn.addEventListener('click', () => {
      const newTheme = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
      localStorage.setItem('analisa-gap-theme', newTheme);
      applyTheme(newTheme);
    });
  </script>
```

(Tidak ada Mermaid → tidak perlu `mermaid.run()`.)

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): tulis §6 Sumber consolidate + cross-ref + theme toggle JS"
```

---

## Task 10: Edit ekosistem-sertifikat.html — Hapus §9 + Teaser + Footer Link

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

- [ ] **Step 1: Read full §9 di ekosistem-sertifikat untuk konfirmasi range yang akan dihapus**

```
Read tool: ekosistem-sertifikat.html offset 432, limit 510
```
Konfirmasi `<section id="sec-9">` mulai di baris ~432 dan tutup `</section>` di sekitar baris ~936.

- [ ] **Step 2: Replace seluruh `<section id="sec-9">...</section>` dengan teaser section**

Cari `<section id="sec-9">` (line ~432) dan replace blok sampai `</section>` penutup section §9 dengan:

```html
    <section id="sec-9">
      <h2><span class="badge bg-secondary">§9</span> Gap &amp; Best Practice External</h2>
      <p>Untuk analisa gap sistem/flow/fitur lengkap (<strong>50 gap dedup</strong>) + benchmark <strong>9 platform</strong> (5 HRIS+LMS + 4 Migas) + roadmap 3-bucket actionable, lihat dokumen terpisah:</p>
      <div class="d-grid gap-2 col-md-6 mx-auto my-3">
        <a href="./analisa-gap-benchmark.html" class="btn btn-primary btn-lg">
          <i class="bi bi-graph-up-arrow"></i> Buka Analisa Gap + Benchmark v2.0
        </a>
      </div>
      <p class="small text-muted text-center">Dokumen tersebut sebelumnya audience executive (v1.0) — sekarang sudah di-reformat untuk audience awam HC non-IT konsisten dengan dokumen ini.</p>
    </section>
```

- [ ] **Step 3: Update footer ekosistem-sertifikat — tambah link ke analisa-gap-benchmark**

Cari footer (line ~432 → setelah replace teaser, line shift):
```html
<p>Ekosistem Sertifikat Portal HC KPB — Panduan Awam v1.0 — 2026-05-26<br>
Versi teknis: <a href="./index.html">index.html</a> | © PT Pertamina (Persero) — KPB</p>
```

Ganti dengan:
```html
<p>Ekosistem Sertifikat Portal HC KPB — Panduan Awam v1.0 — 2026-05-26<br>
Versi teknis: <a href="./index.html">index.html</a> | Gap Analysis: <a href="./analisa-gap-benchmark.html">analisa-gap-benchmark.html</a> | © PT Pertamina (Persero) — KPB</p>
```

- [ ] **Step 4: Verifikasi line count ekosistem-sertifikat berkurang drastis**

```bash
wc -l docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: ~480-490 baris (dari 972, turun karena §9 ~505 baris diganti ~12 baris teaser).

- [ ] **Step 5: Verifikasi link `#sec-9` di mini-nav tetap valid**

```bash
grep 'sec-9' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: ada `<a href="#sec-9">` di mini-nav + ada `<section id="sec-9">` teaser. Anchor tetap work.

- [ ] **Step 6: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): hapus §9 dari ekosistem-sertifikat — ganti teaser link ke analisa-gap-benchmark v2.0"
```

---

## Task 11: Final QA + Tag

**Files:**
- Modify: tidak ada (verifikasi + tag saja)

- [ ] **Step 1: Line count check kedua file**

```bash
wc -l docs/sertifikat-ecosystem/analisa-gap-benchmark.html docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected:
- `analisa-gap-benchmark.html`: ~750-850 baris (dari 914 v1.0)
- `ekosistem-sertifikat.html`: ~480-490 baris (dari 972 §9-version)

- [ ] **Step 2: Section count check analisa-gap-benchmark**

```bash
grep -c 'id="sec-' docs/sertifikat-ecosystem/analisa-gap-benchmark.html
```
Expected: ≥9 (sec-1..sec-6 + sec-2-1, sec-2-2, sec-6-1, sec-6-2, sec-6-3 = ~11).

- [ ] **Step 3: Verifikasi tidak ada placeholder/draft text**

```bash
grep -nE '(Konten Task|TODO|TBD|placeholder)' docs/sertifikat-ecosystem/analisa-gap-benchmark.html docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: no match.

- [ ] **Step 4: External link count check**

```bash
grep -c 'target="_blank"' docs/sertifikat-ecosystem/analisa-gap-benchmark.html
```
Expected: ≥20 (5 HRIS card + 4 Migas card + 5 Top-5 citations + 12 sumber referensi).

- [ ] **Step 5: Verifikasi mini-nav 6 link di analisa-gap-benchmark**

```bash
grep 'href="#sec-' docs/sertifikat-ecosystem/analisa-gap-benchmark.html | head -10
```
Expected: 6 mini-nav links (§1-§6).

- [ ] **Step 6: Verifikasi cross-link dari ekosistem-sertifikat ke analisa-gap-benchmark**

```bash
grep -c 'analisa-gap-benchmark.html' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: ≥2 (teaser button + footer link).

- [ ] **Step 7: Playwright spot check (kalau available)**

Coba navigate ke kedua file via Playwright:
- `analisa-gap-benchmark.html` — verifikasi accordion expand, theme toggle work, table render
- `ekosistem-sertifikat.html` — verifikasi teaser §9 render, click button → load analisa-gap-benchmark

Kalau gagal (Chrome session conflict), skip dan minta user manual verify.

- [ ] **Step 8: Tag v2.0**

```bash
git tag -a sertifikat-doc-gap-benchmark-v2.0-awam -m "Analisa Gap + Benchmark v2.0 — audience awam HC non-IT

Refactor v1.0 executive (914 baris ICE/Quadrant/R-rec code) ke v2.0 awam HC non-IT (~750-850 baris). Merge §9 dari ekosistem-sertifikat.html. Hasil: 50 gap unique dedup (7 merge groups), 9 platform benchmark (5 HRIS+LMS + 4 Migas), top-5 accordion, roadmap 3-bucket.

V1.0 preserved at tag sertifikat-doc-gap-benchmark-v1.0."

git log --oneline -3
```

- [ ] **Step 9: Final report ke user**

Sebutkan:
- Total commits Task 1-10 (11 commits)
- File path baru: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` v2.0 + `ekosistem-sertifikat.html` post-edit
- Tag baru: `sertifikat-doc-gap-benchmark-v2.0-awam`
- Tag preserved: `sertifikat-doc-gap-benchmark-v1.0`
- Pending: user manual visual verify di browser + push origin/main + push tag baru

---

## Final Report Items

Setelah Task 11 selesai:

1. Tampilkan ke user:
   - 2 file path final (analisa-gap-benchmark.html v2.0 + ekosistem-sertifikat.html)
   - Total commits 11 task (1 research + 8 content + 1 ekosistem edit + 1 QA tag)
   - 50 unique gap dedup confirmation
   - Tag v2.0 created
   - Acceptance criteria 15/15 (analisa-gap-benchmark) + 5/5 (ekosistem-sertifikat) ✅ pending user manual verify
2. Ingatkan user:
   - Visual verify final di browser Chrome/Edge
   - Push origin/main (banyak commit unpushed termasuk kerjaan §9 sebelumnya + Phase 324)
   - Push tag `sertifikat-doc-gap-benchmark-v2.0-awam` dengan `git push origin sertifikat-doc-gap-benchmark-v2.0-awam`
   - Decision: keep/delete scratchpad `.planning/research/2026-05-26-gap-merge-dedup-mapping.md`
   - Update MEMORY.md (project_analisa_gap_benchmark_shipped.md superseded oleh v2.0)
