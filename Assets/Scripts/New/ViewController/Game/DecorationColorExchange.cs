using System;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationColorExchange : ViewControllerBase
    {
        public Color32[] colors;
        public SpriteRenderer sr;
        private int index = 0;

        private void Start()
        {
            var anim = DOTween.Sequence();
            anim.Append(sr.DOColor(Color.black, 0.5f));
            anim.Append(sr.DOColor(colors[0], 0.5f));

            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                if(index == evt.index)
                    return;
                index = evt.index;
                var anim1 = DOTween.Sequence();
                anim1.Append(sr.DOColor(Color.black, 0.5f));
                anim1.Append(sr.DOColor(colors[evt.index], 0.5f));
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

    }
}