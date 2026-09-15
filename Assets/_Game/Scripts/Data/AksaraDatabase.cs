using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    [CreateAssetMenu(fileName = "AksaraDatabase", menuName = "Aksara Sunda/Aksara Database")]
    public class AksaraDatabase : ScriptableObject
    {
        public List<AksaraData> semuaAksara;

        public AksaraData GetByNama(string nama)
        {
            foreach (var aksara in semuaAksara)
            {
                if (aksara.namaHuruf == nama)
                    return aksara;
            }
            return null;
        }

        public List<AksaraData> GetByKategori(KategoriAksara kategori)
        {
            var result = new List<AksaraData>();
            foreach (var aksara in semuaAksara)
            {
                if (aksara.kategori == kategori)
                    result.Add(aksara);
            }
            return result;
        }

        public List<AksaraData> GetRandom(int jumlah)
        {
            var pool = new List<AksaraData>(semuaAksara);
            // Fisher-Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<AksaraData>();
            int ambil = Mathf.Min(jumlah, pool.Count);
            for (int i = 0; i < ambil; i++)
                result.Add(pool[i]);
            return result;
        }
    }
}
