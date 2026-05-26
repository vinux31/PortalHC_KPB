# Ekosistem Sertifikat — §9 Gap & Best Practice — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Append section §9 ke `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — gap analysis sistem/flow/fitur Portal HC vs 5 platform enterprise HRIS+LMS, dengan matrix 23 gap + top-5 accordion deep-dive + roadmap 3-bucket.

**Architecture:** Modify existing single HTML file (append §9 setelah §8, sebelum footer). Reuse Bootstrap 5.3.0 + Bootstrap Icons existing — tidak tambah CDN baru. Pakai Bootstrap accordion component untuk §9.3 top-5. Live web research dulu (WebSearch + WebFetch 5 platform), lalu tulis konten dengan cite verified.

**Tech Stack:** HTML5, Bootstrap 5.3.0 (existing CDN), Bootstrap Icons (existing), Mermaid (existing — tidak dipakai di §9), WebSearch + WebFetch (untuk research phase).

**Spec:** `docs/superpowers/specs/2026-05-26-ekosistem-sertifikat-gap-section-design.md`

**Verification model:** Static HTML, verifikasi via line count + grep + manual browser check + (kalau bisa) Playwright snapshot.

---

## File Structure

**File yang dimodifikasi:**
- `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — append §9 (target +335 baris, total ~800 baris)

**File yang dibuat:**
- `.planning/research/2026-05-26-gap-section-research.md` — temporary scratchpad untuk catat hasil WebSearch + WebFetch (di-keep di repo sebagai traceable source, atau di-delete post-implementation kalau user mau)

**File reference (read-only):**
- `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — existing 467 baris (target append point)
- `docs/superpowers/specs/2026-05-26-ekosistem-sertifikat-gap-section-design.md` — spec sumber

---

## Task 1: Research Phase — WebSearch + WebFetch 5 platform

**Files:**
- Create: `.planning/research/2026-05-26-gap-section-research.md`

**Tujuan**: Kumpulkan feature claim + source URL untuk 5 platform sebelum tulis konten. Hasil di-save ke scratchpad untuk traceability.

- [ ] **Step 1: Buat scratchpad file**

```bash
mkdir -p .planning/research
```

Tulis file `.planning/research/2026-05-26-gap-section-research.md` dengan header:

```markdown
# Research §9 Gap & Best Practice — 2026-05-26

Source notes untuk implementasi §9 di ekosistem-sertifikat.html.

## 1. Workday Learning

### Search queries
- (akan diisi)

### Sources
- (akan diisi)

### Key features (certification management)
- (akan diisi)

## 2. SAP SuccessFactors Learning
(struktur sama)

## 3. Cornerstone Learning
(struktur sama)

## 4. Docebo
(struktur sama)

## 5. TalentLMS
(struktur sama)

## Synthesis — 23 Gap Final List
(akan diisi setelah research)

## Synthesis — Top-5 Gap Kritis
(akan diisi setelah research)

## Synthesis — Roadmap 3-Bucket
(akan diisi setelah research)
```

- [ ] **Step 2: WebSearch + WebFetch Workday Learning**

WebSearch query: `Workday Learning certification management features 2025`
Pilih 1 vendor doc URL (workday.com/community) + 1 review URL (g2.com / capterra.com).
WebFetch keduanya, ekstrak feature claim ke scratchpad section "1. Workday Learning".

- [ ] **Step 3: WebSearch + WebFetch SAP SuccessFactors**

WebSearch query: `SAP SuccessFactors Learning certification compliance features`
Sama: 1 vendor doc + 1 review, fetch, ekstrak ke scratchpad section "2. SAP SuccessFactors Learning".

- [ ] **Step 4: WebSearch + WebFetch Cornerstone Learning**

WebSearch query: `Cornerstone Learning certification compliance audit trail`
Sama: 1 vendor doc + 1 review, fetch, ekstrak.

- [ ] **Step 5: WebSearch + WebFetch Docebo**

WebSearch query: `Docebo certification renewal automation AI skill`
Sama: 1 vendor doc + 1 review, fetch, ekstrak.

- [ ] **Step 6: WebSearch + WebFetch TalentLMS**

WebSearch query: `TalentLMS certification gamification branded certificate`
Sama: 1 vendor doc + 1 review, fetch, ekstrak.

- [ ] **Step 7: Synthesis di scratchpad — lock final 23 gap + top-5 + roadmap**

Berdasarkan research, tulis di section "Synthesis":
- **23 Gap Final List** — 8 Sistem + 8 Flow + 7 Fitur. Tiap gap: nama, kategori, severity (🔴/🟡/🟢), best practice cite (platform + 1 kalimat), status Portal HC
- **Top-5 Gap Kritis** — 5 gap dari list 23 yang severity 🔴 paling impactful + relevant ke Portal HC. Tiap top-5: judul, current state, best practice detail, rekomendasi, effort
- **Roadmap 3-Bucket** — Quick Win (3-5 item), Medium (3-5 item), Long-term (3-5 item)

Final list jadi single source of truth untuk Task 3-7.

- [ ] **Step 8: Commit research scratchpad**

```bash
git add .planning/research/2026-05-26-gap-section-research.md
git commit -m "research(sertifikat-ecosystem): scratchpad gap §9 — 5 platform WebSearch/Fetch + synthesis 23 gap + top-5 + roadmap"
```

---

## Task 2: Skeleton §9 + mini-nav update

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

- [ ] **Step 1: Read ekosistem-sertifikat.html untuk konfirmasi append point**

Run Read tool pada `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` offset 35-50 (untuk mini-nav) dan 425-440 (untuk insertion point sebelum `</main>`).

- [ ] **Step 2: Tambah link §9 ke mini-nav**

Edit mini-nav (line ~45). Tambah setelah link §8:

```html
<a href="#sec-8">§8 Glosarium</a>
<a href="#sec-9">§9 Gap & Best Practice</a>
```

- [ ] **Step 3: Tambah skeleton §9 setelah §8 (sebelum footer)**

Append setelah closing tag `</section>` dari sec-8 dan sebelum `<footer>`:

```html
    <section id="sec-9">
      <h2><span class="badge bg-secondary">§9</span> Gap & Best Practice External</h2>
      <p><em>Konten Task 3-7</em></p>

      <h4 id="sec-9-1" class="mt-4">9.1 Konteks Benchmark — 5 Platform</h4>
      <p><em>Konten Task 3</em></p>

      <h4 id="sec-9-2" class="mt-4">9.2 Overview Matrix Gap</h4>
      <p><em>Konten Task 4</em></p>

      <h4 id="sec-9-3" class="mt-4">9.3 Deep-Dive Top-5 Gap Kritis</h4>
      <p><em>Konten Task 5</em></p>

      <h4 id="sec-9-4" class="mt-4">9.4 Roadmap Rekomendasi</h4>
      <p><em>Konten Task 6</em></p>

      <h4 id="sec-9-5" class="mt-4">9.5 Sumber Referensi</h4>
      <p><em>Konten Task 7</em></p>
    </section>
```

- [ ] **Step 4: Tambah intro §9.0 (gantikan placeholder `<p><em>Konten Task 3-7</em></p>` di bawah h2)**

```html
      <p>Sistem sertifikat Portal HC KPB sudah jalan dan memenuhi kebutuhan dasar — tapi ada beberapa gap dibanding platform HRIS+LMS enterprise modern. Bagian ini bandingkan Portal HC dengan 5 platform terkemuka dunia (Workday, SAP SuccessFactors, Cornerstone, Docebo, TalentLMS) untuk identifikasi gap di 3 dimensi: <strong>Sistem, Flow, dan Fitur</strong> — plus roadmap rekomendasi yang actionable untuk tim HC.</p>
      <div class="alert alert-light border-start border-info border-3">
        <small><i class="bi bi-info-circle"></i> Untuk versi developer-level gap analysis dengan ICE score (10 R-rec), lihat <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a>.</small>
      </div>
```

- [ ] **Step 5: Commit skeleton**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): skeleton §9 + intro + mini-nav link + cross-link ke analisa-gap-benchmark"
```

---

## Task 3: §9.1 — Konteks Benchmark 5 Platform Card

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

**Source data**: Scratchpad `.planning/research/2026-05-26-gap-section-research.md` section 1-5 (feature claim + source URL per platform).

- [ ] **Step 1: Replace placeholder §9.1 dengan grid 5 card**

Ganti `<p><em>Konten Task 3</em></p>` di bawah `<h4 id="sec-9-1">` dengan:

```html
      <p>5 platform HRIS+LMS terkemuka yang jadi benchmark gap analysis Portal HC KPB:</p>
      <div class="row g-3">

        <!-- Card 1: Workday Learning -->
        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Workday Learning</strong>
              <span class="badge bg-primary">HRIS Enterprise</span>
            </div>
            <div class="card-body">
              <p class="card-text small">{1 paragraf 3-4 kalimat unggulan certification management Workday — diambil dari research scratchpad section "1. Workday Learning"}</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> {1 kalimat — kenapa relevan dibandingkan}</p>
            </div>
            <div class="card-footer small">
              <a href="{WORKDAY_VENDOR_DOC_URL}" target="_blank" rel="noopener">Workday Learning Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <!-- Card 2: SAP SuccessFactors Learning -->
        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>SAP SuccessFactors Learning</strong>
              <span class="badge bg-primary">HRIS Enterprise</span>
            </div>
            <div class="card-body">
              <p class="card-text small">{paragraf SAP SuccessFactors unggulan}</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> {1 kalimat}</p>
            </div>
            <div class="card-footer small">
              <a href="{SAP_VENDOR_DOC_URL}" target="_blank" rel="noopener">SAP SuccessFactors Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <!-- Card 3: Cornerstone Learning -->
        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Cornerstone Learning</strong>
              <span class="badge bg-success">LMS Dedicated</span>
            </div>
            <div class="card-body">
              <p class="card-text small">{paragraf Cornerstone unggulan}</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> {1 kalimat}</p>
            </div>
            <div class="card-footer small">
              <a href="{CORNERSTONE_VENDOR_DOC_URL}" target="_blank" rel="noopener">Cornerstone Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <!-- Card 4: Docebo -->
        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Docebo</strong>
              <span class="badge bg-info text-dark">Cloud LMS Modern</span>
            </div>
            <div class="card-body">
              <p class="card-text small">{paragraf Docebo unggulan}</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> {1 kalimat}</p>
            </div>
            <div class="card-footer small">
              <a href="{DOCEBO_VENDOR_DOC_URL}" target="_blank" rel="noopener">Docebo Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

        <!-- Card 5: TalentLMS -->
        <div class="col-md-6 col-lg-4">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>TalentLMS</strong>
              <span class="badge bg-warning text-dark">Mid-Market LMS</span>
            </div>
            <div class="card-body">
              <p class="card-text small">{paragraf TalentLMS unggulan}</p>
              <p class="small mb-0"><strong>Relevansi Portal HC:</strong> {1 kalimat}</p>
            </div>
            <div class="card-footer small">
              <a href="{TALENTLMS_VENDOR_DOC_URL}" target="_blank" rel="noopener">TalentLMS Docs <i class="bi bi-box-arrow-up-right"></i></a>
            </div>
          </div>
        </div>

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-calendar3"></i> Sumber: vendor docs + G2/Capterra reviews, per riset 2026-05.</p>
```

**CATATAN PENTING**: Setelah paste, **replace semua `{...}` placeholder** dengan konten konkret dari scratchpad. Tidak boleh ada `{...}` tersisa di file final.

- [ ] **Step 2: Verifikasi tidak ada placeholder `{...}` tersisa**

```bash
grep -n '{' docs/sertifikat-ecosystem/ekosistem-sertifikat.html | grep -v 'KPB/{NOMOR' | grep -v 'tahun}' | grep -v 'romawi-bulan'
```
Expected: no match (KPB nomor sertifikat format adalah valid string yang sudah ada di file).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §9.1 — 5 card benchmark Workday/SAP/Cornerstone/Docebo/TalentLMS"
```

---

## Task 4: §9.2 — Overview Matrix Gap (23 row)

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

**Source data**: Scratchpad section "Synthesis — 23 Gap Final List".

- [ ] **Step 1: Replace placeholder §9.2 dengan tabel matrix**

Ganti `<p><em>Konten Task 4</em></p>` di bawah `<h4 id="sec-9-2">` dengan:

```html
      <p>Matrix 23 gap di 3 dimensi (🏗️ Sistem, 🔄 Flow, ✨ Fitur) — dibanding 5 platform benchmark. Severity: 🔴 Kritis = blocking compliance/audit/workflow utama. 🟡 Penting = efficiency/UX hit. 🟢 Nice = enhancement value-add.</p>
      <div class="table-responsive">
        <table class="table table-sm table-bordered align-middle">
          <thead class="table-dark">
            <tr>
              <th style="width: 3rem;">#</th>
              <th>Gap</th>
              <th>Kategori</th>
              <th>Severity</th>
              <th>Best Practice (Platform)</th>
              <th>Status Portal HC</th>
            </tr>
          </thead>
          <tbody>
            <!-- 23 row di sini, di-fill dari scratchpad. Format per row: -->
            <tr>
              <td>1</td>
              <td>{Nama gap singkat}</td>
              <td><span class="badge bg-secondary">🏗️ Sistem</span></td>
              <td><span class="badge bg-danger">🔴 Kritis</span></td>
              <td><small>{1 kalimat best practice + nama platform}</small></td>
              <td><small>{belum ada / sebagian / ada tapi terbatas}</small></td>
            </tr>
            <!-- ... 22 row lainnya, gunakan badge class consistent: -->
            <!-- Kategori: bg-secondary untuk 🏗️ Sistem, bg-info text-dark untuk 🔄 Flow, bg-primary untuk ✨ Fitur -->
            <!-- Severity: bg-danger untuk 🔴, bg-warning text-dark untuk 🟡, bg-success untuk 🟢 -->
          </tbody>
        </table>
      </div>
```

**CATATAN PENTING**: Generate 23 row dari scratchpad. Tidak boleh ada `{...}` tersisa. Sebaran lock: 8 Sistem + 8 Flow + 7 Fitur, 6 kritis + 10 penting + 7 nice.

- [ ] **Step 2: Verifikasi 23 row tercipta**

```bash
grep -c '<tr>' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: increment by ~24 (1 header + 23 body row). Run `wc -l` untuk confirm tambahan baris ~100-120.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §9.2 — matrix 23 gap (8 sistem + 8 flow + 7 fitur)"
```

---

## Task 5: §9.3 — Deep-Dive Top-5 Accordion

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

**Source data**: Scratchpad section "Synthesis — Top-5 Gap Kritis".

- [ ] **Step 1: Replace placeholder §9.3 dengan Bootstrap accordion 5 item**

Ganti `<p><em>Konten Task 5</em></p>` di bawah `<h4 id="sec-9-3">` dengan:

```html
      <p>5 gap paling kritis dengan deep-dive: kondisi saat ini di Portal HC, best practice external, rekomendasi spesifik, dan estimasi effort.</p>
      <div class="accordion" id="topGapAccordion">

        <!-- Item 1 -->
        <div class="accordion-item">
          <h2 class="accordion-header" id="gapHeading1">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gapCollapse1" aria-expanded="false" aria-controls="gapCollapse1">
              <span class="me-2">{Judul Gap 1}</span>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-secondary">{🏗️ Sistem / 🔄 Flow / ✨ Fitur}</span>
            </button>
          </h2>
          <div id="gapCollapse1" class="accordion-collapse collapse" aria-labelledby="gapHeading1" data-bs-parent="#topGapAccordion">
            <div class="accordion-body">
              <p><strong>📍 Current State Portal HC:</strong> {2-3 kalimat — apa yang ada sekarang, kekurangannya}</p>
              <p><strong>🌐 Best Practice External:</strong> {2-3 kalimat cite platform + fitur konkret + inline link <a target="_blank" rel="noopener">}</p>
              <p><strong>💡 Rekomendasi untuk Portal HC:</strong></p>
              <ul>
                <li>{rekomendasi 1}</li>
                <li>{rekomendasi 2}</li>
                <li>{rekomendasi 3}</li>
                <li>{rekomendasi 4 — opsional}</li>
                <li>{rekomendasi 5 — opsional}</li>
              </ul>
              <p class="mb-0"><strong>⚙️ Effort Estimate:</strong> <span class="badge bg-{success|warning|danger}">{Quick Win | Medium | Long-term}</span></p>
            </div>
          </div>
        </div>

        <!-- Item 2-5: struktur sama, ganti angka 1 → 2, 3, 4, 5 di id/href/aria attribute -->
        <!-- Pattern: gapHeading{N} + gapCollapse{N} -->

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-info-circle"></i> Top-5 dipilih berdasarkan impact + frekuensi keluhan HC + benchmark gap. Per riset 2026-05.</p>
```

**CATATAN PENTING**: Generate 5 accordion item (1-5) dari scratchpad. Tidak boleh ada `{...}` tersisa. Semua effort estimate harus valid bucket (Quick Win / Medium / Long-term). Pastikan id attribute `gapHeading{N}` + `gapCollapse{N}` unique per item.

- [ ] **Step 2: Verifikasi 5 accordion item**

```bash
grep -c 'gapCollapse' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: 15 occurrences (3 per item × 5 item: id + data-bs-target + aria-labelledby).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §9.3 — accordion top-5 gap kritis deep-dive"
```

---

## Task 6: §9.4 — Roadmap 3 Bucket Card

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

**Source data**: Scratchpad section "Synthesis — Roadmap 3-Bucket".

- [ ] **Step 1: Replace placeholder §9.4 dengan 3 card vertikal**

Ganti `<p><em>Konten Task 6</em></p>` di bawah `<h4 id="sec-9-4">` dengan:

```html
      <p>Rekomendasi roadmap pengembangan dibagi 3 bucket berdasarkan effort dan dependency:</p>
      <div class="row g-3">

        <!-- Quick Win (1-3 bulan) -->
        <div class="col-md-4">
          <div class="card h-100 border-success">
            <div class="card-header bg-success text-white">
              <strong><i class="bi bi-rocket-takeoff"></i> Quick Win (1-3 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">Config/UI tweak existing kode, no new arsitektur.</p>
              <ul class="small mb-0">
                <li>{item 1 + 1-baris justifikasi}</li>
                <li>{item 2 + justifikasi}</li>
                <li>{item 3 + justifikasi}</li>
                <li>{item 4 — opsional}</li>
                <li>{item 5 — opsional}</li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Medium (3-9 bulan) -->
        <div class="col-md-4">
          <div class="card h-100 border-warning">
            <div class="card-header bg-warning text-dark">
              <strong><i class="bi bi-arrow-up-right-circle"></i> Medium (3-9 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">New feature, 1-2 controller baru, kemungkinan migration DB.</p>
              <ul class="small mb-0">
                <li>{item 1 + justifikasi}</li>
                <li>{item 2 + justifikasi}</li>
                <li>{item 3 + justifikasi}</li>
                <li>{item 4 — opsional}</li>
                <li>{item 5 — opsional}</li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Long-term (>9 bulan) -->
        <div class="col-md-4">
          <div class="card h-100 border-danger">
            <div class="card-header bg-danger text-white">
              <strong><i class="bi bi-flag"></i> Long-term (>9 bulan)</strong>
            </div>
            <div class="card-body">
              <p class="small text-muted mb-2">Arsitektur baru atau integrasi external system.</p>
              <ul class="small mb-0">
                <li>{item 1 + justifikasi}</li>
                <li>{item 2 + justifikasi}</li>
                <li>{item 3 + justifikasi}</li>
                <li>{item 4 — opsional}</li>
                <li>{item 5 — opsional}</li>
              </ul>
            </div>
          </div>
        </div>

      </div>
      <p class="small text-muted mt-3"><i class="bi bi-exclamation-triangle"></i> Estimate awam — verify dengan tim IT sebelum execute. Sequencing dapat berubah berdasarkan resource availability.</p>
```

**CATATAN PENTING**: Generate 3-5 item per bucket dari scratchpad. Tidak boleh ada `{...}` tersisa. Tiap item: nama + 1-baris justifikasi konkret (bukan generic).

- [ ] **Step 2: Verifikasi 3 card roadmap**

```bash
grep -c 'border-success\|border-warning\|border-danger' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: increment by ≥3 (status-card sudah pakai border tapi tidak persis match — verifikasi visual saja).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §9.4 — roadmap 3 bucket Quick Win/Medium/Long-term"
```

---

## Task 7: §9.5 — Sumber Referensi

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

**Source data**: Scratchpad section "Sources" per platform (top 5-7 URL utama).

- [ ] **Step 1: Replace placeholder §9.5 dengan mini list referensi**

Ganti `<p><em>Konten Task 7</em></p>` di bawah `<h4 id="sec-9-5">` dengan:

```html
      <p class="small">5-7 sumber utama yang dicite di §9 (vendor docs + review independen):</p>
      <ul class="small">
        <li><a href="{WORKDAY_DOC_URL}" target="_blank" rel="noopener">Workday Learning — Certification Tracking</a> (vendor docs)</li>
        <li><a href="{SAP_DOC_URL}" target="_blank" rel="noopener">SAP SuccessFactors Learning — Suite Overview</a> (vendor docs)</li>
        <li><a href="{CORNERSTONE_DOC_URL}" target="_blank" rel="noopener">Cornerstone Learning — Compliance & Audit</a> (vendor docs)</li>
        <li><a href="{DOCEBO_DOC_URL}" target="_blank" rel="noopener">Docebo — Certification & Skills</a> (vendor docs)</li>
        <li><a href="{TALENTLMS_DOC_URL}" target="_blank" rel="noopener">TalentLMS — Certifications</a> (vendor docs)</li>
        <li><a href="{G2_OR_CAPTERRA_LMS_URL}" target="_blank" rel="noopener">G2 — Top LMS Comparison 2025</a> (review independen)</li>
        <li><a href="{G2_OR_CAPTERRA_HRIS_URL}" target="_blank" rel="noopener">Capterra — HRIS Certification Management</a> (review independen)</li>
      </ul>
      <p class="small text-muted"><i class="bi bi-shield-check"></i> Semua URL valid per riset 2026-05. Bila vendor mengubah struktur web, link mungkin redirect — gunakan WebArchive bila perlu.</p>
```

**CATATAN PENTING**: Replace semua `{...}` URL dengan URL konkret dari scratchpad. Pastikan tiap href valid format (https://...).

- [ ] **Step 2: Verifikasi tidak ada placeholder `{...}` tersisa di seluruh §9**

```bash
grep -n '{[A-Z_]*}' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: no match.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §9.5 — 5-7 sumber referensi external + disclaimer URL"
```

---

## Task 8: Final QA — line count, accordion test, theme toggle, render check

**Files:**
- Modify: tidak ada (verifikasi saja)

- [ ] **Step 1: Line count check**

```bash
wc -l docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: ~720-820 baris (target spec §8: file final ~800 baris dari 467).

- [ ] **Step 2: Section count check**

```bash
grep -c 'id="sec-' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: 9 (sec-1 sampai sec-9).

- [ ] **Step 3: Sub-section §9 check**

```bash
grep 'id="sec-9-' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: 5 match (sec-9-1 sampai sec-9-5).

- [ ] **Step 4: External link count check**

```bash
grep -c 'target="_blank"' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: ≥12 (5 vendor card footer + 5 sumber referensi list + cite di top-5 accordion + opsional cite di benchmark).

- [ ] **Step 5: Verifikasi tidak ada placeholder/draft text**

```bash
grep -nE '(Konten Task|TODO|TBD|\{[A-Z_]+\}|placeholder)' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```
Expected: no match.

- [ ] **Step 6: Verifikasi acceptance criteria spec §8**

Checklist (12 item):
1. ✅ §9 ter-append setelah §8
2. ✅ Mini-nav punya link §9
3. ✅ §9.1: 5 card benchmark + cite + disclaimer date
4. ✅ §9.2: matrix 20-25 row, wrap table-responsive
5. ✅ §9.3: 5 accordion item top-5
6. ✅ §9.4: 3 card roadmap
7. ✅ §9.5: 5-7 link sumber
8. ✅ Severity pakai badge bg-danger/warning/success
9. ✅ Semua external link target=_blank rel=noopener
10. ✅ Bahasa awam — no endpoint/file:line/SQL/ICE
11. ✅ Live research done (scratchpad ada)
12. ✅ Render OK Chrome+Edge — pending Playwright/manual

- [ ] **Step 7: Playwright spot check (kalau available)**

Coba `mcp__plugin_playwright_playwright__browser_navigate` ke file URL. Bila gagal (Chrome session conflict), skip dan minta user manual verify.

Bila berhasil:
- `browser_snapshot` — verifikasi §9 ada di DOM
- Klik salah satu accordion header (e.g., `#gapHeading1` button) — verifikasi body expand
- Klik `#theme-toggle` — verifikasi §9 ikut switch theme (badge contrast tetap OK)
- `browser_console_messages` — verifikasi no JS error

- [ ] **Step 8: Final commit (kalau ada minor fix dari QA)**

Kalau ada perubahan dari QA:
```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): final QA §9 — accordion + theme + render verified"
```

Kalau tidak ada perubahan, skip commit.

---

## Final Report

Setelah Task 8 selesai:

1. Report ke user:
   - File path: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`
   - Total commits: 7-8 (Task 1 research + Task 2-7 content + opsional Task 8 fix)
   - Total baris file final: ~800
   - Acceptance criteria: 12/12 ✅ (atau 11/12 kalau Playwright skip)
2. Ingatkan user untuk:
   - Visual verify final di browser (Chrome/Edge desktop + mobile)
   - Decision: keep atau delete scratchpad `.planning/research/2026-05-26-gap-section-research.md`
   - Push ke origin/main kalau OK (bersama 12 commit ekosistem-sertifikat awam sebelumnya yang masih unpushed)
   - Update MEMORY.md
