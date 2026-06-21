---
phase: 412-live-monitoring-ui-signalr
milestone: v32.5
secured: 2026-06-21
asvs_level: 1
block_on: high
threats_total: 17
threats_closed: 17
threats_open: 0
unregistered_flags: 0
files_audited:
  - Controllers/AssessmentAdminController.cs
  - Models/AssessmentMonitoringViewModel.cs
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - Views/CMP/StartExam.cshtml
  - Hubs/AssessmentHub.cs
  - HcPortal.Tests/NoopHubContext.cs
---

# Phase 412: Security Verification — Live Monitoring UI + SignalR

**Phase:** 412 — Live Monitoring UI + SignalR
**Milestone:** v32.5 (Flexible Add/Remove Peserta)
**ASVS Level:** 1
**Block-on:** HIGH
**Threats Closed:** 17/17
**threats_open:** 0

Audit ini memverifikasi mitigasi yang dideklarasikan di `<threat_model>` dari 3 PLAN (412-01/02/03) benar-benar hadir di kode terimplementasi. Implementation files READ-ONLY (tidak dimodifikasi). Tiap threat diklasifikasi per disposisi (mitigate / accept) lalu dibuktikan dengan file:line.

---

## Threat Verification — Plan 412-01 (Backend broadcast wiring)

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-412-01 | Tampering (broadcast pre-commit) | mitigate | CLOSED | `AssessmentAdminController.cs:2459` `transaction.CommitAsync()`; broadcast `participantAdded` di `:2488` (post-commit). `RemoveParticipantLive` broadcast `:2616/:2629` setelah `outcome.Ok` core-commit (`:2611-2612`). `RestoreParticipantLive` broadcast `:2686` setelah `SaveChangesAsync` (`:2673`). Tak ada SendAsync di dalam blok try-tx (rollback `:2463`). |
| T-412-02 | Information Disclosure (cross-user kick) | mitigate | CLOSED | `examRemoved` dikirim ke `Clients.User(targetUserId)` (`:2642`) dengan `targetUserId = session.UserId` di-resolve server dari DB (`:2594`) — bukan input client. Identik preseden `examClosed` (`:5013`) / `sessionReset` (`:4948`). SignalR auto-map userId→koneksi. |
| T-412-03 | Information Disclosure (bocor ke batch lain) | mitigate | CLOSED | Semua broadcast group-scoped `monitor-{batchKey}` (`:2488/:2616/:2629/:2686`), batchKey dari `session.Title|Category|Schedule.Date`. `JoinMonitor` role-gate Admin/HC sebelum `AddToGroupAsync`: `Hubs/AssessmentHub.cs:56` `if (!roles.Contains("Admin") && !roles.Contains("HC")) return;` lalu `:58` add-to-group. |
| T-412-04 | Elevation (non-Admin trigger broadcast) | mitigate | CLOSED | Endpoint pemicu broadcast ber-`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`: AddParticipantsLive `:2353-2355`, RemoveParticipantLive `:2571-2573`, RestoreParticipantLive `:2649-2651`. Tak diubah fase ini. Spec §H konsisten (design doc :106). |
| T-412-05 | Information Disclosure (kick non-InProgress) | mitigate | CLOSED | `examRemoved` hanya bila `wasInProgress` (`:2641`), flag di-capture SEBELUM core mutasi (`:2593`: `StartedAt != null && CompletedAt == null && RemovedAt == null`). |
| T-412-06 | Spoofing (event palsu dari client) | accept | CLOSED (by-design) | Server→client only. `participantAdded`/`participantRemoved`/`examRemoved` BUKAN hub method client-invokable — tak ada metode publik di `AssessmentHub.cs` yang memancarkannya. Anti-spoofing struktural (RESEARCH §Security). Disposisi accept terdokumentasi di sini. |
| T-412-07 | Tampering / XSS (RemovalReason di payload) | mitigate | CLOSED | Server hanya MENYALIN `session.RemovalReason` ke payload (`:2624/:2637`); ViewModel typed `string?` (`RemovedParticipantViewModel`). Encoding-at-render dibuktikan di Plan 02 (T-412-08). Carry T-409-10. |

---

## Threat Verification — Plan 412-02 (Monitoring Detail UI)

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-412-08 | Tampering / Stored XSS (panel Alasan/Nama) | mitigate | CLOSED | Razor auto-encode (TANPA Html.Raw): `AssessmentMonitoringDetail.cshtml:448` `@r.FullName`, `:453` `@(string.IsNullOrEmpty(r.RemovalReason) ? "—" : r.RemovalReason)`, `:458` `@r.FullName`. JS inject via `.textContent`: `:2069` nama, `:2087` oleh, `:2094` `tdReason.textContent = data.removalReason` (komentar XSS-safe). 0 `Html.Raw` untuk field user-input; tak ada `innerHTML` untuk reason. Carry T-409-10. |
| T-412-09 | Spoofing / CSRF (POST add/remove/restore) | mitigate | CLOSED | Semua POST AJAX kirim `__RequestVerificationToken` via `getToken()` (`:1045-1046`): Add `:1370/:1377`, Remove `:1461/:1466/:1471`, Restore `:1529/:1531/:1534`. Endpoint tetap `[ValidateAntiForgeryToken]` (controller :2355/:2573/:2651). |
| T-412-10 | Elevation (non-Admin akses kontrol UI) | mitigate | CLOSED | Action + endpoint ber-`[Authorize(Roles="Admin, HC")]` (server-side gate). UI markup hanya di-render untuk role berwenang; kontrol klien bukan boundary keamanan (server-authoritative). |
| T-412-11 | Tampering (userIds di luar eligible) | mitigate | CLOSED | `AddParticipantsLive` server-authoritative: re-resolve eligible + skip idempotent di dalam transaksi (`:2356-2459`); UI checklist tak dipercaya. JSON membedakan `added[]` vs `skipped[]`. |
| T-412-12 | Information Disclosure (reason ke admin lain) | accept | CLOSED (by-design) | Panel "Peserta Dikeluarkan" memang untuk Admin/HC (audit transparan); reason di-encode (T-412-08). Hanya menjangkau grup `monitor-{batchKey}` yang role-gated (T-412-03). Disposisi accept terdokumentasi. |
| T-412-13 | Tampering (double-inject baris dobel) | mitigate | CLOSED | Dedup sebelum inject: handler `participantAdded` cek `tbody:not(#tbodyRemoved) tr[data-session-id]` (`:2028`); fallback 3-detik dibatalkan saat echo datang (`monClearAddedFallback`). Echo SignalR = sumber kebenaran (Pitfall 4). |

---

## Threat Verification — Plan 412-03 (StartExam force-kick worker)

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-412-14 | Tampering / XSS (payload.reason di modal worker) | mitigate | CLOSED | `StartExam.cshtml:1328` `document.getElementById('examRemovedReasonValue').textContent = payload.reason` (komentar "XSS-safe via textContent, BUKAN innerHTML"). Modal markup `:384-398` pakai elemen kosong yang diisi via textContent. Tak ada innerHTML untuk reason. |
| T-412-15 | Spoofing (examRemoved palsu ke worker) | accept | CLOSED (by-design) | Server→client only; tak ada hub method client-invokable untuk `examRemoved`. Guard re-entry server (409 — `IsParticipantRemoved` seam StartExam/SubmitExam/SaveAnswer) tetap penjamin korektnes bila event hilang/dipalsukan. Handler ber-guard idempoten `if (examClosed) return` (`:1320`, var reuse `:1280`). Disposisi accept terdokumentasi. |
| T-412-16 | DoS (force-kick salah target) | mitigate | CLOSED | Plan 01 kirim `Clients.User(targetUserId)` dengan `targetUserId = session.UserId` server-authoritative (`controller:2594/:2642`) + hanya bila `wasInProgress` (`:2641`). Handler ber-guard `if (examClosed) return` (`StartExam.cshtml:1320`) → idempoten, tak re-trigger. |
| T-412-17 | Repudiation (worker klaim tak tahu) | mitigate | CLOSED | Audit `RemovedBy`/`RemovalReason` di server (411 PLIV-03; set di `RemoveParticipantCoreAsync`, audit-log `RestoreParticipantLive:2675`). Notifikasi jelas: modal non-dismissable (`:384` backdrop=static) + banner verbatim "Anda telah dikeluarkan dari ujian ini." (`:396`) + countdown 5 detik + redirect `@Url.Action("Assessment","CMP")` (`:1332`). |

---

## Focused Verification (per audit brief)

| Concern | Verdict | Evidence |
|---------|---------|----------|
| XSS via RemovalReason/fullName/nip — textContent (JS) / @-encode (Razor), NO innerHTML/Html.Raw | PASS | Razor `:448/:453/:458` auto-encode (0 Html.Raw); JS `:1978/:2069/:2087/:2094` textContent; StartExam `:1328` textContent. T-409-10 carry intact. |
| examRemoved targets ONLY removed user (Clients.User(session.UserId), server-resolved) | PASS | `:2642` `Clients.User(targetUserId)`, `targetUserId = session.UserId` (`:2594`). No broadcast/group kick for force-kick. |
| Broadcast strictly post-CommitAsync (no rollback-state leak) | PASS | Add: commit `:2459` → broadcast `:2488`. Remove: core-commit then broadcast `:2616`. Restore: `SaveChangesAsync :2673` → broadcast `:2686`. |
| Antiforgery on AJAX POST (getToken; endpoints [ValidateAntiForgeryToken]) | PASS | `getToken()` `:1045`; tokens sent `:1377/:1471/:1534`; endpoints `:2355/:2573/:2651`. |
| Monitor group authz (JoinMonitor role-gated Admin/HC) | PASS | `AssessmentHub.cs:56` role check BEFORE `AddToGroupAsync :58`. Non-Admin/HC `return` early → cannot subscribe to participant events. |
| Broadcast payload no sensitive fields (no Score/cert/answers) | PASS | participantAdded = `{sessionId,userId,fullName,nip,status}` (`:2488-2495`); participantRemoved = `{sessionId,mode,fullName,nip,removedAt,removedBy,removalReason}` (`:2616-2625`); examRemoved = `{reason}` (`:2642`). No Score/NomorSertifikat/answers. |
| No JSON-shape change to 410/411 endpoints | PASS | Broadcast inserted BEFORE unchanged `return Json(...)` (Add `:2512`, Remove `:2644`, Restore `:2696`). Suite 597/597 (FlexibleParticipant lock tests green). |
| NoopHubContext stub test-only (not in production path) | PASS | `HcPortal.Tests/NoopHubContext.cs` `internal sealed` in `namespace HcPortal.Tests` (`:11-14`). No replica logic (pure no-op SendCoreAsync `:36`). Production controller injects real `IHubContext<AssessmentHub>` via DI ctor (`:41/:51`). No reference to NoopHubContext under `Controllers/`. |

---

## Unregistered Flags

None. SUMMARY files (412-01/02/03) tidak mendeklarasikan `## Threat Flags` baru yang tak ter-map ke threat ID. Semua attack surface (broadcast, force-kick, panel reason render) ter-cover T-412-01..17.

---

## Migration / Deploy

migration=FALSE (view + controller broadcast wiring + ViewModel only; tidak ada Migrations/Data perubahan). Tak ada gate keamanan tambahan untuk promosi.

---

## Verdict

**SECURED — 17/17 threats mitigated, threats_open: 0.**

Seluruh disposisi terpenuhi: 13 `mitigate` dibuktikan hadir di kode (file:line), 4 `accept` (T-412-06/12/15 dan struktur server→client-only) terdokumentasi by-design dengan rasional anti-spoofing/audit. Tidak ada HIGH threat tanpa mitigasi → tidak ada blocker (block_on=HIGH). Konsisten dengan 412-REVIEW.md (0 Critical/0 Warning, 3 Info non-blocking) dan 412-VERIFICATION.md (4/4 truths programatik; 4 alur live runtime deferred ke Phase 413 — bukan gap keamanan).

_Audited: 2026-06-21 · gsd-security-auditor · ASVS L1 · block_on=HIGH_
