# Exam Resilience & Live Monitoring Research — File Index

**Research scope:** Portal HC KPB v2.1 features (auto-save, session resume, exam invalidation, HC monitoring)
**Researched:** February 24, 2026
**Status:** Complete, ready for requirements writing

---

## Quick Navigation

### For Requirements Writers
Start with: **EXAM-RESILIENCE-QA.md**
- Directly answers the 5 questions you asked
- Quick-reference format (minimal reading)
- Concrete recommendations with rationales
- Time: 15-20 minutes to read

### For Roadmap Planning
Start with: **EXAM-RESILIENCE-SUMMARY.md**
- Executive summary of findings
- Phase sequencing recommendations
- High-risk edge cases
- Confidence levels on all areas
- Time: 20-30 minutes to read

### For Detailed Specification
Start with: **EXAM-RESILIENCE-FEATURES.md**
- Full feature landscape per area
- Table stakes vs differentiators
- Edge cases and implementation gotchas
- Visual mockups and UX patterns
- Dependencies between features
- Time: 45-60 minutes to read (comprehensive)

---

## File Contents at a Glance

### 1. EXAM-RESILIENCE-QA.md (Quick Reference — START HERE for QA)

**Purpose:** Answer your specific questions
**Length:** ~420 lines
**Best for:** Requirements writers who need quick, actionable answers

**Covers:**
- Q1: Auto-save table stakes + visual feedback
- Q2: Session resume UX (modal recommended, time calculation)
- Q3: Exam invalidation (soft modal, 5-10s polling)
- Q4: HC monitoring columns (6 core + 3 optional)
- Q5: Save indicators (toast + badge, skip checkmarks)
- Summary table for quick reference

**How to use:**
- Read the relevant section (each Q is self-contained)
- Reference the summary table as checklist
- Link to FEATURES.md for edge cases

---

### 2. EXAM-RESILIENCE-SUMMARY.md (Executive Summary — START HERE for Roadmap)

**Purpose:** Strategic summary + phasing + risk assessment
**Length:** ~229 lines
**Best for:** Roadmap builder, project lead, architecture review

**Covers:**
- Key insight: Auto-save is foundation
- Table stakes vs differentiators vs anti-features
- Feature dependencies & sequencing
- Phase structure (2.1a/b/c recommendations)
- High-risk edge cases with mitigations
- Confidence assessment by area
- Phase-specific questions for later

**How to use:**
- Read "Executive Summary" (5 min)
- Skim "Key Findings by Question" (10 min)
- Review "Phase Structure Recommendation" for planning
- Reference "High-Risk Pitfalls" during architecture review

---

### 3. EXAM-RESILIENCE-FEATURES.md (Detailed Specification — Full Reference)

**Purpose:** Complete feature landscape with patterns & edge cases
**Length:** ~567 lines
**Best for:** Deep reference during architecture/engineering

**Covers:**
- Feature 1: Auto-Save Answers
  - Table stakes behavior (selected option + page + timestamp)
  - Visual feedback pattern (click → saving → saved → persistent)
  - Answer state persistence requirements (JSON schema)
  - Edge cases (network drops, page refresh, offline)
  - Dependencies (countdown timer, shuffled packages)

- Feature 2: Session Resume
  - Recommended UX (explicit modal with time)
  - Resume requirements (what to restore, what not to)
  - Time calculation (server-side only)
  - Auto-reconnect timeout behavior
  - Alternative approaches (not recommended)

- Feature 3: Exam Invalidation Polling
  - Detection flow (5-10s polling)
  - UX pattern (soft modal + auto-redirect)
  - Why not silent or immediate
  - Polling frequency rationale
  - Edge case (worker submitting during close)

- Feature 4: Live Progress Monitoring
  - 6 core columns + 3 optional
  - Why each column is valuable
  - HC actions from dashboard
  - Refresh frequency (5-10s recommended)
  - Polling vs real-time
  - Dashboard layout mockup

- Feature Dependencies & Sequencing
- Table Stakes vs Differentiators vs Anti-Features
- MVP Recommendations (2.1 vs 2.2 scope)
- High-Risk Pitfalls & Moderate Issues
- Conformance to Existing Patterns

**How to use:**
- Find your topic of interest (table of contents)
- Read the detailed section
- Reference edge cases during implementation
- Cross-reference dependencies before estimating

---

## How These Files Fit Together

```
EXAM-RESILIENCE-INDEX.md (you are here)
    ↓
For quick answers:
    → EXAM-RESILIENCE-QA.md (5 Qs answered in 15 min)

For planning:
    → EXAM-RESILIENCE-SUMMARY.md (roadmap, phasing, risks)

For deep work:
    → EXAM-RESILIENCE-FEATURES.md (all details, edge cases, patterns)
```

**Read order recommendation:**
1. This index (you're doing it)
2. SUMMARY (understand the landscape + phasing)
3. QA (get your specific questions answered)
4. FEATURES (when you need the gory details)

---

## Key Takeaways (TL;DR)

### What to Build (v2.1)
1. **Auto-save** (60s polling + per-click) with toast feedback
2. **Session resume** (explicit modal, server-side time calc)
3. **Invalidation polling** (5-10s check, soft modal on close)
4. **HC monitoring** (6 core columns, 5-10s refresh)

### Dependencies
```
Auto-Save (foundation)
    → Session Resume (depends on auto-save state)
    → Invalidation Polling (depends on auto-save + resume)
    → HC Monitoring (depends on all three)
```

### High-Risk Areas
- Timestamp mismatch (use SERVER time only)
- Shuffle seed not saved/restored (breaks resume)
- Auto-save not ack'd before page nav (loses answers)
- Invalidation polling too slow (worker doesn't know exam closed)

### Phase Sequencing
- Phase 2.1a: Auto-save (1 week)
- Phase 2.1b: Session resume (1 week)
- Phase 2.1c: Invalidation + HC integration (1 week)
- Phase 2.2: HC monitoring dashboard (2 weeks)

### Confidence Levels
- **HIGH:** Auto-save patterns, visual feedback, table stakes
- **MEDIUM:** Session resume UX (good pattern, specific wording may vary), monitoring columns (core verified, optional inferred)
- **MEDIUM-LOW:** Invalidation polling timing (5-10s standard but not verified from single source)

---

## How to Cite These Files

**In requirements doc:** "Per EXAM-RESILIENCE-FEATURES.md, Feature 1 (Auto-Save)..."

**In architecture review:** "See EXAM-RESILIENCE-SUMMARY.md, High-Risk Pitfalls section"

**In QA plan:** Reference EXAM-RESILIENCE-QA.md sections as test case drivers

---

## Verification Protocol

All findings in these files follow this hierarchy:

1. **Context7 / Official Docs** (highest authority) — Moodle, Canvas, Exam.net official documentation
2. **Verified Platforms** — Deployed systems (AssessPrep, ProgressLearning, Digiexam)
3. **Design System Standards** — Carbon, Pajamas, NN Group UX guidance
4. **WebSearch Only** — Platforms where official docs unavailable (flagged as MEDIUM confidence)

**Confidence assignment:**
- HIGH: Verified from multiple official sources or design standards
- MEDIUM: One official source + design standard OR multiple deployed platforms
- MEDIUM-LOW: Single inference from standards (timing, frequency) without explicit verification

---

## Questions for Next Phase

These files identify open questions for Phase 2.1 requirements writing:

**Auto-Save specifics:**
- Save interval: 60s or different?
- Save on every click or just on change?
- Should partial answers be saved?

**Resume specifics:**
- Support pause/resume or only disconnect/resume?
- Show saved answers editable or read-only on resume?
- What if exam was already force-closed during disconnect?

**Invalidation specifics:**
- Who can force-close? (HC only, or Admin + HC?)
- Should worker see reason for close in modal?
- Should grace period exist for final submissions (5s)?

**Monitoring specifics:**
- Who can see monitoring dashboard? (HC + Admin only?)
- Can HC extend time mid-exam, or post-exam only?
- Should worker see live dashboard themselves?
- Is connection quality telemetry available?

**All these answered in:** EXAM-RESILIENCE-FEATURES.md and EXAM-RESILIENCE-SUMMARY.md, "Research Gaps" sections

---

## Files Summary

| File | Size | Purpose | Read Time | Audience |
|------|------|---------|-----------|----------|
| **QA.md** | 18K | Direct Q&A | 15 min | Requirements writers |
| **SUMMARY.md** | 15K | Strategic overview | 20 min | Roadmap, leads |
| **FEATURES.md** | 27K | Full specification | 45 min | Engineers, architects |
| **INDEX.md** | This file | Navigation | 10 min | Everyone |

---

## Last Updated

**Research completed:** February 24, 2026
**Research mode:** Ecosystem + UX Pattern Discovery
**Confidence:** MEDIUM overall (HIGH on patterns, MEDIUM on edge cases)
**Status:** Ready for requirements writing + architecture review

---

*These files are ready for handoff to requirements writing. No further research needed before Phase 2.1 planning.*
*Engineering can proceed with architecture review; QA can begin test case design.*
*Roadmap team has recommended phasing and risk assessment.*

*Contact: Research phase complete. Awaiting requirements writer to use QA.md for detailed spec.*
