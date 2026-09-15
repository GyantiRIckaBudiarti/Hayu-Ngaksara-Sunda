using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public class SlotBoard : MonoBehaviour
    {
        public event Action OnSlotCorrect;
        public event Action OnSlotWrong;
        public event Action OnBoardComplete;

        [SerializeField] private Slot[] slots;

        private List<AksaraData> _urutanYangBenar;
        private int              _hintThreshold = 3;
        private int[]            _wrongCountPerSlot;

        public void Setup(List<AksaraData> urutan)
        {
            _urutanYangBenar   = urutan;
            _wrongCountPerSlot = new int[slots.Length];
            foreach (var slot in slots)
                slot.ClearSlot();
        }

        public bool TryPlaceAksara(AksaraData aksara, Slot targetSlot)
        {
            int slotIndex = System.Array.IndexOf(slots, targetSlot);
            if (slotIndex < 0 || targetSlot.IsOccupied) return false;

            bool benar = _urutanYangBenar[slotIndex].namaHuruf == aksara.namaHuruf;
            if (benar)
            {
                targetSlot.PlaceAksara(aksara);
                targetSlot.FlashCorrect();
                _wrongCountPerSlot[slotIndex] = 0;
                OnSlotCorrect?.Invoke();

                if (IsComplete())
                    OnBoardComplete?.Invoke();
            }
            else
            {
                _wrongCountPerSlot[slotIndex]++;
                targetSlot.FlashWrong();
                OnSlotWrong?.Invoke();

                if (_wrongCountPerSlot[slotIndex] >= _hintThreshold)
                    ShowHint(slotIndex);
            }

            return benar;
        }

        public bool IsComplete()
        {
            foreach (var slot in slots)
                if (!slot.IsOccupied) return false;
            return true;
        }

        private void ShowHint(int slotIndex)
        {
            // Visual hint: highlight the correct slot with a pulse
            // Implementasi detail bisa ditambah nanti via shader/glow
            Debug.Log($"[Hint] Slot {slotIndex} seharusnya: {_urutanYangBenar[slotIndex].namaHuruf}");
        }
    }
}
