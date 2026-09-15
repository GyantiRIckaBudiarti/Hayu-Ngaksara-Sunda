using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class CutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public class DialogStep
        {
            public string speaker;
            [TextArea(2, 4)] public string text;
            public float duration = 3f;
        }

        [Header("Dialog")]
        [SerializeField] private List<DialogStep> dialogSteps;
        [SerializeField] private string nextScene = "08_Level1_Game";

        [Header("UI")]
        [SerializeField] private GameObject panelDialog;
        [SerializeField] private TextMeshProUGUI txtSpeaker;
        [SerializeField] private TextMeshProUGUI txtDialog;
        [SerializeField] private Button btnSkip;
        [SerializeField] private Button btnNext;

        private int _stepIndex;

        private void Start()
        {
            if (btnSkip != null) btnSkip.onClick.AddListener(SkipAll);
            if (btnNext != null) btnNext.onClick.AddListener(NextStep);
            StartCoroutine(PlayCutscene());
        }

        private IEnumerator PlayCutscene()
        {
            for (_stepIndex = 0; _stepIndex < dialogSteps.Count; _stepIndex++)
            {
                ShowStep(_stepIndex);
                yield return new WaitForSecondsRealtime(dialogSteps[_stepIndex].duration);
            }
            LoadNext();
        }

        private void ShowStep(int idx)
        {
            if (panelDialog != null) panelDialog.SetActive(true);
            var step = dialogSteps[idx];
            if (txtSpeaker != null) txtSpeaker.text = step.speaker;
            if (txtDialog   != null) txtDialog.text  = step.text;
        }

        private void NextStep()
        {
            StopAllCoroutines();
            _stepIndex++;
            if (_stepIndex >= dialogSteps.Count)
                LoadNext();
            else
                StartCoroutine(PlayCutscene_FromStep(_stepIndex));
        }

        private IEnumerator PlayCutscene_FromStep(int start)
        {
            for (int i = start; i < dialogSteps.Count; i++)
            {
                _stepIndex = i;
                ShowStep(i);
                yield return new WaitForSecondsRealtime(dialogSteps[i].duration);
            }
            LoadNext();
        }

        private void SkipAll()
        {
            StopAllCoroutines();
            LoadNext();
        }

        private void LoadNext()
        {
            SceneTransitionManager.Instance?.LoadScene(nextScene);
        }
    }
}
