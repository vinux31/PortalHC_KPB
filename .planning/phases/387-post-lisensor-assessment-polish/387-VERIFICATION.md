---
phase: 387-post-lisensor-assessment-polish
verified: 2026-06-16T04:00:00Z
status: passed
score: 7/7 must-haves verified
overrides_applied: 0
re_verification: false
---

# Phase 387: Post-Lisensor Assessment Polish — Verification Report

**Phase Goal:** Post-lisensor polish — close 7 readiness items (1 MED + 6 LOW): PXF-06 SubmitEssayScore hardening, PXF-08 cert-number retry+log+surface in FinalizeEssayGrading, PXF-09 Excel "Detail Jawaban" essay cell shows text+score, PXF-10 FinalizeEssayGrading workerSubmitted monitor broadcast, PXF-11 a11y per-letter aria on option images (Results + ExamSummary), PXF-12 SubmitExam MC absent-question no null-overwrite, PXF-13 Hub.SaveTextAnswer timer-expiry guard. Build 0 error + tests green. 0 migration.
**Verified:** 2026-06-16T04:00:00Z
**Status:** PASSED
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | PXF-06: Edit skor essay pasca-finalize (Completed) ditolak; PendingGrading tetap bekerja. WR-01 (type guard) + WR-02 (ownership guard) hadir; 386 status-guard `!= PendingGrading` (L3539) sudah menolak Completed. | VERIFIED | `AssessmentAdminController.cs:3555-3562` — `if (question.QuestionType != "Essay")` return "Soal ini bukan tipe Essay."; `AnyAsync(q => q.Id == questionId && q.AssessmentPackage.AssessmentSessionId == sessionId)` return "Soal bukan milik sesi ini."; 386 guard L3539 `!= PendingGrading` tetap utuh. xUnit 8/8 PASS (PXF-06 facts). |
| 2 | PXF-08: FinalizeEssayGrading retry cert 3x pada collision + LogError pada kegagalan persisten + certError dalam JSON. | VERIFIED | `AssessmentAdminController.cs:L3743-3772` — `maxCertAttempts = 3`, `while (!certSaved && certAttempts < maxCertAttempts)`, `catch (DbUpdateException ex) when (... CertNumberHelper.IsDuplicateKeyException(ex))`, `_logger.LogError("Failed to generate certificate number for SessionId={SessionId}...")`. `L3830-3838` — `certError` field dalam return Json. Browser UAT: cert KPB/005/VI/2026 ter-assign sukses. |
| 3 | PXF-09: Excel BulkExport "Detail Jawaban" essay cell menampilkan TextAnswer peserta + "Skor: X/Y" / "Belum dinilai" (bukan "—"). | VERIFIED | `AssessmentAdminController.cs:L4911-4919` — `essayResp.TextAnswer` (atau "Tidak dijawab"), `"Skor: {essayResp.EssayScore}/{q.ScoreValue}"` (atau "Belum dinilai"). `ExcelExportHelper.cs` tidak tersentuh (sesuai D-06). xUnit PXF-09 facts (graded/"Belum dinilai"/"Tidak dijawab") 3/3 PASS. |
| 4 | PXF-10: FinalizeEssayGrading broadcast workerSubmitted ke grup monitor-{batchKey} setelah finalize. | VERIFIED | `AssessmentAdminController.cs:L3815-3826` — `fbatchKey = $"{session.Title}\|{session.Category}\|{session.Schedule.Date:yyyy-MM-dd}"`, `_hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", new { sessionId, workerName, score, result, status, nomorSertifikat })`. Browser UAT: event `workerSubmitted` diterima live oleh klien SignalR pada grup monitor. |
| 5 | PXF-11: Gambar opsi di Results.cshtml dan ExamSummary.cshtml membawa AriaContext per-huruf (opsi A/B/C/D). | VERIFIED | `Views/CMP/Results.cshtml:L356-391` — `for (int oi = 0; oi < question.Options.Count; oi++)`, `letters[oi]`, `AriaContext = "opsi " + letter`. `Views/CMP/ExamSummary.cshtml:L57-63` — identik. Markup per-opsi dan partial Cap=240 tidak tersentuh. Playwright PXF-11 a11y spec 3/3 PASS (aria-label "opsi A" runtime di KEDUA surface). |
| 6 | PXF-12: SubmitExam MC upsert tidak men-null-overwrite jawaban tersimpan bila soal absent dari form answers. | VERIFIED | `Controllers/CMPController.cs:L1712-1718` — `if (existingResponses.TryGetValue(q.Id, out var existingResponse))` wrap `if (answers.ContainsKey(q.Id)) // guard: jangan null-overwrite`. `PackageOptionId`/`SubmittedAt` hanya diupdate bila kunci hadir di dict. MA dan Essay branch tidak tersentuh. xUnit PXF-12 facts (absent→unchanged, present→updates) PASS. |
| 7 | PXF-13: Hub.SaveTextAnswer menolak penulisan essay setelah timer habis (memperhitungkan ExtraTimeMinutes), mencatat LogWarning. | VERIFIED | `Hubs/AssessmentHub.cs:L151-161` — `if (session.StartedAt.HasValue && session.DurationMinutes > 0)` { `elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds`; `allowed = (DurationMinutes + ExtraTimeMinutes ?? 0) * 60`; `if (elapsed > allowed)` `_logger.LogWarning("SaveTextAnswer: timer expired for session {SessionId}", sessionId); return;` }. Mirror verbatim SaveMultipleAnswer:L217-227. Browser UAT A/B: sesi expired → tulis ditolak; sesi valid → tulis sukses. |

**Score: 7/7 truths verified**

---

### PXF-06 Redirect Note

Plan 387-01 Task 1 menyuntikkan guard `Status == Completed` di SubmitEssayScore. Namun Phase 386 D-08 sudah menambahkan guard `Status != PendingGrading` (L3539) yang secara efektif menolak sesi Completed (dan semua status non-PendingGrading). Guard plan menjadi dead code. Atas arahan orchestrator, Task 1 di-redirect untuk menutup 2 celah hardening tertunda 386-REVIEW: WR-01 (type guard Essay) + WR-02 (ownership guard cross-session). Must-have PXF-06 "edit pasca-finalize ditolak" tetap terpenuhi via guard 386 L3539. Tidak ada deviasi dari intent — hanya implementasi berbeda yang ekuivalen.

---

### Required Artifacts

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `Controllers/AssessmentAdminController.cs` | WR-01 type guard + WR-02 ownership guard + cert retry maxCertAttempts=3 + LogError + certError + workerSubmitted broadcast + essay BulkExport TextAnswer+EssayScore | VERIFIED | L3549-3562 (WR-01/WR-02), L3743-3772 (cert retry), L3830-3838 (certError + return Json), L3815-3826 (broadcast), L4908-4921 (essay cell) |
| `Controllers/CMPController.cs` | MC upsert guarded by answers.ContainsKey | VERIFIED | L1714 — `if (answers.ContainsKey(q.Id)) // guard: jangan null-overwrite` membalut assignment L1716-1717 |
| `Hubs/AssessmentHub.cs` | SaveTextAnswer timer-expiry guard, LogWarning | VERIFIED | L151-161 — guard identik dengan SaveMultipleAnswer; `elapsed > allowed` muncul 2x (L156 + L222) |
| `Views/CMP/Results.cshtml` | indexed for loop + `AriaContext = "opsi " + letter` | VERIFIED | L356-393 — `for (int oi = 0; oi < question.Options.Count; oi++)`, letters[], AriaContext tepat; "(Jawaban Benar)"/"(Jawaban Anda)" labels preserved |
| `Views/CMP/ExamSummary.cshtml` | indexed for loop + `AriaContext = "opsi " + letter` | VERIFIED | L57-63 — `for (int oi = 0; oi < item.OptionImages.Count; oi++)`, letters[], AriaContext tepat; partial Cap=240 tidak tersentuh |
| `HcPortal.Tests/PostLisensorPolishTests.cs` | 8 xUnit Integration facts, disposable fixture | VERIFIED | File ada (387 baris), `[Trait("Category","Integration")]`, `PostLisensorPolishFixture : IAsyncLifetime`, `HcPortalDB_Test_{guid}`, 8 facts PXF-06/09/12 |
| `tests/e2e/aria-opsi-387.spec.ts` | Playwright PXF-11 a11y spec | VERIFIED | File ada (164 baris), assert aria-label "opsi A" pada Results + ExamSummary, `--workers=1` |

---

### Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| SubmitEssayScore | question.QuestionType | `question.QuestionType != "Essay"` guard (WR-01) | WIRED | L3555-3556 — conditional return Json rejection |
| SubmitEssayScore | session ownership | `AnyAsync(q => q.Id == questionId && q.AssessmentPackage.AssessmentSessionId == sessionId)` (WR-02) | WIRED | L3559-3562 — navigation chain verified via EF |
| FinalizeEssayGrading | cert retry | `while (!certSaved && certAttempts < maxCertAttempts)` + `IsDuplicateKeyException` | WIRED | L3751-3767 — cert block dengan LogError L3770-3771 |
| FinalizeEssayGrading | monitor-{batchKey} group | `_hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", ...)` | WIRED | L3818-3826 — fire-and-forget setelah NotifyIfGroupCompleted |
| BulkExport essay branch | TextAnswer + EssayScore | `essayResp.TextAnswer`, `essayResp.EssayScore` | WIRED | L4911-4919 — data dari sessionResp yang di-load dari DB |
| SubmitExam MC upsert | answers dict | `if (answers.ContainsKey(q.Id))` guard | WIRED | CMPController.cs:L1714 — nested di dalam TryGetValue block |
| SaveTextAnswer | session timer | `elapsed > allowed` check | WIRED | AssessmentHub.cs:L156 — reject + LogWarning + return |
| Results.cshtml option image | AriaContext per-letter | `AriaContext = "opsi " + letter` dalam indexed for loop | WIRED | Results.cshtml:L391 |
| ExamSummary.cshtml option image | AriaContext per-letter | `AriaContext = "opsi " + letter` dalam indexed for loop | WIRED | ExamSummary.cshtml:L62 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| BulkExport essay cell | `essayResp.TextAnswer`, `essayResp.EssayScore` | `sessionResp` di-load dari `_context.PackageUserResponses` (real DB query sebelum loop L4904) | Ya — query DB aktual, bukan hardcoded | FLOWING |
| FinalizeEssayGrading certError | `updatedSession?.NomorSertifikat` | `_context.AssessmentSessions.FindAsync` (reload setelah ExecuteUpdateAsync) | Ya — DB value setelah retry | FLOWING |
| Results.cshtml AriaContext | `option.ImagePath`, `letter` | `question.Options` dari model server-rendered; letter dari `letters[oi]` index | Ya — render-time derivation, tidak hardcoded | FLOWING |
| ExamSummary.cshtml AriaContext | `optImg.ImagePath`, `letter` | `item.OptionImages` dari model server-rendered; letter dari `letters[oi]` index | Ya — render-time derivation, tidak hardcoded | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Evidence | Status |
|----------|----------|--------|
| Build 0 error | `dotnet build HcPortal.csproj` → "Build succeeded. 0 Error(s)" | PASS |
| xUnit Integration tests PXF-06/09/12 | 8/8 PASS (PostLisensorPolishFixture disposable DB) | PASS (per SUMMARY-04) |
| Playwright a11y PXF-11 | 3/3 PASS (aria-opsi-387.spec.ts, --workers=1) | PASS (per SUMMARY-04) |
| Fast suite regresi | 347/347 GREEN (dotnet test --filter "Category!=Integration") | PASS (per SUMMARY-02) |
| PXF-08 browser: cert assign sukses | NomorSertifikat = "KPB/005/VI/2026" ter-assign | PASS (per SUMMARY-04 Task 3) |
| PXF-10 browser: monitor broadcast | Event workerSubmitted diterima live oleh klien SignalR | PASS (per SUMMARY-04 Task 3) |
| PXF-13 browser A/B: timer guard | Expired → tulis ditolak; valid → tulis sukses | PASS (per SUMMARY-04 Task 3) |
| 0 migration baru | git log -- Migrations/ tidak menampilkan commit Phase 387 | PASS |

---

### Requirements Coverage

| REQ-ID | Source Plan | Deskripsi | Status | Evidence |
|--------|-------------|-----------|--------|----------|
| PXF-06 | 387-01 | SubmitEssayScore guard — edit skor essay pasca-Completed ditolak | SATISFIED | WR-01 + WR-02 guards + 386 status-guard ekuivalen; 8/8 xUnit PASS |
| PXF-08 | 387-01 | FinalizeEssayGrading cert retry 3x + log + surface | SATISFIED | maxCertAttempts, LogError, certError field hadir; browser UAT PASS |
| PXF-09 | 387-01 | Excel "Detail Jawaban" essay cell tampil text+score | SATISFIED | TextAnswer + EssayScore di L4915-4919; xUnit PXF-09 3 facts PASS |
| PXF-10 | 387-01 | FinalizeEssayGrading broadcast monitor group | SATISFIED | workerSubmitted SendAsync ke monitor-{fbatchKey}; browser UAT PASS |
| PXF-11 | 387-03 | A11y: gambar opsi Results/ExamSummary label huruf A/B/C/D | SATISFIED | AriaContext = "opsi " + letter pada kedua view; Playwright 3/3 PASS |
| PXF-12 | 387-02 | SubmitExam MC upsert tidak null-overwrite jawaban tersimpan | SATISFIED | answers.ContainsKey guard L1714 CMPController; xUnit PXF-12 facts PASS |
| PXF-13 | 387-02 | Hub.SaveTextAnswer timer-expiry guard | SATISFIED | elapsed > allowed guard L156 AssessmentHub; browser UAT A/B PASS |

**Coverage: 7/7 REQ satisfied. Orphan: 0.**

---

### Anti-Patterns Found

Tidak ada anti-pattern yang ditemukan. Scan pada file yang dimodifikasi:

- Tidak ada `TODO`/`FIXME`/`HACK`/`PLACEHOLDER` baru
- Tidak ada `return null`/`return []`/`return {}` stub baru yang mengaliri output user-visible
- Tidak ada handler kosong (`() => {}`, `console.log` only)
- Semua guard return rejection JSON dengan pesan Bahasa Indonesia yang jelas
- `catch (DbUpdateException) { }` yang kosong (silent) di cert block sudah diganti dengan retry loop

---

### Human Verification Required

Semua item human verification sudah selesai dilakukan oleh orchestrator (SUMMARY-04 Task 3, checkpoint APPROVED):

- PXF-08: browser cert assign → NomorSertifikat = "KPB/005/VI/2026" PASS
- PXF-10: SignalR monitor tab → event workerSubmitted diterima live PASS
- PXF-13: timer A/B test → expired ditolak, valid ditulis PASS

Tidak ada item human verification yang tersisa.

---

### Gaps Summary

Tidak ada gap. Semua 7 must-have terverifikasi di level kode aktual. Redirect PXF-06 (WR-01/WR-02 sebagai ganti Completed-guard) diterima secara sah — intent anti-tamper terpenuhi via guard 386 yang sudah ada + guards baru yang lebih kuat (type + ownership).

---

_Verified: 2026-06-16T04:00:00Z_
_Verifier: Claude (gsd-verifier)_
