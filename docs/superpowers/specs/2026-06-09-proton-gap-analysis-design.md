# PROTON Gap Analysis — Design Spec

**Tanggal:** 2026-06-09
**Status:** Brainstorm selesai, design disetujui. PAUSED sebelum writing-plans (user lanjut nanti).
**Branch:** ITHandoff

## Tujuan

Analisa gap fitur/menu PROTON end-to-end. Pendekatan: **inventaris dulu** — petakan lengkap menu → flow → page → fitur in-page (filter, tabel, tombol, modal, export). Gap muncul SETELAH peta jadi, lewat 4 lensa analisa.

## Keputusan brainstorm (terkunci)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| 1 | Definisi gap | Inventaris dulu (peta lengkap), gap = hal hilang/inkonsisten setelah peta |
| 2 | Cakupan | PROTON dulu (1 modul, dalam) sbg pilot; replikasi modul lain nanti |
| 3 | Metode gali | Hybrid: baca kode + crawl live Playwright (Razor dynamic wajib live) |
| 4 | Bentuk output | HTML interaktif (stakeholder) |
| 5 | Eksekusi | Inline sekuensial page-by-page (pilot); naik ke Workflow fan-out saat replikasi |
| 6 | CoachMapping | MASUK scope (setup assignment PROTON) |
| 7 | Renewal | SKIP (modul cert terpisah) |

## Scope final — 14 page (terverifikasi dari kode + menu + entry-point)

**A. Coachee/Coach — area CDP (8)** — menu atas `CDP` → hub 4 kartu → drill
| Page | Route | Cara dicapai |
|------|-------|--------------|
| Hub CDP | `CDP/Index` | Menu atas, 4 kartu |
| Plan IDP / Silabus | `CDP/PlanIdp` | Kartu hub #1 |
| Coaching PROTON (core) | `CDP/CoachingProton` | Kartu hub #2 |
| Proton History | `CDP/HistoriProton` | Kartu hub #3 |
| Proton Dashboard | `CDP/Dashboard` | Kartu hub #4 |
| Deliverable detail | `CDP/Deliverable/{id}` | Drill dari Coaching/Dashboard |
| Histori Detail | `CDP/HistoriProtonDetail/{userId}` | Drill dari History |
| Edit Coaching Session | `CDP/EditCoachingSession/{id}` | Drill dari Coaching/Deliverable |

**B. HC/Admin (4)**
| Page | Route | Cara dicapai |
|------|-------|--------------|
| Certification Management | `CDP/CertificationManagement` | Dari Admin/Home dashboard (BUKAN hub CDP) |
| Master Silabus | `ProtonData/Index` | Admin — CRUD kurikulum/track |
| Override Silabus | `ProtonData/Override` | Admin |
| Import Silabus | `ProtonData/ImportSilabus` | Admin |

**C. Setup assignment (2)**
| Page | Route | Cara dicapai |
|------|-------|--------------|
| Coach×Coachee Mapping | `CoachMapping/CoachCoacheeMapping` | Admin — assign coach + ProtonTrackAssignment |
| Coach Workload | `CoachMapping/CoachWorkload` | Admin — beban coach |

Catatan: action lain (`Export*`, `Filter*`, `Approve/Reject/HCReview`, `UploadEvidence`, `Guidance*`, `SilabusSave/Delete/Deactivate`) BUKAN page — itu fitur in-page (AJAX/export/partial), masuk kolom "elemen in-page".

## Skema tangkap per-page (seragam)

```
Page: <nama>
├─ Route + action + view file
├─ Purpose (1 kalimat)
├─ Posisi flow (dari mana → ke mana)
├─ Role akses (Coachee / Coach / Reviewer / HC / Admin)
├─ Elemen in-page:
│   ├─ Filter (field apa saja)
│   ├─ Tabel (kolom + sumber data)
│   ├─ Tombol/Aksi (+ endpoint yg dipanggil)
│   ├─ Modal/Form
│   └─ Export (Excel/PDF)
├─ Data source (entity/query utama)
└─ GAP / anomali (diisi saat analisa)
```

## Role model (dari Models/UserRoles.cs)

Tak ada role harfiah "Reviewer". Gate berlapis:
- `[Authorize]` (any auth) → Coachee lihat base CDP pages
- `RolesCoachAndAbove` = Coach, SrSupervisor, SectionHead, HC, Admin → UploadEvidence, EditCoachingSession, Export, SubmitEvidence
- `RolesReviewerAndAbove` = SrSupervisor, SectionHead, HC, Admin → Approve/Reject deliverable+progress, ExportHistori
- `"HC, Admin"` → HCReview deliverable
- `Admin,HC` → seluruh ProtonData

Tier relevan PROTON: **Coachee → Coach/Supervisor → SrSupervisor/SectionHead → HC/Admin**

## 4 lensa deteksi gap

1. **Konsistensi** — page sejenis beda fitur (mis. History ada export, Dashboard nggak?)
2. **Flow buntu** — page tanpa entry-point jelas / drill tanpa balik
3. **Role mismatch** — fitur kebuka ke role salah / fitur hilang utk role tertentu
4. **Dead/half feature** — tombol/endpoint ada tapi nggak ke-link, atau handler kosong

## Crawl live (hybrid)

- **AD = `true`** di `appsettings.json:14`. Aturan: AD True → cuma admin pwd `123456`. AD False → semua akun `123456`.
- Strategi: login admin `admin@pertamina.com` / `123456` → `Admin/Impersonate` jadi coach/coachee/dst (TIDAK ubah config). Alternatif: toggle `UseActiveDirectory=false` lokal (jangan commit).
- Dev URL lokal: `http://localhost:5277`
- Tiap page: Playwright snapshot → konfirmasi elemen render runtime/JS (lesson: grep+build tak cukup untuk Razor dynamic)

## Output

`docs/proton-gap-analysis/index.html`:
- Flow diagram 3-tier (hub → tier-1 → drill) + branch admin + branch setup
- Accordion per page (14) isi skema di atas
- Matrix konsistensi (baris=page, kolom=filter/tabel/export/modal/role) → spot gap sekilas
- Panel temuan gap (badge severity, dari 4 lensa)
- Offline (Bootstrap + icon, pola doc sebelumnya)

## Next step (saat resume)

1. User review spec ini.
2. Lanjut `superpowers:writing-plans` → buat implementation plan.
3. Eksekusi inline page-by-page (pilot).

## File rujukan terverifikasi sesi ini

- `Controllers/CDPController.cs` — 8 view-returning actions + banyak Export/Filter/Approve
- `Controllers/ProtonDataController.cs` — Index/Override/ImportSilabus (view) + Silabus CRUD; class `[Authorize(Roles="Admin,HC")]`
- `Controllers/CoachMappingController.cs` — CoachCoacheeMapping + CoachWorkload + ProtonTrackAssignments
- `Models/UserRoles.cs` — definisi role + RolesCoachAndAbove/RolesReviewerAndAbove
- `Views/CDP/Index.cshtml` — hub 4 kartu (PlanIdp/CoachingProton/HistoriProton/Dashboard)
- `Views/Shared/_Layout.cshtml:116` — satu-satunya link menu CDP (→ Index)
- `appsettings.json:14` — UseActiveDirectory: true
