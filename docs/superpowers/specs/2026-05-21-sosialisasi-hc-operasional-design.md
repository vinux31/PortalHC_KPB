# Sosialisasi HC Operasional — Slide Deck — Design Spec

**Date**: 2026-05-21
**Author**: Rino + Claude (brainstorming session, follow-up dari panduan-operasional-hc)
**Status**: Approved (pending implementation)

---

## 1. Latar Belakang

`docs/Panduan-Operasional-HC-PortalHC-KPB.html` v1.0 sudah ter-ship sebagai **long-scroll reference doc**. User feedback: file terlalu rumit untuk dipresentasikan saat sosialisasi tatap muka tim HC. Mismatch antara format reference doc dan format presentasi training.

Solusi: buat **slide deck terpisah** khusus untuk presentasi training HC. Panduan tetap berfungsi sebagai self-service reference saat HC butuh detail.

## 2. Tujuan

- **Format presentasi training** — 23 slide untuk session 35-50 menit
- **Audience HC tim** saat sosialisasi onboarding atau refresher
- **Selektif top fitur** — skip detail teknis, fokus alur kerja + impact
- **Visual & punchy** — card grid, icon, big number, hindari wall of text
- **Tone training** — warm/edukatif, bukan dokumentasi formal
- **Self-contained presentasi** — bisa di-projector langsung tanpa instruktur baca dokumen

## 3. Non-Tujuan

- **BUKAN reference doc detail** — itu ranah Panduan Operasional HC v1.0
- **BUKAN sosialisasi general** untuk semua role — itu ranah `Sosialisasi-Aplikasi-PortalHC-KPB.html`
- **BUKAN deep-dive technical** — tidak include endpoint URL, JSON body, query string
- **TIDAK mengubah** panduan atau sosialisasi general existing
- **TIDAK** cover semua fitur — selektif top 10-15 fitur paling sering dipakai HC harian

## 4. Audience

Tim HC (Human Capital) saat sosialisasi tatap muka — training session 35-50 menit. Bisa pakai untuk:
- Onboarding HC baru
- Refresher annual / quarterly
- Sosialisasi fitur baru milestone update

## 5. File Output

- **Path**: `docs/Sosialisasi-HC-Operasional.html`
- **Title**: "Sosialisasi Operasional HC — Portal HC KPB"
- **Format**: Standalone HTML slide deck 16:9
- **Estimasi**: ~2000-2500 baris HTML

## 6. Style & Visual

**Fresh palette** (beda dari 2 file existing):
- Primary: **Teal** `#0d9488` (calming, training-friendly)
- Secondary: **Amber** `#f59e0b` (warmth, attention)
- Success: **Green** `#10b981`
- Warning: **Orange** `#ea580c`
- Danger: **Red** `#dc2626`
- Background: gradient soft `#f0fdfa` → `#fff`
- Dark mode: deep teal `#0f2027` → `#203a43`

**Layout per slide**:
- 16:9 aspect ratio (1280x720)
- Slide header: title + section eyebrow + slide counter
- Slide body: max 3-4 elemen utama (bullet, card, diagram)
- Hindari wall of text → break ke card grid / icon + label
- Decorative blob bg subtle
- Animation: cross-fade smooth transition

**Controls**:
- Sticky bottom: prev / next / counter + slide navigator dots
- Top right: dark mode toggle + fullscreen + print PDF
- Keyboard nav: arrow keys, space, esc

## 7. Struktur 23 Slide

### INTRO (4 slide)
1. **Cover** — "Sosialisasi Operasional HC Portal KPB" + tagline + role badge HC + date
2. **Selamat Datang** — what we'll cover + agenda + duration estimate
3. **Role HC di Portal** — visual L2 dalam ladder 6-level + 4 area authority (CMP/CDP/Data/Admin)
4. **Alur Kerja Harian HC** — flow diagram big picture: check notif → review evidence → monitor assessment → schedule renewal → resolve issues

### CMP (4 slide)
5. **CMP Overview** — 4 sub-modul yang HC pegang (Records, Analytics, Pre/Post, Budget) dengan icon grid
6. **Records Team** — monitoring tim lintas section + filter cascade + export
7. **Analytics Dashboard** — compliance chart + fail rate + trend + expiring soon
8. **Pre/Post Test + Gain Score** — alur: Pre → Training → Post → Gain Score → Item Analysis

### CDP (4 slide)
9. **CDP Overview + Reviewer Chain** — visual chain Sr Spv → Section Head → **HC (Final)** dengan badge prominent
10. **Coaching Proton Dashboard** — global view + filter + drill to coachee + bottleneck tab
11. **Histori Proton + Export** — riwayat per periode + tombol export Excel/PDF + per coachee
12. **Renewal Certificate Lifecycle** — flow: expired → schedule → assess → certificate baru

### KELOLA DATA (2 slide)
13. **Silabus + Guidance Files** — kelola silabus per Bagian/Unit/Track + upload guidance dokumen
14. **Override Data Pekerja** — kapan dipakai (sync gagal) + warning (sensitif, ter-audit)

### ADMIN PANEL (5 slide)
15. **Admin Panel Map** — 16 menu visual grid dengan kategori (Workers, Assessment, Mapping, Files, Maintenance)
16. **Kelola Pekerja + Bank Soal** — operasi paling sering: CRUD pekerja + import soal Excel
17. **Create + Monitoring Assessment** — alur create jadwal → assign peserta → monitor real-time → force-close kalau perlu
18. **Coach-Coachee Mapping + Workload** — assign mapping + monitor beban + auto-suggest reassign
19. **Maintenance Mode + Audit Log** — toggle scope All/Specific + audit log investigation playbook

### CLOSING (4 slide)
20. **Notifikasi & Workflow** — bell icon + 9 event auto-notif matrix singkat
21. **Tugas HC Cepat** — daily/weekly/monthly checklist visual ringkas
22. **Reference Card** — link ke Panduan Operasional HC + Panduan Penggunaan Website + URL cheatsheet
23. **Q&A / Terima Kasih** — kontak person + thank you

## 8. Per-Slide Design Principles

- **Max 3-4 bullet point** per slide. Lebih dari itu → split ke multiple slide
- **Visual ≥ text** — icon, card, diagram > paragraph
- **Big number / metric** untuk impact (mis: "16 menu", "9 event notif")
- **Color-coded callout** sparingly (info/warn/success)
- **Cross-reference Panduan** di slide-slide complex — "Detail lengkap → Panduan HC §X.Y"
- **Tidak ada endpoint URL** — pakai "klik menu Kelola Pekerja" not "/Worker/ManageWorkers"

## 9. Workflow Build

1. Scaffold HTML skeleton — style palette + cover + closing template + controls + keyboard nav script
2. Intro slides 1-4
3. CMP slides 5-8
4. CDP slides 9-12
5. Kelola Data slides 13-14
6. Admin Panel slides 15-19
7. Closing slides 20-23
8. Visual review + dark mode test + print test + tag

Commit per group. Tag `sosialisasi-hc-operasional-v1.0` setelah verify.

## 10. Acceptance Criteria

- File `docs/Sosialisasi-HC-Operasional.html` exist, 23 slide ter-render
- Slide navigation work: keyboard (arrow keys, space, esc) + button (prev/next)
- Slide counter update saat navigate
- Sticky bottom controls + top utility bar functional
- Dark mode toggle work, persist via state (in-memory OK, no localStorage required)
- Print preview render 23 halaman (1 slide = 1 halaman A4 landscape)
- Visual: tidak ada slide dengan wall of text (max 3-4 element)
- Cross-reference ke Panduan ada di slide complex (≥ 5 slide pakai cross-ref)

## 11. Out of Scope (Future)

- Animation per element (slide entrance OK, element-level entrance optional)
- Embed video / screenshot — versi v1 visual elements simple (icon, card, diagram)
- Speaker notes (versi v1 tanpa, kalau perlu bisa ditambah v1.1)
- Translation ke English
- Auto-advance mode (manual nav only)

## 12. Decisions Log

| Decision | Pilihan | Alasan |
|----------|---------|--------|
| Format | Slide deck (B) | Sesuai feedback "panduan terlalu rumit untuk presentasi" |
| Branch | Same worktree (A) | 1 PR untuk 2 file complement (panduan + slide) |
| Style | Fresh palette (C) | Beda dari sosialisasi general (Pertamina navy/red) dan panduan (biru), tone training warm |
| Scope | Top fitur 20-25 slide (B) | Sweet spot durasi 35-50 menit, padat tapi tidak overwhelming |
| Slide count | 23 | 4+4+4+2+5+4 distribusi seimbang |
| Detail URL | Skip | Push ke panduan reference; presentasi fokus alur, bukan endpoint |
