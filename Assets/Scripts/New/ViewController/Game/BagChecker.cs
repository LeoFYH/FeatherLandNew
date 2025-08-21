using System;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class BagChecker : ViewControllerBase
    {
        public Sprite openBag;
        public Sprite closeBag;
        public Transform menu;

        private SpriteRenderer sp;

        private bool isOpen = false;
        private Tweener anim;
        
        private void Start()
        {
            sp = GetComponent<SpriteRenderer>();
            sp.sprite = closeBag;
            menu.localScale = Vector3.zero;
            OpenBag();
        }

        private void OpenBag()
        {
            anim?.Kill();
            sp.sprite = openBag;
            anim = menu.DOScale(1, 0.3f);
            isOpen = true;
        }

        private void CloseBag()
        {
            anim?.Kill();
            anim = menu.DOScale(0, 0.3f).OnComplete(() =>
            {
                sp.sprite = closeBag;
            });
            isOpen = false;
        }

        public void OnClick()
        {
            if (isOpen)
            {
                CloseBag();
            }
            else
            {
                OpenBag();
            }
            //this.GetSystem<IUISystem>().SendEvent<ShowBranchEvent>();
        }
    }
}