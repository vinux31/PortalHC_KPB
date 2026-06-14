# Phase 377: Impersonation Identity Across Surfaces - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Surface worker-data (CMP/Records, Assessment, Home progress, dan semua read worker-data-diri lain) harus me-resolve **identitas user yang di-impersonate** (mode `user` → `TargetUserId`), bukan admin asli. Mencakup:

1. **Audit (SC1):** Petakan SEMUA call-site `GetCurrentUserRoleLevelAsync` / `_userManager.GetUserAsync(User)` lintas controller, klasifikasi worker-data-read (in-scope) vs admin-scope/`[Authorize]`-gated (out-scope).
2. **Fix (SC2/SC3):** Worker-data read surfaces resolve effective user X saat impersonasi aktif → banner "Anda melihat sebagai X" jujur.
3. **No regression (SC4):** Mode normal (non-impersonate) tak berubah.

**Migration=false.** Backend identity-resolution only — tak ada perubahan skema, tak ada view baru (kecuali hint mode-role minor).

**OUT of scope (tegas):**
- Attribute `[Authorize(Roles=...)]` ASP.NET tetap pakai principal asli (admin) — phase 377 TIDAK mengubah authorization gating controller. Admin tetap bisa mengakses halaman admin saat impersonasi (perilaku pre-existing).
- Write paths — sudah diblok `ImpersonationMiddleware` (mode read-only). Audit menyempit ke jalur GET/read.
- Copy banner impersonasi (sudah ada).

</domain>

<decisions>
## Implementation Decisions

### Model Fidelity Impersonasi
- **D-01:** **Full-fidelity (effective user = X)** untuk worker-data read surfaces. Saat impersonate user X, jalur read worker-data resolve identitas X penuh: **data, authz/ownership, DAN scope** ikut X. "Impersonate X = lihat persis seperti X login" — batasan akses X berlaku (Coachee X tak lihat team-view; Coach/SectionHead X lihat tim sesuai `X.Section`). Effective role-level + effective user-id digabung jadi satu identitas efektif.
- **D-02:** **Boundary = worker-data read surfaces saja.** Resolusi identitas diterapkan ke jalur read worker-data-diri; attribute `[Authorize]` gating TETAP principal asli (out of scope). Audit SC1 fokus call-site worker-data.

### Mode "role" (tanpa user spesifik)
- **D-03:** Saat impersonate ROLE (bukan user X), tak ada `TargetUserId` → **effective user = null → surface worker-data tampil kosong (0 record) + hint** "Pilih user spesifik untuk melihat data worker". Mode role tetap berguna untuk preview UI/fitur per role-level (sudah jalan via `GetEffectiveRoleLevel`). Tidak menampilkan data admin (akar bug), tidak redirect/hide.

### Fallback Target User Null/Terhapus
- **D-04:** Saat impersonate user X tapi X tak ditemukan (`FindByIdAsync` null / user terhapus): **auto-`Stop()` sesi impersonasi + redirect `/Admin/Index` + pesan** "User yang di-impersonate tidak ditemukan". Konsisten dengan pola auto-expire `ImpersonationMiddleware` yang sudah ada. Aman, tak bocor, jelas ke admin.

### Unifikasi Resolusi Role-Level (kill split-brain)
- **D-05:** **Satukan jadi 1 sumber kebenaran.** `GetCurrentUserRoleLevelAsync()` dibuat impersonation-aware: return **effective USER (X)** + **effective ROLE-LEVEL (role X)** saat impersonasi. Call-site `GetEffectiveRoleLevel()` existing (HomeController:53, CMPController:88) dikonsolidasi/diselaraskan ke sumber tunggal. Sesuai pola proyek "shared core kill drift" (363/365/366).

### Fix-Set Worker-Data In-Scope
- **D-06:** **Semua read worker-data-diri** difix, bukan hanya 3 yang disebut roadmap. Termasuk: `Records` + `RecordsWorkerDetail` (own) + `Results`/`Certificate`/`CertificatePdf` (ownership = X via `IsResultsAuthorized`) + Home `GetProgress`/upcoming-events + exam `StartExam`/exam-taking + sisanya yang ditemukan audit. **Kriteria:** jalur read yang resolve identitas worker untuk data-diri. **Audit SC1 = enumerator otoritatif** (cakupan penuh → banner jujur di semua surface).

### Deliverable Audit (SC1)
- **D-07:** Peta call-site disimpan di **`377-AUDIT.md`** di phase dir. Tabel terstruktur per call-site: `file:line` | surface | jenis read | impersonation-aware sekarang? | in-scope? | aksi fix. Doc audit mandiri, jadi input planner, sesuai pola fase audit lalu (328).

### Claude's Discretion
- **Arsitektur fix konkret:** Bentuk helper terpusat (mis. `ImpersonationService.GetEffectiveUserId()` + `GetEffectiveUserAsync()`, atau ubah `GetCurrentUserRoleLevelAsync` per-controller vs ekstrak shared helper) diserahkan ke research/planner — selaras D-05 (1 sumber) + pola shared-core. CMP & CDP punya `GetCurrentUserRoleLevelAsync` terpisah; planner putuskan konsolidasi vs paralel-konsisten.
- **Lokasi fallback D-04:** di middleware (saat resolve `SetContextItems`) atau di helper resolusi — planner pilih yang paling DRY.
- **Bentuk hint mode-role (D-03):** copy & penempatan hint UI minor — Claude/planner.
- **Strategi test SC4 (no-regression):** xUnit/e2e coverage normal vs impersonate — planner/research.

### Folded Todos
*(none — tidak ada todo yang di-fold)*

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/ROADMAP.md` — Phase 377 entry + SC1-SC4 (baris ~42-46, 67-71). Goal: worker-data resolve impersonated identity.
- `.planning/REQUIREMENTS.md` §IMP — **IMP-01** (worker-data tampil data user impersonated, banner jujur) + **IMP-02** (audit cakupan semua call-site `GetCurrentUserRoleLevelAsync`/`GetUserAsync(User)`).
- `.planning/ROADMAP.md` Phase 999.6 (baris ~713-727) — **bukti live 2026-06-10**: impersonate Iwan → `/CMP/Records` tampil 2 assessment MILIK ADMIN (AssessmentSessions Id 157+66, UserId admin@pertamina.com), Training Manual=0 padahal Iwan punya 3 TrainingRecords. `CMPController.Records:481` pakai `GetCurrentUserRoleLevelAsync()` → resolve admin asli.

### Project workflow
- `CLAUDE.md` — Develop Workflow (Lokal→Dev→Prod; verifikasi lokal wajib; jangan edit Dev/Prod; flag IT migration).

*Tidak ada spec/ADR eksternal — bug-fix di codebase existing (REQUIREMENTS.md: "Bug-fix di codebase existing — no domain research").*

</canonical_refs>

<code_context>
## Existing Code Insights

### Akar Bug (terkonfirmasi saat scout)
- `Services/ImpersonationService.cs` — `GetTargetUserId()` (L92) ADA tapi **hanya dipakai middleware**, tak ada controller resolve identitas user X buat query data. `GetEffectiveRoleLevel()` (L100) + `GetEffectiveSelectedView()` (L122) sudah ada (role-level/view). **Gap = effective USER resolution.**
- `Middleware/ImpersonationMiddleware.cs` — `SetContextItems` (L111) set `HttpContext.Items`: role-level/view/name. mode=`user` resolve target role info (L128-145) TAPI tak menyetel identitas user buat data query. Enforcement read-only (block write L84-104) + auto-expire 30min Stop()+redirect (L57-66) = **pola fallback untuk D-04**.
- `Controllers/CMPController.cs:2388` `GetCurrentUserRoleLevelAsync()` → `_userManager.GetUserAsync(User)` (L2390) = **admin asli** (User principal tak berubah saat impersonasi). Call-site worker-data: `Records:481`→`GetUnifiedRecords(user.Id)`(L484), `RecordsWorkerDetail:545`, dan 660/721/774/1733/1839/2080/3693/3870.
- `Controllers/CDPController.cs:3696` `GetCurrentUserRoleLevelAsync()` (varian terpisah, L3698 `GetUserAsync(User)`); call-site 3740/3859.
- `Controllers/HomeController.cs:38` `GetUserAsync(User)` → `GetProgress(user.Id)` (L42) = progress admin (bug). L53 sudah pakai `GetEffectiveRoleLevel()` (role-level aware) — **bukti split-brain** (role-level effective tapi user identity asli). Juga `GetUserAsync` L329/L346.

### Established Patterns
- **Read-only impersonation:** write diblok middleware → audit hanya GET/read paths.
- **Effective role-level sudah wired** (Home:53, CMP:88) — fix tinggal tambah effective USER + unify (D-05).
- **Shared-core extract kill drift** (363/365/366) — fix terpusat (D-05/Discretion).
- `Models/UserRoles.GetRoleLevel`/`GetDefaultView` — sumber mapping role→level/view.
- `CMPController.IsResultsAuthorized` (L2399) — ownership authz (owner ∥ L1-3 ∥ L4 section-scoped); di full-fidelity `currentUserId` = X.

### Integration Points
- `ImpersonationService` — titik tambah resolusi effective user (D-05).
- `GetCurrentUserRoleLevelAsync` (CMP + CDP) — titik unify impersonation-aware (D-05).
- `WorkerDataService.GetUnifiedRecords(userId)` / `GetWorkersInSection(sectionFilter)` — konsumen `user.Id`/`user.Section`; otomatis benar bila effective user di-resolve di hulu.

### Controller untuk di-triage audit (SC1)
Punya match `GetUserAsync(User)`/`GetCurrentUserRoleLevelAsync` — klasifikasi worker-data-read vs admin-scope: AssessmentAdminController, TrainingAdminController, CoachMappingController, ProtonDataController, WorkerController, OrganizationController, DocumentAdminController, AccountController, AdminController, OrgLabelController, GuideContentProvider (+ CMP/CDP/Home di atas).

</code_context>

<specifics>
## Specific Ideas

- Banner "Anda melihat sebagai X" sudah ada — fix harus membuatnya JUJUR (data = X), bukan mengubah copy.
- Mental model target: "impersonate user X = act as X for read" (full-fidelity) — effective user X + effective role-level X dari satu sumber.

</specifics>

<deferred>
## Deferred Ideas

- **Copy/UX banner & hint mode-role** — penyempurnaan teks minor, bukan inti bug (Claude discretion saat implementasi).
- **Full sandbox login-as-X** (swap identitas di SEMUA call-site termasuk non-worker-data + override `[Authorize]`) — DITOLAK untuk 377 (scope luas, risiko regresi tinggi). Bila perlu, fase tersendiri.

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` — match false-positive (keyword "data/audit/phase"); soal cleanup DB test lokal pasca Phase 367, tak terkait identitas impersonasi. Tidak di-fold.

</deferred>

---

*Phase: 377-impersonation-identity-across-surfaces*
*Context gathered: 2026-06-14*
