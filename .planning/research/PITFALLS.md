# Pitfalls Research

**Domain:** Tree view + drag-drop + AJAX CRUD pada ManageOrganization (ASP.NET Core Razor + Bootstrap)
**Researched:** 2026-04-02
**Confidence:** HIGH (berbasis analisis kode aktual di codebase)

---

## Critical Pitfalls

### Pitfall 1: Cascade Rename Rusak Saat Level Berubah Bersamaan

**What goes wrong:**
`EditOrganizationUnit` melakukan cascade rename hanya berdasarkan `unit.Level` pada saat edit. Jika drag-drop mengubah `ParentId` (sehingga level berubah dari 0 ke 1 atau sebaliknya) sebelum rename dicek, logika cascade salah menentukan apakah harus update `User.Section` atau `User.Unit`. Contoh: unit yang semula Level 0 (Bagian) dipindahkan ke bawah node lain — levelnya berubah jadi 1 — tapi cascade rename masih pakai path Level 0 kalau urutan operasi salah.

**Why it happens:**
Di `EditOrganizationUnit` (OrganizationController.cs baris 164), nama diubah setelah parent diubah. Tapi cascade rename membaca `unit.Level` yang sudah diupdate (baris 148: `unit.Level = newLevel`). Jika drag-drop AJAX menjadi endpoint terpisah yang mengubah parent dulu kemudian rename menyusul, ada window di mana level sudah berubah tapi denormalized fields belum diupdate.

**How to avoid:**
Endpoint AJAX drag-drop harus menggabungkan reparent + cascade dalam satu transaksi database. Jangan pisahkan "pindahkan parent" dan "cascade rename" menjadi dua AJAX call terpisah. Gunakan `IDbContextTransaction` eksplisit jika ada kemungkinan partial failure.

**Warning signs:**
- Ada endpoint AJAX terpisah untuk reorder/reparent yang tidak menyentuh cascade
- `User.Section` dan `User.Unit` tidak match setelah drag-drop antar level
- Unit yang awalnya Bagian (Level 0) menjadi sub-unit tanpa `User.Section` terupdate

**Phase to address:**
Phase implementasi drag-drop AJAX — harus menyertakan cascade dalam payload yang sama, bukan sebagai afterthought.

---

### Pitfall 2: CSRF Token Hilang Saat AJAX Menggantikan Form POST

**What goes wrong:**
View saat ini menggunakan `<form method="post">` dengan `@Html.AntiForgeryToken()` di setiap tombol. Jika diganti AJAX (`fetch`/jQuery), developer sering lupa menyertakan antiforgery token di header request, sehingga semua endpoint POST mengembalikan 400.

**Why it happens:**
Saat PRG (Post-Redirect-Get), Razor otomatis inject token ke form. Saat beralih ke `fetch()`, token harus diambil manual dari cookie atau meta tag dan disertakan sebagai header `RequestVerificationToken`. Developer yang tidak familiar ASP.NET Core sering melewatkan ini.

**How to avoid:**
Tambahkan meta tag di layout: `<meta name="__RequestVerificationToken" content="@Antiforgery.GetAndStoreTokens(Context).RequestToken" />`. Di semua `fetch()` call, sertakan header:
```js
headers: { 'RequestVerificationToken': document.querySelector('meta[name="__RequestVerificationToken"]').content }
```
Semua endpoint yang ada sudah memiliki `[ValidateAntiForgeryToken]` — jangan hapus atribut ini.

**Warning signs:**
- Console browser menunjukkan 400 Bad Request saat AJAX POST
- Developer menghapus `[ValidateAntiForgeryToken]` sebagai "solusi cepat"
- Hanya GET endpoint yang berhasil, semua POST gagal

**Phase to address:**
Phase pertama yang memperkenalkan AJAX — buat utility function terpusat untuk fetch dengan CSRF token, bukan copy-paste per tombol.

---

### Pitfall 3: DisplayOrder Korup Setelah Drag-Drop Antar Parent

**What goes wrong:**
`ReorderOrganizationUnit` saat ini hanya swap DisplayOrder antar sibling (OrganizationController.cs baris 344-354). Jika drag-drop memindahkan node ke parent baru pada posisi tertentu, DisplayOrder di parent lama dan baru harus di-recompute. Jika hanya `ParentId` yang diupdate tanpa normalisasi ulang DisplayOrder di parent baru, node bisa memiliki DisplayOrder yang sama dengan sibling lain atau urutan yang tidak terduga.

**Why it happens:**
Sistem saat ini mengasumsikan reorder hanya terjadi dalam satu parent (sibling swap). Drag-drop lintas parent adalah use case baru yang tidak ditangani oleh logika reorder yang ada.

**How to avoid:**
Endpoint drag-drop harus melakukan dua operasi atomik:
1. Update `ParentId` dan `Level` pada node yang dipindahkan
2. Recompute `DisplayOrder` seluruh sibling di parent lama (normalkan agar tidak ada gap) dan sisipkan node di posisi target di parent baru dengan menggeser DisplayOrder sibling yang ada

Gunakan satu `SaveChangesAsync()` setelah semua perubahan, bukan pemanggilan terpisah.

**Warning signs:**
- Setelah drag-drop, urutan node tidak sesuai dengan yang di-drop
- Dua node dalam parent yang sama memiliki DisplayOrder yang sama
- Refresh halaman menampilkan urutan yang berbeda dari yang terlihat setelah drag

**Phase to address:**
Phase implementasi drag-drop — tulis endpoint `MoveOrganizationUnit` baru yang menggantikan `ReorderOrganizationUnit` lama, jangan modifikasi endpoint reorder yang ada.

---

### Pitfall 4: GetSectionUnitsDictAsync Hanya Membaca 2 Level — Rusak Jika Struktur 3+ Level

**What goes wrong:**
`GetSectionUnitsDictAsync` di `ApplicationDbContext.cs` (baris 105-113) hanya membaca Level 0 (Bagian) dan direct children-nya. Jika drag-drop memungkinkan pembuatan struktur 3+ level, fungsi ini tidak akan mengembalikan unit yang ada di Level 2+. Semua 7 controller yang memanggil fungsi ini (WorkerController, CoachMappingController, CMPController, ProtonDataController, CDPController, dll.) akan menampilkan dropdown yang tidak lengkap.

**Why it happens:**
Fungsi ini ditulis saat struktur hanya 2 level (Bagian > Unit). Asumsi ini di-hardcode dengan `u.ParentId != null && bagianIds.Contains(u.ParentId!.Value)`. Penambahan level baru di UI tidak otomatis mengubah fungsi ini.

**How to avoid:**
Sebelum mengaktifkan struktur 3+ level di UI, update `GetSectionUnitsDictAsync` untuk menggunakan recursive query atau CTE. Atau, buat konvensi eksplisit bahwa tree view mendukung unlimited depth di tampilan admin, tapi `GetSectionUnitsDictAsync` tetap hanya expose Level 0-1 untuk dropdown pekerja (dokumentasikan limitasi ini secara eksplisit).

**Warning signs:**
- Unit Level 2+ yang dibuat via drag-drop tidak muncul di dropdown "Section/Unit" saat ManageWorkers
- `CoachMappingController` tidak menampilkan unit baru di filter
- Tidak ada error — hanya data yang "hilang" secara diam-diam

**Phase to address:**
Phase desain tree view — putuskan dulu apakah struktur 3+ level akan didukung end-to-end atau hanya di tampilan admin. Dokumentasikan keputusan ini sebelum implementasi.

---

### Pitfall 5: UpdateChildrenLevelsAsync Melakukan N+1 Query

**What goes wrong:**
`UpdateChildrenLevelsAsync` (OrganizationController.cs baris 236-247) melakukan `FindAsync` rekursif per child. Untuk tree dengan 20 node, ini menghasilkan 20 roundtrip database terpisah. Saat drag-drop sering digunakan, ini bisa menyebabkan latensi yang terasa di UI.

**Why it happens:**
Implementasi rekursif yang natural untuk update level, tapi tanpa batch loading. Tidak terasa saat data kecil (< 10 unit), tapi menjadi bottleneck saat data berkembang.

**How to avoid:**
Ganti dengan load seluruh subtree sekali (`_context.OrganizationUnits.Where(u => /* all descendants */).ToListAsync()`) kemudian update level di memory sebelum satu `SaveChangesAsync()`. Atau gunakan CTE recursive di SQL.

**Warning signs:**
- EF Core SQL profiling menunjukkan puluhan query kecil saat satu operasi reparent
- Operasi drag-drop terasa lambat (> 500ms) meski data sedikit

**Phase to address:**
Phase implementasi drag-drop AJAX — refactor `UpdateChildrenLevelsAsync` sebelum mengekspos operasi reparent di UI yang bisa sering dipanggil.

---

### Pitfall 6: TempData Tidak Berfungsi Setelah AJAX (Tidak Ada Redirect)

**What goes wrong:**
Semua endpoint saat ini menggunakan `TempData["Success"]` / `TempData["Error"]` yang ditampilkan oleh Razor view setelah redirect. Saat beralih ke AJAX, tidak ada redirect — sehingga TempData tidak pernah ditampilkan ke user. Operasi CRUD tampak "berhasil tanpa feedback" dari sudut pandang user.

**Why it happens:**
PRG pattern mengandalkan redirect sebagai mekanisme untuk membawa TempData ke render berikutnya. AJAX menghilangkan redirect ini. Developer kadang membiarkan TempData di server dan tidak menambahkan feedback di client, menghasilkan UX yang membingungkan.

**How to avoid:**
Endpoint AJAX harus mengembalikan JSON dengan `{ success: true, message: "..." }`. Client-side JavaScript menampilkan toast/alert berdasarkan response ini. Jangan campurkan dua pattern: jika endpoint sudah return JSON, hapus `TempData["Success"]` dari logika tersebut.

**Warning signs:**
- AJAX request berhasil (200) tapi tidak ada feedback visual
- TempData masih ada di endpoint tapi tidak pernah ditampilkan
- User mengklik tombol berulang kali karena tidak yakin berhasil atau tidak

**Phase to address:**
Phase implementasi AJAX — buat komponen toast JavaScript sebelum mengkonversi endpoint pertama.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Reuse `ReorderOrganizationUnit` untuk drag-drop lintas parent | Tidak perlu endpoint baru | DisplayOrder korup | Never — buat endpoint `MoveOrganizationUnit` baru |
| Hapus `[ValidateAntiForgeryToken]` untuk AJAX | Fix 400 error cepat | Seluruh halaman rentan CSRF | Never |
| Biarkan `GetSectionUnitsDictAsync` 2-level saja | Tidak perlu refactor | Data silently missing di 7 controller jika 3+ level dipakai | Acceptable HANYA jika keputusan eksplisit: tree UI dibatasi 2 level |
| Inline CSRF token per fetch call | Lebih sederhana | Copy-paste bug, mudah terlewat | Hanya di prototype; buat util function sebelum fase UAT |
| Tidak validasi circular reference di AJAX endpoint | Implementasi cepat | Data corrupt (node jadi ancestor dirinya sendiri) | Never |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| SortableJS / drag-drop library | Langsung update DOM tanpa konfirmasi server sukses | Optimistic UI: update DOM dulu, rollback jika AJAX gagal |
| Bootstrap Collapse + dynamic tree | Collapse state hilang setelah re-render HTML dari AJAX | Simpan expand/collapse state di `data-*` attribute atau localStorage sebelum re-render |
| `[ValidateAntiForgeryToken]` + fetch | 400 error karena header missing | Set `RequestVerificationToken` header di semua fetch calls via utility function terpusat |
| EF Core `FindAsync` dalam loop rekursif | N+1 query, tidak terasa saat dev | Load bulk dengan `.Where()` dan proses di memory |
| `TempData` + AJAX response | TempData tidak muncul karena tidak ada redirect | Kembalikan pesan sukses/error dalam JSON response, tampilkan via JavaScript toast |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Load seluruh tree di setiap page load dengan 3-level Include | Lambat saat ratusan unit | Lazy load children via AJAX expand | > 50 node total |
| N+1 di `UpdateChildrenLevelsAsync` | Operasi reparent lambat | Batch load subtree sekali | > 10 children dalam subtree |
| Re-render seluruh tabel setelah setiap AJAX | Flicker, collapse state hilang | Update hanya node yang berubah via targeted DOM manipulation | Setiap operasi AJAX |
| `Include().ThenInclude().ThenInclude()` hardcoded kedalaman | Level 4+ tidak ter-load | Gunakan recursive CTE atau tambah level include sesuai kebutuhan | Jika struktur > 3 level |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Hapus `[ValidateAntiForgeryToken]` untuk AJAX | CSRF attack pada endpoint CRUD | Sertakan token via header, jangan hapus atribut |
| Endpoint drag-drop tanpa validasi circular reference | Node bisa menjadi ancestor dirinya sendiri, data corrupt | Jalankan `IsDescendantAsync` di endpoint AJAX reparent |
| Return full entity di AJAX response | Expose field internal yang tidak diperlukan | Return DTO minimal (id, name, level, parentId, displayOrder) |
| Tidak validasi `parentId` di AJAX endpoint | User bisa assign parent ke node yang tidak ada | Validasi `parentId` ada di database sebelum update |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Collapse semua tree setelah setiap aksi CRUD | User harus expand ulang setiap kali edit | Pertahankan collapse state; expand node yang baru diedit |
| Tidak ada feedback saat drag-drop berhasil/gagal | User tidak tahu apakah operasi sukses | Tampilkan toast singkat setelah AJAX sukses/gagal |
| Tombol reorder up/down tetap setelah drag-drop aktif | Redundant dengan drag-drop; membingungkan | Hapus tombol up/down setelah drag-drop aktif, atau sembunyikan |
| Edit form muncul di atas tabel (PRG pattern saat ini) | Scroll jump, form jauh dari node yang diedit | Pindahkan edit ke inline form atau modal yang dekat dengan node |
| Tidak ada indikasi visual saat drag sedang terjadi | User tidak tahu drop target valid atau tidak | Gunakan CSS drop-target highlight dari library drag-drop |

---

## "Looks Done But Isn't" Checklist

- [ ] **Cascade rename:** Verifikasi `User.Section` dan `User.Unit` terupdate — cek di database setelah rename unit, bukan hanya di UI
- [ ] **Cascade reparent:** Verifikasi `User.Section` berubah saat unit Level 1 dipindahkan ke Bagian berbeda — ada potensi bug di OrganizationController.cs baris 189 yang hanya update Section untuk user di unit langsung, tidak untuk grandchildren
- [ ] **Circular reference:** Test drag node induk ke salah satu descendant-nya — harus ditolak dengan pesan jelas
- [ ] **Delete dengan FK:** Test hapus unit yang masih punya KkjFile/CpdpFile — harus ditolak (sudah ada di delete logic tapi perlu diverifikasi di AJAX flow)
- [ ] **CSRF di semua AJAX endpoint:** Test dengan browser dev tools — verifikasi header `RequestVerificationToken` ada di setiap POST
- [ ] **GetSectionUnitsDictAsync:** Cek apakah unit baru yang dibuat via drag-drop muncul di dropdown ManageWorkers
- [ ] **DisplayOrder setelah drag lintas parent:** Verifikasi tidak ada duplicate DisplayOrder dalam satu parent
- [ ] **Bootstrap Collapse state:** Setelah AJAX edit, tree expand/collapse state harus dipertahankan
- [ ] **TempData tidak muncul:** Verifikasi setiap operasi AJAX menampilkan feedback ke user

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| DisplayOrder korup | MEDIUM | Query manual: `UPDATE OrganizationUnits SET DisplayOrder = ROW_NUMBER() OVER (PARTITION BY ParentId ORDER BY Name)` |
| User.Section/Unit tidak sinkron | HIGH | Audit query users mana yang Section/Unit tidak match OrganizationUnit aktif; buat migration script untuk re-sinkronisasi |
| Circular reference di database | HIGH | Hapus manual via `UPDATE OrganizationUnits SET ParentId = NULL WHERE Id = X`; pastikan validasi di endpoint sebelum deploy |
| CSRF vulnerability terekspos | MEDIUM | Tambahkan antiforgery token ke semua fetch calls; tidak perlu perubahan data |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Cascade rename rusak saat level berubah | Phase desain endpoint AJAX reparent | Test: rename unit yang baru saja di-drag ke level berbeda — cek User.Section/Unit di DB |
| CSRF token hilang di AJAX | Phase pertama AJAX (sebelum implementasi fitur apapun) | Test: semua POST AJAX mengembalikan 200, bukan 400 |
| DisplayOrder korup lintas parent | Phase implementasi drag-drop | Test: drag node ke parent berbeda, query DB untuk cek tidak ada duplicate DisplayOrder |
| GetSectionUnitsDictAsync 2-level hardcoded | Phase desain (keputusan arsitektur sebelum implementasi) | Test: unit Level 2 muncul/tidak di dropdown ManageWorkers |
| N+1 di UpdateChildrenLevelsAsync | Phase implementasi drag-drop | Profiling: reparent node dengan 5+ children harus < 5 query |
| Bootstrap Collapse state hilang | Phase implementasi AJAX re-render | Test: edit unit, verifikasi tree state sama sebelum dan sesudah |
| Circular reference di AJAX endpoint | Phase implementasi drag-drop | Test: drag parent ke salah satu descendant — harus ditolak |
| TempData tidak berfungsi setelah AJAX | Phase pertama AJAX | Test: setiap operasi CUD menampilkan feedback visual |

---

## Sources

- Analisis langsung `Controllers/OrganizationController.cs` — kode cascade rename, reparent, reorder (HIGH confidence)
- Analisis langsung `Models/OrganizationUnit.cs` — struktur model dengan FK ke KkjFile/CpdpFile (HIGH confidence)
- Analisis langsung `Views/Admin/ManageOrganization.cshtml` — PRG pattern, multiple forms per row (HIGH confidence)
- Analisis langsung `Data/ApplicationDbContext.cs` baris 105-113 — `GetSectionUnitsDictAsync` 2-level hardcoded (HIGH confidence)
- Grep hasil: 7 file controller memanggil `GetSectionUnitsDictAsync` (HIGH confidence)
- ASP.NET Core antiforgery token dengan fetch API — domain knowledge (HIGH confidence)
- SortableJS drag-drop + Bootstrap Collapse interaction patterns — domain knowledge (MEDIUM confidence)

---
*Pitfalls research for: ManageOrganization Tree View + Drag-Drop + AJAX CRUD*
*Researched: 2026-04-02*
