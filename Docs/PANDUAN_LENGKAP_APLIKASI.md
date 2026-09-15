# PANDUAN LENGKAP APLIKASI — "Hayu Ngaksara Sunda"

> Dokumen belajar end-to-end. Tujuannya: siapa pun (termasuk kamu saat menjelaskan ke dosen/tim)
> bisa memahami **cara kerja seluruh aplikasi**, dari tombol ditekan sampai piksel bergerak di layar,
> lengkap dengan **penjelasan baris-demi-baris** untuk kode inti (terutama pergerakan karakter).
>
> Semua isi diambil dari kode **nyata** di `Assets/_Game/Scripts/`. Kalau ada file yang belum
> dibedah baris-per-baris, ditandai jelas dan dijelaskan per-metode agar tetap utuh.

Daftar isi:
1. [Apa ini & teknologi yang dipakai](#1-apa-ini--teknologi-yang-dipakai)
2. [Peta arsitektur (diagram teks)](#2-peta-arsitektur-diagram-teks)
3. [Alur hidup aplikasi end-to-end](#3-alur-hidup-aplikasi-end-to-end)
4. [Struktur folder & daftar semua file](#4-struktur-folder--daftar-semua-file)
5. [DEEP DIVE — Cara menggerakkan karakter (PlayerTopDown, baris-per-baris)](#5-deep-dive--cara-menggerakkan-karakter)
6. [DEEP DIVE — Kamera mengikuti pemain](#6-deep-dive--kamera-mengikuti-pemain)
7. [DEEP DIVE — Sistem interaksi & NPC](#7-deep-dive--sistem-interaksi--npc)
8. [DEEP DIVE — Dialog & pilihan dinamis](#8-deep-dive--dialog--pilihan-dinamis)
9. [DEEP DIVE — Manajer inti (GameManager, ChapterManager, dst.)](#9-deep-dive--manajer-inti)
10. [DEEP DIVE — Perpindahan scene (transisi)](#10-deep-dive--perpindahan-scene)
11. [DEEP DIVE — Mini-game (belajar → kuis → hasil)](#11-deep-dive--mini-game)
12. [Sistem progres & tabel PlayerPrefs](#12-sistem-progres--tabel-playerprefs)
13. [Pipeline animasi karakter (sprite walk)](#13-pipeline-animasi-karakter)
14. [Cara menambah konten baru](#14-cara-menambah-konten-baru)
15. [Glosarium](#15-glosarium)

---

## 1. Apa ini & teknologi yang dipakai

**Hayu Ngaksara Sunda** adalah game edukasi 2D *top-down* untuk belajar **aksara Sunda**. Pemain
berjalan di sekolah, mendatangi NPC (guru/pelatih), lalu masuk mini-game (membaca, menulis,
pelafalan, refleksi). Menyelesaikan semua aktivitas sebuah *chapter* membuka *chapter* berikutnya.

| Aspek | Nilai |
|-------|-------|
| Engine | Unity 6 (6000.3.x) |
| Bahasa | C# |
| Namespace | `HayuNgaksara` (satu namespace untuk semua) |
| Render 2D | URP 2D Renderer |
| Input | **New Input System** (`UnityEngine.InputSystem`) |
| Kamera | Cinemachine (follow) + `CameraFollow` fallback |
| Teks | TextMeshPro (UI & world text) |
| Penyimpanan | `PlayerPrefs` (key-value lokal) |
| Suara ucapan | Vosk (offline speech recognition) — mini-game pelafalan |
| Pengenal tulisan | `$Q` QDollarRecognizer + `DirectionRecognizer` |

**Dua "sistem" cerita:**
- **Sistem A (AKTIF)** — sekolah + chapter (`Tutorial → Swara → Ngalagena1 → Ngalagena2 → Rarangken → UjianFinal`). Ini yang dipakai build.
- **Sistem B (LEGACY)** — scene naratif Level1/2/3 lama (folder `Level1/2/3`). Tidak dipakai di build, tapi skripnya masih ada.

---

## 2. Peta arsitektur (diagram teks)

Objek "manajer" bersifat **singleton** (`Instance`) dan sebagian **DontDestroyOnLoad (DDOL)** —
artinya hidup terus lintas scene.

```
                         ┌─────────────────────────────────────────────┐
                         │          OBJEK GLOBAL (DDOL, singleton)      │
                         │                                             │
   Input keyboard ─────► │  GameManager        state/skor/database     │
                         │  ChapterManager     progres chapter + event │
                         │  SceneTransitionMgr  fade antar-scene        │
                         │  AudioManager       musik & SFX             │
                         └───────────────┬─────────────────────────────┘
                                         │ (dibaca oleh scene aktif)
     ┌───────────────────────────────────┼───────────────────────────────────┐
     │ SCENE 01_MainMenu                  │ SCENE 00_Sekolah (overworld)       │
     │  MainMenuController                │  PlayerTopDown  ← gerak karakter   │
     │  SettingsPanel                     │  InteractSystem ← deteksi "E"      │
     │                                    │  NPCController(×6) : IInteractable  │
     │                                    │  DialogSystem   ← percakapan       │
     │                                    │  ObjectiveHUD / ObjectiveArrow     │
     │                                    │  CameraFollow / Cinemachine        │
     └────────────────────────────────────┴──────────────┬────────────────────┘
                                                          │ pilih "mulai belajar"
                                                          ▼
                                       SCENE MG_* (mini-game per aktivitas)
                                        MiniGameManager (Intro→Learn→Trace→Quiz→Result)
                                        └─ simpan hasil → ChapterManager.CompleteSubActivity()
```

**Pola komunikasi utama:**
- **Singleton**: `X.Instance.Method()` — akses langsung antar-manajer.
- **Event C#**: `ChapterManager` menyiarkan `OnChapterCompleted`, `OnSubActivityCompleted`, dll. UI (mis. `ObjectiveHUD`) berlangganan dan memperbarui diri.
- **PlayerPrefs**: kebenaran tunggal (*source of truth*) untuk progres yang bertahan antar-sesi.

---

## 3. Alur hidup aplikasi end-to-end

### 3a. Diagram urutan: dari buka game sampai bermain

```
Pemain            MainMenu           SceneTransition        00_Sekolah            ChapterManager
  │                  │                     │                    │                       │
  │ klik "Mulai"     │                     │                    │                       │
  ├─────────────────►│ LoadScene(00a_Nama) │                    │                       │
  │                  ├────────────────────►│ fade out           │                       │
  │ isi nama+gender  │                     │ load scene         │                       │
  │                  │                     │ fade in            │                       │
  │                  │  (NameInput simpan PlayerPrefs, ResetAll) │                       │
  │                  │                     ├───────────────────►│ Awake: spawn player   │
  │                  │                     │                    │ PlayerTopDown.Instance │
  │ tekan WASD       │                     │                    │ Update→FixedUpdate     │
  │                  │                     │                    │ rb.velocity = input*spd│
  │ dekati Jajang, E │                     │                    │ InteractSystem.TryInteract()
  │                  │                     │                    ├──────────────────────►│ cek objektif
  │                  │                     │                    │ DialogSystem tampil    │
```

### 3b. Diagram alur: satu siklus belajar (satu chapter)

```
                 ┌── ObjectiveHUD menampilkan target NPC berikutnya ──┐
                 ▼                                                     │
  Temui SINTA  ──► Mini-game "Membaca"  ──► CompleteSubActivity(baca) ─┤
  Temui UCUP   ──► Mini-game "Menulis"  ──► CompleteSubActivity(tulis)─┤
  Temui NABILA ──► Mini-game "Pelafalan"──► CompleteSubActivity(pelafalan)
  Temui GURU   ──► "Refleksi" (ujian)   ──► CompleteSubActivity(refleksi)
                                                    │
                                                    ▼
                                   ChapterManager.CompleteChapter()
                                     ├─ set chapter.completed = true
                                     ├─ unlock chapter berikutnya
                                     └─ set PlayerPrefs "pending_levelup"
                                                    │  (kembali ke 00_Sekolah)
                                                    ▼
                                   LevelUpAnnouncer membaca "pending_levelup"
                                     └─ Dialog Jajang: "Selamat naik level!"
```

### 3c. Loop frame Unity (kenapa gerak halus)

Unity memanggil metode tiap frame dengan urutan tetap. Yang relevan untuk karakter:

```
    tiap frame:
      Update()       → baca input keyboard, set arah + parameter animasi
      FixedUpdate()  → (fisika, langkah tetap ~50Hz) set kecepatan Rigidbody2D
      LateUpdate()   → kamera menyesuaikan posisi (setelah pemain bergerak)
```

Memisahkan **baca input** (`Update`) dari **terapkan fisika** (`FixedUpdate`) = gerak stabil dan
tidak tergantung frame-rate.

---

## 4. Struktur folder & daftar semua file

```
Assets/_Game/Scripts/
├── Core/          → manajer global (game, audio, scene, TTS, suara)
├── Overworld/     → sekolah: pemain, NPC, dialog, interaksi, kamera, objektif
├── MiniGame/      → mesin mini-game + panel (trace, kuis suara, pengenal)
├── UI/            → menu, HUD, panel hasil, pengaturan, pause
├── Data/          → ScriptableObject aksara (data huruf)
├── Characters/    → controller karakter spesifik (Sistem B/legacy sebagian)
├── Level1/2/3/    → scene naratif lama (LEGACY, tidak di build)
└── Editor/        → alat editor (generator, builder) — hanya jalan di Editor
```

### Daftar file & perannya

**Core/**
| File | Peran |
|------|-------|
| `GameManager.cs` | Singleton DDOL: state game, skor, nyawa, akses `AksaraDatabase`, simpan/baca progres umum. |
| `ChapterManager.cs` | **Otak progres**: unlock/complete chapter + sub-aktivitas, event, PlayerPrefs. |
| `SceneTransitionManager.cs` | Pindah scene dengan efek fade/slide (overlay Canvas sortingOrder 9999). |
| `AudioManager.cs` | Musik (loop+fade) & SFX; volume tersimpan di PlayerPrefs. |
| `TTSService.cs` | Text-to-speech (bila dipakai). |
| `VoiceRecognitionService.cs` | Wrapper Vosk untuk pelafalan (offline). |
| `JuiceManager.cs` | Efek "juice" (shake, dsb). |
| `TestHelper.cs` | Utilitas test. |

**Overworld/**
| File | Peran |
|------|-------|
| `PlayerTopDown.cs` | **Gerak karakter** + input + set parameter animasi + trigger interaksi. |
| `InteractSystem.cs` | Deteksi objek `IInteractable` terdekat dalam radius; tekan E → `Interact()`. |
| `NPCController.cs` | NPC sebagai `IInteractable`: label nama, dialog, memulai mini-game per skill. |
| `DialogSystem.cs` | Kotak dialog + efek ketik + daftar pilihan dinamis (gaya Skyrim). |
| `CameraFollow.cs` | Kamera lerp mengikuti pemain (fallback non-Cinemachine). |
| `ObjectiveHUD.cs` | HUD kanan-atas: checklist objektif + skor per aktivitas. |
| `ObjectiveArrow.cs` | Panah penunjuk arah ke NPC target objektif. |
| `QuestGuide.cs` | Logika penuntun urutan quest. |
| `TutorialManager.cs` | Alur tutorial Jajang di awal. |
| `LevelUpAnnouncer.cs` | Saat kembali ke sekolah, jika `pending_levelup` → dialog naik level. |
| `LearningPanelManager.cs` | Jembatan membuka scene mini-game untuk sebuah chapter/mode. |
| `NameInputController.cs` | Scene input nama + gender; memanggil `ResetAll()` untuk game baru. |
| `InteractHintUI.cs` | Tampilkan hint "Tekan E" saat dekat interaktif. |
| `FloatingText.cs`, `RoomLabel.cs`, `ReturnFeedback.cs`, `OpeningCutscene.cs` | Elemen pendukung overworld. |

**MiniGame/**
| File | Peran |
|------|-------|
| `MiniGameManager.cs` | Mesin utama: Intro→Learn→Trace→Quiz→Result, routing per `MiniGameMode`. |
| `MiniGameData.cs` | Data satu mini-game (mode, daftar kartu aksara, dsb). |
| `AksaraCardData.cs` | Struktur satu kartu aksara. |
| `TracePanel.cs` | Panel latihan menulis (menggores aksara). |
| `QDollarRecognizer.cs`, `DirectionRecognizer.cs` | Pengenal bentuk goresan tulisan. |
| `VoiceQuizPanel.cs` | Panel kuis pelafalan (rekam → cocokkan). |
| `AllTemplatesView.cs` | Peninjau template goresan. |

**UI/**
| File | Peran |
|------|-------|
| `MainMenuController.cs` | Menu utama: Mulai, Lanjutkan, Pengaturan, Keluar + animasi buka. |
| `SettingsPanel.cs` | Panel pengaturan self-building (slider Musik/Efek Suara). |
| `PauseMenuController.cs` | Menu jeda (Esc): resume/restart/menu/quit + progres menulis. |
| `ObjectiveHUD.cs` | (lihat Overworld) — RPG-style objective. |
| `LevelSummaryController.cs` | Ringkasan nilai chapter (persentase). |
| `KuisResultPanel.cs`, `KuisManager.cs` | Hasil & pengelola kuis. |
| `WritingProgressPanel.cs` | Progres latihan menulis per aksara. |
| `PlaceholderSceneController.cs` | Scene placeholder. |

**Data/** — `KategoriAksara.cs`, `AksaraData.cs`, `AksaraDatabase.cs` (ScriptableObject huruf & database).
**Characters/** — `CharacterBase.cs` + controller Jajang/Sinta/Nabila/Ucup/Andre (sebagian legacy).
**Level1/2/3/** — scene naratif lama (LEGACY).
**Editor/** — generator & builder (hanya di Editor, tidak masuk build runtime).

---

## 5. DEEP DIVE — Cara menggerakkan karakter

File: `Assets/_Game/Scripts/Overworld/PlayerTopDown.cs`. **Inilah inti "cara menggerakkan karakter".**
Kita bedah **baris demi baris**.

### 5a. Header kelas & komponen wajib

```csharp
using UnityEngine;
using UnityEngine.InputSystem;   // New Input System (Keyboard.current)

namespace HayuNgaksara
{
    [RequireComponent(typeof(Rigidbody2D))]  // (a)
    [RequireComponent(typeof(Animator))]     // (b)
    public class PlayerTopDown : MonoBehaviour
    {
        public static PlayerTopDown Instance { get; private set; }  // (c)
```
- **(a)/(b)** `RequireComponent` memastikan GameObject **wajib** punya `Rigidbody2D` (untuk gerak fisika) dan `Animator` (untuk animasi). Kalau belum ada, Unity menambahkannya otomatis.
- **(c)** `Instance` = pola *singleton*. Skrip lain cukup panggil `PlayerTopDown.Instance` untuk menemukan pemain tanpa mencari-cari di scene.

### 5b. Field yang bisa diatur di Inspector

```csharp
        [SerializeField] private float moveSpeed = 5f;        // (d) kecepatan (unit/detik)
        [SerializeField] private string playerName = "Raka";  // (e) nama default

        [SerializeField] private RuntimeAnimatorController animatorPria;    // (f)
        [SerializeField] private RuntimeAnimatorController animatorWanita;  // (f)

        public string PlayerName => playerName;               // (g) baca-saja
        public bool   CanMove { get; set; } = true;           // (h) gerbang gerak
```
- **(d)** `moveSpeed` — makin besar makin cepat. `[SerializeField]` membuat field `private` tetap bisa diatur di Inspector.
- **(e)** Nama default; nanti ditimpa oleh nama tersimpan.
- **(f)** Dua controller animasi (pria/wanita) sesuai pilihan gender. Boleh kosong.
- **(g)** Properti baca-saja agar skrip lain bisa membaca nama.
- **(h)** `CanMove` — saklar utama. Saat dialog/pause aktif, di-set `false` supaya pemain diam.

### 5c. State internal

```csharp
        private Rigidbody2D _rb;                    // (i) referensi fisika
        private Animator    _anim;                  // (j) referensi animator
        private Vector2     _input;                 // (k) arah input frame ini
        private Vector2     _lastDir = Vector2.down;// (l) arah hadap terakhir

        private static readonly int HashMoveX  = Animator.StringToHash("MoveX");   // (m)
        private static readonly int HashMoveY  = Animator.StringToHash("MoveY");
        private static readonly int HashMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashLastX  = Animator.StringToHash("LastX");
        private static readonly int HashLastY  = Animator.StringToHash("LastY");
```
- **(i)/(j)** Cache komponen agar tidak `GetComponent` tiap frame (mahal).
- **(k)** `_input` menyimpan arah gerak hasil pembacaan tombol (contoh `(1,0)` = kanan).
- **(l)** `_lastDir` = arah menghadap terakhir. Dipakai animasi **idle**: saat berhenti, karakter tetap menghadap arah terakhir (default menghadap bawah).
- **(m)** `StringToHash` mengubah nama parameter animator ("MoveX", dst.) jadi **angka hash**. Menyetel parameter via hash jauh lebih cepat daripada via string, dan `static readonly` = dihitung sekali saja.

### 5d. `Awake()` — inisialisasi sekali di awal

```csharp
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; } // (n)
            Instance = this;                                                            // (o)
            _rb   = GetComponent<Rigidbody2D>();                                        // (p)
            _anim = GetComponent<Animator>();

            string saved = PlayerPrefs.GetString("PlayerName", "");                     // (q)
            if (!string.IsNullOrEmpty(saved)) playerName = saved;

            string gender = PlayerPrefs.GetString("PlayerGender", "pria");              // (r)
            var ctrl = (gender == "wanita" && animatorWanita != null) ? animatorWanita : animatorPria;
            if (ctrl != null) _anim.runtimeAnimatorController = ctrl;                    // (s)
        }
```
- **(n)** Jaga singleton: kalau sudah ada pemain lain, hancurkan duplikat ini lalu keluar.
- **(o)** Daftarkan diri sebagai satu-satunya `Instance`.
- **(p)** Ambil & simpan komponen fisika + animator.
- **(q)** Baca nama tersimpan; kalau ada, pakai itu.
- **(r)** Baca gender tersimpan (default "pria"); pilih controller animasi sesuai.
- **(s)** Pasang controller terpilih ke Animator. Kalau kosong, biarkan controller bawaan.

### 5e. `Update()` — baca input tiap frame (bagian terpenting)

```csharp
        private void Update()
        {
            if (!CanMove) { _input = Vector2.zero; return; }   // (t)

            var kb = Keyboard.current;                         // (u)
            if (kb == null) return;

            float x = 0f, y = 0f;                              // (v)
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x =  1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x = -1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y =  1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y = -1f;

            _input = new Vector2(x, y).normalized;             // (w)

            bool moving = _input.sqrMagnitude > 0.01f;         // (x)
            if (moving) _lastDir = _input;                     // (y)
```
- **(t)** Kalau `CanMove` mati (sedang dialog/pause), nolkan input dan **keluar** — pemain tak bergerak.
- **(u)** `Keyboard.current` = keyboard aktif dari New Input System. Bisa `null` (mis. tak ada keyboard) → keluar aman.
- **(v)** Baca 4 arah. WASD **atau** panah, keduanya bekerja. `isPressed` = tombol sedang ditahan.
- **(w)** Gabung jadi vektor lalu `.normalized`. **Kenapa dinormalisasi?** Gerak diagonal `(1,1)` panjangnya ≈1.41; tanpa normalisasi, jalan diagonal lebih cepat dari lurus. Normalisasi menyamakan kecepatan ke segala arah.
- **(x)** `sqrMagnitude > 0.01` = cara murah cek "apakah sedang bergerak" (hindari akar kuadrat).
- **(y)** Kalau bergerak, simpan arah ini sebagai arah hadap terakhir (untuk idle menghadap benar).

```csharp
            if (_anim.runtimeAnimatorController != null)       // (z)
            {
                float animX = _input.x, animY = _input.y;      // (aa)
                if (moving && Mathf.Abs(_input.x) > 0.01f && Mathf.Abs(_input.y) > 0.01f)
                {
                    if (Mathf.Abs(_input.x) >= Mathf.Abs(_input.y)) animY = 0f;  // (bb) horizontal menang
                    else                                            animX = 0f;  // (bb) vertikal menang
                }
                _anim.SetFloat(HashMoveX, animX);              // (cc)
                _anim.SetFloat(HashMoveY, animY);
                _anim.SetBool (HashMoving, moving);
                _anim.SetFloat(HashLastX, _lastDir.x);
                _anim.SetFloat(HashLastY, _lastDir.y);
            }
```
- **(z)** Hanya set parameter kalau ada controller (hindari error).
- **(aa)–(bb)** **Trik "sumbu dominan"**: saat diagonal, animasi 4-arah bisa bingung. Kita pilih **satu** sumbu terkuat: kalau |x| ≥ |y| anggap gerak horizontal (nolkan y), sebaliknya vertikal. Hasilnya animasi jalan tidak "kedip" antar-arah. *Catatan: ini hanya untuk memilih animasi; gerak fisika tetap diagonal penuh.*
- **(cc)** Kirim ke Animator: `MoveX/MoveY` (arah berjalan), `IsMoving` (jalan vs diam), `LastX/LastY` (arah idle). Blend Tree di controller memakai nilai ini untuk memilih klip yang benar (depan/belakang/kiri/kanan).

```csharp
            if ((kb.eKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame ||
                 kb.spaceKey.wasPressedThisFrame) && CanMove)   // (dd)
                InteractSystem.Instance?.TryInteract();
        }
```
- **(dd)** Tombol interaksi: **E / Z / Space**. `wasPressedThisFrame` = *baru* ditekan frame ini (sekali per tekan, bukan ditahan). Kalau ada `InteractSystem`, panggil `TryInteract()` (lihat §7). `?.` = null-safe.

### 5f. `FixedUpdate()` — terapkan gerak ke fisika

```csharp
        private void FixedUpdate()
        {
            _rb.linearVelocity = _input * moveSpeed;   // (ee)
        }
```
- **(ee)** Di sinilah karakter benar-benar bergerak. Kecepatan Rigidbody2D = arah input × kecepatan.
  - Karena `_input` sudah dinormalisasi, kecepatan konsisten.
  - `FixedUpdate` berjalan pada langkah fisika tetap → gerak mulus dan tabrakan (collider dinding) dihitung benar oleh engine.
  - Saat `_input = (0,0)` (diam), velocity = 0 → berhenti seketika.

### 5g. Helper publik

```csharp
        public void SetName(string n)  => playerName = n;   // (ff)
        public void SetCanMove(bool v) => CanMove   = v;    // (gg)
```
- **(ff)/(gg)** Cara skrip lain mengubah nama / mengunci gerak (mis. dialog memanggil `SetCanMove(false)`).

### 5h. Ringkasan alur gerak (satu tekan tombol)

```
  tekan "D"
     │  Update(): kb.dKey.isPressed → x=1 → _input=(1,0)
     │            _lastDir=(1,0); animator MoveX=1, IsMoving=true
     ▼
  FixedUpdate(): _rb.linearVelocity = (1,0)*5 = (5,0)  → badan bergeser ke kanan
     │
     ▼
  LateUpdate() kamera: lerp ke posisi pemain (halus)  → layar ikut
```

---

## 6. DEEP DIVE — Kamera mengikuti pemain

File: `Overworld/CameraFollow.cs` (fallback bila tidak pakai Cinemachine).

```csharp
private void LateUpdate()                                   // (a) setelah pemain gerak
{
    if (target == null)
    {
        if (PlayerTopDown.Instance != null)
            target = PlayerTopDown.Instance.transform;      // (b) auto-cari pemain
        return;
    }
    Vector3 desired = target.position + offset;             // (c) posisi ideal
    desired.z = offset.z;                                   // (d) kunci Z (kamera di belakang)
    transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime); // (e)
}
```
- **(a)** `LateUpdate` dipanggil **setelah** semua `Update` — jadi kamera menyesuaikan posisi pemain terbaru (tidak "ketinggalan").
- **(b)** Kalau target belum diisi, ambil pemain dari singleton.
- **(c)** Posisi ideal = posisi pemain + offset (mis. `(0,0,-10)`).
- **(d)** Paksa Z kamera tetap (di 2D, kamera harus di belakang bidang).
- **(e)** `Lerp` = gerak halus menuju posisi ideal (tidak menempel kaku). `smoothSpeed * Time.deltaTime` mengatur seberapa cepat mengejar, independen frame-rate.

> Di scene aktif dipakai **Cinemachine** (`CM_Player_Follow`, ortho size 5). `CameraFollow` adalah
> alternatif ringan dengan logika serupa.

---

## 7. DEEP DIVE — Sistem interaksi & NPC

### 7a. Kontrak `IInteractable` + `InteractSystem`

File: `Overworld/InteractSystem.cs`.

```csharp
public interface IInteractable                     // (a) kontrak
{
    string InteractLabel { get; }                  //     teks hint ("Bicara dengan Sinta")
    void Interact(PlayerTopDown player);           //     apa yang terjadi saat ditekan
}
```
- **(a)** Semua benda yang bisa diajak interaksi (NPC, pintu, dsb.) cukup **mengimplementasikan** interface ini. `InteractSystem` tak perlu tahu tipe konkretnya — ini contoh *polymorphism*.

```csharp
private void Update()
{
    if (PlayerTopDown.Instance == null) return;

    int count = Physics2D.OverlapCircleNonAlloc(               // (b) sapu lingkaran
        PlayerTopDown.Instance.transform.position,
        interactRadius, _hits, interactLayer);

    IInteractable best = null; float bestDist = float.MaxValue;
    for (int i = 0; i < count; i++)                            // (c) cari terdekat
    {
        var interactable = _hits[i].GetComponent<IInteractable>();
        if (interactable == null) continue;
        float d = (_hits[i].transform.position -
                   PlayerTopDown.Instance.transform.position).sqrMagnitude;
        if (d < bestDist) { bestDist = d; best = interactable; }
    }
    _nearest = best;

    InteractHintUI.Instance?.Show(_nearest != null, _nearest?.InteractLabel);  // (d) hint
}

public void TryInteract() => _nearest?.Interact(PlayerTopDown.Instance);       // (e)
```
- **(b)** Tiap frame, sapu lingkaran radius `interactRadius` di sekitar pemain, hanya pada `interactLayer`. `NonAlloc` = pakai array `_hits` yang sudah disiapkan (tanpa alokasi memori tiap frame → hemat GC).
- **(c)** Dari semua yang kena, ambil `IInteractable` **terdekat** (pakai `sqrMagnitude`, tanpa akar).
- **(d)** Tampilkan/sembunyikan hint "Tekan E".
- **(e)** Saat `PlayerTopDown` menekan E (§5e-dd), `TryInteract()` memanggil `Interact()` pada yang terdekat.

### 7b. `NPCController` sebagai `IInteractable`

File: `Overworld/NPCController.cs` (dibedah per-bagian).

- **Identitas** (`npcName`, `npcTitle`, `chapterOwned`, `portrait`).
- **`isSkillNPC` + `skillMode`**: kalau `true`, NPC mengabaikan `chapterOwned` dan mengajar **chapter aktif** (`ChapterManager.GetCurrentActiveChapter()`), dengan mode aktivitas (`ReadBaca`/`TraceOnly`/`Pelafalan`/`Refleksi`).
- **Registry statis** `Dictionary<string,Transform>` — memetakan nama NPC → posisi, dipakai `ObjectiveArrow` untuk menunjuk NPC target.
- **`CreateNameLabel()`** — membuat label nama 3D di atas kepala. Label **dikompensasi skala** induk (`localScale = 1/skalaNPC`, `worldFont` tetap) supaya ukuran nama **seragam** meski tiap NPC beda skala; shadow offset kecil di world agar tidak "dobel". *(Lihat riwayat perbaikan di §13/Changelog.)*
- **`Interact(player)`** (dari interface): mengunci gerak pemain, menghadapkan NPC ke pemain, lalu menampilkan dialog. Untuk NPC skill: menampilkan **daftar pilihan** (mulai aktivitas chapter aktif, ulang aktivitas chapter sebelumnya, atau "Tidak sekarang") via `DialogSystem.ShowWithChoices(...)`. Untuk Guru (Refleksi): pilihan "Hafal dulu → Refleksi", "Langsung Refleksi", "Ulang Ujian {chapter}", dsb.
- **`facePlayer`** di `Update()`: bila pemain dalam `faceRadius`, set `FaceX/FaceY` animator NPC agar menoleh ke pemain.

Alur ringkas menemui NPC:
```
E ditekan → InteractSystem.TryInteract() → NPC.Interact(player)
   → player.SetCanMove(false)
   → NPC menghadap pemain
   → DialogSystem.ShowWithChoices(dialog, opsi, onPilih)
        └─ pemain pilih "Mulai belajar" → LearningPanelManager membuka scene MG_*
```

---

## 8. DEEP DIVE — Dialog & pilihan dinamis

File: `Overworld/DialogSystem.cs`.

- **`DialogLine`** (`[Serializable]`): satu baris dialog = `speakerName`, `text`, `portrait`, `autoAdvanceAfter`.
- **Efek ketik**: teks muncul huruf-demi-huruf (`typeSpeed`), koroutin `_typingCoroutine`. Tekan lagi saat mengetik → langsung tampilkan penuh (`_lineComplete`).
- **Antrian**: `Queue<DialogLine> _queue` — banyak baris ditampilkan berurutan; selesai → panggil `_onFinished`.
- **Pilihan dinamis (gaya Skyrim)**: `ShowWithChoices(lines, labels, onChosen)` membangun daftar tombol kecil bernomor ("1. 2. 3."), warna normal/hover/press, bisa diklik atau ditekan angka 1–9. Dibuat runtime (`_choiceRoot`, `_choiceEntries`) supaya jumlah opsi fleksibel. Tombol pilihan lama (`btnChoice0/1`) disembunyikan.
- **Status**: `IsOpen` & `ChoiceOpen` dipakai skrip lain untuk tahu dialog sedang aktif (mis. mengunci gerak).

Diagram:
```
ShowWithChoices(lines, labels, onChosen)
   → tampilkan baris dialog (ketik) ── selesai ──►  bangun daftar opsi kecil
                                                     │
                              klik / tekan angka ────┤
                                                     ▼
                                             onChosen(indexTerpilih)
```

---

## 9. DEEP DIVE — Manajer inti

### 9a. `GameManager` (Core/GameManager.cs)

Singleton DDOL. Menyimpan **state** (`enum GameState`), **skor**, **nyawa**, dan referensi `AksaraDatabase`. Menyiarkan event:
- `OnStateChanged`, `OnScoreChanged`, `OnLivesChanged`, `OnLevelComplete`.

Metode penting:
- `SetState`, `AddScore`, `LoseLife` (kalau nyawa habis → `GameOver`), `ResetLevel`.
- `SaveProgress(levelKey, bintang)` / `LoadProgress` / `IsLevelComplete` — progres per-level (dipakai Sistem B).
- `HitungBintang(jumlahKesalahan)` → 0 salah = 3★, ≤2 = 2★, selebihnya 1★.

### 9b. `ChapterManager` (Overworld/ChapterManager.cs) — **baris kunci**

Ini otak progres Sistem A. Poin penting per-baris:

```csharp
public enum ChapterID { Tutorial=0, Swara=1, Ngalagena1=2, Ngalagena2=3, Rarangken=4, UjianFinal=5 } // (a)
```
- **(a)** Urutan chapter = nilai enum berurutan. Karena itu "chapter berikutnya" cukup `id + 1` (lihat CompleteChapter).

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    transform.SetParent(null);   // (b) harus root agar DDOL sah
    DontDestroyOnLoad(gameObject);
    LoadProgress();              // (c) muat dari PlayerPrefs
}
```
- **(b)** DDOL hanya bekerja untuk objek **root**; makanya di-`SetParent(null)`.
- **(c)** Bangun kamus progres dari PlayerPrefs (Tutorial default **unlocked**, lain default terkunci).

```csharp
public void CompleteChapter(ChapterID id, int stars)
{
    ... p.completed = true; if (stars > p.stars) p.stars = stars; SaveChapter(p);
    OnChapterCompleted?.Invoke(id);                       // (d)
    ChapterID next = id + 1;                              // (e)
    if (_chapters.TryGetValue(next, out var np) && !np.unlocked)
    {
        np.unlocked = true; SaveChapter(np);
        OnChapterUnlocked?.Invoke(next);
        PlayerPrefs.SetInt("pending_levelup", (int)next); // (f)
        PlayerPrefs.Save();
    }
}
```
- **(d)** Siarkan event → UI (ObjectiveHUD) menyegarkan diri.
- **(e)** Buka chapter berikutnya (`id+1`).
- **(f)** Tandai `pending_levelup` supaya `LevelUpAnnouncer` menampilkan dialog "naik level" saat kembali ke sekolah.

```csharp
public void CompleteSubActivity(ChapterID id, string activity, int stars=0, int score=0, int total=0)
{
    PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_done", 1);           // (g)
    int prev = PlayerPrefs.GetInt($"ch_{(int)id}_{activity}_stars", 0);
    if (stars > prev) PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_stars", stars); // (h)
    if (score > 0) PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_score", score);
    if (total > 0) PlayerPrefs.SetInt($"ch_{(int)id}_{activity}_total", total);
    PlayerPrefs.Save();
    OnSubActivityCompleted?.Invoke(id);                              // (i)
}
```
- **(g)** Tandai sub-aktivitas (baca/tulis/pelafalan/refleksi) selesai.
- **(h)** Simpan bintang **terbaik** (tidak turun saat mengulang).
- **(i)** Siarkan event → HUD memperbarui checklist & skor.

`GetCurrentActiveChapter()` = chapter pertama yang **unlocked tapi refleksi-nya belum selesai** → inilah "chapter aktif" yang diajarkan NPC skill. `GetObjectiveTargetNPC()` = NPC berikutnya yang harus ditemui (Jajang→Sinta→Ucup→Nabila→Guru). `ResetAll()` = hapus semua key (dipakai saat "Mulai Baru").

### 9c. `AudioManager` (Core/AudioManager.cs)

- 3 `AudioSource`: musik, SFX, pelafalan. Volume awal dari PlayerPrefs (`MusicVolume`/`SFXVolume`, default 0.8).
- `PlayMusic` (fade-in dgn koroutin), `StopMusic` (fade-out), `PlaySFX(PlayOneShot)`, `PlayPelafalan`.
- `SetMusicVolume`/`SetSFXVolume` menyimpan ke PlayerPrefs → dipakai `SettingsPanel`.

---

## 10. DEEP DIVE — Perpindahan scene

File: `Core/SceneTransitionManager.cs`.

- Singleton DDOL yang **membangun sendiri** Canvas overlay hitam (`sortingOrder 9999`) di `SetupCanvas()`.
- `LoadScene(name)` → koroutin `TransitionCoroutine`:
  1. `OnTransitionStart`.
  2. `TransitionOut` (fade overlay ke hitam / slide).
  3. `SceneManager.LoadSceneAsync(...)` dengan `allowSceneActivation=false`; tunggu `progress ≥ 0.9`, baru aktifkan → scene siap sebelum tampil.
  4. `TransitionIn` (fade balik ke bening).
  5. `OnTransitionComplete`.
- Efek: `Fade` (alpha 0↔1) dan `SlideLeft` (geser). Durasi `transitionDuration` (0.5s).

Diagram:
```
LoadScene("00_Sekolah")
  → fade ke hitam ──► LoadSceneAsync (tunggu 90%) ──► aktifkan ──► fade ke bening
```

---

## 11. DEEP DIVE — Mini-game

File: `MiniGame/MiniGameManager.cs` (+ `MiniGameData`, panel-panel).

**Panel (fase):** `panelIntro → panelLearn → panelTrace → panelQuiz → panelResult`.

**Data:** `MiniGameData` berisi `MiniGameMode` + daftar kartu aksara. `MiniGameMode` menentukan **rute**:
| Mode | NPC | Rute |
|------|-----|------|
| `ReadBaca` | Sinta | Learn → Quiz baca (aksara → pilih nama) |
| `TraceOnly` | Ucup | Trace (menulis) → penilaian |
| `Pelafalan` | Nabila | Learn (audio) → Quiz dengar / rekam suara (Vosk) |
| `Refleksi` | Guru | Pilih review/langsung → Quiz campuran (lebih ketat) |
| `CombineLetters` | Andre | Gabung konsonan + rarangken |

**Alur umum:**
```
Intro (NPC bicara)  → Learn (jelajah kartu: sprite aksara + latin + audio)
   → Trace (opsional, gores aksara; dinilai QDollar/Direction recognizer)
   → Quiz (pilih ganda / dengar / rekam)  → Result (skor + bintang)
        └─ ChapterManager.CompleteSubActivity(chapter, mode, stars, score, total)
```
Hasil disimpan → HUD & progres ter-update; jika Refleksi lulus → `CompleteChapter` → unlock berikutnya.

---

## 12. Sistem progres & tabel PlayerPrefs

Semua progres bertahan di `PlayerPrefs` (registry lokal). Kunci utama:

| Key | Arti |
|-----|------|
| `PlayerName` | Nama pemain |
| `PlayerGender` | "pria"/"wanita" (pilih animator) |
| `tutorial_done` | 1 = tutorial selesai (dipakai tampilkan "Lanjutkan") |
| `ch_{id}_unlock` | Chapter terbuka (Tutorial default 1) |
| `ch_{id}_done` | Chapter selesai |
| `ch_{id}_stars` | Bintang chapter (0–3) |
| `ch_{id}_{act}_done` | Sub-aktivitas selesai (`act` = baca/tulis/pelafalan/refleksi/combine) |
| `ch_{id}_{act}_stars` | Bintang sub-aktivitas (terbaik) |
| `ch_{id}_{act}_score` / `_total` | Skor benar / total soal |
| `pending_levelup` | ChapterID yang baru terbuka → dialog naik level |
| `MusicVolume` / `SFXVolume` | Volume (0–1) |
| `HighScore` | Skor tertinggi (Sistem B) |

`{id}` = angka `ChapterID` (0..5). Contoh: `ch_1_baca_done` = aktivitas "Membaca" chapter Swara.

---

## 13. Pipeline animasi karakter

**Parameter Animator** (di-set oleh `PlayerTopDown`): `MoveX`, `MoveY` (arah jalan), `IsMoving`
(jalan/diam), `LastX`, `LastY` (arah idle). Blend Tree memilih klip depan/belakang/kiri/kanan.

**Klip walk (Cowok/Cewek), per arah:** dibangun dari spritesheet `walk_<arah>.png` (ppu 70, pivot
tengah), tiap frame 70×80. Prinsip penting hasil perbaikan terbaru:
- **Kaki dikunci** di baris `y=26` (sama seperti idle) → tidak ada lompatan posisi / kepala terpotong.
- **Atas/bawah**: 6 frame penuh (siklus jalan).
- **Kiri/kanan**: 2 pose (contact + passing) di-anchor pada **posisi kepala** (bukan tengah badan)
  supaya kepala tidak goyang ("pargoy"); ditambah **bounce 1px** agar terbaca melangkah. @6fps.

**Label nama NPC:** dibuat runtime oleh `NPCController.CreateNameLabel()`, **kebal skala** (counter-
scale) → ukuran seragam untuk semua NPC; shadow offset kecil di world agar tidak dobel.

**Ukuran NPC:** disetel by-formula `scale = tinggiTargetWorld × PPU ÷ tinggiArtPiksel` supaya semua
NPC punya tinggi art dunia sama (~0.98u), sekaligus kaki di-reground agar tetap napak.

---

## 14. Cara menambah konten baru

**Menambah NPC skill baru:**
1. Duplikat GameObject NPC di scene `00_Sekolah`, pasang `NPCController`.
2. Set `npcName`, centang `isSkillNPC`, pilih `skillMode`.
3. Pastikan ada `Collider2D` di `interactLayer` agar `InteractSystem` mendeteksinya.
4. Isi `dialogIdle`/`dialogLocked` bila perlu.

**Menambah chapter baru:**
1. Tambah nilai di `enum ChapterID` (jaga urutan).
2. Siapkan `MiniGameData` (kartu aksara) untuk tiap mode.
3. Sesuaikan `GetCurrentActiveChapter()` & `GetObjectiveTargetNPC()` bila urutannya khusus.

**Menambah aktivitas:** tambahkan string `act` baru dan panggil `CompleteSubActivity(id, "act", ...)`
di akhir mini-game; tambahkan ke `AllSubActivitiesDone`/`GetTotalStars` bila ikut menentukan kelulusan.

---

## 15. Glosarium

| Istilah | Arti singkat |
|---------|-------------|
| **Singleton** | Objek satu-satunya, diakses via `X.Instance`. |
| **DDOL** | `DontDestroyOnLoad` — objek bertahan saat scene berganti. |
| **`[SerializeField]`** | Field privat yang tetap bisa diatur di Inspector. |
| **`Update` vs `FixedUpdate`** | Update = tiap frame (input); FixedUpdate = langkah fisika tetap (gerak/tabrakan). |
| **`LateUpdate`** | Setelah semua Update — dipakai kamera. |
| **Blend Tree** | Struktur Animator yang memilih klip berdasarkan parameter (arah). |
| **`normalized`** | Vektor dipendekkan jadi panjang 1 → kecepatan seragam. |
| **`sqrMagnitude`** | Panjang kuadrat vektor (tanpa akar, lebih murah). |
| **PlayerPrefs** | Penyimpanan key-value lokal yang bertahan antar-sesi. |
| **Event C#** | Mekanisme "siaran" (publisher) yang di-*subscribe* skrip lain. |
| **`IInteractable`** | Interface: kontrak agar objek bisa diajak interaksi. |
| **Chapter / Sub-aktivitas** | Bab (Swara, dst.) / langkah di dalamnya (baca/tulis/pelafalan/refleksi). |

---

## 16. Model data aksara (ScriptableObject)

File: `Data/AksaraData.cs`, `AksaraDatabase.cs`, `KategoriAksara.cs`.

`AksaraData` adalah **ScriptableObject** (aset data, bukan objek scene) — dibuat via menu
`Create → Aksara Sunda/Aksara Data`. Isinya satu huruf aksara:

| Field | Tipe | Arti |
|-------|------|------|
| `namaHuruf` | string | Nama huruf (mis. "Ka") |
| `kategori` | `KategoriAksara` | Swara / Ngalagena / Rarangken / dll (enum) |
| `spriteHuruf` | Sprite | Gambar aksara |
| `audioPelafalan` | AudioClip | Suara ucapan huruf |
| `fonetik` | string | Cara baca (fonetik) |
| `rarangkenList` | `List<RarangkenData>` | Varian huruf + rarangken (pengubah vokal) |

`RarangkenData` (struct): `namaRarangken`, `bunyiVokal`, `spriteRarangken`,
`spriteHurufDenganRarangken`. Helper `GetRarangken(nama)` & `HasRarangken(nama)` mencari varian.

`AksaraDatabase` mengumpulkan banyak `AksaraData` (diakses lewat `GameManager.AksaraDatabase`).
Mini-game membaca database ini untuk menyusun kartu belajar & soal kuis.

```
GameManager ──► AksaraDatabase ──► AksaraData[] ──► (dipakai) MiniGameManager
                                       └─ RarangkenData[]  (varian vokal)
```

---

## 17. Scene, build, & titik masuk (entry point)

Alur scene resmi (Sistem A):

```
01_MainMenu ──► 00a_NamaKarakter ──► 00_Sekolah ⇄ MG_* (mini-game) ──► kembali 00_Sekolah
     ▲                                   │
     └──────────── (Pengaturan/Keluar) ──┘
```

- **`01_MainMenu`** — titik masuk. `MainMenuController` menyalakan "Lanjutkan" hanya bila ada save
  (`PlayerName` terisi & `tutorial_done==1`).
- **`00a_NamaKarakter`** — input nama + gender; `NameInputController.OnMulai()` memanggil
  `ChapterManager.ResetAll()` (game baru bersih).
- **`00_Sekolah`** — overworld utama (pemain, NPC, HUD, kamera).
- **`MG_*`** — scene mini-game per aktivitas.

Objek global (GameManager, ChapterManager, AudioManager, SceneTransitionManager) bersifat DDOL,
jadi dibuat sekali lalu hidup lintas scene. Banyak UI (ObjectiveHUD, SettingsPanel) **self-bootstrap**
lewat `[RuntimeInitializeOnLoadMethod]` sehingga tak perlu dipasang manual di tiap scene.

---

## 18. Pengujian & verifikasi

- Skrip Editor di folder `Editor/` (generator, builder) hanya jalan di Unity Editor — **tidak** masuk build runtime.
- Verifikasi cepat via **MCP for Unity**: `refresh_unity` (cek 0 error), `manage_editor play/stop`,
  `execute_code` untuk inspeksi state (mis. mengukur `worldFont` label NPC atau bbox frame walk).
- Untuk cek visual world (sprite/animasi/label) gunakan render kamera ke `RenderTexture` → PNG; UI
  overlay (ScreenSpaceOverlay) tidak tertangkap kamera, jadi diperiksa lewat inspeksi struktur.

---

## 19. Changelog perbaikan penting (konteks)

Ringkasan perbaikan terbaru yang memengaruhi bab animasi/tata-letak:

| Area | Masalah | Perbaikan |
|------|---------|-----------|
| Walk atas/bawah | Cuma 2 frame nyaris sama → macet | Rebuild 6 frame dari sheet `_original`, kaki dikunci y=26 |
| Walk kiri/kanan | Kepala goyang ("pargoy") | Anchor ke posisi kepala + bounce 1px, 2 pose @6fps |
| Label nama NPC | Kecil & tidak konsisten / dobel | Kebal-skala (counter-scale), world font tetap, shadow offset kecil |
| Ukuran NPC | Beda-beda (PPU 32 vs 100) | Set scale by-formula → tinggi art dunia seragam ~0.98u + reground kaki |

---

## 20. Berkas turunan dokumen ini

| Berkas | Isi |
|--------|-----|
| `Docs/PANDUAN_LENGKAP_APLIKASI.md` | Dokumen sumber (Markdown). |
| `Docs/PANDUAN_LENGKAP_APLIKASI.docx` | Versi Word (siap cetak) + gambar diagram tertanam. |
| `Docs/DIAGRAMS.drawio` | Diagram editable (buka di draw.io / diagrams.net). Berisi halaman: Arsitektur, Alur end-to-end, Loop belajar, Pipeline gerak, Diagram kelas. |
| `Docs/diagrams/*.png` | Gambar diagram (dipakai di .docx). |

---

*Dokumen ini merujuk kode nyata di `Assets/_Game/Scripts/`. Untuk detail alur naratif & level, lihat
`Docs/STORY_LEVELS.md`; untuk diagram aktivitas, `Docs/ACTIVITY_DIAGRAMS.md`; untuk algoritma spesifik,
`Docs/ALGORITHM_DETAIL.md`.*
