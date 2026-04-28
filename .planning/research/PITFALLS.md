# Pitfalls Research

**Domain:** v15.0 Audit Findings 27 April 2026 — pitfalls saat patching live HR portal
**Researched:** 2026-04-28
**Confidence:** HIGH (berbasis plan teknis 11 temuan konkret + retrospective v14.0 + Playwright E2E inventory)

## Critical Pitfalls per Temuan

### T1 — Show Password Toggle

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Password plaintext bocor di shared device (admin lupa toggle off) | Auto-revert ke masked setelah 10s atau pada `blur` event | Tab-switch / window focus loss test |
| Screen reader announce password karakter saat unmasked | `aria-live="off"` di input wrapper saat unmasked + `aria-label` toggle button | NVDA mute test |
| Browser autofill conflict / saved-password override | Set `autocomplete="current-password"` eksplisit | Test pada Chrome/Edge dengan saved password |
| Toggle accidentally submit form | `type="button"` (bukan default `submit`) di toggle button | E2E test: klik toggle tidak men-trigger login attempt |

**Wave:** 1 (low risk)

---

### T2 — Score Editable per Question Type

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Retroactive grade change pada soal yang sudah completed/graded → invalidasi hasil historis | Warning modal "session sudah digunakan: X kali" + AuditLog entry sebelum save | DB query: count session yang refer question, log before/after |
| Score < 1 atau > 100 di-bypass karena server-side override sudah dihapus | Server-side range validation `Range(1, 100)` di model atau ModelState | Boundary test: 0, -1, 101, 999 |
| Total package score break grading formula (mis. PassPercentage relatif ke max score) | Display total package score di UI + warning jika berubah signifikan | Manual: edit score, verify Pass % calculation di Result |
| Excel import lama yang asumsi score=10 hardcoded | Update template Excel + import validation (warn, jangan reject) | Test import Excel dengan score variatif |

**Wave:** 2 (medium)

---

### T3 — Performance Optimization

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| `AsNoTracking()` bocor ke shared method yang punya write path | Per-method scope (jangan di context-level), explicit `AsNoTracking()` di action read-only saja | Verify Reset/ForceClose tetap save changes correctly |
| Drop `Include(a => a.User)` jadi client-eval N+1 (EF translate ke separate query per-row) | Verify generated SQL = single JOIN, bukan multiple queries | EF Core logging assert "1 query, ≥1 JOIN" |
| Pre-mature optimization tanpa baseline | Measure dulu (SQL profiler / Stopwatch), patch, measure ulang | Target: 30%+ improvement p95 response time |
| DB index migration break existing migration chain | Test migration apply + rollback di staging dulu | `dotnet ef migrations script` review sebelum apply |
| `IMemoryCache` invalidation lupa saat HC create category baru | TTL 5 menit OK untuk dropdown; OR explicit `cache.Remove()` di POST CreateCategory | Manual: tambah category, verify muncul max 5 menit |

**Wave:** 5 (perf, last)

---

### T4 — Selected List Inline (Peserta)

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| O(n²) saat 50+ workers (re-render seluruh list per change event) | DocumentFragment + debounce 100ms + diff-based update | Performance test: 50+ workers, render < 200ms |
| Double event listener cumulative (tambah `.on('change')` tanpa `.off()`) | Idempotent `.off().on()` atau use `addEventListener` once | Test: re-init step 2 tidak duplicate listener |
| Step 4 summary diverge dari Step 2 inline list | Single `renderSelectedParticipants(targetEl, checkboxes)` reuse di kedua tempat | E2E: pilih 7 peserta, verify Step 2 list = Step 4 summary |

**Wave:** 2 (medium)

---

### T5 / T6 — WIB Labels

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Misleading bila portal deploy ke region lain (KPB Sorong = WIT) | Label saja, dengan TODO komentar untuk multi-tz future | Grep label coverage 100% |
| Server time zone vs label inkonsistensi (server UTC, label WIB) | Verifikasi `TimeZoneInfo` config + storage convention | Manual: schedule jam 14:00 WIB, verify display 14:00 (bukan 07:00 UTC) |

**Wave:** 1 (UI)

---

### T7 — Rename Label MC/MA

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| HC confused, docs/training drift dengan label baru | Email blast + banner notif + update PDF panduan | Manual review training material |
| Excel import template asumption ("MC" / "MA" string keys) | Audit grep di `Services/ExcelImport*.cs`, `Controllers/AssessmentAdminController.cs` ImportPackageQuestions | DB: verify enum unchanged setelah import |
| GuideDetail / FAQ pages screenshot/text outdated | Audit `Views/Home/GuideDetail.cshtml` dan FAQ untuk MC/MA references | E2E: visit guide page, verify label baru |
| E2E Playwright test break karena hardcode label | Update test bersamaan dengan view change (single commit) | Run full Playwright suite |

**Wave:** 1 (UI + docs cross-cutting)

---

### T8 — DEFERRED

| Pitfall | Mitigation |
|---------|-----------|
| Tertimbun seperti research gap v14.0 (essay char limit) | Due date eksplisit di STATE.md + Jalur A (label fix) sebagai fallback minimal |

---

### T9 — Idempotent Finalize Essay Grading

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Race condition concurrent click (HC double-click "Selesaikan") | `ExecuteUpdateAsync` atomic CAS dengan WHERE clause sudah ada | `Task.WhenAll` parallel integration test |
| `NotifyIfGroupCompleted` dipanggil 2x → notif duplicate ke worker | `NotificationSentAt` guard atau dedupe via NotificationKey | Notif log: distinct entries per session |
| AuditLog spam karena retry | Log di success/failure terminal saja, bukan setiap call | DB: count AuditLog entries per session, expect ≤2 |
| UI tombol "Create Sertifikasi" muncul lagi setelah refresh meskipun sudah Completed | Hide condition `Status == "Completed" && NomorSertifikat != null` | Manual: finalize, refresh, verify tombol tidak ada |

**Wave:** 4 (high care)

---

### T10 — Certificate 500 Error

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Generic `catch (Exception)` hides root cause forever | Specific catches (DbException, FormatException, NRE) + structured logging | Edge case inputs: User=null, Category=null, exotic Category string |
| Defensive null check menutupi data corruption (mis. UserId orphan) | Log warning saat null detected, bukan silent fallback | `_logger.LogWarning` saat assessment.User == null setelah Include |
| Try-catch terlalu broad menutupi NotFound 404 → user lihat 500 | Letakkan try-catch hanya di area yang berpotensi exception (helper call), biarkan return NotFound() di luar try | Test: invalid id → 404 (bukan 500 nor redirect to error) |
| Log destination tidak dikonfigurasi production | Cek `appsettings.Production.json` log sink (file/EventLog/Seq) | Manual: trigger error, verify log accessible |

**Wave:** 3 (defensif + investigasi)

---

### T11 — PrePost Status Validation

| Pitfall | Mitigation | Test |
|---------|-----------|------|
| Logic disable Status bocor ke Standard mode → regression bug yang sama | `setStatusFieldVisibility(mode)` idempotent function, panggil di kedua arah | Test matrix 4 kombinasi: Standard, S→PP→S, PP, PP→S→PP |
| jQuery validate cache stale validator setelah dynamic show/hide | Re-parse `$(form).removeData('validator').removeData('unobtrusiveValidation')` lalu re-init | E2E: switch mode 3x, submit, verify tidak gagal |
| Server-side `ModelState.Remove` lupa untuk PrePost → tetap fail | Conditional `if (isPrePostMode) ModelState.Remove("Status")` di POST | Server unit test: POST PrePost tanpa Status → success |
| Test wizard return-to-step-1 behavior tidak ter-cover E2E | Tambah Playwright test untuk PrePost flow | `tests/e2e/assessment.spec.ts` baru atau augment |

**Wave:** 2 (medium)

## Integration Pitfalls (Cross-Milestone)

### UAT Pending dari Milestone Sebelumnya

| Source | Item | Risk untuk v15.0 |
|--------|------|------------------|
| Phase 235 (v8.2) | 5 items pending UAT setup audit | Medium — bisa overlap dengan T2, T4 (CreateQuestion/CreateAssessment area) |
| Phase 247 (v8.6) | 2 TODO HC review + resubmit notification | High — overlap dengan T9 NotifyIfGroupCompleted |
| Phase 303 (v14.0) | UAT 12-langkah Coach Workload dormant | Low — area Coach Workload tidak tersentuh v15.0 |

**Mitigasi:** Sebelum patch T9, audit Phase 247 status untuk pastikan notification flow tidak break.

### Known Pattern dari RETROSPECTIVE.md

- HANDOFF stale (v14.0) — pastikan v15.0 update HANDOFF eksplisit per phase
- Research gap dibiarkan tanpa due date — terapkan ke T8 dengan due date

### E2E Playwright Tests Berisiko Break

| Test File | Temuan Berisiko | Mitigasi |
|-----------|-----------------|----------|
| `tests/e2e/assessment.spec.ts` | T2 (score field locator), T4 (peserta list), T7 (label hardcoded?), T11 (Status field flow) | Update test bersamaan dengan patch (single commit) |
| `tests/e2e/exam-taking.spec.ts` | T3 (timeout perf), T9 (finalize flow), T10 (certificate redirect), T7 (label exam) | Update test |
| `tests/e2e/impersonation.spec.ts` | Tidak overlap langsung | — |

**Action:** Sebelum T7, grep `"Multiple Choice\|Multiple Answer\|MC\|MA"` di `tests/e2e/`.

## Pitfall-to-Phase Mapping

Phase numbering melanjutkan dari 303:

| Wave | Phase Suggestion | Temuan | Karakteristik |
|------|------------------|--------|---------------|
| 1 (UI low-risk) | 304 | T1 + T5 + T6 | Inline JS + label string changes |
| 1 (UI + docs) | 305 | T7 | Label rename + audit docs/tests |
| 2 (medium) | 306, 307, 308 | T2, T4, T11 | View+controller, file conflicts di CreateAssessment.cshtml — serialize |
| 3 (defensif) | 309 | T10 | Try-catch + structured log + post-deploy investigate |
| 3 (state machine) | 310 | T9 | Idempotent message + UI button hide |
| 4 (perf) | 311 | T3 | Measure-first, EF migration |
| Tracked only | — | T8 | STATE.md, due date + Jalur A fallback |

## Looks-Done-But-Isn't Checklist

Sebelum claim phase done, verifikasi:

- [ ] T1: Toggle button `type="button"` (bukan submit)
- [ ] T2: Server-side `scoreValue = 10` override sudah dihapus
- [ ] T3: SQL profiler measurement before/after didokumentasikan
- [ ] T4: 50+ peserta render < 200ms verified
- [ ] T5/T6: Grep coverage 100% — semua label time punya "(WIB)"
- [ ] T7: E2E Playwright tests passing dengan label baru
- [ ] T9: NotifyIfGroupCompleted tidak duplicate (cek log)
- [ ] T10: Specific exception catches + structured log fields
- [ ] T11: Test matrix 4 kombinasi mode switching pass
- [ ] Documentation update (panduan PDF, FAQ) untuk T7

## Sources

- Plan teknis: `C:\Users\Administrator\.claude\plans\berikut-temuan-audit-tanggal-fizzy-lampson.md`
- `.planning/RETROSPECTIVE.md` — pattern stale HANDOFF, research gap due date
- `.planning/MILESTONES.md` v8.2/v8.6/v14.0 entries — UAT pending inventory
- `tests/e2e/*.spec.ts` — Playwright test inventory
- OWASP defensive coding guide
- Microsoft Learn — EF Core 8 best practices

---
*Pitfalls research for: v15.0 Audit Findings 27 April 2026*
*Researched: 2026-04-28*
