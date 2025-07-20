using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ClickAnim : ViewControllerBase, IPointerDownHandler
    {
        private Sequence anim;

        public void OnPointerDown(PointerEventData eventData)
        {
            anim?.Kill();
            transform.localScale = Vector3.one;
            anim = DOTween.Sequence();
            anim.Append(transform.DOScale(new Vector3(0.8f, 0.8f, 1f), 0.15f).SetEase(Ease.InSine));
            anim.Append(transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutSine));
        }
    }
}