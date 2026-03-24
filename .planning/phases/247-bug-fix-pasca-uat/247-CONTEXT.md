# Phase 247: Bug Fix Pasca-UAT - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaiki semua bug yang ditemukan selama simulasi UAT (Phase 242–246), selesaikan semua pending human UAT (Phase 235, 244, 246), dan pastikan tidak ada regresi. Termasuk fix ET distribution algorithm. Admin status updates dilakukan bersamaan dengan fix yang relevan.

</domain>

<decisions>
## Implementation Decisions

### Scope Bug Fix
- **D-01:** ET Distribution Algorithm fix MASUK scope phase ini — perbaiki `BuildCrossPackageAssignment` di CMPController.cs agar distribusi soal balanced per Elemen Teknis, bukan per package
- **D-02:** Semua pending human UAT masuk scope: Phase 235 (5 item approval chain), Phase 244 (browser test monitoring), Phase 246 (4 item: token error, force close, alarm expired, records export)
- **D-03:** REQUIREMENTS.md status update (SETUP-01/02 → Complete) dan VERIFICATION status cleanup masuk scope

### Strategi Verifikasi
- **D-04:** Fix satu-satu: fix bug → langsung test → next bug. Bukan batch.
- **D-05:** Regresi fokus area yang diubah saja. v8.6 fix sudah verified sendiri, tidak perlu re-verify.

### Admin Updates
- **D-06:** Update status REQUIREMENTS.md dan VERIFICATION.md dilakukan otomatis bersamaan setiap fix yang relevan — tidak perlu task terpisah.

### Claude's Discretion
- Urutan prioritas bug mana yang di-fix duluan
- Cara testing ET distribution edge cases
- Grouping fix ke dalam plan (bisa per-bug atau per-area)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### ET Distribution Bug
- `.planning/phases/243-uat-exam-flow/243-UAT.md` — Test 6 note: "ET distribution algorithm Phase 2 distribusi per package bukan per ET"
- `Controllers/CMPController.cs` baris ~1099-1148 — BuildCrossPackageAssignment Phase 2 logic

### Pending Human UAT
- `.planning/phases/246-uat-edge-cases-records/246-HUMAN-UAT.md` — 4 pending items (token, force close, alarm, records)
- `.planning/phases/244-uat-monitoring-analytics/244-HUMAN-UAT.md` — JS fix sudah done, browser verification pending
- `.planning/phases/244-uat-monitoring-analytics/244-VERIFICATION.md` — status human_needed
- `.planning/phases/235-audit-execution-flow/235-UAT.md` — 5 skipped items (approval chain multi-role)

### Admin Status
- `.planning/REQUIREMENTS.md` — SETUP-01/02 masih "Pending", perlu update ke "Complete"
- `.planning/phases/242-uat-setup-flow/242-VERIFICATION.md` — mencatat REQUIREMENTS.md perlu update

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Controllers/CMPController.cs` BuildCrossPackageAssignment — target fix ET distribution
- v8.6 fixes (Phase 248-252) — null safety, XSS, input validation sudah diterapkan

### Established Patterns
- UAT pattern: code review → fix → browser verify (dari Phase 242-246)
- Atomic commits per fix (dari v8.6 pattern)

### Integration Points
- AssessmentMonitoringDetail.cshtml — JS token script (sudah di-fix UAT 244)
- Home/Index — alarm banner expired certificate
- Admin/RenewalCertificate — renewal flow
- CDPController — approval chain (Phase 235 items)

</code_context>

<specifics>
## Specific Ideas

- ET distribution harus setara per Elemen Teknis: misal 4 ET, 15 soal → 3-4 soal per ET (bukan timpang 6 vs 2)
- Phase 235 items memerlukan multi-role login switch (SrSpv/SH/HC/Coach) untuk testing approval chain

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 247-bug-fix-pasca-uat*
*Context gathered: 2026-03-24*
