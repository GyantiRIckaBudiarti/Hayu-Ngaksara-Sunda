# Permintaan Dokumentasi untuk Skripsi — dari terminal Skripsi

> Terminal Unity: tolong lengkapi dokumentasi bagian-bagian di bawah, dalam format
> yang SAMA seperti `ACTIVITY_DIAGRAMS.md` (PlantUML `@startuml` + tabel langkah
> "Aktor | Aksi | Output"). Semua HARUS dari kode/scene yang benar-benar ada — jangan
> mengarang. Kalau suatu bagian belum diimplementasi, tulis "BELUM ADA".
> Tujuan: skripsi mendokumentasikan SELURUH game (registrasi → overworld → level
> cerita → cutscene → chase → semua modul → 2 algoritma). Simpan hasil ke file baru
> `Docs/STORY_LEVELS.md` (atau tambahkan ke ACTIVITY_DIAGRAMS.md).

## Yang SUDAH ada (jangan diulang)
`ACTIVITY_DIAGRAMS.md` (AD-01..AD-12) + `ALGORITHM_DETAIL.md` ($Q & Vosk). Sudah cukup.

## Yang DIBUTUHKAN (mohon dilengkapi)

### 1. Peta alur scene (scene flow map)
Diagram alur perpindahan SEMUA scene + pemicunya:
`01_MainMenu → 00a_NamaKarakter → 00_Sekolah(overworld) → {02_BelajarHuruf, 03_LatihanBaca, 04_LatihanTulis, 05_Pelafalan, 06_Kuis, MG_Swara, MG_Ngalagena1} → 07_Level1_Cutscene → 08_Level1_Game → 10_Level2_Game → 10b_Level2_Chase → 11_Level3_Game`.
Sebutkan kondisi/tombol yang memicu tiap transisi (mis. selesai chapter, tekan NPC, dsb).

### 2. Main Menu (`MainMenuController.cs`, scene 01_MainMenu)
Tombol apa saja, logika "Lanjutkan vs Mulai Baru", ke mana masing-masing.

### 3. Opening Cutscene (`OpeningCutscene.cs`) + Cutscene Level (`Level1/CutsceneController.cs`)
Alur cutscene: dialog siapa, urutan, skippable?, ke mana setelah selesai.

### 4. Level 1 — Susun Kertas (Swara) — `Level1GameManager.cs` + `WindPaperSpawner`, `SlotBoard`, `DraggablePaper`, `Slot`, `WindAmbience`
State: Cutscene → Collect (kumpulkan kertas tertiup angin) → Arrange (susun di papan) → Complete.
Jelaskan: mekanik drag kertas, cara papan menilai benar/salah, rumus bintang (dari `_totalKesalahan`), NPC Sinta & Jajang perannya apa.

### 5. Level 2 — Chase + Trace Rarangken — `Level2GameManager.cs`, `ChaseManager.cs`, `ChaseJajang`, `ChaseObstacle(+Spawner)`, `ChaseCatchBar`, `ChaseBackground`, `RarangkenSelector`, `TraceCanvas`, `TraceEvaluator`, `TraceUI`
Jelaskan: mekanik chase (kejar/hindari rintangan, catch bar), kapan trace rarangken muncul, bagaimana TraceEvaluator menilai (apakah pakai $Q juga?), kondisi menang/kalah, bintang.

### 6. Level 3 — Pelafalan — `Level3GameManager`/`PelafalanGameManager.cs` + `GuruController.cs`
Alur khusus level 3 (beda dari panel Pelafalan biasa?), peran NPC Guru, penilaian.

### 7. Sistem Chapter, Bintang & Nilai — `ChapterManager.cs` (sudah saya baca, tolong konfirmasi/koreksi)
Chapter: Tutorial, Swara, Ngalagena1, Ngalagena2, Rarangken, UjianFinal.
Sub-aktivitas: baca(Sinta), tulis(Ucup), pelafalan(Nabila), refleksi(Guru), combine(Andre).
Nilai akhir A–E: A≥11,B≥9,C≥7,D≥5, else E (total bintang). → benar?

### 8. Level Summary / Rapor — `LevelSummaryController.cs`, `WritingProgressPanel.cs`, `ObjectiveHUD.cs`, `QuestGuide.cs`
Apa yang ditampilkan (bintang, nilai, progres), kapan muncul.

### 9. NPC & Karakter — `Characters/*Controller.cs` + `NPCController`, `DialogSystem`, `InteractSystem`
Ringkas peran tiap NPC (Jajang, Sinta, Ucup, Nabila, Andre) + sistem dialog & interaksi (trigger E/tap).

### 10. Daftar kelas inti + relasi (untuk Class Diagram)
Daftar class utama per folder (Core, Overworld, Level1-3, MiniGame, Characters, Data, UI) + siapa memanggil siapa (mis. Level1GameManager → SlotBoard, WindPaperSpawner; MiniGameManager → QDollarRecognizer). Cukup poin-poin.

### 11. Data & aset
- Isi `Resources/QTemplates/` (aksara apa saja punya template).
- Struktur `AksaraData`/`AksaraDatabase`/`MiniGameData` (field-nya).
- Kunci PlayerPrefs lengkap yang benar-benar dipakai.

### 12. Platform/build
Konfirmasi build target aktif (skripsi akan tulis Windows PC/.exe — apakah cocok, atau sebenarnya Android?).

---
Setelah diisi, beri tahu terminal Skripsi: "STORY_LEVELS.md sudah siap". Terima kasih 🙏
