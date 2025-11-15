using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class ViewControllerBase : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}