using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class ResetSpriteController : ViewControllerBase
    {
        public Sprite[] sps;
        public GameObject[] anims;
        public SpriteRenderer sr;

        private int id;

        public void SetSp(int index)
        {
            id = index;
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
                    sr.enabled = false;
                    anims[id].SetActive(true);
                }
                else
                {
                    sr.enabled = true;
                    anims[id].SetActive(false);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}