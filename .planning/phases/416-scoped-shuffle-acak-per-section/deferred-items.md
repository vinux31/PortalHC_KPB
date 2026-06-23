# Phase 416 — Deferred Items

Temuan di luar scope tugas saat ini (Plan 03). Tidak di-fix di sini; diangkat ke owner Plan 02 / verifier / backlog.

## DEF-416-01 (Plan 02 logic) — Predikat ET-coverage warning `DistinctEt > K` tak terjangkau data nyata

- **Ditemukan saat:** Plan 03 (otoring skenario S3 e2e ET-coverage warning).
- **Lokasi:** `Controllers/AssessmentAdminController.cs:7673-7680` (`ViewBag.SectionEtWarnings = ... .Where(w => w.DistinctEt > w.K)`).
- **Isi temuan:** Predikat warning `DistinctEt > K` di mana `K = COUNT(soal SectionId=s.Id)` dan
  `DistinctEt = distinct ElemenTeknis non-kosong dari soal Section yang sama`. Karena tiap `PackageQuestion`
  menyimpan SATU string `ElemenTeknis`, jumlah ET distinct di sebuah Section SELALU ≤ jumlah soalnya (K).
  Maka `DistinctEt > K` **tidak pernah** bisa true dengan data normal → alert peringatan cakupan ET
  (D-416-03) **tak pernah dirender**. Fitur peringatan efektif dead-code.
- **Dampak:** NON-BLOCKING by design (warning = sinyal, bukan error). Tidak ada kerusakan/keamanan —
  hanya nicety yang tak pernah tampil. Inti D-416-03 yang load-bearing ("warning != error; Section sempit
  tetap boleh") TETAP benar & terbukti runtime (S3 + S3b).
- **Kenapa tidak di-fix di Plan 03:** (a) di luar scope (logika Plan 02, bukan disebabkan otoring spec);
  (b) memperbaiki predikat butuh redefinisi semantik (mis. bandingkan distinct ET terhadap kuota presentasi
  K = min antar paket-saudara, atau terhadap pool ET lintas-paket) — ini keputusan desain (Rule 4), bukan
  bug-fix sepele; (c) autopilot §5 tanpa human untuk keputusan desain.
- **Saran fix (untuk owner Plan 02 / re-spec):** definisikan ulang sumber `DistinctEt` agar terjangkau —
  mis. distinct ET pada pool soal Section LINTAS paket-saudara dibandingkan `K = min(count Section antar
  paket-saudara)` (kuota yang benar-benar dipresentasikan). Saat itu warning bisa fire bermakna.
- **Coverage e2e saat ini:** S3 membuktikan NON-BLOCKING (load-bearing); S3b membuktikan tak ada
  false-positive (cakupan penuh → tak ada alert). Sisi-positif render alert = di-defer (predikat unreachable).
- **DISPOSISI (keputusan user 2026-06-23):** **→ FIX DI PHASE 419** (carry-over). Phase 419 (Export Label
  Section + Polish + Test/UAT Milestone) memang audit ulang sync lintas-paket — perbaikan predikat ET-warning
  (pool ET lintas-sibling + group by `SectionNumber`, plus test positif) masuk scope 419. 416 ditutup sekarang
  dengan alert dead tapi non-blocking. Saat `/gsd-discuss-phase 419` / `/gsd-plan-phase 419`: angkat DEF-416-01
  sebagai REQ tambahan (fix ET-warning predicate) + IN-01 (selaraskan grouping SectionId→SectionNumber).
