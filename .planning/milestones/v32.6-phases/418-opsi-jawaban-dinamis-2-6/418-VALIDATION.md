---
phase: 418
slug: opsi-jawaban-dinamis-2-6
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
finalized: 2026-06-24
---

# Phase 418 — Validation Report (FINAL)

> Gate report Nyquist. Draf strategi (di bawah) difinalisasi jadi laporan gate berdasarkan cakupan aktual repo. Audit retroaktif oleh gsd-validate-phase 2026-06-24.

---

## Verdict: COMPLIANT

Ketiga requirement (OPT-01/02/03) + D-418-02 (tutup hazard 999.14) punya verifikasi otomatis yang menyematkan perilakunya. **Tidak ada requirement tanpa cakupan otomatis; tidak ada 3 task berturut tanpa verify.** Logika murni (validator min-2/max-6, irisan OptionShrinkGuard) disematkan xUnit; perilaku render/JS/form disematkan e2e + integration real-SQL.

**Cakupan otomatis aktual (2026-06-24):**
- `OptionValidationTests.cs` — **12 Fact** (validator min-2/max-6/5-6-accept/correct-tanpa-teks, MC+MA). +1 gap-fill `MaxSix_MultipleAnswer_Rejected` (simetri batas max-6 untuk MA, sebelumnya hanya MC).
- `EditShrinkGuardLogicTests.cs` — **4 Fact pure** irisan `removedOptionIds ∩ answeredOptionIds` (D-418-02).
- `EditShrinkGuardIntegrationTests.cs` — **2 test real-SQL** drive `EditQuestion` ASLI (no-500 FK Restrict + opsi belum-dijawab boleh hapus). Juga melatih jalur `correctIndex → IsCorrect` (`correctIndex: 0`).
- Filter `"OptionValidation|EditShrinkGuard"` = **18/18 GREEN** (2026-06-24).
- `tests/e2e/option-dynamic-418.spec.ts` — **8 skenario S1–S8** (add→disabled@6, remove/min-2/re-letter, image-reassoc flag#4, single-select MC + render A–F, PreviewPackage 6th="F", edit 5-opt prefill, edit-shrink alert, backward-compat 4-opt). Dilaporkan PASS oleh executor 418-04 (perlu app live @5277 + DB backup/restore — referensi pass sebelumnya; tidak di-rerun di audit ini).
- Full xUnit suite 685/685 (per 418-VERIFICATION).

**Catatan `correctIndex → IsCorrect` (audit_focus #4):** Mapping single-select MC (flag #1 keystone) berada di `AssessmentAdminController.ResolveCorrectness` yang **`private static`**. Proyek TIDAK punya `[assembly: InternalsVisibleTo("HcPortal.Tests")]` (konvensi: helper test-reachable dibuat `public static`). Karena file implementasi READ-ONLY (tak boleh ubah visibility) dan test refleksi rapuh + di luar konvensi, **tidak ada pure unit test murah** untuk mapping ini. Sudah TER-cover oleh integration real-SQL (`EditQuestion(..., correctIndex: 0)` → assert opsi tersimpan, tepat-1-benar) + e2e S4 (radio single-select lintas 6 baris → DB `correctCount==1`, benar="Echo"). Tidak dibuatkan pure test (sesuai instruksi: "else note it's covered by e2e single-select + integration").

**Catatan render A–F (OPT-02):** Logika huruf adalah Razor inline di 5 view (`letters[optIdx]` + fallback numerik), TIDAK diekstrak ke helper C# — tidak ada fungsi murni untuk di-unit-test. Tepat di-cover e2e (S4/S5/S8). migration=FALSE (terkonfirmasi 418-VERIFICATION: 0 file Migrations/Data di 13 commit).

---

> _Di bawah ini: draf strategi Wave-0 asli (dipertahankan sebagai jejak). Status per-baris diperbarui ke hasil aktual._

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

| Req ID | Behavior | Test Type | Test Reference (aktual) | Status |
|--------|----------|-----------|-------------------------|--------|
| OPT-03 | Validator tolak <2 opsi | unit | `OptionValidationTests.MultipleAnswer_OneFilled_Rejected` / `MultipleChoice_ZeroOptions_Rejected` | ✅ green |
| OPT-03 | Validator tolak >6 opsi (MC) | unit | `OptionValidationTests.MaxSix_Rejected` | ✅ green |
| OPT-03 | Validator tolak >6 opsi (MA — simetri, gap-fill) | unit | `OptionValidationTests.MaxSix_MultipleAnswer_Rejected` (BARU 2026-06-24) | ✅ green |
| OPT-03 | Validator terima 5 & 6 opsi valid | unit | `OptionValidationTests.FiveOptions_Accepted` + `SixOptions_Accepted` | ✅ green |
| OPT-03 | correct-tanpa-teks ditolak (array-6) | unit | `OptionValidationTests.SixOpt_CorrectWithoutText_Rejected` (+ MC/MA varian) | ✅ green |
| D-418-02 | Guard edit-shrink: opsi dijawab → tolak (logic murni) | unit | `EditShrinkGuardLogicTests` (4 Fact: irisan removed∩answered) | ✅ green |
| D-418-02 | Guard: opsi dijawab → no-500 + redirect + state utuh | integration SQL | `EditShrinkGuardIntegrationTests.EditShrinkGuard_AnsweredOption_NotRemoved_NoException` | ✅ green |
| D-418-02 | Guard: opsi belum dijawab → boleh hapus | integration SQL | `EditShrinkGuardIntegrationTests.EditShrinkGuard_UnansweredOption_Removed_Succeeds` | ✅ green |
| OPT-01 | Form authoring: Tambah baris → 5,6 lalu disabled@6 | e2e | `option-dynamic-418.spec.ts` S1 | ✅ green (e2e, prior pass) |
| OPT-01 | Form: Hapus baris (>2) → re-letter; tak boleh <2 | e2e | `option-dynamic-418.spec.ts` S2 | ✅ green (e2e, prior pass) |
| OPT-01 (flag#4) | Hapus baris-tengah B saat C punya gambar → gambar tetap di soal benar | e2e | `option-dynamic-418.spec.ts` S3 (KRITIS, +DB assert ImagePath) | ✅ green (e2e, prior pass) |
| OPT-01 (flag#2) | Edit soal 5-opsi import → prefill 5 baris | e2e | `option-dynamic-418.spec.ts` S6 | ✅ green (e2e, prior pass) |
| OPT-01 (Inject) | Form Inject baris dinamis A–F (client-side JS) | manual/UAT | UAT live @5277 (418-VERIFICATION human-verify) — `_InjectQuestionForm.cshtml` `injAddOptionBtn` | ✅ manual (lihat Manual-Only) |
| OPT-02 | Render A–F: ujian soal 6-opsi → huruf E,F tampil | e2e | `option-dynamic-418.spec.ts` S4 | ✅ green (e2e, prior pass) |
| OPT-02 | PreviewPackage 6-opsi → ke-6 "F" (regresi modulo) | e2e | `option-dynamic-418.spec.ts` S5 | ✅ green (e2e, prior pass) |
| OPT-02 | correctIndex→IsCorrect (single-select MC) + grading by-Id benar | integration + e2e | `EditShrinkGuardIntegrationTests` (correctIndex:0) + `option-dynamic-418` S4 (DB correctCount==1) | ✅ green (lihat catatan #4) |
| Backward-compat | Soal 4-opsi: create/render/preview identik (A–D, "D.") | e2e | `option-dynamic-418.spec.ts` S8 | ✅ green (e2e, prior pass) |
| Edit-shrink UX | Hapus opsi dijawab → `alert-danger`, BUKAN 500 | e2e | `option-dynamic-418.spec.ts` S7 (real-SQL seed response) | ✅ green (e2e, prior pass) |

*Status: ⬜ pending · ✅ green · ❌ red. "e2e, prior pass" = dilaporkan PASS oleh executor 418-04 (app live @5277 + DB backup/restore); tidak di-rerun di audit (butuh app live + seed).*

---

## Wave 0 Requirements — SELESAI

- [x] `OptionValidationTests.cs` — Fact `MaxSix_Rejected`, `FiveOptions_Accepted`, `SixOptions_Accepted`, `SixOpt_CorrectWithoutText_Rejected` (array-6). **+ gap-fill** `MaxSix_MultipleAnswer_Rejected` (2026-06-24). Covers **OPT-03**. ✅
- [x] Edit-shrink guard — pure-logic `EditShrinkGuardLogicTests.cs` (4 Fact irisan) **+** integration real-SQL `EditShrinkGuardIntegrationTests.cs` (2 test, drive `EditQuestion` ASLI, seed `PackageUserResponse`, FK Restrict no-500). Covers **D-418-02**. ✅
- [x] `tests/e2e/option-dynamic-418.spec.ts` — 8 skenario S1–S8 (add/remove/disabled@6/min-2/re-letter/render A–F/PreviewPackage-F/edit-5-opsi-prefill/image-reassoc flag#4/edit-shrink-blocked + backward-compat 4-opsi). DB backup/restore (SEED_WORKFLOW). Covers **OPT-01/OPT-02/D-418-02**. ✅ (prior pass)
- [x] Extend `wizardSelectors.ts` — `optionE/F`, `correctE/F`, `addOptionBtn`, `removeOptionBtn` (digunakan spec). ✅
- [N/A] Grading regresi 6-opsi terpisah — grading by-Id agnostik & tak disentuh 418 (verifier: `GradingService.cs` 0 diff); jalur correctIndex→IsCorrect ter-cover integration `correctIndex:0` + e2e S4 (DB `correctCount==1`). Tidak perlu file baru.

*Infrastruktur xUnit + Playwright sudah ada — Wave 0 menambah Fact/file test, bukan install framework. Status: semua dependensi Wave-0 TERPENUHI.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual styling baris opsi dinamis + tombol Tambah/Hapus + alert edit-shrink | OPT-01/D-418-02 | Estetika/layout Bootstrap tak terukur unit | Playwright UAT @5277 (autopilot §5) — assert DOM live + screenshot |

---

## Validation Sign-Off

- [x] All tasks have automated verify or Wave 0 dependencies — OPT-01/02/03 + D-418-02 semua punya xUnit/integration/e2e
- [x] Sampling continuity: no 3 consecutive tasks without automated verify — tiap REQ punya minimal 1 verify otomatis
- [x] Wave 0 covers all MISSING references — semua dependensi Wave-0 terpenuhi (lihat checklist)
- [x] No watch-mode flags — filter `--filter` one-shot, no `--watch`
- [x] Feedback latency < 120s — filter `OptionValidation|EditShrinkGuard` ~8 dtk (18/18)
- [x] `nyquist_compliant: true` set in frontmatter (gsd-validate-phase, 2026-06-24)

**Approval:** COMPLIANT (audit retroaktif gsd-validate-phase, 2026-06-24). Filter `OptionValidation|EditShrinkGuard` = **18/18 GREEN**; build 0-error; e2e S1–S8 prior-pass; full suite 685/685. Gap-fill: `MaxSix_MultipleAnswer_Rejected` (simetri batas max-6 MA). Tidak ada gap pure-logic tersisa yang bisa ditutup tanpa mengubah file implementasi (mapping `correctIndex→IsCorrect` `private static` — ter-cover integration + e2e).
