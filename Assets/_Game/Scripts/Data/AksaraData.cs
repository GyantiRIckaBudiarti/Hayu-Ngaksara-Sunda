using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    [System.Serializable]
    public struct RarangkenData
    {
        public string namaRarangken;
        public string bunyiVokal;
        public Sprite spriteRarangken;
        public Sprite spriteHurufDenganRarangken;
    }

    [CreateAssetMenu(fileName = "AksaraData", menuName = "Aksara Sunda/Aksara Data")]
    public class AksaraData : ScriptableObject
    {
        public string namaHuruf;
        public KategoriAksara kategori;
        public Sprite spriteHuruf;
        public AudioClip audioPelafalan;
        public string fonetik;
        public List<RarangkenData> rarangkenList;

        public RarangkenData GetRarangken(string nama)
        {
            foreach (var r in rarangkenList)
            {
                if (r.namaRarangken == nama)
                    return r;
            }
            return default;
        }

        public bool HasRarangken(string nama)
        {
            foreach (var r in rarangkenList)
            {
                if (r.namaRarangken == nama)
                    return true;
            }
            return false;
        }
    }
}
