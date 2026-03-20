---
phase: 208
slug: grouped-view-structure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-20
---

# Phase 208 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | Buka browser, navigasi ke /Admin/RenewalCertificate |
| **Full suite command** | Manual testing checklist (GRP-01 s/d GRP-04) |
| **Estimated runtime** | ~120 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Buka browser, cek visual accordion dan toggle
- **After every plan wave:** Full checklist GRP-01 s/d GRP-04
- **Before `/gsd:verify-work`:** Semua GRP requirements pass
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 208-01-01 | 01 | 1 | GRP-01 | smoke | Buka /Admin/RenewalCertificate, verifikasi accordion cards muncul | N/A manual | ⬜ pending |
| 208-01-02 | 01 | 1 | GRP-02 | smoke | Expand group, verifikasi header: judul + kategori + badge counts | N/A manual | ⬜ pending |
| 208-01-03 | 01 | 1 | GRP-03 | smoke | Buka halaman: semua collapsed. Klik header: expand/collapse | N/A manual | ⬜ pending |
| 208-01-04 | 01 | 1 | GRP-04 | smoke | Expand group, verifikasi kolom: Checkbox, Nama, Valid Until, Status, Aksi only | N/A manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Project uses manual browser testing — no automated test framework to install.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Grouped accordion display | GRP-01 | Visual UI verification | Buka halaman, verifikasi tabel flat tidak ada, accordion cards muncul |
| Header content & badges | GRP-02 | Visual content check | Expand group, verifikasi judul, kategori/sub-kategori, badge count |
| Collapse/expand toggle | GRP-03 | Interactive behavior | Buka halaman: semua collapsed. Klik header: expand. Klik lagi: collapse |
| Simplified table columns | GRP-04 | Visual column check | Expand group, verifikasi hanya kolom Checkbox, Nama, Valid Until, Status, Aksi |

---

## Validation Sign-Off

- [ ] All tasks have manual verify steps defined
- [ ] Sampling continuity: every task commit triggers browser check
- [ ] Wave 0: no automated infrastructure needed
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
