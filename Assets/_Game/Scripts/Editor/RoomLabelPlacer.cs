using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HayuNgaksara;

/// <summary>
/// Tools > Hayu Ngaksara > Place Room Labels
/// Menempatkan label nama ruangan di scene 00_Sekolah secara otomatis.
/// </summary>
public class RoomLabelPlacer
{
    private struct RoomInfo
    {
        public string name;
        public Vector3 position;
    }

    [MenuItem("Tools/Hayu Ngaksara/Place Room Labels")]
    public static void PlaceLabels()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);

        var rooms = new RoomInfo[]
        {
            new RoomInfo { name = "Kelas Sinta",    position = new Vector3(-17.5f, 14f,  0) },
            new RoomInfo { name = "Kelas Nabila",   position = new Vector3( 17.5f, 14f,  0) },
            new RoomInfo { name = "Kelas Ucup",     position = new Vector3(-22.5f, -12f, 0) },
            new RoomInfo { name = "Ruang Ujian",    position = new Vector3(  0f,   -12f, 0) },
            new RoomInfo { name = "Kelas Andre",    position = new Vector3( 22.5f, -12f, 0) },
        };

        // Cari atau buat parent GameObject
        var parent = GameObject.Find("RoomLabels");
        if (parent == null)
        {
            parent = new GameObject("RoomLabels");
            Undo.RegisterCreatedObjectUndo(parent, "Create RoomLabels");
        }

        foreach (var info in rooms)
        {
            // Hapus label lama dengan nama yang sama bila ada
            var existing = parent.transform.Find(info.name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var go = new GameObject(info.name);
            Undo.RegisterCreatedObjectUndo(go, $"Create RoomLabel {info.name}");
            go.transform.SetParent(parent.transform);
            go.transform.position = info.position;

            var label = go.AddComponent<RoomLabel>();

            // Atur field via SerializedObject agar Unity mencatat perubahan
            var so = new SerializedObject(label);
            so.FindProperty("roomName").stringValue = info.name;
            so.FindProperty("fontSize").floatValue  = 2.5f;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RoomLabelPlacer] 5 room labels placed and scene saved.");
    }
}
