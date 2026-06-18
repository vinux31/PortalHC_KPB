# Phase 397: Link Pre/Post ke room existing - Context

**Gathered:** 2026-06-18 (discuss interaktif, advisor-off)
**Status:** Ready for planning — 12 keputusan LOCKED (4 area inti + 4 gray-area tambahan)

<domain>
## Phase Boundary

Saat HC membuat room inject ber-tipe **Pre/Post** di wizard `/Admin/InjectAssessment`, ia dapat **mencari & memilih assessment room existing** (search picker, reuse query `ManageAssessmentTab_Assessment`) lalu **menautkan** sesi inject ke room itu — mewujudkan grouping Pre/Post **"seakan online"** dengan dukungan skenario **silang inject↔online** (Pre di-inject ↔ Post online asli, atau sebaliknya) maupun **inject↔inject** (kedua sisi di-inject). Mekanik penautan = wiring `LinkedGroupId` (level-room) + `LinkedSessionId` (sibling per-pekerja) **reuse logika PrePost existing — nol grouping baru**. Cakupan REQ **INJ-12**.

**Scope-lock:** **Sequential SETELAH 395 & 396 ter-commit** (file-overlap `InjectAssessmentController.cs` + `InjectAssessment.cshtml` + `InjectAssessmentService.cs`; bukan paralel). **0 migration** (kolom `LinkedGroupId`/`LinkedSessionId`/`AssessmentType` sudah ada di `AssessmentSession`; DTO `InjectRequest` + `InjectAssessmentService` sudah menerima & mem-persist keduanya). RBAC `Admin,HC`. Atomic per-batch + AuditLog (warisi 393). Penautan **opsional** saat Pre/Post (D-04). **Scope addition sesi ini:** fitur **unlink/ubah tautan pasca-commit** dimasukkan (D-12, keputusan user — bukan defer).

**Temuan teknis kunci (mempengaruhi keputusan):** Records/Results/gain-score memasangkan Pre↔Post **by `LinkedGroupId` + `UserId`** — *bukan* `LinkedSessionId` (`CMPController.cs:250-344` & `:3412-3502`). Jadi yang **wajib benar** = `LinkedGroupId`; `LinkedSessionId` = integritas/fidelitas (online set bidirectional). Saat ini service **broadcast 1 nilai** `LinkedSessionId` ke semua sesi (`InjectAssessmentService.cs:120`) → **harus diubah jadi per-pekerja** untuk D-02.

</domain>

<decisions>
## Implementation Decisions

> Format: **D-0x** = keputusan user terkunci. "→ Catatan" = turunan teknis (Claude-resolved; planner/researcher boleh detailkan, jangan ubah keputusan).

### Semantik group-link / LinkedGroupId (Area 1)
- **D-01:** **Adopt jika sudah ber-grup; tulis stiker grup ke ONLINE jika standalone.**
  - **Kasus A** (room target sudah ber-`LinkedGroupId`): sesi inject **ADOPT** `LinkedGroupId` target — **tak menyentuh** data online.
  - **Kasus B** (room target **standalone**, `LinkedGroupId=null`): **TULIS `LinkedGroupId` baru ke sesi online existing JUGA** + ke sesi inject, dalam **transaksi atomic yang sama** + AuditLog (D-09). Hanya **nomor grup** yang ditulis — **skor/jawaban/status online TIDAK diubah**. Rollback total bila error.
  - Justifikasi: display memasangkan by `LinkedGroupId`; bila sisi online tetap null, pasangan **tak akan tampil**. Menulis stiker = satu-satunya cara Kasus B tampil berpasangan, tetap "seakan online".
  - → **Catatan:** Nilai `LinkedGroupId` baru (Kasus B) = ikut konvensi online (`AssessmentAdminController.cs:1270` `linkedGroupId = preSessions[0].Id`). Rekomendasi: pakai **`RepresentativeId` room target** (sesi nyata, level-room) lalu tulis ke **SEMUA sesi room target** (konsistensi level-room) + sesi inject. Detail = discretion.

### Pairing per-pekerja / LinkedSessionId (Area 2)
- **D-02:** **Bidirectional per-pekerja.** Per pekerja: `sesiInject.LinkedSessionId` → sibling existing, **DAN** `siblingExisting.LinkedSessionId` → sesi inject (dua arah, mirror `CreateAssessment` `:1307-1314`). → **Butuh ubah service**: resolve sibling **by-`UserId`** per sesi (bukan broadcast 1 nilai `:120`) + **write balik** ke sesi existing (online maupun inject). "Seakan online" penuh.
- **D-03:** **Pekerja inject tanpa pasangan di room target → izinkan unpaired + warn.** Set `LinkedGroupId` (gabung grup), `LinkedSessionId=null` → tampil sebagai **sisi tunggal** (mis. Pre-only). **Tandai jelas di preview** ("N pekerja tanpa pasangan"). Pola warn-but-allow (carry 395/396). Bukan blok.

### UX picker & cakupan (Area 3)
- **D-04:** **Penautan OPSIONAL** saat tipe Pre/Post. Picker tersedia tapi **boleh di-skip** → HC bisa inject Pre/Post **standalone** (tautkan nanti, atau inject pasangannya belakangan lalu link ke room inject ini). Mendukung "inject kedua sisi" & "link belakangan".
- **D-05:** **Picker = MODAL pop-up.** Tombol "Cari Room" di Step-1 → modal pencarian → pilih room → modal tutup, room terpilih tampil sebagai **chip** di Step-1. (Bukan panel inline.)
- **D-06:** **Filter = tipe LAWAN saja + search.** Inject Pre → hanya tampilkan room **PostTest**; inject Post → hanya room **PreTest** (grouped **maupun** standalone). Reuse query `ManageAssessmentTab_Assessment`, search by **Judul/Kategori/jadwal**. Baris room tampil: judul + kategori + tanggal + **badge tipe** + jumlah peserta + **indikator sudah-ber-grup** (Kasus A vs B).
- **D-10:** **Room target boleh = room hasil INJECT** (bukan hanya online asli). Picker tampilkan room **inject MAUPUN online**. Mendukung "inject kedua sisi" (inject Post dulu, lalu inject Pre tautkan ke room inject itu). Link **inject↔inject tak menyentuh data online** (lebih aman). → Picker tak mem-filter `IsManualEntry`.

### Validasi & preview link (Area 4)
- **D-07:** **Preview pra-commit tampilkan ringkasan PAIRING** (selain skor). Tampilkan: berapa pekerja akan **ter-pair** ke sibling existing, berapa **unpaired** (D-03), dan **apakah akan menempel stiker grup ke room online** (Kasus B, peringatkan "data online akan disentuh"). Reuse engine preview `PreviewInjectScore` + `AssessmentScoreAggregator` (395 D-09 / 396 D-08).
- **D-08:** **Anti-dobel-link per-pekerja → blok/peringatkan.** Bila per-pekerja **sudah ada sibling tipe-sama** di grup target (mis. pekerja X sudah punya Pre online di grup itu), **tolak/peringatkan pekerja itu** — hindari 2 Pre untuk 1 pekerja dalam 1 grup (ambigu di pairing & gain-score yang match by `UserId`). Masuk **daftar error/warn preview** (pola daftar-lengkap 396 D-09).

### Gray-area tambahan
- **D-09:** **Audit terpisah untuk perubahan data ONLINE.** Tiap kali sesi **online existing** diubah (tempel stiker grup Kasus B / set `LinkedSessionId` bidirectional), tulis **AuditLog tersendiri** (mis. `ActionType="LinkPrePost"`) berisi actor + `sessionId` online yang diubah + `LinkedGroupId` + room target — **terpisah** dari `"ManualInject"` pada sesi inject. Jejak compliance "data online disentuh" jelas. (Carry transparansi spec §10.)
- **D-11:** **Koherensi tanggal Pre vs Post → WARN di preview (allow).** Bila tanggal Pre (`CompletedAt`) lebih BARU dari Post target (urutan janggal untuk gain-score), tampilkan **peringatan** di preview tapi **jangan blok** commit (warn-but-allow; backdate manual bisa keliru, HC tahu konteks luring).
- **D-12:** **Unlink/ubah tautan pasca-commit → SERTAKAN di Phase 397** (scope addition, keputusan user — BUKAN defer). HC dapat membatalkan/mengubah tautan setelah commit. → **Catatan teknis (discretion):** endpoint + UI unlink; saat unlink, **putuskan nasib stiker Kasus B** (apakah revert `LinkedGroupId` pada sesi online existing bila grup jadi kosong-sebelah) + revert `LinkedSessionId` bidirectional; tetap atomic + audit (`"LinkPrePost"` reverse). Jaga minimal & fokus (jangan melar jadi editor link umum).

### Claude's Discretion (teknis — researcher/planner tetapkan)
- **Nilai `LinkedGroupId` baru (Kasus B):** rekomendasi = `RepresentativeId` room target; tulis ke **SEMUA** sesi room target + sesi inject (konsistensi level-room). Konfirmasi konvensi vs `CreateAssessment:1270`.
- **Endpoint picker:** reuse `ManageAssessmentTab_Assessment` (verifikasi bentuk balikan JSON vs view) atau endpoint baru ringan ter-filter tipe-lawan. Pilihan = discretion.
- **Komponen modal:** reuse pola Bootstrap modal existing (mis. di `CreateAssessment`/`ManageAssessment`); styling chip, debounce search.
- **Perubahan signature `InjectAssessmentService`:** resolusi sibling per-`UserId` dalam grup target (ganti broadcast `:120`), write-back ke sesi existing — semua dalam transaksi `InjectBatchAsync` yang sama (D-01 atomic).
- **Penamaan `ActionType` audit** ("LinkPrePost") + payload.
- Penempatan tombol "Cari Room" + chip di Step-1; copy notice; ikon.

### Folded Todos
[Tak ada todo yang di-fold. Lihat Reviewed Todos di bawah.]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — **§9** (Link Pre/Post ke Room Existing, D-8: picker reuse `ManageAssessmentTab_Assessment`, set `LinkedGroupId`/`LinkedSessionId`, silang inject↔online, **reuse logika PrePost — jangan bikin grouping baru**), **§10** (transparansi/audit wajib + atomic per-batch + anti-double-cert), **§13** ("Link silang Pre/Post: verifikasi grouping `LinkedGroupId` tak rusak saat satu sisi inject & satu sisi real online"), **§11 F5** (deliverable: search picker + wiring), **§12** (out-of-scope: multi-paket per room, import gambar Excel).
- `.planning/REQUIREMENTS.md` — **INJ-12** (cari & pilih room existing untuk tautkan sesi inject Pre/Post via `LinkedGroupId`+`LinkedSessionId`, reuse `ManageAssessmentTab_Assessment`, dukung silang inject↔online).
- `.planning/ROADMAP.md` — Phase 397 details + 4 Success Criteria + UI hint:yes; dependency **sequential-setelah-394** (praktis: setelah 395 & 396 ter-commit, file-overlap).

### CONTEXT carry-forward (keputusan terkunci)
- `.planning/phases/393-backend-core-inject/393-CONTEXT.md` — atomic per-batch, `IsManualEntry=true` + AuditLog `"ManualInject"`, anti-double-cert dedup, kontrak `InjectAssessmentService.InjectBatchAsync` + `InjectRequest`.
- `.planning/phases/394-page-setup-room-authoring-soal/394-CONTEXT.md` — **D-03 setup room** (`AssessmentType` Standard/PreTest/PostTest di Step-1, backdate `CompletedAt`, `AllowAnswerReview` default true), **D-01 wizard nav-pills** (jangan refactor pills/nav), **D-07 0-DB-write sampai commit**.
- `.planning/phases/395-mode-jawaban-input-asli-auto-generate/395-CONTEXT.md` — **D-09 preview via `AssessmentScoreAggregator` (pure/EF-free → preview==commit)** + `PreviewInjectScore`, `MapToRequest` server-otoritas, seam Step-5.
- `.planning/phases/396-import-excel-retire-bulkbackfill/396-CONTEXT.md` — **D-08 preview dry-run wajib**, **D-09 error report daftar-lengkap atomic** (pola untuk anti-dobel-link D-08 & warn list D-03/D-11), seam toggle Step-5 tanpa refactor nav.

### Kode di-reuse / di-extend (verifikasi line saat plan)
- `Models/AssessmentSession.cs:161-178` — `AssessmentType` (`string?`), `AssessmentPhase`, **`LinkedGroupId` (`int?`)**, **`LinkedSessionId` (`int?`)**; `IsManualEntry` (`bool`, :137).
- `Models/AssessmentConstants.cs:5-11` — `AssessmentType.PreTest="PreTest"` / `PostTest="PostTest"` (nilai eksak, bukan "Pre"/"Post").
- `Controllers/AssessmentAdminController.cs:1233-1315` — **pembuatan Pre/Post online 3-fase** (cross-link loop **`:1307-1314`**: `pre.LinkedGroupId=linkedGroupId; pre.LinkedSessionId=post.Id; post.LinkedSessionId=pre.Id`; `linkedGroupId=preSessions[0].Id` `:1270`) = **template wiring D-01/D-02**. `:7301-7327` **`TryAutoDetectCounterpartGroup`** (late-pairing by judul `^(Pre|Post)\s*Test\s+(.+)$`) — informasi, tapi user pilih picker eksplisit (bukan title-only). `:112-246` **`ManageAssessmentTab_Assessment`** (projeksi `:141-152` bawa `AssessmentType`+`LinkedGroupId`; room rows bawa `RepresentativeId`+`IsPrePostGroup`+`LinkedGroupId`) = sumber picker (D-06).
- `Controllers/CMPController.cs:250-344` (Results: pasangkan Pre↔Post **by `LinkedGroupId`**, Post boleh null) + **`:3412-3502` `GetGainScoreData`** (pasangkan **by `UserId`** dalam `LinkedGroupId`; **tak pakai `LinkedSessionId`**) = **bukti display pairing** → `LinkedGroupId` wajib benar (D-01), roster mismatch aman (D-03).
- `Services/InjectAssessmentService.cs:42-334` `InjectBatchAsync` (atomic tx) — konstruksi `AssessmentSession` **`:104-128`** (set `LinkedGroupId` **`:119`** / `LinkedSessionId` **`:120`** dari `req`; **kini broadcast 1 nilai → ubah per-pekerja** D-02). `InjectRequest` DTO **`:45-62`** (`AssessmentType` default "Standard", `LinkedGroupId`/`LinkedSessionId` `int?` sudah ada `:58-59`).
- `Controllers/InjectAssessmentController.cs` — **`MapToRequest:163-220`** (`AssessmentType` di-map `:174`; **`LinkedGroupId`/`LinkedSessionId` BELUM di-populate dari VM** → wiring 397). Actions: GET/POST `/Admin/InjectAssessment` + POST `/Admin/PreviewInjectScore`. Tambah endpoint picker/link + unlink (D-12) di sini.
- `Views/Admin/InjectAssessment.cshtml` — Step-1 `#step-1` (`:118`), **`#assessmentTypeInput` (`:154-163`)** + placeholder note "Penautan Pre/Post ke room existing tersedia pada fase berikutnya" (`:162` → ganti). Wizard: `#pill-1..6` + `#step-1..6`, single `<form id="injectAssessmentForm">` (`:114`). Tambah tombol "Cari Room" + modal + chip di Step-1 + ringkasan pairing di preview Step-5/6.
- `Helpers/AssessmentScoreAggregator.cs` — engine preview (`Compute` pure EF-free, preview==commit, D-07).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Wiring Pre/Post online** (`AssessmentAdminController.cs:1307-1314`) — template eksak set `LinkedGroupId`+`LinkedSessionId` bidirectional per-pekerja (D-01/D-02).
- **`ManageAssessmentTab_Assessment`** (`:112-246`) — query room (search by judul/kategori/jadwal, bawa `RepresentativeId`/`LinkedGroupId`/`IsPrePostGroup`/`AssessmentType`) → sumber picker (D-06).
- **`InjectRequest` + `InjectBatchAsync`** (393) — DTO sudah ber-`LinkedGroupId`/`LinkedSessionId`; service mem-persist & atomic; tinggal isi per-pekerja + write-back existing.
- **`PreviewInjectScore` + `AssessmentScoreAggregator`** (395) — extend untuk ringkasan pairing (D-07), preview==commit.
- **Pola Bootstrap modal + chip** (CreateAssessment/ManageAssessment) — UI picker (D-05).

### Established Patterns
- **Display pairing = `LinkedGroupId` (room) + `UserId` (per-pekerja)**, BUKAN `LinkedSessionId` → `LinkedGroupId` adalah yang menentukan tampil-berpasangan; `LinkedSessionId` fidelitas (D-02).
- Atomic batch: `BeginTransactionAsync`→SaveChanges→`Commit`/`Rollback` — semua write link (existing online + inject) **WAJIB dalam transaksi `InjectBatchAsync` yang sama** (D-01).
- Warn-but-allow + daftar-error-lengkap (395/396) → unpaired (D-03), tanggal janggal (D-11), anti-dobel (D-08) semua via preview/error list.
- Razor + JS + modal runtime → **Playwright wajib** (pelajaran 354): grep+build tak cukup untuk modal/picker/preview/chip.

### Integration Points
- Step-1 → tombol "Cari Room" (saat Pre/Post) → modal `ManageAssessmentTab_Assessment` (filter tipe-lawan) → chip → VM (`LinkedGroupId` target) → `MapToRequest` → `InjectBatchAsync` (resolve sibling per-`UserId` + write-back existing + audit `"LinkPrePost"`).
- **File-overlap SEQUENTIAL:** 397 extend controller/view/service yang sama dgn 395 & 396. Plan 397 **setelah 395 & 396 ter-commit** (hindari merge-clobber).
- Unlink (D-12): endpoint + UI baru di controller/view yang sama; revert link + (opsional) revert stiker Kasus B; atomic + audit.

### Risiko teknis utama (wajib ditangani plan)
- **Service broadcast 1 `LinkedSessionId`** (`:120`) — D-02 butуh per-pekerja; jangan biarkan broadcast (semua sesi salah-tunjuk).
- **Write ke data ONLINE existing** (Kasus B + bidirectional) — harus atomic dgn batch + audit terpisah (D-09); **jangan ubah skor/jawaban/status online**, cuma `LinkedGroupId`/`LinkedSessionId`.
- **Konsistensi level-room `LinkedGroupId`** (Kasus B) — tulis ke SEMUA sesi room target (bukan hanya yang ke-pair), kalau tidak grup tak konsisten.
- **Anti-dobel-link per-`UserId`** (D-08) — display gain-score match by `UserId`; 2 sibling tipe-sama = ambigu → blok/warn.
- **Link silang grouping utuh** (spec §13) — verifikasi Records/Monitoring tetap tampilkan pasangan saat 1 sisi inject 1 sisi online (test wajib).
- **Unlink scope-creep** (D-12) — jaga minimal (batal tautan, bukan editor link umum); rollback aman.

</code_context>

<specifics>
## Specific Ideas

- User minta penjelasan **sederhana** konsep grup (LinkedGroupId) sebelum memutuskan D-01 → framing analogi "stiker grup" membantu; pertahankan bahasa non-teknis saat menjelaskan dampak ke HC.
- Benang merah pilihan user 397: **"seakan online" penuh** (bidirectional D-02, tulis-ke-online untuk Kasus B D-01) + **transparansi/compliance** (audit terpisah `"LinkPrePost"` D-09) + **warn-but-allow** (unpaired D-03, tanggal D-11, anti-dobel D-08) + **fleksibel & lengkap** (link opsional D-04, inject↔inject D-10, **unlink dimasukkan** D-12).
- Prinsip menyeluruh (carry 393/395/396): hasil inject byte-identik online; **reuse logika PrePost existing, nol grouping baru** (spec §9) — wiring cuma isi `LinkedGroupId`/`LinkedSessionId`, jangan bikin mekanisme pairing sendiri.

</specifics>

<deferred>
## Deferred Ideas

- **Multi-paket variasi per room inject** — out-of-scope spec §12 (1 paket per room).
- **Import gambar soal via Excel** — out-of-scope spec §12.
- **Auto-detect by judul saja** (`TryAutoDetectCounterpartGroup`) sebagai satu-satunya jalur — ditolak; user pilih picker eksplisit (D-05/D-06). (Boleh dipakai sebagai bantuan/hint, bukan pengganti.)
- **Editor link umum / bulk re-link** — di luar D-12 (unlink minimal saja).
- **Link untuk tipe Standard** — hanya Pre/Post yang punya picker (D-06 tipe-lawan); Standard tak relevan.

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` ("One-time cleanup data test/audit lokal setelah Phase 367") — **false-positive** keyword match (test/controllers); tugas cleanup DB lokal pasca-367, bukan scope link Pre/Post 397. Tidak di-fold (sama keputusan 394/396).

</deferred>

---

*Phase: 397-link-pre-post-ke-room-existing*
*Context gathered: 2026-06-18 (discuss interaktif, advisor-off; 4 area inti + 4 gray-area tambahan = 12 keputusan locked)*
