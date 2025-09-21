using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DesktopViewController : ViewControllerBase
    {
        public GameObject exitPop;

        private void Start()
        {
            if (exitPop.activeSelf)
                exitPop.SetActive(false);
            
            this.GetSystem<IDesktopSystem>().SetClickThrough(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                exitPop.SetActive(!exitPop.activeSelf);
                this.GetSystem<IDesktopSystem>().SetClickThrough(!exitPop.activeSelf);
            }
        }
    }
}