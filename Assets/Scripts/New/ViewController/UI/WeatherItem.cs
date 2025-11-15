using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class WeatherItem : ViewControllerBase
    {
        public int index;

        public void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                this.SendCommand(new ChangeWeatherCommand(index));
            });
        }
    }
}