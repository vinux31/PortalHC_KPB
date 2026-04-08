# Smoke Test Result — CRIT-02 Proton Coaching

**Tanggal:** 2026-04-09
**Branch:** main (after merge bugfix/proton-coaching)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277 (Admin KPB login)
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev` (Windows Auth, `-C` trust cert)

**Scope:** Happy path Skenario 1, 2, 3 (Unit change, ProtonTrack change, AJAX shape). Skenario failure/rollback (2 & 4 di plan original) **tidak dijalankan** (butuh code injection).

---

## Target data

- **Mapping Id = 4** (Coachee: Rino, Coach: Rustam Santiko)
- **Coachee GUID:** `4a624dbc-3241-4207-92d7-d1d5784c7137`
- Snapshot awal: Section=GAST, Unit=Alkylation Unit (065), Assignment Id=1 (Track 4 Operator-1) aktif, 3 progress (no evidence files on disk)

---

## Skenario 1 — Edit Unit (happy path) ✅

**Aksi:** Ubah AssignmentUnit `Alkylation Unit (065)` → `Amine Regeneration Unit I & II (069 & 079)`, ProtonTrack biarkan "Tanpa Track" (hit cabang `unitChanged && !ProtonTrackId.HasValue`).

**Verifikasi:**
| Check | Expected | Actual |
|---|---|---|
| POST `/Admin/CoachCoacheeMappingEdit` status | 200 | **200 ✓** |
| Mapping.AssignmentUnit di DB | Amine Regeneration... | **Amine Regeneration... ✓** |
| Progress lama (3 row pada assignment 1) terhapus | ya | **ya (0 row) ✓** |
| Progress baru dibuat | 0 (unit baru tidak punya deliverable untuk track) | **0 ✓** |
| UI warning TempData | tampil | **"Tidak ada deliverable untuk unit Amine Regeneration... di track Operator - Tahun 1" ✓** |

---

## Skenario 2 — Edit ProtonTrack (happy path) ✅

**Aksi:** Ubah Unit balik ke `Alkylation Unit (065)` + ProtonTrack `Tanpa Track` → `Panelman - Tahun 1` (hit cabang `req.ProtonTrackId.HasValue`).

**Verifikasi:**
| Check | Expected | Actual |
|---|---|---|
| POST status | 200 | **200 ✓** |
| Mapping.AssignmentUnit | Alkylation Unit (065) | **Alkylation Unit (065) ✓** |
| Assignment 1 (Operator-1) | IsActive = 0 | **IsActive = 0 ✓** |
| Assignment baru (Id=5, Track Panelman-1) dibuat | IsActive = 1 | **IsActive = 1 ✓** |
| UI warning (tidak ada deliverable utk kombinasi baru) | tampil | **"Tidak ada deliverable untuk unit Alkylation Unit (065) di track Panelman - Tahun 1" ✓** |

**State akhir assignment:**
```
Id=1 Track=4 (Operator-1)  IsActive=0
Id=2 Track=6               IsActive=0
Id=5 Track=1 (Panelman-1)  IsActive=1  [baru]
```

---

## Skenario 3 — AJAX response shape ✅

**Aksi:** Explicit `fetch()` POST via `browser_evaluate` untuk inspect header dan body.

**Verifikasi:**
| Check | Expected | Actual |
|---|---|---|
| HTTP Status | 200 | **200 ✓** |
| Content-Type | `application/json` | **`application/json; charset=utf-8` ✓** |
| Body parseable JSON | ya | **ya ✓** |
| Body shape | `{success: bool, message: string}` | **`{"success":true,"message":"Mapping berhasil diperbarui."}` ✓** |
| Tidak ada redirect (302 → HTML) | tidak redirect | **tidak redirect ✓** |

Ini secara langsung memverifikasi fix CRIT-02 Bug D (regresi `RedirectToAction` dari JSON endpoint).

---

## Ringkasan

| Skenario | Status |
|---|---|
| 1. Edit Unit (happy path) | ✅ PASS |
| 2. Edit ProtonTrack (happy path) | ✅ PASS |
| 3. AJAX response shape | ✅ PASS |

**3/3 hijau.** Fix CRIT-02 di `Controllers/CoachMappingController.cs::CoachCoacheeMappingEdit` bekerja sesuai desain untuk happy path:
- Transaction wrap mutasi ✓
- Phase 129 unit-change rebuild ✓
- ProtonTrack rebuild ✓
- JSON response shape konsisten (bukan redirect) ✓

## Skenario yang belum diuji

- **Skenario 2-orig (failure Phase 129 rollback)** — butuh inject `throw` di `AutoCreateProgressForAssignment`, revert setelah tes.
- **Skenario 4-orig (failure ProtonTrack rollback / Bug C)** — idem.
- **Test dengan evidence file fisik** — DB saat ini tidak punya progress dengan EvidencePath non-null; path deferred file cleanup belum ter-exercise end-to-end.

Rekomendasi: jadwalkan failure injection terpisah setelah ada opportunity untuk modifikasi code sementara; atau tambah seed data evidence file untuk test cleanup path.
