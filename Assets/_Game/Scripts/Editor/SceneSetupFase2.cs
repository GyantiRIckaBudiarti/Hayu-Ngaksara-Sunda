using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HayuNgaksara;

namespace HayuNgaksara.Editor
{
    /// <summary>
    /// Fase 2 scene setup: konfigurasikan NPC Inspector + rename Quiz→Refleksi
    /// Jalankan melalui menu Tools/Hayu Ngaksara/
    /// </summary>
    public static class SceneSetupFase2
    {
        // ── NPC CONFIGURATION ───────────────────────────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Configure NPCs (00_Sekolah)")]
        public static void ConfigureNPCs()
        {
            string scenePath = "Assets/_Game/Scenes/00_Sekolah.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // isSkillNPC=true: gunakan GetCurrentActiveChapter() secara dinamis
            ConfigureNPC("NPC_Sinta",  isSkill: true,  MiniGameMode.ReadBaca);
            ConfigureNPC("NPC_Ucup",   isSkill: true,  MiniGameMode.TraceOnly);
            ConfigureNPC("NPC_Nabila", isSkill: true,  MiniGameMode.Pelafalan);
            ConfigureNPC("NPC_Guru",   isSkill: true,  MiniGameMode.Refleksi);
            ConfigureNPC("NPC_Andre",  isSkill: true,  MiniGameMode.CombineLetters);
            ConfigureNPC("NPC_Jajang", isSkill: false, MiniGameMode.Standard); // tetap tutorial flow

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fase2] NPC configuration done — 00_Sekolah saved.");
        }

        private static void ConfigureNPC(string goName, bool isSkill, MiniGameMode mode)
        {
            var go = GameObject.Find(goName);
            if (go == null) { Debug.LogWarning($"[Fase2] NPC not found: {goName}"); return; }

            var npc = go.GetComponent<NPCController>();
            if (npc == null) { Debug.LogWarning($"[Fase2] NPCController missing on {goName}"); return; }

            var so = new SerializedObject(npc);
            so.FindProperty("isSkillNPC").boolValue     = isSkill;
            so.FindProperty("skillMode").enumValueIndex = (int)mode;

            // Set dialogIdle untuk skill NPCs (jika belum ada isinya)
            var dialogIdleProp = so.FindProperty("dialogIdle");
            if (dialogIdleProp != null && dialogIdleProp.arraySize == 0)
            {
                string[] lines = GetNPCDialogLines(goName);
                dialogIdleProp.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                {
                    var elem = dialogIdleProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("speakerName").stringValue = NpcDisplayName(goName);
                    elem.FindPropertyRelative("text").stringValue = lines[i];
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
            Debug.Log($"[Fase2] {goName}: isSkillNPC={isSkill}, skillMode={mode}");
        }

        private static string NpcDisplayName(string goName) =>
            goName.Replace("NPC_", "");

        private static string[] GetNPCDialogLines(string goName)
        {
            switch (goName)
            {
                case "NPC_Sinta":
                    return new[] {
                        "Hai! Aku Sinta. Aku akan mengajarimu cara MEMBACA Aksara Sunda.",
                        "Kita akan belajar mengenal setiap aksara dan bagaimana cara membacanya.",
                        "Sudah siap belajar membaca bersama aku?"
                    };
                case "NPC_Ucup":
                    return new[] {
                        "Halo! Aku Ucup. Bagian favoritku adalah latihan MENULIS aksara.",
                        "Aku akan melatih tanganmu agar bisa menulis setiap aksara dengan benar.",
                        "Yuk, kita latihan menulis bersama!"
                    };
                case "NPC_Nabila":
                    return new[] {
                        "Assalamualaikum! Aku Nabila. Aku ahli dalam PELAFALAN Aksara Sunda.",
                        "Kita akan belajar mengucapkan setiap aksara dengan tepat dan benar.",
                        "Siap belajar melafalkan aksara bersama aku?"
                    };
                case "NPC_Guru":
                    return new[] {
                        "Selamat datang! Aku adalah Guru di sekolah ini.",
                        "Setelah kamu belajar dari Sinta, Ucup, dan Nabila — saatnya kita REFLEKSI bersama.",
                        "Kita akan menguji seberapa jauh pemahamanmu tentang Aksara Sunda."
                    };
                case "NPC_Andre":
                    return new[] {
                        "Yo! Aku Andre. Kalau kamu mau latihan ekstra menggabungkan aksara, datanglah ke aku!",
                        "Di sini kita latihan menyatukan konsonan dan rarangken menjadi suku kata.",
                        "Ini latihan bonus yang seru, mau coba?"
                    };
                default:
                    return new string[0];
            }
        }

        // ── RENAME QUIZ → REFLEKSI IN ALL MG SCENES ─────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Rename Quiz→Refleksi (all MG scenes)")]
        public static void RenameQuizToRefleksi()
        {
            string[] scenePaths = new[]
            {
                "Assets/_Game/Scenes/MG_Swara.unity",
                "Assets/_Game/Scenes/MG_Ngalagena1.unity",
                "Assets/_Game/Scenes/MG_Ngalagena2.unity",
                "Assets/_Game/Scenes/MG_Rarangken.unity",
                "Assets/_Game/Scenes/MG_UjianFinal.unity",
            };

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = RenameInOpenScene();
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Fase2] {path}: {count} label(s) renamed → saved.");
            }
        }

        private static int RenameInOpenScene()
        {
            int count = 0;
            foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>())
            {
                string t = tmp.text;
                bool changed = false;

                if (t == "Quiz" || t == "Kuis")
                { tmp.text = "Refleksi"; changed = true; }
                else if (t.Contains("Mulai Quiz") || t.Contains("Mulai Kuis"))
                { tmp.text = t.Replace("Mulai Quiz", "Mulai Refleksi").Replace("Mulai Kuis", "Mulai Refleksi"); changed = true; }
                else if (t.Contains("Hasil Quiz") || t.Contains("Hasil Kuis"))
                { tmp.text = t.Replace("Hasil Quiz", "Hasil Refleksi").Replace("Hasil Kuis", "Hasil Refleksi"); changed = true; }
                else if (t.Contains("Latihan Quiz") || t.Contains("Latihan Kuis"))
                { tmp.text = t.Replace("Latihan Quiz", "Latihan Refleksi").Replace("Latihan Kuis", "Latihan Refleksi"); changed = true; }

                if (changed)
                {
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
            }
            return count;
        }

        // ── MAIN MENU UI SETUP ───────────────────────────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Setup MainMenu 4 Buttons")]
        public static void SetupMainMenu()
        {
            string scenePath = "Assets/_Game/Scenes/01_MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var ctrl = Object.FindObjectOfType<MainMenuController>();
            if (ctrl == null) { Debug.LogError("[Fase2] MainMenuController not found in 01_MainMenu"); return; }

            var so = new SerializedObject(ctrl);

            // Tombol yang sudah ada (nama dari hierarchy)
            var btnMulai      = FindButtonGO("Btn_Mulai");
            var btnPengaturan = FindButtonGO("Btn_Pengaturan");
            var btnKeluar     = FindButtonGO("Btn_Keluar");
            var btnTutup      = FindButtonGO("Btn_TutupPengaturan");

            // Sembunyikan tombol lama yang tidak diperlukan
            HideGO("Btn_BelajarHuruf");
            HideGO("Btn_LatihanBaca");
            HideGO("Btn_LatihanTulis");
            HideGO("Btn_Pelafalan");
            HideGO("Btn_Kuis");

            // Update label Btn_Mulai
            SetButtonLabel(btnMulai, "Mulai Petualangan Baru");

            // Buat atau cari Btn_Lanjutkan
            var btnLanjutkanGO = GameObject.Find("Btn_Lanjutkan");
            if (btnLanjutkanGO == null)
                btnLanjutkanGO = DuplicateButton(btnMulai, "Btn_Lanjutkan", "Lanjutkan Petualangan", new Vector2(0f, -90f));

            // Wire fields ke MainMenuController
            if (btnMulai      != null) SetBtn(so, "btnMulai",          btnMulai.GetComponent<Button>());
            if (btnLanjutkanGO!= null) SetBtn(so, "btnLanjutkan",      btnLanjutkanGO.GetComponent<Button>());
            if (btnPengaturan != null) SetBtn(so, "btnPengaturan",     btnPengaturan.GetComponent<Button>());
            if (btnKeluar     != null) SetBtn(so, "btnKeluar",         btnKeluar.GetComponent<Button>());
            if (btnTutup      != null) SetBtn(so, "btnTutupPengaturan",btnTutup.GetComponent<Button>());

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl.gameObject);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fase2] MainMenu 4-button setup done — 01_MainMenu saved.");
        }

        private static GameObject FindButtonGO(string name) => GameObject.Find(name);

        private static void HideGO(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(false);
                EditorUtility.SetDirty(go);
                Debug.Log($"[Fase2] Hidden: {name}");
            }
        }

        private static void SetButtonLabel(GameObject go, string label)
        {
            if (go == null) return;
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = label; EditorUtility.SetDirty(tmp); }
        }

        private static GameObject DuplicateButton(GameObject source, string newName, string label, Vector2 offset)
        {
            if (source == null)
            {
                Debug.LogWarning("[Fase2] Cannot duplicate null source button for " + newName);
                return null;
            }
            var dup = Object.Instantiate(source, source.transform.parent);
            dup.name = newName;
            var rt = dup.GetComponent<RectTransform>();
            if (rt != null)
            {
                var srcRT = source.GetComponent<RectTransform>();
                rt.anchoredPosition = srcRT != null ? srcRT.anchoredPosition + offset : offset;
            }
            SetButtonLabel(dup, label);
            dup.SetActive(true);
            EditorUtility.SetDirty(dup);
            Debug.Log($"[Fase2] Created: {newName}");
            return dup;
        }

        private static void SetBtn(SerializedObject so, string fieldName, Button btn)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = btn;
            else Debug.LogWarning($"[Fase2] Field missing in MainMenuController: {fieldName}");
        }

        // ── WIRE COMBO DATA OVERRIDE ─────────────────────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Wire Andre ComboData (all MG scenes)")]
        public static void WireComboData()
        {
            string comboAssetPath = "Assets/_Game/Data/MiniGame/MG_Andre_Combo.asset";
            var comboData = AssetDatabase.LoadAssetAtPath<MiniGameData>(comboAssetPath);
            if (comboData == null)
            {
                Debug.LogError($"[Fase2] MG_Andre_Combo.asset not found at {comboAssetPath}. Run Generate MiniGame Data first.");
                return;
            }

            string[] scenePaths = new[]
            {
                "Assets/_Game/Scenes/MG_Swara.unity",
                "Assets/_Game/Scenes/MG_Ngalagena1.unity",
                "Assets/_Game/Scenes/MG_Ngalagena2.unity",
                "Assets/_Game/Scenes/MG_Rarangken.unity",
                "Assets/_Game/Scenes/MG_UjianFinal.unity",
            };

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var mgm = Object.FindObjectOfType<MiniGameManager>();
                if (mgm == null) { Debug.LogWarning($"[Fase2] MiniGameManager not found in {path}"); continue; }

                var so = new SerializedObject(mgm);
                var prop = so.FindProperty("comboDataOverride");
                if (prop != null)
                {
                    prop.objectReferenceValue = comboData;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mgm);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[Fase2] comboDataOverride set in {System.IO.Path.GetFileName(path)}");
                }
            }
        }

        // ── RECREATE SUNDANESE FONT ASSET (dengan sub-assets benar) ─────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Recreate Sundanese Font Asset")]
        public static void RecreateSundaneseFont()
        {
            string ttfPath  = "Assets/_Game/Fonts/NotoSansSundanese-Regular.ttf";
            string fontPath = "Assets/_Game/Fonts/NotoSansSundanese-Regular SDF.asset";

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null) { Debug.LogError("[Fase2] TTF not found: " + ttfPath); return; }

            AssetDatabase.DeleteAsset(fontPath);

            // Static mode: semua glyph di-bake ke atlas sekarang dan DISIMPAN ke disk.
            // Dynamic mode tidak menyimpan characterTable ke .asset file sehingga chars=0 saat reload.
            var fa = TMPro.TMP_FontAsset.CreateFontAsset(
                ttf, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024,
                TMPro.AtlasPopulationMode.Static,
                enableMultiAtlasSupport: false);

            // KRITIS: tambah karakter SEBELUM CreateAsset agar TMP init font engine dulu,
            // lalu panggil ReadFontAssetDefinition() agar internal lookup ter-serialize.
            fa.TryAddCharacters(new uint[]{ 0x1B83 }); // init engine dengan 1 char dulu
            var unicodeList = new System.Collections.Generic.List<uint>();
            for (uint u = 0x1B80; u <= 0x1BBF; u++) unicodeList.Add(u);
            bool ok = fa.TryAddCharacters(unicodeList.ToArray());
            fa.ReadFontAssetDefinition(); // paksa rebuild lookup tables agar ter-serialize
            Debug.Log("[Fase2] TryAddCharacters: ok=" + ok + " chars=" + fa.characterTable.Count);

            AssetDatabase.CreateAsset(fa, fontPath);

            // Simpan sub-assets (atlas texture + material)
            foreach (var tex in fa.atlasTextures)
                if (tex != null) AssetDatabase.AddObjectToAsset(tex, fa);
            if (fa.material != null)
                AssetDatabase.AddObjectToAsset(fa.material, fa);

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verifikasi hasil
            var verify = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
            Debug.Log("[Fase2] Font OK — mode:" + fa.atlasPopulationMode
                      + " chars_inmem:" + fa.characterTable.Count
                      + " chars_ondisk:" + (verify != null ? verify.characterTable.Count.ToString() : "NULL")
                      + " src:" + (fa.sourceFontFile != null ? fa.sourceFontFile.name : "NULL"));
        }

        // ── WIRE SUNDANESE FONT ──────────────────────────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Wire Sundanese Font (all MG scenes)")]
        public static void WireSundaneseFont()
        {
            string fontPath = "Assets/_Game/Fonts/NotoSansSundanese-Regular SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[Fase2] Font tidak ditemukan: {fontPath}");
                return;
            }

            string[] scenePaths = new[]
            {
                "Assets/_Game/Scenes/MG_Swara.unity",
                "Assets/_Game/Scenes/MG_Ngalagena1.unity",
                "Assets/_Game/Scenes/MG_Ngalagena2.unity",
                "Assets/_Game/Scenes/MG_Rarangken.unity",
                "Assets/_Game/Scenes/MG_UjianFinal.unity",
            };

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                // Wire ke MiniGameManager.sundaneseFont
                var mgm = Object.FindObjectOfType<MiniGameManager>();
                if (mgm != null)
                {
                    var so = new SerializedObject(mgm);
                    var prop = so.FindProperty("sundaneseFont");
                    if (prop != null) { prop.objectReferenceValue = font; so.ApplyModifiedProperties(); EditorUtility.SetDirty(mgm); }
                }

                // Wire ke TracePanel.sundaneseFont
                var tp = Object.FindObjectOfType<TracePanel>();
                if (tp != null)
                {
                    var so2 = new SerializedObject(tp);
                    var prop2 = so2.FindProperty("sundaneseFont");
                    if (prop2 != null) { prop2.objectReferenceValue = font; so2.ApplyModifiedProperties(); EditorUtility.SetDirty(tp); }
                }

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Fase2] Font wired in {System.IO.Path.GetFileName(path)}");
            }
            Debug.Log("[Fase2] Sundanese font wire complete.");
        }

        // ── FIX TMP FONTS DIRECTLY IN ALL MG SCENES ─────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Debug - Fix TMP Aksara Fonts (all MG scenes)")]
        public static void FixTMPAksaraFonts()
        {
            string fontPath = "Assets/_Game/Fonts/NotoSansSundanese-Regular SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
            if (font == null)
            {
                Debug.LogError("[Fix] Font not found: " + fontPath);
                return;
            }

            Debug.Log("[Fix] Font: " + font.name
                + " src:" + (font.sourceFontFile != null ? font.sourceFontFile.name : "NULL")
                + " mode:" + font.atlasPopulationMode
                + " atlas:" + font.atlasTextures.Length
                + " chars:" + font.characterTable.Count);

            // Add to TMP global fallback
            var tmps = TMPro.TMP_Settings.instance;
            if (tmps != null)
            {
                var so0 = new SerializedObject(tmps);
                var fallback = so0.FindProperty("m_fallbackFontAssets");
                if (fallback != null)
                {
                    bool found = false;
                    for (int i = 0; i < fallback.arraySize; i++)
                        if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == font) { found = true; break; }
                    if (!found)
                    {
                        fallback.InsertArrayElementAtIndex(0);
                        fallback.GetArrayElementAtIndex(0).objectReferenceValue = font;
                        so0.ApplyModifiedProperties();
                        EditorUtility.SetDirty(tmps);
                        Debug.Log("[Fix] Added to TMP global fallback");
                    }
                }
            }

            string[] scenePaths = {
                "Assets/_Game/Scenes/MG_Swara.unity",
                "Assets/_Game/Scenes/MG_Ngalagena1.unity",
                "Assets/_Game/Scenes/MG_Ngalagena2.unity",
                "Assets/_Game/Scenes/MG_Rarangken.unity",
                "Assets/_Game/Scenes/MG_UjianFinal.unity",
            };

            string[] aksaraTmpNames = { "Txt_AksaraChar", "Txt_QuizAksara", "Txt_CombineA", "Txt_CombineB" };

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int fixed2 = 0;

                // Fix TMP components directly (includeInactive:true — panels are inactive by default)
                foreach (var tmp in Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true))
                {
                    foreach (var n in aksaraTmpNames)
                    {
                        if (tmp.gameObject.name == n)
                        {
                            tmp.font = font;
                            tmp.text = (tmp.gameObject.name == "Txt_AksaraChar") ? "ᮃ" : tmp.text;
                            EditorUtility.SetDirty(tmp);
                            fixed2++;
                            Debug.Log("[Fix] " + System.IO.Path.GetFileNameWithoutExtension(path)
                                + " / " + tmp.gameObject.name
                                + " path:" + GetHierarchyPath(tmp.transform));
                            break;
                        }
                    }
                }

                // Fix MiniGameManager.sundaneseFont serialized field
                var mgm = Object.FindObjectOfType<MiniGameManager>(true);
                if (mgm != null)
                {
                    var so = new SerializedObject(mgm);
                    var p = so.FindProperty("sundaneseFont");
                    if (p != null) { p.objectReferenceValue = font; so.ApplyModifiedProperties(); }
                    EditorUtility.SetDirty(mgm);
                }

                // Fix TracePanel.sundaneseFont serialized field
                var tp = Object.FindObjectOfType<TracePanel>(true);
                if (tp != null)
                {
                    var so2 = new SerializedObject(tp);
                    var p2 = so2.FindProperty("sundaneseFont");
                    if (p2 != null) { p2.objectReferenceValue = font; so2.ApplyModifiedProperties(); }
                    EditorUtility.SetDirty(tp);
                }

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Fix] {System.IO.Path.GetFileNameWithoutExtension(path)}: fixed {fixed2} TMP(s)");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Fix] Done — check Console for per-scene results.");
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        // ── RESET PLAYERPREFS (untuk testing ulang) ──────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Debug - Reset ALL PlayerPrefs")]
        public static void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Debug] Semua PlayerPrefs dihapus — game akan mulai dari awal.");
        }

        // ── RUN ALL ─────────────────────────────────────────────────────────

        [MenuItem("Tools/Hayu Ngaksara/Fase2 - Run ALL Setup")]
        public static void RunAll()
        {
            ConfigureNPCs();
            RenameQuizToRefleksi();
            SetupMainMenu();
            WireComboData();
            RecreateSundaneseFont(); // recreate dulu sebelum wire
            WireSundaneseFont();
            Debug.Log("[Fase2] All setup complete!");
        }
    }
}
