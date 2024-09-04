using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Runtime.Entities;
using Runtime.Extensions;
using Runtime.Managers;

public class SlotManager : SingletonMonoBehaviour<SlotManager> {
    public List<Slot> slots;


    public void Place(ItemRef itemRef) {
        var hasAvailable = HasAvailableSlot();

        if (!hasAvailable) {
            return;
        }

        var slotIndex = FindSlotByKey(itemRef.key);

        if (slotIndex + 1 >= slots.Count) {
            return;
        }
        
        if (slotIndex < 0)
        {
            var slot = GetAvailableSlot();
            if (slot == null)
                return;

            slot.Place(itemRef);
        }
        else
        {
            var slot = slots[slotIndex + 1];
            if (slot == null)
                return;

            ShiftSlots(slotIndex + 1);
            slot.Place(itemRef);
            DOVirtual.DelayedCall(.2f, () => {
                CheckBlocks();
            });
        }

        DOVirtual.DelayedCall(.75f, () => {
            if(GetEmptySlotCount() <= 0) {
                // EventManager.TriggerEvent("OnOutOfSpace");
            }
        });
    }

    public Slot GetAvailableSlot() {
        foreach (var slot in slots) {
            if (slot.Unlocked && slot.IsAvailable()) {
                return slot;
            }
        }

        return null;
    }

    public bool HasAvailableSlot()
    { 
        return slots[slots.Count - 1].active == null;
    } 
        
   

    public int FindSlotByKey(string key) {
        return slots.FindLastIndex(s => s.active != null && s.active.key == key);
    }

    public void ShiftSlots(int index) {
        if (!HasAvailableSlot())
            return;

        for (int i = slots.Count - 1; i > index; i--) {
            slots[i].Place(slots[i - 1].active, false);
        }

        slots[index].active = null;
    }

    public bool IsAllSlotsFilled() {
        foreach (var slot in slots) {
            if (slot.Unlocked && !slot.IsAvailable()) {
                return false;
            }
        }

        return true;
    }

    public void ClearAllSlots() {
        foreach (var slot in slots) {
            slot.Clear();
        }
    }

    public void CheckBlocks()
    {
        for (int i = 2; i < slots.Count; i++)
        {
            var success = CheckBlocks(i);

            if (success)
            {
                i += 3;
            }
        }
    }

    public bool CheckBlocks(int index)
    {
        if (index < 2)
            return false;
 
        if (slots[index].active == null || slots[index - 1].active == null || slots[index - 2].active == null)
            return false;

        var cube1 = slots[index].active;
        var cube2 = slots[index - 1].active;
        var cube3 = slots[index - 2].active;

        if (cube1.key == cube2.key && cube2.key == cube3.key)
        {
             slots[index].AnimateStar(.5f, Sort);
             slots[index - 1].AnimateStar(.25f);
             slots[index - 2].AnimateStar(.0f);
            
            // audioManager.PlayAudio(audioManager.successSound);


            if(ItemManager.Instance.ActiveCubeCount() <= 0) 
            {
                DOVirtual.DelayedCall(.5f, () => {
                    // EventManager.TriggerEvent("OnGameFinished");
                });
            }

            return true;
        }

        return false;
    }

    public void Sort()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].active == null)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[j].active != null)
                    {
                        slots[i].Place(slots[j].active, false);
                        slots[j].active = null;
                        break;
                    }
                }
            }
        }
    }

    public ItemRef GetLastCube()
    {
        ItemRef lastSlotCube = null;
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].active != null)
            {
                lastSlotCube = slots[i].active;
                slots[i].active = null;
                break;
            }
        }
        return lastSlotCube;        
    }

    [Button]
    public int GetEmptySlotCount()
    {
        var emptySlotCount = 0;
        foreach (var slot in slots)
        {
            if (slot.active == null)
            {
                emptySlotCount++;
            }
        }
        return emptySlotCount;
    }
    
    [Button]
    public Dictionary<string, int> GetCubeKeys()
    {
        Dictionary<string, int> keys = new Dictionary<string, int>();
        
        for (int i = 0; i < slots.Count; i++)
        {
            var key = "";
            if(slots[i].active != null)
            {
                key = slots[i].active.key;
                if (keys.ContainsKey(key))
                {
                    keys[key]++;
                }
                else
                {
                    keys[key] = 1;
                }
            }
        }

        return keys;     
    }
}