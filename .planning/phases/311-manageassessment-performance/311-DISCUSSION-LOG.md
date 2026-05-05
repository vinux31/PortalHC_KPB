# Phase 311: ManageAssessment Performance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-05
**Phase:** 311-manageassessment-performance
**Mode:** auto (autonomous pass — system auto mode active)
**Areas analyzed:** Cache Strategy, Database Indexes, Query Optimization, Measurement Methodology, Scope Boundaries

---

## Cache Strategy (Categories distinct dropdown)

| Option | Description | Selected |
|--------|-------------|----------|
| Static key + TTL only | Single global key, 5min absolute expiration, no explicit invalidation | ✓ |
| Versioned key | Key bumped on Categories CRUD, instant invalidation precision | |
| Sliding TTL | Reset window on each access, longer effective TTL during burst | |

**Auto choice rationale:** Categories rarely change, 5-min staleness acceptable per ROADMAP SC #5 explicit. Simplest implementation = lowest defect risk.

---

## Database Indexes (composite vs single-column)

| Option | Description | Selected |
|--------|-------------|----------|
| Two single-column indexes (ExamWindowCloseDate + LinkedGroupId) | Optimizer chooses, minimal migration | ✓ |
| Composite IX_Schedule_ExamWindowCloseDate per ROADMAP literal | Single composite per ROADMAP SC #4 wording | (deferred) |
| Persisted computed column EffectiveDate + index | Solves COALESCE seek issue | (deferred) |

**Auto choice rationale:** COALESCE in WHERE clause prevents perfect seek even with composite. Optimizer-friendly approach with two single-column indexes (Schedule already exists). Composite kept as deferred optimization if measurement post-patch shows residual bottleneck.

---

## Query Optimization (AsNoTracking + Include removal)

| Option | Description | Selected |
|--------|-------------|----------|
| AsNoTracking on managementQuery + remove redundant Include | Two-step minimal change per ROADMAP SC #2-3 literal | ✓ |
| Compile query (`EF.CompileQuery`) | Pre-compiled LINQ for hot path | (deferred) |
| Raw SQL via `FromSqlRaw` | Maximum control, lose EF translation safety | (rejected — backward-compat risk) |

**Auto choice rationale:** ROADMAP explicit. Compile query is overkill for single hot endpoint and adds complexity; raw SQL breaks portability.

---

## Measurement Methodology

| Option | Description | Selected |
|--------|-------------|----------|
| Stopwatch in action body | Lightweight, captures full request scope, no infra dep | ✓ |
| MiniProfiler integration | Comprehensive request profiling | (deferred) |
| SQL Profiler / EF Core LogTo | DB-level only, less context | |
| Application Insights / OpenTelemetry | Production APM, broader scope | (out of scope) |

**Auto choice rationale:** ROADMAP SC #1 mentions "Stopwatch atau SQL profiler". Stopwatch wins on simplicity + production-deployable + log-friendly format.

---

## Scope Boundaries (Training/History tabs)

| Option | Description | Selected |
|--------|-------------|----------|
| Optimize ONLY Assessment tab query | Strict ROADMAP scope, smoke test parity for other tabs | ✓ |
| Extend optimization ke Training/History tabs | More improvement, scope creep | (deferred) |

**Auto choice rationale:** ROADMAP SC #7 require smoke test parity, NOT performance parity. Training/History optimization tracked as Phase 315+ candidate.

---

## Cache Invalidation Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Pure TTL (5 min absolute) | Simplest, accept ≤5min staleness | ✓ |
| TTL + explicit invalidation on Category CRUD | Precise invalidation, more touch points | (deferred) |
| Sliding expiration with periodic refresh | Hybrid approach | |

**Auto choice rationale:** Category CRUD operations not in this controller's hot path; user-side ≤5min staleness acceptable. Explicit invalidation noted as future enhancement if user reports stale dropdowns.

---

## Migration Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Single migration with both indexes (D-05 + D-06) | Atomic deploy, easy rollback | ✓ |
| Two separate migrations (one per index) | Fine-grained rollback | |

**Auto choice rationale:** Both indexes target same table (`AssessmentSessions`), same migration window. Atomic = simpler ops.

---

## Claude's Discretion

- Migration timestamp generation (auto via `dotnet ef migrations add`)
- Stopwatch logging field naming convention (planner verifies by grep existing controller)
- EF Core SQL diff capture method (planner picks cleanest approach)
- Cache key namespace literal vs helper class (literal sufficient for single use case)

## Deferred Ideas

- Composite index `IX_Schedule_ExamWindowCloseDate` (optional post-measurement)
- Persisted computed column `EffectiveDate` (alternative COALESCE fix)
- Cache invalidation on Category CRUD
- Training/History tab perf optimization (Phase 315 candidate)
- MiniProfiler integration
- Application Insights / OpenTelemetry APM

---

*Generated: 2026-05-05 — autonomous pass, no interactive Q&A*
