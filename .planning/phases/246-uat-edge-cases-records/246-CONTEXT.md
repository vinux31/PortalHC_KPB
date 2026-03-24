# Phase 246: UAT Edge Cases & Records - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Verifikasi bahwa sistem menangani kondisi tidak normal (token salah, force close, regenerate token) dengan benar, renewal sertifikat expired berjalan end-to-end, dan worker/HC dapat melihat riwayat assessment lengkap. Ini fase UAT — bukan coding fitur baru, kecuali seed data tambahan.

</domain>

<decisions>
## Implementation Decisions

### Strategi Verifikasi Edge Cases
- **D-01:** Browser UAT langsung tanpa code review ulang — code review sudah dilakukan di Phase 244 (MON-01/MON-02 semua 9 poin OK)
- **D-02:** Perlu seed data tambahan: minimal 1 assessment session dengan `IsTokenRequired = true` untuk test EDGE-01 (token salah) dan EDGE-03 (regenerate token). Data existing semua `IsTokenRequired = false`.

### Scope Renewal Expired E2E
- **D-03:** Full flow test: Home/Index alarm banner → klik link → RenewalCertificate → proses renewal → sertifikat baru terbuat
- **D-04:** Perlu seed sertifikat expired — tidak ada di seed data existing. Seed harus membuat sertifikat dengan `ValidUntil` di masa lalu agar alarm banner muncul.

### Records View & Export
- **D-05:** Verifikasi fitur existing saja — halaman My Records (worker) dan UserAssessmentHistory (HC team view) sudah ada. Cukup verifikasi kolom lengkap + export Excel berfungsi.

### Plan Structure
- **D-06:** 2 plans — Plan 1: Seed data tambahan (assessment IsTokenRequired=true + sertifikat expired). Plan 2: Browser UAT semua 6 requirements (EDGE-01–04, REC-01–02).

### Claude's Discretion
- Detail implementasi seed data (nama assessment, token value, tanggal expired)
- Urutan test scenario dalam browser UAT

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment & Monitoring
- `Controllers/AdminController.cs` — ForceClose, ResetExam, RegenerateToken actions
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — HC monitoring UI with action buttons
- `Data/SeedData.cs` — Existing UAT seed data (lines 407-912)

### Renewal Certificate
- `Controllers/AdminController.cs` — RenewalCertificate action
- `Views/Admin/RenewalCertificate.cshtml` — Renewal UI
- `Views/Home/_CertAlertBanner.cshtml` — Alarm banner partial
- `Controllers/HomeController.cs` — Home/Index with cert alert

### Records
- `Views/Admin/UserAssessmentHistory.cshtml` — HC team view records
- `Models/RecordsWorkerListViewModel.cs` — Worker records view model

### Prior Phase Artifacts
- `.planning/phases/244-uat-monitoring-analytics/244-VERIFICATION.md` — Phase 244 verification (code review OK)
- `.planning/phases/240-alarm-sertifikat-expired/240-CONTEXT.md` — Alarm expired context

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Seed pattern di `Data/SeedData.cs` — extend dengan assessment `IsTokenRequired=true` + sertifikat expired
- `CertNumberHelper.Build()` — generator nomor sertifikat yang sudah ada
- SignalR hub `/hubs/assessment` — sudah verified di Phase 244

### Established Patterns
- UAT seed menggunakan idempotency guard by Title (Phase 241 decision)
- Assessment session copy pattern: setiap session punya PackageQuestion+Option baru (Phase 241)
- TempData guard di StartExam (Phase 244)

### Integration Points
- Home/Index → `_CertAlertBanner.cshtml` → alarm banner trigger
- AssessmentMonitoringDetail → ForceClose/ResetExam/RegenerateToken actions
- StartExam → token validation → error message display

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 246-uat-edge-cases-records*
*Context gathered: 2026-03-24*
