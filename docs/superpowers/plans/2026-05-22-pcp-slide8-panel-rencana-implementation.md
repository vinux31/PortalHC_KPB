# PCP Slide 8 — Panel Rencana Pembuatan (System) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a 450×148px compact HTML file that renders as a PNG suitable for insertion into placeholder #2 (RENCANA PEMBUATAN) on slide 8 of Risalah Web.pptx, focused on the **System** dimension (tech stack + deployment + 1-line metode), dropping `metode/` prefix and `/fabrikasi alat` since this is a web project not physical equipment.

**Architecture:** Single self-contained HTML file at `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`. Two horizontal strips inside a yellow-border-top card: Strip 1 = 6 tech stack badges (`.NET 8`, `ASP.NET Core MVC`, `EF Core 8`, `SQL Server`, `SignalR`, `Bootstrap`); Strip 2 = deployment flow (Lokal → Dev → Prod) plus 1-liner metode footer (Internal Gugus PROTON · ~1 bulan · DEV_WORKFLOW SOP). Same html2canvas @2x export pattern as sibling `versi-p-compact.html`.

**Tech Stack:** Plain HTML5 + CSS3 (no build), html2canvas 1.4.1 via CDN, Chrome for verification.

**Source spec:** `docs/superpowers/specs/2026-05-22-pcp-slide8-panel-rencana-design.md`

**Sibling file (style reference, read-only):** `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`

---

## File Structure

| File | Role | Status |
|---|---|---|
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html` | Compact HTML for slide 8 placeholder #2 | CREATE |
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md` | Add new entry to file list + future-stage update | MODIFY |
| `docs/pcp-HCPortal-2026/Risalah Web.pptx` | Insert PNG into slide 8 placeholder #2 (manual step) | MODIFY |
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md` | Append changelog entry | MODIFY |

No tests authored (visual artifact, manual verification via Chrome + Playwright dimension check).

---

## Task 1: Scaffold panel-rencana HTML

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`

- [ ] **Step 1: Write the full HTML file**

Create `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<title>§3.4 Slide 8 — Panel Rencana Pembuatan (System)</title>
<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-red-light: #fce8eb;
    --pertamina-blue: #00558C;
    --pertamina-blue-dark: #003D63;
    --pertamina-blue-light: #e6f0f7;
    --pertamina-green: #00A551;
    --pertamina-yellow: #FFC72C;
    --pertamina-yellow-light: #fff5d6;
    --neutral-gray: #6b7280;
    --neutral-light: #d1d5db;
    --bg: #f6f7fb;

    --stack-net: #C8102E;
    --stack-mvc: #6f42c1;
    --stack-ef: #00558C;
    --stack-sql: #003D63;
    --stack-signalr: #f97316;
    --stack-bs: #4c1d95;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    padding: 0;
    width: 450px;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    background: var(--bg);
    color: #1f2937;
  }
  #export-btn {
    position: absolute;
    top: 4px;
    right: 4px;
    background: var(--pertamina-red);
    color: white;
    border: none;
    padding: 3px 8px;
    border-radius: 3px;
    font-size: 9px;
    font-weight: 600;
    cursor: pointer;
    z-index: 100;
  }

  #panel-wrap {
    background: white;
    border-radius: .35rem;
    padding: 4px;
    box-shadow: 0 1px 4px rgba(0,0,0,.06);
    border-top: 3px solid var(--pertamina-yellow);
  }

  /* Strip 1: Tech Stack */
  .stack-strip {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    padding: 4px 2px;
    border-bottom: 1px dashed var(--neutral-light);
    justify-content: center;
  }
  .badge {
    padding: 2px 6px;
    border: 1px solid;
    border-radius: 3px;
    font-size: 8.5px;
    font-weight: 600;
    white-space: nowrap;
    line-height: 1.3;
  }
  .badge.net { background: #fbe5e9; border-color: var(--stack-net); color: var(--stack-net); }
  .badge.mvc { background: #ede4f7; border-color: var(--stack-mvc); color: var(--stack-mvc); }
  .badge.ef { background: var(--pertamina-blue-light); border-color: var(--stack-ef); color: var(--stack-ef); }
  .badge.sql { background: #d6e1ea; border-color: var(--stack-sql); color: var(--stack-sql); }
  .badge.signalr { background: #fde6cf; border-color: var(--stack-signalr); color: #c2410c; }
  .badge.bs { background: #e1d6f2; border-color: var(--stack-bs); color: var(--stack-bs); }

  /* Strip 2a: Deployment flow */
  .flow-strip {
    display: flex;
    gap: 6px;
    align-items: center;
    justify-content: center;
    padding: 3px 2px;
  }
  .flow-node {
    background: var(--pertamina-blue-light);
    border: 1px solid var(--pertamina-blue);
    color: var(--pertamina-blue-dark);
    padding: 3px 8px;
    border-radius: 3px;
    font-size: 9px;
    font-weight: 600;
    white-space: nowrap;
  }
  .flow-node small { font-weight: 400; opacity: .8; }
  .flow-arrow {
    font-size: 14px;
    font-weight: 800;
    color: var(--neutral-gray);
  }

  /* Strip 2b: Metode footer */
  .metode-footer {
    text-align: center;
    font-size: 7.5px;
    font-style: italic;
    color: var(--neutral-gray);
    padding: 2px 4px 1px;
  }

  @media print { #export-btn { display: none; } }
</style>
</head>
<body>
<button id="export-btn" onclick="exportPNG()">📸 PNG</button>

<div id="panel-wrap">
  <div class="stack-strip">
    <span class="badge net">⚙️ .NET 8</span>
    <span class="badge mvc">🌐 ASP.NET Core MVC</span>
    <span class="badge ef">🔗 EF Core 8</span>
    <span class="badge sql">🗄️ SQL Server</span>
    <span class="badge signalr">📡 SignalR</span>
    <span class="badge bs">🎨 Bootstrap</span>
  </div>

  <div class="flow-strip">
    <div class="flow-node">💻 Lokal</div>
    <div class="flow-arrow">→</div>
    <div class="flow-node">🛠️ Dev <small>(10.55.3.3)</small></div>
    <div class="flow-arrow">→</div>
    <div class="flow-node">🚀 Prod <small>(IIS Windows)</small></div>
  </div>

  <div class="metode-footer">
    👥 Internal Gugus PROTON · ⏱️ ~1 bulan build · 📋 SOP DEV_WORKFLOW
  </div>
</div>

<script>
async function exportPNG() {
  const btn = document.getElementById('export-btn');
  btn.style.display = 'none';
  const target = document.getElementById('panel-wrap');
  const canvas = await html2canvas(target, {
    scale: 2,
    backgroundColor: '#ffffff',
    useCORS: true
  });
  btn.style.display = '';
  const link = document.createElement('a');
  link.download = 'panel-rencana-slide8.png';
  link.href = canvas.toDataURL('image/png');
  link.click();
}
</script>
</body>
</html>
```

- [ ] **Step 2: Commit**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html"
git commit -m "feat(pcp-slide8): panel rencana compact (stack + deploy + metode)"
```

---

## Task 2: Visual Verify via Playwright

**Files:**
- Verify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`

- [ ] **Step 1: Start local HTTP server (if not running)**

```bash
python -m http.server 8765 --directory docs
```

(Run in background. If port busy, kill prior server: `pkill -f "python.*http.server.*8765"`)

- [ ] **Step 2: Navigate Chrome to file**

URL: `http://localhost:8765/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`

Verify:
- Page loads (no 404)
- No console errors
- Export button visible top-right
- Yellow border-top on panel-wrap card

- [ ] **Step 3: Check dimensions via DevTools / Playwright evaluate**

```javascript
() => {
  const body = document.body.getBoundingClientRect();
  const wrap = document.getElementById('panel-wrap').getBoundingClientRect();
  const stack = document.querySelector('.stack-strip').getBoundingClientRect();
  const flow = document.querySelector('.flow-strip').getBoundingClientRect();
  const footer = document.querySelector('.metode-footer').getBoundingClientRect();
  return {
    body: { w: body.width, h: body.height },
    panelWrap: { w: wrap.width, h: wrap.height },
    stackStrip: { h: stack.height, wrapped: stack.height > 28 ? 'WRAPPED' : 'single-row' },
    flowStrip: { h: flow.height },
    metodeFooter: { h: footer.height },
    aspect: (wrap.width / wrap.height).toFixed(2),
    targetAspect: '3.03 (450x148)'
  };
}
```

Expected:
- `body.w` = 450
- `panelWrap.h` ≤ 160px (target 148, tolerance +12)
- `stackStrip.wrapped` = `single-row` (6 badges fit width 450 single line)
- Aspect close to 3.03

If `stackStrip.wrapped = WRAPPED`: shorten one badge text (e.g., `ASP.NET Core MVC` → `ASP.NET MVC`), reduce badge padding to `2px 5px`, or reduce font to 8px. Apply minimal fix, re-verify.

- [ ] **Step 4: Take screenshot of panel-wrap for visual record**

Playwright command:

```javascript
await page.locator('#panel-wrap').screenshot({ path: 'panel-rencana-preview.png', type: 'png' });
```

Inspect screenshot — confirm:
- 6 badges colored distinctly (red, purple, blue, dark-blue, orange, dark-purple)
- 3 flow nodes (Lokal, Dev, Prod) with arrows
- Metode footer 1-liner italic gray
- No overflow / clipping

- [ ] **Step 5: No commit** (verification only)

---

## Task 3: Export PNG (User-Driven)

**Files:**
- Output: PNG file (`panel-rencana-slide8.png`)

- [ ] **Step 1: User opens HTML in their Chrome and clicks Export PNG button**

URL: `http://localhost:8765/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html`

Click `📸 PNG` top-right corner. Browser downloads `panel-rencana-slide8.png` (~900×~296 retina 2× scale) to Downloads folder.

- [ ] **Step 2: Verify PNG dimensions**

Open PNG in image viewer or Chrome:
- Width ≈ 900 px (2× of 450)
- Height ≈ 296 px (2× of ~148)
- White background, no transparent
- Yellow border-top visible
- All 6 badges + 3 flow nodes + footer readable

- [ ] **Step 3: User inserts PNG into Risalah Web.pptx slide 8 placeholder #2**

Manual:
1. Open `docs/pcp-HCPortal-2026/Risalah Web.pptx`
2. Slide 8
3. Right-click placeholder text `RENCANA PEMBUATAN OLEH RINO` (bottom-left small box) → delete
4. Insert → Pictures → select downloaded `panel-rencana-slide8.png`
5. Resize to fit the box (Shift+drag for aspect preserve)
6. Save pptx (keep .pptx format)

- [ ] **Step 4: User confirms visual fit OK**

If too tall / too small / unreadable / aspect bad: report back, controller iterates Task 1 CSS.
If OK: proceed to Task 4 (commit).

---

## Task 4: Commit PNG Insertion + Update READMEs

**Files:**
- Modify: `docs/pcp-HCPortal-2026/Risalah Web.pptx` (PNG inserted by user)
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md`
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`

- [ ] **Step 1: Update slide8 README (mark panel-rencana SHIPPED)**

Read `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md`. Find the line:

```markdown
- (future) `panel-rencana-compact.html` — placeholder #2 RENCANA PEMBUATAN
```

Replace with:

```markdown
- `panel-rencana-compact.html` — placeholder #2 RENCANA PEMBUATAN (System focus, 450×148)
```

- [ ] **Step 2: Append changelog entry to parent README**

Read `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`. Find:

```markdown
## Changelog

### v3.7-slide8 — Versi P Compact untuk Risalah Web slide 8 (2026-05-22)
```

Prepend a new entry above:

```markdown
## Changelog

### v3.7-slide8-p2 — Panel Rencana Pembuatan System untuk slide 8 (2026-05-22)

- New: `slide8/panel-rencana-compact.html` (450×148, aspect 3.03:1, match box pptx 11.92×3.93 cm)
- Focus **System** dimension: drop `metode/` prefix + `/fabrikasi alat` (web project ≠ alat fisik)
- Strip 1: 6 tech stack badge — `.NET 8`, `ASP.NET Core MVC`, `EF Core 8`, `SQL Server`, `SignalR`, `Bootstrap`
- Strip 2a: Deployment flow `💻 Lokal → 🛠️ Dev (10.55.3.3) → 🚀 Prod (IIS Windows)`
- Strip 2b: Metode footer `👥 Internal Gugus PROTON · ⏱️ ~1 bulan · 📋 SOP DEV_WORKFLOW`
- Border-top yellow (signal "planning/rencana"), bedain dengan placeholder #1 dual red/green
- Export PNG via html2canvas @2x retina
- Inserted ke `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8 placeholder #2
- Spec: `docs/superpowers/specs/2026-05-22-pcp-slide8-panel-rencana-design.md`
- Plan: `docs/superpowers/plans/2026-05-22-pcp-slide8-panel-rencana-implementation.md`

### v3.7-slide8 — Versi P Compact untuk Risalah Web slide 8 (2026-05-22)
```

- [ ] **Step 3: Stage + commit pptx + READMEs + spec + plan**

```bash
git add "docs/pcp-HCPortal-2026/Risalah Web.pptx" \
        "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md" \
        "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md" \
        "docs/superpowers/specs/2026-05-22-pcp-slide8-panel-rencana-design.md" \
        "docs/superpowers/plans/2026-05-22-pcp-slide8-panel-rencana-implementation.md"

git commit -m "docs(pcp-slide8): ship panel rencana PNG ke Risalah Web slide 8 + spec/plan/changelog"
```

- [ ] **Step 4: Tag v1.1**

```bash
git tag pcp-hcportal-3.4-slide8-v1.1
git tag --list "pcp-hcportal-3.4-slide8-*"
```

Expected output:
```
pcp-hcportal-3.4-slide8-v1.0
pcp-hcportal-3.4-slide8-v1.1
```

---

## Verification Checklist (Definition of Done)

After Tasks 1–4 complete:

- [ ] File `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/panel-rencana-compact.html` exists
- [ ] Body width = 450px (DevTools verified)
- [ ] Panel-wrap height ≤ 160px
- [ ] 6 badges visible single row (no wrap) with distinct colors
- [ ] 3 flow nodes (Lokal, Dev, Prod) + 2 arrows readable
- [ ] Metode footer 1-liner visible italic gray
- [ ] Yellow border-top on card
- [ ] Export PNG button hidden in PNG (display:none during capture)
- [ ] PNG ~900×~296 inserted into pptx slide 8 placeholder #2
- [ ] Visual fit in slide acceptable (user confirmed)
- [ ] No modify ke `versi-p-compact.html` placeholder #1
- [ ] Changelog v3.7-slide8-p2 added to parent README
- [ ] Tag `pcp-hcportal-3.4-slide8-v1.1` created

---

## Out of Scope (Confirmed)

- Spec/build placeholder #3 (Standard Design) — separate plan
- Modify placeholder #1 `versi-p-compact.html`
- Architecture deep-dive diagram
- Service-by-service breakdown (AuditLog, Grading, Notification, etc.)
- Auth strategy detail (LDAP/AD hybrid)
- Print stylesheet
- Responsive breakpoints
- HTMX badge (Phase 311 direction noted, dropped for slide compactness per spec §9)

---

## Recovery

If badge wraps or visual fit fails after Task 2 verify:

**Common fixes:**
- Badge wrap: shorten `ASP.NET Core MVC` → `ASP.NET MVC`, or reduce `.badge` font-size 8.5 → 8
- Panel too tall: tighten strip padding 4 → 3, or reduce `.flow-node` padding 3 8 → 2 6
- Aspect mismatch: adjust `#panel-wrap` padding from 4 to 3 or 2
- Color contrast weak in PNG: bump badge background opacity (e.g., use slightly darker `--stack-*` color variants)

Apply fix in Task 1 file, re-verify Task 2, re-export Task 3, re-insert.

If full rollback needed:

```bash
git reset --hard pcp-hcportal-3.4-slide8-v1.0
```

Master Versi P recoverable via tag at any time.
