using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class RarangkenSelector : MonoBehaviour
    {
        public event Action OnAnswerCorrect;
        public event Action OnAnswerWrong;

        [Header("UI")]
        [SerializeField] private SpriteRenderer previewHuruf;
        [SerializeField] private TextMeshProUGUI labelFonetik;
        [SerializeField] private Button[]        tombolRarangken;
        [SerializeField] private TextMeshProUGUI[] labelTombol;
        [SerializeField] private Image[]         imageTombol;
        [SerializeField] private Button          btnKonfirmasi;

        [Header("Feedback")]
        [SerializeField] private AudioClip sfxBenar;
        [SerializeField] private AudioClip sfxSalah;

        private AksaraData _aksaraBase;
        private string     _targetRarangken;
        private string     _selectedRarangken;
        private int        _wrongStreak;

        private static readonly string[] SemuaRarangken = {
            "panghulu", "panyuku", "paneuleung", "paneleng", "panolong", "pamepet", "tanpa vokal"
        };
        private static readonly string[] BunyiVokal = {
            "i", "u", "eu", "é", "o", "e", "a"
        };

        private void Awake()
        {
            for (int i = 0; i < tombolRarangken.Length; i++)
            {
                int idx = i;
                tombolRarangken[i].onClick.AddListener(() => SelectRarangken(idx));
            }
            btnKonfirmasi.onClick.AddListener(Konfirmasi);
            btnKonfirmasi.interactable = false;
        }

        public void Setup(AksaraData aksara, string targetRarangken)
        {
            _aksaraBase      = aksara;
            _targetRarangken = targetRarangken;
            _selectedRarangken = null;
            _wrongStreak     = 0;

            if (previewHuruf != null && aksara.spriteHuruf != null)
                previewHuruf.sprite = aksara.spriteHuruf;

            if (labelFonetik != null)
                labelFonetik.text = aksara.fonetik;

            for (int i = 0; i < tombolRarangken.Length && i < SemuaRarangken.Length; i++)
            {
                if (labelTombol != null && i < labelTombol.Length)
                    labelTombol[i].text = BunyiVokal[i];
                tombolRarangken[i].interactable = true;
            }

            btnKonfirmasi.interactable = false;
            ResetTombolWarna();
        }

        private void SelectRarangken(int idx)
        {
            if (idx >= SemuaRarangken.Length) return;
            _selectedRarangken = SemuaRarangken[idx];

            ResetTombolWarna();
            if (imageTombol != null && idx < imageTombol.Length)
                imageTombol[idx].color = new Color(0.8f, 0.9f, 1f);

            // Preview: tampilkan huruf + rarangken
            if (previewHuruf != null && _aksaraBase != null)
            {
                var rd = _aksaraBase.GetRarangken(_selectedRarangken);
                if (rd.spriteHurufDenganRarangken != null)
                    previewHuruf.sprite = rd.spriteHurufDenganRarangken;
            }

            // Play audio preview
            if (_aksaraBase != null)
            {
                var rd = _aksaraBase.GetRarangken(_selectedRarangken);
                // Audio per rarangken bisa diimplementasi nanti
            }

            btnKonfirmasi.interactable = true;
        }

        private void Konfirmasi()
        {
            if (_selectedRarangken == null) return;

            bool benar = _selectedRarangken == _targetRarangken;
            if (benar)
            {
                _wrongStreak = 0;
                if (AudioManager.Instance != null && sfxBenar != null)
                    AudioManager.Instance.PlaySFX(sfxBenar);
                StartCoroutine(FlashAll(Color.green));
                OnAnswerCorrect?.Invoke();
            }
            else
            {
                _wrongStreak++;
                if (AudioManager.Instance != null && sfxSalah != null)
                    AudioManager.Instance.PlaySFX(sfxSalah);
                StartCoroutine(FlashAll(Color.red));
                OnAnswerWrong?.Invoke();

                if (_wrongStreak >= 2)
                    ShowHint();
            }
        }

        private void ShowHint()
        {
            for (int i = 0; i < SemuaRarangken.Length; i++)
            {
                if (SemuaRarangken[i] == _targetRarangken && imageTombol != null && i < imageTombol.Length)
                {
                    imageTombol[i].color = Color.yellow;
                    break;
                }
            }
        }

        private IEnumerator FlashAll(Color c)
        {
            if (imageTombol == null) yield break;
            foreach (var img in imageTombol)
                if (img != null) img.color = c;
            yield return new WaitForSeconds(0.3f);
            ResetTombolWarna();
        }

        private void ResetTombolWarna()
        {
            if (imageTombol == null) return;
            foreach (var img in imageTombol)
                if (img != null) img.color = Color.white;
        }
    }
}
