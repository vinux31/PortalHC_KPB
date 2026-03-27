# Phase 263: Fix Database-Stored Upload Paths for Sub-path Deployment Compatibility - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix rendering file upload paths yang disimpan di database (`/uploads/...`) agar kompatibel dengan sub-path deployment `/KPB-PortalHC/`. Path disimpan tanpa PathBase prefix oleh controllers, lalu di-render langsung sebagai `href`/`src` di views — akan 404 saat deploy.

</domain>

<decisions>
## Implementation Decisions

### Fix Strategy
- **D-01:** Fix di render-time only (Opsi A). Path di DB tetap disimpan sebagai `/uploads/...` — tidak diubah format penyimpanan.
- **D-02:** Di Razor views, prefix path dengan `~` agar ASP.NET Core resolve PathBase otomatis. Contoh: `href="~@model.FilePath"`.
- **D-03:** Di JavaScript renders, prefix path dengan `basePath` (global variable dari _Layout.cshtml). Contoh: `basePath + data.evidencePath`.

### Data Lama
- **D-04:** Tidak perlu migrasi data di database. Semua path (lama dan baru) tetap format `/uploads/...` — fix hanya di cara render.

### Scope — 2 View Renders yang Bermasalah
- **D-05:** `Views/Admin/AssessmentMonitoringDetail.cshtml` line 529 — `href="@existingDto.SupportingDocPath"` → tambah `~` prefix
- **D-06:** `Views/ProtonData/Override.cshtml` line 354 — JS render `data.evidencePath` → prefix `basePath`

### Claude's Discretion
- Apakah ada tempat render lain yang terlewat (audit menyeluruh)
- Exact syntax untuk Razor `~` prefix di dynamic path

</decisions>

<specifics>
## Specific Ideas

- Ini lanjutan dari Phase 262 (fix hardcoded URLs di views)
- Bedanya: Phase 262 fix URL di source code, Phase 263 fix URL yang berasal dari database
- User bukan IT — output harus kode yang sudah benar, siap deploy

</specifics>

<canonical_refs>
## Canonical References

### Prior Phase Context
- `.planning/phases/262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility/262-CONTEXT.md` — Keputusan PathBase, basePath/appUrl globals, pattern yang sudah established

### File yang perlu diubah
- `Views/Admin/AssessmentMonitoringDetail.cshtml` line 529 — Razor render SupportingDocPath
- `Views/ProtonData/Override.cshtml` line 354 — JS render evidencePath

### File sumber path (untuk audit)
- `Controllers/AdminController.cs` lines 171, 511, 2612 — upload kkj, cpdp, interviews
- `Controllers/CDPController.cs` line 2182 — upload evidence
- `Controllers/ProtonDataController.cs` lines 1139, 1213 — upload guidance

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `basePath` global variable di `_Layout.cshtml` line 35
- `appUrl()` helper function di `_Layout.cshtml` line 36
- Razor `~/` prefix pattern — sudah dipakai luas untuk static assets

### Established Patterns
- Phase 262: semua hardcoded JS URLs sudah di-wrap `appUrl()` atau `basePath +`
- Razor `~` prefix: `href="~/images/..."`, `src="~/css/..."` — ASP.NET Core auto-resolve PathBase
- `asp-action` tag helper untuk download links (sudah PathBase-aware)

### Integration Points
- `Views/Admin/AssessmentMonitoringDetail.cshtml:529` — direct Razor render dari DB path
- `Views/ProtonData/Override.cshtml:354` — JS string concatenation dari API response
- Tempat lain sudah aman (pakai `asp-action` tag helper)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 263-fix-database-stored-upload-paths-for-sub-path-deployment-compatibility*
*Context gathered: 2026-03-27*
