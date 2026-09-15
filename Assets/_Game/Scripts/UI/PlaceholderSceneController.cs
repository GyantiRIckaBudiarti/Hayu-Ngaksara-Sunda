using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HayuNgaksara
{
    public class PlaceholderSceneController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtJudul;
        [SerializeField] private Button btnKembali;
        [SerializeField] private string judulScene = "Fitur ini";
        [SerializeField] private string kembaliKe  = "01_MainMenu";

        private void Start()
        {
            if (txtJudul != null) txtJudul.text = judulScene + "\nsedang dalam pengembangan";
            if (btnKembali != null)
                btnKembali.onClick.AddListener(() => SceneTransitionManager.Instance?.LoadScene(kembaliKe));
        }
    }
}
