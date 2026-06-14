# Phase 381: Worker Entry (StartExam integrity) - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Worker **MASUK** ujian (Normal + PrePost, single-answer, non-Proton) dengan **paket yang BENAR** — Pre & Post yang dijadwalkan **same-day** tidak saling memungut paket — dan **state worker tidak dirusak** oleh admin yang impersonate/membuka ujian (tidak memulai timer, tidak mengunci shuffle, tidak broadcast palsu). Fix di `Controllers/CMPController.cs:StartExam` GET; sibling-query helper di-mirror ke reshuffle (`Controllers/AssessmentAdminController.cs`) untuk jaga determinisme.

Dua REQ: **WSE-04** (NEW-same-day-PrePost — sibling filter `AssessmentType`), **WSE-05** (OPS-01 + TOK-03 — write-on-GET impersonation guard).

OUT (dikunci roadmap): Proton, essay, multi-answer, grading/lifecycle/cert (→ Phase 382), reshuffle hygiene SHF-02/03, perubahan UI ujian. **Depends: Phase 380** (SHF-01 `ShuffleEngine` fix wajib landing dulu — StartExam meng-consume `BuildQuestionAssignment`).
</domain>

<decisions>
## Implementation Decisions

### WSE-04 — Diskriminator pool sibling Pre/Post
- **D-01:** Sibling-query StartExam ditambah `s.AssessmentType == assessment.AssessmentType`. **AssessmentType WAJIB & satu-satunya** yang memisahkan Pre vs Post. **LinkedGroupId TIDAK dipakai** sebagai pemisah Pre/Post — temuan kode: Pre & Post dalam 1 group **share nilai LinkedGroupId yang sama** (`= preSessions[0].Id`, di-set di KEDUA sisi: Pre `AssessmentAdminController.cs:~1289`, Post `~1272`) → tidak memisahkan. AssessmentType bernilai `"PreTest"`/`"PostTest"`/`"Standard"`.
- **Rationale:** minimal & benar. Normal exam (semua `"Standard"`) tak berubah perilaku (filter `Standard==Standard` no-op). LinkedGroupId tidak menambah nilai untuk pemisahan Pre/Post; hanya berguna untuk isolasi antar-group (edge, ditolak demi minimalisme).

### WSE-04 — Determinisme sibling-set (invariant Phase 373)
- **D-02:** **Extract helper bersama** (mis. `GetSiblingSessionIds(assessment)`) yang dipakai BOTH `StartExam` GET **dan SEMUA** endpoint reshuffle. Filter identik (`Title + Category + Schedule.Date + AssessmentType`) + order `OrderBy(x => x)` → sibling-set & workerIndex konsisten lintas StartExam ↔ reshuffle. Menjaga invariant OFF≥2 round-robin (Phase 373 / v27 OQ#1).
- **D-03 (SCOPE EXPANSION eksplisit):** Meski roadmap 381 Files hanya menyebut `CMPController` StartExam + VerifyToken, fix WSE-04 **WAJIB** juga menyentuh sibling-query reshuffle di `AssessmentAdminController.cs` (≥3 titik: `ReshufflePackage ~5182`, `ReshuffleAll ~5360`, reshuffle Post `~5482`, + varian by-param `~5252` bila ber-sibling). Ini **bukan scope creep** (tak ada capability baru) — melainkan konsekuensi-benar agar tak memecah determinisme. Reshuffle **hygiene** SHF-02/03 TETAP out-of-scope: kita hanya **samakan filter sibling**, tidak mengubah perilaku reshuffle.
- **Rationale:** mengubah sibling-query satu sisi (StartExam) tanpa sisi lain (reshuffle) = bug determinisme baru pada combo same-day-PrePost + shuffle OFF + ≥2 paket + reshuffle.

### WSE-05 — Guard write-on-GET impersonasi (OPS-01 / TOK-03)
- **D-04:** Bungkus **3 write site** di `StartExam` GET dengan `if(!_impersonationService.IsImpersonating())`, **mirror precedent Phase 377 line 905** (yang sudah membungkus Upcoming→Open):
  1. **justStarted** — `Status="InProgress"` + `StartedAt=now` + `SaveChangesAsync` (`CMPController.cs:962-967`)
  2. **SignalR `workerStarted`** broadcast + `LogActivityAsync("started")` (`969-978`)
  3. **create `UserPackageAssignment`** + `SaveChangesAsync` (`1012-1056`)
  Implementasi sebagai SATU perubahan koheren (OPS-01 & TOK-03 = 1 fix, tak dipecah).
- **D-05:** **`VerifyToken` (`850-884`) TIDAK disentuh** — hanya menulis `TempData[$"TokenVerified_{id}"]` (state session per-request, BUKAN mutasi DB worker). Tak ada timer/assignment/notif yang dibakar. Dicatat agar planner tidak menambah guard tak perlu.

### WSE-05 — Render ujian saat impersonasi (assignment belum ada)
- **D-06:** Saat impersonate **dan** `assignment == null`: build `ShuffledQuestionIds` + `optionShuffleDict` **DI MEMORI** (panggil `ShuffleEngine.BuildQuestionAssignment/BuildOptionShuffle` seperti biasa) **tanpa** `_context.UserPackageAssignments.Add` / `SaveChangesAsync`. Admin melihat **preview soal read-only**, zero mutasi DB. Saat worker asli login & `StartExam` → assignment baru ter-create & persist normal (timer mulai dari nol, set shuffle X tak terkunci lebih awal — **SC#3**).
- **D-07:** Block **stale-question-check** (`1065`) tak terpengaruh — sudah dijaga `assessment.StartedAt != null`; saat impersonate-belum-mulai `StartedAt` null → block tak fire. Tak perlu perubahan tambahan.

### Claude's Discretion
- Nama/signature/lokasi helper sibling (`GetSiblingSessionIds`, private method vs util bersama) — selama dipakai **identik** StartExam + reshuffle.
- Stabilitas RNG preview impersonasi: assignment in-memory akan re-shuffle tiap reload (acak beda per refresh). Boleh pakai seed stabil (mis. derive dari `id`) agar preview konsisten, atau biarkan acak — preview saja, tak memengaruhi worker.
- Cara cabang "impersonate-render in-memory" mengonsumsi `_impersonationService.IsImpersonating()`.
- Wording pesan (bila ditampilkan) saat mode impersonate.

### Folded Todos
(none)
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Audit source (fix detail + bukti + E2E test plan)
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — Phase 1 detail (c) NEW-package-render-shuffle-1 + (d) OPS-01/TOK-03; E2E scenario **#4** (PrePost same-day, pool Pre-only) & **#7** (impersonasi read-only); §VERIFIKASI risiko "OPS-01/TOK-03 effectively one change" (line ~172) + "SHF-01 ordering vs P1" (line ~158) + "Test that needs BOTH P1 and P2" (#4 full pass/grade = acceptance pasca-382, line ~170).
- `docs/assessment-audit/2026-06-14-code-audit-findings.md` — master finding (severity, verifikasi adversarial).

### Requirements & Roadmap
- `.planning/REQUIREMENTS.md` — WSE-04 (line 21), WSE-05 (line 22), traceability (57-58).
- `.planning/ROADMAP.md` — §"Phase 381" (line 1297-1308): Goal, Files, 3 Success Criteria, Depends 380.

### Prior phase carry-forward
- `.planning/phases/380-admin-engine-integrity/380-CONTEXT.md` — SHF-01 engine fix (D-04 `BuildCrossPackageAssignment` filter paket kosong) yang **di-consume** StartExam (`1019`); **381 depends 380**.
- Phase 377 (IMP, `milestones/v28.0-phases/`) — precedent guard `if(!_impersonationService.IsImpersonating())` (CMPController.cs:905) + single-source `ImpersonationService.GetEffectiveUserAsync`/`IsImpersonating()`.
- Phase 372/373 (v27 SHUF) — determinisme OFF≥2 round-robin `workerIndex % count`, sibling-set+order StartExam = reshuffle (invariant LOCKED).

### Code anchors
- `Controllers/CMPController.cs` — `StartExam` GET **887-1078**: guard precedent **905**; write sites **962-967** / **969-978** / **1012-1056** (D-04); sibling query **982-987** (D-01); workerIndex **992-993**; engine consume **1019-1027**; stale-check **1065** (D-07). `VerifyToken` **850-884** (D-05, NOT touched).
- `Controllers/AssessmentAdminController.cs` — reshuffle sibling queries **~5182** (ReshufflePackage), **~5252** (by-param), **~5360** (ReshuffleAll), **~5482** (Post) → semua pakai helper sama (D-02/D-03). Pre/Post create `Title` shared **1235/1271**; LinkedGroupId set Pre+Post **~1272/~1289** (bukti D-01).
- `Helpers/ShuffleEngine.cs` — `BuildQuestionAssignment` / `BuildOptionShuffle` (dipanggil in-memory untuk preview impersonasi, D-06).
- `Models/AssessmentSession.cs` — `AssessmentType` (line 161), `LinkedGroupId` (line 172).
- `Services/ImpersonationService` — `IsImpersonating()` (sudah ada, no service edit).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`_impersonationService.IsImpersonating()`** — sudah dipakai di 905; guard 3 write site tanpa edit service (D-04).
- **Guard precedent line 905** = template persis untuk D-04.
- **`ShuffleEngine.BuildQuestionAssignment/BuildOptionShuffle`** — dipakai apa adanya untuk preview in-memory (D-06).
- **Sibling-query pattern** (`Title + Category + Schedule.Date` sorted) tersebar di StartExam + 3+ reshuffle → kandidat extract helper (D-02).

### Established Patterns
- write-on-GET guard `if(!IsImpersonating())` (Phase 377 Pitfall 3 / read-only invariant saat impersonate).
- sibling-set in-memory sort `OrderBy(x => x)` untuk determinisme (SQL tak jamin order tanpa ORDER BY — Phase 373 Pitfall 2).

### Integration Points
- `StartExam` GET = **handler tunggal** yang sekaligus menyentuh SHF-01 engine (380, consume) + WSE-04 sibling (381) + WSE-05 guard (381) → koordinasi 1 file, urut setelah 380 landing.
- Helper sibling baru = **cross-file** (`CMPController` + `AssessmentAdminController`) — extract sekali, panggil dari keduanya (D-02).
</code_context>

<specifics>
## Specific Ideas

- **D-01 filter:** `.Where(s => s.Title == assessment.Title && s.Category == assessment.Category && s.Schedule.Date == assessment.Schedule.Date && s.AssessmentType == assessment.AssessmentType)`.
- **D-04 guard:** mirror 905 — `if(!_impersonationService.IsImpersonating()){ /* write */ }` bungkus tiap dari 3 site.
- **D-06 in-memory:** panggil `ShuffleEngine` → set `vm` dari `shuffledIds`/`optionShuffleDict`, SKIP `_context.Add` + `SaveChangesAsync` saat `IsImpersonating()`.
- **Test (audit E2E):**
  - **#4** PrePost same-day → `StartExam` sesi Pre assert pool = paket Pre saja (jumlah & teks soal == Pre; Post tak tercampur). *Assertion entry-pool di phase ini; full pass/grade = acceptance pasca-382.*
  - **#7** impersonate buka Open `StartedAt==null` non-token → assert **no mutation** (`StartedAt` null, Status Open, tak ada `UserPackageAssignment`, tak ada SignalR `workerStarted`) → stop impersonate → worker login → `StartExam` → assert **baru saat itu** `StartedAt` ter-set.
  - **Unit determinism:** sibling helper menghasilkan set+workerIndex identik antara StartExam dan reshuffle (jaga D-02).
</specifics>

<deferred>
## Deferred Ideas

### Reviewed Todos (not folded)
- **One-time cleanup data test/audit lokal pasca-367** (`.planning/todos/pending/...`) — chore pembersihan DB lokal, tak terkait scope 381. Tetap pending (konsisten dgn 380-CONTEXT).

### Out-of-scope reminder (jangan dikerjakan di 381)
- **Full PrePost pass/grade E2E** (#4 lanjutan) = acceptance test **pasca-Phase 382**, BUKAN gate 381.
- Reshuffle **hygiene** SHF-02/03 (orphan/SavedQuestionCount drop pada Abandoned) — out, kita hanya samakan filter sibling.
- Grading/lifecycle/cert (SAVE-01/STAT/TMR/TOK-02/CERT-01) → Phase 382.
- Proton, essay, multi-answer.

(Milestone-level defer RES-02/GRD-02 dicatat di REQUIREMENTS.md.)
</deferred>

---

*Phase: 381-worker-entry-startexam-integrity*
*Context gathered: 2026-06-14*
