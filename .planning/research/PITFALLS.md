# Pitfalls Research

**Domain:** Coaching/Mentoring Platform Audit — Proton Coaching Ecosystem (PortalHC KPB)
**Researched:** 2026-03-22
**Confidence:** HIGH (berdasarkan inspeksi langsung kode + riwayat audit v4.0 dan v8.1)

---

## Critical Pitfalls

### Pitfall 1: Approval Chain State Explosion — Modifikasi Multi-Flag Status Tanpa Atomic Guard

**What goes wrong:**
`ProtonDeliverableProgress` menyimpan tiga jalur approval terpisah: `Status` (main), `SrSpvApprovalStatus`, `ShApprovalStatus`, dan `HCApprovalStatus`. Ketika kode audit mencoba memperbaiki logika transisi, perubahan parsial pada satu flag tanpa memperbarui flag lain secara atomik menghasilkan state tidak konsisten — misalnya `Status = "Approved"` tapi `SrSpvApprovalStatus = "Pending"`, atau `HCApprovalStatus = "Reviewed"` padahal `Status` masih "Submitted".

**Why it happens:**
Developer memodifikasi satu action approval tanpa memetakan seluruh state machine. Ada 4 flag independen plus `RejectionReason`, mudah melewatkan sinkronisasi. Override admin (`OverrideSaveRequest`) menulis `Status` dan `HCApprovalStatus` secara bersamaan tanpa validasi konsistensi — ini adalah tech debt yang sudah diidentifikasi di v4.0.

**How to avoid:**
Buat `ApprovalStateMachine` helper kecil yang menerima seluruh state lama dan event baru, lalu mengembalikan seluruh state baru yang valid. Setiap action approval hanya memanggil helper ini — tidak langsung mengubah field individual. Tulis unit test untuk setiap kombinasi transisi legal.

**Warning signs:**
- Query dashboard menggunakan `WHERE Status = 'Approved'` tapi hasilnya tidak cocok dengan detail worker
- Progress record dengan `ApprovedAt != null` tapi `SrSpvApprovalStatus = "Pending"`
- Override admin menghasilkan `Status = "Approved"` tapi `HCApprovalStatus = "Pending"`

**Phase to address:**
Audit Execution Flow (Evidence submission + Approval chain)

---

### Pitfall 2: Cascade Deactivation Race Condition — Coach-Coachee Mapping dan Assignment

**What goes wrong:**
Saat mapping coach-coachee dinonaktifkan, kode harus juga menonaktifkan `ProtonTrackAssignment` terkait dan menandai `DeactivatedAt`. Jika proses ini tidak atomik dalam satu transaksi DB, partial deactivation bisa terjadi: mapping nonaktif tapi assignment masih aktif, sehingga coachee masih bisa submit evidence tanpa coach yang valid.

**Why it happens:**
v4.0 audit memperbaiki "mapping reactivation cascade", artinya cascading logic sudah ada tapi mungkin masih ada edge case — terutama ketika satu coachee memiliki lebih dari satu mapping aktif (multi-unit pattern), atau ketika admin mengimpor ulang mapping via Excel setelah deactivation dan bypass cascade logic.

**How to avoid:**
Bungkus seluruh deactivation logic dalam `using var transaction = await _context.Database.BeginTransactionAsync()`. Verifikasi bahwa `DeactivatedAt` di `ProtonTrackAssignment` di-set bersamaan dengan mapping dinonaktifkan. Cek kasus import bulk via Excel yang mungkin melewati cascade logic.

**Warning signs:**
- `ProtonTrackAssignment.IsActive = true` tapi tidak ada `CoachCoacheeMapping` aktif yang sesuai untuk coachee tersebut
- `ProtonTrackAssignment.DeactivatedAt` null padahal `IsActive = false`
- Coachee bisa mengakses halaman submit evidence setelah coach-nya di-unmap

**Phase to address:**
Audit Setup Flow (Coach-Coachee Mapping + Track Assignment)

---

### Pitfall 3: Sequential Lock Bypass via Direct URL atau Manipulasi POST

**What goes wrong:**
Sequential lock deliverable mencegah coachee mengerjakan deliverable N+1 sebelum deliverable N selesai. Namun jika validasi lock hanya ada di View (conditional render tombol Submit) dan tidak ada di controller action `SubmitEvidence`, maka coachee yang tahu URL endpoint bisa POST evidence untuk deliverable yang seharusnya terkunci.

**Why it happens:**
v4.0 audit menemukan "coachee URL manipulation" di CDP Dashboard — ini menunjukkan pola yang sama sangat mungkin ada di evidence submission. Developer sering mengandalkan UI lock (tombol disabled) tanpa guard di server.

**How to avoid:**
Setiap action `SubmitEvidence` harus memvalidasi di controller: apakah semua deliverable dengan `Urutan < deliverable.Urutan` dalam SubKompetensi yang sama sudah berstatus "Approved". Kembalikan `Forbid()` atau redirect dengan error jika belum terpenuhi.

**Warning signs:**
- Controller action `SubmitEvidence` tidak memiliki query untuk mengecek progress deliverable sebelumnya
- UI menampilkan lock icon tapi tidak ada `if (!isUnlocked) return Forbid()` di controller
- Test manual: ubah URL dari `/CoachingProton/SubmitEvidence/5` ke `/CoachingProton/SubmitEvidence/6` saat deliverable 5 masih Pending — jika berhasil, ada bug

**Phase to address:**
Audit Execution Flow (Sequential lock edge cases)

---

### Pitfall 4: File Path Traversal dan Missing Authorization pada Evidence Download

**What goes wrong:**
Evidence tersimpan di `/uploads/evidence/{id}/{filename}` dengan path disimpan sebagai string di `EvidencePath`. Jika action download tidak memvalidasi bahwa path yang diminta memang milik coachee yang sedang login (atau coach/supervisor yang berwenang), maka siapapun yang menebak URL bisa mengunduh bukti milik orang lain. Ini adalah tech debt yang sudah diidentifikasi di v4.0 ("Download auth concern").

**Why it happens:**
Pattern umum: `return PhysicalFile(path, contentType)` tanpa cek kepemilikan. Karena file disimpan dengan ID progress sebagai folder name, path bisa ditebak secara sekuensial.

**How to avoid:**
Action download wajib: (1) load `ProtonDeliverableProgress` dari DB menggunakan id parameter, (2) verifikasi user saat ini adalah CoacheeId, atau coach yang ter-mapping, atau SrSpv/SH/HC yang berwenang, (3) konstruksi path dari DB record — jangan terima path dari request parameter. Gunakan `Path.GetFullPath()` dan verifikasi hasilnya masih dalam direktori uploads yang diizinkan.

**Warning signs:**
- Action download menerima `filePath` sebagai query parameter dari request body/URL
- Tidak ada query ke DB sebelum `return File(...)` untuk memvalidasi kepemilikan
- Tidak ada role check sebelum serving file evidence

**Phase to address:**
Audit Execution Flow (Evidence file management + security)

---

### Pitfall 5: Silabus Delete Tanpa Peringatan Dampak ke Progress Aktif

**What goes wrong:**
Menghapus `ProtonKompetensi` atau `ProtonDeliverable` yang sudah ada `ProtonDeliverableProgress` aktif akan membuat progress record menjadi orphan (karena tidak ada FK constraint — pattern deliberate proyek ini). Data progress tidak hilang tapi tidak bisa ditampilkan dengan benar karena deliverable induknya sudah tidak ada. Ini adalah tech debt yang sudah diidentifikasi di v4.0 ("Silabus delete warning missing").

**Why it happens:**
Soft delete (`IsActive = false`) sudah diimplementasikan untuk `ProtonKompetensi`, tapi hard delete mungkin masih bisa dilakukan dari endpoint admin. Tanpa peringatan, admin HC bisa menghapus silabus yang sedang dipakai coachee aktif.

**How to avoid:**
Sebelum setiap delete operasi, query `ProtonDeliverableProgress` untuk menghitung berapa record aktif yang akan terdampak. Jika > 0, tampilkan modal konfirmasi dengan jumlah "X coachee sedang mengerjakan deliverable ini". Untuk kompetensi: gunakan soft delete saja, jangan hard delete jika ada progress aktif.

**Warning signs:**
- Action `DeleteKompetensi` atau `DeleteDeliverable` tidak memiliki query pre-delete untuk cek progress
- Tidak ada modal konfirmasi dengan data impact sebelum delete dilaksanakan
- Filter `IsActive = true` tidak konsisten — beberapa query filter, yang lain tidak

**Phase to address:**
Audit Setup Flow (Silabus CRUD)

---

### Pitfall 6: Dashboard N+1 Query — Role-Scoped Monitoring dengan Banyak Pekerja

**What goes wrong:**
Dashboard monitoring coaching membutuhkan data yang di-scope per role: SrSpv hanya melihat coachee di unitnya, SH melihat lebih luas, HC melihat semua. Jika scoping dilakukan dengan load-all-then-filter in memory, atau dengan N+1 pattern (loop per coachee untuk ambil progress), maka performa turun drastis saat jumlah pekerja bertambah.

**Why it happens:**
Role-scoping seringkali ditambahkan sebagai afterthought — pertama dibuat tanpa scoping, lalu ditambahkan filter di atas existing query. Hasilnya sering berupa `.ToList()` diikuti `.Where()` yang seharusnya jadi satu query terkompilasi ke SQL.

**How to avoid:**
Semua query monitoring harus dimulai dari `IQueryable<>` dengan WHERE clause yang di-build secara kondisional berdasarkan role, baru di-materialize dengan satu `ToListAsync()`. Untuk chart/stats, gunakan aggregation query langsung ke DB (`.GroupBy().Select(g => new { g.Key, Count = g.Count() })`) daripada load semua data ke memory lalu hitung di C#.

**Warning signs:**
- Method dashboard yang ada `.ToList()` di tengah-tengah sebelum filter role diterapkan
- Loop `foreach (var coachee in coachees) { var progress = _context.Progress.Where(...coachee.Id...) }` tanpa batching
- Halaman monitoring lambat (>3 detik) saat ada >50 coachee aktif

**Phase to address:**
Audit Monitoring dan Dashboard

---

### Pitfall 7: Legacy CoachingLog Coexistence — Data Duplikasi dan Query Ambiguitas

**What goes wrong:**
`CoachingLog` (model lama) dan model baru (`ProtonDeliverableProgress`, `DeliverableStatusHistory`) bisa mengandung data untuk user yang sama. Jika kode audit menambahkan fitur baru yang query dari salah satu sumber saja, atau jika HistoriProton menggabungkan kedua sumber dengan logika yang tidak konsisten, maka timeline history akan menampilkan duplikat atau missing entries.

**Why it happens:**
Legacy data dipertahankan karena sudah ada — migrasi full tidak dilakukan. Setiap kali ada fitur baru yang menyentuh history/timeline, developer harus ingat ada dua sumber data. Mudah lupa karena CoachingLog tidak terlihat di flow utama.

**How to avoid:**
Dokumentasikan secara eksplisit di komentar kode mana sumber data yang digunakan untuk setiap tampilan. `HistoriProton` harus memiliki komentar yang jelas tentang sumber legacy vs baru. Tambahkan test case dengan coachee yang memiliki data di kedua sumber.

**Warning signs:**
- Query di HistoriProton atau timeline yang tidak menyebut kedua tabel
- Export Excel HistoriProton yang jumlah barisnya tidak cocok dengan yang ditampilkan di UI
- Coachee lama (punya CoachingLog) memiliki tampilan history yang berbeda dengan coachee baru

**Phase to address:**
Audit Completion dan History (HistoriProton, Coaching Sessions)

---

### Pitfall 8: Final Assessment Double-Creation — ProtonFinalAssessment Duplicate Guard

**What goes wrong:**
`ProtonFinalAssessment` dibuat oleh HC setelah semua deliverable di-review. Jika tidak ada unique constraint atau guard di action CreateFinalAssessment, double-click atau dua HC yang bersamaan submit bisa membuat dua record assessment untuk satu `ProtonTrackAssignmentId`. v4.0 sudah memperbaiki "duplicate key crash on multiple assignments" — pattern duplikasi masih relevan di context yang berbeda.

**Why it happens:**
Tidak ada UNIQUE constraint di DB untuk `(CoacheeId, ProtonTrackAssignmentId)` di tabel `ProtonFinalAssessment`. Guard hanya ada di UI (tombol disabled setelah submit), bukan di server.

**How to avoid:**
Tambahkan check di action: `if (await _context.ProtonFinalAssessments.AnyAsync(f => f.ProtonTrackAssignmentId == request.TrackAssignmentId)) return ...`. Idealnya tambahkan UNIQUE index di migration untuk kolom `ProtonTrackAssignmentId`.

**Warning signs:**
- Tidak ada `.AnyAsync()` check sebelum `.Add()` di action CreateFinalAssessment
- Tidak ada unique index di tabel `ProtonFinalAssessments` untuk `ProtonTrackAssignmentId`
- HC bisa membuka form final assessment meskipun assessment untuk track assignment yang sama sudah ada

**Phase to address:**
Audit Completion (Final Assessment)

---

### Pitfall 9: 3-Year Journey Progression — Cross-Track Assignment Tanpa Validasi Tahun Sebelumnya

**What goes wrong:**
Proton coaching adalah perjalanan 3 tahun (Tahun 1 → 2 → 3). Jika assignment ke Tahun 2 bisa dilakukan sebelum Tahun 1 selesai (semua deliverable Approved dan final assessment ada), maka coachee bisa mengerjakan Tahun 2 sambil Tahun 1 masih ada yang belum beres. Ini menghasilkan progress data yang incoherent dan laporan yang misleading.

**Why it happens:**
Assignment dilakukan oleh HC/Admin secara manual. Tidak ada validasi otomatis bahwa tahun sebelumnya sudah complete sebelum assignment tahun berikutnya diizinkan. Interface assignment mungkin hanya menampilkan dropdown track tanpa konteks completion status.

**How to avoid:**
Di action `AssignTrack`, jika track yang akan di-assign adalah "Tahun 2" atau "Tahun 3", cek bahwa ada `ProtonFinalAssessment` dengan status "Completed" untuk track tahun sebelumnya milik coachee yang sama. Tampilkan warning yang jelas jika belum, dan minta konfirmasi eksplisit jika admin tetap ingin lanjutkan.

**Warning signs:**
- Action `AssignTrack` tidak memiliki query untuk cek existing final assessment sebelum membuat assignment baru
- Bisa assign coachee ke "Panelman - Tahun 2" tanpa ada completed assessment untuk "Panelman - Tahun 1"
- Dashboard menampilkan coachee dengan dua track aktif sekaligus untuk track type yang sama

**Phase to address:**
Audit Setup Flow (Track Assignment)

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| No FK constraint untuk CoacheeId/CoachId | Hindari cascade delete complexity | Orphan records tidak terdeteksi DB, harus divalidasi di aplikasi | Acceptable — sudah established pattern di proyek ini, jangan ubah |
| String status enum tanpa DB check constraint | Mudah extend tanpa migration | Nilai typo lolos tanpa error (`"Approvd"` vs `"Approved"`) | Hanya jika ada validasi di layer aplikasi untuk setiap write |
| Override admin set status bebas tanpa validasi konsistensi | Admin bisa fix data corrupt | Bisa menciptakan state baru yang corrupt (tech debt v4.0) | Never tanpa validasi state machine |
| `EvidencePath` disimpan sebagai relative string | Simple storage | Path invalid setelah restructuring folder, hard to validate | Acceptable jika ada download auth guard di controller |
| Dashboard query load-all lalu filter di memory | Cepat develop | Performa O(n) di memory, bukan di DB | Never untuk production dengan >50 concurrent users |
| Denormalized `CoacheeName` di `ProtonNotification` | Cepat display tanpa join | Stale jika user ganti nama | Acceptable — nama jarang berubah, tradeoff masuk akal |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Excel Import CoachCoacheeMapping | Import langsung update DB tanpa trigger cascade logic yang sama dengan UI deactivation | Import harus memanggil service method yang sama dengan deactivation, bukan bypass via direct DB write |
| Excel Import Silabus (upsert) | Update deliverable via import tidak memeriksa apakah ada progress aktif yang ter-link | Tambahkan warning di import result jika deliverable yang diupdate sudah punya progress aktif |
| Export HistoriProton | Query hanya dari satu sumber data (baru atau legacy CoachingLog) tanpa union | Pastikan export dan UI view menggunakan query yang identik dengan sumber yang sama |
| ProtonFinalAssessment + AssessmentSession CMP | Mencoba unify dua model terpisah karena keduanya menyimpan "assessment" | Jaga kedua model independen — FinalAssessment adalah Proton-specific, AssessmentSession adalah CMP-specific |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Load semua `ProtonDeliverableProgress` lalu filter by role in memory | Halaman monitoring lambat, memory spike | Build `IQueryable` dengan role-based WHERE sebelum `ToListAsync` | >100 progress records aktif |
| Include full navigation tree untuk dashboard stats | Timeout pada chart loading | Gunakan projection `.Select()` dan aggregation query di DB | >50 coachee aktif dengan >20 deliverable each |
| DeliverableStatusHistory tanpa pagination | Halaman history coachee lama sangat lambat | Tambahkan `.Take(50)` dengan tombol "Load more" | Coachee >1 tahun dengan banyak rejection/resubmit cycle |
| N+1 pada approval list — loop per deliverable untuk get approver name | Approval list page lambat | Gunakan `Include()` atau projection dengan join di satu query | >20 deliverable per coachee per halaman |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Download evidence tanpa verifikasi kepemilikan (tech debt v4.0) | Coachee A bisa unduh evidence milik Coachee B dengan menebak URL | Selalu load record dari DB dan validasi `CoacheeId == currentUserId` atau role yang berwenang sebelum serve file |
| Override admin tidak di-audit log | Admin bisa ubah status tanpa trace investigasi | Setiap override harus mencatat ke `AuditLog` dan `DeliverableStatusHistory` dengan actor dan alasan |
| ExportProgressExcel tanpa role attribute (tech debt v4.0) | Unauthenticated atau low-privilege access ke data progress seluruh pekerja | Tambahkan `[Authorize(Roles = "Admin,HC")]` pada action export |
| Path traversal via manipulasi EvidencePath | Akses file di luar direktori uploads | Validasi `Path.GetFullPath(filePath).StartsWith(uploadsDir)` sebelum serve file |
| Guidance file download tanpa auth | File guidance bisa diakses publik tanpa login | Pastikan endpoint download guidance ada di controller yang ber-`[Authorize]` |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Lock icon tanpa penjelasan mengapa terkunci | Coachee bingung, kontak HC untuk hal yang tidak perlu | Tooltip atau teks: "Selesaikan [nama deliverable sebelumnya] terlebih dahulu" |
| Rejection tanpa notifikasi ke coachee | Coachee tidak tahu harus resubmit, progress stagnan | Badge/notifikasi di navbar atau halaman CoachingProton saat ada rejection |
| Override admin berhasil tapi tidak ada feedback visual | Admin tidak tahu apakah override berhasil | Toast/flash message setelah override save berhasil |
| Dashboard chart tanpa filter tanggal | HC tidak bisa lihat trend, hanya snapshot saat ini | Filter periode (bulan/kuartal/tahun) pada chart analytics |
| HistoriProton timeline tanpa paginasi | Halaman sangat panjang untuk coachee lama | Tampilkan 20 entri terbaru, tombol "Tampilkan lebih banyak" |
| Final assessment form tidak tampilkan ringkasan deliverable | HC harus buka tab lain untuk verifikasi sebelum submit | Summary: "X dari Y deliverable Approved" sebelum form final assessment |

---

## "Looks Done But Isn't" Checklist

- [ ] **Sequential Lock:** Tombol submit terkunci di UI — verifikasi ada guard `if (!isUnlocked) return Forbid()` di controller action, bukan hanya conditional button render di View
- [ ] **Evidence Download Auth:** Tombol download ada — verifikasi controller query DB dulu sebelum serve file, bukan hanya serve path dari request parameter
- [ ] **Override Validation:** Form override bisa disimpan — verifikasi ada validasi konsistensi antara `Status` dan `SrSpvApprovalStatus`/`ShApprovalStatus` (tech debt v4.0)
- [ ] **Cascade Deactivation:** Mapping dinonaktifkan di UI — verifikasi `ProtonTrackAssignment.DeactivatedAt` ikut ter-set di DB dalam transaksi yang sama
- [ ] **Silabus Delete Guard:** Tombol delete ada di admin — verifikasi ada query pre-delete yang cek active progress, dan modal konfirmasi dengan impact count (tech debt v4.0)
- [ ] **Final Assessment Guard:** Form final assessment tersedia — verifikasi ada `AnyAsync` check untuk mencegah duplicate creation per `ProtonTrackAssignmentId`
- [ ] **ExportProgressExcel Auth:** Link export ada di halaman — verifikasi action memiliki role attribute `[Authorize(Roles = "Admin,HC")]` (tech debt v4.0)
- [ ] **3-Year Progression:** Tombol assign tahun berikutnya ada — verifikasi ada validasi completion tahun sebelumnya sebelum assignment diizinkan
- [ ] **CoachingLog Union:** HistoriProton menampilkan data — verifikasi query menggabungkan data legacy CoachingLog dan data baru DeliverableStatusHistory
- [ ] **Role-Scoped Dashboard:** Dashboard menampilkan data — verifikasi query scoping ada di `IQueryable` sebelum `ToListAsync`, bukan setelah materialize

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Approval state inconsistency sudah terjadi di production | MEDIUM | Tulis migration SQL satu kali untuk fix state berdasarkan business rules, backup dulu, jalankan off-peak, dokumentasikan |
| Orphan progress records akibat silabus dihapus | LOW | Script query untuk identify orphans, tampilkan sebagai "Deliverable tidak ditemukan" di UI, jangan hapus data progress |
| Evidence files rusak/hilang setelah storage restructuring | HIGH | Selalu migrate path di DB saat restructuring folder — tidak pernah rename/move folder uploads secara manual tanpa update DB |
| Duplicate ProtonFinalAssessment | LOW | Script identifikasi duplikat (keep latest, soft-delete yang lama), tambahkan unique index setelah cleanup |
| Legacy CoachingLog data conflict dengan data baru | MEDIUM | Tetapkan cutoff: data CoachingLog hanya dibaca untuk history, tidak di-edit. Dokumentasikan sebagai "read-only legacy boundary" |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Approval chain state inconsistency | Audit Execution Flow | SQL: `SELECT COUNT(*) FROM ProtonDeliverableProgress WHERE Status='Approved' AND SrSpvApprovalStatus='Pending'` harus 0 |
| Cascade deactivation race condition | Audit Setup Flow (Mapping) | Manual test: deactivate mapping → cek `DeactivatedAt` di assignment ter-set di DB |
| Sequential lock bypass | Audit Execution Flow | Postman/curl: POST ke SubmitEvidence untuk deliverable terkunci → harus return non-200 |
| Evidence download tanpa auth | Audit Execution Flow (File security) | Test akses URL download dengan session user yang tidak berwenang → harus redirect atau 403 |
| Silabus delete tanpa warning | Audit Setup Flow (Silabus CRUD) | Test delete deliverable yang punya progress aktif → harus tampilkan modal dengan jumlah terdampak |
| Dashboard N+1 performance | Audit Monitoring | EF SQL logging: hitung jumlah query per page load halaman monitoring, harus <10 query |
| Legacy CoachingLog ambiguity | Audit Completion dan History | Test dengan coachee yang punya CoachingLog lama: verifikasi HistoriProton tampilkan semua entri |
| Final assessment duplicate | Audit Completion | Double-submit test dalam 1 detik: hanya 1 record yang boleh masuk DB |
| 3-year progression tanpa validasi | Audit Setup Flow (Track Assignment) | Test assign Tahun 2 tanpa completed Tahun 1 → harus tampilkan warning atau validation error |
| Override tanpa konsistensi state | Audit Monitoring (Override) | Test override ke kombinasi state yang invalid → harus ditolak dengan pesan error yang jelas |

---

## Sources

- Inspeksi kode langsung: `Models/ProtonModels.cs` (14 entity — ProtonTrack, ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonTrackAssignment, ProtonDeliverableProgress, DeliverableStatusHistory, ProtonNotification, CoachingGuidanceFile, ProtonFinalAssessment)
- Inspeksi kode langsung: `Controllers/ProtonDataController.cs` (ProtonDataController class-level authorize Admin,HC)
- Tech debt registry v4.0: `.planning/PROJECT.md` — 5 item terbuka: ExportProgressExcel role attr, evidence storage concern, download auth concern, silabus delete warning missing, override status validation
- Audit findings v4.0: Coaching Proton audit — mapping reactivation cascade fix, SubmitInterviewResults ProtonFinalAssessment creation fix
- Audit findings v4.0: CDP Dashboard audit — coachee URL manipulation fix, duplicate key crash fix on multiple assignments
- Pattern memory (MEMORY.md): No FK constraint pattern, string-based status enum, multi-unit user membership pattern
- Referensi metodologi v8.1 (Renewal & Assessment Ecosystem Audit) — sebagai benchmark kedalaman audit yang sesuai

---
*Pitfalls research for: Proton Coaching Ecosystem Audit (PortalHC KPB v8.2)*
*Researched: 2026-03-22*
