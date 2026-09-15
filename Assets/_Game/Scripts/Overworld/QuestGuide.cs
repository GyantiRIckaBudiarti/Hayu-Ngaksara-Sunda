using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    /// <summary>
    /// Menampilkan panah "▼" di atas NPC yang harus dikunjungi selanjutnya.
    /// Tambahkan komponen ini ke GameObject QuestGuide di scene Sekolah.
    /// </summary>
    public class QuestGuide : MonoBehaviour
    {
        [System.Serializable]
        public class NPCEntry
        {
            public ChapterID chapter;
            public Transform npcTransform;
        }

        [Header("NPC Map")]
        [SerializeField] private List<NPCEntry> npcMap = new List<NPCEntry>();

        [Header("Arrow Settings")]
        [SerializeField] private float arrowOffsetY = 1.1f;
        [SerializeField] private float bobSpeed     = 3f;
        [SerializeField] private float bobAmount    = 0.12f;
        [SerializeField] private Color arrowColor   = new Color(1f, 0.9f, 0.1f);

        private GameObject    _arrow;
        private TextMeshPro   _arrowTmp;
        private Transform     _currentTarget;
        private float         _bobTime;

        private void Start()
        {
            _arrow = CreateArrow();
            _arrow.SetActive(false);
        }

        private void Update()
        {
            RefreshTarget();

            if (_currentTarget == null)
            {
                _arrow.SetActive(false);
                return;
            }

            _arrow.SetActive(true);
            _bobTime += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(_bobTime) * bobAmount;
            _arrow.transform.position = _currentTarget.position + new Vector3(0, arrowOffsetY + bob, 0);
        }

        private void RefreshTarget()
        {
            if (ChapterManager.Instance == null) return;

            Transform found = null;
            foreach (var entry in npcMap)
            {
                var prog = ChapterManager.Instance.GetProgress(entry.chapter);
                if (prog != null && prog.unlocked && !prog.completed
                    && !ChapterManager.Instance.IsSubActivityDone(entry.chapter, "refleksi"))
                {
                    found = entry.npcTransform;
                    break;
                }
            }
            _currentTarget = found;
        }

        private GameObject CreateArrow()
        {
            var go = new GameObject("QuestArrow");

            var tmp      = go.AddComponent<TextMeshPro>();
            tmp.text     = "▼";
            tmp.fontSize = 3f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = arrowColor;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(80, 60, 0, 255);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Characters";
                mr.sortingOrder     = 20;
            }

            _arrowTmp = tmp;
            return go;
        }
    }
}
