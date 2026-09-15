# -*- coding: utf-8 -*-
"""Render diagram PNG (matplotlib) + konversi PANDUAN_LENGKAP_APLIKASI.md -> .docx."""
import os, re
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch

DOCS = os.path.dirname(os.path.abspath(__file__))
IMG  = os.path.join(DOCS, "diagrams")
os.makedirs(IMG, exist_ok=True)

# ---------- helper diagram ----------
def box(ax, x, y, w, h, text, fc="#dae8fc", ec="#6c8ebf", fs=10, bold=False):
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.08",
                                linewidth=1.3, edgecolor=ec, facecolor=fc))
    ax.text(x+w/2, y+h/2, text, ha="center", va="center", fontsize=fs,
            fontweight="bold" if bold else "normal", wrap=True)

def arrow(ax, x1, y1, x2, y2, dashed=False, label=None):
    ax.add_patch(FancyArrowPatch((x1, y1), (x2, y2), arrowstyle="-|>", mutation_scale=14,
                                 linewidth=1.2, color="#444444",
                                 linestyle="--" if dashed else "-"))
    if label:
        ax.text((x1+x2)/2, (y1+y2)/2, label, fontsize=8, color="#333",
                ha="center", va="center", backgroundcolor="white")

def save(fig, name):
    fig.savefig(os.path.join(IMG, name), dpi=140, bbox_inches="tight", facecolor="white")
    plt.close(fig)

# 1. Arsitektur
fig, ax = plt.subplots(figsize=(10, 6)); ax.set_xlim(0, 10); ax.set_ylim(0, 6); ax.axis("off")
ax.text(5, 5.8, "Arsitektur Aplikasi", ha="center", fontsize=15, fontweight="bold")
box(ax, 0.3, 4.6, 9.4, 0.9, "OBJEK GLOBAL (Singleton + DontDestroyOnLoad)", fc="#dae8fc", ec="#6c8ebf", bold=True)
for i,(t) in enumerate(["GameManager","ChapterManager","SceneTransition","AudioManager"]):
    box(ax, 0.5+i*2.35, 4.75, 2.1, 0.55, t, fc="#ffffff", ec="#6c8ebf", fs=9)
box(ax, 0.3, 2.9, 2.6, 1.2, "01_MainMenu\nMainMenuController\nSettingsPanel", fc="#d5e8d4", ec="#82b366", bold=True, fs=9)
box(ax, 3.2, 2.4, 6.5, 1.7, "00_Sekolah (Overworld)\nPlayerTopDown  •  InteractSystem\nNPCController×6  •  DialogSystem\nObjectiveHUD  •  CameraFollow", fc="#d5e8d4", ec="#82b366", bold=True, fs=9)
box(ax, 3.2, 0.9, 6.5, 1.0, "MG_* Mini-game\nMiniGameManager: Intro→Learn→Trace→Quiz→Result", fc="#ffe6cc", ec="#d79b00", bold=True, fs=9)
box(ax, 0.3, 1.0, 2.3, 0.9, "PlayerPrefs\n(progres)", fc="#f8cecc", ec="#b85450", fs=9)
arrow(ax, 5, 4.6, 6.4, 4.1)
arrow(ax, 6.4, 2.4, 6.4, 1.9, label="mulai")
arrow(ax, 3.2, 1.4, 2.6, 1.4, dashed=True, label="simpan")
save(fig, "arch.png")

# 2. Flow end-to-end (vertikal)
fig, ax = plt.subplots(figsize=(6.5, 9)); ax.set_xlim(0, 6.5); ax.set_ylim(0, 9); ax.axis("off")
steps = ["Buka game","01_MainMenu (Mulai/Lanjutkan)","00a_NamaKarakter (nama+gender)",
         "00_Sekolah — jalan (WASD) + temui NPC","Tekan E — Dialog + pilihan",
         "Mini-game (Learn/Trace/Quiz)","CompleteSubActivity() simpan skor",
         "4 aktivitas selesai? → CompleteChapter","LevelUpAnnouncer: 'Naik Level!'"]
cols = ["#d5e8d4","#dae8fc","#dae8fc","#fff2cc","#d5e8d4","#ffe6cc","#f8cecc","#d5e8d4","#dae8fc"]
y = 8.1
for i, s in enumerate(steps):
    box(ax, 1.0, y, 4.5, 0.62, s, fc=cols[i], ec="#666", fs=9)
    if i < len(steps)-1: arrow(ax, 3.25, y, 3.25, y-0.28)
    y -= 0.9
arrow(ax, 1.0, 0.9+0.31, 0.5, 5.0, dashed=True); ax.text(0.35,3.0,"ulang\n(chapter\nberikutnya)",fontsize=8,rotation=90,va="center")
save(fig, "flow.png")

# 3. Loop belajar (horizontal)
fig, ax = plt.subplots(figsize=(11, 3.2)); ax.set_xlim(0, 11); ax.set_ylim(0, 3.2); ax.axis("off")
ax.text(5.5, 2.95, "Satu Siklus Belajar (per Chapter)", ha="center", fontsize=14, fontweight="bold")
labels = ["SINTA\nMembaca","UCUP\nMenulis","NABILA\nPelafalan","GURU\nRefleksi/Ujian"]
cols = ["#d5e8d4","#d5e8d4","#d5e8d4","#ffe6cc"]
for i,l in enumerate(labels):
    box(ax, 0.4+i*2.6, 1.6, 2.1, 0.8, l, fc=cols[i], ec="#82b366", bold=True, fs=10)
    if i<3: arrow(ax, 0.4+i*2.6+2.1, 2.0, 0.4+(i+1)*2.6, 2.0)
box(ax, 2.0, 0.4, 7.0, 0.7, "CompleteChapter → unlock berikutnya (Swara→Ngalagena1→Ngalagena2→Rarangken→UjianFinal)", fc="#dae8fc", ec="#6c8ebf", fs=9)
arrow(ax, 10.0, 1.6, 10.0, 1.1, dashed=True)
save(fig, "loop.png")

# 4. Pipeline gerak
fig, ax = plt.subplots(figsize=(9, 5)); ax.set_xlim(0, 9); ax.set_ylim(0, 5); ax.axis("off")
ax.text(4.5, 4.7, "Pipeline Gerak Karakter (per frame)", ha="center", fontsize=14, fontweight="bold")
box(ax, 0.3, 3.6, 2.2, 0.7, "Tekan WASD/Panah", fc="#d5e8d4", ec="#82b366", fs=9)
box(ax, 3.0, 3.5, 2.6, 0.9, "Update()\nbaca input\n_input=(x,y).normalized", fc="#fff2cc", ec="#d6b656", fs=9)
box(ax, 6.1, 3.5, 2.6, 0.9, "Set Animator\nMoveX/Y, IsMoving\n(sumbu dominan)", fc="#dae8fc", ec="#6c8ebf", fs=9)
box(ax, 3.0, 2.1, 2.9, 0.8, "FixedUpdate()\nrb.velocity=_input*speed", fc="#ffe6cc", ec="#d79b00", fs=9)
box(ax, 3.0, 1.0, 2.9, 0.7, "Rigidbody2D gerak\n(+tabrakan dinding)", fc="#f8cecc", ec="#b85450", fs=9)
box(ax, 3.0, 0.05, 2.9, 0.65, "LateUpdate() Kamera lerp", fc="#e1d5e7", ec="#9673a6", fs=9)
box(ax, 6.3, 2.15, 2.5, 0.7, "Tekan E →\nInteractSystem", fc="#d5e8d4", ec="#82b366", fs=9)
arrow(ax, 2.5, 3.95, 3.0, 3.95); arrow(ax, 5.6, 3.95, 6.1, 3.95)
arrow(ax, 4.4, 3.5, 4.4, 2.9); arrow(ax, 4.4, 2.1, 4.4, 1.7); arrow(ax, 4.4, 1.0, 4.4, 0.7)
arrow(ax, 5.9, 3.5, 6.3, 2.85, dashed=True)
save(fig, "move.png")

print("PNG diagrams done")

# ---------- markdown -> docx ----------
from docx import Document
from docx.shared import Pt, RGBColor, Inches
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

MD = os.path.join(DOCS, "PANDUAN_LENGKAP_APLIKASI.md")
with open(MD, "r", encoding="utf-8") as f:
    lines = f.read().split("\n")

doc = Document()
# base style
st = doc.styles["Normal"]; st.font.name = "Calibri"; st.font.size = Pt(10.5)

# heading->image map (substring match)
img_after = {
    "Peta arsitektur": "arch.png",
    "Alur hidup aplikasi": "flow.png",
    "Cara menggerakkan karakter": "move.png",
}
inserted_loop = [False]

def shade(par, color="F2F2F2"):
    pPr = par._p.get_or_add_pPr()
    shd = OxmlElement("w:shd"); shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto"); shd.set(qn("w:fill"), color)
    pPr.append(shd)

def add_inline(par, text):
    # strip links [t](u)->t ; handle **bold** and `code`
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)
    tokens = re.split(r"(\*\*[^*]+\*\*|`[^`]+`)", text)
    for tk in tokens:
        if not tk: continue
        if tk.startswith("**") and tk.endswith("**"):
            r = par.add_run(tk[2:-2]); r.bold = True
        elif tk.startswith("`") and tk.endswith("`"):
            r = par.add_run(tk[1:-1]); r.font.name = "Consolas"; r.font.size = Pt(9.5)
            r.font.color.rgb = RGBColor(0xC0, 0x30, 0x30)
        else:
            par.add_run(tk)

def add_code(buf):
    p = doc.add_paragraph(); shade(p)
    r = p.add_run("\n".join(buf)); r.font.name = "Consolas"; r.font.size = Pt(8.5)
    p.paragraph_format.space_before = Pt(3); p.paragraph_format.space_after = Pt(3)

def add_table(rows):
    # rows: list of list of cell strings; first row header
    ncol = max(len(r) for r in rows)
    t = doc.add_table(rows=0, cols=ncol); t.style = "Light Grid Accent 1"
    for ri, row in enumerate(rows):
        cells = t.add_row().cells
        for ci in range(ncol):
            val = row[ci] if ci < len(row) else ""
            cells[ci].text = ""
            add_inline(cells[ci].paragraphs[0], val.strip())
            if ri == 0:
                for run in cells[ci].paragraphs[0].runs: run.bold = True

i = 0
code_buf = None
tbl_buf = None
while i < len(lines):
    ln = lines[i]
    # code fences
    if ln.strip().startswith("```"):
        if code_buf is None: code_buf = []
        else: add_code(code_buf); code_buf = None
        i += 1; continue
    if code_buf is not None:
        code_buf.append(ln); i += 1; continue
    # tables
    if ln.strip().startswith("|"):
        if tbl_buf is None: tbl_buf = []
        # skip separator row |---|
        if not re.match(r"^\s*\|[\s:\-|]+\|\s*$", ln):
            tbl_buf.append([c for c in ln.strip().strip("|").split("|")])
        i += 1
        if i >= len(lines) or not lines[i].strip().startswith("|"):
            add_table(tbl_buf); tbl_buf = None
        continue
    # headings
    m = re.match(r"^(#{1,6})\s+(.*)$", ln)
    if m:
        lvl = len(m.group(1)); txt = m.group(2).strip()
        txt = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", txt)
        h = doc.add_heading(txt, level=min(lvl, 4))
        for key, img in img_after.items():
            if key in txt:
                doc.add_picture(os.path.join(IMG, img), width=Inches(6.2))
                doc.paragraphs[-1].alignment = WD_ALIGN_PARAGRAPH.CENTER
                if img == "flow.png" and not inserted_loop[0]:
                    doc.add_picture(os.path.join(IMG, "loop.png"), width=Inches(6.2))
                    doc.paragraphs[-1].alignment = WD_ALIGN_PARAGRAPH.CENTER
                    inserted_loop[0] = True
        i += 1; continue
    # hr
    if ln.strip() == "---":
        i += 1; continue
    # blockquote
    if ln.strip().startswith(">"):
        p = doc.add_paragraph(); p.paragraph_format.left_indent = Inches(0.3)
        r = p.add_run(ln.strip()[1:].strip()); r.italic = True
        r.font.color.rgb = RGBColor(0x55, 0x55, 0x55)
        i += 1; continue
    # bullet
    if re.match(r"^\s*[-*]\s+", ln):
        p = doc.add_paragraph(style="List Bullet")
        add_inline(p, re.sub(r"^\s*[-*]\s+", "", ln)); i += 1; continue
    # numbered
    if re.match(r"^\s*\d+\.\s+", ln):
        p = doc.add_paragraph(style="List Number")
        add_inline(p, re.sub(r"^\s*\d+\.\s+", "", ln)); i += 1; continue
    # blank
    if ln.strip() == "":
        i += 1; continue
    # normal paragraph
    p = doc.add_paragraph(); add_inline(p, ln)
    i += 1

# title page tweak: make first heading a title
out = os.path.join(DOCS, "PANDUAN_LENGKAP_APLIKASI.docx")
doc.save(out)
print("DOCX saved:", out)
