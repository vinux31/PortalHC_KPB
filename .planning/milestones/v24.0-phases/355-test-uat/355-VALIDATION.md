---
phase: 355
slug: test-uat
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-09
---

# Phase 355 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 355-RESEARCH.md §Validation Architecture. Phase 355 IS the test phase — its
> "implementation" is test code; validation = the tests themselves are green + the suite
> they belong to stays green + no regression.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (unit)** | xUnit (net8.0), project `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Framework (e2e)** | Playwright `@playwright/test` ^1.58.2, config `tests/playwright.config.ts` (baseURL `http://localhost:5277`, project chromium) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~PackageImage"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (then `cd tests; npx playwright test image-in-assessment.spec.ts`) |
| **Estimated runtime** | unit ~5–10s filtered / ~full <2min · e2e ~30–90s (app must run on localhost:5277) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~PackageImage"` (fast).
- **After every plan wave:** Run full `dotnet test` + 1× `cd tests; npx playwright test image-in-assessment.spec.ts`.
- **Before `/gsd-verify-work`:** `dotnet build HcPortal.sln` 0 error + full `dotnet test` green + image spec green + 1 baseline regression spec (e.g. `exam-taking.spec.ts`) green.
- **Max feedback latency:** ~10s (filtered unit) · ~90s (single e2e spec).

---

## Per-Task Verification Map

> Task IDs assigned by planner. Map below is by behavior/requirement; planner attaches the
> concrete `{padded}-{plan}-{task}` IDs. "File Exists" reflects pre-phase state (Wave 0 closes ❌).

| Behavior | Plan/Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|----------|-----------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| Upload valid (JPG/PNG/JPEG) accepted | W1 | TST-01 | T-355-01 (file-type) | magic-byte allowlist enforced | unit | `dotnet test --filter "FullyQualifiedName~FileUploadHelperTests.ValidateImageFile"` | ✅ FileUploadHelperTests.cs | ⬜ pending |
| Upload invalid (PDF-ext / exe-renamed / oversize) rejected | W1 | TST-01 | T-355-01 | non-image rejected by magic-byte | unit | `dotnet test --filter "FullyQualifiedName~FileUploadHelperTests.ValidateImageFile"` | ✅ FileUploadHelperTests.cs | ⬜ pending |
| `SyncPackagesToPost` copies ImagePath/ImageAlt Pre→Post (shared path, no dup) | W1 | TST-01 | — | sync never creates/deletes physical file | unit | `dotnet test --filter "FullyQualifiedName~PackageImageSyncTests"` | ✅ PackageImageSyncTests.cs | ⬜ pending |
| Delete: ref-count delete orphan / skip shared / collect all non-null paths | W1 | TST-01 | T-355-02 (atomic delete) | post-commit File.Delete, no orphan/over-delete | unit | `dotnet test --filter "FullyQualifiedName~PackageImageDeleteTests"` | ✅ PackageImageDeleteTests.cs | ⬜ pending |
| **Replace deletes OLD file ON DISK** (write real file → assert File.Exists==false after delete loop) | W0/W1 | TST-01 | T-355-02 | old image physically removed on replace | unit | `dotnet test --filter "FullyQualifiedName~PackageImageDeleteTests.Replace_NewFileWins_DeletesOldFileOnDisk"` | ❌ W0 — add `[Fact]` to PackageImageDeleteTests.cs | ⬜ pending |
| Admin upload image soal + each opsi via real form → save → StartExam shows `<img>` (responsive img-fluid+lazy+alt) | W2/W3 | TST-02 | — | encoded src (no raw HTML XSS surface) | e2e | `cd tests; npx playwright test image-in-assessment.spec.ts -g "StartExam"` | ❌ W0 — new spec | ⬜ pending |
| Results (pembahasan) shows gambar soal + opsi | W2/W3 | TST-02 | — | render only if ImagePath non-null | e2e | `cd tests; npx playwright test image-in-assessment.spec.ts -g "Results"` | ❌ W0 — new spec | ⬜ pending |
| Soal TANPA gambar → tidak render `<img>` (null-branch guard) | W2/W3 | TST-02 (SC#3/D-06) | — | partial renders nothing when path null | e2e | `cd tests; npx playwright test image-in-assessment.spec.ts -g "no image"` | ❌ W0 — new spec | ⬜ pending |
| Klik gambar opsi di StartExam → lightbox open, radio NOT toggled (guard bug 926a57e1) | W2/W3 | TST-02 | — | preventDefault on option-image click | e2e | `cd tests; npx playwright test image-in-assessment.spec.ts -g "lightbox"` | ❌ W0 — new spec | ⬜ pending |
| Regression: existing exam flow (MC/MA/Essay no image) still green | W3 | SC#3 | — | no regression | e2e | `cd tests; npx playwright test exam-taking.spec.ts` | ✅ exam-taking.spec.ts | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/image-in-assessment.spec.ts` — TST-02 + D-06 end-to-end spec (NEW); per-spec `beforeAll/afterAll` DB snapshot/restore (pattern `cmp-records-351.spec.ts:44-70`) + `fs.rmSync` cleanup of `wwwroot/uploads/questions/{pkgId}/` (NOT covered by DB RESTORE).
- [ ] `tests/fixtures/q-image.jpg` + `tests/fixtures/opt-image.png` — real small VALID magic-byte fixtures (NEW, D-04). Do NOT rename .txt → must be genuine JPG/PNG bytes.
- [ ] `HcPortal.Tests/PackageImageDeleteTests.cs` — +1 `[Fact] Replace_NewFileWins_DeletesOldFileOnDisk` reusing existing `MakeTempDir`/`ApplyIntent`/`DeleteIfUnreferenced` helpers (EDIT — the one real gap per D-02).
- [ ] `tests/e2e/helpers/examTypes.ts` — extend `addQuestionViaForm` to accept image fixture (setInputFiles on hidden `#questionImgField`/`#optAImgField`..); `tests/e2e/helpers/wizardSelectors.ts` — add image selectors (EDIT).
- [ ] `docs/SEED_JOURNAL.md` — append Phase 355 entry (D-05, Seed Workflow).
- [ ] Framework install: if `npx playwright test` fails "browser not found" → `cd tests; npx playwright install chromium`.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Final UAT acceptance on localhost:5277 per CLAUDE.md Develop Workflow | SC#3 | Human sign-off required before phase close (project rule); committed spec automates the steps but human confirms live render | Run app (`dotnet run`, localhost:5277), execute `image-in-assessment.spec.ts`, optionally eyeball StartExam/Results render + lightbox; confirm no seed/file left (journal `cleaned`). |

*All other phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (new spec, fixtures, replace-delete fact, helper extend)
- [ ] No watch-mode flags (no `--watch` / `playwright --ui` in committed commands)
- [ ] Feedback latency < 90s (single e2e spec) / < 10s (filtered unit)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
