# Feature Research

**Domain:** Organization Tree Management UI (Admin Panel)
**Researched:** 2026-04-02
**Confidence:** HIGH (multiple design system sources: Adobe Commerce, PatternFly/Red Hat, Retool, Carbon)

## Konteks Proyek

Ini adalah **redesign**, bukan greenfield. Fitur backend sudah ada di OrganizationController:
- CRUD (Add, Edit, Delete, Toggle active/inactive OrganizationUnit)
- Reorder (up/down dalam siblings)
- Hierarki 3 level (Bagian → Unit → Sub-unit)
- Cascade rename/reparent ke User.Section/Unit dan CoachCoacheeMapping
- Constraint: tidak bisa hapus unit dengan active children/users/files

Fokus penelitian: **pola UI/UX untuk menampilkan dan mengelola tree tersebut di halaman admin.**

---

## Feature Landscape

### Table Stakes (Pengguna Mengharapkan Ini Ada)

Fitur yang dianggap sudah seharusnya ada. Tidak ada kredit jika ada, tapi terasa cacat jika tidak ada.

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Expand/collapse node | Standar universal semua tree UI (VSCode, Windows Explorer, macOS Finder) | LOW | Toggle per node; chevron/arrow di sebelah kiri label; state per-session |
| Visual indentasi hierarki | Menunjukkan parent-child secara visual tanpa membaca label | LOW | Indentasi per level; garis penghubung (guides) menguatkan hierarki |
| Inline action menu per node | Standar enterprise tree (Adobe Commerce, PatternFly) — edit dan delete ada di mana datanya | LOW | "..." button atau hover actions: Edit, Delete, Toggle; muncul on-hover, max 1 style action per tree |
| Tombol Add di level yang tepat | Admin harus bisa tambah child tanpa keluar halaman | LOW | "Tambah Unit" di bawah Bagian, "Tambah Sub-unit" di bawah Unit; tombol kecil + ikon "+" |
| Indikator status aktif/nonaktif | Admin harus bisa lihat status sekilas tanpa klik | LOW | Badge atau warna muted/grey untuk yang nonaktif; jangan sembunyikan info ini |
| Disable destructive actions saat tidak valid | Standar untuk semua CRUD dengan constraint | LOW | "Delete" disabled + tooltip penjelasan jika ada children aktif/users/files. Backend sudah ada constraint-nya, tinggal refleksikan ke UI |
| Expand All / Collapse All | Standar untuk tree dengan lebih dari 2 level | LOW | Tombol global di header area; toggle antara keduanya |

### Differentiators (Keunggulan yang Menambah Nilai)

Fitur yang tidak wajib tapi memberikan nilai nyata untuk admin HC.

| Fitur | Proposisi Nilai | Kompleksitas | Catatan |
|-------|-----------------|--------------|---------|
| Badge jumlah pekerja per unit | Admin HC langsung lihat distribusi pekerja tanpa navigasi ke halaman lain | LOW | COUNT query user per OrganizationUnit; sangat berguna untuk HC |
| Badge jumlah children per node | Admin tahu ada berapa unit/sub-unit tanpa expand | LOW | Data sudah tersedia dari relasi; "(3)" di sebelah nama Bagian |
| Inline rename langsung di node | Rename sederhana tanpa buka modal — lebih cepat untuk perubahan kecil | MEDIUM | Double-click label → input field; Enter save, Escape cancel; wire ke existing Edit action via AJAX |
| Konfirmasi delete dengan penjelasan dampak | "Unit ini memiliki 3 pekerja aktif" — lebih informatif dari dialog browser default | MEDIUM | Modal konfirmasi dengan detail: jumlah children, pekerja, file; berbeda dari alert box generik |
| Highlight visual node yang baru dibuat/diedit | Admin tahu persis mana yang baru berubah setelah operasi | LOW | Flash/highlight sementara (2-3 detik) pada node setelah add/edit berhasil |

### Anti-Features (Sering Diminta, Tapi Bermasalah)

| Fitur | Mengapa Diminta | Mengapa Bermasalah | Alternatif |
|-------|-----------------|--------------------|------------|
| Drag-and-drop reorder/reparent | Terlihat modern dan intuitif | (1) Reparent men-cascade ke User.Section/Unit dan CoachCoacheeMapping — risiko data rusak tinggi. (2) Fat-finger di tree kecil mudah terjadi. (3) Tidak ada undo. (4) Adobe Commerce sendiri menyebut drag "opsional" dan "mutually exclusive dengan checkbox". | Pertahankan tombol up/down yang sudah ada, tapi tampilkan inline di node (bukan di tabel). Lebih deliberate, lebih aman untuk data organisasi production |
| Org chart visual box-and-line diagram | Terlihat profesional dan sesuai "struktur organisasi" | Tidak practical untuk CRUD — susah klik target kecil, tidak scale untuk list panjang, butuh layout engine kompleks (d3.js/Mermaid) | Tree list view (file explorer style) adalah standar untuk management UI; org chart hanya cocok untuk read-only view/presentasi |
| Multi-select bulk delete | Tampak efisien | Sangat berbahaya — satu klik bisa hapus banyak unit dengan cascade effect ke ratusan user dan mapping. Tidak ada undo. Org structure jarang butuh bulk delete | Single delete dengan konfirmasi dan penjelasan dampak; jika perlu bulk, tambahkan confirmation step yang eksplisit |
| Inline add form yang expand di dalam tree | Tidak perlu modal, lebih contextual | Form add unit butuh beberapa field (nama, parent, status) — inline form dalam tree menjadi sesak, susah scroll, dan mati gaya di mobile | Modal/dialog untuk add; inline hanya untuk rename field tunggal |

---

## Feature Dependencies

```
[Tree visual dengan expand/collapse]
    └──requires──> [Data hierarkis diload dengan parent-child relationship dalam satu response]

[Inline rename]
    └──requires──> [Tree expand/collapse sudah stabil]
    └──requires──> [Existing Edit action di OrganizationController — SUDAH ADA]
    └──uses──AJAX──> [Edit action]

[Badge jumlah pekerja per unit]
    └──requires──> [COUNT query ApplicationUser per OrganizationUnitId]
    └──enhances──> [Badge jumlah children] (tampil bersama)

[Konfirmasi delete dengan detail dampak]
    └──requires──> [Query jumlah children aktif + users + files sebelum modal muncul]
    └──enhances──> [Disable destructive actions + tooltip]

[Highlight node setelah operasi]
    └──requires──> [AJAX-based add/edit/delete — tidak full page refresh]

[Search/filter dalam tree]
    └──requires──> [Tree sudah ter-render dengan benar]
    └──conflicts──> [Collapse All] (saat search aktif, collapse all tidak boleh collapse hasil)
```

### Dependency Notes

- **Inline rename requires AJAX**: Jika add/edit masih pakai full page refresh, inline rename tidak bisa. Perlu partial AJAX untuk edit action.
- **Drag-and-drop conflicts dengan up/down buttons**: Jika keduanya ada, pengguna bingung mana yang canonical. Pilih satu — rekomendasi: tetap up/down (sudah ada) tapi tampilkan sebagai arrow button di node, bukan di tabel.
- **Semua differentiators butuh tree foundation dulu** — jangan bangun badge atau inline rename sebelum tree expand/collapse stabil.

---

## MVP Definition

### Launch With — Redesign v1

Ini redesign bukan greenfield. MVP = semua fungsi yang sudah ada, ditampilkan dalam tree view modern.

- [ ] Tree view dengan expand/collapse — foundation dari seluruh redesign
- [ ] Visual indentasi hierarki dengan guides/connectors
- [ ] Action menu per node (Edit, Delete, Toggle) — menggantikan tombol di baris tabel
- [ ] Tombol Add per level yang tepat (contextual di bawah parent node)
- [ ] Badge status aktif/nonaktif per node
- [ ] Disable + tooltip untuk destructive actions yang invalid
- [ ] Expand All / Collapse All
- [ ] Tombol up/down reorder tetap ada, tapi sebagai arrow icon di node (bukan kolom tabel)

### Add After Validation — v1.x

- [ ] Badge jumlah pekerja per unit — trigger: admin merasa perlu lihat distribusi lebih cepat
- [ ] Badge jumlah children — trigger: tree besar, admin perlu orientasi cepat
- [ ] Konfirmasi delete dengan detail dampak — trigger: admin sering confused mengapa delete gagal
- [ ] Inline rename — trigger: admin sering rename dan merasa modal lambat

### Future Consideration — v2+

- [ ] Search/filter dalam tree — defer: org hanya 3 level, kemungkinan tidak banyak node (< 50)
- [ ] Highlight node setelah operasi — defer: nice polish, bukan blocker
- [ ] Read-only org chart view untuk presentasi — defer: beda audience (manajemen), beda halaman

---

## Feature Prioritization Matrix

| Fitur | User Value | Implementation Cost | Priority |
|-------|------------|---------------------|----------|
| Tree expand/collapse | HIGH | LOW | P1 |
| Visual indentasi + guides | HIGH | LOW | P1 |
| Action menu per node | HIGH | LOW | P1 |
| Tombol Add per level | HIGH | LOW | P1 |
| Badge status aktif/nonaktif | HIGH | LOW | P1 |
| Disable actions + tooltip | HIGH | LOW | P1 |
| Expand All / Collapse All | MEDIUM | LOW | P1 |
| Arrow up/down reorder di node | HIGH | LOW | P1 |
| Badge jumlah pekerja per unit | HIGH | LOW | P2 |
| Badge jumlah children | MEDIUM | LOW | P2 |
| Konfirmasi delete dengan detail dampak | HIGH | MEDIUM | P2 |
| Inline rename | MEDIUM | MEDIUM | P2 |
| Search/filter dalam tree | LOW | MEDIUM | P3 |
| Highlight node setelah operasi | LOW | MEDIUM | P3 |
| Read-only org chart diagram | LOW | HIGH | P3 |

**Priority key:**
- P1: Harus ada untuk launch redesign — semua adalah pemetaan fitur yang sudah ada ke UI baru
- P2: Tambahkan setelah P1 stabil dan divalidasi
- P3: Iterasi berikutnya, tidak mendesak

---

## Referensi Pola dari Design System Terkemuka

| Pola | Adobe Commerce Admin | PatternFly (Red Hat) | Carbon (IBM) | Retool |
|------|---------------------|---------------------|--------------|--------|
| Expand/collapse | Ya, arrow kiri | Ya, arrow kiri | Ya | Ya |
| Action menu per node | Ya, di hover | Ya, hover actions max 1 type | Ya | Ya |
| Inline rename | Tidak (modal) | Via separate inline edit component | Tidak | Ya |
| Drag-and-drop | Ya (opsional, mutually exclusive checkbox) | Disebutkan sebagai opsional | Tidak standar | Opsional |
| Badge children count | Tidak | Ya (badges opsional) | Tidak | Ya |
| Expand All/Collapse All | Ya | Tidak disebutkan secara eksplisit | Disebutkan | Tidak |
| Max recommended depth | 2 level | Tidak ada batas formal | Tidak ada | Tidak ada |

**Catatan kedalaman:** PortalHC butuh 3 level (Bagian → Unit → Sub-unit). Design system merekomendasikan max 2 sebagai guideline, bukan hard limit. Dengan visual hierarchy yang jelas (indentasi + guides), 3 level masih sangat manageable.

---

## Sources

- [Adobe Commerce Admin - Tree Pattern](https://developer.adobe.com/commerce/admin-developer/pattern-library/displaying-data/tree) — HIGH confidence
- [PatternFly Tree View Design Guidelines](https://www.patternfly.org/components/tree-view/design-guidelines/) — HIGH confidence
- [Carbon Design System - Tree View](https://carbondesignsystem.com/components/tree-view/usage/) — HIGH confidence
- [Retool - Designing a UI for Tree Data](https://retool.com/blog/designing-a-ui-for-tree-data) — MEDIUM confidence
- [Interaction Design for Trees - Hagan Rivers](https://medium.com/@hagan.rivers/interaction-design-for-trees-5e915b408ed2) — MEDIUM confidence

---
*Feature research for: Organization Tree Management UI — PortalHC KPB ManageOrganization Redesign*
*Researched: 2026-04-02*
