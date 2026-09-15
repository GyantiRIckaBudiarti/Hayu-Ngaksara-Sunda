using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class Level1GameManager : MonoBehaviour
    {
        private enum Level1State { Cutscene, Collect, Arrange, Complete }

        [Header("References")]
        [SerializeField] private SintaController  sinta;
        [SerializeField] private JajangController jajang;
        [SerializeField] private WindPaperSpawner spawner;
        [SerializeField] private SlotBoard        slotBoard;

        [Header("Soal")]
        [SerializeField] private List<AksaraData> aksaraLevel1;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI  hudKertas;
        [SerializeField] private GameObject       panelLevelComplete;
        [SerializeField] private TextMeshProUGUI  textBintang;
        [SerializeField] private Button           tombolLanjut;

        private Level1State _state;
        private int         _totalKesalahan;
        private int         _kertasTerkumpul;
        private float       _waktuMulai;

        private void Start()
        {
            WindPaperSpawner.OnPaperPickedUp    += HandlePaperPickedUp;
            WindPaperSpawner.OnAllPapersPickedUp += HandleAllCollected;

            slotBoard.OnSlotCorrect   += HandleSlotCorrect;
            slotBoard.OnSlotWrong     += HandleSlotWrong;
            slotBoard.OnBoardComplete += HandleBoardComplete;

            if (tombolLanjut != null)
                tombolLanjut.onClick.AddListener(LoadNextLevel);

            StartCoroutine(StartCutscene());
        }

        private void OnDestroy()
        {
            WindPaperSpawner.OnPaperPickedUp    -= HandlePaperPickedUp;
            WindPaperSpawner.OnAllPapersPickedUp -= HandleAllCollected;
        }

        private IEnumerator StartCutscene()
        {
            _state = Level1State.Cutscene;
            GameManager.Instance?.SetState(GameState.Cutscene);

            sinta?.GiveInstruction("Jajang, tulung atuh kertasna katebak angin!");
            yield return new WaitForSecondsRealtime(3f);

            spawner.Setup(aksaraLevel1);
            spawner.SpawnAll();
            EnterCollect();
        }

        private void EnterCollect()
        {
            _state          = Level1State.Collect;
            _kertasTerkumpul = 0;
            _waktuMulai     = Time.time;
            GameManager.Instance?.SetState(GameState.Playing);
            UpdateHUD();
        }

        private void HandlePaperPickedUp(AksaraData aksara)
        {
            if (_state != Level1State.Collect) return;
            _kertasTerkumpul++;
            UpdateHUD();
        }

        private void HandleAllCollected()
        {
            if (_state != Level1State.Collect) return;
            StartCoroutine(TransitionToArrange());
        }

        private IEnumerator TransitionToArrange()
        {
            sinta?.GiveInstruction("Ayeuna susun di papan nya!");
            yield return new WaitForSecondsRealtime(3f);

            _state = Level1State.Arrange;
            slotBoard.Setup(aksaraLevel1);
            slotBoard.gameObject.SetActive(true);
        }

        private void HandleSlotCorrect()
        {
            sinta?.ReactToCorrect();
        }

        private void HandleSlotWrong()
        {
            _totalKesalahan++;
            sinta?.ReactToWrong();
            jajang?.PlayShakeHead();
        }

        private void HandleBoardComplete()
        {
            StartCoroutine(ShowComplete());
        }

        private IEnumerator ShowComplete()
        {
            _state = Level1State.Complete;
            GameManager.Instance?.SetState(GameState.LevelComplete);

            int bintang = GameManager.Instance?.HitungBintang(_totalKesalahan) ?? 1;
            GameManager.Instance?.SaveProgress("level1", bintang);

            yield return new WaitForSecondsRealtime(1f);

            if (panelLevelComplete != null) panelLevelComplete.SetActive(true);
            if (textBintang != null) textBintang.text = $"Bintang: {new string('★', bintang)}{new string('☆', 3 - bintang)}";
        }

        private void UpdateHUD()
        {
            if (hudKertas != null)
                hudKertas.text = $"Kertas: {_kertasTerkumpul}/{aksaraLevel1.Count}";
        }

        private void LoadNextLevel()
        {
            SceneTransitionManager.Instance?.LoadScene("10_Level2_Game");
        }
    }
}
