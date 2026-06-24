---
phase: 418
slug: opsi-jawaban-dinamis-2-6
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 418 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Diturunkan dari 418-RESEARCH.md §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/playwright.config.ts` (no webServer — app manual `dotnet run` @5277) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OptionValidation"` |
| **Full suite command** | `dotnet test HcPortal.Tests` (+ Playwright combined `--workers=1`) |
| **Estimated runtime** | ~30–120 detik (unit); e2e terpisah |

---

## Sampling Rate

- **After every task commit:** Run quick command (filter `OptionValidation` / `EditShrinkGuard`)
- **After every plan wave:** Run full xUnit suite + build 0-err
- **Before `/gsd-verify-work`:** Full xUnit green + Playwright `option-dynamic-418.spec.ts` + regresi `option-validation-386` green (real-browser WAJIB — lesson 354 Razor/JS)
- **Max feedback latency:** ~120 detik (unit)

---

## Per-Task Verification Map

| Req ID | Behavior | Test Type | Automated Command | File Exists | Status |
|--------|----------|-----------|-------------------|-------------|--------|
| OPT-03 | Validator tolak <2 opsi | unit | `--filter OptionValidation` | ✅ `QuestionOptionValidatorTests` (min-2 ada) | ⬜ |
| OPT-03 | Validator tolak >6 opsi (BARU) | unit | `--filter OptionValidation_MaxSix` | ❌ W0 | ⬜ |
| OPT-03 | Validator terima 5 & 6 opsi valid | unit | idem | ❌ W0 | ⬜ |
| OPT-03 | correct-tanpa-teks ditolak (array-6) | unit | idem | ❌ W0 (extend) | ⬜ |
| D-418-02 | Guard edit-shrink: opsi dijawab → tolak (logic) | unit | `--filter EditShrinkGuard` | ❌ W0 (pure: removedOptionIds ∩ responses) | ⬜ |
| D-418-02 | Guard: opsi belum dijawab → boleh hapus | integration SQL | `--filter EditShrinkGuard` (real-SQL) | ❌ W0 (pola `SectionFixRegressionTests`) | ⬜ |
| OPT-01 | Form authoring: Tambah baris → 5,6 lalu disabled@6 | e2e | `option-dynamic-418` | ❌ W0 | ⬜ |
| OPT-01 | Form: Hapus baris (>2) → re-letter; tak boleh <2 | e2e | idem | ❌ W0 | ⬜ |
| OPT-01 (flag#4) | Hapus baris-tengah B saat C punya gambar → gambar tetap di soal benar | e2e | idem | ❌ W0 (KRITIS) | ⬜ |
| OPT-01 | Inject form: Tambah/Hapus baris A–F (client-side JS) | e2e | `inject-assessment-418` / extend 394 | ❌ W0 | ⬜ |
| OPT-01 (flag#2) | Edit soal 5-opsi import → prefill 5 baris | e2e | `option-dynamic-418` | ❌ W0 | ⬜ |
| OPT-02 | Render A–F: ujian soal 6-opsi → huruf E,F tampil | e2e | `option-dynamic-418` | ❌ W0 | ⬜ |
| OPT-02 | PreviewPackage 6-opsi → ke-6 "F" (bukan "A", regresi modulo) | e2e | idem | ❌ W0 | ⬜ |
| OPT-02 | Grading soal 6-opsi benar (by Id, post-shuffle) | integration | `--filter Grading` | ✅ regresi (tambah kasus 6-opsi) | ⬜ |
| Backward-compat | Soal 4-opsi: create/edit/render/grade identik | e2e+unit | `option-validation-386` (regresi) | ✅ regresi (tambah assert) | ⬜ |
| Edit-shrink UX | Hapus opsi dijawab → `alert-danger` (TempData), BUKAN 500 | e2e | `option-dynamic-418` (real-SQL seed response) | ❌ W0 | ⬜ |

*Status: ⬜ pending · ✅ green · ❌ red*

---

## Wave 0 Requirements

- [ ] `QuestionOptionValidatorTests` (atau `OptionValidationTests`) — tambah Fact: `MaxSix_Rejected`, `FiveOptions_Accepted`, `SixOptions_Accepted`, `SixOpt_CorrectWithoutText_Rejected` (extend array→6). Covers **OPT-03**.
- [ ] Edit-shrink guard test — pure-logic (`removedOptionIds ∩ responseOptionIds`) ATAU integration real-SQL (pola `SectionFixRegressionTests`/`SubmitResurrectionTests` yang seed `PackageUserResponse`). Covers **D-418-02**.
- [ ] `tests/e2e/option-dynamic-418.spec.ts` — add/remove rows, disabled@6, min-2, re-letter, render A–F, PreviewPackage 6th="F", edit 5-opsi prefill, image-row reassociation (flag#4), edit-shrink blocked message. Covers **OPT-01/OPT-02/D-418-02**. Pakai DB BACKUP/RESTORE (SEED_WORKFLOW).
- [ ] Extend `wizardSelectors.ts` — `optionE/F`, `correctE/F`, `optE/FImgField`/`ImageAlt`, `addOptionBtn`, `removeOptionBtn`.
- [ ] (opsional) Grading regresi: kasus 6-opsi ke `IsQuestionCorrectTests`/`GradingDedupeTests` (grading by Id sudah agnostik; bukti eksplisit).

*Infrastruktur xUnit + Playwright sudah ada — Wave 0 hanya menambah file/Fact test, bukan install framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual styling baris opsi dinamis + tombol Tambah/Hapus + alert edit-shrink | OPT-01/D-418-02 | Estetika/layout Bootstrap tak terukur unit | Playwright UAT @5277 (autopilot §5) — assert DOM live + screenshot |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter (oleh gsd-validate-phase setelah execute)

**Approval:** pending
