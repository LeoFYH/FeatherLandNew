using System;

namespace BirdGame
{
    public class NotwalkableController : ViewControllerBase
    {
        public int notWalkableIndex = 0;

        private void Start()
        {
            NavigationManager.Instance?.EnalbeNotWalk(notWalkableIndex);
        }

        private void OnDestroy()
        {
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance?.DisableNotWalk(notWalkableIndex);
            }
        }
    }
}