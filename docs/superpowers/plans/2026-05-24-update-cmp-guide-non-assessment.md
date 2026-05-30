# Update CMP Guide Non-Assessment Accordion — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite 6 existing CMP non-assessment accordion (Library KKJ, Alignment, Training Records, Records Tim, Pre-Post, Monitoring Compliance) untuk konsistensi step depth + HTML formatting dgn 6 accordion assessment yang shipped sebelumnya, tambah 1 accordion baru (Budget Training), fix 2 role bug existing (Acc-4 +Manager, Acc-6 -Manager).

**Architecture:** Single-file refactor di `Services/GuideContentProvider.cs` — preserve 6 existing IDs untuk bookmark/keyword compat, full rewrite Title/Steps/Keywords/Roles. Insert 1 baru `cmp-budget-training` setelah `cmp-monitoring-manager`. No DB, no controller, no view, no PDF change. Playwright test extend pakai pattern existing.

**Tech Stack:** .NET Core 8 (C# record `GuideItem`/`GuideStep`/`RoleGroup` enum), Playwright TypeScript untuk e2e.

**Spec reference:** `docs/superpowers/specs/2026-05-24-update-cmp-guide-non-assessment-design.md`

---

## File Structure

### Files Modified

| File | Change |
|------|--------|
| `Services/GuideContentProvider.cs` | 6 GuideItem rewrite (cmp-library-kkj → cmp-monitoring-manager) + 1 GuideItem baru (cmp-budget-training) + 2 role array fix |
| `tests/e2e/cmp-guide.spec.ts` | Fix assertion 5.3 (12→13), 5.4 (7→5); add test 5.9 |

### Files Not Touched

- DB schema — no migration
- Controllers, Models (non-Guide), Views — no UI/logic change
- PDF HTML files — out of scope
- CSS / JS

---

## Pre-flight

- [ ] **P1: Verify working tree state**

```bash
git status
```
Expected: docs/SEED_JOURNAL.md modified OK (Playwright teardown leftover, leave). No staged. No untracked source files in scope.

- [ ] **P2: Baseline build clean**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`. Note warning count (baseline 23). Refactor target: keep ≤23.

- [ ] **P3: Confirm latest origin/main pulled (kalau push diharapkan)**

```bash
git fetch origin && git log origin/main..HEAD --oneline | wc -l
```
Expected: N commits ahead (catat angka).

---

## Task 1: Rewrite 6 Accordion + Add Acc-7 + Fix 2 Role Bug

**Atomic commit:** semua perubahan provider di 1 commit karena cohesive content update.

**Files:**
- Modify: `Services/GuideContentProvider.cs:10-86` (replace 6 GuideItem entries)
- Modify: `Services/GuideContentProvider.cs:~87` (insert Acc-7 cmp-budget-training)

### Step 1.1: Locate existing 6 CMP GuideItem entries

Run grep to confirm line ranges:
```
Grep pattern: "Id: \"cmp-(library-kkj|mapping-kkj-cpdp|training-records|monitoring-records-tim|pre-post-test|monitoring-manager)\""
```
Expected: 6 hits at lines ~11, 23, 35, 47, 60, 74 (verify before editing).

### Step 1.2: Read 6 existing entries (must read before edit per Edit tool requirement)

```
Read tool: Services/GuideContentProvider.cs lines 10-90
```

### Step 1.3: Replace 6 entries dgn full rewrite

Find the block from `// ═══════════════════ CMP ═══════════════════` comment through `cmp-monitoring-manager` closing (line ~86) and replace with:

```csharp
        // ═══════════════════ CMP ═══════════════════
        new GuideItem(
            Id: "cmp-library-kkj",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Library KKJ (Kebutuhan Kompetensi Jabatan)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Dokumen KKJ",
                    "Buka menu <b>CMP</b> di navbar → klik card <b>Dokumen KKJ & Alignment KKJ/IDP</b>. Halaman muncul dgn 2 tab: <b>KKJ</b> (default) + <b>Alignment KKJ/IDP</b>."),
                new GuideStep(2, "Pahami Filter Role-Based",
                    "Sistem otomatis filter daftar bagian sesuai role Anda: <b>L1-L4</b> (Admin/HC/Manager/Atasan) lihat semua bagian; <b>L5-L6</b> (Coach/Coachee) lihat bagian sendiri saja (sesuai field <code>Section</code> di profil Anda)."),
                new GuideStep(3, "Browse Per Bagian",
                    "Daftar bagian terurut berdasarkan <code>DisplayOrder</code>. Klik baris bagian → expand → muncul daftar file KKJ yang tersedia untuk bagian tersebut."),
                new GuideStep(4, "Download File KKJ",
                    "Klik tombol <b>Download</b> di samping file → file ter-download (endpoint <code>/CMP/KkjFileDownload/:id</code>). Nama file menyimpan informasi versi."),
                new GuideStep(5, "Cari Versi Lama (Archived)",
                    "File yang sudah di-archive oleh AdminHC tidak tampil di list. Kalau perlu versi historis tertentu, koordinasi langsung dengan AdminHC.")
            },
            Keywords: new[] { "kkj", "library", "kebutuhan kompetensi jabatan", "download", "dokumen", "bagian", "section" }
        ),
        new GuideItem(
            Id: "cmp-mapping-kkj-cpdp",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Alignment KKJ ↔ IDP (Mapping Pengembangan)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Tab Alignment",
                    "Dari halaman <b>Dokumen KKJ & Alignment KKJ/IDP</b>, klik tab <b>Alignment KKJ/IDP</b> (di samping tab KKJ). URL bisa langsung: <code>/CMP/DokumenKkj?tab=alignment</code>."),
                new GuideStep(2, "Tujuan Alignment",
                    "Dokumen ini menyelaraskan <b>KKJ</b> (Kebutuhan Kompetensi Jabatan) dengan <b>IDP</b> (Individual Development Plan). Tujuannya: rencana pelatihan setiap pekerja sesuai kompetensi yang dibutuhkan jabatannya."),
                new GuideStep(3, "Browse Per Bagian",
                    "Pattern sama dgn tab KKJ — daftar bagian terurut. Klik bagian → expand → tampil daftar file CPDP (Competency Personal Development Plan) per bagian."),
                new GuideStep(4, "Download File CPDP",
                    "Klik tombol <b>Download</b> per file → endpoint <code>/CMP/CpdpFileDownload/:id</code>."),
                new GuideStep(5, "Refresh Frekuensi",
                    "Koordinasi AdminHC bila ada update KKJ — dokumen CPDP harus mengikuti supaya mapping tetap akurat. Versi lama akan di-archive otomatis.")
            },
            Keywords: new[] { "alignment", "mapping", "cpdp", "kkj", "idp", "individual development plan", "pengembangan" }
        ),
        new GuideItem(
            Id: "cmp-training-records",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Training Records (Capability Building Records)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Records",
                    "Buka menu <b>CMP</b> → klik card <b>Riwayat Pelatihan</b> → masuk halaman <b>Capability Building Records</b>."),
                new GuideStep(2, "Tab My Records (Default)",
                    "Tab pertama yang aktif. Menampilkan semua record peserta yang sedang login. Badge angka di tab = total record (Assessment Online + Training Manual)."),
                new GuideStep(3, "Pahami 2 Tipe Record",
                    "<b>Assessment Online</b>: record otomatis dari ujian sistem yang Anda kerjakan. <b>Training Manual</b>: record yang di-input oleh HC dari pelatihan eksternal/sertifikasi vendor (tidak melalui ujian online)."),
                new GuideStep(4, "Filter & Search",
                    "Tersedia filter: <b>Section</b>, <b>Unit</b>, <b>Kategori</b>, <b>Status</b>. Kolom search untuk cari berdasarkan judul atau nama penyelenggara."),
                new GuideStep(5, "Export Personal",
                    "Klik tombol <b>Export Excel</b> di sudut kanan → file Excel 2 sheet (Assessment + Training) ter-download untuk dokumentasi personal Anda.")
            },
            Keywords: new[] { "training records", "riwayat pelatihan", "capability building", "assessment online", "training manual", "export excel" }
        ),
        new GuideItem(
            Id: "cmp-monitoring-records-tim",
            Module: GuideModule.Cmp,
            Title: "Cara Monitoring Records Tim (Team View)",
            Roles: new[] { RoleGroup.Atasan, RoleGroup.Manager, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Team View",
                    "Buka halaman <b>Records</b> (CMP → Riwayat Pelatihan). Klik tab <b>Team View</b> di samping tab My Records. Tab ini hanya muncul untuk role level ≤4 (Atasan, Manager, AdminHC)."),
                new GuideStep(2, "Cakupan Tim",
                    "Tab menampilkan bawahan langsung Anda. Kalau Anda <b>Manager+</b>, sistem juga menampilkan bawahan tidak langsung mengikuti struktur organisasi (rekursif via <code>OrganizationUnit.ParentId</code>)."),
                new GuideStep(3, "Cascade Filter + Date Range",
                    "Filter cascade: pilih <b>Bagian</b> → <b>Unit</b> terkait muncul; pilih <b>Kategori</b> → <b>Sub-Kategori</b> muncul. Plus filter <b>Status</b> dan rentang tanggal <b>Date From</b> / <b>Date To</b>. Partial reload tabel via <code>RecordsTeamPartial</code> tanpa full page refresh."),
                new GuideStep(4, "Detail Worker",
                    "Klik tombol <b>Detail</b> di samping nama pekerja → halaman <code>/CMP/RecordsWorkerDetail?workerId=...</code>. Tampil riwayat lengkap (assessment + training) per individu."),
                new GuideStep(5, "Export Excel Team",
                    "Dua tombol export terpisah: <b>Export Assessment Team</b> (Excel khusus assessment) dan <b>Export Training Team</b> (Excel khusus training manual). Filter yang aktif di view ikut diteruskan ke export.")
            },
            Keywords: new[] { "monitoring", "team view", "records tim", "atasan", "manager", "cascade filter", "export team", "bawahan" }
        ),
        new GuideItem(
            Id: "cmp-pre-post-test",
            Module: GuideModule.Cmp,
            Title: "Cara Mengerjakan Pre-Post Test",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Assessment",
                    "Buka <b>CMP</b> → klik card <b>Assessment Saya</b>. Tampil daftar semua ujian yang ditujukan untuk Anda."),
                new GuideStep(2, "Pahami Pairing Pre-Post",
                    "Sistem otomatis memasangkan <b>Pre-Test</b> + <b>Post-Test</b> berdampingan dalam 1 card visual. Badge <b>Pre-Test</b> (warna info) dan badge <b>Post-Test</b> (warna primary) untuk identifikasi cepat."),
                new GuideStep(3, "Kerjakan Pre-Test",
                    "Klik tombol <b>Mulai Pre-Test</b> (kalau belum pernah mulai) atau <b>Lanjutkan Pre-Test</b> (kalau ada saved progress dari sesi sebelumnya). Wizard ujian akan terbuka dengan timer + soal."),
                new GuideStep(4, "Ikuti Pelatihan",
                    "Setelah Pre-Test selesai, ikuti program pelatihan sesuai jadwal HC. Post-Test belum aktif sampai pelatihan dianggap selesai oleh sistem."),
                new GuideStep(5, "Kerjakan Post-Test",
                    "Setelah pelatihan, tombol <b>Mulai Post-Test</b> muncul di card yang sama. Klik → ujian Post-Test. Sistem otomatis menghitung <b>Gain Score</b> = nilai Post − nilai Pre untuk lihat seberapa besar peningkatan Anda."),
                new GuideStep(6, "Lihat Hasil",
                    "Setelah Post-Test selesai, klik tombol <b>Lihat Hasil</b> → halaman <code>/CMP/Results/:id</code>. Tampil breakdown nilai per soal + analisa per elemen teknis (skor per kompetensi).")
            },
            Keywords: new[] { "pre-test", "post-test", "pretest", "posttest", "gain score", "pairing", "ujian pasangan", "improvement" }
        ),
        new GuideItem(
            Id: "cmp-monitoring-manager",
            Module: GuideModule.Cmp,
            Title: "Monitoring Compliance via Analytics Dashboard",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses & Pahami Summary",
                    "Buka <b>CMP</b> → klik card <b>Dasbor Analitik</b> (hanya visible untuk Admin/HC). Halaman menampilkan 4 KPI cards atas: <b>Total Sessions</b>, <b>Pass Rate</b>, <b>Expiring</b> (sertifikat akan expire), <b>Avg Gain Score</b>."),
                new GuideStep(2, "Tab Fail Rate",
                    "Tab pertama. Visualisasi persentase peserta yang <b>fail</b> per kategori/bagian. Klik bar/segment chart → drill-down ke daftar pekerja yang gagal di kategori tersebut."),
                new GuideStep(3, "Tab Trend",
                    "Chart line menunjukkan tren <b>pass rate</b> over time. Filter periode: bulan / quarter / tahun. Berguna untuk lihat dampak training program pada periode tertentu."),
                new GuideStep(4, "Tab Skor Elemen Teknis (ET)",
                    "Breakdown skor rata-rata per <b>elemen kompetensi teknis</b>. Identify area kompetensi yang lemah cross-unit — input untuk perencanaan training kolektif."),
                new GuideStep(5, "Tab Sertifikat Expired",
                    "Daftar pekerja yang sertifikatnya akan expire dalam rentang tertentu. Sort by tanggal expiry — data untuk plan perpanjangan sertifikat (renewal chain di Bagian Assessment Admin)."),
                new GuideStep(6, "Tab Item Analysis + Gain Score Report",
                    "Tab analisa lanjutan untuk Pre-Post pair. Pilih <b>Assessment Group</b> → <b>Item Analysis</b> (per-soal: persentase peserta jawab benar/salah, identify soal terlalu sulit/mudah) + <b>Gain Score Report</b> (per-peserta: improvement Pre→Post). Masing-masing punya tombol <b>Export Excel</b>.")
            },
            Keywords: new[] { "analytics dashboard", "monitoring", "compliance", "fail rate", "trend", "elemen teknis", "sertifikat expired", "item analysis", "gain score report" }
        ),
        new GuideItem(
            Id: "cmp-budget-training",
            Module: GuideModule.Cmp,
            Title: "Cara Kelola Budget Training & Assessment",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Budget Training",
                    "Buka <b>CMP</b> → klik card <b>Budget Training</b> (hanya visible untuk Admin/HC). Halaman dgn 2 tab: <b>Data Budget</b> (default) + <b>Ringkasan</b>."),
                new GuideStep(2, "Tab Data Budget",
                    "Tampil tabel semua budget item dgn filter: <b>Tahun</b>, <b>Type</b>, <b>Kategori</b>, dan <b>Search</b> by judul. Kolom sortable + pagination."),
                new GuideStep(3, "Tambah Budget Item",
                    "Klik tombol <b>Tambah</b> → form wizard 3 card: <b>Card 1</b> (Tahun Anggaran + Judul), <b>Card 2</b> (Kategori + SubKategori cascade + Jumlah Peserta), <b>Card 3</b> (Anggaran/<code>EstimasiBiayaTotal</code> + Realisasi Biaya + Vendor)."),
                new GuideStep(4, "Quick Update Realisasi",
                    "Inline edit nilai realisasi langsung di tabel tanpa buka form penuh. Endpoint <code>BudgetTrainingQuickUpdate</code> — perubahan tersimpan otomatis saat blur input."),
                new GuideStep(5, "Import Excel",
                    "Klik tombol <b>Import</b> → halaman import. <b>Download Template</b> Excel dulu → isi data → upload via <b>Choose File</b>. Sistem validate per baris dan tampilkan hasil <b>Success / Skip / Error</b> per row dengan pesan detail."),
                new GuideStep(6, "Export Excel & Tab Ringkasan",
                    "Tombol <b>Export Excel</b> di header → download filtered data ke Excel. Tab <b>Ringkasan</b> menampilkan grafik total anggaran vs realisasi per kategori/tahun untuk monitoring progress.")
            },
            Keywords: new[] { "budget training", "anggaran", "realisasi", "import excel", "export", "vendor", "kategori biaya", "estimasi biaya" }
        ),

        // ═══════════════════ CDP ═══════════════════
```

**Note:** Replace old block from start of `═ CMP ═` comment through closing `),` of `cmp-monitoring-manager`, including the blank line + `// ═══════════════════ CDP ═══════════════════` comment. The replacement above ends with `// ═══════════════════ CDP ═══════════════════` so the comment is preserved.

Verify after edit: 7 GuideItem dgn ID `cmp-*` di section sebelum CDP comment.

### Step 1.4: Build verification

```bash
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`. Warning count ≤23 (baseline). Kalau ada compile error, biasanya: missing koma, salah tipe RoleGroup, typo enum.

### Step 1.5: Static count verification

Run grep:
```
Pattern 1: 'Id: "cmp-' di Services/GuideContentProvider.cs → expected 13 hits (12 existing assessment-related sudah ada + 1 baru Budget Training)
Pattern 2: 'Id: "cmp-budget-training"' → expected 1 hit
Pattern 3: 'cmp-monitoring-records-tim' followed by Manager → confirm Acc-4 fix applied
Pattern 4: 'cmp-monitoring-manager' followed by Manager (di body Roles array) → expected 0 hit (Acc-6 fix applied)
```

Concrete grep commands (run via Grep tool):

```
Grep pattern "Id: \"cmp-" path "Services/GuideContentProvider.cs" output_mode count
```
Expected count: 13

### Step 1.6: Commit Task 1

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): rewrite 6 CMP non-assessment accordion + add Budget Training

Full rewrite 6 existing accordion (preserve ID untuk bookmark compat):
- cmp-library-kkj: 2 step -> 5 step (filter role-based, browse, download)
- cmp-mapping-kkj-cpdp: 2 step -> 5 step (rename "Cara Akses Alignment")
- cmp-training-records: 2 step -> 5 step (My Records, 2 tipe, filter, export)
- cmp-monitoring-records-tim: 3 step -> 5 step (cascade filter + date range,
  detail worker, export team)
- cmp-pre-post-test: 4 step -> 6 step (pairing, Mulai/Lanjutkan, Gain Score)
- cmp-monitoring-manager: 4 step -> 6 step (6 tab Analytics Dashboard)

NEW accordion:
- cmp-budget-training (AdminHC): 6 step covering Data Budget tab, form
  Create wizard, Quick Update, Import Excel, Export, tab Ringkasan

2 role bug fix:
- cmp-monitoring-records-tim: +RoleGroup.Manager
  (Records Team View visible level <=4 termasuk Manager Direktur VP)
- cmp-monitoring-manager: -RoleGroup.Manager
  (controller [Authorize(Roles="Admin,HC")] strict, Manager dapat 403)

Role visibility matrix:
- Admin/HC: 13 acc (semua + Acc-7 baru)
- Manager/Direktur/VP: 6 (5 role-All + Acc-4 fix)
- Atasan: 6 (5 role-All + Acc-4)
- Coach/Coachee: 5 (5 role-All)

No DB, no controller, no view, no PDF change.

Spec: docs/superpowers/specs/2026-05-24-update-cmp-guide-non-assessment-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Playwright Test Fix + Extend

**Files:**
- Modify: `tests/e2e/cmp-guide.spec.ts` (fix assertion 5.3, 5.4; add 5.9)

### Step 2.1: Read existing test file

```
Read tool: tests/e2e/cmp-guide.spec.ts
```
Confirm test 5.3 + 5.4 + structure pattern.

### Step 2.2: Fix test 5.3 assertion (12 → 13)

Edit:

Sebelum:
```typescript
  test('5.3 - Admin sees 12 accordion items on CMP page', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(12);
  });
```

Sesudah:
```typescript
  test('5.3 - Admin sees 13 accordion items on CMP page (post Budget Training)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(13);
  });
```

### Step 2.3: Fix test 5.4 assertion (7 → 5)

Sebelum:
```typescript
  test('5.4 - Coachee sees 7 accordion items on CMP page (1 new + 6 existing)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(7);

    // Acc-5 (Tipe Assessment, role All) MUST be visible to coachee
    await expect(page.locator('text=Tipe-tipe Assessment')).toBeVisible();

    // AdminHC-only accordion MUST NOT be visible
    await expect(page.locator('text=Fitur Khusus Admin')).not.toBeVisible();
    await expect(page.locator('text=Cara Manage Kategori Assessment')).not.toBeVisible();
  });
```

Sesudah:
```typescript
  test('5.4 - Coachee sees 5 accordion items on CMP page (5 role-All)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(5);

    // 5 role-All accordion MUST be visible
    await expect(page.locator('text=Library KKJ')).toBeVisible();
    await expect(page.locator('text=Alignment KKJ')).toBeVisible();
    await expect(page.locator('text=Training Records')).toBeVisible();
    await expect(page.locator('text=Pre-Post Test')).toBeVisible();
    await expect(page.locator('text=Tipe-tipe Assessment')).toBeVisible();

    // AdminHC-only accordion MUST NOT be visible
    await expect(page.locator('text=Fitur Khusus Admin')).not.toBeVisible();
    await expect(page.locator('text=Cara Manage Kategori Assessment')).not.toBeVisible();
    await expect(page.locator('text=Budget Training')).not.toBeVisible();
  });
```

### Step 2.4: Add test 5.9 NEW

Locate closing `});` of test 5.8 (just before `});` of `test.describe(...)`). Add test 5.9 BEFORE the describe closing:

Sebelum:
```typescript
  test('5.8 - Accordion Acc-5 expands to show 3 steps (Pre/Post/Regular)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const acc5 = page.locator('.accordion-item', { hasText: 'Tipe-tipe Assessment' });
    await acc5.locator('button.accordion-button').click();

    await page.waitForTimeout(500);

    const body = acc5.locator('.accordion-collapse.show');
    await expect(body).toContainText('Pre-Test');
    await expect(body).toContainText('Post-Test');
    await expect(body).toContainText('Regular Assessment');
  });

});
```

Sesudah:
```typescript
  test('5.8 - Accordion Acc-5 expands to show 3 steps (Pre/Post/Regular)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const acc5 = page.locator('.accordion-item', { hasText: 'Tipe-tipe Assessment' });
    await acc5.locator('button.accordion-button').click();

    await page.waitForTimeout(500);

    const body = acc5.locator('.accordion-collapse.show');
    await expect(body).toContainText('Pre-Test');
    await expect(body).toContainText('Post-Test');
    await expect(body).toContainText('Regular Assessment');
  });

  test('5.9 - Admin sees Budget Training accordion + 13 total', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(13);

    // Acc-7 Budget Training visible to admin
    const budgetAcc = page.locator('.accordion-item', { hasText: 'Budget Training' });
    await expect(budgetAcc).toBeVisible();

    // Expand and verify content
    await budgetAcc.locator('button.accordion-button').click();
    await page.waitForTimeout(500);
    const body = budgetAcc.locator('.accordion-collapse.show');
    await expect(body).toContainText('Data Budget');
    await expect(body).toContainText('Import Excel');
    await expect(body).toContainText('Ringkasan');
  });

});
```

### Step 2.5: Verify test file syntax

Run TypeScript compile check (Playwright auto-compiles, no separate run needed). Test by listing tests:

```bash
cd tests && npx playwright test cmp-guide.spec.ts --list
```
Expected: 9 tests listed (5.1 through 5.9). No syntax error.

### Step 2.6: Commit Task 2

```bash
git add tests/e2e/cmp-guide.spec.ts
git commit -m "$(cat <<'EOF'
test(e2e): fix cmp-guide assertion + add Budget Training test

Fix 2 pre-existing assertion bug + add 1 NEW test:

- 5.3 assertion 12 -> 13 (post Acc-7 Budget Training added)
- 5.4 assertion 7 -> 5 (was always wrong: Coachee aktual lihat 5
  role-All accordion, not 7. Test never validated due LDAP block
  pada env tanpa Pertamina domain access).
- 5.9 NEW: Admin sees Budget Training accordion + expand verify
  content (Data Budget, Import Excel, Ringkasan).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Verification (Post-Task)

### V1: Build clean

```bash
dotnet build
```
Expected: 0 error, ≤23 warning.

### V2: Static count

Use Grep tool:
| Pattern | Path | Expected |
|---------|------|----------|
| `Id: "cmp-` | `Services/GuideContentProvider.cs` | 13 (CMP module accordion total) |
| `Module: GuideModule\.Cmp` | `Services/GuideContentProvider.cs` | 16 (13 GuideItem + 2 GuidePdfLink + 1 module card line) |
| `RoleGroup\.Manager` (line dgn `cmp-monitoring-records-tim` di neighbor) | provider | should appear 1× di Acc-4 Roles |
| `RoleGroup\.Manager` (line dgn `cmp-monitoring-manager` di neighbor) | provider | should NOT appear (0×) |

### V3: Playwright run (optional, requires dev server + DB + LDAP access)

```bash
cd tests && npx playwright test cmp-guide.spec.ts --reporter=line
```

Expected on Bapak's environment (full LDAP access):
- 9 passed (5.1-5.9)

On env tanpa LDAP (server saya):
- 5.1, 5.3, 5.5, 5.6, 5.7, 5.9 pass (admin path)
- 5.2, 5.4, 5.8 blocked (coachee LDAP) — known limitation

### V4: Browser manual UAT

| Login | URL | Expected |
|-------|-----|----------|
| admin@pertamina.com | /Home/GuideDetail?module=cmp | 13 accordion termasuk "Cara Kelola Budget Training & Assessment" |
| Coachee (rino.prasetyo) | sda | 5 accordion: Library KKJ, Alignment, Training Records, Pre-Post, Tipe-tipe Assessment |
| Manager (manager@pertamina.com) | sda | 6 accordion (5 All + Records Tim post-fix) |
| Atasan (taufik.hartopo) | sda | 6 accordion (sama dgn Manager) |
| Coach (rustam.nugroho) | sda | 5 accordion (role-All only) |

### V5: Klik tiap accordion baru → expand verify

- Acc-7 Budget Training (admin) expand → 6 step render dgn HTML formatting `<b>`, `<i>`, `<code>` correct.
- 6 OLD accordion expand → step baru render dgn formatting consistent.

---

## Push Plan

Setelah Task 1 + Task 2 commit:
```bash
git push origin main
```

Notify IT untuk promo Dev:
- Commit hash: Task 1 + Task 2 commit hashes
- Migration: NONE
- Risk: rendah (content-only refactor + test fix)
- Test URL Dev: `http://10.55.3.3/KPB-PortalHC/Home/GuideDetail?module=cmp`

---

## Self-Review Checklist (post-write)

- [x] **Spec coverage:**
  - 7 accordion rewrite + new → Task 1 ✓
  - 2 role bug fix → Task 1 Step 1.3 ✓
  - Playwright test fix → Task 2 ✓
  - Verification approach → V1-V5 ✓

- [x] **Placeholder scan:** no TBD / TODO / "implement later" → clean

- [x] **Type consistency:**
  - `RoleGroup` enum used konsisten (`RoleGroup.All`, `RoleGroup.Manager`, `RoleGroup.Atasan`, `RoleGroup.AdminHC`) ✓
  - `GuideStep(int, string, string)` constructor matches existing pattern ✓
  - `GuideItem` named params (Id, Module, Title, Roles, Steps, Keywords) konsisten ✓

- [x] **Concrete code:** Step 1.3 punya full C# code untuk 7 GuideItem; Step 2.2-2.4 punya complete TS test code

- [x] **Verifikasi step:** tiap commit punya build + grep static check

- [x] **Atomic commit:** 2 commit logikal (provider content + test fix), tidak bercampur

---

## Plan Summary

| Task | Files | Verification | Commit |
|------|-------|--------------|--------|
| 1. Rewrite 6 + Acc-7 + 2 role fix | 1 file (provider) | dotnet build + grep static count | Commit 1 |
| 2. Playwright fix + 5.9 | 1 file (cmp-guide.spec.ts) | npx playwright test --list (syntax check) | Commit 2 |

**Total:** 2 files modified. 2 atomic commits. No new files. No migration. No controller/view/PDF change.
