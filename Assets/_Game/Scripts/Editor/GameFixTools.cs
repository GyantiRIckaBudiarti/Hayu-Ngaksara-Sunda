using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using HayuNgaksara;
using System.Collections.Generic;

public class GameFixTools
{
    // ─── Debug: Reset Chapter Progress ──────────────────────────────────────
    [MenuItem("Tools/Hayu Ngaksara/Debug Reset Chapter Progress")]
    public static void ResetChapterProgress()
    {
        if (!EditorUtility.DisplayDialog("Reset Chapter Progress",
            "Ini akan reset semua chapter (1–5) ke done=0.\n" +
            "Tutorial (0) tetap selesai.\nLanjut?", "Reset", "Batal")) return;

        for (int i = 1; i <= 5; i++)
        {
            PlayerPrefs.SetInt($"ch_{i}_unlock", 1);
            PlayerPrefs.SetInt($"ch_{i}_done",   0);
            PlayerPrefs.SetInt($"ch_{i}_stars",  0);
        }
        PlayerPrefs.Save();
        Debug.Log("[GameFixTools] Chapter 1–5 reset: unlock=1, done=0, stars=0");
        EditorUtility.DisplayDialog("Reset Selesai",
            "Chapter 1–5 direset.\nSekarang semua NPC bisa diajak mulai latihan lagi.",
            "OK");
    }

    [MenuItem("Tools/Hayu Ngaksara/Debug Reset ALL (termasuk Tutorial)")]
    public static void ResetAllProgress()
    {
        if (!EditorUtility.DisplayDialog("Reset SEMUA Progress",
            "Ini akan reset SEMUA chapter termasuk Tutorial ke kondisi awal.\n" +
            "Game akan mulai dari awal seperti install baru.\nLanjut?", "Reset Semua", "Batal")) return;

        for (int i = 0; i <= 5; i++)
        {
            PlayerPrefs.SetInt($"ch_{i}_unlock", i == 0 ? 1 : 0);
            PlayerPrefs.SetInt($"ch_{i}_done",   0);
            PlayerPrefs.SetInt($"ch_{i}_stars",  0);
        }
        PlayerPrefs.DeleteKey("mg_pending_feedback");
        PlayerPrefs.DeleteKey("ActiveChapter");
        PlayerPrefs.Save();
        Debug.Log("[GameFixTools] ALL chapters reset to initial state");
        EditorUtility.DisplayDialog("Reset Selesai",
            "Semua progress direset ke awal.\nHanya Tutorial yang unlock.",
            "OK");
    }

    // ─── 1. Fix font Aksara Sunda di semua MiniGame scenes ───────────────────
    [MenuItem("Tools/Hayu Ngaksara/Fix Aksara Font in All MiniGame Scenes")]
    public static void FixAksaraFont()
    {
        var notoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Game/Fonts/NotoSansSundanese-Regular SDF.asset");
        if (notoFont == null) { Debug.LogError("NotoSans SDF not found!"); return; }

        string[] scenes = {
            "Assets/_Game/Scenes/MG_Swara.unity",
            "Assets/_Game/Scenes/MG_Ngalagena1.unity",
            "Assets/_Game/Scenes/MG_Ngalagena2.unity",
            "Assets/_Game/Scenes/MG_Rarangken.unity",
            "Assets/_Game/Scenes/MG_UjianFinal.unity"
        };
        string[] aksaraNames = { "Txt_AksaraChar", "Txt_QuizAksara" };

        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int fixed2 = 0;
            foreach (var name in aksaraNames)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;
                tmp.font = notoFont;
                tmp.fontSize = name == "Txt_AksaraChar" ? 80f : 90f;
                tmp.enableAutoSizing = false;
                EditorUtility.SetDirty(tmp);
                fixed2++;
            }
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FixAksaraFont] {System.IO.Path.GetFileNameWithoutExtension(scenePath)}: {fixed2} fixed");
        }
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);
        Debug.Log("[FixAksaraFont] Done!");
    }

    // ─── 2. Simplify Main Menu ────────────────────────────────────────────────
    [MenuItem("Tools/Hayu Ngaksara/Simplify Main Menu")]
    public static void SimplifyMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/01_MainMenu.unity", OpenSceneMode.Single);

        // Ganti btn labels dan sembunyikan yg tidak relevan
        var mmc = Object.FindObjectOfType<MainMenuController>();
        var t = typeof(MainMenuController);
        var f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        // Sembunyikan tombol lama yg tidak relevan
        string[] toHide = { "Btn_BelajarHuruf", "Btn_LatihanBaca", "Btn_LatihanTulis", "Btn_Pelafalan", "Btn_Kuis" };
        string[] lockNames = { "Lock_Baca", "Lock_Tulis", "Lock_Kuis" };

        var canvas = GameObject.Find("UICanvas");
        if (canvas != null)
        {
            var panel = canvas.transform.Find("Panel_MainMenu");
            if (panel != null)
            {
                foreach (string name in toHide)
                {
                    var go = panel.Find(name);
                    if (go != null) { go.gameObject.SetActive(false); EditorUtility.SetDirty(go.gameObject); }
                }
                foreach (string name in lockNames)
                {
                    var go = panel.Find(name);
                    if (go != null) { go.gameObject.SetActive(false); EditorUtility.SetDirty(go.gameObject); }
                }
                // Rename Btn_Mulai label to be clearer
                var btnMulai = panel.Find("Btn_Mulai");
                if (btnMulai != null)
                {
                    var lbl = btnMulai.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl != null) { lbl.text = "MULAI PETUALANGAN"; EditorUtility.SetDirty(lbl); }
                }
            }
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);
        Debug.Log("[SimplifyMainMenu] Done! Tombol lama disembunyikan.");
    }

    // ─── 3. Add Portrait System to Dialog ────────────────────────────────────
    [MenuItem("Tools/Hayu Ngaksara/Setup Dialog Portrait System")]
    public static void SetupDialogPortrait()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);

        // Buat portrait sprites per karakter
        CreatePortraitSprites();

        // Update Panel_Dialog dengan portrait panel
        var panelDialog = FindInactive("Panel_Dialog");
        if (panelDialog == null) { Debug.LogError("Panel_Dialog not found!"); return; }

        // Cek apakah sudah ada portrait image
        var existingPortrait = panelDialog.transform.Find("Img_Portrait");
        if (existingPortrait == null)
        {
            // Buat Portrait Image di kiri dialog
            var portraitGO = new GameObject("Img_Portrait");
            var img = portraitGO.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.white;
            portraitGO.transform.SetParent(panelDialog.transform, false);
            var rt = portraitGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(10f, 10f);
            rt.offsetMax = new Vector2(210f, -10f);  // 200px wide portrait
            EditorUtility.SetDirty(portraitGO);

            // Geser Text_Speaker dan Text_Dialog ke kanan (hindari portrait)
            var txtSpeaker = panelDialog.transform.Find("Text_Speaker");
            var txtDialog  = panelDialog.transform.Find("Text_Dialog");
            if (txtSpeaker != null)
            {
                var rt2 = txtSpeaker.GetComponent<RectTransform>();
                if (rt2 != null) { rt2.offsetMin = new Vector2(220f, rt2.offsetMin.y); EditorUtility.SetDirty(txtSpeaker.gameObject); }
            }
            if (txtDialog != null)
            {
                var rt2 = txtDialog.GetComponent<RectTransform>();
                if (rt2 != null) { rt2.offsetMin = new Vector2(220f, rt2.offsetMin.y); EditorUtility.SetDirty(txtDialog.gameObject); }
            }
        }

        // Wire DialogSystem.imgPortrait
        var ds = Object.FindObjectOfType<DialogSystem>();
        if (ds != null)
        {
            var ft = typeof(DialogSystem);
            var ff = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var imgPortraitField = ft.GetField("imgPortrait", ff);
            if (imgPortraitField != null)
            {
                var portraitImg = panelDialog.transform.Find("Img_Portrait")?.GetComponent<UnityEngine.UI.Image>();
                imgPortraitField.SetValue(ds, portraitImg);
                EditorUtility.SetDirty(ds);
                Debug.Log("[SetupDialogPortrait] imgPortrait wired: " + (portraitImg != null));
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupDialogPortrait] Done!");
    }

    static void CreatePortraitSprites()
    {
        string dir = "Assets/_Game/Sprites/Portraits";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        // Buat portrait per karakter: 200x300 solid color dengan inisial
        var characters = new System.Collections.Generic.Dictionary<string, Color32>
        {
            { "Jajang",  new Color32(255, 165,  50, 255) },  // oranye
            { "Sinta",   new Color32(255, 130, 180, 255) },  // pink
            { "Nabila",  new Color32( 80, 200, 120, 255) },  // hijau
            { "Ucup",   new Color32(160, 100, 220, 255) },  // ungu
            { "Andre",   new Color32( 80, 150, 240, 255) },  // biru
            { "Player",  new Color32(120, 200, 255, 255) },  // cyan
        };

        foreach (var kv in characters)
        {
            string path = $"{dir}/{kv.Key}_portrait.png";
            if (System.IO.File.Exists(path)) continue;

            // 200x300 portrait dengan warna karakter
            var tex = new Texture2D(200, 300, TextureFormat.RGBA32, false);
            var pixels = new Color32[200 * 300];
            var col = kv.Value;

            // Fill gradient (lebih gelap di bawah)
            for (int y = 0; y < 300; y++)
            for (int x = 0; x < 200; x++)
            {
                float t2 = (float)y / 300f;
                pixels[y * 200 + x] = Color32.Lerp(
                    new Color32((byte)(col.r / 2), (byte)(col.g / 2), (byte)(col.b / 2), 255),
                    col, t2);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        AssetDatabase.Refresh();
        // Import as Sprite
        foreach (var kv in characters)
        {
            string path = $"{dir}/{kv.Key}_portrait.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                AssetDatabase.ImportAsset(path);
            }
        }
        Debug.Log("[CreatePortraitSprites] Portraits created in " + dir);
    }

    static GameObject FindInactive(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindInChildren(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    static GameObject FindInChildren(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindInChildren(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
