namespace BirdGame
{
    public class AddedWalkableController : ViewControllerBase
    {
        public int notWalkableIndex = 0;

        private void Start()
        {
            NavigationManager.Instance?.EnableAddedArea(notWalkableIndex);
        }

        private void OnDestroy()
        {
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance?.DisableAddedArea(notWalkableIndex);
            }
        }
    }
}