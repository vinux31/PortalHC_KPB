# Phase 384: Monitoring Essay Grading UI Refactor (Fase 2) - Research

**Researched:** 2026-06-15
**Domain:** ASP.NET Core MVC + Razor UI refactor (in-repo reuse), backend unchanged
**Confidence:** HIGH (semua klaim diverifikasi langsung dari kode in-repo session ini)

> Catatan provenance: Phase ini adalah refactor UI murni di codebase yang sudah dikenal. Hampir semua temuan = `[VERIFIED: codebase]` (dibaca langsung dari file). Tidak ada library/versi baru untuk diverifikasi via npm registry — Bootstrap 5 + Bootstrap Icons sudah app-wide. Tidak ada lookup dokumentasi eksternal yang diperlukan; semua pola sudah ada di repo.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 Scope ganti:** Ganti HANYA section essay `AssessmentMonitoringDetail.cshtml:381-481`. Tabel sesi utama (atas, `~:218-378`) + sisanya TIDAK disentuh. Pertahankan guard `essayGradingMap != null && essayGradingMap.Any()` (`:385`) — section disembunyikan bila tak ada worker beressay.
- **D-02 Isi tabel:** HANYA worker dengan `HasManualGrading == true`. Worker murni MC TIDAK muncul. Kolom: Worker (FullName) + NIP, jumlah essay belum dinilai (`EssayPendingCount`), badge status 3-state, tombol "Tinjau Essay" (kanan).
- **D-03 Urutan:** Urut by NIP / nama (alfabet) — `.OrderBy(s => s.UserNIP)` — BUKAN pending-first.
- **D-04 Status badge 3-state:** 🟡 `"{N} belum dinilai"` (`EssayPendingCount > 0`, `bg-warning text-dark`) · 🔵 `"Siap difinalisasi"` (`EssayPendingCount == 0` && belum finalized, `bg-info`) · 🟢 `"Selesai"` (finalized = `Status == Completed && !IsNullOrEmpty(NomorSertifikat)`, `bg-success`). REUSE gate Phase 310 D-02 — JANGAN ubah kriteria.
- **D-05 Empty state:** Tak ada worker beressay → section essay disembunyikan total (guard existing dipertahankan). TIDAK ada copy empty-state.
- **D-06 Tombol "Tinjau Essay":** Tiap baris worker → navigasi GET ke page penilaian essay per-worker terpisah (GET action baru). Route shape = Claude's Discretion.
- **D-07 Layout page per-worker:** REUSE kartu essay existing (markup `:407-446`). Tambah header identitas worker (nama + NIP) + tombol "Kembali ke Monitoring".
- **D-08 Simpan Skor = AJAX in-place:** REUSE handler JS existing `btn-save-essay-score` + `btn-finalize-grading` (AJAX, tak reload). Handler saat ini inline `<script>` di `AssessmentMonitoringDetail.cshtml` → planner putuskan extract vs duplikat (Discretion).
- **D-09 Return flow:** Setelah "Selesaikan Penilaian" → TETAP di page per-worker, update in-place ke state "Selesai" (badge hijau, input/tombol read-only). TIDAK auto-redirect. User klik "Kembali" manual.
- **D-10 Finalized = READ-ONLY (BUKAN re-grade):** Session finalized (gate D-04 🟢) → page per-worker mode read-only: skor tampil, input `disabled`, tombol Simpan/Selesaikan disabled/hidden. Backend TAK diubah. Tombol "Tinjau Essay" tetap MUNCUL setelah finalized (lihat hasil read-only).
- **D-11 Playwright e2e (UIG-04):** Razor dynamic → Playwright runtime WAJIB (pelajaran Phase 354). Flow: tabel worker-list render → klik "Tinjau Essay" → navigasi → beri skor (Simpan Skor AJAX) + Selesaikan Penilaian round-trip → state "Selesai" in-place. Local `dotnet run` → `http://localhost:5277`. Login admin lokal: `Authentication__UseActiveDirectory=false`.

### Claude's Discretion

- **Route/URL shape GET action baru** — mis. `EssayGrading?sessionId={id}` atau `/Admin/AssessmentEssayGrading/{id}`. Pilih yang konsisten dgn konvensi controller existing.
- **Perlu ViewModel worker-list baru atau tidak** — `MonitoringSessionViewModel` (`:48-68`) SUDAH punya semua field. Kemungkinan cukup `Model.Sessions.Where(s => s.HasManualGrading)`. Konfirmasi saat planning.
- **Cara page per-worker memuat `List<EssayGradingItemViewModel>` untuk 1 session** — clone logic map-builder (`AssessmentAdminController.cs:~3413-3448`) untuk single session.
- **Authorization page baru** — samakan dengan `AssessmentMonitoringDetail`/`SubmitEssayScore` (Admin/HC). Verifikasi attribute.
- **Extract inline JS handler ke shared script/partial vs duplikat** (lihat D-08).

### Deferred Ideas (OUT OF SCOPE)

- **Alur grading berantai** (auto-buka worker pending berikutnya setelah selesai 1) — DITOLAK fase ini. Future enhancement.
- **Re-grade setelah finalized** (edit skor sesi Completed) — DITOLAK (read-only). Butuh perubahan backend → di luar scope "backend unchanged".
- One-time cleanup data test lokal pasca Phase 367 — di luar scope, tidak di-fold.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| UIG-01 | Halaman Monitoring menampilkan tabel list worker (Worker/NIP, jml essay belum dinilai, status) menggantikan blok essay inline `:381-481` | `MonitoringSessionViewModel` (`:48-68`) sudah punya `UserFullName`/`UserNIP`/`HasManualGrading`/`EssayPendingCount`/`Status`/`NomorSertifikat` — TIDAK perlu ViewModel baru. Pola tabel `<table table-hover align-middle>` + `<thead table-light>` (`:218-231`). Badge 3-state derivasi inline di view. |
| UIG-02 | Tiap baris worker punya tombol "Tinjau Essay" → membuka page essay per-worker terpisah | GET action baru di `AssessmentAdminController` + view baru. Tombol = `<a href="@Url.Action(...)">` navigasi GET (BUKAN AJAX). Route shape default `EssayGrading?sessionId={id}` (Discretion). |
| UIG-03 | Page per-worker reuse `SubmitEssayScore` + `FinalizeEssayGrading` (backend TIDAK diubah) + `EssayGradingItemViewModel`; tombol "Selesaikan Penilaian" di page ini | Endpoint kontrak terverifikasi (lihat §Interaction Contract). Clone `EssayGradingMap` builder (`:3413-3448`) untuk single session. Markup `finalizeSection_{sessionId}` (`:448-476`) di-clone. |
| UIG-04 | Playwright e2e: list render → "Tinjau Essay" navigasi → beri skor + Selesaikan round-trip sukses | Harness `tests/` ada (`playwright.config.ts` baseURL `localhost:5277`, `fullyParallel:false`). Pola seed DB-snapshot di `assessment-pending-grade.spec.ts`. Selector essay sudah dipakai di `assessment.spec.ts` FLOW 9. Detail di §Validation Architecture. |
</phase_requirements>

## Summary

Phase 384 adalah **refactor UI murni** (structural reuse) di codebase yang sudah matang. Tidak ada library/komponen/migration baru. Inti kerja: (1) ganti blok essay inline panjang di `AssessmentMonitoringDetail.cshtml:381-481` dengan tabel ringkas worker-beressay, dan (2) buat **1 GET action baru + 1 view baru** untuk page penilaian essay per-worker yang me-REUSE markup kartu essay existing, endpoint POST existing (`SubmitEssayScore`/`FinalizeEssayGrading`, tak diubah), dan `EssayGradingItemViewModel`.

Semua bahan sudah ada di repo: `MonitoringSessionViewModel` punya seluruh field untuk tabel (jadi **ViewModel worker-list baru TIDAK diperlukan**); `EssayGradingMap` builder (`AssessmentAdminController.cs:3413-3448`) tinggal di-clone untuk single session; markup kartu essay (`:407-446`) + section finalize (`:448-476`) tinggal di-clone; handler AJAX (`:1472-1558`) tinggal di-reuse (helper `appUrl` sudah global di `_Layout.cshtml:55`). Authz target = `[Authorize(Roles = "Admin, HC")]` (verbatim dari `SubmitEssayScore`/`FinalizeEssayGrading`).

Risiko utama bukan teknis melainkan **fidelity**: selector JS (`.essay-grading-card`, `.essay-score-input`, `#badge_{sid}_{qid}`, `#finalizeSection_{sid}`) HARUS dipertahankan byte-for-byte di markup clone agar handler tetap match, dan perubahan D-09 (finalize → in-place "Selesai", BUKAN `location.reload()`) harus berlaku HANYA di page baru tanpa merusak handler monitoring existing. IDOR adalah satu-satunya threat baru (GET action expose grading per `sessionId`) — mitigasi = reuse authz role-gate yang sama (Admin/HC), tanpa DB write baru.

**Primary recommendation:** GET action baru `EssayGrading(int sessionId, string title, string category, DateTime scheduleDate, string? assessmentType)` dengan `[Authorize(Roles = "Admin, HC")]`, load single session + clone map-builder, pass ViewModel pembungkus (single session + `List<EssayGradingItemViewModel>` + flag finalized + 4 param navigasi "Kembali"). REUSE markup via di-clone, EXTRACT handler AJAX ke `wwwroot/js/essay-grading.js` (param-kan behavior finalize on-success), reuse di kedua page.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Tabel worker-list (render) | Frontend Server (Razor SSR) | — | Data sudah ada di `Model.Sessions`; render server-side, tak perlu API call client |
| Navigasi "Tinjau Essay" | Browser (anchor GET) | Frontend Server (routing) | Navigasi halaman penuh (`<a href>`), bukan AJAX (D-06) |
| Load essay items page per-worker | API/Backend (EF query) | Frontend Server (Razor render) | Clone `EssayGradingMap` builder — query DB single session, render kartu SSR |
| Simpan Skor (per-soal) | API/Backend (`SubmitEssayScore` POST) | Browser (AJAX fetch + DOM update in-place) | Backend unchanged; UI update in-place via handler |
| Selesaikan Penilaian | API/Backend (`FinalizeEssayGrading` POST) | Browser (AJAX + D-09 in-place state) | Backend unchanged; D-09 ubah on-success behavior di client SAJA |
| Authz / akses kontrol | API/Backend (`[Authorize(Roles)]`) | — | Role-gate Admin/HC di action GET baru + endpoint POST existing |
| Read-only finalized (D-10) | Frontend Server (Razor conditional) | Browser (disabled inputs) | Render disabled berdasarkan flag finalized dari server — hindari panggil backend di sesi Completed |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC + Razor | (existing .NET app) | Controller + `.cshtml` view | Sudah seluruh app `[VERIFIED: codebase]` |
| Bootstrap 5 | (vendored/CDN app-wide) | `card`/`badge`/`table table-hover align-middle`/`btn`/`form-control`/`breadcrumb` | Sudah dipakai di view ini, tak ada tambahan `[VERIFIED: AssessmentMonitoringDetail.cshtml]` |
| Bootstrap Icons | (app-wide) | `<i class="bi bi-*">` (`bi-pencil-square`, `bi-arrow-left`, `bi-check-circle`, `bi-person-badge`) | Sudah dipakai `[VERIFIED: codebase]` |
| EF Core | (existing) | Query single session essay items | Reuse pola builder existing `[VERIFIED: AssessmentAdminController.cs:3413-3448]` |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit (`HcPortal.Tests/`) | (existing) | Unit/regression test backend | Backend tak diubah — minimal test baru; verifikasi build hijau |
| Playwright (`tests/e2e/`, TypeScript) | (existing) | e2e UIG-04 | Razor dynamic → runtime wajib (D-11) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ViewModel worker-list baru | `Model.Sessions.Where(s => s.HasManualGrading)` (reuse `MonitoringSessionViewModel`) | **RECOMMENDED: reuse.** Semua field sudah ada (`UserFullName`/`UserNIP`/`HasManualGrading`/`EssayPendingCount`/`Status`/`NomorSertifikat`). ViewModel baru = boilerplate tanpa nilai tambah. `[VERIFIED: Models/AssessmentMonitoringViewModel.cs:48-68]` |
| Extract handler ke `.js` shared | Duplikat inline `<script>` di page baru | **RECOMMENDED: extract** (lihat §Don't Hand-Roll). Repo punya 8 file `wwwroot/js/*.js` extracted (precedent kuat). Duplikat = drift risk D-09 (2 handler beda behavior). |
| Pass ViewModel pembungkus baru ke view per-worker | ViewBag untuk essay items | Pembungkus ViewModel lebih type-safe & jelas; ViewBag = pola map existing tapi kurang eksplisit untuk single-session page. Planner pilih; pembungkus direkomendasikan. |

**Installation:** Tidak ada. Semua dependency sudah terpasang app-wide. `[VERIFIED: codebase]`

**Version verification:** N/A — tidak ada package npm/NuGet baru. Bootstrap 5 + Bootstrap Icons + .NET MVC stack sudah ada di proyek (tak ada perubahan versi).

## Architecture Patterns

### System Architecture Diagram

```
  HC/Admin (browser)
        │
        │ 1. GET /Admin/AssessmentMonitoringDetail?title&category&scheduleDate&assessmentType
        ▼
  ┌─────────────────────────────────────────────┐
  │ AssessmentMonitoringDetail (existing action) │  [Authorize] role Admin/HC (class+base)
  │  builds Model.Sessions + ViewBag.EssayGrading│
  │  Map (UNCHANGED)                             │
  └─────────────────────────────────────────────┘
        │ renders
        ▼
  AssessmentMonitoringDetail.cshtml
   ├── tabel sesi utama (UNCHANGED :218-378)
   └── [REFACTOR :381-481] → TABEL WORKER-LIST
         rows = Model.Sessions.Where(HasManualGrading).OrderBy(UserNIP)
         cols: Worker+NIP │ EssayPendingCount │ badge 3-state │ [Tinjau Essay]
                                                                    │
        ┌───────────────────────────────────────────────────────────┘
        │ 2. GET navigasi (anchor) — carry sessionId + title/category/scheduleDate/type
        ▼
  ┌─────────────────────────────────────────────┐
  │ EssayGrading (NEW action)                    │  [Authorize(Roles="Admin, HC")]
  │  load 1 AssessmentSession by sessionId       │
  │  clone EssayGradingMap builder → single sess │
  │  compute isFinalized (D-10)                  │
  └─────────────────────────────────────────────┘
        │ renders
        ▼
  EssayGrading.cshtml (NEW view)
   ├── header: <h2>UserFullName</h2> + NIP + [Kembali ke Monitoring →parent params]
   ├── CLONE kartu essay (:407-446) per EssayGradingItemViewModel
   │      keep classes: .essay-grading-card .essay-score-input #badge_{sid}_{qid}
   ├── CLONE finalizeSection_{sid} (:448-476) — gate D-10 read-only
   └── #antiforgeryForm (:484) + script extract essay-grading.js
        │
        │ 3a. AJAX POST /Admin/SubmitEssayScore (sessionId,questionId,score,token)
        │        → {success, pendingCount, allGraded}  → badge bg-success, show finalize
        │ 3b. AJAX POST /Admin/FinalizeEssayGrading (sessionId,token)
        │        → {success, alreadyFinalized?, nomorSertifikat?, message}
        │        → D-09: update IN-PLACE ke "Selesai" (NOT location.reload())
        ▼
  ┌─────────────────────────────────────────────┐
  │ SubmitEssayScore / FinalizeEssayGrading      │  UNCHANGED — [Authorize(Roles="Admin, HC")]
  │ (existing POST, antiforgery, JSON)           │  + [ValidateAntiForgeryToken]
  └─────────────────────────────────────────────┘
```

### Recommended Project Structure

```
Controllers/
  AssessmentAdminController.cs   # + GET EssayGrading(...) sebelah AssessmentMonitoringDetail (~:3273)
Views/Admin/
  AssessmentMonitoringDetail.cshtml   # REFACTOR :381-481 (tabel worker-list)
  EssayGrading.cshtml                 # NEW view (clone kartu essay + header worker + back)
Models/
  AssessmentMonitoringViewModel.cs    # reuse; OPSIONAL: + EssayGradingPageViewModel pembungkus
wwwroot/js/
  essay-grading.js                    # NEW (extract handler :1472-1558, param-kan finalize-on-success)
```

### Pattern 1: GET action baru — clone single-session essay loader

**What:** GET action yang load 1 session + essay items-nya (clone `EssayGradingMap` builder untuk satu `sessionId`).
**When to use:** Page per-worker (UIG-02/03).
**Signature direkomendasikan (verifikasi planner):**
```csharp
// Source: pola AssessmentAdminController.cs:3273 (signature) + :3413-3448 (builder)
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> EssayGrading(
    int sessionId, string title, string category, DateTime scheduleDate, string? assessmentType = null)
{
    var session = await _context.AssessmentSessions
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == sessionId);
    if (session == null || !session.HasManualGrading)
    {
        TempData["Error"] = "Sesi penilaian essay tidak ditemukan.";
        return RedirectToAction("AssessmentMonitoringDetail",
            new { title, category, scheduleDate, assessmentType });
    }
    // CLONE :3419-3446 untuk single session: UserPackageAssignments.FirstOrDefault →
    // GetShuffledQuestionIds() → PackageQuestions QuestionType=="Essay" → PackageUserResponses map →
    // List<EssayGradingItemViewModel> (QuestionId/DisplayNumber/QuestionText/Rubrik/TextAnswer/
    // EssayScore/ScoreValue/ImagePath/ImageAlt)
    // compute isFinalized = session.Status == AssessmentConstants.AssessmentStatus.Completed
    //                       && !string.IsNullOrEmpty(session.NomorSertifikat)
    // pass pembungkus VM (single session identity + items + isFinalized + 4 nav params)
}
```
**Catatan routing:** Controller `[Route("Admin/[action]")]` (`AssessmentAdminController.cs:19` + `AdminBaseController.cs:13`) → action `EssayGrading` otomatis ter-route ke `/Admin/EssayGrading`. Query string `?sessionId=...&title=...` cocok dgn convention. Default route Program.cs (`:214-216`) = `{controller=Account}/{action=Login}` (fallback, tak relevan; attribute route menang). `[VERIFIED: codebase]`

### Pattern 2: Tabel worker-list (mirror tabel sesi existing)

**What:** Tabel ringkas worker-beressay menggantikan `:381-481`.
**Example struktur:**
```cshtml
@* Source: mirror AssessmentMonitoringDetail.cshtml:218-231 (tabel sesi) *@
@if (essayGradingMap != null && essayGradingMap.Any())   @* guard D-01/D-05 dipertahankan *@
{
    <div class="mt-4">
        <h5 class="fw-semibold mb-3"><i class="bi bi-pencil-square me-2 text-warning"></i>Penilaian Essay</h5>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light"><tr>
                    <th>Worker</th><th class="text-center">Essay Belum Dinilai</th>
                    <th class="text-center">Status</th><th>Aksi</th>
                </tr></thead>
                <tbody>
                @foreach (var s in Model.Sessions.Where(x => x.HasManualGrading).OrderBy(x => x.UserNIP))
                {
                    var isFinalized = s.Status == AssessmentConstants.AssessmentStatus.Completed
                                      && !string.IsNullOrEmpty(s.NomorSertifikat);
                    @* badge 3-state D-04: pending(bg-warning text-dark) / siap(bg-info) / selesai(bg-success) *@
                    <tr>
                        <td class="fw-semibold">@s.UserFullName<br><small class="text-muted">@s.UserNIP</small></td>
                        <td class="text-center">@(s.EssayPendingCount > 0 ? s.EssayPendingCount.ToString() : "—")</td>
                        <td class="text-center">@* badge sesuai 3-state *@</td>
                        <td><a class="btn btn-primary btn-sm" href="@Url.Action("EssayGrading", new { sessionId = s.Id, title = Model.Title, category = Model.Category, scheduleDate = Model.Schedule.ToString("yyyy-MM-dd"), assessmentType = ViewBag.AssessmentType })"><i class="bi bi-pencil-square me-1"></i>Tinjau Essay</a></td>
                    </tr>
                }
                </tbody>
            </table>
        </div>
    </div>
}
```
**Catatan:** `ViewBag.AssessmentType` di-set di action (`:3285`) — tersedia untuk carry param.

### Anti-Patterns to Avoid

- **Mengubah class selector saat clone kartu essay:** Handler JS mencari `.essay-grading-card` (`:1476`), `.essay-score-input` (`:1477`), `#badge_{sid}_{qid}` (`:1499`), `#finalizeSection_{sid}` (`:1505`). Jika class/id berubah → handler diam-diam gagal. **CLONE byte-for-byte struktur class/id.** `[VERIFIED: :1472-1558]`
- **Mengubah handler `location.reload()` global:** D-09 hanya berlaku page baru. Jika handler di-extract dan behavior on-success diubah global, page monitoring existing kehilangan reload (tapi karena tabel monitoring TAK lagi punya tombol finalize setelah refactor, ini perlu dicek). Lihat §Pitfall 2.
- **Memanggil backend di sesi finalized:** D-10 read-only menghindari `SubmitEssayScore`/`FinalizeEssayGrading` di sesi Completed. JANGAN render input enabled saat `isFinalized`.
- **Menambah ViewModel worker-list baru tanpa kebutuhan:** `MonitoringSessionViewModel` cukup. Hindari boilerplate.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| AJAX save/finalize handler | Tulis handler baru di page per-worker | EXTRACT `:1472-1558` ke `wwwroot/js/essay-grading.js`, param-kan finalize-on-success | Kontrak request/response sudah teruji (Phase 298/310); tulis ulang = drift bug |
| URL prefix (basePath) | Hardcode `/Admin/...` | `appUrl('/Admin/SubmitEssayScore')` (global `_Layout.cshtml:55`) | App di-deploy under sub-path `/KPB-PortalHC` — `appUrl` handle basePath `[VERIFIED: _Layout.cshtml:55]` |
| Antiforgery token plumbing | Tulis ulang form token | REUSE `<form id="antiforgeryForm">@Html.AntiForgeryToken()</form>` (`:484`) + `querySelector('input[name="__RequestVerificationToken"]')` | Pola identik dipakai 4× di view existing `[VERIFIED: :847,:1110,:1479,:1524]` |
| Gambar soal render | Tulis `<img>` markup | `@await Html.PartialAsync("_QuestionImage", new { ImagePath, ImageAlt, Cap = 240 })` | Single-source anti-drift, handle lightbox + lazy + a11y + null-skip `[VERIFIED: _QuestionImage.cshtml]` |
| Load essay items single session | Query baru ad-hoc | CLONE `EssayGradingMap` builder `:3419-3446` (assignment → GetShuffledQuestionIds → Essay questions → response map) | Logic sudah benar (shuffle-aware, response join); ad-hoc berisiko miss shuffle/response mapping |
| Worker-list ViewModel | Class baru | `Model.Sessions.Where(s => s.HasManualGrading)` (reuse `MonitoringSessionViewModel`) | Semua field sudah ada `[VERIFIED: :48-68]` |
| Disabled-button tooltip | CSS custom | WRAP `<span data-bs-toggle="tooltip" style="pointer-events:none">` (pola `:460-468`) | Bootstrap tooltip tak fire di `disabled` button; wrapper span solusi standar in-repo `[VERIFIED: :460-468]` |

**Key insight:** Phase ini ~80% adalah memindahkan/meng-clone markup & logic yang sudah ada dan terbukti benar. Setiap "membuat baru" yang punya padanan in-repo = sumber bug + inkonsistensi. Reuse maksimal; satu-satunya logic *baru* yang nyata = (a) derivasi badge 3-state D-04 dan (b) param-isasi finalize-on-success D-09.

## Common Pitfalls

### Pitfall 1: Selector mismatch saat clone kartu essay
**What goes wrong:** Handler AJAX gagal diam-diam (skor tak tersimpan / badge tak update) padahal POST sukses.
**Why it happens:** Handler hard-bind ke class/id: `.closest('.essay-grading-card')` → `.querySelector('.essay-score-input')` → `getElementById('badge_'+sid+'_'+qid)` → `getElementById('finalizeSection_'+sid)`.
**How to avoid:** Clone markup `:407-446` + `:448-476` mempertahankan PERSIS: `class="...essay-grading-card"`, `class="...essay-score-input"` + `data-question-id`/`data-session-id`, `id="badge_@(session.Id)_@essayItem.QuestionId"`, `id="finalizeSection_@session.Id"`, `class="btn-save-essay-score"`, `class="btn-finalize-grading"`.
**Warning signs:** e2e: klik Simpan Skor, badge tetap "Belum Dinilai" walau response 200.

### Pitfall 2: D-09 finalize behavior bocor ke handler monitoring
**What goes wrong:** Mengubah `location.reload()` (`:1547`) jadi in-place update secara global, atau sebaliknya page baru masih reload.
**Why it happens:** Handler di-extract jadi shared, tapi on-success behavior berbeda per-page (monitoring existing: reload; page baru: in-place "Selesai").
**How to avoid:** Param-kan handler: `initEssayGrading({ onFinalizeSuccess: 'reload' | 'inplace' })`, atau dispatch berdasar flag `data-finalize-mode` di tombol/page. **Setelah refactor D-01, tabel monitoring TAK lagi merender tombol finalize** (blok `:381-481` diganti tabel tanpa tombol Selesaikan) — jadi behavior reload hanya relevan jika handler masih ter-attach di monitoring untuk surface lain. Verifikasi: setelah refactor, apakah `.btn-finalize-grading` masih ada di `AssessmentMonitoringDetail.cshtml`? Jika TIDAK, handler finalize cukup hidup di page baru saja (in-place). Planner konfirmasi via grep pasca-refactor.
**Warning signs:** Page baru reload setelah Selesaikan (melanggar D-09), atau double-handler attach.

### Pitfall 3: Param navigasi "Kembali" hilang/salah
**What goes wrong:** Tombol "Kembali ke Monitoring" balik ke grup yang salah atau 404 (TempData["Error"] "Assessment group not found").
**Why it happens:** `AssessmentMonitoringDetail` butuh `(title, category, scheduleDate, assessmentType)` (`:3273`), bukan sessionId. Referer rapuh (hilang saat refresh/bookmark).
**How to avoid:** **Carry 4 param via query string** pada GET action `EssayGrading` + render back-link `@Url.Action("AssessmentMonitoringDetail", new { title, category, scheduleDate, assessmentType })`. Robust (survive refresh/bookmark). Referer = fallback opsional saja.
**Warning signs:** Klik Kembali → redirect ke ManageAssessment dgn toast "Assessment group not found".

### Pitfall 4: `scheduleDate` round-trip timezone drift
**What goes wrong:** `scheduleDate` di-pass sebagai string lalu parse jadi `DateTime` dengan jam/tz berbeda → `a.Schedule.Date == scheduleDate.Date` (`:3279`) miss.
**Why it happens:** Format date string inkonsisten antar surface.
**How to avoid:** Pass `Model.Schedule.ToString("yyyy-MM-dd")` (pola hidden field existing `:489 hScheduleDate`). Action bind `DateTime scheduleDate` → `.Date` comparison aman.
**Warning signs:** Kembali ke monitoring kosong padahal data ada.

### Pitfall 5: Login lokal gagal (AD) saat e2e/dotnet run
**What goes wrong:** `dotnet run` default AD-mode → login admin lokal gagal → e2e 500/redirect loop.
**Why it happens:** `Authentication:UseActiveDirectory` default `false` di config (`Program.cs:80`) TAPI override env bisa aktif; pelajaran Phase 355.
**How to avoid:** Jalankan `dotnet run` dengan `Authentication__UseActiveDirectory=false` (env var override). `[VERIFIED: Program.cs:79-81]` + memory Phase 355.
**Warning signs:** e2e gagal di step login; redirect `/Account/Login`.

### Pitfall 6: Tooltip tak muncul di tombol disabled (read-only D-10)
**What goes wrong:** Tombol "Selesaikan Penilaian" disabled tapi tooltip "Sudah selesai..." tak muncul saat hover.
**Why it happens:** Bootstrap tooltip tak fire `mouseenter` di elemen `disabled`.
**How to avoid:** WRAP `<span data-bs-toggle="tooltip" title="..." style="display:inline-block;">` + button `disabled style="pointer-events:none;"` (pola existing `:460-468`). Inisialisasi tooltip Bootstrap di page baru.
**Warning signs:** Hover tombol disabled = no tooltip.

## Code Examples

Verified patterns from in-repo sources:

### Handler AJAX Simpan Skor (sumber yang di-extract)
```javascript
// Source: AssessmentMonitoringDetail.cshtml:1472-1517 (REUSE — extract ke essay-grading.js)
document.querySelectorAll('.btn-save-essay-score').forEach(function(btn) {
  btn.addEventListener('click', async function() {
    const sessionId = this.dataset.sessionId, questionId = this.dataset.questionId;
    const card = this.closest('.essay-grading-card');
    const score = parseInt(card.querySelector('.essay-score-input').value);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    if (isNaN(score)) { alert('Masukkan nilai skor yang valid.'); return; }
    const res = await fetch(appUrl('/Admin/SubmitEssayScore'), {
      method: 'POST',
      headers: {'Content-Type':'application/x-www-form-urlencoded','X-Requested-With':'XMLHttpRequest'},
      body: 'sessionId='+sessionId+'&questionId='+questionId+'&score='+score
            +'&__RequestVerificationToken='+encodeURIComponent(token)
    });
    const data = await res.json();
    if (data.success) { /* badge → bg-success "Sudah Dinilai"; if allGraded → show finalizeSection */ }
  });
});
```

### Finalize handler — titik perubahan D-09
```javascript
// Source: AssessmentMonitoringDetail.cshtml:1536-1547
// EXISTING (monitoring): on success pertama → location.reload()  (:1547)
// PAGE BARU (D-09): GANTI location.reload() jadi update in-place:
//   - badge soal → bg-success "Sudah Dinilai" (sudah dari Simpan Skor)
//   - tombol finalize → disabled + label "Selesai" / state hijau
//   - inputs skor → disabled (read-only)
//   - TIDAK redirect; cabang alreadyFinalized (:1537-1544) DIPERTAHANKAN
```

### Endpoint contract (UNCHANGED — reuse apa adanya)
```csharp
// SubmitEssayScore — Source: AssessmentAdminController.cs:3455-3487
[HttpPost][Authorize(Roles = "Admin, HC")][ValidateAntiForgeryToken]
// params: int sessionId, int questionId, int score
// returns Json: { success:bool, pendingCount:int, allGraded:bool }  (or {success:false, message})

// FinalizeEssayGrading — Source: AssessmentAdminController.cs:3496-3534+
[HttpPost][Authorize(Roles = "Admin, HC")][ValidateAntiForgeryToken]
// param: int sessionId
// returns Json: success path → {success:true} (first finalize) ;
//   already-Completed → {success:true, alreadyFinalized:true, message, score, isPassed, nomorSertifikat} ;
//   non-PendingGrading → {success:false, message (BI literal per status)}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Essay grading inline stacked cards per-worker (`:381-481`) | Tabel worker-list + page per-worker terpisah | Phase 384 (ini) | UX: 1 worker = 1 page, no scroll numpuk |
| Finalize → `location.reload()` | Page baru: in-place "Selesai" (D-09) | Phase 384 | Tetap di page, no flash reload |

**Deprecated/outdated:** Tidak ada. Stack stabil. Markup essay-grading inline (`:381-481`) di-pindah ke page baru, bukan dibuang.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Setelah refactor D-01, `.btn-finalize-grading` TIDAK lagi ada di `AssessmentMonitoringDetail.cshtml` (hanya di page baru) | Pitfall 2 | Jika masih ada (surface lain pakai handler), extract harus dukung dua mode finalize-on-success. **Mitigasi: planner grep pasca-refactor.** |
| A2 | `EssayGradingItemViewModel` cukup untuk page per-worker tanpa field tambahan | Standard Stack | Jika butuh meta worker lain (mis. nilai MC), perlu pembungkus VM — sudah direkomendasikan pembungkus, risiko rendah |
| A3 | Carry 4 nav param via query string adalah pendekatan paling robust untuk "Kembali" | Pitfall 3 | Referer/TempData fallback ada; risiko rendah |

*Semua A1-A3 ringan dan punya mitigasi grep/verifikasi saat planning. Tidak ada asumsi compliance/security yang butuh konfirmasi user.*

## Open Questions

1. **Apakah `.btn-finalize-grading` masih dirender di `AssessmentMonitoringDetail.cshtml` setelah blok `:381-481` diganti tabel?**
   - What we know: Tombol finalize saat ini HANYA di dalam blok essay `:448-476` yang akan diganti. Tabel worker-list baru TIDAK punya tombol finalize (hanya "Tinjau Essay" navigasi).
   - What's unclear: Apakah handler finalize masih perlu hidup di monitoring page.
   - Recommendation: Setelah refactor, grep `btn-finalize-grading` di `AssessmentMonitoringDetail.cshtml`. Jika 0 match → handler finalize pindah total ke `essay-grading.js` + page baru, behavior selalu in-place (D-09). Jika masih ada → param-kan mode.

2. **Pembungkus ViewModel page per-worker: bentuk final?**
   - What we know: Butuh single session identity (`UserFullName`/`UserNIP`/`Status`/`NomorSertifikat`/`Id`/`EssayPendingCount`), `List<EssayGradingItemViewModel>`, flag `IsFinalized`, dan 4 nav param.
   - Recommendation: Buat `EssayGradingPageViewModel` pembungkus (type-safe) ATAU pass `MonitoringSessionViewModel` + `List<EssayGradingItemViewModel>` via ViewBag (pola map existing). Pembungkus lebih bersih untuk single-session page. Planner finalisasi.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet build`/`run`) | Build + lokal verify | ✓ (proyek berjalan) | existing | — |
| SQL Server lokal (HcPortalDB_Dev) | e2e fixture + run | ✓ (asumsi, pola existing pakai) | existing | — |
| Playwright (`tests/`) | UIG-04 e2e | ✓ (`tests/playwright.config.ts`) | existing | — |
| Node/npm (run Playwright) | e2e | ✓ (suite e2e existing) | existing | — |
| `sqlcmd` (snapshot/restore) | e2e seed (SEED_WORKFLOW) | ✓ (dipakai `assessment-pending-grade.spec.ts`) | existing | — |

**Missing dependencies with no fallback:** Tidak ada — semua tooling sudah dipakai suite existing `[VERIFIED: tests/, playwright.config.ts]`.

**Catatan run lokal:** `playwright.config.ts` TIDAK punya blok `webServer` → app TIDAK auto-start. WAJIB jalankan `dotnet run` manual (dengan `Authentication__UseActiveDirectory=false`) sebelum e2e. e2e combined run WAJIB `--workers=1` (memory: NTLM loopback + shared-memory conn). `fullyParallel: false` sudah di config.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework (unit) | xUnit (`HcPortal.Tests/`) |
| Framework (e2e) | Playwright + TypeScript (`tests/e2e/`) |
| Config file (e2e) | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, globalTeardown restore) |
| Quick run command | `dotnet build` (0 error gate) |
| Full unit suite | `dotnet test` (non-Integration; baseline 314/314 per STATE.md) |
| e2e command | `cd tests && npx playwright test --workers=1` (app harus running via `dotnet run` Authentication__UseActiveDirectory=false) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| UIG-01 | Tabel worker-list render (kolom Worker/NIP, EssayPendingCount, badge 3-state) menggantikan blok inline | e2e (runtime — Razor dynamic) | `npx playwright test essay-grading-384 --workers=1` | ❌ Wave 0 |
| UIG-02 | Tombol "Tinjau Essay" navigasi ke page per-worker (URL `/Admin/EssayGrading?sessionId=...`) | e2e | (same spec) | ❌ Wave 0 |
| UIG-03 | Page per-worker: Simpan Skor (AJAX persist EssayScore) + Selesaikan Penilaian (AJAX) reuse endpoint | e2e + build | (same spec) + `dotnet build` | ❌ Wave 0 |
| UIG-04 | Round-trip penuh: list → Tinjau → skor → Selesaikan → state "Selesai" in-place (D-09) | e2e | (same spec) | ❌ Wave 0 |
| (all) | Build hijau 0 error | build | `dotnet build` | ✅ infra ada |
| (regression) | Suite non-Integration tetap hijau | unit | `dotnet test` | ✅ infra ada (314/314 baseline) |

### Sampling Rate

- **Per task commit:** `dotnet build` (0 error). Untuk task view-only (markup): build + grep selector preserved.
- **Per wave merge:** `dotnet test` (non-Integration full, ≥314 baseline) + e2e spec baru hijau.
- **Phase gate:** `dotnet build` 0 error + `dotnet test` hijau + e2e UIG-04 round-trip PASS + UAT manual (browser localhost:5277) sebelum `/gsd-verify-work`.

### e2e Fixture Requirement (UIG-04)

Butuh **1 AssessmentSession** dengan:
- `HasManualGrading = 1`
- Punya ≥1 `PackageQuestion` `QuestionType='Essay'` + `UserPackageAssignment` dgn `ShuffledQuestionIds` berisi essay question id
- Punya `PackageUserResponse` (TextAnswer terisi, `EssayScore = NULL` = ungraded) untuk essay tsb
- `Status = AssessmentConstants.AssessmentStatus.PendingGrading` (agar Selesaikan Penilaian lolos gate `:3524`)
- Untuk uji read-only D-10 (opsional kedua fixture): `Status='Completed'` + `NomorSertifikat` terisi

**Pola seed:** ikuti `tests/sql/pending345-seed.sql` + harness `assessment-pending-grade.spec.ts` (snapshot DB → execScript seed → UAT → restore di `afterAll`, Layer-4 assert bersih). Klasifikasi `temporary + local-only` (CLAUDE.md Seed Workflow). Pertimbangkan reuse fixture e2e exam-taking yang sudah menghasilkan essay-pending session jika ada (cek `exam-taking.spec.ts` helpers) untuk hindari seed manual.

**Flow e2e (D-11):**
1. login admin (`accounts.admin`, inline `loginAny` pola `assessment-pending-grade.spec.ts:25`)
2. goto `/Admin/AssessmentMonitoringDetail?title&category&scheduleDate` → assert tabel worker-list render, baris worker target visible, badge 🟡 "{N} belum dinilai"
3. klik `a:has-text("Tinjau Essay")` → `waitForURL(/EssayGrading/)` → assert header `<h2>` nama worker + kartu `.essay-grading-card` render
4. isi `.essay-score-input` → klik `.btn-save-essay-score` → `waitForResponse(/SubmitEssayScore/ 200)` → assert badge `#badge_{sid}_{qid}` → "Sudah Dinilai" (`bg-success`)
5. klik `.btn-finalize-grading` (auto-confirm dialog) → `waitForResponse(/FinalizeEssayGrading/ 200)` → assert **in-place** "Selesai" state (badge hijau, input disabled), URL TETAP `/EssayGrading` (BUKAN reload ke monitoring) — kunci D-09
6. (opsional) klik "Kembali ke Monitoring" → assert balik ke `AssessmentMonitoringDetail` benar (badge worker tsb → 🟢 "Selesai" setelah reload)

**Razor dynamic → runtime wajib:** grep+build TIDAK cukup membuktikan handler/selector match runtime (pelajaran Phase 354). e2e WAJIB.

### Wave 0 Gaps

- [ ] `tests/e2e/essay-grading-384.spec.ts` — covers UIG-01..04 round-trip
- [ ] `tests/sql/essay-grading-384-seed.sql` — fixture session essay-pending (atau reuse exam-taking helper bila tersedia)
- [ ] (opsional) `HcPortal.Tests/` regression — endpoint TIDAK diubah; minimal — verifikasi build + suite existing hijau (tak perlu test backend baru kecuali planner ingin lock new GET action authz)

*(Tidak ada framework install gap — Playwright + xUnit sudah ada.)*

## Security Domain

> `security_enforcement` tidak di-set `false` di config → disertakan.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Backend unchanged; 1 GET action baru read-only (clone loader), 0 DB write baru |
| V2 Authentication | yes | `[Authorize]` class-level (`AdminBaseController:12`) + role gate eksplisit |
| V4 Access Control (IDOR utama) | **yes** | GET `EssayGrading(sessionId)` HARUS `[Authorize(Roles = "Admin, HC")]` (samakan `SubmitEssayScore`/`FinalizeEssayGrading`). Admin/HC = full-access tier (pola in-repo, lihat `AdminBaseController` komentar "no role scoping — Admin/HC full access" `:47`). Tidak ada user-level ownership check yang relevan (HC menilai semua worker). |
| V5 Input Validation | yes | `sessionId` int bind; validasi `session == null \|\| !HasManualGrading` → redirect aman. Skor divalidasi backend existing (`:3472` range 0..ScoreValue). |
| V4.2 CSRF | yes | POST reuse `[ValidateAntiForgeryToken]` (existing) + token via `#antiforgeryForm`. GET action = read-only, no state change → no antiforgery needed pada GET. |
| V6 Cryptography | no | Tidak ada operasi kripto baru |

### Known Threat Patterns for ASP.NET Core MVC + page-by-id

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — buka grading sesi sembarang via `?sessionId=` | Information Disclosure / Elevation | `[Authorize(Roles = "Admin, HC")]` pada GET action baru (gate role; Admin/HC memang berwenang lihat semua sesi — konsisten dgn monitoring). Verifikasi attribute terpasang. |
| CSRF pada submit/finalize | Tampering | Endpoint POST existing sudah `[ValidateAntiForgeryToken]` — TAK diubah; page baru kirim token `#antiforgeryForm` |
| Bypass read-only finalized → submit ke sesi Completed | Tampering | D-10 render input disabled client-side; **defense-in-depth backend sudah ada** — `FinalizeEssayGrading` `:3506` no-op friendly saat Completed (`alreadyFinalized`); `SubmitEssayScore` tetap validasi range (tak ada gate status, tapi tak mengubah lifecycle — skor saja). Read-only client = UX guard, bukan satu-satunya. |
| XSS via TextAnswer/QuestionText/Rubrik di kartu clone | Tampering/Info | Razor auto-encode `@essayItem.TextAnswer` (pola existing `:419` aman). JANGAN pakai `@Html.Raw`. |

**Catatan:** Tidak ada DB write baru, tidak ada endpoint POST baru, tidak ada migration. Permukaan serang tambahan = 1 GET read-only yang sudah ter-gate role. Risk profile rendah asalkan authz attribute terpasang benar.

## Sources

### Primary (HIGH confidence) — semua VERIFIED dari codebase session ini
- `Controllers/AssessmentAdminController.cs:3273-3452` — `AssessmentMonitoringDetail` action + `EssayGradingMap` builder `:3413-3448`
- `Controllers/AssessmentAdminController.cs:3455-3534+` — `SubmitEssayScore` + `FinalizeEssayGrading` (kontrak + authz + antiforgery)
- `Controllers/AssessmentAdminController.cs:19` + `Controllers/AdminBaseController.cs:12-13` — routing `[Route("Admin/[action]")]` + class `[Authorize]`
- `Views/Admin/AssessmentMonitoringDetail.cshtml:218-231` (tabel sesi), `:381-481` (blok essay), `:407-446` (kartu), `:448-476` (finalize), `:484` (antiforgery), `:1472-1558` (handler AJAX), `:55-67` (breadcrumb+back), `:1232` (@section Scripts), `:489` (hScheduleDate)
- `Models/AssessmentMonitoringViewModel.cs:48-86` — `MonitoringSessionViewModel` + `EssayGradingItemViewModel`
- `Views/Shared/_QuestionImage.cshtml` — partial gambar soal (reflection-based, null-skip)
- `Views/Shared/_Layout.cshtml:55` — global `appUrl(path)` helper (basePath-aware)
- `Program.cs:79-81,214-216` — auth toggle `Authentication:UseActiveDirectory` (default false) + default route
- `tests/playwright.config.ts` (baseURL/fullyParallel/teardown), `tests/helpers/accounts.ts` (admin/hc creds), `tests/e2e/assessment-pending-grade.spec.ts` (seed/snapshot pola), `tests/e2e/assessment.spec.ts:266-394` (FLOW 9 essay finalize selectors), `tests/sql/pending345-seed.sql` (seed shape)
- `.planning/config.json` (nyquist_validation:true, ui_phase:true), `.planning/STATE.md` (baseline 314/314)

### Secondary (MEDIUM)
- `.claude` memory: Phase 354 (Razor dynamic → Playwright runtime), Phase 355 (Authentication__UseActiveDirectory=false), Phase 310 (finalize gate D-02), local e2e SQL env fix (--workers=1, shared-memory)

### Tertiary (LOW)
- Tidak ada — phase tidak butuh sumber eksternal/web.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua komponen dibaca langsung dari kode, tak ada library baru
- Architecture: HIGH — pola GET action + clone builder + reuse markup terverifikasi line-by-line
- Pitfalls: HIGH — selector binding & handler behavior dibaca dari `:1472-1558`; D-09 titik perubahan eksplisit
- Validation: HIGH (infra) / MEDIUM (fixture seed — perlu konfirmasi apakah reuse helper exam-taking vs seed SQL baru)
- Security: HIGH — IDOR mitigasi = reuse role-gate existing; 0 write baru

**Research date:** 2026-06-15
**Valid until:** 2026-07-15 (30 hari — codebase stabil; re-verify line numbers bila `AssessmentAdminController.cs` diedit session paralel, seperti diperingatkan)
