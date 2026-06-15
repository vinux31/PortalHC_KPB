# Phase 361: Bypass UI (B) - Research

**Researched:** 2026-06-11
**Domain:** ASP.NET Core MVC Razor view (server-rendered) + Bootstrap 5.3 + vanilla-JS fetch UI consuming existing JSON endpoints. Internal-codebase research (NOT web research).
**Confidence:** HIGH (semua temuan diverifikasi langsung dari source codebase via Read/Grep)

## Summary

Phase 361 adalah fase **UI murni** yang membungkus 6 endpoint JSON bypass yang sudah final di Phase 360 (`ProtonDataController.cs:1499-1684`) ke dalam Tab2 baru di `/ProtonData/Override`. Tidak ada logika domain baru — wizard, panel pending, dan modal konfirmasi semuanya hanya merangkai response JSON existing dengan markup Bootstrap dan pola fetch vanilla yang sudah hidup di `Override.cshtml`. Satu-satunya sentuhan backend yang disahkan adalah extend `SELECT` di `BypassPendingList` (D-18) — query-only, tanpa migration, tanpa ubah kontrak field existing. [VERIFIED: codebase]

Stack realitas sudah dikonfirmasi UI-SPEC: server-rendered MVC, **bukan** React/Next/Vite — tidak ada shadcn/Tailwind. Nilai fase ini adalah konsistensi visual dengan Tab1 yang **tidak boleh berubah perilaku** (PBYP-08, D-01). Semua pola yang dibutuhkan (cascade filter client-side, AntiForgery fetch header, modal Bootstrap, spinner-on-button) sudah ada di `Override.cshtml` dan tinggal direplikasi. [VERIFIED: codebase]

Dua gap nyata yang harus planner selesaikan SEBELUM menulis task: (1) **tidak ada helper toast global** — toast yang dirujuk UI-SPEC (`_Layout.cshtml:289`) berada di dalam blok kondisional impersonation read-only, bukan helper reusable; planner harus membangun helper toast kecil. (2) **sumber data dropdown coach belum ada** — `Override()` GET hanya set `ViewBag.AllTracks` + `ViewBag.SectionUnitsJson`, tidak ada daftar coach; pola yang sudah terbukti adalah `_userManager.GetUsersInRoleAsync(UserRoles.Coach)` (lihat `CoachMappingController.cs:146-149`). [VERIFIED: codebase]

**Primary recommendation:** Replikasi pola `Override.cshtml` (cascade client-side dari `ViewBag.SectionUnitsJson`, `appUrl()`/`basePath` helper, `RequestVerificationToken` header, spinner-on-button), bungkus Tab1 markup existing apa adanya dalam `tab-pane` Bootstrap, dan tulis JS Tab2 sebagai IIFE terpisah di file `<script>` yang sama (pola dominan codebase). Tambah helper toast reusable + endpoint/ViewBag coach. Verifikasi nama kolom `AssessmentSession` (`Score`/`IsPassed`/`CompletedAt`) sudah dikonfirmasi REAL di research ini.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Tab shell + deep-link routing | Browser (vanilla JS) | — | Tab activation, `history.replaceState`, URL param read = murni client; tidak ada server-side `ActiveTab` dipakai (beda dari ManageAssessment) karena param juga harus trigger modal-open |
| Cascade filter Bagian→Unit→Track | Browser (vanilla JS) | Frontend Server (ViewBag) | Cascade client-side dari `ViewBag.SectionUnitsJson` (pre-loaded), BUKAN fetch per-level — pola Tab1 existing |
| Worker table + pending panel render | Browser (vanilla JS) | API (`BypassList`/`BypassPendingList`) | Fetch JSON → render innerHTML, search nama client-side |
| Wizard 3-step state + validasi client | Browser (vanilla JS) | API (`BypassDetail` untuk eligibleModes) | State wizard di JS; backend re-validasi semua (V5) di `BypassSave` |
| Coach dropdown data | Frontend Server (ViewBag/endpoint) | — | Daftar coach harus dari server (Identity role lookup); planner pilih ViewBag vs endpoint kecil |
| Bypass save/confirm/cancel | API (existing 360 endpoints) | Browser (POST fetch) | Semua mutasi + transaksi + audit sudah di backend; UI hanya POST + tampilkan message |
| Re-grade Pass→Fail cascade | API (GradingService existing) | Browser (UAT trigger via Edit Nilai UI) | Logika sudah xUnit-covered (360); UI hanya assert efek |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC Razor | net (existing project) | Server-rendered view | Stack proyek; Tab2 = view extension, bukan komponen baru [VERIFIED: HcPortal.csproj] |
| Bootstrap | 5.3 | `nav-tabs`, `modal`, `card`, `badge`, `toast`, `form-select`, `spinner-border` | Sudah di-bundle `_Layout.cshtml`; UI-SPEC melarang token baru [CITED: 361-UI-SPEC.md] |
| Bootstrap Icons | CDN (loaded `_Layout.cshtml`) | `bi bi-*` ikon | Sudah loaded global [VERIFIED: codebase] |
| vanilla JS (fetch API) | native | AJAX ke endpoint JSON, render, modal control | Pola dominan `Override.cshtml` — TIDAK pakai HTMX di page ini [VERIFIED: Override.cshtml] |

### Supporting (testing)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Playwright | (existing `tests/`) | e2e spec committed (D-22) + UAT live MCP | `tests/e2e/*.spec.ts`, config `tests/playwright.config.ts` [VERIFIED] |
| xUnit | (existing `HcPortal.Tests`) | Sudah cover service logic 360 | TIDAK perlu test baru untuk bypass logic — sudah ada `ProtonBypassServiceTests.cs` [VERIFIED] |
| sqlcmd (SQL Server Express) | system | Seed fixture + snapshot/restore | Via `tests/helpers/dbSnapshot.ts` (`backup`/`restore`/`execScript`) [VERIFIED] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| vanilla-JS fetch (Override pola) | HTMX (ManageAssessment pola) | HTMX dipakai di `ManageAssessment.cshtml` untuk tab loading, TAPI Override.cshtml murni vanilla. **Jangan campur** — D-01 modal multi-step + state wizard JS lebih natural vanilla. Pilih vanilla untuk konsistensi page. |
| Server-set `ViewBag.ActiveTab` (ManageAssessment) | Client-side URLSearchParams + `bootstrap.Tab().show()` | ManageAssessment baca tab dari server. Phase 361 butuh client-side karena `?pending={id}` juga harus auto-open modal — logika lebih bersih full client (D-05/D-08). |

**Installation:** Tidak ada dependency baru. Semua sudah ter-bundle.

**Version verification:** N/A — tidak ada package npm/nuget baru. Bootstrap 5.3 + Bootstrap Icons sudah live di `_Layout.cshtml` [VERIFIED: codebase].

## Architecture Patterns

### System Architecture Diagram

```
                    ┌─────────────────────────────────────────────────────┐
   Lonceng notif    │  /ProtonData/Override?tab=bypass&pending={id}        │
   PROTON_BYPASS ──▶│  (GET Override action — set ViewBag AllTracks +      │
   _READY (360)     │   SectionUnitsJson + [NEW] coach list)               │
                    └───────────────────────┬─────────────────────────────┘
                                            │ Razor render
                                            ▼
            ┌───────────────────────────────────────────────────────────┐
            │  Override.cshtml — 2 Bootstrap tabs                        │
            │  ┌─────────────────────┐  ┌──────────────────────────────┐ │
            │  │ Tab1 Override Deliv │  │ Tab2 Bypass Tahun (NEW)       │ │
            │  │ (existing, UNCHANGED)│  │                              │ │
            │  └─────────────────────┘  │ ┌──────────────────────────┐ │ │
            │                           │ │ Pending panel (always)   │ │ │
            │  [client JS reads URL param:                            │ │ │
            │   tab=bypass → show Tab2  │ │  GET BypassPendingList    │◀─┼─┼─ poll on activate
            │   pending={id} → openModal]│ └──────────────────────────┘ │ │
            │                           │ ┌──────────────────────────┐ │ │
            │                           │ │ Filter cascade + Muat Data│ │ │
            │                           │ │  (client cascade from     │ │ │
            │                           │ │   SectionUnitsJson)       │ │ │
            │                           │ │  GET BypassList ──────────┼─┼─┼─▶ worker table
            │                           │ └──────────────────────────┘ │ │
            │                           └──────────────────────────────┘ │
            └───────────────────────────────────────────────────────────┘
                  │ [Bypass] row click          │ [Lihat&Konfirmasi] / [Batal]
                  ▼                              ▼
          ┌──────────────────┐         ┌────────────────────────┐
          │ GET BypassDetail │         │ extended BypassPending  │
          │ → eligibleModes  │         │ List row (D-18 fields)  │
          └────────┬─────────┘         └───────────┬────────────┘
                   │ wizard step 2                 │ confirm modal
                   ▼                               ▼
          ┌──────────────────┐         ┌────────────────────────┐
          │ POST BypassSave  │         │ POST BypassConfirm /    │
          │ [FromBody] JSON   │         │ BypassCancelPending     │
          │ + AntiForgery hdr │         │ [FromBody] {pendingId}  │
          └────────┬─────────┘         └───────────┬────────────┘
                   │ { success, message,           │ { success, message }
                   │   pendingId,                  │
                   │   showAttachPackageReminder } │
                   ▼                               ▼
            toast + auto-refresh worker table + pending panel; modal close
```

### Recommended Project Structure
```
Views/ProtonData/
└── Override.cshtml          # 2 tab — Tab1 markup existing UTUH dibungkus tab-pane;
                             # Tab2 markup baru; JS Tab2 = IIFE terpisah di @section Scripts
                             # (partial _Tab2Bypass.cshtml = discretion planner, asal Tab1 utuh)
Controllers/
└── ProtonDataController.cs  # Override() GET: +coach list (ViewBag atau endpoint baru)
                             # BypassPendingList: extend SELECT (D-18) — query only
HcPortal.Tests/              # TIDAK ditambah — service sudah ter-cover 360
tests/e2e/
└── proton-bypass.spec.ts    # NEW — committed e2e (D-22); pakai login() helper + admin nav
tests/sql/  (atau .planning/seeds/)
└── bypass-fixtures.sql      # NEW — worker multi-state fixture (D-23), pola 313-timer-fixtures
```

### Pattern 1: Cascade filter CLIENT-SIDE dari pre-loaded JSON (BUKAN fetch per-level)
**What:** Dropdown Bagian→Unit di-populate dari objek JS `orgStructure` yang di-inject Razor, bukan AJAX.
**When to use:** Tab2 filter (D-14) dan TargetUnit cascade di wizard step 3 (D-11).
**Example:**
```javascript
// Source: Views/ProtonData/Override.cshtml:170-207 [VERIFIED]
const orgStructure = @Html.Raw(ViewBag.SectionUnitsJson ?? "{}");
// Bagian change → repopulate Unit dari orgStructure[bagian], enable/disable berurutan
overrideBagian.addEventListener('change', function () {
    var unitSelect = document.getElementById('overrideUnit');
    unitSelect.innerHTML = '<option value="">-- Pilih @OrgLabels.GetLabel(1) --</option>';
    if (this.value && orgStructure[this.value]) {
        orgStructure[this.value].forEach(function (u) { /* append option */ });
    }
    unitSelect.disabled = !this.value;
});
```
**Catatan:** `OrgLabels.GetLabel(0)`=Bagian, `GetLabel(1)`=Unit — label dinamis, jangan hardcode. Wizard step 3 butuh `orgStructure` yang sama → cukup reuse satu konstanta.

### Pattern 2: AntiForgery + base-path-aware fetch POST
**What:** POST `[FromBody]` JSON ke endpoint dengan token AntiForgery di header.
**When to use:** Semua mutasi (`BypassSave`/`BypassConfirm`/`BypassCancelPending`).
**Example:**
```javascript
// Source: Views/ProtonData/Override.cshtml:415-420 + _Layout.cshtml:54-55 [VERIFIED]
// basePath/appUrl WAJIB — Dev di-host di sub-path /KPB-PortalHC, hardcode '/ProtonData/...' rusak
var basePath = '@Url.Content("~/")'.replace(/\/$/, '');          // _Layout helper
function appUrl(path){ return basePath + (path.startsWith('/')?path:'/'+path); }
var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
var resp = await fetch(appUrl('/ProtonData/BypassSave'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
    body: JSON.stringify(payload)
});
var data = await resp.json();
```
**Catatan:** `@Html.AntiForgeryToken()` sudah di `Override.cshtml:22` — token field tersedia. Backend pakai `[ValidateAntiForgeryToken]` (verified `:1609,1652,1671`).

### Pattern 3: Spinner-on-button anti-double-click (D-21)
**What:** Disable tombol + ganti label dengan spinner selama request in-flight.
**Example:**
```javascript
// Source: Views/ProtonData/Override.cshtml:410-413,432-433 [VERIFIED]
btn.disabled = true;
btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Menyimpan...';
// ... await fetch ...
btn.disabled = false;
btn.innerHTML = '<i class="bi bi-save me-1"></i>Jalankan Bypass';
```

### Pattern 4: Bootstrap nav-tabs markup
**What:** `ul.nav.nav-tabs` dengan `button[data-bs-toggle="tab"][data-bs-target="#pane-x"]` + `div.tab-content > div.tab-pane`.
**Example:**
```html
<!-- Source: Views/Admin/ManageAssessment.cshtml:68-96 [VERIFIED] -->
<ul class="nav nav-tabs" id="overrideTabs" role="tablist">
  <li class="nav-item"><button class="nav-link active" id="tab-deliverable"
      data-bs-toggle="tab" data-bs-target="#pane-deliverable" type="button" role="tab">
      Override Deliverable</button></li>
  <li class="nav-item"><button class="nav-link" id="tab-bypass"
      data-bs-toggle="tab" data-bs-target="#pane-bypass" type="button" role="tab">
      <i class="bi bi-arrow-left-right me-1"></i>Bypass Tahun</button></li>
</ul>
<div class="tab-content pt-2">
  <div class="tab-pane fade show active" id="pane-deliverable"><!-- Tab1 markup existing UTUH --></div>
  <div class="tab-pane fade" id="pane-bypass"><!-- Tab2 baru --></div>
</div>
```
**Programmatic activation (deep-link D-05):**
```javascript
// Source pattern: ManageAssessment.cshtml:225-228 [VERIFIED]
new bootstrap.Tab(document.getElementById('tab-bypass')).show();
// Lazy-load Tab2 data on 'shown.bs.tab' first time (D-07), update URL on switch (D-08):
document.getElementById('tab-bypass').addEventListener('shown.bs.tab', loadTab2Once);
history.replaceState(null, '', appUrl('/ProtonData/Override?tab=bypass'));
```

### Anti-Patterns to Avoid
- **Mengubah Tab1 markup/JS perilaku:** D-01/PBYP-08 — Tab1 dibungkus apa adanya. JS Tab1 saat ini adalah IIFE `(function(){...})()` di `Override.cshtml:186-445`; biarkan utuh, tambah IIFE Tab2 terpisah.
- **Fetch per-level untuk cascade:** Tab1 TIDAK fetch unit list — pakai `orgStructure` client. Jangan buat endpoint cascade baru.
- **Hardcode path `/ProtonData/...`:** Rusak di Dev sub-path. Selalu `appUrl()`.
- **Pakai TempData untuk feedback:** D-04 — semua feedback via JSON response + toast. `BypassSave` action MASIH set `TempData["Warning"]` di `:1634` (peninggalan 360) TAPI juga return `showAttachPackageReminder` di JSON `:1646` — UI **konsumsi field JSON**, abaikan TempData.
- **Mengandalkan helper toast yang tidak ada:** lihat Pitfall 1.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Eligibility closure mode | Hitung sourceComplete/hasFinal di client | `BypassDetail.eligibleModes` (server) | Logika B-03 (CL-A butuh complete+final) sudah benar di `:1587-1593`; client cukup enable/disable card per `eligibleModes` |
| Validasi |Δtahun|≤1 final | Validasi hanya client | Client UX + backend re-validasi | `BypassDetail.sourceTahun` (Urutan global) untuk validasi client; backend `BypassSave`/service tetap re-cek (V5) |
| Pesan error | Tulis pesan sendiri | Surface `data.message` backend verbatim (D-20) | Service sudah punya pesan Indonesia ramah (lihat Code Examples) |
| Cancel exam saat batal pending | Logika hapus exam di UI | `BypassCancelPending` (auto-cancel §8.1) | Backend handle belum-dikerjakan→hapus, sudah-lulus→pertahankan |
| Re-grade Pass→Fail → pending balik Menunggu | Test logic baru | xUnit `Revert_PassFail_PendingBalikMenunggu` sudah ada | Phase 360 cover; UI e2e cukup assert badge (D-24) |

**Key insight:** Backend 360 adalah otoritas tunggal untuk SEMUA keputusan domain. UI Phase 361 tidak mengambil keputusan apa pun yang tidak di-echo dari response JSON — ini menjaga UI tipis dan menghindari split-brain logic.

## Runtime State Inventory

> Fase ini greenfield-UI (tambah view + minor select extend). Tidak ada rename/refactor/migrasi data. Inventory tetap diisi untuk kepatuhan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — UI hanya membaca/menulis via endpoint existing; tidak ada string key baru | None — verified: tidak ada migration (D-17 nilai DB `Menunggu`/`Siap` tetap, hanya label tampilan) |
| Live service config | None — tidak ada konfigurasi service eksternal disentuh | None — verified Read Override()/endpoints |
| OS-registered state | None | None |
| Secrets/env vars | None baru. Catatan ops: UAT lokal WAJIB `Authentication__UseActiveDirectory=false dotnet run` (appsettings handoff AD=true) | None — env flag run-time, bukan kode |
| Build artifacts | None | None — `dotnet build` regenerate normal |

**Notif template:** `PROTON_BYPASS_READY` + `ActionUrlTemplate="/ProtonData/Override?tab=bypass&pending={PendingId}"` sudah shipped 360 — UI hanya handle param, tidak ubah template. [VERIFIED: 361-CONTEXT.md:97]

## Common Pitfalls

### Pitfall 1: Tidak ada helper toast global — UI-SPEC reference menyesatkan
**What goes wrong:** UI-SPEC menyebut "Toast `_Layout.cshtml:289-293`" sebagai pola reusable. Faktanya toast itu **di dalam blok `if (document.body.dataset.impersonating === 'true')`** (`_Layout.cshtml:270-302`) — hanya muncul saat impersonation read-only intercept 403. **Tidak ada fungsi `showToast()` global** di codebase.
**Why it happens:** Grep `text-bg-danger` menemukan satu hit yang terlihat seperti pola umum.
**How to avoid:** Planner WAJIB membuat helper toast reusable kecil (markup `toast align-items-center text-bg-{variant} position-fixed top-0 end-0` sesuai pola di `:289-291`, tapi diekstrak jadi fungsi). Toast warna: hijau sukses, merah error (verbatim `data.message`), kuning info (deep-link stale D-06). [VERIFIED: _Layout.cshtml:270-302 — toast hanya kondisional]
**Warning signs:** Memanggil `showToast(...)` tanpa mendefinisikannya → ReferenceError silent di console.

### Pitfall 2: Sumber data dropdown coach belum ada di Override()
**What goes wrong:** Wizard step 3 butuh `<select>` coach (D-12), tapi `Override()` GET (`:231-243`) hanya set `ViewBag.AllTracks` + `ViewBag.SectionUnitsJson` — **tidak ada coach list**.
**Why it happens:** 360 tidak membutuhkan coach list (backend ambil dari payload).
**How to avoid:** Planner pilih salah satu (CONTEXT risiko — jangan hardcode):
- **Opsi A (paling simpel):** Tambah `ViewBag.AllCoaches` di `Override()` via `_userManager.GetUsersInRoleAsync(UserRoles.Coach)` lalu `.Where(u => u.IsActive).OrderBy(u => u.FullName)` — pola PERSIS `CoachMappingController.cs:146-149`. Render `<option>` semua coach. Cocok karena D-12 default "Pertahankan coach saat ini" + dropdown opsional (tidak perlu filter per-unit ketat).
- **Opsi B (lebih presisi):** Endpoint kecil GET yang return coach per unit target (re-validasi backend tetap menolak coach invalid via constraint E15). Lebih banyak kode.
**Rekomendasi:** Opsi A — selaras D-12 (coach opsional, default keep), backend service sudah handle deactivate-lama/create-baru + constraint E15. [VERIFIED: CoachMappingController.cs:146-149, ProtonBypassService E15 message :207]
**Warning signs:** Hardcode daftar coach, atau dropdown coach kosong di wizard.

### Pitfall 3: Razor `@model dynamic` / runtime binding di partial
**What goes wrong:** (Lesson dari Phase 354) — partial view dengan `@model dynamic`/`object` + akses properti yang absen → `RuntimeBinderException` 500 yang **tidak terdeteksi build maupun grep**.
**Why it happens:** Razor dynamic resolve di runtime.
**How to avoid:** Jika planner pecah jadi partial (`_Tab2Bypass.cshtml`), gunakan model typed atau markup statik (data di-fetch JS, bukan Razor model). Tab2 bersifat data-driven JS — markup Razor cukup kerangka kosong + container `id`. **WAJIB verifikasi via Playwright runtime**, bukan hanya build. [CITED: MEMORY project_354 lesson]
**Warning signs:** Build hijau tapi page 500 saat tab dibuka.

### Pitfall 4: BypassPendingList SELECT extend — verifikasi nama kolom (D-18)
**What goes wrong:** Extend SELECT dengan nama kolom salah → compile error atau null silent.
**How to avoid:** Nama kolom REAL sudah diverifikasi research ini (lihat Code Examples). `AssessmentSession`: `Score` (int?), `IsPassed` (bool?), `CompletedAt` (DateTime?). `PendingProtonBypass`: `Reason` (string), `TargetCoachId` (string?), `CreatedAt`. Join coach name via `Users` on `TargetCoachId` (nullable → LEFT JOIN/DefaultIfEmpty). [VERIFIED: AssessmentSession.cs:26,38,39; ProtonModels.cs:235-249]
**Warning signs:** `s.Skor` (salah — `Score`), `s.Completed` (salah — `CompletedAt`).

### Pitfall 5: Mencampur pola HTMX ManageAssessment dengan vanilla Override
**What goes wrong:** Meniru ManageAssessment yang pakai `hx-get`/`hx-trigger` untuk tab loading → konflik dengan fetch vanilla + state wizard JS.
**How to avoid:** Page Override = murni vanilla. Ambil HANYA markup `nav-tabs` dari ManageAssessment; lazy-load (D-07) via `shown.bs.tab` event + `fetch`, bukan htmx. [VERIFIED: Override.cshtml vanilla vs ManageAssessment.cshtml:97-99 htmx]

## Code Examples

Verified patterns dari source codebase:

### 6 Endpoint bypass — response shape EXACT (kontrak UI)
```csharp
// Source: Controllers/ProtonDataController.cs:1499-1684 [VERIFIED]

// GET BypassList?bagian=&unit=&trackId=  → array rows:
//   { coacheeId, nama, trackId, trackAktif, progressApproved, progressTotal, finalAda(bool) }   (:1514-1526)

// GET BypassPendingList  → array rows (status Menunggu|Siap):
//   { id, coacheeId, nama, sourceTrack, targetTrack, targetUnit, status, hasilExam(bool? IsPassed), createdAt }  (:1546-1555)
//   D-18 EXTEND tambah: skor exam, tanggal selesai exam, reason, nama coach target (lihat di bawah)

// GET BypassDetail?coacheeId=X  → wizard state:
//   sukses: { success:true, sourceTrackId, sourceTahun(Urutan global 1-6 utk Δ≤1),
//             sourceTahunKe(label "Tahun 1" utk display), sourceComplete(bool),
//             sourceHasFinal(bool), eligibleModes:[...] }   (:1595-1604)
//   gagal:  { success:false, message }  (mis. assignment aktif != 1 → message :1572)
//   eligibleModes logic (:1590-1593): CL-A bila complete&&final; CL-B(a)+CL-B(b) bila !final; CL-C selalu

// POST BypassSave  [FromBody] BypassSaveRequest, [ValidateAntiForgeryToken]
//   → { success, message, pendingId(int?), showAttachPackageReminder(bool) }   (:1641-1647)

// POST BypassConfirm  [FromBody] {PendingId:int}  → { success, message }   (:1665)
// POST BypassCancelPending  [FromBody] {PendingId:int}  → { success, message }   (:1683)
```

### BypassSaveRequest payload shape (yang UI kirim)
```csharp
// Source: Controllers/ProtonDataController.cs:80-90 [VERIFIED]
public class BypassSaveRequest {
    public string CoacheeId;            // wajib (V5 :1616)
    public int SourceProtonTrackId;
    public int TargetProtonTrackId;
    public string TargetUnit;           // WAJIB (360 WR-02, V5 :1622) — dropdown cascading D-11
    public string? TargetCoachId;       // null = pertahankan coach (D-12)
    public string Reason;               // wajib (V5 :1618)
    public string Mode;                 // "CL-A"|"CL-B(a)"|"CL-B(b)"|"CL-C" (validModes :1624)
    public int? DurationMinutes;        // D-09: UI TIDAK kirim (default murni) → null
}
// BypassConfirm/Cancel payload: { PendingId:int } (:92-95)
```

### D-18 SELECT extend — nama kolom REAL terverifikasi
```csharp
// Source verifikasi: AssessmentSession.cs:26,38,39 + ProtonModels.cs:235-249 [VERIFIED]
// Extend di BypassPendingList (:1545-1555). Tambah ke anonymous projection:
select new {
    /* existing fields tetap (jangan ubah kontrak) */
    id = p.Id, coacheeId = p.CoacheeId, nama = ..., sourceTrack = ts.DisplayName,
    targetTrack = tt.DisplayName, targetUnit = p.TargetUnit, status = p.Status,
    hasilExam = s != null ? s.IsPassed : null, createdAt = p.CreatedAt,
    // ===== NEW (D-18) =====
    skorExam = s != null ? s.Score : null,            // AssessmentSession.Score  (int?)
    tanggalExam = s != null ? s.CompletedAt : null,   // AssessmentSession.CompletedAt (DateTime?)
    reason = p.Reason,                                // PendingProtonBypass.Reason
    targetCoachId = p.TargetCoachId,                  // PendingProtonBypass.TargetCoachId (string?)
    // nama coach target — LEFT JOIN Users on p.TargetCoachId (nullable, pakai .DefaultIfEmpty()):
    targetCoachNama = /* join Users c on p.TargetCoachId == c.Id → c.FullName ?? c.UserName */
}
// CATATAN: p.LinkedAssessmentSessionId NON-nullable (int) tapi join s pakai DefaultIfEmpty()
//          (existing :1542-1543) — null-safe sudah ada. Tambah join Users untuk coach name.
```

### Pesan error backend (surface verbatim di toast D-20)
```
// Source: Services/ProtonBypassService.cs [VERIFIED] — semua Indonesia, ramah, tanpa ex.Message (D6)
"Worker punya {n} assignment aktif (harus tepat 1)."                       (:111,231)
"Track asal/tujuan tidak ditemukan."                                       (:126,240)
"Worker sudah punya rencana bypass aktif. Selesaikan/batalkan dulu."       (:259)
"Kondisi rencana sudah berubah (assignment/exam). Konfirmasi dibatalkan."  (:505) ← stale D-11
"Pending sudah diproses (klik ganda)."                                     (:516) ← race D-12
"Pending tidak bisa dibatalkan (status tidak valid)."                      (:569)
"Bypass berhasil.{warnSuffix}" / "Pindah tahun berhasil." / "Rencana bypass dibatalkan."  (:198,535,609)
```

### Playwright e2e — login + admin nav pattern
```typescript
// Source: tests/helpers/auth.ts + tests/helpers/accounts.ts [VERIFIED]
import { login } from '../helpers/auth';
// accounts: admin(admin@pertamina.com/123456), hc(meylisa.tjiang@pertamina.com/123456)
await login(page, 'hc');                                  // login → waitForURL **/Home/**
await page.goto('/ProtonData/Override?tab=bypass');       // baseURL http://localhost:5277 (config)
// deep-link assert: await expect(page.locator('#pane-bypass')).toBeVisible();
```

### Seed fixture + snapshot/restore (D-23)
```typescript
// Source: tests/helpers/dbSnapshot.ts [VERIFIED] — localhost-only guard built-in
import * as db from '../helpers/dbSnapshot';
await db.backup(snapshotPath);          // BACKUP DATABASE ... WITH INIT, FORMAT (default backup dir)
await db.execScript(SEED_SQL);          // sqlcmd -i <fixture.sql>  (-b flag = fail-on-error)
// ... test ...
await db.restore(snapshotPath);         // SINGLE_USER ROLLBACK IMMEDIATE → RESTORE → MULTI_USER
// SEED_JOURNAL.md append status=active (setup) → cleaned (teardown) — pola global.setup.ts:122-129
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Feedback via TempData (360 D-02) | Feedback via JSON response + toast (D-04) | Phase 361 | UI baca `showAttachPackageReminder` field, bukan TempData |
| Spec §9 "panel pending (kalau ada)" | Panel SELALU tampil + empty state (D-16) | Phase 361 | Deviasi sadar — HC selalu aware antrian |
| Spec §9 redesign | UI-SPEC.md approved (otoritas layout final) | 2026-06-11 | Token/copy/interaction terkunci di UI-SPEC |

**Deprecated/outdated:** Tidak ada. Semua endpoint 360 final & live.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Opsi A (ViewBag.AllCoaches via GetUsersInRoleAsync) cukup untuk dropdown coach | Pitfall 2 | Jika butuh filter coach per-unit ketat di UI, perlu endpoint (Opsi B). Mitigasi: backend constraint E15 tetap menolak coach invalid — risk rendah |
| A2 | Coach name di-join via Users on TargetCoachId untuk D-18 | Code Examples (D-18) | Jika TargetCoachId menyimpan format lain (bukan User.Id), join gagal → null. Mitigasi: `TargetCoachId` di-set dari payload `TargetCoachId` yang sama dengan dropdown User.Id → konsisten |

**Catatan:** Hanya 2 asumsi, keduanya low-risk dengan mitigasi backend. Semua claim lain VERIFIED dari source.

## Open Questions

1. **Pecah partial view `_Tab2Bypass.cshtml` atau inline?**
   - What we know: CONTEXT discretion planner; Tab1 markup WAJIB utuh.
   - What's unclear: Trade-off keterbacaan vs risiko RuntimeBinder (Pitfall 3).
   - Recommendation: Inline di `Override.cshtml` (kerangka markup) + IIFE JS terpisah. Jika dipecah partial, gunakan model typed/statik (bukan dynamic) dan WAJIB Playwright runtime verify.

2. **Struktur file JS: inline `<script>` vs `wwwroot/js/`?**
   - What we know: Override.cshtml saat ini inline IIFE di `@section Scripts`.
   - Recommendation: Inline IIFE Tab2 di `@section Scripts` yang sama (pola dominan page ini) — IIFE terpisah dari IIFE Tab1 agar Tab1 utuh.

3. **`?pending={id}` stale detection (D-06): client-side atau dari BypassPendingList?**
   - What we know: `BypassPendingList` hanya return status `Menunggu`|`Siap` (`:1537`) — pending Selesai/Dibatalkan TIDAK muncul.
   - Recommendation: Setelah load pending panel, cari `{id}` di array hasil. Tidak ketemu → toast info stale (D-06) + Tab2 normal. Tidak perlu endpoint baru.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (dotnet build/run) | Verifikasi lokal | ✓ (project aktif) | net (csproj) | — |
| SQL Server Express (localhost\SQLEXPRESS) | Seed fixture + snapshot/restore | ✓ (HcPortalDB_Dev) | — | — |
| sqlcmd | dbSnapshot.ts backup/restore | ✓ (helper assumes -E -C -I -b) | — | — |
| Playwright (`tests/`) | e2e committed + UAT MCP | ✓ (config + node_modules) | @playwright/test (tests/node_modules) | UAT MCP live tanpa spec committed |
| Bootstrap 5.3 + Bootstrap Icons | UI render | ✓ (bundled _Layout) | 5.3 | — |

**Missing dependencies with no fallback:** Tidak ada.
**Missing dependencies with fallback:** Tidak ada — semua infra UAT/seed sudah terbukti dipakai Phase 313/315/355.

**Catatan ops:** UAT lokal WAJIB jalankan `Authentication__UseActiveDirectory=false dotnet run` (appsettings handoff AD=true; admin lolos via hybrid). [CITED: CLAUDE.md + MEMORY 355 lesson]

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework (e2e) | Playwright @playwright/test |
| Config file | `tests/playwright.config.ts` (testDir `./e2e`, baseURL `http://localhost:5277`, globalTeardown RESTORE) |
| Framework (unit) | xUnit `HcPortal.Tests/HcPortal.Tests.csproj` — service bypass SUDAH ter-cover (360), TIDAK ditambah |
| Quick run command | `dotnet build` (compile gate) |
| Full e2e command | `cd tests && npx playwright test proton-bypass.spec.ts` (setelah `dotnet run` AD=false @5277) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated/Manual Command | File Exists? |
|--------|----------|-----------|--------------------------|-------------|
| PBYP-08 | 2 tab; Tab1 unchanged; wizard 3-step Tujuan→Closure→Detail | e2e + UAT | `playwright test proton-bypass.spec.ts` (assert tab switch, wizard nav, Tab1 markup intact) | ❌ Wave 0 |
| PBYP-08 | 4 closure mode save (CL-A/B(a)/B(b)/C) | e2e + UAT | spec: per-mode BypassSave → success toast; CL-B(b) → alert kuning | ❌ Wave 0 |
| PBYP-09 | Panel pending + [Konfirmasi]/[Batal]; deep-link `?tab=bypass&pending={id}` auto-open modal | e2e + UAT | spec: goto deep-link → modal open; confirm/cancel flow | ❌ Wave 0 |
| PBYP-10 | UAT e2e: 4 mode + pending konfirmasi + batal + re-grade fail | e2e committed (D-22) + live MCP | full spec + Playwright MCP @5277 manual UAT | ❌ Wave 0 |
| PBYP-10 | re-grade Pass→Fail → badge balik "Menunggu Exam", tombol Konfirmasi hilang | e2e UI ringan (D-24) | trigger via `/AssessmentAdmin/EditPesertaAnswers` (RegradeAfterEditAsync :3265) → assert badge | ❌ Wave 0 (logic xUnit-covered) |

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error) — gate compile setelah ubah view/controller.
- **Per wave merge:** `dotnet build` + Playwright spec relevan @5277 (AD=false).
- **Phase gate:** Full spec hijau + UAT live MCP 4 mode + pending konfirmasi + batal + re-grade fail (Success Criteria 1-3) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `tests/e2e/proton-bypass.spec.ts` — covers PBYP-08/09/10 (pembagian file 1 vs per-skenario = discretion D-22).
- [ ] `tests/sql/bypass-fixtures.sql` (atau `.planning/seeds/`) — worker multi-state (komplit CL-A, partial CL-B, punya final→tolak CL-B, exam in-progress E5) per D-23; pola `313-timer-fixtures.sql` (WIPE-AND-INSERT idempotent + THROW guard user + BEGIN TRAN).
- [ ] Reuse `tests/helpers/dbSnapshot.ts` (backup/restore/execScript) + `tests/helpers/auth.ts` (login) — TIDAK perlu helper baru.
- [ ] SEED_JOURNAL.md entry (active→cleaned) — pola `global.setup.ts:122-129`.
- [ ] Framework install: TIDAK perlu — `tests/node_modules` sudah ada.

**Catatan:** xUnit `HcPortal.Tests` (ProtonBypassServiceTests/EndpointTests/ValidationTests) sudah meng-cover logika service termasuk `Revert_PassFail_PendingBalikMenunggu` (test :496). Phase 361 TIDAK menambah xUnit — re-grade UAT (D-24) hanya assert efek UI.

## Security Domain

> `security_enforcement` tidak ditemukan `false` eksplisit — disertakan. Fase UI tipis, permukaan keamanan minim.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin,HC")]` di `ProtonDataController` class-level (`:97`) — Tab2 + endpoint terlindungi otomatis [VERIFIED] |
| V3 Session Management | no | ASP.NET Core Identity existing, tidak disentuh |
| V4 Access Control | yes | Authorize class-level + impersonation read-only intercept (`_Layout.cshtml:270` block POST saat impersonate) |
| V5 Input Validation | yes | Server-side V5 di `BypassSave` (`:1616-1626`): CoacheeId/Reason/TargetUnit wajib, Mode whitelist. **Jangan percaya form UI** — backend re-validasi |
| V6 Cryptography | no | Tidak ada |

### Known Threat Patterns for ASP.NET MVC Razor + vanilla-JS
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST mutasi | Tampering | `[ValidateAntiForgeryToken]` + `RequestVerificationToken` header (existing pattern, semua 3 POST endpoint sudah `[ValidateAntiForgeryToken]` :1609/1652/1671) [VERIFIED] |
| XSS via innerHTML render data user (nama worker, reason) | Tampering/Info disclosure | `escHtml()` helper sudah ada (`Override.cshtml:437-444`) — WAJIB pakai untuk SEMUA data dari server saat render innerHTML (nama, reason, track) |
| Double-submit / race konfirmasi | Tampering | Client anti-double-click (D-21 spinner) + backend guard atomik D-12 (unique index, message "klik ganda" :516) — defense berlapis |
| Stale-state confirm (state basi) | Tampering | Backend re-cek (D-11, message :505) — UI surface message + auto-refresh (D-20) |
| Privilege escalation via dropdown coach manipulasi | Elevation | Backend constraint E15 + re-validasi service — UI dropdown tidak bisa bypass server check |

**Key:** Permukaan baru = 1 view + 1 minor select extend. Semua kontrol keamanan (authz, antiforgery, input validation) sudah terpasang di backend 360; Phase 361 hanya WAJIB konsisten pakai `escHtml()` saat render innerHTML.

## Sources

### Primary (HIGH confidence) — semua dari source codebase
- `Controllers/ProtonDataController.cs:1499-1684` — 6 endpoint bypass response shape + `BypassSaveRequest` :80-90
- `Controllers/ProtonDataController.cs:231-243` — Override() GET ViewBag (AllTracks, SectionUnitsJson; NO coach)
- `Views/ProtonData/Override.cshtml` (447 baris) — cascade client, AntiForgery fetch, spinner, escHtml, appUrl/basePath
- `Views/Shared/_Layout.cshtml:54-55,270-302` — appUrl/basePath helper; toast (kondisional impersonation)
- `Views/Admin/ManageAssessment.cshtml:68-96,225-228` — nav-tabs markup + programmatic Tab().show()
- `Models/AssessmentSession.cs:26,38,39` — Score/IsPassed/CompletedAt (D-18 verify)
- `Models/ProtonModels.cs:235-249` — PendingProtonBypass (Reason, TargetCoachId, CreatedAt, LinkedAssessmentSessionId)
- `Services/ProtonBypassService.cs` — pesan error backend verbatim (toast D-20)
- `Services/GradingService.cs:469-552` — RegradeAfterEditAsync re-grade cascade (D-24)
- `Controllers/CoachMappingController.cs:146-149,1334` — pola coach list (GetUsersInRoleAsync)
- `Controllers/AssessmentAdminController.cs:3056,3265` — EditPesertaAnswers → RegradeAfterEditAsync (D-24 UI trigger)
- `HcPortal.Tests/ProtonBypassServiceTests.cs:496` — Revert_PassFail_PendingBalikMenunggu (sudah cover)
- `tests/playwright.config.ts`, `tests/helpers/{auth,accounts,dbSnapshot}.ts`, `tests/e2e/global.setup.ts` — e2e infra
- `.planning/seeds/313-timer-fixtures.sql` — pola seed fixture (WIPE-AND-INSERT + THROW guard)
- `361-CONTEXT.md` (24 keputusan D-01..D-24), `361-UI-SPEC.md` (kontrak visual approved), `REQUIREMENTS.md §PBYP`

### Secondary (MEDIUM confidence)
- MEMORY project_354 (lesson Razor dynamic → Playwright runtime), project_355 (AD=false UAT, legacy spec broken note), project_360 (backend final)

### Tertiary (LOW confidence)
- Tidak ada — fase internal-codebase, semua diverifikasi langsung.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua sudah live di codebase, tidak ada dependency baru
- Architecture: HIGH — pola direplikasi langsung dari Override.cshtml + ManageAssessment.cshtml verified
- Endpoint contracts: HIGH — dibaca langsung dari ProtonDataController.cs:1499-1684
- D-18 column names: HIGH — diverifikasi dari AssessmentSession.cs + ProtonModels.cs
- Coach data source: MEDIUM — pola terbukti (CoachMapping) tapi keputusan A vs B planner's call (A1)
- Pitfalls: HIGH — toast-gap & coach-gap dikonfirmasi via grep/read; lesson 354 dari MEMORY

**Research date:** 2026-06-11
**Valid until:** 2026-07-11 (stable — internal codebase, backend 360 final; re-verify hanya jika Override.cshtml/ProtonDataController disentuh sesi lain)
