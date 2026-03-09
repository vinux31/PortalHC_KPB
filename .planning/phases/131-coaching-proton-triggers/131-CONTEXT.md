# Phase 131: Coaching Proton Triggers - Context

**Gathered:** 2026-03-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire notification triggers into existing Coaching Proton controller actions: mapping assign/edit/deactivate and deliverable submit/approve/reject/all-complete. Migrate ProtonNotification code references to UserNotification. No new UI — triggers call existing NotificationService.

</domain>

<decisions>
## Implementation Decisions

### Notification Message Content
- Bahasa Indonesia, tone semi-formal ("Anda telah ditunjuk sebagai coach untuk [Nama]")
- Sertakan nama orang spesifik di setiap message
- Reject deliverable: TIDAK sertakan alasan rejection di message — user klik untuk lihat detail
- Mapping edit/deactivate: cukup info "mapping diubah/dinonaktifkan" tanpa detail perubahan
- Submit deliverable ke reviewer: sebut nama coachee saja ("Deliverable [Nama Coachee] telah disubmit untuk review")
- All-complete ke HC: sebut nama coachee saja ("Semua deliverable [Nama Coachee] telah selesai")
- Assign ke coach: sebut nama coachee ("Anda ditunjuk sebagai coach untuk [Nama Coachee]")

### ActionUrl Deep Links
- Semua coaching proton notifications link ke `/CDP/CoachingProton`
- Satu target URL untuk semua event types — simpel dan konsisten

### Recipient Targeting
- COACH-01 (assign): notifikasi ke coach
- COACH-02 (edit): notifikasi ke coach dan coachee
- COACH-03 (deactivate): notifikasi ke coach dan coachee
- COACH-04 (submit deliverable): notifikasi ke SrSpv/SH yang satu section (AssignmentSection) dengan coachee
- COACH-05 (approve): notifikasi ke coach dan coachee saja
- COACH-06 (reject): notifikasi ke coach dan coachee saja
- COACH-07 (all complete): notifikasi ke semua HC users

### ProtonNotification Migration
- New all-complete notifications pakai UserNotification (bukan ProtonNotification lagi)
- Data lama di tabel ProtonNotifications dibiarkan (tidak di-migrate)
- Code references ke ProtonNotification diganti ke UserNotification (CDPController dll)
- Tabel ProtonNotifications tetap di DB — drop table nanti (bukan sekarang)
- Model ProtonNotification code references dihapus, model class biarkan dulu

### Claude's Discretion
- Exact notification template wording per trigger (within guidelines above)
- How to resolve SrSpv/SH users matching coachee's AssignmentSection
- Error handling if recipient lookup fails (fail silently per existing pattern)
- Order of trigger insertion in controller actions

</decisions>

<specifics>
## Specific Ideas

- Templates sudah ada di NotificationService (COACH_ASSIGNED, COACH_EVIDENCE_SUBMITTED, etc.) — update wording sesuai keputusan di atas
- Trigger points sudah identified: AdminController (mapping actions), ProtonDataController (UpdateDeliverableStatus), CDPController (ProtonNotification replacement)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `NotificationService.SendAsync` and `SendByTemplateAsync` — ready to call from any controller
- `INotificationService` already injected via DI
- Existing templates in NotificationService._templates dictionary
- `NotificationController` with List/MarkAsRead/Dismiss endpoints (Phase 130)

### Established Patterns
- ProtonNotification in CDPController: checks `alreadyNotified`, creates for all HC users — same pattern needed for UserNotification
- AdminController mapping actions: CoachCoacheeMappingAssign (line 2920), CoachCoacheeMappingEdit (line 3012), CoachCoacheeMappingDeactivate (line 3145)
- ProtonDataController.UpdateDeliverableStatus: handles Approved/Rejected status changes (line 894+)
- CoachCoacheeMapping has AssignmentSection field for section-based recipient targeting

### Integration Points
- AdminController needs INotificationService injected (for mapping triggers)
- ProtonDataController needs INotificationService injected (for deliverable triggers)
- CDPController ProtonNotification code replaced with UserNotification via NotificationService
- UserRoles.SrSupervisor and UserRoles.SectionHead for role-based recipient lookup

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 131-coaching-proton-triggers*
*Context gathered: 2026-03-09*
