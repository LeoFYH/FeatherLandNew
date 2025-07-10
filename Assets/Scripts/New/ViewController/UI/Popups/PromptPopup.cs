using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using QFramework;

namespace BirdGame
{
    public class PromptPopup : UIBase
    {
        public Text descTxt;
        public CanvasGroup cg;

        public void Init(string s)
        {
            descTxt.text = s;
            transform.DOLocalMoveY(0, 1).OnComplete(delegate
            {
                cg.DOFade(0, 1);
                transform.DOLocalMoveY(150, 1).OnComplete(delegate
                {
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.PromptPopup);
                });
            });
        }
    }
}