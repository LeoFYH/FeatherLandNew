using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class BagChecker : ViewControllerBase
    {
        private void OnMouseDown()
        {
            this.GetSystem<IUISystem>().SendEvent<ShowBranchEvent>();
        }
    }
}