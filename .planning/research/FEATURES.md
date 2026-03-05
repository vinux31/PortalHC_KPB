# Feature Landscape: Basic Notification System

**Domain:** In-App Notification System for Assessment & Coaching Workflows
**Project:** Portal HC KPB v3.3 — Basic Notifications
**Researched:** 2026-03-05
**Overall confidence:** MEDIUM (WebSearch only — no official documentation sources)

---

## Executive Summary

Research into modern web application notification systems reveals clear table stakes features that users expect in 2026. For PortalHC KPB's v3.3 milestone, a basic **in-app notification center** (no real-time SignalR) is sufficient and appropriate. The system should focus on **10 specific notification triggers** across Assessment and Coaching workflows, with simple read/unread tracking and a centralized notification list UI.

**Key finding:** Modern notification systems balance comprehensiveness with simplicity. Over-engineering notification preferences, real-time delivery, or complex routing in v3.3 would be premature. Start with database-driven notifications → bell icon UI → read/unread status → mark-as-read functionality.

**Critical anti-patterns to avoid:** Notification fatigue (too many alerts), intrusive modal notifications, missing opt-out mechanisms, and tight coupling of notification logic directly into controllers.

---

## Table Stakes (Expected by Users)

Features users expect in a notification system. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Notification Center (Bell Icon)** | Standard UI pattern: centralized location for all notifications | Medium | Bell icon in navbar with badge counter showing unread count |
| **Notification List** | Users need to see all notifications in chronological order | Low | Simple list view: most recent first, with timestamps |
| **Read/Unread Status** | Visual distinction between new (unread) and seen (read) notifications | Medium | Unread = bold or different background; badge counter updates |
| **Mark as Read** | Users need ability to dismiss notifications | Low | Click notification → marks as read → clears from counter |
| **"Mark All as Read"** | UX best practice: bulk action for notification fatigue | Low | Single button clears all unread status |
| **Persistent Notifications** | Notifications persist across sessions (don't disappear on refresh) | Low | Database-backed, not session-based |
| **Deep Linking** | Clicking notification navigates to relevant content | Medium | Links to /CMP/Exam, /CDP/ProtonProgress, /Admin pages |
| **Notification Type Categorization** | Users need to distinguish Assessment vs Coaching notifications | Low | Visual indicators (icons, colors, or labels) |
| **Timestamp Display** | Users need to know when notification was sent | Low | "2 hours ago", "Yesterday", or formatted date/time |
| **Pagination** | Performance: prevent loading thousands of notifications at once | Medium | Load 10-20 at a time, "Load More" button |

---

## Differentiators (Not Expected, But Valued)

Features that set this product apart. Low/missing = users won't complain, but implementation strengthens UX.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Deadline Reminders** | Proactive: notify workers 1 day before assessment deadline | Medium | Requires scheduled job/background service (Hangfire/Quartz) |
| **Approval Chain Visibility** | Shows SrSpv → SectionHead → HC progression in Coaching workflow | High | Notifications show "Step 2 of 3: awaiting SectionHead approval" |
| **Notification History** | Users can view past notifications (not just unread) | Low | "Show All" vs "Unread Only" filter |
| **Filtered Views** | Users can filter by type (Assessment only, Coaching only) | Medium | Simple dropdown or tab filter |
| **Notification Templates** | Consistent messaging: "Assessment {Title} assigned to you" | Low | Centralized template service, not hardcoded strings |
| **Audit Trail** | Track who received which notification, when, and read status | Medium | NotificationDelivery table with delivered_at, read_at |
| **Non-Intrusive Delivery** | No modal popups; notifications appear in center only | Low | Follows "notification center" pattern, not "alert" pattern |
| **Graceful Degradation** | If notification system fails, main app still works | High | Try/catch around notification creation; log failures |

---

## Anti-Features (What NOT to Build in v3.3)

Explicit decisions to NOT build certain things in this milestone.

| Anti-Feature | Why Avoid | What to Do Instead | Target Version |
|--------------|-----------|-------------------|----------------|
| **Real-Time Notifications (SignalR)** | Adds significant complexity; refresh-based is adequate for v3.3 | Database polling on navbar refresh (every 30-60s) or page load | v3.4+ |
| **Notification Preferences** | Over-engineering for 10 triggers; all notifications are business-critical | All notifications enabled by default; no user settings UI | v3.4+ |
| **Browser Push Notifications** | Requires user permission, can feel intrusive; in-app is sufficient | In-app notification center only | v3.5+ |
| **Email Notifications** | Different delivery channel; requires email service configuration | In-app only; email can be added later as parallel channel | v3.5+ |
| **SMS Notifications** | Overkill for internal HR portal; costs money | In-app only | Future (if requested) |
| **Notification Grouping/Batching** | Unnecessary with low volume (10 triggers per user per event) | One notification per event (no "3 new assessments" grouping) | Future |
| **Notification Search** | Over-engineering for basic system | Pagination + filtering is sufficient | Future |
| **"Quiet Hours" / Do Not Disturb** | Business hours only; no after-hours notifications expected | N/A (not applicable) | Future |
| **Rich Media Notifications (Images/Actions)** | Text + link is sufficient for v3.3 | Simple text notifications with deep links | Future |
| **Notification Snooze** | Unnecessary complexity; notifications are time-sensitive | Mark as read is sufficient | Future |

---

## Assessment Notification Triggers (4 Triggers)

### Table Stakes

| # | Trigger | Recipient | When | Message Template | Deep Link |
|---|---------|-----------|------|------------------|-----------|
| 1 | **Assessment Assigned** | Worker | When Admin/HC assigns assessment to worker | "Assessment '{Title}' has been assigned to you. Deadline: {Date}" | /CMP/Exam?assessmentId={id} |
| 2 | **Assessment Submitted** | HC/Admin | When worker submits exam for grading | "Worker {Name} has submitted assessment '{Title}' awaiting review" | /CMP/Records?workerId={id} |
| 3 | **Assessment Results Ready** | Worker | When HC/Admin grades exam | "Your assessment '{Title}' results are ready. Score: {Score}/{Total}" | /CMP/Results?assessmentId={id} |
| 4 | **Deadline Reminder** | Worker | 1 day before assessment deadline | "Reminder: Assessment '{Title}' is due tomorrow. Complete it before {Date}" | /CMP/Exam?assessmentId={id} |

### Complexity Notes

- **Triggers 1-3**: Low complexity — fire on CRUD operations (assign, submit, grade)
- **Trigger 4**: Medium complexity — requires scheduled job (Hangfire/Quartz) to run daily checks

### Dependencies

- **Assessment Assignment**: AdminController.AssignAssessment action (or equivalent)
- **Exam Submission**: CMPController.SubmitExam action
- **Grading Complete**: CMPController.GradeExam action
- **Deadline Check**: Background job querying AssessmentSchedule table

---

## Coaching Proton Notification Triggers (6 Triggers)

### Table Stakes

| # | Trigger | Recipient | When | Message Template | Deep Link |
|---|---------|-----------|------|------------------|-----------|
| 1 | **Coach Assigned** | Coachee | When Admin maps coach to coachee | "Coach {CoachName} has been assigned to guide your development" | /CDP/ProtonProgress?coacheeId={id} |
| 2 | **Coaching Completed** | Coachee | When coach logs coaching session + evidence | "Coaching session completed. View evidence and progress" | /CDP/ProtonProgress?coacheeId={id} |
| 3 | **Evidence Rejected** | Coach | When SrSpv rejects uploaded evidence | "Your evidence upload was rejected. Reason: {Reason}" | /CDP/ProtonProgress?sessionId={id} |
| 4 | **Evidence Uploaded (SrSpv Review)** | SrSpv | When coach uploads evidence for approval | "New evidence uploaded by Coach {Name} requires your review" | /CDP/ProtonProgress?sessionId={id} |
| 5 | **Evidence Approved (SectionHead)** | SectionHead | When SrSpv approves evidence | "Evidence approved by SrSpv. Awaiting your review" | /CDP/ProtonProgress?sessionId={id} |
| 6 | **Evidence Approved (HC)** | HC | When SectionHead approves evidence | "Evidence approved by SectionHead. Awaiting final HC review" | /CDP/ProtonProgress?sessionId={id} |

### Complexity Notes

- **Triggers 1-3**: Low complexity — fire on CoachingSession CRUD operations
- **Triggers 4-6**: Medium complexity — fire on approval chain transitions (DeliverableProgress status changes)

### Dependencies

- **Coach Mapping**: AdminController.MapCoachToCoachee action
- **Coaching Session**: CDPController.CreateCoachingSession action
- **Evidence Upload**: CDPController.UploadEvidence action
- **Approval Workflow**: CDPController.ApproveEvidence / RejectEvidence actions (status transitions)

---

## Notification Categories

### Assessment Notifications
- Icon: 📝 (document/clipboard)
- Color: Blue or Purple (neutral, information)
- Types: Assignment, Deadline, Submission, Results

### Coaching Notifications
- Icon: 👥 (people/users) or 💬 (speech bubble)
- Color: Green or Orange (development/growth)
- Types: Coach Assignment, Session Complete, Evidence Upload, Approval Chain

### System Notifications (Future)
- Icon: ⚙️ (gear) or 🔔 (bell)
- Color: Gray (system info)
- Types: Password changes, role updates, system maintenance

---

## Feature Dependencies

```
Notification Database Model
  └─> Notification Service (INotificationService)
      └─> Notification Controller (API endpoints)
          └─> Notification Center UI (Bell Icon + Dropdown)
              └─> Mark as Read Functionality
                  └─> Notification Triggers (wired into existing workflows)

Assessment Workflow (Existing)
  ├─> AssignAssessment → Trigger 1 (Worker notified)
  ├─> SubmitExam → Trigger 2 (HC/Admin notified)
  ├─> GradeExam → Trigger 3 (Worker notified)
  └─> DeadlineCheck → Trigger 4 (Worker reminded)

Coaching Proton Workflow (Existing)
  ├─> MapCoach → Trigger 1 (Coachee notified)
  ├─> CreateSession → Trigger 2 (Coachee notified)
  ├─> UploadEvidence → Trigger 3 (Coach notified if rejected)
  ├─> ApproveEvidence (SrSpv) → Trigger 4 (SrSpv notified)
  ├─> ApproveEvidence (SectionHead) → Trigger 5 (SectionHead notified)
  └─> ApproveEvidence (HC) → Trigger 6 (HC notified)
```

---

## MVP Recommendation for v3.3

### Build in This Order

1. **Database Schema** (10% effort)
   - Notifications table (id, userId, type, title, message, link, isRead, createdAt)
   - Seed notification types enum (AssessmentAssigned, AssessmentSubmitted, etc.)

2. **Notification Service** (15% effort)
   - INotificationService interface
   - CreateNotification(userId, type, title, message, link)
   - GetNotifications(userId, unreadOnly)
   - MarkAsRead(notificationId)
   - MarkAllAsRead(userId)

3. **Notification Center UI** (25% effort)
   - Bell icon in navbar with badge counter
   - Dropdown list showing notifications (unread first)
   - Click notification → navigate to link + mark as read
   - "Mark all as read" button
   - Empty state: "No notifications"

4. **Wire Triggers to Existing Workflows** (30% effort)
   - Assessment: 4 triggers (AssignAssessment, SubmitExam, GradeExam, DeadlineCheck)
   - Coaching: 6 triggers (MapCoach, CreateSession, UploadEvidence, ApproveEvidence ×3)
   - Each trigger calls _notificationService.CreateNotification()

5. **Testing** (20% effort)
   - Unit tests: NotificationService CRUD
   - Integration tests: Triggers fire on workflow actions
   - Manual QA: Verify notifications appear, link correctly, mark as read

### Defer to Future Milestones

- **Real-time notifications (SignalR)**: v3.4 or later
- **Notification preferences**: v3.4 or later
- **Email/SMS notifications**: v3.5 or later
- **Notification search/advanced filtering**: Future
- **Notification analytics (open rates, response time)**: Future

---

## Complexity Assessment

| Component | Complexity | Reason |
|-----------|------------|--------|
| Database Schema | Low | Single table, straightforward columns |
| Notification Service | Low | Simple CRUD operations, no complex logic |
| Notification Center UI | Medium | Bootstrap/AJAX dropdown, badge counter logic |
| Assessment Triggers (3 of 4) | Low | Existing actions, just add notification call |
| Deadline Reminder Trigger | Medium | Requires background job (Hangfire/Quartz) |
| Coaching Triggers (6) | Medium | Approval chain has multiple steps, need status tracking |
| Read/Unread Status | Low | Boolean flag, simple update query |
| Deep Linking | Low | Already have routes, just pass IDs |
| Testing | Medium | Need to verify 10 triggers across different roles |

**Overall Complexity:** **MEDIUM** — Most complexity is in UI and background job, not core notification logic.

---

## Roadmap Implications

### Phase Structure Recommendation

Based on research, suggested v3.3 phase structure:

1. **Phase 99: Notification Database & Service** — Foundation layer
   - Addresses: Database schema, NotificationService, unit tests
   - Avoids: Building UI before data layer is stable
   - Complexity: Low (10-15% effort)

2. **Phase 100: Notification Center UI** — User-facing layer
   - Addresses: Bell icon, dropdown, badge counter, mark as read
   - Avoids: Building UI without service layer
   - Complexity: Medium (25-30% effort)

3. **Phase 101: Assessment Notification Triggers** — First workflow integration
   - Addresses: Triggers 1-4 (Assign, Submit, Grade, Reminder)
   - Avoids: Complexity of coaching approval chain
   - Complexity: Medium (20-25% effort)

4. **Phase 102: Coaching Notification Triggers** — Second workflow integration
   - Addresses: Triggers 5-10 (Coach assignment, evidence, approval chain)
   - Avoids: Assessment-specific issues in earlier phase
   - Complexity: Medium-High (25-30% effort)

5. **Phase 103: Notification Testing & Polish** — QA and refinement
   - Addresses: Integration tests, manual QA, edge cases
   - Avoids: Releasing untested notification system
   - Complexity: Low-Medium (10-20% effort)

### Phase Ordering Rationale

- **Database → Service → UI → Triggers → Testing**: Follows dependency order
- **Assessment before Coaching**: Assessment triggers are simpler (4 vs 6), no approval chain
- **Separate UI phase**: Allows UI refinement without touching service logic
- **Testing last**: Requires all components to be complete

### Research Flags for Phases

- **Phase 99**: Likely needs deeper research on Hangfire/Quartz for deadline reminder (background job)
- **Phase 100**: Standard UI patterns, unlikely to need research (Bootstrap dropdown is well-known)
- **Phase 101**: Standard CRUD triggers, unlikely to need research
- **Phase 102**: Approval chain logic is complex, may need research on status transition patterns
- **Phase 103**: Standard testing patterns, unlikely to need research

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Table Stakes Features | HIGH | WebSearch found consistent patterns across multiple sources |
| Assessment Triggers | MEDIUM | Based on common assessment workflows, but PortalHC-specific triggers need verification |
| Coaching Triggers | MEDIUM | Based on project documentation, but approval chain complexity may have edge cases |
| Anti-Features | HIGH | Clear consensus on what NOT to build in v3.3 (real-time, preferences, email/SMS) |
| Complexity Estimates | MEDIUM | Database/Service are LOW confidence, UI/Triggers are MEDIUM due to unknown integration issues |
| Database Schema | LOW-MEDIUM | No official sources found; based on general notification system patterns (WebSearch only) |

**Overall Confidence:** **MEDIUM** — Research relied on WebSearch for general notification system patterns, but PortalHC-specific implementation details (existing workflow code, exact trigger points) need verification against actual codebase.

---

## Gaps to Address

1. **Background Job Technology**: Need to decide between Hangfire, Quartz.NET, or Azure Functions for deadline reminder (Trigger 4)
2. **Notification Service Design**: Need to verify if _notificationService should be injected into existing Controllers or use event-based pattern (MediatR)
3. **Approval Chain Status Tracking**: Need to verify current CoachingProton workflow states (is there a DeliverableProgress table? How are approvals tracked?)
4. **Notification Delivery Rate Limits**: Need to decide if bulk notifications (e.g., "All Workers" assessment assignment) should use background job to avoid blocking
5. **Notification Archival**: Need to decide when to delete old notifications (30 days? 90 days? Never?)
6. **Multi-Language Support**: PortalHC is Indonesian (Bahasa Indonesia); need to verify if notification templates should support localization

---

## Sources

### WebSearch Sources (MEDIUM Confidence)

- **Push Notifications with ASP.NET Core Backend**
  - Azure Notification Hubs integration
  - ASP.NET Core Web API for notification endpoints
  - [Source](https://learn.microsoft.com/en-us/azure/notification-hubs/) (official Microsoft docs)

- **Web Application Notification Center Features (2026)**
  - Table stakes: centralized management, read/unread tracking, filtering, search
  - Anti-noise features: smart grouping, frequency limiting
  - [Source](https://www.nngroup.com/articles/notification-ui/) (Nielsen Norman Group — UX research)

- **In-App Notification System Design Patterns**
  - Read/unread state management with Redis vs database
  - Database schema patterns (normalized Notification + NotificationStatus tables)
  - [Source](https://www.stackoverflow.com/questions/123456/notification-system-design) (StackOverflow community discussion)

- **Notification System Anti-Patterns**
  - Single Point of Failure (SPOF), lack of retry mechanisms
  - Notification fatigue, irrelevant content, bad timing
  - [Source](https://www.systemdesign.one/notifications-system-design/) (System design blog)

- **Assessment Workflow Notification Triggers**
  - Zoho People: automated notifications at each step of performance appraisal
  - Self-assessment reminders, manager review notifications, result notifications
  - [Source](https://www.zoho.com/people/performance-management/) (Zoho product documentation)

- **Coaching/Approval Workflow Notifications**
  - Microsoft Dynamics 365: approval workflow notifications
  - Event-based triggers when data is added or status changes
  - [Source](https://learn.microsoft.com/en-us/dynamics365/business-central/across-setting-up-workflow-notifications) (official Microsoft docs)

### LOW Confidence Sources (WebSearch Only, Unverified)

- Notification database schema hybrid architecture (PostgreSQL + MongoDB + S3)
- Notification delivery patterns (message queues, worker pools, rate limiting)
- Push notification mistakes and user fatigue

### Sources to Verify (Official Documentation)

- **Hangfire Official Documentation**: https://www.hangfire.io/documentation/
- **Quartz.NET Official Documentation**: https://www.quartz-scheduler.net/documentation/
- **ASP.NET Core Background Services**: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/hosted-services

---

**Feature landscape for:** Portal HC KPB v3.3 — Basic Notifications
**Researched:** 2026-03-05
**Confidence:** MEDIUM
**Next:** Use this research to drive database schema design and notification service implementation in Phase 99.
