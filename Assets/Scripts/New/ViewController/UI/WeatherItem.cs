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
            GetComponent<Toggle>().onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    this.SendCommand(new ChangeWeatherCommand(index));
            });
        }
    }
}