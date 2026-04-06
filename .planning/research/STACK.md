# Technology Stack

**Project:** Portal HC KPB — v14.0 Assessment Enhancement
**Researched:** 2026-04-06
**Scope:** Penambahan library untuk fitur baru assessment. Tidak mencakup stack yang sudah ada dan tervalidasi.

---

## Stack yang SUDAH ADA (Tidak Diubah)

Stack berikut sudah berjalan di production dan tidak perlu diteliti ulang:

| Technology | Version | Status |
|------------|---------|--------|
| ASP.NET Core MVC + Razor | — | Aktif |
| Entity Framework Core + SQL Server | — | Aktif |
| SignalR | — | Aktif |
| Bootstrap 5.3 + Bootstrap Icons | — | Aktif |
| Chart.js 4 | — | Aktif |
| jQuery 3.7.1 | — | Aktif |
| ClosedXML | — | Aktif |
| QuestPDF | — | Aktif |
| SortableJS | — | Aktif |

---

## Penambahan Stack untuk Fitur Baru

### 1. Rich Text Editor — Essay Question

**Rekomendasi: Quill.js 2.0.3**

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Quill.js | 2.0.3 | Editor teks kaya untuk soal dan jawaban essay | Gratis (BSD-3-Clause), mobile-optimized, ringan, TypeScript rewrite April 2024 |

**Mengapa Quill, bukan TinyMCE:**
- TinyMCE mengunci banyak fitur penting (markdown, mentions, advanced typography) di balik paywall berbayar. Tidak cocok untuk portal internal Pertamina.
- Quill 2.0 sepenuhnya gratis, ditulis ulang dalam TypeScript, mendukung ESM, performa lebih ringan (memory usage lebih rendah dari TinyMCE dan CKEditor 5).
- Mobile-first: antarmuka responsif dan touch-friendly secara native — penting untuk aksesibilitas mobile di v14.0.
- Integrasi Razor Views sederhana: hidden field untuk model binding, ambil konten saat form submit.
- 3.3 juta downloads/minggu di npm; digunakan Slack, LinkedIn, Figma.

**CDN (tidak perlu npm/bundler):**
```html
<!-- CSS: taruh di <head> atau @section Styles -->
<link href="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.snow.css" rel="stylesheet">

<!-- JS: taruh di @section Scripts, sebelum inisialisasi -->
<script src="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.js"></script>
```

**Pola integrasi Razor (model binding via hidden field):**
```html
<!-- Di view, dalam <form> -->
<div id="quill-editor-essay"></div>
<input type="hidden" asp-for="EssayContent" id="hiddenEssayContent" />

<script>
  const quill = new Quill('#quill-editor-essay', {
    theme: 'snow',
    modules: { toolbar: ['bold', 'italic', 'underline', { 'list': 'ordered' }] }
  });

  // Populate jika edit (bukan baru)
  @if (Model.EssayContent != null) {
    <text>quill.setContents(JSON.parse('@Html.Raw(Model.EssayContent)'));</text>
  }

  // Simpan ke hidden field saat submit
  document.querySelector('form').addEventListener('submit', () => {
    document.getElementById('hiddenEssayContent').value =
      quill.getSemanticHTML();
  });
</script>
```

**Confidence: HIGH** — Diverifikasi dari Quill official docs + npm (v2.0.3 stable, April 2024).

---

### 2. Mobile Touch Navigation — Exam UI

**Rekomendasi: CSS Scroll Snap (Native Browser API, Zero Dependency)**

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| CSS Scroll Snap | Native | Navigasi soal dengan swipe kiri/kanan pada mobile | GPU-accelerated, didukung semua browser modern, zero download |

**Mengapa tidak memakai library touch/swipe:**
- **Hammer.js tidak dimaintain.** Issue #1273 di GitHub repo resmi secara eksplisit mempertanyakan apakah project ini sudah mati. Tidak ada commit signifikan sejak >6 tahun. Hindari untuk project baru.
- **CSS Scroll Snap adalah API native browser** yang di-handle oleh compositor thread (GPU-accelerated). Lebih smooth dari implementasi JavaScript pada mobile.
- Didukung penuh di semua browser modern termasuk Safari iOS — tanpa polyfill.
- Zero weight, zero maintenance risk, zero bundle impact.
- Cukup untuk use case navigasi soal (swipe antar pertanyaan). Tombol Prev/Next tetap pakai `scrollIntoView()` via jQuery.

**Pola implementasi exam UI:**
```css
/* Container soal — scroll horizontal dengan snap */
.exam-question-track {
  display: flex;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
  -webkit-overflow-scrolling: touch; /* iOS Safari */
  scrollbar-width: none;             /* Sembunyikan scrollbar desktop */
}
.exam-question-track::-webkit-scrollbar { display: none; }

/* Setiap slide pertanyaan */
.exam-question-slide {
  min-width: 100%;
  flex-shrink: 0;
  scroll-snap-align: start;
  scroll-snap-stop: always; /* Cegah skip soal */
}
```

```javascript
// Navigasi Prev/Next (jQuery, tidak butuh library baru)
function goToQuestion(index) {
  const slide = document.querySelectorAll('.exam-question-slide')[index];
  slide.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'start' });
}
```

**Confidence: HIGH** — MDN Web Docs + web.dev mengkonfirmasi dukungan universal.

---

### 3. Visualisasi Gain Score — Advanced Reporting

**Rekomendasi: chartjs-plugin-annotation 3.1.0**

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| chartjs-plugin-annotation | 3.1.0 | Anotasi garis referensi dan label threshold pada chart | Extends Chart.js 4 yang sudah ada; tidak perlu library chart baru |

**Mengapa plugin ini, bukan library chart baru:**
- Chart.js 4 (sudah terpasang) mendukung radar chart secara native — tipe chart yang ideal untuk visualisasi perbandingan Pre/Post/Gain Score per kompetensi.
- Plugin annotation menambahkan: garis horizontal target score, label nilai rata-rata, highlight area gain — semua di atas chart yang sudah ada.
- Tidak memerlukan library charting baru sama sekali. Cukup load satu plugin tambahan.
- Compatible dengan Chart.js >= 4.0.0 (diverifikasi dari npm docs).
- v3.1.0 dirilis Oktober 2024, didukung oleh Chart.js team sendiri.

**CDN:**
```html
<!-- Taruh setelah script Chart.js 4 yang sudah ada -->
<script src="https://cdn.jsdelivr.net/npm/chartjs-plugin-annotation@3.1.0/dist/chartjs-plugin-annotation.min.js"></script>
```

**Pola visualisasi Pre/Post/Gain Score (Radar Chart):**
```javascript
// Register plugin (diperlukan sekali)
Chart.register(ChartAnnotation);

new Chart(ctx, {
  type: 'radar',
  data: {
    labels: ['Kompetensi A', 'Kompetensi B', 'Kompetensi C'],
    datasets: [
      {
        label: 'Pre-Test',
        data: [60, 55, 70],
        borderColor: '#dc3545',
        backgroundColor: 'rgba(220,53,69,0.1)'
      },
      {
        label: 'Post-Test',
        data: [80, 75, 85],
        borderColor: '#0d6efd',
        backgroundColor: 'rgba(13,110,253,0.1)'
      },
      {
        label: 'Gain Score',
        data: [20, 20, 15],
        borderColor: '#198754',
        borderDash: [5, 5],    // Garis putus-putus untuk gain
        backgroundColor: 'rgba(25,135,84,0.05)'
      }
    ]
  },
  options: {
    scales: { r: { min: 0, max: 100, ticks: { stepSize: 20 } } },
    plugins: {
      annotation: {
        annotations: {
          passingLine: {
            type: 'line',
            yMin: 70, yMax: 70,
            borderColor: 'orange',
            borderWidth: 2,
            label: { content: 'Batas Lulus 70', display: true }
          }
        }
      }
    }
  }
});
```

**Untuk Item Analysis (Bar Chart + annotation):**
```javascript
// Annotation: garis rata-rata di atas bar chart
plugins: {
  annotation: {
    annotations: {
      avgLine: {
        type: 'line',
        yMin: avgScore, yMax: avgScore,
        borderColor: 'red',
        borderWidth: 1,
        label: { content: `Rata-rata: ${avgScore}%`, display: true, position: 'end' }
      }
    }
  }
}
```

**Confidence: HIGH** — Diverifikasi dari npm (v3.1.0, Oktober 2024) + official Chart.js annotation docs.

---

### 4. Accessibility Testing — Development & QA Only

**Rekomendasi: axe DevTools Browser Extension (non-code) + Deque.AxeCore.Playwright (optional automated)**

| Technology | Version | Purpose | Scope |
|------------|---------|---------|-------|
| axe DevTools (browser extension) | Latest | Manual WCAG audit selama development | Dev machine only |
| Deque.AxeCore.Playwright | 4.11.1 | Automated accessibility regression test | Test project only, bukan runtime |

**Pendekatan bertahap:**

**Fase awal (segera, tanpa code):**
Install browser extension axe DevTools (gratis, dari Deque) di Chrome/Edge developer. Jalankan audit manual pada halaman exam UI yang baru. Hasilnya tersedia langsung di DevTools panel.

**Fase lanjut (jika dibutuhkan automated regression):**
```xml
<!-- Hanya di test project, bukan di PortalHC_KPB.csproj -->
<PackageReference Include="Deque.AxeCore.Playwright" Version="4.11.1" />
<PackageReference Include="Microsoft.Playwright" Version="1.*" />
```

```csharp
// Contoh test accessibility pada halaman exam
[Test]
public async Task ExamPage_ShouldPassWCAG21AA()
{
    await Page.GotoAsync("/Assessment/TakeExam/123");
    var results = await Page.RunAxe();
    Assert.That(results.Violations, Is.Empty,
        "WCAG violations ditemukan: " + string.Join(", ",
            results.Violations.Select(v => v.Description)));
}
```

**Mengapa axe-core:**
- Standard industri — digunakan oleh Google, Microsoft
- Zero false positives (by design)
- Mendukung WCAG 2.0, 2.1, 2.2 level A/AA/AAA + Section 508
- v4.11.1 NuGet (November 2025) — aktif dimaintain

**Confidence: HIGH** — Diverifikasi dari NuGet Gallery (November 2025).

---

## Alternatif yang Dipertimbangkan

| Kategori | Direkomendasikan | Alternatif | Mengapa Tidak |
|----------|-----------------|------------|---------------|
| Rich Text Editor | Quill 2.0.3 | TinyMCE | Fitur penting di balik paywall; lebih berat; mobile experience inferior |
| Rich Text Editor | Quill 2.0.3 | CKEditor 5 | License lebih kompleks; lebih berat dari Quill |
| Touch/Swipe | CSS Scroll Snap (native) | Hammer.js 2.0.8 | Tidak dimaintain >6 tahun; issue aktif tanpa response dari maintainer |
| Touch/Swipe | CSS Scroll Snap (native) | Interact.js | Overkill untuk navigasi soal; dirancang untuk drag-drop interaktif |
| Touch/Swipe | CSS Scroll Snap (native) | ZingTouch | Dependency tambahan tanpa manfaat nyata vs. CSS native |
| Chart Annotation | chartjs-plugin-annotation 3.1.0 | Library chart baru (Recharts, ApexCharts) | Chart.js 4 sudah ada; tidak perlu dependency baru sama sekali |
| Accessibility | axe DevTools extension | Pa11y | Ekosistem lebih kecil; kurang mature; tidak zero-false-positive |

---

## Ringkasan Penambahan (Runtime)

Hanya **2 CDN script baru** yang perlu ditambahkan ke halaman:

```html
<!-- 1. Quill 2.0.3 — lazy load HANYA di halaman yang ada soal essay -->
<link href="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.snow.css" rel="stylesheet">
<script src="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.js"></script>

<!-- 2. chartjs-plugin-annotation 3.1.0 — lazy load HANYA di halaman reporting -->
<script src="https://cdn.jsdelivr.net/npm/chartjs-plugin-annotation@3.1.0/dist/chartjs-plugin-annotation.min.js"></script>
```

**CSS Scroll Snap:** tidak ada download, murni CSS.
**Axe-core:** hanya di test project, tidak masuk production bundle.

**Total tambahan weight:** ~400KB (Quill snow theme + JS) + ~30KB (annotation plugin) = ~430KB, dimuat secara conditional per halaman.

---

## What NOT to Add

| Hindari | Alasan |
|---------|--------|
| Hammer.js | Tidak dimaintain >6 tahun. Issue bertumpuk tanpa response. |
| TinyMCE (paid tier) | Paywall untuk fitur standar. Tidak sesuai untuk portal internal. |
| Library chart baru (ApexCharts, Recharts) | Chart.js 4 sudah ada dan sudah terintegrasikan. Tambah plugin saja. |
| React/Vue component library | Bertentangan dengan arsitektur Razor Views tanpa bundler. |
| Bootstrap Table plugin | Bootstrap 5 sudah cukup. Table sorting cukup dengan JavaScript sederhana. |
| ARIA library (aria-query, aria-utils) | Overkill. ARIA attribute ditulis langsung di HTML Razor. |

---

## Version Compatibility Matrix

| Library Baru | Chart.js 4 | Bootstrap 5.3 | jQuery 3.7.1 | ASP.NET Core |
|-------------|-----------|---------------|--------------|--------------|
| Quill 2.0.3 | Tidak berkaitan | Tidak ada konflik | Tidak ada konflik (vanilla JS) | Integrasi via hidden field standar |
| chartjs-plugin-annotation 3.1.0 | Compatible (requires >= 4.0.0) | Tidak berkaitan | Tidak berkaitan | Tidak ada konflik |
| CSS Scroll Snap | Tidak berkaitan | Tidak ada konflik | Tidak ada konflik | Tidak ada konflik |
| Deque.AxeCore.Playwright 4.11.1 | Tidak berkaitan | Tidak berkaitan | Tidak berkaitan | .NET 8 compatible |

---

## Sources

- [Quill 2.0 Official Docs](https://quilljs.com/) — HIGH confidence
- [Quill 2.0 Release Announcement — Slab Blog](https://slab.com/blog/announcing-quill-2-0/) — HIGH confidence
- [Quill npm (v2.0.3)](https://www.npmjs.com/package/quill) — HIGH confidence
- [TinyMCE vs Quill comparison](https://www.tiny.cloud/tinymce-vs-quill/) — MEDIUM confidence (vendor comparison, bias TinyMCE)
- [Which rich text editor in 2025 — Liveblocks](https://liveblocks.io/blog/which-rich-text-editor-framework-should-you-choose-in-2025) — MEDIUM confidence
- [chartjs-plugin-annotation npm (v3.1.0)](https://www.npmjs.com/package/chartjs-plugin-annotation) — HIGH confidence
- [chartjs-plugin-annotation Official Docs](https://www.chartjs.org/chartjs-plugin-annotation/latest/) — HIGH confidence
- [Hammer.js abandonment issue #1273](https://github.com/hammerjs/hammer.js/issues/1273) — HIGH confidence
- [CSS Scroll Snap — web.dev](https://web.dev/css-scroll-snap/) — HIGH confidence
- [CSS Scroll Snap — MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/scroll-snap-type) — HIGH confidence
- [Deque.AxeCore.Playwright NuGet (v4.11.1)](https://www.nuget.org/packages/Deque.AxeCore.Playwright) — HIGH confidence
- [axe-core-nuget GitHub](https://github.com/dequelabs/axe-core-nuget) — HIGH confidence
- [Chart.js Radar Chart Docs](https://www.chartjs.org/docs/latest/charts/radar.html) — HIGH confidence

---

*Stack research for: v14.0 Assessment Enhancement — Assessment Types, Question Types, Mobile Exam, Advanced Reporting, Accessibility*
*Researched: 2026-04-06*
