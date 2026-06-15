---
title: "Readiness Ujian Lisensor — Profil & Peta Risiko (gladi-bersih E2E)"
date: "2026-06-15"
promoted: false
source: "/gsd-explore session 2026-06-15"
---

# Readiness Ujian Lisensor — Profil & Peta Risiko

Tujuan: pastikan fitur Assessment **berjalan lancar di ujian REAL** (kategori
Training **lisensor**, tipe **standard**). Bukan audit kode lagi — code-audit full
Assessment sudah dilakukan 2026-06-13 (lihat
`.planning/notes/2026-06-13-audit-full-sistem-assessment.md`) dan jadi pendorong
v29.0 + v30.0. Yang kurang = **verifikasi E2E "gladi-bersih"**: jalankan satu ujian
lisensor realistis end-to-end di kondisi nyata.

## Profil ujian (dari sesi explore 2026-06-15)

| Aspek | Nilai |
|---|---|
| Kategori | Training **lisensor** (berujung kelulusan/sertifikat) |
| Tipe assessment | **Standard** |
| Skala | **≤ 30 peserta serentak**, 1 window |
| Jenis soal | **Single Answer + Multiple Answer + Essay + soal bergambar** (keempatnya) |
| Cakupan "harus lancar" | **Full lifecycle + monitoring admin real-time** |

Cakupan penuh = peserta start → jawab (4 jenis soal) → submit → scoring auto →
grading essay manual admin → skor akhir → lulus/tidak → **sertifikat lisensor terbit**
→ **admin pantau progress real-time** saat ujian berlangsung.

Beban server **bukan** risiko utama (≤30 peserta). Fokus = **correctness scoring +
kelancaran alur lifecycle**.

## Peta risiko → area bug historis

Tiap jalur scoring di ujian ini menyentuh area yang historisnya paling sering bug.
Prioritas verifikasi:

| Jalur | Risiko / referensi historis |
|---|---|
| Essay grading & correctness | **v30.0** (ECG-01..06, helper `IsQuestionCorrect` essay>0). Area bug terbaru — prioritas tinggi |
| Multiple Answer scoring | Partial vs all-or-nothing — pastikan aturan benar untuk lisensor |
| Single Answer scoring | exact-match auto — risiko rendah, tetap diverifikasi |
| Soal bergambar (render+upload) | **v24.0** (IMG-01..07, RND-04, SYN). Razor dynamic WAJIB diuji Playwright runtime — grep+build tak cukup (lesson Phase 354) |
| Sertifikat lisensor | terbit setelah lulus — template + data benar |
| Monitoring admin real-time | **v30.0** UIG-01..04 (page /Admin/EssayGrading + worker-list) |

## Constraint kerja (WAJIB — dari user 2026-06-15)

Code **baru di-deploy IT ke Dev**. Saat gladi-bersih:

1. **Report-first, BUKAN auto-fix.** Temuan bug/error/issue → **laporkan ke user dulu**.
2. Sertakan klasifikasi tiap temuan: tingkat (HIGH/MED/LOW), apakah butuh **fix lokal**,
   apakah butuh **re-deploy ke Dev IT**.
3. **User yang putuskan** fix & re-deploy. Jangan langsung patch tanpa persetujuan.
4. ❌ Jangan edit kode/DB langsung di Dev/Prod (sesuai
   [`docs/DEV_WORKFLOW.md`](../../docs/DEV_WORKFLOW.md)). Fix = lokal → verify → push →
   notify IT.

Output gladi-bersih = **checklist readiness PASS/FAIL + daftar temuan terklasifikasi**,
bukan diff.

## Verifikasi lokal (lihat CLAUDE.md)

- Seed realistis = klasifikasi `temporary + local-only`, snapshot DB dulu, catat
  `docs/SEED_JOURNAL.md`, restore setelah test (SEED_WORKFLOW.md).
- E2E gambar/dynamic → Playwright runtime, `--workers=1` (reference_local_e2e_sql_env_fix).
- AD lokal: `Authentication__UseActiveDirectory=false` saat `dotnet run` (Phase 355 lesson).
- Admin login lokal: `admin@pertamina.com` (reference_dev_credentials).

## Next

- Phase verifikasi E2E gladi-bersih (lihat ROADMAP / research questions).
- Open unknown belum dijawab → `.planning/research/questions.md`.
