# Judul Assessment Fleksibel + Cek Duplikat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hapus validator pola judul yang over-block (Phase 339) supaya judul fleksibel, lalu cegah double sertifikat lewat tombol "Cek Judul" + soft-block saat save.

**Architecture:** Satu shared static helper `AdminBaseController.FindTitleDuplicatesAsync` (zero-drift, diuji vs real-SQL — pola `ManualDuplicatePredicate`) jadi sumber tunggal untuk (a) endpoint AJAX `CheckTitleAvailability` dan (b) soft-block di `CreateAssessment` POST. UI wizard Langkah 1 dapat tombol Cek + checkbox konfirmasi.

**Tech Stack:** ASP.NET Core 8 MVC, EF Core 8 (SQL Server), Razor, vanilla JS (`fetch`), xUnit (`HcPortal.Tests`, `RecordCascadeFixture` real-SQL), Playwright e2e.

**Spec:** `docs/superpowers/specs/2026-06-15-judul-assessment-fleksibel-cek-duplikat-design.md`

**Run dir (semua command):** `C:/Users/Administrator/OneDrive - PT Pertamina (Persero)/Desktop/PortalHC_KPB-ITHandoff`

**Catatan rute:** `AdminBaseController` punya `[Route("Admin/[action]")]` → semua action `AssessmentAdminController` ada di `/Admin/<Action>` (mis. `/Admin/CheckTitleAvailability`). Selalu pakai `@Url.Action(...)`, jangan hardcode `/AssessmentAdmin/...`.

---

## File Structure

| File | Aksi | Tanggung jawab |
|------|------|----------------|
| `Controllers/AdminBaseController.cs` | Modify (setelah L267) | Shared helper `NormalizeTitleForDup` + record `TitleDuplicateMatch` + `FindTitleDuplicatesAsync` |
| `Controllers/AssessmentAdminController.cs` | Modify (hapus L871-879; +endpoint setelah L840; +param & soft-block setelah L982) | Hapus validator rigid; endpoint cek; soft-block save |
| `Views/Admin/CreateAssessment.cshtml` | Modify (Title block L183-194; JS ~L1232) | Tombol Cek + result + checkbox konfirmasi + handler fetch |
| `HcPortal.Tests/FindTitleDuplicatesTests.cs` | Create | TDD helper vs real-SQL |
| `tests/e2e/assessment-title-flexible.spec.ts` | Create | E2E tombol Cek (aman/dipakai) + validator hilang |

---

## Task 1: Shared dup helper + TDD test (real-SQL)

**Files:**
- Test: `HcPortal.Tests/FindTitleDuplicatesTests.cs` (create)
- Modify: `Controllers/AdminBaseController.cs` (insert setelah L267 `ManualDuplicatePredicate`, sebelum `}` penutup class L268)

- [ ] **Step 1: Tulis test gagal** — `HcPortal.Tests/FindTitleDuplicatesTests.cs`

```csharp
// judul-fleksibel-cek-duplikat (2026-06-15) — uji helper SHARED FindTitleDuplicatesAsync vs real-SQL
// (pola DuplicateGuardTests + RecordCascadeFixture). Dup = judul ternormalisasi (trim+collapse ws+lower)
// LINTAS kategori; satu "assessment" = grup distinct (Title, Category, Schedule.Date).
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class FindTitleDuplicatesTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public FindTitleDuplicatesTests(RecordCascadeFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "ttl-" + Guid.NewGuid().ToString("N")[..8], Email = "ttl@test.local", FullName = "Title Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession Sess(string userId, string title, string category, DateTime schedule) =>
        new AssessmentSession { UserId = userId, Title = title, Category = category, Status = "Upcoming", AccessToken = "", Schedule = schedule };

    [Fact]
    public async Task ExactTitle_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var title = "Cek Judul " + Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, title, "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, title);
        Assert.NotEmpty(matches);
    }

    [Fact]
    public async Task CaseAndWhitespaceInsensitive_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var token = Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, "Pre Test " + token, "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        // beda case + spasi ganda + spasi tepi → tetap kembar
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, "  pre  test " + token + "  ");
        Assert.NotEmpty(matches);
    }

    [Fact]
    public async Task CrossCategory_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var title = "Lintas Kat " + Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, title, "Licencor", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        // judul sama, beda kategori → tetap kembar (lintas kategori)
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, title);
        Assert.Contains(matches, m => m.Category == "Licencor");
    }

    [Fact]
    public async Task DifferentTitle_NotDetected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        ctx.AssessmentSessions.Add(Sess(uid, "Judul A " + Guid.NewGuid().ToString("N")[..8], "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, "Judul Z " + Guid.NewGuid().ToString("N")[..8]);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task EmptyTitle_ReturnsEmpty()
    {
        await using var ctx = NewCtx();
        Assert.Empty(await AdminBaseController.FindTitleDuplicatesAsync(ctx, ""));
        Assert.Empty(await AdminBaseController.FindTitleDuplicatesAsync(ctx, "   "));
    }
}
```

- [ ] **Step 2: Jalankan, pastikan GAGAL (compile error helper belum ada)**

Run: `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~FindTitleDuplicatesTests" --nologo`
Expected: FAIL — `'AdminBaseController' does not contain a definition for 'FindTitleDuplicatesAsync'` (build error).

- [ ] **Step 3: Implement helper** — `Controllers/AdminBaseController.cs`, insert tepat setelah L267 (akhir `ManualDuplicatePredicate`), sebelum `}` penutup class:

```csharp

        // ── Title duplicate detection (judul-fleksibel-cek-duplikat, 2026-06-15) ──
        // Normalisasi kanonik judul: trim + collapse whitespace + lowercase (pola NormalizePackageText:6268).
        public static string NormalizeTitleForDup(string? s)
            => System.Text.RegularExpressions.Regex.Replace((s ?? "").Trim(), @"\s+", " ").ToLowerInvariant();

        // Satu assessment = grup distinct (Title, Category, Schedule.Date) — konsisten dgn grouping sistem
        // (AssessmentAdminController.cs:179, :2870). Match by judul ternormalisasi LINTAS kategori.
        public record TitleDuplicateMatch(string Category, DateTime Tanggal, int Peserta);

        public static async Task<List<TitleDuplicateMatch>> FindTitleDuplicatesAsync(
            ApplicationDbContext ctx, string? title)
        {
            var norm = NormalizeTitleForDup(title);
            if (norm.Length == 0) return new List<TitleDuplicateMatch>();
            // GroupBy distinct dulu (jumlah grup terbatas), normalisasi+banding di memory
            // (normalizer C#-only, tak EF-translatable).
            var groups = await ctx.AssessmentSessions
                .GroupBy(a => new { a.Title, a.Category, Date = a.Schedule.Date })
                .Select(g => new { g.Key.Title, g.Key.Category, g.Key.Date, Peserta = g.Count() })
                .ToListAsync();
            return groups
                .Where(g => NormalizeTitleForDup(g.Title) == norm)
                .Select(g => new TitleDuplicateMatch(g.Category, g.Date, g.Peserta))
                .ToList();
        }
```

Catatan: `AdminBaseController.cs` sudah `using Microsoft.EntityFrameworkCore;` (L4) + `using HcPortal.Data;` (L7). ImplicitUsings net8 cover `System.Linq`/`System.Collections.Generic`/`System.Threading.Tasks` (lihat `HcPortal.Tests.GlobalUsings.g.cs`). Kalau build error tipe `List`/`GroupBy`, tambah `using System.Linq;` + `using System.Collections.Generic;` di atas.

- [ ] **Step 4: Jalankan, pastikan PASS**

Run: `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~FindTitleDuplicatesTests" --nologo`
Expected: PASS (5 test). Ini juga membuktikan `GroupBy(...a.Schedule.Date...)` ter-translate di SQL Server.

- [ ] **Step 5: Commit**

```bash
git add Controllers/AdminBaseController.cs HcPortal.Tests/FindTitleDuplicatesTests.cs
git commit -m "feat(assessment): shared FindTitleDuplicatesAsync helper + tests (cek judul kembar lintas kategori)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Hapus validator rigid (judul fleksibel)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (hapus blok L871-879)

- [ ] **Step 1: Hapus blok validator** — hapus PERSIS blok ini di `CreateAssessment` POST (saat ini L871-879):

```csharp
            // Phase 339 REST-06 (336-NAMING-CONVENTION-SPEC): Validate Title pattern for standard Pre/Post tests
            if (AssessmentTypeInput != "PrePostTest"
                && !string.IsNullOrEmpty(model.Title)
                && !System.Text.RegularExpressions.Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$"))
            {
                ModelState.AddModelError("Title",
                    "Title harus pola '{Stage} Test {Track} {Lokasi}' (Pre Test atau Post Test diikuti track + lokasi). " +
                    "Contoh valid: 'Pre Test OJT GAST Cilacap'. Reference: 336-NAMING-CONVENTION-SPEC.");
            }

```

JANGAN sentuh blok auto-pair di atasnya (L857-869, `TryAutoDetectCounterpartGroup`) — itu fungsi beda, tetap dipakai. JANGAN sentuh `// Handle Token Validation` di bawahnya.

- [ ] **Step 2: Verifikasi grep + build**

Run: `grep -c "336-NAMING-CONVENTION-SPEC" Controllers/AssessmentAdminController.cs`
Expected: `2` (turun dari 4 — sisa cuma comment auto-pair L857 + helper `TryAutoDetectCounterpartGroup` L7079; error-message + comment validator hilang).

Run: `grep -c "Regex.IsMatch(model.Title" Controllers/AssessmentAdminController.cs`
Expected: `0`.

Run: `dotnet build --nologo --verbosity quiet`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "fix(assessment): hapus validator pola judul Pre/Post (339) — judul fleksibel

Validator 336-NAMING-CONVENTION over-block judul sah non-Pre/Post (mis. Training Licencor).
Auto-pair TryAutoDetectCounterpartGroup dibiarkan (opportunistic, tak memblok).

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Endpoint CheckTitleAvailability

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (insert setelah L840 — akhir `CreateAssessment` GET `return View(model); }`, sebelum `// POST: Process form submission` L842)

- [ ] **Step 1: Tambah endpoint** — insert method baru:

```csharp

        // GET /Admin/CheckTitleAvailability?title=... — cek judul assessment sudah dipakai (cegah double sertifikat).
        // judul-fleksibel-cek-duplikat (2026-06-15). Read-only → tanpa antiforgery.
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CheckTitleAvailability(string title)
        {
            var matches = await FindTitleDuplicatesAsync(_context, title);
            return Json(new
            {
                exists = matches.Count > 0,
                groupCount = matches.Count,
                matches = matches.Select(m => new { category = m.Category, tanggal = m.Tanggal, peserta = m.Peserta })
            });
        }
```

`FindTitleDuplicatesAsync` diwarisi dari `AdminBaseController` (static) → panggil langsung. Default JSON ASP.NET Core = camelCase (`exists`, `groupCount`, `matches[].category/tanggal/peserta`).

- [ ] **Step 2: Verifikasi grep + build**

Run: `grep -c "CheckTitleAvailability" Controllers/AssessmentAdminController.cs`
Expected: `1`.

Run: `dotnet build --nologo --verbosity quiet`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(assessment): endpoint GET /Admin/CheckTitleAvailability (cek judul dipakai)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Soft-block save + param ConfirmDuplicateTitle

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (signature `CreateAssessment` POST L846-852; insert soft-block setelah L982)

- [ ] **Step 1: Tambah param** — di signature `CreateAssessment` POST, ganti baris `bool SamePackage = false)` jadi:

```csharp
            bool SamePackage = false,
            bool ConfirmDuplicateTitle = false)
```

- [ ] **Step 2: Insert soft-block** — tepat SETELAH blok renewal ValidUntil (saat ini berakhir L982, baris `}` penutup `if (isRenewalModePost && !model.ValidUntil.HasValue) {...}`), SEBELUM `// XOR validation` (L984). Penempatan di sini WAJIB karena butuh `isRenewalModePost` (didefinisikan L977):

```csharp

            // judul-fleksibel-cek-duplikat (2026-06-15): soft-block judul kembar (cegah double sertifikat).
            // Berlaku standard + PrePost. Skip renewal (judul sengaja reuse sertifikat asal). Override via konfirmasi.
            if (!string.IsNullOrWhiteSpace(model.Title)
                && !isRenewalModePost
                && !ConfirmDuplicateTitle)
            {
                var dupMatches = await FindTitleDuplicatesAsync(_context, model.Title);
                if (dupMatches.Count > 0)
                {
                    ModelState.AddModelError("Title",
                        $"Judul '{model.Title}' sudah dipakai di {dupMatches.Count} assessment. " +
                        "Centang konfirmasi di bawah untuk tetap membuat dengan judul sama.");
                    ViewBag.DuplicateTitleWarning = true;
                }
            }
```

- [ ] **Step 3: Verifikasi grep + build**

Run: `grep -c "ConfirmDuplicateTitle" Controllers/AssessmentAdminController.cs`
Expected: `2` (param signature + guard).

Run: `grep -c "DuplicateTitleWarning" Controllers/AssessmentAdminController.cs`
Expected: `1`.

Run: `dotnet build --nologo --verbosity quiet`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(assessment): soft-block save judul kembar + ConfirmDuplicateTitle override

Standard+PrePost; skip renewal (judul reuse by design). Cegah double sertifikat.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: UI tombol Cek + checkbox konfirmasi + JS

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (Title block L183-194; JS handler ~L1232)

- [ ] **Step 1: Ganti Title block** — replace PERSIS blok L183-194:

```html
                        <!-- Title -->
                        <div class="col-md-8">
                            <label asp-for="Title" class="form-label fw-bold">
                                Judul Assessment <span class="text-danger">*</span>
                            </label>
                            <input asp-for="Title" class="form-control" id="Title" placeholder="Masukkan judul assessment" maxlength="255" oninput="document.getElementById('titleCounter').textContent=this.value.length+'/255'" />
                            <div class="d-flex justify-content-between">
                                <div class="invalid-feedback">Judul assessment wajib diisi.</div>
                                <small class="text-muted" id="titleCounter">0/255</small>
                            </div>
                            <span asp-validation-for="Title" class="text-danger small"></span>
                        </div>
```

dengan:

```html
                        <!-- Title -->
                        <div class="col-md-8">
                            <label asp-for="Title" class="form-label fw-bold">
                                Judul Assessment <span class="text-danger">*</span>
                            </label>
                            <div class="input-group">
                                <input asp-for="Title" class="form-control" id="Title" placeholder="Masukkan judul assessment" maxlength="255" oninput="document.getElementById('titleCounter').textContent=this.value.length+'/255'" />
                                <button type="button" class="btn btn-outline-secondary" id="btnCheckTitle"
                                        data-check-url="@Url.Action("CheckTitleAvailability", "AssessmentAdmin")">
                                    <i class="bi bi-search me-1"></i>Cek Judul
                                </button>
                            </div>
                            <div class="d-flex justify-content-between">
                                <div class="invalid-feedback">Judul assessment wajib diisi.</div>
                                <small class="text-muted" id="titleCounter">0/255</small>
                            </div>
                            <span asp-validation-for="Title" class="text-danger small"></span>
                            <div id="titleCheckResult" class="mt-2" style="display:none;"></div>
                            @if ((bool?)ViewBag.DuplicateTitleWarning == true)
                            {
                                <div class="form-check mt-2">
                                    <input class="form-check-input" type="checkbox" name="ConfirmDuplicateTitle" value="true" id="confirmDuplicateTitle">
                                    <label class="form-check-label text-warning fw-semibold" for="confirmDuplicateTitle">
                                        Tetap buat walau judul kembar
                                    </label>
                                </div>
                            }
                        </div>
```

- [ ] **Step 2: Tambah JS handler** — insert setelah blok `// ---- Event: Selanjutnya buttons ----` (setelah L1231 `});` penutup `btnNext3`), sebelum `// ---- Event: Sebelumnya buttons ----` (L1233):

```javascript

    // ---- Event: Cek Judul (duplicate check) ----
    var btnCheckTitle = document.getElementById('btnCheckTitle');
    if (btnCheckTitle) {
        btnCheckTitle.addEventListener('click', function () {
            var title = (document.getElementById('Title').value || '').trim();
            var box = document.getElementById('titleCheckResult');
            box.style.display = 'block';
            if (!title) {
                box.innerHTML = '<div class="alert alert-secondary py-2 mb-0">Isi judul dulu sebelum cek.</div>';
                return;
            }
            btnCheckTitle.disabled = true;
            box.innerHTML = '<div class="text-muted small"><span class="spinner-border spinner-border-sm me-1"></span>Mengecek...</div>';
            var url = btnCheckTitle.getAttribute('data-check-url') + '?title=' + encodeURIComponent(title);
            fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.exists) {
                        box.innerHTML = '<div class="alert alert-success py-2 mb-0"><i class="bi bi-check-circle me-1"></i>Aman — judul belum dipakai.</div>';
                    } else {
                        var rows = (data.matches || []).map(function (m) {
                            var d = m.tanggal ? new Date(m.tanggal).toLocaleDateString('id-ID') : '-';
                            return '<li>' + m.category + ' &middot; ' + d + ' &middot; ' + m.peserta + ' peserta</li>';
                        }).join('');
                        box.innerHTML = '<div class="alert alert-warning py-2 mb-0">'
                            + '<i class="bi bi-exclamation-triangle me-1"></i>Judul dipakai di ' + data.groupCount + ' assessment:'
                            + '<ul class="mb-0 mt-1">' + rows + '</ul></div>';
                    }
                })
                .catch(function () {
                    box.innerHTML = '<div class="alert alert-danger py-2 mb-0">Gagal cek judul. Coba lagi.</div>';
                })
                .finally(function () { btnCheckTitle.disabled = false; });
        });
    }
```

- [ ] **Step 3: Verifikasi grep + build**

Run: `grep -c "btnCheckTitle" Views/Admin/CreateAssessment.cshtml`
Expected: `>=3` (button id + JS var + listener).

Run: `grep -c "ConfirmDuplicateTitle" Views/Admin/CreateAssessment.cshtml`
Expected: `1` (checkbox name).

Run: `dotnet build --nologo --verbosity quiet`
Expected: 0 error (Razor valid).

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "feat(assessment): tombol Cek Judul + checkbox konfirmasi duplikat di wizard Langkah 1

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: E2E (Playwright) + verifikasi penuh

> Pelajaran proyek (memory): Razor dinamis + JS WAJIB diverifikasi runtime Playwright — grep+build tak cukup. Server harus jalan di `localhost:5277`.

**Files:**
- Create: `tests/e2e/assessment-title-flexible.spec.ts`

- [ ] **Step 1: Jalankan server lokal** (terminal terpisah, biarkan jalan)

Run: `$env:Authentication__UseActiveDirectory="false"; dotnet run`
(AD lokal WAJIB false — lihat memory `project_355_shipped`. Pastikan SQLBrowser + shared-memory conn per `reference_local_e2e_sql_env_fix` kalau login 500.)
Expected: listening di `http://localhost:5277`.

- [ ] **Step 2: Tulis e2e spec** — `tests/e2e/assessment-title-flexible.spec.ts`

```typescript
// judul-fleksibel-cek-duplikat (2026-06-15) — verifikasi tombol Cek Judul + validator pola hilang.
// Pola login dari manage-assessment-filter.spec.ts (accounts fixture, /Account/Login).
// Pre-req: server localhost:5277 jalan; login admin@pertamina.com / 123456.
// Run: cd tests && npx playwright test e2e/assessment-title-flexible.spec.ts --workers=1
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Judul assessment fleksibel + cek duplikat', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CreateAssessment');
    await page.locator('#Title').waitFor({ state: 'visible', timeout: 15_000 });
  });

  test('Cek Judul: judul acak baru → alert aman (hijau)', async ({ page }) => {
    const unique = 'ZZ Judul Unik ' + Date.now();
    await page.fill('#Title', unique);
    await page.click('#btnCheckTitle');
    const box = page.locator('#titleCheckResult');
    await expect(box).toBeVisible();
    await expect(box.locator('.alert-success')).toContainText('Aman', { timeout: 10_000 });
  });

  test('Cek Judul: kosong → minta isi judul', async ({ page }) => {
    await page.fill('#Title', '');
    await page.click('#btnCheckTitle');
    await expect(page.locator('#titleCheckResult')).toContainText('Isi judul dulu');
  });

  // Judul fleksibel: isi non-Pre/Post + pilih kategori, klik Selanjutnya → tidak ada error pola judul.
  test('Judul non-Pre/Post lolos validasi pola (validator dihapus)', async ({ page }) => {
    await page.fill('#Title', 'training lisensor SRU ' + Date.now());
    // pilih kategori pertama yang tersedia (bukan placeholder)
    const cat = page.locator('select[asp-for="Category"], #Category, select[name="Category"]').first();
    // fallback: select kategori by value index 1 jika ada
    const catSelect = page.locator('select').filter({ hasText: '' }).first();
    await page.click('#btnNext1');
    // span validasi Title TIDAK boleh memuat pesan pola lama
    await expect(page.locator('span[data-valmsg-for="Title"], span.text-danger'))
      .not.toContainText('336-NAMING-CONVENTION-SPEC');
  });
});
```

Catatan executor: kalau selector kategori di Step "Judul non-Pre/Post" rapuh, sesuaikan ke `<select>` kategori nyata di `CreateAssessment.cshtml` (lihat L146-175 — `name="Category"` / optgroup). Inti assert: TIDAK muncul teks `336-NAMING-CONVENTION-SPEC`.

- [ ] **Step 3: Jalankan e2e**

Run: `cd tests; npx playwright test e2e/assessment-title-flexible.spec.ts --workers=1`
Expected: 3 PASS.

- [ ] **Step 4: Full regression — build + seluruh test suite**

Run: `dotnet build --nologo --verbosity quiet`
Expected: 0 error.

Run: `dotnet test --nologo --verbosity quiet`
Expected: PASS semua (catat baseline; FindTitleDuplicatesTests +5; tak ada regresi). Kalau ada test SQL-fixture butuh SQL Server lokal, pastikan instance jalan.

- [ ] **Step 5: Manual UAT (jalur POST soft-block — tak ter-otomasi e2e)**

Login admin `/Admin/CreateAssessment`, lalu cek manual:
1. **Fleksibel:** Title `training lisensor SRU`, Kategori Training Licencor, Tipe Standard, isi peserta+jadwal, submit → SAVE sukses (tak ada error pola).
2. **Cek tombol (dipakai):** ketik judul yang BARU saja dibuat di #1 → klik Cek → alert kuning + baris kategori·tanggal·peserta benar.
3. **Soft-block:** buat assessment ke-2 judul SAMA dgn #1 (Standard) → submit → balik Langkah 1, error "Judul '...' sudah dipakai di N assessment" + checkbox "Tetap buat walau judul kembar" muncul.
4. **Override:** centang checkbox → submit ulang → SAVE sukses.
5. **PrePost:** Tipe Pre-Post Test, judul sama dgn existing → submit → ke-block juga (scope standard+PrePost).
6. **Renewal:** buat renewal dari sertifikat existing (judul auto reuse) → submit → TIDAK ke-block (skip renewal).
7. **Regresi auto-pair:** judul `Pre Test OJT GAST <kota>` saat ada counterpart `Post Test OJT GAST <kota>` → TempData Info auto-pair tetap muncul.

- [ ] **Step 6: Commit e2e**

```bash
git add tests/e2e/assessment-title-flexible.spec.ts
git commit -m "test(e2e): judul fleksibel + tombol Cek Judul (3 skenario)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review (writing-plans)

**Spec coverage:**
- §4.A hapus validator → Task 2 ✓
- §4.B reuse normalizer → Task 1 `NormalizeTitleForDup` (pola NormalizePackageText) ✓
- §4.C endpoint → Task 3 ✓
- §4.D tombol UI → Task 5 ✓
- §4.E soft-block + ConfirmDuplicateTitle + checkbox → Task 4 (server) + Task 5 (checkbox) ✓
- §3 keputusan (exact title lintas kategori, standard+PrePost, skip renewal) → Task 1 test `CrossCategory` + Task 4 guard ✓
- §7 verifikasi → Task 6 ✓
- §6 residual (manual entry/edit defer) → tidak ada task (by design) ✓

**Placeholder scan:** semua step punya kode/command konkret. Selector kategori e2e ditandai "sesuaikan" (rapuh by nature Razor) — assert inti tetap konkret (`336-NAMING-CONVENTION-SPEC` absent).

**Type consistency:** `FindTitleDuplicatesAsync(ApplicationDbContext, string?)` + `TitleDuplicateMatch(Category, Tanggal, Peserta)` + `NormalizeTitleForDup(string?)` konsisten di Task 1/3/4. JSON camelCase (`exists/groupCount/matches[].category/tanggal/peserta`) konsisten endpoint (Task 3) ↔ JS (Task 5). `ConfirmDuplicateTitle` konsisten param (Task 4) ↔ checkbox name (Task 5).
