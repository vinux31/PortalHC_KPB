# Proton Bypass Tahun — Design Spec (Diskusi B)

> **STATUS:** Design final 2026-06-09, siap review.
> **DEPENDS ON:** `2026-06-09-proton-completion-logic-design.md` (Diskusi A) — **wajib diimplementasi & diverifikasi DULU**, baru bangun bypass ini.
> Menggantikan `2026-06-09-proton-bypass-tahun-DRAFT-PAUSED.md` (superseded).

## 1. Konteks fitur

Perluasan fitur **Proton Override** (page admin di Kelola Data, `ProtonData/Override`, `ProtonDataController.cs:210`).

- **Sekarang:** override status deliverable Proton per-sel (`OverrideSave`, `ProtonDataController.cs:1400`).
- **Ditambah:** **Bypass Tahun** — admin/HC pindahin coachee antar tahun/track dengan alasan tertulis + audit. Contoh: Worker A Tahun 1 → Tahun 2.

Gate: `[Authorize(Admin,HC)]`, AntiForgery, audit log (ikut pola `OverrideSave`).

## 2. Domain (verified dari kode)

- `ProtonTrack` = TrackType (Panelman/Operator) × TahunKe (1/2/3). 6 track. Unit BUKAN di track — unit ada di `ProtonKompetensi`.
- `ProtonTrackAssignment` (`CoacheeId`, `ProtonTrackId`, `IsActive`, `AssignedAt`, `DeactivatedAt`) — assign coachee ke 1 track. Dibuat lewat `CoachMappingController` (sepaket CoachCoacheeMapping).
- `ProtonDeliverableProgress` — progress per deliverable (Pending/Submitted/Approved/Rejected), unit-filtered saat bootstrap.
- `ProtonFinalAssessment` (`CompetencyLevelGranted` 0-5 [dormant per A-3], `ProtonTrackAssignmentId`, `Origin` [Exam/Interview/Bypass — ditambah Diskusi A-M9]) — penanda "lulus tahun".
- `DeliverableStatusHistory` — jejak perubahan status deliverable (sudah ada, migration `20260307114502`).
- Progresi Tahun 1→2→3 = **manual re-assign** (gak ada tombol naik tahun). Bypass = bungkus resmi + audit + alasan.
- "Lulus tahun" (CDP:3204) = `allDeliverableApproved && ProtonFinalAssessment ada`.

## 3. Keputusan terkunci

### 3.1 Keputusan desain dasar (A–E)
- **Fork1 = B**: saat skip deliverable, **force semua deliverable jadi "Approved"** + tulis `DeliverableStatusHistory` (`StatusType="Bypassed-AutoApprove"` [nilai string baru, tanpa schema change], `ActorId/Name/Role`=HC, `Timestamp`) sebagai jejak jujur. Alasan bypass disimpan di AuditLog + `PendingProtonBypass.Reason` (field `RejectionReason` di history hanya untuk rejection, tidak dipakai).
- **Fork3 = redesign** page Override jadi 2 tab: Tab1 "Override Deliverable" (existing, **tidak diubah**), Tab2 "Bypass Tahun" (baru).
- **D-A = setuju**: closure mode **CL-C "Tinggalkan"** (deactivate tanpa nilai) buat demosi/koreksi.
- **D-B = max 1 langkah**: |Δtahun| ≤ 1. Lompat 1→3 harus 2× bypass.
- **D-C = blok pindah**: CL-B(b) — worker baru pindah SETELAH assessment di-skor/lulus.
- **D-D = blok**: kalau final assessment tahun asal sudah ada, CL-B ditolak → arahkan CL-A.
- **D-E = create baru**: bypass ke tahun yang worker pernah jalani → selalu bikin assignment baru fresh; row lama jadi history mati.

### 3.2 Keputusan baru (sesi 2026-06-09, lanjutan setelah Diskusi A final)
- **OQ-1 Trigger CL-B(b) = Opsi B (HC-konfirmasi).** Exam source-year lulus → status pending jadi "siap" + notif HC. Worker **TIDAK** auto-pindah; HC klik `[Konfirmasi Pindah]` baru pindah. (Bukan auto-fire — pindah tahun = aksi resmi, HC lihat hasil dulu; juga hindari naro eksekusi pindah di hot-path grading.)
- **OQ-3/4 Penyimpanan = tabel baru `PendingProtonBypass`.** Simpan rencana lengkap (lihat §6). Sumber data panel "Menunggu Konfirmasi" + pemicu notif + 1-klik konfirmasi.
- **Penempatan UI = inline Tab2 (R1).** Pending konfirmasi = state di dalam Tab2, BUKAN page terpisah. Notif deep-link ke `/ProtonData/Override?tab=bypass&pending={id}`.
- **Notif tujuan = HC/admin inisiator** (pemilik pending), bukan worker. Worker tetap dapat `ASMT_RESULTS_READY` biasa.
- **Batal PENDING (Jenis 1) = in scope** (§8.1).
- **Undo bypass EXECUTED (Jenis 2) = C — TIDAK ada tombol undo** (§8.2). Koreksi via bypass lagi.

## 4. Closure mode (4 mode, HC pilih per bypass)

| Mode | Kapan | Syarat | Hasil | Sifat |
|------|-------|--------|-------|-------|
| CL-A Lulus (sudah ada) | tahun asal komplit normal | allApproved + final ada | langsung pindah | **INSTAN** |
| CL-B(a) Input manual | offline assessment sudah jalan, tinggal catat | final belum ada (D-D) | terbit penanda (tanpa level, A-3) + force-approve → pindah | **INSTAN** |
| CL-B(b) Buat assessment baru | butuh record exam + sertifikat | final belum ada | buat AssessmentSession tahun-asal → **tunggu lulus (D-C)** → notif → HC konfirmasi → pindah | **TUNGGU** |
| CL-C Tinggalkan | demosi/koreksi/ganti track | — | deactivate tanpa nilai → pindah | **INSTAN** |

Pola: NAIK → CL-A/CL-B. TURUN/koreksi → CL-C. Tipe assessment CL-B(b) **ikut tahun** (Tahun 1/2 = exam online worker; Tahun 3 = interview HC).

## 5. Pohon keputusan 1 operasi

```
BYPASS(worker, sourceYear S, targetTrack T, mode, alasan*):
VALIDASI (blok kalau gagal):
  - alasan wajib
  - T ≠ track aktif S (E14 no-op)
  - |tahun(T) − tahun(S)| ≤ 1 (D-B)
  - worker punya tepat 1 assignment aktif (E8)
  - mode CL-B → final S BELUM ada (D-D)
  - mode CL-A → S komplit (allApproved+final), else tolak

EKSEKUSI:
  CL-A / CL-B(a) / CL-C  → eksekusi LANGSUNG (1 transaksi, §5.1)
  CL-B(b)                → buat catatan-tunggu (1 transaksi, §5.2), pindah ditunda

  §5.1 PINDAH-INSTAN (CL-A / CL-B(a) / CL-C), 1 transaksi:
    [tutup tahun asal]
      CL-A    : nilai S sudah ada
      CL-B(a) : force-approve deliverable + history("Bypassed-AutoApprove") + EnsureProtonFinalAssessment(Origin="Bypass")
      CL-C    : tanpa nilai
    [cancel exam aktif S] (E5)
    [deactivate assignment S] IsActive=false, DeactivatedAt=now
    [aktifkan target T] create ProtonTrackAssignment baru (D-E), AssignedById=HC
    [bootstrap deliverable T] unit-filtered pakai Unit dari FORM (E7, M-6)
    [coach (M-5): deactivate CoachCoacheeMapping aktif lama DULU → create baru]
                 (constraint filtered-unique 1-aktif/coachee, E15)
    [audit log]

  §5.2 BUAT CATATAN-TUNGGU (CL-B(b)), 1 transaksi:
    [force-approve deliverable S + history("Bypassed-AutoApprove")]
    [buat AssessmentSession(TahunKe=S, tipe ikut tahun)]
    [insert PendingProtonBypass(status="Menunggu", linked AssessmentSessionId)]
    [audit log] → STOP. Worker tetap di tahun asal.

  §5.3 KONFIRMASI PENDING (dipicu HC setelah exam lulus, §7):
    eksekusi PINDAH-INSTAN (§5.1, tanpa re-force-approve & tanpa re-create final —
    penanda sudah terbit Origin="Exam" oleh GradingService) → set pending status="Selesai"
```

## 6. `PendingProtonBypass` — tabel baru (migration)

Kolom:

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| `Id` | int PK | |
| `CoacheeId` | string (FK User) | worker |
| `SourceProtonTrackId` | int | track asal (yang ditutup) |
| `TargetProtonTrackId` | int | track tujuan |
| `TargetUnit` | string | unit tujuan (dari form, E7) |
| `TargetCoachId` | string? (FK User) | coach tujuan (M-5; null = pertahankan) |
| `Reason` | string | alasan wajib |
| `LinkedAssessmentSessionId` | int (FK) | exam source-year yang dipantau |
| `Status` | string | "Menunggu" / "Siap" / "Selesai" / "Dibatalkan" |
| `InitiatedById` | string (FK User) | HC/admin inisiator (tujuan notif) |
| `CreatedAt` | DateTime | |
| `ResolvedAt` | DateTime? | saat Selesai/Dibatalkan |

**Lifecycle:** `Menunggu` → (exam lulus) `Siap` → (HC konfirmasi) `Selesai` | (HC batal) `Dibatalkan`.

⚠️ **MIGRATION** — tabel baru. Notify IT dgn flag migration. Ini migration ke-2 setelah `Origin` (Diskusi A); urutan: A dulu, lalu B.

## 7. Pemicu notif (CL-B(b) lulus)

Di `GradingService.GradeAndCompleteAsync` (`GradingService.cs:49`) (dan `RegradeAfterEditAsync` :420), **setelah** `EnsureProtonFinalAssessment` (Diskusi A-4) untuk session yang lulus:
- Cek apakah ada `PendingProtonBypass` dengan `LinkedAssessmentSessionId == session.Id` & `Status=="Menunggu"`.
- Jika ada → set `Status="Siap"` + kirim notif tipe baru **`PROTON_BYPASS_READY`** ke `InitiatedById`.
  - Template di `NotificationService._templates`: Title "Bypass Siap Diselesaikan", `ActionUrlTemplate = "/ProtonData/Override?tab=bypass&pending={PendingId}"`.

Coupling ringan: GradingService cuma **flip flag + kirim notif**, BUKAN eksekusi pindah (pindah tetap di tangan HC via konfirmasi). Guard sama seperti A-M11 (`Category=="Assessment Proton"` && `IsPassed` && `ProtonTrackId.HasValue`).

> **Re-grade Pass→Fail** (exam source-year yang sudah "Siap" di-regrade jadi gagal): set pending balik ke `"Menunggu"` (penanda Origin="Exam" juga dihapus per A-M1). Worker belum pindah (Opsi B) → aman, gak ada rollback assignment.

## 8. Batal & Undo

### 8.1 Batal PENDING (sebelum pindah) — IN SCOPE
Tombol `[Batal]` di panel "Menunggu Konfirmasi" (Tab2). 1 transaksi:
- Set `PendingProtonBypass.Status="Dibatalkan"`, `ResolvedAt=now`.
- **Batalin `LinkedAssessmentSession` otomatis** (HC tidak perlu ke Manage Assessment):
  - worker **belum** kerjakan exam → cancel/hapus session.
  - worker **sudah lulus** (status="Siap") → **pertahankan hasil exam** (worker beneran lulus + penanda Origin="Exam" sah); hanya batalkan rencana pindah.
- Force-approve deliverable source **tetap** (jejak history). HC rapikan via Override Tab1 bila perlu.
- Audit log.

### 8.2 Undo bypass EXECUTED (sesudah pindah) — TIDAK ADA TOMBOL (Opsi C)
Tidak ada fitur undo khusus. Koreksi bypass yang salah = **jalankan bypass LAGI** ke track yang benar:
- Bypass mendeactivate assignment salah otomatis + bikin yang benar.
- Assignment salah → inactive/arsip (E10); deliverable force-approve nyangkut jadi history mati (harmless).
- Koreksi **wajib lewat bypass** (exempt gate antar-tahun A-M4), BUKAN assign biasa (keblok gate A-5.1).
- Catatan: kalau undo-executed mau dibangun nanti, butuh kolom baru (mis. `PreviousStatus` di `DeliverableStatusHistory`) untuk restore status deliverable presisi — **bukan scope v1**.

## 9. Redesign page (Fork3)

2 tab. **Tab1 "Override Deliverable"** = existing, tidak diubah. **Tab2 "Bypass Tahun"** (baru):
- Panel **"Menunggu Konfirmasi (N)"** di atas (kalau ada) — tiap row: nama, S→T, hasil exam, `[Lihat & Konfirmasi]` + `[Batal]`.
- Filter Bagian/Unit/Track → tabel worker (nama, track aktif, progress X/Y, final ✓/✗, `[Bypass]`).
- Klik `[Bypass]` → wizard 3 langkah: Tujuan → Closure mode (auto enable/disable per state) → Detail (unit, coach dropdown, alasan) + konfirmasi.
- Klik `[Lihat & Konfirmasi]` → modal hasil exam + `[Konfirmasi Pindah]`/`[Batal]`.

## 10. Endpoint baru (pola OverrideSave)

- `GET BypassList` (bagian, unit, trackId) → tabel worker.
- `GET BypassPendingList` → daftar pending status Menunggu/Siap (panel atas).
- `GET BypassDetail` (coacheeId) → state worker untuk wizard (mode mana yang boleh).
- `POST BypassSave` → eksekusi (CL-A/B(a)/C instan; CL-B(b) buat pending).
- `POST BypassConfirm` (pendingId) → §5.3 pindah.
- `POST BypassCancelPending` (pendingId) → §8.1.

Semua `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` + audit log.

## 11. Edge case & resolusi

- E1/D-E: target pernah dijalani → create baru fresh.
- E2: turun, T-asal punya final "Lulus" → final lama disimpan history; status aktif = tahun baru.
- E5: exam in-progress saat bypass → auto-cancel (kecuali assessment CL-B(b) yang baru dibuat).
- E7: assignment tanpa mapping → bootstrap pakai Unit dari form.
- E8: dobel assignment aktif → bypass selalu deactivate asal → dijamin 1 aktif.
- E10: evidence file orphan saat ganti unit → KEEP (assignment inactive = arsip).
- E14: T == track aktif S → no-op, tolak.
- F1 (CL-B(b) lupa skor): gak terjadi — D-C blok pindah sampai lulus.
- F2 duplicate final: blok via D-D.
- MISS-2 (exam gagal di CL-B(b)): pending tetap "Menunggu", worker tetap di tahun asal, bisa retry exam, atau HC batal pending (§8.1).
- **E15 (constraint DB — verified):** `CoachCoacheeMapping` punya filtered-unique `CoacheeId WHERE IsActive=1` (`ApplicationDbContext.cs:326`) → bypass WAJIB deactivate mapping aktif lama SEBELUM create baru (§5.1). `ProtonTrackAssignment` TIDAK punya unique (Coachee,Track) → D-E create-baru aman; "1 aktif" (E8) dijaga **kode**, bukan DB. `ProtonDeliverableProgress` unique (AssignmentId, DeliverableId) → aman krn assignment baru.
- **Field nyata (verified):** `ProtonFinalAssessment` = unique `ProtonTrackAssignmentId`, ada `Notes`/`CreatedById`/`CompetencyLevelGranted[0-5]`, BELUM ada `Origin` (migration A). `AssessmentSession` = `Category`/`DurationMinutes`/`ProtonTrackId`(int?)/`TahunKe`(string?)/`NomorSertifikat`. `ProtonTrackAssignment` = `CoacheeId`/`AssignedById`/`ProtonTrackId`/`IsActive`/`AssignedAt`/`DeactivatedAt`.

## 12. Dependency → Diskusi A

Bypass numpang fondasi Diskusi A (`2026-06-09-proton-completion-logic-design.md`):
- Helper `EnsureProtonFinalAssessment` (A-4) dipakai CL-B(a) & CL-B(b)-confirm (`Origin="Bypass"`/`"Exam"`).
- `Origin` marker (A-M9) — bypass set/cek Origin.
- Bypass-assignment **exempt** gate antar-tahun (A-M4).
- Level dimatikan (A-3) → form CL-B(a) **tanpa input level**.

**Urutan kerja: implement + verify Diskusi A DULU, baru bangun bypass ini.**

## 13. Out of scope
- **Audit/improve Tab1 (Override Deliverable)** — ditunda (backlog). Catatan sinkron yang ditemukan: (1) Tab1 belum tulis `DeliverableStatusHistory` (cuma AuditLog), (2) belum warning saat un-approve worker yang sudah punya penanda Lulus, (3) belum rekam `RejectedById`. Audit lebih akurat setelah Diskusi A jalan.
- Undo bypass executed (§8.2 = C, tanpa tombol).
- Menghidupkan kembali level kompetensi (dibuang, A-3).
- Konfigurasi gate via UI.
