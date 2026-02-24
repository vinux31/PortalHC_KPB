# v2.1 Assessment Resilience & Real-Time Monitoring — Research Index

**Project:** PortalHC Online Assessment System
**Researched:** 2026-02-24
**Status:** RESEARCH COMPLETE

---

## Quick Navigation

**Start here based on your role:**

- **Project Lead / Product Manager** → [INTEGRATION-SUMMARY-v2.1.md](./INTEGRATION-SUMMARY-v2.1.md) (10 min read)
- **Architect / Tech Lead** → [ARCHITECTURE-v2.1-FEATURES.md](./ARCHITECTURE-v2.1-FEATURES.md) (20 min read)
- **Developer** → [PITFALLS-AUTO-SAVE-RESUME-POLLING.md](./PITFALLS-AUTO-SAVE-RESUME-POLLING.md) (30 min read)
- **QA / Tester** → [SUMMARY-AUTO-SAVE-RESUME-POLLING.md](./SUMMARY-AUTO-SAVE-RESUME-POLLING.md) (sections: "Risk Assessment" + "Load Testing")
- **Decision Maker** → This index + INTEGRATION-SUMMARY-v2.1.md (5-10 min)

---

## The Four Features in v2.1

### Feature 1: Auto-Save (Phase 41)
**What:** JavaScript auto-saves user's answer selection as they click options
**Why:** Prevents data loss if browser crashes; enables resume functionality
**Integration:** Enhance existing SaveAnswer endpoint with atomic upsert + debounce
**Complexity:** Medium | **Risk:** High (race conditions)
**Timeline:** 1 week

### Feature 2: Session Resume (Phase 42)
**What:** System remembers where worker paused and how much time elapsed
**Why:** Enables pausing and resuming exams without losing progress
**Integration:** Add LastPageIndex + ElapsedSeconds to AssessmentSession; new UpdateSessionProgress endpoint
**Complexity:** Medium-High | **Risk:** High (timer security)
**Timeline:** 1.5 weeks

### Feature 3: Worker Polling (Phase 43)
**What:** Worker's exam page polls every 10-30s to detect if HC closes exam early
**Why:** Early-close detection without page refresh; better UX than polling alone
**Integration:** Add IMemoryCache to CheckExamStatus; cache invalidation in CloseEarlySession
**Complexity:** Medium | **Risk:** Medium (cache staleness)
**Timeline:** 1 week

### Feature 4: HC Live Monitoring (Phase 45)
**What:** HC's monitoring dashboard shows real-time worker progress (# answers submitted)
**Why:** HC can monitor exam progress without waiting for submission
**Integration:** New GetMonitoringProgress endpoint; auto-refresh monitoring table
**Complexity:** Medium | **Risk:** Low
**Timeline:** 1 week

---

## Research Deliverables

### Primary Documents (Read These)

#### 1. INTEGRATION-SUMMARY-v2.1.md
**Purpose:** Quick reference for all stakeholders
**Length:** ~400 lines
**Contains:**
- What changes vs. what stays the same (per feature)
- Build order & dependencies
- Risk assessment summary
- Configuration checkpoints
- Gaps requiring phase-specific research

**When to read:** First document; answers "what's changing and why?"

---

#### 2. ARCHITECTURE-v2.1-FEATURES.md
**Purpose:** Detailed technical architecture
**Length:** ~1200 lines
**Contains:**
- Current architecture overview
- Feature integration points (deep dive)
- Data flow changes (diagrams)
- Recommended build order with rationale
- Architectural patterns & anti-patterns
- Scalability analysis
- Implementation checklist per phase

**When to read:** After INTEGRATION-SUMMARY; before coding

---

#### 3. SUMMARY-AUTO-SAVE-RESUME-POLLING.md (Existing)
**Purpose:** Executive summary + risk/load testing
**Length:** ~280 lines
**Contains:**
- Key findings (10 pitfalls)
- Implications for roadmap
- Phase structure recommendations
- Risk assessment
- Load testing scenarios (critical!)
- Confidence levels

**When to read:** For risk assessment and load testing strategy

---

#### 4. PITFALLS-AUTO-SAVE-RESUME-POLLING.md (Existing)
**Purpose:** Detailed pitfall analysis with prevention
**Length:** ~1100 lines
**Contains:**
- 10 documented pitfalls (CRITICAL, MODERATE, MINOR)
- Root causes (why pitfalls happen)
- Prevention strategies (code examples)
- Detection methods (how to find in testing)
- Phase-specific warnings
- Verification checklist per phase

**When to read:** During development; before code review

---

### Supporting Documents (Reference Only)

#### STACK.md (Existing)
**Relevant excerpts:** ASP.NET Core 8, EF Core concurrency patterns, IMemoryCache

#### FEATURES.md (Existing)
**Relevant excerpts:** Assessment session lifecycle, exam workflow

#### PITFALLS.md (Existing)
**Relevant excerpts:** Monolithic CMPController concerns, data integrity issues

#### ARCHITECTURE.md (Existing)
**Relevant excerpts:** Current CMPController architecture, data layers, EF Core patterns

---

## The 10 Critical Pitfalls

All discovered in existing research; repeated here for reference:

1. **Auto-Save Race:** Multiple AJAX calls create duplicate database rows
   - Prevention: Atomic upsert + unique constraint + RowVersion
   - Phase: 41

2. **Timer Drift:** Client-side timer manipulated (DevTools) or drifts
   - Prevention: Server-side elapsed time calculation (DateTime.UtcNow - StartedAt)
   - Phase: 42

3. **Double-Submit Race:** Worker submits, HC closes simultaneously; data lost
   - Prevention: RowVersion on AssessmentSession; DbUpdateConcurrencyException
   - Phase: 44

4. **TempData Consumed:** Token verified in TempData; consumed after first read; polling fails
   - Prevention: Move token verification to database (TokenVerified bool)
   - Phase: 41

5. **Polling Storm:** 20 workers × 10s polling = 2 DB hits/sec; scales to overload
   - Prevention: IMemoryCache with 5s TTL; reduces DB hits 3-6x
   - Phase: 43

6. **Answer Duplication:** Same question answered twice; grades use wrong answer
   - Prevention: UNIQUE constraint on (SessionId, QuestionId); atomic upsert
   - Phase: 41

7. **Question Count Change:** HC changes questions mid-exam; worker's answers invalid
   - Prevention: Validate question count unchanged in UpdateSessionProgress
   - Phase: 42

8. **Progress Inflated:** Progress counts saved answers, but saves can be cleared
   - Prevention: Count only non-null answers (WHERE PackageOptionId IS NOT NULL)
   - Phase: 45

9. **Cache Stale:** HC closes exam; polling still shows "open" for 5+ seconds
   - Prevention: Clear cache in CloseEarlySession
   - Phase: 43

10. **Audit Trail Missing:** Auto-saves not logged; can't trace grading disputes
    - Prevention: Log SaveAnswer to audit table
    - Phase: 41 (optional)

**Full details:** See PITFALLS-AUTO-SAVE-RESUME-POLLING.md

---

## Build Order (with Strict Dependencies)

```
Phase 41: Auto-Save + RowVersion (no dependencies)
    │
    ├─→ Phase 42: Resume (hard dep: RowVersion from 41)
    │       │
    │       └─→ Phase 43: Polling (hard dep: stable session state from 42)
    │               │
    │               ├─→ Phase 44: Close Early (optional; uses RowVersion + caching)
    │               │
    │               └─→ Phase 45: HC Monitoring (reuses caching from 43)
    │
    └─→ Cannot skip phases; cannot parallelize (dependencies blocking)
```

**Critical path:** 41 → 42 → 43 → 45 = 6–7 weeks
**With Phase 44:** 41 → 42 → 43 → 44 → 45 = 7–8 weeks

---

## Effort Estimate (by Phase)

| Phase | Frontend | Backend | DB | Testing | Total |
|-------|----------|---------|----|---------| ------|
| 41 | 1-2d | 2-3d | 1d | 2-3d | **1 week** |
| 42 | 2-3d | 2-3d | 1d | 2-3d | **1.5 weeks** |
| 43 | 1-2d | 2-3d | - | 2-3d | **1 week** |
| 44 | - | 1-2d | - | 2-3d | **4-5 days** (optional) |
| 45 | 2-3d | 2-3d | - | 1-2d | **1 week** |

**Total:** 6-7 weeks (or 4 weeks if Phase 44 skipped)

---

## Key Technical Decisions

### Decision 1: No API Layer Refactor
**Why?** Monolithic CMPController (2700 lines) already has SaveAnswer + CheckExamStatus. Better to enhance incrementally than refactor everything.

### Decision 2: Atomic Upsert (ExecuteUpdateAsync)
**Why?** Single transaction, no race between read + update. Safer than delete + insert.

### Decision 3: IMemoryCache (Not Redis)
**Why?** Single-server deployment. In-process cache is <1ms latency. Can migrate to Redis later if scaling.

### Decision 4: Server-Side Time Authority
**Why?** Client timer can be hacked with DevTools. Server clock (DateTime.UtcNow) is authoritative.

### Decision 5: 5-Second Cache TTL
**Why?** Polling every 10-30s. 5s cache = 2-6 cache hits per poll interval. Acceptable freshness.

---

## Risk Summary

### Critical Risks (Must Mitigate)

| Risk | Phase | Probability | Impact | Mitigation |
|------|-------|-------------|--------|-----------|
| Race condition (SaveAnswer) | 41 | HIGH | Data corruption | ExecuteUpdateAsync + unique constraint |
| Timer manipulation | 42 | MEDIUM | Grade fraud | Server-side elapsed time |
| Cache staleness | 43 | MEDIUM | Wrong exam state | Cache invalidation in CloseEarlySession |
| Double-submit loss | 44 | MEDIUM | Score discrepancy | RowVersion + exception handling |
| Wrong progress count | 45 | LOW | Misleading UI | WHERE PackageOptionId IS NOT NULL |

### Recommended Load Tests

1. **Phase 41:** 1 worker, 1000 rapid clicks; verify 0 duplicates
2. **Phase 43:** 100 workers, 10s polling, 10 min duration; verify DB <30% CPU
3. **Phase 44:** Simultaneous SubmitExam + CloseEarlySession; verify scores match
4. **Phase 45:** 500 workers; progress queries <2s latency

---

## Configuration Checklist

### Phase 41
- [ ] Add [Timestamp] to AssessmentSession model
- [ ] Add [Timestamp] to PackageUserResponse model
- [ ] Create migration: unique constraint (SessionId, QuestionId)
- [ ] Replace SaveAnswer upsert with ExecuteUpdateAsync
- [ ] Add debounce(300ms) to Exam.cshtml

### Phase 42
- [ ] Add LastPageIndex, ElapsedSeconds to AssessmentSession
- [ ] Create migration for 2 new columns
- [ ] Implement UpdateSessionProgress endpoint
- [ ] Enhance StartExam: load resume state
- [ ] Add page tracking + timer JS to Exam.cshtml

### Phase 43
- [ ] Add to Startup.cs: `services.AddMemoryCache()`
- [ ] Enhance CheckExamStatus: add caching
- [ ] Enhance CloseEarlySession: clear cache
- [ ] Add polling loop to Exam.cshtml (10-30s interval)
- [ ] Configure POLLING_INTERVAL_MS constant

### Phase 44
- [ ] Test close-early race condition
- [ ] Verify DbUpdateConcurrencyException handling
- [ ] Ensure Close Early grading matches SubmitExam

### Phase 45
- [ ] Implement GetMonitoringProgress endpoint
- [ ] Add Progress column to monitoring table
- [ ] Add refresh JS to AssessmentMonitoringDetail.cshtml
- [ ] Configure MONITORING_REFRESH_MS constant (5-10s)

---

## Verification Checklist (Per Phase)

### Phase 41 Verification
- [ ] 0 duplicate (SessionId, QuestionId) pairs after 1000 rapid clicks
- [ ] SaveAnswer latency <10ms per click
- [ ] RowVersion increments on each save
- [ ] Debounce prevents excessive AJAX calls

### Phase 42 Verification
- [ ] Resume works after network disconnect (5+ min outage)
- [ ] Elapsed time correct (within 1 second of wall-clock)
- [ ] Question count change blocks resume
- [ ] LastPageIndex jumps to correct page on reload
- [ ] UpdateSessionProgress latency <50ms

### Phase 43 Verification
- [ ] Cache hit rate >80% (5s cache + 10-30s polling)
- [ ] Polling detects close within 5 seconds
- [ ] 100 workers polling: DB <30% CPU
- [ ] CloseEarlySession invalidates cache immediately
- [ ] Next poll after close shows updated status

### Phase 44 Verification
- [ ] Simultaneous SubmitExam + CloseEarlySession: both succeed, consistent final state
- [ ] DbUpdateConcurrencyException caught and handled gracefully
- [ ] Competency records created once (no duplicates)
- [ ] Scores match between concurrent requests

### Phase 45 Verification
- [ ] Progress updates within 5-10s of answer submission
- [ ] Count matches manual count (non-null answers only)
- [ ] 500 workers: progress queries <2s latency
- [ ] Completed sessions highlighted immediately

---

## What This Research Does NOT Cover

1. **UX Design:** Button placement, modal design, progress bar styling
2. **Accessibility:** ARIA labels, keyboard navigation, color contrast
3. **Localization:** Translation strings, time zone handling (beyond UTC)
4. **Mobile:** Responsive design, touch event handling
5. **Security:** CSRF tokens (already in place), rate limiting, IP restrictions
6. **Analytics:** Tracking auto-save events, user behavior logging
7. **Database Optimization:** Indexes for query performance, query plans
8. **Monitoring:** Application Insights, error tracking, performance monitoring

**These topics are addressed during each phase's implementation research.**

---

## Gaps Requiring Phase-Specific Research

These questions should be investigated as each phase begins:

### Phase 41
- How to test unique constraint migration on production DB (with existing duplicates)?
- Performance impact of ExecuteUpdateAsync vs. current pattern (benchmarks)?

### Phase 42
- What is the maximum pause duration (1 min, 1 hour, unlimited)?
- Should UI show "paused" state to worker?
- Backward compatibility: how to handle sessions started before Phase 42?

### Phase 43
- Should cache be cleared on other state changes (e.g., session role change)?
- Interaction with 2-minute grace period after exam time expires?

### Phase 44
- Does Close Early use same grading logic as SubmitExam (or different score calculation)?
- When Close Early grades, do Competency levels update the same way as SubmitExam?

### Phase 45
- What's the definition of "progress" (answered % vs completion %)?
- Should HC monitoring and worker polling use same refresh rate?
- Should audit trail log every progress change or only final state?

---

## How to Use This Research in Roadmap

### For Roadmap Document
1. Include build order: 41 → 42 → 43 → 44 (optional) → 45
2. Include effort estimate: 1 week per phase (except 42 = 1.5 weeks)
3. Include risk flags:
   - Phase 41: Race condition testing critical
   - Phase 43: Load testing mandatory (100 workers)
   - Phase 44: Concurrency testing required
4. Include blockers:
   - Cannot start Phase 42 until Phase 41 RowVersion in DB
   - Cannot start Phase 44 until Phase 41 + 43 complete

### For Phase Planning
1. Use PITFALLS-AUTO-SAVE-RESUME-POLLING.md as implementation spec
2. Use load testing scenarios from SUMMARY as QA plan
3. Use verification checklists from ARCHITECTURE-v2.1-FEATURES.md as acceptance criteria

### For Code Review
1. Checklist: Does code implement atomic upsert correctly?
2. Checklist: Does timer use DateTime.UtcNow (not client timer)?
3. Checklist: Is cache invalidated on state changes?
4. Checklist: Are RowVersion conflicts handled?

---

## Quick Reference: File Map

```
.planning/research/
├── INTEGRATION-SUMMARY-v2.1.md ⭐ START HERE
│   └── What changes, what stays, build order, risks, gaps
│
├── ARCHITECTURE-v2.1-FEATURES.md ⭐ THEN READ THIS
│   └── Detailed integration, data flows, patterns, implementation checklist
│
├── SUMMARY-AUTO-SAVE-RESUME-POLLING.md (Existing)
│   └── Executive summary, 10 pitfalls, load testing scenarios
│
├── PITFALLS-AUTO-SAVE-RESUME-POLLING.md (Existing)
│   └── Deep dive: 10 pitfalls, prevention, detection, code examples
│
├── INDEX-v2.1-RESEARCH.md (THIS FILE)
│   └── Navigation guide, quick reference, file map
│
├── EXAM-RESILIENCE-*.md (Existing)
│   └── Earlier research iterations; references for context
│
└── Other files (STACK.md, FEATURES.md, PITFALLS.md, ARCHITECTURE.md)
    └── Codebase overview; reference as needed
```

---

## Next Steps

1. **Project Lead:** Read INTEGRATION-SUMMARY-v2.1.md (10 min)
2. **Tech Lead:** Read ARCHITECTURE-v2.1-FEATURES.md (20 min)
3. **Team:** Review build order and risk summary above
4. **Decision:** Approve/modify phase structure + effort estimate
5. **Planning:** Create Phase 41 roadmap task with PITFALLS verification checklist
6. **Development:** Begin Phase 41 with ExecuteUpdateAsync + RowVersion + debounce

---

## Research Completion Summary

| Area | Status | Confidence | Notes |
|------|--------|-----------|-------|
| Feature integration | ✅ COMPLETE | HIGH | All 4 features mapped to existing endpoints |
| Architecture | ✅ COMPLETE | HIGH | No refactoring needed; enhancement only |
| Data flows | ✅ COMPLETE | HIGH | Current vs. new flows documented |
| Dependencies | ✅ COMPLETE | HIGH | Build order locked; all blockers identified |
| Pitfalls | ✅ COMPLETE | HIGH | 10 pitfalls + prevention patterns |
| Load testing | ✅ COMPLETE | MEDIUM | Scenarios defined; need actual load test execution |
| Effort estimate | ✅ COMPLETE | MEDIUM | 6-7 weeks; refined during phase planning |
| Risk assessment | ✅ COMPLETE | HIGH | Critical risks identified + mitigations |
| Phase-specific gaps | ✅ IDENTIFIED | - | Will be researched during each phase |

**Overall research confidence: HIGH**

All key architectural decisions made. All integration points identified. All risks documented. Ready for roadmap synthesis and phase execution.

---

## Contact & Questions

For questions about this research:
1. Check INTEGRATION-SUMMARY-v2.1.md (common questions)
2. Check ARCHITECTURE-v2.1-FEATURES.md (detailed explanations)
3. Check PITFALLS-AUTO-SAVE-RESUME-POLLING.md (prevention patterns)
4. Review SUMMARY-AUTO-SAVE-RESUME-POLLING.md (risk/load testing)

If answers not found, topic is a gap requiring phase-specific research (see "Gaps" section above).

---

**Research completed by:** Claude Code (Agent Research Mode)
**Date:** 2026-02-24
**Status:** Ready for roadmap synthesis
