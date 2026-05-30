---
phase: 326
plan: 02
status: complete
completed: 2026-05-27
build: 0 Error, 23 Warning (pre-existing)
---

# Plan 326-02 — Razor View Tweaks

## Files Modified (2)

1. **`Views/Admin/EditTraining.cshtml`**
   - **Section card "Renewal Source"** L127-156 (~30 baris) — conditional render `@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)` antara card "Data Training" + card "Data Sertifikat"
     - h5 heading "Renewal Source" + icon `bi-arrow-repeat me-2 text-primary`
     - Read-only display `Renewal dari: @(Model.RenewalSourceTitle ?? "(sertifikat sumber tidak ditemukan)")`
     - Button `[Hapus link renewal]` icon `bi-x-circle me-1` + class `btn-outline-danger btn-sm`
     - Inline onclick handler: clear `RenewsTrainingId` + `RenewsSessionId` hidden inputs + hide section
     - Hidden inputs `RenewsTrainingId` + `RenewsSessionId` (raw `<input type="hidden">` with explicit `id` for JS getElementById)
   - **`<span asp-validation-for="ValidUntil">`** L184 dalam card "Data Sertifikat" "Berlaku Sampai" block

2. **`Views/Admin/AddTraining.cshtml`**
   - **`<span asp-validation-for="ValidUntil">`** L199 dalam "Berlaku Sampai (shared)" block

## Markup Delta

- EditTraining.cshtml: +30 baris section card + 1 baris span = **+31 baris**
- AddTraining.cshtml: +1 baris span
- **Total UI delta: 32 baris** (UI-SPEC budget ≤ 35 ✓)

## Copywriting (UI-SPEC L177-188 Verbatim)

- `Renewal Source` (h5 heading)
- `Renewal dari` (label)
- `(sertifikat sumber tidak ditemukan)` (fallback)
- `Hapus link renewal` (button)

## Cross-Views Validation

- `asp-validation-for="ValidUntil"` cross-Views grep: **exactly 2 hits** (Edit L184 + Add L199)
- Pre-Phase-326 hit count: 0 (PATTERNS critical gap #3 closed)

## Acceptance Criteria

- [x] `id="renewalSourceSection"` 1 hit (conditional section)
- [x] `Renewal Source` 1 hit (h5)
- [x] `Renewal dari` 1 hit (label)
- [x] `Hapus link renewal` 1 hit (button)
- [x] `(sertifikat sumber tidak ditemukan)` 1 hit (fallback)
- [x] `bi-arrow-repeat me-2 text-primary` 1 hit (icon header)
- [x] `bi-x-circle me-1` 1 hit (icon button)
- [x] `@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)` 1 hit
- [x] `name="RenewsTrainingId"` + `name="RenewsSessionId"` hidden inputs present
- [x] `btn-outline-danger btn-sm` 1 hit
- [x] `form-control-plaintext` 1 hit
- [x] Cross-Views `asp-validation-for="ValidUntil"` exactly 2 hits
- [x] `dotnet build` 0 Error
- [x] Existing card "Data Training" + "Data Sertifikat" markup NOT modified
- [x] No new CSS, no new JS file, no new dependency

## Decision Compliance

- D-07 (read-only display + clear button) ✓ section + button + inline JS clear
- UI-SPEC L115-146 markup verbatim ✓ byte-for-byte
- UI-SPEC L177-188 copywriting ✓ 4 string locked

## Next

Plan 326-03 — UAT manual 6 SC via browser lokal + commit + push approval gate.
