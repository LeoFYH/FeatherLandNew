using System;
using BirdGame;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace BirdGame
{


    /// <summary>
    /// 天气管理器
    /// </summary>
    public class WeatherManager : ViewControllerBase
    {
        public SpriteRenderer background;
        public SpriteRenderer backgroundTrees;
        public SpriteRenderer backgroundGrass;
        public SpriteRenderer light;
        public SpriteRenderer foregroundTree;
        public SpriteRenderer ground;
        public SpriteRenderer groundCover1;
        public SpriteRenderer groundCover2;
        public SpriteRenderer foregroundGrass;
        public Material birdMaterial;
        public Weather[] weathers;
        public CanvasGroup uiGroup;
        public SpriteRenderer[] others;

        private int currentIndex = -1;

        private void Start()
        {
            if (uiGroup == null)
                uiGroup = GameObject.Find("UIRoot").GetComponent<CanvasGroup>();
            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                int index = currentIndex;
                index++;
                if (index >= weathers.Length)
                    index = 0;
                SwitchWeather(index);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            SwitchWeather(0);
        }

        // private void Update()
        // {
        //     // if (Input.GetKeyDown(KeyCode.Alpha0))
        //     // {
        //     //     
        //     //     SwitchWeather(0);
        //     // }
        //     // else if (Input.GetKeyDown(KeyCode.Alpha1))
        //     // {
        //     //     SwitchWeather(1);
        //     // }
        //     // else if (Input.GetKeyDown(KeyCode.Alpha2))
        //     // {
        //     //     SwitchWeather(2);
        //     // }
        //     // else if (Input.GetKeyDown(KeyCode.Alpha3))
        //     // {
        //     //     SwitchWeather(3);
        //     // }
        //     // else if (Input.GetKeyDown(KeyCode.Alpha4))
        //     // {
        //     //     SwitchWeather(4);
        //     // }
        // }

        private void SwitchWeather(int index)
        {
            if (currentIndex == index)
                return;

            if (currentIndex != -1)
            {
                var lastWeather = weathers[currentIndex];
                lastWeather.onWeatherExit?.Invoke();
            }
            
            this.GetModel<IGameModel>().WeatherIndex.Value = index;
            currentIndex = index;
            var weather = weathers[index];
            var anim0 = DOTween.Sequence();
            anim0.Append(background.DOColor(Color.black, 0.5f));
            anim0.AppendCallback(() =>
            {
                weather.onWeatherEnter?.Invoke();
                background.sprite = weather.background.sprite;
                background.transform.localScale = Vector3.one * weather.background.scale;
            });
            anim0.Append(background.DOColor(Color.white, 0.5f));

            var anim1 = DOTween.Sequence();
            anim1.Append(backgroundTrees.DOColor(Color.black, 0.5f));
            anim1.AppendCallback(() =>
            {
                backgroundTrees.sprite = weather.backgroundTree.sprite;
                backgroundTrees.transform.localScale = Vector3.one * weather.backgroundTree.scale;
            });
            anim1.Append(backgroundTrees.DOColor(Color.white, 0.5f));

            var anim2 = DOTween.Sequence();
            anim2.Append(backgroundGrass.DOColor(Color.black, 0.5f));
            anim2.AppendCallback(() =>
            {
                backgroundGrass.sprite = weather.backgroundGrass.sprite;
                backgroundGrass.transform.localScale = Vector3.one * weather.backgroundGrass.scale;
            });
            anim2.Append(backgroundGrass.DOColor(Color.white, 0.5f));

            var anim3 = DOTween.Sequence();
            anim3.Append(light.DOColor(Color.black, 0.5f));
            anim3.AppendCallback(() =>
            {
                light.sprite = weather.light.sprite;
                light.transform.localScale = Vector3.one * weather.light.scale;
            });
            anim3.Append(light.DOColor(Color.white, 0.5f));

            var anim4 = DOTween.Sequence();
            anim4.Append(foregroundTree.DOColor(Color.black, 0.5f));
            anim4.AppendCallback(() =>
            {
                foregroundTree.sprite = weather.foregroundTree.sprite;
                foregroundTree.transform.localScale = Vector3.one * weather.foregroundTree.scale;
            });
            anim4.Append(foregroundTree.DOColor(Color.white, 0.5f));



            var anim5 = DOTween.Sequence();
            anim5.Append(foregroundGrass.DOColor(Color.black, 0.5f));
            anim5.AppendCallback(() =>
            {
                foregroundGrass.sprite = weather.foregroundGrass.sprite;
                foregroundGrass.transform.localScale = Vector3.one * weather.foregroundGrass.scale;
            });
            anim5.Append(foregroundGrass.DOColor(Color.white, 0.5f));


            var anim6 = DOTween.Sequence();
            anim6.Append(birdMaterial.DOColor(Color.black, 0.5f));
            anim6.Append(birdMaterial.DOColor(weather.birdColor, 0.5f));

            var anim7 = DOTween.Sequence();
            anim7.Append(uiGroup.DOFade(0, 0.5f));
            anim7.Append(uiGroup.DOFade(1, 0.5f));

            var anim8 = DOTween.Sequence();
            anim8.Append(ground.DOColor(Color.black, 0.5f));
            anim8.AppendCallback(() =>
            {
                ground.sprite = weather.ground.sprite;
                ground.transform.localScale = Vector3.one * weather.ground.scale;
            });
            anim8.Append(ground.DOColor(Color.white, 0.5f));

            var anim9 = DOTween.Sequence();
            anim9.Append(groundCover1.DOColor(Color.black, 0.5f));
            anim9.AppendCallback(() =>
            {
                groundCover1.sprite = weather.foreCover1.sprite;
                groundCover1.transform.localScale = Vector3.one * weather.foreCover1.scale;
            });
            anim9.Append(groundCover1.DOColor(Color.white, 0.5f));

            var anim10 = DOTween.Sequence();
            anim10.Append(groundCover2.DOColor(Color.black, 0.5f));
            anim10.AppendCallback(() =>
            {
                groundCover2.sprite = weather.foreCover2.sprite;
                groundCover2.transform.localScale = Vector3.one * weather.foreCover2.scale;
            });
            anim10.Append(groundCover2.DOColor(Color.white, 0.5f));

            for (int i = 0; i < others.Length; i++)
            {
                var anim = DOTween.Sequence();
                anim.Append(others[i].DOColor(Color.black, 0.5f));
                anim.Append(others[i].DOColor(Color.white, 0.5f));
            }
        }
    }

    [Serializable]
    public class Weather
    {
        public string weatherName;
        public SpriteItem background;
        public SpriteItem backgroundTree;
        public SpriteItem backgroundGrass;
        public SpriteItem light;
        public SpriteItem foregroundTree;
        public SpriteItem ground;
        public SpriteItem foreCover1;
        public SpriteItem foreCover2;
        public SpriteItem foregroundGrass;
        public Color32 birdColor;

        [Serializable]
        public class WeatherEvent : UnityEvent
        {
        }

        public WeatherEvent onWeatherEnter;
        public WeatherEvent onWeatherExit;
    }

    [Serializable]
    public class SpriteItem
    {
        [PreviewField]
        public Sprite sprite;
        public float scale = 1f;
    }
}