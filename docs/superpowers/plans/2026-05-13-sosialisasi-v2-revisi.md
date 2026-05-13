# Sosialisasi-v2 Revisi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Revisi `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` dari 15 slide menjadi 20 slide — rewrite slide 2/3, fix slide 5/12/13, insert 5 slide baru (3 BigMenu + Pre/Post Test + IDP/Training Records). Spec: `docs/superpowers/specs/2026-05-13-sosialisasi-v2-revisi-design.md`.

**Architecture:** Single monolithic HTML + Alpine.js state machine (`x-show="current === N"`). Edit dilakukan bottom-up agar comment marker `<!-- Slide N: ... -->` tetap unique untuk Edit tool match. Renumbering x-show dilakukan SEBELUM insert, sehingga insert tinggal pakai slot yang sudah dilepas. Total slide-state `total: 15` di Alpine `deck()` function diupdate ke `20` di task terakhir.

**Tech Stack:** HTML5, Alpine.js (existing), Tailwind CDN (existing), shared.css. No new dependencies.

---

## File Structure

**File yang disentuh (semua di satu file):**
- `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — semua perubahan content + state max value.

**Tidak ada file baru.** Asset folder (`assets/`) tidak disentuh — semua icon pakai Unicode emoji & Tailwind classes existing.

**Backup branch:** Sebelum mulai, snapshot di branch `sosialisasi-v2-revisi`.

---

## Task 0: Setup Branch & Baseline

**Files:**
- Verify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` (15 slide existing)

- [ ] **Step 1: Check git status clean**

```bash
git status
```
Expected: `working tree clean` di branch `main`.

- [ ] **Step 2: Create working branch**

```bash
git checkout -b sosialisasi-v2-revisi
```
Expected: `Switched to a new branch 'sosialisasi-v2-revisi'`.

- [ ] **Step 3: Verify total slide existing = 15**

Run: read line 900 di HTML file. Expected: `total: 15,`. Read line 191 expected: `<!-- Slide 1: Cover -->`. Read line 819 expected: `<!-- Slide 15: Closing -->`.

- [ ] **Step 4: Buka file di browser, screenshot baseline**

Buka `sosialisasi-v2.html` di browser, klik tombol next sampai slide 15, screenshot tiap slide ke folder lokal `/tmp/sosialisasi-baseline/` sebagai bukti rollback point. (Opsional kalau tidak ada browser di env — skip.)

---

## Task 1: Renumber Existing Slides (Bottom-Up)

**Goal:** Mass rename `x-show="current === N"` + comment marker `<!-- Slide N: ... -->` ke nomor slide BARU. Tidak ada perubahan content. Order bottom-up agar marker tetap unique saat Edit.

**Mapping rename:**
- Slide 15 → 20 (Closing)
- Slide 14 → 19 (Timeline Summary)
- Slide 13 → 18 (Alur Coaching Th 3)
- Slide 12 → 17 (Alur Coaching Th 1-2)
- Slide 11 → 16 (Fokus Kompetensi)
- Slide 10 → 15 (Hierarki Kompetensi)
- Slide 9 → 13 (Coaching CDP Overview) *(skip 14 — reserved untuk IDP)*
- Slide 8 → 12 (Alur Proton Th 3)
- Slide 7 → 11 (Alur Proton Th 1-2)
- Slide 6 → 10 (Assessment Proton)
- Slide 5 → 9 (Alur Assessment) *(akan di-fix di Task 9)*
- Slide 4 → 7 (Sistem Assessment CMP) *(skip 8 — reserved untuk Pre/Post Test)*
- Slide 3 → 6 (Role) *(akan di-rewrite di Task 8 — pakai slot 6)*

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Rename slide 15 → 20**

```bash
# Search & replace dengan Edit tool:
# old: <!-- Slide 15: Closing -->
# new: <!-- Slide 20: Closing -->
# AND
# old: <section class="slide" x-show="current === 15" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
# new: <section class="slide" x-show="current === 20" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
```

- [ ] **Step 2: Rename slide 14 → 19**

```
old: <!-- Slide 14: Timeline Summary -->
new: <!-- Slide 19: Timeline Summary -->
old: <section class="slide" x-show="current === 14"
new: <section class="slide" x-show="current === 19"
```

- [ ] **Step 3: Rename slide 13 → 18**

```
old: <!-- Slide 13: Alur Coaching Th 3 (MERGED 15+16) -->
new: <!-- Slide 18: Alur Coaching Th 3 (will fix in Task 11) -->
old: <section class="slide" x-show="current === 13"
new: <section class="slide" x-show="current === 18"
```

- [ ] **Step 4: Rename slide 12 → 17**

```
old: <!-- Slide 12: Alur Coaching Th 1-2 (MERGED 13+14) -->
new: <!-- Slide 17: Alur Coaching Th 1-2 (will fix in Task 10) -->
old: <section class="slide" x-show="current === 12"
new: <section class="slide" x-show="current === 17"
```

- [ ] **Step 5: Rename slide 11 → 16**

```
old: <!-- Slide 11: Fokus Kompetensi (Table 4x5) -->
new: <!-- Slide 16: Fokus Kompetensi (Table 4x5) -->
old: <section class="slide" x-show="current === 11"
new: <section class="slide" x-show="current === 16"
```

- [ ] **Step 6: Rename slide 10 → 15**

```
old: <!-- Slide 10: Hierarki Kompetensi -->
new: <!-- Slide 15: Hierarki Kompetensi -->
old: <section class="slide" x-show="current === 10"
new: <section class="slide" x-show="current === 15"
```

- [ ] **Step 7: Rename slide 9 → 13**

```
old: <!-- Slide 9: Coaching CDP Overview -->
new: <!-- Slide 13: Coaching CDP Overview -->
old: <section class="slide" x-show="current === 9"
new: <section class="slide" x-show="current === 13"
```

- [ ] **Step 8: Rename slide 8 → 12**

```
old: <!-- Slide 8: Alur Proton Th 3 -->
new: <!-- Slide 12: Alur Proton Th 3 -->
old: <section class="slide" x-show="current === 8"
new: <section class="slide" x-show="current === 12"
```

- [ ] **Step 9: Rename slide 7 → 11**

```
old: <!-- Slide 7: Alur Proton Th 1-2 -->
new: <!-- Slide 11: Alur Proton Th 1-2 -->
old: <section class="slide" x-show="current === 7"
new: <section class="slide" x-show="current === 11"
```

- [ ] **Step 10: Rename slide 6 → 10**

```
old: <!-- Slide 6: Assessment Proton -->
new: <!-- Slide 10: Assessment Proton -->
old: <section class="slide" x-show="current === 6"
new: <section class="slide" x-show="current === 10"
```

- [ ] **Step 11: Rename slide 5 → 9**

```
old: <!-- Slide 5: Alur Assessment (MERGED 5+6) -->
new: <!-- Slide 9: Alur Assessment OJT (will fix in Task 9) -->
old: <section class="slide" x-show="current === 5"
new: <section class="slide" x-show="current === 9"
```

- [ ] **Step 12: Rename slide 4 → 7**

```
old: <!-- Slide 4: Sistem Assessment CMP -->
new: <!-- Slide 7: Sistem Assessment CMP -->
old: <section class="slide" x-show="current === 4"
new: <section class="slide" x-show="current === 7"
```

- [ ] **Step 13: Rename slide 3 → 6**

```
old: <!-- Slide 3: Role -->
new: <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
old: <section class="slide" x-show="current === 3"
new: <section class="slide" x-show="current === 6"
```

- [ ] **Step 14: Verify no stray x-show in range 3-5, 8, 14**

Run Grep: `pattern: x-show="current === (3|4|5|8|14)"`. Expected: 0 matches. Slot 3/4/5/8/14 sudah free untuk insert.

- [ ] **Step 15: Commit**

```bash
git add sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
git commit -m "refactor(sosialisasi-v2): renumber existing slides bottom-up

Free slot 3/4/5/8/14 untuk insert slide baru (BigMenu CMP/CDP/BP,
Pre/Post Test, IDP/Training Records). Content & total state belum diupdate."
```

---

## Task 2: Rewrite Slide 2 (Definisi + 3 Card Value)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 2 section.

- [ ] **Step 1: Replace slide 2 content**

```
Find:
        <!-- Slide 2: Definisi -->
        <section class="slide" x-show="current === 2" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-3xl mx-auto">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Pengenalan</div>
            <h2 class="text-4xl font-bold brand-navy mb-6">Apa itu HC Portal KPB?</h2>
            <div class="bg-slate-50 dark:bg-slate-700 rounded-xl p-6 border-l-4 border-brand-navy">
              <p class="text-lg leading-relaxed text-slate-700 dark:text-slate-200">
                Sistem informasi berbasis web yang digunakan oleh
                <strong class="brand-navy">Tim Human Capital Kilang Pertamina Balikpapan</strong>
                untuk mengelola <strong>kompetensi</strong> dan <strong>pengembangan pekerja</strong>
                melalui program <strong class="brand-red">CMP (Competency Management Program)</strong>.
              </p>
            </div>
            <div class="grid grid-cols-3 gap-4 mt-8">
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-4 text-center">
                <div class="text-3xl mb-2">📊</div>
                <div class="text-sm font-medium">Assessment</div>
              </div>
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-4 text-center">
                <div class="text-3xl mb-2">🎯</div>
                <div class="text-sm font-medium">Coaching</div>
              </div>
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-4 text-center">
                <div class="text-3xl mb-2">🏆</div>
                <div class="text-sm font-medium">Sertifikasi</div>
              </div>
            </div>
          </div>
        </section>
```

Replace with:

```html
        <!-- Slide 2: Definisi -->
        <section class="slide" x-show="current === 2" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-4xl mx-auto">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Pengenalan</div>
            <h2 class="text-4xl font-bold brand-navy mb-6">Apa itu HC Portal KPB?</h2>
            <div class="bg-slate-50 dark:bg-slate-700 rounded-xl p-6 border-l-4 border-brand-navy">
              <p class="text-lg leading-relaxed text-slate-700 dark:text-slate-200">
                Sistem informasi berbasis web Tim
                <strong class="brand-navy">Human Capital Kilang Pertamina Balikpapan</strong>
                untuk <strong>MENGELOLA</strong> &middot; <strong>MENGEMBANGKAN</strong> &middot; <strong>MENDAMPINGI</strong>
                kompetensi pekerja lewat tiga platform terpadu:
                <strong class="brand-red">CMP</strong> (Assessment),
                <strong class="brand-red">CDP</strong> (Coaching &amp; Development),
                <strong class="brand-red">BP</strong> (Business Partner &mdash; For Future).
              </p>
            </div>
            <div class="grid grid-cols-3 gap-4 mt-8">
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-5 text-center">
                <div class="text-3xl mb-2">🎯</div>
                <div class="text-sm font-bold brand-navy mb-1">Terpusat</div>
                <div class="text-xs text-slate-600">Satu portal untuk seluruh proses kompetensi &amp; pengembangan</div>
              </div>
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-5 text-center">
                <div class="text-3xl mb-2">📐</div>
                <div class="text-sm font-bold brand-navy mb-1">Terstandar</div>
                <div class="text-xs text-slate-600">Kriteria, deliverable, sertifikasi mengacu standard KPB</div>
              </div>
              <div class="card-hover bg-white border border-slate-200 rounded-lg p-5 text-center">
                <div class="text-3xl mb-2">📊</div>
                <div class="text-sm font-bold brand-navy mb-1">Terukur</div>
                <div class="text-xs text-slate-600">Skor, progress, level kompetensi tertrace per pekerja</div>
              </div>
            </div>
          </div>
        </section>
```

- [ ] **Step 2: Verify replacement**

Run Grep: `pattern: "Terpusat"`. Expected 1 match in slide 2. Run Grep: `pattern: "Competency Management Program"`. Expected 0 matches (replaced by "tiga platform terpadu").

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): rewrite slide 2 — definisi cakup 3 platform + 3 card value

- Definisi: tambah cakupan CMP/CDP/BP, ganti 'CMP Program' jadi 3 platform
- 3 card: ganti Assessment/Coaching/Sertifikasi → Terpusat/Terstandar/Terukur
- max-w 3xl → 4xl agar text fit"
```

---

## Task 3: Insert Slide 3 — BigMenu CMP

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — insert sebelum slide 6 (Role).

- [ ] **Step 1: Insert slide 3 block sebelum `<!-- Slide 6: Role`**

```
Find:
        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

Replace with:

```html
        <!-- Slide 3: BigMenu CMP -->
        <section class="slide" x-show="current === 3" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Big Menu 1 / 3</div>
            <h2 class="text-4xl font-bold brand-navy mb-2">CMP <span class="text-2xl font-normal text-slate-500">— Competency Management Platform</span></h2>
            <p class="text-slate-600 mb-6">Platform untuk <strong>mengukur &amp; memvalidasi</strong> kompetensi pekerja melalui assessment &amp; sertifikasi.</p>
            <div class="grid grid-cols-4 gap-3 mb-6">
              <div class="card-hover bg-white border-t-4 border-brand-navy rounded-lg p-4 shadow-sm text-center">
                <div class="text-3xl mb-2">📝</div>
                <div class="font-bold brand-navy text-sm mb-1">Assessment OJT</div>
                <div class="text-xs text-slate-600">Ujian online per unit operasi</div>
              </div>
              <div class="card-hover bg-white border-t-4 border-brand-red rounded-lg p-4 shadow-sm text-center">
                <div class="text-3xl mb-2">🎓</div>
                <div class="font-bold brand-red text-sm mb-1">Assessment Proton</div>
                <div class="text-xs text-slate-600">Program 3-tahun (Th 1-2 online, Th 3 interview)</div>
              </div>
              <div class="card-hover bg-white border-t-4 border-blue-500 rounded-lg p-4 shadow-sm text-center">
                <div class="text-3xl mb-2">🔄</div>
                <div class="font-bold text-blue-700 text-sm mb-1">Pre / Post Test</div>
                <div class="text-xs text-slate-600">Ukur efektivitas training (gain score)</div>
              </div>
              <div class="card-hover bg-white border-t-4 border-amber-500 rounded-lg p-4 shadow-sm text-center">
                <div class="text-3xl mb-2">🏆</div>
                <div class="font-bold text-amber-700 text-sm mb-1">Sertifikasi</div>
                <div class="text-xs text-slate-600">Otomatis + renewal lifecycle</div>
              </div>
            </div>
            <div class="bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-sm text-slate-700 dark:text-slate-200">
              <strong>Role yang akses:</strong> Admin · HC · Coachee
            </div>
          </div>
        </section>

        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Big Menu 1 / 3"`. Expected 1 match. Run Grep: `pattern: x-show="current === 3"`. Expected 1 match.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): insert slide 3 — BigMenu CMP

4 sub-modul: Assessment OJT, Assessment Proton, Pre/Post Test, Sertifikasi.
Role akses: Admin/HC/Coachee."
```

---

## Task 4: Insert Slide 4 — BigMenu CDP

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — insert antara slide 3 (CMP, baru) dan slide 6 (Role).

- [ ] **Step 1: Insert slide 4 block sebelum `<!-- Slide 6: Role`**

```
Find:
        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

Replace with:

```html
        <!-- Slide 4: BigMenu CDP -->
        <section class="slide" x-show="current === 4" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Big Menu 2 / 3</div>
            <h2 class="text-4xl font-bold brand-navy mb-2">CDP <span class="text-2xl font-normal text-slate-500">— Competency Development Platform</span></h2>
            <p class="text-slate-600 mb-6">Platform untuk <strong>mengembangkan</strong> kompetensi lewat coaching, development plan, &amp; training records.</p>
            <div class="grid grid-cols-3 gap-4 mb-6">
              <div class="card-hover bg-white border-t-4 border-brand-navy rounded-lg p-5 shadow-sm text-center">
                <div class="text-3xl mb-2">🎯</div>
                <div class="font-bold brand-navy text-sm mb-1">Coaching Proton</div>
                <div class="text-xs text-slate-600">Silabus + deliverable + review multi-role (Th 1-3)</div>
              </div>
              <div class="card-hover bg-white border-t-4 border-green-600 rounded-lg p-5 shadow-sm text-center">
                <div class="text-3xl mb-2">📋</div>
                <div class="font-bold text-green-700 text-sm mb-1">IDP</div>
                <div class="text-xs text-slate-600">Individual Development Plan, target tahunan per pekerja</div>
              </div>
              <div class="card-hover bg-white border-t-4 border-blue-500 rounded-lg p-5 shadow-sm text-center">
                <div class="text-3xl mb-2">📚</div>
                <div class="font-bold text-blue-700 text-sm mb-1">Training Records</div>
                <div class="text-xs text-slate-600">Riwayat training internal/eksternal + sertifikat upload</div>
              </div>
            </div>
            <div class="bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-sm text-slate-700 dark:text-slate-200">
              <strong>Role yang akses:</strong> HC · Coach · Sr Supervisor · Section Head · Coachee
            </div>
          </div>
        </section>

        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Big Menu 2 / 3"`. Expected 1 match. Run Grep: `pattern: x-show="current === 4"`. Expected 1 match.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): insert slide 4 — BigMenu CDP

3 sub-modul: Coaching Proton, IDP, Training Records.
Role akses: HC/Coach/SrSpv/SH/Coachee."
```

---

## Task 5: Insert Slide 5 — BigMenu BP (HRBP, For Future)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — insert antara slide 4 (CDP, baru) dan slide 6 (Role).

- [ ] **Step 1: Insert slide 5 block sebelum `<!-- Slide 6: Role`**

```
Find:
        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

Replace with:

```html
        <!-- Slide 5: BigMenu BP (HRBP, For Future) -->
        <section class="slide" x-show="current === 5" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full" style="opacity: 0.85;">
            <div class="flex items-center gap-3 mb-2">
              <div class="text-sm uppercase tracking-wide brand-red font-bold">Big Menu 3 / 3</div>
              <span class="px-3 py-1 bg-amber-100 text-amber-800 text-xs font-bold rounded-full uppercase tracking-wide">🚧 Coming Soon</span>
            </div>
            <h2 class="text-4xl font-bold brand-navy mb-2">BP <span class="text-2xl font-normal text-slate-500">— Business Partner</span></h2>
            <p class="text-slate-600 mb-6">Modul <strong>HRBP</strong> (Human Resources Business Partner) — strategic partner antara HC &amp; unit operasional untuk workforce planning, employee relations, &amp; advisory.</p>
            <div class="grid grid-cols-3 gap-4 mb-6">
              <div class="bg-slate-100 border border-dashed border-slate-300 rounded-lg p-5 text-center" style="filter: grayscale(0.4);">
                <div class="text-3xl mb-2">🤝</div>
                <div class="font-bold text-slate-600 text-sm mb-1">Workforce Planning</div>
                <div class="text-xs text-slate-500">Perencanaan SDM unit operasional</div>
                <div class="mt-2 text-xs text-amber-700">🚧 In Roadmap</div>
              </div>
              <div class="bg-slate-100 border border-dashed border-slate-300 rounded-lg p-5 text-center" style="filter: grayscale(0.4);">
                <div class="text-3xl mb-2">👁️</div>
                <div class="font-bold text-slate-600 text-sm mb-1">Employee Relations</div>
                <div class="text-xs text-slate-500">Manajemen hubungan pekerja</div>
                <div class="mt-2 text-xs text-amber-700">🚧 In Roadmap</div>
              </div>
              <div class="bg-slate-100 border border-dashed border-slate-300 rounded-lg p-5 text-center" style="filter: grayscale(0.4);">
                <div class="text-3xl mb-2">💡</div>
                <div class="font-bold text-slate-600 text-sm mb-1">Strategic Advisory</div>
                <div class="text-xs text-slate-500">Konsultasi HC untuk leadership unit</div>
                <div class="mt-2 text-xs text-amber-700">🚧 In Roadmap</div>
              </div>
            </div>
            <div class="bg-amber-50 border-l-4 border-amber-500 p-3 rounded text-sm text-amber-900">
              <strong>Status:</strong> In Roadmap — definisi &amp; implementasi menyusul. Slot ini akan diisi modul HRBP pada milestone berikutnya.
            </div>
          </div>
        </section>

        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Big Menu 3 / 3"`. Expected 1 match. Run Grep: `pattern: x-show="current === 5"`. Expected 1 match.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): insert slide 5 — BigMenu BP (HRBP, For Future)

Coming Soon badge + 3 placeholder modul (Workforce/Employee/Advisory).
Style muted dengan opacity & grayscale untuk indikasi in-roadmap."
```

---

## Task 6: Rewrite Slide 6 — Role Piramida 6 Tier

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 6 section (was slide 3).

- [ ] **Step 1: Replace seluruh isi slide 6**

```
Find:
        <!-- Slide 6: Role (will rewrite to piramida in Task 8) -->
        <section class="slide" x-show="current === 6" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-6xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Struktur Role di Sistem</div>
            <h2 class="text-4xl font-bold brand-navy mb-8">Role Pengguna</h2>
            <div class="grid grid-cols-5 gap-3">
```

Replace with:

```html
        <!-- Slide 6: Role Piramida 6 Tier -->
        <section class="slide" x-show="current === 6" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Struktur Role di Sistem</div>
            <h2 class="text-3xl font-bold brand-navy mb-4">Role Pengguna <span class="text-base font-normal text-slate-500">(10 role · 6 level hierarki)</span></h2>
            <div class="space-y-2">
              <!-- Level 1 - Admin -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #002e6d22, transparent);">
                <div class="text-xs font-bold text-white brand-navy bg-brand-navy rounded px-2 py-1 w-16 text-center" style="background: var(--navy);">L1</div>
                <div class="flex-1 flex justify-center gap-2">
                  <span class="px-3 py-1 bg-white border-2 border-brand-navy rounded-full text-sm font-bold brand-navy" title="Administrator sistem — akses seluruh fitur">🛡️ Admin</span>
                </div>
              </div>
              <!-- Level 2 - HC -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #002e6d1e, transparent);">
                <div class="text-xs font-bold text-white rounded px-2 py-1 w-16 text-center" style="background: var(--navy);">L2</div>
                <div class="flex-1 flex justify-center gap-2">
                  <span class="px-3 py-1 bg-white border-2 border-brand-navy rounded-full text-sm font-bold brand-navy" title="Tim Human Capital — kelola data, monitoring, coaching review, sertifikasi">👥 HC</span>
                </div>
              </div>
              <!-- Level 3 - Management -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #3b82f622, transparent);">
                <div class="text-xs font-bold text-white rounded px-2 py-1 w-16 text-center" style="background: #3b82f6;">L3</div>
                <div class="flex-1 flex justify-center gap-2 flex-wrap">
                  <span class="px-3 py-1 bg-white border-2 border-blue-500 rounded-full text-sm font-medium text-blue-700" title="Direktur — view-only level direksi">👔 Direktur</span>
                  <span class="px-3 py-1 bg-white border-2 border-blue-500 rounded-full text-sm font-medium text-blue-700" title="Vice President">📌 VP</span>
                  <span class="px-3 py-1 bg-white border-2 border-blue-500 rounded-full text-sm font-medium text-blue-700" title="Manager unit">🧭 Manager</span>
                </div>
              </div>
              <!-- Level 4 - Section Supervisory -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #0ea5e922, transparent);">
                <div class="text-xs font-bold text-white rounded px-2 py-1 w-16 text-center" style="background: #0ea5e9;">L4</div>
                <div class="flex-1 flex justify-center gap-2 flex-wrap">
                  <span class="px-3 py-1 bg-white border-2 border-sky-500 rounded-full text-sm font-medium text-sky-700" title="Section Head — approval bertingkat coaching">🏢 Section Head</span>
                  <span class="px-3 py-1 bg-white border-2 border-sky-500 rounded-full text-sm font-medium text-sky-700" title="Senior Supervisor — approval evidence deliverable">🧑‍💼 Sr Supervisor</span>
                </div>
              </div>
              <!-- Level 5 - Coaching -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #f59e0b22, transparent);">
                <div class="text-xs font-bold text-white rounded px-2 py-1 w-16 text-center" style="background: #f59e0b;">L5</div>
                <div class="flex-1 flex justify-center gap-2 flex-wrap">
                  <span class="px-3 py-1 bg-white border-2 border-amber-500 rounded-full text-sm font-medium text-amber-700" title="Coach — bimbing coachee, review deliverable, log coaching session">🎓 Coach</span>
                  <span class="px-3 py-1 bg-white border-2 border-amber-500 rounded-full text-sm font-medium text-amber-700" title="Supervisor — akses sama Coach tanpa coachee mapping">👤 Supervisor</span>
                </div>
              </div>
              <!-- Level 6 - Operational -->
              <div class="flex items-center gap-3 rounded-lg p-2" style="background: linear-gradient(90deg, #10b98122, transparent);">
                <div class="text-xs font-bold text-white rounded px-2 py-1 w-16 text-center" style="background: #10b981;">L6</div>
                <div class="flex-1 flex justify-center gap-2">
                  <span class="px-3 py-1 bg-white border-2 border-green-500 rounded-full text-sm font-bold text-green-700" title="Coachee — pekerja peserta program, assessment & submit deliverable">👨‍🎓 Coachee</span>
                </div>
              </div>
            </div>
            <div class="mt-4 bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-xs text-slate-700 dark:text-slate-200">
              <strong>Catatan:</strong> Hover tiap role untuk detail akses. Hierarki sesuai <code>Models/UserRoles.cs</code> — Admin (L1) akses penuh, Coachee (L6) akses operasional pekerja.
            </div>
          </div>
        </section>
```

**Note penting:** Edit ini harus juga menghapus 5 card grid existing + catatan kaki existing. Karena placeholder `Find` di atas hanya match awal block, gunakan strategi: read line range 235-281 dulu (existing slide 3 / sekarang slide 6), salin seluruh content, lalu Edit dengan old_string = seluruh slide content, new_string = block baru di atas.

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Role Piramida 6 Tier"`. Expected 1 match. Run Grep: `pattern: "border-t-4 border-amber-500 rounded-lg p-4 shadow-sm"`. Expected harus turun 1 dari sebelum (card existing role yang amber dihapus).

Run Grep: `pattern: "Direktur, VP, Manager, Section Head, Sr Supervisor"`. Expected 0 matches (catatan kaki existing dihapus karena sekarang semua role tampil).

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): rewrite slide 6 — role piramida 6 tier

10 role tampil (was 5+catatan), grouped per level L1-L6.
Tooltip per role chip. Color gradient navy→green per tier."
```

---

## Task 7: Insert Slide 8 — Pre/Post Test

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — insert antara slide 7 (Sistem Assessment) dan slide 9 (Alur OJT).

- [ ] **Step 1: Insert slide 8 block sebelum `<!-- Slide 9: Alur Assessment OJT`**

```
Find:
        <!-- Slide 9: Alur Assessment OJT (will fix in Task 9) -->
```

Replace with:

```html
        <!-- Slide 8: Pre/Post Test -->
        <section class="slide" x-show="current === 8" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Bagian 1 · CMP</div>
            <h2 class="text-3xl font-bold brand-navy mb-2">Pre &amp; Post Test <span class="text-base font-normal text-slate-500">— Ukur Efektivitas Training</span></h2>
            <p class="text-slate-600 mb-6">Pasangan ujian sebelum &amp; sesudah training untuk menghitung peningkatan kompetensi (gain score).</p>

            <!-- Flow 4 step horizontal -->
            <div class="grid grid-cols-9 gap-2 items-stretch mb-6">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📋</div>
                <div class="font-bold brand-navy text-sm mb-1">Pre Test</div>
                <div class="text-xs text-slate-600">Sebelum training, ukur baseline kompetensi peserta</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">🎓</div>
                <div class="font-bold brand-navy text-sm mb-1">Training / OJT</div>
                <div class="text-xs text-slate-600">Sesi pembelajaran (in-class atau on-the-job)</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">✅</div>
                <div class="font-bold brand-navy text-sm mb-1">Post Test</div>
                <div class="text-xs text-slate-600">Setelah training, ujian dengan paket soal sejenis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-1 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">4</div>
                <div class="text-2xl mb-1">📊</div>
                <div class="font-bold text-sm" style="color: var(--green)">Gain Score</div>
                <div class="text-xs text-slate-600">Analisis</div>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div class="bg-blue-50 border-l-4 border-blue-500 p-4 rounded text-sm">
                <div class="font-bold text-blue-700 mb-1">📈 Gain Score</div>
                <div class="text-slate-700">Selisih skor Post - Pre. Indikator efektivitas training per peserta &amp; per kategori.</div>
              </div>
              <div class="bg-amber-50 border-l-4 border-amber-500 p-4 rounded text-sm">
                <div class="font-bold text-amber-700 mb-1">🔍 Item Analysis</div>
                <div class="text-slate-700">Per-soal: kesulitan, daya beda, distractor power. Bantu HC perbaiki paket soal.</div>
              </div>
            </div>
          </div>
        </section>

        <!-- Slide 9: Alur Assessment OJT (will fix in Task 9) -->
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Pre &amp; Post Test"`. Expected 1 match. Run Grep: `pattern: x-show="current === 8"`. Expected 1 match.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): insert slide 8 — Pre/Post Test

4-step flow horizontal: Pre Test → Training → Post Test → Gain Score.
Highlight box: Gain Score + Item Analysis (v14.0 feature)."
```

---

## Task 8: Fix Slide 9 — Alur Assessment OJT (7-step + Arrow)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — replace seluruh isi slide 9.

- [ ] **Step 1: Replace slide 9 (existing 8-step) dengan 7-step versi baru**

Read line range slide 9 (was slide 5), salin seluruh `<section ... x-show="current === 9"...>...</section>` block. Edit dengan:

```
Find: seluruh content slide 9 dari `<!-- Slide 9: Alur Assessment OJT (will fix in Task 9) -->` sampai `</section>` terdekat (sebelum slide 10)
```

Replace with:

```html
        <!-- Slide 9: Alur Assessment OJT -->
        <section class="slide" x-show="current === 9" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-6xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Bagian 1 · Alur</div>
            <h2 class="text-3xl font-bold brand-navy mb-1">Alur Assessment OJT</h2>
            <p class="text-sm text-slate-500 mb-5">Persiapan → Pelaksanaan → Penilaian (7 step end-to-end)</p>

            <!-- Row 1: Step 1-4 with arrows -->
            <div class="grid grid-cols-9 gap-2 items-stretch mb-3">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📁</div>
                <div class="font-bold brand-navy mb-1 text-sm">Persiapan Data</div>
                <div class="text-xs text-slate-600">Kategori per unit, organisasi, daftar pekerja</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">📝</div>
                <div class="font-bold brand-navy mb-1 text-sm">Buat Assessment</div>
                <div class="text-xs text-slate-600">Pilih kategori, set durasi &amp; soal</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">💻</div>
                <div class="font-bold brand-navy mb-1 text-sm">Peserta Ujian</div>
                <div class="text-xs text-slate-600">Login portal, <strong>sistem random soal otomatis</strong>, kerjakan timer</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-1 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">👁️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Monitoring</div>
                <div class="text-xs text-slate-600">Pantau real-time</div>
              </div>
            </div>

            <!-- Connector arrow turun -->
            <div class="text-center text-2xl text-slate-400 my-1">↓</div>

            <!-- Row 2: Step 5-7 with arrows -->
            <div class="grid grid-cols-9 gap-2 items-stretch mt-1">
              <div class="col-span-3 flow-step">
                <div class="step-num">5</div>
                <div class="text-2xl mb-1">📤</div>
                <div class="font-bold brand-navy mb-1 text-sm">Submit Ujian</div>
                <div class="text-xs text-slate-600">Manual atau auto-submit saat timer habis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">6</div>
                <div class="text-2xl mb-1">⚙️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Penilaian Otomatis</div>
                <div class="text-xs text-slate-600">Skor vs passing grade otomatis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">7</div>
                <div class="text-2xl mb-1">🏆</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Hasil &amp; Laporan</div>
                <div class="text-xs text-slate-600">Sertifikasi + rekap per unit/kategori</div>
              </div>
            </div>

            <div class="mt-5 bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-xs text-slate-700 dark:text-slate-200 flex items-center gap-4">
              <div><strong>Role:</strong> Admin / HC siapkan data, Tim Operasional jadi peserta.</div>
              <div class="text-slate-300">|</div>
              <div><strong>Output:</strong> skor pekerja, status kelulusan, rekap per unit.</div>
            </div>
          </div>
        </section>
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Distribusi Soal"`. Expected 0 matches in this file (distribusi sudah merged ke "Peserta Ujian"). Run Grep: `pattern: "(7 step end-to-end)"`. Expected 1 match. Run Grep di slide 9 region: `pattern: "step-num">[1-7]<"`. Expected 7 matches (step 1-7 saja, bukan 8).

- [ ] **Step 3: Commit**

```bash
git commit -am "fix(sosialisasi-v2): slide 9 alur OJT — 7-step + arrow snake-flow

- Merge step 3 'Distribusi Soal' ke 'Peserta Ujian' (sistem random soal otomatis runtime)
- Arrow → antar card horizontal + ↓ antar row
- Rename 'Alur Assessment' → 'Alur Assessment OJT' (Proton terpisah di slide 11/12)"
```

---

## Task 9: Insert Slide 14 — IDP & Training Records

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — insert antara slide 13 (Coaching CDP Overview) dan slide 15 (Hierarki Kompetensi).

- [ ] **Step 1: Insert slide 14 block sebelum `<!-- Slide 15: Hierarki Kompetensi`**

```
Find:
        <!-- Slide 15: Hierarki Kompetensi -->
```

Replace with:

```html
        <!-- Slide 14: IDP & Training Records -->
        <section class="slide" x-show="current === 14" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-5xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Bagian 3 · CDP</div>
            <h2 class="text-3xl font-bold brand-navy mb-2">IDP &amp; Training Records</h2>
            <p class="text-slate-600 mb-6">Dua komponen pelengkap Coaching Proton di CDP — perencanaan pengembangan &amp; arsip pelatihan.</p>

            <div class="grid grid-cols-2 gap-5">
              <!-- IDP card -->
              <div class="bg-white border-t-4 border-green-600 rounded-lg p-5 shadow-sm">
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

              <!-- Training Records card -->
              <div class="bg-white border-t-4 border-blue-500 rounded-lg p-5 shadow-sm">
                <div class="flex items-center gap-2 mb-3">
                  <div class="text-3xl">📚</div>
                  <div>
                    <h3 class="text-xl font-bold text-blue-700">Training Records</h3>
                    <div class="text-xs uppercase text-slate-500">Riwayat Pelatihan</div>
                  </div>
                </div>
                <ul class="text-sm text-slate-700 space-y-2">
                  <li>• Training internal &amp; eksternal</li>
                  <li>• Kategori + sub-kategori</li>
                  <li>• Sertifikat upload (PDF/image)</li>
                  <li>• Validity period &amp; renewal</li>
                </ul>
                <div class="mt-3 text-xs text-slate-500"><strong>Akses:</strong> HC kelola data master, Coachee submit record.</div>
              </div>
            </div>

            <div class="mt-5 bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-sm text-slate-700 dark:text-slate-200">
              <strong>Catatan:</strong> IDP &amp; Training Records terintegrasi dengan profile pekerja — jadi referensi gap analysis &amp; promosi.
            </div>
          </div>
        </section>

        <!-- Slide 15: Hierarki Kompetensi -->
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Individual Development Plan"`. Expected 1 match (di slide 14 baru). Run Grep: `pattern: x-show="current === 14"`. Expected 1 match.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): insert slide 14 — IDP & Training Records

2-col layout: IDP card (green) + Training Records card (blue).
Akses notes per komponen."
```

---

## Task 10: Fix Slide 17 — Coaching Th 1-2 (Review Multi-Role + Arrow)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 17 section.

- [ ] **Step 1: Tambah arrow → antar card di Row 1 dan Row 2 + fix step 5 label**

Replace entire slide 17 (was slide 12) section. Find dari `<!-- Slide 17: Alur Coaching Th 1-2 (will fix in Task 10) -->` sampai `</section>` slide 17, replace dengan:

```html
        <!-- Slide 17: Alur Coaching Th 1-2 -->
        <section class="slide" x-show="current === 17" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-6xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Coaching · Tahun 1 &amp; 2</div>
            <h2 class="text-3xl font-bold brand-navy mb-1">Alur Coaching — Tahun 1 &amp; 2</h2>
            <p class="text-sm text-slate-500 mb-5">Persiapan Silabus → Review Multi-Role → Sertifikasi (8 step end-to-end)</p>

            <!-- Row 1: Step 1-4 with arrows -->
            <div class="grid grid-cols-11 gap-2 items-stretch mb-3">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📋</div>
                <div class="font-bold brand-navy mb-1 text-sm">Siapkan Silabus</div>
                <div class="text-xs text-slate-600">Kompetensi &amp; deliverable per track tahun</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">📤</div>
                <div class="font-bold brand-navy mb-1 text-sm">Upload Guidance</div>
                <div class="text-xs text-slate-600">Dokumen panduan belajar per kompetensi</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">🔗</div>
                <div class="font-bold brand-navy mb-1 text-sm">Mapping Track</div>
                <div class="text-xs text-slate-600">Assign coachee ke track tahun program</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">✍️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Kerjakan Deliverable</div>
                <div class="text-xs text-slate-600">Coachee submit evidence per deliverable</div>
              </div>
            </div>

            <div class="text-center text-2xl text-slate-400 my-1">↓</div>

            <!-- Row 2: Step 5-8 with arrows -->
            <div class="grid grid-cols-11 gap-2 items-stretch mt-1">
              <div class="col-span-2 flow-step" style="border-color: #0ea5e9; background: #f0f9ff;">
                <div class="step-num" style="background: #0ea5e9;">5</div>
                <div class="text-2xl mb-1">👀</div>
                <div class="font-bold mb-1 text-sm" style="color: #0369a1">Review Multi-Role</div>
                <div class="text-xs text-slate-600">Coach + SrSpv + SH + HC review <strong>paralel</strong> per-role</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">6</div>
                <div class="text-2xl mb-1">✅</div>
                <div class="font-bold brand-navy mb-1 text-sm">Approval / Revisi</div>
                <div class="text-xs text-slate-600">Approve atau request revisi dgn komentar</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">7</div>
                <div class="text-2xl mb-1">📊</div>
                <div class="font-bold brand-navy mb-1 text-sm">Hitung Progress</div>
                <div class="text-xs text-slate-600">% penyelesaian deliverable dalam track</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">8</div>
                <div class="text-2xl mb-1">🏅</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Sertifikasi</div>
                <div class="text-xs text-slate-600">Lulus tahun, naik ke tahun berikutnya</div>
              </div>
            </div>

            <div class="mt-5 bg-slate-50 dark:bg-slate-700 rounded-lg p-3 text-xs text-slate-700 dark:text-slate-200 flex items-center gap-4">
              <div><strong>Role:</strong> HC kelola silabus + guidance. Coachee submit. Reviewer (Coach/SrSpv/SH/HC) independent per-role (Phase 65 architecture).</div>
              <div class="text-slate-300">|</div>
              <div class="text-green-700"><strong>✅ Output:</strong> sertifikat tahun + eligible naik tahun berikutnya.</div>
            </div>
          </div>
        </section>
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Review Multi-Role"`. Expected 1 match. Run Grep: `pattern: "Review HC"`. Expected ≤0 atau hanya di footer string yang akurat (HC sebagai salah satu reviewer, bukan only). Confirm step 5 label berubah.

- [ ] **Step 3: Commit**

```bash
git commit -am "fix(sosialisasi-v2): slide 17 — review HC → review multi-role paralel

- Step 5: 'Review HC' → 'Review Multi-Role' (Coach+SrSpv+SH+HC independent per-role, Phase 65)
- Arrow → antar card horizontal di tiap row
- Footer note diperjelas: independent per-role bukan cascade"
```

---

## Task 11: Fix Slide 18 — Coaching Th 3 (Coaching Intensif + Arrow)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 18 section.

- [ ] **Step 1: Replace slide 18 (interview online → coaching intensif + tambah arrow)**

Find seluruh isi `<!-- Slide 18: Alur Coaching Th 3 (will fix in Task 11) -->` sampai `</section>`, replace dengan:

```html
        <!-- Slide 18: Alur Coaching Th 3 -->
        <section class="slide" x-show="current === 18" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-6xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Coaching · Tahun 3 (Level Mahir)</div>
            <h2 class="text-3xl font-bold brand-navy mb-1">Alur Coaching — Tahun 3</h2>
            <p class="text-sm text-slate-500 mb-5">Silabus Mahir → Coaching Intensif → Sertifikasi Final (8 step)</p>

            <!-- Row 1: Step 1-4 (red theme — preparation & deliverable) -->
            <div class="grid grid-cols-11 gap-2 items-stretch mb-3">
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">1</div>
                <div class="text-2xl mb-1">🎓</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Silabus Mahir</div>
                <div class="text-xs text-slate-600">Level mahir, beda dari Th 1-2</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">2</div>
                <div class="text-2xl mb-1">🔗</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Mapping Th 3</div>
                <div class="text-xs text-slate-600">Coachee lulus Th 2 → Track Tahun 3</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">3</div>
                <div class="text-2xl mb-1">✍️</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Kerjakan Deliverable</div>
                <div class="text-xs text-slate-600">Submit evidence level mahir</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">4</div>
                <div class="text-2xl mb-1">🔍</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Review Mendalam</div>
                <div class="text-xs text-slate-600">HC review lebih ketat</div>
              </div>
            </div>

            <div class="text-center text-2xl text-slate-400 my-1">↓</div>

            <!-- Row 2: Step 5-8 (red→green progression untuk sertifikasi final) -->
            <div class="grid grid-cols-11 gap-2 items-stretch mt-1">
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">5</div>
                <div class="text-2xl mb-1">🗣️</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Coaching Intensif</div>
                <div class="text-xs text-slate-600">Sesi coaching mendalam per deliverable, dicatat di CoachingSession log</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--red); background: #fef2f2;">
                <div class="step-num" style="background: var(--red);">6</div>
                <div class="text-2xl mb-1">📊</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--red)">Hitung Progress</div>
                <div class="text-xs text-slate-600">% deliverable + skor review</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">7</div>
                <div class="text-2xl mb-1">🏆</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Sertifikasi Final</div>
                <div class="text-xs text-slate-600">Semua deliverable Th 3 selesai</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">8</div>
                <div class="text-2xl mb-1">⭐</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Penetapan Level</div>
                <div class="text-xs text-slate-600">Tetapkan level kompetensi pekerja</div>
              </div>
            </div>

            <div class="mt-5 bg-gradient-to-r from-red-50 to-green-50 dark:from-slate-700 dark:to-slate-700 rounded-lg p-3 text-xs text-slate-700 dark:text-slate-200 flex items-center gap-4 border-l-4 border-green-500">
              <div><strong class="text-red-700">🎯 Tahun 3 = Mahir.</strong></div>
              <div class="text-slate-300">|</div>
              <div><strong class="text-green-700">🏆 Output:</strong> Pekerja kompeten penuh, sertifikasi final, eligible role advance.</div>
            </div>
          </div>
        </section>
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "Coaching Intensif"`. Expected 1 match. Run Grep: `pattern: "Interview Online"`. Expected 0 matches.

- [ ] **Step 3: Commit**

```bash
git commit -am "fix(sosialisasi-v2): slide 18 — interview online → coaching intensif

- Step 5: 'Interview Online' → 'Coaching Intensif' (sesuai CoachingSession model, generic per deliverable)
- Arrow → antar card horizontal di tiap row
- Sub-text: dicatat di CoachingSession log per session"
```

---

## Task 12: Update Alpine State + Slide Counter

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — line ~900 Alpine `deck()` function.

- [ ] **Step 1: Ubah `total: 15` → `total: 20`**

```
Find:
        current: 1,
        total: 15,
```

Replace with:

```
        current: 1,
        total: 20,
```

- [ ] **Step 2: Verify**

Run Grep: `pattern: "total: 20"`. Expected 1 match. Run Grep: `pattern: "total: 15"`. Expected 0 matches.

- [ ] **Step 3: Verify slide count file utuh**

Run Grep: `pattern: <!-- Slide \d+:`. Expected 20 matches (slide 1 sampai slide 20, sequential).

Run Grep: `pattern: x-show="current === \d+"`. Expected 20 matches.

Periksa daftar slide:

```bash
grep -E "<!-- Slide [0-9]+:" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html | sed 's/.*<!--/<!--/'
```

Expected output (20 baris):
```
<!-- Slide 1: Cover -->
<!-- Slide 2: Definisi -->
<!-- Slide 3: BigMenu CMP -->
<!-- Slide 4: BigMenu CDP -->
<!-- Slide 5: BigMenu BP (HRBP, For Future) -->
<!-- Slide 6: Role Piramida 6 Tier -->
<!-- Slide 7: Sistem Assessment CMP -->
<!-- Slide 8: Pre/Post Test -->
<!-- Slide 9: Alur Assessment OJT -->
<!-- Slide 10: Assessment Proton -->
<!-- Slide 11: Alur Proton Th 1-2 -->
<!-- Slide 12: Alur Proton Th 3 -->
<!-- Slide 13: Coaching CDP Overview -->
<!-- Slide 14: IDP & Training Records -->
<!-- Slide 15: Hierarki Kompetensi -->
<!-- Slide 16: Fokus Kompetensi (Table 4x5) -->
<!-- Slide 17: Alur Coaching Th 1-2 -->
<!-- Slide 18: Alur Coaching Th 3 -->
<!-- Slide 19: Timeline Summary -->
<!-- Slide 20: Closing -->
```

- [ ] **Step 4: Commit**

```bash
git commit -am "chore(sosialisasi-v2): update Alpine total 15 → 20 + verify slide numbering

Slide counter, dot indicators, end-key navigation otomatis follow total
karena pakai x-for=\"n in total\"."
```

---

## Task 13: Visual QA + Tag

**Files:**
- Verify only.

- [ ] **Step 1: Buka file di browser**

```bash
# Windows
start sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
# atau
explorer.exe sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

- [ ] **Step 2: Klik through semua 20 slide pakai keyboard arrow / tombol Next**

Periksa:
- Slide counter `1/20` … `20/20` (bukan `1/15`)
- 20 dot indicator di nav bar
- Slide 2 menampilkan card Terpusat/Terstandar/Terukur (bukan Assessment/Coaching/Sertifikasi lagi)
- Slide 3 (CMP), Slide 4 (CDP), Slide 5 (BP) tampil dengan badge & sub-modul masing-masing
- Slide 5 (BP) muted style + "Coming Soon" badge
- Slide 6 (Role) menampilkan piramida 6 tier dengan semua 10 role (Admin, HC, Direktur, VP, Manager, SectionHead, SrSupervisor, Coach, Supervisor, Coachee)
- Slide 8 (Pre/Post Test) 4-step flow dengan arrow →
- Slide 9 (Alur OJT) 7-step (bukan 8), arrow → antar card + ↓ antar row
- Slide 14 (IDP/Training Records) 2-kolom card
- Slide 17 (Coaching Th 1-2) step 5 = "Review Multi-Role", arrow → antar card
- Slide 18 (Coaching Th 3) step 5 = "Coaching Intensif" (bukan "Interview Online"), arrow → antar card

- [ ] **Step 3: Tes navigation**

- Tombol End → ke slide 20
- Tombol Home → ke slide 1
- Klik dot indicator slide 5, 10, 15 → loncat ke slide tersebut
- F → toggle fullscreen
- Spacebar / Right → next slide

- [ ] **Step 4: Tes hash navigation**

URL: `sosialisasi-v2.html#slide-14` → harus load langsung ke slide 14 (IDP/Training Records).

- [ ] **Step 5: Tes print preview**

Ctrl+P di browser. Pastikan 20 slide tampil semua di print preview (`@media print` set `[x-show] { display: flex !important; }`).

- [ ] **Step 6: Tes dark mode**

Klik 🌓 di nav bar. Pastikan slide 2/3/4/5/6/8/14/17/18 tetap readable (background card, text color, gradient adjust).

- [ ] **Step 7: Final commit + tag**

Jika semua QA pass:

```bash
# Optional empty commit kalau ada QA-only changes (biasanya tidak ada)
git tag sosialisasi-v2.1 -m "Sosialisasi v2.1 — 20 slide (revisi cakupan 3 platform + audit alur)"
```

Output expected:
```
$ git log --oneline -15
<hash> chore(sosialisasi-v2): update Alpine total 15 → 20 + verify slide numbering
<hash> fix(sosialisasi-v2): slide 18 — interview online → coaching intensif
<hash> fix(sosialisasi-v2): slide 17 — review HC → review multi-role paralel
<hash> feat(sosialisasi-v2): insert slide 14 — IDP & Training Records
<hash> fix(sosialisasi-v2): slide 9 alur OJT — 7-step + arrow snake-flow
<hash> feat(sosialisasi-v2): insert slide 8 — Pre/Post Test
<hash> feat(sosialisasi-v2): rewrite slide 6 — role piramida 6 tier
<hash> feat(sosialisasi-v2): insert slide 5 — BigMenu BP (HRBP, For Future)
<hash> feat(sosialisasi-v2): insert slide 4 — BigMenu CDP
<hash> feat(sosialisasi-v2): insert slide 3 — BigMenu CMP
<hash> feat(sosialisasi-v2): rewrite slide 2 — definisi cakup 3 platform + 3 card value
<hash> refactor(sosialisasi-v2): renumber existing slides bottom-up
```

- [ ] **Step 8: Merge ke main (atau buka PR)**

User confirm merge strategy. Default:

```bash
git checkout main
git merge sosialisasi-v2-revisi --no-ff -m "merge: sosialisasi-v2 revisi → 20 slide"
git push origin main --tags
```

---

## Verification Summary

Setelah Task 13 selesai, file `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`:

- 20 slide section dengan `x-show="current === N"` sequential 1-20.
- `total: 20` di Alpine `deck()`.
- Slide 2 definisi cakup 3 platform + 3 card value (Terpusat/Terstandar/Terukur).
- 3 BigMenu slide (CMP, CDP, BP-ComingSoon) dengan sub-modul + role akses.
- Slide 6 Role piramida 6 tier (10 role tampil).
- Slide 8 Pre/Post Test flow.
- Slide 14 IDP & Training Records 2-kolom.
- Slide 9/17/18 alur dengan arrow → antar card + ↓ antar row.
- Step 5 slide 9 merge "Distribusi Soal" ke "Peserta Ujian" (7 step).
- Step 5 slide 17 "Review Multi-Role" (sesuai Phase 65 independent per-role).
- Step 5 slide 18 "Coaching Intensif" (sesuai CoachingSession model).
- Tag `sosialisasi-v2.1`.
