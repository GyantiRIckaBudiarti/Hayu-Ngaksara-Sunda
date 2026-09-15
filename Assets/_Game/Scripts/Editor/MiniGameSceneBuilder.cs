using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using HayuNgaksara;

public class MiniGameSceneBuilder
{
    [MenuItem("Tools/Hayu Ngaksara/Build All MiniGame Scenes")]
    public static void BuildAll()
    {
        string[] scenes = { "MG_Swara", "MG_Ngalagena1", "MG_Ngalagena2", "MG_Rarangken", "MG_UjianFinal" };
        string[] datas  = {
            "Assets/_Game/Data/MiniGame/MG_Swara.asset",
            "Assets/_Game/Data/MiniGame/MG_Ngalagena1.asset",
            "Assets/_Game/Data/MiniGame/MG_Ngalagena2.asset",
            "Assets/_Game/Data/MiniGame/MG_Rarangken.asset",
            "Assets/_Game/Data/MiniGame/MG_UjianFinal.asset",
        };

        // Load Noto font (TrueType — TMP will use fallback)
        var notoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Game/Fonts/NotoSansSundanese-Regular SDF.asset");

        for (int i = 0; i < scenes.Length; i++)
        {
            string scenePath = $"Assets/_Game/Scenes/{scenes[i]}.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BuildScene(scene, datas[i], notoFont);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[MiniGameSceneBuilder] {scenes[i]} built and saved.");
        }

        // Reload overworld
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);
        Debug.Log("[MiniGameSceneBuilder] All done! Reopened 00_Sekolah.");
    }

    static void BuildScene(UnityEngine.SceneManagement.Scene scene, string dataPath, TMP_FontAsset notoFont)
    {
        // Clear existing objects
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // Camera
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
        cam.transform.position = new Vector3(0,0,-10);

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── BG ──
        MakeBG(canvasGO);

        // ── PANELS ──
        var panelIntro  = MakePanel(canvasGO, "Panel_Intro",  Color.clear);
        var panelLearn  = MakePanel(canvasGO, "Panel_Learn",  Color.clear);
        var panelQuiz   = MakePanel(canvasGO, "Panel_Quiz",   Color.clear);
        var panelResult = MakePanel(canvasGO, "Panel_Result", Color.clear);

        // Build each panel
        BuildIntroPanel(panelIntro);
        BuildLearnPanel(panelLearn, notoFont);
        BuildQuizPanel(panelQuiz, notoFont);
        BuildResultPanel(panelResult);

        // ── MiniGameManager GO ──
        var mgrGO = new GameObject("MiniGameManager");
        var mgr = mgrGO.AddComponent<MiniGameManager>();

        // AudioSource
        var asGO = new GameObject("AudioSource");
        var audioSrc = asGO.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        // Assign data
        var data = AssetDatabase.LoadAssetAtPath<MiniGameData>(dataPath);

        // Wire via reflection
        var t = typeof(MiniGameManager);
        var f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        t.GetField("data",        f)?.SetValue(mgr, data);
        t.GetField("panelIntro",  f)?.SetValue(mgr, panelIntro);
        t.GetField("panelLearn",  f)?.SetValue(mgr, panelLearn);
        t.GetField("panelQuiz",   f)?.SetValue(mgr, panelQuiz);
        t.GetField("panelResult", f)?.SetValue(mgr, panelResult);
        t.GetField("audioSource", f)?.SetValue(mgr, audioSrc);

        WireIntroPanel(panelIntro, mgr, t, f);
        WireLearnPanel(panelLearn, mgr, t, f);
        WireQuizPanel(panelQuiz, mgr, t, f);
        WireResultPanel(panelResult, mgr, t, f);
    }

    // ── PANEL BUILDERS ────────────────────────────────────────────────────────

static void MakeBG(GameObject canvas)
    {
        var bg = new GameObject("BG");
        var img = bg.AddComponent<Image>(); // AddComponent FIRST — creates RectTransform before SetParent
        img.color = new Color(0.08f, 0.10f, 0.14f);
        bg.transform.SetParent(canvas.transform, false);
        Stretch(bg);
    }

static GameObject MakePanel(GameObject canvas, string name, Color color)
    {
        var go = new GameObject(name);
        var img = go.AddComponent<Image>(); // AddComponent FIRST — creates RectTransform before SetParent
        img.color = color == Color.clear ? new Color(0,0,0,0) : color;
        img.raycastTarget = false;
        go.transform.SetParent(canvas.transform, false);
        Stretch(go);
        return go;
    }

    static void BuildIntroPanel(GameObject p)
    {
        MakeTMP(p, "Txt_NPC", "Nama NPC", 28, FontStyles.Bold, Color.yellow,
            new Vector2(0.1f,0.65f), new Vector2(0.9f,0.8f));
        MakeTMP(p, "Txt_Intro", "Teks intro...", 18, FontStyles.Normal, Color.white,
            new Vector2(0.1f,0.35f), new Vector2(0.9f,0.65f));
        MakeBtn(p, "Btn_StartLearn", "Ayo Belajar!", new Vector2(0.35f,0.18f), new Vector2(0.65f,0.30f),
            new Color(0.2f,0.65f,0.3f));
    }

    static void BuildLearnPanel(GameObject p, TMP_FontAsset noto)
    {
        MakeTMP(p, "Txt_Chapter", "Bab 1: Aksara Swara", 22, FontStyles.Bold, Color.yellow,
            new Vector2(0.05f,0.88f), new Vector2(0.95f,0.98f));

        // Aksara card background
        var card = new GameObject("Card_BG");
        var cardImg = card.AddComponent<Image>(); // Image creates RectTransform BEFORE SetParent
        cardImg.color = new Color(0.15f,0.18f,0.22f);
        card.transform.SetParent(p.transform, false);
        SetRect(card, new Vector2(0.15f,0.35f), new Vector2(0.85f,0.85f));

        // Aksara char (big, Noto font if available)
        var charGO = MakeTMP(p, "Txt_AksaraChar", "ᮃ", 80, FontStyles.Normal, Color.white,
            new Vector2(0.2f,0.55f), new Vector2(0.8f,0.85f));
        if (noto != null) charGO.GetComponent<TextMeshProUGUI>().font = noto;
        charGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        MakeTMP(p, "Txt_Latin", "A", 36, FontStyles.Bold, new Color(0.9f,0.8f,0.3f),
            new Vector2(0.2f,0.44f), new Vector2(0.8f,0.58f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        MakeTMP(p, "Txt_Desc", "Deskripsi aksara", 16, FontStyles.Italic, new Color(0.7f,0.7f,0.7f),
            new Vector2(0.2f,0.35f), new Vector2(0.8f,0.44f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        MakeTMP(p, "Txt_Progress", "1 / 6", 18, FontStyles.Normal, Color.white,
            new Vector2(0.4f,0.28f), new Vector2(0.6f,0.36f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        MakeBtn(p, "Btn_Audio", "Dengarkan", new Vector2(0.35f,0.20f), new Vector2(0.65f,0.29f),
            new Color(0.2f,0.4f,0.75f));
        MakeBtn(p, "Btn_Prev", "< Sebelum", new Vector2(0.05f,0.08f), new Vector2(0.30f,0.18f),
            new Color(0.4f,0.4f,0.5f));
        MakeBtn(p, "Btn_Next", "Berikut >", new Vector2(0.70f,0.08f), new Vector2(0.95f,0.18f),
            new Color(0.3f,0.6f,0.4f));
        var quizBtn = MakeBtn(p, "Btn_StartQuiz", "Mulai Kuis!", new Vector2(0.30f,0.02f), new Vector2(0.70f,0.09f),
            new Color(0.7f,0.4f,0.1f));
        quizBtn.SetActive(false);
    }

    static void BuildQuizPanel(GameObject p, TMP_FontAsset noto)
    {
        MakeTMP(p, "Txt_QuizProgress", "1 / 6", 18, FontStyles.Normal, Color.white,
            new Vector2(0.0f,0.90f), new Vector2(0.5f,0.98f));
        MakeTMP(p, "Txt_QuizScore", "Skor: 0", 18, FontStyles.Normal, new Color(1f,0.9f,0.3f),
            new Vector2(0.5f,0.90f), new Vector2(1.0f,0.98f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        // Layout: aksara (kotak besar) di KIRI, 4 opsi 2x2 di KANAN
        MakeTMP(p, "Txt_Question", "Aksara ini dibaca...?", 22, FontStyles.Normal, Color.white,
            new Vector2(0.04f,0.82f), new Vector2(0.52f,0.90f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        var aksaraGO = MakeTMP(p, "Txt_QuizAksara", "ᮃ", 90, FontStyles.Normal, Color.white,
            new Vector2(0.06f,0.20f), new Vector2(0.50f,0.78f));
        if (noto != null) aksaraGO.GetComponent<TextMeshProUGUI>().font = noto;
        aksaraGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 4 answer buttons in 2x2 grid — sisi KANAN, di tengah vertikal
        float[,] bPos = { {0.57f,0.52f,0.76f,0.72f}, {0.78f,0.52f,0.97f,0.72f},
                          {0.57f,0.28f,0.76f,0.48f}, {0.78f,0.28f,0.97f,0.48f} };
        for (int i = 0; i < 4; i++)
            MakeBtn(p, "Btn_Answer_" + i, "Pilihan " + (char)('A'+i),
                new Vector2(bPos[i,0], bPos[i,1]), new Vector2(bPos[i,2], bPos[i,3]),
                new Color(0.2f,0.4f,0.7f));
    }

    static void BuildResultPanel(GameObject p)
    {
        MakeTMP(p, "Txt_ResultTitle", "Bagus sekali!", 42, FontStyles.Bold, Color.yellow,
            new Vector2(0.1f,0.65f), new Vector2(0.9f,0.82f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        MakeTMP(p, "Txt_ResultStars", "Bintang 3!", 28, FontStyles.Normal, new Color(1f,0.8f,0.2f),
            new Vector2(0.2f,0.52f), new Vector2(0.8f,0.65f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        MakeTMP(p, "Txt_ResultScore", "6 / 6 benar", 22, FontStyles.Normal, Color.white,
            new Vector2(0.2f,0.40f), new Vector2(0.8f,0.52f)).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        MakeBtn(p, "Btn_Retry", "Coba Lagi", new Vector2(0.1f,0.22f), new Vector2(0.45f,0.35f),
            new Color(0.6f,0.3f,0.2f));
        MakeBtn(p, "Btn_Back", "Kembali ke Sekolah", new Vector2(0.55f,0.22f), new Vector2(0.9f,0.35f),
            new Color(0.2f,0.55f,0.35f));
    }

    // ── WIRING ────────────────────────────────────────────────────────────────

    static void WireIntroPanel(GameObject p, MiniGameManager mgr, System.Type t, System.Reflection.BindingFlags f)
    {
        t.GetField("txtIntroNPC",  f)?.SetValue(mgr, p.transform.Find("Txt_NPC")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtIntroText", f)?.SetValue(mgr, p.transform.Find("Txt_Intro")?.GetComponent<TextMeshProUGUI>());
        t.GetField("btnStartLearn",f)?.SetValue(mgr, p.transform.Find("Btn_StartLearn")?.GetComponent<Button>());
    }

    static void WireLearnPanel(GameObject p, MiniGameManager mgr, System.Type t, System.Reflection.BindingFlags f)
    {
        t.GetField("txtLearnChapter",f)?.SetValue(mgr, p.transform.Find("Txt_Chapter")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtAksaraChar",  f)?.SetValue(mgr, p.transform.Find("Txt_AksaraChar")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtAksaraLatin", f)?.SetValue(mgr, p.transform.Find("Txt_Latin")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtAksaraDesc",  f)?.SetValue(mgr, p.transform.Find("Txt_Desc")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtCardProgress",f)?.SetValue(mgr, p.transform.Find("Txt_Progress")?.GetComponent<TextMeshProUGUI>());
        t.GetField("btnPrev",        f)?.SetValue(mgr, p.transform.Find("Btn_Prev")?.GetComponent<Button>());
        t.GetField("btnNext",        f)?.SetValue(mgr, p.transform.Find("Btn_Next")?.GetComponent<Button>());
        t.GetField("btnAudio",       f)?.SetValue(mgr, p.transform.Find("Btn_Audio")?.GetComponent<Button>());
        t.GetField("btnStartQuiz",   f)?.SetValue(mgr, p.transform.Find("Btn_StartQuiz")?.GetComponent<Button>());
    }

    static void WireQuizPanel(GameObject p, MiniGameManager mgr, System.Type t, System.Reflection.BindingFlags f)
    {
        t.GetField("txtQuizQuestion",f)?.SetValue(mgr, p.transform.Find("Txt_Question")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtQuizAksara",  f)?.SetValue(mgr, p.transform.Find("Txt_QuizAksara")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtQuizScore",   f)?.SetValue(mgr, p.transform.Find("Txt_QuizScore")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtQuizProgress",f)?.SetValue(mgr, p.transform.Find("Txt_QuizProgress")?.GetComponent<TextMeshProUGUI>());
        var btns = new Button[4];
        for (int i = 0; i < 4; i++)
            btns[i] = p.transform.Find("Btn_Answer_"+i)?.GetComponent<Button>();
        t.GetField("answerButtons", f)?.SetValue(mgr, btns);
    }

    static void WireResultPanel(GameObject p, MiniGameManager mgr, System.Type t, System.Reflection.BindingFlags f)
    {
        t.GetField("txtResultTitle", f)?.SetValue(mgr, p.transform.Find("Txt_ResultTitle")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtResultScore", f)?.SetValue(mgr, p.transform.Find("Txt_ResultScore")?.GetComponent<TextMeshProUGUI>());
        t.GetField("txtResultStars", f)?.SetValue(mgr, p.transform.Find("Txt_ResultStars")?.GetComponent<TextMeshProUGUI>());
        t.GetField("btnRetry", f)?.SetValue(mgr, p.transform.Find("Btn_Retry")?.GetComponent<Button>());
        t.GetField("btnBack",  f)?.SetValue(mgr, p.transform.Find("Btn_Back")?.GetComponent<Button>());
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    static RectTransform RT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        return rt;
    }

    static void Stretch(GameObject go)
    {
        var rt = RT(go); if (rt == null) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetRect(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = RT(go); if (rt == null) return;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Image always creates RectTransform automatically — safest UI factory
static GameObject UI(string name, GameObject parent)
    {
        var go = new GameObject(name);
        var img = go.AddComponent<Image>(); // AddComponent FIRST — creates RectTransform before SetParent
        img.color = Color.clear;
        img.raycastTarget = false;
        go.transform.SetParent(parent.transform, false);
        return go;
    }

static GameObject MakeTMP(GameObject parent, string name, string text, float size,
        FontStyles style, Color color, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        var tmp = go.AddComponent<TextMeshProUGUI>(); // AddComponent FIRST — creates RectTransform before SetParent
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.color = color;
        tmp.enableWordWrapping = true;
        go.transform.SetParent(parent.transform, false);
        SetRect(go, aMin, aMax);
        return go;
    }

static GameObject MakeBtn(GameObject parent, string name, string label,
        Vector2 aMin, Vector2 aMax, Color bgColor)
    {
        var go = new GameObject(name);
        var img = go.AddComponent<Image>(); // AddComponent FIRST — creates RectTransform before SetParent
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.transform.SetParent(parent.transform, false);
        SetRect(go, aMin, aMax);

        var txtGO = new GameObject("Text");
        var tmp = txtGO.AddComponent<TextMeshProUGUI>(); // AddComponent FIRST
        tmp.text = label; tmp.fontSize = 18; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        txtGO.transform.SetParent(go.transform, false);
        Stretch(txtGO);
        return go;
    }
}
