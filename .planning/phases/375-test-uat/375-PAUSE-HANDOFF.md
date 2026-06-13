# Phase 375 — Pause Handoff (2026-06-14)

**Status:** ~95% selesai. HANYA tinggal: approve checkpoint UAT + tulis `375-03-SUMMARY.md` + tutup phase.

## Yang sudah selesai (committed, NOT PUSHED)

| Plan | Status | Commit |
|------|--------|--------|
| **375-01** xUnit sweep + SHUF-15 | ✅ DONE — `ShuffleModeMatrixTests.cs` (5 test) + full suite **352/352**; SHUF-15 closed (CMPController clean). SC#1 ✓ | `fcc0d020`, `c981a773` |
| **375-02** Playwright `shuffle.spec.ts` | ✅ DONE — **5/5 skenario** ManagePackages hijau (render+save-PRG, lock, reminder, warning live-JS, hide) | `f5378eef`, `8673a174`, `f21ac40d` |
| **375-03** manual exam-diff + UAT doc | ◑ Tasks 1-2-4 DONE, Task 3 checkpoint **evidence captured, approval ditunda** | `ffc48e8b` |

## SC#2 evidence — SEMUA terbukti LIVE @5277 (3/3 + 5/5)

- **B1** ShuffleQuestions ON → urutan soal BEDA (Rino vs Iwan, 6 soal) ✓
- **B2** ShuffleOptions ON → urutan opsi BEDA ✓
- **B3** ShuffleQuestions OFF + 2 paket → Rino (worker0)=Paket A utuh urutan asli, Iwan (worker1)=Paket B utuh — round-robin paket beda per worker ✓
- **Grup A** 5 skenario ManagePackages = 5/5 PASS (Playwright)
- Detail order + justifikasi: `375-HUMAN-UAT.md` (status partial, 8/8 live, Gaps none)
- Screenshot LOKAL (gitignored *.png): `docs/uat-evidence/375-exam{,OFF}-{rino,iwan3}-*.png` (4 file)

## Kebersihan DB

- **DB lokal sudah di-RESTORE** ke baseline (snapshot `HcPortalDB_Dev_pre375uat_20260614T003317.bak`): matrix_sessions=0, pkg9999=0, total_sessions=58. **Tidak ada seed nempel.** `SEED_JOURNAL.md` entry 375 = cleaned.
- **App `dotnet run` @5277 SUDAH dimatikan** (PID 21480 killed).

## Lanjut besok

1. Tinjau bukti: buka 4 screenshot di `docs/uat-evidence/` + baca `375-HUMAN-UAT.md`.
2. Bila OK → bilang **"approved 375"**. Claude akan: tulis `375-03-SUMMARY.md`, `roadmap update-plan-progress 375-03 complete`, tandai phase 375 done.
3. Opsional pasca-phase: `/gsd-secure-phase 375` (test-only, threats minimal) + `/gsd-validate-phase 375` + `/gsd-verify-work 375`.
4. v27.0 = phase **372→373→374→375 SEMUA shipped lokal** → bisa milestone close (HATI-HATI: STATE.md pinned v25.0, v27.0 append-only — JANGAN complete-milestone vanilla, lihat memory `project_v27_shuffle_toggle`).

## Catatan

- STATE.md sengaja TIDAK disentuh (pin v25.0). begin-phase/planned-phase di-skip (pola 372-374).
- Bundle ITHandoff masih NOT PUSHED (v24-v27 lokal). Notify IT migration=false untuk 375 (tak ada migration).
- Commit session ini: `fcc0d020 c981a773 f5378eef 8673a174 f21ac40d ffc48e8b` (HEAD).
