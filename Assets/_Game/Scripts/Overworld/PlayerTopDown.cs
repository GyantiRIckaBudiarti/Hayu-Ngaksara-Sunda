using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace HayuNgaksara
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerTopDown : MonoBehaviour
    {
        public static PlayerTopDown Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Name")]
        [SerializeField] private string playerName = "Raka";

        [Header("Gender Animator (opsional — kosongkan jika belum ada aset)")]
        [SerializeField] private RuntimeAnimatorController animatorPria;
        [SerializeField] private RuntimeAnimatorController animatorWanita;

        public string PlayerName => playerName;
        public bool CanMove { get; set; } = true;

        private Rigidbody2D _rb;
        private Animator    _anim;
        private Vector2     _input;
        private Vector2     _lastDir = Vector2.down;

        // Animator parameter hashes
        private static readonly int HashMoveX  = Animator.StringToHash("MoveX");
        private static readonly int HashMoveY  = Animator.StringToHash("MoveY");
        private static readonly int HashMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashLastX  = Animator.StringToHash("LastX");
        private static readonly int HashLastY  = Animator.StringToHash("LastY");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _rb   = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();

            string saved = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(saved)) playerName = saved;

            // Switch animator sesuai gender yang dipilih
            string gender = PlayerPrefs.GetString("PlayerGender", "pria");
            var ctrl = (gender == "wanita" && animatorWanita != null) ? animatorWanita : animatorPria;
            if (ctrl != null) _anim.runtimeAnimatorController = ctrl;

            CreateNameLabel();
        }

        // Nama karakter di atas kepala (kebal-skala, gaya sama seperti label NPC).
        private void CreateNameLabel()
        {
            if (string.IsNullOrEmpty(playerName)) return;
            float parentScale = Mathf.Abs(transform.lossyScale.y);
            if (parentScale < 0.0001f) parentScale = 1f;
            float inv       = 1f / parentScale;
            float worldFont = 3.2f;          // sedikit lebih kecil dari NPC (MC lebih pendek)
            float offsetY   = 0.72f;         // di atas kepala (world), dibagi skala di bawah
            float shOff     = 0.03f * inv;

            // Shadow (outline sederhana)
            var goShadow = new GameObject("MC_NameLabel_Shadow");
            goShadow.transform.SetParent(transform);
            goShadow.transform.localPosition = new Vector3(shOff, offsetY / parentScale - shOff, 0f);
            goShadow.transform.localRotation = Quaternion.identity;
            goShadow.transform.localScale    = Vector3.one * inv;
            var tmpS = goShadow.AddComponent<TextMeshPro>();
            tmpS.text = playerName; tmpS.fontSize = worldFont; tmpS.fontStyle = FontStyles.Bold;
            tmpS.alignment = TextAlignmentOptions.Center; tmpS.color = new Color(0f, 0f, 0f, 0.85f);
            var mrS = goShadow.GetComponent<MeshRenderer>();
            if (mrS != null) { mrS.sortingLayerName = "Characters"; mrS.sortingOrder = 19; }

            // Teks utama (warna emas agar beda dari NPC putih)
            var go = new GameObject("MC_NameLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, offsetY / parentScale, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one * inv;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = playerName; tmp.fontSize = worldFont; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color(1f, 0.85f, 0.25f, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) { mr.sortingLayerName = "Characters"; mr.sortingOrder = 20; }
        }

        private void Update()
        {
            if (!CanMove) { _input = Vector2.zero; return; }

            var kb = Keyboard.current;
            if (kb == null) return;

            float x = 0f, y = 0f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x =  1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x = -1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y =  1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y = -1f;

            _input = new Vector2(x, y).normalized;

            bool moving = _input.sqrMagnitude > 0.01f;
            if (moving) _lastDir = _input;

            if (_anim.runtimeAnimatorController != null)
            {
                // Dominant-axis: saat diagonal, pilih sumbu terkuat → animasi tidak kacau
                float animX = _input.x, animY = _input.y;
                if (moving && Mathf.Abs(_input.x) > 0.01f && Mathf.Abs(_input.y) > 0.01f)
                {
                    if (Mathf.Abs(_input.x) >= Mathf.Abs(_input.y)) animY = 0f; // horizontal dominan
                    else                                               animX = 0f; // vertical dominan
                }
                _anim.SetFloat(HashMoveX, animX);
                _anim.SetFloat(HashMoveY, animY);
                _anim.SetBool(HashMoving,  moving);
                _anim.SetFloat(HashLastX,  _lastDir.x);
                _anim.SetFloat(HashLastY,  _lastDir.y);
            }

            // Interact
            if ((kb.eKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame ||
                 kb.spaceKey.wasPressedThisFrame) && CanMove)
                InteractSystem.Instance?.TryInteract();
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = _input * moveSpeed;
        }

        public void SetName(string n) => playerName = n;
        public void SetCanMove(bool v) => CanMove = v;

        /// <summary>Hadapkan MC ke arah sebuah posisi (dipakai saat mulai ngobrol dengan NPC).</summary>
        public void FaceTowards(Vector3 worldPos)
        {
            Vector2 d = (Vector2)(worldPos - transform.position);
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 dir = Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                ? new Vector2(Mathf.Sign(d.x), 0f)
                : new Vector2(0f, Mathf.Sign(d.y));
            _lastDir = dir;
            if (_anim != null && _anim.runtimeAnimatorController != null)
            {
                _anim.SetFloat(HashMoveX, 0f);
                _anim.SetFloat(HashMoveY, 0f);
                _anim.SetBool (HashMoving, false);
                _anim.SetFloat(HashLastX, dir.x);
                _anim.SetFloat(HashLastY, dir.y);
            }
        }
    }
}
