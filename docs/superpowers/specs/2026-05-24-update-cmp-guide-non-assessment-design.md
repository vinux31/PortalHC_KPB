# Update CMP Guide — Non-Assessment Accordion Rewrite

**Status:** APPROVED — 5 Q locked + 3 section design + 5 miss fix lokal 2026-05-24. Siap invoke `writing-plans`.

**Trigger:** Update materi accordion bagian CMP yang sudah ada (6 accordion non-assessment), tambah 1 accordion baru untuk fitur Budget Training. Tujuan: konsistensi step depth dgn 6 accordion baru yang baru di-ship (Acc-5..10), surface fitur Budget Training yang belum tercover, fix 2 role bug existing.

**Predecessor:** [2026-05-23-update-cmp-guide-assessment-content-design.md](./2026-05-23-update-cmp-guide-assessment-content-design.md) shipped 2026-05-24 (commits `d636f155..ab212b8a`).

---

## Konteks

- Page CMP dirender dari `Views/Home/GuideDetail.cshtml` + data `Services/GuideContentProvider.cs`.
- Sekarang 12 accordion CMP: 6 OLD non-assessment (step depth 2-4) + 6 NEW assessment (step depth 3-6, just shipped).
- 6 OLD accordion step depth tidak konsisten dgn 6 NEW (full step + HTML formatting).
- 2 PDF CMP existing tidak diubah (sudah refresh kemarin).

---

## Decisions Locked

| # | Topik | Keputusan |
|---|-------|-----------|
| Q1 | Scope | **A** — Update existing 6 accordion non-assessment, no PDF baru |
| Q2 | Sumber konten | **A** — Reverse-engineer dari source code (CMPController + Views/CMP/*) |
| Q3 | Cakupan per accordion | **A** — Semua 6 termasuk light refresh #5 #6 |
| Q4 | Strategy edit | **B** — Full rewrite (preserve ID, ganti Title/Steps/Keywords) |
| Q5 | Role re-evaluation | **C** — Allow new accordion kalau fitur surface; **B** picked: Budget Training jadi Acc-7 baru |

**Role bug fix (added during verification):**
- **Acc-4** Records Tim: `Atasan + AdminHC` → `Atasan + Manager + AdminHC` (Records Team View visible level ≤4 termasuk Manager)
- **Acc-6** Monitoring Compliance: `Manager + AdminHC` → `AdminHC` (controller `[Authorize(Roles="Admin, HC")]` strict)

---

## Section 1 — Architecture

**File touch:** `Services/GuideContentProvider.cs` ONLY.

**Pattern:**
- Preserve 6 existing GuideItem IDs (`cmp-library-kkj`, `cmp-mapping-kkj-cpdp`, `cmp-training-records`, `cmp-monitoring-records-tim`, `cmp-pre-post-test`, `cmp-monitoring-manager`) — full rewrite Title/Steps/Keywords/Roles.
- Insert 1 baru `cmp-budget-training` setelah `cmp-monitoring-manager` (sebelum 6 accordion assessment yang sudah ada).
- Step target 4-6 per accordion (consistency dgn shipped Acc-5..10).
- HTML formatting `<b>`, `<i>`, `<code>` di body untuk visual parity.

**No DB. No migration. No CSS/JS. No PDF. No controller.**

---

## Section 2 — Step Content per Accordion

### Acc-1: `cmp-library-kkj` — Library KKJ — Role: All — 5 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Dokumen KKJ | Buka menu **CMP** → klik card "Dokumen KKJ & Alignment KKJ/IDP" → masuk halaman 2-tab, default tab KKJ |
| 2 | Filter Role-Based | L1-L4 (Admin/HC/Manager/Atasan) lihat semua bagian; L5-L6 (Coach/Coachee) lihat bagian sendiri otomatis |
| 3 | Browse Per Bagian | Daftar bagian terurut DisplayOrder; expand → tampil daftar file KKJ tersedia |
| 4 | Download File KKJ | Klik tombol Download per file → endpoint `/CMP/KkjFileDownload/:id` |
| 5 | Cari Versi Lama | File archived tidak ditampilkan otomatis; koordinasi AdminHC kalau perlu versi historis |

### Acc-2: `cmp-mapping-kkj-cpdp` — Alignment KKJ ↔ IDP — Role: All — 5 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Tab Alignment | `/CMP/DokumenKkj?tab=alignment`, atau klik tab "Alignment KKJ/IDP" di page Dokumen KKJ |
| 2 | Tujuan Alignment | Dokumen yang menyelaraskan **KKJ** (kebutuhan kompetensi jabatan) dengan **IDP** (Individual Development Plan) untuk perencanaan pelatihan |
| 3 | Browse Per Bagian | List bagian sama pattern dgn tab KKJ; expand → file CPDP per bagian |
| 4 | Download File CPDP | Klik Download → endpoint `/CMP/CpdpFileDownload/:id` |
| 5 | Refresh Frekuensi | Koordinasi AdminHC; kalau KKJ update, CPDP harus diperbarui mengikuti |

### Acc-3: `cmp-training-records` — Training Records (Capability Building) — Role: All — 5 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Records | CMP → "Riwayat Pelatihan" → halaman Capability Building Records |
| 2 | Tab My Records | Tab default; tampil semua record peserta yang login dgn badge count total |
| 3 | Tipe Record | 2 tipe: **Assessment Online** (dari ujian sistem) + **Training Manual** (HC input pelatihan eksternal/sertifikasi vendor) |
| 4 | Filter & Search | Filter section, unit, kategori, status; search by judul atau penyelenggara |
| 5 | Export Personal | Tombol **Export Excel** download Excel 2 sheet (Assessment + Training) untuk dokumentasi personal |

### Acc-4: `cmp-monitoring-records-tim` — Monitoring Records Tim — Role: **Atasan + Manager + AdminHC** (was Atasan + AdminHC) — 5 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Team View | Records page → tab "Team View" (visible role level ≤4: Atasan, Manager+, AdminHC) |
| 2 | Cakupan Tim | Bawahan langsung; Manager+ juga lihat bawahan tidak langsung sesuai struktur organisasi |
| 3 | Cascade Filter + Date Range | Pilih bagian → unit terkait muncul; pilih kategori → sub-kategori muncul; plus filter **status** + **dateFrom / dateTo** (rentang tanggal). Partial reload via `RecordsTeamPartial` |
| 4 | Detail Worker | Klik nama pekerja → `/CMP/RecordsWorkerDetail?workerId=...`, lihat riwayat lengkap individu |
| 5 | Export Excel Team | 2 tombol terpisah: **ExportRecordsTeamAssessment** (Excel assessment) + **ExportRecordsTeamTraining** (Excel training manual); filter mengikuti view aktif |

### Acc-5: `cmp-pre-post-test` — Cara Mengerjakan Pre-Post Test — Role: All — 6 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Assessment | CMP → "Assessment Saya" → daftar semua ujian Anda |
| 2 | Pahami Pairing | Sistem otomatis pasangkan **Pre-Test + Post-Test** berdampingan dalam 1 card visual. Badge **Pre-Test** (info) dan **Post-Test** (primary) untuk identifikasi |
| 3 | Kerjakan Pre-Test | Tombol **Mulai Pre-Test** (atau **Lanjutkan Pre-Test** kalau sudah ada saved progress); klik → wizard ujian |
| 4 | Ikuti Pelatihan | Setelah Pre-Test selesai, ikuti program pelatihan sesuai jadwal HC |
| 5 | Kerjakan Post-Test | Tombol **Mulai Post-Test** muncul setelah pelatihan; sistem auto-hitung **Gain Score** (selisih Pre vs Post) |
| 6 | Lihat Hasil | Tombol **Lihat Hasil** → halaman Results dgn breakdown nilai + analisa per elemen teknis |

### Acc-6: `cmp-monitoring-manager` — Monitoring Compliance via Analytics Dashboard — Role: **AdminHC** (was Manager + AdminHC) — 6 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses & Summary | CMP → "Dasbor Analitik" (visible Admin/HC only); 4 KPI cards atas: **Total Sessions**, **Pass Rate**, **Expiring**, **Avg Gain Score** |
| 2 | Tab Fail Rate | Persentase fail per kategori/bagian, drill-down ke detail pekerja yang fail |
| 3 | Tab Trend | Chart line pass rate over time, filter by period (bulan/quarter/tahun) |
| 4 | Tab Skor Elemen Teknis (ET) | Breakdown skor per elemen kompetensi teknis, identify area lemah cross-unit |
| 5 | Tab Sertifikat Expired | Daftar pekerja yang sertifikatnya akan expire, sort by expiry date; data untuk plan perpanjangan |
| 6 | Tab Item Analysis + Gain Score | Pilih Assessment Group → per-soal item analysis (identify soal terlalu sulit/mudah) + per-peserta Gain Score Report; **Export Excel** masing-masing |

### Acc-7 (NEW): `cmp-budget-training` — Budget Training & Assessment — Role: **AdminHC** — 6 step

| # | Step Title | Body |
|---|-----------|------|
| 1 | Akses Budget Training | CMP → "Budget Training" (visible Admin/HC only) → halaman dgn 2 tab: Data Budget + Ringkasan |
| 2 | Tab Data Budget | Tab default; filter **Tahun** + **Type** + **Kategori** + **Search**; sort per kolom; pagination |
| 3 | Tambah Budget Item | Tombol **Tambah** → form Create wizard 3 card: (1) Tahun Anggaran + Judul, (2) Kategori + SubKategori (cascade) + Jumlah Peserta, (3) Anggaran (Estimasi Biaya Total) + Realisasi Biaya + Vendor |
| 4 | Quick Update Realisasi | Inline edit realisasi per baris tanpa buka form penuh (endpoint `BudgetTrainingQuickUpdate`) |
| 5 | Import Excel | Tombol **Import** → Download Template Excel → isi → upload → sistem validate per baris + tampilkan hasil import (Success/Skip/Error per row) |
| 6 | Export Excel + Ringkasan | Tombol **Export Excel** untuk download filtered data; tab **Ringkasan** untuk grafik total anggaran vs realisasi per kategori/tahun |

---

## Role Visibility Matrix (after update — verified per GuideRoleAccess.GroupsFor)

Total 13 accordion CMP setelah update (12 existing post-assessment + Acc-7 NEW).

5 accordion role `All` (visible semua): Acc-1 Library KKJ, Acc-2 Alignment, Acc-3 Training Records, Acc-5 Pre-Post, cmp-tipe-assessment (shipped).

| Pertamina Role | Lvl | Sees groups | Acc count |
|----------------|-----|-------------|-----------|
| Admin / HC | 1, 2 | All groups | **13** (semua, termasuk Acc-7) |
| Direktur / VP / Manager | 3 | Manager + All | **6** (5 All + Acc-4-fix dgn Manager) |
| SectionHead / SrSupervisor (Atasan) | 4 | Atasan + All | **6** (5 All + Acc-4 existing) |
| Coach / Supervisor | 5 | Coach + All | **5** (5 All only) |
| Coachee | 6 | Coachee + All | **5** (5 All only) |

**Catatan:** Acc-6 Compliance (AdminHC only post-fix) + Acc-7 Budget Training (AdminHC) hanya visible Admin/HC. Acc-4 Records Tim visible Manager+Atasan+AdminHC.

---

## Section 3 — Verification

### Layer 1 — Build

```bash
dotnet build
```
Expected: `0 Error, 23 Warning` (baseline).

### Layer 2 — Static Check

| Check | Expected |
|-------|----------|
| Total `Module: GuideModule.Cmp` entries | 16 (existing 15 + Acc-7) |
| New ID `cmp-budget-training` present | 1 hit |
| Acc-4 role contains `RoleGroup.Manager` | 1 hit (was 0, bug fix) |
| Acc-6 role contains `RoleGroup.Manager` | 0 hit (was 1, bug fix) |
| 6 existing IDs preserved | All 6 hit |

### Layer 3 — Playwright Extend + Pre-existing Fix

**Fix pre-existing bug di test 5.3 + 5.4 (`tests/e2e/cmp-guide.spec.ts`):**

| Test | Assertion lama | Assertion benar |
|------|----------------|-----------------|
| 5.3 Admin accordion count | `toHaveCount(12)` | `toHaveCount(13)` (post Acc-7) |
| 5.4 Coachee accordion count | `toHaveCount(7)` ❌ | `toHaveCount(5)` ✅ |

Test 5.4 belum pernah pass (LDAP block coachee login di env saya), tapi assertion 7 salah bahkan tanpa LDAP issue. Coachee aktual lihat 5 accordion role All (Library + Mapping + Training Records + Pre-Post + Tipe Assessment).

**Tambah test 5.9 baru:**

```typescript
test('5.9 - Admin sees 13 CMP accordion + Acc-7 Budget Training visible', async ({ page }) => {
  await login(page, 'admin');
  await page.goto('/Home/GuideDetail?module=cmp');
  await page.waitForLoadState('networkidle');
  await expect(page.locator('#guideAccordion .accordion-item')).toHaveCount(13);
  await expect(page.locator('text=Budget Training & Assessment')).toBeVisible();
});
```

Total 9 test e2e (8 fix + 1 NEW).

### Layer 4 — Manual Browser UAT

Bapak verifikasi di browser dgn LDAP working:
- HC → /Home/GuideDetail?module=cmp → 13 accordion, Acc-7 "Budget Training" muncul
- Manager → 6 accordion: Acc-1,2,3,4-fix,5 + Acc-5-shipped (Acc-6 hilang, Acc-7 hilang)
- Coachee → 7 accordion (no Acc-7)
- Klik tiap Acc baru → step render correct dgn HTML formatting

### Commit Plan

**Commit 1 — Rewrite + Role fix:**
- Rewrite 6 existing CMP non-assessment accordion (Title/Steps/Keywords)
- Add 1 NEW accordion `cmp-budget-training`
- Fix 2 role bug (Acc-4 +Manager, Acc-6 -Manager)

**Commit 2 — Playwright fixes:**
- Fix pre-existing test 5.3 assertion: 12 → 13 (post Acc-7)
- Fix pre-existing test 5.4 assertion: 7 → 5 (was always wrong, never validated due to LDAP block)
- Add test 5.9 NEW (Acc-7 Budget Training visible + 13 accordion total)

---

## File Refs

- `Services/GuideContentProvider.cs` (1046 baris, after assessment update)
- `Controllers/CMPController.cs` (sumber reverse-engineer)
- `Views/CMP/Index.cshtml` (UI navigasi)
- `Views/CMP/DokumenKkj.cshtml` (Library KKJ + Alignment)
- `Views/CMP/Records.cshtml` (Training Records + Team View)
- `Views/CMP/Assessment.cshtml` (Pre-Post pairing)
- `Views/CMP/AnalyticsDashboard.cshtml` (Compliance — 6 tabs)
- `Views/CMP/BudgetTraining.cshtml` (Budget Training — 2 tabs)
- `Services/GuideRoleAccess.cs` (role mapping)
- `Models/UserRoles.cs` (RoleLevel definitions)

## Next Steps

1. Commit spec
2. Bapak review spec
3. Invoke `writing-plans` untuk PLAN.md
4. Execute via inline executing-plans atau subagent-driven-development
