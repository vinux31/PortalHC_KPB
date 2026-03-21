# Phase 221: Integrasi Codebase - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 221-integrasi-codebase
**Areas discussed:** Strategi query data, Cascade dropdown JS, Validasi worker, DokumenKkj grouping

---

## Strategi Query Data

### Q1: Bagaimana controllers mengambil data OrganizationUnit dari database?

| Option | Description | Selected |
|--------|-------------|----------|
| Helper method di DbContext | Tambah method di ApplicationDbContext atau extension method. Semua controller pakai method yang sama | ✓ |
| Service class terpisah | Buat OrganizationService dengan DI injection | |
| Query langsung per-action | Setiap action query _context langsung | |

**User's choice:** Helper method di DbContext
**Notes:** Konsisten, satu titik perubahan

### Q2: Perlu caching untuk data OrganizationUnit?

| Option | Description | Selected |
|--------|-------------|----------|
| Tanpa cache | Data ringan (4 Bagian, 17 Unit), query langsung | ✓ |
| MemoryCache sederhana | Cache 5-10 menit, invalidate saat CRUD | |

**User's choice:** Tanpa cache

### Q3: Helper method mengembalikan data dalam format apa?

| Option | Description | Selected |
|--------|-------------|----------|
| Same signature | GetAllSections() → List<string>, drop-in replacement | ✓ |
| Return entity objects | GetSections() → List<OrganizationUnit> | |

**User's choice:** Same signature — drop-in replacement

---

## Cascade Dropdown JS

### Q1: Bagaimana cascade dropdown mendapat data?

| Option | Description | Selected |
|--------|-------------|----------|
| ViewBag JSON dari controller | Controller serialize Dictionary ke ViewBag.SectionUnitsJson | ✓ |
| AJAX endpoint | API /Admin/GetUnitsForSection | |
| Inline dari ViewBag dictionary | Controller kirim Dictionary, view serialize di Razor | |

**User's choice:** ViewBag JSON dari controller

### Q2: Pertahankan pattern Razor loop di beberapa views?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, pertahankan | Ganti sumber dari OrganizationStructure ke ViewBag | |
| Ganti semua ke JS | Semua dropdown pakai JS populate dari JSON | ✓ |

**User's choice:** Ganti semua ke JS — konsistensi penuh

### Q3: JS ambil JSON dari mana?

| Option | Description | Selected |
|--------|-------------|----------|
| Embedded JSON di page | <script>var sectionUnits = @Html.Raw(...)</script> | ✓ |
| Shared partial view | _SectionUnitsData.cshtml partial | |

**User's choice:** Embedded JSON di page

---

## Validasi Worker

### Q1: Validasi Section/Unit di mana?

| Option | Description | Selected |
|--------|-------------|----------|
| Server-side only | Controller cek terhadap OrganizationUnits aktif | ✓ |
| Server + client | Tambah JS validation juga | |

**User's choice:** Server-side only

### Q2: Dropdown Section/Unit di form ManageWorkers?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, ganti ke DB | Dropdown ambil dari OrganizationUnits | ✓ |
| Tetap manual input | Text input, validasi saat submit | |

**User's choice:** Ya, ganti ke DB

### Q3: Role-based section locking (L4/L5)?

| Option | Description | Selected |
|--------|-------------|----------|
| Cek user.Section ada di DB | Fallback tampilkan semua jika tidak match | ✓ |
| Strict validation | Error jika tidak match | |

**User's choice:** Graceful fallback

---

## DokumenKkj & ProtonData

### Q1: DokumenKkj grouping perubahan apa?

| Option | Description | Selected |
|--------|-------------|----------|
| Update query grouping | GroupBy dari KkjBagian ke OrganizationUnit | ✓ |
| Tidak perlu perubahan | Skip area ini | |

**User's choice:** Update query grouping

### Q2: ProtonKompetensi dan CoachingGuidanceFile sinkronisasi?

| Option | Description | Selected |
|--------|-------------|----------|
| Ganti dropdown sumber | ProtonDataController ganti ke DB query | ✓ |
| Claude's discretion | Serahkan ke Claude | |

**User's choice:** Ganti dropdown sumber

---

## Claude's Discretion

- Urutan pengerjaan controllers
- Error handling saat OrganizationUnit kosong
- Exact JS refactor untuk views Razor→JS

## Deferred Ideas

None
