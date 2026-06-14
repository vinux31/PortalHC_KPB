# Phase 361: Bypass UI (B) - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

UI fitur **Bypass Tahun** di page `/ProtonData/Override` (PBYP-08..10):
- Page Override jadi **2 tab** — Tab1 "Override Deliverable" (existing, **TIDAK diubah**) + Tab2 "Bypass Tahun" (baru).
- Tab2: panel "Menunggu Konfirmasi" + filter + tabel worker + wizard 3-langkah (Tujuan → Closure mode → Detail).
- Notif deep-link `/ProtonData/Override?tab=bypass&pending={id}` (template `PROTON_BYPASS_READY` sudah shipped 360).
- UAT e2e: 4 closure mode + pending konfirmasi + batal + re-grade fail.

Backend 360 FINAL — UI konsumsi 6 endpoint JSON existing (`BypassList`, `BypassPendingList`, `BypassDetail`, `BypassSave`, `BypassConfirm`, `BypassCancelPending` di `ProtonDataController.cs:1499-1682`). **Satu pengecualian backend (D-09):** extend field select `BypassPendingList` — minor, tanpa migration.

**Migration:** false.
</domain>

<decisions>
## Implementation Decisions

### Wizard 3-langkah
- **D-01:** Bentuk = **modal multi-step** — satu modal Bootstrap berisi 3 step (Tujuan → Closure → Detail), konsisten pola `overrideModal` existing. State wizard di JS, tanpa navigasi page.
- **D-02:** Navigasi **linear wajib** — tombol Lanjut disabled sampai step valid, bisa Kembali, step indicator 1-2-3 di atas.
- **D-03:** Step 3 diakhiri **ringkasan + warning** sebelum submit: recap (worker, S→T, mode, unit, coach, alasan) + warning konsekuensi per mode (CL-B(a) force-approve deliverable; CL-B(b) sesi exam tanpa paket soal; CL-C tanpa nilai) + tombol "Jalankan Bypass".
- **D-04:** Feedback sukses = **toast + auto-refresh** tabel worker & panel pending via fetch (modal tutup). Khusus CL-B(b): alert kuning tambahan "Sesi exam dibuat — lampirkan paket soal di Kelola Assessment sebelum worker bisa ujian" (adaptasi 360 D-02 — respons JSON, bukan TempData).

### Deep-link & tab
- **D-05:** `?tab=bypass&pending={id}` → aktifkan Tab2 → load panel pending → **auto-buka modal "Lihat & Konfirmasi"** untuk pending {id}. 1 klik dari lonceng ke aksi.
- **D-06:** Pending param **stale** (Status Selesai/Dibatalkan atau id tak ada) → toast info "Pending bypass sudah diproses / tidak ditemukan" + Tab2 tampil normal. Notif lama aman diklik.
- **D-07:** Tab2 data **lazy-load** saat tab pertama kali aktif (atau langsung saat deep-link `tab=bypass`). Page load Tab1 default tidak berubah dari existing.
- **D-08:** Switch tab manual → **update query param** via `history.replaceState` (`?tab=bypass` / `?tab=deliverable`). Refresh/bookmark balik ke tab sama.

### Form detail & closure mode UX
- **D-09 (form CL-B(b)):** **Default murni** — wizard TIDAK minta jadwal/durasi/KKM; sesi bare dibuat full default backend + info text "Atur jadwal & paket soal di Kelola Assessment". (Menutup discretion 360 D-01 — opsi "default murni" yang dipilih.)
- **D-10:** Closure mode tak memenuhi syarat (dari `BypassDetail`) = **card disabled + teks alasan** (mis. "CL-A butuh semua deliverable approved + final ada"). Spec §9 "auto enable/disable per state".
- **D-11:** TargetUnit (wajib, 360 WR-02) = **dropdown cascading Bagian → Unit** dari OrganizationUnits — pola filter existing page, nilai valid terjamin.
- **D-12:** Coach dropdown opsional — default "Pertahankan coach saat ini" (= `TargetCoachId` null, selaras 360 D-16).
- **D-13:** Row pending status "Menunggu" = **badge + [Batal] saja**; tombol [Lihat & Konfirmasi] hanya muncul saat Status="Siap" (selaras backend D-11 360 yang menolak konfirmasi non-Siap).

### Panel pending, tabel worker, copy
- **D-14:** Filter tabel worker Tab2 = **reuse pola Tab1**: cascading Bagian → Unit → Track + tombol "Muat Data" (param `BypassList` sudah match, semua opsional).
- **D-15:** Tabel worker pakai **search nama client-side**, tanpa pagination (data sudah tersaring filter). Kolom sesuai spec §9: nama, track aktif, progress X/Y, final ✓/✗, tombol [Bypass].
- **D-16:** Panel "Menunggu Konfirmasi" **SELALU TAMPIL** — saat 0 pending render empty state "Tidak ada bypass menunggu konfirmasi". ⚠️ Deviasi sadar dari spec §9 "(kalau ada)" — keputusan user, panel selalu kelihatan.
- **D-17:** Label status di UI **deskriptif**: badge "Menunggu Exam" (kuning) / "Siap Dikonfirmasi" (hijau). Nilai DB tetap `Menunggu`/`Siap` — hanya label tampilan.

### Modal "Lihat & Konfirmasi" & error/race
- **D-18:** Isi modal = **extend backend dikit** — tambah field di select `BypassPendingList` (skor exam, tanggal selesai exam, `Reason`, nama coach target). ⚠️ Sentuh `ProtonDataController.cs` (kode 360) minor — query select only, TANPA migration, TANPA ubah kontrak field existing. Modal tampil: identitas worker, S→T, unit, coach, alasan, hasil+skor exam, tanggal.
- **D-19:** Tombol [Batal] pending → **confirm dialog ringan**: "Batalkan rencana bypass? Exam yang belum dikerjakan akan dihapus." (destructive §8.1 auto-cancel exam).
- **D-20:** Konfirmasi/Batal ditolak backend (state basi, D-11 360) → **toast merah pesan backend + auto-refresh panel pending**, modal tutup. State terbaru langsung kelihatan.
- **D-21:** **Anti dobel-klik client-side** — semua tombol POST (Save/Confirm/Cancel) disable + spinner saat request in-flight. Lengkapi guard atomik backend D-12 360.

### Strategi UAT e2e (PBYP-10)
- **D-22:** Dua lapis — **UAT live via Playwright MCP** @localhost:5277 (pola 358/359/360) + **spec `.spec.ts` committed** di `tests/e2e/` untuk regresi (pola 313/355).
- **D-23:** Seed = **SQL fixture temporary + snapshot/restore** per SEED_WORKFLOW (pola `313-timer-fixtures.sql`): worker multi-state (komplit CL-A, partial CL-B, punya final → D-D tolak, exam in-progress E5) + catat `docs/SEED_JOURNAL.md` + restore setelah selesai.
- **D-24:** Skenario re-grade Pass→Fail = **e2e UI ringan** — trigger re-grade via UI admin edit nilai (alur nyata), assert panel pending badge balik "Menunggu Exam" + tombol Konfirmasi hilang. Logic service sudah ter-cover xUnit 360.

### Claude's Discretion
- Struktur file JS Tab2 (inline `<script>` di Override.cshtml vs file terpisah wwwroot/js) — ikuti pola dominan codebase.
- Detail markup step indicator, styling card closure mode, ikon badge — ikuti Bootstrap 5 + pola visual existing.
- Pemecahan partial view (_Tab2Bypass.cshtml dsb.) vs satu file — planner pilih, asal Tab1 markup existing tidak berubah perilaku.
- Detail assert tiap spec e2e + pembagian file spec (1 file vs per-skenario).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (otoritas)
- `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` — spec final Diskusi B. §9 redesign page (otoritas layout Tab2), §8.1 batal pending, §5 pohon keputusan (untuk warning wizard), §10 endpoint, §11 edge case.
- `.planning/REQUIREMENTS.md` §PBYP — PBYP-08..10 (scope 361); PBYP-01..07 (konteks backend done).

### Backend 360 (kontrak API yang dikonsumsi)
- `.planning/phases/360-bypass-backend-b/360-CONTEXT.md` — keputusan backend D-01..D-16 (CL-B(b) bare session, D-10 blok dobel pending, D-11 cek basi, D-12 atomik, D-16 coach null).
- `Controllers/ProtonDataController.cs:1499-1682` — 6 endpoint bypass + `BypassSaveRequest` (`:80`). Bentuk JSON response = kontrak UI.
- `Services/ProtonBypassService.cs` — pesan error backend yang akan tampil di toast.

### Workflow & ops
- `docs/DEV_WORKFLOW.md` — verifikasi lokal build+run+UAT @5277 sebelum commit; migration=false (kecuali D-18 yang memang tanpa migration).
- `docs/SEED_WORKFLOW.md` — klasifikasi seed temporary + snapshot/restore + SEED_JOURNAL (D-23).
- `CLAUDE.md` — AD lokal: `Authentication__UseActiveDirectory=false dotnet run`.

### Pattern (reuse)
- `Views/ProtonData/Override.cshtml` — pola existing: filter cascading Bagian→Unit→Track + "Muat Data" (`:24-67`), empty state placeholder (`:70-74`), `overrideModal` (`:75+`), AntiForgeryToken (`@Html.AntiForgeryToken()` `:23`).
- `tests/e2e/` + `tests/e2e/helpers/` — pola spec Playwright committed; `.planning/seeds/313-timer-fixtures.sql` — pola SQL fixture.
- `Views/Admin/ManageAssessment.cshtml` dkk. — pola Bootstrap nav-tabs existing.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/ProtonData/Override.cshtml` (447 baris) — page yang di-redesign. Filter cascading + modal + vanilla JS fetch sudah ada; Tab1 = konten existing dibungkus tab-pane TANPA ubah perilaku.
- 6 endpoint bypass JSON live (360): `BypassList` (filter bagian/unit/trackId → rows nama/track/progress/finalAda), `BypassPendingList` (status Menunggu|Siap → rows + hasilExam), `BypassDetail` (state worker → mode allowed), `BypassSave` ([FromBody] JSON), `BypassConfirm`/`BypassCancelPending` ([FromBody] pendingId).
- Notif template `PROTON_BYPASS_READY` + `ActionUrlTemplate="/ProtonData/Override?tab=bypass&pending={PendingId}"` sudah shipped — UI tinggal handle param.
- `OrgLabels.GetLabel(0/1)` — label dinamis Bagian/Unit di filter.

### Established Patterns
- POST [FromBody] JSON + header AntiForgery (`RequestVerificationToken`) — cek pola fetch existing di Override.cshtml JS sebelum tulis call baru.
- Toast/alert pattern existing di views admin; Bootstrap 5.3 nav-tabs.
- Playwright e2e: `global.setup.ts` login state + helpers/.

### Integration Points
- `Views/ProtonData/Override.cshtml` — restruktur jadi 2 tab (Tab1 markup existing utuh).
- `Controllers/ProtonDataController.cs:231` (`Override()` GET) — mungkin perlu pass ViewBag tambahan (mis. AllTracks sudah ada; daftar coach untuk dropdown wizard → cek apakah perlu endpoint/ViewBag baru).
- `Controllers/ProtonDataController.cs:1534` (`BypassPendingList`) — extend select per D-18 (skor, tanggal exam, Reason, coach target).
- `tests/e2e/` — spec baru bypass.

### Risiko (planner harus selesaikan)
- Sumber data **dropdown coach** wizard (list coach eligible per unit target): belum ada endpoint khusus — planner tentukan (ViewBag dari Override() / endpoint kecil / reuse endpoint existing CoachMapping). Jangan hardcode.
- Skor exam untuk D-18: pastikan field yang tepat di `AssessmentSession` (Score/IsPassed/CompletedAt) — verifikasi nama kolom nyata sebelum extend select.
- Wizard step 2 bergantung `BypassDetail` response shape — baca implementasi `:1563+` untuk field `allowedModes`/alasan exact.
</code_context>

<specifics>
## Specific Ideas

- Alur notif → aksi harus 1-klik: lonceng → Tab2 → modal konfirmasi langsung terbuka (D-05). Ini nilai utama fitur pending.
- Warning di step ringkasan wizard = "jejak jujur" versi UI — HC sadar konsekuensi force-approve / sesi-tanpa-paket SEBELUM eksekusi.
- Panel pending selalu tampil (D-16) supaya HC selalu aware ada/tidaknya antrian bypass — walau spec aslinya "(kalau ada)".
</specifics>

<deferred>
## Deferred Ideas

- **Audit/improve Tab1 Override Deliverable** — tetap backlog (spec §13, carry dari 360).
- **Undo bypass executed** — tidak ada tombol (spec §8.2 Opsi C).
- **Polling/auto-refresh berkala panel pending** — tidak dibangun; refresh manual + setelah aksi cukup. Kalau dibutuhkan nanti, jadi improvement terpisah.

### Reviewed Todos (not folded)
None — `todo match-phase 361` kosong (0 todo pending).
</deferred>

---

*Phase: 361-bypass-ui-b*
*Context gathered: 2026-06-11*
