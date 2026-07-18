using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class ResetSpriteController : ViewControllerBase
    {
        public Sprite[] sps;
        public GameObject light;
        public SpriteRenderer sr;

        public void SetSp(int index)
        {
            if (sps == null || sps.Length == 0)
                return;
            if (sps.Length <= index)
            {
                sr.sprite = sps[^1];
                return;
            }

            sr.sprite = sps[index];
        }

        private void Start()
        {
            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                if(evt.index == 2)
                {
                    light.SetActive(true);
                }
                else
                {
                    light.SetActive(false);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}