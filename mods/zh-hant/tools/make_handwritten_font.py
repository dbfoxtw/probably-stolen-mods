r"""產生發布版的手寫字型：佑字 肅（Yuji Syuku，SIL OFL 1.1）補上繁中缺的字，並調整刪節號。

遊戲的手寫字型用在簽名與紙條，原版是只收簡體的馬善政體（毛筆）。佑字是書法家片岡佑之的毛筆字，風格最接近；
簽名與紙條用到、佑字沒有的字（Demo 的「你喔嗎汙」、正式版新紙條的「卡垃圾幫撐跑躲韌」），這裡用它自己的部件拼出來：
從別的字取出部件的輪廓（左右拼取偏旁、上下拼取上半或下半，或整個字當部件），縮放、移位後組成新字；
也可以直接沿用寫法相近的日文異體字（例如拿「躱」當「躲」）。刪節號「…」的三個點擠在中間，兩個連用（……）時中間會斷開，
所以改成平均分布在整個字寬。

佑字沒有保留字型名稱（Reserved Font Name），OFL 允許修改後散布；修改版仍是 OFL。字型名稱改成 Yuji Syuku ZhHant，
授權檔附上原作者的版權聲明、修改說明與 OFL 全文。

原檔：https://github.com/google/fonts/tree/main/ofl/yujisyuku（YujiSyuku-Regular.ttf 與 OFL.txt，v3.002）
用法：python tools/make_handwritten_font.py [--src 原檔] [--ofl OFL.txt] [--out 輸出資料夾]
  預設讀 fonts/candidates/YujiSyuku-Regular.ttf 與 fonts/candidates/YujiSyuku-OFL.txt，
  輸出 fonts/handwritten.ttf 與 fonts/handwritten.LICENSE.txt（安裝與打包預設用這兩個檔）。
需要 fonttools（pip install -r requirements.txt）。
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
FAMILY, NEW_FAMILY = "Yuji Syuku", "Yuji Syuku ZhHant"
PS, NEW_PS = "YujiSyuku", "YujiSyukuZhHant"
EM = 1000  # 佑字的漢字字寬（unitsPerEm 也是 1000）；右半部以右緣為基準縮放


@dataclass(frozen=True)
class Part:
    """新字的一個部件：取 src 這個字裡、中心落在 x／y 範圍內的輪廓，以 anchor 為基準縮放 scale 倍，再平移 move。

    範圍是輪廓外框中心的開區間，None 表示不限；這套字常把好幾筆連成一個輪廓，部件拆不拆得開要看輪廓，
    拆不開就整個字拿來當部件（範圍都不限）。expect 是 v3.002 取到的輪廓數，對不上代表原檔改了，要重新調整。
    """
    src: str
    expect: int
    x: tuple[float | None, float | None] = (None, None)
    y: tuple[float | None, float | None] = (None, None)
    scale: tuple[float, float] = (1.0, 1.0)
    anchor: tuple[float, float] = (0, 0)
    move: tuple[float, float] = (0, 0)

    def transform(self) -> tuple:
        (sx, sy), (ax, ay), (mx, my) = self.scale, self.anchor, self.move
        return (sx, 0, 0, sy, ax * (1 - sx) + mx, ay * (1 - sy) + my)


def left(src: str, xmax: float, expect: int, sx: float = 1.0) -> Part:
    """左右拼的左半：中心 x < xmax 的輪廓，以 x=0 為基準縮窄。"""
    return Part(src, expect, x=(None, xmax), scale=(sx, 1.0))


def right(src: str, xmin: float, expect: int, sx: float = 1.0) -> Part:
    """左右拼的右半：中心 x > xmin 的輪廓，以右緣為基準縮窄。"""
    return Part(src, expect, x=(xmin, None), scale=(sx, 1.0), anchor=(EM, 0))


# 新字的做法：一串部件（Part），或一個字（直接沿用那個字的字形）。字寬照第一個部件的字。
# 部件的輪廓中心與座標：漢字的字身大約是 x 0～1000、y -120～880。
RECIPES: dict[str, tuple[Part, ...] | str] = {
    "你": (left("他", 330, 1), right("弥", 420, 3)),                # 亻＋尓
    "喔": (left("呼", 300, 2, 0.72), right("渥", 400, 3, 0.88)),    # 口＋屋（這套字的口很寬，要縮窄；「握」的扌和屋連成一筆，拆不開）
    "嗎": (left("喝", 300, 2), right("媽", 380, 8)),                # 口＋馬
    "汙": (left("汗", 300, 3), right("紆", 480, 1)),                # 氵＋于
    # 以下是正式版新紙條用到的字。上下拼的部件要壓扁，橫畫會跟著變細，比例取在放進句子裡不突兀的地方。
    # 這套字常把相鄰的筆畫連成一個輪廓（「路」「跳」的足連著右邊、「常」「掌」的口連著下面、「韜」的韋連著右邊），
    # 拆不開的部件改從別的字取，或整個字拿來縮放。
    # 卡＝上＋卜：上的長橫當中間那一橫，卜右移對齊上的直畫
    "卡": (Part("上", 1, scale=(0.95, 0.6), anchor=(500, 810)),
           Part("卜", 1, scale=(0.92, 0.5), anchor=(500, -110), move=(55, 0))),
    "垃": (left("地", 350, 1), right("拉", 420, 2)),                # 土＋立
    "圾": (left("坑", 350, 1), right("汲", 400, 2, 0.88)),          # 土＋及（及的撇往左伸，縮窄才不會碰到土的提）
    # 幫＝封＋帛（日文的「幇」下面是巾，少了白，所以不沿用）
    "幫": (Part("封", 3, scale=(0.85, 0.47), anchor=(500, 800)),
           Part("帛", 2, scale=(1.0, 0.55), anchor=(500, -110))),
    # 撐＝扌＋牚，牚＝「賞」的上半（⺌冖口）＋「冴」的牙，縮窄放到右邊
    "撐": (left("抱", 420, 1),
           Part("賞", 3, y=(300, None), scale=(0.62, 0.85), anchor=(EM, 814), move=(0, -4)),
           Part("冴", 1, x=(600, None), scale=(0.8, 0.55), anchor=(EM, -100))),
    "跑": (left("跪", 350, 2), right("抱", 420, 2)),                # 足＋包
    "躲": "躱",                                                      # 沿用日文寫法（右上的几寫得像乃），和繁中幾乎一樣
    "韌": (Part("韋", 5, scale=(0.55, 1.0)), right("仞", 400, 1, 0.85)),  # 整個韋縮窄＋「仞」的刃（「靭」的左邊是革，不能直接用）
}
ELLIPSIS = "…"


class Font:
    def __init__(self, path: Path):
        self.tt = TTFont(str(path))
        self.cmap = self.tt.getBestCmap()
        self.gs = self.tt.getGlyphSet()

    def contours(self, ch: str) -> list[list[tuple[str, tuple]]]:
        """一個字的輪廓（組合字形先拆開），每個輪廓是一串畫筆指令。"""
        if ord(ch) not in self.cmap:
            raise SystemExit(f"原檔沒有「{ch}」，補字的做法要重新調整")
        pen = DecomposingRecordingPen(self.gs)
        self.gs[self.cmap[ord(ch)]].draw(pen)
        out, cur = [], []
        for op, args in pen.value:
            cur.append((op, args))
            if op in ("closePath", "endPath"):
                out.append(cur)
                cur = []
        return out

    def bounds(self, contour) -> tuple[float, float, float, float]:
        bp = BoundsPen(self.gs)
        for op, args in contour:
            getattr(bp, op)(*args)
        return bp.bounds

    def set_glyph(self, name: str, parts: list[tuple[list, tuple]], like: str) -> None:
        """用 (輪廓, 仿射變換) 組出字形，寫進 glyf；水平與垂直度量照 like 這個字形，左側空白依新字形重算。"""
        pen = TTGlyphPen(None)
        for contour, t in parts:
            tp = TransformPen(pen, t)
            for op, args in contour:
                getattr(tp, op)(*args)
        glyf = self.tt["glyf"]
        if name not in glyf:
            self.tt.setGlyphOrder(self.tt.getGlyphOrder() + [name])
        g = pen.glyph()
        glyf[name] = g
        g.recalcBounds(glyf)
        adv, _ = self.tt["hmtx"][like]
        self.tt["hmtx"][name] = (adv, g.xMin)
        if "vmtx" in self.tt:
            vadv, vtsb = self.tt["vmtx"][like]
            like_ymax = glyf[like].yMax if hasattr(glyf[like], "yMax") else g.yMax
            self.tt["vmtx"][name] = (vadv, vtsb + like_ymax - g.yMax)


def inside(v: float, lo: float | None, hi: float | None) -> bool:
    return (lo is None or v > lo) and (hi is None or v < hi)


def compose(f: Font, recipes: dict[str, tuple[Part, ...] | str] = RECIPES) -> None:
    for new, recipe in recipes.items():
        if ord(new) in f.cmap:
            raise SystemExit(f"原檔已經有「{new}」，不用再補（原檔可能更新了）")
        if isinstance(recipe, str):
            # 沿用異體字：cmap 直接指到那個字的字形，不另做一份
            name = f.cmap.get(ord(recipe))
            if name is None:
                raise SystemExit(f"原檔沒有「{recipe}」，補字的做法要重新調整")
            desc = f"沿用「{recipe}」的字形"
        else:
            parts, got = [], []
            for p in recipe:
                picked = [c for c, b in ((c, f.bounds(c)) for c in f.contours(p.src))
                          if inside((b[0] + b[2]) / 2, *p.x) and inside((b[1] + b[3]) / 2, *p.y)]
                if len(picked) != p.expect:
                    raise SystemExit(f"「{new}」從「{p.src}」取到 {len(picked)} 個輪廓，預期 {p.expect}，原檔可能改了")
                parts += [(c, p.transform()) for c in picked]
                got.append(f"{p.src} {len(picked)} 筆")
            name = f"uni{ord(new):04X}"
            f.set_glyph(name, parts, like=f.cmap[ord(recipe[0].src)])
            desc = "＋".join(got)
        for t in f.tt["cmap"].tables:
            if t.isUnicode() and t.format in (4, 12):
                t.cmap[ord(new)] = name
        print(f"補字：{new} ← {desc}")
    f.cmap = f.tt.getBestCmap()


def respace_ellipsis(f: Font) -> None:
    """「…」的三個點平均分布在整個字寬（中心在 1/6、3/6、5/6），連用時點距一致。"""
    name = f.cmap[ord(ELLIPSIS)]
    dots = sorted(f.contours(ELLIPSIS), key=lambda c: f.bounds(c)[0])
    if len(dots) != 3:
        raise SystemExit(f"「…」預期 3 個點，實際 {len(dots)} 個，原檔可能改了")
    adv = f.tt["hmtx"][name][0]
    parts = []
    for i, c in enumerate(dots):
        b = f.bounds(c)
        parts.append((c, (1, 0, 0, 1, adv * (2 * i + 1) / 6 - (b[0] + b[2]) / 2, 0)))
    f.set_glyph(name, parts, like=name)
    print(f"刪節號：三個點的中心改到 {adv // 6}、{adv // 2}、{adv * 5 // 6}（字寬 {adv}）")


def rename(f: Font) -> None:
    """修改版改名（OFL 建議），版本加註；原作者版權聲明不動。"""
    for rec in f.tt["name"].names:
        s = rec.toUnicode()
        if rec.nameID in (1, 4, 16, 18):
            s = s.replace(FAMILY, NEW_FAMILY)
        elif rec.nameID in (3, 6):
            s = s.replace(PS, NEW_PS)
        elif rec.nameID == 5:
            s = f"{s}; ZhHant 1"
        else:
            continue
        rec.string = s
    f.tt["name"].setName(f"Yuji Syuku with {len(RECIPES)} Traditional Chinese glyphs ({''.join(RECIPES)}) composed from "
                         "its own glyphs and a respaced ellipsis, for the Probably Stolen Traditional Chinese mod.",
                         10, 3, 1, 0x409)


def license_text(ofl: Path) -> str:
    """原作者的版權聲明＋修改說明＋OFL 全文（取自原檔附的 OFL.txt）。"""
    text = ofl.read_text(encoding="utf-8-sig")
    head, sep, body = text.partition("-----")
    if not sep:
        raise SystemExit(f"{ofl} 看起來不是 OFL 授權檔")
    note = ("\nThis is a modified version (\"Yuji Syuku ZhHant\") made for the Probably Stolen\n"
            "Traditional Chinese mod (https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/zh-hant):\n"
            f"{len(RECIPES)} Traditional Chinese glyphs ({''.join(RECIPES)}) were composed from the font's own glyphs\n"
            + ("(or mapped to an existing Japanese variant glyph), " if any(isinstance(r, str) for r in RECIPES.values())
               else "")
            + "and the ellipsis (…) was respaced.\n"
            "The modified version is also licensed under the SIL Open Font License, Version 1.1.\n\n\n")
    return head.rstrip() + "\n" + note + sep + body


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", type=Path, default=ROOT / "fonts" / "candidates" / "YujiSyuku-Regular.ttf")
    ap.add_argument("--ofl", type=Path, default=ROOT / "fonts" / "candidates" / "YujiSyuku-OFL.txt")
    ap.add_argument("--out", type=Path, default=ROOT / "fonts")
    args = ap.parse_args()
    f = Font(args.src)
    compose(f)
    respace_ellipsis(f)
    rename(f)
    args.out.mkdir(parents=True, exist_ok=True)
    out = args.out / "handwritten.ttf"
    f.tt.save(str(out))
    (args.out / "handwritten.LICENSE.txt").write_text(license_text(args.ofl), encoding="utf-8", newline="\n")
    print(f"→ {out}（{out.stat().st_size / 1e6:.1f} MB）與 {out.with_name('handwritten.LICENSE.txt').name}")


if __name__ == "__main__":
    main()
