using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class LoadingPanel : UIBase
    {
        public Image fill;
        public RectTransform iconRect;
        public TextMeshProUGUI loadingText;
        
        public override void OnShowPanel()
        {
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
        }

        private void Start()
        {
            var loadingModel = this.GetModel<ILoadingModel>();
            float with = fill.GetComponent<RectTransform>().sizeDelta.x;

            loadingModel.LoadingText.Register(v =>
            {
                loadingText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            loadingModel.Progress.Register(v =>
            {
                fill.fillAmount = v;
                iconRect.anchoredPosition = new Vector2(with * v, -3f);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            loadingText.text = loadingModel.LoadingText.Value;
            fill.fillAmount = loadingModel.Progress.Value;
        }
    }
}