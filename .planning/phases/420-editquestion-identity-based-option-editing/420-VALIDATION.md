---
phase: 420
slug: editquestion-identity-based-option-editing
status: compliant
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-25
finalized: 2026-06-25
---

# Phase 420 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) — `HcPortal.Tests` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test HcPortal.Tests` (Integration trait needs SQLEXPRESS) |
| **Estimated runtime** | quick ~20–40s · full ~2–4 min (real-SQL fixtures) |

Notes: identity-edit proof tests are `[Trait("Category","Integration")]` (real SQL Server via `SectionFixture` — FK `PackageUserResponse→PackageOption` Restrict only real on SQL Server, not InMemory). Pure-helper `EditShrinkGuardLogicTests` runs in the quick set. Playwright e2e is separate (`--workers=1`, SEED_WORKFLOW snapshot→seed→restore).

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + quick test set
- **After every plan wave:** Run full suite (incl. Integration)
- **Before `/gsd-verify-work`:** Full suite green + Playwright UAT green
- **Max feedback latency:** ~40s (quick) / ~4 min (full)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|------|--------|
| OPTEDIT-01 | 01 | 1 | Hapus opsi tengah belum-dijawab → record tepat terhapus, survivors tidak ter-relabel | — | identity upsert match by Id; C tetap "C" bukan ter-geser | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_MiddleDelete_Unanswered_NoRelabel_Succeeds"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-02 | 01 | 1 | Hapus opsi sudah-dijawab (posisi manapun, termasuk tengah) → diblokir pesan ramah; jawaban peserta utuh | T-420-04 | set-difference `existingIds.Except(keptIds)` → guard menyala untuk posisi manapun | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "EditShrinkGuard_AnsweredOption_NotRemoved_NoException"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-03 | 01 | 1 | Edit teks/kebenaran opsi terjawab → UPDATE record by Id; jawaban peserta tetap merujuk Id yang sama | — | `keptIds.Contains(o.Id)` → UPDATE path; PackageOptionId peserta stabil | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_EditAnsweredOption_TextAndCorrectness_UpdatesById"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-04 | 01 | 1 | Konversi MC/MA→Essay + shrink opsi terjawab → ditolak tanpa error 500 (regression-lock 999.14) | T-420-05 | Essay branch: removedOptionIds=semua → guard menyala pre-SaveChanges; FK-Restrict tak ter-trip | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_ConvertAnsweredMcToEssay_Blocked_NoException"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-05 (add-opt) | 01 | 1 | Add opsi baru (Id null) → di-ADD; opsi A Id stabil tidak ter-overwrite | — | `newRows = options.Where(!Id.HasValue)` → INSERT; `keptIds.Contains(o.Id)` → UPDATE existing | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_AddOption_NullId_Adds_NotOverwriteExisting"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-05 (unanswered-remove) | 01 | 1 | Edit soal belum-dijawab: hapus opsi → sukses; existing flow unaffected | — | identity contract: keep by Id, remove by absence; unanswered option boleh dihapus | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "EditShrinkGuard_UnansweredOption_Removed_Succeeds"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| OPTEDIT-05 (new-as-correct) | 01 | 1 | Add opsi baru dan jadikan benar (correctIndex ke baris null-Id) → berfungsi | — | `ResolveCorrectness` posisional masih berlaku; opsi baru ditambah + IsCorrect=true | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_AddOption_SetNewAsCorrect_AddsAndMarksCorrect"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| T-420-01 anti-tamper | 01 | 1 | Id asing (dari soal/paket lain) → reject fail-closed pre-mutation; 0 mutasi | T-420-01 | `submittedIds.Any(id => !existingIds.Contains(id))` → reject sebelum SaveChanges | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_AntiTamper_ForeignOptionId_Rejected_NoMutation"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| T-420-02 duplicate-Id | 01 | 1 | Id duplikat dalam submit → reject fail-closed; 0 mutasi | T-420-02 | `submittedIds.Count != submittedIds.Distinct().Count()` → reject "duplikat" | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "IdentityEdit_DuplicateSubmittedId_Rejected"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| Pure guard helper | 01 | quick | Irisan `FindBlockedOptionIds(removed, answered)` = removedOptionIds ∩ answeredOptionIds | — | signature dan semantik UNCHANGED dari Phase 418 | Unit (pure, no DB) | `dotnet test HcPortal.Tests --filter "Category!=Integration"` | `HcPortal.Tests/EditShrinkGuardLogicTests.cs` | ✅ green |
| VRF-01 (integration leg) | 01 | 1 | Integration test mereproduksi relabel-senyap (bug 999.15) lalu membuktikan kini diblokir | T-420-04 | TEST1 port: B di-omit dari submit → removedOptionIds={B} → guard fires; RED-on-main → GREEN | Integration (real-SQL) | `dotnet test HcPortal.Tests --filter "EditShrinkGuard_AnsweredOption_NotRemoved_NoException"` | `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | ✅ green |
| VRF-01 (Playwright leg) | 03 | manual | Real-browser: hidden Id survive JS reletter/add/remove; friendly error renders di browser | T-420-04 | Razor+JS DOM behavior — tidak bisa dijangkau controller test (lesson 354) | Playwright e2e | manual-only — lihat Manual-Only Verifications | `tests/e2e/identity-option-edit-420.spec.ts` (belum dibuat — Plan 03 Task 2, autonomous:false) | ⬜ manual pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Summary:** 11 automated tests green (10 Integration real-SQL + 4 pure-unit quick set). 1 manual-only item registered (VRF-01 Playwright leg). No automated coverage gap.

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` — TEST 1/TEST 2 di-port ke identity contract (`OptionInput { Id = optionIds[i] }`); 7 identity tests baru (#1 MiddleDelete_Unanswered, #2 EditAnswered, #3 ConvertToEssay, #4 AntiTamper, #5 AddOption, #6 DuplicateId, #7 AddOption_SetNewAsCorrect) — **8 total, semua green 702/702 full suite SQLEXPRESS 2026-06-25**
- [x] `HcPortal.Tests/EditShrinkGuardLogicTests.cs` — 4 pure-logic tests masih green (signature `FindBlockedOptionIds` UNCHANGED); verifikasi tidak perlu edit
- [x] `HcPortal.Tests/SectionFixRegressionTests.cs` — Edit6Options/H3 (edit-unanswered regresi Section) tetap green; tidak disentuh Phase 420
- [ ] Playwright spec `tests/e2e/identity-option-edit-420.spec.ts` — 3 skenario (delete-middle-answered, delete-middle-unanswered-no-relabel, add-option-no-clone-gotcha) — **Plan 03 Task 2, autonomous:false, PENDING**

*Wave 0 automated = COMPLETE. Playwright adalah registered manual gate, bukan automated gap.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Real-browser: hapus opsi tengah sudah-dijawab (B) → pesan error memuat "sudah dijawab" + "B"; DB 4 opsi utuh | VRF-01 | Razor+JS DOM behavior — hidden Id carrier via `populateEditForm`, reletter via `reletterRows`, submit; controller integration tidak bisa menguji reindex client-side (lesson 354) | App @5277. Seed (SEED_WORKFLOW): soal MC 4-opsi, PackageUserResponse ke opsi B. Buka ManagePackageQuestions → edit soal → hapus baris B → Simpan. Assert: TempData error "sudah dijawab" + "B"; DB soal masih 4 opsi. |
| Real-browser: hapus opsi tengah belum-dijawab → sukses; reload form: C tidak ter-relabel jadi "B" | VRF-01 | Membuktikan hidden Id bertahan melewati JS reindex (reletterRows) saat hapus baris dari DOM | Soal MC 4-opsi BELUM dijawab. Hapus baris B → Simpan → buka form edit lagi. Assert: 3 opsi teks A, C, D — C tidak ter-relabel. |
| Real-browser: tambah opsi → opsi A tidak ter-overwrite (GOTCHA §2c clone-reset hidden Id) | VRF-01 | Efektivitas `if (inp.type === 'hidden') inp.value = ''` di DOM live harus dibuktikan Playwright (analog lesson 413 — runtime-only bug) | Edit soal MC 4-opsi → klik "Tambah Opsi" → isi "E" → Simpan. Assert: 5 opsi tersimpan; opsi A teks tetap "A"; opsi baru teks "E". |

---

## Validation Sign-Off

- [x] All tasks have automated verify or registered manual-only entry
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (all tasks in Plan 01 Wave 1 covered)
- [x] Wave 0 automated covers all MISSING references (8 new/ported integration tests + 4 pure-logic green)
- [x] No watch-mode flags
- [x] Feedback latency < 4 min (full suite ~3m25s confirmed 2026-06-25)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** COMPLIANT — 2026-06-25 (Nyquist auditor pass; 702/702 full suite green; 0 automated coverage gaps; 1 registered manual-only Playwright gate does not block compliance)
