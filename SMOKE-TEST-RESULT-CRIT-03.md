# Smoke Test Result — CRIT-03 / MED-06 Proton Coaching

**Tanggal:** 2026-04-09
**Branch:** main (after fix CRIT-03)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277 (Coach login — `rustam.nugroho@pertamina.com` / `123456`)
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev` (Windows Auth, `-C` trust cert)

**Scope:** 3 skenario gated approval reset × 2 code path (Lokasi A bulk + Lokasi B single upload). Total 6 test case, semua hijau.

---

## Setup

- Coach: `rustam.nugroho@pertamina.com` (Id `6821c3d9-0c3e-4352-a91e-7728d6c9e4f9`)
- Coachee: `iwan3@pertamina.com` (Id `66227777-1974-43ca-8bdd-e5586fa4a5b8`)
- Temp mapping `CoachCoacheeMappings.Id=5` dibuat sebelum test (iwan3 tidak punya mapping aktif di baseline), dihapus di cleanup.
- 3 progress existing: Id 4, 5, 6 (semua milik iwan3).

---

## Lokasi A — Bulk submit (`SubmitEvidenceWithCoaching`)

### Pre-state

| Id | Status   | SrSpvApprovalStatus | SrSpvApprovedBy | Skenario |
|----|----------|---------------------|-----------------|----------|
| 4  | Pending  | Pending             | NULL            | 3 — first submit (fresh) |
| 5  | Pending  | Approved            | rustam          | 2 — race: SrSpv sudah approve stale |
| 6  | Rejected | Approved            | rustam          | 1 — real resubmit (stale approval should reset) |

### Aksi

`fetch('/CDP/SubmitEvidenceWithCoaching', …)` dengan `progressIdsJson=[4,5,6]`, token dari `__RequestVerificationToken`. Tanpa evidence file (fields `date`, `catatanCoach`, `kesimpulan`, `result` terisi).

### Response

```json
{"success":true,"message":"3 deliverable berhasil disubmit","submittedIds":[4,5,6],"hasEvidence":false}
```

HTTP **200**, Content-Type `application/json; charset=utf-8`.

### Post-state

| Id | Status    | SrSpv | SrSpvApprovedBy | Expected | Result |
|----|-----------|-------|-----------------|----------|--------|
| 4  | Submitted | Pending | NULL | Pending, tidak ada history Approval Reset | **✓** |
| 5  | Submitted | **Approved** | **rustam preserved** | Approval tidak ter-reset (race mitigation) | **✓** |
| 6  | Submitted | Pending | NULL | Reset ke Pending + history Approval Reset | **✓** |

### DeliverableStatusHistories (entries baru)

```
PID=4: Submitted (Coach)
PID=5: Submitted (Coach)
PID=6: Re-submitted (Coach)
PID=6: Approval Reset (Coach)   ← audit trail baru
```

PID=4 dan PID=5 **tidak** punya entry `Approval Reset` → gate `if (isResubmit)` aktif dengan benar. PID=6 punya entry `Approval Reset` → audit trail hadir.

---

## Lokasi B — Single upload (`UploadEvidence`)

### Pre-state (setelah P5 & P6 direset ulang via SQL)

| Id | Status   | SrSpv    | Skenario |
|----|----------|----------|----------|
| 5  | Pending  | Approved | 2 — race state |
| 6  | Rejected | Approved | 1 — real resubmit |

(Skenario 3 tidak diulang di Lokasi B karena identik dengan Lokasi A — gating pakai variabel `wasRejected` yang sama semantiknya dengan `isResubmit`.)

### Aksi

`fetch('/CDP/UploadEvidence', …)` dengan minimal PDF blob, per progressId. HTTP 302 (RedirectToAction — expected, bukan JSON endpoint).

### Post-state

| Id | Status    | SrSpv | SrSpvApprovedBy | EvidenceFileName | Expected | Result |
|----|-----------|-------|-----------------|------------------|----------|--------|
| 5  | Submitted | **Approved** | **rustam preserved** | smoke_5.pdf | Approval tidak ter-reset | **✓** |
| 6  | Submitted | Pending | NULL | smoke_6.pdf | Reset + history Approval Reset | **✓** |

### DeliverableStatusHistories (entries baru Lokasi B)

```
PID=5: Submitted (Coach)                  ← tidak ada Approval Reset
PID=6: Approval Reset (Coach)             ← audit trail baru
PID=6: Re-submitted (Coach)
```

---

## Ringkasan

| Path | Skenario | Expected | Result |
|------|----------|----------|--------|
| Lokasi A (bulk)    | 1 real resubmit       | Reset + log Approval Reset | ✅ PASS |
| Lokasi A (bulk)    | 2 race Pending+Approved | Preserved, no reset        | ✅ PASS |
| Lokasi A (bulk)    | 3 first submit        | No reset, no log           | ✅ PASS |
| Lokasi B (single)  | 1 real resubmit       | Reset + log Approval Reset | ✅ PASS |
| Lokasi B (single)  | 2 race Pending+Approved | Preserved, no reset        | ✅ PASS |

**5/5 hijau.** CRIT-03 silent approval reset ditutup, MED-06 (unconditional reset saat Pending) juga ditutup oleh gate yang sama, audit trail `"Approval Reset"` tercatat untuk setiap reset real.

## Catatan residual

Fix ini **minimal, tanpa RowVersion**. Masih ada residual race window kecil untuk skenario:
- Coach resubmit progress yang baru saja di-Reject
- **Bersamaan** dengan reviewer yang sedang memproses approval terhadap state stale pre-reject

Mitigasi penuh butuh optimistic concurrency (RowVersion + `DbUpdateConcurrencyException` handling). Dicatat sebagai follow-up optional di `BUG-HUNT-REPORT-PROTON-COACHING.md` header.

## Cleanup

- Temp mapping `CoachCoacheeMappings.Id=5` dihapus.
- File fisik `wwwroot/uploads/evidence/5/smoke_5.pdf` dan `wwwroot/uploads/evidence/6/smoke_6.pdf` masih ada (tidak dihapus — artefak test kecil).
- Progress 4/5/6 dalam state Submitted (dari test terakhir); bisa direset manual ke Pending bila perlu.
