using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Membangun peta sekolah 80x60 tile yang detail, dengan 5 ruang kelas masing-masing NPC.
/// Jalankan dari menu: Tools > Hayu Ngaksara > Build Detailed School Map
/// </summary>
public class SchoolMapBuilder
{
    // ─── Path assets ──────────────────────────────────────────────────────────
    const string RB_PATH   = "Assets/_Game/Temp/Modern tiles_Free/Interiors_free/16x16/Room_Builder_free_16x16.png";
    const string INT_PATH  = "Assets/_Game/Temp/Modern tiles_Free/Interiors_free/16x16/Interiors_free_16x16.png";
    const string TILE_DIR  = "Assets/_Game/Tiles/School/";

    // ─── Map dimensions (tile coords, centered at 0,0) ────────────────────────
    // Map : x[-40..40], y[-30..30]  → 80 wide × 60 tall
    // Building: x[-36..36], y[-26..26]
    const int MAP_W  = 80;  const int MAP_H  = 60;
    const int BLD_L  = -36; const int BLD_R  = 36;
    const int BLD_B  = -26; const int BLD_T  = 26;

    // ─── Koridor Utama (horizontal) ───────────────────────────────────────────
    const int COR_B  = -2;  const int COR_T  = 1;  // y range (3 tile wide)

    // ─── South wing (below corridor) ─────────────────────────────────────────
    const int SW_B   = -25; const int SW_T   = -3;  // y range
    const int UCUP_L = -35; const int UCUP_R  = -12; // Kelas Ucup x
    const int UJIAN_L= -11; const int UJIAN_R = 10;  // Ruang Ujian x (Bu Guru)
    const int ANDR_L = 11;  const int ANDR_R  = 35;  // Kelas Andre x

    // ─── North wing (above corridor) ─────────────────────────────────────────
    const int NW_B   = 2;   const int NW_T   = 25;  // y range
    const int SINT_L = -35; const int SINT_R  = -1;  // Kelas Sinta x
    const int NABI_L = 1;   const int NABI_R  = 35;  // Kelas Nabila x

    // ─── Entrance (south of building) ────────────────────────────────────────
    const int ENT_L  = -6;  const int ENT_R   = 6;
    const int ENT_B  = -29; const int ENT_T   = -26;

    // ─── Tile instances (lazily created) ──────────────────────────────────────
    static Tile _grass, _wall, _wood, _stone, _yellow, _red, _cream, _corridor;

    // ─── Entry point ──────────────────────────────────────────────────────────
    [MenuItem("Tools/Hayu Ngaksara/Build Detailed School Map")]
    public static void BuildSchoolMap()
    {
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Sekolah.unity", OpenSceneMode.Single);
        if (!Directory.Exists(TILE_DIR)) Directory.CreateDirectory(TILE_DIR);

        Debug.Log("[SchoolMap] Membuat tile assets...");
        CreateTiles();

        Debug.Log("[SchoolMap] Menemukan tilemaps...");
        var ground = FindTilemap("Tilemap_Ground");
        var walls  = FindTilemap("Tilemap_Walls");
        if (ground == null || walls == null)
        {
            Debug.LogError("[SchoolMap] Tilemap_Ground atau Tilemap_Walls tidak ditemukan!");
            return;
        }

        Debug.Log("[SchoolMap] Menghapus tilemap lama...");
        ground.ClearAllTiles();
        walls.ClearAllTiles();

        Debug.Log("[SchoolMap] Melukis peta...");
        PaintGrass(ground);
        PaintBuilding(ground, walls);

        Debug.Log("[SchoolMap] Menambah furniture...");
        DeleteOldFurnitureParent();
        AddFurnitureAll();

        Debug.Log("[SchoolMap] Memindahkan NPC...");
        RepositionNPCs();

        Debug.Log("[SchoolMap] Update boundaries...");
        UpdateBoundaries();

        EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[SchoolMap] SELESAI! Peta sekolah baru telah dibangun.");
    }

    // ─── 1. Buat tile assets dari Room_Builder sprites ────────────────────────
    static void CreateTiles()
    {
        _grass     = LoadOrCreate("tile_grass",     "Assets/_Game/Tiles/tile_grass.asset",    null);
        _wall      = LoadOrCreate("tile_wall",      "Assets/_Game/Tiles/tile_wall.asset",     null);
        _wood      = MakeRBTile("tile_rb_wood",     0, 10);  // brown wood floor row 10
        _stone     = MakeRBTile("tile_rb_stone",    13, 8);  // gray stone row 8 right-side cols
        _yellow    = MakeRBTile("tile_rb_yellow",   0, 6);   // yellow floor row 6
        _red       = MakeRBTile("tile_rb_red",      0, 4);   // red/terra floor row 4
        _cream     = MakeRBTile("tile_rb_cream",    0, 16);  // cream/beige floor row 16
        _corridor  = MakeRBTile("tile_rb_corridor", 0, 14);  // light floor row 14 for corridor
    }

    static Tile LoadOrCreate(string tileName, string existPath, Sprite spr)
    {
        var t = AssetDatabase.LoadAssetAtPath<Tile>(existPath);
        if (t != null) return t;
        t = ScriptableObject.CreateInstance<Tile>();
        if (spr != null) t.sprite = spr;
        AssetDatabase.CreateAsset(t, existPath);
        return t;
    }

    static Tile MakeRBTile(string tileName, int col, int row)
    {
        string assetPath = TILE_DIR + tileName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (existing != null) return existing;

        string sprName = "RB_" + col + "_" + row;
        Sprite spr = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(RB_PATH))
            if (obj is Sprite s && s.name == sprName) { spr = s; break; }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = spr;
        tile.color  = Color.white;
        AssetDatabase.CreateAsset(tile, assetPath);
        return tile;
    }

    // ─── 2. Paint grass (seluruh map background) ─────────────────────────────
    static void PaintGrass(Tilemap tm)
    {
        for (int x = -40; x <= 40; x++)
        for (int y = -30; y <= 30; y++)
            tm.SetTile(new Vector3Int(x, y, 0), _grass);
    }

    // ─── 3. Paint building (floors + walls) ───────────────────────────────────
    static void PaintBuilding(Tilemap ground, Tilemap walls)
    {
        // ── Entrance path dari grass ke pintu ─────────────────────────────────
        for (int x = ENT_L; x <= ENT_R; x++)
        for (int y = -30; y < ENT_B; y++)
            ground.SetTile(new Vector3Int(x, y, 0), _red);

        // ── Entrance hall (area depan, di luar exterior wall) ─────────────────
        for (int x = ENT_L; x <= ENT_R; x++)
        for (int y = ENT_B; y < BLD_B; y++)
            ground.SetTile(new Vector3Int(x, y, 0), _cream);

        // ── Exterior walls ────────────────────────────────────────────────────
        // South wall (kecuali pintu entrance)
        for (int x = BLD_L; x <= BLD_R; x++)
        {
            bool isDoor = (x >= ENT_L && x <= ENT_R);
            walls.SetTile(new Vector3Int(x, BLD_B, 0), isDoor ? null : _wall);
            if (isDoor) ground.SetTile(new Vector3Int(x, BLD_B, 0), _cream);
        }
        // North wall
        for (int x = BLD_L; x <= BLD_R; x++)
            walls.SetTile(new Vector3Int(x, BLD_T, 0), _wall);
        // West wall
        for (int y = BLD_B; y <= BLD_T; y++)
            walls.SetTile(new Vector3Int(BLD_L, y, 0), _wall);
        // East wall
        for (int y = BLD_B; y <= BLD_T; y++)
            walls.SetTile(new Vector3Int(BLD_R, y, 0), _wall);

        // ── Main horizontal corridor ──────────────────────────────────────────
        for (int x = BLD_L + 1; x < BLD_R; x++)
        for (int y = COR_B; y <= COR_T; y++)
            ground.SetTile(new Vector3Int(x, y, 0), _stone);

        // ── South wing floors ─────────────────────────────────────────────────
        // Kelas Ucup (Ngalagena2)
        PaintRoom(ground, walls, UCUP_L, SW_B, UCUP_R, SW_T, _wood, 'W', hasDoorS: false, hasDoorN: true, hasDoorE: false, hasDoorW: false);

        // Ruang Ujian (Bu Guru)
        PaintRoom(ground, walls, UJIAN_L, SW_B, UJIAN_R, SW_T, _yellow, 'E', hasDoorS: false, hasDoorN: true, hasDoorE: false, hasDoorW: false);

        // Kelas Andre (Rarangken)
        PaintRoom(ground, walls, ANDR_L, SW_B, ANDR_R, SW_T, _wood, 'E', hasDoorS: false, hasDoorN: true, hasDoorE: false, hasDoorW: false);

        // ── North wing floors ─────────────────────────────────────────────────
        // Kelas Sinta (Swara)
        PaintRoom(ground, walls, SINT_L, NW_B, SINT_R, NW_T, _wood, 'W', hasDoorS: true, hasDoorN: false, hasDoorE: false, hasDoorW: false);

        // Kelas Nabila (Ngalagena1)
        PaintRoom(ground, walls, NABI_L, NW_B, NABI_R, NW_T, _wood, 'E', hasDoorS: true, hasDoorN: false, hasDoorE: false, hasDoorW: false);

        // ── Interior walls between south rooms ────────────────────────────────
        // Dinding antara Ucup dan Ujian
        for (int y = SW_B; y <= SW_T; y++)
            walls.SetTile(new Vector3Int(UCUP_R + 1, y, 0), _wall);
        // Dinding antara Ujian dan Andre
        for (int y = SW_B; y <= SW_T; y++)
            walls.SetTile(new Vector3Int(ANDR_L - 1, y, 0), _wall);
        // Dinding antara Sinta dan Nabila
        for (int y = NW_B; y <= NW_T; y++)
            walls.SetTile(new Vector3Int(SINT_R + 1, y, 0), _wall);

        // ── Lobby/hall area (entrance dalam) ─────────────────────────────────
        for (int x = BLD_L + 1; x < BLD_R; x++)
        for (int y = BLD_B + 1; y < SW_B; y++)
            ground.SetTile(new Vector3Int(x, y, 0), _cream);
    }

    // Paint satu ruangan: floor + 4 sisi wall + 1 door di sisi tertentu
    // doorSide: 'N','S','E','W' — sisi yang ada pintu (door width = 3 tile di tengah)
    static void PaintRoom(Tilemap ground, Tilemap walls,
                          int l, int b, int r, int t,
                          Tile floorTile, char doorSide,
                          bool hasDoorS, bool hasDoorN, bool hasDoorE, bool hasDoorW)
    {
        // Fill floor
        for (int x = l + 1; x < r; x++)
        for (int y = b + 1; y < t; y++)
            ground.SetTile(new Vector3Int(x, y, 0), floorTile);

        // Floor di posisi wall juga (wall layer di atas)
        for (int x = l; x <= r; x++) ground.SetTile(new Vector3Int(x, b, 0), floorTile);
        for (int x = l; x <= r; x++) ground.SetTile(new Vector3Int(x, t, 0), floorTile);
        for (int y = b; y <= t; y++) ground.SetTile(new Vector3Int(l, y, 0), floorTile);
        for (int y = b; y <= t; y++) ground.SetTile(new Vector3Int(r, y, 0), floorTile);

        int midX = (l + r) / 2;
        int midY = (b + t) / 2;

        // South wall
        for (int x = l; x <= r; x++)
        {
            bool isDoor = hasDoorS && (x >= midX - 1 && x <= midX + 1);
            if (!isDoor) walls.SetTile(new Vector3Int(x, b, 0), _wall);
        }
        // North wall
        for (int x = l; x <= r; x++)
        {
            bool isDoor = hasDoorN && (x >= midX - 1 && x <= midX + 1);
            if (!isDoor) walls.SetTile(new Vector3Int(x, t, 0), _wall);
        }
        // West wall
        for (int y = b; y <= t; y++)
        {
            bool isDoor = hasDoorW && (y >= midY - 1 && y <= midY + 1);
            if (!isDoor) walls.SetTile(new Vector3Int(l, y, 0), _wall);
        }
        // East wall
        for (int y = b; y <= t; y++)
        {
            bool isDoor = hasDoorE && (y >= midY - 1 && y <= midY + 1);
            if (!isDoor) walls.SetTile(new Vector3Int(r, y, 0), _wall);
        }
    }

    // ─── 4. Furniture ──────────────────────────────────────────────────────────
    static void DeleteOldFurnitureParent()
    {
        var old = GameObject.Find("Furniture");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
    }

    static void AddFurnitureAll()
    {
        var furParent = new GameObject("Furniture");
        furParent.transform.position = Vector3.zero;

        // NPC classrooms: desk rows + teacher desk + board + plants
        // Kelas Sinta (Swara): NW  x[SINT_L+1, SINT_R-1], y[NW_B+1, NW_T-1]
        FillClassroom(furParent, "Kelas_Sinta",
            SINT_L + 2, NW_B + 2, SINT_R - 2, NW_T - 2,
            teacherSide: 'N', npcName: "Sinta (Swara)");

        // Kelas Nabila (Ngala1): NE  x[NABI_L+1, NABI_R-1], y[NW_B+1, NW_T-1]
        FillClassroom(furParent, "Kelas_Nabila",
            NABI_L + 2, NW_B + 2, NABI_R - 2, NW_T - 2,
            teacherSide: 'N', npcName: "Nabila (Ngalagena1)");

        // Kelas Ucup (Ngala2): SW  x[UCUP_L+1, UCUP_R-1], y[SW_B+1, SW_T-1]
        FillClassroom(furParent, "Kelas_Ucup",
            UCUP_L + 2, SW_B + 2, UCUP_R - 2, SW_T - 2,
            teacherSide: 'S', npcName: "Ucup (Ngalagena2)");

        // Kelas Andre (Rarangken): SE  x[ANDR_L+1, ANDR_R-1], y[SW_B+1, SW_T-1]
        FillClassroom(furParent, "Kelas_Andre",
            ANDR_L + 2, SW_B + 2, ANDR_R - 2, SW_T - 2,
            teacherSide: 'S', npcName: "Andre (Rarangken)");

        // Ruang Ujian (Bu Guru): Center south
        FillExamRoom(furParent, "Ruang_Ujian",
            UJIAN_L + 1, SW_B + 2, UJIAN_R - 1, SW_T - 2);

        EditorUtility.SetDirty(furParent);
    }

    // Isi satu ruang kelas dengan meja, kursi, papan tulis, tanaman
    static void FillClassroom(GameObject parent, string roomName,
                               int l, int b, int r, int t, char teacherSide, string npcName)
    {
        var roomObj = new GameObject(roomName);
        roomObj.transform.SetParent(parent.transform, false);

        int w = r - l; int h = t - b;

        // Papan tulis (chalkboard) — gunakan colored quad placeholder
        PlaceProp(roomObj, "Papan_Tulis", GetIntSprite(64),   // desk-like sprite
            teacherSide == 'N' ? new Vector2((l + r) * 0.5f, t - 0.5f)
                               : new Vector2((l + r) * 0.5f, b + 0.5f),
            new Color(0.2f, 0.5f, 0.2f), sortOrder: 3);

        // Meja guru
        Vector2 teacherPos = teacherSide == 'N'
            ? new Vector2((l + r) * 0.5f, t - 2.5f)
            : new Vector2((l + r) * 0.5f, b + 2.5f);
        PlaceProp(roomObj, "Meja_Guru", GetIntSprite(80), teacherPos, new Color(0.6f, 0.35f, 0.1f), sortOrder: 2);

        // Meja murid — baris 2 kolom
        int deskRows = Mathf.Clamp((h - 6) / 3, 2, 5);
        int deskCols = 2;
        float deskSpacingX = (w - 4) / (float)(deskCols + 1);
        float deskSpacingY = (h - 8) / (float)(deskRows + 1);

        float startY = teacherSide == 'N' ? b + 4 : t - 4;
        float dirY   = teacherSide == 'N' ? 1 : -1;

        for (int row = 0; row < deskRows; row++)
        for (int col = 0; col < deskCols; col++)
        {
            float dx = l + 2 + deskSpacingX * (col + 1);
            float dy = startY + dirY * deskSpacingY * (row + 1);
            PlaceProp(roomObj, "Meja_Murid_" + row + "_" + col,
                GetIntSprite(96), new Vector2(dx, dy),
                new Color(0.7f, 0.5f, 0.2f), sortOrder: 2);
        }

        // Rak buku di sisi kiri
        for (int i = 0; i < 3; i++)
            PlaceProp(roomObj, "Rak_" + i, GetIntSprite(160),
                new Vector2(l + 0.5f, b + 4 + i * 3),
                new Color(0.4f, 0.25f, 0.08f), sortOrder: 2);

        // Tanaman pojok
        PlaceProp(roomObj, "Tanaman_1", GetIntSprite(224),
            new Vector2(r - 0.5f, t - 0.5f),
            new Color(0.1f, 0.6f, 0.15f), sortOrder: 2);
        PlaceProp(roomObj, "Tanaman_2", GetIntSprite(224),
            new Vector2(r - 0.5f, b + 0.5f),
            new Color(0.1f, 0.6f, 0.15f), sortOrder: 2);

        // Label nama ruangan
        PlaceLabel(roomObj, "Label_" + roomName, npcName,
            new Vector2((l + r) * 0.5f, teacherSide == 'N' ? t + 0.5f : b - 0.5f));
    }

    // Isi Ruang Ujian dengan barisan meja ujian
    static void FillExamRoom(GameObject parent, string roomName, int l, int b, int r, int t)
    {
        var roomObj = new GameObject(roomName);
        roomObj.transform.SetParent(parent.transform, false);

        int w = r - l; int h = t - b;

        // Meja guru/penguji di depan (north)
        PlaceProp(roomObj, "Meja_Penguji", GetIntSprite(80),
            new Vector2((l + r) * 0.5f, t - 2),
            new Color(0.5f, 0.1f, 0.1f), sortOrder: 2);

        // Papan ujian
        PlaceProp(roomObj, "Papan_Ujian", GetIntSprite(64),
            new Vector2((l + r) * 0.5f, t - 0.5f),
            new Color(0.2f, 0.2f, 0.5f), sortOrder: 3);

        // Meja ujian (3 baris, 2 kolom)
        int rows = 3, cols = 2;
        float sx = l + 2, sy = b + 3;
        float gapX = (w - 3) / (float)(cols + 1);
        float gapY = (h - 7) / (float)(rows + 1);
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < cols; col++)
        {
            float dx = sx + gapX * (col + 1);
            float dy = sy + gapY * (rows - 1 - row);
            PlaceProp(roomObj, "Meja_Ujian_" + row + "_" + col,
                GetIntSprite(96), new Vector2(dx, dy),
                new Color(0.8f, 0.75f, 0.5f), sortOrder: 2);
        }

        PlaceLabel(roomObj, "Label_RuangUjian", "Ruang Ujian",
            new Vector2((l + r) * 0.5f, t + 0.5f));
    }

    // Ambil sprite dari Interiors_free (by index 0..259)
    static Sprite GetIntSprite(int idx)
    {
        string name = "Interiors_free_16x16_" + idx;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(INT_PATH))
            if (obj is Sprite s && s.name == name) return s;
        return null;
    }

    // Buat prop SpriteRenderer GameObject
    static void PlaceProp(GameObject parent, string name, Sprite spr,
                           Vector2 pos, Color color, int sortOrder = 2)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite        = spr;
        sr.color         = color;
        sr.sortingOrder  = sortOrder;
        sr.sortingLayerName = "Default";
        EditorUtility.SetDirty(go);
    }

    // Buat label TextMesh
    static void PlaceLabel(GameObject parent, string name, string text, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = Vector3.one * 0.3f;
        var tm = go.AddComponent<TMPro.TextMeshPro>();
        tm.text = text;
        tm.fontSize = 4;
        tm.color = Color.white;
        tm.fontStyle = TMPro.FontStyles.Bold;
        tm.alignment = TMPro.TextAlignmentOptions.Center;
        tm.sortingOrder = 10;
    }

    // ─── 5. Reposisi NPC ke ruang kelas masing-masing ─────────────────────────
    static void RepositionNPCs()
    {
        MoveNPC("NPC_Jajang",  new Vector2(0, -24));           // lobby
        MoveNPC("NPC_Sinta",   new Vector2(SINT_L + 8, NW_B + 10));  // KL Sinta NW
        MoveNPC("NPC_Nabila",  new Vector2(NABI_R - 8, NW_B + 10));  // KL Nabila NE
        MoveNPC("NPC_Ucup",    new Vector2(UCUP_L + 8, SW_B + 10));  // KL Ucup SW
        MoveNPC("NPC_Andre",   new Vector2(ANDR_R - 8, SW_B + 10));  // KL Andre SE
        MoveNPC("NPC_Guru",    new Vector2(0, SW_B + 14));     // Ruang Ujian
        MoveNPC("Player",      new Vector2(0, -24));            // start di lobby
    }

    static void MoveNPC(string name, Vector2 pos)
    {
        var go = FindInactive(name);
        if (go == null) { Debug.LogWarning("[SchoolMap] NPC not found: " + name); return; }
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        EditorUtility.SetDirty(go);
    }

    // ─── 6. Update boundary colliders ─────────────────────────────────────────
    static void UpdateBoundaries()
    {
        var bnd = GameObject.Find("--- BOUNDARIES ---");
        if (bnd == null) { Debug.LogWarning("[SchoolMap] BOUNDARIES not found!"); return; }

        // Update each BoxCollider2D to match new map
        // Harapkan 4 boundary colliders: Top, Bottom, Left, Right
        float halfW = 42f; float halfH = 32f;
        SetBoundary(bnd, "Boundary_Top",    new Vector2(0,  halfH), new Vector2(halfW * 2, 1));
        SetBoundary(bnd, "Boundary_Bottom", new Vector2(0, -halfH), new Vector2(halfW * 2, 1));
        SetBoundary(bnd, "Boundary_Left",   new Vector2(-halfW, 0), new Vector2(1, halfH * 2));
        SetBoundary(bnd, "Boundary_Right",  new Vector2( halfW, 0), new Vector2(1, halfH * 2));
    }

    static void SetBoundary(GameObject parent, string name, Vector2 offset, Vector2 size)
    {
        var go = parent.transform.Find(name)?.gameObject;
        if (go == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<BoxCollider2D>();
        }
        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        go.transform.localPosition = new Vector3(offset.x, offset.y, 0);
        col.size   = size;
        col.offset = Vector2.zero;
        EditorUtility.SetDirty(go);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────
    static Tilemap FindTilemap(string name)
    {
        foreach (var tm in UnityEngine.Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tm.gameObject.name == name) return tm;
        return null;
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
