---
phase: 398
slug: test-uat-seakan-online
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-18
updated: 2026-06-19
---

# Phase 398 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail kontrak validasi (testable contracts) ada di `398-RESEARCH.md` §Validation Architecture.
> CATATAN: Phase 398 ADALAH fase test/verifikasi — "test" di sini = deliverable utama (spec e2e + rerun regresi + audit milestone), bukan test untuk kode baru.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (`tests/e2e`) + xUnit (`HcPortal.Tests`) |
| **Config file** | `tests/playwright.config.ts` + `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `cd tests && npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1` |
| **Full suite command** | `dotnet test` + `cd tests && npx playwright test --workers=1` (incl. online-path regresi) |
| **Estimated runtime** | e2e 398 ~1.1m (real-SQL + browser, 5 skenario); `dotnet test` ~2 menit (557 test) |
| **Runtime pre-req** | server localhost:5277 dari MAIN tree, `Authentication__UseActiveDirectory=false`, SQLEXPRESS+SQLBrowser hidup (lpc shared-memory) |

---

## Sampling Rate

- **After every task commit:** Run skenario e2e yang sedang dikerjakan (`--workers=1`)
- **After every plan wave:** Run spec 398 penuh + `dotnet test`
- **Before milestone audit:** Full suite hijau + spec 398 hijau + online-path regresi (lihat catatan D-05 ii)
- **Max feedback latency:** per-skenario ~1-2 menit

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| D-02a Records label | 398-01 T1 | 1 | INJ-13 | T-398-01 | inject row "Assessment Online" (data-type) tanpa penanda Manual/Inject | e2e | `npx playwright test e2e/inject-seakan-online-398.spec.ts -g "Form: 4 surface" --workers=1` | ✅ | ✅ green |
| D-02b Results per-soal | 398-01 T1 | 1 | INJ-13 | T-398-04 | badge Benar/Salah, no empty-state | e2e | `...inject-seakan-online-398 -g "Form: 4 surface"` | ✅ | ✅ green |
| D-02c Elemen Teknis card | 398-01 T1 | 1 | INJ-13 | — | "Analisis Elemen Teknis" render (soal ber-ElemenTeknis) | e2e | `...inject-seakan-online-398 -g "Form: 4 surface"` | ✅ | ✅ green |
| D-02d Cert PDF | 398-01 T1+T2 | 1 | INJ-13 | — | 200 + application/pdf + >1024 byte | e2e | `...inject-seakan-online-398` | ✅ | ✅ green |
| D-04 essay §13 Completed | 398-01 T1+T2 | 1 | INJ-13 | — | essay Status=Completed, `.bi-hourglass-split` count 0 (Form + Excel) | e2e | `...inject-seakan-online-398 -g "Form\|Excel"` | ✅ | ✅ green |
| D-04 Pre/Post linked silang | 398-01 T3 | 1 | INJ-13 | T-398-05 | inject Pre & online Post berbagi LinkedGroupId, keduanya tampil | e2e | `...inject-seakan-online-398 -g "Pre/Post"` | ✅ | ✅ green |
| D-03 side-by-side parity | 398-01 T3 | 1 | INJ-13 | T-398-05 | inject vs online sibling (IsManualEntry=0) tak bisa dibedakan (row-level) | e2e | `...inject-seakan-online-398 -g "side-by-side"` | ✅ | ✅ green |
| D-05 i full xUnit suite | 398-02 T1 | 2 | INJ-13 | T-398-08 | no regression dari milestone | unit | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` | ✅ | ✅ green (557/0) |
| D-05 ii online-path rerun | 398-02 T1 | 2 | INJ-13 | T-398-08 | jalur online utuh berdampingan inject | e2e | `npx playwright test e2e/exam-types.spec.ts e2e/exam-taking.spec.ts --workers=1` | ✅ | ⚠️ MC/MA/cert green; essay-submit (FLOW L/Flow K) pre-existing test-helper issue (non-inject) — lihat catatan ↓ |
| 0-migration gate | 398-02 T2 | 2 | INJ-13 | T-398-07 | `git diff HEAD -- Migrations/ Data/` kosong | gate | `git diff --stat HEAD -- Migrations/ Data/` | ✅ | ✅ green (empty) |
| DB cleanliness | 398-02 T2 | 2 | INJ-13 | T-398-06 | COUNT 'ZZ %398%' = 0 pasca-restore | gate | `sqlcmd ... COUNT(*) WHERE Title LIKE 'ZZ %398%'` | ✅ | ✅ green (0) |
| D-06 audit milestone v32.2 | 398-03 | 3 | INJ-13 | — | traceability INJ-01..13 + integration wired | command | `/gsd-audit-milestone v32.2` | ⬜ 398-03 | ⬜ pending (Plan 398-03) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky/exception*

### Catatan D-05 ii (online-path essay-submit) — pre-existing, NON-inject

Rerun online-path: **MC/MA full-cycle + cert (FLOW K-MA, FLOW R) PASS**, **full xUnit 557/0 PASS**, **essay flush+submit produk PASS** (`essay-flush-385.spec.ts` 3/3 — assert DB `PackageUserResponses.TextAnswer` persist + tak ditolak "belum dijawab"; server log: session essay → "Menunggu Penilaian"). **Gagal:** `exam-types FLOW L` + `exam-taking Flow K` essay-submit e2e (ExamSummary "belum dijawab" → tombol Kumpulkan blocked).

Investigasi (per permintaan): **BUKAN regresi inject** — `git 8cd59fa3..HEAD -- Views/CMP/*.cshtml` = kosong (393-397 nol perubahan view exam-taking); `Hubs/AssessmentHub.cs` last change = `0cd566ae` (Phase 387-02, v31.0, timer-guard PXF-13), **nol commit dari 393-397**. **Timer guard di-exonerasi** — server log nol "SaveTextAnswer: timer expired" / "unauthorized". Akar = test-helper `fillEssayAnswer` (examTypes.ts) jalur DIRECT `hub.invoke('SaveTextAnswer')` yang tak konsisten mantulkan answered-state di ExamSummary (jalur produk via `flushEssay`/`#reviewSubmitBtn` terbukti benar). Disposition: **pre-existing test-infra issue (Assumption A3), bukan regresi inject, bukan defect produk.** → backlog perbaikan helper. D-05 ("online tetap utuh berdampingan inject") TERPENUHI.

---

## Wave 0 Requirements

- [x] `tests/e2e/inject-seakan-online-398.spec.ts` — spec konsolidasi downstream parity (5 skenario: Form+essay+ElemenTeknis / Auto-gen / Excel / Pre-Post linked silang / side-by-side vs online asli) — **DONE (398-01, 5/5 green)**
- [x] Reuse online-path regresi specs (`exam-types.spec.ts` FLOW K/L/M/R, `exam-taking.spec.ts` Flow A) — rerun, jangan tulis ulang — **DONE (398-02; MC/MA/cert green, essay-submit pre-existing exception didokumentasi)**

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| (tak ada baru) | INJ-13 | D-01: human UAT 398 di-skip — bukti mata-manusia sudah dari per-phase UAT 394-397 | n/a |

*Sebagian besar perilaku punya automated verify; human UAT 398 sengaja di-skip (D-01).*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency reasonable (per-skenario)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (398-02) — INJ-13 downstream parity fully covered (e2e 5/5 green) + regresi (xUnit 557/0 + online MC/MA/cert green); 1 pre-existing non-inject essay-submit test-helper exception documented + routed to backlog.
