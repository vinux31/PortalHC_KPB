# Tutorial Workflow Claude Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membuat satu file HTML standalone (`docs/tutorial-workflow-claude.html`) berisi tutorial 6-step alur kerja coding dengan Claude Code (GSD vs Superpower) untuk rekan developer PortalHC KPB, print-friendly A4, exportable ke PDF.

**Architecture:** Single self-contained HTML file. Inline `<style>` block (no external CSS, no JS). Visual hybrid mengikuti gaya `docs/checklist-deploy.html` (max-width 720px, header biru `#1a56db`, checkbox list) plus elemen step-num bulat dari `docs/ActiveDirectory-Guide.html`. Konten disusun 6 step linear: Trigger → Kompleksitas → Flow GSD/SP → Verifikasi Playwright → Commit/Push → Handoff IT.

**Tech Stack:** HTML5, CSS3 (inline). Tidak ada JavaScript, tidak ada external assets. Konten Bahasa Indonesia (sesuai `CLAUDE.md`).

**Spec Reference:** `docs/superpowers/specs/2026-04-28-tutorial-workflow-claude-design.md`

---

## File Structure

| File | Action | Purpose |
|---|---|---|
| `docs/tutorial-workflow-claude.html` | Create | Single output file — tutorial standalone HTML |

Tidak ada file lain yang dimodifikasi atau dibuat. Output PDF dihasilkan rekan via "Save as PDF" dari browser, bukan dari proses build.

---

## Verification Strategy

Karena ini dokumen statis (bukan kode), "test" diganti dengan **manual verification** di tiap step besar:
1. Buka file di browser (Chrome/Edge) → cek rendering normal
2. Print preview (Ctrl+P) → cek layout A4, jumlah halaman, tidak ada elemen terpotong
3. Cek konten konsisten dengan spec acceptance criteria

Verifikasi final dilakukan di Task 8 setelah semua konten selesai.

---

## Task 1: Skeleton HTML + Style Block

**Files:**
- Create: `docs/tutorial-workflow-claude.html`

- [ ] **Step 1: Tulis skeleton HTML dengan style block lengkap**

Isi file:

```html
<!DOCTYPE html>
<html lang="id">
<head>
    <meta charset="UTF-8">
    <title>Tutorial Workflow Claude — PortalHC KPB</title>
    <style>
        @page { margin: 2cm; size: A4; }
        body { font-family: 'Segoe UI', Tahoma, sans-serif; max-width: 720px; margin: 40px auto; color: #222; line-height: 1.6; font-size: 11pt; padding: 0 20px; }
        h1 { color: #c62828; text-align: center; font-size: 22px; border-bottom: 3px solid #c62828; padding-bottom: 10px; margin-bottom: 6px; }
        .subtitle { text-align: center; color: #666; margin-top: -2px; margin-bottom: 30px; font-size: 11pt; }
        h2 { font-size: 16px; background: #1a56db; color: white; padding: 8px 14px; border-radius: 4px; margin-top: 30px; display: flex; align-items: center; gap: 10px; }
        h3 { font-size: 14px; color: #1a56db; margin-top: 20px; margin-bottom: 8px; }
        p { margin-bottom: 10px; }
        ul, ol { padding-left: 24px; margin-bottom: 10px; }
        li { margin-bottom: 4px; }
        .step-num { display: inline-block; background: white; color: #1a56db; width: 26px; height: 26px; border-radius: 50%; text-align: center; line-height: 26px; font-weight: 700; font-size: 13px; }
        .checklist { list-style: none; padding: 0; }
        .checklist li { padding: 6px 0 6px 30px; position: relative; border-bottom: 1px solid #eee; margin-bottom: 0; }
        .checklist li::before { content: "\2610"; position: absolute; left: 4px; font-size: 18px; color: #1a56db; }
        table { width: 100%; border-collapse: collapse; margin: 10px 0; font-size: 12px; }
        th { background: #f0f4ff; text-align: left; padding: 8px 10px; border: 1px solid #ccc; }
        td { padding: 8px 10px; border: 1px solid #ccc; vertical-align: top; }
        code { background: #f3f4f6; padding: 2px 6px; border-radius: 3px; font-size: 12px; font-family: 'Consolas', monospace; }
        .command-box { background: #1e293b; color: #e2e8f0; padding: 12px 16px; border-radius: 6px; font-family: 'Consolas', monospace; font-size: 12px; margin: 10px 0; white-space: pre-wrap; line-height: 1.5; }
        .info { background: #e3f2fd; border-left: 4px solid #1a56db; padding: 10px 14px; margin: 10px 0; font-size: 12px; border-radius: 0 4px 4px 0; }
        .note { background: #fef3c7; border-left: 4px solid #f59e0b; padding: 10px 14px; margin: 10px 0; font-size: 12px; border-radius: 0 4px 4px 0; }
        .danger { background: #fee2e2; border-left: 4px solid #dc2626; padding: 10px 14px; margin: 10px 0; font-size: 12px; border-radius: 0 4px 4px 0; }
        .flow-diagram { background: #fafafa; border: 1px solid #e0e0e0; border-radius: 6px; padding: 14px 16px; margin: 12px 0; font-family: 'Consolas', monospace; font-size: 11px; white-space: pre; line-height: 1.7; overflow-x: auto; }
        .badge { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 11px; font-weight: 600; }
        .badge-gsd { background: #c8e6c9; color: #1b5e20; }
        .badge-sp { background: #ffe0b2; color: #e65100; }
        .footer { text-align: center; color: #999; font-size: 11px; margin-top: 40px; border-top: 1px solid #eee; padding-top: 10px; }
        @media print {
            body { margin: 0; padding: 0; max-width: 100%; }
            h2 { break-after: avoid; }
            .checklist li, .flow-diagram, .command-box, .info, .note, .danger { break-inside: avoid; }
        }
    </style>
</head>
<body>

<!-- KONTEN AKAN DIISI DI TASK BERIKUTNYA -->

</body>
</html>
```

- [ ] **Step 2: Verifikasi file bisa dibuka di browser**

Run: buka `docs/tutorial-workflow-claude.html` di browser
Expected: halaman kosong (cuma body putih), tidak ada error di console (F12)

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: skeleton HTML untuk tutorial workflow Claude"
```

---

## Task 2: Header & Intro

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (replace `<!-- KONTEN AKAN DIISI DI TASK BERIKUTNYA -->` comment)

- [ ] **Step 1: Tambahkan header dan paragraf intro**

Ganti komentar `<!-- KONTEN AKAN DIISI DI TASK BERIKUTNYA -->` dengan:

```html
<h1>Alur Kerja Coding dengan Claude Code</h1>
<p class="subtitle">PortalHC KPB &mdash; Panduan untuk Rekan Developer | April 2026</p>

<p>Dokumen ini menjelaskan alur kerja dari <strong>temuan audit</strong> atau <strong>request fitur baru</strong> sampai kode di-deploy ke server developer. Ada dua mode utama saat coding bareng Claude:</p>

<ul>
    <li><span class="badge badge-gsd">GSD</span> &mdash; untuk task kompleks yang butuh perencanaan multi-step (struktur <code>.planning/phases/</code>)</li>
    <li><span class="badge badge-sp">Superpower</span> &mdash; untuk task ringan yang bisa selesai cepat (<code>brainstorming</code> &rarr; <code>writing-plans</code> &rarr; <code>executing-plans</code>)</li>
</ul>

<p>Pilih mode yang tepat di awal supaya tidak ada overhead yang tidak perlu. Total ada 6 step dari awal sampai handoff ke IT.</p>

<!-- STEP 1 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Judul merah "Alur Kerja Coding dengan Claude Code" muncul dengan border bawah
- Subtitle abu-abu kecil di bawahnya
- Paragraf intro tampil
- Badge GSD (hijau) dan Superpower (oranye) muncul inline di list

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah header dan intro tutorial workflow"
```

---

## Task 3: Step 1 — Trigger

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 1 -->`)

- [ ] **Step 1: Tambahkan section Step 1**

Ganti `<!-- STEP 1 -->` dengan:

```html
<h2><span class="step-num">1</span>Trigger: Audit Findings atau Fitur Baru</h2>

<p>Siklus coding biasanya dimulai karena salah satu pemicu berikut:</p>

<ul>
    <li><strong>Temuan audit milestone</strong> &mdash; lihat file <code>.planning/milestones/v{X}-MILESTONE-AUDIT.md</code> untuk daftar gap yang perlu ditutup</li>
    <li><strong>Request fitur atau perbaikan</strong> &mdash; dari HC, atasan, atau end-user</li>
</ul>

<h3>Siapkan Info Sebelum Prompt Claude</h3>
<p>Supaya Claude tidak perlu tanya banyak hal di awal, siapkan dulu:</p>

<ul class="checklist">
    <li>Deskripsi masalah atau kebutuhan dalam 1&ndash;2 kalimat</li>
    <li>File terkait kalau sudah tahu (mis. <code>Pages/Assessments/Index.cshtml</code>)</li>
    <li>Screenshot atau contoh output untuk bug UI</li>
    <li>Skenario reproduksi step-by-step untuk laporan bug</li>
</ul>

<!-- STEP 2 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Heading biru "1. Trigger: Audit Findings atau Fitur Baru" dengan lingkaran putih bertanda "1"
- Sub-heading "Siapkan Info Sebelum Prompt Claude" warna biru
- Checklist dengan kotak ☐ di sebelah kiri tiap item

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 1 (Trigger) tutorial workflow"
```

---

## Task 4: Step 2 — Tentukan Kompleksitas

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 2 -->`)

- [ ] **Step 1: Tambahkan section Step 2**

Ganti `<!-- STEP 2 -->` dengan:

```html
<h2><span class="step-num">2</span>Tentukan Kompleksitas</h2>

<p>Kriteria sederhana untuk pilih mode:</p>

<table>
    <tr>
        <th style="width:50%">Pakai <span class="badge badge-gsd">GSD</span> kalau&hellip;</th>
        <th style="width:50%">Pakai <span class="badge badge-sp">Superpower</span> kalau&hellip;</th>
    </tr>
    <tr>
        <td>
            &bull; Menyentuh &ge;3 file<br>
            &bull; Ada migrasi DB<br>
            &bull; Lintas role atau lintas halaman<br>
            &bull; Butuh perencanaan multi-step / multi-phase
        </td>
        <td>
            &bull; Ubah 1&ndash;2 file<br>
            &bull; Tweak UI satu halaman<br>
            &bull; Bug fix terlokalisasi<br>
            &bull; Estimasi &lt;30 menit
        </td>
    </tr>
</table>

<div class="info"><strong>Kalau ragu:</strong> mulai dari Superpower. Kalau ternyata scope membesar di tengah jalan, bisa di-promote ke GSD pakai <code>/gsd-import</code>.</div>

<!-- STEP 3 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Heading "2. Tentukan Kompleksitas" muncul
- Tabel 2 kolom dengan badge GSD/Superpower di header, masing-masing 4 bullet
- Box info biru di bawah tabel

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 2 (Tentukan Kompleksitas) tutorial workflow"
```

---

## Task 5: Step 3 — Flow GSD vs Superpower

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 3 -->`)

- [ ] **Step 1: Tambahkan section Step 3 dengan dua flow stacked vertikal**

Ganti `<!-- STEP 3 -->` dengan:

```html
<h2><span class="step-num">3</span>Flow GSD vs Superpower</h2>

<h3>Flow <span class="badge badge-gsd">GSD</span> &mdash; untuk task kompleks</h3>

<div class="flow-diagram">/gsd-discuss-phase   →  kumpulkan konteks, tanya jawab
        ↓ checkpoint: review jawaban
/gsd-plan-phase      →  buat PLAN.md
        ↓ checkpoint: review plan
/gsd-execute-phase   →  eksekusi (atomic commits)
        ↓
/gsd-verify-work     →  UAT &amp; verifikasi goal</div>

<ul>
    <li>Artefak disimpan di <code>.planning/phases/&lt;NN-nama-phase&gt;/</code></li>
    <li><strong>Checkpoint</strong> = Claude berhenti minta konfirmasi sebelum lanjut ke step berikutnya</li>
    <li>Untuk task lebih cepat tapi tetap GSD, pakai <code>/gsd-quick</code> (skip optional agents)</li>
</ul>

<h3>Flow <span class="badge badge-sp">Superpower</span> &mdash; untuk task ringan</h3>

<div class="flow-diagram">brainstorming                   →  gali tujuan &amp; desain
        ↓ design approval
writing-plans                   →  tulis plan implementasi
        ↓ plan review
executing-plans                 →  eksekusi step-by-step
        ↓
verification-before-completion  →  verifikasi sebelum claim selesai</div>

<ul>
    <li>Spec disimpan di <code>docs/superpowers/specs/YYYY-MM-DD-&lt;topik&gt;-design.md</code></li>
    <li>Plan disimpan di <code>docs/superpowers/plans/</code></li>
    <li>Skill diaktifkan otomatis oleh Claude lewat <code>Skill</code> tool, atau ketik nama skill di prompt</li>
</ul>

<!-- STEP 4 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Heading "3. Flow GSD vs Superpower"
- Dua sub-heading dengan badge inline (hijau & oranye)
- Dua flow-diagram box dengan latar abu-abu, monospace, anak panah Unicode (↓ →) tampak benar

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 3 (Flow GSD vs Superpower) tutorial workflow"
```

---

## Task 6: Step 4 — Verifikasi via Playwright MCP

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 4 -->`)

- [ ] **Step 1: Tambahkan section Step 4**

Ganti `<!-- STEP 4 -->` dengan:

```html
<h2><span class="step-num">4</span>Verifikasi via Playwright MCP (localhost)</h2>

<p>Claude Code yang Anda pakai sudah punya plugin <strong>Playwright MCP</strong> terpasang. Claude bisa membuka browser otomatis ke <code>localhost</code>, klik UI, mengambil screenshot, dan membaca console error untuk verifikasi end-to-end.</p>

<h3>Prasyarat</h3>
<ul class="checklist">
    <li>Plugin Playwright MCP aktif di Claude Code (tool <code>mcp__plugin_playwright_playwright__*</code> tersedia)</li>
    <li>Dev server jalan: <code>dotnet run</code> di terminal terpisah</li>
    <li>Aplikasi accessible di <code>https://localhost:5001</code> (Kestrel default)</li>
    <li>Akun test sudah disiapkan (HC, Worker, Admin sesuai kebutuhan)</li>
</ul>

<h3>Contoh Prompt</h3>
<p>Ganti placeholder <code>&lt;...&gt;</code> dengan kredensial test Anda:</p>

<div class="command-box">Pakai Playwright, buka https://localhost:5001, login sebagai HC
(email: &lt;email-test&gt;, password: &lt;password-test&gt;),
buat assessment baru, assign 1 worker, dan ambil screenshot tiap step.
Laporkan kalau ada error di console.</div>

<div class="note"><strong>Catatan:</strong> Dev server harus sudah jalan dulu. Kalau belum, suruh Claude jalankan <code>dotnet run</code> di background bash sebelum verifikasi (Claude punya tool background process).</div>

<!-- STEP 5 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Heading "4. Verifikasi via Playwright MCP (localhost)"
- Sub-heading "Prasyarat" dengan checklist 4 item
- Sub-heading "Contoh Prompt" dengan command-box gelap (background `#1e293b`) berisi prompt template
- Box note kuning di bawahnya

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 4 (Verifikasi Playwright) tutorial workflow"
```

---

## Task 7: Step 5 — Commit & Push

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 5 -->`)

- [ ] **Step 1: Tambahkan section Step 5**

Ganti `<!-- STEP 5 -->` dengan:

```html
<h2><span class="step-num">5</span>Commit &amp; Push</h2>

<h3>Konvensi Prefix Commit</h3>
<p>Berdasarkan pola git log proyek ini:</p>

<table>
    <tr><th style="width:25%">Prefix</th><th>Kapan dipakai</th></tr>
    <tr><td><code>feat:</code></td><td>Fitur baru</td></tr>
    <tr><td><code>fix:</code></td><td>Bug fix</td></tr>
    <tr><td><code>chore:</code></td><td>Maintenance, cleanup, config</td></tr>
    <tr><td><code>docs:</code></td><td>Dokumentasi</td></tr>
    <tr><td><code>refactor:</code></td><td>Restrukturisasi tanpa ubah behavior</td></tr>
    <tr><td><code>test:</code></td><td>Penambahan atau perubahan test</td></tr>
</table>

<h3>Alur Perintah</h3>
<div class="command-box">git status              # cek file yang berubah
git add -p              # review per-hunk (interaktif)
git commit -m "fix: &lt;deskripsi singkat&gt;"
git push origin main</div>

<p>Claude bisa bantu draft commit message &mdash; minta: <em>"tolong buatkan commit message untuk perubahan ini"</em>.</p>

<div class="danger"><strong>Jangan commit:</strong>
    <ul>
        <li>File <code>.env</code>, <code>appsettings.Development.json</code> dengan secret</li>
        <li>File database <code>HcPortal.db</code> development</li>
        <li>File besar binary (&gt;10 MB) &mdash; pakai <code>.gitignore</code></li>
    </ul>
</div>

<!-- STEP 6 -->
```

- [ ] **Step 2: Verifikasi rendering**

Run: refresh browser
Expected:
- Heading "5. Commit & Push"
- Tabel 6 baris berisi prefix commit
- Command-box dengan 4 perintah git
- Box danger merah dengan list 3 item

- [ ] **Step 3: Commit**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 5 (Commit & Push) tutorial workflow"
```

---

## Task 8: Step 6 — Handoff ke IT + Footer + Verifikasi Final

**Files:**
- Modify: `docs/tutorial-workflow-claude.html` (ganti komentar `<!-- STEP 6 -->` + tambah footer)

- [ ] **Step 1: Tambahkan section Step 6 dan footer**

Ganti `<!-- STEP 6 -->` dengan:

```html
<h2><span class="step-num">6</span>Handoff ke IT</h2>

<p>Setelah commit ter-push ke GitHub, kirim info berikut ke IT supaya mereka bisa pull dan upload ke server developer.</p>

<h3>Info yang Dikirim ke IT</h3>
<table>
    <tr><th style="width:30%">Item</th><th>Penjelasan</th></tr>
    <tr><td>Branch / commit hash</td><td>Hash commit terakhir yang siap deploy &mdash; <code>git rev-parse HEAD</code></td></tr>
    <tr><td>Migrasi DB baru</td><td>Daftar file migrasi di <code>Migrations/</code> yang belum ada di server, atau "tidak ada"</td></tr>
    <tr><td>Perubahan <code>appsettings</code></td><td>Apakah ada key baru di <code>appsettings.json</code> yang perlu diisi di <code>appsettings.Production.json</code></td></tr>
    <tr><td>File / folder baru</td><td>Folder upload, folder static, dll yang perlu permission write</td></tr>
    <tr><td>UAT yang sudah dilakukan</td><td>Daftar flow yang sudah diverifikasi (Login, Assessment, Coaching, dll)</td></tr>
    <tr><td>Catatan khusus</td><td>Bug bekas, cleanup data yang perlu dijalankan, dll</td></tr>
</table>

<h3>Langkah IT (ringkas)</h3>
<ul class="checklist">
    <li>Pull dari GitHub: <code>git pull origin main</code></li>
    <li>Jalankan migrasi (kalau ada migrasi baru): <code>dotnet ef database update</code></li>
    <li>Build &amp; publish: <code>dotnet publish -c Release -o ./publish</code></li>
    <li>Copy isi <code>publish/</code> ke server, jalankan <code>dotnet HcPortal.dll</code></li>
</ul>

<div class="info"><strong>Untuk checklist deploy lengkap</strong> (server config, security check, post-deploy verification), lihat <code>docs/checklist-deploy.html</code>. Dokumen ini hanya menjelaskan alur sampai handoff ke IT, bukan langkah teknis IT-nya.</div>

<div class="footer">
    Dokumen ini dibuat 28 April 2026 &mdash; PortalHC KPB | Untuk konteks proyek lihat <code>.planning/PROJECT.md</code> dan <code>.planning/MILESTONES.md</code>
</div>
```

- [ ] **Step 2: Verifikasi rendering full document**

Run: refresh browser
Expected:
- Heading "6. Handoff ke IT" muncul
- Tabel 6 baris berisi info yang dikirim ke IT
- Checklist 4 item langkah IT
- Box info biru di bawah
- Footer abu-abu di paling bawah dengan border-top
- Scroll dari atas ke bawah: 6 step lengkap, semua step-num bulat 1–6 ada, tidak ada komentar `<!-- STEP X -->` yang masih nyangkut

- [ ] **Step 3: Verifikasi print preview A4**

Run: di browser, tekan Ctrl+P (print preview)
Expected:
- Total halaman 4–6 halaman A4
- Margin rapi (sesuai `@page { margin: 2cm }`)
- Tidak ada heading yang terpotong di tengah (h2 selalu di awal halaman atau utuh)
- Tidak ada `flow-diagram`, `command-box`, atau `checklist li` yang terbelah dua halaman

Kalau ada elemen kepotong: catat selisih dan adjust `break-inside: avoid` di style block.

- [ ] **Step 4: Verifikasi konten konsisten dengan spec**

Cek spec acceptance criteria di `docs/superpowers/specs/2026-04-28-tutorial-workflow-claude-design.md` section 5 (Acceptance Criteria) — pastikan semua 7 poin terpenuhi:
- File ada dan bisa dibuka di browser ✓
- Print preview ≤5 halaman A4 ✓
- Export PDF tidak terpotong di tengah step ✓
- 6 step ada dengan struktur sesuai spec ✓
- Slash command GSD yang disebut tersedia (`/gsd-discuss-phase`, `/gsd-plan-phase`, `/gsd-execute-phase`, `/gsd-verify-work`, `/gsd-quick`) ✓
- Skill Superpower yang disebut tersedia (`brainstorming`, `writing-plans`, `executing-plans`, `verification-before-completion`) ✓
- Path artefak benar (`.planning/phases/`, `docs/superpowers/specs/`, `docs/superpowers/plans/`, `docs/checklist-deploy.html`) ✓
- Tidak ada placeholder/lorem ipsum tersisa ✓
- Bahasa Indonesia konsisten ✓

- [ ] **Step 5: Test export ke PDF**

Run: Ctrl+P → Destination: "Save as PDF" → Save ke folder Downloads sementara
Expected:
- File PDF ter-generate
- Buka PDF, scroll dari halaman 1 sampai akhir
- Layout sama dengan print preview (warna, posisi step-num, tabel)
- Bisa di-search teks (bukan rendered as image)
- Hapus PDF sementara setelah verifikasi

- [ ] **Step 6: Commit final**

```bash
git add docs/tutorial-workflow-claude.html
git commit -m "docs: tambah Step 6 (Handoff IT) dan finalisasi tutorial workflow Claude"
```
