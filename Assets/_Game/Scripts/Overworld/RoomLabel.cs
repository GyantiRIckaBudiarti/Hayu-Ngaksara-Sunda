using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Menampilkan nama ruangan sebagai teks 3D di world space.
    /// Tambahkan ke GameObject kosong di tengah setiap ruangan.
    /// Nama ruangan bisa diisi dari Inspector atau via SchoolMapBuilder.
    /// </summary>
    [ExecuteAlways]
    public class RoomLabel : MonoBehaviour
    {
        [SerializeField] private string roomName = "Nama Ruangan";
        [SerializeField] private float  fontSize = 2.5f;
        [SerializeField] private Color  textColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private string sortingLayer = "Characters";
        [SerializeField] private int    sortingOrder = 3;

        private TextMeshPro _tmp;

        private void Awake() => Build();

        private void OnValidate() => Build();

        private void Build()
        {
            if (_tmp == null)
                _tmp = GetComponentInChildren<TextMeshPro>();

            if (_tmp == null)
            {
                // Jangan buat material instance baru di edit mode (menyebabkan material leak warning)
                if (!Application.isPlaying) return;
                var go        = new GameObject("RoomLabelText");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;
                _tmp = go.AddComponent<TextMeshPro>();
            }

            _tmp.text      = roomName;
            _tmp.fontSize  = fontSize;
            _tmp.fontStyle = FontStyles.Bold;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.color     = textColor;

            // outlineWidth/outlineColor mengakses renderer.material (instance) — hanya set saat play mode
            // agar tidak menyebabkan material leak di edit mode
            if (Application.isPlaying)
            {
                _tmp.outlineWidth = 0.3f;
                _tmp.outlineColor = new Color32(0, 0, 0, 200);
            }

            var mr = _tmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayer;
                mr.sortingOrder     = sortingOrder;
            }
        }
    }
}
