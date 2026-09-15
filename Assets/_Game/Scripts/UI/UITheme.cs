using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// DESIGN SYSTEM (token 3-lapis: primitive → semantic → component).
    /// Satu sumber kebenaran untuk warna, tipografi, spacing, radius, dan builder
    /// komponen UI (panel, tombol, teks, slider). Semua UI yang dibangun via kode
    /// memakai kelas ini agar tampilan konsisten & mudah diubah dari satu tempat.
    ///
    /// Tema: edukasi aksara Sunda — gelap hangat + aksen emas batik.
    /// </summary>
    public static class UITheme
    {
        // ── PRIMITIVE (nilai mentah) ─────────────────────────────────────────
        public static readonly Color Ink900  = Hex("#12151C"); // paling gelap (scrim solid)
        public static readonly Color Ink800  = Hex("#1B2130"); // panel dasar
        public static readonly Color Ink700  = Hex("#232B3D"); // surface
        public static readonly Color Ink600  = Hex("#2E3850"); // surface terangkat
        public static readonly Color Line    = Hex("#46536B"); // garis/border
        public static readonly Color Gold500 = Hex("#E7B23C"); // aksen utama (emas)
        public static readonly Color Gold600 = Hex("#C9922A");
        public static readonly Color Teal500 = Hex("#2FB3A8"); // aksen sekunder
        public static readonly Color Blue500 = Hex("#3E7BD6"); // aksi/info
        public static readonly Color Blue600 = Hex("#2F62B0");
        public static readonly Color Green500= Hex("#46BF6B"); // sukses
        public static readonly Color Red500  = Hex("#E0574F"); // bahaya
        public static readonly Color Cream50 = Hex("#F6F1E7"); // teks utama
        public static readonly Color Cream300= Hex("#C3BCAD"); // teks redup

        // ── SEMANTIC (alias tujuan) ──────────────────────────────────────────
        public static Color Scrim        => new Color(Ink900.r, Ink900.g, Ink900.b, 0.86f);
        public static Color Surface      => Ink800;
        public static Color SurfaceRaised=> Ink700;
        public static Color Border       => Line;
        public static Color Primary      => Gold500;
        public static Color OnPrimary    => Ink900;
        public static Color Action       => Blue500;
        public static Color OnAction     => Cream50;
        public static Color Text         => Cream50;
        public static Color TextMuted    => Cream300;
        public static Color Success      => Green500;
        public static Color Danger       => Red500;

        // ── TIPOGRAFI (ukuran, ref 1080p) ────────────────────────────────────
        public const float FDisplay = 52f;
        public const float FH1      = 38f;
        public const float FH2      = 28f;
        public const float FTitle   = 24f;
        public const float FBody    = 20f;
        public const float FSmall   = 16f;
        public const float FTiny    = 13f;

        // ── SPACING ──────────────────────────────────────────────────────────
        public const float S1 = 4f, S2 = 8f, S3 = 12f, S4 = 16f, S5 = 24f, S6 = 32f, S7 = 48f;

        // ── RADIUS (dipetakan ke sprite sliced) ──────────────────────────────
        public const float RadSm = 8f, RadMd = 14f, RadLg = 22f;

        // ── BUILDER ──────────────────────────────────────────────────────────

        /// <summary>Canvas overlay penuh + scaler standar.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Overlay gelap penuh (scrim) sebagai anak dari canvas.</summary>
        public static Image Scrim2(Transform parent)
        {
            var go = new GameObject("Scrim");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = Scrim;
            return img;
        }

        /// <summary>Panel/kartu bersudut membulat memakai UISprite sliced.</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = UISprite();
            img.type   = img.sprite != null ? Image.Type.Sliced : Image.Type.Simple; // build-safe (sprite bisa null)
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size,
            Color color, FontStyles style = FontStyles.Normal,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Txt_" + Safe(text));
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = align;
            return tmp;
        }

        public enum BtnVariant { Primary, Secondary, Danger, Ghost }

        /// <summary>Tombol berlabel dengan gaya sesuai varian + state hover/press.</summary>
        public static Button Button(Transform parent, string label, BtnVariant variant,
            Vector2 size, System.Action onClick, float fontSize = FTitle)
        {
            var go = new GameObject("Btn_" + Safe(label));
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = UISprite(); img.type = img.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Color bg, fg;
            switch (variant)
            {
                case BtnVariant.Primary:   bg = Primary;         fg = OnPrimary; break;
                case BtnVariant.Danger:    bg = Danger;          fg = Cream50;   break;
                case BtnVariant.Ghost:     bg = SurfaceRaised;   fg = Text;      break;
                default:                   bg = Action;          fg = OnAction;  break;
            }
            img.color = bg;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor    = Color.white;
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;

            var tmp = Label(go.transform, label, fontSize, fg, FontStyles.Bold);
            var lrt = tmp.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            tmp.raycastTarget = false;

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>Bar horizontal (track + fill). Return fill RectTransform agar bisa dianimasikan.</summary>
        public static RectTransform Bar(Transform parent, Color trackColor, Color fillColor, out Image fillImg)
        {
            var track = new GameObject("Bar");
            track.transform.SetParent(parent, false);
            var trackImg = track.AddComponent<Image>();
            trackImg.color = trackColor; trackImg.sprite = UISprite(); trackImg.type = trackImg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(track.transform, false);
            var rtF = fill.AddComponent<RectTransform>();
            rtF.anchorMin = new Vector2(0f, 0f); rtF.anchorMax = new Vector2(0f, 1f);
            rtF.pivot = new Vector2(0f, 0.5f);
            rtF.offsetMin = new Vector2(0f, 0f); rtF.offsetMax = new Vector2(0f, 0f);
            fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor; fillImg.sprite = UISprite(); fillImg.type = fillImg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return rtF;
        }

        // ── DIALOG BILINGUAL (Sunda + terjemahan) ────────────────────────────
        /// <summary>Format teks dialog: Bahasa Sunda di atas, terjemahan Indonesia
        /// (kecil + miring) di bawahnya. Dipakai semua dialog NPC.</summary>
        public static string Bi(string sunda, string indo)
            => $"{sunda}\n<size=68%><i><color=#C3BCAD>({indo})</color></i></size>";

        // ── util ─────────────────────────────────────────────────────────────
        // Sprite UI bawaan (sudut membulat). Di sebagian project (URP) resource ini tak
        // tersedia → coba SEKALI lalu cache. Kalau null, komponen pakai quad polos (Simple)
        // agar tetap ber-geometri & BISA DIKLIK (Sliced tanpa sprite = 0 geometri = tak ter-raycast).
        // Resource "UI/Skin/UISprite.psd" adalah builtin EDITOR yang tak tersedia di project
        // ini (URP) maupun di build → memanggilnya hanya menimbulkan error di konsol. Kembalikan
        // null saja; komponen otomatis pakai quad polos (Simple) yang tetap ber-geometri & bisa diklik.
        public static Sprite UISprite() => null;

        public static Color Hex(string hex)
        {
            Color c; ColorUtility.TryParseHtmlString(hex, out c); return c;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static string Safe(string s)
            => string.IsNullOrEmpty(s) ? "x" : s.Replace(" ", "").Replace(".", "");
    }
}
