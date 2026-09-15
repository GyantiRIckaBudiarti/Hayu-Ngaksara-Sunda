using UnityEditor;
using UnityEngine;
using HayuNgaksara;

namespace HayuNgaksara.Editor
{
    /// <summary>
    /// Assign expression sprites ke CharacterBase di prefab secara otomatis.
    /// Jalankan via Tools > Hayu Ngaksara > Assign Expression Sprites.
    /// </summary>
    public static class ExpressionAssigner
    {
        private const string PlaceholderRoot = "Assets/_Game/Sprites/Characters/Placeholders";
        private const string PrefabRoot      = "Assets/_Game/Prefabs/Characters";

        private static readonly string[] CharNames =
            { "Jajang", "Sinta", "Nabila", "Ucup", "Andre", "Guru" };

        private static readonly Expression[] Expressions = (Expression[])System.Enum.GetValues(typeof(Expression));

        [MenuItem("Tools/Hayu Ngaksara/Assign Expression Sprites ke Prefab")]
        public static void AssignAll()
        {
            foreach (var charName in CharNames)
                AssignToPrefab(charName);

            AssetDatabase.SaveAssets();
            Debug.Log("[ExpressionAssigner] Selesai assign expression sprites ke semua prefab.");
        }

        private static void AssignToPrefab(string charName)
        {
            string prefabPath = $"{PrefabRoot}/{charName}.prefab";
            var    prefab     = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[ExpressionAssigner] Prefab {charName} tidak ditemukan di {prefabPath}");
                return;
            }

            var charBase = prefab.GetComponent<CharacterBase>();
            if (charBase == null)
            {
                Debug.LogWarning($"[ExpressionAssigner] CharacterBase tidak ada di prefab {charName}");
                return;
            }

            var so           = new SerializedObject(charBase);
            var entriesProp  = so.FindProperty("expressionEntries");
            if (entriesProp == null)
            {
                Debug.LogWarning($"[ExpressionAssigner] Field 'expressionEntries' tidak ditemukan di {charName}");
                return;
            }

            entriesProp.ClearArray();
            int idx = 0;

            foreach (var expr in Expressions)
            {
                // Coba load dari folder placeholder dulu; kalau tidak ada, skip
                string spritePath = $"{PlaceholderRoot}/{charName}/{charName}_{expr}.png";
                var    sprite     = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                // Jika tidak ada di placeholder, cari di folder Sprites/Characters/charName
                if (sprite == null)
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"Assets/_Game/Sprites/Characters/{charName}/{charName}_{expr}.png");

                if (sprite == null) continue;

                entriesProp.InsertArrayElementAtIndex(idx);
                var element     = entriesProp.GetArrayElementAtIndex(idx);
                element.FindPropertyRelative("expression").enumValueIndex = (int)expr;
                element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                idx++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);

            Debug.Log($"[ExpressionAssigner] {charName}: {idx} ekspresi di-assign.");
        }
    }
}
