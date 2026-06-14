---
phase: 360-bypass-backend-b
plan: 02
status: complete
completed: 2026-06-10
commits:
  - 0bb66540: "feat(360-02): ekstrak ProtonDeliverableBootstrap helper parametrik unit + guard anti-dobel B-06"
  - 4d9b27df: "feat(360-02): isi 2 titik exempt gate cross-year Origin=Bypass (D-06) — gate 100% tetap (D-05)"
one_liner: "Helper bootstrap deliverable parametrik unit-from-form (PBYP-05) + guard anti-dobel B-06, dan 2 titik exempt gate cross-year Origin=='Bypass' (CreateAssessment + CoachMapping) dengan gate 100% deliverable tetap berlaku (D-05)."
---

# 360-02 Summary — Bootstrap Helper + Gate Exempt

## Accomplishments
- **`Helpers/ProtonDeliverableBootstrap.cs` (BARU)**: `static CreateProgressAsync(context, assignmentId, protonTrackId, coacheeId, resolvedUnit)` — unit EKSPLISIT dari caller (Pitfall 3), filter `.Trim()` 2-sisi identik gate 100%, **B-06 guard anti-dobel** (exclude `ProtonDeliverableId` yang sudah punya progress untuk coachee → warning "skip duplikat" bila semua sudah ada), insert progress `Pending` + initial `DeliverableStatusHistory` ActorId="system" (D-17). SaveChangesAsync internal — caller wajib dalam transaksi.
- **`CoachMappingController.AutoCreateProgressForAssignment`** kini: resolve unit existing (active mapping → fallback User.Unit) + delegasi ke helper. Method scope BERSIH dari query `ProtonDeliverableList` (single source filter). Signature tak berubah — caller existing aman.
- **Exempt (a)** `AssessmentAdminController` gate cross-year: `isBypassAssignment` (AnyAsync `IsActive && Origin=="Bypass"` per uid+track) → `!isRenewal && !isBypassAssignment && !IsPrevYearPassed`. Gate 100% (:1378-1393) TIDAK disentuh (D-05).
- **Exempt (b)** `CoachMappingController` :533: placeholder `= false` diganti predikat aktif `Origin=="Bypass"` untuk `requestedTrack.Id`. **Defense-in-depth** (W-07/I-08): cabang 1 `hasForRequestedTrack` (:517-520, TANPA filter IsActive) sudah men-skip coachee bypass lebih dulu — predikat menjaga semantik D-06b bila cabang 1 kelak di-refactor. Cabang 1 dipertahankan apa adanya.

## Kontrak untuk Plan 03+
```csharp
ProtonDeliverableBootstrap.CreateProgressAsync(ApplicationDbContext, int assignmentId, int protonTrackId, string coacheeId, string resolvedUnit) → Task<List<string>> // warnings
```
Catatan: helper return warnings; "Semua deliverable unit X sudah ter-bootstrap" = bukan error (B-06 by design, kasus CL-C turun).

## Verification
- `dotnet build` 0 error; `dotnet test --filter "Category!=Integration"` **152/152**.
- AC Task 1: 7/7 (helper signature, Trim 2-sisi, existingDeliverableIds guard, StatusHistory, delegasi, method scope bersih). AC Task 2: 5/5 (isBypassAssignment ×2, Origin=="Bypass" 1 per controller, hardcode false = 0, IsEligiblePerUnit utuh :1392).

## Deviations
- Tidak ada. (Anchor :1368 dst bergeser +5 baris pasca-edit — perilaku sesuai plan.)

## Key Files
created:
  - Helpers/ProtonDeliverableBootstrap.cs
modified:
  - Controllers/CoachMappingController.cs (delegasi :1443-1452 + exempt D-06b :530-541)
  - Controllers/AssessmentAdminController.cs (exempt D-06a :1367-1376)

## Next
Plan 03 `ProtonBypassService`: panggil helper dengan `TargetUnit` dari form; D-16b (keep-coach + ganti-unit → update `mapping.AssignmentUnit`) direkonsiliasi di sana (threat T-360-05).
