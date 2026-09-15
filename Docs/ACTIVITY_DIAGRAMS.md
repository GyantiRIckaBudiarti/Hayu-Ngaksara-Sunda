# Activity Diagram — Hayu Ngaksara Sunda
## Bab 4 Skripsi · PlantUML Source + Deskripsi Langkah

> Render di: https://www.plantuml.com/plantuml/uml/  
> Atau gunakan plugin PlantUML di VS Code / IntelliJ

---

## AD-01 — Alur Sistem Keseluruhan (System Overview)

```plantuml
@startuml AD01_SystemOverview
skinparam ActivityFontSize 13
skinparam ArrowFontSize 11
title Alur Sistem Keseluruhan — Hayu Ngaksara Sunda

|Pemain|
start
:Buka Aplikasi;
:Layar Main Menu tampil;

if (Ada data save\n(nama tersimpan)?) then (ya)
  :Tampilkan tombol\n"Lanjutkan Petualangan";
else (tidak)
  :Tampilkan tombol\n"Mulai Baru" saja;
endif

if (Pilih aksi?) then (Mulai Baru)
  :Scene NamaKarakter;
  :Input nama & pilih gender;
  :Reset semua progress\n(PlayerPrefs);
  :Load Scene 00_Sekolah;
else if (Lanjutkan) then
  :Load Scene 00_Sekolah;
else (Keluar)
  :Application.Quit();
  stop
endif

|Sistem|
:Overworld aktif\n(Sekolah Aksara);

repeat
  :Pemain navigasi;
  if (Mendekat NPC?) then (ya)
    :Deteksi trigger NPC;
    :Dialog NPC tampil;
    if (Konfirmasi belajar?) then (ya)
      :Set PlayerPrefs mode\n(ActiveMode, ActiveNPCTitle);
      :Load Scene MiniGame;
      :MiniGame selesai;
      :Kembali ke Overworld;
    else (tidak)
      :Tutup dialog;
    endif
  endif
repeat while (Pemain keluar?) is (tidak)

:Game selesai;
stop
@enduml
```

### Deskripsi Langkah AD-01

| # | Aktor | Aksi | Output |
|---|-------|------|--------|
| 1 | Pemain | Membuka aplikasi | Main Menu tampil |
| 2 | Sistem | Cek PlayerPrefs "PlayerName" | Tombol Lanjutkan aktif/hidden |
| 3 | Pemain | Pilih Mulai Baru / Lanjutkan / Keluar | Scene change |
| 4 | Sistem | Load `00a_NamaKarakter` (baru) atau `00_Sekolah` (lanjut) | Scene transition |
| 5 | Sistem | Overworld aktif, player spawn | Player dapat bergerak |
| 6 | Sistem | Deteksi jarak Player–NPC (OnTriggerEnter2D) | Dialog NPC tampil |
| 7 | Pemain | Konfirmasi interaksi | MiniGame scene di-load |
| 8 | Sistem | MiniGame selesai → CompleteChapter/SubActivity | Progress tersimpan |
| 9 | Sistem | Kembali ke Overworld | Loop berlanjut |

---

## AD-02 — Registrasi Pemain (Nama & Gender)

```plantuml
@startuml AD02_Registration
title Registrasi Pemain

|Pemain|
start
:Scene NamaKarakter\ntampil;

:Ketik nama karakter\n(InputField);

if (Pilih gender?) then (Pria)
  :PlayerGender = "pria";
  :Animator = animatorPria;
else (Wanita)
  :PlayerGender = "wanita";
  :Animator = animatorWanita;
endif

:Tekan "Mulai";

|Sistem|
if (Nama kosong?) then (ya)
  :Tampilkan peringatan\n"Nama tidak boleh kosong";
  |Pemain|
  :Isi nama;
else (tidak)
  :ResetAll() —\nhapus semua PlayerPrefs;
  :Simpan PlayerPrefs:\n- PlayerName = nama\n- PlayerGender = gender;
  :Set tutorial_done = 0;
  :Load Scene "00_Sekolah";
endif

stop
@enduml
```

### Deskripsi Langkah AD-02

| # | Aksi | Komponen | Keterangan |
|---|------|----------|-----------|
| 1 | Input nama | NameInputController.InputField | Nama bebas, maks 20 karakter |
| 2 | Pilih gender | NameInputController.btnPria / btnWanita | Default: Pria |
| 3 | Validasi nama | NameInputController.OnMulai() | IsNullOrEmpty check |
| 4 | Reset progress | ChapterManager.ResetAll() | Hapus semua key ch_*_unlock |
| 5 | Simpan data | PlayerPrefs.SetString("PlayerName") | Persisten di device |
| 6 | Load Overworld | SceneManager.LoadScene("00_Sekolah") | Transition ke game |

---

## AD-03 — Navigasi Overworld & Interaksi NPC

```plantuml
@startuml AD03_Overworld
title Navigasi Overworld

|Pemain|
start
:Berada di Overworld\n(00_Sekolah);

|Sistem|
:ObjectiveHUD tampilkan\nobjektif saat ini;

|Pemain|
repeat
  :Tekan tombol arah\n(WASD / Arrow);
  
  |Sistem|
  :Update posisi Player\n(velocity × speed × Time.deltaTime);
  :Animator update\n(idle/walk berdasarkan input);
  :Camera ikuti Player\n(Cinemachine / transform.position);
  :Tilemap Walls (CompositeCollider2D)\ncegah melewati dinding;
  
  if (Masuk trigger NPC?) then (ya)
    :Tampilkan tombol\n"Tekan E / Tap untuk bicara";
    
    |Pemain|
    if (Tekan interaksi?) then (ya)
      |Sistem|
      :NPCController.OnInteract();
      :Dialog NPC tampil\n(DialogPanel);
      
      |Pemain|
      :Baca dialog;
      :Tekan lanjut/konfirmasi;
      
      |Sistem|
      if (NPC punya MiniGame?) then (ya)
        :Set PlayerPrefs:\n- ActiveMode\n- ActiveNPCTitle\n- ActiveNPCName;
        :SceneTransitionManager\n.LoadScene(miniGameScene);
      else (tidak)
        :Dialog tutorial Jajang\n(hanya info);
        :Set tutorial_done = 1;
      endif
    else (tidak)
      :Lanjutkan jalan;
    endif
  endif

repeat while (Game berlanjut?) is (ya)
stop
@enduml
```

---

## AD-04 — Panel Belajar Aksara (Learn Panel)

```plantuml
@startuml AD04_LearnPanel
title Panel Belajar Aksara

|Pemain|
start
:Panel Belajar terbuka;

|Sistem|
:Muat data dari MiniGameData\n(chapter cards);
:Tampilkan kartu pertama\n(index 0);

|Pemain|
repeat
  |Sistem|
  :Tampilkan:\n- Aksara (font Sundanese)\n- Nama Latin\n- Deskripsi\n- Progress "N/Total";
  :Auto-play audio pelafalan\n(0.3 detik delay);
  
  |Pemain|
  :Pelajari aksara;
  
  if (Aksi pemain?) then (Tombol Audio)
    |Sistem|
    :PlayCurrentAudio()\n→ Resources.Load AudioClip\n→ Fallback ke TTS;
  else if (Tombol Berikutnya)
    |Sistem|
    if (Bukan kartu terakhir?) then (ya)
      :cardIndex++;
      :ShowCard(cardIndex);
    else (tidak)
      :Tampilkan tombol\n"Mulai Kuis →";
    endif
  else if (Tombol Sebelumnya)
    |Sistem|
    :cardIndex--;
    :ShowCard(cardIndex);
  else if (Tombol Keluar)
    :ExitEarly();
    :Load 00_Sekolah;
    stop
  endif

repeat while (Belum klik Mulai Kuis?) is (ya)

|Pemain|
:Klik "Mulai Kuis →";
|Sistem|
:StartQuiz() / StartVoiceQuiz();

stop
@enduml
```

---

## AD-05 — Latihan Menulis / Trace Panel (+ Algoritma Q$)

```plantuml
@startuml AD05_TracePanel
title Latihan Menulis Aksara (Trace + Q$ Recognizer)

|Pemain|
start
:Panel Trace terbuka;

|Sistem|
:Muat semua kartu chapter;
:Tampilkan aksara target\n(kartu pertama);
:LoadFromResources("QTemplates")\n→ muat template Q$;

|Pemain|
repeat
  |Sistem|
  :Tampilkan:\n- Aksara target (besar)\n- Canvas gambar (putih)\n- Panduan stroke (opsional)\n- Progress "N/Total";
  
  |Pemain|
  :Mulai menggambar di canvas;
  :Angkat jari = satu stroke selesai;
  
  |Sistem|
  :Kumpulkan List<Vector2> per stroke;
  :Ketika "Cek" ditekan:;
  
  :━━ ALGORITMA Q$ ━━;
  :1. Gabung semua stroke → all points;
  :2. Resample(all, N=64)\n   → 64 titik berjarak sama;
  :3. Scale(points)\n   → normalisasi ke kotak satuan;
  :4. TranslateCentroid(points)\n   → centroid ke origin;
  :5. ToIntCoords(points)\n   → map ke [0..1023];
  :6. BuildLUT(intX, intY)\n   → tabel 64×64 nearest-point;
  :7. GreedyCloudMatch terhadap\n   semua template;
  :8. Hitung score = 1 − minDist/maxDist;
  
  if (score ≥ threshold?) then (ya — cocok)
    :Tampilkan feedback hijau\n"✓ Benar! (score%)";
    :Simpan trace_{latinName}_practiced = 1;
  else (tidak)
    :Tampilkan feedback merah\n"Coba lagi";
  endif
  
  |Pemain|
  if (Aksi?) then (Coba Lagi)
    :Bersihkan canvas;
  else if (Kartu Berikut)
    |Sistem|
    if (Bukan terakhir?) then (ya)
      :traceIndex++;
      :ShowTraceCard();
    else (tidak)
      :TrackLastCardAndStartQuiz();
      if (Mode TraceOnly?) then (ya)
        :ShowTraceOnlyResult()\n(hitung BestMatch / Total);
        stop
      else (tidak)
        :StartQuiz();
      endif
    endif
  endif

repeat while (Semua kartu selesai?) is (tidak)

stop
@enduml
```

---

## AD-06 — Panel Kuis Normal (Aksara → Nama Latin)

```plantuml
@startuml AD06_QuizNormal
title Kuis Normal: Aksara → Nama Latin

|Pemain|
start

|Sistem|
:BuildQuestions(hardMode=false);
:Shuffle semua kartu chapter;
:Untuk tiap kartu:\n  - 1 jawaban benar\n  - 3 distractor acak;
:quizIndex = 0, score = 0;

repeat
  :ShowQuestion(quizIndex);
  :Tampilkan aksara (font Sundanese)\nbesar di tengah layar;
  :Tampilkan pertanyaan:\n"Aksara ini dibaca...?";
  :Tampilkan 4 tombol:\n[KA] [GA] [PA] [NA]\n(teks LATIN — showLatin: true);
  
  |Pemain|
  :Pilih salah satu tombol;
  
  |Sistem|
  if (Jawaban benar?) then (ya)
    :score++;
    :Tombol hijau;
  else (tidak)
    :Tombol merah;
    :Jawaban benar highlight hijau;
  endif
  
  :Tunggu 1.2 detik (flash);
  :quizIndex++;

repeat while (quizIndex < total?) is (ya)

:ShowResult();
:Hitung persentase = score/total;
:Tentukan bintang:\n  ≥90% = 3★\n  ≥60% = 2★\n  ≥40% = 1★\n  <40% = 0★;

if (Bintang > 0?) then (ya)
  :CompleteChapter / CompleteSubActivity;
  :Simpan ch_{id}_stars di PlayerPrefs;
endif

|Pemain|
if (Aksi hasil?) then (Coba Lagi — hanya jika 0★)
  :RetryQuiz();
else (Kembali)
  :BackToOverworld();
  :Load 00_Sekolah;
  stop
endif
@enduml
```

---

## AD-07 — Panel Kuis Hard Mode / Refleksi (Normal + Reverse)

```plantuml
@startuml AD07_QuizHard
title Kuis Refleksi (Hard Mode) — Mode Guru

|Pemain|
start

|Sistem|
:BuildQuestions(hardMode=true);
:Untuk tiap kartu:\n  Buat 2 soal:\n  1. isReverse=false (aksara→latin)\n  2. isReverse=true (latin→aksara);
:Total soal = 2 × jumlah kartu;
:quizIndex = 0, score = 0;

repeat
  :Ambil question[quizIndex];
  
  if (isReverse=false?) then (ya)
    :Tampilkan aksara besar;
    :Pertanyaan: "Aksara ini dibaca...?";
    :4 tombol berisi TEKS LATIN;
  else (tidak — isReverse=true)
    :Tampilkan nama Latin besar;
    :Pertanyaan: "Aksara [X] yang mana?";
    :4 tombol berisi AKSARA (Sundanese font);
  endif
  
  |Pemain|
  :Pilih jawaban;
  
  |Sistem|
  if (Benar?) then (ya)
    :score++;
    :Flash hijau 0.8 detik;
  else (tidak)
    :Flash merah + highlight benar;
  endif
  
  :quizIndex++;
repeat while (quizIndex < total?) is (ya)

:Hitung threshold KETAT:\n  ≥95% = 3★\n  ≥80% = 2★\n  ≥60% = 1★\n  <60% = 0★;

if (0★ AND retryCount < 2?) then (ya)
  :retryCount++;
  |Pemain|
  :Coba lagi;
else
  if (Bintang > 0?) then (ya)
    :CompleteSubActivity("refleksi");
    :CompleteChapter(chapterID, stars);
    :Unlock chapter berikutnya;
  endif
  
  |Pemain|
  :Kembali ke Overworld;
  stop
endif
@enduml
```

---

## AD-08 — Panel Pelafalan / Voice Quiz

```plantuml
@startuml AD08_VoiceQuiz
title Kuis Pelafalan — Voice Recognition

|Pemain|
start

|Sistem|
:VoiceRecognitionService.Init();
:Load model Vosk\n(StreamingAssets/vosk-model-small-id);
:voiceIndex=0, voiceScore=0;
:Deteksi perangkat mikrofon;

if (Mikrofon tersedia?) then (ya)
  :btnMic aktif;
else (tidak)
  :Tampilkan tombol "Lewati →";
  :Status: "Mikrofon tidak tersedia";
endif

repeat
  :Tampilkan aksara (index voiceIndex);
  :Monitor level suara realtime\n(RMS setiap 50ms);
  
  |Pemain|
  if (Tekan btnMic?) then (ya)
    |Sistem|
    :StartRecording(3 detik);
    :Slider timer berjalan;
    :VoiceRecognitionService\n.StartRecording();
    
    :Rekam audio 3 detik\n(Microphone.Start);
    :Timer habis;
    
    :Spinner "Memproses..." tampil;
    :StopAndRecognize();
    :Microphone.End → AudioClip;
    :ConvertToPCM16(audioClip)\n→ short[];
    :vosk_recognizer_accept_waveform_s\n(recognizer, pcmData, length);
    :vosk_recognizer_final_result()\n→ JSON string;
    :Parse JSON → ambil field "text";
    
    :━━ PENCOCOKAN STRING ━━;
    :recognized = parsed text;
    :IsMatch(recognized, expected):;
    :1. Exact match? → recognized == expected;
    :2. Contains? → recognized.Contains(expected);
    :3. Levenshtein(recognized, expected) ≤ 1?;
    
    if (IsMatch = true?) then (ya)
      :voiceScore++;
      :Tampilkan "✓ Benar!";
    else (tidak)
      :Tampilkan "✗ Jawaban: [expected]";
    endif
    
    :Tunggu 2 detik;
    :voiceIndex++;
    
  else if (Tekan Lewati?) then
    :voiceIndex++;
  endif

repeat while (voiceIndex < total?) is (ya)

:ShowVoiceResult();
:_score = voiceScore;
:Hitung bintang & simpan progress;
:CompleteSubActivity("pelafalan");

|Pemain|
:Kembali ke Overworld;
stop
@enduml
```

---

## AD-09 — Panel Gabung Aksara (Ujian Final — Andre)

```plantuml
@startuml AD09_CombineLetters
title Gabung Aksara (CombineLetters Mode — Andre)

|Pemain|
start

|Sistem|
:BuildCombineQuestions();
:Pisahkan cards:\n  - Konsonan (aksaraChar.Length = 1, bukan Swara)\n  - Rarangken (aksaraChar.Length > 1);
:Buat kombinasi: tiap konsonan × tiap rarangken;
:combined = konsonan[:-1] + vokal_rarangken;
:Contoh: "ka" + "i" (panghulu) = "ki";
:Max 12 soal, lalu shuffle;

repeat
  :ShowCombineQuestion(idx);
  :Tampilkan komponen A (konsonan)\nfont Sundanese;
  :Tampilkan komponen B (rarangken)\nfont Sundanese;
  :Tampilkan 4 tombol jawaban\n(1 benar + 3 salah)\nberisi teks LATIN KAPITAL;
  
  |Pemain|
  :Pilih jawaban;
  
  |Sistem|
  if (Jawaban.ToLower() == combined?) then (ya)
    :combineScore++;
    :Tombol hijau;
    :Tunggu 1 detik;
  else (tidak)
    :Tombol merah;
    :Highlight jawaban benar hijau;
    :Tunggu 2 detik;
  endif
  
  :combineIndex++;

repeat while (combineIndex < total?) is (ya)

:ShowCombineResult();
:pct = combineScore/total;
:stars: ≥90%=3★, ≥60%=2★, ≥40%=1★;

if (stars > 0?) then (ya)
  :CompleteChapter(UjianFinal, stars);
endif

|Pemain|
:Kembali ke Overworld;
stop
@enduml
```

---

## AD-10 — Sistem Progres & Chapter Unlock

```plantuml
@startuml AD10_ChapterProgress
title Sistem Progres Chapter (ChapterManager)

|Sistem|
start
:Init ChapterManager;
:Baca semua key PlayerPrefs:\n  ch_{id}_unlock\n  ch_{id}_done\n  ch_{id}_stars\n  ch_{id}_baca_done\n  ch_{id}_tulis_done\n  ch_{id}_pelafalan_done\n  ch_{id}_refleksi_done;

:Chapter Tutorial selalu unlock;

if (Pemain pertama kali?) then (ya)
  :Hanya Tutorial unlock;
else (tidak)
  :Load status dari PlayerPrefs;
endif

:Tampilkan ObjectiveHUD\n→ GetCurrentObjective();

|Pemain|
:Selesaikan aktivitas;

|Sistem|
:CompleteSubActivity(chapterID, activity);
:Simpan ch_{id}_{activity}_done = 1;

if (activity == "refleksi"?) then (ya)
  :Cek apakah stars > 0;
  if (stars > 0?) then (ya)
    :CompleteChapter(chapterID, stars);
    :ch_{id}_done = 1;
    :ch_{id}_stars = stars;
    :Cari chapter berikutnya\n→ nextID = chapterID + 1;
    if (nextID ada?) then (ya)
      :ch_{nextID}_unlock = 1;
      :ObjectiveHUD update;\
    else (tidak)
      :Tampilkan "Selamat! Semua selesai!";
    endif
  else (tidak)
    :Tidak unlock chapter baru;
    :Tampilkan "Coba lagi";
  endif
endif

:Update ObjectiveHUD\n→ GetCurrentObjective()\nprioritas: Baca→Tulis→Pelafalan→Refleksi;

stop
@enduml
```

---

## AD-11 — Algoritma $Q Super-Quick Recognizer (Detail)

```plantuml
@startuml AD11_QDollar
title Algoritma $Q Dollar Recognizer — Pengenalan Coretan Aksara

|Input|
start
:Input: List<List<Vector2>> strokes\n(multi-stroke coretan pemain);

|Preprocessing|
:1. RESAMPLE\n   Gabung semua stroke → all_points\n   Hitung total panjang kurva\n   Bagi menjadi N=64 interval sama panjang\n   Interpolasi titik baru di tiap interval;

:2. SCALE\n   Cari bounding box (minX,maxX,minY,maxY)\n   scale = max(width, height)\n   Bagi tiap titik dengan scale\n   → normalisasi ke [0,1]×[0,1];

:3. TRANSLATE CENTROID\n   Hitung centroid c = mean(all_points)\n   Geser semua titik: p = p - c\n   → centroid ke origin (0,0);

:4. INTEGER COORDINATES\n   Cari range baru\n   Map ke [0..1023] dengan aspect ratio dipertahankan\n   → intX[], intY[];

:5. BUILD LUT (Candidate)\n   Grid 64×64 sel\n   Tiap sel [lx,ly] → index titik terdekat\n   LUT_SCALE_FACTOR = 1024/64 = 16;

|Matching|
:Untuk setiap template tersimpan:;

repeat
  :6. GREEDY CLOUD MATCH\n   step = floor(sqrt(N)) = 8\n   Iterasi i = 0, 8, 16, ..., 56;\n
  
  :   6a. CloudDistance(candidate→template)\n       Untuk setiap titik candidat[i..(i+N-1) mod N]:\n         - Cari sel LUT: lx=intX/16, ly=intY/16\n         - j = templateLUT[lx,ly] → nearest index\n         - weight = 1 - idx/N (makin belakang makin kecil)\n         - sum += weight × dist(candidat[i], template[j])\n         - Early abandon jika sum ≥ minSoFar;

  :   6b. CloudDistance(template→candidate)\n       Arah terbalik menggunakan candidateLUT;
  
  :   Update minSoFar = min(d1, d2, minSoFar);
repeat while (template berikutnya?)

|Output|
:7. HITUNG SCORE\n   maxDist = 0.5 × √2 × 1024 × 64\n   score = clamp(1 − minDist/maxDist, 0, 1);

:8. CEK THRESHOLD\n   Swara: threshold = 0.40\n   Rarangken: threshold = 0.35\n   Ngalagena: threshold = 0.30;

if (recognized == expected\nAND score ≥ threshold?) then (Match)
  :Hasil: BENAR\n(aksara dikenali);
else (No Match)
  :Hasil: TIDAK COCOK\n(coba lagi);
endif

stop
@enduml
```

---

## AD-12 — Algoritma Voice Recognition Pipeline

```plantuml
@startuml AD12_VoiceRecognition
title Pipeline Pengenalan Suara (Vosk Offline ASR)

|Input Audio|
start
:Pemain tekan tombol Mic;
:Microphone.Start(device, false, 3s, 16000Hz);
:Rekam selama 3 detik;
:Microphone.End() → AudioClip;

|Preprocessing|
:ConvertToPCM16(AudioClip):\n  Ambil float[] samples\n  Kalikan tiap sample × 32767\n  Cast ke short[]\n  → PCM 16-bit, 16 kHz mono;

|Vosk Engine (Native)|
:vosk_recognizer_accept_waveform_s\n  (recognizer, pcmData, pcmData.Length);
:Vosk internal pipeline:\n  1. Frame audioditambahkan ke buffer\n  2. MFCC extraction per frame 25ms\n  3. Acoustic model (Kaldi DNN-HMM)\n     hitung P(observasi|fonem)\n  4. WFST decoder dengan language model\n     (weighted finite-state transducer)\n  5. Beam search: pertahankan N-best hypothesis;
:vosk_recognizer_final_result()\n  → JSON: { "text": "ka" };

|Post-processing|
:Marshal.PtrToStringAnsi(ptr)\n→ JSON string;
:Parse JSON → ekstrak field "text";
:recognized = text.ToLower().Trim();

|String Matching|
:IsMatch(recognized, expected):;

if (recognized == expected?) then (ya)
  :Match = true;
else
  if (recognized.Contains(expected)?) then (ya)
    :Match = true;
  else
    :Levenshtein(recognized, expected);
    if (distance ≤ 1?) then (ya)
      :Match = true;
    else (tidak)
      :Match = false;
    endif
  endif
endif

|Output|
if (Match = true?) then (Benar)
  :Tampilkan "✓ Benar!"\nvoiceScore++;
else (Salah)
  :Tampilkan "✗ Jawaban: [expected]\n(ucapan: [recognized])";
endif

:Lanjut ke aksara berikutnya;
stop
@enduml
```

---

## Ringkasan Diagram & Keterkaitan

| Kode | Nama Diagram | Scene/File Terkait | Aktor |
|------|-------------|-------------------|-------|
| AD-01 | Sistem Keseluruhan | Semua scene | Pemain, Sistem |
| AD-02 | Registrasi | `00a_NamaKarakter` | Pemain, Sistem |
| AD-03 | Navigasi Overworld | `00_Sekolah` | Pemain, Sistem |
| AD-04 | Belajar Aksara | `MiniGameManager.ShowCard()` | Pemain, Sistem |
| AD-05 | Latihan Menulis | `TracePanel`, `QDollarRecognizer` | Pemain, Sistem |
| AD-06 | Kuis Normal | `MiniGameManager.ShowQuestion()` | Pemain, Sistem |
| AD-07 | Kuis Hard/Refleksi | `MiniGameManager.BuildQuestions(true)` | Pemain, Sistem |
| AD-08 | Kuis Pelafalan | `VoiceQuizPanel`, `VoiceRecognitionService` | Pemain, Sistem, Mic |
| AD-09 | Gabung Aksara | `MiniGameManager.StartCombine()` | Pemain, Sistem |
| AD-10 | Progres Chapter | `ChapterManager` | Sistem, DB |
| AD-11 | Algoritma Q$ | `QDollarRecognizer.cs` | Sistem |
| AD-12 | Algoritma Voice | `VoiceRecognitionService.cs`, `VoiceQuizPanel.cs` | Sistem |
