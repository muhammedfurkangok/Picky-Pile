using System.Collections.Generic;
using DG.Tweening;
using Runtime.Entities;
using Runtime.Enums;
using Runtime.Extensions;
using UnityEngine;

namespace Runtime.Managers
{
    public class UtilityManager : SingletonMonoBehaviour<UtilityManager>
    {

        private Dictionary<UtilityType, bool> utilityStatus = new Dictionary<UtilityType, bool>
        {
            { UtilityType.Bomb, true },
            { UtilityType.Unlock, true },
            // { UtilityType.Shuffle, true }
        };
        public bool isAnyUtilityActive = false;
        public int bombCount = 3;
        public int unlockCount = 3;


        public void UseUtility(UtilityType utilityType)
        { 
            isAnyUtilityActive = true;
            InputManager.Instance.SetUtilityActive(utilityType);
        }
        
        public void ApplyUtilityToObject(UtilityType utilityType, Item item)
        {
            switch (utilityType)
            {
                case UtilityType.Bomb:
                    Bomb(item);
                    break;
                case UtilityType.Unlock:
                    Unlock(item);
                    break;
                // case UtilityType.Shuffle:
                //     Shuffle();
                //     break;
            }
        }

        public void Bomb(Item item )
        {
            bombCount--;
            UIManager.Instance.UpdateBombCountText();
            SoundManager.Instance.PlaySound(GameSoundType.Bomb);
            item.transform.DOPunchScale(transform.localScale + Vector3.one * .5f, 0.25f).SetEase(Ease.Linear);
            UIManager.Instance.utilityCanvas.gameObject.SetActive(false);
            isAnyUtilityActive = false;
        }

        public void Unlock(Item item)
        {
            unlockCount--; 
            UIManager.Instance.UpdateUnlockCountText();
          item.SetCollider(false);
          SoundManager.Instance.PlaySound(GameSoundType.Unlock);
          item.transform.DOJump( item.transform.position + Vector3.up, 0.5f, 1, 0.25f).SetEase(Ease.Linear).OnComplete( () =>
          {
              item.SetRigidBody(false);
              item.gameObject.SetActive(false);
              item.OnClick();
          });
          UIManager.Instance.utilityCanvas.gameObject.SetActive(false);
            isAnyUtilityActive = false;
        }

        // public void Shuffle()
        // {
        //     Debug.Log("Shuffle utility used!");
        // }
    }
}
