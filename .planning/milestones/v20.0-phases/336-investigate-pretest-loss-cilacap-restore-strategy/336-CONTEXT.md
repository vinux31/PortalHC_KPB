# Phase 336: Investigate PreTest Loss Cilacap + Restore Strategy Decision — Context

**Gathered:** 2026-05-30
**Status:** Ready for research/planning
**Milestone:** v20.0
**Source todo:** `.planning/todos/pending/002-restore-pretest-ojt-gast-cilacap.md`
**Incident note:** `.planning/notes/2026-05-29-pretest-ojt-gast-cilacap-lost.md`
**Phase type:** Investigation-only (NO code change)
**Effort:** S (~1-2 hari)

---

## Goal

Identifikasi root cause loss PreTest OJT GAST Cilacap (Mar 30, 2026 → hilang dari Dev DB 10.55.3.3 antara Mar–May 2026), decide restore strategy A/B/C berdasarkan root cause, dan tulis spec naming convention "{Pre|Post} Test {Track} {Lokasi}".

Output: 3 dokumen deliverable (no code change, no migration, no model touch).

---

## Decisions Locked

| # | Decision | Pilihan | Reason |
|---|----------|---------|--------|
| D-01 | **Investigation scope** | **Narrow** — PreTest OJT GAST Cilacap saja (1 dataset) | Scope clear, hindari creep. Sweep wider = phase tersendiri kalau ketemu pattern broader. |
| D-02 | **Restore strategy decision gating** | **Defer ke output Phase 336** | Pilih A/B/C BERDASARKAN root cause (e.g., migration drop column → forced B; seed reset → A feasible; schema-preserving change → C dgn Gap #5 enabler). Scientific, hindari salah pilih sebelum tau penyebab. |
| D-03 | **Phase 336 deliverable scope** | **Decision doc only** | ROOT_CAUSE.md + RESTORE-DECISION.md + NAMING-CONVENTION-SPEC.md. NO executable SQL/code. Phase 338 W4 generate executable saat eksekusi. |
| D-04 | **Investigation tooling** | **Git log + Migrations file read** (no DB introspection) | Paling cepat, sufficient untuk root cause migration/seed. DB introspection skip — kalau perlu, eskalasi later (akses Dev butuh IT). |
| D-05 | **Naming convention spec output** | **Spec only di Phase 336** (BUKAN backfill identification) | Spec text dokumen. Enforcement code + existing record audit = Phase 338 W5 scope. |
| D-06 | **Guardrail backup tool (REST-05)** | **DEFER ke Phase 338 W5 discuss** | Tool decision (SQL Server `.bak` vs Litestream vs snapshot) butuh IT coordination — defer. |

---

## Scope IN

- Read-only analysis `Migrations/*.cs` antara `20260330` – `20260520` (13 candidate migrations identified)
- Read-only analysis `Models/AssessmentSession.cs` schema evolution via `git log -p` window
- Read-only analysis `Data/SeedData.cs` evolution untuk detect reset pattern
- Decision tree restore strategy A/B/C dengan branches per root cause type
- Naming convention spec text (format definition + rationale + examples + edge cases)
- Cross-link incident note + Excel backup `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`

## Scope OUT (defer Phase 338 atau later)

- ❌ NO code change (no controller, no model, no view, no migration)
- ❌ NO DB introspection / sqlcmd query Dev (Phase 336 read-only file system)
- ❌ NO executable restore SQL script (Phase 338 W4)
- ❌ NO naming convention code enforcement (Phase 338 W5)
- ❌ NO LinkedGroupId auto-pair admin form change (Phase 338 W5)
- ❌ NO pre-deploy backup hook implementation (Phase 338 W5)
- ❌ NO sweep wider data loss across AssessmentSession+TrainingRecord history (out per D-01)
- ❌ NO backup tool selection (defer Phase 338 W5 IT coordination)

---

## Migration Candidates (investigation window Mar 30 – May 19)

13 migrations identified pre-scout:

```
20260401_AddMaintenanceMode
20260402_FixInterviewResultsJsonColumnType
20260406_AddAssessmentV14Columns       ← strong suspect (schema change AssessmentSession?)
20260407_AddRubrikEssayScoreMaxCharFields
20260407_RemoveUniqueIndexOnPackageUserResponse
20260407_AddExtraTimeMinutesToAssessmentSession
20260409_AddBudgetItems
20260409_RemoveBudgetItemStatus
20260410_AddCoachWorkloadThreshold
20260413_AddSamePackageToAssessmentSession  ← strong suspect (AssessmentSession column add)
20260414_AddManualEntryToAssessmentSession  ← strong suspect (AssessmentSession lifecycle add)
20260414_AddAssessmentExtraFields           ← potential
20260507_AddManageAssessmentPerfIndexes
```

Top 3 prime suspects (highest probability data-affecting on AssessmentSession): rows 3, 10, 11 di tabel di atas. Investigation Phase 336 confirm/eliminate.

---

## Existing Artifact References

| File | Purpose |
|------|---------|
| `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` | User backup PreTest (13 peserta, score total only, NO Elemen Teknis breakdown) |
| `downloads/Post Test OJT Cilacap/04-Pre-vs-Post-Comparison.csv` | CSV pre/post comparison (gain +25.46, pass 1/13→13/13) |
| `.planning/notes/2026-05-29-pretest-ojt-gast-cilacap-lost.md` | Discovery note 29 Mei (state DB 0 row + audit log 0 entry + naming convention beda dari PostTest counterpart) |
| `Models/AssessmentSession.cs` (194 LoC) | Target schema evolution analysis |
| `Data/SeedData.cs` (106 LoC) | Target seed reset pattern check |
| `Migrations/` (13 file dalam window) | Target git log + file content read |

---

## Decision Tree Restore Strategy (D-02 deferred — fill saat investigation done)

Restore strategy keluar dari Phase 336 output BERDASARKAN root cause:

| Root Cause | Forced Strategy | Rationale |
|------------|-----------------|-----------|
| Migration `DROP COLUMN` tanpa preserve | **B (skip restore)** | Schema info hilang, restore impossible. Treat Excel sebagai archive. |
| Migration recreate table (drop+create) | **B (skip)** ATAU **A (re-import manual)** | Tergantung backup .bak tersedia atau tidak. Default A kalau metadata 13 peserta + score reconstructible dari Excel. |
| `EnsureCreated()` reset Dev DB | **A (re-import manual)** | Data hilang accidental, re-import via `AddManualAssessment` feasible. AuditLog tag `ManualImport-Backfill`. |
| `SeedData.cs` reset/cleanup | **A (re-import manual)** | Same as EnsureCreated reset. |
| Schema-preserving migration (just add column) | **C (tunggu Gap #5)** | Data theoretically should persist — kalau hilang karena cascade lain, tunggu Gap #5 Excel breakdown supaya restore comprehensive dengan spider Elemen Teknis. |
| Manual cleanup (cosmetic delete dari admin UI) | **B (skip)** + **investigate AuditLog** | Bukan migration loss, beda kategori. AuditLog 0 entry suggests NOT this case. |
| Unable to determine | **B (skip) + document** | Default safe. |

---

## Naming Convention Spec — Initial Draft (D-05 polish at Phase 336 exec)

**Format wajib:** `{Stage} Test {Track} {Lokasi}`

- **Stage:** `Pre` | `Post` (capitalize first letter)
- **Test:** literal word "Test"
- **Track:** kode track sesuai Master Track table (e.g., `OJT GAST`, `OJT Pekerja GAST`, `CMP`)
- **Lokasi:** `di Unit {Unit} RU {Refinery}` ATAU short form `{Lokasi}` (e.g., `Cilacap`)

**Example mismatch ditemukan (incident source):**
- PreTest user: `OJT GAST - GTO & SRU RU IV` ← TIDAK comply (no "Pre Test" + no "Cilacap")
- PostTest counterpart: `Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap` ← partially comply

**Rationale:** Naming inconsistency = search "Cilacap" miss PreTest. Stage prefix absence = filter `Pre Test` zero result.

**Edge case Phase 336 polish:**
- Multi-unit handling (`SRU dan GTO`)
- Refinery short vs long (`RU IV` vs `Refinery Unit IV`)
- Pre/Post LinkedGroupId pairing — auto-detect or admin manual?

---

## Cross-phase Dependencies

| From | To | Hand-off |
|------|-----|---------|
| Phase 336 → Phase 338 W4 | Restore strategy decision (A/B/C) → input REST-04 execute plan |
| Phase 336 → Phase 338 W5 | Naming convention spec → input REST-06 admin form enforce + backfill plan |
| Phase 337 W3 (Gap #5 Excel breakdown) → Phase 338 W4 (kalau Option C) | Excel sheet "Elemen Teknis" required untuk comprehensive restore |

---

## Open Questions (resolve during Phase 336 execution)

1. **OQ-336-1**: Apakah `.bak` SQL Server snapshot Dev DB ada untuk window Mar 30 – May 19? Kalau ada → unblock Option A robustness.
2. **OQ-336-2**: Apakah `EnsureCreated()` ever called di Program.cs / Startup? (Kalau ya → high prob culprit untuk Dev DB reset.)
3. **OQ-336-3**: AuditLog DeleteAssessment 0 entry untuk PreTest — confirm bukan silent delete via direct SQL execute? (Out-of-band manual delete tidak masuk AuditLog.)
4. **OQ-336-4**: Naming convention enforce backward — rename existing records yang violate? Atau new records only? (Defer ke Phase 338 W5 detailed discuss.)

---

## Deliverable Phase 336 (3 doc)

1. **`336-ROOT_CAUSE.md`** — Investigation findings, culprit commit (kalau ketemu), schema evolution analysis, decision tree path taken.
2. **`336-RESTORE-DECISION.md`** — Picked strategy (A/B/C) dgn rationale, OQ-336-1..3 answered, hand-off spec ke Phase 338 W4.
3. **`336-NAMING-CONVENTION-SPEC.md`** — Format spec final + examples + edge cases + hand-off ke Phase 338 W5.

Plus standard `336-VERIFICATION.md` + `336-SUMMARY.md` pada eksekusi end.

---

## Next Step

**`/gsd-plan-phase 336`** — generate PLAN.md dengan task breakdown investigation (likely single wave: git log analysis → schema evolution map → decision tree fill → 3 doc write → verification).

Atau langsung **`/gsd-execute-phase 336`** kalau plan opsional untuk investigation phase (no code = lower planning overhead).

---

*Created: 2026-05-30 via streamlined `/gsd-discuss-phase 336` — 4 gray area decided via AskUserQuestion (all recommended picks: D-01 narrow + D-02 defer + D-03 decision doc only + D-04 git+files read only). D-05+D-06 captured no-discuss (default reasonable).*
