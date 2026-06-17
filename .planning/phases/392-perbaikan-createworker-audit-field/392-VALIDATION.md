---
phase: 392
slug: perbaikan-createworker-audit-field
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
validated: 2026-06-17
---

# Phase 392 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase 392 = VIEW-ONLY edit of `Views/Admin/CreateWorker.cshtml` (0 migration, controller/model frozen). Validation = compile gate + Playwright runtime (Razor + cascade JS dinamis → runtime wajib, lesson Phase 354) + static source-grep (readonly/bg-light removal) + git-diff 0-diff guard (controller/model).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright e2e (`tests/e2e/`) + `dotnet build` compile gate |
| **Config file** | `playwright.config.ts` (existing) |
| **Quick run command** | `dotnet build HcPortal.csproj` |
| **Full suite command** | `npx playwright test tests/e2e/createworker-392.spec.ts --workers=1` |
| **Estimated runtime** | build ~30–60s · spec ~30–90s |

> Local run prasyarat (lihat [[reference_local_e2e_sql_env_fix]]): `Authentication__UseActiveDirectory=false dotnet run` (localhost:5277), start SQLBrowser + shared-memory conn override; combined Playwright run WAJIB `--workers=1`. Login admin: admin@pertamina.com (mode lokal).

---

## Sampling Rate

- **After every task commit:** Run `dotnet build HcPortal.csproj` (0 error)
- **After every plan wave:** Run the Playwright spec `--workers=1` + grep guards
- **Before `/gsd-verify-work`:** spec green + `git diff --stat` shows ONLY `Views/Admin/CreateWorker.cshtml` (+ new spec) changed
- **Max feedback latency:** ~90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 392-01-01 | 01 | 1 | WRKR-01 | — | FullName/Email editable di SEMUA mode (readonly+bg-light dihapus unconditional) | static-grep + e2e | `! grep -nE 'readonly=|bg-light' Views/Admin/CreateWorker.cshtml` (pada input FullName/Email) + Playwright type FullName/Email | ✅ | ✅ green |
| 392-01-02 | 01 | 1 | WRKR-02 | — | Email `type=email` + span inline 4 field org (Position/Directorate/Section/Unit), no span dobel | grep + e2e | `grep type=\"email\"` + `grep -c asp-validation-for` == jumlah benar | ✅ | ✅ green |
| 392-01-03 | 01 | 1 | WRKR-02 | — | Validasi live aktif (`_ValidationScriptsPartial` di `@section Scripts`) | grep + e2e runtime | `grep _ValidationScriptsPartial` + Playwright assert pesan error muncul pra-submit | ✅ | ✅ green |
| 392-02-01 | 02 | 2 | WRKR-03 | — | Audit SEMUA field + create submission sukses (record tersimpan + redirect ManageWorkers) | e2e | `npx playwright test ...createworker-392.spec.ts --workers=1` | ✅ | ✅ green |
| 392-02-02 | 02 | 2 | WRKR-01/02/03 | — | Controller/model 0-diff (view-only) | git-diff guard | `git diff --name-only` excludes `WorkerController.cs`/`ManageUserViewModel.cs` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · (Threat Ref — none: view-only, no new attack surface; existing server-side authz/antiforgery/validation di controller FROZEN tetap berlaku)*

---

## Wave 0 Requirements

- [x] `tests/e2e/createworker-392.spec.ts` — spec baru (AD-off, `--workers=1`, self-cleaning unique-email + teardown via DeleteWorker POST). **DONE** — 137 baris, committed `840fab21` (+ WR-01 fix `e6d7b890`), TEST A static + TEST B runtime, green 3/3.
- [x] Source-grep guard helper/asserts untuk readonly/bg-light removal — **DONE** inline di spec TEST A (L41-45: `not.toMatch(/readonly=/)` + `/bg-light/` + `type="email"` + `_ValidationScriptsPartial`).

*Infrastruktur Playwright + `dbSnapshot` sudah ada (v16.0); hanya spec baru yang ditambah.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Editable Nama/Email saat mode AD (`UseActiveDirectory=true`) | WRKR-01 | Login lokal wajib AD-off (NTLM loopback fail) → runtime AD-on tak bisa diuji lokal | Di-cover **by construction** via static source-grep (readonly/bg-light dihapus unconditional → editable di kedua mode). Opsional: IT konfirmasi di Dev pasca-deploy. |

*Sisanya punya verifikasi otomatis (build + Playwright + grep + git-diff).*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (spec baru createworker-392)
- [x] No watch-mode flags
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** ✅ validated 2026-06-17 — 5/5 tasks COVERED, 0 gaps.

---

## Validation Audit 2026-06-17

State A audit (VALIDATION.md existing, reconciled against shipped artifacts). Tidak ada gap — semua 5 task sudah punya automated verify yang lolos.

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

**Bukti COVERED (static guard re-run sesi audit, app tak distart):**

| Guard | Hasil | Task |
|-------|-------|------|
| `grep -c readonly=` CreateWorker.cshtml | 0 | 392-01-01 |
| `grep -c bg-light` CreateWorker.cshtml | 0 | 392-01-01 |
| `grep -c type="email"` | 1 | 392-01-02 |
| `grep -c asp-validation-for` | 11 | 392-01-02 |
| `grep -c _ValidationScriptsPartial` + `@section Scripts` | 1 + 1 | 392-01-03 |
| `git diff --quiet HEAD -- WorkerController.cs ManageUserViewModel.cs EditWorker.cshtml` | exit 0 (ZERO_DIFF_OK) | 392-02-02 |
| `tests/e2e/createworker-392.spec.ts` exists + committed | 137 baris, `e6d7b890` | 392-02-01 |

Runtime e2e (TEST B: editable + jQuery.validator + cascade + create→ManageWorkers+DB row) green 3/3 pada eksekusi Plan 02 (SUMMARY-02 + VERIFICATION truth #8); re-run runtime butuh live app AD-off + SQLBrowser (di luar lingkup audit retroaktif) — dipercaya dari execution record + file committed + target-behavior terkonfirmasi saat baca spec.

**Manual-only carry:** editable Nama/Email saat AD-on (`UseActiveDirectory=true`) — covered BY CONSTRUCTION via static grep (readonly/bg-light dihapus unconditional); visual AD-on di Dev tetap human-UAT (lihat 392-VERIFICATION human_verification). Tidak memblok nyquist compliance karena WRKR-01 punya automated coverage (grep + AD-off runtime type fields).
