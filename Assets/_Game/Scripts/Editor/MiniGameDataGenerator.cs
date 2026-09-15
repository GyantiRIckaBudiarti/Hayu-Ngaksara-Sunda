using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using HayuNgaksara;

public class MiniGameDataGenerator
{
    [MenuItem("Tools/Hayu Ngaksara/Generate MiniGame Data")]
    public static void Generate()
    {
        string dir = "Assets/_Game/Data/MiniGame";
        Directory.CreateDirectory(Application.dataPath.Replace("Assets","") + dir);

        CreateSwara(dir);
        CreateNgalagena1(dir);
        CreateNgalagena2(dir);
        CreateRarangken(dir);
        CreateUjianFinal(dir);
        CreateAndreCombo(dir);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MiniGameDataGenerator] 6 MiniGameData assets created!");
    }

    static MiniGameData GetOrCreate(string path, ChapterID id, string npc, string title, string intro)
    {
        var ex = AssetDatabase.LoadAssetAtPath<MiniGameData>(path);
        if (ex != null) { ex.cards.Clear(); ex.chapterID=id; ex.npcName=npc; ex.chapterTitle=title; ex.introText=intro; EditorUtility.SetDirty(ex); return ex; }
        var so = ScriptableObject.CreateInstance<MiniGameData>();
        so.chapterID=id; so.npcName=npc; so.chapterTitle=title; so.introText=intro;
        so.cards = new List<AksaraCard>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    static void A(MiniGameData d, string ch, string lat, string aud, string desc)
        => d.cards.Add(new AksaraCard { aksaraChar=ch, latinName=lat, audioFile=aud, description=desc });

    static void CreateSwara(string dir)
    {
        var d = GetOrCreate(dir+"/MG_Swara.asset", ChapterID.Swara, "Sinta",
            "Bab 1: Aksara Swara", "Halo! Aku Sinta. Ayo belajar 6 Aksara Swara (huruf vokal) Sunda!");
        A(d, "ᮃ", "a",  "a",  "Vokal A, seperti kata 'ayam'");
        A(d, "ᮄ", "i",  "i",  "Vokal I, seperti kata 'ikan'");
        A(d, "ᮅ", "u",  "u",  "Vokal U, seperti kata 'uang'");
        A(d, "ᮆ", "e",  "e",  "Vokal E terbuka, seperti 'enak'");
        A(d, "ᮇ", "eu", "eu", "Vokal EU, khas Bahasa Sunda");
        A(d, "ᮈ", "o",  "o",  "Vokal O, seperti kata 'obat'");
        EditorUtility.SetDirty(d);
    }

    static void CreateNgalagena1(string dir)
    {
        var d = GetOrCreate(dir+"/MG_Ngalagena1.asset", ChapterID.Ngalagena1, "Nabila",
            "Bab 2: Ngalagena (ka - na)", "Aku Nabila! Ayo belajar 9 aksara konsonan pertama Sunda!");
        A(d, "ᮊ", "ka",  "ka",  "Konsonan KA");
        A(d, "ᮌ", "ga",  "ga",  "Konsonan GA");
        A(d, "ᮍ", "nga", "nga", "Konsonan NGA");
        A(d, "ᮎ", "ca",  "ca",  "Konsonan CA");
        A(d, "ᮏ", "ja",  "ja",  "Konsonan JA");
        A(d, "ᮑ", "nya", "nya", "Konsonan NYA");
        A(d, "ᮒ", "ta",  "ta",  "Konsonan TA");
        A(d, "ᮓ", "da",  "da",  "Konsonan DA");
        A(d, "ᮔ", "na",  "na",  "Konsonan NA");
        EditorUtility.SetDirty(d);
    }

    static void CreateNgalagena2(string dir)
    {
        var d = GetOrCreate(dir+"/MG_Ngalagena2.asset", ChapterID.Ngalagena2, "Ucup",
            "Bab 3: Ngalagena (pa - ha)", "Gue Ucup! Sekarang belajar 9 aksara konsonan berikutnya!");
        A(d, "ᮕ", "pa", "pa", "Konsonan PA");
        A(d, "ᮘ", "ba", "ba", "Konsonan BA");
        A(d, "ᮙ", "ma", "ma", "Konsonan MA");
        A(d, "ᮚ", "ya", "ya", "Konsonan YA");
        A(d, "ᮛ", "ra", "ra", "Konsonan RA");
        A(d, "ᮜ", "la", "la", "Konsonan LA");
        A(d, "ᮝ", "wa", "wa", "Konsonan WA");
        A(d, "ᮞ", "sa", "sa", "Konsonan SA");
        A(d, "ᮠ", "ha", "ha", "Konsonan HA");
        EditorUtility.SetDirty(d);
    }

    static void CreateRarangken(string dir)
    {
        var d = GetOrCreate(dir+"/MG_Rarangken.asset", ChapterID.Rarangken, "Andre",
            "Bab 4: Rarangken (Tanda Vokal)", "Bro, gue Andre! Rarangken = tanda vokal yang dipakai di konsonan.");
        A(d, "ᮊᮤ", "panghulu (i)",  "i",  "Tanda vokal I — ka+panghulu = ki");
        A(d, "ᮊᮥ", "panyuku (u)",   "u",  "Tanda vokal U — ka+panyuku = ku");
        A(d, "ᮊᮦ", "paneleng (e)",  "e",  "Tanda vokal E — ka+paneleng = ke");
        A(d, "ᮊᮧ", "panolong (o)",  "o",  "Tanda vokal O — ka+panolong = ko");
        A(d, "ᮊᮨ", "pamepet (eu)",  "eu", "Tanda vokal EU — ka+pamepet = keu");
        A(d, "ᮊ᮪", "pamaeh",        "ka", "Tanda mematikan — ka+pamaeh = k");
        EditorUtility.SetDirty(d);
    }

    static void CreateUjianFinal(string dir)
    {
        var d = GetOrCreate(dir+"/MG_UjianFinal.asset", ChapterID.UjianFinal, "Jajang",
            "Ujian Final!", "Ini ujian beneran! Semua aksara yang udah kamu pelajari akan diuji. Ayo!");
        d.gameMode = HayuNgaksara.MiniGameMode.CombineLetters;
        A(d, "ᮃ", "a",  "a",  "Swara A");
        A(d, "ᮄ", "i",  "i",  "Swara I");
        A(d, "ᮇ", "eu", "eu", "Swara EU");
        A(d, "ᮈ", "o",  "o",  "Swara O");
        A(d, "ᮊ", "ka", "ka", "Ngalagena KA");
        A(d, "ᮍ", "nga","nga","Ngalagena NGA");
        A(d, "ᮒ", "ta", "ta", "Ngalagena TA");
        A(d, "ᮕ", "pa", "pa", "Ngalagena PA");
        A(d, "ᮙ", "ma", "ma", "Ngalagena MA");
        A(d, "ᮛ", "ra", "ra", "Ngalagena RA");
        A(d, "ᮊᮤ", "panghulu(i)", "i", "Rarangken I");
        A(d, "ᮊᮥ", "panyuku(u)",  "u", "Rarangken U");
        EditorUtility.SetDirty(d);
    }

    static void CreateAndreCombo(string dir)
    {
        var d = GetOrCreate(dir+"/MG_Andre_Combo.asset", ChapterID.Rarangken, "Andre",
            "Latihan Ekstra: Gabung Aksara",
            "Yo! Aku Andre. Kita gabungkan konsonan dengan rarangken jadi suku kata. Asyik kan?");
        d.gameMode = HayuNgaksara.MiniGameMode.CombineLetters;

        // Konsonan dasar (aksaraChar 1 karakter)
        A(d, "ᮊ", "ka",  "ka",  "Konsonan KA");
        A(d, "ᮌ", "ga",  "ga",  "Konsonan GA");
        A(d, "ᮍ", "nga", "nga", "Konsonan NGA");
        A(d, "ᮒ", "ta",  "ta",  "Konsonan TA");
        A(d, "ᮔ", "na",  "na",  "Konsonan NA");
        A(d, "ᮕ", "pa",  "pa",  "Konsonan PA");
        A(d, "ᮙ", "ma",  "ma",  "Konsonan MA");
        A(d, "ᮛ", "ra",  "ra",  "Konsonan RA");
        A(d, "ᮜ", "la",  "la",  "Konsonan LA");
        A(d, "ᮞ", "sa",  "sa",  "Konsonan SA");

        // Rarangken komposit (aksaraChar 2 karakter = konsonan+tanda vokal) — dipakai sebagai daftar rarangken
        A(d, "ᮊᮤ", "panghulu (i)",  "i",  "Tanda vokal I  — ka+panghulu = ki");
        A(d, "ᮊᮥ", "panyuku (u)",   "u",  "Tanda vokal U  — ka+panyuku = ku");
        A(d, "ᮊᮦ", "paneleng (e)",  "e",  "Tanda vokal E  — ka+paneleng = ke");
        A(d, "ᮊᮧ", "panolong (o)",  "o",  "Tanda vokal O  — ka+panolong = ko");
        A(d, "ᮊᮨ", "pamepet (eu)",  "eu", "Tanda vokal EU — ka+pamepet = keu");

        EditorUtility.SetDirty(d);
    }
}
