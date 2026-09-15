using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class TraceUI : MonoBehaviour
    {
        [SerializeField] private TraceCanvas    traceCanvas;
        [SerializeField] private SpriteRenderer guideSprite;
        [SerializeField] private Image          progressFill;
        [SerializeField] private Button         btnReset;
        [SerializeField] private Button         btnLanjut;
        [SerializeField] private TextMeshProUGUI txtStatus;

        private AksaraData _currentAksara;
        private bool       _isPassed;

        private void Awake()
        {
            traceCanvas.OnStrokeComplete += EvaluateStroke;
            btnReset.onClick.AddListener(ResetTrace);
            btnLanjut.onClick.AddListener(OnLanjut);
            btnLanjut.interactable = false;
        }

        public void Setup(AksaraData aksara)
        {
            _currentAksara = aksara;
            _isPassed      = false;
            btnLanjut.interactable = false;

            if (guideSprite != null && aksara.spriteHuruf != null)
            {
                guideSprite.sprite = aksara.spriteHuruf;
                var c = guideSprite.color;
                c.a = 0.4f;
                guideSprite.color = c;
            }

            traceCanvas.ClearAll();
            if (progressFill != null) progressFill.fillAmount = 0f;
            if (txtStatus != null) txtStatus.text = "Ikuti garis panduan!";
        }

        private void EvaluateStroke(List<Vector3> userPoints)
        {
            // Guide points minimal — gunakan bounding box aksara sebagai guide sederhana
            var guide = GetSimpleGuide();
            var result = TraceEvaluator.Evaluate(userPoints, guide);

            if (progressFill != null) progressFill.fillAmount = result.score;

            if (result.passed)
            {
                _isPassed = true;
                traceCanvas.SetTraceColor(Color.green);
                btnLanjut.interactable = true;
                if (txtStatus != null) txtStatus.text = "Bagus! Tekan Lanjut.";
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayPelafalan(_currentAksara.audioPelafalan);
            }
            else
            {
                traceCanvas.SetTraceColor(Color.red);
                if (txtStatus != null) txtStatus.text = "Coba lebih dekat ke garis!";
            }
        }

        private List<Vector3> GetSimpleGuide()
        {
            // Guide path sederhana berdasarkan posisi guide sprite
            // Dalam produksi, data guide per aksara disimpan di AksaraData
            var bounds = guideSprite != null ? guideSprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            return new List<Vector3>
            {
                new Vector3(bounds.min.x, bounds.max.y, 0),
                new Vector3(bounds.center.x, bounds.center.y, 0),
                new Vector3(bounds.max.x, bounds.min.y, 0)
            };
        }

        private void ResetTrace()
        {
            _isPassed = false;
            btnLanjut.interactable = false;
            traceCanvas.ClearAll();
            if (progressFill != null) progressFill.fillAmount = 0f;
            if (txtStatus != null) txtStatus.text = "Ikuti garis panduan!";
        }

        public event System.Action OnTraceComplete;

        private void OnLanjut()
        {
            OnTraceComplete?.Invoke();
        }
    }
}
