# Phase 306: Score Editable per Question Type - Context

**Gathered:** 2026-04-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Memungkinkan Admin/HC untuk menyimpan skor 1–100 untuk soal MultipleChoice, MultipleAnswer, dan Essay (REQ QSCR-01, maps Audit Temuan 2). Saat ini server-side memaksa `scoreValue=10` untuk MC/MA dan input view di-disable kecuali untuk Essay. Phase ini melepaskan restriksi tersebut dengan validasi range 1–100 + audit log saat soal dengan session associated diubah scorenya.

**Out of scope:**
- Bulk-set score untuk semua soal sekaligus (T2 differentiator — defer ke roadmap backlog)
- Score change pada PackageQuestion individual yang sudah completed dengan reset force-recalculate (current formula auto-recalculate via `(sumAnswered/sumPossible)*100` di runtime)
- Migration/backfill explicit ScoreValue di DB (sudah explicit, tidak NULL)

</domain>

<decisions>
## Implementation Decisions

### Form UX (default value, switch behavior, help text)
- **D-01:** Default value input baru tetap `value="10"` untuk semua tipe (MC/MA/Essay). Familiar untuk user existing, minimal disruption ke UX.
- **D-02:** Saat user switch tipe via dropdown (Essay→MC, MC→MA, dll), nilai user-entered DI-PRESERVE (tidak reset ke 10). Hapus baris `if (qtype !== 'Essay') scoreInput.value = 10;` di line ~298.
- **D-03:** Hapus attribute `disabled` dari input scoreValue di line 186 (`<input ... value="10" min="1" max="100" disabled />` → `<input ... value="10" min="1" max="100" step="1" required />`).
- **D-04:** Hapus juga baris JS `scoreInput.disabled = (qtype !== 'Essay');` di line ~297. Input selalu enabled untuk semua tipe.
- **D-05:** Help text `<div id="scoreHelp">` line 187 dan baris JS line ~308-309 yang ubah text per tipe → ganti jadi static text **"Range 1–100"**. Hapus logika dynamic per-tipe wording. (Atau: drop entirely jika label form `Nilai Soal (1–100)` sudah cukup. Implementasi pilih salah satu — Claude's Discretion.)

### AuditLog UX & gating (untuk EditQuestion saat session associated)
- **D-06:** Saat user submit EditQuestion AND `newScoreValue != oldScoreValue` AND ada `PackageUserResponses` rows untuk soal tersebut → tampilkan **modal warning + confirm** sebelum POST.
- **D-07:** Modal text: *"Skor soal #{Order} akan diubah dari **{old}** menjadi **{new}**. **{N} peserta** sudah menjawab — persentase mereka akan dihitung ulang otomatis. Lanjutkan?"*
- **D-08:** Modal buttons: "Ya, Lanjutkan" (btn-primary) + "Batal" (btn-secondary). Konsisten dengan modal pattern existing di ManagePackageQuestions (Peringatan Ubah Tipe Soal di line ~e156).
- **D-09:** Implementation: server pass `data-original-score` + `data-affected-sessions` attribute ke form Edit. Client-side: form submit handler check delta + count, trigger modal jika condition met.
- **D-10:** Audit log entry MANDATORY di server-side EditQuestion (line ~4822) saat scoreValue change: `await _auditLog.LogAsync(currentUser.Id, actorName, "EditQuestion-ScoreChange", $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)");`. Pakai pattern existing yang sudah established (line 326, 378, 1292, 1794, dll).
- **D-11:** Audit log entry juga dibuat untuk CreateQuestion non-default score (mis. admin set score=15 saat create) — tapi WITHOUT modal (no existing session to recalculate). Format: `"CreateQuestion: Question added with custom ScoreValue={scoreValue} (default 10) for Package #{packageId}"`.

### Validation error UX (Range 1–100 enforcement)
- **D-12:** **Both layered defense in depth:** HTML5 client-side (`min="1" max="100" step="1" required`) + server-side explicit check.
- **D-13:** Server-side check di CreateQuestion (line ~4681) DAN EditQuestion (line ~4822):
  ```csharp
  if (scoreValue < 1 || scoreValue > 100)
  {
      TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
      return RedirectToAction("ManagePackageQuestions", new { packageId });
  }
  ```
- **D-14:** **HAPUS** baris existing line 4682-4683 dan 4823-4824:
  - `if (questionType != "Essay") scoreValue = 10;` → REMOVE (over-restrictive — phase 306 hapus)
  - `if (scoreValue <= 0) scoreValue = 10;` → REMOVE (over-permissive — silently coerce invalid input ke default tanpa user awareness; ganti dengan range check yang reject)
- **D-15:** Error message konsisten Bahasa Indonesia, format flash error pakai TempData["Error"] (sama dengan validation error existing untuk correctCount + Essay rubrik).

### Existing data + Total possible score impact
- **D-16:** **NO backfill DB.** Existing 50 MC + 8 MA dengan ScoreValue=10 dibiarkan as-is (sudah explicit, bukan NULL — verified via sqlcmd query). Admin bisa edit per-soal sesuai kebutuhan.
- **D-17:** **Tampilkan "Total Points" di header list** ManagePackageQuestions.cshtml line 49 (existing `Daftar Soal (3 soal)` → `Daftar Soal (3 soal • Total {X} poin)`). Computed inline dari `Model.Questions.Sum(q => q.ScoreValue)`.
- **D-18:** **No formula change.** Scoring formula `finalPercentage = (totalScore / maxScore) * 100` di `CMPController.cs:1705` sudah robust untuk varied score. Tidak perlu adjustment di SubmitExam atau ExamSummary atau CertificatePdf.
- **D-19:** **No retroactive rescore.** Saat admin edit score soal yang sudah punya completed sessions, **percentage di stored di AssessmentSessions.Score** TIDAK auto-recalculate (per architecture existing — Score di-persist saat SubmitExam). Modal warning di D-07 menjelaskan dampak hanya untuk session **future** yang akan menjawab soal ini, plus session yang sedang InProgress (belum SubmitExam) — Completed sessions retain their stored Score.

### Claude's Discretion
- **CD-01:** Help text exact wording — "Range 1–100" vs "Skor 1–100 (default 10)" vs drop entirely. Pilih saat plan/execute berdasarkan visual fit.
- **CD-02:** Modal CSS styling — reuse existing `peringatan-ubah-tipe` modal pattern atau pakai bootstrap modal generik. Implementer pilih based on existing assets.
- **CD-03:** "Total Points" exact format — `• Total 30 poin` vs `(30 pts)` vs `— 30 pts total`. Pilih yang konsisten dengan visual style ManagePackageQuestions header.
- **CD-04:** Apakah tambah `[Range(1, 100)]` data annotation di parameter signature di addition to inline check (D-13). Jika MVC convention demand, pakai. Jika tidak konflik dengan inline check style, skip.
- **CD-05:** Apakah CreateQuestion juga perlu detect non-default score creation (D-11) sebagai informational audit, atau cukup edit-only audit (D-10). Implementer pilih based on audit verbosity tolerance.

</decisions>

<specifics>
## Specific Ideas

- **Pattern compliance:** Audit log call follows existing pattern `_auditLog.LogAsync(userId, actorName, "ActionName", $"...details...")` — see line 326 (AddCategory), 378 (EditCategory), 1292 (CreateAssessment), 1794 (EditAssessment) for reference.
- **Modal pattern:** ManagePackageQuestions sudah punya modal "Peringatan Ubah Tipe Soal" untuk dropdown change (saat user switch tipe pada existing question dengan answers). Pakai pattern yang sama untuk score change modal — visual + flow konsisten.
- **Field signature stability:** Parameter `int scoreValue` di Create/EditQuestion JANGAN ubah ke `int?` atau `decimal` — current usage cukup int (whole numbers 1–100).
- **Validation UX expectation:** User trust HTML5 native validation di Chrome/Edge browser yang dipakai admin — instant tooltip saat blur/submit. Server-side hanya defensive untuk DevTools bypass case (low risk, internal admin tool).

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase requirement spec
- `.planning/REQUIREMENTS.md` — Section "Question Management" / QSCR-01 — full requirement text + maps Audit Temuan 2
- `.planning/ROADMAP.md` — Phase 306 entry (line 99-107) — 5 success criteria

### Existing audit log infrastructure
- `Services/AuditLogService.cs` — `LogAsync(userId, actorName, action, details)` signature; service injected via constructor (see AssessmentAdminController:32)
- `Controllers/AssessmentAdminController.cs:326, 378, 407, 430, 1292, 1334, 1794, 1897, 2007` — established call sites pattern reference

### Files to be modified (per success criteria)
- `Views/Admin/ManagePackageQuestions.cshtml` — Lines 184-187 (input + help text), 290-310 (JS dropdown change handler), 49 (header total display addition)
- `Controllers/AssessmentAdminController.cs` — Lines 4675-4690 (CreateQuestion validation), 4815-4830 (EditQuestion validation)

### Scoring formula (NO change, but verify dependencies)
- `Controllers/CMPController.cs:1646, 1670, 1699, 1705` — totalScore accumulation + finalPercentage formula. Phase 306 tidak ubah, tapi planner verify dengan grep "ScoreValue" untuk confirm no breaking caller exists.

### Phase 305 baseline (just-completed phase 305 context — depends_on)
- `.planning/phases/305-question-type-naming-clarity/305-CONTEXT.md` — Helper class QuestionTypeLabels available for label rendering, internal enum DB tetap (D-17 schema lock confirms phase 306 also no DB schema change)
- `.planning/phases/305-question-type-naming-clarity/305-01-SUMMARY.md` — Form ManagePackageQuestions structure already understood; Phase 306 hanya hapus disabled + ubah help text

### Audit Pertamina compliance context
- No external compliance ADR file exists in repo. Internal Pertamina HC audit (April 2026) Temuan 2 source: tracked di REQUIREMENTS.md saja.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_auditLog.LogAsync(...)` service: established pattern, 9+ existing call sites di AssessmentAdminController.cs. Reuse langsung, no new abstraction.
- Modal Bootstrap pattern: `Peringatan Ubah Tipe Soal` modal di ManagePackageQuestions.cshtml — reuse template structure dengan content baru (Score Change warning).
- `QuestionTypeLabels.cs` helper (created phase 305): TIDAK relevan untuk phase 306 (label tipe sudah final dari phase 305, phase 306 tidak ubah label).

### Established Patterns
- **Validation flow:** Server-side check → `TempData["Error"] = "..."` → `return RedirectToAction("ManagePackageQuestions", new { packageId });`. Konsisten dengan correctCount validation (line 4686-4699) dan rubrik check (line 4696-4699).
- **Audit log call:** Always inside `try/catch` dengan logger fallback `_logger.LogWarning(auditEx, "Audit logging failed during ...");` (per pattern di line 1342, 2015). Adopt same defensive pattern di phase 306 implementation.
- **JS dropdown change handler:** Line 290-310 di ManagePackageQuestions.cshtml — function dipanggil saat dropdown change, ubah visibility + reset values. Phase 306 hanya hapus 2 baris (disabled toggle + value reset).

### Integration Points
- **CMPController scoring** (line 1646, 1670, 1699, 1705): downstream consumer of ScoreValue. Tidak butuh modification (formula robust). Planner perlu grep verification untuk confirm no other consumers break with custom ScoreValue.
- **AssessmentSessions.Score** field: stored at SubmitExam time, NOT recomputed retroactively. Phase 306 modal D-07 mention "future + InProgress" sessions affected, NOT Completed sessions (their stored Score remains).
- **ImportPackageQuestions Excel template** (`Views/Admin/ImportPackageQuestions.cshtml` + `AssessmentAdminController.ImportQuestions`): Excel column "ScoreValue" (kolom 9?) atau implicit default 10. Planner verify apakah import flow juga butuh accept range 1-100 atau hanya UI form.

</code_context>

<deferred>
## Deferred Ideas

- **Bulk-set score** (semua soal di package set ScoreValue=X sekaligus) — T2 differentiator audit, defer ke roadmap backlog v16+.
- **Per-question elemen teknis weighted score** — beyond MVP audit fix.
- **Re-score historical Completed sessions** — risk-heavy (Pass↔Fail flip historical), butuh business approval. Defer ke separate phase jika ada permintaan eksplisit.
- **Excel import scoreValue range validation** — perlu di-verify apakah ImportQuestions juga perlu accept varied score. Jika butuh, tambah ke phase 306 saat planning, atau spawn sub-task. Default: ImportQuestions tetap accept current behavior (MC/MA→10, Essay varied) sebelum confirmed need to update.

</deferred>

---

*Phase: 306-score-editable-per-question-type*
*Context gathered: 2026-04-28*
