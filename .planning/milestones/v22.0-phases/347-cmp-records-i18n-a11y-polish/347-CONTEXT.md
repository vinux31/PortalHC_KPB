# Phase 347: cmp-records-i18n-a11y-polish - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Konsistensi Bahasa Indonesia + aksesibilitas (a11y) + DRY pada 3 view halaman CMP/Records (POL-01..10, 15 finding LOW). **No behavior change, no logic change, no migration.** Sequential setelah Phase 346, final phase CMP/Records trilogy (345→346→347).

Cakupan = teks i18n (badge/header/subtitle/opsi), a11y (aria modal/label `for=`/pagination), DRY (ekstrak `<style>` duplikat → `wwwroot/css/records.css`), mobile grid responsif. Pure polish — JANGAN ubah perilaku/query/authz yang sudah diverifikasi Phase 346.

Files: `Views/CMP/Records.cshtml` + `Views/CMP/RecordsWorkerDetail.cshtml` + `Views/CMP/RecordsTeam.cshtml` (PARTIAL) + `Views/CMP/_RecordsTeamBody.cshtml` + `Views/Shared/_Layout.cshtml` (POL-08 RenderSection) + `wwwroot/css/records.css` (NEW).
</domain>

<decisions>
## Implementation Decisions

### Keputusan terkunci dari spec §347 (user APPROVED 2026-06-04)
Spec `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` §"Phase 347" (baris 135-157) — tabel POL-01..10 dgn file:line + teks exact. JANGAN dilitigasi ulang:
- **POL-01:** Badge `Passed`→`Lulus`, `Failed`→`Tidak Lulus` (HANYA case `IsPassed==true/false`). **null tetap "Menunggu Penilaian"** (Phase 345 — JANGAN ditimpa). JANGAN ubah class `bg-success`/`bg-danger`.
- **POL-02:** Header `Score`→`Nilai`.
- **POL-03:** `Position`→`Jabatan`; `Section`→`@OrgLabels.GetLabel(0)`; header Team `Position`→`Jabatan`.
- **POL-04:** `All Categories/Sub/Types`→`Semua Kategori`/`Semua Sub Kategori`/`Semua Tipe` + label opsi tipe konsisten.
- **POL-05:** Subtitle Inggris RecordsWorkerDetail → Indonesia ("Lihat detail rekam jejak penilaian dan pelatihan anggota tim.").
- **POL-06:** Modal `aria-labelledby`+`role="dialog"`; btn-close `aria-label="Tutup"` (RecordsWorkerDetail modal + **modal baru REC-02 di Records.cshtml** dari Phase 346).
- **POL-07:** Label `for=` association semua filter (3 view) + My Records search visible label.
- **POL-09:** Mobile grid filter responsif `col-12 col-sm-6 col-md-...` (RecordsWorkerDetail).
- **POL-10:** `type="button"` di reset; label tombol konsisten (Lihat/Sertifikat); pagination `aria-current="page"`.

### Keputusan diskusi 2026-06-04 (user: "semua sesuai reko")
- **D-01 (POL-08) Inclusion = `@section Styles` + _Layout RenderSection.** Tambah `@await RenderSectionAsync("Styles", required: false)` di `<head>` `Views/Shared/_Layout.cshtml` (backward-compat: page tanpa section render kosong). Lalu di view FULL-PAGE: `@section Styles { <link rel="stylesheet" href="~/css/records.css" /> }`. **NUANCE KRITIS:** `RecordsTeam.cshtml` = **PARTIAL** (di-render `Html.PartialAsync("RecordsTeam")` dari Records.cshtml L272) → **TAK BISA `@section`**. Maka: (1) `Records.cshtml` (host My Records + Team partial) → `@section Styles` link records.css (cover Team partial sekalian); (2) `RecordsWorkerDetail.cshtml` (full page) → `@section Styles` link records.css; (3) `RecordsTeam.cshtml` (partial) → HANYA hapus `<style>` block (styling di-serve link parent Records page). *(User: reko.)*
- **D-02 (POL-08) Extraction scope = SEMUA selector common.** Ekstrak ke records.css: `.sticky-header` + `@keyframes fadeIn` (di 3 view) + `.stat-card`/`.stat-card::before`/`.stat-icon` + variannya (di Records + RecordsWorkerDetail). **Executor WAJIB baca isi `<style>` block aktual tiap view + ekstrak verbatim** (hindari visual regression — selector/value harus identik). Bila ada style view-specific (tidak shared), boleh tinggal inline atau ikut ke css dengan komentar. *(User: reko.)*
- **D-03 Verifikasi = build+grep + Playwright spot-check ringan.** `dotnet build` 0 error + grep konfirmasi teks/aria + Playwright spot-check 1-2 surface (Records + RecordsWorkerDetail) cek **no visual regression** (stat-card/sticky-header/fadeIn tetap render via records.css; badge "Lulus"/"Tidak Lulus"/"Menunggu Penilaian" benar). Risk LOW; regresi utama dari POL-08 CSS extraction. **Tidak perlu xUnit baru** (no logic change). SEED bila perlu sesi pending utk verify POL-01 null-case (reuse `tests/sql/cmp346-seed.sql`). *(User: reko.)*
- **D-04 i18n terms = default spec verbatim.** Lulus/Tidak Lulus · Nilai · Jabatan · Semua Kategori/Semua Sub Kategori/Semua Tipe · subtitle ID · btn-close "Tutup". Tidak ada override. *(User: reko.)*

### Claude's Discretion (planner refine)
- Plan split (mungkin 1 plan i18n + 1 plan a11y + 1 plan POL-08 DRS, atau 1 plan per-view). Planner putuskan; POL-08 (touch _Layout shared + new css) layak diisolasi utk review.
- Apakah POL-08 _Layout RenderSection layak (vs inline `<link>` per-view) bila planner temukan _Layout edit berisiko — fallback inline link di top view (D-01 reko tetap @section).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements & Roadmap
- `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` — **spec utama** §"Phase 347" (baris 135-157): tabel POL-01..10 file:line + teks exact + §"Coverage Phase 347" (15 finding LOW mapping).
- `.planning/REQUIREMENTS.md` — POL-01..10 (baris 55-64) + depends note (53).
- `.planning/ROADMAP.md` — §Phase 347 (baris 774-788): 5 SC, files affected.

### Dependency (sequential predecessor)
- `.planning/phases/346-cmp-records-detail-search-logic/346-CONTEXT.md` + `346-VERIFICATION.md` — 346 SHIPPED (REC-01..09, 9/9 browser-verified). 347 sentuh baris berdekatan di Records/RecordsWorkerDetail/RecordsTeam — JANGAN regресi.
- `.planning/phases/345-assessment-pending-grade-display-fix/345-CONTEXT.md` — POL-01 koordinasi: null badge = "Menunggu Penilaian" amber (`bg-warning text-dark`), JANGAN ditimpa.

### Kode wajib (verified 2026-06-04 pasca-346)
- `Views/_ViewImports.cshtml:6` — `@inject HcPortal.Services.IOrgLabelService OrgLabels` GLOBAL (Phase 343) → `@OrgLabels.GetLabel(0)` works semua view (POL-03).
- `Views/Shared/_Layout.cshtml:266` — `@await RenderSectionAsync("Scripts", required: false)` ADA; **`RenderSectionAsync("Styles")` TAK ADA** → POL-08 D-01 tambah di `<head>` (sekitar L43 dekat `<link ~/css/site.css>`).
- Badge POL-01: `Records.cshtml:185/187` + `RecordsWorkerDetail.cshtml:228/230` (`>Passed<`/`>Failed<`). Records.cshtml juga punya 3-way switch L183-192 (true/false/null) — ubah HANYA true/false text.
- `<style>` block: `Records.cshtml` (.stat-card/.stat-icon/.sticky-header/@keyframes fadeIn), `RecordsWorkerDetail.cshtml` (sama), `RecordsTeam.cshtml` (.sticky-header/@keyframes fadeIn subset).
- `RecordsTeam.cshtml` = PARTIAL (`Records.cshtml:272` PartialAsync) — no @section.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `@OrgLabels.GetLabel(N)` global-injected (Phase 343) — RecordsTeam sudah pakai (L20/153). POL-03 tinggal apply ke RecordsWorkerDetail Section + header.
- Phase 345 amber badge pattern (`bg-warning text-dark` + "Menunggu Penilaian") — POL-01 preserve null case verbatim.
- records.css NEW — single source utk .sticky-header/.stat-card/.stat-icon/@keyframes fadeIn (hilangkan 3× duplikasi).

### Established Patterns
- `_Layout` pakai `RenderSectionAsync("Scripts", required:false)` → tambah pola sama utk "Styles" (POL-08 D-01).
- `<link rel="stylesheet" href="~/css/site.css">` (_Layout L43) — pola link app CSS; records.css ikut pola `~/css/`.

### Integration Points
- POL-06 aria: modal `#trainingDetailModal` ADA di 2 tempat — Records.cshtml (baru Phase 346 REC-02) + RecordsWorkerDetail.cshtml. Keduanya butuh aria-labelledby+role=dialog+btn-close aria-label.
- POL-08 touch `_Layout.cshtml` (shared) — isolasi + review; required:false = zero-risk utk page lain.
</code_context>

<specifics>
## Specific Ideas

- POL-01 null-case: pastikan switch 3-way (Phase 345) tidak rusak — null→"Menunggu Penilaian" amber HARUS tetap setelah ubah true/false text. Verify di Playwright spot-check (reuse cmp346-seed.sql utk sesi pending).
- POL-08 visual regression risk: ekstrak `<style>` HARUS verbatim (selector + property + value identik). Executor baca isi block aktual, jangan asumsi. Setelah ekstrak, hapus `<style>` inline + link records.css; render hasil harus identik (stat-card hover, sticky-header, fadeIn animation).
- POL-04 RecordsTeam sudah pakai `@OrgLabels.GetLabel` utk Bagian/Unit options — POL-04 "Semua ..." untuk Category/SubCategory/Type/Tahun yang masih Inggris.
- Dev creds UAT: admin+coach pwd `123456`, DB `HcPortalDB_Dev`. App run `ASPNETCORE_ENVIRONMENT=Development` (CATATAN: tanpa env ini connection string default ke placeholder Production — lihat 346-06 env note).
</specifics>

<deferred>
## Deferred Ideas

- **MAM/MAP** (ManageAssessment + Monitoring) — Phase 348/349. **MAP-20 (Tab3 History pending badge) SUDAH COVERED Phase 346** (T-346-UAT-01 fix `_HistoryTab.cshtml` badge "Menunggu Penilaian") → saat plan 349, DROP MAP-20 / tandai covered.
- Behavior/logic/authz changes — out of scope 347 (pure polish; semua sudah diverifikasi Phase 346).
- xUnit baru — tidak perlu (no logic change).

### Reviewed Todos (not folded)
Tidak ada — `todo match-phase 347` = 0 match.
</deferred>

---

*Phase: 347-cmp-records-i18n-a11y-polish*
*Context gathered: 2026-06-04*
