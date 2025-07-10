using QFramework;

namespace BirdGame
{
    public class GameEntry : ViewControllerBase
    {
        private void Start()
        {
            this.GetSystem<ISceneSystem>().LoadScene(0);
        }
    }
}