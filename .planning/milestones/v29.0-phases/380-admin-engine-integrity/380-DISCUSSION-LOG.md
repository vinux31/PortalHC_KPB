# Phase 380: Admin/Engine Integrity - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-14
**Phase:** 380-admin-engine-integrity
**Areas discussed:** Token repair (TOK-01), Extra-time authz+cap (RST-01/04), Empty-package edge (SHF-01)

---

## Token fix (TOK-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Defensive compare + write | Uppercase both sides at VerifyToken + uppercase on write; auto-heal existing lowercase tokens, no migration | ✓ |
| Forward write-only | Uppercase only on edit; existing broken tokens stay until re-edited | |
| Forward + repair script IT | Uppercase on edit + one-time SQL for IT | |

**User's choice:** Defensive compare + write.
**Notes:** Heals currently-locked workers instantly, zero DB touch.

---

## Extra-time role (RST-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Admin + HC | Match sibling actions (`[Authorize(Roles="Admin,HC")]`) | ✓ |
| Admin only | Stricter, but diverges from siblings; HC loses operational access | |

**User's choice:** Admin + HC.

---

## Extra-time cap (RST-04)

| Option | Description | Selected |
|--------|-------------|----------|
| ≤ original exam duration | Total extra ≤ DurationMinutes (max 2× total); scales with exam length | ✓ |
| Absolute (e.g. ≤120 min) | Fixed minute cap regardless of duration | |
| Per-grant cap only | Limit per click but total still unbounded | |

**User's choice:** ≤ original exam duration.

---

## All-empty (SHF-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Block + friendly message | Show "ujian belum siap" + don't start session/auto-grade; engine fix covers StartExam + ReshufflePackage + ReshuffleAll | ✓ |
| Leave 0 (status quo) | Worker gets 0 questions → auto-grade 0% Fail (the bug) | |

**User's choice:** Block + friendly message.
**Notes:** Common 1-empty-package case still proceeds (worker gets questions from non-empty packages).

## Claude's Discretion
- All-empty message wording (BI), guard placement in StartExam, RST-04 reject message, optional UI hint for cap.

## Deferred Ideas
- Reviewed todo (not folded): one-time test-data cleanup post-367 (DB chore, unrelated).
