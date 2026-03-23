---
phase: 240
slug: alarm-sertifikat-expired
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-23
---

# Phase 240 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (proyek tidak menggunakan automated test framework) |
| **Config file** | none |
| **Quick run command** | Jalankan aplikasi, login sebagai HC/Admin, buka Home/Index |
| **Full suite command** | Jalankan seluruh use-case flows secara manual |
| **Estimated runtime** | ~5 minutes per full manual pass |

---

## Sampling Rate

- **After every task commit:** Build + spot-check di browser
- **After every plan wave:** Full use-case flow manual
- **Before `/gsd:verify-work`:** Semua 7 requirements pass
- **Max feedback latency:** ~60 seconds (build + page load)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 240-01-01 | 01 | 1 | ALRT-01 | manual-only | — | N/A | ⬜ pending |
| 240-01-02 | 01 | 1 | ALRT-02 | manual-only | — | N/A | ⬜ pending |
| 240-01-03 | 01 | 1 | ALRT-03 | manual-only | — | N/A | ⬜ pending |
| 240-01-04 | 01 | 1 | ALRT-04 | manual-only | — | N/A | ⬜ pending |
| 240-02-01 | 02 | 1 | NOTF-01 | manual-only | — | N/A | ⬜ pending |
| 240-02-02 | 02 | 1 | NOTF-02 | manual-only | — | N/A | ⬜ pending |
| 240-02-03 | 02 | 1 | NOTF-03 | manual-only | — | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Tidak ada test framework yang perlu disiapkan — proyek menggunakan manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Banner expired merah muncul di Home/Index | ALRT-01, ALRT-02 | No automated test framework | Login HC/Admin, buka Home/Index, verifikasi banner merah + kuning |
| Link "Lihat Detail" navigasi ke RenewalCertificate | ALRT-03 | Browser navigation | Klik link, verifikasi URL /Admin/RenewalCertificate |
| Banner tidak muncul saat tidak ada expired | ALRT-04 | Requires clean data state | Pastikan tidak ada sertifikat expired, verifikasi banner absent |
| Notifikasi CERT_EXPIRED terbuat | NOTF-01 | DB + UI check | Buat sertifikat expired baru, reload Home/Index, cek bell |
| Notifikasi ke semua HC + Admin | NOTF-02 | Multi-user check | Login sebagai HC dan Admin berbeda, verifikasi notifikasi muncul |
| Bell menampilkan nama pekerja + judul | NOTF-03 | UI content check | Buka bell dropdown, verifikasi format teks |

---

## Validation Sign-Off

- [ ] All tasks have manual verification steps documented
- [ ] Sampling continuity: setiap task diverifikasi sebelum lanjut
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
