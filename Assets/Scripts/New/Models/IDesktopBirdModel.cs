using System.Collections.Generic;
using QFramework;

namespace BirdGame
{
    public interface IDesktopBirdModel : IModel
    {
        List<DesktopBird> DesktopBirds { get; }
    }

    public class DesktopBirdModel : AbstractModel, IDesktopBirdModel
    {
        protected override void OnInit()
        {
            
        }

        public List<DesktopBird> DesktopBirds { get; } = new List<DesktopBird>();
    }

    public struct DesktopBird
    {
        public int birdType;
        public bool isGrowUp;
    }
}