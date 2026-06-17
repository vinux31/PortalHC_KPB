---
phase: 392-perbaikan-createworker-audit-field
plan: 01
status: complete
requirements: [WRKR-01, WRKR-02]
migration: false
date: 2026-06-17
---

# Plan 392-01 Summary — CreateWorker View Fix

**Status:** ✅ Complete · 2/2 tasks · **VIEW-ONLY** (`Views/Admin/CreateWorker.cshtml`) · 0 migration · 0-diff controller/model · branch `main` · commit `0d788e8a`

## What changed (1 file)

| Decision | Change |
|----------|--------|
| D-01 | Hapus `readonly="@(isAdMode ? ...)"` + `bg-light` ternary **unconditional** dari FullName & Email → editable di SEMUA env (form CreateWorker bisa dipakai di AD mode) |
| D-02 | Reword AD info-text (×2): "Dikelola oleh AD — akan disinkronkan saat login" → "Isi sesuai akun AD Pertamina pekerja. Nama & Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali." |
| D-03 | `type="email"` eksplisit di input Email |
| D-04 | `<span asp-validation-for>` inline untuk Position/Directorate/Section/Unit + Role (6 existing tidak diduplikasi) |
| D-05 | Aktifkan validasi client-side: `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }` + relokasi `shared-cascade.js`/`initSectionUnitCascade`/`initFormLoading` ke dalam section (jQuery footer load dulu) |

## Verification

- `dotnet build HcPortal.csproj` → **0 error** (Razor compile OK).
- Grep acceptance:
  - readonly/bg-light = **0**; `type="email"` = 1; `asp-validation-for` = **11** (6 existing + 4 org + Role); "Isi sesuai akun AD" = 2; "Dikelola oleh AD" = 0.
  - `@section Scripts` = 1; `_ValidationScriptsPartial` = 1 (L198, SEBELUM shared-cascade L199); `initSectionUnitCascade` = 1 (dalam section); `currentSection` interpolation utuh.
- `git diff --name-only` → **hanya** `Views/Admin/CreateWorker.cshtml` (WorkerController.cs + ManageUserViewModel.cs + EditWorker.cshtml UNCHANGED).

## key-files
- created: (none)
- modified: `Views/Admin/CreateWorker.cshtml`

## Next
- Plan 392-02: Playwright runtime verify (editable + cascade + live validation + create success, AD-off) + static grep guard + seed cleanup. **WAJIB** (Razor + cascade JS dinamis — lesson Phase 354; relokasi @section Scripts perlu runtime confirm).

## Self-Check: PASSED
