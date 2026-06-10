# Phase 363 — Findings: Audit Fix Alur PROTON (T1-T10)

> Sumber: verifikasi adversarial alur PROTON end-to-end, 2026-06-10. Workflow 9 agent (6 verifier per fase klaim + 3 mapper subsystem) cek dokumen pemahaman vs kode aktual. Verdict global: alur 6 fase benar, 0 klaim fundamental salah — temuan di bawah adalah bug/inkonsistensi/loophole yang KE-MISS dari dokumentasi dan belum pernah jadi REQ phase manapun.
> Dokumen alur lengkap terverifikasi: `C:\Users\Administrator\.claude\plans\saya-mau-tanya-di-breezy-abelson.md` (v2).

## Tabel Ringkas

| # | Temuan | Tipe | Severity (perkiraan) | Lokasi |
|---|--------|------|---------------------|--------|
| T1 | `ApproveFromProgress` tidak cek allApproved → notif HC silent miss | Bug notif | HIGH | CDPController.cs:1939-2013 |
| T2 | `RejectFromProgress` tidak reset chain; HCApprovalStatus "Reviewed" survive rejection | Inkonsistensi | HIGH | CDPController.cs:2054-2071 |
| T3 | Reaktivasi assignment skip year gate total | Loophole gate | HIGH | CoachMappingController.cs:516-528 |
| T4 | Lulus exam saat assignment nonaktif → penanda tidak terbit, silent | Edge case | MED | ProtonCompletionService.cs:39-42 |
| T5 | Badge "Belum Mulai" HistoriProton unreachable | Dead code | LOW | CDPController.cs:3250; Views/CDP/HistoriProton.cshtml:79,152-155 |
| T6 | Asimetri ValidUntil: jalur pass normal tidak set, regrade Fail→Pass hardcode +3thn | Inkonsistensi | MED | GradingService.cs:505-510 vs 279-283 |
| T7 | Varian FromProgress tanpa race-guard D-10 | Drift | MED | CDPController.cs:1939+ vs 882-901 |
| T8 | `SubmitEvidenceWithCoaching` overwrite EvidencePath tanpa append EvidencePathHistory | Drift | MED | CDPController.cs:2284-2292 vs 1279-1287 |
| T9 | Track tanpa Urutan-1 → prev resolution gagal → diperlakukan Tahun 1 (gate lolos) | Edge case | LOW | AssessmentAdminController.cs:1344-1349; CoachMappingController.cs:506-509 |
| T10 | `BackfillProtonPenanda` tanpa year-gate check | By design? | LOW | AssessmentAdminController.cs:3904-3996 |

## Detail per Temuan

### T1 — Notif HC allApproved silent miss (HIGH)
- `ApproveDeliverable` (form Deliverable page) cek `allApproved` semua progress track in-memory (CDPController.cs:930-931) → kalau true panggil `CreateHCNotificationAsync` `COACH_ALL_COMPLETE` ke semua user HC (L966-969, L1111-1140).
- `ApproveFromProgress` (modal inline CoachingProton, L1939-2013) **tidak pernah cek allApproved dan tidak pernah kirim notif** → approve deliverable TERAKHIR lewat CoachingProton = HC tidak pernah tahu coachee selesai.
- CoachingProton justru surface approval utama L4 → kemungkinan jalur tersering.
- Fix arah: extract check allApproved + notif jadi helper, panggil dari kedua endpoint.
- Catatan: tabel `ProtonNotification` (ProtonModels.cs:170-185) DORMANT — notif aktual via `UserNotification`/`NotificationService.SendAsync`. Jangan bangunkan tabel dorman, ikut jalur existing.

### T2 — Reject chain divergen (HIGH)
- `RejectDeliverable` (L1022-1037): Status="Rejected" + reset SrSpv+SH+HC ke "Pending" + null semua approver ID/timestamp.
- `RejectFromProgress` (L2054-2071): hanya set role penolak "Rejected" + overall Status="Rejected" — approval co-signer & `HCApprovalStatus` TIDAK direset.
- Reset susulan saat resubmit (`UploadEvidence` wasRejected L1297-1308; `SubmitEvidenceWithCoaching` L2270-2279) hanya cover SrSpv+SH — **HCApprovalStatus "Reviewed" tidak pernah direset di jalur ini** → HC review menempel di evidence yang sudah ditolak+diganti.
- Fix arah: samakan perilaku — RejectFromProgress reset full chain seperti RejectDeliverable (atau keputusan eksplisit kenapa beda).

### T3 — Loophole year gate jalur reaktivasi (HIGH)
- `CoachCoacheeMappingAssign` cabang 1 (L516-528): coachee yang sudah punya `ProtonTrackAssignment` APAPUN untuk track yang diminta (query L517-520 **tanpa filter IsActive** — termasuk inaktif/deactivated) → **skip year gate sepenuhnya** (reaktivasi L593-603).
- Konsekuensi: assignment Tahun 2 lama yang dideaktivasi bisa direaktivasi tanpa cek Tahun 1 lulus — escape dari hard-block Phase 359 D-05.
- Bersinggungan Phase 360: hook exempt `isExemptFromCrossYear = false` hardcoded (L530-534) akan diisi bypass-origin/renewal. Keputusan T3 harus konsisten dengan desain 360 (kalau 360 jalan duluan, koordinasikan file yang sama).
- Pertanyaan desain: apakah reaktivasi memang dianggap "sudah pernah valid, boleh lanjut" (by-design) atau loophole? Kalau by-design → dokumentasikan + test; kalau loophole → gate juga jalur reaktivasi.

### T4 — Penanda silent-miss saat assignment nonaktif (MED)
- `EnsureAsync` filter assignment `IsActive` (ProtonCompletionService.cs:39); tidak ketemu → warn log + return false (L42). Lulus ujian valid → TANPA penanda, tanpa surface ke admin.
- Skenario riil: admin deactivate mapping (cascade deactivate assignment) saat exam masih berjalan → coachee submit & lulus → penanda hilang senyap → year gate tahun berikut menolak padahal lulus.
- Mitigasi existing: `BackfillProtonPenanda` bisa menambal (resolusi tanpa IsActive, A-M10) tapi manual + admin harus tahu.
- Fix arah (pilih saat planning): (a) longgarkan EnsureAsync pakai resolusi gaya A-M10, (b) surface warning ke admin (TempData/notif/audit), (c) by-design + dokumentasi.

### T5 — Dead branch "Belum Mulai" (LOW)
- `HistoriProton` hanya menampilkan coachee dengan ≥1 assignment (HIST-05, CDPController.cs:3179-3180); ternary status L3250 hanya hasilkan "Lulus"/"Dalam Proses".
- Badge "Belum Mulai" (HistoriProton.cshtml:152-155) + opsi filter (:79) tidak pernah aktif.
- Fix arah: hapus dead branch + filter option, ATAU ubah query supaya coachee ber-mapping tanpa assignment ikut tampil ber-badge "Belum Mulai" (keputusan produk).

### T6 — Asimetri ValidUntil (MED)
- Jalur pass normal (`GradeAndCompleteAsync` L263-297): set NomorSertifikat saja; ValidUntil dari setup sesi oleh HC (AssessmentAdminController.cs:1263 dst).
- `RegradeAfterEditAsync` Fail→Pass (L491-517): set NomorSertifikat + **ValidUntil = today+3 tahun hardcoded** (L505-510).
- Konsekuensi: dua sertifikat dari sesi sama bisa beda masa berlaku tergantung jalur penerbitan.
- Fix arah: regrade pakai ValidUntil sesi (kalau ada) atau jangan override; hardcode +3thn dibuang.

### T7 — FromProgress tanpa race-guard D-10 (MED)
- `ApproveDeliverable` punya reload-fresh race guard untuk approver konkuren (L882-901, D-10). `ApproveFromProgress` (L1939+) tidak.
- Dua L4 approve bersamaan via modal → potensi lost update / double history.
- Fix arah: port guard D-10 ke varian FromProgress.

### T8 — EvidencePathHistory drift (MED)
- `UploadEvidence` append path lama ke `EvidencePathHistory` JSON saat re-upload (L1279-1287).
- `SubmitEvidenceWithCoaching` overwrite `EvidencePath` TANPA append (L2284-2292) → jejak evidence lama hilang dari history (file fisik?? cek juga apakah file lama orphan).
- Fix arah: samakan — append history di SubmitEvidenceWithCoaching sebelum overwrite.

### T9 — Prev-track resolution Urutan-based (LOW)
- prevTahunKe di-resolve via `Urutan` dalam TrackType sama (AssessmentAdminController.cs:1344-1349; CoachMappingController.cs:506-509). Track tanpa Urutan-1 (data master rusak/kosong) → prev=null → diperlakukan Tahun 1 → gate lolos.
- Probabilitas rendah (master ProtonTrack 6 baris seeded — lihat SeedProtonTracksAsync 0814640b) tapi silent kalau data berubah.
- Fix arah: guard defensif (log/throw kalau Urutan tidak kontigu) ATAU terima sebagai by-design dengan catatan.

### T10 — BackfillProtonPenanda tanpa year-gate (LOW, kemungkinan by-design)
- Backfill enforce 100% deliverable Approved (D-08, L3943-3951) tapi tidak cek tahun sebelumnya lulus.
- Argumen by-design: backfill = tambal data historis pre-Phase 358 yang lulus exam beneran; year gate justru baru bermakna setelah penanda lengkap.
- Fix arah: kemungkinan besar dokumentasi by-design saja; konfirmasi saat planning.

## Konteks Pendukung (hasil verifikasi, relevan untuk planning)

- **4 jalur tulis penanda:** GradingService hook L302-309; FinalizeEssayGrading D-05a L3750-3759; AkhiriUjian/AkhiriSemuaUjian → GradeAndCompleteAsync; BackfillProtonPenanda direct-write (tanpa EnsureAsync).
- **Duplicate endpoint pairs** (Deliverable-page vs CoachingProton JSON): ApproveDeliverable/ApproveFromProgress, RejectDeliverable/RejectFromProgress, HCReviewDeliverable/HCReviewFromProgress, UploadEvidence/SubmitEvidenceWithCoaching. Drift = akar T1/T2/T7/T8 — pertimbangkan konsolidasi helper bersama daripada patch per-endpoint.
- **Approval semantik (keputusan v25.0 A-2, JANGAN diubah):** 1 approver L4 cukup (SrSpv ATAU SH), co-sign opsional, HC bukan approver deliverable. Fix T1/T2/T7 tidak boleh mengubah semantik ini.
- **Test suite:** 159/159 hijau (termasuk 3 [Fact] SeedProtonTracks). Tambah [Fact]/integration per fix sesuai pola phase 358/359.
- **File-overlap:** CDPController.cs (T1/T2/T5/T7/T8) + CoachMappingController.cs (T3) + GradingService.cs (T6) + ProtonCompletionService.cs (T4) + AssessmentAdminController.cs (T9/T10). CoachMappingController & GradingService juga disentuh Phase 360 → urutan eksekusi 363 vs 360 perlu diputuskan (363 sesudah 362; 360 belum diplan).
