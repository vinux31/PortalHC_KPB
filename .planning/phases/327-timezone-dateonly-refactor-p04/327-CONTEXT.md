# Phase 327: Timezone DateOnly Refactor (P04) - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** interactive (4 awal + 2 follow-up VM rollup + 3 audit-miss follow-up = 9 questions, 13 decisions locked)

<domain>
## Phase Boundary

Eliminasi timezone drift permanen untuk `ValidUntil` dengan migrasi `DateTime? → DateOnly?` di 2 entity (TrainingRecord + AssessmentSession) + cascade refactor downstream (4 VM + 5 rollup props + 2 computed props + DeriveCertificateStatus + view binding audit + EF migration `ChangeValidUntilToDateOnly`).

**In-scope:**
- P04 (MED): DateOnly migration penuh end-to-end. Type konsisten dari entity → VM → rollup → display.
- DeriveCertificateStatus refactor pakai `DateOnly.FromDateTime(DateTime.UtcNow)`.
- TrainingRecord.DaysUntilExpiry + IsExpiringSoon rewrite ke DateOnly arithmetic.
- UnifiedTrainingRecord.IsExpired flip dari `DateTime.Now` → `DateOnly.FromDateTime(DateTime.UtcNow)` (kill 2nd tz bug).
- ImportTraining controller Excel parse audit + cast ClosedXML DateTime → DateOnly.
- xUnit DeriveCertificateStatus test 5 case + boundary di HcPortal.Tests/.
- Pre-migration sqlcmd check komponen jam non-zero (inline IT_NOTIFY.md).
- 5 halaman wajib + PDF generation smoke test.

**Out-of-scope (defer v20.0):**
- `DateTime.Now` di lokasi non-ValidUntil (logging, audit timestamp, CreatedAt) — defer per spec §7.1.
- P05 Soft Delete proper (implement IsDeleted column + global query filter).
- P09 DB CHECK constraint untuk Permanent+ValidUntil mutual exclusion.
- Phase 326 sisa non-blocking finding (validator order self-renewal di Edit + Tom Select UX) — per memory 326 finding "defer" (D-12).
- Helper `Clock.Today()` centralized + DI swap untuk test (overkill saat ini).
- JsonConverter explicit DateOnly format spoof (lawan tujuan migrasi).
- Renewal dashboard cache (P11), RBAC integration test (P12).

</domain>

<decisions>
## Implementation Decisions

### Locked di Spec (`docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §7)

- **D-01:** Strategy A DateOnly (bukan B UtcNow standardize). Rasional: cert harian semantik, mati permanen, scope sempit, low risk.
- **D-02:** Migration name `ChangeValidUntilToDateOnly`. Apply: `TrainingRecords.ValidUntil datetime2 → date`, `AssessmentSessions.ValidUntil datetime2 → date`. SQL Server auto `CAST(datetime2 AS date)` truncate time component.
- **D-03:** Rollback EF `Down()` revert date → datetime2 (data jam = 00:00:00 acceptable lossy).
- **D-04:** Razor `[DataType(DataType.Date)]` annotation tetap (kompatibel DateOnly?). Format display `yyyy-MM-dd` atau `"dd MMMM yyyy"` output identik untuk specifier date (d/M/y).
- **D-05:** .NET 8 confirmed (HcPortal.csproj `<TargetFramework>net8.0</TargetFramework>`) → DateOnly native binder + EF Core LINQ translation native.
- **D-06:** DeriveCertificateStatus signature refactor (`DateOnly? validUntil`). Today reference `DateOnly.FromDateTime(DateTime.UtcNow)`. `days = validUntil.DayNumber - today.DayNumber`.

### Gray Area Decisions (user-selected interactive)

#### Type Scope — VM Flip
- **D-07:** Entity + ALL 4 VM ValidUntil flip ke DateOnly? (Q1 jawaban "Entity + ALL VMs flip Recommended").
  - **Rasional:** Konsisten type end-to-end. Form binder kerja native (`asp-for="ValidUntil" type="date"`). Tidak ada cast manual di handler. SertifikatRow.ValidUntil + DeriveCertificateStatus signature all DateOnly?.
  - **Scope flip:**
    - `Models/CreateTrainingRecordViewModel.cs:51` — `DateTime? ValidUntil` → `DateOnly? ValidUntil`
    - `Models/EditTrainingRecordViewModel.cs:53` — sama
    - `Models/CreateManualAssessmentViewModel.cs:34, 97` — 2 occurrence, sama
    - `Models/CertificationManagementViewModel.cs:38` (SertifikatRow.ValidUntil) — sama

#### Type Scope — Rollup Props Flip
- **D-08:** Semua rollup props flip DateOnly? juga (Q1 follow-up jawaban "Semua rollup flip DateOnly? Recommended").
  - **Scope flip:**
    - `Models/CertificationManagementViewModel.cs:74` (CertificateChainGroup.LatestValidUntil) → DateOnly?
    - `Models/CertificationManagementViewModel.cs:108` (CertificateGroup.MinValidUntil) → DateOnly?
    - `Models/UnifiedTrainingRecord.cs:26` (UnifiedTrainingRecord.ValidUntil) → DateOnly?
    - `Models/UnifiedTrainingRecord.cs:40` (IsExpired computed) → `ValidUntil.HasValue && ValidUntil.Value < DateOnly.FromDateTime(DateTime.UtcNow)` — DateTime.Now → UtcNow alignment Q2.
    - `RenewalCertificateRow.TanggalExpired` (CMPController L2611, L2634, L2992 area) — VM property flip DateOnly?. Plan-phase audit nama exact VM kelas.

#### Today Reference
- **D-09:** `DateOnly.FromDateTime(DateTime.UtcNow)` per spec verbatim (Q2 jawaban "Recommended").
  - **Rasional:** Pertamina pakai aplikasi jam kerja 07:00-17:00 WIB — boundary 00:00-07:00 WIB (UtcNow.Date = kemarin) tidak kena workflow. Acceptable.
  - **Call sites refactor:**
    - `Models/CertificationManagementViewModel.cs:59` (DeriveCertificateStatus) — refactor signature + today
    - `Models/TrainingRecord.cs:77` (IsExpiringSoon `(ValidUntil.Value - DateTime.UtcNow).Days`) — rewrite
    - `Models/TrainingRecord.cs:91` (DaysUntilExpiry `(ValidUntil.Value - DateTime.UtcNow).Days`) — rewrite
    - `Models/UnifiedTrainingRecord.cs:40` (IsExpired) — flip + UtcNow
    - `Controllers/CMPController.cs:2597, 2620, 2762, 2980, 3000` (`var today = ...`) — rewrite ke `var today = DateOnly.FromDateTime(DateTime.UtcNow)`. Plan-phase audit `thirtyDaysFromNow` + `futureDate` derive method.
    - `Controllers/CDPController.cs:3823-3886` query expr ValidUntil — confirm today variable type alignment.
    - `Controllers/RenewalController.cs:123-149` query expr — sama.

#### Computed Properties Strategy
- **D-10:** Rewrite ke DateOnly arithmetic (Q3 jawaban "Recommended").
  - **Pattern:** `(ValidUntil.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber)`
  - **Files:**
    - `Models/TrainingRecord.cs:75-78` (IsExpiringSoon) — rewrite
    - `Models/TrainingRecord.cs:89-92` (DaysUntilExpiry) — rewrite
    - `Models/UnifiedTrainingRecord.cs:40` (IsExpired) — rewrite + UtcNow alignment
  - **Audit call site:** Plan-phase tugas verify ada usage di Razor/Controller — kalau zero (computed prop unused), pertimbangkan delete untuk dedup logic. SertifikatRow.DeriveCertificateStatus sudah ada paralel implementation.

#### Pre-migration Check Artifact
- **D-11:** Inline sqlcmd di IT_NOTIFY.md + execute lokal manual (Q4 jawaban "Recommended").
  - **Rasional:** Pattern Phase 323/324 IT_NOTIFY. Zero file artifact baru di repo.
  - **Cek SQL:**
    ```sql
    SELECT COUNT(*) FROM TrainingRecords WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';
    SELECT COUNT(*) FROM AssessmentSessions WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';
    ```
  - **Capture lokasi:** SEED_JOURNAL.md entry + IT_NOTIFY.md "MIGRATION REQUIRED" section.

#### Phase 326 Followup Bundle
- **D-12:** Defer ke v20.0 backlog (Q follow-up jawaban "Defer Recommended").
  - **Rasional:** Sequential strict v19.0. Phase 327 fokus pure DateOnly + migration. Bundle bikin scope creep risk delay IT promo batch.
  - **Item defer:** Validator order self-renewal di Edit handler (P03 minor) + Tom Select UX (Razor change beda area).

#### Excel Import Parse Path
- **D-13:** Audit controller ImportTraining dulu, then `DateOnly.FromDateTime(cell.GetDateTime())` cast (Q5 jawaban "Recommended").
  - **Pattern:** ClosedXML `cell.GetDateTime()` return DateTime → wrap `DateOnly.FromDateTime(...)`. Cell empty/null guard tetap.
  - **Plan-phase task:** Read ImportTraining handler (controller location TBD plan-phase grep), identify line parse cell column "ValidUntil", apply cast.
  - **Manual smoke:** 1 row Excel ValidUntil = 2027-03-15 → import sukses → DB simpan date `2027-03-15` (no time component).
  - **Reference views:** `Views/Admin/ImportTraining.cshtml:210, 244` — Excel template column doc "Tanggal berlaku, format YYYY-MM-DD" tetap valid.

#### xUnit Test Library
- **D-14:** Match Phase 325 pattern (xUnit + Assert vanilla, no FluentAssertions) (Q6 jawaban "Recommended").
  - **Rasional:** Konsisten HcPortal.Tests/ existing. Zero new dependency.
  - **Test file:** `HcPortal.Tests/Models/CertificationManagementViewModelTests.cs` (new file) atau `HcPortal.Tests/CertificateStatusTests.cs` — plan-phase decide naming pattern.
  - **Style:** `Assert.Equal(CertificateStatus.Expired, result)`. Boleh combine dengan `[Theory] + [InlineData(...)]` parameterized untuk 5 case + boundary days=30 (inclusive vs exclusive: spec `if (days <= 30) return AkanExpired` → exclusive 31+ = Aktif, inclusive 30 = AkanExpired) + null ValidUntil non-Permanent + Permanent override.
  - **Coverage minimum 7 case:**
    1. `validUntil = today + 100, type = "Annual"` → Aktif
    2. `validUntil = today + 30, type = "Annual"` → AkanExpired (boundary inclusive)
    3. `validUntil = today + 1, type = "Annual"` → AkanExpired
    4. `validUntil = today, type = "Annual"` → AkanExpired (days = 0)
    5. `validUntil = today - 1, type = "Annual"` → Expired
    6. `validUntil = null, type = null` → Expired
    7. `validUntil = today + 100, type = "Permanent"` → Permanent (validUntil ignored)
    8. `validUntil = null, type = "Permanent"` → Permanent

#### JSON API Audit
- **D-15:** Audit endpoint, accept default DateOnly serialization `"yyyy-MM-dd"` (Q7 jawaban "Recommended").
  - **Plan-phase task:** Grep `return Json(...)` atau `JsonResult` di Controllers/ + Services/ yang touch ValidUntil. Candidates: RenewalController, CMPController, CDPController, AssessmentAdminController partial endpoints.
  - **Risk:** Consumer JS yang parse `new Date(jsonString)` lalu format pakai komponen jam → break. `"2027-03-15"` parse valid tapi tz interpret bisa beda.
  - **Mitigasi:** Smoke verify 5 halaman wajib partial rendering — kalau ada tabel partial AJAX return JSON, browser dev tool Network tab cek response payload.
  - **Defer JsonConverter explicit:** Per D-08 spirit (konsisten type end-to-end), tidak spoof format. Kalau ada specific consumer break, fix per case.

### Implementation Order (Plan-phase guidance)

Sequential strict — migration apply WAJIB step 7 sebelum step 8+:

1. **xUnit test bootstrap** — Add `HcPortal.Tests/Models/CertificateStatusTests.cs` dengan 8 test case GREEN dulu pakai DateTime arithmetic existing (baseline). Confirm 8/8 pass.
2. **Entity flip** — `Models/TrainingRecord.cs:43` + `Models/AssessmentSession.cs:65` ValidUntil DateTime? → DateOnly?.
3. **Computed props rewrite** — `Models/TrainingRecord.cs:75-78, 89-92` + `Models/UnifiedTrainingRecord.cs:40` ke DateOnly arithmetic + UtcNow.
4. **ViewModel + rollup flip** — 4 VM + 3 rollup props (D-07 + D-08) DateOnly?. Build pasti error di banyak call site — fix bertahap.
5. **DeriveCertificateStatus refactor** — signature DateOnly? + today UtcNow per spec §7.3. Refactor test ke DateOnly inputs. Confirm 8/8 GREEN.
6. **Controller call site fix** — CMP/CDP/Renewal `var today = ...` rewrite, query expr ValidUntil comparison, RenewalCertificateRow.TanggalExpired assign cast hilang (sudah DateOnly).
7. **ImportTraining audit + Excel parse cast** — D-13. Manual smoke 1 row.
8. **EF migration generate + apply lokal** — D-02 + D-11. `dotnet ef migrations add ChangeValidUntilToDateOnly`. Review file. Pre-check sqlcmd. Snapshot DB. Apply `dotnet ef database update`.
9. **Razor view smoke + DataAnnotations confirm** — D-04. 5 halaman wajib (/Admin/ManageAssessment tab Training, /Admin/RenewalCertificate, /CMP/Records, /CDP/CertificationManagement, Worker dashboard sertifikat) + PDF /CMP/CertificatePdf/{id} render tanggal benar tanpa jam.
10. **JSON API audit + browser smoke partial** — D-15. Network tab cek response payload format.
11. **Manual UAT 7 SC** (per ROADMAP.md:663-672) + commit + IT_NOTIFY.md draft (push approval gate, IT promo batch akhir per spec §11).

### Claude's Discretion

- **Test file naming:** `CertificateStatusTests.cs` vs `CertificationManagementViewModelTests.cs` — pilih yang konsisten dengan Phase 325 convention (kalau ada `FileUploadHelperTests.cs`, mirror per-class pattern).
- **Theory data:** Inline `[InlineData(...)]` vs `[MemberData(...)]` static method — inline cocok untuk 8 case, member data overkill.
- **Razor format string:** Kalau ada view yang display `.ToString("dd MMM yyyy")` tanpa CultureInfo — biarkan apa adanya (DateOnly + IFormatProvider default OK). Tidak perlu sweep.
- **EF migration file edit:** Generated `Up()/Down()` biasanya minimal (`AlterColumn`). Kalau EF generate weird intermediate (drop column + re-add), pakai manual `migrationBuilder.Sql("ALTER COLUMN ... TYPE date")` atau accept generated kalau valid.

### Folded Todos

Tidak ada todo difold. Pending todos zero per `init` snapshot (todo `realtime-assessment.md` deleted on disk uncommitted, unrelated to v19.0).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca file ini sebelum planning/implementing.**

### Spec Utama (sumber decision lock)
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §7 (line 255-361) — Phase 327 full detail: strategy A DateOnly rasional, model changes, viewmodel + status derivation, EF migration, Razor adjustments, testing, migration workflow.
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §11 (line 407-427) — IT promo strategy batch akhir setelah Phase 327 ship (push 325+326+327 sekaligus, IT 1× notify + deploy + `dotnet ef database update`).
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §12 (line 429-437) — Risk register: Razor binder DateOnly, migration data loss, validator P03 false positive.

### Bug Source
- `docs/sertifikat-ecosystem/bug-findings.html` §P04 (line 441-490 area) — P04 timezone drift root cause analysis. DateTime.Now vs UtcNow mix di CMPController (18×) + CDPController (8×). Server tz WIB (+7), boundary 23:30 WIB → UtcNow 16:30 UTC kemarin → pembulatan salah.

### Workflow Wajib
- `docs/DEV_WORKFLOW.md` — Lokal → Dev → Prod promo SOP. Phase 327 commit + push, IT promo batch akhir migration `dotnet ef database update`.
- `docs/SEED_WORKFLOW.md` — Snapshot DB lokal sebelum apply EF migration (D-11 pre-check), restore kalau drama, journal entry SEED_JOURNAL.md.
- `CLAUDE.md` (project root) — Develop Workflow + Seed Data Workflow section.

### Codebase Existing (touch points — confirmed grep)

#### Entity & Computed Props (flip)
- `Models/TrainingRecord.cs:43` — `public DateTime? ValidUntil { get; set; }` → DateOnly?
- `Models/TrainingRecord.cs:75-78` — `IsExpiringSoon` computed: `(ValidUntil.Value - DateTime.UtcNow).Days` → DateOnly arithmetic
- `Models/TrainingRecord.cs:89-92` — `DaysUntilExpiry` computed: sama, rewrite
- `Models/AssessmentSession.cs:65` — `public DateTime? ValidUntil { get; set; }` → DateOnly?

#### ViewModel (flip)
- `Models/CreateTrainingRecordViewModel.cs:51` — VM property flip
- `Models/EditTrainingRecordViewModel.cs:53` — sama
- `Models/CreateManualAssessmentViewModel.cs:34, 97` — 2 occurrence flip
- `Models/CertificationManagementViewModel.cs:38` — SertifikatRow.ValidUntil flip
- `Models/CertificationManagementViewModel.cs:53-63` — DeriveCertificateStatus signature DateOnly? + body refactor DayNumber arithmetic
- `Models/CertificationManagementViewModel.cs:74` — CertificateChainGroup.LatestValidUntil flip
- `Models/CertificationManagementViewModel.cs:108` — CertificateGroup.MinValidUntil flip
- `Models/UnifiedTrainingRecord.cs:26` — ValidUntil flip
- `Models/UnifiedTrainingRecord.cs:40` — IsExpired flip + DateTime.Now → UtcNow alignment

#### Controller Call Sites (audit + fix)
- `Controllers/AdminBaseController.cs:132, 186` — DeriveCertificateStatus call sites (signature compat OK setelah refactor)
- `Controllers/CMPController.cs:626` — Excel export `r.ValidUntil?.ToString("yyyy-MM-dd")` (DateOnly compatible)
- `Controllers/CMPController.cs:2083-2086` — CertificatePdf `assessment.ValidUntil.Value.ToString("dd MMMM yyyy", culture)` (DateOnly compatible)
- `Controllers/CMPController.cs:2597-2622, 2762-2764, 2980-3000` — `var today = ...` + `t.ValidUntil >= today && t.ValidUntil <= thirtyDaysFromNow` queries → rewrite today DateOnly + `TanggalExpired = t.ValidUntil!.Value` cast hilang
- `Controllers/CMPController.cs:4033-4093` — DeriveCertificateStatus call sites
- `Controllers/CDPController.cs:3823-3886` — DeriveCertificateStatus + query expr
- `Controllers/RenewalController.cs:123-149` — DeriveCertificateStatus + query expr
- `Controllers/AssessmentAdminController.cs` — ValidUntil touch (line TBD plan-phase grep)
- `Controllers/TrainingAdminController.cs` — Add/Edit Training POST handler assign ValidUntil entity (post Phase 326 baseline)
- `Controllers/HomeController.cs` — ValidUntil touch (line TBD plan-phase grep)
- `Services/GradingService.cs` — ValidUntil touch (line TBD plan-phase grep, post Phase 324 baseline)
- `Services/WorkerDataService.cs` — ValidUntil touch (line TBD plan-phase grep)

#### Excel Import Path (D-13 audit)
- `Controllers/TrainingAdminController.cs` (atau `Controllers/AdminController.cs`?) — ImportTraining handler. **Plan-phase task:** grep `ImportTraining` action method + ClosedXML cell parse loop untuk "ValidUntil" column.
- `Views/Admin/ImportTraining.cshtml:210, 244` — Excel template doc (read-only)

#### Razor Views (audit display + form binding)
- `Views/Admin/AddTraining.cshtml:197-199` — form input + asp-validation-for ValidUntil
- `Views/Admin/EditTraining.cshtml:182-184` — sama
- `Views/Admin/AddManualAssessment.cshtml:217-222` — sama
- `Views/Admin/EditManualAssessment.cshtml:133-134` — sama
- `Views/Admin/CreateAssessment.cshtml:546-563, 1131, 1168` — form + JS getElementById ValidUntil
- `Views/Admin/EditAssessment.cshtml:458-462` — form + `value="@Model.ValidUntil?.ToString("yyyy-MM-dd")"` (DateOnly compatible)
- `Views/CMP/Certificate.cshtml:253-256` — display `.ToString("dd MMMM yyyy", culture)`
- `Views/CMP/Records.cshtml:186-187` — display `.ToString("dd MMM yyyy", culture)`
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml:71` — display
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — partial
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml:58` — display
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — partial
- `Views/Shared/_CertificateHistoryModalContent.cshtml:50` — display

#### EF Migration
- `Migrations/20260317132516_AddValidUntilToAssessmentSession.cs` — reference: kapan ValidUntil ditambahkan ke AssessmentSession (history context).
- `Migrations/ApplicationDbContextModelSnapshot.cs` — auto-update saat add migration.

#### Test Project (HcPortal.Tests/)
- `HcPortal.Tests/` — Phase 325 bootstrap, xUnit + Assert vanilla pattern. **Plan-phase task:** confirm directory structure + add `Models/CertificateStatusTests.cs` (atau equivalent path).

### Roadmap & State
- `.planning/ROADMAP.md:657-673` — Phase 327 goal + 7 SC + files affected + migration flag ✅
- `.planning/STATE.md` — Last activity Phase 326 SHIPPED LOCAL.
- `.planning/phases/325-security-hardening-p01-p02-p05/325-CONTEXT.md` — Prior phase context: xUnit project bootstrap pattern (D-08).
- `.planning/phases/326-validator-hardening-p03-p06/326-CONTEXT.md` — Prior phase context: Add/Edit Training POST handler baseline post Phase 326, validator P03+P06 sudah merged.

### Memory Snapshot Sesi
- v19.0 strategy: sequential strict 325 → 326 → 327 → 328, IT promo Dev 1× batch akhir setelah Phase 327 ship.
- Phase 325 SHIPPED LOCAL 7069ead2..77a9c375 (5/5 plan + 5/5 SC PASS) — `HcPortal.Tests/` project bootstrap, AddTraining POST handler touched FileUploadHelper.
- Phase 326 SHIPPED LOCAL 718c67b8..f659ff91 (3/3 plan + 6/6 SC PASS) — TrainingAdminController.cs Add/Edit POST handler P03+P06 validators merged. Edit handler L442+ + EditTrainingRecordViewModel.cs extended 3 field (RenewsTrainingId + RenewsSessionId + RenewalSourceTitle).
- Phase 327 plan-phase guidance: 11-step sequential implementation order (D-06 → D-15 cascade dependency).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`HcPortal.Tests/` xUnit project** (Phase 325 bootstrap) — Add 1 test file `Models/CertificateStatusTests.cs` (atau equivalent). Pattern xUnit + Assert vanilla, no FluentAssertions dependency (D-14).
- **`SertifikatRow.DeriveCertificateStatus`** (CertificationManagementViewModel.cs:53-63) — Single source of truth status derivation. Phase 327 refactor signature + body, semua 6+ call site di Controllers/ auto-compat.
- **AdminBaseController helper pattern** (L132, L186) — Shared row build untuk Renewal/CMP. Refactor signature compat lewat sini.
- **ClosedXML library** (existing, dipakai CMPController.cs:626 export) — Cell read API: `cell.GetDateTime()` return DateTime → wrap `DateOnly.FromDateTime(...)` (D-13 import).
- **`[DataType(DataType.Date)]` + `asp-for` TagHelper** — .NET 7+ native support DateOnly form binding. Zero migration cost.

### Established Patterns
- **`(date - DateTime.UtcNow).Days` arithmetic** — pattern existing 3 lokasi (TrainingRecord.cs:77, 91 + CertificationManagementViewModel.cs:59). Rewrite ke `(date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber)` (D-09, D-10).
- **`var today = DateTime.UtcNow.Date`** (CMPController L2597 area) — pattern existing query expr. Rewrite ke `var today = DateOnly.FromDateTime(DateTime.UtcNow)` (D-09).
- **`.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"))`** — display format Razor existing. DateOnly compatible (specifier date d/M/y identik).
- **EF migration `AlterColumn`** — pattern existing Migrations/. Generated `Up()/Down()` minimal untuk type change (D-02).
- **SEED_WORKFLOW snapshot + restore** — pattern existing dari Phase 324 cleanup. Apply sebelum migration apply lokal (D-11).
- **IT_NOTIFY.md inline sqlcmd** — pattern existing Phase 323/324. Capture pre-check + commit hashes + migration flag (D-11, spec §11).

### Integration Points
- **EF Core 8 LINQ translation DateOnly** — `t.ValidUntil >= today && t.ValidUntil <= thirtyDaysFromNow` translate native ke SQL `date` comparison. Zero EF config change.
- **System.Text.Json default DateOnly** — serialize `"yyyy-MM-dd"` string. Consumer JS yang `new Date(jsonString)` parse OK, tapi timezone interpret bisa beda. Mitigasi smoke partial render (D-15).
- **CMPController query expr `t.ValidUntil >= today && t.ValidUntil <= thirtyDaysFromNow`** — `today` + `thirtyDaysFromNow` derive variable type harus DateOnly. Rewrite pattern: `var today = DateOnly.FromDateTime(DateTime.UtcNow); var thirtyDaysFromNow = today.AddDays(30);`.
- **RenewalCertificateRow.TanggalExpired** — assign `TanggalExpired = t.ValidUntil!.Value` (CMP L2611, L2992 area) — setelah ValidUntil DateOnly, target prop TanggalExpired DateOnly (D-08 cascade). Confirm nama exact + propagate ke ViewModel kelas.
- **CertificatePdf QuestPDF rendering** (CMPController.cs:2083-2086) — DateOnly.ToString format spec ID-ID identik output dengan DateTime untuk specifier date. Smoke test PDF byte count + visual.

### Constraint
- **Pertamina server WIB-only** — Today reference UtcNow.Date vs Now.Date boundary 00:00-07:00 WIB beda 1 hari. Workflow jam kerja 07:00-17:00 WIB tidak kena (D-09 acceptable).
- **EF auto truncate datetime2 → date** — komponen jam lossy (D-02). Pre-check confirm zero row punya jam non-zero (D-11). Probability nol untuk PortalHC (form `<input type="date">` midnight default).
- **`HcPortal.csproj` DefaultItemExcludes** — Phase 325 sudah exclude `HcPortal.Tests/`. Pastikan test project tetap exclude dari main build, jalan via `dotnet test`.
- **JSON consumer JS** — risk break kalau ada `new Date(jsonString).getHours()` di JS frontend. Mitigasi: D-15 smoke partial render.
- **No new NuGet package** — D-14 reject FluentAssertions, D-15 reject custom JsonConverter. Zero dependency drift.

</code_context>

<specifics>
## Specific Ideas

- **Spec §7.6 (line 329-348) literal 5 halaman wajib + PDF + Playwright** — implementation order step 9 (Razor smoke).
- **Spec §7.7 step 1-8 migration workflow** — implementation order step 8 (EF migration apply).
- **Bug-findings P04 root cause** (line 441-490 area) — DateTime.Now vs UtcNow mix CMPController 18× + CDPController 8×. Phase 327 fix paling impactful di DeriveCertificateStatus (single source truth). Sites lain (audit/logging/CreatedAt) defer per spec §13.
- **Phase 326 sisa finding** — defer v20.0 backlog (D-12). Tidak bundle ke 327.
- **xUnit 8 test case minimum coverage** — D-14 enumerate explicit 8 case (boundary inclusive, null, Permanent override). Plan-phase task generate test data.
- **D-09 acceptable WIB workflow boundary rationale** — Pertamina jam kerja 07:00-17:00 WIB tidak kena UtcNow boundary 00:00-07:00 WIB. Locked decision.

</specifics>

<deferred>
## Deferred Ideas

- **Phase 326 sisa non-blocking finding** (D-12): validator order self-renewal di Edit handler + Tom Select UX. → v20.0 backlog item.
- **`DateTime.Now` standardize di non-ValidUntil sites** (audit/logging/CreatedAt). → v20.0 backlog per spec §13.
- **Helper `Clock.Today()` centralized + DI swap untuk test mocking** (Q2 option C rejected). → Defer indefinitely, overkill kalau tidak ada strategi clock-mocking lebih luas.
- **Explicit `JsonConverter` untuk DateOnry format spoof backward-compat** (Q7 option B rejected). → Lawan tujuan migrasi, defer. Kalau ada consumer specific break, fix per case.
- **P05 Soft Delete proper** (IsDeleted column + global query filter) → v20.0 backlog per spec §13.
- **P09 DB CHECK constraint Permanent+ValidUntil mutual exclusion** → v20.0 backlog.
- **P11 Renewal dashboard cache performance optimization** → v20.0 backlog.
- **P12 RBAC integration test Playwright/xUnit coverage** → v20.0 backlog.
- **Computed prop `DaysUntilExpiry`/`IsExpiringSoon` di TrainingRecord.cs delete kalau unused** (Q3 option B partial) → audit call site task plan-phase, decide saat itu. Default rewrite (D-10).

### Reviewed Todos (not folded)

Tidak ada todo direview-dan-tidak-difold sesi ini. Pending todos zero.

</deferred>

---

*Phase: 327-timezone-dateonly-refactor-p04*
*Context gathered: 2026-05-28*
*Mode: interactive (9 questions: 4 awal + 2 follow-up VM rollup + 3 audit-miss follow-up = 13 decisions D-01..D-15 + 2 spec defaults)*
