# Phase 395: Mode jawaban (input asli + auto-generate) - Context

**Gathered:** 2026-06-18 (discuss `--power`) · **3 open Q di-resolve interaktif 2026-06-18** (semua locked = default; nol reversal)
**Status:** Ready for planning — semua keputusan produk LOCKED
**Verifikasi codebase:** workflow `wf_c2c181e3-87b` (4 agent adversarial — grading-math / service-autogen / 394-handoff-cert / conflict-critic). Semua klaim teknis di bawah CONFIRMED dengan `file:line`.

<domain>
## Phase Boundary

Isi **Langkah 5 (Jawaban per-pekerja)** wizard `/Admin/InjectAssessment` yang Phase 394 tinggalkan sebagai placeholder, + **mode auto-generate dari skor target**, + **preview skor final aktual pra-commit**, + **wire `#btnInject` → commit aktual** (panggil `InjectAssessmentService.InjectBatchAsync` — pertama kali di milestone; 394 berhenti sebelum commit). Cakupan REQ **INJ-08** (input jawaban asli per pekerja → skor via pipeline grading) + **INJ-09** (auto-generate pola dari skor target + skor final aktual ditampilkan, perhitungkan pembulatan).

**Scope-lock:** Sequential setelah 394 (file-overlap `InjectAssessmentController.cs` + `InjectAssessment.cshtml` + `InjectAssessmentService.cs` dengan 396/397 — bukan paralel). **0 migration** (DTO `InjectAnswerSpec` + service consumption loop sudah ada & teruji dari 393; auto-gen/mode/target/preview = lapisan baru di controller/view + kemungkinan 1 helper service). Import Excel = 396; link Pre/Post = 397.

**Fakta inti grading (CONFIRMED — basis semua keputusan auto-gen):** skor = `floor( Σ(ScoreValue soal benar) / Σ(ScoreValue semua soal) × 100 )` (`GradingService.cs:92-145`, `AssessmentScoreAggregator.cs:26-60`). Integer **truncation** (bukan round-half-up), `Score`=int, lulus = `skor >= PassPercentage` (operator `>=`). MC/MA **all-or-nothing** (MC: opsi terpilih `IsCorrect`; MA: `SetEquals` kunci persis). Essay = tambah `EssayScore` poin ke numerator (bukan ScoreValue penuh), essay **selalu** masuk denominator. Inject reuse mesin ini → **byte-identik online**.
</domain>

<decisions>
## Implementation Decisions

> Format: **D-0x (Q-0x = pilihan user)** keputusan. Sub-poin "→ Resolusi/Catatan" = turunan teknis hasil verifikasi (Claude-resolved; planner/user boleh override).

### Mode & cakupan jawaban
- **D-01 (Q-01 = c):** Pemilihan mode (input-asli vs auto-generate) = **DEFAULT per-room + OVERRIDE per-pekerja** (boleh campur dalam 1 room). → Kedua mode sama-sama menghasilkan `InjectWorkerSpec.Answers` yang service sudah konsumsi identik; mencampur input-asli & auto-gen dalam 1 batch **nol perubahan service** (service perlakukan tiap worker sama, `InjectAssessmentService.cs:189-218`). Resolusi mode = **controller-side**.
- **D-02 (Q-02 = c):** Skor target auto-gen = **batch default + override per-pekerja**. → Target adalah **input auto-gen saja**, tidak pernah masuk service (tak ada field `Mode`/`TargetScore` di DTO mana pun, `InjectAssessmentDtos.cs:27-62`). Controller pegang default + override, hitung answer-set, emit `Answers` eksplisit.

### Form input jawaban asli (INJ-08)
- **D-03 (Q-03 = a):** Tata letak = **satu pekerja per layar** (navigasi Prev/Next antar pekerja), BUKAN matrix/accordion. → Sub-komponen **self-contained** menggantikan `#step5Placeholder` (`InjectAssessment.cshtml:399-417`, seam comment :403 "tanpa menyentuh pills/nav"); **independen** dari `goToStep` luar (yang hanya toggle `.step-panel .d-none`, tak sentuh DOM dalam, :503-549). Daftar pekerja dari `#userCheckboxContainer .user-checkbox:checked`; soal dari client-state `injQuestions[]` (:498) — **nol round-trip server** untuk render.
- **D-04 (Q-04 = b):** Essay (mode input-asli) = **teks WAJIB + skor wajib**. → **Resolusi tegangan vs D-05:** "wajib" berlaku **per-essay-yang-DIISI** (engaged) di mode input-asli — essay yang **di-skip** = omit answer spec → ter-grade 0, tanpa teks. → **Butuh rule validasi BARU** (service `PreflightValidateAsync` saat ini cuma cek `EssayScore`, `TextAnswer` nullable & tak divalidasi, `InjectAssessmentService.cs:382-391` + `InjectAssessmentDtos.cs:32`). Rule **wajib di-scope** `mode==input-asli && essay engaged` — kalau global, ia memblokir essay auto-gen (yang sah ber-teks kosong per D-08). Rekomendasi: taruh di service (dipakai ulang Excel 396) dengan guard mode.

### Validasi kelengkapan (INJ-08)
- **D-05 (Q-05 = c):** **Warn-but-allow** — soal di-skip dihitung **salah/0**, HC konfirmasi (tidak diblok). → **Resolusi mekanis (KRITIS):** "skip" = **OMIT** `InjectAnswerSpec`-nya, **BUKAN** kirim spec kosong. Service grade baris-hilang sebagai 0 (`AssessmentScoreAggregator`); tapi kirim MC/MA kosong → **reject-all D-03** (`MC Count!=1` :398, `MA Count<1` :400). UI **wajib** terjemahkan skip-checkbox → "tak ada answer row dikirim". Service saat ini **tidak** mewajibkan semua soal terjawab → kompatibel dengan warn-but-allow.

### Auto-generate dari skor target (INJ-09)
- **D-06 (Q-06 = b):** Pembulatan = **bulat ke ATAS** — jamin capaian **≥ target**. → **Resolusi:** "bulat ke atas" = misnomer; sebenarnya = **pilih subset soal MC/MA jadi benar** sehingga `floor(Σpoin_benar/maxScore×100)` = nilai **terkecil yang ≥ target**. Diskret subset-sum. **Common case equal-weight** (`ScoreValue=10` default semua, `AssessmentPackage.cs:41`): `k = ceil(target×N/100)` benar → closed-form, selalu solvable. **Wajib** cek persentase **setelah** truncation int (boundary off-by-one).
- **D-07 (Q-07 = c):** Variasi pola = **acak per-pekerja dgn SEED TETAP** (reproducible). → **FLAG seed recipe (under-specified):** seed **TIDAK boleh** NIP-saja (worker sama → pola identik lintas room = terlihat sintetis, gugur "seakan online"). Seed = hash stabil **(NIP + identitas room [Title+Category+CompletedAt / sentinel pkg] + target)** + **urutan soal stabil** agar preview (D-09) == commit. → Konsekuensi: breakdown **Elemen Teknis** per worker akan bervariasi (natural; konfirmasi OK).
- **D-08 (Q-08 = c):** Essay dalam auto-gen = **EssayScore di-set 0 / HC isi manual**; auto-gen **hanya** sentuh MC/MA. → **Resolusi 3 interaksi (PENTING):**
  1. **Hybrid form:** room ber-essay → mode "auto-gen" **tetap** render input skor essay (+teks per D-04) → **tak pernah full-auto** bila ada essay. Mode toggle (D-01) tak sepenuhnya menentukan form.
  2. **Tak ada state "isi nanti":** finalize inject set `Status=Completed` **tanpa syarat** walau essay=0 (`InjectAssessmentService.cs:230-243`, beda dari `FinalizeEssayGrading` yang butuh semua essay ter-skor). → "HC isi manual" = ketik skor essay di **form yang sama sebelum commit** (essay efektif input-asli walau worker mode=auto), BUKAN deferred.
  3. **Ceiling target (interaksi keras D-06):** essay `ScoreValue` tetap di denominator tapi numerator 0 → **ceiling MC/MA-only** = `floor(Σ(MC+MA ScoreValue)/maxScore×100) < 100`, bisa `< target`. Maka D-06 "jamin ≥target" **mustahil** bila bobot essay besar. Auto-gen **WAJIB** hitung ceiling; bila `target > ceiling` → **warning blocking per-worker** ("target X% tak tercapai: bobot essay dikecualikan, maks Y%") + arahkan ke D-10. **JANGAN** diam-diam cap di ceiling (integritas sertifikasi).

### Preview & konfirmasi pra-commit (INJ-09)
- **D-09 (Q-09 = b):** Preview = **skor final + status Lulus/Tidak**, TANPA preview nomor sertifikat. → **Resolusi engine:** pakai **`AssessmentScoreAggregator.Compute`** (pure, EF-free) atas answer-pattern yang diusulkan — formula identik → **preview == commit**. **Belum ada** endpoint preview (394 POST tak grade; `GradingService.PreviewScoreAsync` ada tapi atas session ter-persist, bukan `InjectRequest` pra-persist → **butuh jalur dry-run baru**). **JANGAN** panggil `CertNumberHelper` di preview: `GetNextSeqAsync` read-only (Max+1, no reserve, `CertNumberHelper.cs:23-35`) tapi nomor **tak ter-reserve** sampai commit (unique-index 3-retry, `InjectAssessmentService.cs:258-283`) → nomor pra-commit advisory & bisa geser → menyesatkan. Trigger preview = **tombol "Pratinjau" on-demand** (bukan per-keystroke).
- **D-10 (Q-10 = c):** Bila auto-gen tak capai target persis = **tawarkan beralih ke input-asli** untuk pekerja itu (HC pilih soal benar manual). → **Resolusi:** dengan D-06 (bulat ke atas), capaian **selalu ≥ target** → trigger sebenarnya = **OVERSHOOT** (capaian > target krn diskret, contoh "target 80 → aktual 83 (+3)"; contoh "−2" di teks soal **stale**) **atau** target-unreachable (ceiling essay D-08). → **UNIFIKASI dengan D-01:** "switch to manual" = set mode worker itu = input-asli (mekanisme override per-pekerja yang **sama**, bukan jalur "sesuaikan manual" terpisah). **Retensi jawaban saat switch:** **PRE-FILL** answer-grid dari pola auto-gen terakhir (HC tweak beberapa soal) — rekomendasi.

### Claude's Discretion (teknis — diserahkan ke researcher/planner)
- **Lokasi lapisan auto-gen:** rekomendasi **server-side** `InjectAssessmentService.BuildAutoGenAnswers(questions, targetPercent, seed) → List<InjectAnswerSpec>` agar **Excel 396 reuse** (hindari duplikasi). Alternatif client-side (lebih cepat, tapi duplikat ke 396).
- **Resep seed eksplisit** (D-07) — komposisi & hashing.
- **Konstruksi MA "salah"** untuk option-set degenerate: (a) semua opsi `IsCorrect` → MC tak bisa dibuat salah; MA hanya bisa salah via proper-subset bila >1 opsi; (b) soal 1-opsi → terpaksa benar. Planner **wajib** tetapkan rule deterministik + pre-scan distribusi `IsCorrect` per soal + flag soal 1-opsi.
- **Model state per-worker tunggal** `{mode, targetScore, answers}` + **roster ringkas** (auto vs manual) agar HC tak hilang jejak di navigasi 1-pekerja-per-layar (D-03).
- **Serialisasi:** tambah hidden `#AnswersJson` paralel pola `#QuestionsJson` (`InjectAssessment.cshtml:868-875`) + `ParseAnswerVms` paralel `ParseQuestionVms` (controller:126-139) → isi `InjectWorkerSpec.Answers` di `MapToRequest` (controller:116, kini hardcoded `Answers = new()`).
- Lokasi rule TextAnswer-wajib (D-04): service (reuse 396) vs controller — rekomendasi service ber-guard mode.

### Folded Todos / Carry-in
- **Carry dari Phase 394 UAT (cosmetic, 394-UAT.md @2bcd4dd7):** label tipe soal tak konsisten — `injTypeLabel()` (`Views/Admin/InjectAssessment.cshtml:735-738`) + pesan validasi (`:832-833`) render istilah lama "Pilihan Ganda"/"Pilihan Majemuk", padahal dropdown `#QuestionType` sudah standar LBL-02 "Single Answer"/"Multiple Answer". **Fix saat 395** (file yang sama disentuh): ganti 2 return value `injTypeLabel` + 2 string validasi ke istilah baru. ~4 baris, non-blocking. Planner: masukkan sebagai task kecil di plan 395.
</decisions>

<open_questions>
## Keputusan Produk — RESOLVED (dikunci user 2026-06-18, sesi discuss interaktif)

Ketiga open question ditutup; semua = konfirmasi default yang sudah tercatat (nol reversal). Sekarang **LOCKED**, bukan lagi "default diambil".

1. **Auto-gen pada room BER-ESSAY → LOCKED = HYBRID.** Worker mode=auto-gen di room ber-essay: form **tetap wajib** HC ketik skor essay (+teks) manual sebelum commit; auto-gen **hanya** sentuh MC/MA. "Full-auto" tak berlaku bila ada essay. (Memperkuat D-08.1; full-auto essay → [[deferred]].)
2. **`target > ceiling MC/MA-only` (essay berat) → LOCKED = BLOCKING + switch input-asli.** Warning **blocking per-worker** ("target X% tak tercapai: bobot essay dikecualikan, maks Y%"), arahkan HC switch ke input-asli + naikkan skor essay manual. **JANGAN** diam-diam cap di ceiling (integritas sertifikasi). (Memperkuat D-08.3 + D-10.)
3. **Resep seed D-07 → LOCKED = NIP + identitas room + target.** Prinsip "pola tak berulang lintas room" disetujui — seed = hash stabil (NIP + [Title+Category+CompletedAt] + target) + urutan soal stabil. NIP-saja **ditolak** (sintetis). (Memperkuat D-07.)

**Status: semua keputusan produk terkunci. Tidak ada flag tersisa — siap plan.**
</open_questions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — §7.2 auto-generate (subset selection, "MA set tak-cocok", essay menyumbang), §13 "ambil terdekat & laporkan" (di-override D-06 jadi "≥target"), §10 audit/atomic.
- `.planning/REQUIREMENTS.md` — INJ-08 (input jawaban asli per soal → skor via pipeline grading), INJ-09 (auto-generate pola dari skor target + skor final aktual ditampilkan, pembulatan).
- `.planning/ROADMAP.md` — Phase 395 details + 5 Success Criteria + UI hint:yes; dependency sequential-setelah-394.
- `.planning/phases/393-backend-core-inject/393-CONTEXT.md` — kontrak `InjectAssessmentService` + D-03 reject-all, D-05 essay wajib EssayScore, D-07 range 0..ScoreValue, D-12 cert tahun-backdate.
- `.planning/phases/394-page-setup-room-authoring-soal/394-CONTEXT.md` — D-01 wizard nav-pills, D-02 6-langkah (step-5 = sisip 395), D-07 0-DB-write-di-394 (commit baru 395).

### Kode terverifikasi (workflow wf_c2c181e3-87b — semua CONFIRMED)
- `Services/GradingService.cs:92-145` — formula grade: `floor(totalScore/maxScore×100)`, MC :105-113, MA SetEquals :116-128, Essay=0-di-pass-awal :130-134, lulus `>=` :145. `:578-585` `PreviewScoreAsync` (atas session ter-persist — TIDAK langsung dipakai inject preview).
- `Helpers/AssessmentScoreAggregator.cs:26-60` — **engine preview D-09** (pure, EF-free): `Compute`, essay `+EssayScore` :52-55, maxScore selalu termasuk essay :35, floor :58, lulus `>=` :59, `IsQuestionCorrect` :73.
- `Services/InjectAssessmentService.cs:42-334` — `InjectBatchAsync`; reject-all :48-68; write answers :189-218; **validasi :382-401** (MC==1, MA>=1, essay EssayScore 0..ScoreValue, no TextAnswer check); cert reserve :258-283; essay-finalize set Completed tanpa syarat :230-243.
- `Models/InjectAssessmentDtos.cs:27-62` — `InjectAnswerSpec{QuestionTempId, SelectedOptionTempIds, TextAnswer?(nullable), EssayScore?}`, `InjectWorkerSpec.Answers`. **TAK ADA** field Mode/Target → auto-gen/mode/target = lapisan baru.
- `Controllers/InjectAssessmentController.cs:52-66` (POST tak commit di 394), `:71-123` `MapToRequest` (**injection point `Answers = new()` :116**), `:126-139` `ParseQuestionVms`.
- `Views/Admin/InjectAssessment.cshtml:399-417` (step-5 seam `#step5Placeholder` :404, btnPrev5/Next5 :408/411), `:479` `#btnInject`, `:503-549` `goToStep`/pills, `:868-875` serialisasi `#QuestionsJson`, `:498` `injQuestions[]`, `:577/605/651` baca pekerja terpilih.
- `Helpers/CertNumberHelper.cs:23-35` — `GetNextSeqAsync` read-only (no reserve) → D-09 jangan preview nomor cert.

### Pola test
- `InjectAssessmentServiceTests.cs:539` — essay EssayScore>ScoreValue ditolak (acuan validasi). Phase 354 lesson: Razor+JS runtime → **Playwright wajib** (grep+build tak cukup untuk wizard/sub-nav/serialize).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **DTO + service consumption** (`InjectAnswerSpec` + `InjectAssessmentService.cs:189-218`) — sudah ada & teruji; **happy-path nol perubahan backend** (auto-gen cukup emit `SelectedOptionTempIds`/`EssayScore`).
- **`AssessmentScoreAggregator.Compute`** — engine preview pra-commit (D-09), identik formula commit.
- **Wizard seam 394** — `#step5Placeholder` (sisip sub-komponen 1-pekerja-per-layar), `#AnswersJson` paralel `#QuestionsJson`, `MapToRequest` :116 (`Answers = new()` → isi).
- **`#btnInject` + POST** — wire ke `_injectService.InjectBatchAsync` (commit pertama milestone).

### Established Patterns
- Client-state serialize: hidden JSON field + `JSON.stringify` on submit + `Parse*Vms` server. Ikuti untuk answers.
- Sub-nav 1-pekerja-per-layar = IIFE/closure ber-state sendiri (currentWorkerIdx + Prev/Next + render), independen `goToStep` luar.
- Grading reuse, **nol duplikasi**: jangan hitung skor di JS untuk commit — biarkan service grade; JS preview pakai semantik sama dgn Aggregator (atau panggil endpoint preview baru).

### Integration Points
- `InjectAssessment.cshtml` step-5 + step-6 confirm → `#btnInject` POST → controller `MapToRequest` → `InjectBatchAsync`.
- **File-overlap SEQUENTIAL:** 395 extend controller/view/service yang sama dgn 396 (Excel) & 397 (Pre/Post). 395 lebih dulu; auto-gen helper sebaiknya server-side agar 396 reuse.

### Risiko teknis utama (dari verifikasi — wajib ditangani plan)
- **Ceiling essay** (D-08.3): `target > MC/MA-ceiling` saat essay berat → auto-gen mustahil → warning + switch (D-10). **Risiko integritas** bila diam-diam cap.
- **Skip = omit, bukan empty** (D-05): kirim MC/MA kosong = reject-all batch.
- **Serialize answers wajib di submit-listener yang sama** (`:871-874`) — lupa = POST answers kosong → semua worker grade 0 (silent). Playwright runtime-check wajib.
- **TempId dangling** (D-04/D-05): hapus/edit soal pasca-isi-jawaban → answer TempId basi; service skip unmatched (`:191 continue`) silent → re-validate/prune answers saat soal berubah.
- **Truncation boundary** (D-06): cek `floor()` setelah pilih subset, jangan asumsi `ceil(target×N/100)` cukup di mixed-weight.
- **Seed lintas-room** (D-07): NIP-saja → pola berulang = sintetis.
- **MA degenerate** (Claude-discretion): all-correct / 1-opsi → rule "salah" tak terdefinisi.
</code_context>

<specifics>
## Specific Ideas

- **Jawaban user ≠ rekomendasi Claude di 6 dari 10** (Q-03 a, Q-04 b, Q-05 c, Q-06 b, Q-08 c, Q-10 c). Benang merah pilihan user: **HC pegang kontrol manual penuh; otomasi asistif bukan otoritatif; essay selalu dinilai manusia; bias jamin-lulus (bulat ke atas); jangan blok—cuma warn.** Internal-konsisten setelah resolusi tegangan.
- Auto-gen = **lapisan translasi** (target → subset soal benar → `SelectedOptionTempIds`/`EssayScore` eksplisit) sebelum `InjectBatchAsync`. Service tetap "dumb executor" — byte-identik online terjaga.
- Preview = "what-you-see-is-what-commits": pakai Aggregator yang sama → angka preview = angka tersimpan.
</specifics>

<deferred>
## Deferred Ideas

- **Full-auto essay** (skor essay auto-proporsional) — ditolak user (Q-08=c manual). Bila kelak ingin room ber-essay benar-benar full-auto → milestone berikut.
- **Preview nomor sertifikat pra-commit** — ditolak (D-09=b); nomor dihitung pasca-commit (tak ter-reserve sampai unique-index write).
- **Matrix / accordion layout** input-asli — ditolak demi 1-pekerja-per-layar (D-03=a).
- **Blok commit sampai target persis** (Q-10=b) — ditolak; izinkan + tawarkan switch manual (D-10=c).
- **Import Excel** = Phase 396 (reuse `BuildAutoGenAnswers` server-side bila dibuat). **Link Pre/Post** = Phase 397.
</deferred>

---

*Phase: 395-mode-jawaban-input-asli-auto-generate*
*Context gathered: 2026-06-18 (discuss --power) · verifikasi codebase: workflow wf_c2c181e3-87b (4 agent, semua CONFIRMED)*
