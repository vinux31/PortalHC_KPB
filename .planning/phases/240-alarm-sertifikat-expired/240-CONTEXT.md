# Phase 240: Alarm Sertifikat Expired - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Banner alert di Home/Index + bell notification tipe CERT_EXPIRED untuk HC/Admin. Banner menampilkan jumlah sertifikat Expired (merah) dan Akan Expired ≤30 hari (kuning) dengan link ke RenewalCertificate. Notifikasi bell di-generate on page load untuk setiap sertifikat expired yang belum pernah dinotifikasikan.

</domain>

<decisions>
## Implementation Decisions

### Posisi & Desain Banner
- **D-01:** Banner ditampilkan setelah greeting, sebelum card progress/upcoming events
- **D-02:** Banner tidak bisa di-dismiss — selalu tampil selama ada sertifikat bermasalah, hilang otomatis jika semua sudah diurus
- **D-03:** Dua baris terpisah: Expired (latar merah) dan Akan Expired (latar kuning), masing-masing dengan link "Lihat Detail"

### Deduplikasi Notifikasi
- **D-04:** Unique key = UserNotification.Type (CERT_EXPIRED) + source record ID di message. Satu notifikasi per sertifikat expired per user HC/Admin
- **D-05:** Notifikasi lama tidak auto-cleanup saat sertifikat di-renew — user bisa dismiss manual

### Template & Konten Notifikasi
- **D-06:** Format per-sertifikat: "Sertifikat [Judul] milik [Nama Pekerja] telah expired"
- **D-07:** ActionUrl mengarah ke Admin/RenewalCertificate (sama dengan link "Lihat Detail" di banner)
- **D-08:** Notifikasi bell hanya untuk sertifikat expired, bukan akan expired (sesuai Out of Scope di REQUIREMENTS.md)

### Data Source
- **D-09:** Query dari TrainingRecord + AssessmentSession — konsisten dengan RenewalCertificate page
- **D-10:** Banner dan notifikasi hanya tampil untuk user dengan role HC atau Admin

### Claude's Discretion
- Query optimization strategy (single query vs separate)
- Banner HTML/CSS styling details selama sesuai warna merah/kuning
- Template registration di NotificationService

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Notification Infrastructure
- `Services/INotificationService.cs` — Interface dengan SendAsync dan SendByTemplateAsync
- `Services/NotificationService.cs` — Implementasi notification service dengan template system
- `ViewComponents/NotificationBellViewComponent.cs` — Bell icon ViewComponent di navbar
- `Controllers/NotificationController.cs` — AJAX endpoints untuk bell dropdown
- `Models/UserNotification.cs` — Model notifikasi dengan Type, Title, Message, ActionUrl

### Certificate/Renewal Data
- `Models/TrainingRecord.cs` — ValidUntil, IsExpired, IsExpiringSoon (≤30 hari), CertificateType
- `Models/AssessmentSession.cs` — ValidUntil untuk assessment-generated certificates
- `Controllers/AdminController.cs` — RenewalCertificate action (target ActionUrl)
- `Views/Admin/RenewalCertificate.cshtml` — Target page untuk "Lihat Detail"

### Home/Index
- `Controllers/HomeController.cs` — Index action, DashboardHomeViewModel
- `Views/Home/Index.cshtml` — Dashboard view (banner insertion point)

### Requirements
- `.planning/REQUIREMENTS.md` — ALRT-01..04, NOTF-01..03

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `NotificationService.SendByTemplateAsync` — template-based notification, perlu register template CERT_EXPIRED
- `NotificationBellViewComponent` — sudah render badge count + dropdown via AJAX
- `TrainingRecord.IsExpired` / `TrainingRecord.IsExpiringSoon` — computed properties siap pakai

### Established Patterns
- Notification trigger di controller action (lihat CDPController coaching triggers Phase 131)
- ViewComponent untuk navbar elements (NotificationBellViewComponent)
- Server-side badge count refresh on page load

### Integration Points
- HomeController.Index — inject certificate alert data ke ViewModel
- Home/Index.cshtml — render banner partial setelah greeting section
- NotificationService — register template baru CERT_EXPIRED

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 240-alarm-sertifikat-expired*
*Context gathered: 2026-03-23*
