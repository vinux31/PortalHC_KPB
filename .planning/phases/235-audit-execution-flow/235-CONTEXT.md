# Phase 235: Audit Execution Flow - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Memastikan alur operasional harian Proton (evidence submission, approval chain, status history, notifikasi, PlanIdp view) aman dari sisi server dan state-nya selalu konsisten. Audit + fix bugs yang ditemukan.

</domain>

<decisions>
## Implementation Decisions

### Evidence & Resubmit
- **D-01:** Coach yang upload evidence, bukan coachee — Coach adalah role uploader untuk coaching Proton
- **D-02:** File lama dipertahankan saat resubmit — evidence baru ditambah sebagai versi terbaru, file lama tetap di server sebagai history. Fix: simpan history path di DB agar traceable
- **D-03:** Concurrent upload: last-write-wins — upload terakhir yang masuk DB adalah yang tersimpan
- **D-04:** Single file per deliverable — sesuai model saat ini, tidak perlu multi-file
- **D-05:** Rejection reason wajib diisi oleh approver — sudah ada di codebase (L897 CDPController)
- **D-06:** Resubmit tanpa batas — coachee/coach bisa resubmit berapa kali pun sampai approved
- **D-07:** Approval chain reset dari awal saat resubmit — sudah di-implement di codebase (L935-943)
- **D-08:** File validation server-side: PDF/JPG/PNG, max 10MB — sudah ada di codebase (L1099-1112)
- **D-09:** Upload gagal: rollback + error message — jangan update status deliverable, state tetap seperti sebelum upload

### Approval Chain Consistency
- **D-10:** Race condition: first-write-wins — cek status sebelum update, approve kedua mendapat error "sudah diproses"
- **D-11:** Admin Override: Claude investigasi di codebase apakah ada override flow dan audit consistency-nya
- **D-12:** Status per-level dan chain flow: sesuai existing — co-sign pattern (SrSpv dan SH bisa approve independen) tetap dipertahankan
- **D-13:** Status "Approved" prematur (SrSpv approve langsung set overall Approved) — TIDAK di-fix, behavior existing dianggap acceptable
- **D-14:** Fix notification dedup fragile — `CreateHCNotificationAsync` pakai `Message.Contains()` untuk dedup, ganti dengan structured field check

### Status History & Notifikasi
- **D-15:** StatusHistory insert di SEMUA transisi: Initial Pending (saat seed), Evidence Upload/Submit, Resubmit after reject, Approve, Reject, HC Review
- **D-16:** Gap saat ini: `UploadEvidence` (L1086) tidak record StatusHistory — hanya `SubmitEvidenceWithCoaching` yang record. Fix: tambah RecordStatusHistory di UploadEvidence
- **D-17:** Initial "Pending" insert saat ProtonDeliverableProgress pertama kali di-seed — baseline history
- **D-18:** Tambah notifikasi resubmit — kirim notif khusus saat evidence diresubmit setelah reject

### PlanIdp View Accuracy
- **D-19:** Audit general — verifikasi silabus display, guidance tabs, role filtering semua benar
- **D-20:** Coach role access — pastikan Coach hanya lihat data coachee yang di-map ke mereka
- **D-21:** Inactive silabus filtering — pastikan PlanIdp tidak tampilkan silabus/deliverable yang IsActive=false
- **D-22:** Guidance tab access — pastikan coachee tidak bisa akses admin guidance management tab

### Claude's Discretion
- Evidence path history storage mechanism (new column vs separate table)
- Notification dedup structured field approach
- Exact implementation of first-write-wins race condition guard
- PlanIdp audit detail findings dan fix approach

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone & Requirements
- `.planning/ROADMAP.md` — Phase 235 success criteria, dependency chain Phase 234→235
- `.planning/REQUIREMENTS.md` — EXEC-01 through EXEC-05 requirement definitions

### Phase 233 Research (lens untuk audit)
- `docs/audit-v7.7.html` — Dokumen riset perbandingan coaching platform, gap analysis execution flow
- `.planning/phases/233-riset-perbandingan-coaching-platform/233-CONTEXT.md` — Keputusan riset Phase 233

### Phase 234 Context (predecessor decisions)
- `.planning/phases/234-audit-setup-flow/234-CONTEXT.md` — Transaction patterns, all-or-nothing approach, carried forward

### Existing Code (audit targets)
- `Controllers/CDPController.cs` — UploadEvidence (L1086), ApproveDeliverable (L746), RejectDeliverable (L884), HCReviewDeliverable (L1048), SubmitEvidenceWithCoaching (L1943), RecordStatusHistory (L3021), PlanIdp (L57), NotifyReviewersAsync (L988), CreateHCNotificationAsync (L1016)
- `Models/ProtonModels.cs` — ProtonDeliverableProgress model (Status, SrSpvApprovalStatus, ShApprovalStatus, HCApprovalStatus, EvidencePath fields)
- `Views/CDP/PlanIdp.cshtml` — Silabus display dan guidance tabs

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RecordStatusHistory` helper (CDPController:3021) — private method, sudah dipakai di approve/reject/HC review. Perlu dipanggil juga di UploadEvidence dan seed
- `_notificationService.SendAsync()` — notification service sudah ada, tinggal tambah trigger points
- `FileUploadHelper.SaveFileAsync()` — file upload helper sudah ada
- Transaction pattern dari Phase 234 — `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`

### Established Patterns
- Approval chain fields: `SrSpvApprovalStatus`, `ShApprovalStatus`, `HCApprovalStatus` — per-role tracking
- Status flow: Pending → Submitted → Approved/Rejected → (Resubmitted → Submitted → ...)
- Notification try-catch wrap — semua notification calls di-wrap catch agar tidak menggagalkan main operation
- Role check: `UserRoles.GetRoleLevel()` + `HasSectionAccess()` untuk authorization

### Integration Points
- `ProtonDeliverableProgress.EvidencePath` — single string field, perlu history mechanism untuk D-02
- `DeliverableStatusHistory` — existing table, perlu insert di lebih banyak transition points
- `UserNotifications` — used by CreateHCNotificationAsync for dedup check (fragile)

</code_context>

<specifics>
## Specific Ideas

- Coach yang upload evidence, bukan coachee — ini klarifikasi penting untuk semua flow
- Approval chain: Sr SPV dan SH sebagai approver, HC sebagai reviewer — Coach tidak punya peran approve
- File history traceable penting karena audit trail coaching evidence

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 235-audit-execution-flow*
*Context gathered: 2026-03-22*
