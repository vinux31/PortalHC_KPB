# Phase 336 — NAMING-CONVENTION-SPEC.md

**Phase:** 336-investigate-pretest-loss-cilacap-restore-strategy
**Date:** 2026-05-30
**REQ:** REST-03 (naming convention portion) + hand-off REST-06 (Phase 338 W5 enforce)
**Scope:** Spec text only (NO code change) per CONTEXT.md D-05.

---

## Format Definition Final

**Wajib (strict):**

```
{Stage} Test {Track} {Lokasi}
```

### Komponen

| Token | Rule | Examples Valid | Examples Invalid |
|-------|------|----------------|------------------|
| `{Stage}` | `Pre` atau `Post` literal (capitalize first letter, single word, NO dash/space) | `Pre`, `Post` | `pre`, `PRE`, `Pre-`, `PreTest` (no space) |
| `Test` | Literal word "Test" dengan space pemisah | `Test` | `test`, `TEST`, no `Test` token |
| `{Track}` | Track name dari master list (lihat section "Track Master") — multi-word OK | `OJT GAST`, `OJT Pekerja GAST`, `CMP`, `CDP` | `OJT-GAST` (dash), `Ojt Gast` (mixed case) |
| `{Lokasi}` | Format primary `di Unit {Unit} RU {Refinery} {Kota}` ATAU fallback short `{Kota}` | `di Unit SRU dan GTO RU IV Cilacap`, `Cilacap` | `Cilacap-IV`, `RU IV` alone (no kota) |

### Track Master (initial — Phase 338 W5 validate vs DB)

Track yang sudah terverifikasi dari incident scope + memory project:
- `OJT GAST` (general OJT)
- `OJT Pekerja GAST` (OJT khusus pekerja level)
- `CMP` (Competency Management Platform — assessment OJT umum)
- `CDP` (Competency Development Platform — IDP track)
- `BP` (Business Process)
- `KKJ` (Kompetensi Kerja Jabatan)

Phase 338 W5 verify master list lengkap via grep `Models/Track.cs` atau `Master/` table reference + tambah missing.

---

## Examples (8 contoh)

### ✅ Good — Comply

1. **Long form Pre:** `Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`
2. **Long form Post:** `Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`
3. **Short form Pre:** `Pre Test CMP Operator Cilacap`
4. **Short form Post:** `Post Test CDP Coach Tahun 1 Balongan`
5. **Multi-unit explicit:** `Pre Test OJT GAST di Unit SRU + GTO RU V Balikpapan` *(alternative pemisah `+` vs `dan`, lihat edge case 1)*

### ❌ Bad — Violate Convention

6. **No Stage prefix (incident source):** `OJT GAST - GTO & SRU RU IV` — missing `Pre Test`/`Post Test`, no `Cilacap`, pemisah `-` invalid
7. **No space PreTest:** `PreTest OJT GAST Cilacap` — `PreTest` concat tanpa space melanggar `{Stage} Test` literal
8. **No Lokasi:** `Pre Test OJT GAST` — kosong tanpa `{Lokasi}`, ambiguous lokasi pelaksanaan

---

## Edge Cases (4 case)

### Edge 1: Multi-unit pemisah

**Pilihan:** `di Unit SRU dan GTO` (preferred, Bahasa Indonesia natural) vs `di Unit SRU + GTO` (compact) vs `di Unit SRU, GTO` (comma)

**Rekomendasi:** `dan` untuk 2 unit, `, ... dan ...` untuk 3+ unit (Oxford comma Bahasa).
- 2 unit: `di Unit SRU dan GTO`
- 3 unit: `di Unit SRU, GTO, dan HCC`

**Rationale:** Konsisten dgn Bahasa Indonesia formal, screen reader friendly, search keyword `dan` umum.

### Edge 2: Refinery short vs long form

**Pilihan:** `RU IV` (short, default) vs `Refinery Unit IV` (long, formal)

**Rekomendasi:** `RU IV` (short) PREFERRED — consistent dgn enterprise convention Pertamina (RU I/II/III/IV/V/VI/VII).

**Exception:** Kalau ada lokasi outside RU framework (e.g., shipping, terminal), pakai format eksplisit:
- `Pre Test OJT BBM di Terminal Wayame`
- `Post Test CMP Shipping di Kapal Pertamina Tankers`

### Edge 3: Pre/Post LinkedGroupId pairing strategy

**Pilihan A (default):** Admin manual assign saat create form (dropdown "Pair with existing Pre/Post").
**Pilihan B (auto-detect):** Regex match `^(Pre|Post) Test (.+)$` untuk extract Track+Lokasi, query DB find counterpart auto-suggest.

**Rekomendasi Phase 338 W5:** Default **A (manual assign)** untuk safety, future enhancement B (auto-suggest) sebagai UX polish kalau ada bandwidth.

**Rationale:** Auto-detect rentan false positive kalau naming inconsistent (legacy data violate convention). Manual = explicit operator choice = audit trail jelas.

### Edge 4: Kota dengan multiple kata atau special char

**Examples:**
- ✓ `Cilacap` (1 kata)
- ✓ `Pangkalan Brandan` (2 kata, capitalize each)
- ✓ `Plaju Sumatera Selatan` (3 kata extended)
- ✗ `cilacap` (lowercase)
- ✗ `CILACAP` (uppercase)

**Rule:** Title Case (capitalize first letter each word), NO special char (apostrophe, dash, etc.).

---

## Rationale — Kenapa Convention Ini Fix Incident

**Incident asal:** PreTest user title `OJT GAST - GTO & SRU RU IV` → search "Cilacap" di Dev DB return 0 hit. Cause:
1. NO Stage prefix → filter `Pre Test`/`Post Test` di /Admin/ManageAssessment miss
2. NO Lokasi `Cilacap` → search "Cilacap" miss
3. Pemisah `-` `&` non-standard → tokenizer search engine inconsistent match

**Convention fix:**
1. **Stage prefix wajib** → filter Pre/Post di admin UI selalu hit
2. **Lokasi wajib + Kota explicit** → search "Cilacap" selalu hit (Kota token persistent)
3. **Format `{Stage} Test {Track} {Lokasi}` strict** → tokenizer consistent, predictable parse

**Bonus benefit untuk Phase 338 W5:**
- LinkedGroupId auto-pair (Edge 3 future enhancement) jadi feasible karena format predictable
- `ExportGainScoreExcel` (existing endpoint kalau ada) bisa auto-detect pair via Title regex
- Audit trail / reporting lebih clean dengan naming uniform

---

## OQ-336-4 Resolution

**Pertanyaan:** Naming convention enforce backward — rename existing AssessmentSession yang violate convention, ATAU new-only enforce?

**Jawaban:** **DEFER ke Phase 338 W5 detailed discuss.**

**Rationale defer:**
1. Backward rename = high-risk operation (touches existing data, audit log impact, training/CDP references downstream affected)
2. Impact analysis butuh DB introspection (count row violate convention) yang OUT-OF-SCOPE Phase 336 (D-04 git+files read only)
3. User decision needed: trade-off "data uniformity" vs "audit log explosion + downstream link breaks"
4. New-only enforce = low-risk (cuma validate input form, 0 impact existing data) — bisa di-implement Phase 338 W5 standalone tanpa backward concern
5. Hybrid possible: new-only enforce + rename existing OPSIONAL batch tool (admin trigger manual)

**Hand-off Phase 338 W5 questions:**
- Q5-1: New-only enforce SAJA (skip backward) ATAU plus opsional rename tool?
- Q5-2: Kalau rename tool, scope: ALL existing violate ATAU selective (e.g., recent 3 month)?
- Q5-3: Audit log strategy untuk rename: log per-row ATAU 1 entry batch?

---

## Hand-off ke Phase 338 W5 (REST-06)

### Implementation Spec

**1. Validation di Admin Create Form**
- File: `Controllers/AssessmentAdminController.cs` (create endpoint — find via `Create` POST action)
- Add ViewModel validation `[RegularExpression(@"^(Pre|Post) Test .+ (di Unit .+ RU [IVX]+ \w+|\w+)$", ErrorMessage = "Title harus comply convention: '{Pre|Post} Test {Track} {Lokasi}'")]` attribute ke `Title` property AssessmentCreateViewModel
- Atau pakai FluentValidation kalau project sudah pakai pattern itu
- Error message user-friendly: "Format judul harus: '{Pre|Post} Test {Track} {Lokasi}' (contoh: 'Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap')"

**2. LinkedGroupId Auto-pair UI (Edge 3 Default A — manual)**
- View: `Views/AssessmentAdmin/Create.cshtml` (atau equivalent) — tambah dropdown "Pair with existing assessment" dengan list AssessmentSession `WHERE AssessmentType = ('Pre' if creating Post else 'Post') AND CompletedAt > now() - 6 month`
- Saat user pilih dari dropdown → auto-set `LinkedSessionId` + `LinkedGroupId` (kalau target di group)

**3. Backward Audit Tool (DEFER OQ-336-4)**
- Standalone admin-only page `/Admin/NamingConventionAudit`
- Query: `SELECT Id, Title, CompletedAt FROM AssessmentSessions WHERE Title NOT REGEXP '^(Pre|Post) Test .+ (di Unit .+ RU [IVX]+ \w+|\w+)$' ORDER BY CompletedAt DESC`
- Display table: 10 row pertama violate + count total
- Action button: "Rename Selected" → modal input nama baru per row, save + audit log per rename
- NO bulk-rename auto (avoid mass mistake)

**4. Master Track Lookup**
- Verify Track Master list di section atas vs DB existing `Tracks` table (kalau ada) atau `Master/` reference
- Tambah missing track ke dropdown saat admin pilih Track di create form
- Drop-down ↔ free-text fallback dengan validation regex

---

## Cross-link

- Source incident analysis: `336-ROOT_CAUSE.md` section "Conclusion"
- Companion: `336-RESTORE-DECISION.md` (Strategy A re-import butuh rename Title PreTest sesuai convention ini)
- Hand-off Phase 338 W5: REST-06 implementation spec di file ini
- Cross-REST: REST-04 Phase 338 W4 PreTest rename per spec sebelum re-import (Title baru = `Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`)
