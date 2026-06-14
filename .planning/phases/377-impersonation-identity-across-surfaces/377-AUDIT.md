# Phase 377 — Audit Call-Site Identity Resolution (SC1 / D-07 / IMP-02)

**Tanggal:** 2026-06-14
**Tujuan:** Enumerator otoritatif fix-set (D-06). Fix Wave 2-3 WAJIB mengikuti audit ini, bukan menebak. Setiap surface read worker-data-diri yang resolve identitas user harus me-resolve **effective user X** saat impersonasi (mode `user`), bukan admin asli.

## Metodologi

Enumerasi via Grep di `Controllers/`:
```
GetUserAsync\(User\)|GetCurrentUserRoleLevelAsync
```
plus equivalents (`User.FindFirstValue`, `GetEffectiveRoleLevel()`). Triage per kriteria **D-06**:
- **IN-SCOPE** = jalur GET yang me-resolve identitas worker untuk **data-diri** (records/cert/results/progress/assessment milik diri).
- **BORDERLINE** = team-view scope (D-01 full-fidelity menyelesaikan otomatis via effective role-level + section) atau split-brain per-bagian.
- **OUT-OF-SCOPE** = POST/write (diblok `ImpersonationMiddleware` mode read-only, actor=admin asli BENAR), admin-scope read (`[Authorize]`-gated, bukan self-data), atau konten per-role (guide).

Total raw match `GetUserAsync(User)` di codebase ~120 (mayoritas write-actor controller admin). Tabel di bawah = yang RELEVAN + parity penuh untuk CMP/CDP/Home (lihat §Parity Check).

> **⚠ DRIFT CATALOG (Phase 378 paralel, commits `5cd3bda6`/`6e439d06`/`bfee5a16`):** Line number di plan 377-01 (ditulis sebelum 378 ship) sebagian bergeser:
> - `CMPController.cs:3693` (CertificationManagement full action + `ViewBag.UserBagian`) — **DIHAPUS oleh 378**. Sekarang `CMP/CertificationManagement` = redirect stub `CMPController.cs:3589` → `RedirectToAction("CertificationManagement","CDP")` (canonical pindah ke CDP). Tidak lagi membaca user-data → **N/A** (gugur dari audit fix).
> - `CMPController.cs:3870` (BuildSertifikatRowsAsync) — sekarang **`CMPController.cs:3658`** (def L3656).
> - CDP line (3696/3740/3859) TETAP.
> Audit ini memakai **line number CURRENT (verified 2026-06-14)**.

---

## Tabel 1 — IN-SCOPE: self-worker-data READ (WAJIB fix)

| file:line | surface (action) | verb | jenis read | impersonation-aware sekarang? | in-scope? | aksi fix |
|-----------|------------------|------|-----------|------------------------------|-----------|----------|
| `CMPController.cs:481` | `Records` | GET | unified records diri `GetUnifiedRecords(user.Id)` | ❌ | **YA** | via resolver (auto, hulu di `GetCurrentUserRoleLevelAsync`) + **D-03 null-handling** (mode-role → kosong+hint, BUKAN redirect ke Login — Pitfall 1) |
| `CMPController.cs:545` | `RecordsWorkerDetail` (own) | GET | unified records `workerId`; own-branch `workerId==user.Id` | ❌ | **YA** | via resolver (auto); cabang other-worker tetap pakai role-level efektif X |
| `CMPController.cs:1733` | `Certificate` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | **YA** | via resolver (auto); ownership ikut X (D-01) |
| `CMPController.cs:1839` | `CertificatePdf` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | **YA** | via resolver (auto) |
| `CMPController.cs:2080` | `Results` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | **YA** | via resolver (auto) |
| `CMPController.cs:203` | `Assessment` | GET | **BYPASS resolver** — `GetUserAsync(User)` → `AssessmentSessions.UserId==userId` | ❌ | **YA** | **route ke resolver** (BUKAN auto — direct `GetUserAsync` call) |
| `CMPController.cs:611` | `ExportRecords` (personal) | GET | **BYPASS resolver** — `GetUnifiedRecords(user.Id)` | ❌ | **YA** | route ke resolver |
| `CMPController.cs:867` | `StartExam` | GET | **BYPASS resolver** — `assessment.UserId != user.Id` authz; **ADA write-on-GET** (auto-transition Save) | ❌ | **YA** | route ke resolver + **GUARD write-on-GET** (skip `SaveChangesAsync` saat `IsImpersonating()` — Pitfall 3, invariant read-only) |
| `CMPController.cs:3658` | `BuildSertifikatRowsAsync` (l5OwnDataOnly) | GET | scoped user-ids by role + own-data L5 (`user.Id`) | ❌ | **YA** | via resolver CMP (auto). *(was plan:3870 — drift 378)* |
| `HomeController.cs:38` | `Index` | GET | **BYPASS** — `GetProgress(user.Id)` + `GetUpcomingEvents(user.Id)` | ❌ (**split-brain**: L53 role-level efektif, identitas asli) | **YA** | route ke effective user X (Plan 05); fold L53 jadi 1 sumber (D-05) |
| `CDPController.cs:3859` | `BuildSertifikatRowsAsync` (l5OwnDataOnly) | GET | scoped user-ids by role + own-data L5 | ❌ | **YA** | via resolver CDP (auto, Plan 04) |

**Total IN-SCOPE: 11 call-site** (10 lewat 2 resolver `GetCurrentUserRoleLevelAsync` + 3 direct-bypass yang di-route manual: Assessment/ExportRecords/StartExam).

---

## Tabel 2 — BORDERLINE: team-view scope & split-brain (D-01 selesaikan otomatis)

| file:line | surface (action) | verb | jenis read | impersonation-aware sekarang? | in-scope? | aksi fix |
|-----------|------------------|------|-----------|------------------------------|-----------|----------|
| `CMPController.cs:660` | `ExportRecordsTeamAssessment` | GET | team-view; `if(roleLevel>=5) Forbid()`; `user.Section` scope | otomatis-via-resolver | **YA (otomatis)** | Level/section efektif X mengatur (D-01: Coachee X L6 → Forbid BENAR; L4 X → X.Section). Tak butuh edit terpisah |
| `CMPController.cs:721` | `ExportRecordsTeamTraining` | GET | sama 660 | otomatis | **YA (otomatis)** | sama |
| `CMPController.cs:774` | `RecordsTeamPartial` | GET | sama; `user.Section` scope L4 | otomatis | **YA (otomatis)** | sama |
| `CMPController.cs:507` (dalam `Records`) | Team-View tab block | GET | `if(roleLevel<=4)` + `user.Section` filter | otomatis | **YA (otomatis)** | effective level/section X (via resolver hulu) |
| `CMPController.cs:86` | `DokumenKkj` | GET | **split-brain**: `GetEffectiveRoleLevel()` (L88) tapi `currentUser.Section` (admin, dari `GetUserAsync` L86) | ❌ partial | **TIDAK (default OUT, A3)** | KKJ docs per-bagian, bukan data-diri murni. **Rationale:** dokumen kompetensi per-section, bukan record/cert milik worker. **Follow-up flag:** bila UAT temukan leak self-data section admin saat impersonate → naikkan in-scope. Catat eksplisit (resolved Q3 — tidak silently drop) |

---

## Tabel 3 — OUT-OF-SCOPE: write/admin/authz/guide (TIDAK disentuh, D-02)

| pola / file:line | surface (action) | verb | jenis read | imp-aware? | in-scope? | alasan OUT |
|------------------|------------------|------|-----------|-----------|-----------|-----------|
| POST write-actor (audit `actor`/`CreatedBy`) — `TrainingAdminController`, `CoachMappingController`, `AssessmentAdminController`, `DocumentAdminController`, `OrgLabelController`, `OrganizationController`, `WorkerController`, `AdminController`, `AccountController` | berbagai admin action | POST | actor untuk log/CreatedBy | n/a (principal) | **TIDAK** | Write diblok `ImpersonationMiddleware` (mode read-only, L84-104). Actor audit = admin asli (BENAR — admin yang melakukan aksi) |
| Admin-scope read (semua user, bukan diri) — `ProtonDataController` (176,233,…), `AssessmentAdminController` GET monitoring | monitoring/dashboard admin | GET | data semua worker | n/a | **TIDAK** | Bukan self-worker-data; `[Authorize]`-gated; admin/HC memang lihat semua |
| `CMPController.cs:3589` | `CertificationManagement` (redirect stub) | GET | — (redirect ke CDP) | n/a | **TIDAK (N/A)** | **378 hapus full action**; sekarang `RedirectToAction("CertificationManagement","CDP")`. Tidak baca user-data. *(plan:3693 obsolete)* |
| `CDPController.cs:3740` | `CertificationManagement` (`ViewBag.UserBagian = ...User.Section`) | GET | section admin untuk filter cert-mgmt | ❌ | **TIDAK (default OUT, A4)** | Admin-scope cert management (lihat semua sertifikat per-bagian). **Rationale:** bukan data-diri; section dipakai untuk filter admin-view, bukan ownership worker. Tidak ada data-leak risk (cert-mgmt memang admin-scope). Klasifikasi eksplisit (resolved Q3). *(Catatan: BuildSertifikatRowsAsync CDP:3859 yang dipanggilnya = IN-SCOPE Tabel 1 untuk path l5OwnDataOnly)* |
| `NotificationController.cs:18` | notifikasi user | GET | `User.FindFirstValue(NameIdentifier)` | n/a | **TIDAK** | Notif milik principal (admin) — bukan worker-data read surface |
| `Services/GuideContentProvider.cs` + `HomeController.cs:329`, `HomeController.cs:346` | `Guide` / `GuideDetail` | GET | konten guide per-role | n/a | **TIDAK** | Konten statis per-role, tak resolve identitas worker untuk data-diri. OUT (CONTEXT: Guide tegas OUT) |

---

## Resolver definition sites (di-rewrite Wave 2-3, BUKAN call-site)

| file:line | signature current | rewrite |
|-----------|-------------------|---------|
| `CMPController.cs:2388` | `private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()` — **nullable** (guard `if(user==null)`) | Konsumsi resolver Plan 02; impersonation-aware (effective user X + effective role-level X). ~8 caller self-read terfix otomatis di hulu (Plan 03) |
| `CDPController.cs:3696` | `private async Task<(ApplicationUser User, int RoleLevel)> GetCurrentUserRoleLevelAsync()` — **non-null** (`user!`) | Inject `ImpersonationService` dulu (**Pitfall 2** — CDP constructor belum punya, beda dari CMP/Home) + rewrite + **seragamkan ke NULLABLE** (mode-role D-03 butuh null path). Plan 04 |

**Signature divergence [VERIFIED]:** CMP `(ApplicationUser? User, int)` nullable vs CDP `(ApplicationUser User, int)` non-null + `user!`. Konsolidasi → **NULLABLE**. Caller yang asumsi non-null perlu null-guard:
- CDP: `CDPController.cs:3740` (`.User.Section`), `CDPController.cs:3859`.
- CMP: sudah nullable (guard ada).

---

## Parity Check

**Grep:** `GetUserAsync\(User\)|GetCurrentUserRoleLevelAsync` di `Controllers/` (run 2026-06-14).

**CMPController.cs** — 25 match. Triage:
- IN-SCOPE (Tabel 1): 481, 545, 1733, 1839, 2080, 203, 611, 867, 3658 (9).
- BORDERLINE (Tabel 2): 86, 660, 721, 774 (+ 507 non-grep, dalam Records) (4).
- Resolver def: 2388, 2390 (def + body `GetUserAsync`).
- OUT (Tabel 3 — write/other-action GET non-self): 359 (`AssessmentResult`/other), 435, 831, 1195, 1250, 1276, 1504, 3978, 4202, 4406 → write-actor POST atau admin/other-worker action (diblok middleware / bukan self-data-read). Diringkas per-pola Tabel 3 (acceptance: admin/write boleh ringkas).
- 3589 (CertificationManagement) tidak match grep (redirect stub, tak panggil GetUserAsync) — dicatat sebagai drift N/A.

**CDPController.cs** — 33 match. Triage:
- IN-SCOPE: 3859 (1).
- OUT: 3740 (cert-mgmt admin-scope). Resolver def 3696/3698.
- Sisanya (60,265,295,316,676,820,886,1213,…,4302) = write-actor POST / admin-scope CDP read (track/coaching/assignment admin) → OUT per-pola (diblok middleware / `[Authorize]`-gated, bukan self-worker-data-read).

**HomeController.cs** — 3 match: 38 (IN-SCOPE), 329 (OUT Guide), 346 (OUT GuideDetail). Semua ter-triage.

✅ **Parity terpenuhi:** setiap match CMP/CDP/Home ter-triage di salah satu tabel (in-scope, borderline, atau out per-pola). Tidak ada borderline (DokumenKkj, CertificationManagement) yang hilang diam-diam (T-377-02 mitigated).

---

## Ringkasan fix-set untuk Wave 2-3

| Plan | Target | Aksi |
|------|--------|------|
| 02 | `ImpersonationService` + `ImpersonationMiddleware` | Tambah `ResolveEffectiveUserDecision` (pure) + `GetEffectiveUserAsync()` resolver + fail-closed D-04 |
| 03 | `CMPController` (2388) + `Records.cshtml` | Rewrite resolver → 9 caller self-read auto-fix; route 3 bypass (203/611/867); D-03 null-handling Records; guard write-on-GET StartExam |
| 04 | `CDPController` (3696) | Inject service (Pitfall 2) + rewrite resolver → nullable; null-guard 3740/3859 |
| 05 | `HomeController` (38) | Fold split-brain L38/L53 → effective user X; D-03 kosong |
| 06 | e2e + integration | SC2/SC3/SC4 verify + UAT browser |
