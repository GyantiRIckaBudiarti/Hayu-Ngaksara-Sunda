using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public enum MiniGameMode { Standard, CombineLetters, VoiceQuiz, ReadBaca, TraceOnly, Pelafalan, Refleksi }
    public enum QuizType { ReadBaca, ListenPick, HafalanMix, Combine }

    [CreateAssetMenu(menuName = "HayuNgaksara/MiniGame Data", fileName = "MiniGameData")]
    public class MiniGameData : ScriptableObject
    {
        public ChapterID   chapterID;
        public string      npcName;
        public string      introText;
        public string      chapterTitle;
        public MiniGameMode gameMode = MiniGameMode.Standard;
        public List<AksaraCard> cards = new List<AksaraCard>();
    }
}
