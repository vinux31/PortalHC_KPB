# Phase 420: Form Create/Edit — Persistensi Field + UX Pre-Post - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-22
**Phase:** 420-form-create-edit-persistensi-field-ux-pre-post
**Areas discussed:** Fix E-01 (shuffle reset), Scope setelan Pre-Post, Pre baseline (retake/pass), Letak field (SamePackage + input standard)

---

## Fix E-01 — Bug "Acak Soal/Pilihan reset OFF tiap simpan Edit"

| Option | Description | Selected |
|--------|-------------|----------|
| Render toggle Acak di Edit | Tampilkan toggle Acak Soal/Pilihan di EditAssessment, terisi dari Model; 3 jalur (Create/Edit/ManagePackages) konsisten | ✓ |
| Buang write shuffle dari POST Edit | Edit tak sentuh shuffle; kelola eksklusif via ManagePackages | |

**User's choice:** Render toggle Acak di form Edit (Recommended)
**Notes:** Shuffle dapat diatur dari Create + Edit + ManagePackages tanpa saling timpa.

---

## Scope setelan mode Pre-Post

| Option | Description | Selected |
|--------|-------------|----------|
| Pisah sub-kartu Post/Bersama | Kartu "Setelan Post-Test" (pass/cert/validuntil/retake) vs "Setelan Bersama Pre & Post" (shuffle/review/token) | ✓ |
| Label/badge inline per setelan | Satu blok, badge "Post"/"Pre & Post" per setelan | |
| Cukup note penjelas singkat | Satu kalimat keterangan di atas Group setelan | |

**User's choice:** Pisah sub-kartu (Recommended)
**Notes:** Paling jelas; berlaku khusus mode Pre-Post, Standard tetap.

---

## Pre baseline — Retake & Nilai-Lulus

| Option | Description | Selected |
|--------|-------------|----------|
| Post saja | Sembunyikan Retake di Pre-Post; PassPercentage utk Post; Pre tanpa lulus/gagal | ✓ |
| Tampilkan, label "Post saja" | Kontrol tetap terlihat dengan label scope | |
| Berlaku keduanya | Pre juga punya pass/retake | |

**User's choice:** Post saja (Recommended)
**Notes:** Pre = baseline murni, sesuai praktik Pre/Post.

---

## Letak field — SamePackage + input standard tersembunyi

| Option | Description | Selected |
|--------|-------------|----------|
| Header pasangan + buang input standard | SamePackage ke header Pre-Post; input jadwal/durasi/EWCD standard tidak ter-POST saat Pre-Post | ✓ |
| Baris antara Pre & Post, input standard dibiarkan | SamePackage jadi baris sendiri; input standard tetap (server abaikan) | |

**User's choice:** Header pasangan + buang input standard (Recommended)
**Notes:** Bersih sesuai audit; eliminasi field duplikat ter-POST.

---

## Claude's Discretion

- FORM-10 rename `AssessmentTypeInput` → `CreationMode` (internal); label UI boleh tetap; perbarui XML-doc.
- FORM-05 lock granularity: group-aware (blok bila sesi/grup Completed) — planner tentukan presisi.
- FORM-02/03/04/06: ikuti pola existing (bulk-add copy :2184-2186; IsManualEntry filter :994).

## Deferred Ideas

- Overlap v32.6 (branch main) layout form + Section/Opsi-Dinamis — rekonsiliasi saat merge, tidak ditarik ke fase 420.
