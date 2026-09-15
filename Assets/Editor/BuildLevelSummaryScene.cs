
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildLevelSummaryScene
{
    [MenuItem("Tools/Hayu/Build LevelSummary Scene")]
    public static void Build()
    {
        // ── helpers ───────────────────────────────────────────────────────────
        GameObject MakeGO(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        void SetRT(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 sz, Vector2 pos)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
            rt.sizeDelta = sz; rt.anchoredPosition = pos;
        }

        TextMeshProUGUI MakeTMP(GameObject go, string text, float size, Color col,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
            return t;
        }

        Image MakeImg(GameObject go, Color col)
        { var i = go.AddComponent<Image>(); i.color = col; return i; }

        void MakeCard(Transform parent, string titleTxt, string starsName, string scoreName)
        {
            var card = MakeGO("Card_" + titleTxt.Split('(')[0].Trim().Replace(" ", "_"), parent);
            MakeImg(card, new Color(0.10f, 0.09f, 0.22f, 0.95f));
            var vl = card.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.spacing = 6;
            vl.padding = new RectOffset(12, 12, 14, 14);
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            var t1 = MakeGO("Txt_CardTitle", card.transform);
            MakeTMP(t1, titleTxt, 24, new Color(0.75f, 0.75f, 1f, 1f));
            t1.AddComponent<LayoutElement>().preferredHeight = 36;

            var t2 = MakeGO(starsName, card.transform);
            MakeTMP(t2, "☆☆☆", 52, new Color(0.55f, 0.55f, 0.55f, 1f));
            t2.AddComponent<LayoutElement>().preferredHeight = 68;

            var t3 = MakeGO(scoreName, card.transform);
            MakeTMP(t3, "0 / 0 benar", 22, new Color(0.8f, 0.8f, 0.8f, 1f));
            t3.AddComponent<LayoutElement>().preferredHeight = 34;
        }

        void MakeBtn(Transform cvParent, string goName, string labelText, string lblName,
            Color bgCol, Vector2 ancPos, Vector2 size)
        {
            var bGO = MakeGO(goName, cvParent);
            var img = MakeImg(bGO, bgCol);
            img.raycastTarget = true;
            SetRT(bGO, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                  new Vector2(0.5f, 0f), size, ancPos);
            bGO.AddComponent<Button>();

            var lGO = MakeGO(lblName, bGO.transform);
            SetRT(lGO, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                  Vector2.zero, Vector2.zero);
            MakeTMP(lGO, labelText, 28, Color.white);
        }

        // ── Canvas ─────────────────────────────────────────────────────────────
        var cvGO = MakeGO("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;

        var cvs = cvGO.AddComponent<CanvasScaler>();
        cvs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution = new Vector2(1920, 1080);
        cvs.matchWidthOrHeight  = 0.5f;

        cvGO.AddComponent<GraphicRaycaster>();

        // ── Background ─────────────────────────────────────────────────────────
        var bgGO = MakeGO("BG_Panel", cvGO.transform);
        MakeImg(bgGO, new Color(0.055f, 0.047f, 0.137f, 1f));
        // Image auto-adds RectTransform, so use GetComponent not AddComponent
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // ── Header ─────────────────────────────────────────────────────────────
        var hdr = MakeGO("Header", cvGO.transform);
        MakeImg(hdr, new Color(0.08f, 0.06f, 0.18f, 0.9f));
        SetRT(hdr, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
              new Vector2(0,170), Vector2.zero);

        var tTitle = MakeGO("Txt_LevelTitle", hdr.transform);
        SetRT(tTitle, new Vector2(0,0.45f), new Vector2(1,1),
              new Vector2(0.5f,1), new Vector2(-40,0), Vector2.zero);
        MakeTMP(tTitle, "Level 1 Selesai!", 64, new Color(1f,0.9f,0.3f,1f));

        var tSub = MakeGO("Txt_LevelSubtitle", hdr.transform);
        SetRT(tSub, new Vector2(0,0f), new Vector2(1,0.45f),
              new Vector2(0.5f,0), new Vector2(-40,0), Vector2.zero);
        MakeTMP(tSub, "Aksara Swara", 36, new Color(0.85f,0.85f,1f,1f));

        // ── Cards Row ──────────────────────────────────────────────────────────
        var row = MakeGO("CardsRow", cvGO.transform);
        SetRT(row, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
              new Vector2(0.5f,0.5f), new Vector2(1780,340), new Vector2(0,80));

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment    = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        MakeCard(row.transform, "Belajar (Sinta)",    "Txt_BacaStars",      "Txt_BacaScore");
        MakeCard(row.transform, "Menulis (Ucup)",     "Txt_TulisStars",     "Txt_TulisScore");
        MakeCard(row.transform, "Pelafalan (Nabila)", "Txt_PelafalanStars", "Txt_PelafalanScore");
        MakeCard(row.transform, "Refleksi (Guru)",    "Txt_RefleksiStars",  "Txt_RefleksiScore");

        // ── Grade Panel ────────────────────────────────────────────────────────
        var gp = MakeGO("GradePanel", cvGO.transform);
        MakeImg(gp, new Color(0.08f, 0.06f, 0.18f, 0.92f));
        SetRT(gp, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
              new Vector2(0.5f,0.5f), new Vector2(700,280), new Vector2(0,-260));

        var gvl = gp.AddComponent<VerticalLayoutGroup>();
        gvl.childAlignment = TextAnchor.MiddleCenter; gvl.spacing = 4;
        gvl.padding = new RectOffset(20,20,16,16);
        gvl.childForceExpandWidth  = true;
        gvl.childForceExpandHeight = false;

        var gLbl = MakeGO("Txt_GradeLabel", gp.transform);
        MakeTMP(gLbl, "NILAI KESELURUHAN", 22, new Color(0.7f,0.7f,1f,1f));
        gLbl.AddComponent<LayoutElement>().preferredHeight = 30;

        var gVal = MakeGO("Txt_Grade", gp.transform);
        MakeTMP(gVal, "A", 110, new Color(1f,0.84f,0f,1f));
        gVal.AddComponent<LayoutElement>().preferredHeight = 120;

        var gDesc = MakeGO("Txt_GradeDesc", gp.transform);
        MakeTMP(gDesc, "Luar Biasa! Kamu menguasai semua aksara Sunda!", 22, Color.white);
        gDesc.AddComponent<LayoutElement>().preferredHeight = 34;

        var gTot = MakeGO("Txt_TotalStars", gp.transform);
        MakeTMP(gTot, "Total: 12 / 12 Bintang", 24, new Color(1f,0.85f,0.1f,1f));
        gTot.AddComponent<LayoutElement>().preferredHeight = 32;

        // ── Buttons ────────────────────────────────────────────────────────────
        MakeBtn(cvGO.transform, "Btn_Kembali", "Kembali ke Sekolah",
            "Txt_Label",    new Color(0.25f,0.25f,0.45f,1f),
            new Vector2(-440,50), new Vector2(380,80));

        MakeBtn(cvGO.transform, "Btn_Lanjut",  "Lanjut ke Level Berikutnya →",
            "Txt_BtnLanjut", new Color(0.1f,0.45f,0.15f,1f),
            new Vector2(440,50), new Vector2(500,80));

        // ── Controller ─────────────────────────────────────────────────────────
        MakeGO("LevelSummaryController");

        // ── EventSystem ────────────────────────────────────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = MakeGO("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[HayuNgaksara] LevelSummary UI built! Wire SerializeField in Inspector next.");
    }

    [MenuItem("Tools/Hayu/Wire LevelSummaryController")]
    public static void WireController()
    {
        var ctrlGO = GameObject.Find("LevelSummaryController");
        if (ctrlGO == null) { Debug.LogError("LevelSummaryController not found!"); return; }

        // Attach script component
        var comp = ctrlGO.GetComponent<HayuNgaksara.LevelSummaryController>();
        if (comp == null)
            comp = ctrlGO.AddComponent<HayuNgaksara.LevelSummaryController>();

        // Use SerializedObject to set private serialized fields
        var so = new SerializedObject(comp);

        void SetTMP(string field, string path)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("Not found: " + path); return; }
            so.FindProperty(field).objectReferenceValue = go.GetComponent<TextMeshProUGUI>();
        }
        void SetImg(string field, string path)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("Not found: " + path); return; }
            so.FindProperty(field).objectReferenceValue = go.GetComponent<Image>();
        }
        void SetBtn(string field, string path)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("Not found: " + path); return; }
            so.FindProperty(field).objectReferenceValue = go.GetComponent<Button>();
        }

        SetTMP("txtLevelTitle",      "Txt_LevelTitle");
        SetTMP("txtLevelSubtitle",   "Txt_LevelSubtitle");

        SetTMP("txtBacaStars",       "Txt_BacaStars");
        SetTMP("txtBacaScore",       "Txt_BacaScore");
        SetImg("imgBacaBg",          "Card_Belajar");

        SetTMP("txtTulisStars",      "Txt_TulisStars");
        SetTMP("txtTulisScore",      "Txt_TulisScore");
        SetImg("imgTulisBg",         "Card_Menulis");

        SetTMP("txtPelafalanStars",  "Txt_PelafalanStars");
        SetTMP("txtPelafalanScore",  "Txt_PelafalanScore");
        SetImg("imgPelafalanBg",     "Card_Pelafalan");

        SetTMP("txtRefleksiStars",   "Txt_RefleksiStars");
        SetTMP("txtRefleksiScore",   "Txt_RefleksiScore");
        SetImg("imgRefleksiBg",      "Card_Refleksi");

        SetTMP("txtGrade",           "Txt_Grade");
        SetTMP("txtGradeDesc",       "Txt_GradeDesc");
        SetTMP("txtTotalStars",      "Txt_TotalStars");

        SetBtn("btnLanjut",          "Btn_Lanjut");
        SetBtn("btnKembali",         "Btn_Kembali");
        SetTMP("txtBtnLanjut",       "Txt_BtnLanjut");

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[HayuNgaksara] LevelSummaryController wired successfully!");
    }
}
#endif
