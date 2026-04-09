# Smoke Test Result — HIGH-05 Bulk Submit Lintas Coachee

**Tanggal:** 2026-04-09
**Branch:** main (after fix HIGH-05)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277 (Coach `rustam.nugroho@pertamina.com` / `123456`)
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev` (Windows Auth)

**Scope:** Verifikasi server guard `coacheeIds.Count > 1` di `SubmitEvidenceWithCoaching`.

---

## Setup

- Coach: Rustam (`6821c3d9-0c3e-4352-a91e-7728d6c9e4f9`)
- Coachee A: iwan3 (`66227777-1974-43ca-8bdd-e5586fa4a5b8`) — temp mapping Id=6, progress 4/5/6
- Coachee B: Rino (`4a624dbc-3241-4207-92d7-d1d5784c7137`) — mapping Id=4 (existing, active), temp progress Id=7 (PTAssignmentId=5, ProtonDeliverableId=1)
- Reset progress 4/5/6 → `Pending` sebelum test.

---

## Scenario A — Negative (mixed coachee)

**Request:** `progressIdsJson=[4,7]` (iwan3 PID=4 + Rino PID=7)

**Response:**
```json
{"success":false,"message":"Bulk submit hanya bisa untuk satu coachee per request."}
```
HTTP 200. ✅

**DB post-state:** 4=Pending, 5=Pending, 6=Pending, 7=Pending — tidak berubah. ✅

---

## Scenario B — Positive (single coachee, 3 progress)

**Request:** `progressIdsJson=[4,5,6]` (semua iwan3)

**Response:**
```json
{"success":true,"message":"3 deliverable berhasil disubmit","submittedIds":[4,5,6],"hasEvidence":false}
```
HTTP 200. ✅ Guard tidak false-positive pada single-coachee multi-progress.

---

## Scenario C — Backward compat (count=1)

**Request:** `progressIdsJson=[7]` (single id, Rino)

**Response:**
```json
{"success":true,"message":"1 deliverable berhasil disubmit","submittedIds":[7],"hasEvidence":false}
```
HTTP 200. ✅

---

## Ringkasan

| Scenario | Expected | Result |
|----------|----------|--------|
| A — mixed coachee (2 id, 2 coachee) | reject, DB unchanged | ✅ PASS |
| B — single coachee (3 id) | success, semua Submitted | ✅ PASS |
| C — single id (1 id) | success | ✅ PASS |

**3/3 hijau.** HIGH-05 ditutup: server guard menolak bulk submit lintas coachee, tidak mengganggu code path single-coachee.

## Cleanup

- Temp mapping iwan3 `CoachCoacheeMappings.Id=6` dihapus.
- Temp progress Rino `ProtonDeliverableProgresses.Id=7` + `DeliverableStatusHistories` + `CoachingSessions` terkait dihapus.
- Progress 4/5/6 tetap di state `Submitted` (dari scenario B) — bisa direset manual bila perlu.
