# -*- coding: utf-8 -*-
import io

path = r"C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Desktop\PortalHC_KPB\.planning\ROADMAP.md"
CONSTR = "\U0001F6A7"  # construction emoji
CHECK = "✅"       # check mark emoji

with io.open(path, "r", encoding="utf-8") as f:
    text = f.read()

# --- Edit 1: Milestones bullet list (mark v32.0 complete + add v32.2) ---
old_ms = (
    "- " + CONSTR + " **v32.0 Manajemen Peserta** — Phases 391-392 (STARTED 2026-06-17, ACTIVE; 7/7 REQ PART-01..04 + WRKR-01..03; **0 migration**; 391 = penambahan peserta fleksibel saat ujian berjalan, 392 = perbaikan CreateWorker view + audit field; branch main, NOT PUSHED)\n\n## Phases"
)
new_ms = (
    "- " + CHECK + " **v32.0 Manajemen Peserta** — Phases 391-392 (COMPLETE local 2026-06-17; 7/7 REQ PART-01..04 + WRKR-01..03; **0 migration**; 391 = penambahan peserta fleksibel saat ujian berjalan, 392 = perbaikan CreateWorker view + audit field; branch main, NOT PUSHED)\n"
    "- " + CONSTR + " **v32.2 Inject Hasil Assessment Manual (\"Seakan Online\")** — Phases 393-398 (STARTED 2026-06-17, ACTIVE; 13/13 REQ INJ-01..13; **0 migration**; page baru `/Admin/InjectAssessment` Section C [Admin+HC] meng-inject hasil assessment identik online via reuse GradingService/authoring/CertNumberHelper; branch main, NOT PUSHED) — [spec](../docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md)\n\n## Phases"
)
assert old_ms in text, "Edit 1 anchor (milestones list) not found"
text = text.replace(old_ms, new_ms, 1)

# --- Edit 2: demote current v32.0 active block from <details open> to <details> + inject v32.2 above ---
old_v32_open = (
    "## Phases\n\n"
    "<details open>\n"
    "<summary>" + CONSTR + " v32.0 Manajemen Peserta (Phases 391-392) — STARTED 2026-06-17 — ACTIVE</summary>"
)
new_v32_closed = (
    "## Phases\n\n"
    "__V32_2_BLOCK__\n\n"
    "---\n\n"
    "<details>\n"
    "<summary>" + CHECK + " v32.0 Manajemen Peserta (Phases 391-392) — COMPLETE local 2026-06-17 — 7/7 REQ — 0 migration</summary>"
)
assert old_v32_open in text, "Edit 2 anchor (v32.0 details open) not found"
text = text.replace(old_v32_open, new_v32_closed, 1)

block_path = r"C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Desktop\PortalHC_KPB\.planning\_v322_block.md"
with io.open(block_path, "r", encoding="utf-8") as f:
    v32_2 = f.read()

text = text.replace("__V32_2_BLOCK__", v32_2, 1)

with io.open(path, "w", encoding="utf-8") as f:
    f.write(text)

print("OK: ROADMAP.md patched")
print("contains v32.2 details open:", "<summary>" + CONSTR + " v32.2 Inject Hasil Assessment Manual" in text)
print("contains v32.0 collapsed:", "<summary>" + CHECK + " v32.0 Manajemen Peserta (Phases 391-392) — COMPLETE" in text)
print("contains Phase 393 detail:", "### Phase 393: Backend core inject" in text)
print("contains Phase 398 detail:", "### Phase 398: Test + UAT" in text)
print("INJ rows in coverage table:", text.count("| INJ-"))
print("placeholder remaining:", "__V32_2_BLOCK__" in text)
