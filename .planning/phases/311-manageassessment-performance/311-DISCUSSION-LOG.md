# Phase 311: ManageAssessment Performance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-05 (auto-pass) + 2026-05-05 (interactive update)
**Phase:** 311-manageassessment-performance
**Mode:** auto-pass (initial) + interactive update via /gsd-discuss-phase
**Areas analyzed (auto-pass):** Cache Strategy, Database Indexes, Query Optimization, Measurement Methodology, Scope Boundaries
**Areas re-discussed (interactive):** Cache TTL & invalidation, Failure mode (redirected to baseline-breakdown approach)

---

# PART 1 — Auto-Pass Log (2026-05-05)

## Cache Strategy (Categories distinct dropdown)

| Option | Description | Selected |
|--------|-------------|----------|
| Static key + TTL only | Single global key, 5min absolute expiration, no explicit invalidation | ✓ (later REVISED) |
| Versioned key | Key bumped on Categories CRUD, instant invalidation precision | |
| Sliding TTL | Reset window on each access, longer effective TTL during burst | |

**Auto choice rationale:** Categories rarely change, 5-min staleness acceptable per ROADMAP SC #5 explicit. Simplest implementation = lowest defect risk.
**⚠ Later revised in Part 2** — explicit invalidation chosen.

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
| Optimize ONLY Assessment tab query | Strict ROADMAP scope, smoke test parity for other tabs | ✓ (later QUALIFIED by D-16) |
| Extend optimization ke Training/History tabs | More improvement, scope creep | (deferred — but resurfaced in Part 2) |

**Auto choice rationale:** ROADMAP SC #7 require smoke test parity, NOT performance parity. Training/History optimization tracked as Phase 315+ candidate.

---

## Cache Invalidation Strategy (auto-pass duplicate of Cache Strategy above)

| Option | Description | Selected |
|--------|-------------|----------|
| Pure TTL (5 min absolute) | Simplest, accept ≤5min staleness | ✓ (later REVISED) |
| TTL + explicit invalidation on Category CRUD | Precise invalidation, more touch points | (deferred — later REPROMOTED) |
| Sliding expiration with periodic refresh | Hybrid approach | |

**Auto choice rationale:** Category CRUD operations not in this controller's hot path; user-side ≤5min staleness acceptable. Explicit invalidation noted as future enhancement if user reports stale dropdowns.
**⚠ Later revised in Part 2** — Option 2 explicit invalidation chosen.

---

## Migration Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Single migration with both indexes (D-05 + D-06) | Atomic deploy, easy rollback | ✓ |
| Two separate migrations (one per index) | Fine-grained rollback | |

**Auto choice rationale:** Both indexes target same table (`AssessmentSessions`), same migration window. Atomic = simpler ops.

---

# PART 2 — Interactive Update (2026-05-05)

## Areas Selection

| Option | Description | Selected for discuss |
|---|---|---|
| Cache TTL & invalidation | D-02/D-03 trade-off: stale dropdown vs explicit invalidation | ✓ (Claude pick) |
| Index investment depth | Composite vs single-column vs computed column | (kept as auto-pass) |
| Measurement methodology | Stopwatch vs external profiler, run count, p95 vs p99 | (kept as auto-pass) |
| Failure mode & rollback | What if post-patch <30%? | ✓ (Claude pick → redirected) |

**User's response:** "yang perlu discuss" (free-text) — deferred selection to Claude.
**Claude pick:** Cache TTL & invalidation + Failure mode/rollback — highest user-impact areas.

---

## Cache TTL & invalidation (interactive revisit)

| Option | Description | Selected |
|---|---|---|
| Keep 5-min TTL drift | Auto-pass default. Stale dropdown ≤5min after CRUD. Simple. | |
| 5-min TTL + explicit invalidation | Add `_cache.Remove("assessment_categories_distinct")` di Category CRUD. Best UX + ROADMAP-compliant. | ✓ |
| Shorter TTL 1-2 min, no invalidation | Reduce staleness window without invalidation logic. | |
| No cache (drop D-01..D-04) | Skip cache. Violates ROADMAP SC #5. | |

**User's response:** "analisa, dan sesuai suggest kamu" (free-text) — deferred to Claude analysis.
**Claude analysis & decision:** Option 2. Rationale: ROADMAP SC #5 require IMemoryCache 5 menit TTL → hanya Option 1/2 yang spec-compliant. Option 2 adds 3 lines code in 3 actions → minimal cost, fix immediate-staleness UX issue. Risk lupa invalidate di action baru = rendah karena CRUD kategori co-located dan jarang di-extend.
**Lock:** D-03 revised — explicit invalidation pattern (`_cache.Remove(...)` di `CreateCategory`/`UpdateCategory`/`DeleteCategory`).

---

## Failure mode & rollback strategy → REDIRECTED to baseline-breakdown approach

| Option | Description | Selected |
|---|---|---|
| Iterative: tambah composite/computed column | Phase 311 PASS sebagian, add follow-up commit, re-measure | (initial Claude rec, later superseded) |
| Accept lower threshold | Document partial improvement, close phase. | |
| Revert migration + kode, defer phase baru | Hard rollback, plan Phase 311.1. | |
| Defer keputusan ke saat itu | No pre-commitment. | |

**User's response sequence:**

1. **First reply:** "maksutnya apa ini, jelaskan dengan simple" — meminta penjelasan dalam bahasa awam.

2. **Claude reframe** ke bahasa simple, rekomendasi Opsi 1.

3. **User redirect** dengan pertanyaan teknis: *"di dalam page manage assessment kan ada beberapa tab juga. apakah tab lainnya juga mempengaruhi lebih cepat load?"*

4. **Claude investigate** `AssessmentAdminController.cs:60-227`. Temukan: controller selalu fetch data ketiga tab dalam 1 request walaupun user buka tab Assessment (komentar L195-197 eksplisit). Implikasi: kalau Training/History query lambat, optimasi Assessment query saja gak akan capai 30% improvement.

5. **Claude present** 3 sub-options: A (lazy-load), B (AsNoTracking di Training/History), C (stay auto-pass D-14). Suggest measure-first approach.

6. **User:** "oke untuk sekarang kita fokus apa sebaiknya:, jangan langsung masukkan semua" — minta narrow focus, jangan over-scope.

7. **Claude propose:** tambah D-16 saja (pre-execute baseline breakdown per-query), defer everything else (D-14 reframe, lazy-load idea, rollback strategy) ke setelah breakdown data tersedia.

8. **User:** "lanjut" — approve narrow focus.

**Lock:**
- D-16 added — pre-execute baseline breakdown per-query (T1..T5 segments) WAJIB sebelum apply patch. Decision gate: 3 skenario (A/B/C).
- Failure mode/rollback strategy explicitly **deferred** sampai D-16 data tersedia (measurement-driven).
- D-14 reframe explicitly **deferred** sampai D-16 menunjukkan Skenario A vs B/C.

---

## Claude's Discretion (carried from auto-pass, untouched)

- Migration timestamp generation (auto via `dotnet ef migrations add`)
- Stopwatch logging field naming convention (planner verifies by grep existing controller)
- EF Core SQL diff capture method (planner picks cleanest approach)
- Cache key namespace literal vs helper class (literal sufficient for single use case)

---

## Deferred Ideas (combined auto-pass + interactive)

- Composite index `IX_Schedule_ExamWindowCloseDate` (optional post-measurement)
- Persisted computed column `EffectiveDate` (alternative COALESCE fix)
- Training/History tab perf optimization (default deferred; can resurface via D-16 Skenario B/C)
- **Failure mode & rollback strategy** — deferred to post-D-16 (different decisions per Skenario A/B/C)
- **Lazy-load tab non-aktif** (`?tab=` param + AJAX partial reload) — phase masa depan, architectural change
- MiniProfiler integration
- Application Insights / OpenTelemetry APM

---

## Lessons / Patterns

- **User redirect dari pre-commitment → measurement-driven decision** adalah pola yang lebih sound untuk perf optimization phase. Auto-pass cenderung pre-commit ke scope awal; user judgment menambah investigation step yang bikin scope decision data-driven.
- **Narrow-focus discipline** — user explicit minta jangan over-include semua sub-decisions. Prinsip ini consistent dengan instruksi sistem "no half-finished implementations" dan harus di-honor downstream (planner jangan pre-emptive expand scope D-14 tanpa data D-16).
- **Bahasa awam request** — user prefer penjelasan plain-Bahasa untuk decision questions. Translate technical jargon (rollback, p95, threshold) ke bahasa sederhana sebelum minta keputusan.

---

*Generated: 2026-05-05 — auto-pass + interactive update via /gsd-discuss-phase*
