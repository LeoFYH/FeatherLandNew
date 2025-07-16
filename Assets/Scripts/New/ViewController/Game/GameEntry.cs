using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class GameEntry : ViewControllerBase
    {
        private void Start()
        {
            this.GetSystem<ISceneSystem>().LoadScene(0);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Click);
            }
        }
    }
}