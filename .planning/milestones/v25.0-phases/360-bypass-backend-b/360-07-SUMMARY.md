---
phase: 360-bypass-backend-b
plan: 07
subsystem: proton-bypass
tags: [proton, bypass, endpoint, controller, authorize, antiforgery]
requires: [360-05, 360-06]
provides:
  - "6 endpoint bypass /ProtonData: BypassList, BypassPendingList, BypassDetail (GET) + BypassSave, BypassConfirm, BypassCancelPending (POST)"
  - "DTO BypassSaveRequest + BypassPendingActionRequest"
affects: [360-08, 361]
tech-stack:
  added: []
  patterns:
    - "POST mutator: [HttpPost]+[ValidateAntiForgeryToken]+[FromBody]+GetUserAsync/Challenge+Json (pola OverrideSave)"
key-files:
  created:
    - HcPortal.Tests/ProtonBypassEndpointTests.cs
  modified:
    - Controllers/ProtonDataController.cs
key-decisions:
  - "eligibleModes B-03: CL-A ⟺ sourceComplete && sourceHasFinal; CL-B(a)/(b) ⟺ !sourceHasFinal; CL-C selalu"
  - "D-02 (W-12): TempData[\"Warning\"]=result.Message saat ShowAttachPackageReminder + flag showAttachPackageReminder diteruskan di Json"
  - "BypassList progress X/Y = count progress Approved / total progress per assignment (bukan per deliverable unit mapping)"
requirements-completed: [PBYP-07]
duration: 6 min
completed: 2026-06-10
---

# Phase 360 Plan 07: 6 Endpoint Bypass ProtonDataController Summary

Pintu tunggal fitur bypass terpasang: 3 GET feed UI Phase 361 (tabel worker, panel pending, state wizard dengan eligibleModes B-03) + 3 POST mutator CSRF-guarded yang delegasi penuh ke ProtonBypassService + audit per aksi — 8/8 reflection test + 203/203 full suite hijau.

## Kontrak endpoint (konsumsi UI Phase 361)

| Endpoint | Verb | Shape |
|----------|------|-------|
| `BypassList(bagian?, unit?, trackId?)` | GET | `[{ coacheeId, nama, trackId, trackAktif, progressApproved, progressTotal, finalAda }]` |
| `BypassPendingList()` | GET | `[{ id, coacheeId, nama, sourceTrack, targetTrack, targetUnit, status, hasilExam, createdAt }]` — Status Menunggu/Siap, tanpa field mode (W-05) |
| `BypassDetail(coacheeId)` | GET | `{ success, sourceTrackId, sourceTahun, sourceComplete, sourceHasFinal, eligibleModes[] }` |
| `BypassSave` | POST | body `BypassSaveRequest` → `{ success, message, pendingId, showAttachPackageReminder }` |
| `BypassConfirm` / `BypassCancelPending` | POST | body `{ pendingId }` → `{ success, message }` |

**eligibleModes (B-03):** CL-A hanya bila `sourceComplete && sourceHasFinal` (worker 100% deliverable TANPA penanda → diarahkan CL-B(a)); CL-B(a)/(b) hanya bila `!sourceHasFinal` (D-D); CL-C selalu.

**D-02 (W-12):** `BypassSave` CL-B(b) set `TempData["Warning"]` DAN meneruskan flag `showAttachPackageReminder` di Json — UI Phase 361 boleh pakai keduanya.

## Keamanan

- Class `[Authorize(Roles="Admin,HC")]` (:79 existing) men-gate 6 endpoint (T-360-22).
- 3 POST `[ValidateAntiForgeryToken]` (T-360-23) — total atribut di file naik 10→13.
- Validasi server-side V5 di BypassSave (coacheeId, alasan, mode whitelist) sebelum delegasi (T-360-24).
- No `ex.Message` di region bypass (D6, T-360-25); audit `ProtonBypassSave`/`Confirm`/`Cancel` per mutator (T-360-26).
- Reflection test mengunci: Authorize class-level + HttpPost+AntiForgery 3 mutator + GET tanpa antiforgery + count 3+3.

## Commits

| Task | Commit | Isi |
|------|--------|-----|
| 1+2 | 5dbef601 | DTO + inject + 3 GET + 3 POST |
| 3 | 98d474cf | ProtonBypassEndpointTests 8 test |

## Deviations from Plan

None - plan executed exactly as written. (Task 1 dan 2 di-commit jadi satu commit karena file sama dan saling terkait; acceptance criteria keduanya diverifikasi terpisah.)

## Verification

- `dotnet build` 0 error; reflection test **8/8**; full suite **203/203**.
- Grep: 6 endpoint signature; AntiForgery count 13 (naik 3); delegasi 3; audit naik 3 (9→12); TempData Warning :1629; Tab1 Override/OverrideSave tak berubah.

## Self-Check: PASSED

## Next

Ready for 360-08 (verifikasi E2E: integration test gate exempt Origin="Bypass" + UAT manusia — checkpoint plan).
