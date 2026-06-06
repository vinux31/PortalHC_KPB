# Deep Audit Pre-Delivery IT — 2026-06-06

**Bundle:** `b34f4475..61c75798` (v19–v23, 294 commit)
**Auditor:** Claude (Rino)
**Scope:** kode nyata (`.cs` / `.cshtml` / `.json` / `.js`), exclude docs & .planning

## Ringkasan Gate Awal
- `dotnet build`: 0 error / 0 warning ✅
- `dotnet test`: 112/112 pass ✅
- git: clean, 0 ahead/0 behind origin ✅
- Migration: 1 (`AddOrganizationLevelLabel`, auto-apply) ✅
- AD flag: `UseActiveDirectory: true` di HEAD (commit `f5ddfcac`) ✅

## Catatan IT (dari audit umum)
1. AD nyala → login non-admin lewat LDAP. Server Dev wajib reach `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com`. Fallback local cuma `admin@pertamina.com`.
2. `appsettings.Production.json` connection string masih placeholder (`YOUR_SQL_SERVER_NAME`/`YOUR_PASSWORD`) — wajib diisi sebelum promosi Prod.
3. `appsettings.json` base = SQLite; ke-override Development → SQL Server. Aman selama env=Development.

---

## Temuan Deep-Audit

> Severity: 🔴 HIGH (blocker/security) · 🟡 MED · 🔵 LOW (polish). Diisi dari hasil reviewer paralel.

### 🔴 HIGH

**H1 — Search admin ManageAssessment ke-drop diam-diam (REGRESSI bundle)** — ✅ FIXED 2026-06-06 (belum commit)
Fix: `WorkerDataService.cs:259` scope `null`/kosong di-treat `"Nama"` (root-cause, defensif). Test regresi `Scope_Null_WithSearch_FiltersByName_H1` ditambah → build 0 err, test 113/113.
`Services/WorkerDataService.cs:259` + `Controllers/AssessmentAdminController.cs:273`
`GetWorkersInSection(... string? searchScope = null)`. Block SQL name-narrow di L259 cuma jalan kalau `searchScope == "Nama"`. AssessmentAdminController:273 panggil `GetWorkersInSection(section, unit, category, search, statusFilter)` — posisional, `searchScope` = `null`. Akibat: ketik nama di search ManageAssessment → search **diabaikan total** (bukan SQL-narrow, bukan post-load). Sebelum bundle kodenya `if (!string.IsNullOrEmpty(search))` unconditional → bundle yg introduce regresi.
**FIX:** treat null/empty sebagai default: `var scope = string.IsNullOrEmpty(searchScope) ? "Nama" : searchScope;` ATAU caller L273 kirim `searchScope: "Nama"`.

**H2 — Seed Level shift vs DB Dev existing (idempotent gak nyentuh data lama)** — ✅ DOCUMENTED → handoff IT (belum commit)
Aksi IT: ditambah ke `docs/DB_HANDOFF_IT_2026-06-06.html` Step 5 (cek `SELECT Level, COUNT(*) ... GROUP BY Level` → kalau 1/2 jalankan guarded `UPDATE OrganizationUnits SET Level=Level-1`). Guard `NOT EXISTS Level=0` cegah double-run. TL;DR diupdate.
`Data/SeedData.cs:93,102` (Bagian `Level 1→0`, Unit `Level 2→1`). Seed di-guard `if (await context.OrganizationUnits.AnyAsync()) return;` (L80) → **DB Dev yg sudah ada org unit lama tetap Level 1/2**, sedang kode baru (EditOrganizationUnit / PreviewEditCascade) anggap `Level==0` = Bagian. Mismatch → cascade rename & label level salah branch di data existing. Migration `AddOrganizationLevelLabel` TIDAK normalize Level.
**FIX / aksi IT:** sebelum pakai fitur org di Dev, cek `SELECT DISTINCT Level FROM OrganizationUnits`. Kalau hasil 1/2 → butuh data-fix `UPDATE OrganizationUnits SET Level = Level - 1` (atau re-seed DB kosong). Kalau sudah 0/1 → aman.

**H3 — AD go-live: 2 risiko laten LdapAuthService (kode lama, AD baru ON)**
`Services/LdapAuthService.cs:85` — Step 2 attribute search pakai `new DirectoryEntry(ldapPath)` **anonymous** (tanpa kredensial). AD modern default block anonymous bind → `FindOne()` null/throw → user password BENAR tetap ditolak "Username atau password salah".
`Services/LdapAuthService.cs:74,90` — Step 1 bind pakai `email` sebagai username; Step 2 filter `(samaccountname={email})`. Kalau user login pakai email (UPN) tapi `samaccountname` ≠ email → FindOne null → ditolak walau password benar. Konvensi login (email vs NIP vs samaccountname) belum dikonfirmasi.
**FIX / aksi IT:** WAJIB tes 1 login user AD asli di Dev SEBELUM cutover penuh. Kalau anon search diblok → search pakai `bindEntry` (authenticated) atau service account. Konfirmasi field login = samaccountname atau mail, samakan filter.

### 🟡 MED

**M1** `Program.cs:148` — migration auto-apply `Database.Migrate()` di-bungkus `catch(Exception)` yg cuma log lalu lanjut boot → migration gagal = DB schema partial tapi app tetap nyala (gagal tersembunyi). FIX: rethrow/fail-fast saat migration error + log daftar migration applied buat verifikasi IT. (Catatan: handoff doc sudah minta IT cek log+tabel — by-design tapi rapuh.)

**M2** `Program.cs:108` — cookie `SecurePolicy = SameAsRequest`; di Dev HTTP (`http://10.55.3.3`) cookie auth dikirim tanpa flag Secure → bisa di-intercept di intranet. FIX: `SecurePolicy.Always` + HTTPS, atau terima risiko intranet-HTTP secara eksplisit.

**M3** `appsettings.json:11` — base `DefaultConnection = "Data Source=HcPortal.db"` (sintaks SQLite) tapi `Program.cs:27` selalu `UseSqlServer`. Aman selama env=Development/Production (ke-override). Tapi env apapun tanpa override → SQL Server parse string ini → startup gagal. FIX: ganti placeholder SQL Server valid / buang remnant SQLite.

**M4** `Controllers/OrgLabelController.cs:107` — `UpdateLevelLabel` cuma catch `InvalidOperationException`; unique index Label → update duplikat racing lempar `DbUpdateException` (uncaught) → 500. FIX: tambah `catch (DbUpdateException)` return Json friendly (sama spt `AddLevelLabel`).

**M5** `Services/OrgLabelService.cs:39` — `GetAll()` cache `IMemoryCache` tanpa TTL, invalidasi cuma in-process. Multi-worker IIS / web-farm → instance lain sajikan label stale setelah CRUD. FIX: pin app pool single worker (maxProcesses=1) + dokumentasi, atau TTL pendek / version-stamp DB.

**M6** `Controllers/OrganizationController.cs:198` — `EditOrganizationUnit` cascade multi-tabel (rename+reparent+update level anak) tanpa explicit transaction. Single `SaveChanges` (L268) atomik IF semua mutasi sebelum itu (terverifikasi: ya). FIX: konfirmasi tidak ada intermediate SaveChanges; kalau ada → wrap `BeginTransactionAsync`.

### 🔵 LOW

**L1** `Controllers/OrgLabelController.cs:145` — `AddLevelLabel` `expectedNext = GetMaxConfiguredLevel()+1`; tabel kosong → max=0 → expectedNext=1 → level 0 gak bisa ditambah (off-by-one). FIX: special-case `dict.Count==0 → expectedNext=0`.
**L2** `Services/OrgLabelService.cs:39,48` — `GetOrCreate` non-atomic (cache stampede, harmless) + mutate→audit tanpa tx (audit gap kalau LogAsync gagal, bukan data loss). FIX: lock/Lazy + wrap audit di tx, atau terima.
**L3** `Helpers/OrgTreePreOrder.cs:41` — `Walk()` recursion tanpa visited-set/depth cap → ParentId siklik (self/A↔B) = StackOverflow (uncatchable). Sekarang cuma dipanggil test, ParentId DB gak ada cycle constraint. FIX: track `HashSet<int>` visited sebelum wiring ke live DB.
**L4** `Program.cs:140` — branch SQLite WAL dead code (provider selalu SqlServer). FIX: hapus remnant SQLite.

### ℹ️ Catatan (bukan defect, by-design)
- `CMPController` — akses Results/Certificate/CertificatePdf sengaja diperlebar dari {owner, Admin, HC} jadi + roleLevel 1-3 (semua section) + roleLevel 4 (section sendiri). Intentional (REC-04). IT aware aja saat promosi.
- HybridAuthService admin fallback (`admin@pertamina.com`) **tidak spoofable** — fallback local tetap butuh password hash Identity yg benar.
- SQL injection: nihil (semua EF LINQ parameterized). Password logging: nihil (log email-only). Hardcoded real secret: nihil (Production conn = placeholder).

---

## Rekomendasi sebelum kirim IT
- **H1** fix dulu (regresi user-facing, 1 baris). **H2 & H3** = aksi verifikasi IT, masukin ke handoff (bukan blocker kode tapi blocker go-live).
- MED/LOW: opsional, bisa fase lanjutan.
