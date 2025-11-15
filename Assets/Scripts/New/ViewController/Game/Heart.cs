using UnityEngine;

namespace BirdGame
{
    public class Heart : ViewControllerBase
    {
        public void OnHide()
        {
            GameObject.Destroy(gameObject);
        }
    }
}