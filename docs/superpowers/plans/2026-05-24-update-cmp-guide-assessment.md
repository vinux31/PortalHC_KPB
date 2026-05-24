# Update CMP Guide — Konten Assessment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update page `/Home/GuideDetail?module=cmp` dengan konten assessment terbaru — refactor PDF slot menjadi list, pindah PDF admin dari Data ke CMP, tambah 6 accordion baru, refresh 2 PDF (coachee + admin) dengan fitur sistem aktual.

**Architecture:** Refactor minimal di `GuideContentProvider.GetPdf` → `GetPdfs` (return list) supaya admin lihat 2 PDF card di CMP. Tambahan content statis di C# `Items` list (6 GuideItem) + HTML PDF refresh (insert sections, no JS/CSS change). Tidak ada DB migration, tidak ada API change.

**Tech Stack:** .NET Core 8 MVC (Razor view), C# static content provider, HTML/CSS static PDF, Playwright TypeScript untuk e2e verification.

**Spec reference:** `docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md`

---

## File Structure

### Files Modified

| File | Change |
|------|--------|
| `Services/GuideContentProvider.cs` | Rename `GetPdf` → `GetPdfs` (list); pindah PDF admin Module `Data` → `Cmp`; tambah 6 entry `GuideItem` baru |
| `Models/Guide/GuideDetailViewModel.cs` | Field `Pdf: GuidePdfLink?` → `Pdfs: IReadOnlyList<GuidePdfLink>` |
| `Controllers/HomeController.cs` | Call site `GetPdf(...)` → `GetPdfs(...)`; rename viewmodel prop |
| `Views/Home/GuideDetail.cshtml` | `@if (Model.Pdf != null)` → `@foreach (var pdf in Model.Pdfs)` |
| `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html` | Insert sec 1.5+1.6, augment sec 6+7, FAQ +2 Q, bump version |
| `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` | Letter rescheme A→J: insert A (Manage Kategori), augment B-E, insert F-J, bump version |

### Files Created

| File | Purpose |
|------|---------|
| `tests/e2e/cmp-guide.spec.ts` | Playwright e2e — PDF card count per role, accordion count per role, PDF accessibility |

### Files Not Touched

- DB schema — no migration
- Other controllers/services
- CSS / JS

---

## Pre-flight

- [ ] **P1: Konfirmasi working tree bersih**

```bash
git status
```
Expected: hanya `docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md` modified/added (spec hasil brainstorming) + this plan file. Tidak ada perubahan source kode pending.

- [ ] **P2: Konfirmasi build hijau di baseline**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Kalau ada warning baseline, catat — supaya bisa bedain warning baru hasil refactor vs warning eksisting.

- [ ] **P3: Konfirmasi server lokal bisa jalan**

```bash
dotnet run
```
Expected: server listening `http://localhost:5277`. Buka URL di browser, login pakai `admin@pertamina.com` / `123456`. Stop server (`Ctrl+C`) setelah konfirmasi UI bisa diakses.

---

## Task 1: Refactor `GetPdf` → `GetPdfs` + Pindah PDF Admin ke CMP

**Atomic commit:** semua 5 file diubah dalam 1 commit karena compile-coupled (rename method = compile error tanpa ubah caller).

**Files:**
- Modify: `Services/GuideContentProvider.cs:814` (pindah Module), `:856-857` (rename + body), `:883` (ItemCount call site)
- Modify: `Models/Guide/GuideDetailViewModel.cs:10` (field rename + type change)
- Modify: `Controllers/HomeController.cs:406` (call site)
- Modify: `Views/Home/GuideDetail.cshtml:33-58` (if → foreach)

- [ ] **Step 1.1: Ubah signature `GetPdf` jadi `GetPdfs`**

File: `Services/GuideContentProvider.cs`, line ~856-857.

Sebelum:
```csharp
public static GuidePdfLink? GetPdf(GuideModule module, string userRole)
    => Pdfs.FirstOrDefault(p => p.Module == module && GuideRoleAccess.CanSee(userRole, p.Roles));
```

Sesudah:
```csharp
public static IReadOnlyList<GuidePdfLink> GetPdfs(GuideModule module, string userRole)
    => Pdfs.Where(p => p.Module == module && GuideRoleAccess.CanSee(userRole, p.Roles)).ToList();
```

- [ ] **Step 1.2: Update `GetModuleCards` ItemCount call site**

File: `Services/GuideContentProvider.cs`, line ~883.

Sebelum:
```csharp
ItemCount: GetItems(m.Module, userRole).Count + (GetPdf(m.Module, userRole) != null ? 1 : 0),
```

Sesudah:
```csharp
ItemCount: GetItems(m.Module, userRole).Count + GetPdfs(m.Module, userRole).Count,
```

- [ ] **Step 1.3: Pindah PDF admin dari Module `Data` → `Cmp`**

File: `Services/GuideContentProvider.cs`, line ~813-821.

Sebelum:
```csharp
new GuidePdfLink(
    Module: GuideModule.Data,
    Title: "Panduan Buat Assessment & Input Soal",
    Description: "Tutorial lengkap membuat assessment, mengelola paket soal, dan mengimpor soal dari Excel — untuk Admin & HC.",
    FilePath: "~/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html",
    CardCssClass: "guide-tutorial-card--data",
    BtnColorClass: "btn-primary",
    Roles: new[] { RoleGroup.AdminHC }
),
```

Sesudah:
```csharp
new GuidePdfLink(
    Module: GuideModule.Cmp,
    Title: "Panduan Buat Assessment & Input Soal",
    Description: "Tutorial lengkap membuat assessment, mengelola paket soal, dan mengimpor soal dari Excel — untuk Admin & HC.",
    FilePath: "~/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html",
    CardCssClass: "guide-tutorial-card--cmp",
    BtnColorClass: "btn-primary",
    Roles: new[] { RoleGroup.AdminHC }
),
```

Catatan: `CardCssClass` ikut ganti dari `--data` ke `--cmp` supaya visual konsisten dengan kartu PDF lain di module CMP (existing CSS `.guide-tutorial-card--cmp` di `wwwroot/css/guide.css`).

- [ ] **Step 1.4: Rename ViewModel field `Pdf` → `Pdfs`**

File: `Models/Guide/GuideDetailViewModel.cs`.

Sebelum:
```csharp
namespace HcPortal.Models.Guide;

public record GuideDetailViewModel(
    string UserRole,
    GuideModule Module,
    string ModuleTitle,
    string ModuleIcon,
    string ModuleBreadcrumb,
    string ModuleCategory,
    GuidePdfLink? Pdf,
    IReadOnlyList<GuideItem> Items
);
```

Sesudah:
```csharp
namespace HcPortal.Models.Guide;

public record GuideDetailViewModel(
    string UserRole,
    GuideModule Module,
    string ModuleTitle,
    string ModuleIcon,
    string ModuleBreadcrumb,
    string ModuleCategory,
    IReadOnlyList<GuidePdfLink> Pdfs,
    IReadOnlyList<GuideItem> Items
);
```

- [ ] **Step 1.5: Update controller call site**

File: `Controllers/HomeController.cs`, line ~399-408.

Sebelum:
```csharp
var vm = new GuideDetailViewModel(
    UserRole: userRole,
    Module: moduleEnum,
    ModuleTitle: moduleTitle,
    ModuleIcon: moduleIcon,
    ModuleBreadcrumb: moduleBreadcrumb,
    ModuleCategory: moduleCategory,
    Pdf: GuideContentProvider.GetPdf(moduleEnum, userRole),
    Items: GuideContentProvider.GetItems(moduleEnum, userRole)
);
```

Sesudah:
```csharp
var vm = new GuideDetailViewModel(
    UserRole: userRole,
    Module: moduleEnum,
    ModuleTitle: moduleTitle,
    ModuleIcon: moduleIcon,
    ModuleBreadcrumb: moduleBreadcrumb,
    ModuleCategory: moduleCategory,
    Pdfs: GuideContentProvider.GetPdfs(moduleEnum, userRole),
    Items: GuideContentProvider.GetItems(moduleEnum, userRole)
);
```

- [ ] **Step 1.6: Update view loop**

File: `Views/Home/GuideDetail.cshtml`, line ~33-58.

Sebelum:
```cshtml
@if (Model.Pdf != null)
{
    var outlineBtn = Model.Pdf.BtnColorClass.Replace("btn-", "btn-outline-");
    var pdfDownloadName = System.IO.Path.GetFileName(Model.Pdf.FilePath);
    <div class="guide-tutorial-card @Model.Pdf.CardCssClass" data-aos="fade-up" data-aos-delay="150">
        <div class="guide-tutorial-inner d-flex align-items-center justify-content-between flex-wrap gap-3">
            <div class="d-flex align-items-center gap-3">
                <div class="guide-tutorial-icon">
                    <i class="bi bi-file-earmark-pdf-fill fs-4"></i>
                </div>
                <div>
                    <h5 class="guide-tutorial-title mb-1 fw-bold">@Model.Pdf.Title</h5>
                    <p class="mb-0 text-muted small">@Model.Pdf.Description</p>
                </div>
            </div>
            <div class="d-flex gap-2">
                <a href="@Url.Content(Model.Pdf.FilePath)" target="_blank" class="btn @outlineBtn btn-sm">
                    <i class="bi bi-eye me-1"></i> Lihat
                </a>
                <a href="@Url.Content(Model.Pdf.FilePath)" download="@pdfDownloadName" class="btn @Model.Pdf.BtnColorClass btn-sm">
                    <i class="bi bi-download me-1"></i> Download
                </a>
            </div>
        </div>
    </div>
}
```

Sesudah:
```cshtml
@{
    var pdfDelay = 150;
}
@foreach (var pdf in Model.Pdfs)
{
    var outlineBtn = pdf.BtnColorClass.Replace("btn-", "btn-outline-");
    var pdfDownloadName = System.IO.Path.GetFileName(pdf.FilePath);
    <div class="guide-tutorial-card @pdf.CardCssClass" data-aos="fade-up" data-aos-delay="@pdfDelay">
        <div class="guide-tutorial-inner d-flex align-items-center justify-content-between flex-wrap gap-3">
            <div class="d-flex align-items-center gap-3">
                <div class="guide-tutorial-icon">
                    <i class="bi bi-file-earmark-pdf-fill fs-4"></i>
                </div>
                <div>
                    <h5 class="guide-tutorial-title mb-1 fw-bold">@pdf.Title</h5>
                    <p class="mb-0 text-muted small">@pdf.Description</p>
                </div>
            </div>
            <div class="d-flex gap-2">
                <a href="@Url.Content(pdf.FilePath)" target="_blank" class="btn @outlineBtn btn-sm">
                    <i class="bi bi-eye me-1"></i> Lihat
                </a>
                <a href="@Url.Content(pdf.FilePath)" download="@pdfDownloadName" class="btn @pdf.BtnColorClass btn-sm">
                    <i class="bi bi-download me-1"></i> Download
                </a>
            </div>
        </div>
    </div>
    pdfDelay += 50;
}
```

Catatan: AOS delay increment 50ms per PDF supaya animasi stagger natural saat 2 PDF render.

- [ ] **Step 1.7: Build & verify**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Kalau muncul compile error, paling sering lupa update salah satu call site — grep:
```bash
```
Pakai Grep tool:
- Pattern: `GetPdf\b` (word boundary)
- Expected: 0 hits (semua sudah jadi `GetPdfs`)
- Pattern: `Model\.Pdf\b`
- Expected: 0 hits (semua sudah jadi `Model.Pdfs`)

- [ ] **Step 1.8: Browser smoke test (admin)**

Jalankan server:
```bash
dotnet run
```

Login `admin@pertamina.com` / `123456`. Buka:
- `http://localhost:5277/Home/GuideDetail?module=cmp` — expected: 2 PDF card (coachee + admin) + 6 accordion existing.
- `http://localhost:5277/Home/GuideDetail?module=data` — expected: 0 PDF card + accordion existing untuk Data module.
- `http://localhost:5277/Home/Guide` — expected: kartu CMP item count naik (existing+2), kartu Data turun (existing-1).

Stop server.

- [ ] **Step 1.9: Browser smoke test (coachee)**

Jalankan server, login `rino.prasetyo@pertamina.com` / `123456`. Buka:
- `http://localhost:5277/Home/GuideDetail?module=cmp` — expected: **1 PDF card** ("Panduan Lengkap Assessment") + accordion existing untuk role coachee. PDF admin TIDAK boleh muncul.

Stop server.

- [ ] **Step 1.10: Commit**

```bash
git add Services/GuideContentProvider.cs Models/Guide/GuideDetailViewModel.cs Controllers/HomeController.cs Views/Home/GuideDetail.cshtml
git commit -m "$(cat <<'EOF'
refactor(guide): GetPdf jadi list, pindah PDF admin Data->CMP

- Rename GuideContentProvider.GetPdf -> GetPdfs (return IReadOnlyList).
- Update ViewModel field Pdf? -> Pdfs (list).
- View loop @foreach gantikan @if single.
- PDF "Panduan Buat Assessment & Input Soal" pindah Module Data -> Cmp,
  CardCssClass --data -> --cmp.
- Admin lihat 2 PDF card di /Home/GuideDetail?module=cmp.
- Coachee tetap 1 PDF (PDF admin role-gated AdminHC).

Spec: docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Tambah 6 Accordion Baru di Module CMP

**Files:**
- Modify: `Services/GuideContentProvider.cs` — tambah 6 `GuideItem` entry di list `Items`, posisi setelah `cmp-monitoring-manager` (line ~86), sebelum komentar `═══ CDP ═══`.

**Catatan posisi insert:** Tambah blok baru tepat setelah `Keywords: new[] { "monitoring", "compliance", "unit", "section", "dashboard", "manager" }\n)` (penutup `cmp-monitoring-manager`), sebelum koma + komentar `// ═══════════════════ CDP ═══════════════════`.

- [ ] **Step 2.1: Tambah Acc-5 (Tipe-tipe Assessment, All)**

Insert block (jangan lupa koma penutup di akhir tiap GuideItem record):

```csharp
new GuideItem(
    Id: "cmp-tipe-assessment",
    Module: GuideModule.Cmp,
    Title: "Tipe-tipe Assessment (Pre-Test, Post-Test, Regular)",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Pre-Test",
            "<b>Pre-Test</b> adalah ujian awal yang dikerjakan <i>sebelum</i> pelatihan atau coaching dimulai. Tujuannya untuk mengukur tingkat pemahaman awal Anda (baseline knowledge) sebelum mendapat materi. Hasil Pre-Test biasanya tidak menentukan kelulusan — fungsi utamanya membandingkan progres."),
        new GuideStep(2, "Post-Test",
            "<b>Post-Test</b> dikerjakan <i>setelah</i> pelatihan selesai. Sistem otomatis menghitung selisih nilai vs Pre-Test (<b>Gain Score</b>) untuk lihat seberapa besar peningkatan pemahaman Anda. Paket soal Post-Test bisa <i>sama</i> dengan Pre-Test (sengaja untuk perbandingan langsung) atau berbeda — tergantung setting HC."),
        new GuideStep(3, "Regular Assessment",
            "<b>Regular Assessment</b> adalah ujian mandiri yang tidak dipasangkan dengan ujian lain. Cakupannya: sertifikasi kompetensi, On-the-Job assessment, Mandatory HSSE Training, dan lain-lain. Tidak ada hitungan Gain Score karena tidak ada pasangan Pre/Post.")
    },
    Keywords: new[] { "tipe", "pre-test", "post-test", "regular", "pretest", "posttest", "jenis assessment", "gain score" }
),
```

- [ ] **Step 2.2: Tambah Acc-6 (Tipe Package Question, AdminHC)**

Insert block setelah Acc-5:

```csharp
new GuideItem(
    Id: "cmp-tipe-package-question",
    Module: GuideModule.Cmp,
    Title: "Tipe Soal: Multiple Choice, Multiple Answer, Essay",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Multiple Choice (MC)",
            "Soal dengan <b>satu</b> jawaban benar dari beberapa opsi (radio button). Default semua soal lama. Auto-grading instant saat peserta submit."),
        new GuideStep(2, "Multiple Answer (MA)",
            "Soal dengan <b>lebih dari satu</b> jawaban benar (checkbox). Peserta harus pilih <i>semua</i> opsi benar untuk dianggap betul — kurang satu = salah. Auto-grading instant. (Feature Phase 296)"),
        new GuideStep(3, "Essay",
            "Soal jawaban teks bebas. Field tambahan saat input soal: <b>Rubrik</b> (kunci jawaban referensi internal HC — tidak ditampilkan ke peserta) dan <b>MaxCharacters</b> (default 2000). <b>Grading manual</b> oleh HC — hasil tidak instan, peserta harus tunggu HC nilai."),
        new GuideStep(4, "Kapan pakai yang mana",
            "<b>MC</b> untuk pengetahuan faktual (definisi, prosedur). <b>MA</b> untuk konsep multi-aspek (mis: \"Sebutkan APD yang wajib dipakai\"). <b>Essay</b> untuk analisis, penjelasan, atau studi kasus.")
    },
    Keywords: new[] { "tipe soal", "multiple choice", "multiple answer", "essay", "rubrik", "package question", "mc", "ma" }
),
```

- [ ] **Step 2.3: Tambah Acc-7 (Cara Buat Assessment, AdminHC)**

```csharp
new GuideItem(
    Id: "cmp-cara-buat-assessment",
    Module: GuideModule.Cmp,
    Title: "Cara Buat Assessment (Admin)",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Akses Admin Assessment",
            "Login sebagai Admin/HC. Buka menu <b>Kelola Data → Manage Assessment & Training</b>. Klik tombol <b>Buat Assessment</b> di kanan atas."),
        new GuideStep(2, "Pilih Kategori",
            "Pilih kategori dari dropdown (OJT, IHT, Mandatory HSSE, dll). Kalau kategori punya sub-kategori, dropdown sub-kategori muncul otomatis. Kalau kategori belum ada, buat dulu via <b>Manage Categories</b> (lihat accordion \"Cara Manage Kategori Assessment\")."),
        new GuideStep(3, "Isi Detail Sesi",
            "Isi: judul, deskripsi, tanggal mulai/akhir, durasi (menit), tipe assessment (Pre-Test / Post-Test / Regular), pass percentage, token akses (opsional). Set <b>Extra Time</b> kalau ada peserta perlu tambahan waktu (aksesibilitas)."),
        new GuideStep(4, "Link Pre-Post (Opsional)",
            "Kalau buat Post-Test, ada field <b>Linked Pre-Test Session</b> untuk pilih sesi Pre yang dipasangkan. Centang <b>SamePackage</b> kalau ingin paket soal Post-Test <i>identik</i> dengan Pre — biar perbandingan apple-to-apple. Kosongkan kalau paket berbeda."),
        new GuideStep(5, "Tambah Peserta",
            "Pilih peserta dari daftar pekerja (filter unit, pencarian nama/email, Select All/Deselect All). Token akses <i>shared</i> per batch — semua peserta sesi pakai token yang sama."),
        new GuideStep(6, "Publish & Monitor",
            "Klik <b>Buat Assessment</b>. Setelah publish, peserta bisa akses ujian. Monitor real-time via <b>Admin Monitoring</b> (SignalR live update — lihat siapa yang sudah masuk, soal ke berapa, sisa waktu).")
    },
    Keywords: new[] { "buat assessment", "create", "wizard", "admin", "settings", "extra time", "linked session", "samepackage" }
),
```

- [ ] **Step 2.4: Tambah Acc-8 (Cara Upload Package Question, AdminHC)**

```csharp
new GuideItem(
    Id: "cmp-cara-upload-package",
    Module: GuideModule.Cmp,
    Title: "Cara Upload Package Question (Paket Soal)",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Manage Package",
            "Dari sesi assessment, klik menu <b>Manage Questions</b>. Bisa buat <b>multi-package</b> per sesi (Paket A, Paket B, Paket C — `PackageNumber`). Berguna untuk anti-contek: peserta dapat paket berbeda."),
        new GuideStep(2, "Pilih Metode Input",
            "Tiga cara input soal: <b>1.</b> Upload file Excel (download template dulu), <b>2.</b> Paste dari clipboard Excel, <b>3.</b> Manual entry per-soal di form."),
        new GuideStep(3, "Format Excel",
            "Kolom wajib: <b>Question</b>, <b>OptionA</b>, <b>OptionB</b>, <b>OptionC</b>, <b>OptionD</b> (E opsional), <b>CorrectAnswer</b> (huruf opsi benar atau pisah koma kalau MA), <b>QuestionType</b> (MC/MA/Essay). Untuk Essay: tambah kolom <b>Rubrik</b> dan <b>MaxCharacters</b>."),
        new GuideStep(4, "Preview & Verifikasi",
            "Setelah import, cek <b>semua</b> soal di tab Preview: pertanyaan, opsi, kunci jawaban. Edit per-soal kalau ada salah ketik atau kunci jawaban keliru. Wajib verifikasi sebelum publish — soal yang sudah dikerjakan peserta sulit di-edit."),
        new GuideStep(5, "Shuffle & Publish",
            "Aktifkan <b>Shuffle Per-User</b> kalau ingin tiap peserta lihat urutan opsi A/B/C/D berbeda (anti-contek). Display saja — grading tetap pakai `PackageOption.Id` di backend. Bisa <b>reshuffle</b> per-sesi atau bulk kalau perlu ulang acak.")
    },
    Keywords: new[] { "upload", "package", "paket soal", "excel", "import", "shuffle", "preview", "multi-package" }
),
```

- [ ] **Step 2.5: Tambah Acc-9 (Cara Manage Kategori Assessment, AdminHC)**

```csharp
new GuideItem(
    Id: "cmp-cara-manage-kategori",
    Module: GuideModule.Cmp,
    Title: "Cara Manage Kategori Assessment (Admin)",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Manage Categories",
            "Login sebagai Admin/HC. Buka menu <b>Admin → Manage Kategori Assessment</b>. Halaman ini punya CRUD penuh untuk kategori assessment (tambah, edit, hapus)."),
        new GuideStep(2, "Buat Parent Category",
            "Klik <b>Tambah Kategori</b>. Isi nama (contoh: \"Assessment OJ\", \"Mandatory HSSE Training\"). Tambah <b>Signatory User</b> (pekerja yang akan TTD sertifikat untuk kategori ini — biasanya manager unit terkait)."),
        new GuideStep(3, "Tambah Sub-Kategori",
            "Untuk hierarki, buat kategori baru dan pilih <b>Parent Category</b> dari dropdown. Sub-kategori bisa <i>inherit signatory parent</i> (kosongkan signatory) atau set signatory sendiri (override). Mendukung 2 level (parent + child)."),
        new GuideStep(4, "Edit / Hapus",
            "Edit kapan saja (nama, signatory). <b>Hapus</b> hanya bisa kalau tidak ada sesi assessment aktif yang masih pakai kategori ini — kalau ada, sistem block dan tampilkan daftar sesi yang menghalangi.")
    },
    Keywords: new[] { "kategori", "manage", "categories", "signatory", "parent", "sub-kategori", "ttd", "sertifikat" }
),
```

- [ ] **Step 2.6: Tambah Acc-10 (Fitur Khusus Admin, AdminHC)**

```csharp
new GuideItem(
    Id: "cmp-fitur-khusus-admin",
    Module: GuideModule.Cmp,
    Title: "Fitur Khusus Admin (Manual Entry, Extra Time, Reshuffle, Edit Jawaban, Renewal)",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Manual Entry Sertifikat",
            "Untuk peserta yang sudah punya sertifikat dari lembaga luar (tidak ikut ujian online), gunakan <b>Manual Entry</b>. Aktifkan toggle saat buat sesi → muncul field: <b>Penyelenggara</b>, <b>Kota</b>, <b>SubKategori</b>, <b>CertificateType</b> (Kompetensi/Profesi/Pelatihan). Sistem set `IsManualEntry=true` — sertifikat tetap masuk records peserta."),
        new GuideStep(2, "Set Extra Time",
            "Per-sesi atau per-peserta, set <b>ExtraTimeMinutes</b> (tambahan waktu menit) untuk aksesibilitas. Saat peserta mulai ujian, timer otomatis dapat tambahan — peserta tidak perlu lapor. (Feature Phase 302)"),
        new GuideStep(3, "Reshuffle Package",
            "Kalau perlu ulang acak urutan opsi A/B/C/D (mis: ada bocor jawaban), klik <b>Reshuffle</b> per-sesi atau bulk (semua sesi sekaligus). Display random, grading tetap konsisten via `PackageOption.Id`."),
        new GuideStep(4, "Akhiri Ujian",
            "<b>AkhiriUjian</b> (1 peserta) atau <b>AkhiriSemuaUjian</b> (bulk semua peserta sesi) — paksa submit semua jawaban yang sudah ada. Berguna kalau waktu habis tapi peserta lupa submit, atau ada keadaan darurat."),
        new GuideStep(5, "Edit Jawaban Peserta",
            "Setelah ujian selesai, Admin bisa <b>override jawaban</b> peserta lewat halaman Detail Sesi (mis: koreksi typo sistem, atau adjudikasi soal yang ambigu). Setiap edit di-log otomatis ke <b>AssessmentEditLog</b> dengan timestamp + user yang edit — audit trail lengkap."),
        new GuideStep(6, "Renewal Chain",
            "Untuk sertifikat yang perlu diperbarui (mis: lisensi tahunan), buat sesi baru lalu link ke sertifikat lama via <b>RenewsSessionId</b> atau <b>RenewsTrainingId</b>. Sistem chain sertifikat lama → baru untuk tracking history perpanjangan. (Feature Phase 200)")
    },
    Keywords: new[] { "manual entry", "extra time", "reshuffle", "akhiri ujian", "edit jawaban", "renewal", "fitur khusus", "audit log", "aksesibilitas" }
),
```

- [ ] **Step 2.7: Build & verify**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Kalau ada error syntax (missing koma, kurung), perbaiki dengan baca pesan error.

- [ ] **Step 2.8: Browser smoke test (admin)**

`dotnet run`. Login `admin@pertamina.com`. Buka `http://localhost:5277/Home/GuideDetail?module=cmp`.

Expected:
- 2 PDF card di atas (coachee + admin)
- **12 accordion** total (6 existing + 6 baru)
- Accordion baru muncul dengan title:
  - "Tipe-tipe Assessment (Pre-Test, Post-Test, Regular)"
  - "Tipe Soal: Multiple Choice, Multiple Answer, Essay"
  - "Cara Buat Assessment (Admin)"
  - "Cara Upload Package Question (Paket Soal)"
  - "Cara Manage Kategori Assessment (Admin)"
  - "Fitur Khusus Admin (Manual Entry, Extra Time, Reshuffle, Edit Jawaban, Renewal)"
- Klik tiap accordion → langkah-langkah render correct (HTML formatting `<b>`, `<i>` work).

- [ ] **Step 2.9: Browser smoke test (coachee)**

Login `rino.prasetyo@pertamina.com`. Buka same URL.

Expected:
- 1 PDF card (coachee only)
- **7 accordion** total (6 existing + 1 baru = "Tipe-tipe Assessment")
- Accordion AdminHC-only (6, 7, 8, 9, 10) TIDAK muncul.

Stop server.

- [ ] **Step 2.10: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): tambah 6 accordion CMP konten assessment terbaru

Acc-5 Tipe Assessment (All): Pre-Test, Post-Test, Regular.
Acc-6 Tipe Soal (AdminHC): MC, MA, Essay + kapan pakai.
Acc-7 Cara Buat Assessment (AdminHC): 6 step end-to-end termasuk
  Pre-Post linking + SamePackage + Extra Time.
Acc-8 Cara Upload Package Question (AdminHC): Excel format termasuk
  QuestionType + Rubrik + MaxCharacters, multi-package, shuffle.
Acc-9 Cara Manage Kategori (AdminHC): CRUD + signatory + sub-kategori.
Acc-10 Fitur Khusus Admin (AdminHC): Manual Entry, Extra Time,
  Reshuffle, Akhiri Ujian, Edit Jawaban (AssessmentEditLog),
  Renewal Chain.

Coachee total view: 6 existing + 1 baru = 7 accordion.
AdminHC total view: 6 existing + 6 baru = 12 accordion.

Spec: docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Refresh PDF Coachee (`Panduan-Lengkap-Assessment.html`)

**Files:**
- Modify: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html` (629 baris → est ~800 baris)

**Strategy:** Insert 2 section baru posisi natural (sec 1.5 setelah "Apa Itu Assessment", sec 1.6 sebelum "Alur Proses"). Augment 2 existing section (Memulai Ujian + Mengerjakan Soal). Tambah 2 FAQ. Renumber heading display number tapi PERTAHANKAN anchor `id` existing (`#step1..#step13`) supaya bookmark eksternal tidak rusak.

- [ ] **Step 3.1: Bump version note**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, line ~100.

Sebelum:
```html
<p class="version">Versi 1.1 &bull; Maret 2026 &bull; Untuk Semua Pengguna</p>
```

Sesudah:
```html
<p class="version">Versi 1.2 &bull; Mei 2026 &bull; Untuk Semua Pengguna</p>
```

- [ ] **Step 3.2: Update TOC (Daftar Isi)**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, line ~106-127.

Sebelum:
```html
<div class="toc">
  <h2>Daftar Isi</h2>
  <ol>
    <li><a href="#overview">Apa Itu Assessment?</a></li>
    <li><a href="#alur">Alur Proses Assessment</a></li>
    <li><a href="#step1">Mengakses Halaman CMP</a></li>
    <li><a href="#step2">Melihat Daftar Assessment</a></li>
    <li><a href="#step3">Memasukkan Token Akses</a></li>
    <li><a href="#step4">Memulai Ujian</a></li>
    <li><a href="#step5">Mengerjakan Soal</a></li>
    <li><a href="#step6">Auto-Save Jawaban</a></li>
    <li><a href="#step7">Melanjutkan Ujian (Resume)</a></li>
    <li><a href="#step8">Review Sebelum Submit</a></li>
    <li><a href="#step9">Submit Ujian</a></li>
    <li><a href="#step10">Auto-Submit jika Waktu Habis</a></li>
    <li><a href="#step11">Melihat Hasil Assessment</a></li>
    <li><a href="#step12">Melihat Riwayat Ujian</a></li>
    <li><a href="#step13">Download Sertifikat</a></li>
    <li><a href="#tips">Tips &amp; Hal Penting</a></li>
    <li><a href="#faq">FAQ (Pertanyaan Umum)</a></li>
  </ol>
</div>
```

Sesudah:
```html
<div class="toc">
  <h2>Daftar Isi</h2>
  <ol>
    <li><a href="#overview">Apa Itu Assessment?</a></li>
    <li><a href="#tipe-assessment">Tipe-tipe Assessment (Pre-Test, Post-Test, Regular)</a></li>
    <li><a href="#pre-post-flow">Pre-Test vs Post-Test &mdash; Apa Bedanya?</a></li>
    <li><a href="#alur">Alur Proses Assessment</a></li>
    <li><a href="#step1">Mengakses Halaman CMP</a></li>
    <li><a href="#step2">Melihat Daftar Assessment</a></li>
    <li><a href="#step3">Memasukkan Token Akses</a></li>
    <li><a href="#step4">Memulai Ujian</a></li>
    <li><a href="#step5">Mengerjakan Soal</a></li>
    <li><a href="#step6">Auto-Save Jawaban</a></li>
    <li><a href="#step7">Melanjutkan Ujian (Resume)</a></li>
    <li><a href="#step8">Review Sebelum Submit</a></li>
    <li><a href="#step9">Submit Ujian</a></li>
    <li><a href="#step10">Auto-Submit jika Waktu Habis</a></li>
    <li><a href="#step11">Melihat Hasil Assessment</a></li>
    <li><a href="#step12">Melihat Riwayat Ujian</a></li>
    <li><a href="#step13">Download Sertifikat</a></li>
    <li><a href="#tips">Tips &amp; Hal Penting</a></li>
    <li><a href="#faq">FAQ (Pertanyaan Umum)</a></li>
  </ol>
</div>
```

- [ ] **Step 3.3: Insert sec 1.5 "Tipe-tipe Assessment" + sec 1.6 "Pre-Test vs Post-Test"**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, line ~146 (setelah table di sec "1. Apa Itu Assessment?", sebelum komentar `<!-- ALUR -->` di line ~148).

Sebelum (konteks):
```html
  <tr><td><span class="badge badge-purple">Assessment Proton</span></td><td>Ujian terkait program Coaching Proton</td></tr>
</table>

<!-- ============================================================ -->
<!-- ALUR -->
<!-- ============================================================ -->
<h2 id="alur">2. Alur Proses Assessment</h2>
```

Sesudah (tambah 2 section baru di antara `</table>` dan `<!-- ALUR -->`):
```html
  <tr><td><span class="badge badge-purple">Assessment Proton</span></td><td>Ujian terkait program Coaching Proton</td></tr>
</table>

<!-- ============================================================ -->
<!-- TIPE ASSESSMENT -->
<!-- ============================================================ -->
<h2 id="tipe-assessment">2. Tipe-tipe Assessment</h2>

<p>Setiap assessment yang Anda kerjakan masuk salah satu dari <strong>tiga tipe</strong> di bawah ini. Penting untuk Anda pahami tipenya supaya tidak bingung saat lihat hasil.</p>

<h3>2.1 Pre-Test</h3>
<p><strong>Pre-Test</strong> adalah ujian awal yang dikerjakan <em>sebelum</em> Anda mengikuti pelatihan atau coaching. Tujuannya untuk mengukur tingkat pemahaman awal Anda (<strong>baseline knowledge</strong>) sebelum mendapat materi.</p>
<p>Hasil Pre-Test biasanya <em>tidak menentukan kelulusan</em> — fungsi utamanya untuk membandingkan progres Anda setelah pelatihan.</p>

<h3>2.2 Post-Test</h3>
<p><strong>Post-Test</strong> adalah ujian yang dikerjakan <em>setelah</em> pelatihan selesai. Sistem otomatis menghitung selisih nilai Anda vs Pre-Test (disebut <strong>Gain Score</strong>) untuk melihat seberapa besar peningkatan pemahaman Anda dari pelatihan tersebut.</p>

<h3>2.3 Regular Assessment</h3>
<p><strong>Regular Assessment</strong> adalah ujian mandiri yang tidak dipasangkan dengan ujian lain. Tipe ini mencakup: sertifikasi kompetensi (OJ), On-the-Job assessment, Mandatory HSSE Training, dan sertifikasi lisensi lainnya.</p>
<p>Tipe Regular tidak punya hitungan Gain Score karena tidak ada pasangan Pre/Post.</p>

<!-- ============================================================ -->
<!-- PRE-POST FLOW -->
<!-- ============================================================ -->
<h2 id="pre-post-flow">3. Pre-Test vs Post-Test &mdash; Apa Bedanya?</h2>

<p>Pertanyaan yang sering muncul: <em>"Apakah soal Post-Test sama dengan Pre-Test?"</em></p>

<p>Jawabannya: <strong>tergantung setting HC</strong>. Sistem menyediakan dua mode:</p>

<table>
  <tr><th>Mode</th><th>Penjelasan</th></tr>
  <tr><td><strong>Paket Sama</strong> (SamePackage <em>ON</em>)</td><td>Soal Post-Test <strong>identik</strong> dengan Pre-Test. Sengaja dipilih agar perbandingan apple-to-apple — peningkatan Anda murni dari penguasaan materi, bukan dari sulit/mudahnya soal.</td></tr>
  <tr><td><strong>Paket Beda</strong> (SamePackage <em>OFF</em>)</td><td>Soal Post-Test berbeda dengan Pre-Test, tapi cakupan kompetensi yang diuji tetap sama. Untuk mencegah peserta "menghafal" soal Pre-Test.</td></tr>
</table>

<div class="callout">
  <p><strong>Catatan untuk Coachee:</strong> Anda <em>tidak perlu</em> menghafal soal Pre-Test. Fokus pelajari materinya — kalau Anda paham konsepnya, mau paket sama atau beda hasilnya tetap bagus.</p>
</div>

<!-- ============================================================ -->
<!-- ALUR -->
<!-- ============================================================ -->
<h2 id="alur">4. Alur Proses Assessment</h2>
```

**Penting:** Heading number sec "Alur Proses" berubah dari `2.` jadi `4.`. Anchor `id="alur"` tetap (TOC link masih kerja).

- [ ] **Step 3.4: Renumber heading existing sec 3-17 jadi 5-19**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, line ~163 onward.

Update heading number satu per satu (gunakan Grep dulu untuk lihat semua `<h2 id="step` + `<h2 id="tips"` + `<h2 id="faq"`):

| Anchor (tetap) | Old number | New number | Title (tetap) |
|----------------|-----------|------------|---------------|
| `#step1` | 3 | 5 | Mengakses Halaman CMP |
| `#step2` | 4 | 6 | Melihat Daftar Assessment |
| `#step3` | 5 | 7 | Memasukkan Token Akses |
| `#step4` | 6 | 8 | Memulai Ujian |
| `#step5` | 7 | 9 | Mengerjakan Soal |
| `#step6` | 8 | 10 | Auto-Save Jawaban |
| `#step7` | 9 | 11 | Melanjutkan Ujian (Resume) |
| `#step8` | 10 | 12 | Review Sebelum Submit |
| `#step9` | 11 | 13 | Submit Ujian |
| `#step10` | 12 | 14 | Auto-Submit jika Waktu Habis |
| `#step11` | 13 | 15 | Melihat Hasil Assessment |
| `#step12` | 14 | 16 | Melihat Riwayat Ujian |
| `#step13` | 15 | 17 | Download Sertifikat |
| `#tips` | 16 | 18 | Tips & Hal Penting |
| `#faq` | 17 | 19 | FAQ (Pertanyaan Umum) |

Contoh edit (heading 1 dari 15):

Sebelum:
```html
<h2 id="step1">3. Mengakses Halaman CMP</h2>
```

Sesudah:
```html
<h2 id="step1">5. Mengakses Halaman CMP</h2>
```

Lanjut sampai semua 15 heading. Pakai Edit tool per-heading (atau script kecil), pastikan setiap nomor benar.

- [ ] **Step 3.5: Augment sec "Memulai Ujian" — tambah Extra Time notice**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, di akhir block `#step4` (sebelum heading `#step5`).

Cari blok step4 (sekarang heading `8. Memulai Ujian`). Tambah callout sebelum penutup section (sebelum `<h2 id="step5">9. Mengerjakan Soal</h2>`).

Tambah block:
```html
<div class="card-blue card" style="margin-top: 14px;">
  <div class="card-title">&#8505; Catatan Extra Time (Aksesibilitas)</div>
  <p>Kalau Anda diberikan <strong>tambahan waktu</strong> oleh HC karena kebutuhan aksesibilitas, timer di pojok kanan atas otomatis sudah ditambahkan — Anda <em>tidak perlu</em> lapor atau minta tambahan saat ujian berjalan.</p>
  <p>Tambahan waktu akan terlihat di info sesi sebelum klik <strong>Mulai</strong> (mis: "Durasi: 60 + 15 menit").</p>
</div>
```

- [ ] **Step 3.6: Augment sec "Mengerjakan Soal" — tambah subsection "Tipe Soal"**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, di dalam block `#step5` (sekarang heading `9. Mengerjakan Soal`).

Cari subsection existing `<h3 ...>Area Soal (Bagian Utama)</h3>`. Tambah subsection baru SETELAH `<h3 ...>Area Soal (Bagian Utama)</h3>` block penuh (sebelum `<h3 ...>Panel Navigasi Soal (Sidebar)</h3>`).

Insert block:
```html
<h3 style="margin-top:12px; margin-left: 0;">Tipe Soal yang Akan Dijumpai</h3>

<p>Tiga tipe soal yang mungkin Anda jumpai dalam satu sesi:</p>

<table>
  <tr><th>Tipe</th><th>Cara Jawab</th><th>Catatan Penting</th></tr>
  <tr>
    <td><strong>Multiple Choice (MC)</strong></td>
    <td>Pilih <strong>satu</strong> jawaban (radio button)</td>
    <td>Tipe paling umum. Hasil instan.</td>
  </tr>
  <tr>
    <td><strong>Multiple Answer (MA)</strong></td>
    <td>Pilih <strong>lebih dari satu</strong> jawaban (checkbox)</td>
    <td>Anda harus pilih <em>semua</em> opsi yang benar untuk dianggap betul. Kurang satu = salah. Hasil instan.</td>
  </tr>
  <tr>
    <td><strong>Essay</strong></td>
    <td>Ketik jawaban di kolom teks bebas</td>
    <td>Ada batas karakter (ditampilkan di bawah kolom). Hasil <strong>tidak instan</strong> — HC akan menilai manual.</td>
  </tr>
</table>

<div class="callout">
  <p><strong>Tip Essay:</strong> Jawaban Essay tidak punya kunci jawaban otomatis. Tuliskan jawaban se-jelas mungkin, gunakan kalimat lengkap. HC akan menilai berdasarkan rubrik internal.</p>
</div>
```

- [ ] **Step 3.7: Tambah 2 FAQ baru**

File: `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`, di dalam block `#faq` (sekarang heading `19. FAQ (Pertanyaan Umum)`).

Cari FAQ terakhir (`<span class="step-title">Kenapa sertifikat tidak muncul?</span>` di line ~613). Tambah 2 FAQ baru setelah block FAQ terakhir tersebut, sebelum `</main>` atau tag penutup body.

Cari pattern existing FAQ untuk konsistensi struktur. Contoh:
```html
<div class="step">
  <div class="step-number">5</div>
  <div class="step-content">
    <span class="step-title">Kenapa sertifikat tidak muncul?</span>
    <p>Sertifikat hanya digenerate kalau...</p>
  </div>
</div>
```

Tambah 2 FAQ baru (number lanjut 6 & 7):
```html
<div class="step">
  <div class="step-number">6</div>
  <div class="step-content">
    <span class="step-title">Kapan hasil Essay keluar?</span>
    <p>Untuk soal tipe <strong>Essay</strong>, hasil tidak instan karena HC menilai manual. Tunggu notifikasi atau cek halaman <em>Riwayat Ujian</em> setelah beberapa hari kerja. Kalau sudah seminggu belum ada update, hubungi HC.</p>
  </div>
</div>

<div class="step">
  <div class="step-number">7</div>
  <div class="step-content">
    <span class="step-title">Kenapa paket soal Post-Test sama dengan Pre-Test?</span>
    <p>Ini <strong>sengaja</strong> — HC mengaktifkan setting <em>SamePackage</em> supaya perbandingan nilai Pre vs Post apple-to-apple. Peningkatan Anda murni dari penguasaan materi, bukan dari sulit/mudahnya soal. Anda <em>tidak perlu</em> menghafal soal Pre-Test — fokus pada materi pelatihan.</p>
  </div>
</div>
```

- [ ] **Step 3.8: Buka file di browser, verifikasi visual**

```bash
dotnet run
```

Login `admin@pertamina.com`. Buka:
- `http://localhost:5277/Home/GuideDetail?module=cmp` — klik tombol Lihat di kartu PDF "Panduan Lengkap Assessment".
- PDF terbuka tab baru.

Cek:
- TOC 19 entry, klik tiap link → scroll ke section yang benar (anchor kerja).
- Sec 2 "Tipe-tipe Assessment" render dengan 3 sub-section (Pre/Post/Regular).
- Sec 3 "Pre-Test vs Post-Test" render dengan table mode SamePackage + callout.
- Sec 8 "Memulai Ujian" punya callout Extra Time di akhir.
- Sec 9 "Mengerjakan Soal" punya subsection "Tipe Soal" dengan table MC/MA/Essay.
- Sec 19 FAQ punya 7 entry (5 existing + 2 baru).
- Tidak ada styling broken, semua heading nomor benar 1→19.

Stop server.

- [ ] **Step 3.9: Commit**

```bash
git add wwwroot/documents/guides/Panduan-Lengkap-Assessment.html
git commit -m "$(cat <<'EOF'
docs(guide-pdf-coachee): refresh konten assessment Mei 2026

- Insert sec 2 Tipe Assessment (Pre/Post/Regular, no Proton mention).
- Insert sec 3 Pre-Test vs Post-Test (SamePackage on/off mode).
- Augment sec 8 Memulai Ujian: callout Extra Time notice.
- Augment sec 9 Mengerjakan Soal: subsection Tipe Soal (MC/MA/Essay
  table + tip Essay).
- FAQ +2 Q: kapan hasil Essay keluar, kenapa paket Post-Test sama Pre.
- Renumber heading 2-17 -> 4-19. Anchor #step1..#step13 + #tips + #faq
  PRESERVED (bookmark eksternal tetap kerja).
- Version bump 1.1 Maret -> 1.2 Mei 2026.

Spec: docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Refresh PDF Admin (`Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`)

**Files:**
- Modify: `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` (268 baris → est ~700 baris)

**Strategy:** Letter rescheme A→J. Insert section A baru (Manage Kategori) sebelum existing A (Buat Assessment, jadi B). Augment B-E dengan field baru. Insert F-J di akhir sebelum Tips.

- [ ] **Step 4.1: Bump version**

File: `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`, line ~70.

Sebelum:
```html
<p class="version">Versi 1.0 &bull; Maret 2026</p>
```

Sesudah:
```html
<p class="version">Versi 2.0 &bull; Mei 2026 &bull; Coverage Phase 195-302</p>
```

- [ ] **Step 4.2: Update Alur Proses + tambah box prerequisite**

File: `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`, line ~74-86.

Sebelum:
```html
<h2>Alur Proses</h2>

<div class="flow">
  <div class="flow-box">1. Buat<br>Assessment</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">2. Buat<br>Paket Soal</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">3. Import<br>Soal</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">4. Preview<br>&amp; Verifikasi</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">5. Peserta<br>Mengerjakan</div>
</div>
```

Sesudah:
```html
<h2>Alur Proses</h2>

<div class="flow">
  <div class="flow-box">0. Setup<br>Kategori</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">1. Buat<br>Assessment</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">2. Buat<br>Paket Soal</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">3. Import<br>Soal</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">4. Preview<br>&amp; Verifikasi</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">5. Peserta<br>Mengerjakan</div>
  <span class="flow-arrow">&#10132;</span>
  <div class="flow-box">6. Monitoring<br>&amp; Operasional</div>
</div>

<div class="card-orange card">
  <div class="card-title">&#9888; Prerequisite</div>
  Sebelum membuat assessment, pastikan <strong>kategori</strong> sudah ada di sistem. Lihat <strong>Bagian A. Manage Kategori Assessment</strong>. Kalau kategori baru, buat dulu — wizard Buat Assessment hanya bisa pilih dari kategori yang sudah terdaftar.
</div>
```

- [ ] **Step 4.3: Insert Bagian A (Manage Kategori Assessment) — NEW**

File: `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`, line ~88 (sebelum `<h2>A. Buat Assessment Baru</h2>`).

Insert block:
```html
<!-- ============================================================ -->
<!-- BAGIAN A: MANAGE KATEGORI -->
<!-- ============================================================ -->
<h2>A. Manage Kategori Assessment</h2>

<p><strong>Navigasi:</strong> Admin Panel &rarr; Manage Kategori Assessment</p>

<p>Halaman ini punya CRUD penuh untuk kategori assessment. Sebelum buat sesi assessment, kategori target harus sudah ada.</p>

<h3>A.1 Tambah Parent Category</h3>
<ol>
  <li>Klik <strong>Tambah Kategori</strong></li>
  <li>Isi <strong>Nama Kategori</strong> (contoh: "Assessment OJ", "Mandatory HSSE Training", "Sertifikasi Operator RFCC")</li>
  <li>Isi <strong>Pass Percentage Default</strong> (nilai minimum lulus untuk sesi di kategori ini; bisa di-override per sesi)</li>
  <li>Pilih <strong>Signatory User</strong> dari dropdown (pekerja yang akan menandatangani sertifikat hasil — biasanya manager unit terkait)</li>
  <li>Kosongkan <strong>Parent Category</strong> (parent = top-level)</li>
  <li>Klik <strong>Simpan</strong></li>
</ol>

<h3>A.2 Tambah Sub-Kategori</h3>
<ol>
  <li>Klik <strong>Tambah Kategori</strong></li>
  <li>Isi nama (contoh: "OJ - Operator", "OJ - Panelman")</li>
  <li>Pilih <strong>Parent Category</strong> dari dropdown (mis: "Assessment OJ")</li>
  <li>Signatory: <strong>kosongkan</strong> jika inherit parent, atau set sendiri jika override</li>
  <li>Klik <strong>Simpan</strong></li>
</ol>

<div class="card-blue card">
  <div class="card-title">&#8505; Hierarki Sub-Kategori</div>
  Sistem mendukung 2 level (parent + child). Sub-kategori inherit signatory parent kalau field signatory dikosongkan. Berguna kalau satu rumpun kategori punya 1 manager yang TTD semua sub-nya.
</div>

<h3>A.3 Edit / Hapus Kategori</h3>
<ul>
  <li><strong>Edit:</strong> klik ikon pensil di baris kategori. Ubah nama, signatory, pass percentage. Klik Simpan.</li>
  <li><strong>Hapus:</strong> klik ikon sampah. Sistem cek apakah ada sesi assessment aktif yang masih pakai kategori ini. Kalau ada, hapus di-block dan muncul daftar sesi yang menghalangi — selesaikan dulu sesi tersebut atau pindahkan ke kategori lain sebelum hapus.</li>
</ul>

<div class="card-red card">
  <div class="card-title">&#9888; Hati-hati Hapus</div>
  Kategori yang sudah dipakai sesi assessment (aktif maupun arsip) berdampak ke histori records peserta. Lebih aman <strong>rename</strong> daripada hapus, kecuali kategori belum pernah dipakai sama sekali.
</div>
```

- [ ] **Step 4.4: Rename existing "A. Buat Assessment Baru" → "B."**

File: `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`, line ~89.

Sebelum:
```html
<h2>A. Buat Assessment Baru</h2>
```

Sesudah:
```html
<h2>B. Buat Assessment Baru</h2>
```

- [ ] **Step 4.5: Augment Bagian B Langkah 3 Settings — tambah field baru**

File: cari table di Langkah 3 (line ~133-143). Tambah row baru ke table existing.

Sebelum (penutup table existing):
```html
<table>
  <tr><th>Field</th><th>Keterangan</th></tr>
  <tr><td>Tanggal &amp; Waktu Jadwal</td><td>Kapan assessment dilaksanakan</td></tr>
  <tr><td>Durasi</td><td>Waktu pengerjaan (menit), maks 480 menit</td></tr>
  <tr><td>Status</td><td><strong>Open</strong> = langsung tersedia, <strong>Upcoming</strong> = belum dibuka</td></tr>
  <tr><td>Pass Percentage</td><td>Nilai minimum lulus (0&ndash;100%), otomatis terisi dari kategori</td></tr>
  <tr><td>Security Token</td><td>Opsional. Aktifkan toggle &rarr; klik <strong>Generate</strong> untuk buat token 6 digit</td></tr>
  <tr><td>Batas Waktu</td><td>Opsional. Tanggal terakhir bisa mengerjakan</td></tr>
  <tr><td>Generate Certificate</td><td>Aktifkan agar peserta lulus bisa cetak sertifikat</td></tr>
  <tr><td>Allow Answer Review</td><td>Aktifkan agar peserta bisa review jawaban setelah selesai</td></tr>
</table>
```

Sesudah (tambah 4 row baru di akhir table):
```html
<table>
  <tr><th>Field</th><th>Keterangan</th></tr>
  <tr><td>Tanggal &amp; Waktu Jadwal</td><td>Kapan assessment dilaksanakan</td></tr>
  <tr><td>Durasi</td><td>Waktu pengerjaan (menit), maks 480 menit</td></tr>
  <tr><td>Status</td><td><strong>Open</strong> = langsung tersedia, <strong>Upcoming</strong> = belum dibuka</td></tr>
  <tr><td>Pass Percentage</td><td>Nilai minimum lulus (0&ndash;100%), otomatis terisi dari kategori</td></tr>
  <tr><td>Security Token</td><td>Opsional. Aktifkan toggle &rarr; klik <strong>Generate</strong> untuk buat token 6 digit (shared per batch peserta)</td></tr>
  <tr><td>Batas Waktu</td><td>Opsional. Tanggal terakhir bisa mengerjakan</td></tr>
  <tr><td>Generate Certificate</td><td>Aktifkan agar peserta lulus bisa cetak sertifikat</td></tr>
  <tr><td>Allow Answer Review</td><td>Aktifkan agar peserta bisa review jawaban setelah selesai</td></tr>
  <tr><td><strong>Tipe Assessment</strong></td><td>Pilih: <em>PreTest</em>, <em>PostTest</em>, atau kosong (Regular). Mempengaruhi linking + Gain Score.</td></tr>
  <tr><td><strong>Linked Pre-Test Session</strong></td><td>Hanya muncul kalau Tipe = PostTest. Pilih sesi Pre yang dipasangkan (<code>LinkedSessionId</code>). Sistem hitung Gain Score otomatis.</td></tr>
  <tr><td><strong>SamePackage</strong></td><td>Checkbox. ON = paket soal Post identik dengan Pre (apple-to-apple). OFF = paket berbeda tapi cakupan sama.</td></tr>
  <tr><td><strong>Extra Time (menit)</strong></td><td>Opsional. Tambahan waktu per sesi (aksesibilitas). Bisa di-override per peserta. Field <code>ExtraTimeMinutes</code> (Phase 302).</td></tr>
  <tr><td><strong>Renews Session ID</strong></td><td>Opsional. Untuk perpanjangan sertifikat — link ke sesi lama (<code>RenewsSessionId</code>). Lihat Bagian I.</td></tr>
</table>
```

- [ ] **Step 4.6: Rename existing "B. Buat Paket Soal" → "C."**

File line ~165 (existing line, akan bergeser karena insert sebelumnya).

Sebelum:
```html
<h2>B. Buat Paket Soal</h2>
```

Sesudah:
```html
<h2>C. Buat Paket Soal</h2>
```

- [ ] **Step 4.7: Augment C. Buat Paket Soal — tambah multi-package note**

Cari paragraf pembuka section C (sebelumnya B). Tambah callout setelah paragraf pertama:

Tambah block:
```html
<div class="card-blue card">
  <div class="card-title">&#8505; Multi-Package per Sesi</div>
  Setiap sesi assessment bisa punya <strong>lebih dari satu paket soal</strong> (Paket A, Paket B, Paket C — field <code>PackageNumber</code>). Berguna untuk anti-contek: peserta yang duduk bersebelahan dapat paket berbeda. Tambah paket via tombol <strong>Tambah Paket Baru</strong> di halaman Manage Questions sesi.
</div>
```

- [ ] **Step 4.8: Rename "C. Import Soal" → "D."**

```html
<h2>D. Import Soal</h2>
```

- [ ] **Step 4.9: Augment D. Import Soal — update format kolom Excel**

Cari subsection `<h3>Format Kolom Excel</h3>` (line ~191). Update table di bawahnya.

Cari table existing format Excel. Tambah row untuk kolom baru:

Sebelum (asumsi struktur — verify dulu via Read):
```html
<h3>Format Kolom Excel</h3>
<table>
  <tr><th>Kolom</th><th>Keterangan</th><th>Wajib?</th></tr>
  <!-- existing rows... -->
</table>
```

Tambah row baru di table:
```html
  <tr><td><strong>QuestionType</strong></td><td>Tipe soal: <code>MC</code> (Multiple Choice, default), <code>MA</code> (Multiple Answer), <code>Essay</code></td><td>Opsional (default MC)</td></tr>
  <tr><td><strong>Rubrik</strong></td><td>Khusus Essay — kunci jawaban referensi internal HC (tidak ditampilkan ke peserta)</td><td>Opsional (Essay only)</td></tr>
  <tr><td><strong>MaxCharacters</strong></td><td>Khusus Essay — batas karakter jawaban (default 2000)</td><td>Opsional (Essay only)</td></tr>
```

Tambah catatan setelah table:
```html
<div class="card-orange card">
  <div class="card-title">&#9888; Format CorrectAnswer untuk MA</div>
  Untuk soal <strong>Multiple Answer</strong>, isi kolom CorrectAnswer dengan huruf opsi benar dipisah <strong>koma tanpa spasi</strong> (contoh: <code>A,C,D</code>). Sistem akan validasi peserta harus pilih ketiga opsi tersebut untuk dianggap betul.
</div>
```

- [ ] **Step 4.10: Rename "D. Preview & Verifikasi Soal" → "E."**

```html
<h2>E. Preview &amp; Verifikasi Soal</h2>
```

- [ ] **Step 4.11: Augment E. Preview & Verifikasi — tambah note Essay/MA**

Tambah callout di akhir section E:
```html
<div class="card-blue card">
  <div class="card-title">&#8505; Preview untuk Essay &amp; MA</div>
  <ul>
    <li><strong>Essay:</strong> preview tampilkan rubrik internal di samping textarea jawaban. Pastikan rubrik jelas — HC lain yang grade nanti pakai rubrik ini sebagai referensi.</li>
    <li><strong>Multiple Answer:</strong> preview tampilkan semua opsi yang ditandai sebagai benar (di-highlight). Cek tidak ada yang lupa ditandai.</li>
  </ul>
</div>
```

- [ ] **Step 4.12: Insert Bagian F. Tipe Soal Detail — NEW**

File: setelah block E. Preview & Verifikasi penuh, sebelum `<h2>Tips Penting</h2>`.

Insert block:
```html
<!-- ============================================================ -->
<!-- BAGIAN F: TIPE SOAL DETAIL -->
<!-- ============================================================ -->
<h2>F. Tipe Soal Detail (MC, MA, Essay)</h2>

<p>Sistem mendukung tiga tipe soal. Pilih tipe saat input soal via Excel kolom <code>QuestionType</code> atau form manual.</p>

<h3>F.1 Multiple Choice (MC)</h3>
<ul>
  <li>Default semua soal lama (null = MC untuk backward compat).</li>
  <li>Satu jawaban benar dari 4&ndash;5 opsi (radio button di UI peserta).</li>
  <li>Auto-grading instan saat peserta submit.</li>
  <li>Format Excel CorrectAnswer: huruf opsi benar (mis: <code>B</code>).</li>
</ul>

<h3>F.2 Multiple Answer (MA)</h3>
<ul>
  <li>Lebih dari satu jawaban benar (checkbox di UI peserta).</li>
  <li>Peserta harus pilih <strong>semua</strong> opsi benar untuk dianggap betul. Kurang satu atau lebih satu = salah (binary scoring).</li>
  <li>Auto-grading instan.</li>
  <li>Format Excel CorrectAnswer: huruf opsi dipisah koma tanpa spasi (mis: <code>A,C,D</code>).</li>
  <li>Feature ditambahkan Phase 296.</li>
</ul>

<h3>F.3 Essay</h3>
<ul>
  <li>Jawaban teks bebas (textarea di UI peserta).</li>
  <li>Field tambahan saat input soal:
    <ul>
      <li><strong>Rubrik</strong> &mdash; kunci jawaban referensi internal HC (TIDAK ditampilkan ke peserta).</li>
      <li><strong>MaxCharacters</strong> &mdash; batas karakter jawaban (default 2000).</li>
    </ul>
  </li>
  <li><strong>Grading manual oleh HC</strong> &mdash; hasil tidak instan. Peserta lihat status "Menunggu Penilaian" di halaman hasil sampai HC selesai grade.</li>
  <li>Workflow grading: HC buka sesi → tab <em>Essay Grading</em> → baca jawaban peserta → bandingkan dengan rubrik → input nilai (0&ndash;100% atau Pass/Fail tergantung config).</li>
</ul>
```

- [ ] **Step 4.13: Insert Bagian G. Manual Entry Sertifikat — NEW**

```html
<!-- ============================================================ -->
<!-- BAGIAN G: MANUAL ENTRY -->
<!-- ============================================================ -->
<h2>G. Manual Entry Sertifikat</h2>

<p>Manual Entry adalah <strong>jalur alternatif</strong> untuk catat sertifikat peserta yang sudah punya hasil dari lembaga eksternal &mdash; tanpa harus bikin ujian online.</p>

<h3>G.1 Kapan Pakai Manual Entry</h3>
<ul>
  <li>Peserta sudah punya sertifikat sertifikasi dari BNSP, LSP, atau lembaga eksternal lain.</li>
  <li>Pelatihan di-host vendor luar (mis: Schneider Electric, Honeywell) yang punya sistem ujian sendiri.</li>
  <li>Historical record &mdash; input sertifikat lama yang belum tercatat di sistem.</li>
</ul>

<h3>G.2 Cara Buat Manual Entry</h3>
<ol>
  <li>Buat assessment baru seperti biasa (Bagian B)</li>
  <li>Di Langkah Settings, aktifkan toggle <strong>Manual Entry</strong> (field <code>IsManualEntry=true</code>)</li>
  <li>Field tambahan muncul:
    <table>
      <tr><th>Field</th><th>Keterangan</th></tr>
      <tr><td><strong>Penyelenggara</strong></td><td>Nama lembaga (mis: "BNSP", "PT Schneider Electric")</td></tr>
      <tr><td><strong>Kota</strong></td><td>Kota pelaksanaan pelatihan/ujian</td></tr>
      <tr><td><strong>SubKategori</strong></td><td>Sub-kategori spesifik (mis: "Operator Listrik Tegangan Menengah")</td></tr>
      <tr><td><strong>CertificateType</strong></td><td>Pilih: <em>Kompetensi</em> (BNSP/LSP), <em>Profesi</em> (asosiasi profesi), <em>Pelatihan</em> (sertifikat partisipasi)</td></tr>
    </table>
  </li>
  <li>Pilih peserta (1 orang per Manual Entry &mdash; biasanya tidak batch)</li>
  <li>Upload file sertifikat (PDF/JPG) sebagai bukti</li>
  <li>Input nilai/status langsung tanpa peserta ujian online</li>
  <li>Submit &mdash; sertifikat masuk records peserta seperti hasil ujian online</li>
</ol>

<div class="card-blue card">
  <div class="card-title">&#8505; Beda Manual Entry vs Regular</div>
  Manual Entry tidak generate sesi ujian online. Peserta tidak akan lihat sesi ini di tab Assessment-nya &mdash; sertifikat langsung muncul di tab <em>Riwayat</em> dan <em>Sertifikat</em>.
</div>
```

- [ ] **Step 4.14: Insert Bagian H. Operasional Sesi Berjalan — NEW**

```html
<!-- ============================================================ -->
<!-- BAGIAN H: OPERASIONAL SESI -->
<!-- ============================================================ -->
<h2>H. Operasional Sesi Berjalan</h2>

<p>Fitur untuk monitor &amp; intervensi sesi assessment yang sedang berjalan atau sudah selesai.</p>

<h3>H.1 Real-Time Monitoring (SignalR)</h3>
<ul>
  <li>Buka <strong>Admin Monitoring</strong> untuk sesi tertentu</li>
  <li>Live dashboard tampilkan: siapa yang sudah masuk, soal ke berapa, sisa waktu, jumlah jawaban terisi</li>
  <li>Update otomatis tanpa refresh (via <code>AssessmentHub</code> SignalR)</li>
  <li>Bisa kirim notifikasi broadcast ke semua peserta sesi (mis: "Soal nomor 7 typo, jawab sesuai pemahaman terbaik")</li>
</ul>

<h3>H.2 Reshuffle Package</h3>
<ul>
  <li>Kalau perlu ulang acak urutan opsi (mis: ada peserta yang bocor jawaban), klik <strong>Reshuffle</strong> di halaman package</li>
  <li><strong>Reshuffle Per-Sesi:</strong> acak ulang untuk semua peserta di 1 sesi</li>
  <li><strong>Reshuffle Bulk:</strong> acak ulang untuk semua sesi yang pakai paket ini</li>
  <li>Hanya display A/B/C/D yang random &mdash; grading tetap konsisten via <code>PackageOption.Id</code> di backend</li>
</ul>

<h3>H.3 Akhiri Ujian</h3>
<ul>
  <li><strong>AkhiriUjian</strong> (per peserta): paksa submit semua jawaban yang sudah ada untuk 1 peserta. Berguna kalau peserta lupa submit atau koneksi peserta putus.</li>
  <li><strong>AkhiriSemuaUjian</strong> (bulk): paksa submit semua peserta sesi sekaligus. Berguna untuk keadaan darurat atau waktu habis sistemik.</li>
  <li>Hasil tetap auto-graded (kecuali Essay yang masih perlu HC nilai manual)</li>
</ul>

<h3>H.4 Edit Jawaban Peserta</h3>
<ul>
  <li>Setelah ujian selesai, Admin bisa <strong>override jawaban peserta</strong> via halaman Detail Sesi → klik nama peserta → edit jawaban per soal</li>
  <li>Use case: koreksi typo sistem, adjudikasi soal yang ambigu, koreksi grading Essay</li>
  <li><strong>Setiap edit di-log otomatis</strong> ke tabel <code>AssessmentEditLog</code> dengan: timestamp, user yang edit, nilai lama, nilai baru, alasan (kalau diisi)</li>
  <li>Audit trail lengkap &mdash; lihat Bagian J. Audit Log</li>
</ul>

<div class="card-red card">
  <div class="card-title">&#9888; Hati-hati Edit Jawaban</div>
  Edit jawaban post-submit adalah fitur sensitif &mdash; bisa mengubah nilai/kelulusan peserta. Gunakan hanya untuk koreksi yang benar-benar perlu. Selalu isi alasan di field <em>Reason</em> untuk audit trail.
</div>
```

- [ ] **Step 4.15: Insert Bagian I. Renewal Chain — NEW**

```html
<!-- ============================================================ -->
<!-- BAGIAN I: RENEWAL CHAIN -->
<!-- ============================================================ -->
<h2>I. Renewal Chain (Perpanjangan Sertifikat)</h2>

<p>Untuk sertifikat yang perlu diperbarui berkala (mis: lisensi tahunan, sertifikasi yang expire), gunakan <strong>Renewal Chain</strong> untuk link sertifikat lama ke baru.</p>

<h3>I.1 Cara Buat Renewal</h3>
<ol>
  <li>Buat sesi assessment baru seperti biasa (Bagian B)</li>
  <li>Di Langkah Settings, isi <strong>Renews Session ID</strong> (<code>RenewsSessionId</code>) dengan ID sesi sertifikat lama yang diperbarui</li>
  <li>Atau, isi <strong>Renews Training ID</strong> (<code>RenewsTrainingId</code>) kalau yang diperbarui adalah training record (bukan sesi assessment)</li>
  <li>Submit dan lanjut flow normal</li>
</ol>

<h3>I.2 Dampak di Sistem</h3>
<ul>
  <li>Sertifikat baru link ke sertifikat lama &mdash; histori chain perpanjangan tersusun otomatis</li>
  <li>Peserta lihat semua sertifikat di tab Riwayat, dengan marker <em>Perpanjangan dari [sesi lama]</em></li>
  <li>Sertifikat lama tetap valid sampai tanggal expire-nya, sertifikat baru ambil alih setelahnya</li>
  <li>Laporan compliance bisa difilter berdasarkan chain (mis: "Pekerja yang sertifikatnya akan expire 3 bulan ke depan")</li>
</ul>

<div class="card-blue card">
  <div class="card-title">&#8505; Use Case Renewal</div>
  Cocok untuk: lisensi K3, sertifikasi BNSP yang berlaku 3 tahun, sertifikat OPS yang perlu refresher tahunan. Feature ditambahkan Phase 200.
</div>
```

- [ ] **Step 4.16: Insert Bagian J. Audit Log — NEW**

```html
<!-- ============================================================ -->
<!-- BAGIAN J: AUDIT LOG -->
<!-- ============================================================ -->
<h2>J. Audit Log</h2>

<p>Sistem mencatat <strong>semua perubahan sensitif</strong> di assessment untuk audit trail &amp; akuntabilitas.</p>

<h3>J.1 AssessmentEditLog (Edit Jawaban Peserta)</h3>
<ul>
  <li>Setiap kali Admin edit jawaban peserta post-submit, log otomatis dengan:
    <ul>
      <li>Timestamp (tanggal + jam edit)</li>
      <li>User yang edit (email + nama Admin)</li>
      <li>Soal nomor yang diedit</li>
      <li>Nilai lama vs nilai baru</li>
      <li>Alasan (kalau diisi)</li>
    </ul>
  </li>
  <li>Akses: halaman Detail Sesi &rarr; tab <strong>Edit Log</strong></li>
</ul>

<h3>J.2 Audit Log Umum</h3>
<ul>
  <li>Akses route terpisah: <strong>Admin Panel &rarr; Audit Log</strong></li>
  <li>Catatan aksi sensitif lain: create/delete sesi assessment, change kategori, change signatory, reshuffle, akhiri ujian bulk</li>
  <li>Filter by: user, tanggal, tipe aksi, sesi target</li>
  <li>Export Excel untuk laporan compliance / audit eksternal</li>
</ul>

<div class="card-orange card">
  <div class="card-title">&#9888; Retention Policy</div>
  Audit log <strong>tidak boleh dihapus</strong> via UI. Retention permanen untuk kebutuhan audit internal/eksternal. Kalau ada constraint storage, koordinasi dengan Team IT.
</div>
```

- [ ] **Step 4.17: Update Tips Penting — tambah tips Essay + signatory inheritance**

File: cari section `<h2>Tips Penting</h2>` (sekarang setelah Bagian J). Tambah 2 tip baru di list:

Cari list existing tips. Tambah di akhir:
```html
<li><strong>Essay grading workflow:</strong> selesaikan grading Essay dalam 5 hari kerja setelah ujian. Peserta yang menunggu lama bakal complain ke HC. Bagi grading antar HC kalau sesi besar (50+ peserta) supaya tidak bottleneck.</li>
<li><strong>Signatory inheritance:</strong> kalau seluruh sub-kategori di satu rumpun pakai signatory yang sama, kosongkan signatory di sub-kategori &mdash; inherit dari parent. Lebih maintainable: ganti signatory di parent = semua sub ikut ganti.</li>
<li><strong>Backup sebelum Reshuffle Bulk:</strong> reshuffle bulk affect semua sesi yang pakai paket ini, termasuk yang sudah selesai. Verify dampak sebelum klik &mdash; preferensi pakai reshuffle per-sesi untuk safety.</li>
```

- [ ] **Step 4.18: Browser verify visual**

```bash
dotnet run
```

Login `admin@pertamina.com`. Buka `http://localhost:5277/Home/GuideDetail?module=cmp` → klik tombol Lihat di kartu PDF "Panduan Buat Assessment & Input Soal".

Cek:
- Cover version "Versi 2.0 • Mei 2026 • Coverage Phase 195-302"
- Alur Proses ada 6 box (Setup Kategori → ... → Monitoring) + callout prerequisite
- Bagian **A. Manage Kategori Assessment** muncul, 3 subsection (A.1 Parent, A.2 Sub, A.3 Edit/Hapus)
- Bagian **B-E** existing dengan field tambahan (Pre-Post, Extra Time, multi-package, QuestionType/Rubrik/MaxCharacters)
- Bagian **F. Tipe Soal Detail** dengan F.1 MC, F.2 MA, F.3 Essay
- Bagian **G. Manual Entry Sertifikat** dengan field table
- Bagian **H. Operasional Sesi** dengan H.1-H.4 (Monitoring, Reshuffle, Akhiri Ujian, Edit Jawaban)
- Bagian **I. Renewal Chain**
- Bagian **J. Audit Log** dengan J.1 + J.2
- Tips Penting bertambah 3 item baru
- Tidak ada styling broken

Stop server.

- [ ] **Step 4.19: Commit**

```bash
git add wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html
git commit -m "$(cat <<'EOF'
docs(guide-pdf-admin): refresh komprehensif coverage Phase 195-302

Letter rescheme A->J. Existing 5 section -> 11 section.

INSERT BARU:
- A. Manage Kategori Assessment (Phase 195): CRUD + signatory + sub-kategori
- F. Tipe Soal Detail: MC, MA (Phase 296), Essay + rubrik + grading manual
- G. Manual Entry Sertifikat: jalur tanpa ujian online, field Penyelenggara/Kota/CertificateType
- H. Operasional Sesi: SignalR monitoring, Reshuffle, Akhiri Ujian, Edit Jawaban + AssessmentEditLog
- I. Renewal Chain (Phase 200): RenewsSessionId, RenewsTrainingId
- J. Audit Log: AssessmentEditLog + audit log umum + retention policy

UPDATE EXISTING:
- Alur Proses: tambah box "Setup Kategori" + "Monitoring & Operasional"
- B. Buat Assessment Langkah 3 Settings: +Tipe Assessment, LinkedSessionId, SamePackage, ExtraTimeMinutes (Phase 302), RenewsSessionId
- C. Buat Paket Soal: callout multi-package per sesi
- D. Import Soal Format Excel: kolom QuestionType, Rubrik, MaxCharacters + format MA CorrectAnswer
- E. Preview & Verifikasi: callout Essay grading + MA highlight
- Tips Penting: +Essay grading workflow, signatory inheritance, backup sebelum Reshuffle Bulk

Version bump 1.0 Maret -> 2.0 Mei 2026.

Spec: docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Playwright E2E Verification

**Files:**
- Create: `tests/e2e/cmp-guide.spec.ts`

**Note:** Tests Playwright untuk lock-in regression coverage. Kalau di masa depan ada yang ubah PDF/accordion config, test akan catch role-gating breakage.

- [ ] **Step 5.1: Write failing test file**

Create `tests/e2e/cmp-guide.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';

test.describe('CMP Guide Page - PDF Card & Accordion Role-Gating', () => {

  test('5.1 - Admin sees 2 PDF cards on /Home/GuideDetail?module=cmp', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(2);

    // Verify content of both cards
    await expect(pdfCards.nth(0)).toContainText('Panduan Lengkap Assessment');
    await expect(pdfCards.nth(1)).toContainText('Panduan Buat Assessment');
  });

  test('5.2 - Coachee sees only 1 PDF card on /Home/GuideDetail?module=cmp', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(1);
    await expect(pdfCards.first()).toContainText('Panduan Lengkap Assessment');
    await expect(pdfCards.first()).not.toContainText('Panduan Buat Assessment');
  });

  test('5.3 - Admin sees 12 accordion items on CMP page', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(12);
  });

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

  test('5.5 - Data module no longer has PDF card (admin)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=data');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(0);
  });

  test('5.6 - Admin can open PDF coachee in new tab', async ({ page, context }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const coacheePdfCard = page.locator('.guide-tutorial-card', { hasText: 'Panduan Lengkap Assessment' });
    const lihatLink = coacheePdfCard.locator('a:has-text("Lihat")');

    await expect(lihatLink).toHaveAttribute('target', '_blank');
    const href = await lihatLink.getAttribute('href');
    expect(href).toContain('/documents/guides/Panduan-Lengkap-Assessment.html');
  });

  test('5.7 - Admin can open PDF admin in new tab', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const adminPdfCard = page.locator('.guide-tutorial-card', { hasText: 'Panduan Buat Assessment' });
    const lihatLink = adminPdfCard.locator('a:has-text("Lihat")');

    await expect(lihatLink).toHaveAttribute('target', '_blank');
    const href = await lihatLink.getAttribute('href');
    expect(href).toContain('/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html');
  });

  test('5.8 - Accordion Acc-5 expands to show 3 steps (Pre/Post/Regular)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const acc5 = page.locator('.accordion-item', { hasText: 'Tipe-tipe Assessment' });
    await acc5.locator('button.accordion-button').click();

    // Wait for collapse animation
    await page.waitForTimeout(500);

    const body = acc5.locator('.accordion-collapse.show');
    await expect(body).toContainText('Pre-Test');
    await expect(body).toContainText('Post-Test');
    await expect(body).toContainText('Regular Assessment');
  });

});
```

- [ ] **Step 5.2: Run test to verify pass**

Pastikan dev server jalan di port lain (atau Playwright config sudah handle webServer):

```bash
cd tests
npx playwright test cmp-guide.spec.ts --reporter=line
```

Expected: 8 passed (5.1 sampai 5.8). Kalau ada yang fail, investigate:
- Login helper return error → verify `accounts.ts` punya entry `admin` + `coachee` (sudah ada per inspection)
- Selector mismatch → buka browser via `--headed` flag, lihat DOM struktur asli
- Count mismatch → konfirmasi Task 1, 2 commit sudah merged + server di-restart

- [ ] **Step 5.3: Commit**

```bash
git add tests/e2e/cmp-guide.spec.ts
git commit -m "$(cat <<'EOF'
test(e2e): cmp-guide PDF card + accordion role-gating coverage

8 test untuk lock-in regression:
- Admin lihat 2 PDF card, Coachee lihat 1 PDF card.
- Admin lihat 12 accordion, Coachee lihat 7 accordion.
- Data module no longer punya PDF (PDF admin pindah ke CMP).
- Admin bisa akses kedua PDF via tombol Lihat (target=_blank, href correct).
- Acc-5 Tipe Assessment expand show 3 step (Pre/Post/Regular).

Coverage role-gating sehingga future change ke PDF/accordion config
auto-detect kalau ada accidental leak ke role yang gak boleh lihat.

Spec: docs/superpowers/specs/2026-05-23-update-cmp-guide-assessment-content-design.md

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Manual UAT Checklist

**Tujuan:** Final visual + behavioral verification oleh user (Bapak Rino) sebelum mark plan complete + push.

**Tidak ada commit** — output checklist tertulis.

- [ ] **Step 6.1: Jalankan server lokal**

```bash
dotnet run
```

- [ ] **Step 6.2: Login admin@pertamina.com / 123456**

Buka `http://localhost:5277/Home/GuideDetail?module=cmp`. Centang manual:

- [ ] 2 PDF card render side-by-side / stacked tanpa overlap atau narrow yang aneh
- [ ] Kedua PDF card punya color theme `--cmp` (biru) konsisten
- [ ] 12 accordion render readable (font, spacing OK)
- [ ] Klik tiap accordion baru (Acc-5 sampai Acc-10) → step content render dengan HTML formatting `<b>`, `<i>`, `<code>` work
- [ ] Klik tombol Lihat di PDF coachee → buka tab baru, render PDF 19 section, TOC link kerja
- [ ] Klik tombol Lihat di PDF admin → buka tab baru, render PDF 11 section (A-J + Tips), TOC kalau ada link kerja
- [ ] Klik tombol Download di kedua PDF → file ter-download

- [ ] **Step 6.3: Buka /Home/GuideDetail?module=data sebagai admin**

- [ ] Tidak ada PDF card (sudah pindah ke CMP)
- [ ] Accordion existing untuk Data module masih render normal

- [ ] **Step 6.4: Logout, login sebagai coachee (rino.prasetyo@pertamina.com / 123456)**

Buka `http://localhost:5277/Home/GuideDetail?module=cmp`:
- [ ] Hanya 1 PDF card ("Panduan Lengkap Assessment")
- [ ] 7 accordion (6 existing + Acc-5 "Tipe-tipe Assessment")
- [ ] Tidak ada accordion AdminHC (Acc-6 sampai Acc-10 invisible)
- [ ] Klik Lihat PDF coachee → render OK

- [ ] **Step 6.5: Logout, test untuk role lain — Coach (rustam.nugroho@pertamina.com / 123456)**

- [ ] /GuideDetail?module=cmp → 1 PDF + accordion sesuai role Coach
- [ ] Acc-5 visible (role All)
- [ ] Acc-6..10 invisible

- [ ] **Step 6.6: Atasan (taufik.hartopo@pertamina.com / 123456)**

- [ ] /GuideDetail?module=cmp → 1 PDF + accordion + Acc-5 visible

- [ ] **Step 6.7: Konfirmasi pak Rino approve**

Tampilkan checklist di chat. Tunggu pak Rino "OK approve" sebelum push.

- [ ] **Step 6.8: Push ke origin/main**

```bash
git log --oneline -5
git push origin main
```

Verify 5 commit terkirim (Task 1-5).

- [ ] **Step 6.9: Notify Team IT untuk promo Dev**

Per `CLAUDE.md`:
- Sertakan commit hash (5 commit terbaru)
- Flag NO migration (refactor + content only, no DB schema change)
- Test URL Dev setelah promo: `http://10.55.3.3/KPB-PortalHC/Home/GuideDetail?module=cmp`

---

## Self-Review Checklist (post-write)

- [ ] **Spec coverage:** semua 9 row di spec "Draft plan execution" table tercover:
  - Pindah Module Data→Cmp → Task 1 Step 1.3 ✅
  - Refactor GetPdf → Task 1 ✅
  - Refresh PDF coachee → Task 3 ✅
  - Refresh PDF admin → Task 4 ✅
  - 6 accordion (Acc-5..10) → Task 2 ✅
- [ ] **Placeholder scan:** no TBD / TODO / "fill in later" → clean
- [ ] **Type consistency:** `Pdfs` (plural) used konsisten di ViewModel, Controller, View, GetModuleCards
- [ ] **Concrete code:** semua step yang touch kode punya code block before/after
- [ ] **Verifikasi step:** tiap commit punya browser smoke test + dotnet build sebelum commit
- [ ] **Atomic commit:** 5 commit logikal (refactor, accordion, PDF coachee, PDF admin, e2e test), tidak bercampur

---

## Plan Summary

| Task | Files | Verifikasi | Commit |
|------|-------|------------|--------|
| 1. Refactor GetPdf + Pindah PDF admin | 5 file (provider, viewmodel, controller, view) | dotnet build + browser admin/coachee | Commit 1 |
| 2. Tambah 6 accordion | 1 file (provider) | dotnet build + browser admin/coachee | Commit 2 |
| 3. PDF coachee refresh | 1 file (HTML 629→~800) | Browser render PDF | Commit 3 |
| 4. PDF admin refresh | 1 file (HTML 268→~700) | Browser render PDF | Commit 4 |
| 5. Playwright e2e | 1 file (8 test) | npx playwright test | Commit 5 |
| 6. UAT checklist | none | Manual user verify + push | No commit, push 5 commits |

**Total touch:** 6 file modify + 1 file create. **No migration. No CSS/JS change. No DB schema change.**
