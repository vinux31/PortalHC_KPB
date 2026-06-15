# Audit Kode Sistem Assessment — Temuan (Code-First Pass)

**Tanggal:** 2026-06-14  
**Metode:** multi-agent workflow (21 area audit paralel -> verifikasi adversarial per-finding -> sintesis). 99 agent, ~6.6M token.  
**Sumber scope:** `.planning/notes/2026-06-13-audit-full-sistem-assessment.md` (7-item + 18 area gap-check).  
**Sifat:** code-first correctness. UI/visual pass = TERPISAH (belum dijalankan).  
**Verdict sistem:** `MATERIAL-BUGS`

**Ringkasan temuan (confirmed 72, refuted 5):** HIGH=13, MED=28, LOW=30, NONE=

---

## 1. Ringkasan Eksekutif

Secara jujur: sistem assessment ini TIDAK dapat dipercaya untuk menghasilkan skor, kelulusan, dan sertifikat yang benar dalam beberapa skenario yang umum terjadi, sehingga verdict keseluruhan adalah MATERIAL-BUGS yang mendekati kritis. Cacat paling parah adalah bug regrade essay (GRD-01 / PASS-01 / EDT-01): satu kali admin mengedit jawaban pilihan ganda pada assessment yang berisi soal essay akan diam-diam menghapus seluruh nilai essay yang sudah dinilai manual (denominator tetap memasukkan poin essay, tetapi numerator tidak), sehingga Score anjlok, IsPassed bisa berbalik Lulusâ†’Tidak Lulus, sertifikat (NomorSertifikat + ValidUntil) dicabut, dan untuk Proton T1/T2 penanda kelulusan ikut dihapus â€” semuanya dari edit yang sama sekali tidak menyentuh essay. Lebih buruk lagi, endpoint perbaikan RecomputeEssayScores hanya menyembuhkan baris ber-Score null/0 sehingga skor yang sudah rusak tapi bernilai non-nol tidak akan pernah pulih otomatis. Integritas ujian berwaktu juga bobol secara fundamental: penegakan timer sisi-server adalah dead code untuk tipe ujian paling umum karena create menetapkan AssessmentType="Standard" sementara guard hanya cocok dengan "Online" yang tidak pernah di-assign (TMR-01 / CAT-01), jadi peserta bisa submit jauh setelah waktu habis tanpa penolakan apa pun.

Di atas dua cacat fondasi itu, terdapat lapisan masalah serius lain: lubang otorisasi (CreateQuestion kehilangan [HttpPost]/[Authorize]/[ValidateAntiForgeryToken] sehingga user non-admin bisa menyuntik soal â€” QTY-01; AddExtraTime tanpa Roles sehingga peserta bisa memperpanjang waktunya sendiri â€” RST-01); pelanggaran invariant impersonasi read-only di mana sekadar admin membuka/impersonate ujian peserta justru MEMULAI ujian itu (menulis StartedAt/InProgress dan mengunci shuffle on-GET â€” OPS-01 / TOK-03); race & TOCTOU pada lifecycle (sesi Abandoned/Cancelled bisa dibangkitkan menjadi Completed bersertifikat â€” STAT-01; AbandonExam blind UPDATE menimpa sesi yang baru saja selesai â€” STAT-02); bug data integrity Proton (membalik hasil interview Tahun 3 Lulusâ†’Tidak Lulus meninggalkan penanda Lulus basi sehingga gate tetap terbuka â€” T3-01; EditAssessment menambah peserta Proton tanpa gate eligibility & ProtonTrackId/TahunKe null â€” PEL-01); serta kehilangan ValidUntil pada siklus regrade Passâ†’Failâ†’Pass yang membuat sertifikat re-issue tanpa tanggal kedaluwarsa dan hilang dari pelacakan renewal (EDT-02 / CERT-02). Matematika skor inti yang tidak melibatkan essay memang terbukti bersih dan konsisten (PASS-02), namun banyak permukaan review/laporan menampilkan hasil yang kontradiktif dengan skor tersimpan (essay selalu "Salah", X/Y benar tak terbobot, gain per-ElemenTeknis tidak dihitung ulang). Kesimpulan: jalur happy-path pilihan-ganda murni umumnya benar, tetapi begitu ada essay, edit pasca-selesai, impersonasi, Proton, atau ujian berwaktu "Standard", sistem dapat secara diam-diam menghasilkan skor/kelulusan/sertifikat yang salah.

## 2. Risiko Teratas (urut paling parah)

1. Bug regrade essay (GRD-01/PASS-01/EDT-01): edit satu jawaban MC pada assessment ber-essay menghapus seluruh nilai essay -> Score rusak, Pass->Fail, sertifikat & penanda Proton dicabut, dan tak bisa pulih otomatis (RecomputeEssayScores hanya perbaiki Score null/0). Korupsi data + pencabutan kredensial yang salah.
2. Penegakan timer sisi-server adalah dead code untuk ujian 'Standard' (TMR-01/CAT-01): hampir semua ujian online dapat di-submit jauh setelah waktu habis tanpa penolakan & tanpa jejak audit -> jendela kecurangan tak terbatas, integritas ujian berwaktu bobol.
3. Lubang otorisasi CreateQuestion (QTY-01): atribut [Authorize]/[HttpPost]/[ValidateAntiForgeryToken] yatim -> user non-admin terautentikasi bisa menyuntik/mengubah soal di paket mana pun, CSRF-able, side-effect on GET. Korupsi konten ujian & skor.
4. Impersonasi memulai ujian peserta (OPS-01/TOK-03): admin yang sekadar melihat/impersonate ujian non-token justru menulis StartedAt/InProgress & mengunci shuffle on-GET -> waktu peserta terbakar diam-diam, melanggar invariant read-only.
5. Sesi Abandoned/Cancelled dibangkitkan jadi Completed bersertifikat (STAT-01): guard grading tak meng-exclude Abandoned/Cancelled & SubmitExam POST tak terlindungi -> percobaan yang sengaja dibatalkan menjadi kelulusan sah bersertifikat (lubang authz/integritas).
6. AddExtraTime tanpa otorisasi role (RST-01) + tanpa batas total (RST-04): peserta dapat memperpanjang waktu ujiannya sendiri (atau peserta lain) tanpa batas -> integritas ujian berwaktu hancur.
7. Membalik hasil interview Proton Tahun 3 Lulus->Tidak Lulus meninggalkan penanda Lulus basi (T3-01): coachee tetap 'Completed' & year-gate tetap terbuka -> kelulusan/eligibility palsu, butuh pembersihan DB manual.
8. EditAssessment menambah peserta Proton tanpa gate eligibility & ProtonTrackId/TahunKe null (PEL-01): peserta yang belum lulus tahun sebelumnya / <100% deliverable tetap dapat sesi ujian, dan sesi malformed mengganggu logika Proton hilir.
9. Paket kosong + shuffle ON menzerokan seluruh batch (SHF-01): worker menerima 0 soal & auto-grade 0% fail untuk seluruh batch saat ada paket yang belum diisi soal â€” keadaan authoring intermediate yang umum.
10. AbandonExam blind UPDATE TOCTOU (STAT-02) tanpa RowVersion: sesi yang baru saja Completed/graded dapat tertimpa kembali ke Abandoned -> hasil ujian & sertifikat hilang dari layar Records meski baris skor tetap ada.
11. Kehilangan ValidUntil pada regrade Pass->Fail->Pass (EDT-02/CERT-02): sertifikat re-issue tanpa tanggal kedaluwarsa, mis-klasifikasi Expired, lenyap dari workflow renewal/expiry -> celah kepatuhan sertifikasi berbatas waktu.
12. Entri manual IsPassed default Lulus & terlepas dari Score/PassPercentage (MAN-02): admin dapat merekam skor di bawah ambang sebagai lulus + menerbitkan sertifikat hanya karena toggle default ON -> inflasi statistik kompetensi & sertifikat untuk nilai gagal.
13. Race upsert SaveAnswer/SaveTextAnswer non-atomik tanpa unique constraint (SAVE-01/SAVE-03): baris jawaban duplikat dapat terbentuk -> grading memilih opsi salah/stale (skor salah) atau memblokir finalize essay secara permanen.
14. Essay kosong dinilai 0 tanpa recourse (ESS-01): peserta yang melewati essay mendapat 0 keras yang dapat membalik IsPassed, sementara HC tidak bisa memasukkan nilai untuk baris yang tidak ada -> hasil assessment salah & tak terkoreksi.
15. Drift tampilan review vs skor tersimpan (RES-01/RES-02/GAIN-01): essay yang lulus tampil 'Salah', hitungan benar tak terbobot kontradiksi dengan Score%, gain per-ElemenTeknis understated -> memicu sengketa nilai meski skor tersimpan benar.

## 3. Tema Lintas-Temuan

- **Bug regrade essay (akar tunggal lintas-lensa): nilai essay terhapus saat recompute** — [GRD-01, PASS-01, EDT-01, GAIN-01]  
  Satu root cause di GradingService.ComputeScoreAndETInternalAsync (Essay branch kosong di numerator tapi maxScore tetap menambah ScoreValue essay). Muncul di 3 lensa audit (GRD/PASS/EDT) + efek turunannya pada breakdown per-ElemenTeknis (GAIN-01). Ini cacat HIGH paling sering muncul dan paling merusak: Score rusak, Pass->Fail, sertifikat & penanda Proton dicabut. RecomputeEssayScores TIDAK menyembuhkan skor rusak non-nol.
- **Penegakan timer sisi-server mati untuk tipe ujian paling umum** — [TMR-01, CAT-01, TMR-02, TMR-03, TMR-04, SAVE-05]  
  AssessmentType='Standard' tidak pernah cocok dengan guard 'Online' (TMR-01=CAT-01, duplikat lintas-lensa), sehingga submit telat diterima tanpa batas. Diperparah trust flag client isAutoSubmit (TMR-02), one-shot token konsumsi sebelum grading (TMR-03), dan drift formula Clamp/ExtraTime (TMR-04/SAVE-05).
- **Lubang otorisasi & atribut HTTP yang hilang/yatim** — [QTY-01, RST-01, RST-04, TOK-02, RST-02]  
  Atribut [HttpPost]/[Authorize(Roles)]/[ValidateAntiForgeryToken] terlepas ke helper privat (QTY-01) dan AddExtraTime tanpa Roles (RST-01) = peserta bisa menyuntik soal / memperpanjang waktunya sendiri tanpa batas (RST-04). Token gate hanya di UI lobby bisa dilewati pemilik (TOK-02). RST-02 = tombol Reset muncul tapi selalu gagal untuk status essay.
- **Pelanggaran invariant impersonasi read-only (write-on-GET)** — [OPS-01, TOK-03]  
  StartExam GET menulis StartedAt/Status=InProgress + assignment shuffle saat impersonasi tanpa cek IsImpersonating (OPS-01=TOK-03, duplikat). Admin yang sekadar melihat ujian peserta justru memulainya & membakar waktu peserta â€” melanggar invariant Phase 377.
- **Race / TOCTOU lifecycle sesi tanpa guard DB (RowVersion absen)** — [STAT-01, STAT-02, SAVE-01, SAVE-03, SAVE-04, OPS-03, BYP-01, MAN-03, CERT-05, GAIN-03]  
  Guard grading tidak meng-exclude Abandoned/Cancelled (sesi dibatalkan dibangkitkan jadi Completed bersertifikat â€” STAT-01); AbandonExam blind UPDATE menimpa sesi yang baru graded (STAT-02). Upsert SaveAnswer/SaveTextAnswer non-atomik bisa membuat baris duplikat (SAVE-01/03) yang memblokir finalize. Guard timer save inkonsisten MA vs MC/Essay (SAVE-04/OPS-03). Invariant 1-assignment-aktif & dedup manual hanya check-then-act tanpa unique constraint (BYP-01/MAN-03/CERT-05); ToDictionary crash pada duplikat (GAIN-03).
- **Bypass gate eligibility Proton & penanda kelulusan tak konsisten** — [T3-01, PEL-01, PEL-02, PEL-03, PEL-04, BYP-02, BYP-03]  
  Membalik interview Tahun 3 Lulus->Tidak Lulus meninggalkan penanda Lulus basi (gate tetap terbuka, status tetap Completed â€” T3-01). EditAssessment & jalur Pre/Post menambah sesi Proton tanpa gate dan dengan ProtonTrackId/TahunKe null (PEL-01/PEL-02). Gate create tidak verifikasi assignment aktif (PEL-03) & fail-open pada TahunKe kosong (PEL-04). Permukaan bypass publik & audit-log menyesatkan (BYP-02/BYP-03).
- **Kehilangan/inkonsistensi sertifikat & ValidUntil (kedaluwarsa)** — [EDT-02, CERT-02, CERT-01, CERT-03, CERT-04, CERT-05]  
  Regrade Pass->Fail->Pass tidak memulihkan ValidUntil (EDT-02=CERT-02, duplikat) -> sertifikat re-issue tanpa kedaluwarsa, lenyap dari renewal/expiry. Sertifikat lulus ValidUntil=null tampil Expired tapi tak dihitung & tak memicu notifikasi (CERT-01). Uniqueness NomorSertifikat tak lintas-tabel (CERT-03), check duplikat untrimmed (CERT-04), path-traversal defense-in-depth gap (CERT-04).
- **Cacat entri manual / backfill: kelulusan & data historis tak terverifikasi** — [MAN-01, MAN-02, MAN-03, MAN-04, MAN-05, MAN-06]  
  IsPassed entri manual default Lulus & terlepas dari Score/PassPercentage -> sertifikat untuk skor gagal (MAN-02). BulkBackfill: NIP non-unik crash 500 (MAN-01), Score tak-terparse jadi 0/gagal diam-diam (MAN-04), LinkedGroupId tanpa validasi (MAN-06), dedup lintas-jalur meleset (MAN-05).
- **Inkonsistensi tampilan review & laporan vs skor tersimpan (drift proyeksi)** — [RES-01, RES-02, RES-03, GAIN-01, GAIN-02, GAIN-03, TMR-04]  
  Essay yang sudah dinilai selalu tampil 'Salah' (RES-01); 'X/Y benar' tak terbobot kontradiksi dengan Score% (RES-02); MA nol-benar drift (RES-03); gain per-ElemenTeknis tak dihitung ulang pasca essay (GAIN-01) & gain dipaksa +100 menutupi regresi (GAIN-02). Skor tersimpan benar, tapi yang dilihat user salah -> memicu sengketa nilai.
- **Shuffle & integritas paket assessment** — [SHF-01, SHF-02, SHF-03]  
  Paket kosong saat shuffle ON membuat K=Min=0 -> seluruh batch dapat 0 soal & auto-grade 0% (SHF-01, HIGH). Reshuffle sesi Abandoned tinggalkan response orphan & drop SavedQuestionCount (SHF-02), penanganan Abandoned inkonsisten single vs bulk (SHF-03).
- **Cascade delete records: kehilangan data & kegagalan opak** — [REC-01, REC-02, REC-03, REC-04, REC-05, CERT-04]  
  Mirror training default-checked dihapus flat -> FK NoAction rollback seluruh delete (REC-01) atau heuristik judul+-1hari menghapus record sah (REC-02). Notif group dedup substring (REC-03=OPS-02), Pre/Post renewal-child lolos guard (REC-04), file sertifikat ditulis sebelum SaveChanges (REC-05).
- **Taksonomi kategori: cycle, cascade rename, & validasi duplikat** — [CAT-02, CAT-03, CAT-04, CAT-05, CAT-06]  
  EditCategory tanpa guard cycle/self-parent/depth (CAT-02); cek duplikat global vs unique index (ParentId,Name) menolak sub-kategori sah (CAT-03) & untrimmed -> 500 (CAT-04); rename/delete tak cascade ke string denormalisasi sesi/training (CAT-05/CAT-06).
- **Notifikasi group-complete: dedup substring & filter tipe** — [OPS-02, REC-03, OPS-05, ESS-03]  
  Dedup Message.Contains(Title) (OPS-02=REC-03 duplikat) menyebabkan notifikasi group lain tertekan (silent-miss). Pre/Post tergabung tanpa filter LinkedGroupId/AssessmentType (OPS-05). ESS-03 = respons AJAX finalize menampilkan NomorSertifikat basi/null.
- **Cacat robustness essay & blank-answer** — [ESS-01, ESS-04, OPS-03, OPS-04]  
  Essay kosong lolos completeness check & dinilai 0 tanpa bisa dinilai HC (ESS-01, MED). SubmitEssayScore tak cek tipe Essay (ESS-04); LogPageNav tanpa validasi ownership (OPS-04).
- **Drift skor MultipleAnswer & definisi MA inkonsisten antar-channel** — [GRD-02, GRD-03, RES-03, EDT-03]  
  Guard Count>0 absen di grading authoritative vs path display (GRD-02/RES-03); definisi MA valid berbeda import (>=1) vs create/edit (>=2) (GRD-03); MC multi-select tak diguard server-side -> HashSet.First() non-deterministik (EDT-03). Semua LOW, dataquality/legacy.
- **Konfirmasi area bersih (informational)** — [PASS-02]  
  Formula skor dan threshold >= konsisten di semua salinan (tidak ada drift), tipe field integer terkonfirmasi. Direkam agar PASS-01/EDT-01 sebagai satu-satunya cacat nyata di area ini tidak ambigu. Bukan defect.

## 4. Usulan Cluster Milestone v29.0 (Audit-Fix)

### P1 â€” Akar grading essay & integritas skor/sertifikat (BLOCKER, kerjakan pertama)
**Finding:** GRD-01, PASS-01, EDT-01, EDT-02, CERT-01, CERT-02, GAIN-01, ESS-01, ESS-03, RES-01, RES-02, RES-03, GRD-02  
Satu root cause grading essay menyebar ke skor, kelulusan, sertifikat (ValidUntil), penanda Proton, breakdown per-ElemenTeknis, dan tampilan review. Memperbaiki ComputeScoreAndETInternalAsync (menambah EssayScore ke numerator, atau exclude essay dari maxScore secara simetris) + memulihkan ValidUntil di branch Fail->Pass + recompute ET pasca-finalize + memproyeksikan EssayScore ke review = menyelesaikan kluster temuan HIGH dengan satu perbaikan terpusat. Wajib juga endpoint repair yang mampu menyembuhkan skor rusak non-nol (bukan hanya null/0). Idealnya tambah skrip backfill untuk memperbaiki data yang sudah terkorupsi.

### P2 â€” Integritas timer ujian & lifecycle race (HIGH keamanan ujian)
**Finding:** TMR-01, CAT-01, TMR-02, TMR-03, TMR-04, SAVE-04, SAVE-05, OPS-03, STAT-01, STAT-02, SAVE-01, SAVE-03  
Perbaiki guard timer agar mencakup AssessmentType='Standard'/Proton (mengaktifkan kembali penegakan Phase 313), hentikan trust flag isAutoSubmit client, seragamkan formula ExtraTime/Clamp, dan samakan guard save MA/MC/Essay. Bersamaan: tambah RowVersion/optimistic concurrency + status-guarded ExecuteUpdateAsync untuk AbandonExam & SubmitExam, exclude Abandoned/Cancelled di guard grading, dan jadikan upsert SaveAnswer/SaveTextAnswer atomik (kembalikan/ganti unique constraint). Satu kluster karena semuanya menyentuh jalur StartExam/SubmitExam/Hub & invariant lifecycle yang sama.

### P3 â€” Otorisasi, impersonasi read-only & token gate (HIGH keamanan/akses)
**Finding:** QTY-01, RST-01, RST-04, RST-02, TOK-01, TOK-02, TOK-03, OPS-01, OPS-04  
Tutup atribut yatim CreateQuestion, tambah Roles ke AddExtraTime + batas total, perbaiki tombol Reset essay, uppercase token di EditAssessment Pre/Post + cek token di SaveAnswer/SubmitExam, dan tambahkan guard IsImpersonating() pada write-on-GET StartExam (justStarted + assignment shuffle) serta validasi ownership LogPageNav. Semua adalah hardening permukaan kontrol akses/identitas yang dapat ditangani oleh tim yang sama secara berurutan.

### P4 â€” Integritas Proton: gate eligibility, penanda kelulusan & bypass
**Finding:** T3-01, PEL-01, PEL-02, PEL-03, PEL-04, BYP-01, BYP-02, BYP-03, SHF-01, SHF-02, SHF-03  
Tambahkan branch remove/replace penanda saat interview T3 dibalik (simetris dengan revoke cert exam), terapkan gate eligibility + isi ProtonTrackId/TahunKe pada jalur EditAssessment bulk-add & Pre/Post, verifikasi assignment aktif di gate create, dan tutup fail-open TahunKe kosong. Sertakan unique index untuk invariant 1-assignment-aktif (BYP-01) + hardening ExecuteInstantBypass/audit-log. Shuffle (filter paket kosong di branch ON, cleanup reshuffle Abandoned, seragamkan single vs bulk) dikelompokkan di sini karena keterkaitan erat dengan paket/sesi Proton.

### P5 â€” Entri manual/backfill, cascade delete & taksonomi kategori (MED data-governance)
**Finding:** MAN-01, MAN-02, MAN-03, MAN-04, MAN-05, MAN-06, REC-01, REC-02, REC-03, REC-04, REC-05, CAT-02, CAT-03, CAT-04, CAT-05, CAT-06, CERT-03, CERT-04, CERT-05, GAIN-02, GAIN-03, GRD-03, EDT-03, OPS-02, OPS-05  
Kluster kebersihan data & robustness administratif: validasi IsPassed manual terhadap Score/PassPercentage, dedup berbasis unique constraint, penanganan NIP/Score/LinkedGroupId backfill, cascade delete mirror/renewal yang aman, guard cycle/cascade-rename kategori, uniqueness NomorSertifikat lintas-tabel, dan perbaikan dedup notifikasi substring + filter Pre/Post. Mayoritas MED/LOW, dapat diparalelkan setelah P1-P4 stabil dan tidak memblokir fungsi inti grading. Diakhiri verifikasi area bersih PASS-02 sebagai baseline regresi.

---

## 5. Temuan Terverifikasi (detail)

### [CAT:CAT-01] Standard online exams bypass server-side timer-expiry enforcement (AssessmentType="Standard" never matches the Online guard)

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** The non-pre/post online assessment creation path stores AssessmentType = "Standard" (AssessmentAdminController.cs:1451), a literal that is NOT one of the AssessmentConstants.AssessmentType values (Manual/Online/PreTest/PostTest â€” AssessmentConstants.cs:6-11). The constant AssessmentType.Online ("Online") is defined but never assigned anywhere in the codebase (grep confirms only "Standard"/"Manual"/"PreTest"/"PostTest" are ever written). The exam-submit time-limit guard EnsureCanSubmitExamAsync (CMPController.cs:4382) only enforces the timer/grace window when AssessmentType is Online, PreTest, or PostTest (CMPController.cs:4390-4395); otherwise it returns null (skips the guard). Because standard online exams carry "Standard", they fall into the skip branch. StartExam still sets StartedAt for these sessions (CMPController.cs:961-965) and SubmitExam still calls the guard (CMPController.cs:1595), but the guard no-ops for them.

**Dampak:** For the most common assessment type (standard online multiple-choice exams), the server never rejects a submission made after the exam time limit + 2-minute grace has elapsed. A worker can leave the exam open past the deadline and still submit answers; the Tier-1 no-grace and Tier-2 grace-limit auto-reject are both inert. The exam duration is effectively client-side-only for standard exams, defeating Phase 313 CR-01/LIFE-03 timer enforcement.

**Bukti:** AssessmentAdminController.cs:1451 `AssessmentType = "Standard"` (standard online create path); AssessmentConstants.cs:6-11 enum has no "Standard"; grep for `AssessmentType = ... Online` returns only the const definition (never assigned); CMPController.cs:4390-4395 guard `if (AssessmentType != Online && != PreTest && != PostTest) return null;`; CMPController.cs:4427/4437 are the actual time-limit rejects that get skipped; CMPController.cs:1595 caller.

**Usulan fix:** Either set AssessmentType = AssessmentConstants.AssessmentType.Online for the standard online create path (AssessmentAdminController.cs:1451) and migrate existing "Standard" rows, OR change the guard at CMPController.cs:4390-4395 to enforce for all non-manual sessions (e.g. invert: skip only when IsManualEntry==true or AssessmentType==Manual/null). Confirm the certificate/grading logic for "Standard" rows also keys off the right discriminator after the change.

**Verifikasi adversarial:** Confirmed every link in the chain against the actual code, and adversarial refutation attempts all failed.

1) CREATE PATH: AssessmentAdminController.cs:1451 writes `AssessmentType = "Standard"` for the non-Pre/Post (standard online) create path. These are genuine timed online exams: the create block (lines 1431-1455) does NOT set IsManualEntry (so it stays the model default false â€” Models/AssessmentSession.cs:137), and DurationMinutes/Schedule/PassPercentage are real. The Pre/Post branch instead writes "PreTest"/"PostTest" (lines 1235/1271), which ARE recognized by the guard.

2) CONSTANTS: Models/AssessmentConstants.cs:7-10 defines only Manual/Online/PreTest/PostTest â€” there is no "Standard". Grep across all *.cs for any assignment of AssessmentType.Online / "Online" returns NO matches, so the Online constant is dead (never assigned). Confirmed every code-site that writes AssessmentType only ever writes "Standard"/"Manual"/"PreTest"/"PostTest".

3) GUARD: CMPController.cs:4390-4395 EnsureCanSubmitExamAsync returns null (skip) unless AssessmentType is Online OR PreTest OR PostTest. "Standard" matches none, so it hits the early `return null` skip branch â€” the Tier-1 no-grace reject (4427-4433) and Tier-2 grace reject (4437-4441) never run for standard exams.

4) FLOW: StartExam sets StartedAt for these sessions (CMPController.cs:961-966); SubmitExam calls the guard (CMPController.cs:1595) but it no-ops for "Standard".

Refutation attempts that FAILED:
- The pre-helper block at CMPController.cs:1551-1587 (`serverTimerExpired`) is NOT a deadline reject â€” it is the Phase 272 "block incomplete submission" check that ALLOWS incomplete submission when the timer is expired (line 1561: `if (!isAutoSubmit && !serverTimerExpired)`). It never rejects a late submission. So it does not cover the gap; if anything it lets a late standard submission through even with unanswered questions.
- No EF SaveChanges override / model normalization rewrites "Standard" to "Online" (grep negative).
- ExamWindowCloseDate (StartExam line 940) only bounds when the exam may be OPENED, not the per-attempt duration deadline.

Corroboration of original intent: the Phase 313 design (.planning/.../313-RESEARCH.md SC 6) explicitly enumerates the three timer-enforced types as Online/PreTest/PostTest with Manual excluded â€” i.e., the design assumed online exams carry "Online". The create path simply never writes "Online", so the most common assessment type silently falls into the skip branch. Net effect: for standard online multiple-choice exams the server never rejects a post-deadline submission; the duration is effectively client-side only, defeating Phase 313 CR-01/LIFE-03.

---

### [EDT:EDT-01] Regrade after edit discards manually-graded Essay scores â†’ spurious Passâ†’Fail, cert revoke, Proton marker removal

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Admin edit-jawaban + regrade + Pass<->Fail cert/Proton cascade |

**Masalah:** RegradeAfterEditAsync (GradingService.cs:449) recomputes the session score via ComputeScoreAndETInternalAsync, whose Essay case is a no-op (GradingService.cs:382-384) â€” it adds q.ScoreValue to maxScore but never adds the manually-entered EssayScore to totalScore. This diverges from the canonical scorer AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:52-54), which DOES add essayResp.EssayScore.Value to totalScore and is the source of truth used by FinalizeEssayGrading (AssessmentAdminController.cs:3555) when the session was first completed. An essay-bearing session becomes Status=='Completed' after HC finalizes essays (AssessmentAdminController.cs:3563-3569), and such a session passes AssessmentEditEligibility.IsEditableAsync (only requires Status=='Completed', not manual-entry, not Proton T3). When an admin then edits even a single MC/MA answer, RegradeAfterEditAsync recomputes the WHOLE score with essay contribution forced to 0, collapsing the percentage. PreviewEditScore/PreviewScoreAsync use the same defective compute path (GradingService.cs:560-566), so the preview shows the same wrong number, masking the bug.

**Dampak:** Any post-completion answer edit on a session that contains essay questions silently wipes the essay points from the recomputed score. A previously-passing candidate can be flipped to Fail, causing the certificate (NomorSertifikat + ValidUntil) to be revoked and, for Assessment Proton T1/T2, the Origin='Exam' completion marker to be removed and pending bypass reverted to 'Menunggu' â€” all from an unrelated MC edit. The corrupted score is also persisted into AssessmentSession.Score and written to the AssessmentEditLog.NewScore (AssessmentAdminController.cs:3152-3156), corrupting the audit trail too.

**Bukti:** GradingService.cs:382-384 Essay case empty in ComputeScoreAndETInternalAsync (no EssayScore added to totalScore) vs Helpers/AssessmentScoreAggregator.cs:52-54 (essayResp?.EssayScore.HasValue == true â†’ totalScore += essayResp.EssayScore.Value). RegradeAfterEditAsync calls ComputeScoreAndETInternalAsync at GradingService.cs:449 and derives newPct at :450. Essay sessions reach Completed via FinalizeEssayGrading AssessmentAdminController.cs:3555/3563-3569 using AssessmentScoreAggregator. Eligibility gate Helpers/AssessmentEditEligibility.cs:17-27 admits any Completed non-manual non-ProtonT3 session. Cascade fires on flip: Passâ†’Fail revokes cert+ValidUntil and calls RemoveExamOriginAsync + RevertPendingToMenungguAsync (GradingService.cs:476-498).

**Usulan fix:** Make RegradeAfterEditAsync use the same essay-inclusive aggregation as FinalizeEssayGrading. Either route ComputeScoreAndETInternalAsync's Essay case through AssessmentScoreAggregator (add EssayScore from PackageUserResponse to totalScore), or have RegradeAfterEditAsync call AssessmentScoreAggregator.Compute for the total/percentage so the kill-drift single-source guarantee actually covers the regrade path. Add a regression test covering editâ†’regrade on a session containing graded essays.

**Verifikasi adversarial:** Confirmed every link in the chain against the actual code.

1) Defective compute path: `GradingService.ComputeScoreAndETInternalAsync` runs `maxScore += q.ScoreValue` for EVERY question (line 364) including Essay, but the Essay branch (lines 382-384) is an empty `break` â€” it never adds the manually-entered `EssayScore` to `totalScore`. So essay points inflate the denominator while contributing 0 to the numerator. The XML doc (320-330) confirms this method backs both `RegradeAfterEditAsync` and `PreviewScoreAsync`.

2) Canonical scorer divergence: `Helpers/AssessmentScoreAggregator.Compute` lines 52-54 DO add `essayResp.EssayScore.Value` to `totalScore` for the Essay case. This is the source of truth used by `FinalizeEssayGrading` (AssessmentAdminController.cs:3555) when an essay session is first completed.

3) Essay sessions reach Status=='Completed': `FinalizeEssayGrading` requires a UserPackageAssignment (3507-3510), checks all essays graded (3523), computes score via the aggregator including essay points (3555-3559), then ExecuteUpdate sets Status='Completed' (3563-3569).

4) Eligibility admits them: `AssessmentEditEligibility.IsEditableAsync` (17-27) only blocks non-Completed, IsManualEntry, and Proton Tahun 3, and requires a UserPackageAssignment to exist â€” which essay sessions always have (per #3). No essay-bearing exclusion. `SubmitEditAnswers` (3032) gates solely on this.

5) Editâ†’regrade wiring: `SubmitEditAnswers` skips Essay rows in the edit loop (3067 `continue`) and only RemoveRange's MC/MA responses with PackageOptionId (3073, 3104), so essay EssayScore data persists in DB. It then calls `RegradeAfterEditAsync` (3150) which recomputes the WHOLE session score via the defective path (449-450), forcing essay contribution to 0 â†’ percentage collapses.

6) Cascade on flip: RegradeAfterEditAsync lines 476-498 â€” on Passâ†’Fail it nulls NomorSertifikat and ValidUntil, and for Assessment Proton with ProtonTrackId calls `RemoveExamOriginAsync` + `RevertPendingToMenungguAsync`.

7) Audit-trail corruption: the collapsed newScore is persisted to AssessmentSession.Score (459) and written into AssessmentEditLog.NewScore (3152-3156) plus the AuditLog description (3163).

8) Preview masks the bug: `PreviewEditScore`â†’`PreviewScoreAsync` uses the same `ComputeScoreAndETInternalAsync` (560-566), so the admin's preview shows the same wrong (essay-zeroed) number.

Adversarial refutation attempts all failed: no essay-special-casing exists before the empty Essay case; no separate essay-session block in the edit handler; essay sessions always have assignments so they are not excluded by the hasAssignment check. The only caveat (not a refutation) is that impact magnitude depends on essay ScoreValue and awarded EssayScore being non-zero, which is the normal case for a passing essay session.

---

### [GRD:GRD-01] RegradeAfterEditAsync drops manually-graded Essay scores to 0, corrupting Score and revoking certificates for essay-containing sessions

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Grading engine (MC/MA/Essay scoring, maxScore denominator) |

**Masalah:** A Completed assessment that contains Essay questions is graded via FinalizeEssayGrading, which uses AssessmentScoreAggregator.Compute â€” that aggregator ADDS each essay's manual EssayScore into totalScore (Helpers/AssessmentScoreAggregator.cs:52-55). The resulting Score and IsPassed therefore reflect MC + MA + Essay points. However, such a session remains editable: AssessmentEditEligibility.IsEditableAsync (Helpers/AssessmentEditEligibility.cs:17-27) gates only on Status=='Completed' (+ not IsManualEntry, not Proton T3) and does NOT exclude essay-containing sessions. When an Admin/HC edits any MC/MA answer via SubmitEditAnswers (Controllers/AssessmentAdminController.cs:3021-3150), the essay rows are skipped for editing (L3067) but RegradeAfterEditAsync is still invoked over the WHOLE session (L3150). RegradeAfterEditAsync calls ComputeScoreAndETInternalAsync, whose switch treats Essay as a no-op (Services/GradingService.cs:382-384) â€” essay points contribute 0 to totalScore while q.ScoreValue is still added to maxScore (L364). The new percentage is computed from MC/MA only over a denominator that still includes the essays, then written to Score/IsPassed under the Completed status guard and committed (AssessmentAdminController.cs:3150-3170). RegradeAfterEditAsync additionally revokes the certificate on a Pass->Fail flip (Services/GradingService.cs:476-498).

**Dampak:** Editing a single MC/MA answer on a Completed mixed (MC/MA + Essay) assessment silently zeroes out all manually-graded essay points in the recomputed Score. The worker's Score drops, IsPassed can flip Pass->Fail, and on that flip the NomorSertifikat + ValidUntil are nulled (certificate revoked) â€” all from an edit that did not touch the essays. The data is not self-healing: RecomputeEssayScores only repairs rows where Score is null or 0 (AssessmentAdminController.cs:3905-3910, 3959-3963), so a corrupted-but-nonzero MC/MA score will NOT be recovered by the repair endpoint.

**Bukti:** Helpers/AssessmentScoreAggregator.cs:52-55 (Essay adds EssayScore.Value to totalScore on finalize) vs Services/GradingService.cs:382-384 and 415-417 (ComputeScoreAndETInternalAsync Essay branch is empty -> 0 points) while Services/GradingService.cs:364 adds q.ScoreValue to maxScore for ALL questions including Essay; Services/GradingService.cs:449-462 writes the recomputed newPct to Score/IsPassed; Helpers/AssessmentEditEligibility.cs:20-26 (only Status=='Completed' gate, no essay exclusion); Controllers/AssessmentAdminController.cs:3067 (essay edits skipped) + 3150 (RegradeAfterEditAsync still called) + 3170 (tx.CommitAsync persists corrupted score). Pass->Fail cert revoke at Services/GradingService.cs:476-498.

**Usulan fix:** Make the regrade essay-aware: either (a) have ComputeScoreAndETInternalAsync include EssayScore for Essay questions (mirror AssessmentScoreAggregator.Compute exactly, reading PackageUserResponse.EssayScore), so the single source of truth is preserved across initial-finalize and post-edit regrade; or (b) block editing of essay-containing Completed sessions in AssessmentEditEligibility.IsEditableAsync / SubmitEditAnswers when any question QuestionType=='Essay'. Option (a) is preferable and aligns with the stated kill-drift intent â€” RegradeAfterEditAsync should reuse AssessmentScoreAggregator rather than a separate essay-blind compute.

**Verifikasi adversarial:** Confirmed end-to-end against the actual code. Two divergent scoring paths over the SAME question scope:

FORWARD (essay finalize) â€” Controllers/AssessmentAdminController.cs:3512-3559 builds allQuestions from packageAssignment.GetShuffledQuestionIds() (which per Models/UserPackageAssignment.cs:31 represents EVERY question in the assigned package, incl. essays) and calls AssessmentScoreAggregator.Compute. In the aggregator (Helpers/AssessmentScoreAggregator.cs:35,52-55) the Essay branch ADDS essayResp.EssayScore.Value to totalScore while maxScore += q.ScoreValue (L35) for all questions. Integration test EssayFinalizeRecomputeTests.Forward_EssayOnly_ScoreNotZero asserts essayScore 80 â†’ Percentage 80, IsPassed true. So a finalized mixed/essay session has Score reflecting essay points.

RE-GRADE (after MC/MA edit) â€” Services/GradingService.cs:437-462 RegradeAfterEditAsync calls ComputeScoreAndETInternalAsync, which iterates the SAME shuffledIds (L361), does maxScore += q.ScoreValue for ALL questions incl. Essay (L364), but the Essay case at L382-384 is an empty no-op (totalScore += 0). newPct (L450) therefore divides MC/MA-only points by a denominator that still includes essay ScoreValue, and the result is written to Score/IsPassed under the WHERE Status=='Completed' guard (L456-462), then committed.

EDITABILITY â€” Helpers/AssessmentEditEligibility.cs:17-27 gates only on Status=='Completed', not IsManualEntry, not Proton T3. No essay-containing exclusion. The GET (AssessmentAdminController.cs:2952) and POST (L3032) both use this gate; the edit UI loads all questions incl. essays. In SubmitEditAnswers the essay rows are skipped for EDITING only (L3067 continue) but RegradeAfterEditAsync is still invoked over the whole session (L3150) and tx.CommitAsync persists (L3170).

CERT REVOKE â€” On Passâ†’Fail flip, GradingService.cs:476-498 nulls NomorSertifikat + ValidUntil. Because the re-grade can lower Score below PassPercentage purely from zeroing essay points, an edit that did not touch essays can flip Passâ†’Fail and revoke the cert.

NOT SELF-HEALING â€” RecomputeEssayScores candidate filter (AssessmentAdminController.cs:3905-3910) and write guard (L3959-3963) both require Score==null||Score==0. A corrupted-but-nonzero score (e.g., 80â†’50) is neither selected nor updated. Test Recompute_Idempotent_OnlyTouchesScoreZero confirms a control row with Score=90 stays untouched. So recovery is impossible via the repair endpoint when the corrupted MC/MA-only percentage is nonzero.

I searched for any guard excluding essay/HasManualGrading sessions from the edit/re-grade path and found none. No test exercises RegradeAfterEditAsync on an essay-containing session, so the regression was never caught. Nothing refutes the finding.

---

### [OPS:OPS-01] StartExam GET claims worker exam state (InProgress/StartedAt/shuffle assignment) during impersonation â€” write-on-GET guard is incomplete

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Notifications + live monitoring + audit-log + impersonation |

**Masalah:** In CMPController.StartExam (GET), the Phase 377 write-on-GET impersonation guard at line 905 (`if (!_impersonationService.IsImpersonating())`) only wraps the Upcoming->Open status transition. The SAME GET handler then performs several other DB writes that are NOT guarded against impersonation: (a) lines 961-967 mark the session InProgress and set StartedAt = DateTime.UtcNow (this starts the worker's exam timer), and (b) lines 1012-1045 create the UserPackageAssignment row that locks in the shuffled question/option assignment, plus (c) lines 1069-1074 can reset ElapsedSeconds. During USER-mode impersonation, GetCurrentUserRoleLevelAsync (line 896, impl 2424-2446) resolves `user` to the impersonated target X, so the owner check `assessment.UserId == user.Id` (line 898) passes for X's own assessment. The impersonation middleware (ImpersonationMiddleware.InvokeAsync lines 70-77) lets all GET requests through. Therefore an admin impersonating worker X who merely opens X's StartExam page for an Open (not Upcoming) assessment that X has not yet started will silently start X's exam: StartedAt is stamped, Status flips to InProgress, the timer begins counting down, and a shuffle assignment is permanently created â€” all without X taking the exam. This is exactly the read-only invariant Phase 377 added the line-905 guard to protect, but the guard was applied to only one of the GET handler's writes.

**Dampak:** An admin reviewing/impersonating a worker accidentally starts the worker's exam: timer begins, status becomes InProgress, and the shuffle/question assignment is locked in. The worker may later find their exam already running with elapsed time burned, or with a question set/order they never saw chosen on their behalf. Corrupts exam integrity and the worker's allotted time.

**Bukti:** Controllers/CMPController.cs:902-911 (guarded Upcoming->Open) vs Controllers/CMPController.cs:960-967 (`bool justStarted = assessment.StartedAt == null; if (justStarted){ assessment.Status = "InProgress"; assessment.StartedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }` â€” NO IsImpersonating() check) and Controllers/CMPController.cs:1042-1045 (`_context.UserPackageAssignments.Add(assignment); await _context.SaveChangesAsync();`). Effective-user resolution confirming user-mode returns target X: Controllers/CMPController.cs:2441-2445. Middleware allows GET during impersonation: Middleware/ImpersonationMiddleware.cs:70-77.

**Usulan fix:** Extend the impersonation read-only guard to wrap ALL state-mutating writes in the StartExam GET handler, not just the Upcoming->Open transition. Wrap lines 961-978 (InProgress claim + SignalR workerStarted + activity log), the assignment-create block (1012-1056), and the stale-question ElapsedSeconds reset (1069-1074) in `if (!_impersonationService.IsImpersonating())`. When impersonating, render the exam read-only (or redirect with a hint) instead of claiming the session. Add a regression test covering user-mode impersonation viewing an Open, not-yet-started session.

**Verifikasi adversarial:** Confirmed against actual code in Controllers/CMPController.cs and supporting files.

1. Effective-user resolution returns target X in user-mode (Controllers/CMPController.cs:2424-2446). GetCurrentUserRoleLevelAsync calls _impersonationService.GetEffectiveUserAsync which (Services/ImpersonationService.cs:172-179) returns the impersonated target user X when ResolveEffectiveUserDecision == TargetUser (mode=="user", non-empty targetUserId). So at line 2445 it returns (X, effLevel).

2. Owner check passes for X. StartExam GET line 896 resolves `var (user, _) = await GetCurrentUserRoleLevelAsync()` (= X), and line 898 `if (assessment.UserId != user.Id && !Admin && !HC) return Forbid()` does NOT trip for X's own assessment (UserId == X.Id), so the handler proceeds.

3. The line-905 guard is scoped to ONLY the Upcoming->Open transition. Lines 902-911: `if (assessment.Status == "Upcoming" ...) { if (!_impersonationService.IsImpersonating()) { Status="Open"; SaveChangesAsync(); } }`. Grep confirms line 905 is the only `!IsImpersonating()` guard in StartExam.

4. The InProgress/StartedAt write is UNGUARDED. Lines 960-967: `bool justStarted = assessment.StartedAt == null; if (justStarted){ assessment.Status="InProgress"; assessment.StartedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }`. Grep confirms line 965 is the sole `StartedAt = DateTime` write and there is no IsImpersonating() check around it. This stamps the timer and flips status during impersonation.

5. The shuffle assignment write is UNGUARDED. Lines 1012-1056: when no UserPackageAssignment exists, it builds shuffledIds via ShuffleEngine and `_context.UserPackageAssignments.Add(assignment); await _context.SaveChangesAsync();` (lines 1042-1045) â€” permanently locking in the question/option order, with UserId = user.Id (= X). No impersonation guard.

6. ElapsedSeconds reset (lines 1069-1074) is reachable and unguarded (stale-question safety net), as claimed.

7. Middleware permits the GET (Middleware/ImpersonationMiddleware.cs:68-77): `if (method == "GET" || method == "HEAD") { SetContextItems...; await _next(context); return; }`. Write-blocking only applies to POST/PUT/DELETE non-whitelisted paths (lines 79-104).

Adversarial refutation attempts, all failed: (a) Class has only [Authorize] (line 24), no impersonation-blocking action filter. (b) IsImpersonating() returns true in this scenario (Services/ImpersonationService.cs:35-38) yet no guard consumes it before the writes. (c) The only universal early-exit that could block before line 962 is the token gate (line 929) â€” but it is conditioned on `assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null`; for assessments where IsTokenRequired is false, the impersonator falls straight through to line 962. The Completed (920), Abandoned (954), DurationMinutes<=0 (947), and ExamWindowClose (940) checks are all conditional and do not block a normal Open assessment. So the unguarded writes are genuinely reachable during user-mode impersonation of worker X on X's own Open, not-yet-started assessment.

The finding precisely matches Phase 377's documented intent (memory: "StartExam write-on-GET guard (Pitfall 3)") â€” the read-only invariant was applied to only one of three+ DB-write sites in the same GET handler.

---

### [PASS:PASS-01] Re-grade after edit drops manually-graded Essay scores, can flip Passâ†’Fail and revoke certificate

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Pass/fail formula + PassPercentage threshold + duplicated aggregator |

**Masalah:** `ComputeScoreAndETInternalAsync` (Services/GradingService.cs:331-429) â€” the engine behind both `RegradeAfterEditAsync` and `PreviewScoreAsync` â€” has an EMPTY Essay branch at L382-383: `case "Essay": break;`. It NEVER reads `PackageUserResponse.EssayScore` into `totalScore`, yet `maxScore` still adds the essay's `q.ScoreValue` at L364. This diverges from the single-source-of-truth `AssessmentScoreAggregator.Compute` (Helpers/AssessmentScoreAggregator.cs:52-55), which DOES add `EssayScore` to the total. A mixed MC/MA+Essay session reaches Status=='Completed' only after `FinalizeEssayGrading` finalizes it WITH the essay points counted (AssessmentAdminController.cs:3555 via the aggregator). The edit-answers path is then fully open on that session: `AssessmentEditEligibility.IsEditableAsync` (Helpers/AssessmentEditEligibility.cs:17-27) gates only on Status=='Completed' / !IsManualEntry / not Proton-Tahun-3 / has-assignment â€” it does NOT exclude essay-containing sessions, and the GET `EditPesertaAnswers` (AssessmentAdminController.cs:2965-2968) loads ALL package questions with no QuestionType filter. When an Admin/HC saves an MC/MA edit, `SubmitEditAnswers` skips essay rows (AssessmentAdminController.cs:3067) leaving the manual `EssayScore` intact in the DB, then calls `RegradeAfterEditAsync` (AssessmentAdminController.cs:3150) which recomputes Score WITHOUT the essay contribution.

**Dampak:** Editing a single multiple-choice answer on a finalized essay-containing assessment silently zeroes ALL essay points from the recomputed score. Example: 100-pt exam = 50 MC + 50 Essay(graded 50) finalized at 100% PASS; an admin correcting one MC key triggers a regrade that computes only MC over a denominator that still includes the 50 essay points â†’ Score collapses to ~50%, IsPassed flips to false, and the worker's certificate (NomorSertifikat + ValidUntil) is revoked. The PreviewEditScore endpoint (AssessmentAdminController.cs:3226) shows the same wrong lower score, so the admin is told the drop is legitimate. Wrongful fail + lost certificate = data-integrity/credential loss.

**Bukti:** Services/GradingService.cs:364 `maxScore += q.ScoreValue;` then L382-383 `case "Essay": break;` (no EssayScore read) inside ComputeScoreAndETInternalAsync; L387 `int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;` â€” totalScore excludes essay while maxScore includes it. Contrast Helpers/AssessmentScoreAggregator.cs:52-55 `case "Essay": var essayResp = ...; if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;`. RegradeAfterEditAsync calls this engine at Services/GradingService.cs:449 then writes the new Score/IsPassed at L456-462. Edit path: AssessmentAdminController.cs:3032 (eligibility, no essay check), 3067 (essay skipped in edit loop), 3150 (RegradeAfterEditAsync invoked). Certificate revocation on the resulting Passâ†’Fail flip: Services/GradingService.cs:476-498 sets NomorSertifikat=null and ValidUntil=null.

**Usulan fix:** Make the regrade/preview engine essay-aware by reusing the single source of truth. Either route `RegradeAfterEditAsync`/`PreviewScoreAsync` through `AssessmentScoreAggregator.Compute` (passing the DB/override responses so EssayScore is honored), or fill the empty `case "Essay":` at GradingService.cs:382-383 (and the ET copy at L415) to add `EssayScore` to totalScore exactly as the aggregator does. The override-answers preview path must also fold in existing EssayScore from dbResponses. Add a regression test for a mixed MC+Essay session edited post-finalize (expected: score unchanged when MC edit is net-neutral; essay points retained).

**Verifikasi adversarial:** Confirmed every link of the chain in the actual code, and tried to refute via guards/handlers elsewhere â€” none exist.

1. DIVERGENT ENGINES (the bug core): Services/GradingService.cs ComputeScoreAndETInternalAsync L360-385 adds maxScore += q.ScoreValue for EVERY question (L364) including Essay, but its Essay branch is EMPTY (`case "Essay": break;` L382-383) â€” it never reads PackageUserResponse.EssayScore into totalScore. pct = totalScore/maxScore*100 (L387) therefore divides MC/MA-only numerator by an essay-inclusive denominator. By contrast Helpers/AssessmentScoreAggregator.cs:52-55 DOES add essayResp.EssayScore.Value to totalScore. So the two engines genuinely diverge exactly as claimed.

2. FINALIZED ESSAY SESSION IS EDITABLE: AssessmentAdminController.cs:3555 FinalizeEssayGrading scores via AssessmentScoreAggregator.Compute (essay-inclusive) and sets Status="Completed" (L3563-3569), possibly issuing a cert. GradingService.cs:196-211 shows essay sessions route through the package path (UserPackageAssignment + PackageQuestions), so the has-assignment eligibility check is satisfied. Helpers/AssessmentEditEligibility.cs:17-27 gates ONLY on Status=="Completed", !IsManualEntry, not Proton-Tahun-3, and has-assignment â€” there is NO essay/HasManualGrading exclusion. I specifically looked for such a guard and it is absent in both IsEditableAsync and the controller checks at L2952 and L3032.

3. EDIT PRESERVES EssayScore THEN REGRADES WITHOUT IT: GET EditPesertaAnswers (L2964-2968) loads all package questions with no QuestionType filter. SubmitEditAnswers skips essay rows in the edit loop (L3067 `if ((q.QuestionType ?? "MultipleChoice") == "Essay") continue;`), so the manually-entered EssayScore stays in the DB, then it calls _gradingService.RegradeAfterEditAsync(session) (L3150). RegradeAfterEditAsync (L437-462) calls ComputeScoreAndETInternalAsync (L449) with overrideAnswers=null (reads DB) and writes the new essay-excluded pct to Score/IsPassed (L459-460).

4. CERT REVOCATION ON FLIP: GradingService.cs:476-498 â€” when wasPassed && !isPassed, ExecuteUpdate sets NomorSertifikat=null and ValidUntil=null (L482-483). A legitimately-passed essay exam re-scored without its essay points can drop below PassPercentage and trigger this.

5. PREVIEW MISLEADS: PreviewEditScore (L3216-3237) calls PreviewScoreAsync, which (GradingService.cs:560-567) wraps the SAME ComputeScoreAndETInternalAsync engine â€” so the admin's preview shows the same wrong, lower score and treats the drop as legitimate. (Note: the grep render showed `\` at L565 but the read of L387 confirms the real operator is `/`; non-load-bearing artifact.)

No compensating guard, handler, or essay-aware branch refutes any step. The audit's line citations are accurate. Impact is exactly as described: editing one MC answer on a finalized essay-containing assessment silently zeroes all essay points from the recomputed Score, can flip Passâ†’Fail, and revokes the worker's certificate â€” data-integrity + credential loss. HIGH is appropriate (silent, admin-triggered in normal workflow, irreversible cert revocation).

---

### [PEL:PEL-01] EditAssessment bulk-add (NewUserIds) creates Proton sessions with NO eligibility gate and unset ProtonTrackId/TahunKe

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Proton cross-year eligibility / completion gating |

**Masalah:** The eligibility gate (cross-year prior-year passed + 100% per-unit deliverables) is only applied in the CreateAssessment POST path (AssessmentAdminController.cs:1342-1406). The EditAssessment POST has a separate 'BULK ASSIGN' block that adds new participants to an EXISTING assessment via NewUserIds. It copies Category verbatim from the saved assessment (line 2128 'Category = savedAssessment.Category'), so it applies to Proton assessments, but it runs ZERO eligibility checks â€” no IsPrevYearPassedAsync, no IsEligiblePerUnit, no bypass-origin logic, no assignment-existence check. It also never sets ProtonTrackId or TahunKe on the new AssessmentSession objects (lines 2125-2144 omit both Proton fields entirely).

**Dampak:** An Admin/HC adding participants to an existing Proton assessment via Edit bypasses BOTH gates: workers who have not passed the prior year and/or are below 100% deliverable completion get exam sessions anyway. Additionally, because ProtonTrackId/TahunKe are left null, the resulting Proton sessions are malformed (downstream Proton-specific logic keyed on ProtonTrackId/TahunKe â€” e.g. completion-marker creation, Tahun 3 interview handling, ManagePackages/Records grouping â€” will misbehave or silently exclude these sessions).

**Bukti:** AssessmentAdminController.cs:2084-2146 â€” bulk-assign block: `var filteredNewUserIds = NewUserIds.Where(...)` then `var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession { Title = savedAssessment.Title, Category = savedAssessment.Category, ... UserId = uid ... })` with no ProtonTrackId/TahunKe and no gate. Method signature AssessmentAdminController.cs:1769-1772 `[HttpPost][Authorize(Roles="Admin, HC")] EditAssessment(int id, AssessmentSession model, List<string> NewUserIds, ...)`. Contrast with the gated create path at 1342-1406.

**Usulan fix:** In the EditAssessment bulk-assign block, when savedAssessment.Category == "Assessment Proton": (1) carry ProtonTrackId/TahunKe from the saved assessment to the new sessions, and (2) run the same eligibility gate used in CreateAssessment (extract the gate at 1342-1406 into a shared service/helper and call it from both paths to avoid drift).

**Verifikasi adversarial:** Confirmed against actual code. (1) Bulk-assign block AssessmentAdminController.cs:2084-2196 copies Category verbatim (line 2128 `Category = savedAssessment.Category`), so Proton assessments flow through it. (2) That block contains ZERO eligibility logic â€” only sibling-dedup (2095-2107) and user-existence validation (2111-2122); no IsPrevYearPassedAsync / CoacheeEligibilityCalculator.IsEligiblePerUnit / bypass-origin check. Contrast: CreateAssessment runs the full gate at 1342-1406 (IsPrevYearPassedAsync line 1382, IsEligiblePerUnit line 1401, Bypass-origin exempt 1378-1380). (3) The new-session object initializer (2125-2144) omits ProtonTrackId AND TahunKe entirely; CreateAssessment sets them at 1460-1461. ProtonTrackId/TahunKe are real persisted nullable columns (Models/AssessmentSession.cs:102,108), not derived. Adversarial refutation attempts all failed: (a) Pre-Post early-return branch (1785) requires AssessmentType PreTest/PostTest, but standard Proton sessions are created with AssessmentType="Standard" (line 1451), so they reach the bulk block. (b) No Proton-specific guard exists in the standard edit path between 1961 and 2084. (c) The EditAssessment view renders the "Tambah Peserta" NewUserIds card UNCONDITIONALLY (Views/Admin/EditAssessment.cshtml:580-651) with no Proton suppression â€” fully reachable by Admin/HC. The hidden ProtonTrackId/TahunKe inputs at view lines 446-448 only preserve the parent/sibling sessions, not the bulk-created ones. (d) Downstream impact confirmed real: Proton completion-marker EnsureAsync is gated on session.ProtonTrackId.HasValue in GradingService.cs:308 and :542 and controller essay path line 3641 â€” a bulk-added session (ProtonTrackId=null) that passes will silently NEVER get its year-completion marker, breaking cross-year gating/graduation; Tahun 3 interview handling requires session.TahunKe=="Tahun 3" (line 3685) and would exclude the malformed session.

---

### [QTY:QTY-01] CreateQuestion action loses [HttpPost]/[Authorize(Roles)]/[ValidateAntiForgeryToken] â€” attributes orphaned onto private TruncateAlt helper

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | security |
| **Confidence** | high |
| **Area** | Question types + image features (atomic delete, ref-count, sync) |

**Masalah:** In AssessmentAdminController.cs the three security attributes [HttpPost] (line 6273), [Authorize(Roles = "Admin, HC")] (6274) and [ValidateAntiForgeryToken] (6275) are written immediately above the private static helper method TruncateAlt (lines 6276-6278), not above the CreateQuestion action (line 6280). In C# attributes bind to the immediately-following member, so they decorate TruncateAlt (where they are inert) and the public action CreateQuestion ends up with NO action-level attributes. There is only one CreateQuestion overload (confirmed via grep), so no other declaration supplies them. The controller class itself has no [Authorize(Roles=...)]; the base class AdminBaseController carries only a plain [Authorize] (AdminBaseController.cs:12 = authenticated-only, no role). Program.cs registers AddControllersWithViews() with no global AutoValidateAntiforgeryToken filter and no role/fallback authorization policy. Net effect on the question-creation endpoint: (a) AUTHZ HOLE â€” any authenticated user (e.g. a worker/coachee, not just Admin/HC) can create exam questions, whereas the sibling actions EditQuestion (6435/6483) and DeleteQuestion (6713) correctly enforce Roles="Admin, HC"; (b) CSRF GAP â€” no antiforgery validation; (c) the missing [HttpPost] means the side-effecting action is reachable via GET as well as POST.

**Dampak:** A non-admin authenticated user can POST (or GET) /Admin/CreateQuestion to inject/modify assessment questions in any package (packageId is a parameter), corrupting exam content and scoring; the endpoint is also CSRF-able and side-effects on GET. Defense-in-depth that every other question-CRUD action has is silently absent here.

**Bukti:** AssessmentAdminController.cs:6273-6278 attributes precede `private static string? TruncateAlt(...)`; line 6279 blank; line 6280 `public async Task<IActionResult> CreateQuestion(` has no attributes. Contrast EditQuestion at 6482-6485 ([HttpPost]/[Authorize(Roles="Admin, HC")]/[ValidateAntiForgeryToken]) and DeleteQuestion at 6712-6714. Base authz: AdminBaseController.cs:12 `[Authorize]` (no roles). No global filter: Program.cs:13 `AddControllersWithViews();` only. Intended verb confirmed by view form Views/Admin/ManagePackageQuestions.cshtml:122 `asp-action="CreateQuestion" ... method="post"`.

**Usulan fix:** Move the [HttpPost], [Authorize(Roles = "Admin, HC")] and [ValidateAntiForgeryToken] attributes down so they decorate the CreateQuestion action (immediately above line 6280), and remove them from above TruncateAlt (a private helper needs no MVC attributes). Verify routing/build afterward.

**Verifikasi adversarial:** Confirmed against actual code. AssessmentAdminController.cs:6273-6275 the three attributes [HttpPost]/[Authorize(Roles="Admin, HC")]/[ValidateAntiForgeryToken] sit above a comment (6276) and the private static helper TruncateAlt (6277-6278); CreateQuestion is the very next member at 6280 with nothing between, so per C# binding the attributes decorate TruncateAlt (inert on a private helper) and the action is undecorated. Grep proves exactly ONE CreateQuestion overload (6280) and ONE TruncateAlt (6277) â€” no second declaration supplies them. No compensating guard exists: AdminBaseController.cs:12 has only plain [Authorize] (no role); the controller class (line 19-20) has [Route] but no class-level [Authorize(Roles=...)]; Program.cs:13 is AddControllersWithViews() with no args and a repo-wide grep for AutoValidateAntiforgeryToken/AddAuthorization/FallbackPolicy/Filters.Add returned zero matches (no global antiforgery filter, no global role policy). MaintenanceModeMiddleware only gates maintenance (it bypasses Admin/HC and lets others through when not in maintenance â€” not an authz layer); ImpersonationMiddleware only routes impersonation. Sibling actions EditQuestion (6482-6485) and DeleteQuestion (6712-6715) correctly carry all three attributes directly above the public action, confirming intent. The view Views/Admin/ManagePackageQuestions.cshtml:122 posts method=post and emits @Html.AntiForgeryToken() (123) but the token is never validated server-side because the attribute is orphaned. All three claimed effects hold: (a) authz hole â€” only base [Authorize] applies so any authenticated worker can create/edit exam questions; (b) CSRF gap â€” antiforgery inert; (c) verb gap â€” no [HttpPost] means GET also reaches the side-effecting action. The action does real packageId/type validation and DB writes, so an unauthorized call genuinely mutates exam content.

---

### [RST:RST-01] AddExtraTime lacks role authorization â€” any authenticated user (incl. worker/peserta) can grant unlimited extra exam time

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | security |
| **Confidence** | high |
| **Area** | Reset / force-end controls + retake semantics |

**Masalah:** AssessmentAdminController.AddExtraTime (Controllers/AssessmentAdminController.cs:6866-6910) carries only [HttpPost] and [ValidateAntiForgeryToken]. It does NOT carry [Authorize(Roles = "Admin, HC")], unlike every other reset/force-end action in the same controller (ResetAssessment:3998, AkhiriUjian:4143, AkhiriSemuaUjian:4208 all have it). The only gate is the base class AdminBaseController [Authorize] (Controllers/AdminBaseController.cs:12) which permits ANY authenticated user. Program.cs has no FallbackPolicy/global role policy (only AddControllersWithViews at :13, no AddAuthorization with policy). The [ValidateAntiForgeryToken] only stops cross-site CSRF; it does not stop a legitimately authenticated worker who holds their own valid antiforgery token + cookie from calling the endpoint directly. The endpoint resolves a batch by Title+Category+Schedule.Date (:6887-6892) and adds time to ALL InProgress sessions in that batch, and ExtraTimeMinutes feeds the server-side expiry enforcement (Hubs/AssessmentHub.cs:209, CMPController.cs:1495/1557 block submit-when-expired) plus the worker timer.

**Dampak:** A regular exam taker can self-extend their own exam window (cheating) or any other worker's exam in the same batch, undermining timed-exam integrity. It is an authorization hole inconsistent with all sibling admin actions.

**Bukti:** Controllers/AssessmentAdminController.cs:6866-6868 â€” `[HttpPost]` / `[ValidateAntiForgeryToken]` then `public async Task<IActionResult> AddExtraTime(int assessmentId, int minutes)` with NO Roles attribute; contrast ResetAssessment Controllers/AssessmentAdminController.cs:3997-4000 `[Authorize(Roles = "Admin, HC")]`. Base class Controllers/AdminBaseController.cs:12 `[Authorize]` (no roles). Consumption: Hubs/AssessmentHub.cs:209 `(session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60`; CMPController.cs:1495,1557 same expiry math.

**Usulan fix:** Add `[Authorize(Roles = "Admin, HC")]` to AddExtraTime, matching the other reset/force-end endpoints.

**Verifikasi adversarial:** Confirmed against actual code. AssessmentAdminController.cs:6866-6868 â€” AddExtraTime carries ONLY [HttpPost] + [ValidateAntiForgeryToken], NO [Authorize(Roles=...)]. The class (line 19-20) has only [Route("Admin/[action]")], no class-level role attribute. Base class AdminBaseController.cs:12 has only [Authorize] (no roles) = any authenticated user. Sibling actions verified with the role gate: ResetAssessment:3998, AkhiriUjian:4143, AkhiriSemuaUjian:4208, plus GetActivityLog:6802 directly above AddExtraTime â€” a grep shows ~60 actions in the controller carry [Authorize(Roles="Admin, HC")] (or "Admin"); AddExtraTime is the lone exception in the monitoring region. Program.cs only calls AddControllersWithViews() (:13); a codebase-wide grep for AddAuthorization|FallbackPolicy|AuthorizeFilter|RequireAuthorization|DefaultPolicy returns ZERO matches â€” no global/fallback role policy. Checked both middlewares that run post-authorization (Program.cs:210-211): MaintenanceModeMiddleware only redirects non-Admin/HC when maintenance mode is ENABLED, otherwise _next() passes through (:60-72); ImpersonationMiddleware short-circuits to _next() for non-impersonating users (:49-53). Neither gates a normal worker on /Admin/AddExtraTime. The action body (6868-6909) has no internal role/identity check â€” only validates minutes range and resolves a batch by Title+Category+Schedule.Date, adding time to ALL InProgress sessions in that batch. Consumption verified: ExtraTimeMinutes feeds server-side expiry at AssessmentHub.cs:209 (blocks save-when-expired), CMPController.cs:1495 (review-page timerExpired) and :1557 (blocks submit-when-expired), so it genuinely relaxes server-enforced timing. View addExtraTime() JS (AssessmentMonitoringDetail.cshtml:1569-1576) shows a form-urlencoded POST with assessmentId+minutes+antiforgery token â€” trivially reproducible by any authenticated worker (antiforgery token is obtainable from any page form they can render, so it does not stop a same-origin authenticated caller). Nothing elsewhere refutes the finding. Minor bound (not a refutation): a worker affects the whole batch (Title+Category+Date), not an arbitrary single session, but that still grants unauthorized exam-time extension / integrity violation.

---

### [SHF:SHF-01] ON-path multi-package: a single empty package zeroes out everyone's exam (K = Min = 0)

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Shuffle engine correctness + toggle + round-robin |

**Masalah:** In the ShuffleQuestions=ON path (the default; ShuffleQuestions defaults to true in AssessmentSession.cs:39), BuildCrossPackageAssignment computes the per-worker question count as K = packages.Min(p => p.Questions.Count) (ShuffleEngine.cs:108). It does NOT filter out empty packages first. If any package in the sibling group has zero questions, K becomes 0 and the method returns an empty list (ShuffleEngine.cs:109-110), so EVERY worker is assigned a zero-question exam. The OFF>=2 path explicitly guards against this by filtering empty packages BEFORE the modulo (ShuffleEngine.cs:53-57, comment 'D-02b: guard SEBELUM modulo'), but the ON path has no equivalent filter â€” a clear drift between the two branches of the same engine. Empty packages are reachable: CreatePackage (AssessmentAdminController.cs:5628-5634) persists a package with only a name + number and zero questions, and StartExam loads all sibling packages with no empty-filter (CMPController.cs:995-1000).

**Dampak:** If HC creates a second (or third) package and has not yet added questions to it â€” a common intermediate authoring state â€” and a worker starts the exam while shuffle is ON, the worker receives ZERO questions. The exam auto-grades against maxScore=0 â†’ finalPercentage=0 (CMPController.cs:1674), producing a bogus 0% / fail result with SavedQuestionCount=0 persisted. This silently corrupts results for the entire batch, not just the empty package's intended takers.

**Bukti:** Helpers/ShuffleEngine.cs:108-110 `int K = packages.Min(p => p.Questions.Count); if (K == 0) return new List<int>();` (no empty-package filter, unlike the OFF branch at lines 53-57). Reachability: Controllers/AssessmentAdminController.cs:5628-5634 creates an empty package; Controllers/CMPController.cs:995-1000 loads all sibling packages without filtering empties; default ShuffleQuestions=true at Models/AssessmentSession.cs:39.

**Usulan fix:** In BuildCrossPackageAssignment, filter empty packages before computing K, mirroring the OFF path: `packages = packages.Where(p => p.Questions != null && p.Questions.Count > 0).ToList(); if (packages.Count == 0) return new List<int>();` then compute K = packages.Min(...). Alternatively, exclude empty packages at the StartExam/reshuffle query level so both branches receive only packages-with-questions.

**Verifikasi adversarial:** Confirmed end-to-end against actual code. (1) ON path: ShuffleEngine.cs:108-110 does `int K = packages.Min(p => p.Questions.Count); if (K == 0) return new List<int>();` with NO empty-package filter â€” verified the single-package early-return (lines 97-105) only covers packages.Count==1, so â‰¥2 packages with one empty falls through to Min=0â†’empty list. (2) OFF path asymmetry confirmed: ShuffleEngine.cs:53-57 filters `.Where(p => p.Questions.Count > 0)` BEFORE modulo (comment 'D-02b: guard SEBELUM modulo'). Clear drift. (3) Default ShuffleQuestions=true at AssessmentSession.cs:39 â†’ ON is default. (4) Reachability: CreatePackage (AssessmentAdminController.cs:5627-5634) persists package with only name+number, zero questions; questions added separately via CreateQuestion (line 6280) â€” empty package is a genuine intermediate state. (5) StartExam loads all sibling packages with no empty filter (CMPController.cs:995-1000) and calls ShuffleEngine.BuildQuestionAssignment with assessment.ShuffleQuestions (line 1019-1020). (6) NO guard catches a zero-question assignment when packages exist: the only 'no questions' guard (CMPController.cs:1198-1203) fires only when packages.Any() is FALSE; with â‰¥1 package present the code enters PACKAGE PATH, builds empty shuffledIds, persists SavedQuestionCount=0 (line 1041), and renders an empty exam. Checked StartExam guards at lines 900-959 (status/token/window/duration) â€” none check question count. (7) Grading impact confirmed: maxScore = shuffledIds.Sum(...) = 0 (CMPController.cs:1614); finalPercentage = maxScore > 0 ? ... : 0 (line 1674) â†’ 0%. (8) Batch-wide: because K=Min across ALL sibling packages, every worker gets the empty list regardless of intended package. Refutation attempts failed: no pre/post guard, single-package early-return doesn't apply. Tests corroborate the gap â€” OFF has explicit empty-package tests (Off_MultiPackage_EmptyPackageExcludedBeforeModulo, Off_AllPackagesEmpty_ReturnsEmpty), but the ON test On_MultiPackage_SeedStable_SamplesKMin only uses two non-empty packages (3 and 2 questions). Only mitigating nuance: ShuffleEngine.cs:107 comment calls Min an 'edge case per user decision', i.e., a deliberate fallback â€” but it silently returns empty rather than filtering empties like OFF, so the hazard stands.

---

### [STAT:STAT-01] Grading race guard does not exclude Abandoned/Cancelled â€” resurrects a cancelled session into Completed

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | race |
| **Confidence** | high |
| **Area** | AssessmentSession status lifecycle + race-safe finalize |

**Masalah:** GradeAndCompleteAsync uses an ExecuteUpdateAsync status guard that only excludes the "Completed" status (non-essay branch: `Where(s => s.Id == session.Id && s.Status != "Completed")`; essay branch: `s.Status != Completed && s.Status != PendingGrading`). Neither branch excludes "Abandoned" or "Cancelled". The only pre-grading status check in SubmitExam is `if (assessment.Status == "Completed")` (CMPController:1545); EnsureCanSubmitExamAsync (CMPController:4382-4444) only enforces the timer/type, never the lifecycle status. AssessmentSession has no RowVersion/concurrency token (Models/AssessmentSession.cs has no [Timestamp] field), so there is no optimistic-concurrency protection either. Sequence: worker (or a stale second tab) calls AbandonExam (status flips to "Abandoned", CMPController:1241), then POSTs SubmitExam from a still-open exam tab. SubmitExam passes the `== "Completed"` check (status is "Abandoned"), reaches GradeAndCompleteAsync, and the guard `Status != "Completed"` matches the Abandoned row, flipping it to Completed/PendingGrading with a real Score/IsPassed and even generating a certificate. The same hole lets a Cancelled session (set by AkhiriSemuaUjian) be revived by a late SubmitExam POST.

**Dampak:** A session a worker explicitly abandoned (or HC cancelled via AkhiriSemuaUjian) can be silently resurrected to Completed with a passing score and an auto-issued certificate, defeating the 'contact HC to retake' control (StartExam blocks Abandoned re-entry at CMPController:954, but the SubmitExam POST endpoint is not protected). This is an integrity/authz hole: an abandoned/cancelled attempt becomes a valid, certificated pass.

**Bukti:** Services/GradingService.cs:238-246 (`.Where(s => s.Id == session.Id && s.Status != "Completed")` â€” no Abandoned/Cancelled exclusion); Services/GradingService.cs:202-206 (essay branch excludes only Completed+PendingGrading); Controllers/CMPController.cs:1544-1549 (SubmitExam only blocks Completed); Controllers/CMPController.cs:4382-4444 (EnsureCanSubmitExamAsync has no status check); Controllers/CMPController.cs:1233-1244 (AbandonExam flips to Abandoned). By contrast AkhiriUjian explicitly excludes Abandoned/Cancelled via its isInProgress check (AssessmentAdminController.cs:4154-4158), showing the intended invariant the grading guard fails to enforce.

**Usulan fix:** Extend the grading ExecuteUpdateAsync guards (both branches in GradeAndCompleteAsync) to also exclude terminal/closed statuses, e.g. `s.Status != "Completed" && s.Status != "Abandoned" && s.Status != "Cancelled"` (and PendingGrading for the non-essay branch). Additionally add an early lifecycle guard in SubmitExam (reject when Status is Abandoned/Cancelled, mirroring AkhiriUjian.isInProgress) so the resurrection is rejected before grading work begins.

**Verifikasi adversarial:** Every cited claim verified against actual code:

1. GradingService.cs:238-246 (non-essay branch) â€” guard is exactly `.Where(s => s.Id == session.Id && s.Status != "Completed")`. No exclusion of Abandoned/Cancelled. Lines 248-255 only treat rowsAffected==0 as "already Completed". An Abandoned/Cancelled row satisfies `Status != "Completed"` and is flipped to Completed with Score/IsPassed/CompletedAt.

2. GradingService.cs:202-211 (essay branch) â€” guard is `s.Status != Completed && s.Status != PendingGrading`. Confirmed Abandoned/Cancelled NOT excluded; row flips to PendingGrading.

3. GradeAndCompleteAsync (GradingService.cs:56-89) has NO early lifecycle status guard â€” it loads PackageAssignment and scores directly; the ExecuteUpdate Where clause is the only protection, and it is incomplete as above.

4. CMPController.cs:1545 â€” SubmitExam's only pre-grading lifecycle check is `if (assessment.Status == "Completed")`. Abandoned/Cancelled pass through. SubmitExam then persists responses (1678) and calls GradeAndCompleteAsync (1681).

5. EnsureCanSubmitExamAsync (CMPController.cs:4382-4444) enforces only AssessmentType + timer/grace; NO lifecycle status check. For a freshly-abandoned exam still inside its time window, elapsedSec < allowedSec so it returns null (pass).

6. AbandonExam (CMPController.cs:1241) sets Status = "Abandoned" and SaveChanges; guard at 1234 only allows the flip from InProgress/Open (so the abandon itself succeeds, then a stale tab can POST SubmitExam).

7. AkhiriSemuaUjian (AssessmentAdminController.cs:4235) sets Status = "Cancelled" via change-tracking â€” a late SubmitExam POST revives it the same way.

8. Model has no concurrency token: Models/AssessmentSession.cs Status field (line 20) has no [Timestamp]; grep for Timestamp|RowVersion|ConcurrencyCheck|ConcurrencyToken returned no matches. So no optimistic-concurrency backstop.

9. Intended invariant confirmed by contrast: AkhiriUjian (AssessmentAdminController.cs:4153-4158) computes isInProgress with explicit `Status != "Cancelled" && Status != "Abandoned"`, and StartExam (CMPController.cs:954) blocks re-entry of Abandoned ("Hubungi HC untuk mengulang"). The SubmitExam POST endpoint bypasses that control.

Certificate note: auto-cert at GradingService.cs:269 fires only if `session.GenerateCertificate && isPassed` â€” conditional, but the finding stated it conditionally ("even generating a certificate"), so the claim stands. The status flip to Completed/PendingGrading with a real Score/IsPassed happens unconditionally regardless of cert config.

No guard elsewhere refutes this. Severity HIGH stands: an explicitly abandoned or HC-cancelled attempt can be silently resurrected into a valid scored/passing (and potentially certificated) Completed session, defeating the documented "contact HC to retake" control. Exploit requires only the session owner (or Admin/HC) â€” i.e., the worker themselves via a stale open exam tab â€” and works within the exam's time window.

---

### [T3:T3-01] Reversing a Tahun 3 interview result (Lulus â†’ Tidak Lulus) leaves a stale ProtonFinalAssessment penanda Lulus â€” coachee stays 'Completed' and the year-gate stays unlocked

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Proton Tahun 3 interview scoring (outside GradingService) |

**Masalah:** SubmitInterviewResults supports re-submission (the view renders an 'Update Hasil Interview' button pre-filled from InterviewResultsJson, and `isPassed` is a plain checkbox: unchecking it sends nothing so the action binds isPassed=false). On the FIRST submit with isPassed=true, the handler calls _protonCompletionService.EnsureAsync(..., "Interview", ...) which inserts a ProtonFinalAssessment penanda (the system-wide source-of-truth for Tahun-3 completion). On a SECOND submit correcting the decision to FAIL, the handler sets session.IsPassed=false / Status=Completed but EnsureAsync is gated behind `if (isPassed && session.ProtonTrackId.HasValue)`, so it is skipped â€” and there is NO compensating removal of the previously created Interview penanda. The exam path is symmetric (GradingService.RegradeAfterEditAsync L476-497 detects Passâ†’Fail and calls RemoveExamOriginAsync to delete the Exam-origin penanda + revoke the certificate), but the interview path has no equivalent. RemoveExamOriginAsync is also deliberately Exam-only (ProtonCompletionService L113-129, Interview/Bypass kebal), so even calling it would not help â€” the Interview path must clean up its own penanda and currently does not.

**Dampak:** After HC reverses a Tahun 3 interview to Tidak Lulus, the coachee permanently displays 'Completed' on CDP/HistoriProton dashboards and the Proton year-gate continues to treat Tahun 3 as passed (allowing onward assignment / graduation as if they qualified). The corrected fail decision is reflected only on the session row (IsPassed=false) and the interview badge, while the authoritative completion penanda silently contradicts it â€” a graduation/eligibility integrity violation requiring manual DB cleanup.

**Bukti:** Controllers/AssessmentAdminController.cs:3744-3758 â€” sets session.IsPassed=isPassed/Status=Completed, then `if (isPassed && session.ProtonTrackId.HasValue) { EnsureAsync(...,"Interview",...) }` with no else/remove branch. Views/Admin/AssessmentMonitoringDetail.cshtml:657,720-733 â€” re-submittable 'Update Hasil Interview' form; isPassed is a bare `<input type=checkbox value=true>` (unchecked => false). ProtonCompletionService.cs:42-107 EnsureAsync inserts penanda; L113-129 RemoveExamOriginAsync filters Origin=="Exam" only. Asymmetry confirmed: GradingService.cs:476-497 (exam Passâ†’Fail revokes cert + RemoveExamOriginAsync) vs no such handling in SubmitInterviewResults. Source-of-truth confirmed: CDPController.cs:424-427 CurrentStatus='Completed' iff penanda exists; CoachMappingController.cs:1105-1108 hasFinalAssessment gates completion; ProtonCompletionService.cs:150-173 IsPrevYearPassedAsync/ProtonYearGate.IsAllowed treat penanda EXISTENCE (not IsPassed) as 'year passed'.

**Usulan fix:** In SubmitInterviewResults, when isPassed==false (or on every submit, mirroring the exam re-grade flip logic), remove any existing Interview-origin ProtonFinalAssessment for the active assignment before/instead of EnsureAsync. Add a ProtonCompletionService.RemoveInterviewOriginAsync(coacheeId, protonTrackId) (analogous to RemoveExamOriginAsync but Origin=="Interview") and call it inside the Proton+ProtonTrackId guard when isPassed is false, so reversal is idempotent and the penanda always matches the latest decision.

**Verifikasi adversarial:** Every claim verified against actual code, none refuted.

1. Re-submittable form + bare checkbox (Views/Admin/AssessmentMonitoringDetail.cshtml): Form posts to SubmitInterviewResults (L657), pre-filled from existingDto (judges L668, aspects L677, notes L703, checkbox `checked` L723), button text toggles "Update Hasil Interview" when hasResults (L731). The pass control is a bare `<input type=checkbox name=isPassed value=true>` (L722) with NO hidden companion field. Action signature is `bool isPassed` (non-nullable, AssessmentAdminController.cs:3673), so unchecking sends nothing â†’ model binding defaults isPassed=false. Confirmed.

2. Handler skips cleanup on Passâ†’Fail (AssessmentAdminController.cs:3743-3758): Sets session.IsPassed=isPassed (L3744), Status="Completed" (L3745), then `if (isPassed && session.ProtonTrackId.HasValue) { await _protonCompletionService.EnsureAsync(..., "Interview", ...) }` (L3753-3758). There is NO else/remove branch. On a second submit with isPassed=false the EnsureAsync (and any removal) is entirely skipped. Confirmed.

3. EnsureAsync inserts an Origin="Interview" penanda keyed by ProtonTrackAssignmentId, idempotent by existence (ProtonCompletionService.cs:42-107, insert L93-105). RemoveExamOriginAsync filters `fa.Origin == "Exam"` only (L121-124), so Interview/Bypass penanda are immune â€” even if it were called it could not remove the Interview penanda. Confirmed; the XML doc (L13-14) explicitly states Bypass/Interview are intentionally immune.

4. Asymmetry vs exam path (GradingService.cs:476-497): On wasPassed && !isPassed the exam path revokes the cert (L479-483) AND calls RemoveExamOriginAsync (L494) + RevertPendingToMenungguAsync (L496). The interview path has no equivalent. Confirmed.

5. Source-of-truth is penanda EXISTENCE not IsPassed:
- CDPController.cs:424-427: CurrentStatus = finalAssessment != null ? "Completed" : "In Progress" (never reads IsPassed). Confirmed.
- CoachMappingController.cs:1107-1109: hasFinalAssessment = AnyAsync(...); returns allApproved && hasFinalAssessment. Confirmed.
- ProtonCompletionService.cs:135-173: GetPassedYearsAsync queries ProtonFinalAssessments existence; IsPrevYearPassedAsync + ProtonYearGate.IsAllowed treat penanda existence as "year passed", never IsPassed. Confirmed.

No compensating handler exists. Grep for ProtonFinalAssessments removal found only 3 call sites: ProtonCompletionService.RemoveExamOriginAsync (Exam-only), WorkerController.cs:611-615 (deletes ALL penanda only on full worker-account deletion), and CoachMappingController.cs:1259-1268 (deletes ALL penanda only on full mapping deletion). Neither of the latter two is triggered by an interview-result reversal. No `Origin == "Interview"` removal exists anywhere. SubmitInterviewResults is the sole interview-result endpoint (single handler at L3669).

Net: after HC reverses a Tahun-3 interview Lulusâ†’Tidak Lulus, session.IsPassed=false but the Origin=Interview ProtonFinalAssessment persists, so CDP/HistoriProton still show "Completed" and the year-gate still treats Tahun 3 as passed (enabling onward eligibility/graduation). Real data-integrity defect requiring manual DB cleanup.

---

### [TMR:TMR-01] 2-tier timer submit block is dead code for the most common exam type (AssessmentType="Standard") â€” late/manual submits accepted

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Timer/duration server-authoritative + 2-tier submit block + auto-submit token |

**Masalah:** EnsureCanSubmitExamAsync only enforces the timer when AssessmentType is one of Online/PreTest/PostTest; everything else returns null (guard skipped). But the value "Online" is NEVER assigned to any session anywhere in the codebase (grep of `"Online"` / `AssessmentType.Online` hits only the constant definition and this comparison). Regular single online exams are created with AssessmentType="Standard" (AssessmentAdminController.cs:1451), and Proton Tahun 1/2 online exams also go through that same loop and get "Standard" (the Proton branch at :1458 only sets ProtonTrackId/TahunKe). ProtonBypassService.cs:332 likewise uses "Standard". Net effect: for the overwhelming majority of online exams ("Standard" + Proton), BOTH Tier-1 (no-grace manual block) and Tier-2 (post-grace auto block) are completely skipped â€” a worker can submit answers arbitrarily late (minutes/hours after the timer expired) and the server accepts and grades them. This is a regression: pre-Phase 313 (commit 2838c079, CMPController old line ~2097-2109) the LIFE-03 enforcement applied to ALL sessions regardless of AssessmentType (only legacy null-StartedAt was skipped). The Phase 313 refactor (677b3fc7) introduced the AssessmentType allowlist intending only to exclude Manual, but the use of the never-assigned "Online" constant collapsed the allowlist down to PreTest/PostTest only, silently dropping timer enforcement for Standard/Proton exams.

**Dampak:** Exam integrity broken for nearly all online exams: a worker can keep the tab open, let the timer expire, and still submit (or auto-submit) answers well past the allowed Duration+ExtraTime window, with zero server-side rejection. Cheating window is unbounded. The audit trail (SubmitExamBlocked) is never written for these exams either, so abuse is invisible.

**Bukti:** Controllers/CMPController.cs:4390-4395 â€” guard `if (AssessmentType != Online && != PreTest && != PostTest) return null;`. AssessmentConstants.cs:8 `Online="Online"` is the ONLY definition; grep shows no assignment of "Online" to any session. AssessmentAdminController.cs:1451 `AssessmentType = "Standard"` for the normal create loop (covers Proton Tahun 1/2 too â€” Proton branch at :1458 sets only ProtonTrackId/TahunKe). ProtonBypassService.cs:332 `AssessmentType = "Standard"`. Pre-313 broad enforcement: `git show 2838c079:Controllers/CMPController.cs` lines ~2097-2109 enforce timer for all StartedAt!=null sessions. No call site re-checks elapsed after EnsureCanSubmitExamAsync (CMPController.cs:1595-1596 then straight to grading at :1602+).

**Usulan fix:** Make the guard target the exam types that actually exist. Either (a) invert the logic to only SKIP for AssessmentConstants.AssessmentType.Manual (and null), matching the pre-313 behavior and the comment's stated intent, or (b) add "Standard" to the allowlist. Backfill/normalize existing "Online" usage if any was intended. Add a regression test asserting a "Standard" session with elapsed>allowed and no valid token is rejected.

**Verifikasi adversarial:** Confirmed against actual code. (1) Guard CMPController.cs:4390-4395 returns null (skips both timer tiers) unless AssessmentType == Online/PreTest/PostTest. (2) The string "Online" appears EXACTLY once in the entire *.cs codebase â€” the constant definition AssessmentConstants.cs:8; case-sensitive grep of "Online" across all *.cs and a targeted grep for `AssessmentType = "Online"` / `AssessmentType.Online` assignments returned zero production assignments. So no session is ever type "Online". (3) Real assigned types are "Standard" (AssessmentAdminController.cs:1451 main create loop â€” Proton branch at :1458 only adds ProtonTrackId/TahunKe/DurationMinutes, never overriding "Standard", and Proton exams go through THIS loop via model.Category=="Assessment Proton"), "PreTest"/"PostTest" (only in the isPrePostMode branch :1235/:1271), and ProtonBypassService.cs:332 "Standard". Therefore Standard + all Proton online exams skip the guard entirely. (4) Refutation attempts fail: SubmitExam's serverTimerExpired (CMPController.cs:1553-1559) is used at line 1561 only to RELAX the incomplete-answer block, never to reject; the only ExamWindowCloseDate check is line 940 inside StartExam (GET), not SubmitExam â€” it is nullable/optional and is a coarse availability window distinct from the per-session StartedAt+Duration+ExtraTime countdown the TMR-01 guard enforces, so a worker who started within the window but let the countdown expire still passes it; SubmitExam POST has no window/timer rejection for "Standard". The call site (CMPController.cs:1595-1596) only invokes EnsureCanSubmitExamAsync and then proceeds to grading at :1602+ with no further elapsed re-check. (5) Regression confirmed: git show 2838c079:Controllers/CMPController.cs shows pre-313 LIFE-03 enforced timer for ALL sessions with non-null StartedAt (no AssessmentType filter); the Phase 313 refactor introduced the allowlist anchored on the never-assigned "Online" constant, collapsing enforcement to PreTest/PostTest only. Corroborating: 313-UAT-FINDINGS.md:81 had to manually SQL-UPDATE a session to AssessmentType="Online" to exercise the guard, evidence nothing in-app sets it.

---

### [TOK:TOK-01] Pre-Post group EditAssessment stores AccessToken without uppercasing â€” breaks token verification

| | |
|---|---|
| **Severity** | HIGH (klaim: HIGH) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Token + StartExam access gates |

**Masalah:** The system establishes an invariant that stored AccessToken values are always UPPERCASE: CreateAssessment uppercases at AssessmentAdminController.cs:1105-1108 (model.AccessToken.ToUpper()), the non-Pre-Post EditAssessment branch uppercases at :2012-2013 (newToken = model.AccessToken.ToUpper()), and GenerateSecureToken (CMPController.cs:2464-2477) emits an uppercase-only charset. Token verification depends on this invariant: VerifyToken compares `assessment.AccessToken != token.ToUpper()` (CMPController.cs:876). HOWEVER, the Pre-Post group edit branch in EditAssessment writes the token verbatim with NO uppercasing: line 1812 `s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";` for the shared-field loop over all group sessions, and the new-participant rows at lines 1916 and 1937 `AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : ""`. There is no .ToUpper() anywhere before the early `return RedirectToAction("ManageAssessment")` at line 1957 that ends this branch (it never reaches the :2012 uppercasing in the non-Pre-Post path). So if an admin edits a Pre/Post-Test assessment and types a token containing any lowercase letters (e.g. 'abc23x'), it is persisted lowercase.

**Dampak:** A worker entering the exact (lowercase) token an admin set on a Pre/Post-Test will be permanently rejected at VerifyToken, because the worker's input is force-uppercased while the stored value is lowercase â€” they can never match. The worker is locked out of a token-required Pre/Post exam with a 'Token tidak valid' error despite typing the correct token. Conversely it silently masks the intended case-insensitive UX only for this edit path.

**Bukti:** AssessmentAdminController.cs:1812 (`s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";`), :1916, :1937 â€” no .ToUpper(); branch returns at :1957 before the uppercasing logic at :2010-2017. Consumer: CMPController.cs:876 `assessment.AccessToken != token.ToUpper()`. Contrast Create path :1105-1108 and non-Pre-Post edit :2012-2013 which DO uppercase.

**Usulan fix:** In the Pre-Post group edit branch, normalize the token before persisting, mirroring the other paths: compute `var newToken = model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken) ? model.AccessToken.ToUpper() : "";` once near the top of the branch and assign it at lines 1812, 1916, and 1937.

**Verifikasi adversarial:** Confirmed against actual code. (1) The Pre-Post edit branch in AssessmentAdminController.cs writes AccessToken verbatim with NO .ToUpper(): line 1812 `s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";` (shared-field loop), and new-participant rows at 1916 and 1937 `AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : ""`. The branch is entered at lines 1785-1786 (AssessmentType PreTest/PostTest && LinkedGroupId.HasValue) and returns at line 1957 (`return RedirectToAction("ManageAssessment")`), so it never reaches the uppercasing in the non-Pre-Post path. (2) The contrast holds: CreateAssessment uppercases at 1105-1108 (`model.AccessToken = model.AccessToken.ToUpper()`), and the non-Pre-Post edit branch uppercases at 2012-2013 (`newToken = model.AccessToken.ToUpper()`). (3) The verification consumer is exactly as claimed â€” CMPController.cs:876 `if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper())`, and this is the ONLY token comparison in the codebase, force-uppercasing the worker's input. (4) GenerateSecureToken (CMPController.cs:2464-2477) uses uppercase-only charset "ABCDEFGHJKLMNPQRSTUVWXYZ23456789", establishing the uppercase invariant.

Tried hard to refute and failed: (a) Models/AssessmentSession.cs:90 `AccessToken` is a plain auto-property â€” no normalizing setter. (b) The EditAssessment view input (Views/Admin/EditAssessment.cshtml:489) carries class `text-uppercase`, but that is CSS text-transform â€” purely presentational; it does NOT change the submitted form value. (c) No JS input/keyup listener uppercases the field; the only getElementById('AccessToken') usage is generateRandomToken() (line 816) which fires only on the Generate button and uses an uppercase charset â€” manual typing of lowercase is not transformed. The only toUpperCase matches in JS are in third-party jquery-validation libs, unrelated. (d) Reachability confirmed: Pre-Post creation sets AssessmentType PreTest/PostTest + LinkedGroupId (lines 1235, 1271-1272, 1288), exactly matching the edit branch entry condition. So an admin manually typing a lowercase token while editing a token-required Pre/Post group persists it lowercase, and the worker (whose input is force-uppercased at :876) can never match â€” permanent lockout with 'Token tidak valid'.

---

### [BYP:BYP-01] Concurrent instant bypass can create two active assignments (E8 invariant has no DB-level guard)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | race |
| **Confidence** | high |
| **Area** | Proton year-bypass workflow (PendingProtonBypass state machine) |

**Masalah:** The E8 invariant 'worker has exactly 1 active ProtonTrackAssignment' is enforced ONLY by an application-level read-then-write in ExecuteInstantBypassAsync (Services/ProtonBypassService.cs:104-118 reads activeAssignments, then 412-426 deactivates source + inserts a new active target). The DB index on ProtonTrackAssignments {CoacheeId, IsActive} is NON-unique (Migrations/ApplicationDbContextModelSnapshot.cs:1848 â€” no .IsUnique()). Two HC users firing instant bypass (CL-A / CL-B(a) / CL-C) for the same worker concurrently both pass the count==1 check under READ COMMITTED, both deactivate the source, and both insert a new active target â†’ worker ends with 2 active assignments. The CoachCoacheeMappings filtered unique index (IX_..._ActiveUnique) would catch the coach-SWAP case, but when TargetCoachId is null/empty (keep coach, MoveAssignmentAsync lines 456-467 inserts NO mapping), that protection does not apply, so nothing blocks the double insert. The CL-B(b) pending path is protected (filtered unique index on PendingProtonBypasses + D-12 atomic Status guard), and ConfirmBypassAsync is protected by the atomic Siapâ†’Selesai ExecuteUpdate guard â€” but the instant-execution path has no equivalent atomic guard.

**Dampak:** Two concurrent HC bypass actions on the same worker corrupt the assignment state to 2 active assignments. Once corrupted, every later bypass for that worker is permanently rejected ('Worker punya 2 assignment aktif (harus tepat 1)') and gate/eligibility resolution (which assumes one active assignment) is ambiguous, until manual DB repair. Requires two near-simultaneous admin actions, so likelihood is low, but impact is data-integrity corruption of the core invariant.

**Bukti:** Services/ProtonBypassService.cs:104-118 (E8 read-check), 412-426 (deactivate source + add new active assignment, no concurrency guard), 456-467 (no mapping insert when TargetCoachId null + unit unchanged); Migrations/ApplicationDbContextModelSnapshot.cs:1848 `b.HasIndex("CoacheeId", "IsActive")` is non-unique; contrast PendingProtonBypasses filtered unique index at snapshot:1479 and migration 20260611001939_AddPendingProtonBypassActiveUniqueIndex.cs:13-18.

**Usulan fix:** Add a filtered unique index on ProtonTrackAssignments(CoacheeId) WHERE IsActive=1 (mirroring IX_PendingProtonBypasses_CoacheeId_ActiveUnique and IX_CoachCoacheeMappings_CoacheeId_ActiveUnique), and catch the resulting DbUpdateException in ExecuteInstantBypassAsync/MoveAssignmentAsync to roll back the transaction with a friendly 'worker sudah dipindah' message. This makes the deactivate-then-insert atomic at the DB level for all coach configurations, not just coach-swap.

**Verifikasi adversarial:** Confirmed against the actual code, not refuted.

1) E8 enforced only app-level: ExecuteInstantBypassAsync (Services/ProtonBypassService.cs:105-118) reads `_context.ProtonTrackAssignments.Where(a => a.CoacheeId==req.CoacheeId && a.IsActive).ToListAsync()` then checks `Count != 1`. BypassSaveAsync repeats the same plain read at 226-234. Both are unhinted reads (no UPDLOCK/SERIALIZABLE), so under SQL Server default READ COMMITTED the shared locks release immediately and two concurrent transactions both observe count==1.

2) No DB guard: ProtonTrackAssignments index {CoacheeId, IsActive} is NON-unique â€” confirmed in Data/ApplicationDbContext.cs:369 (`entity.HasIndex(a => new { a.CoacheeId, a.IsActive });` with no .IsUnique()) and Migrations/ApplicationDbContextModelSnapshot.cs:1848 (`b.HasIndex("CoacheeId","IsActive")` with no IsUnique). Contrast: PendingProtonBypasses HAS a filtered unique index (snapshot:1477-1480, migration 20260611001939:13-18) and CoachCoacheeMappings HAS IX_..._ActiveUnique filtered on [IsActive]=1 (snapshot:711-714).

3) The write path corrupts: MoveAssignmentAsync (400-470) sets source.IsActive=false (413) then inserts a NEW active assignment (417-427). The model ProtonTrackAssignment (Models/ProtonModels.cs:71-87) has NO RowVersion/[Timestamp], so EF emits `UPDATE ... WHERE Id=@id` with no optimistic-concurrency check. The X-lock on the shared source row serializes the two deactivation UPDATEs but does NOT abort the second tx (row still matches WHERE Id; value just re-set to false), and both INSERTs of new active targets succeed â†’ 2 active assignments.

4) CoachCoacheeMappings index does not help the no-coach-change case: when TargetCoachId is null/empty (456-467) MoveAssignmentAsync inserts NO mapping (it only UPDATEs existing unit at 465 or no-ops at 467), so the IX_CoachCoacheeMappings_CoacheeId_ActiveUnique cannot catch the double assignment insert. Controller BypassSave (ProtonDataController.cs:1638) requires TargetUnit but NOT TargetCoachId, so the no-coach-change path is fully reachable.

5) Decisive corroboration: the developer explicitly recognized and TESTED this exact race for the pending path â€” test D10_RaceDobelPending_UniqueIndexTolakRequestKedua (HcPortal.Tests/ProtonBypassServiceTests.cs:523-552) simulates two concurrent inserts both passing the out-of-tx app check and relies on the filtered unique index to reject the second. The instant path (ExecuteInstantBypassAsync) has no equivalent DB constraint and no concurrency test. The pending and Confirm paths ARE protected (filtered unique index + atomic Status ExecuteUpdate guard at 509-517) â€” but the instant-execution path is not.

Found no guard elsewhere that refutes it: no controller-level lock, no SERIALIZABLE/lock hint, no RowVersion, no unique index. Impact = corruption of the core E8 invariant (2 active assignments), which then permanently rejects every later bypass for that worker (count!=1 at 108 / 230) and makes gate/eligibility resolution ambiguous until manual DB repair. Likelihood is low (needs two near-simultaneous HC actions on the same worker; the source-row X-lock narrows but does not close the window), so MED is correct: low probability, genuine sticky data-integrity corruption, and the safe fix (filtered unique index on ProtonTrackAssignments {CoacheeId} WHERE IsActive=1) is already the established pattern in this very file.

---

### [CAT:CAT-02] EditCategory has no cycle/self-parent/depth guard â€” reparenting can orphan or corrupt the category tree

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** EditCategory(int id, ..., int? parentId, ...) (AssessmentAdminController.cs:453-475) assigns category.ParentId = parentId with zero validation. It does not reject parentId == id (self-parent), does not reject reparenting a category under one of its own descendants (cycle), and does not enforce the 2-level depth limit that the rest of the system assumes. The PotentialParents dropdown is restricted to depth 0/1 in the view (SetCategoriesViewBag, AssessmentAdminController.cs:359-364) but that is UI-only; the POST accepts any int. By contrast, OrganizationController.EditUnit performs explicit self-parent and descendant-cycle checks (OrganizationController.cs:160-178 via IsDescendantAsync 339-342). The category tree is read via fixed-depth Includes (parentâ†’childâ†’grandchild only) in SetCategoriesViewBag (AssessmentAdminController.cs:336-347) and rendered only 3 levels in the view (ManageCategories.cshtml:317,381 â€” gc is the deepest).

**Dampak:** An admin (or crafted POST) can set a category as its own parent or as a child of its own descendant, creating a cycle. The self-referencing Include query and the recursive render would either loop/inconsistently materialize or silently drop the node. Reparenting to depth â‰¥3 makes the category invisible/unmanageable in ManageCategories (it is never loaded by the 3-level Include), orphaning it while it still constrains its children.

**Bukti:** AssessmentAdminController.cs:453-475 (no guard, raw `category.ParentId = parentId`); SetCategoriesViewBag depth-limited query 336-347; PotentialParents UI filter 359-364; view renders only parent/child/gc ManageCategories.cshtml:317 & 381; contrast OrganizationController.cs:160-178 + 339-342 which DO guard cycles.

**Usulan fix:** In EditCategory, reject parentId == id; walk ancestors of the proposed parent to reject cycles (mirror IsDescendantAsync); and reject parents that are already children (enforce max depth 1, i.e. parent.ParentId must be null) so the tree never exceeds the depth the queries/views support.

**Verifikasi adversarial:** EditCategory POST at AssessmentAdminController.cs:453-475 sets category.ParentId = parentId (line 473) after only name-empty and name-duplicate checks, with no self-parent, cycle, or depth guard. The PotentialParents dropdown (358-364, view 91-99) is UI-only and the POST accepts any int. Tree loads are fixed-depth at 3 sites (336-347 = 3 levels; 670-674 and 1758-1762 = 2 levels) and the view renders only parent/child/grandchild (ManageCategories.cshtml:317, 381), so a depth-3 reparent orphans the node. OrganizationController.EditUnit (160-178) plus IsDescendantAsync (336-345) DO guard this; the category path does not. No model/DbContext backstop: the self-referencing FK (ApplicationDbContext.cs:637-656, DeleteBehavior.Restrict, unique ParentId/Name) does not block ParentId==Id or cycles, and there is no SaveChanges override. Real. Gated by Admin/HC + antiforgery, so MED data-integrity not auth-bypass.</parameter>
<parameter name="notes">Fix: port OrganizationController self-parent and IsDescendantAsync cycle checks plus a parent-depth limit into EditCategory and AddCategory.</parameter>
</invoke>


---

### [CAT:CAT-04] Duplicate check uses untrimmed input while value is stored trimmed â†’ check bypass + unhandled DbUpdateException (500) on AddCategory

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** AddCategory checks the duplicate against the raw, untrimmed parameter (AssessmentAdminController.cs:406 `c.Name == name`) but stores the trimmed value (line 414 `Name = name.Trim()`). A submission like " K3" or "K3 " passes the duplicate check (no exact untrimmed match to existing "K3") yet is stored as "K3", colliding with the existing row. The composite unique index (ParentId, Name) then rejects the insert, and AddCategory's SaveChangesAsync (line 421-422) is NOT wrapped in try/catch, producing an unhandled DbUpdateException â†’ HTTP 500. EditCategory has the same untrimmed-check/trimmed-store inconsistency (lines 464 vs 470).

**Dampak:** Trailing/leading-whitespace category names slip past the friendly duplicate validation and crash with a raw 500 error instead of the intended "Nama kategori sudah digunakan" message; in the worst case (DB without the composite index applied) it silently creates a true duplicate.

**Bukti:** AssessmentAdminController.cs:406 check on raw `name`; :414 store `name.Trim()`; :421-422 unguarded `Add` + `SaveChangesAsync`; EditCategory :464 vs :470 same pattern; unique index ApplicationDbContext.cs:640.

**Usulan fix:** Trim once at the top (`name = name.Trim()`) and use the trimmed value for both the duplicate check and storage; additionally wrap AddCategory's SaveChangesAsync in a try/catch on DbUpdateException to surface a friendly message, matching DeleteCategory's pattern.

**Verifikasi adversarial:** Confirmed against the actual code. AssessmentAdminController.cs:406 runs the duplicate check on the raw parameter: `await _context.AssessmentCategories.AnyAsync(c => c.Name == name)`, while line 414 stores the trimmed value `Name = name.Trim()`. The insert at lines 421-422 (`_context.AssessmentCategories.Add(category); await _context.SaveChangesAsync();`) is NOT wrapped in try/catch â€” the only try/catch nearby (line 504) belongs to DeleteCategory, verified unrelated. EditCategory repeats the same inconsistency: untrimmed check at line 464 (`c.Name == name && c.Id != id`) vs trimmed store at line 470. The composite unique index is confirmed at ApplicationDbContext.cs:640: `entity.HasIndex(c => new { c.ParentId, c.Name }).IsUnique()`. Provider is SQL Server (Program.cs:28 UseSqlServer). Model (Models/AssessmentCategory.cs) has no setter-level trimming; the view (ManageCategories.cshtml:72) posts raw `name` with only HTML `required`/`maxlength` (no client trim, and `required` permits leading-space non-empty values).

Repro of the worst path holds via LEADING whitespace: existing "K3" stored trimmed; submitting " K3" â†’ check `c.Name == " K3"` does not match (SQL Server treats leading spaces as significant) â†’ bypasses friendly validation â†’ name.Trim() stores "K3" â†’ violates the (ParentId, Name) unique index â†’ DbUpdateException from the unguarded SaveChangesAsync. In non-Development, app.UseExceptionHandler("/Home/Error") (Program.cs:165-167) turns this into an HTTP 500 error page, NOT the intended "Nama kategori sudah digunakan" message â€” exactly as claimed.

One nuance that partially weakens (but does not refute) the finding: the cited TRAILING-space example ("K3 ") would actually be caught on SQL Server, because `=` ignores trailing spaces under ANSI padding, so that input matches the existing row in both the duplicate check and the unique index. The defect is fully real via the leading-space vector. The "silent true duplicate" worst case only materializes if the composite index is not applied to the target DB; with the index present (as configured) the outcome is a 500 rather than a duplicate, so no data corruption when the schema is intact.

---

### [CAT:CAT-05] Category rename does not cascade to denormalized Category/SubKategori strings on sessions and training records

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** AssessmentSession.Category/SubKategori and TrainingRecord.Kategori/SubKategori are denormalized strings that reference AssessmentCategory by Name, not by FK (AssessmentSession.cs:16 & 151; TrainingRecord.cs:16 & 55 â€” comment 'Sub category from AssessmentCategories'). EditCategory (AssessmentAdminController.cs:470-475) updates only the AssessmentCategory row; it performs no cascade rename to existing sessions/training records that stored the old name. OrganizationController.EditUnit, handling the analogous denormalized case, DOES cascade-rename (OrganizationController.cs:197-198+). Filter dropdowns and grouping rely on the session Category string (e.g. categories list built from `AssessmentSessions.Select(a => a.Category).Distinct()` at AssessmentAdminController.cs:232-237; grouping/sibling matching keyed on Category throughout).

**Dampak:** Renaming a category leaves all existing assessments/training records pointing at the old name. They become a separate orphaned bucket in filters/grouping (the old name still appears, the new name applies only to future records), splitting reporting and breaking category-based filtering/sibling-grouping for historical data.

**Bukti:** AssessmentAdminController.cs:470-475 (no cascade); AssessmentSession.cs:16,151; TrainingRecord.cs:16,55; cache/filter source AssessmentAdminController.cs:232-237; contrast OrganizationController.cs:197-198 cascade rename.

**Usulan fix:** On rename (oldName != newName), cascade-update AssessmentSessions.Category/SubKategori and TrainingRecords.Kategori/SubKategori that match the old name (bulk update inside a transaction), mirroring OrganizationController's cascade. Alternatively introduce a real FK and resolve names at read time.

**Verifikasi adversarial:** Confirmed from the actual code on all four claimed points.

1) Denormalized-by-name (no FK): Models/AssessmentSession.cs:16 (`public string Category`) and :151 (`public string? SubKategori` â€” comment "Sub-kategori assessment"); Models/TrainingRecord.cs:16 (`public string? Kategori`) and :55 (`public string? SubKategori // Sub category from AssessmentCategories`). Grep for `CategoryId`/`AssessmentCategoryId` across Models returned NO matches â€” there is no FK. Views/Admin/CreateAssessment.cshtml:139/148/155/168 emit `<option value="@cat.Name">` bound via `asp-for="Category"`, so the form posts the category NAME, which binds to the string `model.Category`. So these columns store the category Name by value.

2) EditCategory has no cascade: AssessmentAdminController.cs:453-488. After validation it sets only category.Name/DefaultPassPercentage/SortOrder/ParentId/SignatoryUserId (lines 470-474), SaveChangesAsync, cache.Remove, audit log, redirect. No query or update touches AssessmentSessions or TrainingRecords. Confirmed no cascade.

3) Contrast with OrganizationController.EditUnit: OrganizationController.cs:193-232 explicitly cascade-renames the analogous denormalized strings when `oldName != name.Trim()` â€” Users.Section/Unit, CoachCoacheeMappings.AssignmentSection/AssignmentUnit, ProtonKompetensiList.Bagian/Unit, CoachingGuidanceFiles.Bagian/Unit. This proves the cascade-on-rename pattern is established in the codebase and EditCategory omits it.

4) Filter/grouping dependence on the string: the categories dropdown source is built from `_context.AssessmentSessions.Select(a => a.Category).Distinct()` (AssessmentAdminController.cs:229-238, cached). Sibling/duplicate/grouping queries key on `a.Category == ...` throughout (e.g. lines 1092, 1709, 2022, 2330, 4334, 4419, 4803, 5183, 6889) and the management list filters on `a.Category == category` (line 137). So a rename leaves historical rows under the OLD name: they form an orphaned filter bucket and won't group/match with newly-created rows under the new name.

No refuting guard found: there is no trigger, EF value-conversion, computed re-derivation, or post-save reconciliation that would update the stored strings. Impact claim (split reporting / broken historical filtering & sibling-grouping) holds.

Severity: data-integrity consistency bug, recoverable (re-rename or SQL), and only triggered when an admin renames a category that already has sessions/records â€” not a default flow, no crash/security exposure. The codebase itself treats the analogous case as worth cascading (OrganizationController), so MED is appropriate.

---

### [CERT:CERT-01] Passed assessment cert with ValidUntil=null shows as Expired on dashboards but is NOT counted in badge and triggers no expiry notification (status/count drift)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | drift |
| **Confidence** | high |
| **Area** | Certificate generation + number + renewal chain + expiry + cascade-delete |

**Masalah:** HC sets ValidUntil at assessment creation and it is OPTIONAL (AssessmentAdminController.cs:1447 / :1269 assign model.ValidUntil which may be null; nowhere is it defaulted for passed certs). The dashboard status helper SertifikatRow.DeriveCertificateStatus treats a non-Permanent cert with ValidUntil==null as Expired (CertificationManagementViewModel.cs:58-59), and assessment rows always pass certificateType:null (AdminBaseController.cs:187, CDPController.cs:4069, CMPController.cs:3892, RenewalController.cs:149). So the Renewal Certificate dashboard and CMP/CDP cert dashboards LIST such an assessment as Expired/needs-renewal. However the home badge counter GetCertAlertCountsAsync filters AssessmentSessions with `a.ValidUntil.HasValue` (HomeController.cs:215) and the notification trigger TriggerCertExpiredNotificationsAsync filters `a.ValidUntil.HasValue && a.ValidUntil.Value < today` (HomeController.cs:124). A ValidUntil==null assessment is therefore excluded from both the badge count and the CERT_EXPIRED notifications. Result: the dashboard says the cert is expired, but the alert badge and HC notifications never reflect it.

**Dampak:** HC sees a certificate flagged Expired in the management list yet receives no expiry notification and the expired badge undercounts. Workers whose passed assessment cert was created without an expiry date silently fall through expiry tracking, so renewals are missed.

**Bukti:** CertificationManagementViewModel.cs:58-59 (validUntil==null -> Expired); AdminBaseController.cs:148,187,200 (assessment rows built with GenerateCertificate && IsPassed only, status via DeriveCertificateStatus(a.ValidUntil,null), then post-filter keeps Expired); HomeController.cs:214-220 (count requires a.ValidUntil.HasValue); HomeController.cs:121-126 (notification requires a.ValidUntil.HasValue && < today); AssessmentAdminController.cs:1447,1269 (ValidUntil = model.ValidUntil, nullable, not defaulted)

**Usulan fix:** Make the count/notification queries consistent with DeriveCertificateStatus: include passed-cert AssessmentSessions with ValidUntil==null in the Expired count and notification set (or, conversely, decide ValidUntil==null means 'no expiry' and treat it as Permanent/Aktif in DeriveCertificateStatus). Pick one definition and apply it across DeriveCertificateStatus, GetCertAlertCountsAsync, and TriggerCertExpiredNotificationsAsync.

**Verifikasi adversarial:** Verified every link of the chain against actual code; no refuting guard exists.

1) Null ValidUntil is allowed for passed certs. AssessmentAdminController.cs:976-982 explicitly comments "ValidUntil: opsional di normal mode, wajib di renewal mode" â€” it calls ModelState.Remove("ValidUntil") and only adds a required error when isRenewalModePost. Create sites assign ValidUntil = model.ValidUntil (nullable, undefaulted) at :1269 and :1447. GradingService.cs:516 confirms ValidUntil is NOT defaulted at cert generation ("ValidUntil mengikuti setup sesi HC ... TIDAK di-hardcode di sini"); the Fail->Pass branch only sets NomorSertifikat. So a GenerateCertificate && IsPassed session can persist with ValidUntil == null.

2) Dashboard status helper maps null -> Expired. CertificationManagementViewModel.cs:58-59: non-Permanent with validUntil == null returns CertificateStatus.Expired.

3) Every dashboard passes certificateType: null for assessment rows and does NOT filter null ValidUntil: AdminBaseController.cs:148 base query (GenerateCertificate && IsPassed only) -> :187 DeriveCertificateStatus(a.ValidUntil, null) -> :199-203 post-filter KEEPS Expired/AkanExpired, so a null-ValidUntil cert is listed as Expired on the main RenewalCertificate page. Same pattern at CMPController.cs:3840/3892, CDPController.cs:4015/4069, RenewalController.cs:55/149 (none filter ValidUntil.HasValue).

4) Badge counter excludes null. HomeController.cs:215: .Where(a => a.GenerateCertificate && a.IsPassed == true && a.ValidUntil.HasValue) â€” null-ValidUntil cert never counted.

5) Notification trigger excludes null. HomeController.cs:124: .Where(a => ... && a.ValidUntil.HasValue && a.ValidUntil.Value < today) â€” null-ValidUntil cert never triggers CERT_EXPIRED.

Net: dashboards show the cert as Expired/needs-renewal, but the home expired badge undercounts it and HC never receives an expiry notification for it. I looked specifically for a defaulting of ValidUntil for passed certs (creation, GradeAndCompleteAsync paths, RegradeAfterEditAsync) and a ValidUntil.HasValue guard on the dashboard queries that would reconcile the two; neither exists. The only constraints found (renewal-mode required ValidUntil; copy/AddYears(1) renewal pre-fill with a warning when source is empty) do not cover the normal-mode no-expiry passed cert. Finding confirmed.

---

### [CERT:CERT-02] Regrade Pass->Fail->Pass leaves ValidUntil NULL: re-issued cert has no expiry date and is mis-classified as Expired

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Certificate generation + number + renewal chain + expiry + cascade-delete |

**Masalah:** In GradingService.RegradeAfterEditAsync, a Pass->Fail flip revokes the cert by nulling BOTH NomorSertifikat and ValidUntil (lines 479-483). A subsequent Fail->Pass flip re-generates only NomorSertifikat (lines 517-521) and deliberately does NOT set ValidUntil (comment line 516 claims parity with GradeAndCompleteAsync). But ValidUntil for assessments is stored on the session row at creation time and is never re-derived in the grading paths; once nulled in step 5 it is lost. After a Pass->Fail->Pass cycle the session ends up IsPassed=true, NomorSertifikat set, ValidUntil=null. DeriveCertificateStatus(null,null) then reports Expired (CertificationManagementViewModel.cs:58-59) and the issued PDF omits the 'Berlaku Hingga' line (CMPController.cs:2046-2052).

**Dampak:** After an admin edits answers causing a fail-then-pass regrade, the worker's reinstated certificate permanently loses its expiry date, shows as Expired in dashboards, and the regenerated PDF lacks the validity date â€” a silent data-loss on the validity window.

**Bukti:** GradingService.cs:479-483 (Pass->Fail sets NomorSertifikat=null AND ValidUntil=null); GradingService.cs:516-521 (Fail->Pass sets only NomorSertifikat, ValidUntil untouched); CertificationManagementViewModel.cs:58-59 (null ValidUntil -> Expired); CMPController.cs:2046 (PDF prints expiry only if assessment.ValidUntil.HasValue)

**Usulan fix:** In the Pass->Fail branch, do not null ValidUntil (only revoke NomorSertifikat), OR in the Fail->Pass branch restore ValidUntil from the original session setup (e.g. capture it before the revoke, or recompute from package/category policy) so the re-issued cert keeps its validity window.

**Verifikasi adversarial:** Confirmed every link of the chain against actual code.

1) Pass->Fail revoke nulls BOTH fields. GradingService.cs:479-483 (inside `if (wasPassed && !isPassed)`) does `ExecuteUpdateAsync(... NomorSertifikat=null ... ValidUntil=(DateOnly?)null)`. Confirmed.

2) Fail->Pass restores ONLY NomorSertifikat. GradingService.cs:516-521 (inside `else if (!wasPassed && isPassed)`) only `SetProperty(r => r.NomorSertifikat, nomor)`; the comment at 516 ("ValidUntil mengikuti setup sesi HC (paritas GradeAndCompleteAsync) â€” TIDAK di-hardcode") explicitly declines to set ValidUntil. Confirmed.

3) ValidUntil is a creation-time value, never re-derived in grading. Grep of `.ValidUntil =` shows it is assigned only at session creation/edit in AssessmentAdminController.cs (1269, 1447, 1834, 1939) from `model.ValidUntil`, and is optional in normal mode (AssessmentAdminController.cs:976 "ValidUntil: opsional di normal mode"). The initial-grading path GradeAndCompleteAsync (GradingService.cs:269-301) generates only NomorSertifikat and never touches ValidUntil â€” so the "parity" comment is technically true but misleading: at initial grade ValidUntil still holds its creation value, whereas after a Pass->Fail revoke it has been nulled and is never restored. Confirmed.

4) Caller does not restore it. AssessmentAdminController.cs:3150 calls RegradeAfterEditAsync and the surrounding edit endpoint (3110-3199) never sets/restores ValidUntil. No backfill or display-time fallback exists anywhere (grep of ValidUntil in CMPController/CDPController/AdminBaseController/RenewalController shows all read paths consume the stored value directly). Confirmed.

5) Null ValidUntil => Expired for assessment rows. CertificationManagementViewModel.cs:53-65: with certificateType=null (line 56 false) and validUntil==null, line 58-59 returns CertificateStatus.Expired. Assessment rows are built with certificateType=null at CMPController.cs:3892, CDPController.cs:4069, AdminBaseController.cs:187, RenewalController.cs:149. (Note: the XML doc comment at line 50-51 wrongly says null->Permanent, but the executable code returns Expired.) Confirmed.

6) PDF omits validity line. CMPController.cs:2046-2052 prints "Berlaku Hingga" only `if (assessment.ValidUntil.HasValue)`. Confirmed.

End state after Pass->Fail->Pass: IsPassed=true, NomorSertifikat set, ValidUntil=null -> dashboard shows Expired and re-issued PDF lacks the validity date. Additionally the expiring-soon reminder queries (CMPController.cs:2618-2620, 2764) require ValidUntil.HasValue, so the cert silently drops out of renewal reminders too. No refuting guard found.

Severity stays MED: it is a real silent data-loss of the validity window with mis-classification, but it only triggers on the specific admin-driven Pass->Fail->Pass edit sequence on a cert-bearing assessment that had a non-null ValidUntil (renewal/expiring certs; many normal-mode assessments may have null ValidUntil anyway, in which case status is already Expired pre-bug). Data-integrity impact, not a security/availability break.

---

### [EDT:EDT-02] ValidUntil not restored on Failâ†’Pass regrade after a prior Passâ†’Fail â†’ certificate re-issued with null expiry

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Admin edit-jawaban + regrade + Pass<->Fail cert/Proton cascade |

**Masalah:** On a Passâ†’Fail flip, RegradeAfterEditAsync nulls both NomorSertifikat and ValidUntil (GradingService.cs:479-483). On a subsequent Failâ†’Pass flip (e.g. the admin re-edits the answer back, or fixes the answer key), the Failâ†’Pass branch re-issues a NomorSertifikat (GradingService.cs:517-520) but never re-populates ValidUntil. ValidUntil is a stored field set only at session creation from HC's configured model.ValidUntil (AssessmentAdminController.cs:1269/1447) â€” there is no DB default and no recomputation. So a session that goes Passâ†’Failâ†’Pass ends up with a valid certificate number but ValidUntil == null permanently.

**Dampak:** A certificate flipped Passâ†’Failâ†’Pass is re-issued with no expiry date. Downstream consumers that key off ValidUntil drop it: expiring-cert tracking (HomeController.cs:215, CMPController.cs:2764/3000) and renewal listing (RenewalController.cs) require ValidUntil.HasValue, so the worker's certificate silently disappears from renewal/expiry workflows and may be treated as a never-expiring cert in displays â€” an integrity/compliance gap for time-bound certifications.

**Bukti:** Passâ†’Fail nulls ValidUntil: GradingService.cs:481-483 (.SetProperty(r => r.ValidUntil, (DateOnly?)null)). Failâ†’Pass branch only sets NomorSertifikat: GradingService.cs:517-520 â€” no ValidUntil assignment anywhere in the branch (confirmed: only two ValidUntil references in GradingService.cs are the null-set at :483 and the comment at :516). ValidUntil originates at creation: AssessmentAdminController.cs:1269/1447; model AssessmentSession.cs:72 has no default.

**Usulan fix:** In the Failâ†’Pass branch of RegradeAfterEditAsync, restore ValidUntil to the session's configured expiry (mirror GradeAndCompleteAsync setup / re-derive from HC config) when re-issuing the certificate, rather than leaving it null. Alternatively, snapshot ValidUntil into the edit log on Passâ†’Fail and restore it on Pass re-issue.

**Verifikasi adversarial:** Confirmed the core defect directly from code. GradingService.RegradeAfterEditAsync: the Passâ†’Fail branch nulls BOTH NomorSertifikat and ValidUntil (GradingService.cs:479-483, literally `.SetProperty(r => r.ValidUntil, (DateOnly?)null)` at line 483). The Failâ†’Pass branch (lines 499-549) only re-issues NomorSertifikat (line 520) and never re-populates ValidUntil; the comment at line 516 explicitly states ValidUntil follows HC session setup and is "TIDAK di-hardcode di sini" (not set here). I tried to refute via three candidate restoration paths and all failed: (1) GradeAndCompleteAsync (initial grading) also never sets/recomputes ValidUntil â€” its non-essay completion (lines 238-246) and cert-gen block (269-301) set only Score/Status/NomorSertifikat, relying on the stored value, so there is no recomputation logic anywhere; (2) AssessmentSession.cs:72 is a plain nullable DateOnly with no EF default and no Data-layer default config (grep of Data found nothing); (3) the standalone EditAssessment POST path is hard-blocked for Completed sessions (AssessmentAdminController.cs:1962 "Cannot edit completed assessments"), and ValidUntil is otherwise only set at creation (lines 1269/1447/1939) â€” so there is no targeted admin path to restore a single Completed session's ValidUntil. The regrade is genuinely reachable on Completed sessions via the EditAssessmentAnswer flow (AssessmentAdminController.cs:3150), so Passâ†’Failâ†’Pass via repeated answer/answer-key edits permanently yields a valid cert number with null expiry. Downstream impact is mostly as claimed but with one correction: HomeController.cs:215 and CMPController.cs:2618/2764/3001 all require ValidUntil.HasValue, so the cert silently drops from expiry/expiring-soon tracking (claim correct). However the RenewalController listing query (RenewalController.cs:54-55) does NOT filter on ValidUntil.HasValue â€” the cert still appears there, and DeriveCertificateStatus (CertificationManagementViewModel.cs:58-59) maps a null non-Permanent ValidUntil to Expired ("needs renewal"), so the "disappears from renewal listing" and "treated as never-expiring" sub-claims are inaccurate (it is flagged Expired, not invisible/never-expiring). This refines, but does not refute, the underlying data-integrity defect.

---

### [ESS:ESS-01] Blank (unanswered) essay bypasses the finalize completeness check and is silently scored 0 with no way to grade it

| | |
|---|---|
| **Severity** | MED (klaim: HIGH) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Essay manual-grade lifecycle (Submit/Finalize/Recompute) |

**Masalah:** Essay text answers are persisted ONLY through the SignalR hub AssessmentHub.SaveTextAnswer (Hubs/AssessmentHub.cs:134-178), which fires on textarea blur/debounce. SubmitExam does NOT persist essay answers from the form (Controllers/CMPController.cs:1670 â€” `// Essay: scored manually by HC (EssayScore), skip here`). Therefore if a worker submits an exam without ever typing into an essay (or if SignalR disconnected), NO PackageUserResponse row exists for that essay question. The session still transitions to PendingGrading because hasEssay is derived from the question set, not from responses (GradingService.cs:136). FinalizeEssayGrading's completeness gate only inspects rows that EXIST: it loads essayResponses filtered to existing rows and checks `essayResponses.Any(r => r.EssayScore == null)` (AssessmentAdminController.cs:3518-3524). A question with no row is invisible to this check, so the gate passes and finalize proceeds. In AssessmentScoreAggregator.Compute the blank essay still adds q.ScoreValue to maxScore (Helpers/AssessmentScoreAggregator.cs:35) but contributes 0 to totalScore because FirstOrDefault finds no row (lines 53-54) â€” so the worker is penalized in the denominator for an essay nobody could grade. Worse, HC literally CANNOT grade it: SubmitEssayScore loads the response via FirstOrDefaultAsync and returns 'Jawaban tidak ditemukan' when the row is null (AssessmentAdminController.cs:3431-3434), even though the grading view renders the essay row (lines 3403-3414, TextAnswer=null/EssayScore=null).

**Dampak:** A worker who skips an essay gets it scored as a hard 0 against the total possible score, lowering their percentage and potentially flipping IsPassed to false, with no recourse â€” HC cannot enter a score for it (the grade endpoint rejects it) yet finalize allows completion. Mis-scored, un-correctable assessment results.

**Bukti:** AssessmentAdminController.cs:3518-3524 (completeness gate over existing rows only); :3431-3434 (SubmitEssayScore returns 'Jawaban tidak ditemukan' for missing row); :3403-3414 (grading view enumerates ALL essay questions); Helpers/AssessmentScoreAggregator.cs:35 + 52-55 (maxScore includes blank essay, totalScore gets 0); CMPController.cs:1670 (essay not saved at submit); GradingService.cs:136 (hasEssay from question set); Hubs/AssessmentHub.cs:134-178 (hub is sole persistence path for essay text)

**Usulan fix:** Derive the essay completeness set from the question list (essayQuestions), not from existing responses: for each essay question id, require an existing response row with EssayScore != null; treat a missing row as ungraded and block finalize (or auto-create a 0-score row only after explicit HC action). Correspondingly, make SubmitEssayScore upsert (create the PackageUserResponse row if absent) so HC can grade a blank essay.

**Verifikasi adversarial:** Verified the full chain against actual code. (1) Essay text is persisted ONLY via the SignalR hub SaveTextAnswer (Hubs/AssessmentHub.cs:134-182, upsert keyed on session+question). SubmitExam explicitly skips essay persistence (CMPController.cs:1670 `// Essay: scored manually by HC (EssayScore), skip here`). No code path pre-seeds essay response rows â€” the only PackageUserResponse inserts are CMPController:380 (MC SaveAnswer, only when an option is chosen), CMPController:1650 (MC upsert at submit), AssessmentAdminController:3107 (admin edit of selected options), and the two hub inserts (172/242). So an essay the worker never typed into has NO row. (2) GradingService.cs:136 derives `hasEssay = packageQuestions.Any(q => QuestionType==\"Essay\")` from the question set, and lines 197-211 set Status=PendingGrading on that basis alone â€” independent of whether any essay response row exists. (3) FinalizeEssayGrading's completeness gate (AssessmentAdminController.cs:3518-3524) loads essayResponses filtered to EXISTING rows and checks `essayResponses.Any(r => r.EssayScore == null)`; a question with no row is invisible, so the gate passes and finalize proceeds (3563-3569). (4) AssessmentScoreAggregator.Compute adds q.ScoreValue to maxScore for the blank essay (Helpers/AssessmentScoreAggregator.cs:35) but its FirstOrDefault finds no row so totalScore gets 0 (52-54) â€” denominator penalty confirmed; this is the same helper used by both finalize and the recompute endpoint. (5) HC cannot grade it: the grading view renders a card + score input + Simpan button for EVERY essay question including blank ones (controller 3403-3414 enumerates all essayQs with TextAnswer/EssayScore null; view Views/Admin/AssessmentMonitoringDetail.cshtml:403-442 shows \"(tidak ada jawaban)\"/\"Belum Dinilai\" and a number input), but SubmitEssayScore loads the row via FirstOrDefaultAsync and returns \"Jawaban tidak ditemukan\" when null (3431-3434), and the JS alerts that message and never creates a row (cshtml:1497-1510). REFUTATION ATTEMPT: there IS a submit-time completeness guard (CMPController.cs:1561-1587) that blocks an unanswered essay â€” BUT only when `!isAutoSubmit && !serverTimerExpired` (line 1561). On timer expiry / client auto-submit the guard is bypassed, which is exactly the realistic unanswered-essay case (worker runs out of time). So the defect is reachable in normal operation; the guard only partially mitigates (manual submit with time left). Net effect: the blank essay is scored a hard 0 in the denominator and is un-correctable in-app, lowering percentage and potentially flipping IsPassed when essay weight is high.

---

### [ESS:ESS-03] FinalizeEssayGrading returns stale/null NomorSertifikat in the AJAX response after a certificate is freshly generated

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | bug |
| **Confidence** | high |
| **Area** | Essay manual-grade lifecycle (Submit/Finalize/Recompute) |

**Masalah:** The session entity is loaded tracked via FindAsync at AssessmentAdminController.cs:3471. The certificate number is written with ExecuteUpdateAsync at :3608-3611, which bypasses the EF change tracker and does NOT update the in-memory entity. At :3652 the code calls `await _context.AssessmentSessions.FindAsync(sessionId)` again â€” but FindAsync returns the already-tracked instance from the local cache (no DB round-trip), so updatedSession.NomorSertifikat is still its load-time value (null for a session that had no cert before). The JSON response at :3656-3662 returns `nomorSertifikat = updatedSession?.NomorSertifikat`, i.e. null, even though a certificate number was just persisted. (score and isPassed in the response are fine â€” they use the in-memory computed finalPercentage/isPassed.)

**Dampak:** Immediately after HC finalizes an essay session that passes and is configured to issue a certificate, the success response shows no certificate number; HC must refresh to see it. Confusing UX and any client logic that surfaces the cert number from this response will be wrong on first finalize.

**Bukti:** AssessmentAdminController.cs:3471 (FindAsync tracked load), :3601-3614 (cert written via ExecuteUpdateAsync, in-memory entity untouched), :3652 (FindAsync returns cached stale instance), :3661 (nomorSertifikat read from stale entity)

**Usulan fix:** Reload with AsNoTracking before returning (e.g. `_context.AssessmentSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId)`) or call _context.Entry(session).ReloadAsync() after the ExecuteUpdate writes, then read NomorSertifikat from the fresh value.

**Verifikasi adversarial:** Confirmed from actual code in Controllers/AssessmentAdminController.cs (FinalizeEssayGrading, lines 3469-3663):

1. Line 3471: `var session = await _context.AssessmentSessions.FindAsync(sessionId);` performs a TRACKED load. The entity enters the identity map with its load-time NomorSertifikat (null for a session with no prior cert).
2. Lines 3601-3614: cert number persisted via `_context.AssessmentSessions.Where(s => s.Id==sessionId && s.NomorSertifikat==null).ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, ...))`. ExecuteUpdateAsync is a direct SQL UPDATE that bypasses the change tracker, so the in-memory tracked `session` is NOT updated.
3. Line 3652: `var updatedSession = await _context.AssessmentSessions.FindAsync(sessionId);`. Per EF Core semantics, FindAsync checks the identity map FIRST and returns the already-tracked instance with NO DB round-trip when an entity with that key is tracked. Same PK is already tracked from line 3471, so updatedSession IS that stale instance â€” NomorSertifikat still null.
4. Line 3661: `nomorSertifikat = updatedSession?.NomorSertifikat` returns null even though a cert number was just persisted.

Tried hard to refute and found no mitigation:
- No global NoTracking override: Data/ApplicationDbContext.cs and Program.cs have no UseQueryTrackingBehavior; codebase CONCERNS.md confirms "Near-Zero AsNoTracking Usage" = default TrackAll. So the tracked-instance return on line 3652 holds.
- No `.Reload()`, `Entry().Reload()`, or `ChangeTracker.Clear()` anywhere in the method between the two FindAsync calls (grep returned no matches).
- The proton services called in between (ProtonBypassService.MarkPendingReadyIfAnyAsync at Services/ProtonBypassService.cs:626 touches only PendingProtonBypasses/Users; ProtonCompletionService.EnsureAsync touches proton markers) never query/refresh the AssessmentSession entity; and even if they did, identity-map resolution returns the stale tracked instance rather than overwriting scalars.
- The contrast at the race-loss branch (lines 3574-3593) deliberately uses `AsNoTracking().FirstOrDefaultAsync` to read FRESH DB state for its response â€” demonstrating fresh reads are possible, but the success path at 3652 does not do this.
- Phase 310 plan (.planning/.../310-01-PLAN.md Warning #10) explicitly documents the `updatedSession?.NomorSertifikat` dependency and instructed preserving the block verbatim, without noticing the staleness â€” so the bug was carried forward, not fixed.

score and isPassed in the JSON are fine (they use computed finalPercentage/isPassed, not the entity), exactly as the finding states. Only nomorSertifikat is affected. Impact: on first finalize of a passing, cert-issuing essay session, the success response shows no certificate number; a page refresh (which re-reads from DB) shows it. UX/contract bug, no data corruption.

---

### [GAIN:GAIN-01] SessionElemenTeknisScores never recomputed after essay grading â†’ per-ET breakdown & gain understated for essay assessments

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Pre/Post gain-score + per-ElemenTeknis scoring |

**Masalah:** At submit time, GradingService computes SessionElemenTeknisScores per ElemenTeknis group with etTotal = etGroup.Count() (counts ALL questions, including Essay) but Essay questions are explicitly skipped from etCorrect (Services/GradingService.cs:145, 169-171). For essay-containing assessments the session goes to PendingGrading with essay contributing 0 to CorrectCount. When the essay is later graded, FinalizeEssayGrading recomputes only the scalar session Score/IsPassed via AssessmentScoreAggregator (Controllers/AssessmentAdminController.cs:3555-3569) and the repair endpoint RecomputeEssayScores likewise updates only Score+IsPassed (AssessmentAdminController.cs:3959-3963). Neither path ever deletes/re-inserts SessionElemenTeknisScores. AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:26-60) returns only a scalar ScoreAggregateResult â€” no ET breakdown â€” so the ET rows stay frozen at the submit-time values where each ET group containing an essay has CorrectCount missing the essay's correctness. Result: for any ET group that includes an essay question, CorrectCount/QuestionCount permanently understates the true result even after a fully-correct essay is graded.

**Dampak:** On the Results page Pre/Post comparison, the gain-per-elemen view, the ET breakdown dashboard, and the Gain/ItemAnalysis Excel exports, any ElemenTeknis containing an essay question shows a lower-than-actual post score and therefore an artificially low (or even negative) normalized gain â€” even though the worker scored full marks on the essay and the session-level Score is correct. This produces a visible inconsistency between the session score and the per-ET breakdown and misrepresents competency improvement for essay-bearing pre/post assessments.

**Bukti:** Services/GradingService.cs:142-181 (etTotal=etGroup.Count() at 145 includes essays; Essay skipped at 169-171, never increments etCorrect); SessionElemenTeknisScore inserted once at 174-180. Controllers/AssessmentAdminController.cs:3555-3569 (FinalizeEssayGrading sets only Score/Status/IsPassed/CompletedAt â€” no ET touch) and 3959-3963 (RecomputeEssayScores sets only Score/IsPassed). Helpers/AssessmentScoreAggregator.cs:52-60 (Essay adds to scalar totalScore only; result has no ET data). Consumers of the stale ET rows: per-ET comparison table CMPController.cs:2375-2403 (postScore = post.CorrectCount/post.QuestionCount*100), gain-per-elemen CMPController.cs:3409-3419, ET dashboard breakdown CMPController.cs:2575-2586, Excel exports CMPController.cs:3601-3631 and AssessmentAdminController.cs:4607-4608/4877.

**Usulan fix:** Extend AssessmentScoreAggregator (or add a companion) to also emit per-ElemenTeknis CorrectCount/QuestionCount accounting for graded essays (an essay counts toward CorrectCount when EssayScore >= the group's threshold, or use proportional EssayScore/ScoreValue), and have FinalizeEssayGrading and RecomputeEssayScores delete the existing SessionElemenTeknisScores for the session and re-insert the recomputed rows inside the same update, mirroring RegradeAfterEditAsync which already does RemoveRange+AddRange (GradingService.cs:453). Alternatively, exclude essay questions from QuestionCount in the ET aggregation so ungraded/graded essays do not depress the ratio.

**Verifikasi adversarial:** Confirmed against actual code.

SUBMIT-TIME ET SCORING (the bug source): GradingService.cs:142-181 groups questions by ElemenTeknis, sets etTotal=etGroup.Count() (line 145, counts ALL questions including Essay), but the Essay case (lines 169-171) is an empty break â€” never increments etCorrect. The row is inserted once at 174-180 with CorrectCount=etCorrect, QuestionCount=etTotal. The duplicate submit path ComputeScoreAndETInternalAsync (GradingService.cs:400-425) behaves identically (essay break at 415-416). ElemenTeknis is a property on PackageQuestion (Models/AssessmentPackage.cs:51) independent of QuestionType (line 48), so an Essay can be tagged into an ET group â€” making the understated-CorrectCount scenario reachable.

ESSAY-GRADING PATHS NEVER RECOMPUTE ET: FinalizeEssayGrading (AssessmentAdminController.cs:3555-3569) calls AssessmentScoreAggregator.Compute and ExecuteUpdateAsync sets only Score/Status/IsPassed/CompletedAt â€” no SessionElemenTeknisScore touch. RecomputeEssayScores repair endpoint (3951-3963) likewise sets only Score+IsPassed. AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:26-60) returns only a scalar ScoreAggregateResult(TotalScore,MaxScore,Percentage,IsPassed) â€” no ET breakdown. So after essay grading the ET rows stay frozen at submit-time values.

REFUTATION ATTEMPTS, all failed:
1. RegradeAfterEditAsync (GradingService.cs:437-453) DOES delete+reinsert ET via ExecuteDeleteAsync (446) + AddRange (453), but its only caller is the Admin edit-answers flow (AssessmentAdminController.cs:3150), NOT essay grading; and it still skips Essay in ET counting (415-416), so it would not fix essay correctness anyway.
2. ResetAssessment (AssessmentAdminController.cs:4086-4090) RemoveRange of ET is the retake-cleanup path, not essay grading.
3. The only essay-related guard is gainPending = assessment.HasManualGrading && assessment.IsPassed == null (CMPController.cs:2361). It suppresses the single-session Results-page gain column only WHILE essay is ungraded; once FinalizeEssayGrading sets IsPassed (3568), gainPending becomes false and the gain is computed from the stale ET rows (postScore = post.CorrectCount/post.QuestionCount*100 at 2380-2381). It does not save the finding.

CONSUMERS verified reading the stale rows with no recompute: Results Pre/Post table CMPController.cs:2380-2381; gain-per-elemen CMPController.cs:3409-3419; ET dashboard breakdown CMPController.cs:2581-2583 (no gainPending guard at all here â€” aggregates across sessions regardless of grading state); Gain Excel export CMPController.cs:3601-3631. All compute CorrectCount/QuestionCount*100, so any ET group containing a fully-correct graded essay shows a permanently lower-than-actual post score and understated/possibly-negative normalized gain, inconsistent with the corrected session-level Score.

Severity MED is appropriate: correctness defect on analytics/reporting surfaces (per-ET breakdown, gain, exports), not a security or data-loss issue; session-level Score is correct. Only nuance: the 'even negative' impact wording is a slight overstatement (negative gain requires the understated post to fall below pre), but the core understatement claim is correct.

---

### [MAN:MAN-01] BulkBackfill: ToDictionaryAsync on non-unique NIP throws unhandled exception (500)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** In BulkBackfillAssessment the user lookup builds a dictionary keyed by NIP: `await _context.Users.Where(u => u.NIP != null && nips.Contains(u.NIP)).ToDictionaryAsync(u => u.NIP!)` (line 890-892). NIP has NO unique constraint anywhere â€” it is a plain nullable string on ApplicationUser (Models/ApplicationUser.cs:18) with no HasIndex/IsUnique in Data/ApplicationDbContext.cs. If two AspNetUsers share the same NIP, ToDictionaryAsync throws ArgumentException (duplicate key). Critically, this lookup sits at lines 888-899, OUTSIDE both the parse try/catch (ends line 880) and the transaction try (starts line 906), so the exception is unhandled and surfaces as a raw 500. ImportTraining's analogous lookup uses FirstOrDefaultAsync(u => u.NIP == nip) (line 1334) which tolerates duplicate NIPs â€” so the two import paths diverge in robustness.

**Dampak:** An admin uploading a backfill Excel containing a NIP that is duplicated in AspNetUsers gets an unhandled server error (500) with no friendly message, instead of the all-or-nothing rollback the UI promises. Duplicate/legacy NIPs are plausible since uniqueness is never enforced.

**Bukti:** Controllers/TrainingAdminController.cs:888-899 (lookup outside try/catch); :890-892 `.ToDictionaryAsync(u => u.NIP!)`; Models/ApplicationUser.cs:18 `public string? NIP`; Data/ApplicationDbContext.cs:225-229 (only NomorSertifikat has a unique index â€” no NIP index); contrast Controllers/TrainingAdminController.cs:1334 FirstOrDefaultAsync in ImportTraining.

**Usulan fix:** Replace ToDictionaryAsync(u => u.NIP!) with a duplicate-safe grouping (e.g. GroupBy(u => u.NIP).ToDictionary(g => g.Key, g => g.First()) with an explicit error report if any NIP maps to >1 user), or move the lookup inside the transaction try and surface a TempData error. Optionally add a filtered unique index on NIP.

**Verifikasi adversarial:** Confirmed all claimed code. TrainingAdminController.cs:890-892 builds the user lookup via `await _context.Users.Where(u => u.NIP != null && nips.Contains(u.NIP)).ToDictionaryAsync(u => u.NIP!)` â€” ToDictionaryAsync throws ArgumentException on a duplicate key. NIP uniqueness is NOT enforced: Models/ApplicationUser.cs:18 is a plain `public string? NIP { get; set; }`, and a grep over Data/ApplicationDbContext.cs shows no HasIndex/IsUnique on NIP anywhere (only NomorSertifikat:226-227, Label:147, ElemenTeknis, OrgUnit, etc. are unique). The lookup sits OUTSIDE protection: the BulkBackfillAssessment method (starts line 836) has only inline `if` validations and no outer try; the parse try/catch ends at line 880 and the transaction try (BeginTransactionAsync) starts at line 905, so lines 888-899 are unwrapped. Thus duplicate NIPs in AspNetUsers that appear in the uploaded Excel cause an unhandled ArgumentException. The sibling path ImportTraining at line 1334 uses FirstOrDefaultAsync(u => u.NIP == nip), which is duplicate-tolerant â€” confirmed divergence. REFUTATION attempt: the only mitigation is Program.cs:167 `app.UseExceptionHandler("/Home/Error")` + :172 UseStatusCodePagesWithReExecute, which in production redirects the unhandled exception to a generic /Home/Error page rather than a raw stack-trace 500. That softens the 'raw 500' framing but does NOT refute the defect: the exception is still unhandled at the action level, the admin gets a generic error page instead of the specific actionable message (vs the line-897 'NIP tidak ditemukan' style), and the all-or-nothing contract message is not delivered. Data integrity is intact because the throw precedes BeginTransactionAsync (nothing is written). Net: real robustness/UX defect, requiring the plausible-but-uncommon precondition of duplicate NIPs which is never enforced against.

---

### [MAN:MAN-02] Manual entry IsPassed is free admin input fully decoupled from Score/PassPercentage, defaulting to Lulus

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** For AddManualAssessment, IsPassed is taken verbatim from the form toggle (Controllers/TrainingAdminController.cs:746 `IsPassed = model.IsPassed`) with no server-side reconciliation against Score and PassPercentage. The view model defaults IsPassed=true (Models/CreateManualAssessmentViewModel.cs:25) and the toggle renders pre-checked (Views/Admin/AddManualAssessment.cshtml:189 `checked`). Score is optional and PassPercentage defaults 70, but nothing validates IsPassed against them. EditManualAssessment behaves identically (line 1060). ImportTraining derives IsPassed from the literal 'Ya' string (line 1337), again ignoring the numeric Score. Only BulkBackfill computes IsPassed=Score>=passPercentage (line 941). Downstream, certificate download and all CMP/CDP/Home/Renewal cert counting gate on IsPassed==true (e.g. CMPController.cs:1792, 1894, 2616, 3000; HomeController.cs:123,215), so IsPassed is the single safety net for certificate eligibility â€” and for manual entries it is an unvalidated checkbox.

**Dampak:** An admin who enters a worker with a sub-threshold Score (e.g. 10 with PassPercentage 70) but leaves the default 'Lulus' toggle ON records the worker as passed and triggers a downloadable certificate, fully bypassing grading. Since the toggle defaults to Lulus, this is easy to do by omission, inflating competency statistics and issuing certificates for failing scores.

**Bukti:** Controllers/TrainingAdminController.cs:744-746,759 (Score/PassPercentage/IsPassed copied from model, GenerateCertificate=true); Models/CreateManualAssessmentViewModel.cs:25 default IsPassed=true; Views/Admin/AddManualAssessment.cshtml:189 toggle pre-`checked`, :162/:176 Score+PassPercentage free; Controllers/TrainingAdminController.cs:1060 (Edit), :1337 (Import 'Ya'); cert guard CMPController.cs:1792,1894.

**Usulan fix:** On the server, when Score and PassPercentage are both present, validate/auto-set IsPassed = Score >= PassPercentage (or at minimum reject a submission where IsPassed=true but Score<PassPercentage unless an explicit override reason is supplied). Apply the same reconciliation to ImportTraining and EditManualAssessment for consistency with BulkBackfill.

**Verifikasi adversarial:** Verified every cited line against the actual code; all claims hold.

ADD path (Controllers/TrainingAdminController.cs:739-762): the AssessmentSession is built with `Score = model.Score` (744), `PassPercentage = model.PassPercentage` (745), `IsPassed = model.IsPassed` (746) â€” IsPassed copied verbatim from the form with NO reconciliation against Score/PassPercentage. `GenerateCertificate = true` is hardcoded (759). The only validation before this is ModelState (711), file-upload validation (704), and a duplicate guard (722) â€” none touch IsPassed-vs-Score consistency.

Model defaults (Models/CreateManualAssessmentViewModel.cs): `IsPassed { get; set; } = true` (25), `Score` is `int?` optional (18), `PassPercentage = 70` (22). EditManualAssessmentViewModel has IsPassed with no `= true` (88) but the toggle still renders pre-checked is only relevant to Add.

VIEW (Views/Admin/AddManualAssessment.cshtml:189): `<input asp-for="IsPassed" ... checked />` â€” pre-checked Lulus. Score (162) and PassPercentage (176) are free numeric inputs. I also read the JS (lines 395-430): the passSwitch `change` handler only updates a visual label and a preview string; there is NO handler that auto-derives the toggle from Score>=PassPercentage. The preview happily renders "10 / Lulus / Batas 70%" with no enforcement. So the toggle is genuinely decoupled and free.

EDIT path (line 1056-1069): `session.IsPassed = model.IsPassed` (1060) verbatim, same decoupling, identical behavior.

IMPORT path (line 1337): `bool isPassed = lulusStr.Equals("Ya", StringComparison.OrdinalIgnoreCase)` derived purely from the Excel 'Ya' string; `score` parsed independently (1338); session sets `IsPassed = isPassed` (1359), `PassPercentage = 70` (1358) â€” score never compared to threshold.

BULKBACKFILL (line 941): `IsPassed = row.Score >= passPercentage` â€” the ONLY path that computes IsPassed from Score, confirming the finding's contrast claim.

Downstream gate is the single safety net: cert download in CMPController.cs gates only on `!assessment.GenerateCertificate` (1788/1892) and `assessment.IsPassed != true` (1792/1894) â€” never re-checks Score vs PassPercentage. HomeController.cs counts certs on `GenerateCertificate && IsPassed == true` (123, 215) and renewal/cert counts gate on `IsPassed == true` (95, 101, 183, 191). So a manual entry with sub-threshold Score and the default Lulus toggle ON is recorded as passed, becomes cert-eligible (GenerateCertificate=true), and inflates competency stats. No guard elsewhere refutes this.

Severity: MED is appropriate, not HIGH â€” exploitation requires an authenticated Admin (Authorize Roles=Admin on these endpoints), so it is a data-integrity/process-control weakness (trusted-but-unvalidated admin input that defaults to the unsafe value by omission), not an external-attacker bypass. Impact is real: certificate issuance for failing scores and skewed statistics.

---

### [MAN:MAN-03] Manual-entry duplicate guard is TOCTOU-only with no backing DB unique constraint

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | race |
| **Confidence** | high |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** The duplicate guard for all manual-entry paths is purely application-level: AnyAsync(ManualDuplicatePredicate(...)) where the predicate is UserId+Title+CompletedAt+IsManualEntry (Controllers/AdminBaseController.cs:265-267). AddManualAssessment checks it pre-loop (line 722) then inserts in a separate SaveChanges (line 765); ImportTraining checks per-row (line 1342). There is NO unique index on (UserId,Title,CompletedAt,IsManualEntry) â€” Data/ApplicationDbContext.cs only declares a unique filtered index on NomorSertifikat (lines 225-229), and NomorSertifikat is optional/null for manual entries. Two concurrent admin submissions (or two near-simultaneous Import runs) can both pass the AnyAsync check and both insert, producing duplicate manual assessment records (and thus duplicate certificate entries) for the same worker/title/date.

**Dampak:** Under concurrency the dedup guard can be defeated, creating duplicate manual assessment records / duplicate certificates for one event. With null NomorSertifikat there is no DB-level backstop to catch it.

**Bukti:** Controllers/AdminBaseController.cs:265-267 (predicate); Controllers/TrainingAdminController.cs:722 (AddManual pre-check), :765 (separate save), :1342 (Import per-row check); Data/ApplicationDbContext.cs:225-229 (only NomorSertifikat unique â€” no composite manual-dedup index).

**Usulan fix:** Add a filtered unique index on (UserId, Title, CompletedAt) WHERE IsManualEntry = 1 so the DB enforces the invariant, and catch DbUpdateException to report 'duplikat' gracefully. The application AnyAsync check can remain as the friendly fast-path.

**Verifikasi adversarial:** All load-bearing claims confirmed against the code.

1) Predicate is exactly as cited â€” AdminBaseController.cs:265-267: `s => s.UserId == userId && s.Title == title && s.CompletedAt == completedAt && s.IsManualEntry`. This is the single source for all manual-entry dedup.

2) AddManualAssessment (TrainingAdminController.cs:685-774): pre-loop `await _context.AssessmentSessions.AnyAsync(ManualDuplicatePredicate(...))` at :722, then a separate `_context.AssessmentSessions.Add(session)` loop ending in a single `await _context.SaveChangesAsync()` at :765. No transaction, no lock â€” classic check-then-act TOCTOU.

3) ImportTraining (TrainingAdminController.cs:1307+): per-row `AnyAsync(ManualDuplicatePredicate(...))` at :1342, SaveChanges per row. NomorSertifikat set to null when blank (:1367).

4) No DB backstop. ApplicationDbContext.cs:225-229 declares only the NomorSertifikat filtered unique index (IX_AssessmentSessions_NomorSertifikat_Unique, HasFilter [NomorSertifikat] IS NOT NULL). The full AssessmentSession index enumeration in ApplicationDbContextModelSnapshot.cs:363-546 lists every index (AccessToken, ExamWindowCloseDate, LinkedGroupId, NomorSertifikat-unique, RenewsSessionId, RenewsTrainingId, Schedule, UserId, {UserId,Status}) â€” none is a composite unique on (UserId,Title,CompletedAt,IsManualEntry). No migration creates such an index (grepped all Migrations for unique:true and Title/CompletedAt/IsManualEntry combos â€” none). For manual entries NomorSertifikat can be null, so even the one unique index provides no backstop.

Searched for any concurrency guard that would refute the finding: grep for BeginTransaction/Serializable/IsolationLevel/UPDLOCK/HOLDLOCK/TransactionScope in TrainingAdminController.cs returns only ONE hit, line 905 â€” and that belongs to BulkBackfill (a third manual path the finding did not even cite), not AddManualAssessment or ImportTraining. Moreover that transaction is opened with default isolation (BeginTransactionAsync() with no IsolationLevel = READ COMMITTED), which makes the batch atomic but does NOT take range locks and therefore does NOT close the TOCTOU window against a concurrent insert. So no guard elsewhere refutes the claim.

Conclusion: the defect is genuine â€” application-level check-then-act with no DB-level unique constraint and no serializable/range-lock protection on any of the three manual-entry paths; concurrent submissions can both pass AnyAsync and both insert duplicates.

Severity calibration: the trigger requires two near-simultaneous human-driven admin/HC submissions (or import runs) for the same worker+title+exact-date â€” a narrow, low-frequency, non-attacker-controlled window producing duplicate records rather than auth bypass/data loss. MED is defensible; practical exploitability leans LOW. I keep MED as the defect (missing DB backstop) is real and the impact (duplicate certificate records) is data-integrity, but flag it as borderline MED/LOW.

---

### [OPS:OPS-02] Group-complete notification dedup uses Message.Contains(Title) â€” substring titles cause false-positive dedup (suppressed notifications)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | medium |
| **Area** | Notifications + live monitoring + audit-log + impersonation |

**Masalah:** WorkerDataService.NotifyIfGroupCompleted dedups by checking whether an existing UserNotification's Message Contains the completed session's Title (the schema lacks SourceTitle/SourceDate, so a substring match on Message is used as the dedup key). The sent message format is `Semua peserta assessment "{Title}" telah menyelesaikan ujian`. Because the dedup is a substring Contains, an assessment whose Title is a substring of another assessment's Title will be wrongly deduped. Example: assessment titled "Safety" completes; a recent (within the 2-day window) notification for "Safety OJT" already exists; `n.Message.Contains("Safety")` returns true, so the legitimate "Safety" group-complete notification is suppressed for all HC/Admin recipients. Same class of false-match also occurs whenever titles share a leading prefix.

**Dampak:** HC/Admin can silently miss a 'group completed' notification for an assessment whose title is a prefix/substring of another recently-completed assessment, delaying grading/follow-up. The miss is silent (logged only as a skip).

**Bukti:** Services/WorkerDataService.cs:456-461 (`bool alreadySent = await _context.UserNotifications.AnyAsync(n => n.UserId == recipientId && n.Type == "ASMT_ALL_COMPLETED" && n.Title == "Assessment Selesai" && n.Message.Contains(completedSession.Title) && n.CreatedAt >= windowStart);`) combined with the message template at Services/WorkerDataService.cs:477 (`$"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian"`).

**Usulan fix:** Make the dedup key exact rather than substring: match the full rendered message (`n.Message == $"Semua peserta assessment \"{title}\"..."`) or compare the quoted title token, or persist a structured dedup key (e.g., a SourceKey column or include the session/group id) instead of relying on Message.Contains.

**Verifikasi adversarial:** Confirmed against actual code. Services/WorkerDataService.cs:456-461 is the only NotifyIfGroupCompleted (verified via Grep â€” single method, no overload, single Message.Contains dedup site). The dedup predicate is `n.UserId==recipientId && n.Type=="ASMT_ALL_COMPLETED" && n.Title=="Assessment Selesai" && n.Message.Contains(completedSession.Title) && n.CreatedAt >= windowStart` where windowStart = UtcNow.AddDays(-2) (line 455). The sent message (line 477) is `Semua peserta assessment "{completedSession.Title}" telah menyelesaikan ujian`, so an existing notification for a LONGER title contains the substring of a SHORTER completing title. The in-code comment (lines 451-452) confirms the design intent: "Schema: UserNotifications TIDAK punya SourceTitle/SourceDate, jadi pakai ... Message.Contains(sessionTitle)" â€” a deliberate substring workaround, not exact match. Title is free-text (Models/AssessmentSession.cs:13, `public string Title { get; set; } = ""`) with no uniqueness/format/length-prefix constraint, so "Safety" vs "Safety OJT" is realistic. No refuting guard exists: the dedup is NOT scoped by Category or Schedule.Date (those only gate the sibling all-completed check at lines 433-441), so a same-window notification for a substring-superset title from ANY category/date suppresses the legitimate one. The suppression is silent (LogInformation skip, lines 465-468). I tried to refute via: (a) an exact-match guard elsewhere â€” none found; (b) quote-wrapping in the dedup that would prevent substring match â€” the dedup uses the raw title without quotes, so it matches inside the quoted message; (c) a uniqueness constraint on Title â€” none in the model. All refutation attempts failed. One legitimate constraint on impact: the false-match is directional (only the shorter, later-completing title is suppressed by an earlier longer-title notification; the reverse does not match) and requires a 2-day timing window plus a substring/prefix title collision â€” which keeps it MED, not HIGH.

---

### [PEL:PEL-02] Pre-Post Test path bypasses the Proton eligibility gate entirely (no server-side Category/Type guard)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Proton cross-year eligibility / completion gating |

**Masalah:** isPrePostMode is determined solely from AssessmentTypeInput == "PrePostTest" (line 916), independent of Category. The Pre-Post branch builds and saves Pre/Post sessions and RETURNS (RedirectToAction at line 1328) BEFORE the eligibility gate at line 1342 is reached. There is no server-side validation rejecting the combination Category=="Assessment Proton" + AssessmentTypeInput=="PrePostTest"; the only AssessmentTypeInput validation (lines 1015-1019) just checks the value is one of {Standard, PrePostTest}. The Category==Proton -> force Standard restriction exists ONLY in client JS (CreateAssessment.cshtml:1322-1355), not on the server.

**Dampak:** A crafted POST (or a JS-disabled/desynced UI state) with Category=Assessment Proton, a valid ProtonTrackId, and AssessmentTypeInput=PrePostTest creates Pre+Post sessions for ALL submitted UserIds with no cross-year or 100%-deliverable gating, and with ProtonTrackId/TahunKe unset. Requires Admin/HC, so this is a privileged data-integrity / gate-bypass rather than privilege escalation.

**Bukti:** AssessmentAdminController.cs:916 `bool isPrePostMode = AssessmentTypeInput == "PrePostTest";`; Pre-Post branch 1212-1338 returns at 1328 `return RedirectToAction("CreateAssessment");` before the gate at 1342. preSession/postSession builders (1218-1239, 1254-1279) set no ProtonTrackId/TahunKe. Client-only enforcement at Views/Admin/CreateAssessment.cshtml:1348-1355 (`ts.value='Standard'; ts.disabled=true`).

**Usulan fix:** Add a server-side guard early in the POST: if model.Category == "Assessment Proton" && isPrePostMode -> ModelState error 'Assessment Proton hanya mendukung tipe Standard.' (mirror the JS restriction server-side).

**Verifikasi adversarial:** Verified all load-bearing claims in AssessmentAdminController.cs. Line 916: `bool isPrePostMode = AssessmentTypeInput == "PrePostTest";` is Category-independent. The Pre-Post branch (1212-1339) builds preSessions (1218-1239) and postSessions (1254-1279) and, on success, returns at line 1328 (`return RedirectToAction("CreateAssessment");`) BEFORE the Proton eligibility gate at 1342-1407 is ever reached. Both session builders set NO ProtonTrackId and NO TahunKe; AssessmentSession.cs:102/108 confirms both are nullable, so they default to null. The standard branch (1457-1465) DOES set session.ProtonTrackId/TahunKe for Proton AND runs the cross-year + 100%-deliverable gate (1346-1407) â€” proving the asymmetry. The only AssessmentTypeInput validation (1015-1019) merely checks the value is Standard|PrePostTest; a whole-controller grep for any Proton+PrePost guard found none (matches at 859/872 are unrelated naming-convention rules). The Proton-metadata lookup that runs before the PrePost branch (1168-1194) only validates the track exists, it does not reject the combination. The Category==Proton -> force Standard restriction is client-JS ONLY (CreateAssessment.cshtml:1320-1356: typeSelect.value='Standard'; typeSelect.disabled=true); a disabled control is not submitted and a crafted/desynced POST can send Category=Assessment Proton + valid ProtonTrackId + AssessmentTypeInput=PrePostTest. The POST is [Authorize(Roles=\"Admin, HC\")] + [ValidateAntiForgeryToken] (844-845), so exploitation requires a privileged actor â€” consistent with the finding self-scoping to data-integrity/gate-bypass, not privesc. Tried to refute via earlier guard, downstream re-check, and combination validation â€” all absent because the branch returns before any gate logic.

---

### [PEL:PEL-03] CreateAssessment gate validates prerequisites but never verifies the worker has an active assignment to the track (enforcement looser than display)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Proton cross-year eligibility / completion gating |

**Masalah:** GetEligibleCoachees (the display endpoint feeding the UI) requires an ACTIVE ProtonTrackAssignment to the track (CoachMappingController.cs:1367-1373 filters a.IsActive). The CreateAssessment enforcement gate iterates over admin-submitted UserIds (line 1374) and only checks (a) cross-year prev-year passed and (b) per-unit deliverable 100%. It has NO requirement that the user actually holds an active assignment to protonTrackId (the only ProtonTrackAssignments reference in the gate is the bypass-origin AnyAsync at line 1378). For tracks where trackHasDeliverables==false (D-08 fallback, e.g. Tahun 3 interview or interview/transition tracks) AND no cross-year prereq (Tahun 1, prevTahunKe==null), BOTH gate checks become no-ops, so an entirely unassigned worker passes the gate and receives a Proton session.

**Dampak:** Admin/HC can create Proton exam sessions for workers never assigned to the track (silent mis-inclusion), most reliably for interview/no-deliverable tracks and Tahun 1. Contradicts the documented 'eligible = assigned-to-track + 100% deliverable' contract and diverges from the display endpoint that does enforce assignment, producing inconsistent behavior between what the UI shows as eligible and what the server actually accepts.

**Bukti:** Gate loop AssessmentAdminController.cs:1373-1406 â€” no `ProtonTrackAssignments.AnyAsync(a => a.CoacheeId==uid && a.ProtonTrackId==protonTrackId && a.IsActive)` existence check; deliverable block guarded by `if (trackHasDeliverables)` (1387) so it is skipped when the track has 0 deliverables (1360-1367, D-08). Cross-year skipped when prevTahunKe==null (ProtonCompletionService.cs:152 returns true). Display contract requires assignment: CoachMappingController.cs:1367-1373.

**Usulan fix:** In the CreateAssessment gate loop, add an active-assignment existence check (skip/log workers without an active ProtonTrackAssignment to protonTrackId), mirroring GetEligibleCoachees, so enforcement matches display. Ideally share one eligibility predicate between the two call sites.

**Verifikasi adversarial:** Verified all cited code. CreateAssessment POST gate (AssessmentAdminController.cs:1344-1407) iterates admin-submitted `UserIds` (line 1374) and performs only two checks per user, then unconditionally `filtered.Add(uid)` (line 1404). The ONLY ProtonTrackAssignments reference in the loop is the bypass-origin AnyAsync (lines 1378-1380, `Origin=="Bypass"`), used to EXEMPT, not to require assignment. There is no `ProtonTrackAssignments.AnyAsync(a => a.CoacheeId==uid && a.ProtonTrackId==protonTrackId && a.IsActive)` existence check anywhere in the loop. The two checks are confirmed skippable exactly as claimed: (a) cross-year â€” line 1381-1385 calls IsPrevYearPassedAsync, which returns true when prevTahunKe is null (ProtonCompletionService.cs:152, confirmed), and prevTahunKe is null for Tahun 1 because line 1351's ternary returns null when protonUrutan <= 1; so the `if (!isRenewal && !isBypassAssignment && !true)` branch never skips. (b) deliverable 100% â€” line 1387 `if (trackHasDeliverables)` wraps the whole block; trackHasDeliverables is false when the track has 0 deliverables (lines 1361-1367, D-08), so the block is skipped. With both no-ops, an unassigned worker passes and a session is created (lines 1428-1471: AssessmentSession built with UserId/ProtonTrackId/TahunKe, no ProtonTrackAssignmentId link, no further assignment guard before AddRange/SaveChanges). Upstream validation on UserIds is existence-only (lines 1133-1162: missingUsers) plus a 50-count cap (line 910); no assignment validation exists. Confirmed the display endpoint DOES require assignment: GetEligibleCoachees (CoachMappingController.cs:1367-1373) filters ProtonTrackAssignments where IsActive, and even in its no-deliverable branch (lines 1384-1392) restricts to assignedCoacheeIds. So display enforces 'assigned-to-track' but the server enforcement gate does not â€” a genuine divergence. Endpoint is [HttpPost][Authorize(Roles=\"Admin, HC\")][ValidateAntiForgeryToken] (lines 843-845): exploitation needs the trusted Admin/HC role plus selecting/forging an unassigned UserId in the POST. Could not find any guard elsewhere that refutes the claim. Every cited line number and behavior matches.

---

### [REC:REC-01] Opt-in mirror training is deleted flat (no renewal cascade) â†’ FK NoAction violation rolls back the entire delete

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Records aggregation + cascade-delete engine |

**Masalah:** In RecordCascadeDeleteService.ExecuteAsync the validated mirror trainings are removed with a plain RemoveRange (Services/RecordCascadeDeleteService.cs:271-280) â€” they are NOT passed through CollectCascadeIds, so any renewal descendant of a mirror TrainingRecord is left untouched. The renewal FKs RenewsTrainingId/RenewsSessionId are configured DeleteBehavior.NoAction (Data/ApplicationDbContext.cs:169-180, 234-245), i.e. SQL Server rejects the DELETE while a child row still references the mirror. Because mirror deletion happens inside the same transaction as the rest of the cascade, SaveChangesAsync throws DbUpdateException, the catch at :285-290 returns the generic failure and the whole transaction rolls back.

**Dampak:** An admin who leaves a default-checked mirror candidate selected (it is checked by default â€” Views/Admin/Shared/_CascadePreviewModal.cshtml:51) cannot delete the record at all if that mirror happens to have its own renewal child: the operation fails with a generic 'constraint dilanggar / coba lagi' and nothing is deleted, leaving the admin unable to complete a legitimate delete without manually un-checking. The failure mode is opaque (generic message).

**Bukti:** Services/RecordCascadeDeleteService.cs:271-280 (mirror loop = flat _context.TrainingRecords.Remove(m), no CollectCascadeIds traversal) vs :177 (root uses CollectCascadeIds); Data/ApplicationDbContext.cs:174-180 & :239-245 (RenewsTrainingId/RenewsSessionId OnDelete NoAction â†’ blocks delete of referenced row). Contrast: root & training nodes ARE traversed, mirrors are not.

**Usulan fix:** Run mirror IDs through CollectCascadeIds('training', mirrorId) and merge their node sets into trainingNodeIds/sessionNodeIds before deletion, OR validate-and-reject mirrors that have renewal descendants with an explicit message instead of letting the whole transaction fail.

**Verifikasi adversarial:** Confirmed every link in the finding's chain from the actual code.

1. Mirror loop is flat (Services/RecordCascadeDeleteService.cs:271-280): mirrors deleted via `_context.TrainingRecords.Remove(m)` with NO CollectCascadeIds traversal. The mirror-validation (lines 187-196) explicitly requires `!trainingNodeIds.Contains(c.Id)`, so a mirror is by definition NOT a traversed cascade node and its own renewal descendants are never collected. Contrast: root traversal at line 177 uses CollectCascadeIds.

2. FK NoAction confirmed (Data/ApplicationDbContext.cs:174-180 for TrainingRecord.RenewsTrainingId/RenewsSessionId; :239-245 for AssessmentSession.RenewsTrainingId/RenewsSessionId), all `OnDelete(DeleteBehavior.NoAction)`. SQL Server rejects DELETE of a row still referenced by a child whose FK is not being deleted in the same operation.

3. Same-transaction rollback confirmed: `using var tx = BeginTransactionAsync()` (line 204), SaveChangesAsync at 282, catch at 285-290 returns generic `CascadeResult(false, ..., "Gagal menghapus record. Silakan coba lagi.")` and tx auto-rolls back on dispose. Whole operation fails, nothing deleted.

4. Default-checked mirror confirmed (Views/Admin/Shared/_CascadePreviewModal.cshtml:51 â€” `checked` attribute present).

5. A mirror CAN have a renewal child: a mirror is a real TrainingRecord (FindMirrorCandidates returns TrainingRecord, Judul = session.Title or "Assessment: "+Title). TrainingAdminController.cs:103 sets `RenewsTrainingId = src.Id` for ANY TrainingRecord selected as a renewal source, so a legacy mirror can be a renewal source, yielding a child with RenewsTrainingId == mirror.Id.

Refutation attempts failed: No pre-validation in DeleteManualAssessment/DeleteTraining removes the problem; the controller's own DbUpdateException catch (TrainingAdminController.cs:1128) only swaps in a different generic message, the delete still fails. The double-renewal guard (lines 241-247) blocks a source being renewed twice but does not stop a mirror from having one renewal child. The only app-level null-clearing on delete is for LinkedSessionId (line 237), unrelated to renewal FKs. So the FK reference genuinely persists at DELETE time and the violation is real.

Severity kept at MED, not raised: impact is bounded (legitimate delete fails opaquely, no data loss, admin recovers by un-checking the mirror) and it requires a narrow conjunction â€” a legacy mirror (#15 edge case) that matches the title/date heuristic, is left default-checked, AND was used as a renewal source. Opaque generic failure is a real data-integrity/operability defect but matches MED.

---

### [REC:REC-02] Mirror candidate heuristic (title + Â±1 day, default-checked opt-out) can hard-delete an unrelated legitimate training record

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Records aggregation + cascade-delete engine |

**Masalah:** FindMirrorCandidates matches ANY TrainingRecord of the same user whose Judul equals the session Title (or 'Assessment: '+Title) and whose Tanggal is within Â±1 day of the session Schedule (Services/RecordCascadeDeleteService.cs:154-162). This is a fuzzy heuristic, not an identity/FK link. In the preview modal these candidates render with the checkbox `checked` (opt-out, Views/Admin/Shared/_CascadePreviewModal.cshtml:51), so an admin who clicks 'Hapus Semua' without un-checking will permanently delete the matched training record â€” including its certificate file (RecordCascadeDeleteService.cs:276 collects SertifikatUrl for physical deletion). If a worker legitimately attended a separate manual training with a coincidentally identical title within a 2-day window, that real record is silently destroyed.

**Dampak:** Permanent data loss + certificate file deletion of a legitimately-distinct training record whenever its title/date coincidentally collide with a deleted assessment within Â±1 day. Opt-out (default-checked) makes accidental deletion the path of least resistance.

**Bukti:** Services/RecordCascadeDeleteService.cs:154-162 (heuristic match: Judul==Title OR 'Assessment: '+Title AND Tanggal in [Schedule-1d, Schedule+1d]); _CascadePreviewModal.cshtml:51 (`checked` default-on opt-out); ExecuteAsync:274-279 (hard Remove + cert collection for valid mirrors).

**Usulan fix:** Make mirror checkboxes default-UNCHECKED (opt-in) so an explicit admin decision is required, and/or tighten the heuristic to require date equality (or a real link column) rather than a Â±1 day window.

**Verifikasi adversarial:** Confirmed all three cited mechanisms in the actual code. (1) FindMirrorCandidates (Services/RecordCascadeDeleteService.cs:154-162) matches purely on t.UserId==session.UserId AND (t.Judul==session.Title OR t.Judul=="Assessment: "+session.Title) AND t.Tanggal in [Schedule.AddDays(-1), Schedule.AddDays(1)] â€” a fuzzy heuristic with NO FK/identity link. TrainingRecord.cs has self/session FKs (RenewsTrainingId/RenewsSessionId, lines 62-68) but the mirror path deliberately does NOT use them (those drive the cascade traversal; mirrors are the separate legacy-heuristic path). MirrorHeuristicTests.cs confirms matching is exactly user+title+date with no NomorSertifikat/Kategori/source discriminator. (2) _CascadePreviewModal.cshtml:51 renders each mirror checkbox with `checked` (default-on opt-out). (3) ExecuteAsync deletes validated mirrors via _context.TrainingRecords.Remove (lines 273-279) and collects m.SertifikatUrl into certPaths (line 276), which are physically File.Delete'd post-commit (lines 293-301). The data flow is wired end-to-end: form submits checked mirror IDs â†’ ParseMirrorTrainingIds (TrainingAdminController.cs:617-625) â†’ ExecuteAsync. Attempted refutations all failed: the only server-side guard on mirror IDs (lines 186-196) validates that the ID is a genuine FindMirrorCandidates result for a same-user in-cascade session â€” i.e. it is an IDOR/ownership check (T-367-05/23), NOT a 'true mirror vs coincidental collision' check; a legitimately distinct training with a colliding title within Â±1 day satisfies the heuristic and is therefore a 'valid' mirror that gets hard-deleted. The SECURITY register (367-SECURITY.md) addressed ownership (T-367-05/23) and deliberate-confirmation (T-367-32/AR-367-04) but never the false-positive-collision case, so no handler refutes the finding. Notably, 367-VALIDATION.md row 07-T1 shows the team chose EXACT match for the duplicate-INSERT guard 'no Â±1 hari false-positive' while knowingly keeping Â±1-day fuzzy for the preview â€” direct evidence the fuzzy match's false-positive nature was understood. Severity stays MED rather than HIGH because the data loss requires a real coincidence (same user, identical or 'Assessment: '-prefixed title, within Â±1 day of a separate manual training) AND survives an explicit admin/HC-only preview modal that lists each mirror's title+date for review; it is not NONE because the code genuinely performs an unrecoverable hard-delete + certificate-file deletion, and the default-checked opt-out makes accidental deletion the path of least resistance.

---

### [RES:RES-01] Graded essay questions always show "Salah" in answer review, ignoring awarded EssayScore

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Results / review projection + authz |

**Masalah:** In the Results review projection (CMPController.Results, package path), the per-question IsCorrect for an Essay is computed via the MultipleChoice branch: essays have no PackageOptions, so selectedOptions is empty and `isCorrect = single != null && single.IsCorrect` is always false (L2188-2192). The IsEssayPending flag is only true while status == PendingGrading (L2222-2223). Therefore once HC finishes grading and the session becomes Completed, every essay question renders with IsEssayPending=false and IsCorrect=false â†’ the view (Results.cshtml L339-350) shows a red "Salah" badge for that essay regardless of the EssayScore the HC actually awarded. The stored Score (Helpers/AssessmentScoreAggregator.cs L52-54 / AssessmentAdminController.FinalizeEssayGrading) DOES add the essay's EssayScore to totalScore, so a worker who earned full essay marks (and possibly LULUS) sees that very essay flagged "Salah" in their own answer review. The essay answer text (PackageUserResponse.TextAnswer) and awarded marks are never surfaced at all (UserAnswer is null because no PackageOptionId).

**Dampak:** A worker reviewing a completed essay assessment sees correctly-graded essays marked wrong (red "Salah"), directly contradicting their passing percentage and the certificate. Confusing and erodes trust; appears as a grading bug to end users. No data loss, but a real review-display correctness defect.

**Bukti:** Controllers/CMPController.cs:2181-2224 (essay falls into else/MC branch L2188-2192 â†’ isCorrect=false; IsEssayPending gated on status==PendingGrading L2222); Views/CMP/Results.cshtml:333-350 (badge: IsEssayPendingâ†’Menunggu, else IsCorrectâ†’Benar, elseâ†’Salah); Helpers/AssessmentScoreAggregator.cs:52-54 (essay EssayScore IS added to stored totalScore); Models/PackageUserResponse.cs:29-32 (TextAnswer/EssayScore exist but never projected into QuestionReviewItem).

**Usulan fix:** In the review loop, special-case Essay: when QuestionType=="Essay", do not use the MC isCorrect path. After grading, derive correctness/partial from the persisted EssayScore (e.g. IsCorrect = EssayScore >= q.ScoreValue, or render a dedicated "X/ScoreValue poin" label), and project TextAnswer into UserAnswer so the worker can see their essay and awarded marks. Keep IsEssayPending for the PendingGrading window.

**Verifikasi adversarial:** Confirmed every link of the chain in actual code. CMPController.Results package path: essays (QuestionType="Essay") are not "MultipleAnswer" so they fall into the else/MC branch at L2188-2192; essays have no PackageOptions, so selectedOptionIds (L2174-2177) and selectedOptions (L2179) are empty, single = selectedOptions.FirstOrDefault() = null, thus isCorrect = (single != null && single.IsCorrect) = false unconditionally â€” even for a fully-marked essay. IsEssayPending (L2222-2223) is true ONLY when assessment.Status == PendingGrading AND QuestionType=="Essay". After grading, FinalizeEssayGrading (AssessmentAdminController.cs L3563-3569) ExecuteUpdateAsync sets Status = Completed; AssessmentConstants confirms PendingGrading="Menunggu Penilaian" and Completed="Completed" are distinct values â€” so once completed, IsEssayPending=false. Results.cshtml L333-350 renders: IsEssayPendingâ†’"Menunggu Penilaian" (secondary badge); else IsCorrectâ†’green "Benar"; elseâ†’red "Salah". A completed essay (IsEssayPending=false, IsCorrect=false) therefore renders red "Salah". Meanwhile AssessmentScoreAggregator.Compute L52-54 DOES add essayResp.EssayScore to totalScore, so the percentage/LULUS reflects awarded essay marks â€” directly contradicting the per-question "Salah" badge. QuestionReviewItem (AssessmentResultsViewModel.cs L24-37) has NO EssayScore/TextAnswer field and CMPController never projects them; UserAnswer is null (no PackageOptionId, L2196-2198) so the essay answer text and awarded marks are never surfaced in the review. Worker reachability confirmed: Results is linked from worker-facing views (CMP/Assessment.cshtml, Records.cshtml, ExamSummary.cshtml, StartExam.cshtml) and essays use package-based responses (ExamSummary path L1408-1420 reads dbTextAnswers), so graded essay sessions land on this exact package path. Refutation attempts all failed: no alternate worker Results path corrects essays; no continue/skip for essays; no later reassignment of isCorrect; the L3745 Status="Completed" is the unrelated interview path; AllowAnswerReview does not auto-disable for essays. The only mitigating condition is the badge only renders when AllowAnswerReview==true, which is a normal config, not a refutation. All cited line numbers are accurate.

---

### [RES:RES-02] "X/Y benar" (CorrectAnswers/TotalQuestions) is an unweighted question count that contradicts the weighted Score% shown next to it

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | drift |
| **Confidence** | high |
| **Area** | Results / review projection + authz |

**Masalah:** The displayed Score is the stored weighted percentage: totalScore/maxScore where each question contributes q.ScoreValue (GradingService L82-134 and AssessmentScoreAggregator L32-58). But CorrectAnswers/TotalQuestions in the Results view is a flat per-question count: correctCount is incremented by 1 per correct question (CMPController L2194/2242/2247) and TotalQuestions = orderedQuestionIds.Count (L2306), with NO weighting by ScoreValue. ScoreValue is admin-editable per question (default 10, range 1-100 â€” Models/AssessmentPackage.cs:41, Views/Admin/ManagePackageQuestions.cshtml:232-235), so packages routinely mix point values. Results.cshtml:54-55 renders `@Model.Score%` immediately above `(@Model.CorrectAnswers/@Model.TotalQuestions benar)`. When ScoreValue is non-uniform, these two numbers diverge (e.g. Score=80% but "2/5 benar" if the high-value questions were correct), making the summary internally inconsistent. For essay-containing assessments the divergence is guaranteed because essays count 0 in CorrectAnswers (see RES-01) but contribute their EssayScore to Score.

**Dampak:** Workers and reviewers see a passing/failing percentage that does not match the displayed "correct count" whenever questions carry different point weights â€” looks like a scoring bug and can trigger grading disputes. Purely a projection/display inconsistency (stored Score itself is correct).

**Bukti:** Controllers/CMPController.cs:2194,2230-2248 (correctCount += 1 per question, no ScoreValue weighting), 2306-2307 (TotalQuestions=count, CorrectAnswers=correctCount); Services/GradingService.cs:89-134 (stored Score weighted by q.ScoreValue); Models/AssessmentPackage.cs:41 (ScoreValue default 10, per-question); Views/CMP/Results.cshtml:54-55 (Score% and X/Y benar shown together).

**Usulan fix:** Either (a) make the review count weighted-consistent with the stored Score (sum q.ScoreValue for correct vs total maxScore, and label as points), or (b) relabel the secondary line as "N dari M soal dijawab benar (poin per-soal dapat berbeda)" so it is clearly an unweighted item count and not expected to equal Score%. Prefer (a) for consistency with the certificate/score.

**Verifikasi adversarial:** Verified all cited facts against the actual code and could not refute.

(1) Displayed Score = stored weighted percentage. Results.cshtml:54 renders @Model.Score, sourced from CMPController.cs:2129 (`score = assessment.Score ?? 0`) â†’ viewModel.Score (L2300). assessment.Score is computed weighted-by-ScoreValue in TWO code paths: GradingService.cs (L89 `maxScore += q.ScoreValue`, L102/L117 `totalScore += q.ScoreValue`, L134 `totalScore/maxScore*100`) and Helpers/AssessmentScoreAggregator.cs:Compute (L35 maxScore+=ScoreValue, L43/L50 totalScore+=ScoreValue, L54 essay adds EssayScore, L58 totalScore/maxScore*100). Weighted percentage confirmed.

(2) CorrectAnswers = flat per-question count. correctCount is `++` (i.e. +1) per correct question in both the review branch (CMPController.cs:2194) and the no-review branch (L2242, L2247) â€” zero ScoreValue weighting. viewModel.CorrectAnswers = correctCount (L2307). Confirmed.

(3) TotalQuestions = orderedQuestionIds.Count (CMPController.cs:2306) â€” flat count. Confirmed.

(4) ScoreValue is per-question admin-editable, default 10, range 1-100: Models/AssessmentPackage.cs:41 (`public int ScoreValue { get; set; } = 10;`) and Views/Admin/ManagePackageQuestions.cshtml:232-236 (input min=1 max=100; note the `data-affected-sessions` attr at L235 shows editing ScoreValue mid-life is a tracked, supported op). So non-uniform weights are routine and the two numbers diverge whenever weights differ.

(5) Both numbers rendered adjacently at Results.cshtml:54-55 (`@Model.Score%` above `(@Model.CorrectAnswers/@Model.TotalQuestions benar)`). Confirmed.

(6) Essay divergence guaranteed: in the correctness loop, essays cannot increment correctCount (the non-MA branch at L2191-2192 only counts when a selected option .IsCorrect; essays have no correct option), yet the stored Score includes EssayScore (Aggregator L52-54). So essay assessments always have Score reflecting essay points while X/Y excludes them.

Refutation attempts that failed: no guard forces ScoreValue uniform (UI explicitly allows 1-100 per question); the X/Y label is not weighted (strictly ++); Score is definitively the weighted percentage, not a flat count. Minor citation nits (aggregator is under Helpers/ not Services/; GradingService weighted block spans ~L82-134 not L89-134) are immaterial â€” finding cited both the Services path and Helpers context and the substance is exact.

This is a true display/projection inconsistency. Stored Score and pass/fail (IsPassed = score >= passPercentage, L2302) are correct; only the parenthetical count contradicts the percentage.

---

### [RST:RST-02] Reset button shown for essay sessions (Menunggu Penilaian) but ResetAssessment rejects that status â€” reset always fails

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | bug |
| **Confidence** | high |
| **Area** | Reset / force-end controls + retake semantics |

**Masalah:** The monitoring view renders the Reset action for every session whose derived UserStatus != "Cancelled" (Views/Admin/AssessmentMonitoringDetail.cshtml:331-343), and DeriveUserStatus returns "Menunggu Penilaian" for raw status PendingGrading (Controllers/AssessmentAdminController.cs:2687-2688). But ResetAssessment's status guard only permits raw statuses Open/InProgress/Completed/Abandoned (Controllers/AssessmentAdminController.cs:4036). The raw PendingGrading value is the string "Menunggu Penilaian" (Models/AssessmentConstants.cs:18), which is NOT in that allow-list, so the guard rejects with TempData["Error"]="Status sesi tidak valid untuk direset." Result: HC sees a Reset button on an essay-pending session, clicks it (confirming the destructive prompt), and reset silently fails â€” the worker who botched an essay exam cannot be reset for a retake. The same UX mismatch applies to Upcoming sessions (DeriveUserStatus â†’ "Not started", button shown, controller rejects raw "Upcoming").

**Dampak:** HC cannot reset/retake essay-pending or upcoming sessions via the UI; the action presents itself as available but fails with a generic error, blocking a legitimate operational flow (essay exam do-over) with no working path.

**Bukti:** View Views/Admin/AssessmentMonitoringDetail.cshtml:331 `@if (session.UserStatus != "Cancelled")` wraps the ResetAssessment form (:334-341). DeriveUserStatus Controllers/AssessmentAdminController.cs:2687-2688 maps PendingGradingâ†’"Menunggu Penilaian". Guard Controllers/AssessmentAdminController.cs:4036 omits "Menunggu Penilaian" (and "Upcoming"). AssessmentConstants.cs:18 PendingGrading = "Menunggu Penilaian".

**Usulan fix:** Either (a) add AssessmentConstants.AssessmentStatus.PendingGrading ("Menunggu Penilaian") to the ResetAssessment allow-list at line 4036 (essay answers live in PackageUserResponses which reset already deletes at :4070-4074, so reset is safe), or (b) hide the Reset button for statuses the controller cannot reset. Reconcile the view condition with the controller allow-list either way.

**Verifikasi adversarial:** Confirmed end-to-end against the actual code.

1. VIEW (Views/Admin/AssessmentMonitoringDetail.cshtml:331-343): The Reset form is wrapped in `@if (session.UserStatus != "Cancelled")`. Verified verbatim â€” line 331 guard, lines 334-341 the ResetAssessment POST form with confirm() prompt "Reset sesi ini? Semua jawaban akan dihapus...".

2. CONSTANT (Models/AssessmentConstants.cs:18): `PendingGrading = "Menunggu Penilaian"`. Confirmed. Also confirmed GradingService.cs:206 actually persists this raw status for essay sessions, so such rows genuinely exist.

3. DERIVATION (Controllers/AssessmentAdminController.cs:2685-2698): DeriveUserStatus maps raw PendingGrading â†’ "Menunggu Penilaian" (line 2687-2688, checked first). Since "Menunggu Penilaian" != "Cancelled", the Reset button IS rendered. The view model's UserStatus is exactly this derived value (line 3310/3317).

4. QUERY (AssessmentMonitoringDetail action, lines 3245-3254): no status filter â€” PendingGrading, Upcoming, and Cancelled sessions all appear in the list.

5. GUARD (Controllers/AssessmentAdminController.cs:4036): `if (assessment.Status != "Open" && != "InProgress" && != "Completed" && != "Abandoned")` â†’ TempData["Error"]="Status sesi tidak valid untuk direset." Raw "Menunggu Penilaian" is NOT in the allow-list, so the guard rejects. No normalization happens between fetch (line 4002) and this guard; IsResettable (3994) only checks IsManualEntry, and the D-17 Pre/Post block is type-specific. So an essay-pending session shows a Reset button that always fails.

SECONDARY claim (Upcoming) also confirmed: sessions are created with Status="Upcoming" (line 1225) with null StartedAt; DeriveUserStatus has no Upcoming case so it falls through to "Not started" (button shown, line 331 passes), and the guard at 4036 rejects raw "Upcoming". Same dead-button mismatch.

ONE NUANCE that does not refute the finding: DeriveUserStatus maps raw "Cancelled" â†’ "Dibatalkan" (line 2692), NOT "Cancelled". So the view's `UserStatus != "Cancelled"` guard never actually matches a cancelled session either â€” meaning even Cancelled sessions get the Reset button shown (and ResetAssessment correctly rejects them). This makes the view guard effectively a no-op for its stated purpose, but it strengthens rather than weakens the finding: the button is shown for essentially every session regardless of status, while the controller only accepts 4 statuses. The defect (Reset button presented on essay-pending/upcoming sessions but the action fails with a generic error) is real.

Severity MED is appropriate: it blocks a legitimate operational flow (resetting an essay-pending session for a worker retake) with no working UI path, but it is a soft-fail with an error message (not data corruption or security), and a workaround exists for completed/in-progress sessions.

---

### [SAVE:SAVE-01] SaveAnswer "atomic upsert" race retry relies on a unique constraint that was dropped â€” concurrent saves create duplicate MC rows

| | |
|---|---|
| **Severity** | MED (klaim: HIGH) |
| **Tipe** | race |
| **Confidence** | high |
| **Area** | Answer persistence / auto-save + SignalR + resume |

**Masalah:** CMPController.SaveAnswer (Controllers/CMPController.cs:370-401) does an ExecuteUpdateAsync, and if updatedCount==0 it Adds a new row and wraps SaveChangesAsync in a try/catch(DbUpdateException) that comments "Race: concurrent request already inserted this response â€” retry as update". This retry-as-update logic depends on a UNIQUE index on (AssessmentSessionId, PackageQuestionId) to throw a duplicate-key DbUpdateException. That unique index was explicitly DROPPED by migration 20260407070949_RemoveUniqueIndexOnPackageUserResponse (Up() recreates the index as NON-unique), and the current model/snapshot confirms it is non-unique (Data/ApplicationDbContext.cs:534 HasIndex with no IsUnique; Migrations/ApplicationDbContextModelSnapshot.cs:1424 non-unique). The constraint had to be removed because MultipleAnswer stores many rows per question. Consequence: when two SaveAnswer requests for the SAME (sessionId, questionId) both observe updatedCount==0 (e.g. worker open in two tabs/devices, or the client offline-retry flush firing concurrently), BOTH insert rows and NEITHER throws, so the catch never runs and two MC response rows persist. MC scoring later resolves via FirstOrDefault (GradingService.cs:96-97, AssessmentScoreAggregator.cs:39, SubmitExam grouping at CMPController.cs:1622-1624 g.First()) over rows with no deterministic ORDER BY â€” so if the duplicate rows hold different optionIds (worker changed answer), the graded option is non-deterministic and may not be the worker's final choice.

**Dampak:** Duplicate MC answer rows can be silently created on concurrent saves; the dead catch block gives a false sense of atomicity. Final grading may pick a stale/wrong option for that question, producing an incorrect score for the worker. Also pollutes the response table, affecting resume pre-fill (see SAVE-02).

**Bukti:** Controllers/CMPController.cs:371-401 (ExecuteUpdateâ†’Addâ†’catch(DbUpdateException) retry); migration Migrations/20260407070949_RemoveUniqueIndexOnPackageUserResponse.cs:13-21 (Up drops unique, recreates non-unique); Data/ApplicationDbContext.cs:534 (HasIndex(new {AssessmentSessionId, PackageQuestionId}) â€” no IsUnique); Migrations/ApplicationDbContextModelSnapshot.cs:1424 (b.HasIndex("AssessmentSessionId","PackageQuestionId") non-unique). Scoring FirstOrDefault: Services/GradingService.cs:96-97; Helpers/AssessmentScoreAggregator.cs:39.

**Usulan fix:** Do not rely on a dropped unique constraint. Either (a) make the upsert genuinely atomic with a single SQL MERGE / serializable transaction, or (b) for MC specifically, after insert, delete any other rows for that (sessionId, questionId) keeping the latest, or (c) re-introduce a partial/filtered unique index that applies only to single-answer question types. At minimum, the catch should also handle the no-exception duplicate path (e.g. delete-extra-then-update) rather than assuming a duplicate-key exception will fire.

**Verifikasi adversarial:** Verified every cited location against the actual code; the mechanism is real.

1. SaveAnswer (Controllers/CMPController.cs:370-401): exactly as described â€” `ExecuteUpdateAsync` on the (AssessmentSessionId, PackageQuestionId) filter; if updatedCount==0 it `Add`s a new PackageUserResponse and wraps `SaveChangesAsync` in `try { } catch (DbUpdateException) { ChangeTracker.Clear(); retry-as-update }` with the literal comment "Race: concurrent request already inserted this response â€” retry as update". That catch can only fire on a unique-key violation.

2. The unique constraint is genuinely gone. Migrations/20260224090357_AddUniqueConstraintPackageUserResponse created it; Migrations/20260407070949_RemoveUniqueIndexOnPackageUserResponse.cs:13-21 Up() DropIndex then CreateIndex WITHOUT `unique: true` (only Down() at :30-34 re-adds unique:true). Data/ApplicationDbContext.cs:534 `entity.HasIndex(r => new { r.AssessmentSessionId, r.PackageQuestionId });` has no .IsUnique()/.HasFilter() and is the ONLY index configured for that entity. Snapshot Migrations/ApplicationDbContextModelSnapshot.cs:1424 confirms `b.HasIndex("AssessmentSessionId", "PackageQuestionId")` non-unique; PK is just surrogate Id (:1418). I scanned all IsUnique()/filtered indexes in the context â€” none survive for PackageUserResponses. So the catch is a dead block: concurrent same-key inserts both succeed, neither throws.

3. Concurrency premise holds server-side: Program.cs:27 uses AddDbContext (scoped, one per request), so two concurrent HTTP requests have separate change-trackers â€” no in-memory dedup. SaveAnswer has no transaction/lock. Two requests both seeing updatedCount==0 will both Add+commit â†’ two rows.

4. Non-deterministic grading confirmed: GradingService.cs:96-97 and Helpers/AssessmentScoreAggregator.cs:39 both `FirstOrDefault(r => r.PackageQuestionId==q.Id && r.PackageOptionId.HasValue)` with no ORDER BY; SubmitExam CMPController.cs:1622-1624 builds the dict via `GroupBy(...).ToDictionary(g => g.First())`. Critically, GradingService is documented (Services/GradingService.cs:51) to grade "selalu dari DB (bukan dari form POST)" and is what persists the final score. At SubmitExam the MC upsert (CMPController.cs:1643-1647) updates only the single row surfaced by g.First(), leaving the duplicate stale; the authoritative re-grade then FirstOrDefaults over both rows non-deterministically.

Refutation attempt (partial mitigation, does NOT kill the finding): Views/CMP/StartExam.cshtml:622-623 has an `inFlightSaves` guard that drops a second SaveAnswer for the same qId while one is in flight, and the offline flush (:640) skips the just-saved qId. This neutralizes the single-tab offline-flush sub-scenario. BUT it is per-page JS state and does not span the two-tabs/two-devices case the finding explicitly names, and there is no server-side equivalent. So the race survives in the cited multi-surface scenario.

---

### [SAVE:SAVE-03] SaveTextAnswer upsert is non-atomic with no race guard â€” concurrent essay saves can create duplicate rows that permanently block FinalizeEssayGrading

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Answer persistence / auto-save + SignalR + resume |

**Masalah:** AssessmentHub.SaveTextAnswer (Hubs/AssessmentHub.cs:161-181) does FirstOrDefaultAsync then update-or-insert with no transaction, no unique constraint (dropped â€” see SAVE-01), and no DbUpdateException retry. Two concurrent SaveTextAnswer invocations for the same (session, question) â€” possible across two connections (multi-tab/multi-device); within one connection SignalR serializes invocations by default (MaximumParallelInvocationsPerClient=1, Program.cs:134 plain AddSignalR()) â€” can both find no existing row and both insert, yielding duplicate essay rows. HC grading writes EssayScore to only one row via FirstOrDefault (AssessmentAdminController.SubmitEssayScore:3431-3447). The duplicate row keeps EssayScore==null, so FinalizeEssayGrading's guard (AssessmentAdminController.cs:3518-3524 essayResponses.Any(r=>r.EssayScore==null)) returns "Masih ada Essay yang belum dinilai" forever, while the grading UI exposes only one row per question (essayRespMap, line 3410) so HC has no way to grade the orphan row. The session is stuck in PendingGrading indefinitely.

**Dampak:** A worker editing the same essay from two tabs/devices, or with a flaky connection, can leave an ungraded duplicate essay row. The assessment can then never be finalized through the normal HC UI (permanently stuck Menunggu Penilaian), requiring manual DB intervention.

**Bukti:** Hubs/AssessmentHub.cs:161-181 (FirstOrDefault then insert/update, no tx/no retry); Program.cs:134 (AddSignalR with no MaximumParallelInvocationsPerClient override = default 1); Controllers/AssessmentAdminController.cs:3431-3447 (SubmitEssayScore writes one row); :3518-3524 (Finalize blocks if any essay row EssayScore==null); :3410 (UI maps one response per question).

**Usulan fix:** Make SaveTextAnswer atomic: wrap in a transaction or catch DbUpdateException and reload, OR collapse duplicates (delete all but one) before writing. Long-term, restore a unique constraint scoped to non-MA question types. Defensively, FinalizeEssayGrading/SubmitEssayScore should dedupe essay rows per question.

**Verifikasi adversarial:** Confirmed from actual code. (1) Hubs/AssessmentHub.cs:134-182 SaveTextAnswer: FirstOrDefaultAsync on (sessionId,questionId) lines 162-163 then update-or-insert lines 165-179 + SaveChangesAsync line 181 â€” no transaction, no DbUpdateException retry, fresh scoped DbContext per invocation (line 139-140). (2) No unique constraint: migration 20260224090357_AddUniqueConstraintPackageUserResponse added it, then 20260407070949_RemoveUniqueIndexOnPackageUserResponse explicitly DropIndex + CreateIndex WITHOUT unique:true; ApplicationDbContext.cs:534 is HasIndex(...) non-unique. So nothing prevents duplicate rows. (3) Program.cs:134 plain AddSignalR() = default MaximumParallelInvocationsPerClient=1; finding correctly notes the race needs two connections (multi-tab/device). Client autosave (StartExam.cshtml:903-911) debounce is per-tab, no cross-tab guard. (4) SubmitEssayScore lines 3431-3447 FirstOrDefaultAsync updates exactly one row's EssayScore; a duplicate stays EssayScore==null. (5) FinalizeEssayGrading lines 3518-3524 loads ALL responses (not FirstOrDefault) and essayResponses.Any(r=>r.EssayScore==null) returns 'Masih ada Essay yang belum dinilai' â€” the orphan null row blocks finalization permanently; Grep confirms no dedup in the path (Distinct() at line 3543 only feeds the fallback question-ID derivation, not allResponses or the null-guard). One description inaccuracy that does NOT refute: the grading UI map at line 3401 uses ToDictionaryAsync(r=>r.PackageQuestionId), which THROWS ArgumentException on a duplicate key (500-errors the grading page) rather than silently showing one row as the finding states â€” making the outcome arguably worse (HC may be unable to grade at all). Either path leaves the session permanently stuck in PendingGrading/Menunggu Penilaian with no automated recovery, requiring manual DB intervention. Core defect confirmed.

---

### [SAVE:SAVE-04] Inconsistent post-timer-expiry save guards: MA blocks expired saves, but MC and Essay do not

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | medium |
| **Area** | Answer persistence / auto-save + SignalR + resume |

**Masalah:** SaveMultipleAnswer enforces a server-side timer check and refuses to save once elapsed exceeds (DurationMinutes+ExtraTimeMinutes)*60 (Hubs/AssessmentHub.cs:205-215). SaveTextAnswer (essay, :134-182) and the MC SaveAnswer endpoint (CMPController.cs:346-417) have NO timer check â€” they only verify the session Status is still InProgress / not closed. Because expiry is enforced client-side (the server has no cron to auto-flip expired sessions to Abandoned/Completed), a session whose timer has run out remains Status==InProgress until someone submits. During that window a worker (or a stale tab/script) can keep changing MC and Essay answers past the deadline, while MA answers are correctly rejected. This is both an inconsistency bug and a fairness/integrity gap.

**Dampak:** Workers can modify MC and essay answers after the official time limit (as long as the session is still InProgress and not yet submitted), while MA answers cannot be changed â€” unfair and a grading-integrity hole.

**Bukti:** Hubs/AssessmentHub.cs:205-215 (SaveMultipleAnswer timer guard) vs Hubs/AssessmentHub.cs:134-182 (SaveTextAnswer: no timer guard) vs Controllers/CMPController.cs:366-401 (SaveAnswer: only Status guard, no timer). No server-side expiry job observed; StartExam/SubmitExam compute expiry inline only on request.

**Usulan fix:** Apply the same elapsed-vs-(DurationMinutes+ExtraTimeMinutes) server-side timer check used in SaveMultipleAnswer to SaveTextAnswer and the SaveAnswer (MC) endpoint, rejecting saves once the allowed time has elapsed.

**Verifikasi adversarial:** Confirmed against actual code. Three (and only three) server-side answer-save paths exist:

1. MA â€” `Hubs/AssessmentHub.cs:188` `SaveMultipleAnswer`: after the ownership+`Status=="InProgress"` check (lines 197-203), it ALSO enforces a server-side timer guard (lines 206-215): `elapsed = (UtcNow - StartedAt).TotalSeconds`, `allowed = (DurationMinutes + ExtraTimeMinutes??0)*60`, and `if (elapsed > allowed) return;`. So expired MA saves are rejected.

2. Essay â€” `Hubs/AssessmentHub.cs:134` `SaveTextAnswer`: loads the session with the SAME predicate `s.Id==sessionId && s.UserId==userId && s.Status=="InProgress"` (lines 143-149) but has NO timer/StartedAt/DurationMinutes check anywhere before the upsert + SaveChangesAsync (lines 161-181). Confirmed no timer guard.

3. MC â€” `Controllers/CMPController.cs:348` `SaveAnswer`: validates params, ownership (line 363), and only `Status=="Completed"|"Abandoned"|"Cancelled"` (line 367). No timer check before the ExecuteUpdateAsync/insert upsert (lines 371-401). Confirmed no timer guard.

The required fields exist on the model (`Models/AssessmentSession.cs:46 StartedAt`, `:19 DurationMinutes`, `:199 ExtraTimeMinutes`), so the MC/Essay paths COULD apply the same guard but do not â€” the asymmetry is real, not a data limitation.

Tried to refute via a server-side auto-expiry mechanism: there is none. Grep for IHostedService/BackgroundService/Hangfire/Quartz/AddHostedService returns ZERO hits in source (all matches are in .planning/docs as future recommendations). Sessions flip out of InProgress only on explicit request: `AbandonExam` (CMPController.cs:1220, user-triggered, InProgressâ†’Abandoned) or `SubmitExam` (CMPController.cs:1523), which computes expiry INLINE at request time (lines 1554-1559) plus `EnsureCanSubmitExamAsync`. So between timer-expiry and an actual submit/abandon, `Status` remains "InProgress" â€” exactly the window the finding describes, during which MC `SaveAnswer` and Essay `SaveTextAnswer` keep accepting writes while MA `SaveMultipleAnswer` rejects them.

`SaveLegacyAnswer` (the only other candidate path) was removed in Phase 227 (CMPController.cs:419 comment), so no alternate handler refutes the gap. Expiry enforcement is otherwise client-side (StartExam.cshtml updateTimer auto-submit), which a stale tab or script can bypass.

The finding's evidence cites are accurate (Hub :205-215 vs :134-182, CMPController :366-401). The inconsistency and integrity gap are genuine.

---

### [SHF:SHF-02] ReshufflePackage on an Abandoned session leaves orphaned PackageUserResponses and drops SavedQuestionCount

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Shuffle engine correctness + toggle + round-robin |

**Masalah:** ReshufflePackage permits reshuffling sessions in status 'Abandoned' (AssessmentAdminController.cs:5178 allows both 'Not started' and 'Abandoned'). An Abandoned session can hold previously auto-saved answers (PackageUserResponses are written while InProgress via SaveAnswer, CMPController.cs:371-401, and AbandonExam keeps them â€” it only flips Status, CMPController.cs:1240-1244). The reshuffle removes and recreates the UserPackageAssignment with a potentially different ShuffledQuestionIds set (in ON mode the K-min sample is a different random subset; in OFF>=2 a different package), but it does NOT delete the existing PackageUserResponses rows (no PackageUserResponses.Remove in the endpoint, AssessmentAdminController.cs:5198-5225) and does NOT set SavedQuestionCount on the new assignment (only StartExam sets it, CMPController.cs:1041 â€” confirmed absent at the reshuffle insert lines 5215-5223). The proper Reset endpoint, by contrast, correctly deletes responses + assignment + ET scores and clears StartedAt (AssessmentAdminController.cs:4069-4108).

**Dampak:** Stale answer rows for questions no longer in the assignment remain in the DB tied to the session. Grading/review iterate over the new ShuffledQuestionIds so orphaned rows are mostly ignored in score totals (CMPController.cs:1626, 2167), but they pollute analytics/exports that count raw PackageUserResponses (e.g. answeredCount CMPController.cs:404-405, per-option tallies AssessmentAdminController.cs:3298/3533) and inflate 'answered' counts. Dropping SavedQuestionCount disables the stale-question safety net (CMPController.cs:1065-1078) for that session going forward. Worker-facing exam corruption is mitigated because StartExam blocks Abandoned sessions entirely (CMPController.cs:954-958), so a Reset (which cleans up) is required before retake â€” but HC reshuffling an Abandoned session directly is a data-hygiene defect.

**Bukti:** Controllers/AssessmentAdminController.cs:5178 (Abandoned allowed), :5198-5225 (Remove old assignment + Add new, no PackageUserResponses cleanup, no SavedQuestionCount set). Contrast Controllers/AssessmentAdminController.cs:4069-4081 (Reset deletes PackageUserResponses + assignment) and Controllers/CMPController.cs:1041 (only place SavedQuestionCount is set).

**Usulan fix:** In ReshufflePackage, when the target is Abandoned (i.e., has saved progress), also delete its PackageUserResponses (and SessionElemenTeknisScores) and set newAssignment.SavedQuestionCount = shuffledIds.Count â€” or simply disallow reshuffling Abandoned sessions and require Reset first (matching ReshuffleAll, which already skips Abandoned).

**Verifikasi adversarial:** Verified every cited claim against the actual code.

1) ReshufflePackage allows Abandoned: AssessmentAdminController.cs:5168-5179 derives userStatus and the guard at 5178 explicitly permits both "Not started" AND "Abandoned" ("Hanya peserta yang belum mulai atau sesi yang ditinggalkan ...").

2) Reshuffle removes old + adds new assignment without cleaning responses or setting SavedQuestionCount: lines 5198-5223 Remove(currentAssignment) then build a fresh ShuffledQuestionIds via ShuffleEngine.BuildQuestionAssignment (ON = random K-min subset, OFFâ‰¥2 = package by workerIndex) and Add(newAssignment). The new UserPackageAssignment initializer (5215-5222) sets only AssessmentSessionId/AssessmentPackageId/UserId/ShuffledQuestionIds/ShuffledOptionIdsPerQuestion â€” no PackageUserResponses.Remove anywhere in the endpoint, and no SavedQuestionCount assignment.

3) Responses exist on Abandoned sessions: CMPController.SaveAnswer (370-401) upserts PackageUserResponses while InProgress and is blocked once Status is Abandoned (367). AbandonExam (1240-1244) only sets Status="Abandoned"+UpdatedAt and SaveChanges â€” responses are kept.

4) SavedQuestionCount: a grep across the repo shows the ONLY production write is CMPController.cs:1041 (StartExam, assignment-creation path); the column is nullable (PROJECT.md:819 "null = pre-v2.1 session"). The stale-question safety net at CMPController.cs:1065-1078 is gated on assignment.SavedQuestionCount.HasValue, so a reshuffle-created assignment (null) disables that guard for the session.

5) Reset contrast holds: AssessmentAdminController.cs:4069-4108 deletes PackageUserResponses (RemoveRange), the UserPackageAssignment, SessionElemenTeknisScores, and clears StartedAt/CompletedAt/Score etc. â€” i.e. proper cleanup, unlike reshuffle.

6) Grading ignores orphans: CMPController.cs:1619-1626 batch-loads responses then iterates shuffledIds (new set), so orphaned rows for dropped questions are not scored â€” worker score totals are not corrupted. StartExam also blocks Abandoned re-entry (954-958), so worker retake requires a Reset (which cleans up). The finding itself concedes both mitigations.

7) Analytics/export pollution is real: raw per-session counts of PackageUserResponses with no ShuffledQuestionIds filter include orphans â€” CMPController.cs:404-405 (answeredCount), AssessmentAdminController.cs:6830-6831 (totalAnswered in exam-detail summary), 6448-6452 (distinct affected-session count per question feeding the HC edit-warning modal), plus export aggregations at 4571-4572 and 4819-4820. A session left reshuffled-but-not-reset will over-report answered counts and falsely appear in per-question affected-session tallies.

8) No FK cascade saves it: PackageUserResponse.cs has FKs only to AssessmentSession/PackageQuestion/PackageOption â€” none to UserPackageAssignment â€” so removing the assignment does not cascade-delete responses; orphans persist tied to AssessmentSessionId.

No refuting guard found. The defect is genuine but bounded to data-hygiene (analytics/export pollution + disabled stale-question safety net) and arises only in a specific HC action sequence (worker abandons after saving answers, HC reshuffles instead of resets). Worker-facing exam integrity is protected by StartExam's Abandoned block and grading's shuffledIds iteration.

---

### [STAT:STAT-02] AbandonExam performs an unguarded blind UPDATE (TOCTOU) â€” can overwrite a freshly-Completed/graded session back to Abandoned

| | |
|---|---|
| **Severity** | MED (klaim: HIGH) |
| **Tipe** | race |
| **Confidence** | high |
| **Area** | AssessmentSession status lifecycle + race-safe finalize |

**Masalah:** AbandonExam reads the entity once (CMPController:1222), checks in-memory `Status != "InProgress" && Status != "Open"` (line 1234), then sets `assessment.Status = "Abandoned"` via change-tracking and calls SaveChangesAsync (lines 1241-1244). Because it uses EF change tracking with no `WHERE Status = ...` clause and there is no RowVersion on AssessmentSession, the resulting SQL is a blind `UPDATE ... WHERE Id=@id`. If a concurrent SubmitExam or HC AkhiriUjian completes grading (sets Status=Completed, Score, IsPassed, NomorSertifikat) in the window between AbandonExam's read and its write, AbandonExam will overwrite the row back to "Abandoned" with last-writer-wins, while leaving the Score/IsPassed/CompletedAt/NomorSertifikat columns from grading intact (AbandonExam only sets Status+UpdatedAt). The result is a logically inconsistent row: Status=Abandoned but Score/IsPassed/certificate populated, and the graded Completed verdict is lost.

**Dampak:** A worker who completes/submits at almost the same moment they (or HC) abandon the exam can end up with a corrupted record: a graded, certificate-bearing session marked Abandoned, or a completed pass silently reverted. Records/Results screens read Status (CMPController:2119 IsAssessmentSubmitted gate) so the verdict disappears even though the score/cert rows persist â€” data-integrity loss and lost exam result.

**Bukti:** Controllers/CMPController.cs:1233-1244 â€” guard read at 1234 then `assessment.Status = "Abandoned"; assessment.UpdatedAt = ...; await _context.SaveChangesAsync();` with no status-guarded ExecuteUpdateAsync. Contrast with the rest of the lifecycle which deliberately uses status-guarded ExecuteUpdateAsync for race safety (GradingService.cs:238-246, AssessmentAdminController.cs:4096-4108 ResetAssessment, :3563-3569 FinalizeEssayGrading). AssessmentSession (Models/AssessmentSession.cs:1-200) has no [Timestamp]/RowVersion property, so no optimistic concurrency catches the conflict.

**Usulan fix:** Convert AbandonExam to a status-guarded atomic flip: `ExecuteUpdateAsync` with `Where(s => s.Id == id && (s.Status == "InProgress" || s.Status == "Open"))` setting Status/UpdatedAt, and branch on rowsAffected==0 to report that the session is no longer abandonable (already completed). This prevents clobbering a concurrently-completed session.

**Verifikasi adversarial:** Every claim in the finding is confirmed against the actual code:

1. AbandonExam (CMPController.cs:1218-1248): reads the entity via FirstOrDefaultAsync at line 1222-1223, checks in-memory `Status != "InProgress" && Status != "Open"` at line 1234, then mutates the tracked entity (`assessment.Status = "Abandoned"; assessment.UpdatedAt = DateTime.UtcNow;` lines 1241-1242) and calls `await _context.SaveChangesAsync()` at line 1244. It sets ONLY Status + UpdatedAt â€” Score/IsPassed/CompletedAt/NomorSertifikat are untouched. This is exactly the TOCTOU pattern described.

2. No optimistic concurrency exists. AssessmentSession.cs (1-200, class ends at 200) has NO [Timestamp]/RowVersion/byte[] property â€” Grep for RowVersion|Timestamp|ConcurrencyCheck returned no matches. ApplicationDbContext.cs has no IsConcurrencyToken/IsRowVersion/SaveChangesInterceptor for AssessmentSession (only check constraints on Progress/Duration/PassPercentage/RenewalChain â€” none enforce Statusâ†”Score consistency). Therefore EF emits a blind `UPDATE ... SET Status=..., UpdatedAt=... WHERE Id=@id` with no status guard, last-writer-wins.

3. Contrast is accurate: the rest of the lifecycle deliberately uses status-guarded ExecuteUpdateAsync with rowsAffected==0 race handling â€” GradingService.cs:238-255 (WHERE Status != "Completed"), AssessmentAdminController.cs:4096-4108 ResetAssessment (WHERE Status != "Cancelled"), :3563-3569 FinalizeEssayGrading (WHERE Status == PendingGrading). AbandonExam is the odd one out.

4. The concurrent grading writer exists and writes the columns claimed: SubmitExam (CMPController.cs:1523) calls GradingService.GradeAndCompleteAsync at 1681, which sets Score/Status=Completed/Progress/IsPassed/CompletedAt (GradingService.cs:240-246) and conditionally NomorSertifikat (269+). So a concurrent SubmitExam/AkhiriUjian completing in AbandonExam's read-write window can be clobbered back to Abandoned while leaving Score/IsPassed/cert populated â†’ inconsistent row.

5. Impact gate confirmed: IsAssessmentSubmitted (AssessmentConstants.cs:87-88) returns true ONLY for Completed or PendingGrading. Results (CMPController.cs:2119) and other surfaces (1774, 1879) gate on it, so an Abandoned-overwritten row fails the gate ("Assessment belum selesai") and the graded verdict disappears from the user's view even though score/cert rows persist.

I could not refute it: there is no guard, interceptor, transaction-level lock, DB constraint, or trigger that prevents this race. The defect is real as described.

Severity adjusted HIGHâ†’MED: the window is narrow (single readâ†’write within one request) and triggering requires the worker to both submit AND abandon (or HC AkhiriUjian) at nearly the same instant â€” AbandonExam is explicitly an owner-only manual action (auth check line 1230) gated to InProgress/Open, and the normal UX flow does not submit and abandon simultaneously. It is exploitable (double-click / dueling tabs / HC-AkhiriUjian-vs-worker-abandon) and causes genuine data-integrity loss / lost result with no recovery guard, which is why it is not LOW, but the practical likelihood and narrow timing window make MED more appropriate than HIGH.

---

### [TOK:TOK-02] Token requirement (IsTokenRequired) enforced only in StartExam UI lobby, not on SaveAnswer/SubmitExam â€” gate is bypassable by the owner

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | security |
| **Confidence** | medium |
| **Area** | Token + StartExam access gates |

**Masalah:** IsTokenRequired is checked only on the StartExam GET lobby (CMPController.cs:929-937). The data-mutating exam endpoints do NOT check the token gate: SaveAnswer (CMPController.cs:348) authorizes by owner only (`session.UserId != user.Id` at :363) and merely requires the session status to be Open/Upcoming/InProgress (:367), with no token / TempData[TokenVerified] check; SubmitExam (CMPController.cs:1523-1542) authorizes owner OR Admin/HC and also performs no token check. A token-required session sits in status 'Open' before the worker verifies the token (StartExam only flips it to InProgress after the lobby gate passes). Because SaveAnswer accepts 'Open' status, the session owner can POST answers and then POST SubmitExam directly (anti-forgery token is obtainable from any authenticated page) without ever entering the access token, fully bypassing the IsTokenRequired control.

**Dampak:** The access-token control (intended to stop a worker from starting/answering a token-required exam before the proctor releases the token / outside the supervised window) can be circumvented entirely by the owning worker via direct API calls â€” they can answer and submit a token-required exam without the token, and it is graded and recorded normally. Mitigating factor: only the owner can do this to their own session, so it is a control-bypass / integrity weakness, not cross-user data access.

**Bukti:** CMPController.cs:929-937 (token gate lives only in StartExam GET, keyed on TempData[`TokenVerified_{id}`]); SaveAnswer CMPController.cs:348,363,367 (owner-only, status Open allowed, no token check); SubmitExam CMPController.cs:1523-1542 (owner/Admin/HC, no token check). Token alphabet is only 6 chars from a 31-symbol set (CMPController.cs:2464-2466), so the control is meaningful as a proctoring/early-entry gate, not authn.

**Usulan fix:** Enforce the token gate on the write endpoints, not just the lobby: before accepting SaveAnswer/SubmitExam for a session with IsTokenRequired==true and StartedAt==null, require the TempData/marker proving VerifyToken succeeded (or refuse SaveAnswer when status is still 'Open' for token-required sessions). Simplest: gate SaveAnswer/SubmitExam on `session.StartedAt != null` (which is only set after the lobby token gate passes in StartExam), so answers/submission cannot occur before a legitimately gated start.

**Verifikasi adversarial:** Confirmed from the actual code. (1) The token gate lives ONLY in StartExam GET (CMPController.cs:929-937), keyed on TempData.Peek($"TokenVerified_{id}"). A repo-wide grep shows IsTokenRequired/TokenVerified appear only in VerifyToken (850-882) and StartExam (929-937) â€” nowhere in the mutating endpoints. (2) SaveAnswer (347-417): inherits only class-level [Authorize] (auth, not role/token); owner-only check at :363; status guard at :367 blocks only Completed/Abandoned/Cancelled, so the pre-token "Open" status IS accepted; no token check and no StartedAt check; it persists answers via ExecuteUpdate/Add. (3) SubmitExam (1521-1542): owner OR Admin/HC at :1539; no token check; the helper EnsureCanSubmitExamAsync (4382-4444) only does timer + one-shot auto-submit-token logic (a different mechanism) and returns null/pass when StartedAt==null (:4398) â€” no access-token validation. (4) A token-required exam genuinely sits in Status="Open" with StartedAt==null before token entry: lobby Assessment.cshtml:465-472 renders the token-entry button exactly for Status=="Open" && IsTokenRequired; Upcomingâ†’Open auto-transitions on scheduled time (StartExam 902-911). (5) SubmitExam's grading needs a UserPackageAssignment; StartExam creates it AFTER the token gate, BUT ReshufflePackage (AssessmentAdminController.cs:5215) and ReshuffleAll (:5316) also create assignments for "Not started" sessions, so an assignment can exist for an Open/StartedAt-null token-required session that never passed the gate â€” making the SubmitExam path fully exploitable too. Even without that, SaveAnswer alone defeats the "no answering before token release" control. (6) No middleware/global/action filter enforces the token (Program.cs has no token logic; controller has only [Authorize]). Refutation attempts (token check in SubmitExam/SaveAnswer/helper, global filter, a status guard rejecting "Open", assignment being creatable only via the gated StartExam) all failed. Token alphabet is 31 chars (ABCDEFGHJKLMNPQRSTUVWXYZ23456789) length 6 (GenerateSecureToken 2464-2466) â€” cryptographic RNG but short, consistent with the finding framing it as a proctoring/early-entry gate rather than authn. Minor inaccuracy: the finding says SaveAnswer allows "Open/Upcoming/InProgress" â€” code actually allows any status except the 3 closed ones, but those three are the relevant live states, so the claim holds in substance.

---

### [TOK:TOK-03] StartExam writes Status=InProgress + StartedAt during impersonation (read-only invariant violated)

| | |
|---|---|
| **Severity** | MED (klaim: MED) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Token + StartExam access gates |

**Masalah:** Phase 377 established a write-on-GET-during-impersonation guard: the Upcomingâ†’Open auto-transition is wrapped in `if (!_impersonationService.IsImpersonating())` (CMPController.cs:905) precisely so an admin who merely views a worker's StartExam page does not mutate the worker's DB state. However, the 'mark InProgress on first load' write a few lines later has NO such guard: CMPController.cs:961-967 sets `assessment.Status = "InProgress"; assessment.StartedAt = DateTime.UtcNow; await _context.SaveChangesAsync();` whenever StartedAt==null. The current user here is the EFFECTIVE (impersonated) user from GetCurrentUserRoleLevelAsync (:896), and ownership passes for the impersonated worker. For a non-token assessment (IsTokenRequired==false), an admin impersonating worker X who opens X's Open exam reaches this write (the token gate at :929 only blocks token-required sessions, and only when StartedAt==null). This flips the worker's session to InProgress and starts the exam clock (StartedAt drives the server-side timer at SubmitExam :1554-1558), also firing the SignalR 'workerStarted' notification (:970-977).

**Dampak:** An admin merely viewing (impersonating) a worker's non-token exam unintentionally STARTS that worker's exam: status becomes InProgress and the countdown timer begins from the moment of the admin's page view. If the worker has not yet started, their available exam time is silently consumed, and HC monitors receive a false 'worker started' signal. This corrupts the worker's session timing/state contrary to the Phase 377 read-only impersonation invariant.

**Bukti:** Guarded write: CMPController.cs:905 `if (!_impersonationService.IsImpersonating())`. Unguarded write: CMPController.cs:961-967 (justStarted branch sets StartedAt + Status=InProgress, SaveChangesAsync) â€” no IsImpersonating() check. Effective-user resolution: :896 GetCurrentUserRoleLevelAsync (returns impersonated X). IsImpersonating() exists at Services/ImpersonationService.cs:35-38. StartedAt consumed by timer at CMPController.cs:1554-1558.

**Usulan fix:** Wrap the InProgress/StartedAt write in the same guard used for the auto-transition: `if (justStarted && !_impersonationService.IsImpersonating()) { ... }` (and likewise skip the StartedAt-dependent SignalR 'workerStarted' push + LogActivity when impersonating), so impersonated views never mutate the worker's session.

**Verifikasi adversarial:** Confirmed against actual code in CMPController.cs and supporting files.

1) StartExam is [HttpGet] (CMPController.cs:887-888). Effective-user resolution at :896 uses GetCurrentUserRoleLevelAsync, which (CMPController.cs:2424-2446) calls _impersonationService.GetEffectiveUserAsync and, for mode=user (TargetUser), returns effUser = X (the impersonated worker). So when an admin impersonates worker X, `user` = X and the ownership gate at :898 (`assessment.UserId == user.Id`) passes.

2) The Upcomingâ†’Open auto-transition write IS guarded: CMPController.cs:902-911 wraps `assessment.Status="Open"; SaveChangesAsync()` in `if (!_impersonationService.IsImpersonating())` (:905), with a comment citing Phase 377 Pitfall 3 / T-377-09 read-only invariant.

3) The justStarted write is NOT guarded: CMPController.cs:960-967 sets `assessment.Status="InProgress"; assessment.StartedAt=DateTime.UtcNow; await _context.SaveChangesAsync();` whenever `assessment.StartedAt == null`, with no IsImpersonating() check. SignalR 'workerStarted' fires at :970-977.

4) The token gate at :929 only blocks when `IsTokenRequired && UserId==user.Id && StartedAt==null`; for a non-token exam (IsTokenRequired==false) it is skipped, so the flow reaches the unguarded write. The other gates between :896 and :967 (Completed :920, Abandoned :954, window-close :940, duration :947) do not block the normal Open/non-token case.

5) StartedAt is consumed as the server-side exam clock at SubmitExam: CMPController.cs:1553-1558 computes `elapsed = UtcNow - StartedAt` vs allowed duration. So setting StartedAt at admin view-time starts the worker's countdown.

Refutation attempts that FAILED to refute:
- I checked whether an assessment can legitimately be 'Open' with StartedAt==null (required for the scenario, since impersonation keeps an Upcoming exam from transitioning and :914 then redirects away). It can: AssessmentAdminController.cs:1118-1120 defaults a newly created session's Status to 'Open' (persisted). So a fresh exam is Open/StartedAt==null without the worker ever touching it.
- I checked the global read-only enforcement in ImpersonationMiddleware.cs. It only blocks POST/PUT/DELETE (lines 79-104); GET/HEAD pass straight through (lines 71-77). StartExam is [HttpGet], so the middleware does NOT block this write. This actually confirms (not refutes) the mechanism and explains why Phase 377 needed the per-action guard at :905 â€” the same class of guard is simply missing at :961-967.
- No Admin/HC early-return skips the write; the Admin/HC role check at :898 is only an OR branch of the authz gate, not a skip of the InProgress write.

All cited line ranges and behaviors verified. The finding accurately describes the code: an admin impersonating worker X who opens X's Open, non-token exam will flip it to InProgress and start the clock, plus emit a false workerStarted signal â€” violating the Phase 377 read-only impersonation invariant.

---

### [BYP:BYP-02] ExecuteInstantBypassAsync is public and skips the D-10 double-pending guard

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Proton year-bypass workflow (PendingProtonBypass state machine) |

**Masalah:** BypassSaveAsync (Services/ProtonBypassService.cs:221-264) is the intended entry point and performs the D-10 'no active pending' check (lines 256-259) before dispatching. ExecuteInstantBypassAsync (line 96) and ExecutePendingBypassAsync (line 272) are both public but do NOT re-check D-10; ExecuteInstantBypassAsync also skips the active-pending check entirely. Today the only production caller of these is BypassSaveAsync itself (verified: the controller calls only BypassSaveAsync/ConfirmBypassAsync/CancelPendingAsync; tests call the inner methods directly). So an instant bypass can be executed for a worker who already has a 'Menunggu'/'Siap' CL-B(b) pending if a future caller invokes ExecuteInstantBypassAsync directly, which would move the worker while a pending still references the (now deactivated) source assignment â€” leaving an orphaned 'Siap' pending whose confirm/cancel logic assumes the source is still active.

**Dampak:** Latent: no current exploit path, but the public surface allows bypassing the double-pending invariant. A future controller/service wiring directly to ExecuteInstantBypassAsync would silently break the pending state machine (worker moved while pending Siap â†’ ConfirmBypassAsync stale-check rejects, leaving a stuck pending).

**Bukti:** Services/ProtonBypassService.cs:96 (public ExecuteInstantBypassAsync, no D-10 check), 256-259 (D-10 only in BypassSaveAsync), 261-263 (dispatch); call-site scan shows no production caller other than BypassSaveAsync.

**Usulan fix:** Make ExecuteInstantBypassAsync and ExecutePendingBypassAsync private (or internal for tests via InternalsVisibleTo), forcing all callers through BypassSaveAsync; or replicate the D-10 active-pending check inside ExecuteInstantBypassAsync.

**Verifikasi adversarial:** Verified every factual claim against Services/ProtonBypassService.cs and call-sites.

(1) ExecuteInstantBypassAsync IS public (line 96) and does NOT contain the D-10 double-pending check. Its only guards are E8 'exactly 1 active assignment' (lines 104-118) plus the pure BypassValidator. There is no query against PendingProtonBypasses for an active 'Menunggu'/'Siap' pending â€” confirmed by reading lines 96-215. ExecutePendingBypassAsync (line 272) is also public and also lacks an app-level D-10 check, BUT it is backstopped by the DB filtered unique index IX_PendingProtonBypasses_CoacheeId_ActiveUnique (Data/ApplicationDbContext.cs:430-433, filter Status IN ('Menunggu','Siap')) which it explicitly catches at lines 377-385. The instant path inserts NO pending row, so that index does NOT protect it.

(2) D-10 lives only in BypassSaveAsync (lines 255-259), executed before the dispatch at lines 261-263.

(3) Call-site scan (Grep across repo): the controller (Controllers/ProtonDataController.cs:1647/1674/1692) calls only BypassSaveAsync / ConfirmBypassAsync / CancelPendingAsync. The only other references to ExecuteInstantBypassAsync/ExecutePendingBypassAsync are (a) the service itself, (b) a comment in ApplicationDbContext.cs:425, and (c) HcPortal.Tests/ProtonBypassServiceTests.cs which calls the inner methods directly (lines 99,143,217,239,...). So in production the inner methods are reached ONLY via BypassSaveAsync, which means D-10 IS enforced on every real-world path today. This matches the finding's own statement: 'no current exploit path.'

So the finding is factually accurate: a public method (ExecuteInstantBypassAsync) bypasses the D-10 invariant and, unlike the pending path, has no DB-level backstop. The hypothetical is coherent: a future direct caller could run an instant move while a 'Siap' pending references the now-deactivated source; D-11 stale-check (lines 490-506: sourceStillActive = source!=null where query filters IsActive) would then reject ConfirmBypassAsync. One nuance that slightly softens the 'stuck pending' impact: CancelPendingAsync (lines 560-617) does NOT require the source to be active and can still set the orphaned pending to 'Dibatalkan', so HC can clean it up manually â€” it is not permanently wedged. But the invariant breach (worker moved while an active pending exists) is genuinely possible if a future caller wires to the inner method.

This is correctly NOT refuted: the code does exactly what's claimed, and no guard elsewhere covers the instant path (the filtered unique index only covers the pending path). It is a latent robustness gap, not a live bug.

---

### [BYP:BYP-03] BypassSave controller writes a success-implying audit log even when the service rejects the request

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Proton year-bypass workflow (PendingProtonBypass state machine) |

**Masalah:** In ProtonDataController.BypassSave (Controllers/ProtonDataController.cs:1652-1654) the audit entry 'ProtonBypassSave ... track Xâ†’Y. Alasan: ...' is written UNCONDITIONALLY, after the service call, regardless of result.Success. When BypassSaveAsync rejects (validation failure, E8 violation, double-pending D-10, track-not-found), the service itself writes NO success audit line, but the controller still records an entry that reads as if a bypass was saved. The sibling endpoints BypassConfirm (1677-1679) and BypassCancelPending (1695-1697) embed result.Success/result.Message into their audit message, so their logs are accurate â€” only BypassSave is unconditional and worded as a completed action.

**Dampak:** Audit trail noise / misleading record: a rejected bypass attempt is logged as 'BypassSave {Mode} coachee ...' with sourceâ†’target tracks, which an auditor would read as a successful save. No data corruption, but it degrades the integrity/usefulness of the privileged-action audit log for the bypass feature.

**Bukti:** Controllers/ProtonDataController.cs:1647 (call), 1652-1654 (unconditional audit with success-implying text, no result.Success check); contrast 1677-1679 and 1695-1697 which interpolate the result status into the message.

**Usulan fix:** Gate the audit log on result.Success, or include the outcome in the message (e.g. `$"BypassSave {req.Mode} coachee {req.CoacheeId}: {(result.Success ? "berhasil" : result.Message)}"`) to match the BypassConfirm/BypassCancelPending pattern.

**Verifikasi adversarial:** Confirmed against actual code. In Controllers/ProtonDataController.cs BypassSave: line 1647 calls `_protonBypassService.BypassSaveAsync(bypassReq)`, then lines 1652-1654 unconditionally call `_auditLog.LogAsync(..., "ProtonBypassSave", $"BypassSave {req.Mode} coachee {req.CoacheeId}: track {req.SourceProtonTrackId}â†’{req.TargetProtonTrackId}. Alasan: {req.Reason}", ...)` with NO `result.Success` check and no interpolation of the result â€” the text reads as a completed save. The return Json at 1657-1663 uses result.Success, but the audit line is already written by then regardless of outcome.\n\nThe claim that the service writes no success audit on rejection is also confirmed. In Services/ProtonBypassService.cs BypassSaveAsync (221-264), every rejection is an early return of `new BypassResult(false, ...)` with no _auditLog call: E8 activeCount!=1 (230-231), track-asal mismatch (233-234), source/target track not found (239-240), BypassValidator failure (252-253), and D-10 double-pending (258-259). The success-path audits live only inside ExecuteInstantBypassAsync (line 190, inside committed tx) and ExecutePendingBypassAsync (line 366), reached only after validation passes. So on a service-layer rejection the controller's unconditional success-implying line is the ONLY audit entry â€” making it actively misleading rather than merely redundant.\n\nSibling endpoints are accurate as claimed: BypassConfirm (1677-1679) interpolates `result.Success ? \"berhasil\" : result.Message`; BypassCancelPending (1695-1697) interpolates `result.Success ? \"dibatalkan\" : result.Message`. Only BypassSave is unconditional and worded as a completed action.\n\nMinor scoping correction (does not refute): the controller's own input pre-validation at 1632-1642 (empty CoacheeId/Reason/TargetUnit, invalid Mode) returns BEFORE reaching line 1652, so those rejections do NOT emit the misleading log. The misleading entry only occurs for service-layer rejections (E8, track mismatch, validator, D-10). Impact remains as described: audit-trail noise/misleading privileged-action record, no data corruption.

---

### [CAT:CAT-03] Duplicate-name check is global (Name only) but the DB unique index is (ParentId, Name) â€” legitimate same-named sub-categories under different parents are wrongly rejected

| | |
|---|---|
| **Severity** | LOW (klaim: MED) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** AddCategory (AssessmentAdminController.cs:406) and EditCategory (AssessmentAdminController.cs:464) reject any category whose Name already exists anywhere (`AnyAsync(c => c.Name == name)` / `... && c.Id != id`), with no ParentId scoping. But the EF model declares the uniqueness as composite `(ParentId, Name)` (ApplicationDbContext.cs:640), and downstream code explicitly assumes a sub-category name can appear under multiple parents (RenewalController.cs:74-77 and AssessmentAdminController.cs:767-768 group by Name and resolve via parent, TrainingAdminController.SetTrainingCategoryViewBag dedups by Name). The application is therefore stricter than its own schema/intended model.

**Dampak:** Admins cannot create the same sub-category name (e.g. "K3 Umum") under two different parent categories, even though the schema and resolution logic support and expect it. This blocks a legitimate, intended taxonomy and forces awkward unique renames.

**Bukti:** AssessmentAdminController.cs:406 `AnyAsync(c => c.Name == name)`; :464 same with `c.Id != id`; ApplicationDbContext.cs:640 `HasIndex(c => new { c.ParentId, c.Name }).IsUnique()`; RenewalController.cs:74-77 / AssessmentAdminController.cs:767-768 treat duplicate sub-cat names across parents as expected.

**Usulan fix:** Scope the duplicate check to the same parent: `AnyAsync(c => c.ParentId == parentId && c.Name == name)` (and `&& c.Id != id` for edit), matching the (ParentId, Name) unique index.

**Verifikasi adversarial:** Confirmed the core claim against actual code. AssessmentAdminController.cs:406 (AddCategory) does `await _context.AssessmentCategories.AnyAsync(c => c.Name == name)` and :464 (EditCategory) does `AnyAsync(c => c.Name == name && c.Id != id)` â€” both global Name-only checks with NO ParentId scoping. ApplicationDbContext.cs:640 declares `entity.HasIndex(c => new { c.ParentId, c.Name }).IsUnique()` â€” a composite index that explicitly permits the same Name under different ParentId. So the application-level validation IS stricter than its own schema: an admin cannot create/rename a sub-category to a name used anywhere, even under a different parent that the unique index would allow. SetCategoriesViewBag (line 358-364) confirms a real 2-level parent/child taxonomy exists and sub-categories with ParentId are a supported, exercised path. The defect is genuine.\n\nCaveats that moderate severity: (1) Two evidence citations for 'intended support' are imprecise. AssessmentAdminController.cs:767-768 groups PARENT categories (`ParentId == null`), not sub-categories across parents, so it does not support the claim. TrainingAdminController.SetTrainingCategoryViewBag:43-46 also dedups PARENT categories only. (2) The downstream sites that DO resolve sub-category nameâ†’parent (RenewalController.cs:74-77, AdminBaseController.cs:140-143, and SertifikatRow.BuildParentNameLookup in Models/CertificationManagementViewModel.cs:72-80) all do `GroupBy(c => c.Name).ToDictionary(g => g.Key, g => byId[g.First().ParentId].Name)` â€” they tolerate duplicate sub-cat names but COLLAPSE them to one arbitrary parent. So if duplicate sub-category names across parents actually existed, parent resolution would silently mis-resolve. This means the 'intended and fully supported' framing is overstated â€” the current over-strict guard is in the fail-safe direction and actually prevents a latent name-resolution ambiguity. Net: a real correctness/consistency mismatch (validation tighter than schema), admin-only, no data corruption, and the 'fix' would need the resolution logic keyed by Id first. Hence LOW rather than MED.

---

### [CAT:CAT-06] DeleteCategory leaves sessions/training records referencing the deleted category as an orphaned string

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Categories + sub-categories + assessment type/jenis integrity |

**Masalah:** DeleteCategory (AssessmentAdminController.cs:493-526) blocks deletion when sub-categories exist (line 498) and catches DbUpdateException for FK-protected relations (line 510). But because sessions/training records reference the category only by denormalized string (not FK), deleting a category that is still used by sessions succeeds silently â€” no FK blocks it. After deletion the category no longer exists in AssessmentCategories, yet sessions retain the now-orphaned Category string. The filter dropdown built from session strings (AssessmentAdminController.cs:232-237) keeps showing the name, but it no longer maps to any DefaultPassPercentage/Signatory/Active config.

**Dampak:** A category referenced by live assessments/records can be deleted, dropping its passing-grade default and signatory configuration while historical records still carry the name. Renewal/grade-resolution that looks up the category by name (RenewalController.cs:69-83, AssessmentAdminController.cs:764-770) silently falls back to hardcoded defaults for those records.

**Bukti:** AssessmentAdminController.cs:493-526 (only sub-category + FK-exception guards, no string-usage check); no FK from AssessmentSession.Category (AssessmentSession.cs:16) or TrainingRecord.Kategori (TrainingRecord.cs:16) to the table.

**Usulan fix:** Before deleting, check whether any AssessmentSession.Category / SubKategori or TrainingRecord.Kategori / SubKategori still equals the category name and block with a friendly message (or require reassignment), consistent with the sub-category guard already present.

**Verifikasi adversarial:** Confirmed against actual code. (1) DeleteCategory (Controllers/AssessmentAdminController.cs:493-526) has only two guards: sub-category existence (line 498, AnyAsync(c => c.ParentId == id)) and catch (DbUpdateException) (line 510). No check for string-based usage by sessions/training. (2) No FK exists from sessions/training to AssessmentCategories: AssessmentSession.Category (Models/AssessmentSession.cs:16) and TrainingRecord.Kategori (Models/TrainingRecord.cs:16) are plain string/string? with no navigation property; AssessmentCategory (Models/AssessmentCategory.cs) navigates only to Parent/Children/Signatory; and the DbContext (Data/ApplicationDbContext.cs:637-656) wires ONLY the self-referencing Parent FK (OnDelete.Restrict) and Signatory FK (OnDelete.SetNull). Since nothing references the category row from sessions/training, the catch(DbUpdateException) will never trigger for those string usages, so deleting an in-use category succeeds silently. (3) The filter dropdown (lines 229-238) is built from AssessmentSessions.Select(a => a.Category).Distinct(), so it keeps showing the orphaned name after deletion. (4) Grade/category resolution looks up by name with hardcoded fallbacks: RenewalController.cs:69-83 (rawToDisplayMapHist with MANDATORY/PROTON fallbacks) and AssessmentAdminController.cs:764-770 (catsForRenewal same pattern); after deletion these name lookups miss and fall back. DefaultPassPercentage defaults to 70 (Models/AssessmentCategory.cs:13, DbContext:643). I searched for any pre-delete usage guard and found none. The finding is accurate as written.

---

### [CERT:CERT-03] NomorSertifikat uniqueness is computed/enforced only within AssessmentSessions; KPB/seq/month/year numbers can collide with manually-entered TrainingRecord numbers

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Certificate generation + number + renewal chain + expiry + cascade-delete |

**Masalah:** Auto cert numbers use the shared namespace KPB/{seq:D3}/{romanMonth}/{year} (CertNumberHelper.Build:20-21). The next sequence is derived by scanning ONLY AssessmentSessions.NomorSertifikat (GetNextSeqAsync:25-34) and the unique index exists ONLY on AssessmentSessions.NomorSertifikat (ApplicationDbContext.cs:226-229; migration AddNomorSertifikatToAssessmentSessions.cs:19-24). TrainingRecords also carry NomorSertifikat (admin/import-supplied, TrainingAdminController.cs:1465/400/369) with NO unique index and NO cross-table check. Consequently the generator can mint KPB/005/I/2026 for an assessment even though a TrainingRecord already holds that exact number, producing two distinct certificates sharing one number with nothing to detect it. The retry/dup-key handling (IsDuplicateKeyException matches IX_AssessmentSessions_NomorSertifikat only) is blind to TrainingRecords.

**Dampak:** Two different certificates (one assessment, one training) can carry an identical official NomorSertifikat with no integrity error, undermining the number's role as a unique certificate identifier for verification/audit.

**Bukti:** CertNumberHelper.cs:20-34 (Build + GetNextSeqAsync scans only AssessmentSessions); CertNumberHelper.cs:37-42 (dup-key check matches AssessmentSessions index only); ApplicationDbContext.cs:226-229 (unique index only on AssessmentSessions.NomorSertifikat); ApplicationDbContext.cs:159-185 (TrainingRecord config has FK + check constraint but no NomorSertifikat unique index); TrainingAdminController.cs:1465 (TrainingRecord.NomorSertifikat set from import sheet text)

**Usulan fix:** Either make GetNextSeqAsync scan both AssessmentSessions and TrainingRecords for the KPB/.../year prefix when computing the next sequence, or add a cross-table guard (e.g. application-level uniqueness check) when generating, and validate admin-entered TrainingRecord numbers do not reuse the KPB auto namespace.

**Verifikasi adversarial:** All claimed code behaviors are confirmed against the actual source.

1. Shared namespace + AssessmentSessions-only sequence scan: CertNumberHelper.cs:20-21 `Build` produces `KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}`. GetNextSeqAsync (lines 23-35) computes the next seq by querying ONLY `context.AssessmentSessions.Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))` â€” TrainingRecords are never consulted.

2. Dup-key handler is AssessmentSessions-blind: IsDuplicateKeyException (lines 37-42) matches only `IX_AssessmentSessions_NomorSertifikat` (plus generic SQL codes 2601/2627, which would still only fire on the AssessmentSessions index since no other unique index on this column exists).

3. Unique index exists ONLY on AssessmentSession: ApplicationDbContext.cs:225-229 declares the filtered unique index `IX_AssessmentSessions_NomorSertifikat_Unique` on `AssessmentSession.NomorSertifikat`; migration AddNomorSertifikatToAssessmentSessions.cs:19-24 creates exactly that. The TrainingRecord entity config (ApplicationDbContext.cs:158-185) has FK relationships + CK_TrainingRecord_RenewalChain check constraint but NO index on NomorSertifikat. TrainingRecord.cs:51 carries `NomorSertifikat` with only `[MaxLength(100)]` â€” no [Index]/unique annotation. Grep for any TrainingRecords NomorSertifikat unique index returned zero matches across model, fluent config, and migrations.

4. TrainingRecord numbers are admin/import-supplied with no cross-table validation: TrainingAdminController.cs:1465 sets `NomorSertifikat = nomorSertifikat` directly from Excel cell text (row.Cell(12), line 1410) inside the import loop with no collision check; create path lines 369/400 and edit line 555 set it directly from the view model. A grep for any query checking TrainingRecords for NomorSertifikat collisions found NONE â€” every `NomorSertifikat ==`/`Where(... NomorSertifikat ...)` query in the codebase (GradingService.cs:284/518, AssessmentAdminController.cs:3609) is scoped to AssessmentSessions only.

5. The generator is actively used to mint such numbers: GradingService.cs:282-288 calls GetNextSeqAsync + Build and writes to AssessmentSession; the surrounding retry (lines 290-293) catches only IsDuplicateKeyException. So an auto-minted `KPB/NNN/.../year` can exactly equal a pre-existing TrainingRecord.NomorSertifikat and will save successfully with no error and nothing to detect it.

I tried to refute via: (a) an [Index] data annotation on the model â€” absent; (b) a cross-table AnyAsync/Where guard on insert â€” absent; (c) the generic 2601/2627 catch accidentally covering TrainingRecords â€” refuted because no unique index on TrainingRecords.NomorSertifikat exists to raise those, so collisions never throw. No mitigation found.

Severity: LOW is appropriate. NomorSertifikat is nullable and used as a certificate identifier, but a collision requires the auto-generated assessment seq to coincide with a manually-entered training number in the same month/year; it is a data-integrity/uniqueness-guarantee gap rather than a security or availability issue, and there is no automated cross-table verification flow that hard-depends on global uniqueness.

---

### [CERT:CERT-04] Cascade cert-file deletion lacks the webroot-containment check used elsewhere (path-traversal defense-in-depth gap)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Certificate generation + number + renewal chain + expiry + cascade-delete |

**Masalah:** RecordCascadeDeleteService deletes physical cert files post-commit by combining WebRootPath with the stored URL and calling File.Delete without verifying the resolved path stays inside wwwroot (RecordCascadeDeleteService.cs:293-301). Compare the file-serve path CDPController.GuidanceDownload which DOES guard: Path.GetFullPath + StartsWith(WebRootPath) before serving (CDPController.cs:244-248). FileUploadHelper.DeleteFile similarly has no containment check (FileUploadHelper.cs:114-122). In current code SertifikatUrl/ManualSertifikatUrl are always produced by FileUploadHelper.SaveFileAsync (safe generated names, TrainingAdminController.cs:320,751; import path does not set a URL), so this is not presently exploitable â€” but it is a missing defense-in-depth: any future code path that lets these URL columns hold admin/external text (e.g. an import column or API) would allow a crafted '..' value to delete files outside wwwroot during cascade.

**Dampak:** No current exploit because cert URLs are server-generated, but the delete primitive is unsafe by construction; a future change introducing admin-controlled cert URLs would turn cascade-delete into an arbitrary-file-delete vector.

**Bukti:** RecordCascadeDeleteService.cs:297-298 (Path.Combine(WebRootPath, url.TrimStart('/')...) then File.Delete with no GetFullPath/StartsWith containment check); CDPController.cs:244-248 (correct containment guard for the analogous serve path); FileUploadHelper.cs:114-122 (DeleteFile also unguarded)

**Usulan fix:** Add the same containment check used in GuidanceDownload before File.Delete in RecordCascadeDeleteService (and FileUploadHelper.DeleteFile): resolve Path.GetFullPath and verify it StartsWith the canonicalized WebRootPath; skip+log otherwise.

**Verifikasi adversarial:** All claims in the finding are confirmed against the actual code.

1) RecordCascadeDeleteService.cs:297-298 â€” post-commit cert deletion does `var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (System.IO.File.Exists(path)) System.IO.File.Delete(path);` with NO Path.GetFullPath + StartsWith(WebRootPath) containment check. The url values come straight from certPaths, which is populated at lines 265/276 from record.SertifikatUrl / m.SertifikatUrl with only an IsNullOrEmpty check â€” no path validation. _env is IWebHostEnvironment (line 26), same source as the CDP guard. Notably the inline comment at line 292 claims "confined webroot" but the code does NOT confine it â€” a comment/code mismatch that reinforces the gap.

2) CDPController.cs:244-248 (GuidanceDownload) DOES guard the analogous serve path: builds physicalPath via the same Path.Combine pattern, then `var fullPath = Path.GetFullPath(physicalPath); if (!fullPath.StartsWith(_env.WebRootPath, StringComparison.OrdinalIgnoreCase)) return BadRequest("Invalid file path.");`. Confirms the asymmetry the finding describes.

3) FileUploadHelper.cs:114-122 (DeleteFile) is also unguarded: `var oldPath = Path.Combine(webRootPath, relativeUrl.TrimStart('/')); if (File.Exists(oldPath)) File.Delete(oldPath);` â€” no containment check. Confirmed.

Not presently exploitable (confirms the LOW severity / defense-in-depth framing): every production write-site of SertifikatUrl/ManualSertifikatUrl traces to FileUploadHelper.SaveFileAsync, which generates safe names (timestamp + GUID + Path.GetFileName-stripped original; line 100) â€” TrainingAdminController lines 337/372/403 (wcUrl/sertifikatUrl via SaveFileAsync), 542/1052 (uploadedUrl), 751 (certUrl via SaveFileAsync at 737). I also checked both import paths: ImportTraining Assessment branch (1352-1382) and Training branch (1452-1466) read NO certificate-URL column and never set SertifikatUrl/ManualSertifikatUrl; the bulk backfill likewise builds AssessmentSession without a cert URL. So no current code path lets these columns hold attacker/admin-controlled traversal text â€” matching the finding's own "not presently exploitable" caveat.

Conclusion: the described condition (unguarded delete primitive lacking the webroot-containment check applied on the serve side) is real and accurately cited; it is a genuine defense-in-depth gap rather than a live vulnerability. No guard elsewhere refutes it â€” certPaths intake only null-checks, and there is no GetFullPath/StartsWith anywhere in the delete flow.

---

### [CERT:CERT-05] Double-renewal prevention is a check-then-act with no DB constraint â€” concurrent renewals of the same source cert can both succeed

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | race |
| **Confidence** | medium |
| **Area** | Certificate generation + number + renewal chain + expiry + cascade-delete |

**Masalah:** Renewal creation guards against renewing an already-renewed source via AnyAsync queries on Renews*Id before insert (TrainingAdminController.cs:243-253; AssessmentAdminController.cs:990-1003), and FK mutual-exclusion is backed by a DB check constraint (ApplicationDbContext.cs:182-184). However there is NO unique index/constraint on RenewsSessionId or RenewsTrainingId, and the 'already renewed' guard is a read-before-write. Two concurrent admin renewal POSTs targeting the same source cert can both pass the AnyAsync check and both insert, producing two renewal records for one source. This corrupts the renewal chain (the single-renewal invariant the dashboard relies on for IsRenewed suppression and the Union-Find chain grouping).

**Dampak:** Rare but possible under concurrent admin actions: a source certificate gets two renewals, breaking the one-to-one renewal chain assumption and producing inconsistent expiry-suppression/chain grouping in the cert dashboards and history.

**Bukti:** TrainingAdminController.cs:243-246,250-253 (AnyAsync check then insert, no locking); AssessmentAdminController.cs:990-1003 (same pattern); ApplicationDbContext.cs:166-184 (FK NoAction + XOR check constraint, but no uniqueness on Renews*Id)

**Usulan fix:** Add a filtered unique index on RenewsSessionId (where not null) and on RenewsTrainingId (where not null) across the relevant tables, or perform the check+insert inside a transaction with appropriate locking, and surface the resulting duplicate-key as the existing 'sudah di-renew' validation message.

**Verifikasi adversarial:** I confirmed every claim against the actual code.

CHECK-THEN-ACT (no DB constraint): TrainingAdminController.cs:243-253 does `srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(...RenewsTrainingId==...) || await _context.AssessmentSessions.AnyAsync(...)` and only AddModelError on hit; the actual insert is later at lines 388-409 (`_context.TrainingRecords.Add(record); await _context.SaveChangesAsync();`). The same pattern is in AssessmentAdminController.cs:990-1003. These are separated read-then-write with no transaction wrapping the check+insert and no row lock.

NO UNIQUE CONSTRAINT: ApplicationDbContext.cs:166-184 (TrainingRecord) and 231-249 (AssessmentSession) configure RenewsTrainingId/RenewsSessionId only as nullable FKs with OnDelete(NoAction) plus a XOR check constraint ("[RenewsTrainingId] IS NULL OR [RenewsSessionId] IS NULL"). Grep for HasIndex/IsUnique confirms unique indexes exist for other columns (NomorSertifikat line 226-229, Label line 147, ProtonTrack composites, etc.) but NONE on Renews*Id. So the DB cannot reject a second renewal of the same source.

NO LOCKING / DEFAULT ISOLATION: Grep across the repo for IsolationLevel/Serializable/lock(/SemaphoreSlim/UPDLOCK/HOLDLOCK found nothing in the renewal path. EF Core default is implicit READ COMMITTED, which does not block a concurrent uncommitted insert, so two concurrent admin POSTs can both pass AnyAsync and both insert.

CLAIMED IMPACT IS REAL (with one nuance): The Union-Find chain grouping in RenewalController.cs:154-202 builds chain edges directly from the renewal FKs (Union(key, $"AS:{RenewsSessionId}") etc.) and the display assumes a linear chain (picks oldest/newest, orders by ValidUntil). Two renewals of one source create a forked chain -> ambiguous/inconsistent history rendering. The dashboard IsRenewed suppression (CMPController.cs:3774-3834,3894 via HashSet membership on Renews*Id values) would still suppress the single source, but the dashboard would now show TWO active renewal certificates for one source cert â€” corrupting the one-to-one renewal invariant exactly as claimed.

No guard/handler elsewhere refutes this; RenewalValidationTests.cs has no concurrency/double-renewal-uniqueness test.

Severity: genuinely LOW â€” exploitable only by two near-simultaneous admin (Admin/HC role) renewal POSTs targeting the same source; not externally triggerable, requires privileged actors and tight timing, and is data-integrity (not security) impact. The check-then-act guard handles the common sequential case.

---

### [EDT:EDT-03] MultipleChoice multi-select not enforced server-side; non-deterministic scoring via HashSet.First() on crafted POST

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Admin edit-jawaban + regrade + Pass<->Fail cert/Proton cascade |

**Masalah:** SubmitEditAnswers sanitizes submitted option IDs only against the question's valid option set (AssessmentAdminController.cs:3069-3070) but does not enforce single-selection for MultipleChoice questions. The UI uses radio inputs for MC (Views/Admin/EditPesertaAnswers.cshtml:68), so this is unreachable via the normal UI, but a crafted POST can submit >1 option for an MC question. The scorer then evaluates MC as mcSel.First() (GradingService.cs:372 / :406) over a HashSet, whose enumeration order is not guaranteed, so the correct/incorrect outcome (and thus the persisted score) can be non-deterministic and inconsistent with the GET-page correctness display, which requires selectedIds.Count == 1 (AssessmentAdminController.cs:2985).

**Dampak:** A malformed/crafted edit POST storing two options for an MC question yields non-deterministic scoring and can persist multiple PackageUserResponse rows for a single MC question, diverging from how the worker's original answer is modeled. Low likelihood (admin-only, antiforgery-protected, UI uses radios) and limited blast radius.

**Bukti:** No MC arity guard in SubmitEditAnswers: AssessmentAdminController.cs:3069-3099 only filters by validOptionIds. Scorer uses HashSet.First() for MC: GradingService.cs:372 and :406. GET-page MC correctness requires Count==1: AssessmentAdminController.cs:2985. UI uses radio for MC: Views/Admin/EditPesertaAnswers.cshtml:68.

**Usulan fix:** In SubmitEditAnswers, for QuestionType MultipleChoice take at most one sanitized option (reject or truncate to a single selection) before writing responses and logging, matching the radio semantics; or validate arity and return an error when >1 option is submitted for an MC question.

**Verifikasi adversarial:** Verified every cited claim against the actual code and could not refute it.

1) Model permits multi-select per question: Models/EditAnswersSubmission.cs:7 declares `public Dictionary<int, List<int>> Answers` (qId -> optionIds), so MVC model binding can populate >1 option ID for a MultipleChoice question from a crafted POST.

2) No MC arity guard in SubmitEditAnswers: AssessmentAdminController.cs:3069-3070 only does `validOptionIds = q.Options.Select(o=>o.Id).ToHashSet()` then `sanitizedNewSet = newOptionIds.Where(id => validOptionIds.Contains(id)).ToHashSet()`. There is no check enforcing exactly 1 selection for MultipleChoice. The persist loop at 3105-3115 adds one PackageUserResponse per element of sanitizedNew, so >1 row IS persisted for a single MC question. Confirmed.

3) Scorer uses HashSet.First() for MC: GradingService.cs:350 `HashSet<int> SelectedOptions(int qId)` returns a HashSet; lines 369-374 (totalScore) and 403-407 (ET scores) do `var mcSel = SelectedOptions(qId); if (mcSel.Count > 0) { ... mcSel.First() ... }`. So scoring of a 2-row MC depends on arbitrary HashSet enumeration order, which is not contractually guaranteed â€” non-deterministic / arbitrary-pick. Confirmed at the exact cited lines 372 and 406.

4) GET-page divergence: AssessmentAdminController.cs:2985 `"MultipleChoice" => selectedIds.Count == 1 && correctIds.Contains(selectedIds[0])` â€” a 2-selection MC renders as Salah on the GET page, which can diverge from the scorer's .First()-based correct/incorrect verdict. Confirmed.

5) UI uses radio for MC: Views/Admin/EditPesertaAnswers.cshtml:68 `type="radio"` for the MultipleChoice branch (checkbox only in the MultipleAnswer else-branch at line 88), so the path is unreachable via normal UI and needs a crafted POST. Confirmed.

6) No upstream guard refutes it: RegradeAfterEditAsync (GradingService.cs:437-449) recomputes with overrideAnswers=null, re-reading the now-multi-row DB state through the same SelectedOptions HashSet + .First() path. No dedup/arity normalization exists anywhere between persistence and grading.

Mitigating context (already reflected in the finding): endpoint is [Authorize(Roles="Admin, HC")] + [ValidateAntiForgeryToken] (AssessmentAdminController.cs:3018-3021) and gated by AssessmentEditEligibility.IsEditableAsync, so exploitation requires a privileged authenticated actor deliberately crafting a malformed POST. Blast radius is limited to that admin-edited session's stored score. This is a real defense-in-depth / robustness gap, correctly rated LOW.

---

### [ESS:ESS-04] SubmitEssayScore does not verify the target question is an Essay type before writing EssayScore

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Essay manual-grade lifecycle (Submit/Finalize/Recompute) |

**Masalah:** SubmitEssayScore loads the question and validates only the score range against question.ScoreValue (AssessmentAdminController.cs:3437-3443); it never checks question.QuestionType == 'Essay'. An Admin/HC could POST a questionId belonging to an MC/MA question (that has a response row) and set EssayScore on that row. The scoring aggregator only reads EssayScore inside the `case "Essay"` branch (Helpers/AssessmentScoreAggregator.cs:52-55), so the stray EssayScore is ignored by scoring and the data does not corrupt the total â€” but it does write a meaningless value and the endpoint silently accepts it.

**Dampak:** Low: no score miscalculation (the value is never read for non-essay questions), but it is an unguarded write with no type validation and could mislead future code that does read EssayScore generically. Defensive gap rather than an active data-integrity break.

**Bukti:** AssessmentAdminController.cs:3437-3447 (loads question, range-checks, writes EssayScore with no QuestionType guard); aggregator reads EssayScore only for Essay-typed questions: Helpers/AssessmentScoreAggregator.cs:52-55

**Usulan fix:** Add `if (question.QuestionType != "Essay") return Json(new { success = false, message = "Soal bukan tipe Essay" });` after loading the question.

**Verifikasi adversarial:** Confirmed against the actual code. SubmitEssayScore (Controllers/AssessmentAdminController.cs:3428-3457): it loads the response (L3431-3434), loads the question via FindAsync (L3437-3439), validates ONLY the score range against question.ScoreValue (L3442-3443: `if (score < 0 || score > question.ScoreValue)`), then writes `response.EssayScore = score` (L3446) and saves. There is NO `question.QuestionType == "Essay"` guard anywhere in the method â€” exactly as the finding claims. PackageQuestion.QuestionType exists (Models/AssessmentPackage.cs:48), so the missing guard is feasible and genuinely absent. The endpoint is reachable by Admin/HC (L3426 `[Authorize(Roles = "Admin, HC")]`); a POST with a questionId belonging to an MC/MA question that has a response row would write a stray EssayScore.

The mitigating claim also holds: the scoring aggregator (Helpers/AssessmentScoreAggregator.cs:52-55) reads EssayScore ONLY inside `case "Essay":`, so a stray EssayScore on an MC/MA question is never summed into totalScore â€” no score miscalculation. MC questions score via the `case "MultipleChoice"` option-correctness path (L38-45) and MA via `case "MultipleAnswer"` (L46-50), neither of which touches EssayScore. So the data-integrity total is not corrupted.

One nuance partially supporting the finding's "could mislead future code that reads EssayScore generically" note: two export readers DO read EssayScore without a QuestionType gate â€” ExcelExportHelper.cs:110-111 and AssessmentAdminController.cs:4987 â€” but both are inside an `else if (!string.IsNullOrEmpty(response.TextAnswer))` branch reached only when PackageOptionId is null. A normal MC/MA response has PackageOptionId set, so it takes the option branch first and never reaches the EssayScore read. Thus practical export impact requires an unusual data state (TextAnswer present + no option on an MC/MA question), keeping real impact low. No guard/handler elsewhere refutes the core claim that SubmitEssayScore writes EssayScore with no type validation.

---

### [GAIN:GAIN-02] Normalized gain forced to 100 when Pre=100 masks regressions (Post<Pre)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | correctness |
| **Confidence** | medium |
| **Area** | Pre/Post gain-score + per-ElemenTeknis scoring |

**Masalah:** The normalized gain formula uses (Post-Pre)/(100-Pre)*100 with the div-by-zero guard 'preScore >= 100 ? 100'. When Pre=100 the gain is unconditionally set to 100 regardless of Post. If a worker scored 100 on the Pre but regressed on the Post (e.g., Post=50), the computed gain is +100 (maximum positive), which is then rendered green as '+100%' while the same table row still displays Post=50%. This is mathematically a divide-by-zero sentinel, but semantically it reports a regression as a perfect gain. Applies identically at every gain site.

**Dampak:** For workers/elements with a perfect Pre score, the gain dashboards, Results comparison table, and Excel exports report +100% gain even when the Post score dropped, hiding genuine competency regressions and inflating aggregate average gain (the regression row contributes +100 instead of a negative value).

**Bukti:** Per-ET: CMPController.cs:2390-2392 (preScore >= 100 ? 100 : ...). Trend: CMPController.cs:2682, GetAnalyticsSummary avg: 2806, GetTrendData: 2912, per-worker: 3385, per-elemen: 3419, Excel: 3586 and 3624. View renders +100% green even when Post<Pre: Views/CMP/Results.cshtml:165-166 (shows real Post) vs 176-180 (gain>0 â†’ text-success '+...%').

**Usulan fix:** When Pre>=100 there is no headroom to measure normalized gain; treat it as not-applicable (null/'â€”' with a tooltip) rather than +100, or special-case Post<Pre to surface the actual delta. At minimum exclude Pre=100 rows from average-gain aggregation so they don't inflate the mean.

**Verifikasi adversarial:** All cited evidence is confirmed verbatim in the actual code. The normalized gain formula `(Post-Pre)/(100-Pre)*100` with the div-by-zero guard appears at every site claimed:

- Per-ET (Results action): CMPController.cs:2390-2392 â€” `gainScore = preScore >= 100 ? 100 : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);`
- Trend (GetAnalyticsData): :2682 â€” `double gain = pre >= 100 ? 100 : ...`
- GetAnalyticsSummary avg: :2806 â€” same pattern
- GetTrendData: :2912 â€” same pattern
- Per-worker (GetGainScoreData): :3385 â€” same pattern, comment "PreScore = 100 â†’ Gain = 100 (WKPPT-06)"
- Per-elemen: :3419 â€” same pattern
- Excel per-worker: :3586; Excel per-elemen: :3624 â€” same pattern

In every case, when Pre>=100 the gain is unconditionally `100` with no reference to Post. I confirmed via computation that Pre=100/Post=50 yields gain=100 (the true formula numerator is -50, i.e. a regression, but the guard masks it to +100).

The View confirms the rendering claim: Views/CMP/Results.cshtml:166 renders the real Post score (`@($"{row.PostScore:0.#}%")`), while :176-180 renders any `gain > 0` (which 100 satisfies) as `text-success` green `+100%`. So a regression row (Post=50, Pre=100) shows "Post 50%" alongside green "+100%".

I tried to refute it three ways and could not:
1. No upstream filter excludes regression rows (Post<Pre). The queries (e.g., :3362-3375, :2364-2370) select all Completed Pre/Post sessions with Score, paired by UserId/ElemenTeknis; no Post>=Pre guard exists anywhere.
2. The scenario is reachable: Score is a real nullable value and ET scores are CorrectCount/QuestionCount*100, both of which legitimately equal 100 (all-correct Pre) and can drop on Post.
3. No alternate handler/clamp downstream â€” the value flows straight into ViewBag/JSON/Excel and into aggregate `Average()` (e.g., :2809, :2920, :2690), so a regression contributes +100 instead of a negative value, inflating averages exactly as claimed.

The math vs semantics distinction the finding draws is accurate: the `>=100 ? 100` is a divide-by-zero sentinel, but it reports a perfect-Pre regression as a maximum positive gain.

---

### [GAIN:GAIN-03] GetGainScoreData / ExportGainScoreExcel ToDictionaryAsync(UserId) throws if a user has >1 completed PostTest in a group (no DB uniqueness)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | low |
| **Area** | Pre/Post gain-score + per-ElemenTeknis scoring |

**Masalah:** Both GetGainScoreData and ExportGainScoreExcel build postSessionDict via ToDictionaryAsync(s => s.UserId) over all completed PostTest sessions with LinkedGroupId == assessmentGroupId. ToDictionaryAsync throws ArgumentException on duplicate keys. There is no unique constraint on (LinkedGroupId, UserId, AssessmentType) â€” only a non-unique index on LinkedGroupId (ApplicationDbContext.cs:203). The normal create flow (each create gets a brand-new LinkedGroupId = preSessions[0].Id) and the edit/add-participant path (which excludes existing users via existingUserIds, AssessmentAdminController.cs:1841-1842) keep one Post per user per group, so in practice this is not reachable today. But any data-integrity slip (manual SQL, future re-linking, or a code path that assigns an existing group to a second Post for the same user) turns the entire Gain Score report and its Excel export into an unhandled 500 for that group, rather than a graceful degrade.

**Dampak:** If duplicate Post sessions ever exist for one user in a group, the per-worker gain report (GET /CMP/GetGainScoreData) and the Gain Score Excel export (GET /CMP/ExportGainScoreExcel) crash with a 500 for that group instead of rendering, blocking HC from viewing/exporting gain analytics.

**Bukti:** CMPController.cs:3370-3375 (ToDictionaryAsync(s => s.UserId)) and 3564-3569 (same in ExportGainScoreExcel). No unique index: ApplicationDbContext.cs:203 (only HasIndex(a => a.LinkedGroupId), non-unique). Create always new group: AssessmentAdminController.cs:1248. Add-participant dedupes: AssessmentAdminController.cs:1841-1842.

**Usulan fix:** Replace ToDictionaryAsync(s => s.UserId) with a defensive grouping that picks one Post per user deterministically (e.g., .GroupBy(s => s.UserId).Select(g => g.OrderByDescending(x => x.CompletedAt).First())) or add a filtered unique index on (LinkedGroupId, UserId) for PostTest/PreTest to guarantee the invariant.

**Verifikasi adversarial:** All citations verified against the actual code. (1) CMPController.cs:3370-3375 (GetGainScoreData) and 3564-3569 (ExportGainScoreExcel) both build postSessionDict via ToDictionaryAsync(s => s.UserId, s => s) over completed PostTest sessions filtered by LinkedGroupId == assessmentGroupId; I read both full method bodies (3357-3449 and 3554-3635) and confirmed neither has a try/catch â€” duplicate UserId keys would throw ArgumentException and propagate. (2) ApplicationDbContext.cs:203 is HasIndex(a => a.LinkedGroupId) â€” non-unique; no unique constraint on (LinkedGroupId, UserId, AssessmentType) exists in the entity config (read 190-219, grep confirms line 203 is the only LinkedGroupId index). (3) Program.cs:165-172 confirms the impact: in production UseExceptionHandler("/Home/Error") + UseStatusCodePagesWithReExecute turn the unhandled exception into a 500/error page rather than a graceful degrade. The not-reachable-today qualifier is also accurate â€” I traced every LinkedGroupId assignment: create (AssessmentAdminController.cs:1248) makes a brand-new group; add-participant (1841-1842, 1896-1947) dedupes to newUserIds only; the auto-pair path that reuses an existing LinkedGroupId (line 866) runs only in the standard branch which hardcodes AssessmentType = "Standard" (line 1451), never "PostTest"; BulkBackfill (TrainingAdminController.cs:947) sets AssessmentType = Manual. Since the Gain Score query filters strictly AssessmentType == "PostTest", no current code path can produce a duplicate PostTest row per (LinkedGroupId, UserId). The defect is a genuine latent robustness gap reachable only via data-integrity slip (manual SQL, future re-linking), exactly as the finding states.

---

### [GRD:GRD-02] Empty-answer MA all-or-nothing (SetEquals) lacks the selection>0 guard present in the display/review paths

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | drift |
| **Confidence** | medium |
| **Area** | Grading engine (MC/MA/Essay scoring, maxScore denominator) |

**Masalah:** Authoritative grading awards full ScoreValue for a MultipleAnswer question when selectedOptionIds.SetEquals(correctOptionIds) (Services/GradingService.cs:116; ComputeScoreAndETInternalAsync at L380; AssessmentScoreAggregator.cs:50). None of these guard against the empty-vs-empty case. If a MA question ever has zero correct options (correctOptionIds empty) and the worker selected nothing (selectedOptionIds empty), SetEquals returns true and full credit is awarded. The Results/review code paths explicitly guard this (CMPController.cs:2186 'selectedOptionIds.Count > 0 &&', L2237 'if (selectedIds.Count == 0) continue;', L2273 same), so the authoritative grade and the displayed review would disagree for such a question.

**Dampak:** Only a robustness/drift concern under a data anomaly (a MA question with no correct option). For such a question, a worker who answers nothing would receive full marks in the authoritative grade while the answer-review screen shows it as incorrect â€” a scoring inconsistency. Not reachable through current create/edit/import validation, hence LOW.

**Bukti:** Services/GradingService.cs:116 and 380; Helpers/AssessmentScoreAggregator.cs:50 (no Count>0 guard) versus Controllers/CMPController.cs:2186, 2237, 2273 (explicit Count>0 / Count==0 guards before SetEquals). Reachability is limited: create/edit enforce MA correctCount>=2 (AssessmentAdminController.cs:6332-6336, 6539-6543) and Excel import requires >=1 (L6064-6068), so a zero-correct MA is only possible via legacy/malformed data.

**Usulan fix:** Add a 'correctOptionIds.Count > 0' (and/or selected.Count > 0) precondition before the SetEquals award in all three authoritative MA branches so empty-vs-empty does not award credit, matching the review-path guards. Optionally add a DB integrity check / migration to flag any existing zero-correct MA questions.

**Verifikasi adversarial:** Confirmed every cited line. Authoritative grading uses bare SetEquals with no Count>0 guard: GradingService.cs:116 (`if (selectedOptionIds.SetEquals(correctOptionIds)) totalScore += q.ScoreValue;`), ComputeScoreAndETInternalAsync at GradingService.cs:380 (`if (maSel.SetEquals(maCorrect)) totalScore += q.ScoreValue;`), and AssessmentScoreAggregator.cs:50 (`if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;`). In all three, the correct-set is built from q.Options.Where(o => o.IsCorrect), so it is genuinely empty when no option is marked correct, and HashSet.SetEquals(emptySet, emptySet)==true â†’ full ScoreValue awarded for a no-answer. There is also an unmentioned 4th instance of the same pattern in ET-scoring at GradingService.cs:166. By contrast the review/display paths in CMPController.cs explicitly guard: L2186 `isCorrect = selectedOptionIds.Count > 0 && correctIds.SetEquals(selectedOptionIds);`, L2237 `if (selectedIds.Count == 0) continue;`, L2273 `if (selectedIds.Count == 0) return false;` â€” so a no-answer MA is shown incorrect. Thus the authoritative grade (full credit) and the review screen (incorrect) diverge for a zero-correct MA answered with nothing. Reachability is correctly limited: create (AssessmentAdminController.cs:6332 `correctCount < 2`) and edit (L6539 same) reject MA with fewer than 2 correct; Excel import (L6064 `correctLetters.Count == 0`) rejects MA with zero valid correct letters â€” so a zero-correct MA is only possible via legacy/malformed/direct-DB data. No pre-filter excludes zero-correct questions before grading. Finding's claims, citations, and impact all match the code.

---

### [GRD:GRD-03] Excel import accepts MultipleAnswer with a single correct option while manual create/edit require >=2 â€” inconsistent MA definition

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Grading engine (MC/MA/Essay scoring, maxScore denominator) |

**Masalah:** The bulk Excel import path validates MA correct answers with 'correctLetters.Count == 0' as the only rejection (Controllers/AssessmentAdminController.cs:6064-6068, message 'minimal 1 jawaban benar'), so a MA question can be imported with exactly one correct option. The interactive create (L6332-6336) and edit (L6539-6543) paths require correctCount < 2 -> reject ('membutuhkan minimal 2 jawaban benar'). A single-correct MA still grades correctly under SetEquals (it behaves like a 1-of-N exact match), so this is not a scoring miscalculation, but it is an inconsistent business rule across entry points.

**Dampak:** Inconsistent definition of a valid MultipleAnswer question depending on the entry channel. No incorrect scoring results (SetEquals still works for a single-correct MA), so impact is limited to data-quality/governance, not grading correctness.

**Bukti:** Controllers/AssessmentAdminController.cs:6064-6068 (import: reject only when count==0) versus 6332-6336 (create: reject when count<2) and 6539-6543 (edit: reject when count<2).

**Usulan fix:** Align the import validation with create/edit: require MA imports to have at least 2 correct letters (or, conversely, relax create/edit to >=1 if single-correct MA is intended). Centralize the MA correct-count rule in one helper to avoid future drift.

**Verifikasi adversarial:** Verified all three cited locations in Controllers/AssessmentAdminController.cs exactly as claimed.

IMPORT path (POST ImportPackageQuestions, method begins L5854): L6057-6068 parses MA correct letters via cor.Split filtered to A-D distinct, and the ONLY rejection is `if (correctLetters.Count == 0)` with message "MA soal harus memiliki minimal 1 jawaban benar valid (A-D)." (L6064-6068). So exactly one correct letter passes. Grep across the whole file confirms there is no other MA minimum-count guard in the import flow; lines 6119-6141 just build options (IsCorrect = correctLetters.Contains(letter)) and persist in a transaction. So a single-correct MA is importable.

CREATE path (POST CreateQuestion, begins L6280): L6326 computes correctCount from the 4 bool flags, then L6332-6336 `if (questionType == "MultipleAnswer" && correctCount < 2)` rejects with "membutuhkan minimal 2 jawaban benar." â†’ requires >=2.

EDIT path (POST EditQuestion, begins L6485): L6533/6539-6543 identical `correctCount < 2` rejection. â†’ requires >=2.

Inconsistency confirmed: import floor=1, create/edit floor=2.

Scoring-impact claim also verified in Services/GradingService.cs: every MultipleAnswer branch (L116, L166, L380, L413) grades via SetEquals(correctOptionIds). With a single correct option, SetEquals still requires the worker to select exactly that one option and nothing else, so it grades as a correct 1-of-N exact match â€” no miscalculation. Thus impact is limited to data-quality/governance (inconsistent business rule per entry channel), not grading correctness, exactly as the finding states.

No guard elsewhere refutes the import gap.

---

### [MAN:MAN-04] BulkBackfill silently coerces unparseable/blank Score cells to 0 (recorded as failed)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | high |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** In BulkBackfillAssessment the per-row Score parse initializes `int score = 0` and only overwrites it when the cell parses as double or int (Controllers/TrainingAdminController.cs:869-872). A blank, text, or malformed Score cell therefore silently becomes Score=0, and with IsPassed=Score>=passPercentage (line 941) the row is recorded as a 0/Not-Passed assessment with no warning to the admin. The success/skip report does not flag coerced-zero rows. This is a bulk-import data-integrity hazard: a column-shift in the Excel (e.g. Score actually in col 2, not col 3) imports an entire batch as zeros silently.

**Dampak:** Misformatted or column-shifted Score cells import as 0 / Not-Passed with no error, corrupting historical assessment data in bulk while the UI reports 'Berhasil insert N'.

**Bukti:** Controllers/TrainingAdminController.cs:869-872 (`int score = 0` then conditional overwrite), :941 (`IsPassed = row.Score >= passPercentage`); column mapping NIP=col1, Score=col3 documented at :857 and Views/Admin/BulkBackfill.cshtml:44.

**Usulan fix:** Treat an unparseable non-empty Score cell as a per-row error (collect into a report) rather than defaulting to 0, and surface a count of zero/blank scores in the success message so the admin can verify column mapping.

**Verifikasi adversarial:** Confirmed from Controllers/TrainingAdminController.cs. Lines 869-872: `int score = 0;` then `if (scoreCell.TryGetValue<double>(out var d)) score = (int)d; else if (int.TryParse(scoreCell.GetString().Trim(), out var i)) score = i;`. If the Score cell is blank, text, or otherwise unparseable as double or int, neither branch executes and `score` remains 0 â€” the row is still added to `rows` (line 873) and inserted. Line 941: `IsPassed = row.Score >= passPercentage`, so a coerced 0 becomes Score=0 / IsPassed=false (0 >= 70 is false). The column mapping NIP=col1, Score=col3 is documented at line 857 and Views/Admin/BulkBackfill.cshtml:44 â€” both confirmed verbatim. I read the entire method (846-985) looking for a refuting guard and found none: the only per-row guard is `if (string.IsNullOrEmpty(nipCell)) continue;` (line 868), which validates NIP, not Score. The success report (lines 974-975) tallies only `success` (insert count) and `skippedNips` (duplicates from lines 925-928); there is NO accumulator or warning for rows whose Score failed to parse â€” a parse-failed row increments `success` identically to a valid row. So a column-shift (e.g. Score actually in col 2) would silently import an entire batch as Score=0/Not-Passed while TempData["Success"] reports "Berhasil insert N", exactly as claimed. The finding is accurate in every cited detail.

---

### [MAN:MAN-05] Cross-path duplicate detection misses when CompletedAt time components differ (date vs datetime-local)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | drift |
| **Confidence** | medium |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** The manual dedup predicate compares CompletedAt by exact equality (AdminBaseController.cs:267 `s.CompletedAt == completedAt`). AddManualAssessment binds CompletedAt from an <input type="date"> (Views/Admin/AddManualAssessment.cshtml:212), so it is stored at midnight (00:00). ImportTraining parses a date string -> also midnight. But BulkBackfill binds CompletedAt from an <input type="datetime-local"> (Views/Admin/BulkBackfill.cshtml:76, default 09:00), so its CompletedAt carries a non-zero time component. A backfill at 2026-03-30T09:00 and a manual/import entry at 2026-03-30 (00:00) for the same user+title are NOT detected as duplicates across paths because the DateTime equality fails on the time portion. Within a single path dedup is consistent; the gap is only cross-path.

**Dampak:** An admin who restores scores via BulkBackfill (with a time-of-day) and later also adds/imports the same event via the date-only paths can create undetected duplicate manual records for the same worker and event.

**Bukti:** Controllers/AdminBaseController.cs:267 (exact `==` on CompletedAt); Views/Admin/AddManualAssessment.cshtml:212 type=date (midnight); Views/Admin/BulkBackfill.cshtml:76 type=datetime-local value=2026-03-30T09:00; insert sites TrainingAdminController.cs:747 (model.CompletedAt), :945 (completedAt with time), :1360 (parsedDate midnight).

**Usulan fix:** Normalize CompletedAt to date-only (.Date) at all three insert sites and in the dedup predicate, or compare by date in ManualDuplicatePredicate (s.CompletedAt!.Value.Date == completedAt.Date), so dedup is consistent regardless of entry path.

**Verifikasi adversarial:** Confirmed all cited facts against the actual code, and ruled out the one realistic refutation (DB-level time truncation).

1. Shared predicate (AdminBaseController.cs:265-267): `ManualDuplicatePredicate` compares `s.CompletedAt == completedAt` â€” exact DateTime equality, no `.Date` normalization. Comment even notes "EXACT: CompletedAt == (BUKAN Â±1 hari)".

2. AddManualAssessment path is date-only -> midnight: View AddManualAssessment.cshtml:212 `<input asp-for="CompletedAt" type="date" />`; bound model property CreateManualAssessmentViewModel.cs:30/93 is `DateTime CompletedAt` with `[DataType(DataType.Date)]` default `DateTime.Today` (00:00). Insert TrainingAdminController.cs:747 uses `model.CompletedAt`; dedup at :722 uses the same. -> 00:00:00.

3. ImportTraining path is date-only -> midnight: line 1331 `DateTime.TryParse(tanggalStr, out var parsedDate)` from an Excel `YYYY-MM-DD` string -> 00:00; insert at :1360 uses `parsedDate`; dedup at :1342 uses `parsedDate`. -> 00:00:00.

4. BulkBackfill path carries a non-zero time: View BulkBackfill.cshtml:76 `<input type="datetime-local" ... value="2026-03-30T09:00">`; action binds `DateTime completedAt` (TrainingAdminController.cs:840); insert at :945 uses `completedAt`; dedup at :914-915 inlines `s.CompletedAt == completedAt`. With the default the stored time is 09:00:00.

5. Refutation attempt (would the DB/EF truncate the time so both normalize to midnight?): NO. Model AssessmentSession.cs:45 is `DateTime? CompletedAt` (not DateOnly). The column is mapped `datetime2` â€” confirmed in migration 20260214011828_AddAssessmentResultFields.cs:24 (`type: "datetime2"`) and ApplicationDbContextModelSnapshot.cs:176-177. `datetime2` preserves the time component, so 2026-03-30T09:00:00 != 2026-03-30T00:00:00 at the SQL comparison level. No Fluent `HasConversion`/`HasColumnType("date")` on CompletedAt anywhere (no matches in Data/), and no `.Date` truncation on any insert/dedup site in TrainingAdminController (grep returned none).

Therefore: within a single path dedup is internally consistent (all three compare like-for-like), but across paths a BulkBackfill record at ...T09:00 and a date-only manual/import record at ...T00:00 for the same UserId+Title will NOT be detected as duplicates. The claim is accurate.

Scope/severity notes: BulkBackfill is `[Authorize(Roles = "Admin")]` (line 834); the 09:00 is the default but the datetime-local field is freely editable, so an admin who zeroes the time would avoid the mismatch. The gap is a real cross-path drift that only manifests when a non-midnight time is used (the default), making the duplicate occurrence conditional/operator-dependent rather than a guaranteed failure â€” consistent with the LOW/drift classification.

---

### [MAN:MAN-06] BulkBackfill accepts arbitrary LinkedGroupId with no validation

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Manual/bulk score entry (TrainingAdmin) bypassing grading |

**Masalah:** BulkBackfillAssessment accepts linkedGroupId directly from the form and stores it on every inserted session (Controllers/TrainingAdminController.cs:841 param, :947 `LinkedGroupId = linkedGroupId`) with no check that the referenced group exists, belongs to the same category, or is a valid Pre/Post counterpart. An admin typo or stale id silently links backfilled sessions into an unrelated Pre/Post group.

**Dampak:** Backfilled records can be mis-grouped into an unrelated Pre/Post pairing, distorting Pre/Post pairing displays and any grouped analytics; no error is shown.

**Bukti:** Controllers/TrainingAdminController.cs:841 (linkedGroupId param), :947 (stored verbatim); Views/Admin/BulkBackfill.cshtml:82-86 (free numeric input, 'ID counterpart').

**Usulan fix:** When linkedGroupId is provided, validate it references an existing AssessmentSession/group of the matching category before insert; otherwise reject with a TempData error.

**Verifikasi adversarial:** Confirmed against the actual code.

1. The cited evidence is accurate. `Controllers/TrainingAdminController.cs:836-844` declares `int? linkedGroupId = null` as a form-bound action parameter on `BulkBackfillAssessment`. At line 947 it is stored verbatim: `LinkedGroupId = linkedGroupId` on every inserted `AssessmentSession`. I read the entire method body (lines 836-985): there is NO validation of `linkedGroupId` anywhere â€” the only checks are excel non-empty (846), title/category non-empty (851), excel-parse (859-880), row-count (882), and NIP-existence (894-899). `linkedGroupId` is never compared against existing groups, category, or a Pre/Post counterpart. It flows straight from the form into the entity and is only echoed back into audit/TempData strings (965, 974).

2. `Views/Admin/BulkBackfill.cshtml:81-86` is exactly as described â€” a free `<input type="number" id="linkedGroupId" name="linkedGroupId">` with help text "ID counterpart (Pre/Post pair)" and no datalist/dropdown of valid ids.

3. The claimed impact is sound. `Models/AssessmentSession.cs:169-172` documents LinkedGroupId as "FK ke grup assessment ... Null = session berdiri sendiri." In the organic flow (`AssessmentAdminController.cs:1248`) the value is a real session Id (`preSessions[0].Id`) used as the group key, and Pre/Post pairing/analytics consume it heavily: `CMPController.cs:253-297` matches Preâ†’Post via `post.LinkedGroupId == pre.LinkedGroupId`; `CMPController.cs:3216-3565` groups/filters by `LinkedGroupId`; `AssessmentAdminController.cs:154-195` splits Pre/Post vs Standard by `LinkedGroupId != null` and groups by it. A typo/stale id therefore silently buckets backfilled rows into an unrelated pairing.

4. No guard refutes it. The DB schema (`Data/ApplicationDbContext.cs:203`) only declares `HasIndex(a => a.LinkedGroupId)` â€” there is no `HasForeignKey`/`HasOne` constraint, so the database will NOT reject an arbitrary/dangling integer. No other handler validates it on this path.

Severity is correctly LOW: the endpoint is gated by `[Authorize(Roles = "Admin")]` + `[ValidateAntiForgeryToken]` (lines 832-834), so it requires an authenticated admin and is a data-integrity/operator-error issue (admin typo), not an external attack. Impact is bounded to mis-grouped Pre/Post display/analytics with no user-facing error, and the value is recorded in the audit log (965), aiding recovery.

---

### [OPS:OPS-03] AssessmentHub.SaveTextAnswer lacks the timer-expired guard that SaveMultipleAnswer enforces â€” essay answers editable after time is up

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Notifications + live monitoring + audit-log + impersonation |

**Masalah:** AssessmentHub.SaveMultipleAnswer enforces a server-side timer check (lines 205-215): if elapsed since StartedAt exceeds DurationMinutes + ExtraTimeMinutes, the write is rejected. AssessmentHub.SaveTextAnswer (the essay save path) performs the same session ownership/InProgress validation but has NO timer-expired check, so a worker can continue mutating their essay TextAnswer via SignalR after their allotted time has expired (up to whenever auto-submit/AkhiriUjian lands). The final submit (AkhiriUjian) does enforce the timer, which bounds the impact, but the two hub save paths are inconsistent and the essay content can be altered past the deadline.

**Dampak:** A worker can keep editing/extending essay answer text after the exam timer has expired (until auto-submit), inconsistent with multiple-answer behavior. Integrity/fairness gap rather than data loss.

**Bukti:** Services/Hubs/AssessmentHub.cs:134-182 (SaveTextAnswer â€” ownership/InProgress check at 142-149 but no elapsed-vs-allowed check) vs Hubs/AssessmentHub.cs:205-215 (SaveMultipleAnswer timer guard: `var elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds; var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60; if (elapsed > allowed){...return;}`).

**Usulan fix:** Apply the same timer-expired guard from SaveMultipleAnswer to SaveTextAnswer (compute elapsed vs DurationMinutes + ExtraTimeMinutes from session.StartedAt and return early when expired). Consider extracting the check into a shared helper to avoid future drift.

**Verifikasi adversarial:** Confirmed against actual code at Hubs/AssessmentHub.cs (note: file is at Hubs/, not Services/Hubs/ as the first evidence line typos; second citation path + all line numbers are correct).

SaveTextAnswer (lines 134-182): only guards are userId non-empty (136-137) and a session query `s.Id == sessionId && s.UserId == userId && s.Status == "InProgress"` (143-144). No time predicate in the query and no elapsed-vs-allowed check anywhere in the method. It then truncates to MaxCharacters and upserts PackageUserResponse.TextAnswer (157-181). Confirmed: NO timer guard.

SaveMultipleAnswer (lines 205-215): contains the exact guard quoted in the finding â€” `var elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds; var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60; if (elapsed > allowed){ log; return; }`. Confirmed verbatim. The two save paths are inconsistent exactly as claimed.

Tried to refute via the InProgress check acting as a de-facto timer guard: I searched for any BackgroundService/IHostedService/AddHostedService â€” none exist in the codebase. Status only transitions to Completed/Abandoned via explicit controller actions (CMPController.cs:1241 Abandoned, AssessmentAdminController.cs:3745 Completed) or submit. Nothing on the server flips a session out of InProgress merely because its timer elapsed, so the InProgress predicate does NOT bound post-deadline essay writes. A worker holding the SignalR connection can invoke('SaveTextAnswer', ...) after the visual/client timer expires.

Mitigation confirmed (bounds impact): the final submit path enforces the server timer â€” CMPController.cs:1492-1496 and 1552-1558 compute the same elapsed>=allowed, and ValidateSubmitTiming (4377-4429, Phase 313 Tier1/Tier2 grace) rejects late manual submits and bounds auto-submit to a 2-min grace. So essay edits are only possible in the window between timer expiry and whenever auto-submit/AkhiriUjian lands â€” integrity/fairness gap, not unbounded and not data loss.

Net: finding is real and accurately described; only a cosmetic path typo in one citation.

---

### [OPS:OPS-04] AssessmentHub.LogPageNav writes ExamActivityLog for arbitrary sessionId with no ownership validation

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | medium |
| **Area** | Notifications + live monitoring + audit-log + impersonation |

**Masalah:** AssessmentHub.LogPageNav accepts a client-supplied sessionId and pageNumber and unconditionally inserts an ExamActivityLog row (EventType="page_nav") for that sessionId, with no check that the calling connection's user owns the session (unlike SaveTextAnswer/SaveMultipleAnswer which validate s.UserId == Context.UserIdentifier && InProgress, and OnConnected/OnDisconnected which derive sessionId from the caller's own InProgress session). Any authenticated user with a hub connection can call LogPageNav(anySessionId, anyPage) and inject fabricated navigation events into another worker's exam activity log, which HC uses for live monitoring/forensics.

**Dampak:** Activity/monitoring log for any session can be polluted with bogus page-nav entries by any authenticated user, undermining the integrity of exam monitoring and any audit/forensic use of ExamActivityLogs.

**Bukti:** Hubs/AssessmentHub.cs:69-92 (LogPageNav: takes sessionId from client, `db.ExamActivityLogs.Add(new ExamActivityLog{ SessionId = sessionId, EventType = "page_nav", ... })` with no ownership lookup) contrasted with the ownership validation present in Hubs/AssessmentHub.cs:142-149 and 197-203.

**Usulan fix:** Validate ownership in LogPageNav before writing: load the session and require s.UserId == Context.UserIdentifier (and Status == InProgress), mirroring SaveTextAnswer/SaveMultipleAnswer; ignore the call otherwise.

**Verifikasi adversarial:** Confirmed against actual code. Hubs/AssessmentHub.cs:69-92 LogPageNav(int sessionId, int pageNumber) takes both args from the client and unconditionally executes db.ExamActivityLogs.Add(new ExamActivityLog{ SessionId = sessionId, EventType = "page_nav", Detail = $"Halaman {pageNumber + 1}", Timestamp = DateTime.UtcNow }) inside Task.Run â€” there is no Context.UserIdentifier read, no s.UserId == userId filter, and no Status == "InProgress" lookup; the only handling is a catch that logs failures. The contrast cited is accurate: SaveTextAnswer (lines 143-149) and SaveMultipleAnswer (lines 197-203) both load the session via s.Id == sessionId && s.UserId == userId && s.Status == "InProgress" and return on null, while OnConnectedAsync (105-108) and OnDisconnectedAsync (265-268) derive sessionId from the caller's own InProgress session instead of trusting input. The hub is [Authorize] (line 9), so this is exploitable by any authenticated user â€” SignalR methods accept arbitrary client-supplied args regardless of what the legitimate JS at Views/CMP/StartExam.cshtml:971 (window.assessmentHub.invoke('LogPageNav', SESSION_ID, currentPage)) sends. Impact is real: ExamActivityLog.cs documents the table as an HC audit trail, and AssessmentAdminController.GetActivityLog (line 6803, [Authorize(Roles="Admin, HC")], comment "Used by HC to audit worker behaviour during the exam") reads these rows back per sessionId for monitoring, so fabricated page_nav rows appear in another worker's log and inflate lastEventTime (which feeds timeSpentSeconds when CompletedAt is null). I could not find any middleware/handler elsewhere that re-validates sessionId ownership for hub invocations. LOW is appropriate: EventType is hardcoded to "page_nav" (cannot forge started/submitted via this path), Detail is server-built from an int so no injection/XSS, no grade/answer integrity is touched â€” ceiling is benign nav-log pollution/forensic noise on arbitrary sessions.

---

### [OPS:OPS-05] NotifyIfGroupCompleted groups Pre/Post sessions together (no LinkedGroupId/AssessmentType filter) when scheduled on the same date

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | correctness |
| **Confidence** | low |
| **Area** | Notifications + live monitoring + audit-log + impersonation |

**Masalah:** NotifyIfGroupCompleted determines the sibling set purely by (Title == X.Title && Category == X.Category && Schedule.Date == X.Schedule.Date) and does not exclude Pre/Post-linked sessions or filter by AssessmentType/LinkedGroupId, unlike the rest of the system (e.g., StandardGroupSiblingPredicate and the monitoring grouping which carefully separate Pre/Post). Pre/Post creation only enforces PostSchedule > PreSchedule (AssessmentAdminController.cs:1042-1043, 1788-1789), so a same-day morning-Pre / afternoon-Post pair shares Title+Category+Schedule.Date. As a result the 'all completed' notification will treat PreTest and PostTest as one group: the PreTest half completing on its own never fires its own notification (because the still-Upcoming PostTest siblings keep allSiblings.All(Completed||Cancelled) false), and a single notification (with the generic ActionUrl /CMP/Assessment) only fires once both halves finish. This is a behavioral mismatch with the carefully Pre/Post-aware grouping used elsewhere.

**Dampak:** For same-day Pre/Post assessments, HC may not get a timely 'PreTest group completed' notification, and the single combined notification is ambiguous. Low impact and only when Pre and Post share a calendar date.

**Bukti:** Services/WorkerDataService.cs:433-441 (sibling query keyed on Title+Category+Schedule.Date with `allSiblings.All(s => s.Status == "Completed" || s.Status == "Cancelled")`, no AssessmentType/LinkedGroupId filter); same-day Pre/Post allowed by AssessmentAdminController.cs:1042-1043 (`if (PostSchedule <= PreSchedule) ... error`).

**Usulan fix:** Scope NotifyIfGroupCompleted to the correct group: for LinkedGroupId != null sessions, group by LinkedGroupId + AssessmentType; for standard sessions keep Title+Category+Schedule.Date but exclude PreTest/PostTest/manual (mirror StandardGroupSiblingPredicate). Make the message/ActionUrl phase-aware.

**Verifikasi adversarial:** Confirmed from source. (1) Services/WorkerDataService.cs:433-441 â€” NotifyIfGroupCompleted builds the sibling set purely from `s.Title == completedSession.Title && s.Category == completedSession.Category && s.Schedule.Date == completedSession.Schedule.Date`, then early-returns unless `allSiblings.All(s => s.Status == "Completed" || s.Status == "Cancelled")`. There is NO AssessmentType or LinkedGroupId filter, verbatim as claimed. (2) Same-day Pre/Post is permitted: AssessmentAdminController.cs:1042-1043 only errors when `PostSchedule <= PreSchedule`; the Edit path repeats this at :1789. No same-day prohibition exists. (3) Pre and Post sessions genuinely share Title+Category: created with `Title = model.Title`/`Category = model.Category` for Pre (:1220-1221) and Post (:1256-1257), and again on the Edit/add-worker path (:1805-1806, :1905-1906). Pre uses `Schedule = PreSchedule!.Value` (:1222) and Post `Schedule = PostSchedule!.Value` (:1258), so on a single calendar day their `Schedule.Date` is identical â†’ they match each other in the sibling query. (4) Post sessions are created with `Status = "Upcoming"` (:1225, :1261); "Upcoming" is neither "Completed" nor "Cancelled," so when the Pre half finishes first the `.All(...)` guard returns false and the per-Pre notification is suppressed â€” exactly the described behavior. (5) The contrast claim holds: StandardGroupSiblingPredicate (AssessmentAdminController.cs:2327-2330) explicitly adds `LinkedGroupId == null && AssessmentType != "PreTest" && != "PostTest" && !IsManualEntry`, AdminBaseController.cs:249 has a Pre/Post helper, and CMPController.cs groups by LinkedGroupId+AssessmentType (e.g. :253-297, :3215-3372). NotifyIfGroupCompleted is the lone path that ignores this separation. No refuting guard exists: the `alreadySent` dedup (:456-461) only prevents duplicate same-Title notifications and does nothing to separate Pre from Post. The notification's ActionUrl is the generic `/CMP/Assessment` (:478). The finding's LOW severity and conditional scope (only when Pre and Post fall on the same calendar date â€” multi-day scheduling naturally separates them via Schedule.Date) is appropriate.

---

### [PEL:PEL-04] Cross-year gate silently fails open when prev track's TahunKe is empty-string (only null is warned)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Proton cross-year eligibility / completion gating |

**Masalah:** prevTahunKe is resolved via Select(t => t.TahunKe).FirstOrDefaultAsync() (lines 1351-1355). ProtonTrack.TahunKe is a non-nullable string defaulting to "" (ProtonModels.cs:14). If a prev-year track row exists but has an empty/whitespace TahunKe (malformed master data), prevTahunKe becomes "" rather than null. IsPrevYearPassedAsync treats IsNullOrWhiteSpace as 'no prereq' and returns true (ProtonCompletionService.cs:152), so the cross-year gate fails open. The non-contiguity warning at line 1357 only fires when prevTahunKe==null, NOT for empty string, so this fail-open is completely silent (no log).

**Dampak:** With malformed/blank-TahunKe master track data, a worker who has not passed the prior year would pass the cross-year gate and be admitted to a higher-year Proton exam, with no warning logged for diagnosis. Low likelihood (requires data corruption) but no defense-in-depth and silent.

**Bukti:** AssessmentAdminController.cs:1351-1358 (warning condition `if (protonUrutan > 1 && prevTahunKe == null)` misses empty-string); ProtonCompletionService.cs:150-155 `if (string.IsNullOrWhiteSpace(prevTahunKe)) return true;`; ProtonModels.cs:13-14 `public string TahunKe { get; set; } = "";`.

**Usulan fix:** Treat empty/whitespace prevTahunKe the same as the non-contiguous case: change the warning condition to `protonUrutan > 1 && string.IsNullOrWhiteSpace(prevTahunKe)` and consider fail-closed (skip the worker) when a prerequisite year is expected (Urutan>1) but its TahunKe cannot be resolved.

**Verifikasi adversarial:** Confirmed all three cited locations in actual code. (1) AssessmentAdminController.cs:1351-1355 resolves prevTahunKe = `.Where(t => t.TrackType==trackType && t.Urutan==protonUrutan-1).Select(t => t.TahunKe).FirstOrDefaultAsync()`. Line 1357-1358 warns ONLY `if (protonUrutan > 1 && prevTahunKe == null)` â€” a strict null check, not IsNullOrWhiteSpace. (2) ProtonCompletionService.cs:152 `if (string.IsNullOrWhiteSpace(prevTahunKe)) return true;` and ProtonYearGate.IsAllowed (line 169) `if (string.IsNullOrWhiteSpace(prevTahunKe)) return true;` â€” so empty/whitespace prevTahunKe is treated as 'no prereq' (fail-open). (3) ProtonModels.cs:14 `public string TahunKe { get; set; } = "";` (non-nullable, default ""). Migration 20260217063156 confirms DB column is nvarchar(max) nullable:false â€” DB blocks null but NOT empty string. The two distinct code paths hold: FirstOrDefaultAsync returns null when NO prev-track row exists (this IS warned), but returns "" when a prev-track row exists with empty TahunKe (NOT warned, and IsPrevYearPassedAsync returns true â†’ cross-year gate silently fails open). I tried to refute via a guard elsewhere: searched all ProtonTrack creation sites â€” only Data/SeedData.cs:155-161 (SeedProtonTracksAsync) and the migration create rows, both always setting well-formed TahunKe ('Tahun 1/2/3'); there is no admin CRUD that could enter empty TahunKe. So the empty-string case only arises from direct DB manipulation / migration drift = genuine 'malformed master data', exactly as the finding's low-likelihood caveat states. The v25.0 milestone audit IN-02 documents only the null-case fail-open as 'by design'; it does NOT cover the empty-string-on-existing-row case this finding raises, so it does not refute it. The claimed silent fail-open is real and there is no defense-in-depth log for it.

---

### [REC:REC-03] NotifyIfGroupCompleted dedup uses Message.Contains(Title) â†’ substring collision suppresses a distinct group's notification

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | correctness |
| **Confidence** | medium |
| **Area** | Records aggregation + cascade-delete engine |

**Masalah:** The 'already sent' guard for the assessment-group-completed notification matches existing notifications with Message.Contains(completedSession.Title) within a -2 day UTC window (Services/WorkerDataService.cs:456-461). Because Contains is a substring test on the title, a session whose Title is a substring of another recently-notified session's title (e.g. Title 'K3' vs an earlier notified 'K3 Dasar') will be treated as already-sent and the notification for the shorter-titled group will be silently skipped for every recipient.

**Dampak:** HC/Admin recipients can miss the 'all participants completed' notification for a real assessment group when its title is a substring of another group's title completed in the prior 2 days. Low probability but a genuine silent-miss.

**Bukti:** Services/WorkerDataService.cs:455-469 (windowStart = UtcNow.AddDays(-2); alreadySent = AnyAsync(... n.Message.Contains(completedSession.Title) ...); if(alreadySent) continue). The Message body is the long sentence containing the title (:477).

**Usulan fix:** Match on an exact title token rather than Contains â€” e.g. compare against the exact composed message string, or add a SourceTitle/SourceId column to UserNotifications and dedup on equality.

**Verifikasi adversarial:** Confirmed from actual code. Services/WorkerDataService.cs:455-461 implements the dedup exactly as described: windowStart = DateTime.UtcNow.AddDays(-2); alreadySent = await _context.UserNotifications.AnyAsync(n => n.UserId == recipientId && n.Type == "ASMT_ALL_COMPLETED" && n.Title == "Assessment Selesai" && n.Message.Contains(completedSession.Title) && n.CreatedAt >= windowStart); if (alreadySent) continue;. The sent Message (line 477) is $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian" â€” the title is embedded as a raw substring, so for a session titled "K3", Message.Contains("K3") matches an earlier message built from "K3 Dasar", flipping alreadySent=true and skipping the notification (continue). The Title column is a fixed constant ("Assessment Selesai") for every group-completed notification, so Title equality provides no discrimination. I verified Models/UserNotification.cs: the schema has NO SourceTitle/SourceDate/Category column (the inline comment at line 451 explicitly states this), so there is no alternate discriminator. The dedup query also does NOT filter by Schedule.Date or Category, so two genuinely distinct groups whose titles are in a substring relationship and that complete within the 2-day UTC window will collide. I checked for a refuting guard or an alternate implementation: grep shows only ONE implementation (WorkerDataService.cs:431) and two live callers (AssessmentAdminController.cs:3654 after essay grading finalization, GradingService.cs:304 after grading) â€” both invoke the same method, so no separate "CMP version" mitigates it. Nothing refutes the finding.

---

### [REC:REC-04] Renewal child that is a Pre/Post session bypasses the single-delete IsPrePostSession guard and is hard-deleted, orphaning its gain-score partner

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | data-integrity |
| **Confidence** | low |
| **Area** | Records aggregation + cascade-delete engine |

**Masalah:** DeleteAssessment / DeleteManualAssessment block deleting a Pre/Post session directly via IsPrePostSession (Controllers/AssessmentAdminController.cs:2226-2230; Controllers/TrainingAdminController.cs:1102-1103). But that guard only checks the ROOT. The cascade engine itself has no Pre/Post guard: CollectCascadeIds traverses renewal children regardless of AssessmentType, and ExecuteAsync hard-deletes every session node, only null-clearing partners that point to it (RecordCascadeDeleteService.cs:236-237). If a renewal descendant of a non-Pre/Post root is a PreTest/PostTest session, it is deleted while its paired partner is merely LinkedSessionId-nulled â€” orphaning the gain-score pairing that the IsPrePostSession guard exists to protect.

**Dampak:** A Pre/Post test session reachable only as a renewal descendant can be destroyed, breaking gain-score pairing for the surviving partner â€” the exact orphan scenario the guard was designed to prevent. Requires the unusual data shape of a Pre/Post session carrying a Renews*Id, so low likelihood.

**Bukti:** AssessmentAdminController.cs:2226-2230 & TrainingAdminController.cs:1102-1103 (guard only on root); RecordCascadeDeleteService.cs:54-92 (CollectCascadeIds traverses children with no AssessmentType filter); :236-237 (#8 only null-clears partner, does not block/keep Pre-Post integrity).

**Usulan fix:** Apply the IsPrePostSession check across the whole cascade set (in the controller pre-check over cascadeSessionIds, or in the engine) and reject/redirect to group-delete when any descendant is Pre/Post.

**Verifikasi adversarial:** Confirmed from actual code. IsPrePostSession (AdminBaseController.cs:248-249) is only AssessmentType==PreTest||PostTest and is applied only to the ROOT entity in DeleteAssessment (AssessmentAdminController.cs:2226) and DeleteManualAssessment (TrainingAdminController.cs:1102). The cascade engine has NO AssessmentType guard: CollectCascadeIds (RecordCascadeDeleteService.cs:54-92) traverses children solely via RenewsSessionId/RenewsTrainingId; ExecuteAsync (236-237, 257) hard-Removes every session node and only null-clears partners pointing to it. Crucially, refuting the finding's own low-likelihood rationale, a Pre/Post session carrying a Renews FK is a DESIGNED flow: CreateAssessment Pre-Post renewal mode creates the PostTest with BOTH AssessmentType=PostTest AND RenewsSessionId/RenewsTrainingId (AssessmentAdminController.cs:1271,1275-1276, comment D-24 renewal FK hanya di Post) and cross-links LinkedSessionId (1290). Nothing prevents a PostTest being a renewal child of a non-Pre/Post source; double-renewal guard (992-1003) only blocks same-source reuse. So deleting a non-Pre/Post root cascades into and hard-deletes a PostTest descendant, leaving its PreTest partner only LinkedSessionId-nulled. The project's own test LinkedSession_NullCleared_OnPartner (RecordCascadeIntegrationTests.cs:186-211) confirms this orphan behavior with no block. No guard, handler, or constraint refutes it.</parameter>
<parameter name="notes">Real data-integrity gap. Finding's likelihood is understated: the Pre/Post-with-renewal-FK shape is the standard output of the D-24 Pre-Post renewal path, so it arises in normal usage. Still LOW overall: admin/HC-only op, no security/authz impact, only breaks gain-score pairing for the surviving partner. Fix: enforce IsPrePostSession inside CollectCascadeIds/ExecuteAsync (refuse or full-cascade the partner) rather than root-only in the two controllers.</parameter>
</invoke>


---

### [REC:REC-05] AddManualAssessment writes certificate files to disk before the single SaveChanges â†’ orphaned files on mid-loop failure

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Records aggregation + cascade-delete engine |

**Masalah:** AddManualAssessment saves each worker's uploaded certificate to disk inside the per-worker loop (Controllers/TrainingAdminController.cs:736-737) and only persists all AssessmentSession rows with a single SaveChangesAsync after the loop (:765). If SaveChanges (or any later worker iteration) throws, the certificate files already written for earlier workers remain on disk with no corresponding DB row and no cleanup, accumulating orphaned files in uploads/certificates.

**Dampak:** Disk accumulation of orphaned certificate files (storage leak) and stale files that could be referenced by a later record by coincidence is unlikely (Guid-named). Pure storage/robustness, no data corruption.

**Bukti:** TrainingAdminController.cs:733-764 (loop: certUrl = SaveFileAsync(...) then session.Add) followed by single :765 SaveChangesAsync; no try/catch to delete written files on failure.

**Usulan fix:** Either persist files only after a successful SaveChanges, or wrap the loop so written file paths are tracked and deleted in a catch on failure.

**Verifikasi adversarial:** Confirmed against actual code in Controllers/TrainingAdminController.cs. AddManualAssessment (lines 689-774) loops over model.WorkerCerts (line 733) and inside the loop calls FileUploadHelper.SaveFileAsync (lines 736-737) for each worker with a certificate file. SaveFileAsync (Helpers/FileUploadHelper.cs:102-105) writes the file to disk immediately via `new FileStream(filePath, FileMode.Create)` + `CopyToAsync` and returns a relative URL. The AssessmentSession rows are only added to the context in-loop (line 763) and persisted by a SINGLE `await _context.SaveChangesAsync()` AFTER the loop (line 765). There is NO try/catch and NO transaction around the method body, and no tracking/cleanup of the certUrl files written for earlier workers. I grepped try/catch/BeginTransaction/DeleteFile across the controller: AddManualAssessment is the only multi-row write path with NO transaction and NO failure cleanup â€” by contrast EditTraining (lines 636-666) and EditManualAssessment (lines 1105-1133) wrap their saves in try/catch and use DeleteFile (lines 567, 1076), and the sibling BulkBackfillAssessment uses BeginTransactionAsync + RollbackAsync (lines 905-984). So if SaveChangesAsync throws (FK violation, column-length overflow, timeout, connection drop) or a later iteration's SaveFileAsync throws (disk/IO error) after earlier files are written, those files remain on disk with no DB row and no cleanup â€” orphan accumulation in uploads/certificates. The DB itself stays consistent (single SaveChanges = atomic, no partial rows), so there is no data corruption â€” only a storage/robustness leak. Filenames are timestamp(ms)+8-char-GUID+original (FileUploadHelper.cs:100), so coincidental reuse by a later record is effectively impossible, matching the finding's impact note. The pre-loop duplicate guard (lines 720-729) eliminates one failure cause but not the others, so the orphan scenario remains reachable. No guard/handler elsewhere refutes it.

---

### [RES:RES-03] MultipleAnswer with zero correct options is scored correct in stored Score but "Salah" in review (narrow drift)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | drift |
| **Confidence** | medium |
| **Area** | Results / review projection + authz |

**Masalah:** For a MultipleAnswer question, the stored-score path scores it correct when the selected set equals the correct set with NO guard against the empty/empty case: GradingService L116 `selectedOptionIds.SetEquals(correctOptionIds)` and AssessmentScoreAggregator L50 `maSelected.SetEquals(maCorrect)`. The Results review and count-only paths instead require at least one selection: review L2186 `selectedOptionIds.Count > 0 && correctIds.SetEquals(...)`, count-only L2237 `if (selectedIds.Count == 0) continue;`. So for a degenerate MA question that an admin saved with zero correct options, a worker who selects nothing yields {}.SetEquals({})==true in the stored Score (full points) but is shown "Salah" / excluded from CorrectAnswers in the Results review. The two surfaces disagree.

**Dampak:** Only manifests for a misconfigured MA question with zero correct options where the worker selects nothing; in that case the review badge contradicts the awarded score. Low likelihood (requires admin misconfiguration), but a genuine logic divergence between stored-score and review projection.

**Bukti:** Controllers/CMPController.cs:2185-2186 (Count>0 guard in review), 2237 (Count==0 continue in count-only); Services/GradingService.cs:116 (no count guard); Helpers/AssessmentScoreAggregator.cs:50 (no count guard).

**Usulan fix:** Make the empty-correct/empty-selection handling consistent across all three sites â€” either add the `Count > 0` (or `maCorrect.Count > 0`) guard to the GradingService/AssessmentScoreAggregator MA branches, or remove it from the review paths. Also consider validating that MA questions have >=1 correct option at save time.

**Verifikasi adversarial:** All four cited code locations are confirmed verbatim. Stored-score paths lack a count guard: Services/GradingService.cs:116 `if (selectedOptionIds.SetEquals(correctOptionIds)) totalScore += q.ScoreValue;` and Helpers/AssessmentScoreAggregator.cs:50 `if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;`. Both build correct-id sets from `q.Options.Where(o => o.IsCorrect)`, so a MA question with zero correct options yields an empty set, and an empty selection set produces `{}.SetEquals({}) == true` â†’ full points awarded. The review/count-only projections do guard against this: Controllers/CMPController.cs:2186 `isCorrect = selectedOptionIds.Count > 0 && correctIds.SetEquals(selectedOptionIds);` and CMPController.cs:2237 `if (selectedIds.Count == 0) continue;`. So for a degenerate MA row with zero correct options and an empty answer, the stored Score awards points while the review badge shows "Salah" and the count excludes it â€” a genuine divergence between the two surfaces, exactly as claimed.

I attempted to refute it via the precondition (can such a MA row be created?). All in-app write paths BLOCK a MA question with <2 correct options: AssessmentAdminController.cs CreateQuestion POST (L6332-6336) and EditQuestion POST (L6539-6543) both reject `questionType == "MultipleAnswer" && correctCount < 2` with TempData error + redirect (before option persistence), and the CSV import path (L6064-6068) skips rows where `correctLetters.Count == 0`. The EditQuestion option rebuild (L6604-6631) runs after the same guard. So the zero-correct precondition is not reachable through the application UI/import â€” it would require direct DB manipulation. This does NOT refute the finding: the finding explicitly scopes itself to a "misconfigured MA question with zero correct options" and rates it low-likelihood/requires-misconfiguration. The two-surface logic divergence is real and verified; it is simply latent/defensive given the write-path guards. Severity correctly LOW (latent consistency drift, no practical exploit path through the app).

---

### [RST:RST-04] AddExtraTime accumulates with no total cap â€” repeated calls grant unbounded total extra time

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Reset / force-end controls + retake semantics |

**Masalah:** AddExtraTime validates only the per-call amount (5â€“120 min, multiple of 5; Controllers/AssessmentAdminController.cs:6870-6871), then accumulates: `session.ExtraTimeMinutes = (session.ExtraTimeMinutes ?? 0) + minutes` (:6899). There is no cap on the running total, so N calls add NÃ—(up to 120) minutes. On its own this is a minor over-grant risk for HC; combined with the missing role gate (RST-01) it becomes abusable by a worker to grant effectively unlimited exam time, since the total directly drives the server-side expiry math (CMPController.cs:1495/1557, AssessmentHub.cs:209).

**Dampak:** Repeated invocations extend an exam without bound; integrity of the timed exam window degrades, especially given the authz gap in RST-01.

**Bukti:** Controllers/AssessmentAdminController.cs:6870 per-call bound `if (minutes < 5 || minutes > 120 || minutes % 5 != 0)`; :6899 accumulation `(session.ExtraTimeMinutes ?? 0) + minutes` with no aggregate ceiling; consumed at CMPController.cs:1495 `(assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60`.

**Usulan fix:** Enforce a sane aggregate ceiling on total ExtraTimeMinutes per session (e.g., cap (existing + minutes) at a configured max) in addition to fixing RST-01.

**Verifikasi adversarial:** Verified all three load-bearing claims against the actual code:

1. Per-call bound only: Controllers/AssessmentAdminController.cs:6870 â€” `if (minutes < 5 || minutes > 120 || minutes % 5 != 0)` validates ONLY the single `minutes` argument, not any running total. Confirmed exactly as cited.

2. Unbounded accumulation: Controllers/AssessmentAdminController.cs:6899 â€” `session.ExtraTimeMinutes = (session.ExtraTimeMinutes ?? 0) + minutes;` inside a foreach over all InProgress batch sessions. No aggregate ceiling anywhere. The model field Models/AssessmentSession.cs:199 is a plain `public int? ExtraTimeMinutes { get; set; }` with NO `[Range]`/DataAnnotations cap, and its XML doc (line 195) explicitly calls it "Akumulasi extra time" â€” accumulation is the designed behavior. I grepped every ExtraTimeMinutes reference repo-wide; there is no cap-enforcing guard anywhere. So N calls grant NÃ—(up to 120) minutes.

3. Total drives server-side expiry math: confirmed at every consumption site â€” CMPController.cs:1495 and :1557 `(assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60`; AssessmentHub.cs:209 same formula for the timer-expiry check; plus CMPController.cs:1127 (durationSeconds for client) and :4403. The accumulated total directly extends the allowed exam window.

I tried to refute via the RST-01 amplification: checked the authz surface. AddExtraTime (line 6866-6868) carries only [HttpPost] + [ValidateAntiForgeryToken] and no in-body role check. Its base, AdminBaseController.cs:12, is annotated only `[Authorize]` (any authenticated user) with NO `[Authorize(Roles=...)]`. So there is indeed no role gate restricting this to HC/Admin â€” the amplification premise holds (a non-admin authenticated user with a valid antiforgery token is not blocked by role). I found no guard/handler elsewhere that caps the total or that re-gates the role.

Standalone, this is a genuine LOW robustness defect: the accumulation has no aggregate ceiling, matching the finding's own severity. The worker-abuse amplification is contingent on RST-01 (which I independently confirmed is a real authz gap), but even ignoring RST-01 the no-cap behavior is real for any caller with access (e.g., HC over-granting).

---

### [SAVE:SAVE-05] UpdateSessionProgress clamp omits ExtraTimeMinutes (uses DurationMinutes*60), freezing saved elapsed during extra time

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Answer persistence / auto-save + SignalR + resume |

**Masalah:** UpdateSessionProgress Clamp 3 caps clampedElapsed to session.DurationMinutes * 60 (CMPController.cs:459-460), excluding ExtraTimeMinutes. Every other timing site includes extra time: SaveMultipleAnswer uses (DurationMinutes + ExtraTimeMinutes)*60 (AssessmentHub.cs:209), SubmitExam uses it (CMPController.cs:1557), and resume durationSeconds uses it (CMPController.cs:1127). Combined with Clamp 2 (monotonic, line 457), once a worker passes DurationMinutes the persisted ElapsedSeconds is frozen at DurationMinutes*60 and never advances through the extra-time window. Resume self-heals because elapsed is recomputed from wall-clock (CMPController.cs:1132-1135) and clamped to the extra-time-inclusive durationSeconds, so the worker's live timer is correct after a reload; the lingering effect is HC monitoring showing a frozen/stale ElapsedSeconds during extra time.

**Dampak:** For sessions granted extra time, the persisted ElapsedSeconds stops advancing once base duration is reached, so HC progress monitoring under-reports elapsed time during the extra-time window. Worker-facing timer is unaffected after resume.

**Bukti:** Controllers/CMPController.cs:459-460 (Clamp 3 = DurationMinutes*60, no ExtraTime); contrast Hubs/AssessmentHub.cs:209, Controllers/CMPController.cs:1557 and :1127 (all include ExtraTimeMinutes); resume recompute CMPController.cs:1132-1139.

**Usulan fix:** Change Clamp 3 to use (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60 for parity with the other timer sites.

**Verifikasi adversarial:** Confirmed all four code claims verbatim. CMPController.cs:460 Clamp 3 = `Math.Min(clampedElapsed, session.DurationMinutes * 60)` â€” no ExtraTimeMinutes. Contrast sites all include extra time: AssessmentHub.cs:209 `(session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60`; CMPController.cs:1557 (SubmitExam serverTimerExpired) same form; CMPController.cs:1127 (resume durationSeconds) same form. The clamp chain holds: Clamp 2 at line 457 `Math.Max(clampedElapsed, session.ElapsedSeconds)` makes the persisted value monotonic, and Clamp 3 caps at DurationMinutes*60, so once a worker reaches base duration the persisted ElapsedSeconds is pinned and cannot advance through the extra-time window. ElapsedSeconds is written in CMPController only at line 466 (this capped path) and line 1072 (reset to 0 on fresh start) â€” I grepped the whole controller; SubmitExam does NOT rewrite ElapsedSeconds from wall-clock at completion, so the capped value persists onto the completed record. The worker-facing timer self-heals on resume (lines 1132-1139 recompute elapsedSec from wall-clock via Math.Max with wallClockElapsed, then clamp to the extra-time-inclusive durationSeconds), exactly as the finding states. Real downstream consumer of the stale value: AssessmentAdminController.cs:4643 `int durasi = session.ElapsedSeconds / 60;` populates the "Durasi Aktual" cell in the HC Excel export â€” confirming the under-reporting impact on HC monitoring/reporting. No guard elsewhere refutes the defect. Severity LOW is correct: the worker's exam experience and grading are unaffected (timer and submission both use the extra-time-inclusive value); only persisted/reported elapsed time under-reports during and after the extra-time window, an informational reporting inaccuracy with no correctness or security consequence.

---

### [SHF:SHF-03] Inconsistent Abandoned handling between ReshufflePackage (single) and ReshuffleAll (bulk)

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | drift |
| **Confidence** | high |
| **Area** | Shuffle engine correctness + toggle + round-robin |

**Masalah:** ReshufflePackage allows reshuffling 'Abandoned' sessions (AssessmentAdminController.cs:5178: guard passes when userStatus is 'Not started' OR 'Abandoned'), while ReshuffleAll explicitly skips any session that is not 'Not started' â€” including Abandoned â€” reporting 'Dilewati â€” dibatalkan' (AssessmentAdminController.cs:5295-5302). The two endpoints share the same shuffle engine and worker-index basis but apply divergent eligibility rules for the same status.

**Dampak:** An HC using the bulk 'Reshuffle All' cannot reshuffle Abandoned sessions, but can reshuffle the very same session individually â€” confusing and inconsistent behavior. Combined with SHF-02, the single-session path is the one that produces the orphaned-response state, so the bulk path's stricter rule is actually the safer one; the divergence makes the safer behavior inconsistently applied.

**Bukti:** Controllers/AssessmentAdminController.cs:5178 `if (userStatus != "Not started" && userStatus != "Abandoned")` (single allows Abandoned) vs Controllers/AssessmentAdminController.cs:5295 `if (userStatus != "Not started")` (bulk skips Abandoned).

**Usulan fix:** Pick one policy. Recommended: make ReshufflePackage also skip Abandoned (route HC to Reset for sessions with saved progress), aligning with ReshuffleAll and avoiding the SHF-02 orphan problem. If reshuffling Abandoned is intentionally supported, apply it to both endpoints and add the response/SavedQuestionCount cleanup from SHF-02.

**Verifikasi adversarial:** Confirmed against Controllers/AssessmentAdminController.cs. Both endpoints derive userStatus with the identical chain including an "Abandoned" branch: single (lines 5168-5176) and bulk (lines 5285-5293) both set userStatus = "Abandoned" when Status == "Abandoned".

The eligibility guards diverge exactly as claimed:
- ReshufflePackage (single), line 5178: `if (userStatus != "Not started" && userStatus != "Abandoned") return Json(... "Hanya peserta yang belum mulai atau sesi yang ditinggalkan ...")` â€” so BOTH "Not started" and "Abandoned" pass and proceed to reshuffle (lines 5181-5241).
- ReshuffleAll (bulk), line 5295: `if (userStatus != "Not started")` â€” only "Not started" proceeds; "Abandoned" enters the skip branch where line 5299 maps it to reason "dibatalkan", and line 5300 records `status = "Dilewati â€” dibatalkan"`, then `continue` (line 5301).

Both endpoints share the same shuffle engine (ShuffleEngine.BuildQuestionAssignment / BuildOptionShuffle at 5210/5212 and 5308/5310) and the same worker-index basis (sortedSiblingIds.IndexOf at 5208-5209 and 5307). So the only divergence is the eligibility rule for the identical "Abandoned" status. No guard/handler elsewhere reconciles this â€” the two HTTP endpoints are independent and apply different rules. The claimed impact (HC can reshuffle an Abandoned session individually but the bulk path silently skips it as "Dilewati â€” dibatalkan") is exactly what the code produces.

---

### [TMR:TMR-02] Incomplete-submission gate still trusts client-supplied isAutoSubmit flag, allowing early submit of an unanswered exam

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | high |
| **Area** | Timer/duration server-authoritative + 2-tier submit block + auto-submit token |

**Masalah:** The Phase-272 'block incomplete submission' gate is bypassed when `!isAutoSubmit` is false, i.e. when the client sends isAutoSubmit=true. isAutoSubmit is a raw form field bound from the request and is fully attacker-controllable (DevTools / crafted POST). Phase 313 CR-01 specifically moved Tier-1 away from trusting this client flag (to a server-issued token), but this completeness gate still trusts it directly. A worker can POST isAutoSubmit=true while the timer is still running to skip the 'answer all questions first' enforcement and submit an incomplete exam early.

**Dampak:** A worker can deliberately submit an exam with unanswered questions before time expires, bypassing the UI/server completeness requirement. Impact is mostly self-inflicted (lower score) but it defeats the intended 'must answer all' rule and could matter for pass/fail integrity in graded contexts.

**Bukti:** Controllers/CMPController.cs:1523 signature `bool isAutoSubmit=false`; :1552 comment 'Allow incomplete if: (a) isAutoSubmit from client'; :1561 `if (!isAutoSubmit && !serverTimerExpired)` gates the entire unanswered-count check at :1561-1587. The ExamSummary form emits this flag verbatim (Views/CMP/ExamSummary.cshtml:128 `name="isAutoSubmit"`).

**Usulan fix:** Gate the incomplete-allow on the server-validated condition only (serverTimerExpired OR a validated auto-submit token), not on the raw client isAutoSubmit flag â€” consistent with the CR-01 token approach used by Tier-1.

**Verifikasi adversarial:** Confirmed against actual code in Controllers/CMPController.cs and Views/CMP/ExamSummary.cshtml.

1) Client-controllable flag: SubmitExam signature (line 1523) binds `bool isAutoSubmit = false` directly from the form with no [BindNever] or server reassignment. Grep for `isAutoSubmit` shows it is never recomputed server-side between binding (1523) and use (1561). ExamSummary.cshtml line 128 emits `<input type="hidden" name="isAutoSubmit" value="@(timerExpired ? "true" : "false")">` verbatim, so an attacker can flip it to true via DevTools/crafted POST.

2) Completeness gate trusts the flag: line 1561 `if (!isAutoSubmit && !serverTimerExpired)` wraps the entire unanswered-count enforcement block (1561-1587, which loads UserPackageAssignments, counts answered MC+DB responses, and redirects with "Masih ada N soal yang belum dijawab" if answeredCount < totalQuestions). When isAutoSubmit=true the whole block is short-circuited regardless of timer state, even while the timer is still running (serverTimerExpired=false). Comment at 1552 explicitly says "Allow incomplete if: (a) isAutoSubmit from client".

3) Inconsistency with Phase 313 CR-01 is real: the timer Tier-1 guard EnsureCanSubmitExamAsync (called at 1595, defined 4382-4444) was hardened to NOT trust the client flag â€” it requires a server-issued one-shot TempData token (4408-4427) and rejects spoofed isAutoSubmit=true without a valid token. The completeness gate at 1561 was NOT given the same treatment, so it still trusts the raw flag.

4) No downstream re-check refutes it: when the timer is still running, EnsureCanSubmitExamAsync passes (elapsedSec < allowedSec â†’ returns null), so it does not block a mid-exam submit. The grading path (1602-1717) computes score from actual answers (unanswered MC = 0 points, lines 1634-1640) and calls GradeAndCompleteAsync to mark the session Completed/PendingGrading â€” no completeness re-validation anywhere.

Severity LOW is appropriate: the bypass only lets a worker submit their own exam early with unanswered questions, which can only lower their own score (cannot inflate). It defeats the intended 'must answer all' rule but impact is self-inflicted. Matches the finding's claimed LOW/robustness rating.

---

### [TMR:TMR-03] One-shot AutoSubmitToken consumed before grading completes; a transient failure on the first auto-submit attempt turns recoverable retries into a permanent rejection

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | robustness |
| **Confidence** | medium |
| **Area** | Timer/duration server-authoritative + 2-tier submit block + auto-submit token |

**Masalah:** The auto-submit client retries the POST up to 3 times (exponential backoff) on network/HTTP errors. The server consumes the one-shot token unconditionally at the very start of EnsureCanSubmitExamAsync (TempData.Remove) â€” before SaveChangesAsync and GradingService.GradeAndCompleteAsync run. If attempt #1 reaches the server (token consumed) but grading/save then throws a transient error (e.g. DB timeout) so Status is NOT set to Completed and a 5xx is returned, attempt #2 arrives with no valid token; since the timer is expired (elapsed>=allowed) Tier-1 rejects it (for PreTest/PostTest where the guard is live), the client detects the redirect to /CMP/StartExam, sets stopRetry=true and shows a permanent 'Server menolak submit' banner. The very retry mechanism designed for resilience is defeated by the eager one-shot consumption.

**Dampak:** For PreTest/PostTest auto-submits, a single transient server/DB hiccup on the first attempt permanently blocks the auto-submit, requiring admin intervention, even though the worker's answers are saved. (For "Standard"/Proton exams the guard is dead per TMR-01 so this path doesn't reject â€” but those would then accept an unbounded-late retry instead.)

**Bukti:** Controllers/CMPController.cs:4414 reads token, :4422 `TempData.Remove(tempKey)` runs unconditionally before any reject/grade logic; grading happens later at :1678 SaveChangesAsync / :1681 GradeAndCompleteAsync. Views/CMP/ExamSummary.cshtml:195-231 retry loop (maxAttempts=3, delays [1000,2000,4000]); :211-214 redirect-to-StartExam => stopRetry + permanent fail banner.

**Usulan fix:** Only consume the token on a path that actually completes the submission (move TempData.Remove to after a successful grade/commit), or make the token validation idempotent for the duration of the grace window (e.g. validate without consuming, and rely on the Status==Completed re-submission guard to prevent double grading).

**Verifikasi adversarial:** Confirmed from actual code, all cited lines verified:

1. Token consumed unconditionally BEFORE grading. In EnsureCanSubmitExamAsync (Controllers/CMPController.cs:4382), the one-shot token is read at :4414 (TempData[tempKey]) and removed at :4422 (TempData.Remove(tempKey)) â€” this runs at the very top, before Tier-1 (:4427) and before returning to the caller. The helper is invoked at :1595 (timerBlockResult = await EnsureCanSubmitExamAsync(...)), well before SaveChangesAsync (:1678) and GradeAndCompleteAsync (:1681). So the token is gone the moment attempt #1 enters the helper, regardless of whether grading later succeeds.

2. No try/catch around grading. The package path (:1602-1717) has NO exception handling. A transient throw from SaveChangesAsync (:1678) or GradeAndCompleteAsync (:1681) propagates unhandled â†’ 500 response, with Status NOT set to Completed (it is set inside GradeAndCompleteAsync, which never completed).

3. Token never re-issued on POST. The token is only minted in the ExamSummary GET (:1505-1509) when timerExpired=true, rendered into a hidden field (Views/CMP/ExamSummary.cshtml:129-133). The SubmitExam POST never re-issues it. Retries are POSTs that reuse the same form value but the server-side session token is already removed after attempt #1.

4. TempData is session-backed (Program.cs:16-23 AddDistributedMemoryCache + AddSession), and one-shot semantics hold both via read-marks-delete and the explicit Remove â€” so attempt #2 finds no token in session.

5. Tier-1 guard is LIVE for PreTest/PostTest. The type filter (:4390-4395) only skips (returns null) for non-{Online,PreTest,PostTest} types. AssessmentConstants.AssessmentType (Models/AssessmentConstants.cs:7-10) = Manual/Online/PreTest/PostTest, so PreTest/PostTest run the guard. Since the auto-submit only fires after timerExpired (elapsed>=allowed), attempt #2 arrives with elapsedSec>=allowedSec and serverApprovedAutoSubmit=false â†’ Tier-1 reject â†’ RedirectToAction("StartExam") (:4432).

6. Client converts this into a permanent failure. The retry loop (Views/CMP/ExamSummary.cshtml:195-231): on attempt #1's 500, r.ok is false â†’ throw 'HTTP 500' (:219) â†’ caught (:223) â†’ setTimeout retry (:227). Attempt #2 gets the StartExam redirect; r.redirected true and r.url matches /\/CMP\/StartExam/ (:211) â†’ stopRetry=true and permanent banner 'Server menolak submit' (:212-214). The resilience mechanism is defeated exactly as described.

Attempted refutations that all failed: (a) no try/catch re-issues the token or recovers; (b) token is not re-minted on POST; (c) TempData one-shot semantics hold under session provider; (d) guard is genuinely live for PreTest/PostTest. I could not refute the finding.

---

### [TMR:TMR-04] UpdateSessionProgress Clamp 3 omits ExtraTimeMinutes, diverging from the 4 other allowed-time formulas and under-reporting actual duration

| | |
|---|---|
| **Severity** | LOW (klaim: LOW) |
| **Tipe** | drift |
| **Confidence** | high |
| **Area** | Timer/duration server-authoritative + 2-tier submit block + auto-submit token |

**Masalah:** Five sites compute the 'allowed/total' exam time. Four include ExtraTime: EnsureCanSubmitExamAsync (CMPController.cs:4403 `DurationMinutes + (ExtraTimeMinutes ?? 0)`), ExamSummary timerExpired (:1495), SubmitExam serverTimerExpired (:1557), and StartExam durationSeconds (:1127). But UpdateSessionProgress Clamp 3 caps the persisted ElapsedSeconds at `DurationMinutes * 60` only (no ExtraTime). For a worker granted ExtraTime who works past the base DurationMinutes, the stored ElapsedSeconds is frozen at DurationMinutes*60. At resume this is masked because StartExam takes Math.Max(ElapsedSeconds, wallClockElapsed) (:1135), but other direct consumers of the stored value are not protected â€” notably the 'Durasi Aktual' Excel export reads `session.ElapsedSeconds / 60` directly (AssessmentAdminController.cs:4643), so it under-reports the real time used inside the ExtraTime window.

**Dampak:** Reporting inaccuracy: actual exam-duration for accommodations/ExtraTime exams is under-stated in the admin Excel export (and any future direct consumer of ElapsedSeconds). Not an integrity break for grading (timer enforcement reads wall-clock), but a data-correctness/drift defect.

**Bukti:** Controllers/CMPController.cs:459-460 `// Clamp 3 ... clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);` (no ExtraTime). Contrast :1127 and :4403 which add ExtraTime. Direct consumer without wallclock Max: AssessmentAdminController.cs:4642-4644 'Durasi Aktual' = `session.ElapsedSeconds / 60`.

**Usulan fix:** Make Clamp 3 use `(session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60` to match the other four formulas, or centralize the allowed-seconds computation in one helper used by all five sites.

**Verifikasi adversarial:** All load-bearing claims confirmed against actual code.

1. Clamp 3 omits ExtraTime: CMPController.cs:460 `clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);` â€” verified, and a repo-wide grep shows this is the ONLY `DurationMinutes * 60` site in Controllers (no ExtraTime addend).

2. Four contrast sites DO add ExtraTime: line 1127 `(assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60` (StartExam durationSeconds), line 1495 (ExamSummary timerExpired), line 1557 (SubmitExam serverTimerExpired), line 4403 (EnsureCanSubmitExamAsync). A 5th exists in Hubs/AssessmentHub.cs:209. So Clamp 3 is genuinely the odd one out.

3. ExtraTimeMinutes is a real, settable column (Models/AssessmentSession.cs:199, migration 20260407110442) and AssessmentAdminController.cs:6899 has an admin endpoint that adds 5-120 min to in-progress sessions and broadcasts via SignalR â€” so non-zero ExtraTime for online exam-takers is achievable.

4. Direct consumer confirmed: AssessmentAdminController.cs:4642-4644 writes `Durasi Aktual` = `session.ElapsedSeconds / 60` for every session in the export loop. The eligibleSessions filter (4559-4563) includes online/non-manual sessions (Status != Cancelled AND (CompletedAt!=null OR Score!=null OR Status==Abandoned)). The Durasi cell is written at 4642 BEFORE the `if (session.IsManualEntry){...continue;}` branch at 4652, so online sessions retain the raw ElapsedSeconds value.

Tried to refute via three angles, all failed:
(a) Does submit/grading recompute ElapsedSeconds to wall-clock (which would un-mask at export, since export only runs on completed/abandoned)? NO â€” a grep for `ElapsedSeconds =` / `.SetProperty(...ElapsedSeconds...)` shows the only writes are: Clamp 3 (CMPController:466), two resets-to-0 (CMPController:1072 stale-question restart, AssessmentAdminController:4105 admin reset-to-Open), and new-manual-entry init=0 (TrainingAdminController:1376). No completion-time wall-clock recompute exists. So the clamped (under-reported) value is what persists to export.
(b) Does StartExam's Math.Max(ElapsedSeconds, wallClockElapsed) (line 1135) fix the stored value? NO â€” it only mutates the in-memory `elapsedSec` fed to ViewBag.ElapsedSeconds (line 1145) for the live timer; it is never written back to the DB column. Exactly as the finding states.
(c) Is the export unreachable for online sessions? NO â€” confirmed reachable per filter above.

Bonus corroboration: the design spec docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md:63 explicitly defines this column as `(ElapsedSeconds / 60) menit â† bukan DurationMinutes (itu batas waktu)` â€” i.e. it is intended to be the ACTUAL used duration, so capping it at the base limit is a real deviation from documented intent.

Net effect: for an accommodation/ExtraTime exam where the worker uses the extra window past base DurationMinutes, the stored ElapsedSeconds freezes at DurationMinutes*60, and the admin Excel 'Durasi Aktual' under-reports the real duration by up to the granted ExtraTime. Grading/timer enforcement are unaffected (they read wall-clock independently), so this is a bounded data-correctness/drift defect.

---

### [PASS:PASS-02] Score formula and >= threshold are consistent across all copies (no drift) â€” verified clean

| | |
|---|---|
| **Severity** | NONE (klaim: LOW) |
| **Tipe** | correctness |
| **Confidence** | high |
| **Area** | Pass/fail formula + PassPercentage threshold + duplicated aggregator |

**Masalah:** Audited every copy of the percentage/pass formula for drift in rounding, int-vs-decimal truncation, and >= vs > threshold. All five GradingService copies (L134, L200, L387, L450, L565), the aggregator (Helpers/AssessmentScoreAggregator.cs:58-59), CMPController.SubmitExam (Controllers/CMPController.cs:1674,1704), and FinalizeEssayGrading/RecomputeEssayScores (AssessmentAdminController.cs:3558-3559, 3962-3963) use the identical expression `(int)((double)totalScore / maxScore * 100)` with `maxScore > 0` guard (â†’ 0 when zero, no DivideByZero) and `percentage >= PassPercentage`. All scoring fields are integers (ScoreValue int, EssayScore int?, Score int?), so the (int) cast is deliberate truncation-toward-zero and is uniform. The score-vs-threshold comparisons at CMPController.cs:2302/2326 and TrainingAdminController.cs:941 compare the already-percentage Score (or, for BulkBackfill, an HC-entered 0-100 score) against PassPercentage on the same 0-100 scale â€” also consistent. PassPercentage source of truth is the per-session column default 70 (Models/AssessmentSession.cs:30), DB default + CHECK 0..100 (Data/ApplicationDbContext.cs:210,214). Aside from the PASS-01 essay divergence, this sub-area is clean.

**Dampak:** No user-facing impact â€” informational confirmation that the pass/fail math itself does not drift between duplicated copies and rounding/threshold semantics are uniform. Recorded so the only real defect in this area (PASS-01) is unambiguous.

**Bukti:** Identical formula at Services/GradingService.cs:134,200,387,450,565; Helpers/AssessmentScoreAggregator.cs:58; Controllers/CMPController.cs:1674; Controllers/AssessmentAdminController.cs:3558,3962. Threshold `>= session.PassPercentage` at GradingService.cs:135,388 and `percentage >= passPercentage` at AssessmentScoreAggregator.cs:59. DB guard CK_AssessmentSession_PassPercentage and default 70 at Data/ApplicationDbContext.cs:210,214; model default at Models/AssessmentSession.cs:30. Integer field types confirmed: Models/AssessmentPackage.cs:41 (ScoreValue), Models/PackageUserResponse.cs:32 (EssayScore), Models/AssessmentSession.cs:26 (Score).

**Usulan fix:** No action required. Optional: once PASS-01 is fixed by routing all paths through AssessmentScoreAggregator, the remaining inline copies in GradingService could be collapsed onto the helper to permanently eliminate future drift risk.

**Verifikasi adversarial:** Verified every cited location against the actual code; all facts hold.

Formula (int)((double)totalScore / maxScore * 100) with `maxScore > 0 ? ... : 0` guard CONFIRMED at all 8 executable copies: GradingService.cs:134, 200, 387, 450, 565; Helpers/AssessmentScoreAggregator.cs:58; Controllers/CMPController.cs:1674; AssessmentAdminController.cs:3558 (via aggregator) and 3962 (via aggregator built at 3951). An independent grep for the formula confirmed these are the ONLY 8 source copies â€” every other hit is .planning markdown.

Threshold `>=` CONFIRMED uniform wherever pass/fail derives from a computed score: GradingService.cs:135 and 388 (>= session.PassPercentage); AssessmentScoreAggregator.cs:59 (percentage >= passPercentage); CMPController.cs:1704 (finalPercentage >= assessment.PassPercentage), 2302 and 2326 (score >= passPercentage); TrainingAdminController.cs:941 (row.Score >= passPercentage). A grep sweep of all PassPercentage/passPercentage uses found NO strict-`>` drift.

Field types CONFIRMED integer: AssessmentPackage.cs:41 ScoreValue int (=10); PackageUserResponse.cs:32 EssayScore int?; AssessmentSession.cs:26 Score int?, line 30 PassPercentage int =70 â€” so the (int) cast is deliberate truncate-toward-zero, uniform.

DB guards CONFIRMED: ApplicationDbContext.cs:210 CHECK CK_AssessmentSession_PassPercentage ([PassPercentage] >= 0 AND <= 100) and line 214 HasDefaultValue(70); mirrored in migration 20260214011828_AddAssessmentResultFields.cs:41-43.

Scale consistency CONFIRMED: CMPController.cs:2128-2129 sets passPercentage = assessment.PassPercentage and score = assessment.Score ?? 0, where assessment.Score is the stored percentage written by GradingService â€” both operands on 0-100 scale at L2302/2326. BulkBackfill at TrainingAdminController.cs:940-941 compares HC-entered row.Score (0-100) to passPercentage. The maxScore==0 â†’ 0 path also means no DivideByZero anywhere.

Adversarial attempt to refute: the only `IsPassed` assignment NOT using the formula is TrainingAdminController.cs:1359, fed by a Ya/Tidak Excel column (L1337), i.e. a manual-entry path the finding never claims uses the formula â€” so it is not a counterexample. No drift found in rounding, int/decimal truncation, or >= vs > anywhere in executable code.

---

## 6. Lampiran — Temuan Ditolak Verifikasi (false-positive, 5)

- **[ESS:ESS-02]** Empty ShuffledQuestionIds fallback fully bypasses the essay-graded gate, allowing finalize of ungraded essays (klaim MED)  
  Alasan tolak: The code-level mechanics the finding describes are accurate, but the claimed IMPACT (an admin finalizing ungraded essays) is unreachable due to an upstream guard, so the defect is refuted.

CONFIRMED code facts:
- Gate: Controllers/AssessmentAdminController.cs:3512-3524 builds essayQuestions via `shuffledIds.Contains(q.Id)` with NO fallback; when shuffledIds is empty, essayQuestions and essayResponses are empty and `essayResponses.Any(r => r.EssayScore == null)` (3523) is vacuously false â€” gate skipped.
- Scoring: lines 3530-3548 DO have the empty-shuffledIds response-derived fallback (3538-3548), so the two derivations are indeed inconsistent.
- AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:33-58) adds full q.ScoreValue to maxScore for an Essay (line 35) but adds 0 to totalScore when EssayScore is null (line 54) â€” confirms the understated-score math.
- GetShuffledQuestionIds (Models/UserPackageAssignment.cs:60-71) returns [] on JsonException/null.
- The project's own 376-DIAGNOSE.md (lines 51, 59) and 376-RESEARCH.md (line 50) explicitly document this exact "guard lolos vacuously" behavior as historical root-cause H1.

WHY THE IMPACT IS REFUTED (upstream guard):
FinalizeEssayGrading only acts on sessions with Status == PendingGrading (controller 3494, 3564 WHERE-guard); Completed/Open/InProgress/Cancelled are rejected at 3476/3494. The ONLY writer of PendingGrading is GradingService.GradeAndCompleteAsync (Services/GradingService.cs:206), and it enters the essay/PendingGrading branch only when `hasEssay` is true â€” `hasEssay` is derived from the SAME shuffledIds (lines 70, 136). If shuffledIds is empty at submit time, hasEssay is false â†’ the session takes the non-essay branch (236) and goes straight to Status="Completed" with Score=0, never reaching PendingGrading. 

Furthermore ShuffledQuestionIds is only ever WRITTEN at assignment creation (CMPController.cs:1037 StartExam; AssessmentAdminController.cs:5220/5321 Reshuffle) â€” grep of `ShuffledQuestionIds\s*=` shows no other writers â€” and Reshuffle skips any session not "Not started" (5295; PendingGrading has CompletedAt set â†’ treated as Completed â†’ skipped). Deserialization of the same stored string is deterministic. Therefore a session that reached PendingGrading necessarily had non-empty shuffledIds at submit and still has the identical non-empty value at finalize, so the gate at 3514-3524 sees the essay question(s) and correctly enforces EssayScore != null. The vacuous-skip branch is unreachable for any session the gate could legitimately process. The historical empty-shuffledIds essay sessions ended up Completed/Score=0 (never PendingGrading), so the finalize gate never applies to them either.

Note also the recompute path (HcPortal.Tests/EssayFinalizeRecomputeTests.cs:139-140 mirror) DID add a consistent response-derived essay-graded gate, while the forward finalize gate was left shuffledIds-only â€” so the inconsistency is real as code smell, just not exploitable.

- **[SAVE:SAVE-02]** Resume MC restore picks g.First() over duplicate/unordered rows â€” can pre-fill the wrong option (klaim LOW)  
  Alasan tolak: The cited code is accurate as quoted: CMPController.cs:1157-1160 builds savedAnswers via GroupBy(PackageQuestionId) then g.First().PackageOptionId with NO OrderBy, and StartExam.cshtml:698-704 correctly skips MA (input.exam-checkbox) / Essay inputs during MC radio restore. I also confirmed the index at ApplicationDbContext.cs:534 on (AssessmentSessionId, PackageQuestionId) is NOT unique (no .IsUnique(), unlike the explicit .IsUnique() at lines 510-511), so the DB does not forbid duplicate rows.

However, the finding's actual IMPACT depends entirely on its own premise: "If SAVE-01 produced duplicate MC rows with different optionIds." For a genuine MC question that premise is not reachable through any write path:
1) SaveAnswer (MC HTTP, lines 371-401) is an atomic upsert: ExecuteUpdateAsync updates ALL rows matching (sessionId, questionId) to the SAME new optionId, and only Adds a new row if updatedCount==0; the concurrent-insert race is caught (DbUpdateException, lines 391-400) and retried as an update. So even if two rows ever coexisted, the next save converges them to one optionId â€” they cannot hold DIFFERING optionIds.
2) SubmitExam MC branch (lines 1643-1657) groups by question and updates the single existing row or inserts one â€” one row per MC.
3) SaveMultipleAnswer (Hubs/AssessmentHub.cs:188-251) is the only multi-row path; it deletes-then-reinserts and is MA-only (checkbox UI, validates optionIds belong to the question). An MC (radio) question never invokes it.

Because at most one distinct MC optionId can exist per (session, question), g.First().PackageOptionId is deterministic in VALUE regardless of row ordering, so the missing OrderBy cannot pre-check the wrong option for a real MC question. The finding itself concedes the savedMultiAnswers cross-leak is benign and that the only real risk is the duplicate-row ambiguity inherited from SAVE-01 â€” and that inherited premise does not materialize given the convergent upsert. Adding OrderBy(SubmittedAt) would be a harmless defensive tidy, but the claimed user-facing wrong-answer outcome does not occur. Refuted (premise unprovable from the actual code; the upsert guard refutes it).

- **[SAVE:SAVE-06]** Client in-flight guard drops MC answer changes that occur while a save is in flight (no re-queue) (klaim LOW)  
  Alasan tolak: The mechanical behavior is accurate but the claimed data-loss impact is refuted by the form-POST + merge + upsert flow.

CONFIRMED mechanics: Views/CMP/StartExam.cshtml:622 â€” saveAnswerAsync returns early when inFlightSaves.has(String(qId)) with no requeue; :673-678 â€” 300ms debounce; :628-645 â€” success handler only flushes the offline pendingAnswers queue, not the dropped latest value. So a value change landing after the debounce timer fired while a prior request is still in-flight is indeed not re-sent via the per-question auto-save.

WHY IMPACT IS REFUTED (no answer lost): (1) StartExam.cshtml:817 updates the hidden form input ans_<qId> synchronously and unconditionally on every radio change â€” no in-flight guard â€” so it always holds the latest MC selection. (2) :81 renders that hidden input as name="answers[@q.QuestionId]" inside examForm; every submit path POSTs the form (review btn :980, mobile :258, timer auto-submit :482 all call examForm.submit()). (3) CMPController.cs ExamSummary GET (1342-1360) explicitly merges DB auto-saved answers with form/TempData answers with documented rule 'TempData wins on conflict... Merge ensures no answers are lost.' (4) SubmitExam MC scoring (CMPController.cs:1632-1657) reads selectedOptId from the form POST `answers` dictionary (not the auto-saved DB rows) and upserts the DB row from it (existingResponse.PackageOptionId = selectedOptId; line 1645) before grading; GradingService grades from DB after this upsert.

Therefore, in the exact SAVE-06 scenario (two rapid MC changes on a slow link, second auto-save dropped, stale value in DB), the final form submission carries the latest selection, overwrites the stale DB row, and grades the correct answer. Auto-save is best-effort progress, not the grading source of truth. Only residual effect is a transiently stale DB row visible to live HC monitoring between the dropped save and submit â€” cosmetic, not lost answer data. Refuted: the worker's final selection is NOT silently lost.

- **[T3:T3-02]** session.Score and session.Progress are never set for Tahun 3 interview completion, leaving stale/zero values on a Completed session (klaim LOW)  
  Alasan tolak: The write-site facts are accurate, but the claimed impact is refuted by null-guards/derivations in every actual consumer.

CONFIRMED at the write site: SubmitInterviewResults (AssessmentAdminController.cs:3743-3746) sets only InterviewResultsJson, IsPassed, Status="Completed", CompletedAt â€” never Score or Progress. GradingService does set them atomically (non-essay L241-243 Score+Progress=100; essay L209 Progress=100). Per Models/AssessmentSession.cs Score is int? (defaults null) and Progress is int (defaults 0); a Tahun 3 interview session is created with Progress=0 and Score unset (AssessmentAdminController.cs:1449,1457-1465), so a completed-passed interview carries Score=null, Progress=0. InterviewResultsDto (Models/ProtonViewModels.cs:129,139) is documented "Informational only â€” pass/fail is HC's manual decision." All literal claims check out.

BUT the finding's core impact â€” that surfaces "misrepresent a completed-and-passed Tahun-3 interview as 0% / incomplete progress" â€” does not materialize, because no surface reads AssessmentSession.Score/Progress generically without a guard:
1. Views/Admin/AssessmentMonitoringDetail.cshtml (the dedicated interview surface): Score column guards `@if (session.Score.HasValue)` else renders "â€”" (L282-286), and JS does the same (L943, L1380: `score !== null ... : 'â€”'`). So Score=null shows a dash, NOT 0%. The Progress column (L279) renders question-count `QuestionCount>0 ? "â€”/{QuestionCount}" : "â€”"` and JS L937 `total>0 ? progress/total : 'â€”'` â€” it derives from question count, never the raw Progress=0 field; a Tahun 3 interview has no questions so it shows "â€”".
2. Views/CMP/Records.cshtml:216 guards `@if (item.Score.HasValue) ... else <span>â€”</span>` (fed by WorkerDataService.GetUnifiedRecords L49 a.Score) â€” shows "â€”", not 0%.
3. A grep for `.Progress` across all .cshtml shows the only matches are unrelated CDP DeliverableProgress objects and the Home/Index dashboard aggregate ViewModel (Model.Progress.AssessmentProgress), NOT the raw AssessmentSession.Progress field â€” which is rendered nowhere directly.

The finding explicitly hinges on "any report/listing that reads AssessmentSession.Score or Progress for Proton sessions uniformly," but no such ungated surface exists. Additionally the finding misstates the value (says Score "0" â€” it is actually null). The hypothesized misrepresentation is speculative and is contradicted by the explicit HasValue guards and question-count derivations in the actual consumers. Default-to-refuted applies: the claimed defect's impact is already covered elsewhere.

- **[RST:RST-03]** JS-rendered (SignalR live-update) Reset button has no Cancelled guard despite its comment, showing Reset for Cancelled rows (klaim LOW)  
  Alasan tolak: REFUTED. The finding claims the JS row renderer emits the Reset form unconditionally for every status including Cancelled because there is "NO surrounding if-check" around the Reset block at Views/Admin/AssessmentMonitoringDetail.cshtml:896-902. That is false. The Reset form lives inside function buildActionsHtml(session, isPackageMode) (line 867), and the very top of that function has a function-level early return: line 871-872 `// Cancelled -- no actions at all` / `if (session.status === 'Cancelled') return html;` where `html` is still the empty string ''. This early return executes BEFORE any action HTML is built â€” Reshuffle (875), Akhiri Ujian (884), Reset (896), View Results (904), Activity Log (915). A Cancelled session therefore never reaches line 896, so the Reset form is NOT emitted; in fact col 6 (Actions) renders empty for Cancelled. The comment "// Reset -- all statuses except Cancelled" (line 896) is accurate precisely because of the line-872 guard; the per-block `if` the finding looked for is simply redundant given the early return. I also confirmed buildActionsHtml is the sole path that rebuilds the actions column: updateRow() calls it at line 961 (`tds[6].innerHTML = buildActionsHtml(...)`), and the SignalR push handlers (progressUpdate/workerStarted/workerSubmitted at lines 1321/1336/1353) only mutate individual cells (progress/status/score/result/completedAt) and never touch the actions column or call buildActionsHtml â€” workerSubmitted even sets status='Completed', never 'Cancelled'. So no live-update path can produce a Reset button on a Cancelled row. The finding's premise (missing guard â†’ Reset shown for Cancelled after SignalR refresh) does not match the actual code; the cited claimed impact cannot occur. The finding overlooked the guard 24 lines above the cited range. Per instructions, default to refuted; here it is affirmatively refuted, not merely uncertain.


---

## 7. Rencana Fix MINIMIZED — 7 Phase (cross-ref test + origin/main)

Cross-ref 71 finding vs coverage test + presence origin/main (prod). **55 in-prod, 14 unshipped; 0 covered test, 40 gap, 31 untested.** Dipangkas dari ~13-16 → 7 phase.

**Logika pangkas:** Menyusut dari ~13-16 fase menjadi 7. Strategi: (1) Gabung root-cause/file yang sama jadi satu fase. Triplet GRD-01/PASS-01/EDT-01 (essay-blind regrade) + tetangga method essay (ESS-01/03/04, EDT-02/CERT-02, CERT-01, GRD-02/RES-03/GRD-03) -> Fase 1 (12 temuan, 1 fix inti). (2) Cross-lens duplicate diperlakukan 1 fix: TMR-01==CAT-01, TMR-04==SAVE-05, OPS-02==REC-03, SAVE-04==OPS-03, GRD-02==RES-03. (3) Batch banyak fix 'batchable+trivial/small' yang regression-safe ke fase tematik: authz (reflection test infra), timer cluster, lifecycle+impersonation+token, persistence+shuffle+results, admin-records+category, proton-flow. (4) DEFER 14 temuan ke 999.x backlog: LOW unshipped/cosmetic/robustness (BYP-02/03 latent, REC-01/02/04/05 cascade unshipped, CERT-03/04 defense-in-depth, GAIN-02/03 latent, EDT-03/OPS-05 minor, CAT-03 fail-open relaxing). Empat fase awal prod_urgent berisi HIGH in-prod. Tiga fase butuh migration (persistence filtered-index, manual-records MAN-03, proton bypass BYP-01 + admin) -> flag IT. Tiap fase ~4-8 commit, coherent shippable unit.

### Phase 1 — Essay grading & cert-status core fix (regrade triplet + finalize hardening)
**PROD-URGENT · ~8 commit**  
**Findings:** CERT-01, CERT-02, EDT-01, EDT-02, ESS-01, ESS-03, ESS-04, GRD-01, GRD-02, GRD-03, PASS-01, RES-03  
Root-cause cluster di GradingService/AssessmentScoreAggregator/Finalize-Submit essay. Triplet GRD-01/PASS-01/EDT-01 = satu defect essay-blind regrade -> 1 commit + 1 regression test. ESS-01/03/04 di neighborhood SubmitEssayScore/FinalizeEssayGrading (stale-read + completeness + guard). CERT-01 reconcile 3 site cert-status; EDT-02+CERT-02 = satu branch Fail->Pass ValidUntil (HATI: jangan promote ITHandoff as-is, restore AddYears). GRD-02+RES-03 = satu SetEquals empty-guard. GRD-03 align import rule ride-along. Semua in-prod kecuali EDT-02/CERT-02 (unshipped) yg dibatch krn shared write-path.

### Phase 2 — Authz hole fix + reflection-authz regression suite
**PROD-URGENT · ~4 commit**  
**Findings:** QTY-01, RST-01, RST-02, RST-04  
HIGH missing [Authorize] (RST-01 AddExtraTime in-prod, QTY-01 CreateQuestion attribute-misplacement unshipped). Satu file test reflection-authz (pola CDPControllerAuthTests) cover semua. RST-04 extra-time cap = method sama dgn RST-01 (1 commit). RST-02 reset allow-list reconcile rides MonitoringUserStatusTests.

### Phase 3 — Timer & client-flag enforcement hardening
**PROD-URGENT · ~5 commit**  
**Findings:** CAT-01, SAVE-05, TMR-01, TMR-02, TMR-03, TMR-04  
TMR-01==CAT-01 (HIGH timer-guard, behavior-changing: re-enable enforcement utk Standard) = 1 atomic commit + verifikasi grading/cert discriminator. TMR-02/03 client-flag/TempData consume hardening, TMR-04==SAVE-05 (1-line Durasi Aktual). Semua localized di exam-taking handler, batch aman.

### Phase 4 — Exam lifecycle race-safety + impersonation/token gate
**PROD-URGENT · ~7 commit**  
**Findings:** OPS-01, OPS-04, STAT-01, STAT-02, TOK-01, TOK-02, TOK-03  
STAT-01 (HIGH abandoned/cancelled->certificated) + STAT-02 guarded ExecuteUpdate = lifecycle pair. OPS-01+TOK-03 = satu StartExam GET handler (wrap all writes !IsImpersonating). TOK-01+TOK-02 token-gate hardening (case-normalize + StartedAt gate). OPS-04 hub session-owner guard ride-along. Semua satu area state-mutation exam-start/save.

### Phase 5 — Answer persistence (race-safe upsert) + shuffle + results review
**PROD-URGENT · ~8 commit · +MIGRATION**  
**Findings:** OPS-03, RES-01, RES-02, SAVE-01, SAVE-03, SAVE-04, SHF-01, SHF-02, SHF-03  
SAVE-01+SAVE-03 dropped-unique root = race-safe answer persistence (filtered unique index = migration, flag IT) + atomic upsert. SAVE-04==OPS-03 elapsed-guard -> shared helper. SHF-01 (HIGH empty-package K=0 auto-grade 0%) 1-line + SHF-02/03 Abandoned-skip co-located. RES-01/RES-02 essay-aware Results review (CMPController.Results+Results.cshtml) localized.

### Phase 6 — Admin data integrity: manual records + category management
**PROD-URGENT · ~8 commit · +MIGRATION**  
**Findings:** CAT-02, CAT-03, CAT-04, CAT-05, CAT-06, MAN-01, MAN-02, MAN-03, MAN-04, MAN-05, MAN-06, OPS-02, REC-03  
MAN-* manual-entry import/validate cluster (MAN-03 filtered unique index = migration). CAT-02..06 category cycle/rename-cascade/dup (port OrganizationController pattern). OPS-02==REC-03 duplicate notif-message match = 1 fix. Semua in-prod admin data-integrity, satu domain admin-records.

### Phase 7 — Proton flow integrity: penanda removal + pelaksanaan gate + bypass index
**PROD-URGENT · ~7 commit · +MIGRATION**  
**Findings:** BYP-01, GAIN-01, PEL-01, PEL-02, PEL-03, PEL-04, T3-01  
T3-01 (HIGH interview Pass->Fail penanda removal in-prod, symmetric exam branch). PEL-01 (HIGH split-status: null ProtonTrackId/TahunKe in-prod + gate-bypass) extract shared gate helper, dipakai PEL-02/03/04. BYP-01 filtered unique index = migration. GAIN-01 essay-aware ET accounting (delete+reinsert SessionElemenTeknisScores) own-effort tapi ditempel di phase Proton/scoring krn in-prod + related finalize.

### Defer ke backlog 999.x (14)
BYP-02, BYP-03, CAT-03, CERT-03, CERT-04, CERT-05, EDT-03, GAIN-02, GAIN-03, OPS-05, REC-01, REC-02, REC-04, REC-05

