# Phase 412: Live Monitoring UI + SignalR - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

UI live + SignalR untuk add/remove peserta di **Monitoring Detail** (milestone v32.5). Deliverable Phase 412:
1. **Kontrol Tambah** — picker modal (konsumsi `GetEligibleParticipantsToAdd` + `AddParticipantsLive` dari 410) → baris baru muncul **live tanpa reload** (PART-05).
2. **Kontrol Hapus** per-baris + **modal konfirmasi keras** (PRMV-02) → panggil `RemoveParticipantLive` (411).
3. **SignalR broadcast** — `participantAdded`/`participantRemoved` ke grup `monitor-{batchKey}` (HANYA setelah `CommitAsync`, dari endpoint 410/411) → semua Admin/HC pemantau ter-update tanpa reload (PLIV-02).
4. **force-kick** — `examRemoved` (reason) ke worker → kunci UI ujian + redirect (PRMV-02).
5. **Panel collapsible "Peserta Dikeluarkan"** — soft-removed (nama/waktu/oleh/alasan) + tombol Restore (PLIV-01).

**TIDAK** termasuk: backend add/remove/restore (410/411 sudah ada), test+UAT lengkap (413). migration=FALSE. File-overlap `AssessmentAdminController.cs` (410/411 sudah complete) → sequential aman.

</domain>

<decisions>
## Implementation Decisions

### Trigger modal konfirmasi keras (D-01) — area dibahas
- **D-01:** **Modal keras** (extra-friction, teks tegas + tombol konfirmasi eksplisit) muncul untuk peserta **InProgress** (sedang ujian → bakal force-kick) **DAN Completed-bersertifikat** (ireversibel terlihat). Peserta **belum-mulai** (jalur hard-delete bersih) → **konfirmasi ringan** biasa (confirm sederhana). Client tentukan tingkat modal dari status baris (`data-status`/`DeriveUserStatus`).

### UX force-kick examRemoved (D-02) — area dibahas
- **D-02:** Worker terima `examRemoved` → **kunci UI ujian + redirect ke daftar Assessment + banner/TempData** "Anda telah dikeluarkan dari ujian ini." **Reuse pola `examClosed`/`AkhiriUjian`** (`AssessmentHub.cs:4379`/`:4430`). **TANPA view/halaman dedicated baru** (selaras 409 D-02 catatan, tapi diputuskan redirect+banner, bukan dedicated page).

### Input alasan di modal hapus (D-03) — area dibahas
- **D-03:** Field **alasan (`RemovalReason`) SELALU tampil** di modal hapus + hint "wajib bila peserta sudah mengerjakan". **Server 411 (D-02) tetap penjaga akhir** (soft-remove tanpa reason → 400). Client tak perlu tebak path; kirim reason apa adanya, server validasi. Sederhana, no client-state guessing.

### Panel "Peserta Dikeluarkan" + Restore (D-04) — area dibahas
- **D-04:** Tombol **Restore = 1-klik langsung** (aksi aman + reversibel) → panggil `RestoreParticipantLive` (411) → baris balik ke tabel aktif **live** (SignalR `participantAdded`). **Tanpa modal konfirmasi** (kurangi friksi). Panel collapsible tampilkan soft-removed: nama/waktu/oleh/alasan.

### Carry-forward dari spec & fase lalu (LOCKED)
- **Broadcast wiring** `participantAdded`/`participantRemoved` **ditambah ke endpoint 410/411** (AddParticipantsLive/RemoveParticipantLive/RestoreParticipantLive) **HANYA setelah `CommitAsync` sukses** (D-04 410 + D-03 411 defer ke sini). `examRemoved` dari RemoveParticipantLive bila target InProgress.
- **DOM handlers** `AssessmentMonitoringDetail.cshtml` (~:1199-1400): `participantAdded` inject `<tr data-session-id>`; `participantRemoved` pindah baris ke panel + update summary count. Mirror DOM logic existing (`workerStarted`/`workerSubmitted`/`progressUpdate`).
- **Grup SignalR** `monitor-{batchKey}` existing (LeaveMonitor/JoinMonitor). `examRemoved` ke target user/grup worker (pola `examClosed`).
- **Exclude removed** dari count aktif sudah di 409 (query) — panel UI baca soft-removed terpisah (PLIV-01 sisi UI).
- RBAC/antiforgery sudah di endpoint (410/411); UI kirim antiforgery token.

### Claude's Discretion
- Bentuk picker modal (reuse komponen assign existing vs baru), styling panel collapsible, bentuk banner force-kick.
- Cara client bedakan tingkat modal (data-attribute status di `<tr>`).
- Optimistic UI vs tunggu SignalR echo (rekomendasi: andalkan SignalR broadcast sebagai sumber kebenaran, hindari double-inject — dedup by sessionId).
- Cakupan Playwright e2e (413 punya e2e lengkap; 412 minimal smoke bila perlu).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & Requirements
- `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` — **§C** SignalR (server→monitor/worker events), **§D** panel Restore + exclude, **§E** guard re-entry (sudah 409).
- `.planning/REQUIREMENTS.md` — PART-05 (add live), PRMV-02 (modal keras + force-kick), PLIV-01 (exclude + panel), PLIV-02 (broadcast live).
- `.planning/ROADMAP.md` — Phase 412 §"Phase Details".
- `.planning/phases/410-...410-CONTEXT.md` (D-04 defer broadcast) + `.../410-01-SUMMARY.md` (AddParticipantsLive JSON shape added[]/skipped[]).
- `.planning/phases/411-...411-CONTEXT.md` (D-03 defer SignalR) + `.../411-01-SUMMARY.md` (RemoveParticipantLive/RestoreParticipantLive JSON outcome shape).
- `.planning/phases/409-...409-CONTEXT.md` (exclude-removed query + guard re-entry).

### Production code (file:line)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (~:1199-1400 DOM handler existing) — tambah picker/remove/modal/panel + handler participantAdded/Removed.
- `Hubs/AssessmentHub.cs` (~:4379/:4430 examClosed/AkhiriUjian pola; grup monitor-{batchKey}) — event participantAdded/participantRemoved + examRemoved.
- `Controllers/AssessmentAdminController.cs` — endpoint 410/411 (AddParticipantsLive/RemoveParticipantLive/RestoreParticipantLive): tambah broadcast post-commit.
- `Views/CMP/StartExam.cshtml` (client worker handler) — terima examRemoved → kunci + redirect.
- `Controllers/AssessmentAdminController.cs` AssessmentMonitoringDetail (~:3273) — data panel soft-removed.

### Lingkungan / workflow
- `CLAUDE.md` — migration=FALSE; verify lokal + Playwright; no push. UAT 412/413 butuh app @5277 + AD-off + seed (Seed Workflow snapshot/restore).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **DOM handler existing** `AssessmentMonitoringDetail.cshtml:1199-1400` (`workerStarted`/`workerSubmitted`/`progressUpdate`) — pola inject/update baris + summary count untuk participantAdded/Removed.
- **`examClosed`/`AkhiriUjian`** `AssessmentHub.cs:4379/4430` — pola server→worker event + client kunci-UI → template examRemoved (D-02).
- **Grup `monitor-{batchKey}`** + Join/LeaveMonitor existing.
- **`GetEligibleParticipantsToAdd`/`AddParticipantsLive`** (410) + **`RemoveParticipantLive`/`RestoreParticipantLive`** (411) — endpoint siap dikonsumsi UI.

### Established Patterns
- **Broadcast post-commit** (hindari notif untuk tx rollback) — pola dari notif existing.
- **Antiforgery token** di AJAX POST (pola form admin existing).
- **DeriveUserStatus** (:2715) — tentukan status baris (untuk tingkat modal D-01).

### Integration Points
- Konsumsi backend 410 (add+eligible) + 411 (remove/restore) + 409 (exclude query).
- Broadcast ditambah ke endpoint 410/411 (post-commit) — modifikasi minimal endpoint existing.
- Worker StartExam terima examRemoved.

</code_context>

<specifics>
## Specific Ideas

- Banner force-kick verbatim: "Anda telah dikeluarkan dari ujian ini." (selaras pesan guard 409).
- Restore 1-klik (aman/reversibel); modal keras hanya InProgress + Completed-cert.
- Field alasan selalu tampil; server 411 penjaga.
- Dedup SignalR by sessionId (hindari double-inject optimistic + echo).

</specifics>

<deferred>
## Deferred Ideas

- **xUnit + Playwright e2e lengkap** (add live + remove modal + worker kick + restore) — Phase 413.
- **Halaman dedicated "Anda dikeluarkan"** — ditolak (D-02 pakai redirect+banner); buka bila UAT minta.
- **IN-02 (411): EditAssessment query exclude soft-removed** — kandidat sini bila relevan ke konsistensi tampilan; evaluasi planner (atau tetap 413/backlog).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` — bukan scope 412.

</deferred>

---

*Phase: 412-live-monitoring-ui-signalr*
*Context gathered: 2026-06-21*
