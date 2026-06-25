# Phase 388: Label Hasil + CoachWorkload Polish (LBL-03 + DSN-04 + DSN-05) - Research

**Researched:** 2026-06-17
**Domain:** Razor view markup polish — Bootstrap 5 + Bootstrap Icons (ASP.NET Core MVC)
**Confidence:** HIGH (semua temuan diverifikasi langsung di codebase; 0 dependensi eksternal baru)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (D-01..D-08 — NON-NEGOTIABLE)

- **D-01 (LBL-03):** `Views/CMP/Results.cshtml:60` `<h6 class="text-muted mb-2">Nilai Kelulusan</h6>` → `Batas Nilai Kelulusan`. HANYA tambah kata "Batas". `<h2>@Model.PassPercentage%</h2>` di bawahnya TIDAK diubah. Grep konfirmasi hanya 1 match di file ini.
- **D-02 (DSN-05):** Pendekatan = Bootstrap utility class sebisanya + 1 blok `<style>` scoped minimal di atas `CoachWorkload.cshtml`. TIDAK buat/extend file CSS bersama (records.css dsb).
- **D-03 (DSN-05):** Magic-number font-size jadi util Bootstrap: `font-size:11px` (L93) & `font-size:12px` (L104) & `font-size: 0.85rem` (L156) → kelas `small` / `text-muted` / `fs-*` (pilih paling dekat visual).
- **D-04 (DSN-05):** Legend dot inline (L157-159) → 1 kelas scoped `.legend-dot` (warna ditahan inline `background` karena = data status). Chevron transition (L193) boleh tetap atau ke kelas — discretion.
- **D-05 (DSN-05):** Inline FUNGSIONAL/layout BIARKAN: `max-height:300px;overflow-y:auto` (L153), `min-height:150px` canvas (L154, di-set JS L321), `max-width:300px` select (L115). Jangan utak-atik yg dipakai JS Chart.js.
- **D-06 (DSN-04):** Filter bar (`<form method="get">` L114-131) dibungkus `card border-0 shadow-sm` + `card-header` (bi-icon + judul, mis. `<i class="bi bi-funnel me-2"></i>Filter`). Form pindah ke `card-body`.
- **D-07 (DSN-04):** Section "Saran Penyeimbangan" jadi 1 card (`card-header` ikon+judul + `card-body`); tiap item saran jadi baris `list-group`/`list-group-flush` DI DALAM card-body — BUKAN card sendiri (hilangkan card-in-card nesting).
- **D-08 (PARITY KRITIS):** Saat ubah item saran → list-group, WAJIB pertahankan SEMUA hook JS: tiap baris tetap `id="sug-@sug.MappingId"` + class mengandung `suggestion-card`; tombol tetap `.approve-btn`/`.skip-btn` + semua `data-*`; `fadeOutCard()` jalan di elemen baris baru. Empty-state + role-gate `User.IsInRole("Admin")` TIDAK berubah.

### Claude's Discretion
- Pemilihan kelas Bootstrap font-size paling dekat visual (D-03).
- Penyelarasan spacing (margin/gap) antar section — util Bootstrap (`mb-*`/`g-*`) konsisten.
- Chevron transition dipindah ke kelas atau dibiarkan (D-04).
- Ikon `bi-*` untuk card-header filter & saran (mis. `bi-funnel`, `bi-arrow-left-right`/`bi-shuffle`).

### Deferred Ideas (OUT OF SCOPE)
- None deferred. OUT-of-phase: redesign CoachCoacheeMapping = Phase 389; verifikasi parity penuh = Phase 390. Perubahan backend/controller, migration, kolom/fungsi baru, file view lain.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LBL-03 | Label "Batas Nilai Kelulusan" (bukan "Nilai Kelulusan") di kartu ringkasan halaman hasil assessment (`Results.cshtml`); nilai persen tidak berubah | §LBL-03 Exact Target — verified L60 single match |
| DSN-04 | Filter bar + section "Saran Penyeimbangan" terbungkus card konsisten dgn section lain | §Card Pattern Excerpt + §list-group Conversion Sketch |
| DSN-05 | CoachWorkload bebas inline magic-number font-size; spacing diselaraskan | §Inline-Style Inventory + §JS Hook Preservation Map |
</phase_requirements>

## Summary

Phase 388 adalah polish view murni di **2 file disjoint** (`Views/CMP/Results.cshtml` dan `Views/Admin/CoachWorkload.cshtml`). Tidak ada controller, endpoint, JS-contract, atau migration yang disentuh. Risiko teknis terkonsentrasi pada **DSN-07/D-08**: refactor item "Saran Penyeimbangan" dari card-per-item menjadi `list-group-item`, sambil mempertahankan setiap hook JS yang dipakai handler approve/skip (`#sug-{MappingId}`, class `suggestion-card`, `.approve-btn`/`.skip-btn`, dan semua `data-*`).

Temuan kunci: aplikasi sudah punya **precedent `.legend-dot`** di `Views/CMP/AnalyticsDashboard.cshtml:34` (`width:12px;height:12px;border-radius:2px;display:inline-block`), dipakai bersama `.legend-item` (flex layout). CoachWorkload memakai lingkaran (`border-radius:50%`) bukan kotak — jadi `.legend-dot` baru di CoachWorkload sah dibuat scoped lokal (D-02 melarang file CSS bersama), tapi konvensi penamaan & struktur (dot + label dalam flex) bisa ditiru verbatim dari AnalyticsDashboard agar konsisten app-wide.

LBL-03 trivial & verified: tepat 1 string "Nilai Kelulusan" di seluruh tree `Views/` (di `Results.cshtml:60`). Edit aman, tidak ada false-positive co-replacement.

**Primary recommendation:** Kerjakan 3 unit independen — (1) LBL-03 satu-baris di Results.cshtml; (2) DSN-04 bungkus filter bar + section saran ke card; (3) DSN-05 ganti 3 magic-number font-size ke util + legend dot ke kelas scoped. Jangan sentuh summary cards (L70-111), chart card (L150-162), table card (L165-225), modal threshold (L274-308), dan SELURUH `@section Scripts` (L310-524). Verifikasi WAJIB runtime Playwright/UAT — grep+build tidak cukup untuk Razor dinamis (lesson Phase 354).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Label teks "Batas Nilai Kelulusan" | Frontend Server (Razor render) | — | Static text di `.cshtml`, tidak ada data flow |
| Card framing filter bar / saran | Frontend Server (Razor markup) | Browser (Bootstrap CSS) | Struktur HTML server-rendered; styling oleh Bootstrap class |
| Font-size util + legend dot kelas | Browser (CSS) | — | Pure presentation; scoped `<style>` block |
| Approve/Skip saran (AJAX) | API/Backend (unchanged) | Browser (fetch handler) | Endpoint `/Admin/Approve/SkipReassignSuggestion` TIDAK disentuh; markup hanya menyediakan hook selector |
| Chart render | Browser (Chart.js runtime) | — | `#workloadChart` canvas + JS L317-371 TIDAK disentuh |

**Catatan tier:** Seluruh perubahan phase ini terjadi di **tier Razor markup + Browser CSS**. Tidak ada perubahan tier API/Backend — selector markup hanya kontrak yang dibaca JS browser. Inilah kenapa D-08 (preservasi hook) kritis: tier markup tidak boleh memutus kontrak yang dibaca tier browser-JS.

## Standard Stack

Tidak ada dependensi baru. Phase ini memakai stack yang sudah terpasang.

### Core (sudah ada, tidak install apa pun)
| Library | Versi | Purpose | Bukti di codebase |
|---------|-------|---------|-------------------|
| Bootstrap 5 | (bundled app) | Card, list-group, utility classes (`small`, `fs-*`, `mb-*`, `g-*`, `text-muted`) | Dipakai luas di kedua file [VERIFIED: codebase] |
| Bootstrap Icons (`bi-*`) | (bundled app) | Ikon card-header (`bi-funnel`, `bi-bar-chart`, `bi-table`, dst) | L151/L166 CoachWorkload [VERIFIED: codebase] |
| Chart.js 4 | 4 (CDN jsdelivr) | Bar chart workload — TIDAK disentuh | L311 `chart.umd.min.js@4` [VERIFIED: codebase] |

**Installation:** Tidak ada. `npm install` tidak diperlukan untuk perubahan view. (Playwright test harness `tests/` sudah terpasang untuk verifikasi.)

## Architecture Patterns

### Pattern 1: Card idiom app (template untuk D-06/D-07)
**What:** `card shadow-sm` (atau `card border-0 shadow-sm`) + `card-header fw-semibold` dengan bi-icon + judul, lalu `card-body`.
**When to use:** Membungkus filter bar (D-06) dan section "Saran Penyeimbangan" (D-07) agar konsisten dgn chart card & table card.
**Example (markup existing yang ditiru, CoachWorkload.cshtml L150-167):**
```html
<!-- Source: Views/Admin/CoachWorkload.cshtml L150-152 (Grafik Beban Coach) -->
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-bar-chart me-2"></i>Grafik Beban Coach</div>
    <div class="card-body">
        ...
    </div>
</div>

<!-- Source: Views/Admin/CoachWorkload.cshtml L165-167 (Detail Beban Coach) -->
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-table me-2"></i>Detail Beban Coach</div>
    <div class="card-body">
        ...
    </div>
</div>
```
**Catatan konsistensi:** Card existing pakai `card shadow-sm` (TANPA `border-0`); summary cards pakai `card border-0 shadow-sm`. CONTEXT D-06 menulis `card border-0 shadow-sm`. **Rekomendasi:** untuk konsistensi VISUAL dengan chart/table card di section yang sama (yang ditiru), pakai `card shadow-sm` agar seragam — atau samakan semua jadi `border-0`. Ini perlu keputusan planner; default aman = tiru `card shadow-sm` (chart/table) karena itu yang berdampingan langsung. [ASSUMED — A1]

### Pattern 2: `.legend-dot` kelas scoped (precedent app)
**What:** Kelas CSS untuk legend marker; warna ditahan inline `style="background:..."` karena warna = data status.
**Precedent app (AnalyticsDashboard.cshtml L33-34, L252-254):**
```css
/* Source: Views/CMP/AnalyticsDashboard.cshtml L33-34 */
.legend-item { display: inline-flex; align-items: center; gap: 4px; margin-right: 12px; font-size: 0.8rem; }
.legend-dot  { width: 12px; height: 12px; border-radius: 2px; display: inline-block; }
```
```html
<!-- Source: Views/CMP/AnalyticsDashboard.cshtml L252 -->
<span class="legend-item"><span class="legend-dot" style="background:#28a745;"></span> &lt;30% (Baik)</span>
```
**Perbedaan untuk CoachWorkload:** dot di CoachWorkload = LINGKARAN (`border-radius:50%`), bukan kotak (`2px`). Karena D-02 melarang file CSS bersama, definisikan `.legend-dot` di blok `<style>` scoped CoachWorkload dengan `border-radius:50%` + `vertical-align:middle` (mempertahankan tampilan asli L157-159). Penamaan kelas `.legend-dot` konsisten dgn AnalyticsDashboard. [VERIFIED: codebase]

### Anti-Patterns to Avoid
- **Card-in-card nesting:** D-07 secara eksplisit menghapus item saran sebagai `card shadow-sm` di dalam card-body. JANGAN biarkan `<div class="card">` per item — ganti `list-group-item`.
- **Hardcode path AJAX:** JS pakai `(window.basePath || '') + '/Admin/...'` (L396/L434/L495). Markup tidak menyentuh ini, tapi jangan menambah link/form baru yg hardcode `/Admin/` atau `/CoachMapping/`.
- **Mengubah selector yang dibaca JS:** lihat §JS Hook Preservation Map. Mengganti id/class struktural = memutus approve/skip/collapse/threshold.
- **Class util `border-0` tidak konsisten:** mencampur `card shadow-sm` dan `card border-0 shadow-sm` di section berdampingan menghasilkan border tak seragam. Pilih satu.

## Inline-Style Inventory — CoachWorkload.cshtml (DSN-05)

Audit lengkap setiap `style=` inline di file. Klasifikasi: **(a) GANTI** (magic-number font-size — target DSN-05), **(b) KELAS** (legend dot → `.legend-dot`), **(c) BIARKAN** (fungsional/layout/JS-driven — D-05).

| Baris | Inline style | Konteks | Klasifikasi | Tindakan |
|-------|-------------|---------|-------------|----------|
| L93 | `style="font-size:11px;"` | `<div class="text-muted">coachee/coach</div>` (sublabel rasio) | **(a) GANTI** | → kelas `small` (≈0.875em) atau pertahankan kecil dgn util. Visual 11px ≈ `small`. Hapus inline. |
| L104 | `style="font-size:12px;"` | `<span class="badge bg-danger ms-1">!</span>` | **(a) GANTI** | → util `small` pada span badge (atau `fs-6`). 12px ≈ `small`. Hapus inline. |
| L115 | `style="max-width: 300px;"` | `<select name="section" class="form-select">` | **(c) BIARKAN** | Layout constraint width — D-05 eksplisit boleh inline. |
| L153 | `style="max-height: 300px; overflow-y: auto;"` | wrapper scroll chart | **(c) BIARKAN** | Fungsional scroll container — D-05 eksplisit. |
| L154 | `style="min-height: 150px;"` | `<canvas id="workloadChart">` | **(c) BIARKAN** | JS-driven: `canvas.style.height` di-set runtime L321. JANGAN sentuh. |
| L156 | `style="font-size: 0.85rem;"` | legend container `<div class="d-flex gap-4 mt-3 flex-wrap">` | **(a) GANTI** | → kelas `small` (0.875em ≈ 0.85rem). Hapus inline; tambah `small` ke div. |
| L157 | `style="display:inline-block;width:12px;height:12px;border-radius:50%;background:#198754;margin-right:6px;vertical-align:middle;"` | legend dot Normal (hijau) | **(b) KELAS** | → `<span class="legend-dot" style="background:#198754;"></span>`. Sisa properti pindah ke kelas scoped. |
| L158 | sama (`background:#ffc107`) | legend dot Mendekati Batas (kuning) | **(b) KELAS** | → `class="legend-dot" style="background:#ffc107;"` |
| L159 | sama (`background:#dc3545`) | legend dot Overloaded (merah) | **(b) KELAS** | → `class="legend-dot" style="background:#dc3545;"` |
| L193 | `style="transition:transform 0.2s;display:inline-block;"` | `<i class="bi bi-chevron-right expand-chevron">` | **(c) BIARKAN atau KELAS (discretion D-04)** | JS set `chevron.style.transform` runtime (L461/L465). Aman dibiarkan; bila ke kelas, JANGAN hapus class `expand-chevron`. Transform tetap di-set inline oleh JS. |

**Blok `<style>` scoped yang direkomendasikan** (1 blok minimal di atas file, D-02):
```html
<style>
    .legend-dot {
        display: inline-block;
        width: 12px;
        height: 12px;
        border-radius: 50%;
        margin-right: 6px;
        vertical-align: middle;
    }
    /* (opsional D-04) .expand-chevron { transition: transform 0.2s; display: inline-block; } */
</style>
```

**Magic-number → util mapping (D-03 discretion, rekomendasi):**
- `11px` (L93) → `small` — paling dekat visual untuk sublabel muted. Bootstrap `small` ≈ 0.875em.
- `12px` (L104) → `small` — badge text kecil. (Alternatif `fs-6` = 1rem, terlalu besar; `small` lebih dekat.)
- `0.85rem` (L156) → `small` — legend container. 0.875em ≈ 0.85rem, match.

> Catatan presisi: util `small` = 0.875em (relatif), magic-number = absolut px. Pergeseran ±1px mungkin terjadi; ini akseptabel untuk polish (visual hampir identik). Bila planner ingin presisi absolut, alternatif = definisikan kelas font-size di blok `<style>` scoped (mis. `.coachee-ratio-sublabel { font-size: 0.6875rem; }`). Default rekomendasi tetap util Bootstrap per D-03. [ASSUMED — A2]

## JS Hook Preservation Map (KRITIS untuk D-08)

Setiap selector/id/atribut yang dibaca `@section Scripts` (L310-524). Markup HARUS mempertahankan semua ini agar behavior parity terjaga.

| Hook | Tipe | Dibaca di JS (baris) | Markup saat ini (baris) | Status saat refactor |
|------|------|---------------------|------------------------|----------------------|
| `#workloadChart` | id canvas | L318 `getElementById('workloadChart')`, L321 set height | L154 | **JANGAN sentuh** — bukan target, di card chart yg tak diubah |
| `.approve-btn` | class | L380 `querySelectorAll('.approve-btn')` | L254 | **PERTAHANKAN** di list-group-item |
| `.skip-btn` | class | L426 `querySelectorAll('.skip-btn')` | L262 | **PERTAHANKAN** di list-group-item |
| `.suggestion-card` | class (closest) | L393 `btn.closest('.suggestion-card')`, L431 | L240 (div card per item) | **PERTAHANKAN** sebagai class di `list-group-item` baru (D-08 eksplisit) |
| `#sug-{MappingId}` | id per item | L407/L444 `getElementById('sug-' + mappingId)` | L240 `id="sug-@sug.MappingId"` | **PERTAHANKAN** di `list-group-item` baru |
| `data-mapping-id` | attr | L382/L428 `btn.dataset.mappingId` | L255/L263 | **PERTAHANKAN** di `.approve-btn`/`.skip-btn` |
| `data-new-coach-id` | attr | L383 `btn.dataset.newCoachId` | L256 | **PERTAHANKAN** di `.approve-btn` |
| `data-coachee-name` | attr | L384 `btn.dataset.coacheeName` | L257 | **PERTAHANKAN** di `.approve-btn` |
| `data-from-coach` | attr | L385 `btn.dataset.fromCoach` | L258 | **PERTAHANKAN** di `.approve-btn` |
| `data-to-coach` | attr | L386 `btn.dataset.toCoach` | L259 | **PERTAHANKAN** di `.approve-btn` |
| `fadeOutCard(card)` | fn target | L408/L445 dipanggil pada elemen `#sug-{id}` | L470-475 def | **TETAP JALAN** — `cardEl.remove()` works di `list-group-item` (manipulasi opacity/transform generik, tidak butuh `.card`) |
| `[data-bs-toggle="collapse"]` + `.expand-chevron` | collapse chevron | L455-467 | L191-193 (di table card) | **JANGAN sentuh** — di table card yg tak diubah |
| `#thresholdModal`, `#maxCoachees`, `#warningThreshold`, `#saveThreshold`, `#thresholdError` | modal ids | L478-521 | L277-307 (modal) | **JANGAN sentuh** — modal tak diubah |
| `window.basePath` | global | L396/L434/L495 fetch prefix | (set di layout) | **JANGAN sentuh** |
| `input[name="__RequestVerificationToken"]` | anti-forgery | L375 `getAntiForgeryToken()` | L16 `@Html.AntiForgeryToken()` | **JANGAN sentuh** |

**Insight kritis:** `fadeOutCard()` (L470-475) memanipulasi `cardEl.style.transition/opacity/transform` lalu `cardEl.remove()`. Ini **generik** — tidak bergantung pada elemen berupa `.card`. Jadi memindahkan ke `list-group-item` AMAN selama elemen tetap punya `id="sug-{id}"`. Approve/skip handler ambil elemen via `getElementById('sug-' + mappingId)` (L407/L444) DAN via `btn.closest('.suggestion-card')` (L393/L431). **Keduanya** harus tetap match → `list-group-item` baru WAJIB punya `id="sug-@sug.MappingId"` DAN `class="... suggestion-card"`.

## Card Pattern Excerpt (template DSN-04 verbatim)

Template yang ditiru, dikutip dari file yang sama (zero ambiguity):

```html
<!-- TEMPLATE A — chart card (CoachWorkload.cshtml L150-162) -->
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-bar-chart me-2"></i>Grafik Beban Coach</div>
    <div class="card-body">
        ...isi...
    </div>
</div>

<!-- TEMPLATE B — table card (CoachWorkload.cshtml L165-167) -->
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-table me-2"></i>Detail Beban Coach</div>
    <div class="card-body">
        ...isi...
    </div>
</div>
```

**Filter bar (D-06) target — bungkus form L114-131 ke card:**
```html
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-funnel me-2"></i>Filter</div>
    <div class="card-body">
        <form method="get" class="d-flex gap-2">  @* hapus mb-4 — spacing pindah ke card mb-4 *@
            <select name="section" class="form-select" style="max-width: 300px;"> ... </select>
            <button type="submit" class="btn btn-primary">Filter</button>
            <a href="@Url.Action("CoachWorkload")" class="btn btn-outline-secondary">Reset</a>
        </form>
    </div>
</div>
```
> Catatan spacing: form saat ini `mb-4` (L114). Saat dibungkus card, pindahkan `mb-4` ke card; hapus `mb-4` dari form agar tak dobel-margin. Select `style="max-width:300px"` BIARKAN (D-05 fungsional).

## list-group Conversion Sketch (D-07 + D-08)

Item saran existing (L238-271): per item = `<div class="card shadow-sm mb-3 suggestion-card" id="sug-{id}">` → `<div class="card-body">` → konten. Target: **1 card luar** + `list-group list-group-flush` di card-body, tiap item = `list-group-item` (BUKAN card).

**Sketch konversi (mempertahankan semua hook D-08):**
```html
@* Saran Penyeimbangan — 1 card (D-07) *@
<div class="card shadow-sm mb-4">
    <div class="card-header fw-semibold"><i class="bi bi-arrow-left-right me-2"></i>Saran Penyeimbangan</div>
    @if (!reassignSuggestions.Any())
    {
        <div class="card-body">
            <div class="alert alert-success mb-0">  @* mb-0 di dalam card-body *@
                <i class="bi bi-check-circle me-2"></i>Tidak ada saran penyeimbangan saat ini. Semua coach memiliki beban yang seimbang.
            </div>
        </div>
    }
    else
    {
        <div class="list-group list-group-flush">
            @foreach (var sug in reassignSuggestions)
            {
                @* PARITY D-08: id + class suggestion-card WAJIB tetap *@
                <div class="list-group-item suggestion-card" id="sug-@sug.MappingId">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>@sug.CoacheeName</strong>
                            <span class="text-muted small ms-2">(@sug.CoacheeSection)</span>
                            <div class="text-muted small mt-1">
                                Dari <span class="fw-bold text-danger">@sug.FromCoachName</span>
                                &rarr; ke <span class="fw-bold text-success">@sug.ToCoachName</span>
                            </div>
                        </div>
                        @if (User.IsInRole("Admin"))  @* role-gate WAJIB tetap (D-08) *@
                        {
                            <div class="d-flex gap-2">
                                <button class="btn btn-primary btn-sm approve-btn"
                                        data-mapping-id="@sug.MappingId"
                                        data-new-coach-id="@sug.ToCoachId"
                                        data-coachee-name="@sug.CoacheeName"
                                        data-from-coach="@sug.FromCoachName"
                                        data-to-coach="@sug.ToCoachName">
                                    <i class="bi bi-check-circle me-1"></i>Setujui Saran
                                </button>
                                <button class="btn btn-outline-secondary btn-sm skip-btn"
                                        data-mapping-id="@sug.MappingId">
                                    <i class="bi bi-x-circle me-1"></i>Lewati Saran
                                </button>
                            </div>
                        }
                    </div>
                </div>
            }
        </div>
    }
</div>
```

**Catatan konversi:**
1. `<h5 class="mb-2">Saran Penyeimbangan</h5>` (L229) → DIHAPUS, judul pindah ke `card-header`.
2. Empty-state alert (L232-234) → masuk ke `card-body` dengan `alert ... mb-0` agar tak ada margin liar di dalam card. Empty-state TETAP ADA (D-08).
3. Setiap `list-group-item` mempertahankan `id="sug-@sug.MappingId"` + `class="...suggestion-card"` (D-08).
4. `list-group-flush` menghilangkan border luar list-group agar nyatu dengan card (idiom yang sama dipakai Results.cshtml L323 `list-group list-group-flush` di dalam card-body — precedent app).
5. Pertimbangan: card luar pakai `card shadow-sm mb-4` (konsisten Template A/B). Bila card luar ada `card-header` + `list-group-flush`, JANGAN bungkus list dengan `card-body` tambahan — `list-group-flush` langsung child card menghasilkan border yang nyatu (Bootstrap 5 idiom). Empty-state pakai `card-body` karena alert butuh padding.

**Precedent list-group-flush dalam card (Results.cshtml L322-323):**
```html
<!-- Source: Views/CMP/Results.cshtml L322-323 -->
<div class="card-body p-0">
    <div class="list-group list-group-flush">
```
> Results pakai `card-body p-0` lalu list-group-flush di dalamnya. Itu pola valid juga. Planner pilih: (a) list-group-flush langsung child card (tanpa card-body), atau (b) `card-body p-0` > list-group-flush. Keduanya menghasilkan visual flush. Rekomendasi (b) konsisten dgn Results.cshtml precedent app. [ASSUMED — A3]

## LBL-03 Exact Target (verified)

| Properti | Nilai |
|----------|-------|
| File | `Views/CMP/Results.cshtml` |
| Baris | **L60** (verified) |
| String saat ini | `<h6 class="text-muted mb-2">Nilai Kelulusan</h6>` |
| String target | `<h6 class="text-muted mb-2">Batas Nilai Kelulusan</h6>` |
| Baris di bawahnya (TIDAK diubah) | L61 `<h2 class="mb-0">@Model.PassPercentage%</h2>` |

**Grep konfirmasi:** `Nilai Kelulusan` muncul **tepat 1×** di seluruh tree `Views/` — hanya di `Results.cshtml:60`. [VERIFIED: Grep "Nilai Kelulusan" path=Views/ → 1 file, 1 match]. Tidak ada risiko co-replacement. Catatan: "Nilai Anda" (L53) berbeda string, tidak terpengaruh.

## Common Pitfalls

### Pitfall 1: Memutus hook JS saat konversi list-group (RISIKO #1)
**What goes wrong:** Tombol approve/skip tampil tapi tidak berfungsi (klik tak ada efek) atau card tidak hilang setelah approve.
**Why it happens:** Hilang/berubah `id="sug-{id}"`, class `suggestion-card`, salah satu `data-*`, atau `.approve-btn`/`.skip-btn`.
**How to avoid:** Ikuti §JS Hook Preservation Map persis. Diff visual markup lama vs baru: pastikan SEMUA id/class/data-* identik, hanya tag pembungkus & class layout (card→list-group-item) yang berubah.
**Warning signs:** Tombol "Setujui Saran" diklik tapi confirm dialog tak muncul (= `.approve-btn` listener tak terpasang) atau muncul tapi card tak fade-out (= `id` mismatch).

### Pitfall 2: Razor dinamis lolos build tapi rusak runtime (lesson Phase 354)
**What goes wrong:** `dotnet build` hijau, grep cocok, tapi UI rusak/JS putus saat runtime.
**Why it happens:** Razor compile tidak mengeksekusi JS handler atau memvalidasi selector match. Markup dinamis (`@foreach`, `@if role`) butuh runtime assert.
**How to avoid:** WAJIB `dotnet run` + Playwright/UAT browser. Assert: tombol setujui/lewati benar-benar jalan (klik → AJAX → card hilang); legend dot tampil bulat berwarna; font-size sublabel terlihat kecil; card framing tampil.
**Warning signs:** "Build sukses jadi selesai" — JANGAN. Phase 354/385/387 semua mengajarkan grep+build INSUFFICIENT.

### Pitfall 3: Spacing dobel/hilang setelah pindah margin ke card
**What goes wrong:** Gap antar section jadi terlalu besar (dobel `mb-4`) atau terlalu rapat (hilang margin form).
**Why it happens:** Form lama punya `mb-4` (L114), `<h5>` saran punya `mb-2` (L229). Saat dibungkus card, margin perlu dipindah ke card luar, bukan dobel.
**How to avoid:** Card luar = `mb-4`; hapus `mb-4`/`mb-3`/`mb-2` dari child yang sebelumnya pegang margin. Konsisten dgn chart/table card (`card shadow-sm mb-4`).
**Warning signs:** Visual gap tak seragam antara filter card / chart card / table card / saran card.

### Pitfall 4: Util `small` menggeser ukuran ±1px (kosmetik)
**What goes wrong:** Sublabel/badge sedikit beda ukuran dari sebelumnya.
**Why it happens:** `small` = 0.875em (relatif), magic-number = px absolut.
**How to avoid:** Terima pergeseran kecil (polish, bukan pixel-perfect). Bila tak bisa, pakai kelas font-size scoped di `<style>`. Default = util Bootstrap (D-03).
**Warning signs:** UAT reviewer komplain ukuran teks "agak beda" — jelaskan trade-off di plan.

## Runtime State Inventory

**Tidak berlaku.** Phase ini pure view markup edit (`.cshtml`) — bukan rename/refactor/migration. Tidak ada stored data, service config, OS-registered state, secrets, atau build artifact yang terdampak.

- Stored data: None — tidak ada DB write.
- Live service config: None — tidak ada perubahan endpoint/config.
- OS-registered state: None.
- Secrets/env vars: None — kecuali `Authentication__UseActiveDirectory=false` untuk run lokal (sudah ada, bukan baru).
- Build artifacts: None — Razor view di-compile saat build, tidak ada artifact stale.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (TypeScript) — `tests/playwright.config.ts` [VERIFIED: codebase] |
| Config file | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel: false`, globalTeardown restore DB) |
| Quick run command | `dotnet build` (Razor compile gate) |
| Full suite command | `cd tests; npx playwright test <spec> --workers=1` (`--workers=1` WAJIB — NTLM loopback/shared-memory SQL, ref: local e2e SQL env fix) |

> Catatan: app TIDAK auto-start (config tak punya `webServer`). Run manual: `dotnet run` dengan `Authentication__UseActiveDirectory=false` (login admin lokal `admin@pertamina.com`, ref reference_dev_credentials) DULU, baru jalankan Playwright. xUnit unit-test (`dotnet test`) juga ada untuk regresi backend — tapi phase ini 0 backend, jadi unit-test berfungsi sebagai guard "tidak ada yang rusak".

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LBL-03 | Label "Batas Nilai Kelulusan" tampil di Results | e2e / manual UAT | `cd tests; npx playwright test <new spec> --workers=1` | ❌ Wave 0 (atau manual UAT cukup — string statis) |
| DSN-04 | Filter bar + saran terbungkus card; filter submit tetap jalan | e2e + manual UAT | `cd tests; npx playwright test coach-workload-388 --workers=1` | ❌ Wave 0 |
| DSN-05 | Tidak ada inline font-size magic-number; legend dot pakai kelas; spacing seragam | manual UAT (visual) + grep guard | grep `font-size:1[12]px` di CoachWorkload = 0 match | ❌ Wave 0 (grep guard) |
| DSN-04/05 parity | Setujui/Lewati saran, set threshold (Admin), export, filter, chart render TETAP jalan | e2e (runtime) | `cd tests; npx playwright test coach-workload-388 --workers=1` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` (Razor compile harus hijau).
- **Per wave merge:** `dotnet run` (AD-off) + manual klik UAT 2 file + Playwright spec baru (bila ada).
- **Phase gate:** Full UAT browser hijau sebelum `/gsd-verify-work`. Parity penuh diverifikasi tuntas di Phase 390 (DSN-06) — tapi TIDAK boleh rusak di sini.

### Wave 0 Gaps
- [ ] `tests/e2e/coach-workload-388.spec.ts` — covers DSN-04 (card framing render) + parity (approve/skip/threshold/filter/export/chart). Selector reuse dari §JS Hook Preservation Map.
- [ ] Pertimbangkan: LBL-03 mungkin cukup manual UAT (string statis di Results) — planner putuskan apakah perlu spec dedicated atau verifikasi visual.
- [ ] Grep guard (bukan file): assert `font-size:11px`/`font-size:12px`/`font-size: 0.85rem` = 0 match di CoachWorkload.cshtml pasca-edit.

*Catatan: tidak ada spec CoachWorkload existing (`Glob **/*workload*` hanya menemukan screenshot, bukan spec). Phase 303 "Coach Workload 12-langkah UAT" = accepted-OK manual, bukan automated. Jadi spec baru = greenfield untuk surface ini.*

## Verification Plan (CONTEXT D-08 + lesson Phase 354)

Urutan verifikasi WAJIB (per CLAUDE.md Develop Workflow + lesson "grep+build tak cukup"):

1. **`dotnet build`** — Razor compile gate. Pastikan kedua `.cshtml` compile (terutama CoachWorkload dgn `@foreach`/`@if` di list-group baru).
2. **`dotnet run`** dengan `Authentication__UseActiveDirectory=false` → buka `http://localhost:5277`, login `admin@pertamina.com`.
3. **UAT browser CoachWorkload** (`/Admin/CoachWorkload` via menu Kelola Data):
   - Filter bar tampil dalam card (header ikon+"Filter"); pilih section → Filter submit → URL `?section=...` + data ter-filter; Reset kembali.
   - Section "Saran Penyeimbangan" tampil dalam 1 card; item = list-group rows (BUKAN card-in-card); empty-state muncul bila tak ada saran.
   - **Setujui Saran** (Admin): klik → confirm → AJAX → baris fade-out & hilang (parity D-08).
   - **Lewati Saran** (Admin): klik → AJAX → baris fade-out & hilang.
   - **Set Threshold** (Admin): modal buka → simpan → reload, badge status berubah.
   - **Export Excel**: tombol unduh file (PathBase-aware).
   - **Chart render**: bar chart tampil, legend dot bulat berwarna (Normal hijau / Mendekati kuning / Overloaded merah), font legend kecil seragam.
   - Sublabel "coachee/coach" (L93) & badge "!" (L104) tampil kecil & rapi.
4. **UAT browser Results** (LBL-03): selesaikan/ buka 1 hasil assessment → kartu tengah tampil "Batas Nilai Kelulusan" + persen tidak berubah.
5. **Playwright** (Wave 0 spec `coach-workload-388`): `cd tests; npx playwright test coach-workload-388 --workers=1` — runtime assert approve/skip/filter/chart.
6. **Grep guard:** konfirmasi 0 match `font-size:1[12]px` & `font-size: 0.85rem` di CoachWorkload.cshtml; 0 match `Nilai Kelulusan` (tanpa "Batas") di Results.cshtml.

**Lesson Phase 354/385/387 (dari STATE/MEMORY):** Razor dinamis + a11y/markup WAJIB Playwright runtime assert — grep+build INSUFFICIENT. JS-driven UI (approve/skip/chart) hanya teruji saat runtime.

## Project Constraints (from CLAUDE.md)

- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Verifikasi lokal WAJIB: `dotnet build` + `dotnet run` (localhost:5277) + Playwright bila ada, SEBELUM commit/push. JANGAN edit kode/DB langsung di Dev/Prod. JANGAN push tanpa verifikasi lokal.
- **Migration flag:** Phase ini = **migration=FALSE** (0 backend, 0 DB). Saat notify IT, sertakan commit hash + flag migration=FALSE.
- **Seed Data Workflow:** Bila butuh seed untuk e2e (saran penyeimbangan butuh data overload), klasifikasi `temporary + local-only`, snapshot DB sebelum, catat `docs/SEED_JOURNAL.md`, RESTORE setelah (sukses/gagal). Playwright globalTeardown (`tests/e2e/global.teardown.ts`) sudah handle restore — selaraskan dgn pola spec essay-grading-384.
- **Bahasa:** Respon developer & teks UI = Bahasa Indonesia (label, judul card, alert).

## State of the Art

Tidak ada perubahan ekosistem relevan. Bootstrap 5 utility classes (`small`, `fs-*`, `mb-*`, `g-*`) dan `list-group-flush` adalah API stabil yang sudah dipakai luas di codebase. Tidak ada deprecation yang berdampak.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Card class untuk D-06/D-07 sebaiknya `card shadow-sm` (tiru chart/table card berdampingan) bukan `card border-0 shadow-sm` per teks D-06 | Architecture Patterns / Card Pattern | LOW — kosmetik border; planner/UAT putuskan. Konsistensi visual = pilih satu, jangan campur. |
| A2 | `small` (0.875em) adalah util terdekat untuk 11px/12px/0.85rem | Inline-Style Inventory | LOW — pergeseran ±1px kosmetik; alternatif kelas font-size scoped tersedia |
| A3 | list-group-flush dalam card pakai `card-body p-0` > list-group-flush (precedent Results.cshtml) ATAU list-group-flush langsung child card; keduanya valid | list-group Conversion Sketch | LOW — keduanya menghasilkan visual flush; UAT verifikasi |

**Catatan:** Semua assumption = LOW risk kosmetik dalam ranah Claude's Discretion (D-03/D-04/spacing). Tidak ada assumption yang menyentuh behavior/JS-contract.

## Open Questions

1. **Card class `border-0` vs tidak (A1)**
   - What we know: D-06 teks tulis `card border-0 shadow-sm`; chart/table card existing pakai `card shadow-sm` (tanpa border-0); summary cards pakai `card border-0 shadow-sm`.
   - What's unclear: konsistensi visual diinginkan dgn yang mana (section berdampingan = chart/table = tanpa border-0).
   - Recommendation: Untuk konsistensi dgn section sekitarnya (chart/table card), pakai `card shadow-sm`. Bila planner ingin literal D-06, pakai `border-0` — tapi samakan SEMUA 4 card (filter/chart/table/saran) agar seragam. Default: `card shadow-sm` (tiru tetangga langsung).

2. **LBL-03 perlu spec automated atau cukup manual UAT?**
   - What we know: string statis, low-risk, 1-baris.
   - What's unclear: apakah milestone butuh automated coverage untuk LBL-03 atau visual UAT cukup.
   - Recommendation: manual UAT cukup untuk LBL-03 (string statis non-JS); fokus automated effort ke CoachWorkload parity (DSN-04/05) yg berisiko JS.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (dotnet) | build + run | ✓ (asumsi — project aktif) | — | — |
| Playwright (`tests/`) | e2e parity | ✓ (`tests/playwright.config.ts` + 25 spec) | @playwright/test | manual UAT browser |
| SQL Server lokal (HcPortalDB) | seed + e2e data | ✓ (workflow CLAUDE.md) | — | manual seed via UI |
| Chart.js CDN (jsdelivr) | chart render (tak diubah) | ✓ (runtime browser) | 4 | — (offline = chart kosong, bukan blok phase) |

**Missing dependencies with no fallback:** None. Semua tool sudah terpasang (project aktif v31.0 baru ship).

> Catatan: Step 2.6 audit dijalankan ringan — phase ini pure view, dependensi = harness verifikasi yang sudah ada (dotnet, Playwright). Tidak ada tool baru.

## Security Domain

Phase ini **tidak mengubah** authentication, access control, input validation, atau cryptography. Yang relevan untuk dijaga (parity, BUKAN diubah):

| ASVS Category | Applies | Standard Control (TIDAK diubah) |
|---------------|---------|---------------------------------|
| V4 Access Control | yes (preserve) | `User.IsInRole("Admin")` role-gate pada tombol setujui/lewati & modal threshold — D-08 WAJIB pertahankan |
| V5 Input Validation | no (no new input) | — (filter `<select>` server-validated di controller, tak disentuh) |
| V13 API / CSRF | yes (preserve) | `@Html.AntiForgeryToken()` (L16) + `RequestVerificationToken` header (L400/L438/L499) — JANGAN sentuh |

**Threat pattern relevan:** Satu-satunya risiko keamanan dari phase ini = **menghapus role-gate secara tak sengaja** saat refactor list-group (mis. memindahkan tombol keluar blok `@if (User.IsInRole("Admin"))`). Mitigasi: §list-group Conversion Sketch mempertahankan `@if (User.IsInRole("Admin"))` membungkus div tombol persis seperti L251. Verifikasi: login sebagai HC (non-Admin) → tombol setujui/lewati & "Set Threshold" TIDAK tampil (notice HC read-only L63-68 tetap).

## Sources

### Primary (HIGH confidence)
- `Views/Admin/CoachWorkload.cshtml` (L1-525) — target DSN-04/05; inline-style inventory; JS hook map. [Read penuh]
- `Views/CMP/Results.cshtml` (L1-443) — target LBL-03; precedent list-group-flush dalam card (L322-323). [Read penuh]
- `Views/CMP/AnalyticsDashboard.cshtml` (L33-34, L252-254) — precedent `.legend-dot` + `.legend-item`. [Grep + Read]
- `tests/playwright.config.ts` — baseURL/test harness. [Read]
- `tests/e2e/essay-grading-384.spec.ts` (L1-60) — pola login + seed/restore + `--workers=1`. [Read]
- `.planning/STATE.md`, `.planning/REQUIREMENTS.md`, CONTEXT.md, `.planning/config.json` — locked decisions + nyquist_validation=true. [Read]
- Grep "Nilai Kelulusan" path=Views/ → 1 file, 1 match (L60). [VERIFIED]
- Grep "legend-dot" → 2 files (388-CONTEXT, AnalyticsDashboard). [VERIFIED]

### Secondary (MEDIUM confidence)
- MEMORY.md context (lesson Phase 354/385/387: Razor dinamis WAJIB Playwright runtime). [Project memory]

### Tertiary (LOW confidence)
- None — phase ini fully codebase-grounded, tidak ada riset eksternal.

## Metadata

**Confidence breakdown:**
- LBL-03 target: HIGH — grep verified 1 match, exact line.
- Inline-style inventory: HIGH — setiap inline style di-baca langsung dari file, klasifikasi per D-03/D-05.
- JS hook map: HIGH — setiap selector di-trace ke baris JS di `@section Scripts`.
- Card/list-group conversion: HIGH (struktur) / MEDIUM (pilihan class kosmetik = discretion, lihat Assumptions).
- Verification plan: HIGH — harness Playwright existing + commands verified.

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (stable — view markup, tidak ada fast-moving dependency; valid selama file target tak di-refactor pihak lain)
