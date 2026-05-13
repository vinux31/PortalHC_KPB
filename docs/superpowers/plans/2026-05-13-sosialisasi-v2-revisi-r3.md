# Sosialisasi-v2 Revisi R3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Revisi konten sosialisasi-v2.html R3 — singkatan platform akurat, nama Assessment Umum (general), IDP page sebagai perpustakaan, alur coaching simplifikasi, semua chip role/akses dihapus. 18 slide unchanged total.

**Architecture:** Single HTML file edit. 13 atomic per-slide commits + setup + final QA. Each task: read existing exact string → Edit tool find/replace → grep verify → commit. Tag `sosialisasi-v2.2.4` after Playwright + PDF QA.

**Tech Stack:** HTML5 + Alpine.js (existing) + Tailwind CDN. No new dependencies.

---

## Context

Spec: `docs/superpowers/specs/2026-05-13-sosialisasi-v2-revisi-r3-design.md`. User feedback round 3 after PDF v2.2.3 ship.

---

## Task 0: Setup Branch

- [ ] **Step 1: Branch out**

```bash
git status  # confirm clean on main
git checkout -b sosialisasi-v2.2.4
```

Expected: `Switched to a new branch 'sosialisasi-v2.2.4'`.

- [ ] **Step 2: Commit spec + plan**

```bash
git add docs/superpowers/specs/2026-05-13-sosialisasi-v2-revisi-r3-design.md docs/superpowers/plans/2026-05-13-sosialisasi-v2-revisi-r3.md
git commit -m "docs(sosialisasi-v2): R3 spec + plan revisi konten

Round 3 setelah v2.2.3: singkatan platform, Assessment Umum,
IDP perpustakaan, coaching workflow simplifikasi, hapus chip role."
```

---

## Task 1: Slide 2 — Singkatan Platform

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update CMP singkatan**

```
old: <strong class="brand-red">CMP</strong> (Assessment),
new: <strong class="brand-red">CMP</strong> (Competency Management Platform),
```

- [ ] **Step 2: Update CDP singkatan**

```
old: <strong class="brand-red">CDP</strong> (Coaching &amp; Development),
new: <strong class="brand-red">CDP</strong> (Competency Development Platform),
```

- [ ] **Step 3: Verify**

```bash
grep -c "Competency Management Platform" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -c "Competency Development Platform" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: ≥ 1 each.

- [ ] **Step 4: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 2 — singkatan CMP/CDP jadi 'Competency Management/Development Platform'

User feedback: 'CMP (Assessment)' / 'CDP (Coaching & Dev)' terlalu reduktif.
Pakai nama platform penuh."
```

---

## Task 2: Slide 3 — CMP Definisi + Subtitle + Hapus Role Chip

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update CMP subtitle**

```
old: <div class="text-xs uppercase text-slate-500 mb-2">Competency Management</div>
new: <div class="text-xs uppercase text-slate-500 mb-2">Competency Management Platform</div>
```

- [ ] **Step 2: Update CDP subtitle**

```
old: <div class="text-xs uppercase text-slate-500 mb-2">Competency Development</div>
new: <div class="text-xs uppercase text-slate-500 mb-2">Competency Development Platform</div>
```

- [ ] **Step 3: Update CMP definisi**

```
old: <p class="text-xs text-slate-700 mb-3">Platform digital untuk pengelolaan kompetensi secara terintegrasi &mdash; penyusunan kebutuhan kompetensi jabatan, pelaksanaan asesmen teknis &amp; leadership, serta IDP.</p>
new: <p class="text-xs text-slate-700 mb-3">Platform digital untuk pengelolaan kompetensi secara terintegrasi &mdash; penyusunan <strong>KKJ, IDP</strong>, pelaksanaan asesmen teknis &amp; <strong>Safety</strong>.</p>
```

- [ ] **Step 4: Hapus CMP role chip footer**

```
old:                 <div class="bg-slate-50 rounded p-2 text-xs text-slate-700">
                  <strong>Role:</strong> Admin · HC · Coachee
                </div>
              </div>

              <!-- Kolom 2: CDP -->
new:               </div>

              <!-- Kolom 2: CDP -->
```

- [ ] **Step 5: Hapus CDP role chip footer**

```
old:                 <div class="bg-slate-50 rounded p-2 text-xs text-slate-700">
                  <strong>Role:</strong> HC · Coach · SrSpv · SH · Coachee
                </div>
              </div>

              <!-- Kolom 3: BP (For Future) -->
new:               </div>

              <!-- Kolom 3: BP (For Future) -->
```

- [ ] **Step 6: Verify**

```bash
grep -c "Competency Management Platform" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 2
grep -c "penyusunan <strong>KKJ, IDP</strong>" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
grep -cE "Role:</strong> Admin · HC|Role:</strong> HC · Coach" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 7: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 3 — CMP definisi reorder + subtitle Platform + hapus role chip

- CMP/CDP subtitle: 'Competency Management/Development' → '... Platform'
- CMP definisi: 'kebutuhan kompetensi jabatan, asesmen teknis & leadership, serta IDP'
  → 'penyusunan KKJ, IDP, pelaksanaan asesmen teknis & Safety'
- Hapus footer chip 'Role: ...' di CMP + CDP kolom"
```

---

## Task 3: Slide 5 — Sistem Assessment (Judul + Row + Kategori + Tips)

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update h2 judul**

```
old: <h2 class="text-4xl font-bold brand-navy mb-6">Sistem Assessment (CMP)</h2>
new: <h2 class="text-4xl font-bold brand-navy mb-6">Sistem Assessment</h2>
```

- [ ] **Step 2: Update subtitle paragraph**

```
old: <p class="text-slate-600 mb-6">Dua jenis assessment utama dalam Competency Management Program:</p>
new: <p class="text-slate-600 mb-6">Dua jenis assessment utama:</p>
```

- [ ] **Step 3: Update Row 1 nama**

```
old:                     <td class="px-4 py-4 font-bold brand-navy">Assessment OJT</td>
new:                     <td class="px-4 py-4 font-bold brand-navy">Assessment Umum</td>
```

- [ ] **Step 4: Update Row 1 kategori**

```
old:                     <td class="px-4 py-4 text-slate-700">Per unit operasi<br><span class="text-xs text-slate-500">(misal: Alkylation, RFCC, NHT)</span></td>
new:                     <td class="px-4 py-4 text-slate-700">Per batch unit operasi / batch</td>
```

- [ ] **Step 5: Update tips text**

```
old:               <div class="text-sm text-slate-700">OJT untuk evaluasi reguler per unit, Proton untuk program pengembangan 3 tahun.</div>
new:               <div class="text-sm text-slate-700">Assessment Umum untuk evaluasi reguler per batch unit/jenis kompetensi, Proton untuk program pengembangan 3 tahun.</div>
```

- [ ] **Step 6: Verify**

```bash
grep -c "Assessment OJT" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Assessment Umum" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 2 (table + tips)
grep -c "Per batch unit operasi" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
```

- [ ] **Step 7: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 5 — Sistem Assessment general (drop OJT spesifik)

- Judul: 'Sistem Assessment (CMP)' → 'Sistem Assessment'
- Row 1 nama: 'Assessment OJT' → 'Assessment Umum' (cover OJT/IHT/K3 dll)
- Kategori: 'Per unit operasi (Alkylation, RFCC, NHT)' → 'Per batch unit operasi / batch'
- Tips: update reference Assessment Umum"
```

---

## Task 4: Slide 6 — Pre/Post Card #2 Training

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update card #2 title**

```
old:                 <div class="font-bold brand-navy text-sm mb-1">Training / OJT</div>
new:                 <div class="font-bold brand-navy text-sm mb-1">Training</div>
```

Sub-text "Sesi pembelajaran (in-class atau on-the-job)" keep — sudah generic.

- [ ] **Step 2: Verify**

```bash
grep -c "Training / OJT" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 6 — Pre/Post card #2 'Training / OJT' → 'Training'"
```

---

## Task 5: Slide 7 — Judul Alur Assessment + Hapus Role Footer

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update comment marker**

```
old: <!-- Slide 7: Alur Assessment OJT -->
new: <!-- Slide 7: Alur Assessment -->
```

- [ ] **Step 2: Update h2 judul**

```
old: <h2 class="text-3xl font-bold brand-navy mb-1">Alur Assessment OJT</h2>
new: <h2 class="text-3xl font-bold brand-navy mb-1">Alur Assessment</h2>
```

- [ ] **Step 3: Hapus Role text di footer flex (keep Output side)**

```
old:               <div><strong>Role:</strong> Admin / HC siapkan data, Tim Operasional jadi peserta.</div>
              <div class="text-slate-300">|</div>
              <div><strong>Output:</strong> skor pekerja, status kelulusan, rekap per unit.</div>
new:               <div><strong>Output:</strong> skor pekerja, status kelulusan, rekap per unit.</div>
```

- [ ] **Step 4: Verify**

```bash
grep -c "Alur Assessment OJT" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Admin / HC siapkan data" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 7 — judul 'Alur Assessment OJT' → 'Alur Assessment' + hapus role footer"
```

---

## Task 6: Slide 8 — Card Tahun 3 Hapus Bullet Penilaian 5 Aspek

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Hapus bullet line**

```
old:                 <ul class="text-sm text-slate-700 space-y-1">
                  <li>• Track mahir</li>
                  <li>• <strong>Interview offline</strong> oleh panel juri</li>
                  <li>• Penilaian 5 aspek (skor 1-5)</li>
                </ul>
new:                 <ul class="text-sm text-slate-700 space-y-1">
                  <li>• Track mahir</li>
                  <li>• <strong>Interview offline</strong> oleh panel juri</li>
                </ul>
```

- [ ] **Step 2: Verify**

```bash
grep -c "Penilaian 5 aspek" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 1 still (slide 14 row Assessment) — kita hapus terpisah di Task 10
```

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 8 — Card Tahun 3 hapus bullet 'Penilaian 5 aspek (skor 1-5)'"
```

---

## Task 7: Slide 9 — Callout "Mirip OJT" → "Mirip Assessment Umum"

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update callout title + body**

```
old:               <div class="bg-blue-50 border-l-4 border-blue-500 p-4 rounded text-sm">
                <div class="font-bold text-blue-700 mb-1">Mirip OJT</div>
                <div class="text-slate-700">Alur online sama dengan OJT, beda di kategori &amp; paket soal per track.</div>
              </div>
new:               <div class="bg-blue-50 border-l-4 border-blue-500 p-4 rounded text-sm">
                <div class="font-bold text-blue-700 mb-1">Mirip Assessment Umum</div>
                <div class="text-slate-700">Alur online sama dengan Assessment Umum, beda di kategori &amp; paket soal per track.</div>
              </div>
```

- [ ] **Step 2: Verify**

```bash
grep -c "Mirip OJT" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Mirip Assessment Umum" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
```

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 9 — callout 'Mirip OJT' → 'Mirip Assessment Umum'

Konsisten dengan rename slide 5."
```

---

## Task 8: Slide 10 — Card #3 Generalize "Penilaian Kompetensi"

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update card #3**

```
old:               <div class="col-span-2 flow-step" style="border-color: var(--amber); background: #fffbeb;">
                <div class="step-num" style="background: var(--amber);">3</div>
                <div class="font-bold" style="color: #b45309">Penilaian 5 Aspek</div>
                <div class="text-xs text-slate-600">Skor 1-5 per aspek kompetensi oleh panel</div>
              </div>
new:               <div class="col-span-2 flow-step" style="border-color: var(--amber); background: #fffbeb;">
                <div class="step-num" style="background: var(--amber);">3</div>
                <div class="font-bold" style="color: #b45309">Penilaian Kompetensi</div>
                <div class="text-xs text-slate-600">Penilaian kompetensi oleh panel juri</div>
              </div>
```

- [ ] **Step 2: Verify**

```bash
grep -c "Penilaian 5 Aspek" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Penilaian Kompetensi" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
```

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 10 — card #3 'Penilaian 5 Aspek' → 'Penilaian Kompetensi' (general)"
```

---

## Task 9: Slide 12 — IDP Card Revamp Perpustakaan + Hapus Akses Footer

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Replace IDP card content**

```
old:               <div class="bg-white border-t-4 border-green-600 rounded-lg p-5 shadow-sm">
                <div class="flex items-center gap-2 mb-3">
                  <div class="text-3xl">📋</div>
                  <div>
                    <h3 class="text-xl font-bold text-green-700">IDP</h3>
                    <div class="text-xs uppercase text-slate-500">Individual Development Plan</div>
                  </div>
                </div>
                <ul class="text-sm text-slate-700 space-y-2">
                  <li>• Target pengembangan tahunan per pekerja</li>
                  <li>• Approval bertahap atasan → HC</li>
                  <li>• Linked ke kompetensi gap analysis</li>
                  <li>• Tracking pencapaian quarterly</li>
                </ul>
                <div class="mt-3 text-xs text-slate-500"><strong>Akses:</strong> Coachee submit, atasan + HC approve.</div>
              </div>
new:               <div class="bg-white border-t-4 border-green-600 rounded-lg p-5 shadow-sm">
                <div class="flex items-center gap-2 mb-3">
                  <div class="text-3xl">📋</div>
                  <div>
                    <h3 class="text-xl font-bold text-green-700">IDP</h3>
                    <div class="text-xs uppercase text-slate-500">Individual Development Plan (Perpustakaan)</div>
                  </div>
                </div>
                <ul class="text-sm text-slate-700 space-y-2">
                  <li>📂 Repository dokumen IDP per pekerja</li>
                  <li>📄 Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)</li>
                  <li>👁️ Worker view &amp; download dokumen</li>
                  <li>🔍 Filter &amp; search per jabatan / unit</li>
                </ul>
              </div>
```

- [ ] **Step 2: Hapus Training Records akses footer**

```
old:                 <div class="mt-3 text-xs text-slate-500"><strong>Akses:</strong> HC kelola data master, Coachee submit record.</div>
              </div>
new:               </div>
```

- [ ] **Step 3: Verify**

```bash
grep -c "Perpustakaan" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
grep -c "Approval bertahap atasan" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Akses:</strong>" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 12 — IDP card revamp jadi perpustakaan dokumen

User feedback: 'page IDP itu sebetulnya perpustakaan, worker mau lihat
dokumen IDP dan KKJ ada disini'. Reframe card jadi repository view:
- Repository dokumen IDP per pekerja
- Akses dokumen KKJ
- View & download
- Filter per jabatan/unit
Hapus 'Akses: ...' footer di IDP + Training Records."
```

---

## Task 10: Slide 14 — Row Coaching Process Update + Drop "5 aspek skor 1-5"

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update Row Coaching Process**

```
old:                   <tr class="bg-white border-b border-slate-200">
                    <td class="px-3 py-3 font-bold brand-navy bg-slate-100">🔄 Coaching Process</td>
                    <td class="px-3 py-3 text-slate-700">Submit evidence → Coach review → HC review</td>
                    <td class="px-3 py-3 text-slate-700">Submit evidence → Coach review → HC review</td>
                    <td class="px-3 py-3 text-slate-700 border-l-2 border-red-300">Submit → Coach → HC review + <strong>Final Assessment Interview</strong></td>
                  </tr>
new:                   <tr class="bg-white border-b border-slate-200">
                    <td class="px-3 py-3 font-bold brand-navy bg-slate-100">🔄 Coaching Process</td>
                    <td class="px-3 py-3 text-slate-700">Submit evidence (Coach) → Multi Approval → Final Assessment</td>
                    <td class="px-3 py-3 text-slate-700">Submit evidence (Coach) → Multi Approval → Final Assessment</td>
                    <td class="px-3 py-3 text-slate-700 border-l-2 border-red-300">Submit evidence (Coach) → Multi Approval → <strong>Final Assessment Interview</strong></td>
                  </tr>
```

- [ ] **Step 2: Drop "5 aspek, skor 1-5" di row Assessment Th3**

```
old:                     <td class="px-3 py-3 text-slate-700 border-l-2 border-red-300"><strong>Interview offline</strong> oleh panel juri<br><span class="text-slate-500">(5 aspek, skor 1-5)</span></td>
new:                     <td class="px-3 py-3 text-slate-700 border-l-2 border-red-300"><strong>Interview offline</strong> oleh panel juri</td>
```

- [ ] **Step 3: Verify**

```bash
grep -c "Submit evidence (Coach) → Multi Approval" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 3
grep -c "5 aspek, skor 1-5" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Coach review → HC review" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 14 — row Coaching Process update + drop '5 aspek skor 1-5'

- Th1/Th2: 'Submit evidence → Coach review → HC review'
  → 'Submit evidence (Coach) → Multi Approval → Final Assessment'
- Th3: '... → Final Assessment Interview'
- Drop sub '(5 aspek, skor 1-5)' di row Assessment (sudah general)"
```

---

## Task 11: Slide 15 — Step 3 HC Assign + Step 4 Coach Submit + Hapus Role Footer

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update Step 3 (Mapping Track → HC Assign Coachee)**

```
old:               <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">🔗</div>
                <div class="font-bold brand-navy mb-1 text-sm">Mapping Track</div>
                <div class="text-xs text-slate-600">Assign coachee ke track tahun program</div>
              </div>
new:               <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">🔗</div>
                <div class="font-bold brand-navy mb-1 text-sm">HC Assign Coachee</div>
                <div class="text-xs text-slate-600">HC assign coachee ke track tahun program</div>
              </div>
```

- [ ] **Step 2: Update Step 4 (Kerjakan Deliverable → Coach Submit Evidence)**

```
old:               <div class="col-span-2 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">✍️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Kerjakan Deliverable</div>
                <div class="text-xs text-slate-600">Coachee submit evidence per deliverable</div>
              </div>
new:               <div class="col-span-2 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">📥</div>
                <div class="font-bold brand-navy mb-1 text-sm">Coach Submit Evidence</div>
                <div class="text-xs text-slate-600">Coach submit evidence per deliverable</div>
              </div>
```

- [ ] **Step 3: Hapus footer Role catatan (keep Output side)**

```
old:               <div><strong>Role:</strong> HC kelola silabus + guidance. Coachee submit. Reviewer (Coach/SrSpv/SH/HC) independent per-role (Phase 65 architecture).</div>
              <div class="text-slate-300">|</div>
              <div class="text-green-700"><strong>✅ Output:</strong> sertifikat tahun + eligible naik tahun berikutnya.</div>
new:               <div class="text-green-700"><strong>✅ Output:</strong> sertifikat tahun + eligible naik tahun berikutnya.</div>
```

- [ ] **Step 4: Verify**

```bash
grep -c "Mapping Track" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "HC Assign Coachee" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
grep -c "Coach Submit Evidence" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
grep -c "Phase 65 architecture" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
```

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 15 — step 3 HC Assign + step 4 Coach Submit + hapus role footer

- Step 3: 'Mapping Track' → 'HC Assign Coachee' (emphasize aktor HC)
- Step 4: 'Kerjakan Deliverable' / 'Coachee submit evidence'
  → 'Coach Submit Evidence' / 'Coach submit evidence per deliverable'
- Hapus footer 'Role: ... Phase 65 architecture' (audience non-teknis)"
```

---

## Task 12: Slide 16 — Step 4 Review Multi-Role + Step 5 Approval/Revisi

**Files:** `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Update Step 4 (Review Mendalam → Review Multi-Role)**

```
old:               <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">4</div>
                <div class="text-2xl mb-1">🔍</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Review Mendalam</div>
                <div class="text-xs text-slate-600">HC review lebih ketat</div>
              </div>
new:               <div class="col-span-2 flow-step" style="border-color: #0ea5e9; background: #f0f9ff;">
                <div class="step-num" style="background: #0ea5e9;">4</div>
                <div class="text-2xl mb-1">👀</div>
                <div class="font-bold mb-1 text-sm" style="color: #0369a1">Review Multi-Role</div>
                <div class="text-xs text-slate-600">Coach + SrSpv + SH + HC review <strong>paralel</strong> per-role</div>
              </div>
```

- [ ] **Step 2: Update Step 5 (Coaching Intensif → Approval/Revisi)**

```
old:               <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">5</div>
                <div class="text-2xl mb-1">🗣️</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Coaching Intensif</div>
                <div class="text-xs text-slate-600">Sesi coaching mendalam per deliverable, dicatat di CoachingSession log</div>
              </div>
new:               <div class="col-span-2 flow-step">
                <div class="step-num">5</div>
                <div class="text-2xl mb-1">✅</div>
                <div class="font-bold brand-navy mb-1 text-sm">Approval / Revisi</div>
                <div class="text-xs text-slate-600">Approve atau request revisi dgn komentar</div>
              </div>
```

- [ ] **Step 3: Verify**

```bash
grep -c "Review Mendalam" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Coaching Intensif" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 0
grep -c "Review Multi-Role" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 2 (slide 15 + slide 16)
grep -c "Approval / Revisi" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 2
```

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(sosialisasi-v2): slide 16 — step 4 Review Multi-Role + step 5 Approval/Revisi

- Step 4: 'Review Mendalam (HC review lebih ketat)' → 'Review Multi-Role
  (Coach+SrSpv+SH+HC paralel per-role)' — match slide 15 step 5
- Step 5: 'Coaching Intensif (CoachingSession log)' → 'Approval / Revisi
  (Approve/request revisi dgn komentar)' — match slide 15 step 6"
```

---

## Task 13: QA + Tag v2.2.4 + Merge

**Files:** Verify only.

- [ ] **Step 1: Grep final consistency check**

```bash
# Yang harus 0:
grep -cE "Assessment OJT|Penilaian 5 Aspek|Mapping Track|Coaching Intensif|Review Mendalam|Training / OJT|Mirip OJT|5 aspek, skor 1-5|Phase 65 architecture" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
# Expected: 0
```

```bash
# Yang harus muncul:
grep -c "Assessment Umum" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 2
grep -c "Penilaian Kompetensi" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 1
grep -c "Perpustakaan" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 1
grep -c "Competency Management Platform" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 2
grep -c "Competency Development Platform" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # >= 2
grep -c "HC Assign Coachee" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
grep -c "Coach Submit Evidence" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html  # 1
```

- [ ] **Step 2: Playwright spot-check slide 3/5/12/15/16**

```bash
cd sosialisasi-portalhc/sosialisasi-v2 && python -m http.server 8765 &
```

Open di Playwright `http://localhost:8765/sosialisasi-v2.html#slide-N` untuk N=3,5,12,15,16. Screenshot each. Verifikasi:
- Slide 3: CMP definisi "penyusunan KKJ, IDP, ... & Safety"; CMP/CDP subtitle "Platform"; no role chip
- Slide 5: judul "Sistem Assessment" tanpa CMP; Row 1 "Assessment Umum"
- Slide 12: IDP card subtitle "Individual Development Plan (Perpustakaan)"; 4 bullet new
- Slide 15: step 3 "HC Assign Coachee", step 4 "Coach Submit Evidence", no role footer
- Slide 16: step 4 "Review Multi-Role" blue, step 5 "Approval / Revisi" navy

- [ ] **Step 3: Headless PDF generate + spot-check**

```bash
"/c/Program Files/Google/Chrome/Application/chrome.exe" --headless=new --disable-gpu --no-margins --no-pdf-header-footer --print-to-pdf=/tmp/v2.2.4-export.pdf --virtual-time-budget=5000 "http://localhost:8765/sosialisasi-v2.html"
python -c "import fitz; doc = fitz.open('/tmp/v2.2.4-export.pdf'); print(f'Pages: {doc.page_count}, Size: {doc[0].rect}')"
```

Expected: 18 pages A4 landscape (842×595pt).

- [ ] **Step 4: Stop server**

```bash
taskkill //F //IM python.exe
```

- [ ] **Step 5: Tag git**

```bash
git tag sosialisasi-v2.2.4 -m "Content revisi R3 — singkatan platform, Assessment Umum, IDP perpustakaan, coaching workflow simplifikasi, hapus chip role

13 region edit:
- Slide 2 singkatan platform (CMP/CDP nama penuh)
- Slide 3 CMP definisi reorder (KKJ + IDP + Safety) + subtitle Platform + hapus role chip
- Slide 5 Sistem Assessment general (drop OJT spesifik)
- Slide 6 Pre/Post card #2 'Training / OJT' → 'Training'
- Slide 7 judul 'Alur Assessment' (drop OJT) + hapus role footer
- Slide 8 Card Th3 hapus bullet 'Penilaian 5 aspek'
- Slide 9 callout 'Mirip Assessment Umum'
- Slide 10 card #3 'Penilaian Kompetensi' (general)
- Slide 12 IDP card → perpustakaan + hapus akses footer
- Slide 14 row Coaching Process update + drop '5 aspek skor 1-5'
- Slide 15 step 3 HC Assign + step 4 Coach Submit + hapus role footer
- Slide 16 step 4 Review Multi-Role + step 5 Approval/Revisi"
```

- [ ] **Step 6: Merge ke main**

```bash
git checkout main
git merge sosialisasi-v2.2.4 --no-ff -m "merge: sosialisasi-v2 R3 content revisi → v2.2.4

Konten revisi: singkatan platform, Assessment Umum (drop OJT spesifik),
IDP page sebagai perpustakaan, coaching workflow simplifikasi,
hapus semua chip role/akses dari slide content."
git branch -d sosialisasi-v2.2.4
```

---

## Verification Summary

Setelah Task 13:

- `sosialisasi-v2.html` 18 slide unchanged structure, 13 region content edit
- Tag `sosialisasi-v2.2.4` on main
- Branch `sosialisasi-v2.2.4` deleted
- PDF export via headless Chrome verify 18 pages A4 landscape, layout match HTML
