# Reframe Abstrak Paten Portal HC KPB → Fokus PROTON

**Tanggal:** 2026-06-03
**Status:** Approved (brainstorm), siap implement
**Artefak tersentuh:** `docs/abstract/abstrak-portal-hc-kpb.html`, `docs/abstract/build_abstrak_docx.py`, `docs/abstract/abstrak-portal-hc-kpb.docx` (regen)

## Masalah

Abstrak paten Portal HC KPB versi lama membingkai sistem sebagai *portal manajemen kompetensi generik*. PROTON — yang sebenarnya program inti — hanya muncul **1 kali** ("pendampingan PROTON"), tenggelam di antara pembahasan assessment, sertifikat, RBAC, dan arsitektur cascade. User minta abstrak **fokus ke PROTON**.

Temuan tambahan saat analisa:
- Abstrak lama meminjam kerangka struktural dari paten DCS (`2. File Deskripsi, Klaim, Abstrak, Drawing.docx`) — analogi "komponen lambat/cepat + orkestrator cascade + penyelesaian simultan" adalah port dari "variabel lambat/cepat + solver adaptif". Dokumen DCS itu **template format**, bukan isi Portal HC.
- Abstrak lama menulis "Coaching berbasis evidensi CMP–CDP–BP" → **keliru**. CMP/CDP/BP adalah **modul/platform**, bukan jenis evidensi.
- Tidak ada dokumen Klaim/Deskripsi paten Portal HC terpisah → abstrak standalone, bebas di-reframe.

## Keputusan (locked via brainstorm)

| # | Keputusan | Pilihan user |
|---|---|---|
| 1 | Derajat refocus | **Reframe penuh** — PROTON jadi tulang punggung; assessment/sertifikat/renewal jadi mekanisme pelayan siklus PROTON |
| 2 | Perlakuan novelty teknis | **Buang detail arsitektur** — fokus digitalisasi program PROTON; cascade/transaksi/renewal-precheck/audit dihapus |
| 3 | Judul | **Sebut PROTON eksplisit + akronim** |
| 4 | Kepanjangan PROTON | Formal: *Professional Refinery Operations Competency Development* (default; memory ref) |
| 5 | Format dokumen | Pertahankan format paten: paragraf tunggal justified, Times New Roman 12pt, spasi 2.0, nomor baris tiap 5, A4 |

## Trade-off yang diterima user

⚠️ **Novelty teknis paten melemah.** Dengan membuang cascade orchestrator, renewal pre-check, transaksi atomik, dan post-commit file cleanup, abstrak menjadi **kuat sebagai deskripsi program** tapi **lemah sebagai klaim invensi teknis**. Jika dokumen ini dipakai untuk filing paten sungguhan, klaim teknis perlu diangkat kembali di bagian Klaim/Deskripsi (bukan abstrak). User sudah memilih ini secara sadar.

## Sumber otoritatif konten PROTON

`docs/Naskah Video PROTON.docx` (revisi 2026-05-20) + `reference_singkatan_portal_hc.md`:
- 2 modul: **CDP** (Competency Development Platform) untuk Coaching PROTON; **CMP** (Competency Management Platform) untuk Assessment PROTON. BP = Business Partner (Coming Soon, di-drop dari abstrak).
- Prinsip **SMART**.
- 2 track: **Panelman** + **Operator**; 3 tahun berjenjang **Foundation → Pendalaman → Mastery**; lulus syarat naik tahap.
- **Alur 6 langkah:** (1) HC tetapkan silabus + assign coach senior → (2) coachee selesaikan + upload deliverable via IDP → (3) coach catat sesi via Coaching PROTON → (4) approval berlapis Sr Supervisor + Section Head + review final HC → (5) Final Assessment (Y1-2 PG auto-grade, Y3 wawancara panel) → (6) Histori PROTON permanen.
- Sertifikat digital terbit otomatis pasca-lulus; status kompetensi terupdate.

## Verifikasi klaim vs kode (fact-check)

| Klaim | Bukti | Verdict |
|---|---|---|
| ASP.NET Core MVC | `.csproj` → `net8.0` + AspNetCore | ✓ |
| Role approval Sr Supervisor/Section Head | `Controllers/CDPController.cs` → `SrSupervisor`, `SectionHead` | ✓ |
| Sertifikat masa berlaku + renewal | Models `ValidUntil`/`Expir` + `RenewsSessionId` | ✓ |
| Nilai kelulusan 75 | Kode pakai **Passing Grade per-kategori configurable**, BUKAN 75 hardcoded | ⚠️ over-specific → tulis "ambang nilai kelulusan yang ditetapkan (75)" |
| Alur "otomatis" | Alur = workflow digerakkan manusia (assign/upload/approve manual); hanya skoring+sertifikat+histori otomatis | ⚠️ overclaim → buang kata "otomatis" sbg label alur |

## Output final

### Judul
```
SISTEM DIGITAL PROGRAM PENGEMBANGAN KOMPETENSI OPERASI KILANG PROFESIONAL (PROTON)
```

### Badan abstrak FINAL (paragraf tunggal, 204 kata — terverifikasi 1 halaman)
> Invensi ini berkaitan dengan suatu sistem digital berbasis web (ASP.NET Core MVC) yang mengoperasionalkan program PROTON (*Professional Refinery Operations Competency Development*), yaitu program pengembangan kompetensi terstruktur berprinsip SMART bagi pekerja kilang minyak pada fase operasi. Program dijalankan pada dua jalur keahlian (Panelman dan Operator) selama tiga tahun berjenjang — *Foundation*, Pendalaman, dan *Mastery* — melalui dua modul terintegrasi: *Competency Management Platform* (CMP) menyusun Kebutuhan Kompetensi Jabatan (KKJ), melaksanakan asesmen kompetensi teknis, dan menyepakati Silabus, sedangkan *Competency Development Platform* (CDP) mengeksekusinya dengan metode *blended learning*. Pelaksanaan PROTON mengikuti alur enam langkah: (1) penetapan Silabus per jabatan berbasis KKJ dan penugasan *coach* oleh *Human Capital*; (2) pengunggahan bukti *deliverable* oleh *coachee* via fitur *Individual Development Plan* (IDP); (3) pencatatan sesi pendampingan via fitur *Coaching* PROTON berpedoman *Coaching Guidance* per dimensi kompetensi; (4) persetujuan berlapis oleh *Sr Supervisor*, *Section Head*, dan *review* final *Human Capital*; (5) *Final Assessment* berupa ujian pilihan ganda otomatis (Tahun 1–2) atau wawancara panel (Tahun 3), ambang kelulusan ditetapkan (75); serta (6) perekaman permanen pada Histori PROTON. Setelah lulus, sertifikat digital terbit otomatis dengan masa berlaku dan rantai pembaruan lintas-periode, sehingga status kompetensi dan riwayat pengembangan termutakhirkan secara konsisten. Dengan demikian, invensi mereplikasi program PROTON dalam lingkungan digital yang terstruktur, terukur, dan terlacak.

### Model inti PROTON (Ref: Kickoff-PROTON.html / Pedoman HCM Dir SDM A5.2-01/K20000/2025-S9)
Pipeline `CMP → CDP`:
- **CMP** (define): susun **KKJ** (Kebutuhan Kompetensi Jabatan) → asesmen kompetensi teknis → penyepakatan **Silabus**
- **CDP** (execute): eksekusi pengembangan kompetensi metode **Blended Learning** (Coaching PROTON + deliverable + Final Assessment)
- **Coaching Guidance**: pedoman materi coaching per dimensi kompetensi (5 dimensi × 2 track = 10 dok), rujukan Coach saat sesi, ikut revisi KKJ & Silabus
- 2 asesmen berbeda (jangan konflasi): **asesmen kompetensi teknis** (CMP, awal, penentu track) vs **Final Assessment** (CDP, per tahun, ujian naik tahap)

### Iterasi panjang & verifikasi 1 halaman
v2 (227 kata) → +unsur blended learning/KKJ/Silabus/Coaching Guidance + perbaikan framing CMP/CDP v3 (285 kata) → pangkas v5 (204 kata). Page count diverifikasi via Word COM `ComputeStatistics(wdStatisticPages)`:
- 231 kata → **2 halaman** ❌
- 204 kata → **1 halaman** ✅ (25 baris; mepet, tak ada baris sisa)

Catatan: 204 kata pas 1 halaman dengan spasi ganda + judul panjang. Bila perlu margin aman lintas-mesin, pangkas ~10 kata lagi atau turunkan spasi body 2.0 → 1.5 (1.5 juga lazim utk abstrak DJKI).

### Yang dibuang vs versi lama
Komponen lambat/cepat, orkestrator cascade adaptif, RBAC 6-tingkat eksplisit, renewal pre-check, transaksi atomik, post-commit file cleanup, audit log, interlock dependensi dokumen, notifikasi pasif on-login, tiga mode penilaian (Manual/Pre-Post/Coaching), frasa keliru "evidensi CMP–CDP–BP", dokumen CPDP.

## Rencana implementasi
1. Update `<title>` div + `<p class="body">` di `abstrak-portal-hc-kpb.html` (em-tag untuk istilah asing).
2. Update string judul + list `segments` di `build_abstrak_docx.py`.
3. Regen `abstrak-portal-hc-kpb.docx` via `python docs/abstract/build_abstrak_docx.py`.
4. Verifikasi: ekstrak teks docx, pastikan judul + badan sesuai, italic ter-render, format paten utuh.
