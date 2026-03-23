# CDP Module — UI Review

**Audited:** 2026-03-23
**Baseline:** Abstract 6-pillar standards (no UI-SPEC)
**Screenshots:** Tidak diambil (dev server tidak aktif)
**Cakupan:** 9 halaman CDP (Index, Dashboard, PlanIdp, CoachingProton, EditCoachingSession, HistoriProton, HistoriProtonDetail, CertificationManagement, Deliverable)

---

## Pillar Scores

| Pillar | Score | Key Finding |
|--------|-------|-------------|
| 1. Copywriting | 3/4 | Mayoritas label sudah Bahasa Indonesia, ada beberapa label Inggris yang inkonsisten |
| 2. Visuals | 3/4 | Hierarki visual jelas dengan card, icon, badge; beberapa halaman kurang breadcrumb |
| 3. Color | 3/4 | Penggunaan warna Bootstrap konsisten; ada hardcoded color di loading spinner |
| 4. Typography | 3/4 | Ukuran font terkendali, weight konsisten; hanya 2-3 ukuran per halaman |
| 5. Spacing | 3/4 | Spacing Bootstrap standar dipakai konsisten; sedikit arbitrary value |
| 6. Experience Design | 3/4 | Loading, empty, error state tertangani baik; beberapa halaman tanpa loading state |

**Overall: 18/24**

---

## Top 3 Priority Fixes

1. **Inkonsistensi bahasa Inggris/Indonesia** — Pengguna melihat campuran bahasa yang membingungkan — Terjemahkan "Pending Actions", "Pending Approvals", "Monitor deliverable progress and approval status", "Result", "Need Improvement/Suitable/Good/Excellence" ke Bahasa Indonesia
2. **Index.cshtml dan Dashboard.cshtml tidak punya breadcrumb** — Navigasi konteks hilang di 2 halaman utama — Tambahkan `<nav aria-label="breadcrumb">` seperti halaman lain
3. **Hardcoded warna di inline style dan CSS** — Sulit maintain dan tidak konsisten dengan tema — Ganti `#dee2e6`, `#0d6efd`, `rgba(0,0,0,0.15)` dengan variabel Bootstrap CSS

---

## Detailed Findings

### Pillar 1: Copywriting (3/4)

**Positif:**
- Mayoritas CTA menggunakan Bahasa Indonesia yang jelas: "Lihat Dokumen", "Muat Data", "Simpan Perubahan", "Kembali", "Lihat Riwayat"
- Empty state copy informatif: "Anda belum memiliki penugasan Proton. Hubungi coach Anda untuk penetapan track."
- Error message spesifik: "Gagal memuat riwayat sertifikat. Periksa koneksi lalu coba lagi."

**Temuan:**
- **CoachingProton.cshtml:73** — Subtitle dalam Inggris: "Monitor deliverable progress and approval status"
- **CoachingProton.cshtml:234,245,254** — Label stat card dalam Inggris: "Progress", "Pending Actions", "Pending Approvals"
- **EditCoachingSession.cshtml:101** — Label "Result" tidak diterjemahkan
- **EditCoachingSession.cshtml:104** — Opsi "Need Improvement", "Suitable", "Good", "Excellence" dalam Inggris
- **Index.cshtml:25,67,114** — Judul card campuran Inggris: "Individual Development Plan", "Proton History", "Certification Management"
- **Deliverable.cshtml:78,118** — Section header "Detail Coachee & Kompetensi", "Approval Chain" dalam Inggris
- **CertificationManagement.cshtml:12** — Header "Certification Management" tanpa terjemahan

### Pillar 2: Visuals (3/4)

**Positif:**
- Card-based layout konsisten di seluruh modul dengan `shadow-sm`, `border-0`
- Icon Bootstrap Icons dipakai konsisten untuk konteks visual (bi-graph-up, bi-clock-history, bi-patch-check)
- Progress indicator (step-dot) di HistoriProton kreatif dan informatif
- Approval chain vertical timeline di Deliverable.cshtml sangat jelas
- Hover effect pada card Index.cshtml memberikan feedback interaksi

**Temuan:**
- **Index.cshtml** — Tidak ada breadcrumb (halaman hub)
- **Dashboard.cshtml** — Tidak ada breadcrumb
- **CoachingProton.cshtml** — Tidak ada breadcrumb
- **PlanIdp.cshtml** — Tidak ada breadcrumb
- **EditCoachingSession.cshtml** — Ada breadcrumb (baik)
- **HistoriProton.cshtml** — Ada breadcrumb (baik)
- **HistoriProtonDetail.cshtml** — Ada breadcrumb (baik)
- **CertificationManagement.cshtml** — Tidak ada breadcrumb, tapi ada tombol "Kembali ke CDP"
- **Deliverable.cshtml** — Ada breadcrumb (baik)

### Pillar 3: Color (3/4)

**Positif:**
- Pola 60/30/10 terpenuhi: bg-light (60%), bg-white cards (30%), accent primary/success/warning/info (10%)
- Badge warna konsisten: bg-success (Lulus/Aktif), bg-warning (Dalam Proses), bg-danger (Expired/Rejected), bg-secondary (Belum Mulai/Pending)
- Setiap card di Index.cshtml punya warna icon unik yang bermakna (primary=dokumen, warning=coaching, info=histori, success=sertifikat)

**Temuan:**
- **Dashboard.cshtml:14-16** — Hardcoded hex: `#dee2e6`, `#0d6efd` untuk spinner
- **CertificationManagement.cshtml:176-178** — Duplikasi hardcoded hex yang sama untuk spinner
- **Index.cshtml:145** — Hardcoded `rgba(0, 0, 0, 0.15)` di hover shadow
- **HistoriProtonDetail.cshtml:149** — Inline style `style="left:11px;top:8px;..."` di Deliverable approval chain
- **Deliverable.cshtml:149** — Hardcoded `#dee2e6` untuk vertical line di approval chain

### Pillar 4: Typography (3/4)

**Positif:**
- Font Inter dari Google Fonts via Layout
- Konsisten menggunakan `fw-bold`, `fw-semibold` untuk heading/label; `text-muted` + `small` untuk meta
- Heading hierarchy jelas: h2 untuk page title, h5/h6 untuk card header, small untuk label

**Temuan:**
- Ukuran font yang digunakan: fs-1, fs-2, fs-3, h2, h4, h5, h6, small — total ~7 level, masih wajar untuk aplikasi kompleks
- Font weight: fw-bold, fw-semibold, normal (implicit) — 3 weight, dalam batas wajar
- **HistoriProtonDetail.cshtml:121** — Inline `style="font-size: 2.5rem"` pada empty state icon, sebaiknya pakai `fs-1`
- **CoachingProton.cshtml** vs **HistoriProton.cshtml** — Page title berbeda level (h2 vs h4), inkonsisten antar halaman

### Pillar 5: Spacing (3/4)

**Positif:**
- Spacing Bootstrap konsisten: `py-4`, `mb-4`, `mb-3`, `g-3`, `gap-2`
- Card body spacing seragam: `card-body` default padding
- Filter bar spacing konsisten: `g-2 align-items-end`

**Temuan:**
- **CoachingProton.cshtml:87,107,148,168** — Inline `style="width:auto;min-width:150px"` pada filter dropdowns; bisa pakai utility class
- **HistoriProtonDetail.cshtml:149** — Inline positioning `style="left:11px;top:8px;bottom:8px;width:2px"` untuk timeline line
- **Deliverable.cshtml:153** — Inline positioning `style="left:-27px;top:2px;font-size:1.2em"` untuk approval icons
- Secara keseluruhan spacing konsisten, hanya timeline/custom component yang pakai arbitrary value (wajar untuk custom layout)

### Pillar 6: Experience Design (3/4)

**Positif:**
- **Loading states:** Dashboard dan CertificationManagement memiliki loading overlay (`dashboard-loading` class); CoachingProton punya spinner overlay
- **Empty states:** PlanIdp ("Pilih Bagian..."), HistoriProton ("Tidak ada pekerja yang sesuai"), Deliverable (locked state), HistoriProtonDetail ("Tidak ada riwayat Proton")
- **Error states:** TempData["Error"] ditangani di Layout, EditCoachingSession, Deliverable; CertificationManagement modal punya error fallback
- **Confirmation dialogs:** `confirm()` pada approve, reject, delete actions
- **Pagination:** HistoriProton dan CoachingProton punya client-side pagination
- **AbortController:** Fetch requests di Dashboard dan CertificationManagement bisa dibatalkan (mencegah race condition)
- **Filter persistence:** CoachingProton dan PlanIdp mempertahankan state filter via query params

**Temuan:**
- **PlanIdp.cshtml** — Tab Coaching Guidance menunjukkan "Memuat data..." tapi sebenarnya render client-side dari JSON yang sudah ada; bisa membingungkan karena terlihat seperti loading padahal data sudah tersedia
- **Index.cshtml** — Tidak ada loading state; jika halaman lambat, user tidak ada feedback
- **CoachingProton.cshtml** — Loading spinner ada tapi pakai `d-none` class, toggle via JS — bisa flash of content sebelum JS load
- **EditCoachingSession.cshtml** — Tidak ada client-side validation; form bisa submit kosong

---

## Files Audited

- `Views/CDP/Index.cshtml` — Hub page (5 menu cards)
- `Views/CDP/Dashboard.cshtml` — Coaching Proton dashboard dengan partial views
- `Views/CDP/PlanIdp.cshtml` — Silabus & Coaching Guidance dengan tab, filter, accordion
- `Views/CDP/CoachingProton.cshtml` — Tracking deliverable dengan filter, pagination, stat cards
- `Views/CDP/EditCoachingSession.cshtml` — Form edit sesi coaching
- `Views/CDP/HistoriProton.cshtml` — Daftar riwayat proton dengan search, filter, pagination
- `Views/CDP/HistoriProtonDetail.cshtml` — Detail timeline proton per pekerja
- `Views/CDP/CertificationManagement.cshtml` — Manajemen sertifikat dengan filter cascade, summary cards, modal
- `Views/CDP/Deliverable.cshtml` — Detail deliverable dengan approval chain, evidence, status history
- `Views/Shared/_Layout.cshtml` — Shared layout (navbar, Bootstrap 5.3, Inter font)
