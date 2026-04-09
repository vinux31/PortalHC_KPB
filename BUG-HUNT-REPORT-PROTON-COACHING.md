# Bug Hunt Report: Proton Coaching

- **Date:** 2026-04-08
- **Auditor:** Claude (read-only static audit)
- **Scope:** Full feature — Coach-Coachee Mapping, CoachingSession, Bulk Evidence Submission, Approval Chain, Role-Based Scoping, Export, Action Item
- **Method:** Static code review (tidak ada modifikasi kode produksi). File yang dibaca terdaftar di Appendix.

---

> **Fix progress 2026-04-09:** CRIT-01, CRIT-02, CRIT-03, MED-06, dan HIGH-03 sudah ditutup. Catatan: fix CRIT-03 bersifat minimal (gated reset + audit log, **tanpa RowVersion**) sehingga masih ada residual race kecil untuk skenario spesifik "coach resubmit progress yg baru saja di-Reject bersamaan dengan reviewer yg sedang approve entry stale" — full optimistic locking via RowVersion adalah follow-up optional.

## Executive Summary

- **Total findings: 21** — Critical: 3, High: 7, Medium: 8, Low: 3
- **Top 3 risiko:**
  1. **[CRIT-01] Shared EvidencePath pada bulk submit** — semua progress yang disubmit bersamaan menunjuk ke folder progress pertama. Jika progress pertama dihapus / unit-rebuild → semua file evidence broken link, dan progress lain kehilangan bukti audit.
  2. **[CRIT-02] Orphan CoachingSession setelah unit-change rebuild** — `ProtonDeliverableProgressId` non-FK; rebuild menghapus progress tapi `CoachingSessions` & `ActionItems` tertinggal → data riwayat coaching menggantung tanpa parent, muncul di export PDF/Excel sebagai entri "hantu", `DownloadEvidencePdf` 404.
  3. **[CRIT-03] Silent approval reset (lost update) saat resubmit** — `SubmitEvidenceWithCoaching` me-reset `SrSpvApprovalStatus`/`ShApprovalStatus` ke "Pending" tanpa concurrency check. Jika SH meng-approve di tab lain bersamaan dengan coach resubmit, approval SH bisa hilang tanpa jejak.

---

## Findings

### [CRIT-01] Shared EvidencePath antar progress di bulk submit
- **Status:** ✅ FIXED 2026-04-09 (buffer file sekali ke memory, tulis satu file per progress folder — lihat `Controllers/CDPController.cs:2169-2235`)
- **Severity:** Critical
- **Category:** Logic / Data Integrity
- **File:** `Controllers/CDPController.cs:2172-2214`
- **Description:** Saat coach submit evidence untuk **banyak** `progressIds` sekaligus dengan satu file, controller membentuk path sekali pakai `firstProgressId`:
  ```csharp
  int firstProgressId = progressIds.First();
  var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "evidence", firstProgressId.ToString());
  ...
  evidencePath = $"/uploads/evidence/{firstProgressId}/{safeFileName}";
  ```
  Lalu loop `foreach (var progress in progresses)` menyalin `evidencePath` yang sama ke setiap `progress.EvidencePath`. Konsekuensi:
  1. Semua progress lain bergantung pada file yang secara fisik berada di folder progress *pertama*.
  2. Jika progress pertama kemudian dihapus (mis. via Phase 129 unit-change rebuild di `CoachMappingController.cs:691` `CleanupProgressForAssignment`), file/folder ikut hilang → evidence progress lain ikut 404.
  3. Audit trail jadi rancu: `EvidencePath` menunjuk ke ID yang tidak lagi ada.
- **Reproduction:**
  1. Sebagai Coach, buka CoachingProton → pilih ≥2 deliverable milik coachee yang sama.
  2. Submit dengan satu file PDF.
  3. Cek DB: `ProtonDeliverableProgresses.EvidencePath` semua row akan identik (`/uploads/evidence/{firstId}/...`).
  4. Hapus/rebuild progress pertama (mis. ubah Assignment Unit di mapping).
  5. Buka deliverable kedua → link evidence broken.
- **Impact:** Hilangnya bukti audit lintas progress. Muncul saat Section Head / HC mereview → mereka mengira coach tidak upload, padahal sebelumnya ada. Berdampak ke compliance (Proton certification).
- **Recommendation:** Simpan satu file per progressId (loop simpan stream per id) atau simpan sekali ke lokasi netral (mis. hash konten / GUID folder) lalu setiap progress menyimpan pointer yang sama tetapi *file lifecycle dilepas dari progress ID*. Idealnya: copy stream ke MemoryStream sekali, lalu tulis N file. Atau buat tabel `EvidenceFile` (1) ↔ N progress.

---

### [CRIT-02] Orphan CoachingSession & ActionItem akibat non-FK + rebuild
- **Severity:** Critical
- **Category:** Logic / Data Integrity
- **File:** `Models/CoachingSession.cs:23` (non-FK), `Controllers/CoachMappingController.cs:686-705` (Phase 129 rebuild), `CoachMappingController.cs:657` (`CleanupProgressForAssignment`)
- **Description:** `CoachingSession.ProtonDeliverableProgressId` adalah `int?` tanpa FK constraint (komentar di model: "No FK constraint — matches project pattern"). Pada Phase 129 rebuild, semua `ProtonDeliverableProgresses` untuk assignment dihapus, kemudian dibuat ulang dengan ID baru. `CoachingSessions` (dan `ActionItems` di bawahnya) tidak ikut dihapus → menggantung dengan FK pointer ke ID yang tidak lagi ada.
- **Reproduction:**
  1. Coach submit evidence + isi catatan untuk sebuah deliverable → CoachingSession #X dibuat dengan `ProtonDeliverableProgressId=Y`.
  2. Admin edit mapping coachee tsb dan ubah `AssignmentUnit`.
  3. Phase 129 trigger: progress Y dihapus, progress baru Z dibuat.
  4. Query `CoachingSessions WHERE Id=X` masih ada, `ProtonDeliverableProgressId=Y` (sudah orphan).
  5. Buka `ExportProgressExcel` / `ExportProgressPdf` untuk coachee → entries lama bisa muncul di lookup `coachingSessions[p.Id]` (tidak match → kosong) tetapi tetap ada di DB konsumsi audit.
  6. `DownloadEvidencePdf(progressId=Y)` → NotFound walau session masih punya konten coaching.
- **Impact:** Riwayat coaching hilang dari UI tetapi tetap di DB → confusion auditor; `ActionItems` tetap "Open" tapi tidak terkait progress manapun → backlog ghost.
- **Recommendation:**
  - Tambahkan FK + cascade soft-handling (mis. set `ProtonDeliverableProgressId=NULL` saat parent dihapus), atau
  - Saat `CleanupProgressForAssignment`, jalankan juga `_context.CoachingSessions.Where(cs => idsBeingDeleted.Contains(cs.ProtonDeliverableProgressId.Value))` → pilihan: archive ke tabel history atau hard delete + cascade ActionItems.

---

### [CRIT-03] Silent approval reset (lost update) saat resubmit
- **Status:** ✅ FIXED 2026-04-09 (minimal — gated reset + history log, no RowVersion)
- **Severity:** Critical
- **Category:** Concurrency / Data Integrity
- **File:** `Controllers/CDPController.cs:2199-2207`
- **Description:** Pada bulk submit, untuk setiap progress (termasuk yang berstatus *Pending*), controller mereset:
  ```csharp
  progress.SrSpvApprovalStatus = "Pending";
  progress.SrSpvApprovedById = null;
  progress.SrSpvApprovedAt = null;
  progress.ShApprovalStatus = "Pending";
  ...
  ```
  Tidak ada concurrency token (`[ConcurrencyCheck]`/`RowVersion`). Skenario race:
  1. Sr Spv membuka tab review pukul 10:00:00, klik Approve pukul 10:00:05.
  2. Coach pukul 10:00:04 me-resubmit (atau bahkan re-edit field) di tab lain.
  3. EF Change Tracker dua request berbeda → request resubmit overwrite kolom approval Sr Spv menjadi `Pending` dengan `null` reviewer.
  4. Tidak ada `RecordStatusHistory` untuk reset → audit trail hilang.
- **Reproduction:** Dua sesi browser, satu sebagai Coach, satu sebagai Sr Spv. Lakukan submit & approve hampir bersamaan; salah satu kalah tanpa error.
- **Impact:** Approval SH/SrSpv hilang silent. Compliance & accountability rusak. Tidak bisa direkonstruksi karena tidak ada history reset.
- **Recommendation:**
  - Tambahkan `RowVersion` pada `ProtonDeliverableProgress` & wrap save dengan `DbUpdateConcurrencyException` handling.
  - Larang submit jika status sudah `Approved` (saat ini hanya cek Pending/Rejected — sudah benar untuk progress.Status; tetapi reset approval columns tetap dilakukan walau status `Pending` tanpa pernah ditolak). Rapikan: hanya reset approval kolom jika ada perubahan substantif, dan log resetnya ke `RecordStatusHistory`.

---

### [HIGH-01] Race condition file overwrite (timestamp collision)
- **Status:** ✅ FIXED 2026-04-09 (timestamp ms + short GUID di FileUploadHelper dan inline SubmitEvidenceWithCoaching)
- **Severity:** High
- **Category:** Concurrency
- **File:** `Controllers/CDPController.cs:2173-2178`
- **Description:** `safeFileName = $"{timestamp}_{Path.GetFileName(...)}"` dengan timestamp `yyyyMMddHHmmss` (resolusi detik) + `FileMode.Create`. Jika dua coach submit dalam detik yang sama dengan nama file identik (mis. "evidence.pdf") ke folder progressId yang sama (skenario mungkin di [CRIT-01]), file dittimpa diam-diam.
- **Reproduction:** Dua coach melakukan submit untuk progressId yang sama (atau bulk yang firstProgressId-nya kebetulan sama) di detik yang sama dengan filename identik.
- **Impact:** File evidence terganti tanpa jejak.
- **Recommendation:** Gunakan `Guid.NewGuid().ToString("N")` atau timestamp `ffffff` (mikrodetik) atau `FileMode.CreateNew` (dengan retry-suffix).

---

### [HIGH-02] CoachCoacheeMappingAssign tidak transactional
- **Status:** ✅ FIXED 2026-04-09 (CoachCoacheeMappingAssign kini dibungkus BeginTransactionAsync — partial failure rollback atomic)
- **Severity:** High
- **Category:** Logic / Data Integrity
- **File:** `Controllers/CoachMappingController.cs:515-572`
- **Description:** `AddRange(newMappings)` + dalam loop untuk side-effect ProtonTrack memanggil `_context.SaveChangesAsync()` (line 563) per coachee, lalu `AutoCreateProgressForAssignment` yang juga menulis. Akhirnya `SaveChangesAsync` final di line 572. Tidak ada `BeginTransactionAsync`. Bila exception terjadi di tengah (mis. coachee ke-3 gagal create progress) → state campur: sebagian mapping ada, sebagian assignment, progress tidak lengkap.
- **Recommendation:** Bungkus seluruh blok dengan `using var tx = await _context.Database.BeginTransactionAsync(); ... await tx.CommitAsync();` setelah save final.

---

### [HIGH-03] Phase 129 unit-change rebuild tidak transactional
- **Status:** ✅ FIXED (ditutup oleh CRIT-02 — transaction wrap di CoachCoacheeMappingEdit)
- **Severity:** High
- **Category:** Logic / Data Integrity
- **File:** `Controllers/CoachMappingController.cs:678-705`
- **Description:** Loop pertama menghapus progress, `SaveChangesAsync()` (line 693), loop kedua membuat ulang progress dengan `AutoCreateProgressForAssignment` (multiple internal saves). Tidak ada transaction. Jika create batch gagal di tengah, coachee kehilangan deliverables tanpa rollback.
- **Reproduction:** Edit mapping → ubah unit ke unit yang `OrganizationUnit` tidak punya kompetensi lengkap (atau hentikan koneksi DB di tengah). Hasil: progress lama hilang, baru hanya sebagian.
- **Impact:** Coachee tidak bisa lanjut Proton karena deliverable tidak lengkap; recovery manual.
- **Recommendation:** Wrap dengan transaction, dan ganti pola "delete-recreate" dengan "diff-merge" agar progress yang masih relevan dipertahankan tanpa kehilangan riwayat (lihat juga CRIT-02).

---

### [HIGH-04] Reactivasi mapping via Excel Import tidak reactivate ProtonTrackAssignment
- **Status:** ✅ FIXED 2026-04-09 (ImportCoachCoacheeMapping kini ikut mereaktivasi last ProtonTrackAssignment per coachee di dalam transaction)
- **Severity:** High
- **Category:** Logic
- **File:** `Controllers/CoachMappingController.cs:330-345`
- **Description:** Path import Excel hanya men-set `IsActive=true` pada `CoachCoacheeMapping`, tetapi **tidak** men-reuse / re-aktifkan `ProtonTrackAssignment` yang dulu pernah dibuat (FIX-02 hanya diterapkan di `CoachCoacheeMappingAssign` line 530). Akibatnya, coachee yang di-reactivate via import tidak punya assignment & progress aktif → halaman CoachingProton menampilkan kosong.
- **Reproduction:** Buat mapping via Assign (dengan ProtonTrackId) → deactivate → import excel berisi NIP yang sama → mapping aktif lagi tetapi `CoachingProton` (level ≤3) tidak lagi memunculkan coachee karena scope filter `ProtonTrackAssignments.IsActive` (line 1427).
- **Impact:** Coach tidak bisa submit evidence; user bingung "kenapa coachee saya hilang".
- **Recommendation:** Tambahkan logika reuse-or-reactivate `ProtonTrackAssignment` di blok reactivation import (line 333-344), dengan TrackId opsional di template Excel (atau tetap pakai track terakhir milik coachee).

---

### [HIGH-05] Bulk submit lintas coachee sharing evidencePath
- **Status:** ✅ FIXED 2026-04-09 (server-side guard coacheeIds.Count > 1)
- **Severity:** High
- **Category:** Logic
- **File:** `Controllers/CDPController.cs:2148-2184`
- **Description:** Validasi `validCoacheeIds` hanya memastikan SEMUA coachee adalah mapped ke coach, tetapi tidak mencegah mengirim progressIds dari beberapa coachee sekaligus. Kombinasi dengan [CRIT-01]: file fisik tersimpan di folder `firstProgressId` (milik coachee A), tetapi `EvidencePath` juga ditulis ke progress milik coachee B → audit visual: file evidence "Coachee B" sebenarnya milik berkas yang difolder progress coachee A.
- **Recommendation:** Tolak request jika `coacheeIds.Count > 1` (atau pisahkan loop file per coachee). Tambah validasi UI agar selection bulk hanya dalam satu coachee.

---

### [HIGH-06] DeleteCoachingSession tidak membersihkan EvidencePath / progress.Status
- **Severity:** High
- **Category:** Logic / Data Integrity
- **File:** `Controllers/CDPController.cs:2346-2375`
- **Description:** Menghapus session + ActionItems, tetapi `ProtonDeliverableProgress.Status` tetap `Submitted/Approved` dan `EvidencePath` tetap menunjuk ke file. Jika ini satu-satunya sesi → progress kehilangan konteks coaching tetapi tetap tampak "sudah upload". File fisik juga tidak dihapus → leak storage.
- **Recommendation:** Hapus juga file via `System.IO.File.Delete(...)`, dan jika ini sesi terakhir untuk progress, set status kembali ke `Pending` + reset approval kolom (dengan log).
- **Status:** ✅ FIXED 2026-04-09 (DeleteCoachingSession sekarang cleanup file fisik + revert progress state saat session terakhir, skip jika Approved)

---

### [HIGH-07] ExportProgressExcel/Pdf — N+1 & scoping tidak konsisten dengan AssignmentSection
- **Status:** ✅ FIXED 2026-04-09 (Coach ditambahkan ke Authorize PDF; scope check direfactor ke helper CanExportCoacheeProgressAsync — Coach pakai mapping aktif, SectionScoped pakai AssignmentSection mapping)
- **Severity:** High
- **Category:** Security / Performance
- **File:** `Controllers/CDPController.cs:2381-2443`, `2447-2542`
- **Description:**
  1. Authorization Excel: `Coach, Sr Supervisor, Section Head, HC, Admin`. Authorization PDF: tanpa `Coach`. Inkonsistensi → coach bisa export Excel tapi tidak PDF (sengaja atau bug?).
  2. Scope check membandingkan `coacheeUser.Section` dengan `user.Section` — mengabaikan `CoachCoacheeMapping.AssignmentSection`. Skenario: coach yang section pribadinya berbeda dari `AssignmentSection` (mungkin saat dipinjam lintas section) tidak bisa export coachee yang dimapping ke dia.
  3. Sebaliknya: Sr Supervisor dengan section sama dengan coachee tetapi tidak punya hak review pada coachee tsb tetap bisa export.
- **Recommendation:** Samakan authorization role list. Untuk Coach: cek `CoachCoacheeMappings.Any(m=>m.CoachId==user.Id && m.CoacheeId==coacheeId && m.IsActive)`. Untuk SectionHead/SrSpv: cek `AssignmentSection` mapping aktif, bukan section coachee.

---

### [MED-01] ImportCoachCoacheeMapping silent skip duplicate NIP
- **Status:** ✅ FIXED 2026-04-09 (deteksi NIP duplikat di Users → `TempData["ImportWarnings"]` non-blocking)
- **Severity:** Medium
- **Category:** UX / Data Integrity
- **File:** `Controllers/CoachMappingController.cs:220-226`
- **Description:** `usersByNip = ... .GroupBy(u => u.NIP!).ToDictionary(g => g.Key, g => g.First());` — jika ada dua user `ApplicationUser` dengan NIP sama (legacy data), mapping akan silent menempel ke user pertama (urutan tidak deterministik). Tidak dilaporkan ke admin.
- **Recommendation:** Saat membangun dictionary, deteksi duplikat dan tambahkan warning ke `TempData["ImportWarnings"]`. Atau tolak import jika ada duplikat NIP.

---

### [MED-02] Acuan & CatatanCoach tanpa max length
- **Status:** ✅ FIXED 2026-04-09 (validasi panjang server-side di SubmitEvidenceWithCoaching & EditCoachingSession: CatatanCoach ≤ 4000, Acuan ≤ 2000, Kesimpulan/Result ≤ 100)
- **Severity:** Medium
- **Category:** UX / DoS
- **File:** `Models/CoachingSession.cs:12-18`
- **Description:** Field `CatatanCoach`, `Acuan*` tidak punya `[MaxLength]`/`[StringLength]` → kolom `nvarchar(max)`. Tidak ada validasi server-side → user (atau attacker dengan akun coach) bisa kirim payload puluhan MB → DB bloat, export PDF/Excel jadi sangat lambat.
- **Recommendation:** Tambah `[StringLength(4000)]` (atau angka business-appropriate) + validasi controller.

---

### [MED-03] `Date` parameter CoachingSession tanpa bound
- **Status:** ✅ FIXED 2026-04-09 (date dibatasi `today.AddYears(-2) ≤ date ≤ today.AddDays(1)` di SubmitEvidenceWithCoaching)
- **Severity:** Medium
- **Category:** UX / Validasi
- **File:** `Controllers/CDPController.cs:2102, 2221`
- **Description:** Tidak ada validasi server-side `date` (tahun 1900? tahun 2099?). Salah ketik tanggal langsung tercatat ke audit & export.
- **Recommendation:** Validasi `date >= mappingStartDate && date <= DateTime.Today.AddDays(1)`.

---

### [MED-04] Notification dispatch silent catch
- **Status:** ✅ FIXED 2026-04-09 (SubmitEvidenceWithCoaching expose `notificationFailed` boolean di JSON response; UI bisa surface warning toast)
- **Severity:** Medium
- **Category:** UX / Observability
- **File:** `Controllers/CDPController.cs:2295`, `Controllers/CoachMappingController.cs:594`
- **Description:** Semua blok notifikasi dibungkus `try/catch (Exception ex) { _logger.LogWarning(...); }`. Kegagalan notifikasi (mis. SignalR down, NotificationService crash) hanya nyangkut di log file. User tetap mendapat success response → asumsi salah bahwa "Sr Spv pasti dinotif".
- **Recommendation:** Sertakan `notificationFailed: true` di response JSON saat catch terpicu, atau push warning toast UI. Atau pakai outbox pattern.

---

### [MED-05] N+1 query di NotifyReviewers loop
- **Status:** ✅ FIXED 2026-04-09 (NotifyReviewersAsync merge mapping+user ke satu LINQ Join query; HIGH-05 fix juga mengurangi outer loop ke 1 coachee per request)
- **Severity:** Medium
- **Category:** Performance
- **File:** `Controllers/CDPController.cs:2245-2293`
- **Description:** Loop per coachee memanggil `NotifyReviewersAsync(cid, ...)` (kemungkinan query DB internal) dan `_context.CoachCoacheeMappings.FirstOrDefaultAsync(...)` di line 2274. Untuk bulk submit 30 deliverable lintas 10 coachee → 10–20 query.
- **Recommendation:** Pre-load semua reviewer dalam satu query, satu mapping query, lalu loop di-memory.

---

### [MED-06] ApprovalStatus reset bahkan saat status Pending (bukan resubmit)
- **Status:** ✅ FIXED 2026-04-09 (ditutup oleh fix CRIT-03)
- **Severity:** Medium
- **Category:** Logic
- **File:** `Controllers/CDPController.cs:2199-2207`
- **Description:** Reset SrSpv/Sh kolom dilakukan untuk setiap progress di loop, padahal hanya progress dengan status `Rejected` yang seharusnya butuh reset. Untuk pertama kali submit (Pending → Submitted), kolom approval awalnya sudah Pending — tidak masalah, tetapi pola ini membuat developer mudah melewatkan kasus "submit ulang setelah Approved" bila validasi line 2157 dilonggarkan suatu hari.
- **Recommendation:** Reset hanya ketika `isResubmit == true`.

---

### [MED-07] CoachingProton level 6 — tidak mempertimbangkan mapping tidak aktif
- **Severity:** Medium
- **Category:** Security / UX
- **File:** `Controllers/CDPController.cs:1453-1457`
- **Description:** Coachee level 6 hanya boleh melihat dirinya jika `ProtonTrackAssignments.IsActive`. OK. Tetapi user tidak diberi feedback alasan jika kosong — mereka mengira sistem rusak.
- **Recommendation:** Tampilkan banner "Belum ada assignment Proton aktif" di view.

---

### [MED-08] EditCoachingSession — ownership berbasis CoachId historis
- **Severity:** Medium
- **Category:** Security / Logic
- **File:** `Controllers/CDPController.cs:2308-2344`
- **Description:** Coach yang sudah TIDAK lagi mapped ke coachee tetap bisa edit session lama miliknya (`session.CoachId == user.Id`). Tidak ada cek `CoachCoacheeMapping.IsActive`. Apakah ini disengaja? Kalau pengelolaan riwayat oleh HC saja, batasi ini ke HC/Admin setelah mapping berakhir.
- **Recommendation:** Tambah cek mapping aktif (atau toleransi window 7 hari setelah end-date).

---

### [LOW-01] Naming inconsistency `Sr Supervisor` vs `SrSpv`
- **Severity:** Low
- **Category:** Maintainability
- **File:** `Controllers/CDPController.cs:2380, 2446`
- **Description:** `[Authorize(Roles = "Sr Supervisor, ...")]` menggunakan string dengan spasi, sementara kolom DB & kode lain memakai `SrSpv`. Sangat mudah salah ketik → 403 sulit didiagnosa.
- **Recommendation:** Konstanta di `UserRoles` (mis. `UserRoles.SrSpv = "Sr Supervisor"`).

---

### [LOW-02] ExportProgressPdf tidak mengizinkan Coach
- **Status:** ✅ FIXED 2026-04-09 (ditutup oleh fix HIGH-07 — Coach ditambahkan ke Authorize list ExportProgressPdf)
- **Severity:** Low
- **Category:** UX
- **File:** `Controllers/CDPController.cs:2446`
- **Description:** Roles `"Sr Supervisor, Section Head, HC, Admin"` — tanpa `Coach`. Inkonsisten dengan Excel.
- **Recommendation:** Tambahkan `Coach` jika memang seharusnya boleh.

---

### [LOW-03] XSS — perlu verifikasi view (Razor auto-encode oke, cek `@Html.Raw`)
- **Status:** ✅ CLEAR 2026-04-09 (audit `@Html.Raw` di Views/CDP & Views/Admin — semua usage adalah output helper server-side atau JSON serializer, tidak ada pass-through user-input seperti `CatatanCoach`/`Acuan*`)
- **Severity:** Low (perlu konfirmasi view-side)
- **Category:** Security
- **File:** `Views/CDP/CoachingProton.cshtml`, `Views/CDP/EditCoachingSession.cshtml`
- **Description:** Field `CatatanCoach`, `Acuan*` di-render via Razor. Razor auto-encode by default. Audit tidak meng-grep `Html.Raw` di view-view ini — perlu verifikasi cepat. Karena field tanpa max length ([MED-02]), payload XSS panjang feasible.
- **Recommendation:** `grep -r "Html.Raw" Views/CDP Views/Admin` dan pastikan tidak ada penggunaan pada field user-input di atas.

---

## Cross-Cutting Observations
- **Audit log coverage:** Semua mutating action di `CoachMappingController` dan `EditCoachingSession`/`DeleteCoachingSession` ter-log via `_auditLog` atau `_context.AuditLogs`. **Tetapi** `SubmitEvidenceWithCoaching` (line 2100) tidak memanggil `_auditLog` — hanya `RecordStatusHistory`. Sebaiknya tambahkan AuditLog untuk traceability cross-feature.
- **Reset approval tanpa history:** `RecordStatusHistory` hanya dipanggil untuk perubahan status `progress.Status`, bukan untuk perubahan kolom `*ApprovalStatus`. Saat reset di [CRIT-03], history approval hilang.
- **Naming role string:** lihat [LOW-01].

## Methodology Notes
- Read-only static review pada controller utama, model terkait, dan path tertentu di view (high-level only).
- Tidak menjalankan kode, tidak membangun dynamic test cases.
- Fokus pada 4 phase audit (Logic, Security, Concurrency, UX) + cross-cutting.
- Race condition findings adalah analisa logis (bukan reproduksi runtime).

## Out of Scope
- Eksekusi fix (sesi terpisah).
- Perubahan migration / schema.
- Perf benchmarking detail (hanya flag N+1 obvious).
- Penetration testing aktif.
- Fitur Assessment.

## Files Audited
- `Controllers/CoachMappingController.cs` (lines 180-720, fokus 197-406, 408-720)
- `Controllers/CDPController.cs` (lines 1408-1740, 2080-2542)
- `Models/CoachingSession.cs`
- `Models/ActionItem.cs`
- *Tidak* dibaca penuh (perlu sesi lanjutan jika diperlukan): `Models/CoachCoacheeMapping.cs`, `Models/ProtonModels.cs`, `Views/CDP/CoachingProton.cshtml`, `Views/CDP/EditCoachingSession.cshtml`, `Views/Admin/CoachCoacheeMapping.cshtml`, migration files, `wwwroot/js/*`.

---

## Prioritization Cheat Sheet (untuk sesi fix berikutnya)
1. **CRIT-01 / HIGH-05** — Fix bulk evidence file storage (satu file per progress, atau tabel EvidenceFile shared).
2. **CRIT-02 / HIGH-03** — Tambahkan FK + cascade strategy + transactionalize Phase 129 rebuild.
3. **CRIT-03 / MED-06** — Concurrency token + history approval reset.
4. **HIGH-02 / HIGH-04** — Transactionalize Assign + Reactivate path; reactivate ProtonTrackAssignment di Import.
5. **HIGH-06 / HIGH-07** — DeleteCoachingSession cleanup; samakan & perbaiki scoping export.
6. **MED-01..08, LOW-01..03** — Cleanup pass berikutnya.
