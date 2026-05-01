# Phase 310: Essay Finalize Idempotency - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-01
**Phase:** 310-essay-finalize-idempotency
**Areas discussed:** UI Scope, UI Gate, No-op Response, Tolak Msg, Notification Dedup, Concurrency Protection, Audit Log Dedup

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Scope UI button | Klarifikasi tombol target di Admin/AssessmentMonitoringDetail vs CDP CertificationManagement | ✓ |
| No-op response style | Friendly success vs silent vs verbose info untuk session terminal | ✓ |
| Notification dedup mechanism | DB column vs lookup vs group-level table | ✓ |
| Concurrency protection scope | EF guards vs SemaphoreSlim vs DB transaction | ✓ |

User's choice: All 4

---

## Area 1: UI Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, scope = btn-finalize | Hide guard di Admin/AssessmentMonitoringDetail.cshtml L414-419 | ✓ (defer to recommended) |
| Plus tambah tombol baru di CDP | Scope expanded ke CDP CertificationManagement | |
| Hide + replacement badge | Hide + badge "Sudah Selesai" replacement | |

**User's choice:** "terserah, sesuai analisa kamu" — defer to recommended (Option a).
**Notes:** Confirmed via grep — tidak ada tombol Create Sertifikasi terpisah di CDP. ROADMAP wording "atau panel detail" = alternative.

---

## Area 2: UI Gate Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Hide button + badge sukses | Tombol hilang, badge "Penilaian Selesai" replacement | |
| Disable button + tooltip | Disabled greyed + tooltip "Sudah selesai pada [tanggal]" | ✓ |
| Hide button only | Cuma hide tanpa replacement | |

**User's choice:** "Disable button + tooltip"
**Notes:** Affordance tetap visible, mencegah user bingung kenapa tombol hilang.

---

## Area 3: No-op Response Style

| Option | Description | Selected |
|--------|-------------|----------|
| Friendly success no-op | API success + alreadyFinalized: true + message + cert info; UI toast biru | ✓ |
| Silent success | API success tanpa pesan; UI silent | |
| Verbose info dengan link | Plus tombol "Lihat Sertifikat" di toast | |

**User's choice:** "Friendly success no-op"
**Notes:** Idempotent semantic, user dapat feedback bahwa nothing-bad happened.

---

## Area 4: Tolak Message untuk Status Non-Terminal

| Option | Description | Selected |
|--------|-------------|----------|
| Pesan spesifik per status | Open/InProgress/Cancelled dapat pesan masing-masing | ✓ |
| Pesan generic + nama status | "Status sekarang: [Status]" interpolation | |
| Biarkan apa adanya | "Session tidak dalam status Menunggu Penilaian" generic | |

**User's choice:** "Pesan spesifik per status" (setelah Claude jelaskan mapping status)
**Notes:** User awalnya minta klarifikasi karena tidak paham — Claude jelaskan status = state machine session (Open/InProgress/Pending/Completed/Cancelled), lalu user pilih per-status messaging untuk actionable feedback ke Admin.

---

## Area 5: Notification Dedup Mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Lookup notif sebelumnya | Query UserNotifications existing sebelum send | ✓ (defer to recommended) |
| Tambah column NotificationSentAt | Schema migration di AssessmentSessions | |
| Tambah column NotifiedAt di group level | Table baru AssessmentGroupNotifications | |

**User's choice:** "sesuai saran kamu" — defer to recommended (Option a).
**Notes:** Simpler, no DB migration. Fallback ke approach (b) kalau schema UserNotifications tidak punya field identifier yang cukup (verify saat planning).

---

## Area 6: Concurrency Protection Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Trust EF WHERE-clause guards | Existing pattern + idempotent return value | ✓ (defer to recommended) |
| Tambah SemaphoreSlim per session | App-level lock dengan ConcurrentDictionary | |
| DB transaction serializable | BeginTransaction Serializable isolation | |

**User's choice:** "ur suggest" — defer to recommended (Option a).
**Notes:** Pattern proven di Phase 309 + GradingService. Single instance KPB tidak butuh distributed lock.

---

## Area 7: Audit Log Dedup

| Option | Description | Selected |
|--------|-------------|----------|
| Add log + dedup via WHERE NOT EXISTS | Caller-side gate dalam if-block "rows affected > 0" | ✓ (defer to recommended) |
| Add log unconditional + WHERE NOT EXISTS guard di service | Dedup di AuditLogService layer | |
| Skip audit log entirely | Tidak tambah log, kontradiksi SC #4 | |

**User's choice:** "ur suggest" — defer to recommended (Option a).
**Notes:** Dedup natural via existing WHERE-clause guard (rows affected gating). Reuse pattern AuditLog di method lain.

---

## Final Check

| Option | Description | Selected |
|--------|-------------|----------|
| Ready untuk context | Lanjut write CONTEXT.md | ✓ |
| Explore lebih lanjut | Identify gray area lain | |

**User's choice:** "Ready untuk context"

---

## Claude's Discretion

Areas where user explicitly deferred to recommended option:
- Area 1 (UI Scope) — "terserah, sesuai analisa kamu"
- Area 5 (Notif Dedup) — "sesuai saran kamu"
- Area 6 (Concurrency) — "ur suggest"
- Area 7 (Audit Dedup) — "ur suggest"

Plus implementation pattern detail (Razor vs JS guard, tooltip styling, JSON contract field naming, audit log payload format, toast vs inline alert) — defer ke planner.

## Deferred Ideas

- Tombol "Create Sertifikasi" baru di CDP CertificationManagement — scope creep
- NotificationSentAt column migration — fallback kalau lookup UserNotifications schema tidak feasible
- AssessmentConstants.IsAssessmentSubmitted reuse di FinalizeEssayGrading — opportunistic refactor
- SemaphoreSlim per-session lock — defer kalau scale-out
