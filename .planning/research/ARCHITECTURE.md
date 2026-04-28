# Architecture Research

**Domain:** v15.0 Audit Findings 27 April 2026 — integrasi 11 fix ke arsitektur ASP.NET Core 8 MVC PortalHC_KPB
**Researched:** 2026-04-28
**Confidence:** HIGH (semua integration point diverifikasi via pembacaan kode aktual)

## Key Findings (Executive)

1. **Tidak ada satu pun temuan yang justified service extraction.** Semua patch tetap di location existing (action method, view, atau helper inline). YAGNI menang clear.
2. **T2 butuh server-side change selain view:** `AssessmentAdminController.CreateQuestion` baris **4681** dan `EditQuestion` baris **4822** memaksa `scoreValue = 10` untuk MC/MA via `if (questionType != "Essay") scoreValue = 10;` (komentar T-298-03). Fix view-only TIDAK CUKUP.
3. **Cross-cutting impact = ZERO.** Tidak ada perubahan pada `AdminBaseController`, `IAuthService`, `IWorkerDataService`, `GradingService`, `PaginationHelper`, `_Layout.cshtml`, atau `wwwroot/js/site.js`. Audit fix isolated.
4. **T9 idempotency partial sudah ada:** Baris 2778–2784 sudah pakai `ExecuteUpdateAsync` dengan WHERE clause `Status == "Menunggu Penilaian"` sebagai replay guard (komentar T-298-16). Fix hanya butuh ganti pesan baris 2713 menjadi success-message ramah.
5. **Pattern existing untuk T11 sudah mature:** `ModelState.Remove()` dipakai 5+ kali di POST `CreateAssessment` (baris 742, 756, 821, 835, 870). T11 hanya menambah satu entry mengikuti pola yang sama.

## Integration Points per Temuan

| # | File | Method/Lokasi | Layer | Service new? |
|---|---|---|---|---|
| T1 | `Views/Account/Login.cshtml` | baris 177–183 + inline script di akhir view | View only | No |
| T2 | `Views/Admin/ManagePackageQuestions.cshtml` + `Controllers/AssessmentAdminController.cs` | view 188/299/300/311 + controller **4681** (CreateQuestion), **4822** (EditQuestion) | View+Controller | No |
| T3 | `Controllers/AssessmentAdminController.cs` | `ManageAssessment` (57–188) inline patch + opsional EF migration untuk index | Controller+DB | No (extract ditolak) |
| T4 | `Views/Admin/CreateAssessment.cshtml` | extract `renderSelectedParticipants(targetEl, checkboxes)` dari `populateSummary` (1062–1095) inline | View only | No |
| T5 | `Views/Admin/CreateAssessment.cshtml` | label baris 362, 383, 404, 412, 425, 432 | View only | No |
| T6 | `Views/Admin/CreateAssessment.cshtml` | baris 1177 + opsional 1117–1130 (PrePost summary) | View only | No |
| T7 | 4 view files (`ManagePackageQuestions.cshtml`, `_PreviewQuestion.cshtml`, `StartExam.cshtml`, `ExamSummary.cshtml`) | label only — string internal `"MultipleChoice"`/`"MultipleAnswer"` **tidak diubah** | View only | No |
| T9 | `Controllers/AssessmentAdminController.cs` (2710–2827) + view tombol "Create Sertifikasi" di CDP | baris 2713 message + UI button hide condition | Controller+View | No (`EssayGradingService` ditolak) |
| T10 | `Controllers/CMPController.cs` (1771–1838) + `Views/CMP/Certificate.cshtml` | try-catch per-action (mirror `CertificatePdf` pattern baris 2078–2083) | Controller+View | No (global filter ditolak) |
| T11 | `Views/Admin/CreateAssessment.cshtml` (1790–1807) + `Controllers/AssessmentAdminController.cs` (~778) | JS handler set value + `ModelState.Remove("Status")` jika `isPrePostMode` | View+Controller | No (FluentValidation ditolak) |

## Build Order (Dependency-Aware, 6 Waves)

1. **Wave 1 (parallel-safe, label only):** T1, T5, T6, T7 — minimal risk
2. **Wave 2 (view+controller logic, file conflict di `CreateAssessment.cshtml` — wajib serialize after Wave 1):** T2, T4, T11
3. **Wave 3 (defensive, post-deploy log analysis):** T10
4. **Wave 4 (state machine):** T9
5. **Wave 5 (perf, last karena perlu measurement):** T3
6. **Wave 6 (deferred):** T8

**T9 vs T3 ordering:** T9 tidak functionally depend pada T3 (independen secara semantik); plan rekomendasi T3 di akhir adalah benar untuk **risk ordering**, bukan dependency.

**File conflict warning:** T4, T5, T6, T11 semua menyentuh `CreateAssessment.cshtml`. Wajib serialize dalam 1 worktree branch dengan careful merge order.

## Components Baru — DITOLAK (dengan justifikasi)

| Component | Untuk Temuan | Verdict | Alasan |
|-----------|--------------|---------|--------|
| `AssessmentManagementQueryService` | T3 | ❌ Tolak | Single caller, ViewBag-coupled. Inline patch ke action method cukup. |
| `EssayGradingService` | T9 | ❌ Tolak | Fix 1 baris message + 1 UI condition. Mirror pattern existing `GradingService` tidak justified untuk delta kecil. |
| Global Exception Filter | T10 | ❌ Tolak | Pola try-catch per-action sudah established di `CertificatePdf`. Konsistensi > new abstraction. |
| FluentValidation / IValidatableObject | T11 | ❌ Tolak | `ModelState.Remove` pattern sudah mature (5+ usage). Add 1 entry konsisten. |
| Site-wide JS file | T1, T4 | ❌ Tolak | Single-page/single-view scope. Inline `<script>` sesuai pola existing (Login, CreateAssessment, ManagePackageQuestions). |
| Helper `GetQuestionTypeDisplayName` di base controller | T7 | ❌ Tolak | 3 mapping × 4 view = 12 string changes. Manageable inline. |
| Timezone-aware service | T5, T6 | ❌ Tolak | Single timezone, label-only. Multi-tz out-of-scope. |

## Pattern Existing yang Diikuti

- **AJAX Dual-Response:** `AdminBaseController.IsAjaxRequest()` — T9 sudah pakai pattern ini di FinalizeEssayGrading
- **Defensive ModelState.Remove:** POST `CreateAssessment` baris 742/756/821/835/870 → T11 menambah 1 entry
- **Try-Catch per Sensitive Action:** `CertificatePdf` baris 2078–2083 → T10 mirror persis
- **ExecuteUpdateAsync Replay Guard:** `FinalizeEssayGrading` baris 2778–2784 → T9 manfaatkan existing guard, hanya ubah message
- **AsNoTracking Read-Only:** `AdminBaseController.BuildRenewalRowsAsync` → T3 apply ke ManageAssessment
- **Inline Script di View:** existing di CreateAssessment, ManagePackageQuestions, Login → T1, T4 ikut

## Phase Structure Implications

**Phase struktur disarankan:** 1 phase per Wave (5 phase) atau lebih granular per temuan (10 phase). Karena kebanyakan T pure-view dan low-risk, batching Wave 1 menjadi 1 phase (4 temuan label) akan efisien.

**Phase yang butuh deeper research:**
- T3 (perlu measurement baseline dengan SQL trace)
- T9 (perlu integration test setup atau session reproduksi log)
- T10 (perlu akses log produksi setelah deploy)

**Phase tanpa research extra:**
- T1, T2, T4, T5, T6, T7, T11 — straightforward implementation per plan

## Open Questions

1. **T7 final label naming** — perlu konfirmasi auditor sebelum touch 4 view files (user sudah pilih: rename label UI saja, label final sebelum commit).
2. **T8 scope** — Jalur A (label fix) vs Jalur B (field baru). Jika Jalur B, defer ke v16.0 karena bertentangan dengan goal "tanpa migrasi DB".
3. **T3 EF migration index** — perlu cek migrasi terbaru apakah index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` dan `IX_LinkedGroupId` sudah ada.
4. **T9 UI tombol "Create Sertifikasi"** — perlu locate exact view/partial yang merender tombol untuk hide-condition `Status == "Completed" && NomorSertifikat != null`.
5. **T10 root cause aktual** — defensive patch dulu, monitor `_logger.LogError` di production untuk diagnose root cause definitif.

## Sources

- `Controllers/AssessmentAdminController.cs` — verified ManageAssessment (57–188), CreateAssessment (730–940), CreateQuestion (4681), EditQuestion (4822), FinalizeEssayGrading (2710–2827)
- `Controllers/CMPController.cs` — verified Certificate (1771–1811), ResolveCategorySignatory (1813–1838), CertificatePdf pattern (1841–2084 with try-catch baris 2078–2083)
- `Controllers/AdminBaseController.cs` — verified `IsAjaxRequest()` pattern, `BuildRenewalRowsAsync` AsNoTracking pattern
- `Views/Admin/ManagePackageQuestions.cshtml` — verified score field 188, JS 291–312
- `Views/Admin/CreateAssessment.cshtml` — verified Step 2 user list 280–321, Step 3 time fields 357–434, Step 3 Status field 460–469, Step 4 summary 1155–1190, JS validation 925–1031, type select handler 1790–1807
- `Views/Account/Login.cshtml` — verified password input 177–183
- `Views/Admin/_PreviewQuestion.cshtml` — verified MC/MA/Essay rendering
- `Models/AssessmentSession.cs` — verified Status field type, no [Required] attribute

---
*Architecture research for: v15.0 Audit Findings 27 April 2026*
*Researched: 2026-04-28*
