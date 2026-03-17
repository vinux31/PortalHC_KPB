# Pitfalls Research

**Domain:** Certificate monitoring with dual data sources (TrainingRecord + AssessmentSession)
**Researched:** 2026-03-17
**Confidence:** HIGH — based on direct inspection of existing models and controller patterns

---

## Critical Pitfalls

### Pitfall 1: AssessmentSession Has No ValidUntil — Null Overloading Corrupts Expiry Logic

**What goes wrong:**
`AssessmentSession` has no `ValidUntil` field and no `CertificateType` field. When building a unified certificate row, developers reach for a shared ViewModel property like `ValidUntil` and assign `null` for assessment-sourced rows, then compute expiry status as `ValidUntil == null ? "Permanen" : ...`. The bug emerges when a `TrainingRecord` row also has `ValidUntil == null` (the field is nullable and many existing records predate it). Both cases map to `null`, but they should produce different labels: assessment = "Permanen", training with null ValidUntil = "Tidak Diketahui" or excluded from the "Aktif" count.

**Why it happens:**
The unified ViewModel flattens two sources into one shape. `null` becomes overloaded — it means two different things depending on the source. Without a discriminator field in the ViewModel, callers cannot distinguish them.

**How to avoid:**
Add a `RecordType` discriminator string (`"Manual"` / `"Assessment Online"`) to the unified ViewModel — exactly as `AllWorkersHistoryRow` already does (established precedent in Phase 40). Derive `CertificateStatus` in the server-side mapping layer, not in the view. Set status to `"Permanen"` only when `RecordType == "Assessment Online"`, regardless of `ValidUntil`.

**Warning signs:**
- Summary card "Aktif" count is inflated because training records with `ValidUntil == null` are counted as permanent
- "Akan Expired" count is always zero even when near-expiry records exist
- View uses `@if (row.ValidUntil == null)` without also checking `row.RecordType`

**Phase to address:**
ViewModel definition phase (first implementation phase). Define `CertificateMonitorRow` with `RecordType`, `CertificateStatus` (computed string), and `ValidUntil` as separate concerns.

---

### Pitfall 2: Role-Scoping Applied to Only One Data Source

**What goes wrong:**
The CDPController has working role-scoping for coaching data. Developers copy the pattern for `TrainingRecord` (filter through a role-scoped user list), but forget to apply the same scope to `AssessmentSession`. The result: Admin/HC see all certificates (correct), but SH/SrSpv see their section's training records and ALL workers' assessment certificates. Or Coach sees only their own training records but every worker's online certificates.

**Why it happens:**
`TrainingRecord` is under `ApplicationUser.TrainingRecords` (navigation property exists — easy to scope). `AssessmentSession` is a separate table with no navigation from `ApplicationUser` — it is queried independently. Developers remember to scope one and forget the other.

**How to avoid:**
Build the role-scoped user ID set first (a `HashSet<string>` of allowed user IDs), then apply it as a `.Where(x => allowedUserIds.Contains(x.UserId))` filter to BOTH `TrainingRecords` and `AssessmentSessions` queries before concatenation. Never inline role-scope logic separately inside each branch.

**Warning signs:**
- SH browsing the certificate monitor sees workers from other sections in the assessment rows but not in the training rows
- Count mismatch between summary cards and table rows when filtered by section

**Phase to address:**
Role-scoping implementation phase. Write a single `GetAllowedUserIdsAsync(currentUser)` helper and call it once; both queries consume the result.

---

### Pitfall 3: Expiry Status Computed in View — Summary Cards Desync from Table

**What goes wrong:**
Status categories (Aktif / Akan Expired / Expired) are computed as Razor conditionals (`@if (row.ValidUntil < DateTime.Now)`). Summary cards call `Count()` on the same in-memory list. This works until filters are applied: when the user filters by Bagian or status, the table is filtered server-side but the summary cards still show unfiltered totals because the count was computed before filtering.

**Why it happens:**
The pattern is easy to prototype — render rows, count rows, show counts. The bug only surfaces once filters are wired in a later phase.

**How to avoid:**
Compute `CertificateStatus` as a stored string property on the ViewModel row in the server-side mapping layer. Summary card counts then derive from `model.Rows.Count(r => r.Status == "Aktif")` on the already-filtered list. Table and cards are always in sync.

**Warning signs:**
- Summary card total does not match visible row count after applying a filter
- "Expired" count stays constant regardless of the status filter selection

**Phase to address:**
ViewModel mapping phase. `CertificateStatus` must be computed and stored on the ViewModel row before any filtering; never derive it in the view.

---

### Pitfall 4: AssessmentSession.IsPassed Is Nullable — Failed Attempts Appear as Certificates

**What goes wrong:**
`AssessmentSession.IsPassed` is `bool?`. A completed session where the worker failed has `Status == "Completed"` and `IsPassed == false`. If the query filters only on `Status == "Completed"`, failed assessments appear in the certificate list. Workers see a certificate row for an exam they failed, with no certificate file to download.

**Why it happens:**
The existing `AllWorkersHistoryRow` merge (Phase 40) includes all completed sessions because it is a history view, not a certificate view. The certificate monitor has a stricter semantic — only passed, certificate-intended sessions should appear.

**How to avoid:**
Filter `AssessmentSession` rows with `IsPassed == true && GenerateCertificate == true`. The `GenerateCertificate` flag on `AssessmentSession` explicitly marks whether a certificate was intended to be issued. Do not rely on `Status == "Completed"` alone.

**Warning signs:**
- Certificate table shows rows with no download button (no cert file was generated for that session)
- Worker sees their own failed attempts listed as certificates

**Phase to address:**
Data query phase. Add both guards to the `AssessmentSession` include predicate from the start.

---

### Pitfall 5: Date Field Mapping Is Ambiguous — Expiry Baseline Is Wrong

**What goes wrong:**
`TrainingRecord` has three date fields: `Tanggal`, `TanggalMulai`, `TanggalSelesai`. `AssessmentSession` has `Schedule`, `CompletedAt`, `StartedAt`. Developers pick one date per source without defining which date represents the certificate issuance date. The result: training records show `Tanggal` (the original record entry date) while assessment records show `CompletedAt`. Some training records have `TanggalSelesai` as the actual completion date. The displayed date is wrong and any `ValidUntil` calculated as a relative offset from the wrong anchor is also wrong.

**Why it happens:**
The two tables were designed for different purposes. Their date semantics are not equivalent. No existing code documents the canonical "certificate issuance date" for each source.

**How to avoid:**
Define the canonical mapping explicitly in a comment inside the ViewModel mapping: training = `TanggalSelesai ?? TanggalMulai ?? Tanggal` (prefer completion date as anchor); assessment = `CompletedAt ?? Schedule`. Use the same field as the baseline if `CertificateType` implies a relative expiry offset.

**Warning signs:**
- Rows sorted by date show assessment rows mixed before older training rows unexpectedly
- "Akan Expired" threshold appears off by weeks because `ValidUntil` was anchored to `Tanggal` instead of `TanggalSelesai`

**Phase to address:**
ViewModel mapping phase. Nail down the date mapping before building expiry status logic on top of it.

---

### Pitfall 6: Coach Role-Scoping Follows Coaching Pattern Instead of "Own Only"

**What goes wrong:**
The milestone spec says Coach and Coachee see only their own certificates. The existing CDPController `BuildProtonProgressSubModelAsync` scopes Coach to their mapped coachees (coaching oversight pattern). If the certificate monitor copies this pattern, a Coach sees their coachees' certificates instead of (or in addition to) their own.

**Why it happens:**
CDPController is the natural reference for CDP role-scoping. Its Coach scope is intentionally designed for coaching oversight. Certificate monitoring is a personal record — the intent is different, but the pattern is the same entry point.

**How to avoid:**
For Coach and Coachee roles, scope the query to `userId == currentUser.Id` only. Do not traverse `CoachCoacheeMapping`. Add a comment explaining the deliberate divergence from the coaching scoping pattern. The four scope tiers are: Admin/HC = all workers, SH/SrSpv = workers in their `Section`, Coach/Supervisor/Coachee = own `userId` only.

**Warning signs:**
- A coach browsing the certificate monitor sees certificates belonging to their coachees
- Removing a coach-coachee mapping causes certificates to disappear from the coach's own view

**Phase to address:**
Role-scoping phase. Document the four tiers explicitly in the action method before writing any query code.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Compute expiry status in Razor view | Faster initial build | Summary card counts diverge from table after filtering is added | Never — status belongs in the ViewModel mapping layer |
| Reuse `AllWorkersHistoryRow` ViewModel for certificates | Saves defining a new type | Missing fields (`CertificateStatus`, `SertifikatUrl`, `ValidUntil`) force nulls and fragile view conditionals | Never — define a dedicated `CertificateMonitorRow` ViewModel |
| Query both sources in memory then filter | Simpler code path | Full table scans on both tables; unacceptable once record count grows | Only in a unit test with in-memory DbContext |
| Hardcode "30 days" expiry warning threshold inline | Quick to ship | Inconsistent with `IsExpiringSoon` on `TrainingRecord` (already uses 30 days) — two sources of truth | Acceptable only if the value matches the existing model property exactly |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| TrainingRecord + AssessmentSession UNION | Join on UserId after fetching both full tables into memory | Apply `Where(x => allowedIds.Contains(x.UserId))` at DB level via `IQueryable` before `.ToListAsync()`; project to flat ViewModel before concatenating |
| Certificate file download (SertifikatUrl) | Assume `SertifikatUrl` is a directly accessible web path for all record types | TrainingRecord stores upload path under `wwwroot/uploads/certificates/`; AssessmentSession has no `SertifikatUrl` field. Serve files through a controller action that verifies ownership before streaming; never expose the raw path |
| Excel export via ClosedXML | Export all columns including internal IDs and raw nullable types | Follow the v7.1 export pattern: project to a string-typed export DTO with human-readable labels before passing to ClosedXML |
| OrganizationStructure cascade filter (Bagian > Unit) | Apply section/unit filter independently to each data source | Apply the selected section/unit as `Where` clauses on the role-scoped user ID set first, not separately on each source — use existing `GetCascadeOptions` endpoint already in CDPController |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Loading full `ApplicationUser` via `TrainingRecord.User` in a loop | Slow page load; EF generates N+1 queries | Use `.Include(r => r.User)` once on the query, or project to flat ViewModel with `.Select()` — never access `.User` in a foreach without eager loading | ~200+ records |
| Fetching all AssessmentSessions then filtering in C# | Acceptable in dev, slow under real data | Apply all `Where` predicates on `IQueryable` before `.ToListAsync()` | ~500+ sessions |
| Four separate DB queries for summary card counts | 4 extra round-trips per page load | Compute all four counts from the same in-memory list after a single DB fetch | Always wasteful — avoid from day one |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Certificate download action does not verify ownership | Worker A downloads Worker B's certificate by guessing a record ID or file URL | Before serving any file, verify `record.UserId == currentUser.Id` OR current user's role level grants access to that worker's section; return 403 otherwise |
| Role-scope bypass via filter query string | Coachee appends `?section=RFCC` to see other sections' data | Server-side: always recompute the allowed user ID set from the authenticated user's role; treat filter parameters as UI narrowing only, never as access control |
| Raw SertifikatUrl exposed as a static file link | Crafted path could traverse out of `uploads/certificates/` | Serve files through a controller action that resolves the path from the stored filename only (no user-controlled path components); never use `SertifikatUrl` as a direct `<a href>` to the file system path |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| "Download" button shown for assessment rows where no certificate file exists | User clicks, gets 404 or empty response | Check `SertifikatUrl != null` before rendering the download button; render a disabled or absent button for rows without a file |
| "Akan Expired" label without showing days remaining | SH cannot prioritize which certificates need immediate action | Show days inline: "Akan Expired (12 hari)" so urgency is immediately visible |
| Permanent certificate rows show an empty ValidUntil column | Users wonder if data is missing | Display "Permanen" string in the ValidUntil column for permanent certs, not blank or dash |
| Cascade filter resets Unit when Bagian changes but table does not reload | Stale unit selection causes confusing filter state | On Bagian change: clear Unit dropdown, repopulate via existing `GetCascadeOptions` endpoint (CDPController line 287), then auto-submit |
| Summary cards count all records regardless of active filter | Cards say "Total: 50" but table shows 12 rows after filtering | Recompute card counts server-side from the filtered set, or add a "(filtered)" label when filters are active |

---

## "Looks Done But Isn't" Checklist

- [ ] **Role-scoping symmetry:** Both `TrainingRecord` and `AssessmentSession` queries are filtered by the same role-scoped user ID set — verify by logging in as SH and confirming only their section appears in both record types
- [ ] **Permanent cert handling:** `TrainingRecord` rows where `ValidUntil == null` are NOT counted as "Aktif" unless explicitly marked as Permanent type — verify summary card math against known data
- [ ] **Failed sessions excluded:** Only `IsPassed == true` assessment sessions appear — verify a known-failed session is absent from the table
- [ ] **Certificate download guard:** Download action returns 403 for records belonging to other workers when accessed by a Coachee — verify with a manually crafted URL
- [ ] **Export respects role-scope:** Excel export contains only the same rows visible in the table for the current role — verify SH export contains only their section
- [ ] **Summary card sync:** After applying a Bagian filter, all four summary card counts update to reflect only the filtered rows
- [ ] **Cascade filter wired:** Changing Bagian clears and repopulates the Unit dropdown without retaining a stale unit from a different section

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Expiry status in view causes wrong summary counts | MEDIUM | Add `CertificateStatus` property to ViewModel row; move derivation to mapping layer; update view to use property; recompute summary counts from `.Count()` on ViewModel list |
| Role-scoping applied to only one source | LOW | Extract `GetAllowedUserIdsAsync()` helper; apply to both queries; no schema migration needed |
| Failed assessments appear as certificates | LOW | Add `&& IsPassed == true && GenerateCertificate == true` to AssessmentSession query predicate |
| Wrong date anchor for expiry math | MEDIUM | Define canonical date mapping in one place; update all `ValidUntil` derivation logic; re-verify summary card counts against known data |
| Coach sees coachees' certificates | LOW | Replace coaching-scoped user ID derivation with `userId == currentUser.Id` for Coach/Coachee roles |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Null ValidUntil overloading (Pitfall 1) | ViewModel definition phase | Confirm TrainingRecord with null ValidUntil maps to status "Tidak Diketahui", not "Permanen" |
| Role-scoping asymmetry (Pitfall 2) | Role-scoping implementation phase | Log in as SH; confirm both assessment and training rows are section-filtered |
| Expiry status in view (Pitfall 3) | ViewModel mapping phase | Apply Bagian filter; verify all four summary card counts change accordingly |
| Failed sessions in cert list (Pitfall 4) | Data query phase | Confirm a known-failed AssessmentSession row is absent from the table |
| Date column divergence (Pitfall 5) | ViewModel mapping phase | Sort by date; verify mixed-source rows sort chronologically |
| Coach sees coachees (Pitfall 6) | Role-scoping phase | Log in as Coach; confirm only own certificates appear |

---

## Sources

- Direct inspection of `Models/TrainingRecord.cs` — `ValidUntil`, `CertificateType`, `IsExpiringSoon` confirmed nullable/computed
- Direct inspection of `Models/AssessmentSession.cs` — confirmed no `ValidUntil`, `IsPassed` is `bool?`, `GenerateCertificate` is `bool`
- Direct inspection of `Models/AllWorkersHistoryRow.cs` — established precedent: dual-source ViewModel uses `RecordType` discriminator (Phase 40)
- Direct inspection of `Models/UserRoles.cs` — role level hierarchy, `HasSectionAccess()`, `IsCoachingRole()` helpers
- Direct inspection of `Controllers/CDPController.cs` — existing `BuildProtonProgressSubModelAsync` role-scoping pattern and coaching scope semantics
- Milestone spec in `PROJECT.md` — role-scope requirements: Admin/HC = all, SH/SrSpv = section, Coach/Coachee = own
- v7.1 milestone in `PROJECT.md` — ClosedXML export pattern established as project standard

---
*Pitfalls research for: Certificate monitoring with dual data sources (TrainingRecord + AssessmentSession)*
*Researched: 2026-03-17*
