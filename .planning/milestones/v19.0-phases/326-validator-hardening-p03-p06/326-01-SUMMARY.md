---
phase: 326
plan: 01
status: complete
completed: 2026-05-27
build: 0 Error, 23 Warning (pre-existing)
---

# Plan 326-01 — Backend Validator + VM Extend

## Files Modified (2)

1. **`Models/EditTrainingRecordViewModel.cs`** — 3 field nullable tambahan (L67-69)
   - `public int? RenewsTrainingId { get; set; }`
   - `public int? RenewsSessionId { get; set; }`
   - `public string? RenewalSourceTitle { get; set; }`

2. **`Controllers/TrainingAdminController.cs`** — 3 location modified:
   - **AddTraining POST (Task 2)**: 3 new validators inserted after srcAlreadyRenewed L254
     - P03 TR branch DAG check `src.Tanggal >= model.Tanggal` (key `""` summary)
     - P03 AS branch DAG check `srcAs.Schedule >= model.Tanggal` (key `""` summary)
     - P06 Permanent+ValidUntil reject (key `"ValidUntil"` field)
   - **EditTraining GET (Task 3 Mod 1)**: VM mapping 2 FK field + RenewalSourceTitle lookup block (L449-461)
   - **EditTraining POST (Task 3 Mod 2)**: 4 new validators before ModelState.IsValid gate (L486-500)
     - P03 TR branch + P03 AS branch (mirror Add)
     - P03 Self-renewal guard `model.RenewsTrainingId.Value == model.Id`
     - P06 Permanent+ValidUntil reject
   - **EditTraining POST (Task 3 Mod 3)**: 2 line entity FK assignment (L541-542)

## Validators Added (Total 7)

| Endpoint | Validator | Key | Line |
|----------|-----------|-----|------|
| AddTraining POST | P03 TR DAG | `""` | 260 |
| AddTraining POST | P03 AS DAG | `""` | 266 |
| AddTraining POST | P06 Permanent | `"ValidUntil"` | 270 |
| EditTraining POST | P03 TR DAG | `""` | 487 |
| EditTraining POST | P03 AS DAG | `""` | 493 |
| EditTraining POST | P03 Self-renewal | `""` | 497 |
| EditTraining POST | P06 Permanent | `"ValidUntil"` | 500 |

## Error Strings (D-03 Verbatim)

- `"Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew."` × 4 (TR+AS branch di Add + TR+AS branch di Edit)
- `"Sertifikat tidak boleh renewal dirinya sendiri."` × 1 (Edit only)
- `"Sertifikat Permanent tidak boleh punya tanggal expired."` × 2 (Add + Edit)
- `"(sertifikat sumber tidak ditemukan)"` × 2 (GET RenewalSourceTitle fallback TR + AS)

## Acceptance Criteria

- [x] VM 3 field tambahan grep verified
- [x] All 4 grep counts exact match (4 / 1 / 2 / 2 hits per error string)
- [x] `record.RenewsTrainingId = model.RenewsTrainingId` grep 1 hit (FK persist)
- [x] `record.RenewsSessionId = model.RenewsSessionId` grep 1 hit
- [x] `model.RenewsTrainingId.Value == model.Id` grep 1 hit (self-renewal)
- [x] `dotnet build HcPortal.csproj` returns 0 Error (23 Warning pre-existing — pre-Phase-326)
- [x] Only 2 file modified (matches frontmatter `files_modified`)

## Decision Compliance

- D-01 (P03 pattern Add) ✓ verbatim
- D-02 (P06 before IsValid) ✓
- D-03 (3 error string verbatim) ✓ byte-for-byte
- D-05 (key conventions `""` summary vs `"ValidUntil"` field) ✓
- D-06 (Extend Edit VM) ✓
- D-07 (Edit GET populate RenewalSourceTitle + Edit POST FK persist) ✓
- D-08 (Symmetric kedua FK TR+AS) ✓ AS.Schedule confirmed
- D-10 (Strict `>` via `>=` reject) ✓ same-day rejected

## Next

Plan 326-02 — Razor view tweaks:
- `Views/Admin/EditTraining.cshtml` section card "Renewal Source" + clear button
- `Views/Admin/EditTraining.cshtml` + `Views/Admin/AddTraining.cshtml` ValidUntil span first introduction

Plan 326-02 depends_on 326-01 (uses 3 new VM field).
