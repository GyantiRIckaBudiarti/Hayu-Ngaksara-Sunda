using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    [Serializable]
    public class StrokePoint
    {
        [Range(0f,1f)] public float x;  // posisi normalized (0-1) di canvas
        [Range(0f,1f)] public float y;
    }

    [Serializable]
    public class AksaraCard
    {
        public string aksaraChar;   // unicode karakter Sunda
        public string latinName;    // misal "ka", "nga"
        public string audioFile;    // nama file tanpa ext, misal "ka"
        [TextArea(1,2)]
        public string description;  // keterangan tambahan
        public Sprite sprite;       // PNG aksara dari atlas
        public List<StrokePoint> strokeOrder = new List<StrokePoint>(); // urutan coretan (opsional)
    }


}
