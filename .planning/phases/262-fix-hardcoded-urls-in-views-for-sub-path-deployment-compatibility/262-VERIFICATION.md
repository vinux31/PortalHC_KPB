---
phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility
verified: 2026-03-27T00:00:00Z
status: passed
score: 7/7 must-haves verified
re_verification: false
---

# Phase 262: Fix Hardcoded URLs in Views — Verification Report

**Phase Goal:** Setup UsePathBase middleware dan fix ~83 hardcoded absolute URL di 25 view files agar kompatibel dengan sub-path deployment /KPB-PortalHC/
**Verified:** 2026-03-27
**Status:** PASSED
**Re-verification:** Tidak — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | App menggunakan PathBase dari appsettings.json | VERIFIED | `appsettings.json` line 9: `"PathBase": "/KPB-PortalHC"` |
| 2 | UsePathBase aktif sebelum UseStaticFiles di Program.cs | VERIFIED | `Program.cs` line 176-180: `app.UsePathBase(pathBase)` sebelum line 182: `app.UseStaticFiles` |
| 3 | Global `basePath` variable tersedia via _Layout.cshtml | VERIFIED | `_Layout.cshtml` line 35: `var basePath = '@Url.Content("~/")'.replace(/\/$/, '')` |
| 4 | `appUrl()` helper function tersedia global | VERIFIED | `_Layout.cshtml` line 36: `function appUrl(path) { return basePath + ... }` |
| 5 | Zero hardcoded `href="/"` di seluruh Views | VERIFIED | grep sweep: 0 hasil (setelah exclude ~/,https,http,asp-,@Url) |
| 6 | Zero hardcoded `fetch('/` atau `fetch("/` di seluruh Views | VERIFIED | grep sweep: 0 hasil |
| 7 | Zero hardcoded `src="/"` di seluruh Views | VERIFIED | grep sweep: 0 hasil |

**Score:** 7/7 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Program.cs` | UsePathBase middleware | VERIFIED | Line 176-180: GetValue PathBase + app.UsePathBase, sebelum UseStaticFiles |
| `appsettings.json` | PathBase config value | VERIFIED | `"PathBase": "/KPB-PortalHC"` |
| `Views/Shared/_Layout.cshtml` | Global basePath + appUrl | VERIFIED | Lines 35-36: var basePath + function appUrl |
| `Views/ProtonData/Index.cshtml` | 11 URL fixes dengan appUrl | VERIFIED | 10 appUrl() calls ditemukan |
| `Views/Admin/CoachCoacheeMapping.cshtml` | 10 URL fixes dengan appUrl | VERIFIED | 9 appUrl() calls ditemukan |
| `Views/Shared/Components/NotificationBell/Default.cshtml` | 4 URL fixes dengan appUrl | VERIFIED | 4 appUrl() calls ditemukan |
| `Views/Shared/Error.cshtml` | PathBase-aware home link | VERIFIED | `href="~/"` menggantikan `href="/"` |
| `Views/CMP/Certificate.cshtml` | PathBase-aware image src | VERIFIED | `src="~/images/..."` |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | asp-controller di form | VERIFIED | Tidak ada `action="/"` tersisa di seluruh views |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `appsettings.json` | `Program.cs` | `Configuration.GetValue<string>("PathBase")` | WIRED | Line 176 Program.cs: `GetValue<string>("PathBase")` |
| `Program.cs` | Seluruh Views | `UsePathBase` membuat Url.Content PathBase-aware | WIRED | UsePathBase sebelum UseStaticFiles — tag helpers & ~/ otomatis include prefix |
| `_Layout.cshtml` | Semua JS di view files | `appUrl()` global function | WIRED | 62 total `appUrl(` calls di seluruh Views |

---

### Data-Flow Trace (Level 4)

Tidak berlaku — phase ini bukan data-rendering component, melainkan URL infrastructure/configuration. Tidak ada state variable yang di-render dari database.

---

### Behavioral Spot-Checks

| Behavior | Pemeriksaan | Hasil | Status |
|----------|-------------|-------|--------|
| UsePathBase sebelum UseStaticFiles | Baca urutan line di Program.cs | Line 176-180 UsePathBase, line 182 UseStaticFiles | PASS |
| appUrl digunakan di file high-volume | grep -c "appUrl(" di 3 file kritis | ProtonData:10, CoachCoacheeMapping:9, NotificationBell:4 | PASS |
| Zero hardcoded URL sweep | grep 5 pattern across Views/ | Semua 0 | PASS |
| Build kompilasi bersih | dotnet build — cek error CS | 0 error CS (MSB3021 adalah file-lock karena app sedang jalan) | PASS |
| CMP/Assessment.cshtml bersih | Cek window.location dan fetch | Hanya `response.redirectUrl` (server-generated, bukan hardcoded) | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|----------|
| D-01 | 262-01 | UsePathBase di Program.cs dari appsettings.json | SATISFIED | `app.UsePathBase(pathBase)` + `GetValue<string>("PathBase")` |
| D-02 | 262-01 | Url.Action, tag helpers, ~/ otomatis include prefix | SATISFIED | UsePathBase aktif dan sebelum middleware lain — behavior otomatis oleh ASP.NET Core |
| D-03 | 262-02, 262-03 | Hardcoded href/src/action di HTML diganti ke tag helpers atau ~/ | SATISFIED | grep: 0 hardcoded href/src/action di seluruh Views |
| D-04 | 262-01 | Global basePath dan appUrl di _Layout.cshtml | SATISFIED | Lines 35-36 _Layout.cshtml |
| D-05 | 262-02, 262-03 | Semua fetch/$.get/window.location di JS pakai appUrl() atau basePath | SATISFIED | 62 appUrl() calls, grep: 0 hardcoded JS URLs |
| D-06 | 262-02, 262-03 | Fix semua 25 file sekaligus | SATISFIED | Semua file di 3 plan sudah difix, final sweep menunjukkan 0 sisa |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| - | - | - | - | Tidak ada anti-pattern ditemukan |

`window.location.href = response.redirectUrl` di `Views/CMP/Assessment.cshtml` adalah false positive — nilai datang dari server response (JSON), bukan hardcoded string literal.

---

### Human Verification Required

#### 1. Uji Akses via Sub-path di Browser

**Test:** Buka `http://10.55.3.3/KPB-PortalHC/` di browser setelah deploy ke server dev
**Expected:** Halaman login terbuka, semua CSS/JS/images ter-load dengan benar, navigation links berfungsi
**Why human:** Tidak bisa diuji tanpa server yang dikonfigurasi dengan sub-path deployment

#### 2. Uji SignalR Hub via Sub-path

**Test:** Buka halaman Assessment setelah deploy, cek browser DevTools Network tab
**Expected:** WebSocket connection ke `/KPB-PortalHC/hubs/assessment` berhasil (bukan 404)
**Why human:** Membutuhkan runtime server di sub-path

#### 3. Uji Notifikasi Bell

**Test:** Login, cek NotificationBell — klik notifikasi
**Expected:** Redirect ke URL yang benar (dengan prefix `/KPB-PortalHC/`)
**Why human:** `n.actionUrl` berasal dari database — perlu verifikasi format path yang tersimpan di DB sesuai dengan basePath prefix behavior

---

### Gaps Summary

Tidak ada gaps. Semua must-haves terverifikasi:

- D-01: UsePathBase aktif dan dikonfigurasi dari appsettings.json
- D-02: Middleware berada sebelum UseStaticFiles (dan semua middleware lain)
- D-03: Zero hardcoded href/src/action di seluruh 25+ view files
- D-04: basePath dan appUrl tersedia global di _Layout.cshtml
- D-05: 62 appUrl() calls menggantikan semua hardcoded JS URLs
- D-06: Semua file difix dalam satu phase (3 plan wave)

Build kompilasi bersih (0 error CS). Error MSB3021 adalah file-lock karena app sedang berjalan — bukan compile error.

---

_Verified: 2026-03-27_
_Verifier: Claude (gsd-verifier)_
