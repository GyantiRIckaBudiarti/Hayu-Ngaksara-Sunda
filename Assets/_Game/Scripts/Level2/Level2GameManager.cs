using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class Level2GameManager : MonoBehaviour
    {
        private enum Level2State { Intro, Trace, Rarangken, ChaseCutscene, ChaseResult }

        [Header("References")]
        [SerializeField] private NabilaController nabila;
        [SerializeField] private UcupController  ucup;
        [SerializeField] private JajangController jajang;
        [SerializeField] private AndreController  andre;
        [SerializeField] private TraceUI          traceUI;
        [SerializeField] private RarangkenSelector rarangkenSelector;

        [Header("Soal")]
        [SerializeField] private List<AksaraData>  aksaraUntukTrace;
        [SerializeField] private List<AksaraData>  aksaraUntukRarangken;
        [SerializeField] private List<string>      targetRarangkenList;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI hudProgress;
        [SerializeField] private GameObject      panelLevelComplete;
        [SerializeField] private TextMeshProUGUI textBintang;
        [SerializeField] private Button          tombolLanjut;

        private Level2State _state;
        private int         _traceIndex;
        private int         _rarangkenIndex;
        private int         _totalKesalahan;

        private void Start()
        {
            rarangkenSelector.OnAnswerCorrect += HandleRarangkenCorrect;
            rarangkenSelector.OnAnswerWrong   += HandleRarangkenWrong;
            ChaseManager.OnChaseEnd           += HandleChaseEnd;

            if (tombolLanjut != null)
                tombolLanjut.onClick.AddListener(LoadNextLevel);

            StartCoroutine(StartIntro());
        }

        private void OnDestroy()
        {
            rarangkenSelector.OnAnswerCorrect -= HandleRarangkenCorrect;
            rarangkenSelector.OnAnswerWrong   -= HandleRarangkenWrong;
            ChaseManager.OnChaseEnd           -= HandleChaseEnd;
        }

        private IEnumerator StartIntro()
        {
            _state = Level2State.Intro;
            nabila?.ShowDialog("Jajang, kami mau belajar nulis aksara sunda tapi susah...", 3f);
            yield return new WaitForSeconds(3f);
            ucup?.ShowDialog("Ieu kumaha cara nulisna?", 2f);
            yield return new WaitForSeconds(2.5f);
            StartTrace();
        }

        private void StartTrace()
        {
            _state      = Level2State.Trace;
            _traceIndex = 0;
            ShowNextTrace();
        }

        private void ShowNextTrace()
        {
            if (_traceIndex >= aksaraUntukTrace.Count)
            {
                StartCoroutine(TransitionToRarangken());
                return;
            }

            UpdateHUD($"Huruf: {_traceIndex + 1}/{aksaraUntukTrace.Count}");
            traceUI.gameObject.SetActive(true);
            traceUI.Setup(aksaraUntukTrace[_traceIndex]);
            traceUI.OnTraceComplete += HandleTraceComplete;
        }

        private void HandleTraceComplete()
        {
            traceUI.OnTraceComplete -= HandleTraceComplete;
            AudioManager.Instance?.PlayPelafalan(aksaraUntukTrace[_traceIndex].audioPelafalan);
            _traceIndex++;
            ShowNextTrace();
        }

        private IEnumerator TransitionToRarangken()
        {
            traceUI.gameObject.SetActive(false);
            nabila?.ShowDialog("Eh... kayaknya ada yang kurang deh!", 2.5f);
            yield return new WaitForSeconds(2.5f);
            ucup?.ShowDialog("Oh iya! Vokalnya belum!", 2f);
            yield return new WaitForSeconds(2.5f);

            _state          = Level2State.Rarangken;
            _rarangkenIndex = 0;
            ShowNextRarangken();
        }

        private void ShowNextRarangken()
        {
            if (_rarangkenIndex >= aksaraUntukRarangken.Count)
            {
                StartCoroutine(ChaseCutscene());
                return;
            }

            UpdateHUD($"Vokal: {_rarangkenIndex + 1}/{aksaraUntukRarangken.Count}");
            rarangkenSelector.gameObject.SetActive(true);
            rarangkenSelector.Setup(aksaraUntukRarangken[_rarangkenIndex],
                                    targetRarangkenList[_rarangkenIndex]);
        }

        private void HandleRarangkenCorrect()
        {
            nabila?.ReactToHelp();
            rarangkenSelector.gameObject.SetActive(false);
            _rarangkenIndex++;
            ShowNextRarangken();
        }

        private void HandleRarangkenWrong()
        {
            _totalKesalahan++;
            ucup?.ReactExaggerated();
        }

        private IEnumerator ChaseCutscene()
        {
            _state = Level2State.ChaseCutscene;
            rarangkenSelector.gameObject.SetActive(false);

            andre?.GrabPaper();
            yield return new WaitForSeconds(0.5f);
            jajang?.ShowDialog("Heeh! Maneh Andre!", 2f);
            yield return new WaitForSeconds(1.5f);
            andre?.RunAway();
            yield return new WaitForSeconds(1f);

            SceneTransitionManager.Instance?.LoadScene("10b_Level2_Chase");
        }

        private void HandleChaseEnd(bool won)
        {
            _state = Level2State.ChaseResult;
            StartCoroutine(ShowComplete());
        }

        private IEnumerator ShowComplete()
        {
            yield return new WaitForSeconds(1f);

            int bintang = GameManager.Instance?.HitungBintang(_totalKesalahan) ?? 1;
            GameManager.Instance?.SaveProgress("level2", bintang);

            if (panelLevelComplete != null) panelLevelComplete.SetActive(true);
            if (textBintang != null)
                textBintang.text = $"Bintang: {new string('★', bintang)}{new string('☆', 3 - bintang)}";
        }

        private void UpdateHUD(string text)
        {
            if (hudProgress != null) hudProgress.text = text;
        }

        private void LoadNextLevel()
        {
            SceneTransitionManager.Instance?.LoadScene("11_Level3_Game");
        }
    }
}
