# Project Research Summary

**Project:** PortalHC KPB v3.3 — Basic Notification System
**Domain:** ASP.NET Core MVC — In-App Notification System for Assessment & Coaching Workflows
**Researched:** 2026-03-05
**Confidence:** HIGH

## Executive Summary

PortalHC KPB requires a basic in-app notification system to inform workers about assessment assignments and coaching progress. Research reveals this is a well-established pattern in ASP.NET Core MVC: follow the existing `AuditLogService` pattern with a new `NotificationService`, two new database tables (Notification + UserNotification), and a Bootstrap bell icon UI. **No new NuGet packages are needed** — all dependencies (Bootstrap 5, Bootstrap Icons, EF Core 8.0) are already present.

The recommended approach is refresh-based (no SignalR) with database polling every 30 seconds. This keeps v3.3 scope manageable while delivering core functionality: 10 notification triggers across Assessment (4 triggers) and Coaching Proton (6 triggers) workflows. Critical risks include N+1 query performance degradation, notification spam causing user fatigue, and tight coupling of notification logic to controllers (breaking existing workflows). These are mitigated through proper indexing, rate limiting, and following service layer patterns.

## Key Findings

### Recommended Stack

This is a brownfield extension, not a greenfield rewrite. Leverage existing infrastructure — no new dependencies required for v3.3. The stack follows the proven `AuditLogService` pattern: scoped service injection, async operations, and clean separation from controllers.

**Core technologies:**
- **ASP.NET Core MVC 8.0** — Web framework (already in use) — mature, fully supported through 2026-11-10
- **Entity Framework Core 8.0** — ORM (already in use) — integrates seamlessly with existing ApplicationDbContext
- **SQL Server** — Database (already in use) — supports filtered indexes for notification query performance
- **Bootstrap Icons 1.10.0** — Bell icon (already loaded in `_Layout.cshtml`) — no new CSS needed
- **Bootstrap 5 Dropdown** — Notification center UI (already in use) — consistent with existing UI patterns

**Two-table database design:**
- **Notification table** — Stores message content once (title, message, type, created_at, sender info)
- **UserNotification table** — Tracks per-user read status (scales efficiently for multi-recipient notifications)

**Service layer:**
- **NotificationService** — Business logic layer following `AuditLogService` pattern (simple service class, injected into controllers)
- **INotificationService** — Interface abstraction for unit testing and dependency injection

### Expected Features

Research identified clear table stakes features for 2026 web applications. Missing these makes the product feel incomplete, while over-engineering notification preferences or real-time delivery in v3.3 would be premature.

**Must have (table stakes):**
- **Notification Center (Bell Icon)** — Standard UI pattern with badge counter showing unread count — users expect centralized location for all notifications
- **Notification List** — Chronological list view with timestamps — users need to see all notifications
- **Read/Unread Status** — Visual distinction (bold or different background) — users need to know what's new
- **Mark as Read** — Click notification → marks as read → clears from counter — users need ability to dismiss
- **"Mark All as Read"** — Bulk action for notification fatigue — UX best practice
- **Persistent Notifications** — Database-backed (not session-based) — notifications persist across sessions
- **Deep Linking** — Clicking notification navigates to relevant content — links to /CMP/Exam, /CDP/ProtonProgress, /Admin pages
- **Pagination** — Load 10-20 at a time with "Load More" button — performance optimization

**Should have (competitive):**
- **Deadline Reminders** — Proactive: notify workers 1 day before assessment deadline — requires scheduled job (Hangfire/Quartz)
- **Filtered Views** — Users can filter by type (Assessment only, Coaching only) — improves UX with higher notification volumes
- **Non-Intrusive Delivery** — No modal popups; notifications appear in center only — follows "notification center" pattern

**Defer (v2+):**
- **Real-Time Notifications (SignalR)** — Adds significant complexity; refresh-based is adequate for v3.3 — target v3.4+
- **Notification Preferences** — Over-engineering for 10 triggers; all notifications are business-critical — target v3.4+
- **Browser Push Notifications** — Requires user permission, can feel intrusive — in-app is sufficient — target v3.5+
- **Email/SMS Notifications** — Different delivery channel; requires service configuration — target v3.5+
- **Notification Search/Advanced Filtering** — Over-engineering for basic system — future
- **"Quiet Hours" / Do Not Disturb** — Business hours only; no after-hours notifications expected — future

### Architecture Approach

Notification systems in ASP.NET Core MVC integrate through a service layer pattern that mirrors the existing `AuditLogService`. The architecture consists of three layers: (1) Database model (Notification entity in DbContext), (2) Service layer (NotificationService with scoped DI), and (3) Trigger points (controller actions that create notifications). Critical integration points include `ApplicationDbContext.Notifications` DbSet, service registration in `Program.cs`, controller constructor injection, and trigger points in Assessment and Coaching Proton workflows.

**Major components:**
1. **Notification Model** — Data persistence (recipient, message, read status, type, created timestamp) — communicates with ApplicationDbContext via DbSet
2. **NotificationService** — Business logic for creating, retrieving, marking notifications as read — uses ApplicationDbContext via constructor injection (scoped lifetime)
3. **Controllers** — Workflow trigger points that call NotificationService — inject NotificationService via constructor dependency injection
4. **Views (Shared/_Layout)** — Notification UI (bell icon, dropdown list, unread count) — communicates with Controller action endpoints for JSON data
5. **Background Service** (optional, future) — Scheduled deadline reminder notifications — uses NotificationService + AssessmentSession data

**Data flow:**
```
[User Action] → [Controller Action] → [NotificationService.SendAsync()]
                                         ↓
                                   [DbContext.Notifications.Add()]
                                         ↓
                                   [DbContext.SaveChangesAsync()]
                                         ↓
                                   [Notification persisted to DB]
                                         ↓
[Browser Poll] → [Controller Action: GetNotifications()] → [JSON response]
                                         ↓
                                   [View renders bell icon + list]
```

### Critical Pitfalls

Research revealed 12 critical, moderate, and minor pitfalls. Top 5 that could cause rewrites or major issues:

1. **N+1 Query Performance Degradation** — Loading notifications triggers 1 + N database queries (one initial query + one per notification for related data). Prevent with eager loading `.Include()`, `.AsNoTracking()` for read-only queries, and composite index `(UserId, IsRead, CreatedAt DESC)`. Enable EF Core logging during development to detect N+1 patterns.

2. **Notification Spam & User Fatigue** — Users receive excessive notifications (assessment reminders every hour, duplicate notifications), leading to notification blindness. Prevent with rate limiting per user per notification type (max 1 reminder/day), deduplication check before creating notification, and implementing notification consolidation for high-volume scenarios.

3. **Broken Trigger Placement - Coupling Business Logic to Notifications** — Notification code scattered throughout controllers creates tight coupling. When notification system fails, core Assessment/Coaching workflows break. Prevent by using `INotificationService` interface, MediatR for domain events, and placing notification calls in service layer NOT controllers. Always wrap notification calls in try-catch (notification failure shouldn't break workflow).

4. **Missing Notification Audit Trail** — No record of which notifications were sent to whom, when, and whether they were read. Cannot investigate "missing notification" complaints. Prevent by adding audit fields (CreatedById, SentMethod, ReadAt, DeliveryStatus), creating NotificationLog table for all send attempts, and implementing read tracking with timestamp updates.

5. **Bulk Notification Performance - Blocking Core Workflows** — When HC assigns assessment to 100 workers, assignment action takes 30+ seconds because notifications are inserted one-by-one in a loop. Prevent with `AddRange()` instead of loop with `Add()`, use EFCore.BulkExtensions for 100+ notifications, disable change tracking during bulk ops, and use background tasks (Hangfire) for bulk notifications.

## Implications for Roadmap

Based on combined research from stack, features, architecture, and pitfalls, the recommended v3.3 phase structure follows dependency order: database foundation → service layer → UI components → workflow triggers → testing & polish. This separates concerns, allows for iterative testing, and avoids breaking existing Assessment/Coaching flows.

### Phase 99: Notification Database & Service
**Rationale:** Foundation layer — cannot create service without model, and UI/trigger integration depends on stable service layer. Building UI before data layer is stable would require rework.
**Delivers:** Two new database tables (Notification + UserNotification) with proper indexes, EF Core migration, NotificationService with full CRUD operations, unit tests for service layer
**Addresses:** Database schema, NotificationService, dependency injection registration
**Avoids:** Building UI without service layer (Pitfall 3: Broken Trigger Placement), missing indexes causing N+1 queries (Pitfall 1: N+1 Query Performance)

### Phase 100: Notification Center UI
**Rationale:** User-facing layer that depends on stable service layer. Separate phase allows UI refinement without touching service logic.
**Delivers:** Bell icon in navbar with badge counter, dropdown list showing notifications, "Mark all as read" button, NotificationController with JSON endpoints, AJAX polling for unread count
**Uses:** Bootstrap Icons 1.10.0 (already loaded), Bootstrap 5 Dropdown (already in use)
**Implements:** AJAX polling every 30 seconds, deep linking to Assessment/Coaching pages, mark-as-read functionality

### Phase 101: Assessment Notification Triggers
**Rationale:** First workflow integration — simpler than Coaching (4 triggers vs 6), no approval chain complexity. Allows testing notification system with straightforward workflow before tackling complex approval chain.
**Delivers:** 4 assessment notification triggers (AssessmentAssigned, AssessmentSubmitted, AssessmentResults, DeadlineReminder), integration with AdminController and CMPController, testing of notification creation and display
**Implements:** Trigger points: AdminController.CreateAssessment, CMPController.SubmitExam, CMPController.GradeExam, background job for deadline reminders
**Avoids:** Complexity of coaching approval chain in first integration (reduces risk)

### Phase 102: Coaching Notification Triggers
**Rationale:** Second workflow integration — more complex approval chain (6 triggers, multi-stage approval: Coach → SrSpv → SectionHead → HC). Assessment phase proves system works before adding complexity.
**Delivers:** 6 coaching notification triggers (CoachAssigned, EvidenceUploaded, EvidenceRejected, EvidenceApprovedBySrSpv, EvidenceApprovedBySH, CoachingCompleted), integration with AdminController and CDPController, approval chain status tracking
**Implements:** Trigger points: AdminController.AssignCoach, CDPController.SubmitEvidence, CDPController.ApproveEvidence (multiple stages), CDPController.RejectEvidence
**Avoids:** Assessment-specific issues in earlier phase (isolates debugging)

### Phase 103: Notification Testing & Polish
**Rationale:** Testing last — requires all components (database, service, UI, triggers) to be complete. Ensures system works end-to-end and catches integration issues.
**Delivers:** Integration tests for each trigger, manual QA of all 10 notification types, performance testing with 100+ notifications per user, edge case handling (empty state, no permissions, deleted entities)
**Addresses:** Pitfall 7 (Missing Triggers — validate all triggers fire), Pitfall 12 (Breaking Existing Flows — regression test), Pitfall 1 (N+1 Queries — performance test)
**Implements:** Trigger validation checklist, load testing scripts, user acceptance testing guide

### Phase Ordering Rationale

- **Database → Service → UI → Triggers → Testing:** Follows strict dependency order. Service layer cannot exist without database model. UI cannot fetch data without service. Triggers cannot send notifications without service. Testing requires complete system.

- **Assessment before Coaching:** Assessment triggers are simpler (4 vs 6), no multi-stage approval chain, single-step state transitions. Coaching triggers require tracking approval chain status (Coach → SrSpv → SectionHead → HC) with multiple notification recipients per event. Proving system works with simpler workflow first reduces debugging complexity.

- **Separate UI phase:** Allows UI refinement (styling, UX, responsiveness) without touching service logic. If UI issues arise during integration, they're isolated from business logic.

- **Testing last:** Integration tests require all components complete. Performance testing requires realistic data volume. User acceptance testing requires working end-to-end system.

### Research Flags

**Phases likely needing deeper research during planning:**

- **Phase 99 (Database & Service):** Need to verify existing `ProtonNotification` model usage — decide between extending it (Option A: low effort) vs creating new unified `Notification` model (Option B: clean semantics). Also need to verify background job technology for deadline reminder (Hangfire vs Quartz.NET vs BackgroundService).

- **Phase 102 (Coaching Triggers):** Approval chain logic is complex — need to verify current CoachingProton workflow states (is there a DeliverableProgress table? How are approvals tracked? What are exact status transitions?). May need research on status transition patterns and multi-recipient notification routing.

**Phases with standard patterns (skip research-phase):**

- **Phase 100 (UI):** Standard Bootstrap dropdown pattern, AJAX polling is well-documented. No niche technologies or complex integration.

- **Phase 101 (Assessment Triggers):** Standard CRUD trigger placement (POST actions after entity creation). Well-established pattern from existing `AuditLogService` usage.

- **Phase 103 (Testing):** Standard ASP.NET Core integration testing patterns, performance testing with SQL Server indexes is well-documented.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Verified against existing codebase (HcPortal.csproj, _Layout.cshtml, ApplicationDbContext.cs). All dependencies already present. No new NuGet packages needed. |
| Features | MEDIUM | WebSearch found consistent patterns across multiple sources for table stakes features. PortalHC-specific triggers need verification against actual workflow code. |
| Architecture | HIGH | Direct codebase inspection confirmed existing patterns (AuditLogService, ProtonNotification, DI setup). Recommended approach matches established portal architecture. |
| Pitfalls | HIGH | Multiple technical sources agree on critical pitfalls (N+1 queries, coupling, performance). Official EF Core documentation confirms prevention strategies. |

**Overall confidence:** **HIGH** — Stack and architecture recommendations are verified against existing codebase. Features and pitfalls are cross-referenced from multiple sources. Gaps are minor (background job technology, approval chain details) and can be resolved during phase planning.

### Gaps to Address

- **Background Job Technology:** Need to decide between Hangfire, Quartz.NET, or ASP.NET Core BackgroundService for deadline reminder scheduled job. Research flags indicate Phase 99 needs deeper research here. Resolution: Evaluate during Phase 99 planning based on complexity vs compatibility with existing infrastructure.

- **Notification Model Design:** Need to verify if `ProtonNotification` model is currently in use (has existing data) or can be replaced. Decide between Option A (extend ProtonNotification) vs Option B (create unified Notification model). Resolution: Check existing ProtonNotification records in database during Phase 99, run data migration if needed.

- **Approval Chain Status Tracking:** Need to verify current CoachingProton workflow states and DeliverableProgress table structure for Phase 102. Resolution: Research flagged for Phase 102 planning — inspect CDPController code and database schema to understand approval chain.

- **Bulk Notification Performance:** Need to decide if EFCore.BulkExtensions is needed for 100+ notifications or if AddRange is sufficient. Resolution: Performance test during Phase 103 — if bulk assignment to 100 workers takes >10 seconds, add EFCore.BulkExtensions package.

- **Notification Archival:** Need to decide when to delete old notifications (30 days? 90 days? Never?). Resolution: Defer to post-v3.3 — for now, keep all notifications. Add "purge old notifications" scheduled job in future milestone if database size becomes issue.

## Sources

### Primary (HIGH confidence)
- **Direct codebase inspection** — Verified existing AuditLogService pattern, ProtonNotification model, ApplicationDbContext configuration, DI setup in Program.cs, Bootstrap Icons loading in _Layout.cshtml
- **Entity Framework Core 8.0 Documentation** — Official EF Core 8.0 docs for HasIndex, migrations, filtered indexes, AsNoTracking, Include patterns
- **ASP.NET Core MVC Documentation** — Official docs for ViewComponent pattern, scoped service lifetime, controller dependency injection
- **SQL Server Filtered Indexes Documentation** — Official documentation for `WHERE` clause indexes (index optimization for unread queries)

### Secondary (MEDIUM confidence)
- **Web Search: Notification System Database Design** (2024-2025) — Two-table pattern verified against database normalization principles from multiple technical blogs
- **Web Search: ASP.NET Core Notification Service Patterns** (2024-2025) — Service layer pattern confirmed by multiple sources, aligns with existing AuditLogService
- **Web Search: Notification System Anti-Patterns** (2024-2025) — N+1 queries, coupling, performance issues documented across multiple technical articles
- **Web Search: ASP.NET Core SignalR vs Polling** (2025) — Confirms polling is acceptable for non-real-time scenarios, SignalR over-engineering for v3.3 scope

### Tertiary (LOW confidence)
- **AWS Well-Architected Framework** — Notification reliability patterns (vendor-specific, adapted to ASP.NET Core context)
- **Notification User Experience Research** — Industry best practices for notification center design (needs validation against Indonesian user expectations)

---
*Research completed: 2026-03-05*
*Ready for roadmap: yes*
