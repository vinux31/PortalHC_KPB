# Proton Bypass Tahun — DRAFT (PAUSED)

> **⚠️ SUPERSEDED 2026-06-09** oleh `2026-06-09-proton-bypass-tahun-design.md` (spec final).
> File ini = arsip draft. Pakai spec final untuk implementasi.

> **STATUS: PAUSED 2026-06-09.** Diskusi bypass dijeda karena ketemu gap fondasi
> (logic kelulusan Proton belum konsisten). **Diskusi A (fondasi) dikerjakan dulu.**
> Bypass ini DEPENDS ON hasil Diskusi A. Lanjutkan brainstorm bypass setelah A selesai.

## Konteks fitur

Perluasan fitur **Proton Override** (page admin di Kelola Data, `ProtonData/Override`).
- **Sekarang:** override status deliverable Proton per-sel (OverrideSave, AssessmentAdminController? → ProtonDataController:1400).
- **Mau ditambah:** **Bypass Tahun** — admin/HC pindahin coachee antar tahun/track/unit dengan alasan tertulis + audit. Contoh: Worker A Tahun 1 unit X → Tahun 2 unit X.

Gate: `[Authorize(Admin,HC)]`, AntiForgery, audit log (ikut pola OverrideSave).

## Domain (verified dari kode)

- `ProtonTrack` = TrackType (Panelman/Operator) × TahunKe (Tahun 1/2/3). 6 track. Unit BUKAN di track — unit ada di `ProtonKompetensi`.
- `ProtonTrackAssignment` (CoacheeId, ProtonTrackId, IsActive, AssignedAt, DeactivatedAt) — assign coachee ke 1 track. Dibuat lewat CoachMappingController (sepaket CoachCoacheeMapping).
- `ProtonDeliverableProgress` — progress per deliverable (Pending/Submitted/Approved/Rejected), unit-filtered saat bootstrap.
- `ProtonFinalAssessment` (CompetencyLevelGranted 0-5, ProtonTrackAssignmentId) — penanda "lulus tahun".
- Progresi Tahun 1→2→3 = **manual re-assign** (gak ada tombol naik tahun). Bypass = bungkus resmi + audit + alasan.
- "Lulus tahun" (CDP:3204) = `allDeliverableApproved && ProtonFinalAssessment ada`.

## KEPUTUSAN TERKUNCI (A–E)

- **Fork1 = B**: saat skip deliverable (CL-B), **force semua deliverable jadi "Approved"** + tulis `DeliverableStatusHistory` (StatusType="Bypassed-AutoApprove", actor, reason) sbg jejak jujur.
- **Fork3 = redesign** page Override jadi 2 tab: Tab1 "Override Deliverable" (existing), Tab2 "Bypass Tahun" (baru).
- **D-A = setuju**: tambah closure mode **CL-C "Tinggalkan"** (deactivate tanpa nilai) buat demosi/koreksi.
- **D-B = max 1 langkah**: |Δtahun| ≤ 1. Lompat 1→3 harus 2× bypass.
- **D-C = ii (blok pindah)**: CL-B(b) — worker baru pindah SETELAH assessment di-skor/lulus.
- **D-D = blok**: kalau final assessment tahun asal sudah ada, CL-B ditolak ("udah ada nilai") → arahkan CL-A.
- **D-E = ii (create baru)**: bypass ke tahun yang worker pernah jalani → selalu bikin assignment baru fresh; row lama jadi history mati.

## CLOSURE MODE (4 mode, HC pilih per bypass)

| Mode | Kapan | Syarat | Hasil |
|------|-------|--------|-------|
| CL-A Lulus (sudah ada) | tahun asal komplit normal | allApproved + final ada | langsung pindah |
| CL-B(a) Input manual | offline assessment udah jalan, tinggal catat | final belum ada (D-D) | input level → create final + force-approve → pindah instan |
| CL-B(b) Buat assessment baru | butuh record exam + sertifikat | final belum ada | buat AssessmentSession tahun-asal → **wajib lulus/skor dulu (D-C)** → baru pindah |
| CL-C Tinggalkan | demosi/koreksi | — | deactivate tanpa nilai → pindah |

Pola: NAIK → CL-A/CL-B. TURUN/koreksi → CL-C. GANTI unit/track → CL-C/CL-B.

## POHON KEPUTUSAN 1 OPERASI

```
BYPASS(worker, sourceYear S, targetTrack T, mode, alasan*):
VALIDASI (blok kalau gagal):
  - alasan wajib
  - T ≠ track aktif S (E14 no-op)
  - |tahun(T) − tahun(S)| ≤ 1 (D-B)
  - worker punya tepat 1 assignment aktif (E8)
  - mode CL-B → final S BELUM ada (D-D)
  - mode CL-A → S komplit (allApproved+final), else tolak
EKSEKUSI (1 transaksi; CL-B(b) tahap-2 pakai catatan-tunggu):
  [tutup tahun asal]
    CL-A    : nilai S sudah ada
    CL-B(a) : force-approve deliverable + history + create ProtonFinalAssessment(level=input HC)
    CL-B(b) : force-approve + history + create AssessmentSession(TahunKe=S, tipe ikut tahun)
              → STOP (D-C). Worker pindah pas assessment LULUS/di-skor.
    CL-C    : tanpa nilai
  [cancel exam aktif S kecuali yang baru dibuat CL-B(b)] (E5)
  [deactivate assignment S] IsActive=false, DeactivatedAt=now
  [aktifkan target T] create baru (D-E)
  [bootstrap deliverable T] unit-filtered pakai Unit dari FORM (E7, bukan dari mapping)
  [coach: pertahankan / ganti via dropdown form] (M-5)
  [audit log]
```

## EDGE CASE & RESOLUSI

- E1/D-E: target pernah dijalani → create baru fresh.
- E2: turun, T-asal punya final "Lulus" → final lama disimpan history; status aktif = tahun baru.
- E5: exam in-progress saat bypass → auto-cancel (kecuali assessment CL-B(b) yang baru dibuat).
- E7: assignment tanpa mapping → bootstrap pakai Unit dari form.
- E8: dobel assignment aktif → bypass selalu deactivate asal → dijamin 1 aktif.
- E10: evidence file orphan saat ganti unit → KEEP (assignment inactive = arsip).
- F1 (CL-B(b) lupa skor): gak terjadi — D-C=ii blok pindah.
- F2 duplicate final: blok via D-D.
- MISS-2 (exam gagal di CL-B(b)): catatan-tunggu gak fire, worker tetap di tahun asal, bisa retry, atau HC batal pending.

## DETAIL FINAL (M-1..M-8)

- **M-1**: bypass terbitin "surat resmi" (ProtonFinalAssessment) buat SEMUA tahun (samain). DEPENDS Diskusi A.
- **M-2**: catatan-tunggu = **opsi (i)** tabel pending (`PendingProtonBypass`: target+unit+mode+alasan+linked AssessmentSessionId). Tipe assessment **ikut tahun** (Tahun 1/2 = exam online worker; Tahun 3 = interview HC). Bukan dipaksa interview.
- **M-3**: ~~form input manual = angka level (0-5) + catatan~~ **UPDATED per Diskusi A (A-3 = level dimatikan):** form CL-B(a) **TANPA input level** — cukup terbit penanda `ProtonFinalAssessment` + catatan/alasan. `KkjMatrixItemId` diabaikan (legacy/mati). `CompetencyLevelGranted` dormant=0.
- **M-4**: (gabung M-3).
- **M-5**: coach — bypass jaga coach. Same unit → default coach lama, **HC boleh ganti via dropdown** di form. Ganti unit → **dropdown pilih coach baru di form** (rekomendasi, atomic; bukan suruh ke page mapping).
- **M-6**: (teknis) extract bootstrap progress yang terima Unit eksplisit (sekarang `AutoCreateProgressForAssignment` ambil unit dari mapping, CoachMapping:1338).
- **M-7**: (teknis) normalisasi status awal deliverable ("Belum Mulai" vs "Pending" — ProtonData:521 vs CDP "Approved").
- **M-8**: tombol batal. (1) Batal pending CL-B(b) sebelum lulus. (2) Undo bypass executed — **DIBATASI**: cuma kalau tahun tujuan belum ada aktivitas. Reverse pakai audit (reactivate asal, deactivate tujuan, hapus final bypass, balikin force-approve).

## REDESIGN PAGE (Fork3)

2 tab. Tab2 Bypass Tahun: filter Bagian/Unit/Track → tabel worker (nama, track aktif, progress X/Y, final ✓/✗, [Bypass]) → wizard 3 langkah (Tujuan → Closure mode (auto enable/disable per state) → Detail+alasan+konfirmasi).

Endpoint baru (pola OverrideSave): `GET BypassList`, `GET BypassDetail`, `POST BypassSave`, + handling pending + cancel.

## DEPENDENCY → Diskusi A

Bypass numpang definisi "lulus Proton tahun X". Diskusi A harus selesai dulu:
- urutan deliverable 100% → final assessment (gate)
- Tahun 1/2 lulus exam → terbit ProtonFinalAssessment (fix gap)
- nasib CompetencyLevelGranted (matikan / hidupkan)
- satukan sinyal AssessmentSession ↔ ProtonFinalAssessment

Setelah A beres → resume brainstorm bypass dari sini.
