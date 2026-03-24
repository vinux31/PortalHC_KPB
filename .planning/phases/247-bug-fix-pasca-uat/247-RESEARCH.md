# Phase 247: Bug Fix Pasca-UAT - Research

**Researched:** 2026-03-24
**Domain:** ASP.NET Core MVC — bug fix assessment system, ET distribution algorithm, approval chain notifications
**Confidence:** HIGH

## Summary

Phase 247 adalah phase bug fix murni — bukan development fitur baru. Semua bug sudah teridentifikasi dari UAT Phases 235, 243, 244, 246. Tidak ada library baru yang perlu dipelajari; pekerjaan adalah memahami root cause setiap bug dan memperbaiki kode yang sudah ada.

Ada tiga kategori pekerjaan: (1) Fix kode nyata — ET distribution algorithm dan notifikasi resubmit; (2) Verifikasi human UAT browser — item yang di-auto-approve atau skip selama UAT; (3) Admin cleanup — update status REQUIREMENTS.md dan VERIFICATION.md.

Berdasarkan analisis kode, `COACH_EVIDENCE_RESUBMITTED` dari Phase 235 **sudah di-fix** di CDPController baris 2265-2300 (kemungkinan dikerjakan di v8.6 atau Phase 235 plan lanjutan). Ini perlu diverifikasi dulu sebelum direncanakan ulang.

**Primary recommendation:** Urutan fix: Phase 235 notifikasi resubmit (verifikasi apakah sudah fix) → ET Distribution algorithm (kode fix) → Phase 244 browser verify → Phase 246 browser verify → Admin cleanup.

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** ET Distribution Algorithm fix MASUK scope phase ini — perbaiki `BuildCrossPackageAssignment` di CMPController.cs agar distribusi soal balanced per Elemen Teknis, bukan per package
- **D-02:** Semua pending human UAT masuk scope: Phase 235 (5 item approval chain), Phase 244 (browser test monitoring), Phase 246 (4 item: token error, force close, alarm expired, records export)
- **D-03:** REQUIREMENTS.md status update (SETUP-01/02 → Complete) dan VERIFICATION status cleanup masuk scope
- **D-04:** Fix satu-satu: fix bug → langsung test → next bug. Bukan batch.
- **D-05:** Regresi fokus area yang diubah saja. v8.6 fix sudah verified sendiri, tidak perlu re-verify.
- **D-06:** Update status REQUIREMENTS.md dan VERIFICATION.md dilakukan otomatis bersamaan setiap fix yang relevan — tidak perlu task terpisah.

### Claude's Discretion
- Urutan prioritas bug mana yang di-fix duluan
- Cara testing ET distribution edge cases
- Grouping fix ke dalam plan (bisa per-bug atau per-area)

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Research Support |
|----|-----------|------------------|
| FIX-01 | Semua bug yang ditemukan selama simulasi UAT diperbaiki dan diverifikasi | Semua bug teridentifikasi dari 235-UAT.md, 243-UAT.md, 244-UAT.md, 246-HUMAN-UAT.md. Kode target sudah diketahui. |
</phase_requirements>

---

## Bug Inventory (Temuan dari UAT)

### BUG-01: ET Distribution Algorithm — Phase 2 distribusi per-package bukan per-ET
**Source:** 243-UAT.md Test 6 note; CONTEXT.md D-01; MEMORY.md project_et_distribution_fix.md
**File:** `Controllers/CMPController.cs` baris 1099-1148
**Severity:** Medium (hasil radar chart ET menjadi tidak representatif)
**Status:** Belum di-fix

**Root Cause (dari analisis kode):**
Phase 2 `BuildCrossPackageAssignment` menggunakan `N = packages.Count` untuk distribusi — soal sisa dibagi rata per-package. Seharusnya sisa soal dibagi rata per-ET group (seperti Phase 1, tapi untuk kuota yang lebih besar dari jumlah ET).

**Contoh masalah:** 4 ET, 15 soal total — Phase 1 picks 1 soal per ET (4 soal). Phase 2 punya 11 soal sisa, dibagi per-package (misalnya 2 package × 5.5 → 6 dan 5). Hasilnya bisa 7 soal dari ET-A dan hanya 2 soal dari ET-C karena distribusi per-package mengabaikan ET balance.

**Fix yang diperlukan:**
Ganti strategi Phase 2: setelah Phase 1 guarantee 1-soal-per-ET, Phase 2 harus iterasi secara round-robin per-ET (bukan per-package) untuk mengisi slot yang tersisa. Algoritma:
1. Hitung `remaining = K - etGroups.Count` (soal yang sudah di-pick Phase 1)
2. Distribusi remaining secara round-robin per-ET:
   - `basePerET = remaining / etGroups.Count`
   - `extraETs = remaining % etGroups.Count` (ET yang dapat 1 soal ekstra, dipilih acak)
3. Untuk setiap ET, ambil soal-soal tambahan (dari pool yang belum dipilih) sejumlah `basePerET` atau `basePerET + 1`
4. Jika satu ET kehabisan soal, redistribute ke ET lain yang masih ada soal

**Catatan edge case:**
- Jika `remaining > total_soal_tersedia_di_semua_ET`, fallback ke soal NULL-ET
- NULL-ET questions tetap berpartisipasi di Phase 2 (per komentar kode Phase 1 line 1080)

---

### BUG-02: Notifikasi COACH_EVIDENCE_RESUBMITTED — kemungkinan sudah di-fix
**Source:** 235-UAT.md Test 6; CDPController.cs baris 2265-2300
**File:** `Controllers/CDPController.cs`
**Severity:** Minor
**Status:** PERLU VERIFIKASI — kode fix sudah terlihat ada di line 2265-2300

**Temuan riset:**
Analisis kode `CDPController.cs` menunjukkan bahwa `COACH_EVIDENCE_RESUBMITTED` notification sudah ada di `SubmitEvidenceWithCoaching` (baris 2265-2300). Komentar baris 2265 menyebutkan "Phase 235-04: Send COACH_EVIDENCE_RESUBMITTED for previously-rejected deliverables". Ini mengindikasikan fix sudah diterapkan (kemungkinan di v8.6 atau plan lanjutan Phase 235).

**Aksi yang diperlukan:**
- Task pertama: lakukan code review konfirmasi bahwa logic `resubmitFlags` benar-benar mengirim `COACH_EVIDENCE_RESUBMITTED` untuk deliverable yang status-nya `Rejected` sebelum submit
- Jika sudah benar: catat sebagai verified, tidak perlu fix tambahan
- Jika masih ada bug: identifikasi root cause dan fix

---

### BUG-03 hingga BUG-06: Pending Human UAT — Phase 246
**Source:** 246-HUMAN-UAT.md
**File:** Berbagai Views dan Controllers
**Status:** Semua 4 item pending (belum pernah ditest di browser)

| Item | Test | Area Kode |
|------|------|-----------|
| HV-01/HV-04 | Token salah ditolak + regenerate | `CMPController.cs` ValidateToken, `AdminController.cs` RegenerateToken |
| HV-02/HV-03 | Force close + reset via monitoring | `AdminController.cs` AkhiriUjian, ResetAssessment |
| HV-05 | Alarm banner expired muncul untuk HC/Admin | `Views/Home/Index.cshtml`, `_CertAlertBanner.cshtml` |
| HV-06/HV-07 | Records + export Excel di browser | `CDPController`/`AdminController` Records, export Excel |

**Catatan penting (dari 246-VERIFICATION.md, STATE.md):**
- `_CertAlertBanner` hanya muncul untuk HC/Admin — ini by-design, bukan bug
- Code review fase 246 sudah done dengan auto-approve — semua logic terverifikasi via code review

---

### BUG-07: Phase 244 — JS Fix Sudah Done, Browser Verify Pending
**Source:** 244-HUMAN-UAT.md, 244-VERIFICATION.md
**File:** `Views/Admin/AssessmentMonitoringDetail.cshtml`
**Status:** Fix sudah diterapkan (literal newline → `\n` di baris 929), browser verify pending

**JS Bug yang sudah di-fix:**
```javascript
// Sebelum fix (bug): literal newline dalam string
alert('Token: ' + text + '
Salin...')

// Sesudah fix: escape sequence
alert('Token: ' + text + '\nSalin...')
```

**Browser verify yang masih perlu dilakukan:**
1. MON-01: SignalR real-time dual browser (stat cards berubah saat worker ujian)
2. MON-02: Token management sequential flow (copy, regenerate, force close, reset)
3. MON-03: Download file Excel dapat dibuka
4. MON-04: Analytics cascading filter mengubah chart

---

### BUG-08: Phase 235 — Approval Chain Items (5 test yang di-skip sebelumnya)
**Source:** 235-UAT.md (summary: 7 passed, 1 issue, 0 pending, 0 skipped sesuai file)
**Catatan:** 235-UAT.md menunjukkan 8 tests, 7 passed, 1 issue (BUG-02 notifikasi resubmit). Tidak ada item "skipped" di file ini.
**CONTEXT.md D-02** menyebutkan "Phase 235 (5 item approval chain)" — kemungkinan merujuk ke 5 item UAT yang *sempat* pending saat Phase 235 berjalan, dan kemudian diselesaikan.
**Rekomendasi:** Planner harus membaca ulang 235-UAT.md dengan teliti. Berdasarkan riset ini, satu-satunya open item dari Phase 235 adalah BUG-02 (notifikasi resubmit) yang mungkin sudah di-fix.

---

### ADMIN-01: REQUIREMENTS.md Status Update
**Source:** CONTEXT.md D-03; 242-VERIFICATION.md
**File:** `.planning/REQUIREMENTS.md`
**Action:** Update SETUP-01 dan SETUP-02 dari "Pending" ke "Complete" (sudah complete berdasarkan Phase 242 UAT pass)

---

## Standard Stack

### Core (tidak berubah)
| Library | Versi | Purpose |
|---------|-------|---------|
| ASP.NET Core MVC | .NET (project existing) | Web framework |
| Entity Framework Core | project existing | ORM database |
| ClosedXML | project existing | Excel export |
| SignalR | project existing | Real-time monitoring |

**Tidak ada library baru yang diperlukan untuk phase ini.**

---

## Architecture Patterns

### Pattern 1: Fix ET Distribution — Round-Robin per-ET
**Apa:** Ganti Phase 2 BuildCrossPackageAssignment dari distribusi per-package ke round-robin per-ET group
**Kapan digunakan:** Saat `K > etGroups.Count` (lebih banyak soal yang dibutuhkan dari jumlah ET)

```csharp
// SEBELUM (distribusi per-package — SALAH)
int N = packages.Count; // N = jumlah paket
// baseCount = remaining / N  → balance per package

// SESUDAH (distribusi per-ET — BENAR)
int M = etGroups.Count; // M = jumlah ET group
int basePerET = remaining / M;
int extraCount = remaining % M;
var extraETs = etGroups.OrderBy(_ => rng.Next()).Take(extraCount).ToHashSet();

foreach (var et in etGroups)
{
    int quota = basePerET + (extraETs.Contains(et) ? 1 : 0);
    var etCandidates = allQuestions
        .Where(x => x.Question.ElemenTeknis == et && !selectedIds.Contains(x.Question.Id))
        .Select(x => x.Question.Id)
        .ToList();
    Shuffle(etCandidates, rng);
    int toTake = Math.Min(quota, etCandidates.Count);
    foreach (var id in etCandidates.Take(toTake))
    {
        selectedIds.Add(id);
        selectedList.Add(id);
    }
    // Jika kurang: deficit akan di-handle di fallback NULL-ET atau redistribute
}
```

**Catatan:** Logika fallback untuk kasus kuota tidak terpenuhi (ET kehabisan soal) harus tetap ada.

### Pattern 2: UAT Browser Verification Flow
**Apa:** Untuk setiap pending human UAT, jalankan aplikasi dan test flow secara manual.
**Pattern yang sudah established (dari Phase 242-246):**
1. Run `dotnet run` di terminal
2. Login dengan role yang sesuai
3. Ikuti test script dari HUMAN-UAT.md
4. Catat hasil: pass / issue
5. Jika issue: fix kode → test ulang

### Pattern 3: Atomic Fix per Bug (D-04)
**Apa:** Fix satu bug → langsung test → next bug. Tidak batch.
**Mengapa:** Memudahkan isolasi regresi jika fix satu bug mempengaruhi bug lain.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Excel export | Custom binary writer | ClosedXML (sudah ada) |
| Real-time push | Custom polling | SignalR (sudah ada) |
| ET distribution balancing | Complex custom scheduler | Round-robin modulo arithmetic (sederhana, sudah pattern-nya) |

---

## Common Pitfalls

### Pitfall 1: NULL-ET Questions Terlupakan di Fix ET Distribution
**Apa yang salah:** Fix Phase 2 hanya iterasi per-ET, tapi soal dengan `ElemenTeknis = null` tidak masuk ET group manapun → tidak ter-pick di Phase 2.
**Root cause:** NULL-ET questions memang exclude dari Phase 1 (by design, komentar baris 1080). Phase 2 HARUS tetap bisa pick NULL-ET questions jika kuota per-ET belum terpenuhi.
**Cara hindari:** Setelah round-robin per-ET selesai, jika `selectedList.Count < K`, gunakan NULL-ET questions sebagai fallback pool.

### Pitfall 2: Fix Mengubah Behavior Yang Sudah Bekerja
**Apa yang salah:** Mengubah Phase 1 atau Phase 3 saat yang perlu diubah hanya Phase 2.
**Root cause:** Scope creep saat editing kode.
**Cara hindari:** Hanya ubah blok Phase 2 (baris 1099-1148). Phase 1 (baris 1079-1097) dan Phase 3 (baris 1151-1153) tidak perlu diubah.

### Pitfall 3: Human UAT Ditandai Pass Tanpa Benar-Benar Ditest di Browser
**Apa yang salah:** Auto-approve checkpoint tanpa browser test menghasilkan false pass.
**Root cause:** --auto mode di Phase 244/246 menyebabkan banyak human checkpoint di-skip.
**Cara hindari:** D-04 — fix satu per satu, test benar-benar di browser.

### Pitfall 4: Regresi di SignalR setelah Fix JS di AssessmentMonitoringDetail
**Apa yang salah:** Saat verify Phase 244, perubahan kecil pada file `.cshtml` bisa merusak SignalR script block lain.
**Root cause:** File 1.233 baris dengan banyak `@if` block interleaved dengan JavaScript.
**Cara hindari:** Jangan ubah file selama browser verify kecuali ada bug ditemukan. Jika fix diperlukan, hanya ubah baris minimal yang terkena bug.

---

## Code Examples

### Lokasi Kode Kritis

**ET Distribution — BuildCrossPackageAssignment:**
```
Controllers/CMPController.cs
- Baris 1000-1024: Single-package early return
- Baris 1027-1072: Fallback (no ET data)
- Baris 1075-1097: Phase 1 — guarantee 1 per ET
- Baris 1099-1148: Phase 2 — YANG PERLU DI-FIX (distribusi per-package → harus per-ET)
- Baris 1151-1153: Phase 3 — Fisher-Yates shuffle (jangan diubah)
```

**Notifikasi Resubmit di SubmitEvidenceWithCoaching:**
```
Controllers/CDPController.cs
- Baris 2195: resubmitFlags tracking (sebelum status diubah)
- Baris 2265-2300: COACH_EVIDENCE_RESUBMITTED block (perlu verifikasi kode sudah benar)
```

**Token Management (Phase 246 verify):**
```
Controllers/AdminController.cs
- Baris 2155-2203: RegenerateToken
- Baris 2693: AkhiriUjian
- Baris 2585: ResetAssessment
Controllers/CMPController.cs
- Baris 693-696: Token validation (menolak token tidak cocok)
```

**Alarm Banner (Phase 246 verify):**
```
Views/Home/Index.cshtml (atau Views/Shared/_CertAlertBanner.cshtml)
- Hanya muncul untuk HC/Admin — by-design
```

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser UAT (sesuai REQUIREMENTS.md "Automated browser testing: Out of Scope") |
| Config file | none |
| Quick run command | `dotnet run` lalu test via browser |
| Full suite command | N/A — manual verification |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | File |
|--------|----------|-----------|------|
| FIX-01 | ET distribution balanced per-ET (≤1 soal gap antar ET) | Manual browser — lihat radar chart hasil ujian | CMPController.cs BuildCrossPackageAssignment |
| FIX-01 | COACH_EVIDENCE_RESUBMITTED dikirim saat re-submit Rejected | Code review + manual verification | CDPController.cs SubmitEvidenceWithCoaching |
| FIX-01 | Token management (copy, regen, force close, reset) berfungsi | Manual browser — monitoring detail | AssessmentMonitoringDetail.cshtml |
| FIX-01 | Alarm banner expired muncul untuk HC/Admin | Manual browser — Home/Index login HC | _CertAlertBanner partial |
| FIX-01 | Records + Excel export berhasil | Manual browser — download dan buka file | CDPController/AdminController export |
| FIX-01 | SignalR real-time monitoring berfungsi | Manual browser — dual browser test | AssessmentHub.cs |

### Wave 0 Gaps
- Tidak ada infrastruktur test otomatis yang perlu disiapkan — semua testing adalah manual browser UAT sesuai design phase ini.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET Runtime | dotnet run | Assume ✓ | Existing project | — |
| Browser (Chrome/Edge) | Human UAT | ✓ | N/A | — |
| Database (SQLite) | Seed data & UAT | Assume ✓ (HcPortal.db) | — | — |

Step 2.6: Environment audit minimal — phase ini murni code fix + browser verify, tidak ada dependensi eksternal baru.

---

## Open Questions

1. **Status sebenarnya BUG-02 (notifikasi resubmit Phase 235)**
   - Yang diketahui: CDPController line 2265-2300 sudah memiliki kode `COACH_EVIDENCE_RESUBMITTED`
   - Yang belum jelas: apakah fix ini sudah complete dan benar, atau hanya partial yang belum teruji
   - Rekomendasi: Task pertama Phase 247 harus code review CDPController `SubmitEvidenceWithCoaching` untuk konfirmasi logic `resubmitFlags` benar

2. **Arti "Phase 235 (5 item approval chain)" di CONTEXT.md D-02**
   - Yang diketahui: 235-UAT.md mencatat 8 tests dengan 7 pass dan 1 issue — tidak ada 5 item skipped
   - Yang belum jelas: apakah "5 item" merujuk ke UAT sebelum Phase 235 dijalankan (pending dari planning), atau ada file UAT lain
   - Rekomendasi: Planner baca MEMORY.md `project_phase235_pending_uat.md` untuk clarifikasi

3. **ET Distribution: apakah ada kasus dengan soal NULL-ET lebih dari soal ber-ET?**
   - Yang diketahui: Seed saat ini punya 15 soal dengan ET terisi semua (4 ET)
   - Yang belum jelas: edge case production di mana banyak soal NULL-ET
   - Rekomendasi: Fix harus tetap handle NULL-ET sebagai fallback pool

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` baris 1000-1153 — BuildCrossPackageAssignment full code review
- `Controllers/CDPController.cs` baris 2195-2300 — SubmitEvidenceWithCoaching + resubmit notification
- `.planning/phases/243-uat-exam-flow/243-UAT.md` — ET distribution bug note
- `.planning/phases/235-audit-execution-flow/235-UAT.md` — approval chain UAT results
- `.planning/phases/244-uat-monitoring-analytics/244-HUMAN-UAT.md` — monitoring UAT status
- `.planning/phases/246-uat-edge-cases-records/246-HUMAN-UAT.md` — edge cases UAT pending items
- `.planning/phases/247-bug-fix-pasca-uat/247-CONTEXT.md` — phase decisions
- `.planning/REQUIREMENTS.md` — FIX-01 requirement

### Secondary (MEDIUM confidence)
- `.planning/phases/244-uat-monitoring-analytics/244-VERIFICATION.md` — human verification requirements detail
- `.planning/STATE.md` — accumulated decisions dan pending todos

---

## Metadata

**Confidence breakdown:**
- Bug inventory: HIGH — semua bug dikonfirmasi dari UAT files dan code review
- Fix strategy (ET distribution): HIGH — root cause dan algoritma fix jelas dari analisis kode
- Fix strategy (notifikasi resubmit): MEDIUM — fix sudah ada di kode tapi belum diverifikasi
- Human UAT pending items: HIGH — jelas apa yang perlu ditest dan di mana kodenya
- Architecture: HIGH — tidak ada teknologi baru, semua existing patterns

**Research date:** 2026-03-24
**Valid until:** 2026-04-24 (stable domain, 30 hari)
