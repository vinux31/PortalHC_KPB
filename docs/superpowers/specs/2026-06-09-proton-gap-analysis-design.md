# PROTON Gap Analysis — Design Spec

**Tanggal:** 2026-06-09 (rev v2: 2026-06-10)
**Status:** Brainstorm v2 selesai, design disetujui. Siap writing-plans.
**Branch:** ITHandoff

## Tujuan

Analisa gap fitur/menu PROTON end-to-end. Pendekatan: **inventaris dulu** — petakan lengkap menu → flow → page → fitur in-page (filter, tabel, tombol, modal, export). Gap muncul SETELAH peta jadi, lewat 6 lensa analisa.

## Keputusan brainstorm (terkunci — rev v2)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| 1 | Definisi gap | Inventaris dulu (peta lengkap), gap = hal hilang/inkonsisten setelah peta |
| 2 | Cakupan | PROTON dulu (1 modul, dalam) sbg pilot; replikasi modul lain nanti |
| 3 | Metode gali | Hybrid: baca kode + crawl live Playwright (Razor dynamic wajib live) |
| 4 | Bentuk output | HTML interaktif (stakeholder) |
| 5 | Eksekusi | **Inline page-by-page murni** (ada plan-phase paralel di sesi lain) |
| 6 | CoachMapping | MASUK scope (setup assignment PROTON) |
| 7 | Renewal | SKIP (modul cert terpisah) |
| 8 | Lensa | **6 lensa** (4 struktural + data-integrity + UX/usability). Spec-compliance DIBUANG (tak ada rujukan resmi) |
| 9 | Output tambahan | + Ringkasan eksekutif + Roadmap rekomendasi (rev v2) |

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
├─ Role akses (tier asli: Coachee / Coach+Supervisor / SrSupervisor+SectionHead / HC+Admin)
├─ Elemen in-page:
│   ├─ Filter (field apa saja)
│   ├─ Tabel (kolom + sumber data)
│   ├─ Tombol/Aksi (+ endpoint yg dipanggil)
│   ├─ Modal/Form
│   └─ Export (Excel/PDF)
├─ Data source (entity/query utama)
└─ GAP / anomali (diisi saat analisa, tag lensa + severity)
```

## Role model (dari Models/UserRoles.cs)

Tak ada role harfiah "Reviewer". Gate berlapis:
- `[Authorize]` (any auth) → Coachee lihat base CDP pages
- `RolesCoachAndAbove` = Coach, SrSupervisor, SectionHead, HC, Admin → UploadEvidence, EditCoachingSession, Export, SubmitEvidence
- `RolesReviewerAndAbove` = SrSupervisor, SectionHead, HC, Admin → Approve/Reject deliverable+progress, ExportHistori
- `"HC, Admin"` → HCReview deliverable
- `Admin,HC` → seluruh ProtonData

Tier relevan PROTON: **Coachee → Coach/Supervisor → SrSupervisor/SectionHead → HC/Admin**

## 6 lensa deteksi gap

**Struktural (4):**
1. **Konsistensi** — page sejenis beda fitur (mis. History ada export, Dashboard nggak?)
2. **Flow buntu** — page tanpa entry-point jelas / drill tanpa balik
3. **Role mismatch** — fitur kebuka ke role salah / fitur hilang utk role tertentu
4. **Dead/half feature** — tombol/endpoint ada tapi nggak ke-link, atau handler kosong

**Tambahan (2 — rev v2):**
5. **Data integrity** — field/kolom kosong, status enum gak lengkap, query rusak karena data ref belum normalisasi (mis. bug ProtonData/PlanIdp Dev akibat OrganizationUnits belum dinormalisasi). Gap data, bukan fitur.
6. **UX/usability** — langkah berlebih, konfirmasi hilang sebelum aksi destruktif, feedback gagal tak muncul, responsive/mobile.

> **Dibuang:** spec-compliance (bandingkan vs Pedoman resmi) — tak ada dokumen rujukan formal di repo. Bisa ditambah lensa ke-7 nanti kalau Pedoman PROTON tersedia.

## Severity gap

Skala **Critical / High / Medium / Low** (selaras pola doc gap sebelumnya: `analisa-gap-benchmark`, `sertifikat-ecosystem`). Dipakai di badge panel gap + breakdown ringkasan eksekutif.

## Crawl live (hybrid)

- **AD = `true`** di `appsettings.json:14`. Aturan: AD True → cuma admin pwd `123456`. AD False → semua akun `123456`.
- Strategi: login admin `admin@pertamina.com` / `123456` → `Admin/Impersonate` jadi coach/coachee/dst. **TIDAK** toggle `UseActiveDirectory=false` (itu edit file → bisa ganggu build sesi paralel).
- Dev URL lokal: `http://localhost:5277`. **Pakai instance dotnet yang sudah jalan** kalau sesi lain udah start app (jangan start kedua → bentrok port).
- Tiap page: Playwright snapshot → konfirmasi elemen render runtime/JS (lesson: grep+build tak cukup untuk Razor dynamic).

## Output

`docs/proton-gap-analysis/index.html` (offline, Bootstrap + icon, pola doc sebelumnya):

1. **Ringkasan eksekutif** (rev v2) — total gap, breakdown per severity, top-5 gap kritis. Buat HC pimpinan.
2. **Flow diagram 3-tier** — hub → tier-1 → drill + branch admin + branch setup.
3. **Accordion per page (14)** — isi skema tangkap di atas.
4. **Matrix konsistensi** — baris=page, kolom=filter/tabel/export/modal/role → spot gap sekilas.
5. **Panel temuan gap** — badge severity + tag lensa (1 dari 6), dari analisa.
6. **Roadmap rekomendasi** (rev v2) — tiap gap: usulan fix + estimasi effort + prioritas. Output actionable, bukan cuma diagnosa.

> Filter/search interaktif TIDAK dipakai (YAGNI — jumlah gap pilot belum tentu banyak).

## Eksekusi — inline page-by-page

Ada **plan-phase paralel di sesi lain** (branch sama ITHandoff). Aturan aman:
- Read-only ke kode + DB (cuma browsing Playwright). **Zero** edit `.cs`/`.cshtml`/migration/DB.
- Commit **cuma** file `docs/proton-gap-analysis/` + spec ini. Sesi lain tulis `.planning/` → file beda, konflik minim.
- Gak spawn Workflow fan-out (saingan CPU/port). Gak start dotnet kedua.
- Urutan: CDP hub dulu (anchor flow) → 8 page CDP → 4 admin → 2 setup. Page penuh 1 giliran (kode + Playwright + isi accordion) sebelum lanjut.

## Next step (saat resume)

1. ✅ User review spec v2 ini.
2. Lanjut `superpowers:writing-plans` → buat implementation plan.
3. Eksekusi inline page-by-page.

## File rujukan terverifikasi sesi ini

- `Controllers/CDPController.cs` — 8 view-returning actions + banyak Export/Filter/Approve
- `Controllers/ProtonDataController.cs` — Index/Override/ImportSilabus (view) + Silabus CRUD; class `[Authorize(Roles="Admin,HC")]`
- `Controllers/CoachMappingController.cs` — CoachCoacheeMapping + CoachWorkload + ProtonTrackAssignments
- `Models/UserRoles.cs` — definisi role + RolesCoachAndAbove/RolesReviewerAndAbove
- `Views/CDP/Index.cshtml` — hub 4 kartu (PlanIdp/CoachingProton/HistoriProton/Dashboard)
- `Views/Shared/_Layout.cshtml:116` — satu-satunya link menu CDP (→ Index)
- `appsettings.json:14` — UseActiveDirectory: true
