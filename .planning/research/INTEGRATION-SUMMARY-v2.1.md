# Integration Summary: v2.1 Assessment Resilience & Real-Time Monitoring

**Project:** PortalHC Online Assessment System
**Researched:** 2026-02-24
**Status:** RESEARCH COMPLETE — Ready for roadmap synthesis

---

## What This Research Answers

**Question:** How do the 4 v2.1 features (auto-save, session resume, worker polling, HC live monitoring) integrate with the existing architecture? What new components are needed? What's the safest build order?

**Answer:** All four features leverage existing endpoints (SaveAnswer, CheckExamStatus, GetMonitorData) and require minimal refactoring. The safe build order is determined by dependencies: Phase 41 (auto-save + RowVersion) → Phase 42 (resume) → Phase 43 (polling + caching) → Phase 44 (close-early hardening) → Phase 45 (HC monitoring).

---

## Research Files Generated

| File | Purpose | Audience |
|------|---------|----------|
| **ARCHITECTURE-v2.1-FEATURES.md** | Component integration, data flows, phase dependencies | Architects, Technical Leads |
| **SUMMARY-AUTO-SAVE-RESUME-POLLING.md** | Executive summary, 10 pitfalls, phase structure | Project Leads, Risk Assessment |
| **PITFALLS-AUTO-SAVE-RESUME-POLLING.md** | Detailed pitfall analysis, prevention patterns, code examples | Developers, QA, Code Reviewers |
| **This file (INTEGRATION-SUMMARY-v2.1.md)** | Quick reference: what changed, what stays same, build order rationale | All stakeholders |

---

## Quick Reference: Integration Points

### Feature 1: Auto-Save (Phase 41)
**What changes:**
- SaveAnswer: Replace upsert pattern with ExecuteUpdateAsync (atomic)
- PackageUserResponse: Add [Timestamp] RowVersion property
- Exam.cshtml: Add debounce(300ms) on option clicks
- Migration: Add unique constraint (SessionId, QuestionId)

**What stays the same:**
- SaveAnswer signature (input/output unchanged)
- Authentication/authorization logic
- PackageUserResponses table structure (just add RowVersion column)

**Why:** Prevents duplicate database records from rapid AJAX calls

---

### Feature 2: Session Resume (Phase 42)
**What changes:**
- AssessmentSession: Add LastPageIndex, ElapsedSeconds columns
- StartExam: Load resume state into ViewBag
- New endpoint: UpdateSessionProgress (track page + elapsed time)
- Exam.cshtml: JS for page navigation tracking + timer UI
- Migration: Add 2 columns to AssessmentSessions

**What stays the same:**
- SubmitExam grading logic (still uses PackageUserResponses)
- Exam time limits (still calculated server-side)
- User authentication

**Why:** Lets workers resume exams at the exact page where they paused

---

### Feature 3: Worker Polling (Phase 43)
**What changes:**
- CheckExamStatus: Add IMemoryCache (5s TTL)
- CloseEarlySession: Add cache invalidation
- Startup.cs: Register IMemoryCache service
- Exam.cshtml: Add polling loop (10-30s interval)
- No database changes

**What stays the same:**
- CheckExamStatus signature (input/output unchanged)
- Session state data model
- HC authorization

**Why:** Reduces database load 3-6x while maintaining <5s freshness for exam close detection

---

### Feature 4: HC Live Monitoring (Phase 45)
**What changes:**
- New endpoint: GetMonitoringProgress (mirrors caching pattern)
- AssessmentMonitoringDetail.cshtml: Add Progress column + refresh JS
- No database changes (reuses existing PackageUserResponses)

**What stays the same:**
- GetMonitorData (still used for initial load)
- Assessment.cshtml layout
- HC authorization

**Why:** Real-time view of worker progress without overwhelming database

---

## Data Flow Changes

### Current Flow (Pre-v2.1)
```
User exam session → SubmitExam → Grades all at once → CompletedAt + Score
```

### New Flow (Post-v2.1)
```
User clicks option
  ├─ Auto-save to PackageUserResponse (Phase 41)
  ├─ Track page + elapsed time (Phase 42)
  ├─ Poll for exam close (Phase 43)
  ├─ HC monitoring sees progress (Phase 45)
  │
  [... exam continues ...]
  │
  User clicks Submit → SubmitExam → Grades from populated PackageUserResponses
```

**Key insight:** Auto-save doesn't change grading logic. SubmitExam still grades the same way; auto-save just populates the answer data incrementally.

---

## Build Order & Dependencies

### Critical Path (Cannot Skip)
```
Phase 41: Auto-Save + RowVersion
    ↓
Phase 42: Resume (depends on RowVersion)
    ↓
Phase 43: Polling + Caching (depends on stable session state)
    ↓
Phase 45: HC Monitoring (reuses caching)
```

### Optional Branch
```
Phase 41 ⟶ Phase 44: Close Early Hardening
    (Hardens existing feature; doesn't block other features)
```

### Why This Order?

**Phase 41 first:**
- Foundation for everything else
- Race condition must be fixed immediately
- RowVersion needed by later phases

**Phase 42 next:**
- Depends on RowVersion from Phase 41
- Independent of polling (no circular deps)
- Enables time-based features

**Phase 43 after:**
- Needs stable session state (Phase 42)
- Introduces caching used by Phase 45
- Highest DB load concern

**Phase 44 optional:**
- Hardens existing Close Early (Phase 39)
- Can defer without blocking other features
- Requires Phase 41 + 43 ready

**Phase 45 last:**
- Lowest priority (nice-to-have)
- Reuses Phase 43 caching infrastructure
- Can be delayed

---

## Schema Changes Summary

### New Columns Required

| Table | Column | Type | Phase | Size |
|-------|--------|------|-------|------|
| AssessmentSessions | RowVersion | byte[] | 41 | +8 bytes |
| AssessmentSessions | LastPageIndex | int | 42 | +4 bytes |
| AssessmentSessions | ElapsedSeconds | int | 42 | +4 bytes |
| PackageUserResponses | RowVersion | byte[] | 41 | +8 bytes |

### New Constraints Required

| Table | Constraint | Phase |
|-------|-----------|-------|
| PackageUserResponses | UNIQUE(SessionId, QuestionId) | 41 |

**Storage impact:** ~16 bytes per session + 8 bytes per response. Negligible at scale.

---

## Code Changes Summary

### New Endpoints

| Endpoint | Phase | Input | Output | Purpose |
|----------|-------|-------|--------|---------|
| UpdateSessionProgress | 42 | sessionId, pageIndex, elapsedSeconds | { success } | Track page + time |
| GetMonitoringProgress | 45 | title, category, scheduleDate | [ { userId, answeredCount, totalQuestions, ... } ] | HC progress |

### Enhanced Endpoints

| Endpoint | Phase | Change |
|----------|-------|--------|
| SaveAnswer | 41 | ExecuteUpdateAsync (atomic) + debounce-safe |
| StartExam | 42 | Populate ViewBag for resume state |
| CheckExamStatus | 43 | Add IMemoryCache (5s TTL) |
| CloseEarlySession | 43 | Add cache invalidation |

### JavaScript Additions

| Component | Phase | Lines | Features |
|-----------|-------|-------|----------|
| Debounce | 41 | +20 | Throttle auto-save AJAX |
| Page Tracking | 42 | +20 | Track navigation + elapsed time |
| Polling Loop | 43 | +15 | Detect exam close |
| Monitoring Refresh | 45 | +25 | Auto-update progress table |

---

## Risk Assessment

### Highest Risks

**Phase 41: Race Condition Persistence**
- Risk: ExecuteUpdateAsync not used correctly; duplicates still created
- Mitigation: Unit test + load test with 100 rapid clicks
- Detection: Query DB for duplicate (SessionId, QuestionId) pairs

**Phase 42: Timer Manipulation**
- Risk: Client timer can be hacked; wrong remaining time displayed
- Mitigation: Always use DateTime.UtcNow on server; client timer decorative only
- Detection: Compare server elapsed vs. actual wall-clock time

**Phase 43: Cache Stale After HC Action**
- Risk: Polling shows "open" after HC closes exam
- Mitigation: Clear cache in CloseEarlySession
- Detection: Close exam, poll immediately; should show closed

**Phase 44: Double-Submit Race**
- Risk: Worker submits at exact moment HC closes; data lost
- Mitigation: RowVersion with DbUpdateConcurrencyException
- Detection: Test harness sends simultaneous requests; verify no score loss

**Phase 45: Progress Count Wrong**
- Risk: Count includes unanswered questions; inflates progress
- Mitigation: WHERE PackageOptionId IS NOT NULL in query
- Detection: Count answers manually, compare with query

### Medium Risks

**Question Count Change Mid-Exam**
- Risk: Worker on page 3 of 5; HC deletes question; resume tries page 3 of 4
- Mitigation: Validate question count unchanged in UpdateSessionProgress
- Detection: Add question mid-exam, try resume; should be blocked

---

## Load Testing Strategy

### Phase 41: Auto-Save Stress Test
```
Setup: 1 worker, 1 question, 1000 rapid clicks (simulate double-click + key repeat)
Measure: DB for duplicate (SessionId, QuestionId) pairs
Target: 0 duplicates
Pass: Query `SELECT COUNT(*) FROM PackageUserResponses
        WHERE SessionId = 123 AND QuestionId = 456` = 1
```

### Phase 43: Polling Load Test
```
Setup: 100 workers in simultaneous exams, polling every 10 seconds for 10 minutes
Measure: DB CPU %, query latency, connection pool usage
Target: DB CPU <30%, latency <100ms, connection pool <70%
Pass: Monitoring shows cache hit rate >80%
```

### Phase 44: Concurrency Test
```
Setup: Worker submits at T=3599s, HC closes at T=3600s (simultaneous)
Measure: Final score, CompletedAt timestamp, no data loss
Target: Both requests succeed, one consistent final state
Pass: Competency records created once, no duplicates
```

---

## Configuration Checkpoints

### Startup.cs (Phase 43)
```csharp
services.AddMemoryCache();
```

### Exam.cshtml (Phase 41)
```javascript
const DEBOUNCE_MS = 300;  // Adjust per testing
```

### Exam.cshtml (Phase 43)
```javascript
const POLLING_INTERVAL_MS = 10000;  // 10 seconds
```

### Exam.cshtml (Phase 45)
```javascript
const MONITORING_REFRESH_MS = 5000;  // 5 seconds
```

---

## Confidence Levels

| Area | Confidence | Why |
|------|-----------|-----|
| Auto-save patterns | HIGH | ExecuteUpdateAsync + RowVersion are proven EF Core patterns |
| Resume logic | HIGH | Server-side time calculation already partially implemented |
| Polling + caching | HIGH | IMemoryCache + 5s TTL standard approach for status endpoints |
| Close Early safety | HIGH | RowVersion + DbUpdateConcurrencyException documented |
| HC monitoring | MEDIUM-HIGH | Query patterns are standard; progress count definition needs validation |
| Integration with existing code | MEDIUM | Monolithic CMPController (2700 lines) requires careful modification |

---

## Gaps Requiring Phase-Specific Research

1. **Phase 41:**
   - How to integrate debounce without breaking current Exam.cshtml functionality
   - Migration strategy for unique constraint (handle historic duplicates if any exist)

2. **Phase 42:**
   - Pause/resume UX (should page show "paused" state? how long can pause last?)
   - Backward compatibility with sessions that started before Phase 42

3. **Phase 43:**
   - Cache invalidation on other state changes (e.g., if worker's role changes mid-exam)
   - Interaction with 2-minute grace period on submission

4. **Phase 44:**
   - Ensure Close Early uses same grading logic as SubmitExam (scores must match)
   - Competency level updates (same as SubmitExam or different?)

5. **Phase 45:**
   - Progress metric definition (answered % vs completion %?)
   - HC monitoring refresh rate vs worker polling rate

---

## Key Decisions Made in Research

### Decision 1: No Separate API Layer
**Considered:** Create new AssessmentApi controller
**Decided:** Enhance existing CMPController methods
**Rationale:** Minimal refactoring, lower risk, easier to audit; monolithic code is already in place

### Decision 2: Atomic Upsert Pattern
**Considered:** Delete old + Insert new (simpler logic)
**Decided:** ExecuteUpdateAsync with WHERE clause
**Rationale:** Single transaction, no race condition between delete and insert, audit trail preserved

### Decision 3: In-Process Caching (IMemoryCache)
**Considered:** External Redis cache
**Decided:** IMemoryCache
**Rationale:** Single-server deployment, <1ms latency, no operational overhead; can migrate to Redis later if scaling

### Decision 4: Server-Side Time Authority
**Considered:** Trust client timer + validation on server
**Decided:** Client timer decorative; server always calculates from StartedAt
**Rationale:** DevTools can manipulate client timer; server clock is authoritative

### Decision 5: 5-Second Cache TTL
**Considered:** 10s, 1s, dynamic TTL
**Decided:** Fixed 5 seconds
**Rationale:** Polling every 10-30s; 5s cache = 2-6 hits per interval; acceptable freshness for exam close; proven effective

---

## Roadmap Implications

### Phase Naming Recommendation
1. **Phase 41:** SaveAnswer Race Condition Prevention & Auto-Save Foundation
2. **Phase 42:** Session Resume with Server-Side Elapsed Time Tracking
3. **Phase 43:** Worker Polling & Caching Infrastructure
4. **Phase 44:** Close Early Concurrency Control (optional)
5. **Phase 45:** HC Live Monitoring Dashboard Enhancement

### Estimated Effort

| Phase | Frontend | Backend | Database | Testing | Total |
|-------|----------|---------|----------|---------|-------|
| 41 | 1-2 days | 2-3 days | 1 day | 2-3 days | 1 week |
| 42 | 2-3 days | 2-3 days | 1 day | 2-3 days | 1.5 weeks |
| 43 | 1-2 days | 2-3 days | 0 days | 2-3 days | 1 week |
| 44 | 0 days | 1-2 days | 0 days | 2-3 days | 4-5 days |
| 45 | 2-3 days | 2-3 days | 0 days | 1-2 days | 1 week |

**Total:** 6-7 weeks (or 4 weeks if Phase 44 deferred)

---

## Reference Architecture Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                      v2.1 Integration Architecture           │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     Browser (Exam.cshtml)                    │
│  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐        │
│  │ Auto-Save    │  │Page Tracking│  │Polling Loop  │        │
│  │(Phase 41)    │  │(Phase 42)   │  │(Phase 43)    │        │
│  │Debounce 300ms│  │AJAX on Nav  │  │Check 10-30s  │        │
│  └──────┬───────┘  └──────┬──────┘  └──────┬───────┘        │
│         │                 │                │                  │
│         └─────────────────┼────────────────┘                  │
│                          │                                     │
│                    AJAX POST/GET                              │
│                          │                                     │
└──────────────────────────┼──────────────────────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
    ┌────▼─────┐    ┌──────▼──────┐   ┌────▼───────┐
    │SaveAnswer │    │CheckExamStat│   │GetMonitoring│
    │(Enhanced) │    │(Cached)     │   │Progress (NEW)│
    │Atomic     │    │5s TTL       │   │5-10s TTL     │
    │Upsert     │    │Phase 43     │   │Phase 45      │
    └────┬──────┘    └──────┬──────┘   └────┬────────┘
         │                  │               │
    ┌────▼──────────────────┼───────────────┴──────┐
    │                       │                      │
    │        CMPController  │  (2700+ lines)       │
    │                       │                      │
    │  ┌──────────────────┐ │ ┌─────────────────┐ │
    │  │StartExam         │ │ │CloseEarlySession│ │
    │  │(Resume logic)    │ │ │(Cache invalid)  │ │
    │  │Phase 42          │ │ │Phase 43         │ │
    │  └──────────────────┘ │ └─────────────────┘ │
    │                       │                      │
    │  ┌──────────────────┐ │ ┌─────────────────┐ │
    │  │UpdateSessionProg │ │ │SubmitExam       │ │
    │  │(NEW, Phase 42)   │ │ │(Unchanged)      │ │
    │  └──────────────────┘ │ └─────────────────┘ │
    │                       │                      │
    └───────────────────────┼──────────────────────┘
                            │
                    ┌───────▼──────────┐
                    │IMemoryCache      │
                    │5-10s TTL         │
                    │(Phase 43)        │
                    └───────┬──────────┘
                            │
                    ┌───────▼──────────────────┐
                    │ApplicationDbContext      │
                    │(EF Core)                 │
                    └───────┬──────────────────┘
                            │
        ┌───────────────────┼────────────────────┐
        │                   │                    │
    ┌───▼─────────┐   ┌────▼──────────┐   ┌───▼──────────┐
    │AssessmentS. │   │PackageUserResp│   │Other Tables  │
    │+ RowVersion │   │+ RowVersion    │   │              │
    │+ LastPageIdx│   │+ UNIQUE constr.│   │              │
    │+ ElapsedSecs│   │(Phase 41)      │   │              │
    │(Phase 42)   │   │                │   │              │
    └─────────────┘   └────────────────┘   └──────────────┘
```

---

## Summary: What Was Researched

### ✅ Completed
1. **Feature integration:** How auto-save, resume, polling, HC monitoring integrate with existing code
2. **Components:** Identified all new endpoints, model changes, JavaScript additions
3. **Data flows:** Mapped current vs. new data flow for exam sessions
4. **Dependencies:** Clear phase ordering with hard/soft blockers
5. **Patterns:** Atomic upsert, server-side time, caching, RowVersion concurrency
6. **Pitfalls:** 10 documented pitfalls from previous research with prevention strategies
7. **Load testing:** Scenarios for each phase to surface issues
8. **Risk assessment:** Critical risks with mitigation strategies
9. **Effort estimation:** 6-7 weeks (4 weeks if Phase 44 deferred)

### ❓ Requiring Phase-Specific Research
1. **Phase 41:** Debounce integration, unique constraint migration strategy
2. **Phase 42:** Pause/resume UX, backward compatibility
3. **Phase 43:** Cache invalidation scope, grace period interaction
4. **Phase 44:** Grading logic consistency, competency level updates
5. **Phase 45:** Progress metric definition, refresh rate tuning

---

## How to Use This Research

**For Project Leads:** Read SUMMARY-AUTO-SAVE-RESUME-POLLING.md (phase structure, risks, load testing)

**For Architects:** Read ARCHITECTURE-v2.1-FEATURES.md (component integration, data flows, patterns)

**For Developers:** Read PITFALLS-AUTO-SAVE-RESUME-POLLING.md (detailed patterns, code examples, verification checklist)

**For QA/Testers:** Use load testing scenarios in SUMMARY and verification checklists in PITFALLS

**For Decision Makers:** This integration summary provides quick reference on build order, risks, and effort

---

## Final Recommendation

**Proceed with v2.1 phases in order: 41 → 42 → 43 → 44 (optional) → 45**

The architecture is sound. All pitfalls have concrete preventions. Risk is manageable with load testing at each phase. Estimated 6-7 weeks to completion (or 4 weeks without Phase 44).

No architectural refactoring needed; all changes are enhancements to existing endpoints. This minimizes risk while delivering all four features.

---

## Files Reference

- **ARCHITECTURE-v2.1-FEATURES.md** — Full architecture integration details
- **SUMMARY-AUTO-SAVE-RESUME-POLLING.md** — Executive summary, phases, risks, load testing
- **PITFALLS-AUTO-SAVE-RESUME-POLLING.md** — 10 pitfalls, prevention patterns, code examples
- **Previous research:** ARCHITECTURE.md, FEATURES.md, PITFALLS.md, STACK.md (codebase overview)
