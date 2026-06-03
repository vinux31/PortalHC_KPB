"""Build Gambar 1 PROTON flowchart untuk paten Portal HC KPB.

Mirror paten DCS reference flowchart style:
- Oval terminator (Mulai/Selesai)
- Parallelogram input/output
- Rectangle proses (rounded) dengan bullet detail
- Diamond decision
- Top-down flow, loop arrows curved
"""
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, Ellipse, Polygon, FancyArrowPatch
import matplotlib

matplotlib.rcParams['font.family'] = 'Times New Roman'

OUT = "docs/abstract/gambar1-proton.png"

fig, ax = plt.subplots(figsize=(8.5, 14), dpi=200)
ax.set_xlim(-1, 11)
ax.set_ylim(-1, 21)
ax.set_aspect('equal')
ax.axis('off')

CX = 5.0
LINE_KW = dict(color='black', linewidth=1.0)
NODE_FILL = dict(fc='white', ec='black', linewidth=1.0)


def draw_oval(cx, cy, w, h, text):
    e = Ellipse((cx, cy), w, h, **NODE_FILL)
    ax.add_patch(e)
    ax.text(cx, cy, text, ha='center', va='center',
            fontsize=10, family='Times New Roman')


def draw_rect(cx, cy, w, h, title, bullets):
    rect = FancyBboxPatch(
        (cx - w / 2, cy - h / 2), w, h,
        boxstyle="round,pad=0.03,rounding_size=0.08",
        **NODE_FILL,
    )
    ax.add_patch(rect)
    ax.text(cx, cy + h / 2 - 0.22, title, ha='center', va='center',
            fontsize=9, weight='bold', family='Times New Roman')
    for i, b in enumerate(bullets):
        ax.text(cx - w / 2 + 0.18, cy + h / 2 - 0.55 - i * 0.27,
                f"• {b}", ha='left', va='top', fontsize=8,
                family='Times New Roman')


def draw_parallelogram(cx, cy, w, h, text):
    skew = 0.3
    pts = [
        (cx - w / 2 + skew, cy + h / 2),
        (cx + w / 2,        cy + h / 2),
        (cx + w / 2 - skew, cy - h / 2),
        (cx - w / 2,        cy - h / 2),
    ]
    p = Polygon(pts, closed=True, **NODE_FILL)
    ax.add_patch(p)
    ax.text(cx, cy, text, ha='center', va='center',
            fontsize=9, family='Times New Roman')


def draw_diamond(cx, cy, w, h, text):
    pts = [(cx, cy + h / 2), (cx + w / 2, cy),
           (cx, cy - h / 2), (cx - w / 2, cy)]
    p = Polygon(pts, closed=True, **NODE_FILL)
    ax.add_patch(p)
    ax.text(cx, cy, text, ha='center', va='center',
            fontsize=9, weight='bold', family='Times New Roman')


def arrow(x1, y1, x2, y2):
    a = FancyArrowPatch((x1, y1), (x2, y2),
                        arrowstyle='-|>', mutation_scale=14,
                        color='black', linewidth=1.0,
                        shrinkA=0, shrinkB=0)
    ax.add_patch(a)


def loop_arrow(x1, y1, x2, y2, curve_x, label=None):
    """Curve arrow from (x1,y1) -> via (curve_x, mid_y) -> (x2, y2)."""
    mid_y = (y1 + y2) / 2
    a1 = FancyArrowPatch((x1, y1), (curve_x, y1),
                         arrowstyle='-', color='black', linewidth=1.0)
    a2 = FancyArrowPatch((curve_x, y1), (curve_x, y2),
                         arrowstyle='-', color='black', linewidth=1.0)
    a3 = FancyArrowPatch((curve_x, y2), (x2, y2),
                         arrowstyle='-|>', mutation_scale=14,
                         color='black', linewidth=1.0)
    for a in (a1, a2, a3):
        ax.add_patch(a)
    if label:
        ax.text(curve_x + (0.18 if curve_x > 5 else -0.18), mid_y, label,
                ha='left' if curve_x > 5 else 'right',
                va='center', fontsize=8, style='italic',
                family='Times New Roman')


# === Coordinates (top to bottom) ===
W = 5.0    # rectangle width
H = 1.55   # rectangle height
DW = 2.0   # diamond width
DH = 1.5   # diamond height
PW = 4.5   # parallelogram width
PH = 0.9   # parallelogram height

Y_MULAI       = 20.0
Y_INPUT       = 18.7
Y_L1          = 16.9
Y_L2          = 14.6
Y_L3          = 12.3
Y_L4          = 10.0
Y_D1          = 7.9
Y_L5          = 5.7
Y_D2          = 3.6
Y_L6          = 1.4
Y_OUTPUT      = -0.2
Y_SELESAI     = -1.4

# === Nodes ===
draw_oval(CX, Y_MULAI, 1.6, 0.7, "Mulai")

draw_parallelogram(CX, Y_INPUT, PW, PH,
                   "Operator HC Admin (Input: Assign Coach + Track Coachee)")

draw_rect(CX, Y_L1, W, H, "Langkah 1: Assign Coach", [
    "HC Admin tunjuk Coach Senior",
    "Pilih Track: PROTON Maintenance / Operasi",
    "Mapping Coachee–Coach disimpan",
])

draw_rect(CX, Y_L2, W, H, "Langkah 2: Upload Bukti Deliverable", [
    "Studi Kasus (per project)",
    "Log Harian (weekly)",
    "Refleksi Coaching (monthly)",
])

draw_rect(CX, Y_L3, W, H, "Langkah 3: Catat Sesi Coaching", [
    "Diskusi",
    "Kesimpulan",
    "Tindak Lanjut",
])

draw_rect(CX, Y_L4, W, H, "Langkah 4: Approve 3-Tier", [
    "Sr Supervisor approve",
    "Section Head approve",
    "HC Final approve",
])

draw_diamond(CX, Y_D1, DW, DH, "Approved?")

draw_rect(CX, Y_L5, W, H, "Langkah 5: Final Assessment", [
    "Kuis 30 soal",
    "Durasi 60 menit",
    "Passing Grade (%)",
])

draw_diamond(CX, Y_D2, DW, DH, "Lulus?")

draw_rect(CX, Y_L6, W, H, "Langkah 6: Sertifikat Issued", [
    "Histori PROTON (rekam permanen)",
    "Sertifikat diterbitkan",
    "Level kompetensi naik",
])

draw_parallelogram(CX, Y_OUTPUT, PW, PH,
                   "Umpan Balik (Dashboard + Notifikasi on-login)")

draw_oval(CX, Y_SELESAI, 1.6, 0.7, "Selesai")

# === Linear arrows top-down ===
def y_top(y, h):  return y + h / 2
def y_bot(y, h):  return y - h / 2

# Mulai -> Input
arrow(CX, Y_MULAI - 0.35, CX, Y_INPUT + PH / 2)
# Input -> L1
arrow(CX, Y_INPUT - PH / 2, CX, Y_L1 + H / 2)
# L1 -> L2
arrow(CX, Y_L1 - H / 2, CX, Y_L2 + H / 2)
# L2 -> L3
arrow(CX, Y_L2 - H / 2, CX, Y_L3 + H / 2)
# L3 -> L4
arrow(CX, Y_L3 - H / 2, CX, Y_L4 + H / 2)
# L4 -> D1
arrow(CX, Y_L4 - H / 2, CX, Y_D1 + DH / 2)
# D1 YA -> L5
arrow(CX, Y_D1 - DH / 2, CX, Y_L5 + H / 2)
ax.text(CX + 0.15, (Y_D1 - DH / 2 + Y_L5 + H / 2) / 2, "Ya",
        ha='left', va='center', fontsize=8, style='italic',
        family='Times New Roman')
# L5 -> D2
arrow(CX, Y_L5 - H / 2, CX, Y_D2 + DH / 2)
# D2 YA -> L6
arrow(CX, Y_D2 - DH / 2, CX, Y_L6 + H / 2)
ax.text(CX + 0.15, (Y_D2 - DH / 2 + Y_L6 + H / 2) / 2, "Ya",
        ha='left', va='center', fontsize=8, style='italic',
        family='Times New Roman')
# L6 -> Output
arrow(CX, Y_L6 - H / 2, CX, Y_OUTPUT + PH / 2)
# Output -> Selesai
arrow(CX, Y_OUTPUT - PH / 2, CX, Y_SELESAI + 0.35)

# === Loop arrows ===

# D1 TIDAK -> back to L2 (left side)
loop_arrow(CX - DW / 2, Y_D1, CX - W / 2, Y_L2,
           curve_x=-0.2, label="Tidak (Rejected)")

# D2 TIDAK -> back to L5 (right side, with retake annotation)
loop_arrow(CX + DW / 2, Y_D2, CX + W / 2, Y_L5,
           curve_x=10.2, label="Tidak (Retake)")

# Umpan Balik -> back to Mulai (right side curve for next Track)
loop_arrow(CX + PW / 2, Y_OUTPUT, CX + 0.8, Y_MULAI,
           curve_x=9.6, label="Track berikutnya")

# === Caption ===
ax.text(CX, -2.2, "Gambar 1.  Diagram Alur Pelaksanaan PROTON (6 Langkah Terstruktur)",
        ha='center', va='center', fontsize=10, style='italic',
        family='Times New Roman')

plt.tight_layout()
plt.savefig(OUT, dpi=200, bbox_inches='tight', facecolor='white')
print(f"Saved: {OUT}")
