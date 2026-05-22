# Audit Sosialisasi-Internal-Tim-HC — Redundansi & Urutan Slide

**Tanggal:** 2026-05-22
**File audit:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (3713 baris · 41 slide)
**Tujuan:** Identifikasi redundansi konten + slide salah urutan/placement + minor inkonsistensi sebelum distribusi internal Tim HC.

---

## 1. Konteks

File ini adalah slide deck operasional untuk Tim HC (Role L2). Sudah melalui Phase A/B/C restruktur + 3 batch feedback fix (commit `790331fd` → `535c45e9`). Audit ini adalah re-pass untuk menangkap residual issues sebelum file dinyatakan final.

**Tidak termasuk scope:**
- Visual styling (CSS, color, layout) — sudah covered di feedback batch 1-3.
- Konten faktual per slide (sudah verified di commit `790331fd` "konten salah + redundansi + nice-to-have").
- Broken link audit (separate concern).

**Termasuk scope:**
- Redundansi konten antar-slide.
- Slide salah urutan / placement BAGIAN.
- Inkonsistensi struktur (section-eyebrow, tip-bar fungsi, label).
- Gap antara claim (agenda/landing) vs deliverable slide.

---

## 2. Map Struktur (As-Is)

```
SLIDE  | BAGIAN          | JUDUL
-------|-----------------|--------------------------------------
1      | Cover           | Portal HC KPB untuk Tim HC
2      | 0 Pengenalan    | Selamat Datang (agenda 6 item)
3      | 0 Pengenalan*   | Latar Belakang
4      | 0 Pengenalan*   | Apa Itu HC Portal KPB?
5      | 0 Pengenalan*   | 3 Platform Terpadu (CMP/CDP/BP)
6      | 0 Pengenalan*   | Struktur Role Pengguna (10 role · 6 level)
7      | 0 Pengenalan*   | Cara Mengakses (Dev vs Prod URL)
8      | 0 Pengenalan*   | Integrasi & Keamanan (6 fitur)
9      | 0 Pengenalan    | Area Kerja HC (4 area)
10     | 0 Pengenalan    | Alur Kerja Harian HC (5 step)
11-17  | 1 CMP           | Overview / Records Team / Analytics / Sistem Assessment / 5 Kategori / Alur 7 Step / Pre-Post Gain
18-20  | 2 Proton        | Assessment Proton / Alur T1-2 / Alur T3
21-30  | 3 Coaching/CDP  | Reviewer Chain / Dual Track / Hierarki / Progresi / Alur 9-step / Alur Mahir / Dashboard / Histori / Renewal / IDP+Training
31-37  | 4 Admin Panel   | Landing / Pekerja / Assessment Monitoring / Coach-Coachee Mapping / Silabus+Guidance / Override / Maintenance+Audit
38-41  | 5 Closing       | Notifikasi & Workflow / Tugas HC Cepat / Reference Card / Terima Kasih
```

\* = label BAGIAN 0 secara structural berlaku (per Phase C `da9169c6`) tapi section-eyebrow HTML element **hilang** di slide 3-8. Lihat finding M4.

---

## 3. Findings — Ringkasan

```
Kategori           | Count | Severity breakdown
-------------------|-------|---------------------
R Redundansi       |   5   | HIGH 0 · MED 3 · LOW 2
O Urutan/Placement |   7   | HIGH 4 · MED 3 · LOW 0
M Minor/Label      |   7   | HIGH 0 · MED 2 · LOW 5
G Gap/Taksonomi    |   2   | HIGH 1 · MED 0 · LOW 1
-------------------|-------|---------------------
TOTAL              |  21   | HIGH 5 · MED 8 · LOW 8
```

---

## 4. Redundansi (R1-R5)

### R1 — Bell icon disebut 4 tempat berbeda (MED)
**Lokasi:** S8 cat-card "Notifikasi Real-time" · S10 step 1 "Cek Notif Bell icon" · S29 callout "Auto bell icon ke pekerja" · S38 full deep-dive bell + dropdown.
**Dampak:** Audience dengar konsep bell 3x sebelum dapat penjelasan utuhnya. Tidak harmful, tapi noise.
**Fix usulan:** Drop card "Notifikasi Real-time" dari S8 (kalau S8 di-drop sekaligus per O1, masalah hilang). Pertahankan ref di S10 + S29 karena kontekstual. S38 jadi single source of truth — atau merge ke S39 per O4.

### R2 — Role-Based Access duplikasi (LOW)
**Lokasi:** S6 visual tangga 10 role · 6 level (full slide) + S8 cat-card "Role-Based Access: 10 role · 6 level akses".
**Fix usulan:** Drop card RBAC dari S8 (S6 sudah otoritatif). Pengurangan terotomatis jika S8 di-drop per O1.

### R3 — Training Records muncul di 2 slide (MED)
**Lokasi:** S11 CMP Overview sub-modul "Riwayat Pelatihan — database training records pribadi + tab Team View" + S30 module-card "Training Records — riwayat training + sertifikat".
**Dampak:** Audience bingung apakah ada 2 modul atau 1 modul yang sama.
**Fix usulan:** Pilih satu lokasi. Rekomendasi: keep di S11 (sudah list 6 sub-modul CMP), drop module-card Training Records dari S30 — S30 jadi single-card "IDP" saja. Atau pindahkan S30 ke akhir BAGIAN 1 per O3.

### R4 — Alur Proton T1-2 (4 step) overlap Alur Umum (7 step) (MED)
**Lokasi:** S16 Alur Assessment 7 step generic · S19 Alur Proton T1-2 4 step.
**Evidence:** S19 sendiri punya callout `<div class="alur-crossref">💡 Mirip Assessment Umum — beda di kategori & paket soal per track</div>` — sudah self-aware redundan.
**Fix usulan:** Simplify S19 dari 4-step stepper jadi callout-only "Identik Alur S16, beda kategori = Proton + paket soal per track Panelman/Operator + tahun". Atau merge sebagai sub-section di S16. Slide 19 standalone tetap valid kalau audience perlu visual stepper terpisah, tapi step-content harus highlight perbedaan saja.

### R5 — Dual Track Panelman/Operator disebut 2x (LOW, **acceptable**)
**Lokasi:** S18 Assessment Proton mention "Panelman/Operator Tahun 1-3" · S22 Coaching Proton Dual Track full.
**Verdict:** **Tidak perlu fix.** Sisi konteks berbeda (Assessment vs Coaching). Acceptable repetition untuk reinforce konsep dual-track sebagai foundation kedua sisi.

---

## 5. Urutan / Placement (O1-O7)

### O1 — S8 Integrasi & Keamanan placement premature di BAGIAN 0 (**HIGH**)
**Problem:** S8 list 6 fitur cross-cutting (LDAP/Anti-Copy/Audit Log/RBAC/Notifikasi Real-time/Import Excel) di BAGIAN 0 PENGENALAN — **sebelum** modul-modulnya dibahas. 4 dari 6 fitur sudah punya slide dedicated:
- Notifikasi → S38 full
- Audit Log → S37 full
- RBAC → S6 full
- Import Excel → S32, S34, S35 (per-modul)

Hanya LDAP + Anti-Copy yang tidak dapat slide khusus.
**Fix usulan (pilih 1):**
- **(a)** Pindah S8 jadi slide ke-N di BAGIAN 5 Closing sebagai recap fitur global.
- **(b)** Drop S8, sebar 2 fitur unik (LDAP + Anti-Copy) ke slot relevan: LDAP ke S7 Cara Mengakses, Anti-Copy ke S16 Alur Assessment step 3-4.
- **Rekomendasi: (b)** — eliminate redundansi sekaligus, deck turun 1 slide.

### O2 — S24 Progresi Kompetensi lompatan jauh dari S18 forward-ref (**HIGH**)
**Problem:** S18 ada callout "Detail komparasi 5 aspek per tahun … di slide Progresi Kompetensi (BAGIAN 3)" — forward-ref 6 slide ke depan. S24 isinya tabel 5 aspek (Fokus/Deliverable/Coaching/Assessment/Akhir Tahun) Tahun 1-2-3 — konten lebih cocok sebagai jembatan BAGIAN 2 → 3 daripada di tengah Coaching mechanics.
**Fix usulan:** Pindah S24 ke posisi setelah S20 (Alur Proton T3). Cross-ref di S25/S26 ("→ slide Progresi Kompetensi") jadi backward-ref, lebih natural. Atau merge S24 ke S18 sebagai expanded section.
**Rekomendasi:** Pindah S24 ke slot 21 (geser S21-S23 +1). Hasil: BAGIAN 2 jadi 4 slide (18,19,20,24-baru), BAGIAN 3 jadi 9 slide.

### O3 — S30 IDP+Training Records break Coaching narrative (MED)
**Problem:** S21-S29 = Coaching Proton lifecycle (mechanics → review → dashboard → histori → renewal). S30 tiba-tiba pivot ke "perpustakaan dokumen + riwayat training" — bukan coaching mechanics.
**Fix usulan (pilih 1):**
- **(a)** Pindah S30 ke akhir BAGIAN 1 CMP (slot 17.5 / setelah S17 Pre-Post).
- **(b)** Jadikan sub-section "Modul Pelengkap CDP" eksplisit di BAGIAN 3 — beri section-eyebrow berbeda "BAGIAN 3 — MODUL PELENGKAP".
- **(c)** Split BAGIAN 3 jadi 2: 3a Coaching Mechanics (21-26), 3b Monitor & Records (27-30). Lihat O5 untuk usulan terkait.
- **Rekomendasi: (a)** — paling clean, S30 secara fungsi memang library docs (CMP-ish), bukan CDP coaching-flow.

### O4 — S38 Notifikasi & Workflow di CLOSING (feature, bukan closing) (**HIGH**)
**Problem:** Closing seharusnya recap/CTA/reference. S38 isinya deep-dive bell icon + dropdown mockup — itu **feature explanation**, sama level dengan slide modul lain.
**Fix usulan (pilih 1):**
- **(a)** Pindah S38 ke akhir BAGIAN 0 (slot setelah S10), karena bell = entry point harian.
- **(b)** Merge S38 jadi prefix S39 Tugas HC Cepat (Daily checklist sudah mention bell).
- **(c)** Pindah S38 jadi awal BAGIAN 3 atau 4 (workflow context).
- **Rekomendasi: (a)** — bell adalah cross-cutting tool yang HC pakai sejak login, layak diperkenalkan di awal.

### O5 — Label BAGIAN 3 "COACHING PROTON / CDP" sempit (MED)
**Problem:** Label menjanjikan "Coaching", tapi 10 slide isinya: 4 coaching mechanics (S21-26) + 6 lifecycle/monitor (S27 Dashboard, S28 Histori, S29 Renewal, S30 IDP+Training). Lifecycle/monitor bukan coaching.
**Fix usulan (pilih 1):**
- **(a)** Rename label → "BAGIAN 3 — PROTON LIFECYCLE & COACHING".
- **(b)** Split jadi BAGIAN 3 "COACHING MECHANICS" (S21-26) + BAGIAN 4 baru "PROTON MONITOR & RECORDS" (S27-30), geser old BAGIAN 4 jadi 5, dst.
- **Rekomendasi: (a)** — minim disruption, lebih akurat.

### O6 — S2 Agenda 6 item ≠ 5 BAGIAN deck (MED)
**Problem:** Agenda promise 6 item:
1. Pengenalan Role HC
2. CMP
3. CDP (Coaching Proton)
4. Kelola Data Proton ← standalone
5. Admin Panel HC
6. Notifikasi & Tugas Cepat

Tapi deck struktur 5 BAGIAN. "Kelola Data Proton" di deck masuk BAGIAN 4 Admin (S35 Silabus+Guidance, S36 Override), bukan section terpisah.

**Fix usulan:** Update S2 agenda ke 5 item match BAGIAN deck:
1. Pengenalan & Role HC (B0)
2. CMP — Competency Mgmt (B1)
3. Assessment Proton (B2)
4. Coaching Proton + Lifecycle (B3)
5. Admin Panel HC (B4)

(BAGIAN 5 Closing tidak perlu di-agenda — implicit Q&A.)

### O7 — Admin slides salah urutan vs ABCD landing (**HIGH**)
**Problem:** S31 Admin Landing declare struktur 4 section: **A** Data Management · **B** Proton · **C** Assessment&Training · **D** System. Tapi slide sequence:
```
S32 ManageWorkers        → A
S33 AssessmentMonitoring → C  ← LOMPAT
S34 CoachCoacheeMapping  → B  ← MUNDUR
S35 Silabus+Guidance     → B
S36 Override             → B
S37 Maintenance+AuditLog → D+C
```
Pattern: A → **C** → B → B → B → D+C. Tidak match landing ABCD.

**Fix usulan:** Reorder slide post-S31 jadi A → B → C → D:
```
S32 ManageWorkers        → A   (tetap)
S34 CoachCoacheeMapping  → B   (was S34, sekarang S33)
S35 Silabus+Guidance     → B   (was S35, sekarang S34)
S36 Override             → B   (was S36, sekarang S35)
S33 AssessmentMonitoring → C   (was S33, sekarang S36)
S37 Maintenance+AuditLog → C+D (tetap S37)
```

---

## 6. Minor / Label (M1-M7)

### M1 — S21 title "Proton — Reviewer Chain" ambigu (LOW)
**Problem:** "Proton" tanpa "Coaching" prefix bisa dibaca sebagai Assessment Proton chain.
**Fix:** Title → "Coaching Proton — Reviewer Chain".

### M2 — S36 title "Override Data Pekerja" misleading (LOW)
**Problem:** Konten sebenarnya = fallback manual untuk KKJ mapping + silabus mapping saat sync gagal. Bukan CRUD data pekerja (yang ada di S32).
**Fix:** Title → "Override KKJ + Mapping Silabus" (atau "Override Data Proton").

### M3 — S11 sebut "6 sub-modul" tapi deck deep-dive 2 saja (LOW)
**Problem:** Audience expect 6 sub-modul dibahas, padahal hanya Records (S12) + Analytics (S13).
**Fix:** Tambah disclaim di S11 mockup-tip: "Fokus HC harian: Records Team + Analytics Dashboard. Sub-modul lain (KKJ, Assessment Saya, Sertifikasi, Budget Training) = ad-hoc, lihat Panduan Bab 2."

### M4 — section-eyebrow MISSING di S3-S8 (MED)
**Problem:** BAGIAN 0 PENGENALAN total 9 slide (S2-S10). Hanya S2, S9, S10 yang punya HTML element `<p class="section-eyebrow">BAGIAN 0 — PENGENALAN</p>`. S3-S8 (6 slide) tidak punya. Inkonsisten dengan BAGIAN 1-5 yang semua slide punya label.
**Fix:** Tambah `<p class="section-eyebrow">BAGIAN 0 &mdash; PENGENALAN</p>` di S3, S4, S5, S6, S7, S8.

### M5 — "tip-bar fungsi" kuning inkonsisten di mockup-slide (LOW)
**Problem:** Style `background:#fef3c7;border-left:3px solid #f59e0b` muncul di 7 mockup-slide (S27, S28, S31, S32, S33, S34, S35) sebagai "Fungsi:" caption. Tapi S37 + S38 yang juga mockup-slide **tidak** punya. Batch fix recent (`ef2a018f` Penjelasan fungsi per slide menu) miss 2 slide.
**Fix:** Tambah tip-bar fungsi style sama di S37 + S38.

### M6 — S2 agenda promise tidak terdeliver (LOW)
**Problem:**
- Agenda item 5 mention "Bank Soal" → tidak ada slide dedicated, hanya callout di S32 mockup-tip.
- S31 Admin landing list "CPDP" menu (section A) → tidak ada slide dedicated CPDP.
**Fix (pilih 1):**
- **(a)** Tambah 1-2 slide ringkas Bank Soal + CPDP (deck +2 slide).
- **(b)** Drop "Bank Soal" dari S2 agenda + tambah disclaim "CPDP = menu sync data eksternal, lihat Panduan §5.3" di S31.
- **Rekomendasi: (b)** — audience HC operasional tidak butuh deep-dive Bank Soal/CPDP.

### M7 — Renewal placement ambigu (MED)
**Problem:** S31 Admin Landing section C list "Renewal" badge angka "3" (actionable, di Admin). S29 Renewal Certificate deep-dive ada di BAGIAN 3 CDP. Audience cari Renewal di section Admin → tidak ketemu.
**Fix:** Tambah cross-ref di S31 mockup-tip: "Renewal deep-dive → lihat BAGIAN 3 slide Renewal Certificate Lifecycle".

---

## 7. Gap / Taksonomi (G1-G2)

### G1 — S9 "Area Kerja HC" 4 area ≠ 5 BAGIAN deck (**HIGH**)
**Problem:** S9 list 4 area (CMP / CDP / Kelola Data / Admin Panel). Deck punya BAGIAN substantif: 1 CMP / 2 Assessment Proton / 3 Coaching/CDP / 4 Admin / 5 Closing. **Assessment Proton tidak masuk taksonomi S9.** Audience bingung: apakah Proton subset CMP?
**Fix usulan (pilih 1):**
- **(a)** Update S9 jadi 5 area: CMP / Assessment Proton / Coaching Proton / Admin Panel / Notifikasi-Reference (atau drop Notifikasi).
- **(b)** Konsolidasi BAGIAN 2 Assessment Proton ke dalam BAGIAN 1 CMP (jadi BAGIAN 1 punya 10 slide), reduce deck ke 4 BAGIAN substantif → S9 4 area match.
- **Rekomendasi: (a)** — minim disruption, fix taksonomi saja.

### G2 — Penomoran "BAGIAN 0" tidak konvensional Bahasa Indonesia (LOW)
**Problem:** "BAGIAN 0" umumnya jarang di konteks Indonesia (biasanya start dari 1). Mungkin intentional ("preface"), mungkin tidak. Layak verifikasi intent.
**Fix usulan:** Konfirmasi ke user. Jika tidak intentional, renumber jadi BAGIAN 1-6.

---

## 8. Reorder Map Final (jika semua HIGH + MED diterapkan)

```
LAMA  →  BARU      ASAL ISSUE       NOTE
─────────────────────────────────────────
S1                                  Cover tetap
S2          (edit) O6               Agenda jadi 5 item
S3-S7       (M4)                    Tambah section-eyebrow
S8    →  DROP    O1(b) + R1 + R2    Sebar LDAP→S7, Anti-Copy→S16
S9          (edit) G1               5 area
S10                                 Alur Kerja Harian tetap
S38   →  S10.5  O4(a)               Bell icon pindah ke akhir B0
S11-S17                             CMP tetap (M3 disclaim)
S30   →  S17.5  O3(a)               IDP+TrainingRecords akhir B1
S18, S19, S20                       Proton tetap (R4 simplify S19)
S24   →  S21    O2                  Progresi jadi jembatan B2→B3
S21-S23     (geser +1)              Coaching mechanics
S25, S26, S27, S28, S29             Lanjutan B3
S31                                 Admin Landing tetap
S32                                 ManageWorkers (A) tetap di slot 32
S34   →  S33    O7                  Coach-Coachee Mapping (B)
S35   →  S34    O7                  Silabus+Guidance (B)
S36   →  S35    O7 + M2 rename      Override (B)
S33   →  S36    O7                  Assessment Monitoring (C)
S37                                 Maintenance+Audit (C+D) tetap di akhir
S39, S40, S41                       Closing tetap (S38 sudah dipindah)
```

**Total:** 41 slide → 40 slide (S8 drop, S38 dipindah bukan dihapus, tidak ada slide baru karena M6 pilih opsi b).

**Renumber side-effects:**
- `data-slide="N"` attribute untuk 40 slide.
- `<div class="slide-badge">SLIDE N / 41</div>` → `/ 40` semua.
- JS `const TOTAL = 41;` → `40`.
- Cross-ref text di slide body ("→ slide Progresi Kompetensi BAGIAN 3", "lihat slide Struktur Role di awal deck", dll) tetap pakai nama slide bukan angka, jadi aman.

---

## 9. Recommended Action Tiers

**Tier 1 — HIGH severity wajib (5 finding):**
- O1 Drop/relocate S8 (Integrasi & Keamanan)
- O2 Pindah S24 (Progresi Kompetensi)
- O4 Pindah S38 (Notifikasi & Workflow)
- O7 Reorder Admin slides ABCD
- G1 Update S9 area kerja jadi 5

**Tier 2 — MED severity sebaiknya (8 finding):**
- R1, R3, R4 (redundansi konten)
- O3 (S30 IDP placement)
- O5 (label B3 rename)
- O6 (S2 agenda update)
- M4 (section-eyebrow B0)
- M7 (Renewal cross-ref)

**Tier 3 — LOW severity boleh (8 finding):**
- R2, R5, M1, M2, M3, M5, M6, G2

---

## 10. Out of Scope / Pending User Decision

- **G2** konvensi BAGIAN 0 — tunggu konfirmasi intent.
- **O3** strategi (a/b/c) — tunggu pilihan.
- **O1** strategi (a/b) — tunggu pilihan (rekomendasi: b).
- **O4** strategi (a/b/c) — tunggu pilihan (rekomendasi: a).
- **M6** strategi (a/b) — tunggu pilihan (rekomendasi: b).
- **Apakah implement** semua tier 1+2 sekaligus, atau pisahkan jadi batch?

---

## 11. Risk & Rollback

- **Renumber data-slide + badge "/41" → "/40"** = 40+ Edit operations, risiko typo. Mitigasi: Edit terstruktur per slide, verifikasi dengan grep count akhir.
- **Drop S8** = kehilangan 6 fitur cross-cutting di awal deck. Mitigasi: O1(b) sebar 2 fitur unik (LDAP, Anti-Copy) ke slot relevan.
- **Reorder Admin S32-S37** = referensi internal cross-slide (kalau ada) bisa stale. Mitigasi: grep `slide [0-9]+` + `S[0-9]+` sebelum + sesudah.
- **Rollback:** git revert per-commit. Tiap finding di commit terpisah untuk granular revert.

---

## 12. Next Step

Setelah spec approved → invoke `superpowers:writing-plans` untuk hasilkan implementation plan dengan:
- Task breakdown per finding (atau per tier).
- Atomic commit strategy.
- Verifikasi script (browser preview + grep count slide).
- Test plan (visual check tiap slide di Chrome / Playwright).
