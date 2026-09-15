using System.Collections;
using UnityEngine;
using TMPro;

namespace HayuNgaksara
{
    public class KuisResultPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textSkor;
        [SerializeField] private TextMeshProUGUI textBenar;
        [SerializeField] private TextMeshProUGUI textBintang;
        [SerializeField] private TextMeshProUGUI textWriting; // opsional

        public void Show(int skor, int benar, int total, int bintang,
                         int writingPracticed = 0, int writingTotal = 0)
        {
            StartCoroutine(CountUpScore(skor, benar, total, bintang, writingPracticed, writingTotal));
        }

        private IEnumerator CountUpScore(int targetSkor, int benar, int total, int bintang,
                                         int writingPracticed, int writingTotal)
        {
            float t   = 0f;
            float dur = 1.5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(0, targetSkor, t / dur));
                if (textSkor != null) textSkor.text = $"Skor: {current}";
                yield return null;
            }
            if (textSkor    != null) textSkor.text    = $"Skor: {targetSkor}";
            if (textBenar   != null) textBenar.text   = $"Jawaban benar: {benar}/{total}";
            if (textBintang != null) textBintang.text = $"{new string('★', bintang)}{new string('☆', 3 - bintang)}";

            if (textWriting != null)
            {
                if (writingTotal > 0)
                {
                    int pct = Mathf.RoundToInt((float)writingPracticed / writingTotal * 100f);
                    textWriting.text = $"Latihan menulis: {writingPracticed}/{writingTotal} aksara ({pct}%)";
                    textWriting.gameObject.SetActive(true);
                }
                else
                {
                    textWriting.gameObject.SetActive(false);
                }
            }
        }
    }
}
