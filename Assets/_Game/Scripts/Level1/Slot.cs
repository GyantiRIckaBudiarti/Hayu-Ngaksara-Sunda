using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class Slot : MonoBehaviour
    {
        public bool      IsOccupied    { get; private set; }
        public AksaraData CurrentAksara { get; private set; }

        [SerializeField] private SpriteRenderer displaySprite;
        [SerializeField] private SpriteRenderer borderSprite;
        [SerializeField] private Color          colorEmpty   = Color.white;
        [SerializeField] private Color          colorCorrect = new Color(0.29f, 0.49f, 0.35f);
        [SerializeField] private Color          colorWrong   = new Color(0.55f, 0.23f, 0.23f);
        [SerializeField] private AudioClip      sfxCorrect;
        [SerializeField] private AudioClip      sfxWrong;

        public void PlaceAksara(AksaraData aksara)
        {
            CurrentAksara = aksara;
            IsOccupied    = true;
            if (displaySprite != null && aksara.spriteHuruf != null)
                displaySprite.sprite = aksara.spriteHuruf;
        }

        public void ClearSlot()
        {
            CurrentAksara = null;
            IsOccupied    = false;
            if (displaySprite != null) displaySprite.sprite = null;
            SetBorderColor(colorEmpty);
        }

        public void FlashCorrect()
        {
            if (AudioManager.Instance != null && sfxCorrect != null)
                AudioManager.Instance.PlaySFX(sfxCorrect);
            StartCoroutine(FlashColor(colorCorrect));
        }

        public void FlashWrong()
        {
            if (AudioManager.Instance != null && sfxWrong != null)
                AudioManager.Instance.PlaySFX(sfxWrong);
            StartCoroutine(FlashRedThreeTimes());
        }

        private IEnumerator FlashColor(Color target)
        {
            SetBorderColor(target);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator FlashRedThreeTimes()
        {
            for (int i = 0; i < 3; i++)
            {
                SetBorderColor(colorWrong);
                yield return new WaitForSeconds(0.1f);
                SetBorderColor(colorEmpty);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SetBorderColor(Color c)
        {
            if (borderSprite != null) borderSprite.color = c;
        }
    }
}
