# Project Research Summary

**Project:** PortalHC KPB — ManageOrganization Tree View Redesign
**Domain:** ASP.NET Core MVC — Tree-view CRUD + AJAX + Bootstrap Modal + Drag-drop Reorder
**Researched:** 2026-04-02
**Confidence:** HIGH

## Executive Summary

ManageOrganization adalah halaman admin yang sudah fungsional secara backend, tapi view-nya bermasalah struktural: 520 baris dengan 3 copy-paste loop identik, setiap aksi menyebabkan full-page refresh (PRG pattern), dan form edit muncul di atas tabel alih-alih dekat dengan node yang diedit. Pendekatannya adalah **redesign UI murni** — backend CRUD sudah benar dan cascade logic harus dipertahankan sepenuhnya, yang berubah adalah presentation layer dari tabel PRG menjadi tree view AJAX.

Pendekatan yang direkomendasikan adalah kombinasi tiga teknologi: Bootstrap 5 Collapse untuk tree expand/collapse (sudah ada, tanpa library baru), SortableJS 1.15.7 via CDN untuk drag-drop reorder dalam sibling yang sama (satu-satunya library baru), dan JavaScript murni (`orgTree.js`) sebagai orchestrator state dan AJAX. Tidak ada SPA framework, tidak ada bundler — konsisten dengan arsitektur proyek yang ada. Semua library tree view yang tersedia baik abandoned maupun memerlukan bundler, sehingga solusi custom dengan Bootstrap Collapse adalah satu-satunya pilihan viable.

Risiko terbesar adalah membuka drag-drop cross-parent: fitur ini akan bypass cascade logic yang ada di backend dan dapat menyebabkan data `User.Section/Unit` tidak sinkron secara diam-diam. Mitigasinya jelas dan tegas — drag-drop HANYA untuk reorder dalam sibling yang sama (parent tetap), pindah parent harus lewat modal Edit. Selain itu, CSRF token harus diimplementasikan via utility function terpusat sejak fase AJAX pertama, dan TempData harus digantikan dengan JSON response plus JavaScript toast.

---

## Key Findings

### Recommended Stack

Stack eksisting sudah mencukupi untuk hampir semua kebutuhan: Bootstrap 5 Modal untuk CRUD dialog, Bootstrap 5 Collapse untuk tree expand/collapse, jQuery AJAX/fetch untuk request, dan ASP.NET anti-forgery untuk keamanan. Satu-satunya penambahan yang diperlukan adalah **SortableJS 1.15.7** via CDN untuk drag-drop reorder.

Semua alternatif tree view library telah di-evaluasi dan ditolak: bstreeview dan bs5treeview keduanya abandoned (bs5treeview bahkan memiliki deprecation notice resmi di README-nya sendiri), jsTree terlalu berat dan CSS-nya sulit di-override dengan Bootstrap 5, dan @jbtronics/bs-treeview memerlukan bundler yang tidak ada di proyek. SortableJS dipilih untuk drag-drop karena Vanilla JS murni, aktif dikembangkan (2.7 juta downloads/minggu), dan tidak ada konflik dengan jQuery atau Bootstrap 5.

**Core technologies:**
- **Bootstrap 5 Collapse** (sudah ada): tree expand/collapse — tidak butuh library tambahan, sudah built-in dengan animasi
- **SortableJS 1.15.7** (satu-satunya library baru, CDN): drag-drop reorder dalam sibling — Vanilla JS, mature, 2.7M downloads/minggu
- **Bootstrap 5 Modal** (sudah ada): Add/Edit dialog — satu modal dual-mode menggantikan multiple modal
- **jQuery AJAX / fetch** (sudah ada): AJAX requests — pattern sudah ada di seluruh proyek
- **orgTree.js** (file baru di wwwroot/js/): state management tree + orchestrator AJAX — semua JS dalam satu file terpusat

### Expected Features

Ini adalah redesign, bukan greenfield. Semua fungsi backend sudah ada — tujuan adalah memetakan fungsi tersebut ke UI tree yang modern. Design system terkemuka (Adobe Commerce, PatternFly, Carbon IBM) semuanya setuju pada pola yang sama: expand/collapse per node, action menu per-hover, tombol contextual Add per level, dan indikator status inline.

**Must have (table stakes) — P1, untuk launch redesign:**
- Tree view dengan expand/collapse per node — standar universal, tidak boleh absen
- Visual indentasi hierarki dengan guides/connectors
- Action menu per node (Edit, Delete, Toggle) — menggantikan kolom tombol di tabel lama
- Tombol Add per level yang tepat, contextual di bawah parent node
- Badge status aktif/nonaktif per node
- Disable + tooltip untuk destructive actions yang tidak valid (ada children/users/files)
- Expand All / Collapse All
- Arrow up/down reorder sebagai icon di node, menggantikan kolom tabel

**Should have (differentiators) — P2, setelah P1 stabil:**
- Badge jumlah pekerja per unit — sangat berguna untuk HC melihat distribusi
- Badge jumlah children — orientasi cepat di tree yang besar
- Konfirmasi delete dengan detail dampak (jumlah children, pekerja, file)
- Inline rename double-click — percepat perubahan kecil tanpa modal

**Defer (v2+):**
- Search/filter dalam tree — org 3 level, kemungkinan < 50 node, tidak urgen
- Highlight node setelah operasi — nice-to-have polish
- Read-only org chart visual (box-and-line) — audience berbeda, halaman berbeda

**Anti-features yang harus ditolak:**
- Drag-drop cross-parent / reparent — terlalu berisiko merusak cascade data
- Multi-select bulk delete — satu klik bisa hapus ratusan user dan mapping
- Org chart diagram untuk CRUD — tidak practical untuk admin management

### Architecture Approach

Target arsitektur memisahkan concern menjadi 4 layer yang jelas: (1) HTML shell minimal di `ManageOrganization.cshtml` sekitar 130 baris, turun dari 520, (2) `orgTree.js` sebagai orchestrator state + AJAX + render + SortableJS init, (3) Controller dual-response yang sama melayani PRG dan AJAX, (4) Endpoint `GetOrganizationTree` baru yang mengembalikan flat JSON array untuk di-render client-side.

Redesign ini sepenuhnya terisolasi — 7 controller dan 19 view lain yang mengonsumsi data organisasi tidak terpengaruh sama sekali, karena data organisasi disimpan sebagai denormalized string di `ApplicationUser.Section/Unit`, bukan sebagai FK ke OrganizationUnit. Cascade logic di backend dipertahankan 100%.

**Major components:**
1. `ManageOrganization.cshtml` — shell halaman: breadcrumb, alert, container div, modal markup, script include (dikurangi dari 520 ke ~130 baris)
2. `wwwroot/js/orgTree.js` (file baru, ~250 baris) — state management treeData[], fetch AJAX, renderTree(), modal controller, SortableJS init
3. `OrganizationController.GetOrganizationTree` (endpoint baru) — GET, return flat JSON array semua OrganizationUnit
4. Controller CRUD actions (dimodifikasi) — dual-response: JSON jika AJAX, redirect jika bukan
5. `OrganizationController.ReorderOrganizationUnit` (dimodifikasi) — ganti up/down swap dengan bulk JSON array update

**Pola utama yang dipakai:**
- **Dual-Response Pattern**: controller cek `X-Requested-With` header, return JSON atau redirect
- **Flat JSON + Client-side Render**: server kirim array flat, JS render rekursif
- **Single Modal Dual-Mode**: satu modal untuk Add dan Edit, mode diset via JS
- **Same-parent drag only**: SortableJS dengan `group: false`, cross-parent hanya via modal Edit

### Critical Pitfalls

1. **Drag-drop cross-parent membypass cascade logic** — Jangan izinkan drag antar parent berbeda. `EditOrganizationUnit` memiliki cascade kompleks (rename Section, reparent users, update CoachCoacheeMappings) yang harus dieksekusi saat parent berubah. Konfigurasi SortableJS dengan `group: false`.

2. **CSRF token hilang saat beralih ke AJAX** — Semua endpoint POST memiliki `[ValidateAntiForgeryToken]` dan akan mengembalikan 400 jika header `RequestVerificationToken` tidak ada. Buat utility function terpusat `ajaxPost(url, data)` di fase AJAX pertama, sebelum mengimplementasikan fitur apapun.

3. **TempData tidak muncul setelah AJAX** — PRG pattern mengandalkan redirect untuk menampilkan TempData. AJAX menghilangkan redirect ini, sehingga operasi tampak "berhasil tanpa feedback". Semua endpoint AJAX harus return `{success, message}` di JSON dan client menampilkan JavaScript toast.

4. **DisplayOrder korup setelah drag lintas parent** — Jika drag-drop cross-parent pernah diizinkan, DisplayOrder di parent lama dan baru harus di-recompute secara atomik. Jangan reuse `ReorderOrganizationUnit` yang ada untuk use case ini.

5. **`GetSectionUnitsDictAsync` hardcoded 2-level** — Fungsi ini hanya membaca Level 0 dan direct children-nya. Jika tree view mendukung Level 2+, unit tersebut tidak akan muncul di dropdown ManageWorkers dan 6 controller lainnya secara diam-diam. Keputusan arsitektur harus dibuat eksplisit sebelum implementasi.

---

## Implications for Roadmap

Berdasarkan penelitian, struktur fase yang disarankan adalah 4 fase dengan urutan berdasarkan dependency antar komponen:

### Phase 1: Backend AJAX Endpoints
**Rationale:** Backend harus siap sebelum frontend bisa di-test. Perubahan ini non-breaking — controller tetap melayani PRG untuk halaman lain, AJAX path hanya ditambahkan. Tidak ada perubahan ke cascade logic. CSRF utility function harus dibuat di sini sebelum fase apapun lainnya.
**Delivers:** Controller yang bisa merespons baik form POST maupun AJAX fetch; `GetOrganizationTree` endpoint baru; CSRF utility function terpusat
**Addresses:** Pitfall CSRF token hilang (P2), Pitfall TempData tidak muncul (P6)
**Implements:** Dual-Response Pattern, `GetOrganizationTree` endpoint baru, `ReorderOrganizationUnit` bulk update
**Research flag:** Tidak perlu research — pattern dual-response adalah standar ASP.NET Core yang sudah ada di proyek

### Phase 2: View Shell + Bootstrap Tree (Statis)
**Rationale:** Setelah backend siap, hapus 3 loop Razor copy-paste dan ganti dengan div container untuk tree. Bootstrap Collapse + custom CSS indent sudah cukup tanpa library baru. Ini adalah fondasi yang harus stabil sebelum AJAX dan drag-drop. View shell dipisah dari JS agar code review lebih mudah.
**Delivers:** Tree view yang ter-render dari JSON via orgTree.js dasar, expand/collapse berfungsi, modal markup tersedia, halaman dikurangi dari 520 ke ~130 baris
**Addresses:** Anti-pattern Razor recursive partial, Anti-pattern inline script di cshtml
**Implements:** ManageOrganization.cshtml shell, Bootstrap 5 Collapse expand/collapse, orgTree.js dengan renderTree() dasar
**Research flag:** Tidak perlu research — Bootstrap Collapse adalah komponen standar yang sudah ada di proyek

### Phase 3: orgTree.js — AJAX CRUD Lengkap
**Rationale:** Dengan backend dan shell siap, implementasikan JavaScript orchestrator secara lengkap. Urutan internal: fetch + renderTree dulu, kemudian Add via modal, kemudian Edit, kemudian Toggle, kemudian Delete. Setiap operasi harus menampilkan toast feedback. Collapse state harus dipertahankan saat re-render.
**Delivers:** CRUD penuh via AJAX tanpa page refresh, tree state dipertahankan, feedback visual toast ke user setiap operasi
**Addresses:** Pitfall TempData tidak muncul (P6), Pitfall Bootstrap Collapse state hilang
**Implements:** orgTree.js terpusat, Single Modal Dual-Mode, Flat JSON + Client-side Render, JavaScript toast
**Research flag:** Tidak perlu research — pola jQuery AJAX / fetch sudah ada di seluruh proyek

### Phase 4: SortableJS Drag-drop Reorder
**Rationale:** Drag-drop adalah fitur terakhir karena paling kompleks dan tidak blocking CRUD. Harus diimplementasikan SETELAH Phase 3 stabil. Batasan kritis yang tidak boleh dilanggar: hanya reorder dalam sibling yang sama, cross-parent diblokir sepenuhnya dengan `group: false`.
**Delivers:** Drag-drop visual untuk reorder sibling, payload bulk ke ReorderOrganizationUnit, visual feedback drag handle
**Addresses:** Pitfall cascade rusak cross-parent (P1), Pitfall DisplayOrder korup (P3), Pitfall N+1 di UpdateChildrenLevelsAsync
**Implements:** SortableJS 1.15.7 via CDN, `collectSiblingOrders()`, `saveReorder()`, `group: false`
**Research flag:** Tidak perlu research — SortableJS dokumentasi sudah dikonfirmasi HIGH confidence

### Phase Ordering Rationale

- Backend-first (Phase 1) memungkinkan testing manual di developer tools sebelum frontend selesai, dan tidak ada breaking change ke halaman lain
- View shell dipisah dari JS (Phase 2 vs Phase 3) agar perubahan Razor dan perubahan JavaScript bisa di-review secara independen
- Drag-drop terakhir (Phase 4) karena tidak blocking CRUD dan memiliki pitfall paling banyak — jika scope harus dipotong, Phase 4 bisa di-defer tanpa kehilangan fungsionalitas inti
- Keputusan eksplisit tentang `GetSectionUnitsDictAsync` (mendukung 2 level atau 3 level) harus dibuat sebelum Phase 2 di-deploy ke lingkungan yang dipakai user, karena berdampak diam-diam ke 7 controller lain

### Research Flags

Semua fase menggunakan pattern yang sudah well-documented dan sudah ada di proyek eksisting. Tidak ada fase yang membutuhkan `/gsd:research-phase` tambahan.

Satu keputusan desain yang harus dibuat sebelum implementasi (bukan butuh research, tapi butuh keputusan eksplisit dari stakeholder):
- **`GetSectionUnitsDictAsync` 2-level vs 3-level**: apakah unit Level 2 (sub-unit dari sub-unit, jika dibuat via tree view) harus muncul di dropdown ManageWorkers? Jika ya, fungsi ini perlu di-update sebelum tree view di-deploy. Jika tidak, dokumentasikan batasan ini eksplisit.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Semua library dikonfirmasi dari npm/GitHub resmi. SortableJS 1.15.7 aktif. Semua alternatif tree view terbukti abandoned atau butuh bundler. |
| Features | HIGH | Dikonfirmasi dari 4 design system terkemuka (Adobe Commerce, PatternFly, Carbon, Retool) yang konsisten satu sama lain. |
| Architecture | HIGH | Berdasarkan analisis langsung kode aktual di codebase: OrganizationController.cs 360 baris, ManageOrganization.cshtml 520 baris, grep audit 7 controller + 19 view. |
| Pitfalls | HIGH | Semua pitfall kritis diidentifikasi dari analisis kode aktual, bukan spekulasi. Nomor baris kode spesifik dikutip untuk setiap pitfall. |

**Overall confidence:** HIGH

### Gaps to Address

- **Keputusan `GetSectionUnitsDictAsync`**: Apakah tree view harus mendukung lebih dari 2 level secara end-to-end (termasuk di dropdown ManageWorkers)? Ini keputusan produk. Saat ini org structure adalah Bagian → Unit → Sub-unit (3 level), dan `GetSectionUnitsDictAsync` hanya expose 2 level pertama untuk dropdown. Jika status quo dipertahankan, tidak perlu perubahan fungsi tersebut.

- **Apakah drag-drop reorder (Phase 4) benar-benar diperlukan?** Research FEATURES.md mengategorikan drag-drop sebagai potensi anti-feature, tapi ARCHITECTURE.md dan STACK.md sudah menyiapkan solusinya. Jika arrow up/down inline sudah cukup untuk kebutuhan user, Phase 4 bisa di-defer sepenuhnya tanpa kehilangan fungsionalitas.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/OrganizationController.cs` — analisis langsung, 360 baris, cascade logic, pitfall mapping per nomor baris
- `Views/Admin/ManageOrganization.cshtml` — analisis langsung, 520 baris, masalah struktural dikonfirmasi
- `Data/ApplicationDbContext.cs` baris 105-113 — `GetSectionUnitsDictAsync` 2-level hardcoded dikonfirmasi
- Grep audit: 7 file controller memanggil `GetSectionUnitsDictAsync`, 19 view mengonsumsi org data
- [SortableJS npm](https://www.npmjs.com/package/sortablejs) — version 1.15.7, 2.7M downloads/minggu
- [SortableJS GitHub](https://github.com/SortableJS/Sortable) — Vanilla JS, 1,100+ commits aktif
- [bs5treeview GitHub deprecated notice](https://github.com/nhmvienna/bs5treeview) — deprecation dikonfirmasi di README resmi
- [Adobe Commerce Admin - Tree Pattern](https://developer.adobe.com/commerce/admin-developer/pattern-library/displaying-data/tree)
- [PatternFly Tree View Design Guidelines](https://www.patternfly.org/components/tree-view/design-guidelines/)
- [Carbon Design System - Tree View](https://carbondesignsystem.com/components/tree-view/usage/)
- [Bootstrap 5 Collapse documentation](https://getbootstrap.com/docs/5.3/components/collapse/)

### Secondary (MEDIUM confidence)
- [Retool - Designing a UI for Tree Data](https://retool.com/blog/designing-a-ui-for-tree-data)
- [Interaction Design for Trees - Hagan Rivers](https://medium.com/@hagan.rivers/interaction-design-for-trees-5e915b408ed2)
- [ASP.NET Core fetch + anti-forgery pattern](https://www.binaryintellect.net/articles/96b2cc91-73a8-480b-9785-fb6cbe7d9401.aspx)
- SortableJS + Bootstrap Collapse interaction patterns — domain knowledge

---
*Research completed: 2026-04-02*
*Ready for roadmap: yes*
