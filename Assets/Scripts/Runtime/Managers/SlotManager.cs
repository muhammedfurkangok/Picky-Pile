using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Runtime.Entities;
using Runtime.Enums;
using Runtime.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Managers
{
    public class SlotManager : SingletonMonoBehaviour<SlotManager> 
    {
        
        public List<Slot> slots;
        public Item item;


        public void Place(ItemRef itemRef)
        {
            
            var hasAvailable = HasAvailableSlot();

            if (!hasAvailable) {
                return;
            }

            var slotIndex = FindSlotByKey(itemRef.key);

            if (slotIndex + 1 >= slots.Count) {
                return;
            }

            
            itemRef.transform.SetLayerRecursive(LayerMask.NameToLayer("Slot"));
            
        
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
                
                DOVirtual.DelayedCall(.45f, () => {
                    CheckBlocks();
                });
            }
            DOVirtual.DelayedCall(.46f, () => {
                if(GetEmptySlotCount() <= 0) {
                    GameManager.Instance.SetGameStateLevelFail();
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

        public  void CheckBlocks()
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
                
                slots[index].ScoreAnim(slots[index - 1].transform.position);    
                slots[index - 1].ScoreAnim(slots[index - 1].transform.position);
                slots[index - 2].ScoreAnim(slots[index - 1].transform.position,Sort);
                
                ItemManager.Instance.glissandoCounterList.Clear();
                SoundManager.Instance.StopGlissando();
                SoundManager.Instance.PlaySound(GameSoundType.Merge);
                
                DOVirtual.DelayedCall(0.45f, () => {
                    var obj = ItemManager.Instance.InstantiateBlockByGivenKey(cube2.key, index - 1);
                    ItemManager.Instance.AddItemToList(obj.GetComponent<Item>());
                    
                    ChangeInstantiatedItemColor(obj, cube2);

                    slots[index - 1].PlayParticle();
                    var objRb = obj.GetComponent<Rigidbody>();
                    objRb.isKinematic = true;
                    obj.transform.SetLayerRecursive(LayerMask.NameToLayer("Merge"));
                    DOVirtual.DelayedCall( 0.15f, () => {
                        objRb.isKinematic = false;
                        
                        Vector3[] torqueOptions = new Vector3[] {
                            new Vector3(0.5f, 0, 0),    
                            new Vector3(-0.5f, 0, 0),  
                            new Vector3(0, 0.5f, 0),    
                            new Vector3(0, -0.5f, 0),   
                            new Vector3(0, 0, 0.5f),    
                            new Vector3(0, 0, -0.5f)   
                        };
                        var randomTorque = torqueOptions[Random.Range(0, torqueOptions.Length)];

                        objRb.AddTorque(randomTorque, ForceMode.Impulse);

                    });
                    DOVirtual.DelayedCall( 0.9f, () => {
                        obj.transform.SetLayerRecursive(LayerMask.NameToLayer("Item"));
                    });
                });
               
                if(ItemManager.Instance.ActiveCubeCount() <= 1 && GetEmptySlotCount() == 7 )
                {
                    DOVirtual.DelayedCall(.5f, () => {
                        GameManager.Instance.SetGameStateLevelComplete();
                    });
                }

                return true;
            }

            return false;
        }

        private static void ChangeInstantiatedItemColor(GameObject obj, ItemRef cube2)
        {
            var objRenderer = obj.GetComponentInChildren<Renderer>();
            var objItemScript = obj.GetComponent<Item>();
            var objMaterial = ItemManager.Instance.GetMaterialByKey(objItemScript.key);
            objItemScript.enabled = false;
            objRenderer.material = ItemManager.Instance.GetMaterialByKey(cube2.key);
                    
            objRenderer.material.DOColor(objMaterial.color, 1f).OnComplete( () => 
            {
                objRenderer.material = objMaterial;
                objItemScript.enabled = true;
            });
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
        
         
        public int GetSlotIndex(Slot slot)
        {
            var index = slots.IndexOf(slot);
            Debug.Log(index);
            return index;
        }
        
        public void IsAnySlotAnimating()
        {
            foreach (var slot in slots)
            {
                if (slot.IsAvailable())
                {
                    Debug.Log("Slot is animating");
                    return;
                }
            }
            Debug.Log("Slot is not animating");
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
}