using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HayuNgaksara;

namespace HayuNgaksara.Editor
{
    public class CharacterPlaceholderGenerator : EditorWindow
    {
        private const string OutputFolder = "Assets/_Game/Sprites/Characters/Placeholders";
        private const int    SpriteW      = 128;
        private const int    SpriteH      = 256;

        // Data tiap karakter
        private static readonly (string name, Color baseColor)[] Characters =
        {
            ("Jajang", new Color(0.18f, 0.29f, 0.56f)),   // Biru Indigo
            ("Sinta",  new Color(0.55f, 0.23f, 0.23f)),   // Merah Soga
            ("Nabila", new Color(0.29f, 0.49f, 0.35f)),   // Hijau Daun
            ("Ucup",   new Color(0.91f, 0.72f, 0.29f)),   // Kuning Kunyit
            ("Andre",  new Color(0.42f, 0.27f, 0.14f)),   // Coklat Kayu
            ("Guru",   new Color(0.42f, 0.29f, 0.56f)),   // Ungu
        };

        // Tint per ekspresi (diterapkan di atas base color)
        private static readonly (Expression expr, float brightness, float saturation)[] Expressions =
        {
            (Expression.Default,   1.00f, 1.00f),
            (Expression.Happy,     1.25f, 0.80f),
            (Expression.Clapping,  1.20f, 0.75f),
            (Expression.Sad,       0.70f, 0.60f),
            (Expression.Angry,     1.10f, 1.30f),
            (Expression.Surprised, 1.30f, 0.70f),
            (Expression.Running,   1.15f, 1.10f),
        };

        [MenuItem("Tools/Hayu Ngaksara/Generate Character Placeholders")]
        public static void ShowWindow()
        {
            GetWindow<CharacterPlaceholderGenerator>("Placeholder Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Character Placeholder Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GUILayout.Label("Akan membuat sprite placeholder untuk semua karakter\ndan semua ekspresi, lalu membuat prefab di scene.", EditorStyles.helpBox);
            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Semua Sprites + Prefab", GUILayout.Height(40)))
                GenerateAll();

            EditorGUILayout.Space();
            if (GUILayout.Button("Hanya Generate Sprites"))
                GenerateSprites();

            if (GUILayout.Button("Hanya Buat Prefab (sprites sudah ada)"))
                CreateAllPrefabs();
        }

        private static void GenerateAll()
        {
            GenerateSprites();
            CreateAllPrefabs();
            Debug.Log("[PlaceholderGen] Selesai! Sprites + Prefab sudah dibuat.");
        }

        // ── Sprite Generation ──────────────────────────────────────────

        private static void GenerateSprites()
        {
            EnsureFolder(OutputFolder);

            foreach (var (charName, baseColor) in Characters)
            {
                EnsureFolder($"{OutputFolder}/{charName}");

                foreach (var (expr, bright, sat) in Expressions)
                {
                    string path = $"{OutputFolder}/{charName}/{charName}_{expr}.png";
                    if (File.Exists(path)) continue;

                    Color finalColor = AdjustColor(baseColor, bright, sat);
                    Texture2D tex    = DrawCharacterTexture(charName, expr, finalColor);
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
            }

            AssetDatabase.Refresh();

            // Set import settings ke Sprite
            foreach (var (charName, _) in Characters)
                foreach (var (expr, _, _) in Expressions)
                {
                    string path = $"{OutputFolder}/{charName}/{charName}_{expr}.png";
                    SetSpriteImportSettings(path);
                }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlaceholderGen] Sprites dibuat di {OutputFolder}");
        }

        private static Texture2D DrawCharacterTexture(string charName, Expression expr, Color bodyColor)
        {
            var tex = new Texture2D(SpriteW, SpriteH, TextureFormat.RGBA32, false);

            // Background transparan
            Color clear = new Color(0, 0, 0, 0);
            var   pixels = new Color[SpriteW * SpriteH];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);

            // Badan (rectangle utama)
            FillRect(tex, 20, 0, 88, 180, bodyColor);

            // Kepala (lingkaran via rectangle rounded simulasi)
            Color headColor = new Color(bodyColor.r * 1.2f, bodyColor.g * 1.1f, bodyColor.b * 1.1f, 1f);
            FillOval(tex, 32, 180, 64, 64, headColor);

            // Label ekspresi
            // (Tidak bisa draw text langsung di Texture2D tanpa font — skip untuk placeholder)

            tex.Apply();
            return tex;
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int px = x; px < x + w && px < tex.width;  px++)
            for (int py = y; py < y + h && py < tex.height; py++)
                tex.SetPixel(px, py, c);
        }

        private static void FillOval(Texture2D tex, int cx, int cy, int rw, int rh, Color c)
        {
            float rx = rw * 0.5f, ry = rh * 0.5f;
            int   ox = cx, oy = cy;
            for (int px = ox; px < ox + rw && px < tex.width;  px++)
            for (int py = oy; py < oy + rh && py < tex.height; py++)
            {
                float dx = (px - ox - rx) / rx;
                float dy = (py - oy - ry) / ry;
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(px, py, c);
            }
        }

        private static Color AdjustColor(Color base_, float brightness, float saturation)
        {
            Color.RGBToHSV(base_, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * saturation);
            v = Mathf.Clamp01(v * brightness);
            return Color.HSVToRGB(h, s, v);
        }

        private static void SetSpriteImportSettings(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType        = TextureImporterType.Sprite;
            importer.spriteImportMode   = SpriteImportMode.Single;
            importer.spritePivot        = new Vector2(0.5f, 0f); // pivot di bawah
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.filterMode         = FilterMode.Point;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // ── Prefab Creation ────────────────────────────────────────────

        private static void CreateAllPrefabs()
        {
            EnsureFolder("Assets/_Game/Prefabs/Characters");

            foreach (var (charName, _) in Characters)
                CreateCharacterPrefab(charName);

            AssetDatabase.SaveAssets();
            Debug.Log("[PlaceholderGen] Prefab karakter selesai dibuat.");
        }

        private static void CreateCharacterPrefab(string charName)
        {
            string prefabPath = $"Assets/_Game/Prefabs/Characters/{charName}.prefab";
            if (File.Exists(prefabPath)) return;

            // Root GO
            var root = new GameObject(charName);

            // SpriteRenderer
            var sr    = root.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder     = 0;

            // Load default sprite
            var defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{OutputFolder}/{charName}/{charName}_Default.png");
            if (defaultSprite != null) sr.sprite = defaultSprite;

            // BoxCollider2D
            var col    = root.AddComponent<BoxCollider2D>();
            col.size   = new Vector2(0.88f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);

            // Character script
            string scriptName = charName + "Controller";
            var    monoType   = GetTypeByName(scriptName);
            if (monoType != null)
            {
                var comp = root.AddComponent(monoType) as CharacterBase;

                // Dialog bubble child
                var bubble      = new GameObject("DialogBubble");
                bubble.transform.SetParent(root.transform, false);
                bubble.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                bubble.SetActive(false);

                var bubbleSR    = bubble.AddComponent<SpriteRenderer>();
                bubbleSR.sortingOrder = 5;

                var textGO      = new GameObject("DialogText");
                textGO.transform.SetParent(bubble.transform, false);
                var tmp         = textGO.AddComponent<TMPro.TextMeshPro>();
                tmp.fontSize    = 4f;
                tmp.color       = Color.black;
                tmp.alignment   = TMPro.TextAlignmentOptions.Center;
                tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

                // Assign references via SerializedObject
                var so = new SerializedObject(comp);
                var dialogBubbleProp = so.FindProperty("dialogBubble");
                var dialogTextProp   = so.FindProperty("dialogText");
                if (dialogBubbleProp != null) dialogBubbleProp.objectReferenceValue = bubble;
                if (dialogTextProp   != null) dialogTextProp.objectReferenceValue   = tmp;
                so.ApplyModifiedProperties();
            }

            // Simpan sebagai prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[PlaceholderGen] Prefab {charName} dibuat.");
        }

        private static System.Type GetTypeByName(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType($"HayuNgaksara.{name}");
                if (t != null) return t;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
