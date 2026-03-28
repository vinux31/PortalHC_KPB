---
phase: 269
slug: loading-overlay-saat-koneksi-signalr-belum-ready-di-startexam
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-28
---

# Phase 269 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC + SignalR — no JS test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && manual browser verification` |
| **Estimated runtime** | ~5 seconds (build) + manual |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Build + manual browser check
- **Before `/gsd:verify-work`:** Full manual UAT
- **Max feedback latency:** 5 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 269-01-01 | 01 | 1 | D-01,D-02,D-03 | manual | `dotnet build` | N/A | ⬜ pending |
| 269-01-02 | 01 | 1 | D-04,D-05 | manual | `dotnet build` | N/A | ⬜ pending |
| 269-01-03 | 01 | 1 | D-06 | manual | `dotnet build` | N/A | ⬜ pending |
| 269-01-04 | 01 | 1 | D-07 | manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Overlay tampil & block interaksi | D-01,D-03 | Visual + interaction behavior | Buka StartExam, verifikasi overlay tampil, coba klik soal — harus blocked |
| Teks status berubah real-time | D-02 | Visual transition | Perhatikan teks overlay berubah dari "Menghubungkan..." ke "Terhubung!" |
| Min 1 detik + fade-out | D-04 | Timing behavior | Overlay harus tampil min 1 detik lalu fade-out ~300ms |
| Timer akurat selama overlay | D-05 | Timer sync | Bandingkan timer saat overlay hilang dengan expected elapsed |
| Error state saat gagal konek | D-06 | Network failure | Simulasi network offline, verifikasi error state + tombol Muat Ulang |
| Resume exam overlay | D-07 | Tab close/reopen | Tutup tab, buka lagi — overlay harus tampil sampai hub reconnect |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
