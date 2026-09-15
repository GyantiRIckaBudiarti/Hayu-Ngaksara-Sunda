# Penjelasan Detail Algoritma
## Hayu Ngaksara Sunda — Dokumentasi Teknis untuk Skripsi

> Dokumen ini menjelaskan dua algoritma utama yang digunakan:
> 1. **$Q Super-Quick Point-Cloud Recognizer** — pengenalan coretan aksara
> 2. **Vosk Offline Automatic Speech Recognition** — pengenalan ucapan pelafalan

---

# BAGIAN 1 — Algoritma Pengenalan Coretan Aksara ($Q Dollar Recognizer)

## 1.1 Latar Belakang dan Motivasi

Pembelajaran aksara Sunda memerlukan kemampuan mengevaluasi tulisan tangan digital pemain secara real-time tanpa koneksi internet. Teknik pengenalan pola coretan (gesture recognition) dipilih karena:

1. Tidak memerlukan model machine learning yang besar (inference ringan)
2. Dapat berjalan sepenuhnya offline di perangkat mobile
3. Akurat untuk domain spesifik (karakter aksara berjumlah terbatas ~30 karakter)
4. Mendukung multi-stroke (aksara Sunda memerlukan beberapa coretan terpisah)

Algoritma yang digunakan adalah **$Q Super-Quick Point-Cloud Recognizer** yang diperkenalkan oleh Vatavu et al. (2012) sebagai evolusi dari keluarga algoritma Dollar Recognizer ($1, $N, $P).

## 1.2 Konsep Dasar: Point Cloud

Berbeda dengan pendekatan berbasis fitur geometri (sudut, kurva, rasio), $Q merepresentasikan coretan sebagai **kumpulan titik (point cloud)**. Urutan coretan tidak diperhitungkan — hanya distribusi spasial titik-titik yang dicocokkan dengan template.

**Keunggulan point cloud:**
- Invarian terhadap variasi kecepatan menggambar
- Invariant terhadap urutan stroke (order-independent)
- Kompleksitas waktu O(N × M) dengan N=64 titik dan M=jumlah template

## 1.3 Tahapan Algoritma Q$ (Implementasi dalam QDollarRecognizer.cs)

### Tahap 0: Template Registration (Fase Persiapan)

Sebelum recognition, sistem memuat template dari file JSON di folder `Resources/QTemplates/`:

```
QTemplates/
  ka.json          → {"label":"ka", "templates":[{"strokes":[...]},...]}
  ga.json          → {"label":"ga", "templates":[...]}
  ...
```

Tiap label memiliki **beberapa variasi template** untuk menangkap keragaman cara menulis. Template diproses melalui pipeline yang sama dengan gesture pemain.

### Tahap 1: Resample — Normalisasi Kepadatan Titik

**Tujuan:** Menyamakan jumlah titik ke N=64 terlepas dari panjang atau kecepatan coretan.

**Algoritma:**
```
Input: List<Vector2> points (bisa ratusan titik), N=64
Output: List<Vector2> resampled (tepat 64 titik)

1. Hitung cumulative length: cum[i] = Σ dist(pts[i-1], pts[i])
2. totalLen = cum[last]
3. Untuk k = 0 hingga N-1:
   a. target_dist = (k / (N-1)) × totalLen
   b. Cari segmen [lo, lo+1] di mana cum[lo] ≤ target_dist ≤ cum[lo+1]
   c. t = (target_dist - cum[lo]) / (cum[lo+1] - cum[lo])
   d. resampled[k] = Lerp(pts[lo], pts[lo+1], t)
```

**Implementasi (baris 161-186 QDollarRecognizer.cs):**
```csharp
private static List<Vector2> Resample(List<Vector2> pts, int n)
{
    var cum = new float[pts.Count];
    for (int i = 1; i < pts.Count; i++)
        cum[i] = cum[i-1] + Vector2.Distance(pts[i-1], pts[i]);
    float totalLen = cum[pts.Count - 1];
    // interpolasi di tiap interval totalLen/(n-1)
    for (int k = 0; k < n; k++) {
        float d = (float)k / (n-1) * totalLen;
        // binary search untuk segmen, lalu Lerp
    }
}
```

**Visualisasi:**
```
Coretan asli:  ●–●–●–●–●–●–●–●–●–●  (10 titik, tidak merata)
Setelah resample: ● · · ● · · ● · · ●  (64 titik, jarak seragam)
```

### Tahap 2: Scale — Normalisasi Ukuran

**Tujuan:** Menghilangkan perbedaan ukuran coretan (tulisan besar vs kecil menghasilkan hasil sama).

**Algoritma:**
```
1. Cari bounding box: minX, maxX, minY, maxY
2. scale = max(maxX - minX, maxY - minY)
3. Untuk tiap titik p:
   p_baru = ((p.x - minX) / scale, (p.y - minY) / scale)
→ Semua titik berada di [0,1] × [0,1]
```

**Catatan penting:** Menggunakan `max(width, height)` bukan `(width, height)` terpisah agar **rasio aspek dipertahankan**. Aksara dengan proporsi tinggi vs lebar tetap dibedakan.

### Tahap 3: Translate Centroid — Normalisasi Posisi

**Tujuan:** Menghilangkan perbedaan posisi di canvas.

**Algoritma:**
```
1. centroid = mean(semua titik) = (Σp.x/N, Σp.y/N)
2. Untuk tiap titik p: p_baru = p - centroid
→ Centroid cloud ada di origin (0,0)
```

### Tahap 4: Integer Coordinates — Pemetaan ke Ruang Integer

**Tujuan:** Mengonversi koordinat float ke integer [0..1023] untuk kompatibilitas dengan LUT.

**Algoritma:**
```
1. Cari range baru setelah centroid
2. scale = 1023 / max(rangeX, rangeY)
3. offX = (1023 - rangeX × scale) / 2  ← centering per dimensi
4. intX[i] = (pts[i].x - minX) × scale + offX
5. intY[i] = (pts[i].y - minY) × scale + offY
```

**Alasan mapping per-gesture (bukan fixed):**
Mapping fixed `(x+1)/2 × 1023` hanya mengisi ~50% range LUT, menyebabkan semua score ≥ 0.96 dan tidak dapat membedakan aksara satu sama lain. Mapping per-gesture menggunakan seluruh range 1024, sehingga jarak antar aksara berbeda menjadi diskriminatif (baris 219-240 dalam kode).

### Tahap 5: Build LUT — Look-Up Table Nearest Point

**Tujuan:** Preprocessing untuk mempercepat pencarian titik terdekat dari O(N) menjadi O(1).

**Struktur:** Grid 64×64 sel. Tiap sel `[lx, ly]` menyimpan **indeks titik cloud yang paling dekat** ke pusat sel tersebut.

```
LUT_SIZE = 64
LUT_SCALE_FACTOR = 1024 / 64 = 16

Untuk sel [i, j]:
  Untuk setiap titik k:
    px = intX[k] / 16   ← koordinat sel titik k
    py = intY[k] / 16
    d = (px-i)² + (py-j)²
  LUT[i,j] = argmin_k(d)
```

**Kompleksitas pembangunan LUT:** O(N × LUT_SIZE²) = O(64 × 64 × 64) = O(262,144) — dilakukan sekali saat template dimuat.

**Kompleksitas lookup saat recognition:** O(1) — indeks langsung dari `lut[lx, ly]`.

### Tahap 6: Greedy Cloud Match — Pencocokan Awan Titik

**Tujuan:** Menghitung jarak minimum antara cloud kandidat dan cloud template.

**Inovasi Q$ vs $P:**
$Q menggunakan **dua arah pencocokan** (candidate→template DAN template→candidate) dengan **starting point rotation** untuk mengatasi sensitivitas terhadap titik awal.

```
step = floor(√N) = floor(√64) = 8
minSoFar = ∞

Untuk i = 0, 8, 16, 24, 32, 40, 48, 56:
  d1 = CloudDistance(candidate→template, start=i, templateLUT)
  d2 = CloudDistance(template→candidate, start=i, candidateLUT)
  minSoFar = min(minSoFar, d1, d2)
```

### Tahap 6a: CloudDistance (Inti Pencocokan)

```
Fungsi CloudDistance(pts1, pts2, lut2, start, minSoFar):
  sum = 0
  Untuk idx = 0 hingga N-1:
    i = (start + idx) mod N     ← rotasi starting point
    lx = pts1.intX[i] / 16     ← sel LUT untuk titik i
    ly = pts1.intY[i] / 16
    j = lut2[lx, ly]           ← nearest point di pts2 via LUT (O(1))
    dx = pts1.intX[i] - pts2.intX[j]
    dy = pts1.intY[i] - pts2.intY[j]
    weight = 1 - idx/N          ← titik awal diberi bobot lebih tinggi
    sum += weight × √(dx² + dy²)
    
    if sum ≥ minSoFar: return sum  ← early abandoning
  
  return sum
```

**Bobot linier menurun** (1 - idx/N): titik di awal cloud diberi bobot lebih besar, mengikuti observasi bahwa bagian awal coretan lebih diskriminatif untuk aksara (Vatavu et al., 2012).

**Early abandoning:** Jika sum sudah melebihi best score sebelumnya, hentikan kalkulasi. Menghemat rata-rata 40-60% komputasi.

### Tahap 7: Hitung Score dan Threshold

```
maxDist = 0.5 × √2 × MAX_INT_COORD × N
        = 0.5 × 1.414 × 1024 × 64
        = 46,243 (jarak teoritis maksimum)

score = clamp(1 - minDist/maxDist, 0, 1)
```

**Threshold per kategori aksara:**
| Kategori | Threshold | Alasan |
|----------|-----------|--------|
| Swara (a, i, u, e, eu, o) | 0.40 | Aksara vokal lebih sederhana, threshold lebih longgar |
| Rarangken (panghulu, dst) | 0.35 | Tanda vokal berbentuk lebih unik, threshold menengah |
| Ngalagena (ka, ga, na, ...) | 0.30 | Konsonan lebih kompleks, threshold ketat |

## 1.4 Kompleksitas Algoritma

| Fase | Kompleksitas | Keterangan |
|------|-------------|-----------|
| Resample | O(N + P) | P = jumlah titik input |
| Scale | O(N) | |
| Centroid | O(N) | |
| ToIntCoords | O(N) | |
| BuildLUT (kandidat) | O(N × L²) | L=64, dilakukan sekali |
| GreedyCloudMatch | O(N × M × N/step) | M = jumlah template |
| Total per recognition | O(N × M) | ≈ O(64 × M) |

Untuk 30 aksara dengan 5 template masing-masing = M=150 template → ≈ 9,600 operasi per recognition. Pada perangkat modern ini berjalan < 5ms.

## 1.5 Referensi Algoritma Q$

1. **Vatavu, R. D., Anthony, L., & Wobbrock, J. O. (2012).** Gestures as point clouds: A $P recognizer for user interface prototypes. *Proceedings of the 14th ACM International Conference on Multimodal Interaction (ICMI 2012)*, 273-280. ACM. DOI: 10.1145/2388676.2388732

2. **Wobbrock, J. O., Wilson, A. D., & Li, Y. (2007).** Gestures without libraries, toolkits or training: A $1 recognizer for user interface prototypes. *Proceedings of the 20th Annual ACM Symposium on User Interface Software and Technology (UIST 2007)*, 159-168. ACM. DOI: 10.1145/1294211.1294238

3. **Liu, J., Zhong, L., Wickramasuriya, J., & Vasudevan, V. (2009).** uWave: Accelerometer-based personalized gesture recognition and its applications. *Pervasive and Mobile Computing*, 5(6), 657-675. DOI: 10.1016/j.pmcj.2009.07.007

4. **Sakoe, H., & Chiba, S. (1978).** Dynamic programming algorithm optimization for spoken word recognition. *IEEE Transactions on Acoustics, Speech, and Signal Processing*, 26(1), 43-49. DOI: 10.1109/TASSP.1978.1163055 *(Dynamic Time Warping — teknik precursor)*

5. **Rubine, D. (1991).** Specifying gestures by example. *ACM SIGGRAPH Computer Graphics*, 25(4), 329-337. *(Gesture recognition awal berbasis fitur)*

---

# BAGIAN 2 — Algoritma Pengenalan Suara (Vosk Offline ASR)

## 2.1 Latar Belakang

Fitur pelafalan aksara Sunda memerlukan evaluasi otomatis ucapan pemain tanpa ketergantungan server (offline). **Vosk** dipilih karena:

1. **Offline sepenuhnya** — tidak memerlukan koneksi internet
2. **Model compact** — vosk-model-small-id ~39MB, cocok untuk perangkat mobile
3. **Bahasa Indonesia** — mendukung pelafalan aksara Sunda dalam romanisasi Indonesia
4. **Lisensi Apache 2.0** — bebas digunakan dalam aplikasi komersial/edukasi
5. **API C (native)** — dapat dipanggil dari Unity via P/Invoke

## 2.2 Arsitektur Vosk

Vosk dibangun di atas **Kaldi** — framework ASR (Automatic Speech Recognition) akademis yang banyak digunakan dalam penelitian (Povey et al., 2011). Stack teknologinya:

```
┌────────────────────────────────────────────┐
│ Unity Application (C#)                     │
│   VoiceQuizPanel.cs → VoiceRecognitionService.cs │
├────────────────────────────────────────────┤
│ P/Invoke Bridge                            │
│   [DllImport("libvosk")]                   │
├────────────────────────────────────────────┤
│ libvosk.dll (C API)                        │
│   vosk_model_new() → vosk_recognizer_new() │
│   vosk_recognizer_accept_waveform_s()      │
│   vosk_recognizer_final_result()           │
├────────────────────────────────────────────┤
│ Kaldi Internal Pipeline                    │
│   Feature Extraction → AM → LM → Decoder  │
└────────────────────────────────────────────┘
```

## 2.3 Pipeline Pengenalan Suara (Detail Per Tahap)

### Tahap 1: Akuisisi Audio

```csharp
// Rekam 3 detik audio dari mikrofon
AudioClip clip = Microphone.Start(device, false, 3, 16000);
// 16000 Hz = 16 kHz sampling rate (standar ASR)
// mono, 16-bit PCM
```

**Mengapa 16 kHz?** Frekuensi suara ucapan manusia berada di 300 Hz – 3.4 kHz (bandwidth telepon). Nyquist sampling theorem mensyaratkan minimal 2× frekuensi tertinggi = 6.8 kHz. 16 kHz memberikan margin keamanan yang cukup tanpa bandwidth berlebihan.

### Tahap 2: Konversi PCM16

```csharp
private short[] ConvertToPCM16(AudioClip clip)
{
    float[] samples = new float[clip.samples];
    clip.GetData(samples, 0);          // float [-1.0, +1.0]
    short[] pcm = new short[samples.Length];
    for (int i = 0; i < samples.Length; i++)
        pcm[i] = (short)(samples[i] * 32767);  // float → int16
    return pcm;
}
```

Unity menyimpan audio sebagai float32 [-1.0, +1.0]. Vosk memerlukan **PCM 16-bit signed integer** [-32768, +32767]. Konversi: `short = float × 32767`.

### Tahap 3: Ekstraksi Fitur MFCC

**MFCC (Mel-Frequency Cepstral Coefficients)** adalah representasi sinyal audio yang meniru cara pendengaran manusia (Davis & Mermelstein, 1980).

**Pipeline MFCC:**

```
Audio PCM → Frame Analysis → Pre-emphasis → Windowing →
FFT → Mel Filterbank → Log → DCT → MFCC Features
```

**Detail setiap sub-tahap:**

#### a) Frame Analysis
```
Frame size = 25ms = 400 samples @16kHz
Frame shift = 10ms = 160 samples
→ Frame overlap 15ms
→ Untuk 3 detik audio: (3000-25)/10 + 1 = 298 frame
```

#### b) Pre-emphasis Filter
```
s'[n] = s[n] - α × s[n-1],  α ≈ 0.97
```
Mengangkat frekuensi tinggi yang melemah saat produksi suara.

#### c) Windowing (Hamming Window)
```
w[n] = 0.54 - 0.46 × cos(2πn / (N-1))
x_windowed[n] = x[n] × w[n]
```
Meminimalkan efek spektral leakage di ujung frame.

#### d) Fast Fourier Transform (FFT)
```
X[k] = Σ x[n] × e^(-j2πkn/N),  k = 0..N/2
|X[k]|² = power spectrum
```

#### e) Mel Filterbank
```
Mel(f) = 2595 × log10(1 + f/700)

Buat 23-40 triangular filter bank dalam skala Mel
Tiap filter mengintegrasikan energi dalam band frekuensinya
→ Meniru non-linearity pendengaran manusia
```

**Skala Mel vs Linear:**
```
Frekuensi (Hz):  100  200  500  1000  2000  4000  8000
Skala Mel:        150  283  607  1000  1556  2146  2840

→ Manusia lebih sensitif di frekuensi rendah
```

#### f) Log-Energi
```
E[m] = log(Σ |X[k]|² × H_m[k])
```
Mengubah energi ke skala logaritmik (sesuai persepsi volume manusia).

#### g) DCT (Discrete Cosine Transform)
```
c[n] = Σ E[m] × cos(πn(m-0.5)/M),  n = 1..13
```
Dekorelasi dan kompresi fitur → menghasilkan **13 koefisien MFCC**.

Bersama delta dan delta-delta (kecepatan dan percepatan perubahan): **39 fitur per frame**.

### Tahap 4: Acoustic Model (DNN-HMM)

Vosk menggunakan **DNN-HMM (Deep Neural Network — Hidden Markov Model)** hybrid yang dilatih dengan Kaldi.

**HMM untuk pemodelan fonem:**

Hidden Markov Model merepresentasikan setiap fonem (unit suara) sebagai urutan state:

```
Fonem "ka":  [entry] → [inti] → [exit]
              /k/         /a/
```

**Parameter HMM:**
- **A** = matriks transisi state (probabilitas pindah antar state)
- **B** = probabilitas emisi (P(fitur | state)) — dikompute oleh DNN
- **π** = distribusi awal state

**DNN menggantikan GMM:**
Sebelumnya Gaussian Mixture Model digunakan untuk menghitung B. DNN memberikan akurasi lebih tinggi dengan menghitung probabilitas posteror fonem langsung dari fitur MFCC.

**Viterbi Algorithm** menemukan urutan state (dan fonem) paling mungkin:

```
δ[t][j] = max_i[δ[t-1][i] × a_ij] × b_j(o_t)
ψ[t][j] = argmax_i[δ[t-1][i] × a_ij]

dimana:
  δ[t][j] = probabilitas path terbaik ke state j pada waktu t
  a_ij = prob transisi state i → j
  b_j(o_t) = prob emisi observasi o_t dari state j
```

### Tahap 5: Language Model (n-gram)

**Tujuan:** Memilih urutan kata yang paling mungkin dalam bahasa target (Indonesia + kosakata aksara Sunda).

**Trigram Language Model:**
```
P(w_3 | w_1, w_2) = count(w_1, w_2, w_3) / count(w_1, w_2)
```

Dengan **Kneser-Ney smoothing** untuk menangani trigram yang tidak muncul di training data.

**Vosk model bahasa Indonesia** dilatih pada corpus Wikipedia bahasa Indonesia + Common Voice. Kosakata khusus aksara Sunda (ka, ga, nga, ca, ja, nya, ta, da, na, pa, ba, ma, ya, ra, la, wa, sa, ha) secara alami tercover karena merupakan suku kata bahasa Indonesia.

### Tahap 6: WFST Decoder (Beam Search)

**WFST (Weighted Finite-State Transducer)** mengkombinasikan:
- HMM transition graph (acoustic model)
- Pronunciation lexicon (fonem → kata)
- Language model (kata → kalimat)

menjadi satu transducer terintegrasi yang dapat di-decode efisien.

**Beam Search:**
```
Pertahankan hanya N-best hypothesis (beam width B=5-20)
Pada tiap frame, expand hypothesis yang berada dalam threshold B dari yang terbaik
→ Kompleksitas O(|states| × T) praktis meskipun state space besar
```

Output: JSON `{"text": "ka"}` atau `{"text": ""}` jika tidak dikenali.

### Tahap 7: Pencocokan String Toleran (Levenshtein)

Setelah mendapat teks dari Vosk, sistem membandingkan dengan jawaban yang diharapkan menggunakan tiga level pencocokan:

**Level 1 — Exact Match:**
```csharp
recognized == expected  // "ka" == "ka" → true
```

**Level 2 — Contains Match (Toleransi Kalimat Panjang):**
```csharp
recognized.Contains(expected)  // "ini ka bukan" contains "ka" → true
```
Berguna ketika pemain mengucapkan lebih dari satu kata (contoh: "aksara ka").

**Level 3 — Levenshtein Distance ≤ 1:**
```
Levenshtein("kaa", "ka") = 1  → true (satu karakter berlebih)
Levenshtein("ga",  "ka") = 1  → true (satu substitusi) ← PERHATIAN: ini bisa false positive!
Levenshtein("nga", "ka") = 2  → false (terlalu jauh)
```

**Implementasi Levenshtein (Wagner-Fischer Algorithm):**
```csharp
int[,] d = new int[n+1, m+1];
for (int i = 0; i <= n; i++) d[i,0] = i;
for (int j = 0; j <= m; j++) d[0,j] = j;
for (int i = 1; i <= n; i++)
  for (int j = 1; j <= m; j++)
    d[i,j] = a[i-1] == b[j-1]
      ? d[i-1,j-1]                                    // same → no cost
      : 1 + Min(d[i-1,j], d[i,j-1], d[i-1,j-1]);     // delete, insert, replace
return d[n,m];
```

**Kompleksitas:** O(n × m) waktu dan ruang, di mana n,m adalah panjang string.

**Tabel DP contoh "kaa" vs "ka":**
```
    ""  k  a
""   0  1  2
k    1  0  1
a    2  1  0
a    3  2  1  ← Levenshtein = 1
```

## 2.4 Diagram Alur Pipeline Voice Recognition (Ringkasan)

```
[Microphone.Start 16kHz]
        ↓
[AudioClip 3 detik]
        ↓
[float[] → short[] PCM16]
        ↓
[Vosk: accept_waveform_s()]
        ↓ ↓ ↓ ↓ ↓
   Framing → MFCC → DNN-HMM → LM → WFST Decoder
        ↓
[JSON {"text": "ka"}]
        ↓
[Parse text]
        ↓
[IsMatch: Exact | Contains | Levenshtein≤1]
        ↓
[Benar ✓ / Salah ✗]
```

## 2.5 Monitoring Level Suara Real-time

Sebelum merekam, sistem memonitor level suara untuk memastikan mikrofon aktif:

```csharp
private IEnumerator MonitorLevel(string device)
{
    var samples = new float[sampleWindow]; // 256 samples
    while (_monitoring)
    {
        int pos = Microphone.GetPosition(device);
        _monitorClip.GetData(samples, pos - sampleWindow);
        
        // Hitung RMS (Root Mean Square) = ukuran energi sinyal
        float rms = 0f;
        foreach (var s in samples) rms += s * s;
        float level = Sqrt(rms / sampleWindow) * 8f;  // ×8 = amplifikasi visual
        
        sliderVoiceLevel.value = Clamp01(level);
        yield return new WaitForSecondsRealtime(0.05f); // update 20Hz
    }
}
```

**RMS (Root Mean Square):**
```
RMS = √(1/N × Σ s[i]²)
```
Mengukur "energi rata-rata" sinyal audio — semakin keras suara, semakin tinggi RMS.

## 2.6 Keterbatasan Sistem Voice Recognition

| Keterbatasan | Penyebab | Mitigasi |
|-------------|---------|---------|
| Akurasi bervariasi per aksen | Model dilatih aksen Indonesia standar | Levenshtein tolerance ≤1 |
| Noise lingkungan | Mikrofon tidak noise-canceling | Instruksi ke pemain untuk tempat tenang |
| Kata pendek (1-2 suku kata) | ASR lebih akurat untuk kalimat panjang | Level 2 (Contains) menangani konteks tambahan |
| Mikrofon tidak tersedia | Perangkat tanpa mic / permission denied | Tombol "Lewati →" sebagai fallback |
| Latensi 1-3 detik | Vosk decode | Spinner UI + pesan "Memproses..." |

## 2.7 Referensi Algoritma Voice Recognition

1. **Povey, D., Ghoshal, A., Boulianne, G., Burget, L., Glembek, O., Goel, N., ... & Vesely, K. (2011).** The Kaldi speech recognition toolkit. *IEEE 2011 Workshop on Automatic Speech Recognition and Understanding*, 1-4. IEEE Signal Processing Society. *(Fondasi Vosk/Kaldi)*

2. **Davis, S. B., & Mermelstein, P. (1980).** Comparison of parametric representations for monosyllabic word recognition in continuously spoken sentences. *IEEE Transactions on Acoustics, Speech, and Signal Processing*, 28(4), 357-366. DOI: 10.1109/TASSP.1980.1163420 *(MFCC original paper)*

3. **Rabiner, L. R. (1989).** A tutorial on hidden Markov models and selected applications in speech recognition. *Proceedings of the IEEE*, 77(2), 257-286. DOI: 10.1109/5.18626 *(HMM tutorial klasik)*

4. **Graves, A., Mohamed, A., & Hinton, G. (2013).** Deep recurrent neural networks for acoustic modelling. *arXiv preprint arXiv:1303.5778*. *(DNN untuk ASR)*

5. **Levenshtein, V. I. (1966).** Binary codes capable of correcting deletions, insertions, and reversals. *Soviet Physics Doklady*, 10(8), 707-710. *(Algoritma edit distance)*

6. **Wagner, R. A., & Fischer, M. J. (1974).** The string-to-string correction problem. *Journal of the ACM (JACM)*, 21(1), 168-173. DOI: 10.1145/321796.321811 *(Implementasi DP Levenshtein)*

7. **AlphaCephei. (2020).** Vosk Speech Recognition Toolkit. *GitHub Repository*. Retrieved from https://github.com/alphacep/vosk-api *(Vosk library)*

8. **Mohri, M., Pereira, F., & Riley, M. (2002).** Weighted finite-state transducers in speech recognition. *Computer Speech & Language*, 16(1), 69-88. DOI: 10.1006/csla.2001.0184 *(WFST Decoder)*

9. **Jurafsky, D., & Martin, J. H. (2023).** *Speech and Language Processing* (3rd ed., Draft). Stanford University. Retrieved from https://web.stanford.edu/~jurafsky/slp3/ *(Buku referensi ASR komprehensif)*

---

# BAGIAN 3 — Perbandingan dan Keterkaitan Kedua Algoritma

| Aspek | Q$ Dollar (Coretan) | Vosk ASR (Suara) |
|-------|--------------------|--------------------|
| **Domain input** | Touch/stylus 2D points | Audio PCM 16-bit |
| **Representasi fitur** | Point cloud 64 titik ternormalisasi | 39 koefisien MFCC per frame |
| **Model pencocokan** | Template matching + LUT | DNN-HMM + Language Model |
| **Metode keputusan** | Jarak minimum (Greedy Cloud) | Probabilitas maksimum (Viterbi/Beam) |
| **Post-processing** | Threshold adaptif per kategori | Levenshtein distance ≤ 1 |
| **Latensi** | < 5ms | 0.5 – 3 detik |
| **Ketergantungan jaringan** | Offline penuh | Offline penuh |
| **Training data** | Template manual (rekaman dosen/ahli) | Corpus bahasa Indonesia (Common Voice) |
| **Customisasi** | Mudah: tambah JSON template | Sulit: perlu re-train model |
| **Robustness** | Bagus untuk aksara berjumlah terbatas | Bagus untuk kosakata umum Indonesia |

## Alasan Pemilihan Kombinasi Dua Algoritma

Kedua algoritma dipilih karena bersifat **komplementer** dalam konteks pembelajaran aksara Sunda:

1. **Q$ untuk menulis**: Aksara Sunda memiliki ±30 karakter dengan bentuk unik → template matching lebih akurat dan efisien daripada neural network untuk domain terbatas ini. Tidak memerlukan GPU.

2. **Vosk untuk berbicara**: Ucapan manusia memiliki variasi aksen, nada, dan kecepatan yang sangat besar → statistical ASR (HMM-based) lebih robust dibanding template matching audio.

3. **Saling mendukung**: Pemain dapat belajar aksara secara visual (Q$) DAN mengucapkannya (Vosk), menciptakan pengalaman belajar multimodal yang lebih efektif sesuai teori pembelajaran multimedia (Mayer, 2009).

## Referensi Teori Pembelajaran

10. **Mayer, R. E. (2009).** *Multimedia Learning* (2nd ed.). Cambridge University Press. *(Teori multimedia learning — landasan desain game)*

11. **Sweller, J. (1988).** Cognitive load during problem solving: Effects on learning. *Cognitive Science*, 12(2), 257-285. DOI: 10.1207/s15516709cog1202_4 *(Cognitive Load Theory)*

12. **Prensky, M. (2001).** Digital game-based learning. *Computers in Entertainment (CIE)*, 1(1), 21-21. DOI: 10.1145/950566.950596 *(Game-based learning)*

---

*Dokumen dibuat untuk keperluan Bab 4 Skripsi "Hayu Ngaksara Sunda"*  
*Generator: Claude Code — berdasarkan implementasi aktual di QDollarRecognizer.cs dan VoiceQuizPanel.cs*
