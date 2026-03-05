# Phase 98: Data Integrity Audit - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit data integrity patterns untuk bugs. Verify IsActive filters applied consistently, soft-delete operations cascade correctly, audit logging captures HC/Admin actions.

Requirements: DATA-01, DATA-02, DATA-03

Controllers in scope: AdminController (Workers, Mappings, Assessments, Silabus), CMPController, CDPController, ProtonDataController. Models: ApplicationUser, CoachCoacheeMapping, ProtonTrack, ProtonGuidance, AuditLog.

</domain>

<decisions>
## Implementation Decisions

### IsActive Filter Consistency
- **Audit depth: Spot-check high-risk queries** — Focus pada user-facing queries: ManageWorkers list, CoachCoacheeMapping list, Assessment lists, Silabus lists. Verify queries return hidden/deleted records ke UI?
- **Model scope: Cek entities lain** — 4 entities punya IsActive (ApplicationUser, CoachCoacheeMapping, ProtonTrack, ProtonGuidance). Cek apakah entities lain (Worker, Assessment, Silabus) perlu tambahkan IsActive filter.
- **Findings: Document semua gaps** — Termasuk minor inconsistencies. Critical gaps: deleted records leak ke user UI. Low severity: internal queries, admin-only pages.
- **Fix strategy: Fix semua gaps segera** — Data integrity adalah critical. Tidak boleh ada deleted records leak ke queries. Fix semua missing IsActive filters.

### Soft-Delete Cascade Verification
- **Verify method: Code review only** — Analisis Entity Framework relationships (HasForeignKey, OnDelete), manual cascade logic di controllers. Verify child queries pakai `.Where(x.ParentId.IsActive || x.ParentId == null)`.
- **Scope: All cascade relationships** — CoachCoacheeMapping → Coach/Coachee IsActive, ProtonTrack → Silabus IsActive, Assessment → Worker, ProtonGuidance → Silabus. Complete verification.
- **Orphan handling: Analyze current behavior** — Document bagaimana app handle orphaned child records: auto-hide via IsActive filter, manual cleanup, atau biarkan?
- **Test scenarios: Basic scenarios** — Soft-delete Coach → verify CoachCoacheeMapping tidak muncul. Soft-delete Silabus → verify ProtonTrack tidak muncul. Basic coverage saja.

### AuditLog Coverage
- **Actions scope: Critical only** — Delete dan mass operations WAJIB di-log (DeleteWorker, DeleteAssessment, ImportWorkers bulk). Create/Update opsional untuk non-critical entities.
- **Verify method: Exhaustive grep audit** — Grep semua Create/Update/Delete actions di AdminController, CMPController, ProtonDataController. Verify AuditLogService.LogAsync dipanggil. Document yang missing.
- **Detail level: Minimal** — Action type, entity ID, user ID, timestamp. Values before/after opsional tapi nice-to-have.
- **Missing logs: Fix critical, document minor** — Fix missing AuditLog untuk critical actions (Delete, bulk). Non-critical Create/Update tanpa log → document saja untuk future cleanup.

### Data Integrity Edge Cases
- **Verify method: Code review only** — Analisis query logic untuk detect edge cases. Cross-entity consistency: active user assigned ke inactive coach? Silabus deleted tapi ProtonTrack active?
- **Edge case types: Cross-entity consistency** — Active user → inactive coach mapping, Active worker → deleted unit, Active assessment → deleted silabus. Focus pada cross-references.
- **Severity: UI leaks = critical** — Critical: User bisa lihat data yang seharusnya hidden (orphaned records muncul di UI). Fix immediately. Non-critical: internal queries, admin-only pages.
- **Test data: Use existing data** — Use existing users dari database. Create edge case scenarios dengan soft-delete records via direct SQL atau Admin pages. Pragmatic approach.

### Bug Fix Approach (sama dengan Phase 83-85, 93-97)
- **Code review dulu → fix bugs → commit → user verify di browser**
- **Fix bugs apapun ukurannya** — Data integrity bugs adalah critical, tidak ada size limit
- **Silent bugs** — Fix jika mudah (<20 baris), otherwise log dan skip

### Claude's Discretion
- IsActive grep query patterns untuk comprehensive search
- Cascade relationship map format untuk documentation
- Edge case scenarios yang cukup untuk "cross-entity consistency" verification
- AuditLog grep patterns untuk identifying missing LogAsync calls

</decisions>

<specifics>
## Specific Ideas

- Prior audit phases (93-97, 95) gunakan "code review + spot checks" approach — ikut pattern ini untuk konsistensi
- Phase 83 (Master Data QA) sudah existing IsActive filters — verify consistent application
- Phase 85 (Coaching Proton Flow QA) punya CoachCoacheeMapping test data — bisa reuse untuk edge case testing
- Phase 97 (Auth Audit) gunakan exhaustive grep audit → pattern ini proven effective

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **AuditLogService** — Service untuk log HC/Admin actions, dipakai di AdminController, CMPController, ProtonDataController
- **AuditLog model** — Fields: Action, EntityId, UserId, Timestamp, Description
- **Soft-delete pattern** — `.Where(x => x.IsActive)` filters exist di AdminController (15+ locations)

### Established Patterns
- **Code review approach** — Audit phases gunakan grep analysis + browser spot checks, bukan exhaustive testing
- **Bug fix pattern** — Fix → commit dengan clear message → user verify di browser
- **Test data reuse** — Phases 83-85 created test data (Workers, Mappings) — reuse untuk edge cases

### Integration Points
- **AdminController** — Workers, CoachCoacheeMapping, Assessments, Silabus management
- **CMPController** — Assessment lifecycle operations
- **CDPController** — Plan IDP, Coaching Proton operations
- **ProtonDataController** — Silabus dan Coaching Guidance management

</code_context>

<deferred>
## Deferred Ideas

- Automated data integrity tests (xUnit integration tests) — future phase
- Data integrity monitoring dashboard — future phase
- Scheduled orphan record cleanup jobs — future phase
- AuditLog export/reporting features — future phase

</deferred>

---

*Phase: 98-data-integrity-audit*
*Context gathered: 2026-03-05*
