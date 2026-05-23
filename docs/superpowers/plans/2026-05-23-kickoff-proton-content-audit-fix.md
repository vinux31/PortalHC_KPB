# Kickoff-PROTON Content Audit Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development atau superpowers:executing-plans. Steps pakai checkbox.

**Goal:** Fix issue konten yang ketemu di audit 21 slide `docs/Kickoff-PROTON.html`. 4 fix konten + 3 verifikasi visual.

**Architecture:** Edit in-place file HTML existing. No structural change (slide count tetap 21, data-slide numbering tetap, JS TOTAL tetap). Atomic commit per fix. Manual UAT Playwright per fix.

**Tech Stack:** Static HTML/CSS. No build. Playwright untuk visual verify.

**Source audit:** session brainstorm 2026-05-23 setelah ship v3.0 (commit `05ad9cc5`).

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `docs/Kickoff-PROTON.html` | Modify | 4 lokasi edit (slot 6 descriptor, slot 7 URL, slot 8 URL, slot 20 Coach row) |

---

## Issue inventory

| Slot | Severity | Issue | Status |
|---|---|---|---|
| 7 | 🔴 P1 | URL bar mockup `localhost:5277/...` — bocorin dev machine, salah untuk presentasi | Fix Task 1 |
| 8 | 🔴 P1 | URL bar mockup `localhost:5277/...` — sama | Fix Task 2 |
| 6 | 🟠 P2 | CDP descriptor "Rumah Coaching Proton" konflik slot 8 menu "Proton Coaching" + `Views/CDP/Index.cshtml:46` | Fix Task 3 |
| 20 | 🟠 P2 | Coach row mention "Coach Mapping" — itu admin menu, Coach tidak akses | Fix Task 4 |
| 4 | 🟡 P3 | innerText concat "TransformasiDigital" — kemungkinan `<br/>` tampil OK | Verify Task 5 |
| 12 | 🟡 P3 | innerText concat "Kompetensi 1Safe Work" — sama | Verify Task 5 |
| 18 | 🟡 P3 | innerText cut "Perencanaan SDM coba-co..." (truncation 800 char) — tampak terpotong | Verify Task 5 |
| 13 | ⚪ P4 | Bagian list RFCC/GAST/NGP/DHT/HMU — verified di SeedData.cs ✅ | No fix |
| 15 | ⚪ P4 | "5 Dimensi × 2 track = 10 dok" — mathematically consistent dengan slot 14 ✅ | No fix |

---

## Task 1: Fix Slot 7 — CMP URL mockup

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — find `localhost:5277/KPB-PortalHC/CMP/Index` di slot 7 mockup browser bar

- [ ] **Step 1: Locate string**

Run:
```bash
grep -n "localhost:5277/KPB-PortalHC/CMP" docs/Kickoff-PROTON.html
```
Expected: 1 line number.

- [ ] **Step 2: Edit string**

Use Edit tool:
```
Find:
localhost:5277/KPB-PortalHC/CMP/Index
Replace:
10.55.3.3/KPB-PortalHC/CMP/Index
```

- [ ] **Step 3: Verify**

Run:
```bash
grep -n "10.55.3.3/KPB-PortalHC/CMP/Index" docs/Kickoff-PROTON.html
grep -c "localhost:5277/KPB-PortalHC/CMP" docs/Kickoff-PROTON.html
```
Expected: line found; count = 0.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 7 URL mockup localhost -> 10.55.3.3 (Dev)

Audience presentasi melihat URL Dev kanonik, bukan dev machine local."
```

---

## Task 2: Fix Slot 8 — CDP URL mockup

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — find `localhost:5277/KPB-PortalHC/CDP/Index` di slot 8 mockup browser bar

- [ ] **Step 1: Locate + edit + verify** (sama pola Task 1)

Find:
```
localhost:5277/KPB-PortalHC/CDP/Index
```
Replace:
```
10.55.3.3/KPB-PortalHC/CDP/Index
```

Verify:
```bash
grep -c "localhost:5277/KPB-PortalHC/CDP" docs/Kickoff-PROTON.html
```
Expected: 0.

- [ ] **Step 2: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 8 URL mockup localhost -> 10.55.3.3 (Dev)"
```

---

## Task 3: Fix Slot 6 — CDP descriptor selaras

**Context:** Slot 6 (3 Pilar) sub-text untuk CDP card sekarang berbunyi "Rumah Coaching Proton". Slot 8 (CDP detail) subtitle berbunyi "rumah Proton Coaching". Menu di `Views/CDP/Index.cshtml:46` = "Proton Coaching". Audit v2.0 decision #4 sudah explicit: menu refs selaras dengan kode.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot 6 (data-slide="6"), CDP card sub-text

- [ ] **Step 1: Locate**

Run:
```bash
grep -nE 'Rumah Coaching Proton|Rumah Assessment Proton' docs/Kickoff-PROTON.html
```
Expected: 2 line (CMP + CDP card di slot 6).

- [ ] **Step 2: Edit slot 6 CDP descriptor**

Find:
```
Rumah Coaching Proton
```
Replace:
```
Rumah Proton Coaching
```

Note: jangan ubah "Rumah Assessment Proton" — CMP menu name (Index.cshtml admin) tetap "Assessment Proton" sesuai memory. Verify after edit:
```bash
grep -c 'Rumah Coaching Proton' docs/Kickoff-PROTON.html
grep -c 'Rumah Proton Coaching' docs/Kickoff-PROTON.html
grep -c 'Rumah Assessment Proton' docs/Kickoff-PROTON.html
```
Expected: 0, 1, 1.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 6 CDP descriptor 'Coaching Proton' -> 'Proton Coaching'

Selaras Views/CDP/Index.cshtml:46 menu card name + slot 8 subtitle.
Konflik internal-deck resolved per v2.0 decision #4."
```

---

## Task 4: Fix Slot 20 — Coach row drop "Coach Mapping"

**Context:** Slot 20 tabel Akses Portal, baris Coach saat ini: "CDP → Proton Coaching (coachee dampingan) + Coach Mapping". "Coach Mapping" route nyata = `Views/Admin/CoachCoacheeMapping.cshtml`, role Admin/HC only. Coach tidak akses menu ini.

**Pengganti yang valid (Coach memang lihat di CDP):**
- Proton History — riwayat session coaching yang sudah dilakukan ✅
- Proton Dashboard — pantau progres coachee dampingan (read-only) ✅

Pilihan kanonik: drop "Coach Mapping" + tambah "Proton History" (sesuai konsep "coachee dampingan" mengalir ke riwayat sesi).

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot 20, baris Coach

- [ ] **Step 1: Locate**

Run:
```bash
grep -nE 'Proton Coaching \(coachee dampingan\)' docs/Kickoff-PROTON.html
```
Expected: 1 line.

- [ ] **Step 2: Edit slot 20 Coach row**

Find (exact match inside table cell):
```
CDP → Proton Coaching (coachee dampingan) + Coach Mapping
```
Replace:
```
CDP → Proton Coaching (coachee dampingan) + Proton History
```

(Sesuaikan dengan markup HTML actual saat edit — kemungkinan ada `<strong>`, `&middot;`, `&rarr;` entitas)

- [ ] **Step 3: Verify**

Run:
```bash
grep -c 'Coach Mapping' docs/Kickoff-PROTON.html
```
Expected: 0 (kalau cuma muncul di slot 20). Kalau >0, cek mana lagi muncul.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 20 Coach row drop 'Coach Mapping' (admin-only menu)

Coach Mapping = Views/Admin/CoachCoacheeMapping.cshtml (admin/HC only).
Coach role akses Proton Coaching + Proton History di CDP.
Sumber: Views/CDP/Index.cshtml + cek Views/Admin/CoachCoacheeMapping.cshtml."
```

---

## Task 5: Visual verify P3 — Slot 4, 12, 18

**Files:** None modified (verification only).

**Context:** Audit innerText extract menemukan concat artifact untuk slot 4 ("TransformasiDigital"), slot 12 ("Kompetensi 1Safe Work Practice"), dan truncation slot 18 ("Perencanaan SDM coba-co..."). Markup punya `<br/>` sehingga visual kemungkinan OK.

- [ ] **Step 1: Start local server (kalau belum running)**

```bash
python -m http.server 8765 --bind 127.0.0.1 --directory .
```
(run_in_background=true)

Verify:
```bash
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:8765/docs/Kickoff-PROTON.html
```
Expected: 200.

- [ ] **Step 2: Navigate + screenshot slot 4**

Via Playwright:
- Navigate `http://127.0.0.1:8765/docs/Kickoff-PROTON.html`
- Resize 1366×800
- JS: activate slot 4
- Screenshot `kickoff-audit-slot-04.png`

Expected visual:
- Arrow connector "Transformasi" (line break) "Digital" — 2-line label visible
- 2 column SEBELUMNYA / PORTAL HC KPB visible
- Footer cite (jika ada) tidak ke-clip

- [ ] **Step 3: Navigate + screenshot slot 12**

JS: activate slot 12
Screenshot `kickoff-audit-slot-12.png`

Expected visual:
- 3 col TAHUN 1/2/3
- Kompetensi label di newline dengan sub-deskripsi
- Tidak ada concat visible

- [ ] **Step 4: Navigate + screenshot slot 18**

JS: activate slot 18
Screenshot `kickoff-audit-slot-18.png`

Expected visual:
- 4 row role × 4 col (atau 2×2 grid) per memory v2.0 decision #9
- Baris Section Head lengkap (kalimat "Perencanaan SDM ..." complete sampai dengan period)
- Metric badge per role visible
- Tidak ada overflow

- [ ] **Step 5: Decision per slot**

Per screenshot:
- Kalau VISUAL OK → mark slot OK, no fix.
- Kalau ADA ISSUE (text terpotong/concat tampil) → tambah Task 6+ ke plan ini secara inline.

- [ ] **Step 6: Close browser + kill server**

```bash
# Playwright close
# Bash kill server bg
```

- [ ] **Step 7: Document hasil verifikasi (no commit kalau no fix)**

Kalau no fix needed: tambah note di commit message Task 7 final summary.
Kalau ada fix: implement + commit per slot.

---

## Task 6: (Reserved) — Slot 4/12/18 fix kalau ditemukan

**Dispatch kalau Task 5 reveal real visual issue.** Jika tidak, skip task ini.

Detail spesifik per slot:

**Kalau Slot 4 "Transformasi Digital" arrow label hancur** → Edit `<br/>` jadi space + adjust line-height. Pattern: cari `Transformasi<br/>Digital` di file, verify markup OK.

**Kalau Slot 12 Kompetensi label hancur** → Cek apakah `<strong>Kompetensi N</strong><br/><span>nama panjang</span>` rendering OK. Mungkin perlu `display: block` di `<strong>`.

**Kalau Slot 18 Section Head row terpotong** → Cek max-height container atau text overflow di card grid. Mungkin reduce padding atau split text.

(Tidak ada code block detail sini sampai issue confirmed di Task 5.)

---

## Task 7: Final verification + handoff

**Files:** None modified.

- [ ] **Step 1: Run grep audit untuk yakinkan semua fix terkena**

```bash
grep -c "localhost:5277/KPB-PortalHC" docs/Kickoff-PROTON.html       # expect 0
grep -c "Rumah Coaching Proton" docs/Kickoff-PROTON.html              # expect 0
grep -c "Rumah Proton Coaching" docs/Kickoff-PROTON.html              # expect 1
grep -c "Coach Mapping" docs/Kickoff-PROTON.html                      # expect 0
grep -c "Proton History" docs/Kickoff-PROTON.html                     # expect >= 1 (existing + slot 20 add)
```

- [ ] **Step 2: Confirm structural integrity unchanged**

```bash
grep -cE 'class="slide[^"]*" data-slide=' docs/Kickoff-PROTON.html
```
Expected: 21.

```bash
grep -oE 'data-slide="[0-9]+"' docs/Kickoff-PROTON.html | sed 's/data-slide="\([0-9]*\)"/\1/' | sort -n | uniq -c | wc -l
```
Expected: 21.

- [ ] **Step 3: Re-run Playwright sanity untuk 4 slot yang di-edit (6, 7, 8, 20)**

Screenshot ulang slot 6/7/8/20 untuk konfirmasi visual:
- Slot 6: card CDP descriptor sekarang "Rumah Proton Coaching"
- Slot 7: browser bar mockup `10.55.3.3/KPB-PortalHC/CMP/Index`
- Slot 8: browser bar mockup `10.55.3.3/KPB-PortalHC/CDP/Index`
- Slot 20: Coach row sekarang "Proton Coaching + Proton History"

- [ ] **Step 4: Git log review**

```bash
git log --oneline -10
```
Expected: lihat commit Task 1..4 (+ Task 6 jika ada).

- [ ] **Step 5: Get final HEAD hash**

```bash
git rev-parse HEAD
```

- [ ] **Step 6: Report ke user**

Summary text 5 baris:
- Fixed 4 issue (Slot 6, 7, 8, 20)
- Verified 3 slot (Slot 4, 12, 18) — [OK / fixed]
- Total commit baru: N
- HEAD: `<hash>`
- Pending: push origin/main + notif IT

---

## Self-Review Notes

**Spec coverage check (audit issues):**
- ✅ Slot 7 URL fix (Task 1)
- ✅ Slot 8 URL fix (Task 2)
- ✅ Slot 6 CDP descriptor (Task 3)
- ✅ Slot 20 Coach row (Task 4)
- ✅ Slot 4/12/18 visual verify (Task 5) → conditional fix Task 6
- ✅ Final integrity check (Task 7)

**Placeholder scan:**
- ✅ All grep commands exact strings
- ✅ Edit operations have explicit find/replace
- ⚠️ Task 4 Step 2 says "Sesuaikan dengan markup HTML actual saat edit" — defer karena entitas HTML belum diverifikasi. Implementer akan find markup actual.
- ⚠️ Task 6 spec deferred conditional on Task 5 outcome — acceptable

**Type consistency:**
- ✅ "Coach Mapping" (drop), "Proton Coaching" (selaras kode), "Proton History" (selaras kode)

**Known gap:**
- Task 4 replace text actual akan tergantung markup HTML (pipe character, entitas, dll). Implementer cek bentuk asli sebelum edit.
- Slot 4/12/18 fix specs not yet defined — defer to Task 5 result.

---

## Out of Scope

- Refactor slide existing yang tidak terkena fix
- Push origin/main (user explicit consent)
- Tag baru
- Re-edit 4 corporate slide content (Img 6/7/8/9) — sudah verified post-v3.0
