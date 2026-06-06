# Spec: Audit Fix — HC/Admin Assign Coach × Coachee (PROTON)

**Tanggal:** 2026-06-06
**Milestone:** v24.0 (Phase 356, addon off-theme atas permintaan user)
**Tipe:** Audit-driven fix (bukan fitur baru)
**Sumber:** Audit read-only sesi 2026-06-06 atas fitur Assign Coach×Coachee
**Tujuan user:** "Pastikan fungsi assign berfungsi dengan baik dan benar."

---

## 1. Konteks & Komponen

Fitur HC/Admin menugaskan Coach ke Coachee untuk program PROTON (coaching multi-tahun: Tahun 1/2/3 per TrackType). Mapping memicu pembuatan `ProtonTrackAssignment` + auto-generate `ProtonDeliverableProgress` (deliverable yang harus diselesaikan coachee, di-approve coach/HC). Coachee yang 100% deliverable Approved → eligible untuk Assessment Proton (ujian final tahap).

| Komponen | File |
|----------|------|
| Controller | `Controllers/CoachMappingController.cs` (1695 baris) |
| Model | `Models/CoachCoacheeMapping.cs` |
| View | `Views/Admin/CoachCoacheeMapping.cshtml`, `Views/Admin/CoachWorkload.cshtml` |
| Entitas terkait | `ProtonTrackAssignment`, `ProtonDeliverableProgress`, `ProtonFinalAssessment`, `ProtonKompetensi` (punya `Unit`), `CoachingSession` |
| Route | `Admin/[action]`, auth `Admin, HC` |

**Invariant DB (sudah benar):** `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` — 1 coach aktif per coachee (filtered unique `WHERE IsActive=1`).

**Kualitas existing: TINGGI.** Transaksi konsisten, audit log, notifikasi, file-delete atomic post-commit (pola Phase 333), banyak fix tercatat (CRIT-02, FIX-01/02, MED-01, HIGH-04, D-08..D-17). Audit ini menemukan sisa bug logic, bukan kelemahan arsitektur.

---

## 2. Flow Saat Ini (ringkas)

1. **List** `CoachCoacheeMapping` — load users+mappings, sembunyikan orphan (coach/coachee non-aktif), group by coach, paginate, workload per-coach. Modal dropdown: coach role aktif; coachee role aktif yang belum punya mapping aktif.
2. **Assign** `CoachCoacheeMappingAssign` (JSON) — validasi → progression-warning (Tahun 2/3 butuh tahun sebelumnya Approved) → transaksi: buat mapping + side-effect ProtonTrackAssignment (deactivate track lama beda / reuse inactive sama / buat baru + `AutoCreateProgressForAssignment`) → audit + notif.
3. **Edit / Deactivate / Reactivate / Delete / MarkMappingCompleted / Import Excel / Export / CleanupOrg / CoachWorkload + ReassignSuggestion**.
4. **`GetEligibleCoachees`** — dipakai form CreateAssessment (kategori Assessment Proton) untuk menampilkan coachee yang layak ujian.

`AutoCreateProgressForAssignment` (L1338-1407): buat `ProtonDeliverableProgress` HANYA untuk deliverable yang `ProtonKompetensi.Unit == unit coachee` (resolved dari `AssignmentUnit` mapping aktif, fallback `User.Unit`).

---

## 3. Temuan Audit (AF-1 .. AF-7)

### AF-1 — HIGH (CONFIRMED via query DB) — Eligibility salah untuk track multi-unit

**Lokasi:** `CoachMappingController.GetEligibleCoachees` L1277-1334 (khususnya L1291-1322).

**Masalah:** `trackDeliverableIds` dihitung dari SELURUH deliverable track (lintas semua unit), TANPA filter `Unit`:
```csharp
var trackDeliverableIds = await _context.ProtonKompetensiList
    .Where(k => k.ProtonTrackId == protonTrackId)
    .SelectMany(k => k.SubKompetensiList)
    .SelectMany(s => s.Deliverables)
    .Select(d => d.Id).ToListAsync();
```
Tapi `AutoCreateProgressForAssignment` hanya membuat progress untuk deliverable **unit coachee saja** (L1363-1367, filter `Unit == resolvedUnit`). Eligibility menguji:
```csharp
return mine.Count == trackDeliverableIds.Count && mine.All(p => p.Status == "Approved");
```
→ `mine.Count` = jumlah deliverable unit coachee (subset) < `trackDeliverableIds.Count` (total semua unit) → **`==` tak pernah true** untuk track multi-unit.

**Bukti data (DB lokal HcPortalDB_Dev):**
- Track id=4 "Operator - Tahun 1" = **4 deliverable / 2 unit**: Alkylation Unit (065) = 3, RFCC NHT (053) = 1.
- Coachee Alkylation: 3 progress, `mine.Count=3 ≠ 4` → **tak pernah eligible**.
- Coachee RFCC: 1 progress, `1 ≠ 4` → **tak pernah eligible**.
- **Tidak ada coachee track 4 yang bisa lolos eligibility** → HC tak bisa memilih mereka untuk Assessment Proton final.
- Query konfirmasi multi-unit: `SELECT ProtonTrackId, COUNT(DISTINCT Unit) FROM ProtonKompetensiList GROUP BY ProtonTrackId HAVING COUNT(DISTINCT Unit) > 1` → 1 track (id=4).

**Fix yang diusulkan:** hitung deliverable yang DIHARAPKAN per unit coachee (mirror filter `AutoCreateProgressForAssignment`), atau bandingkan terhadap jumlah progress aktual coachee untuk track ini. Opsi paling aman & konsisten: resolve unit tiap coachee (dari `AssignmentUnit` aktif → fallback `User.Unit`), hitung `expectedCount = deliverable track WHERE Unit==coacheeUnit`, eligible jika `mine.Count == expectedCount && expectedCount > 0 && all Approved`. Tahun 3 (tanpa deliverable) tetap by-design semua eligible (L1298-1307, dipertahankan).

**Verifikasi:** xUnit untuk helper eligibility per-unit + UAT track id=4 (coachee Alkylation 3/3 Approved → muncul).

---

### AF-2 — MED — Batch-assign memaksa 1 Unit untuk coachee lintas-unit

**Lokasi:** `CoachCoacheeMappingAssign` L548-558 + modal `CoachCoacheeMapping.cshtml` L408-449.

**Masalah:** Modal mengizinkan centang **banyak coachee lintas seksi** (`coacheeChecklist`, checkbox), lalu **satu** `AssignmentSection` + **satu** `AssignmentUnit` diterapkan ke SEMUA. `AutoCreateProgressForAssignment` lalu memakai `AssignmentUnit` tunggal itu → coachee yang sebenarnya berbeda unit mendapat deliverable unit yang salah (atau warning "tidak ada deliverable").

**Fix usulan (pilih satu):**
- **A (UI guard)**: batasi pemilihan coachee dalam satu batch ke satu unit (disable cross-unit select), atau
- **B (per-coachee resolve)**: backend resolve unit per coachee (dari `User.Unit`) saat AutoCreateProgress alih-alih `AssignmentUnit` batch. Trade-off: makna `AssignmentUnit` (penugasan bisa beda dari unit master) jadi kabur untuk batch.
- Rekomendasi: **A** (UI guard) — pertahankan semantik `AssignmentUnit` eksplisit, hindari batch ambigu.

---

### AF-3 — MED (butuh keputusan) — Graduated tetap IsActive=true → blok re-assign

**Lokasi:** `MarkMappingCompleted` L1075-1109.

**Masalah:** Set `IsCompleted=true` tapi **`IsActive` tetap true**. Cek duplikat Assign (L474) & unique-index keduanya key ke `IsActive` (bukan `IsCompleted`) → coachee graduated **tetap "punya coach aktif"** → tak bisa di-assign ulang (mis. siklus/track baru) tanpa Deactivate manual lebih dulu. Juga: tanpa transaksi (single SaveChanges) — minor.

**Keputusan diperlukan (user):**
- **(i)** Graduated = final, re-assign memang harus Deactivate manual dulu → cukup dokumentasikan + (opsional) pesan UI jelas. ATAU
- **(ii)** Graduated boleh di-assign lagi → set `IsActive=false` saat MarkCompleted, atau ubah cek duplikat agar abaikan mapping `IsCompleted`.
- **Default usulan jika user tak menentukan:** (i) — graduated final; tambah hint UI "Coachee graduated, nonaktifkan dulu untuk re-assign". Bungkus MarkCompleted dalam transaksi.

---

### AF-4 — LOW-MED — Reactivate korelasi assignment via window ±5 detik

**Lokasi:** `CoachCoacheeMappingReactivate` L1012-1020.

**Masalah:** Reaktivasi assignment dikorelasikan dengan `DeactivatedAt` dalam ±5 detik dari `originalEndDate`. FIX-01 sudah memitigasi salah-restore, tapi window 5s adalah magic number rapuh (clock skew / operasi lambat → assignment tak ter-reaktivasi → prompt assign ulang muncul meski tak perlu).

**Fix usulan:** ganti korelasi waktu dengan kolom korelasi eksplisit (mis. `DeactivatedByMappingEventId` atau simpan `EndDate` mapping = stempel yang sama persis lalu match `DeactivatedAt == EndDate` exact, bukan range). Severity rendah — boleh defer bila effort besar; minimal naikkan window atau dokumentasikan asumsi.

---

### AF-5 — LOW — ApproveReassignSuggestion tanpa notifikasi

**Lokasi:** `ApproveReassignSuggestion` L1614-1638.

**Masalah:** Reassign coach (dari saran workload) hanya ubah `CoachId` + audit; **tidak kirim notifikasi** ke coach lama/baru/coachee — inkonsisten dengan Assign/Edit/Deactivate yang semua notif. (Progress aman karena keyed by coachee, bukan coach.)

**Fix usulan:** tambah `_notificationService.SendAsync` ke coach lama (dilepas), coach baru (ditunjuk), coachee — selaras pola COACH-02.

---

### AF-6 — LOW — Pesan error duplikat-coachee generic saat race

**Lokasi:** `CoachCoacheeMappingAssign` L474-490 (check-then-insert) + DB unique-index.

**Masalah:** TOCTOU duplikat di-backstop unique-index (aman, tak korup), tapi pada race insert akan jatuh ke `catch` generic "Gagal menyimpan assignment" alih-alih pesan spesifik "coachee sudah punya coach aktif".

**Fix usulan:** tangkap `DbUpdateException` yang melanggar `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` → kembalikan pesan ramah spesifik. Minor UX.

---

### AF-7 — INFO — Progression-warning N+1 query

**Lokasi:** `CoachCoacheeMappingAssign` L497-546.

**Masalah:** Loop per coachee menjalankan ~4 query (AnyAsync existing, FirstOrDefault prev, CountAsync, AnyAsync allApproved). Admin-tool volume rendah → bukan blocker. Opsional batch jika ingin rapi.

---

## 4. Scope Phase 356

**In-scope:**
- AF-1 (HIGH) — WAJIB fix + test (eligibility per-unit). Headline.
- AF-2 (MED) — fix (UI guard rekomendasi A).
- AF-3 (MED) — keputusan user + implement (default i: final + transaksi + hint).
- AF-5 (LOW) — notifikasi reassign.
- AF-6 (LOW) — pesan error duplikat spesifik.

**Tentatif / boleh defer (effort vs nilai):**
- AF-4 (LOW-MED) — refactor korelasi reactivate (bila effort besar → defer ke backlog, dokumentasikan).
- AF-7 (INFO) — perf, opsional.

**Out-of-scope (JANGAN sentuh):**
- Arsitektur transaksi / audit / file-delete atomic existing (sudah benar).
- Unique-index invariant 1-coach-aktif/coachee.
- Fitur image v24.0 (352-355) — jalur file berbeda, independen.

**Migration:** default **false**. Hanya jika AF-3 pilih opsi skema (mis. kolom korelasi AF-4) → migration. Konfirmasi saat plan.

---

## 5. Keputusan Terkunci / Gray Area

| # | Item | Status |
|---|------|--------|
| D-1 | AF-1 fix = eligibility per-unit coachee (bukan total track) | LOCKED |
| D-2 | AF-3 semantik graduated | **OPEN — butuh user** (default i: final) |
| D-3 | AF-2 fix = UI guard 1-unit per batch (Opsi A) | proposed, konfirmasi saat plan |
| D-4 | AF-4 boleh defer jika effort besar | proposed |
| D-5 | Tahun 3 (tanpa deliverable) tetap semua-eligible by-design | LOCKED (dipertahankan) |

---

## 6. Test Plan (CLAUDE.md Develop Workflow)

- **xUnit** logic-bearing: eligibility per-unit (AF-1) — track multi-unit, coachee unit A 3/3 Approved → eligible; unit B 0/1 → tidak.
- **UAT lokal** localhost:5277 (Playwright bila UI): track id=4 — assign coachee, approve deliverable unit-nya, buka CreateAssessment kategori Assessment Proton track 4 → coachee muncul di `GetEligibleCoachees`. AF-2 batch cross-unit guard. AF-5 notif reassign.
- `dotnet build` 0 error + `dotnet test` hijau + tidak ada regresi assign/deactivate/reactivate existing.
- **Seed Workflow**: butuh fixture coachee+track+deliverable approved → snapshot DB lokal sebelum seed, restore sesudah (temporary+local-only, SEED_JOURNAL).

---

## 7. Catatan

- Phase 356 **off-theme** dari v24.0 (image-in-question) — ditambahkan eksplisit atas permintaan user. Independen 352-355, bisa dikerjakan paralel/kapan saja.
- Semua finding code-verified file:line; AF-1 data-verified (track id=4, query DB lokal 2026-06-06).
- Pertimbangkan: jika ingin track baru aman, fix AF-1 mencegah bug serupa pada track multi-unit masa depan.
