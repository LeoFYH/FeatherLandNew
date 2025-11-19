using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class EggInfo : ViewControllerBase
    {
        public RectTransform rect;
        public LocalizationText text;

        private RectTransform thisRect;
        private bool isActive = true;

        private void Start()
        {
            thisRect = GetComponent<RectTransform>();
            int index = this.GetModel<IGameModel>().ShopEggSelectIndex.Value;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            string key = this.GetModel<IConfigModel>().ShopConfig?.sceneEggs[mapIndex]?.eggs[index].description;
            if (string.IsNullOrEmpty(key))
            {
                Debug.Log("空");
                return;
            }
            text.SetKey(key);
            SetPos();
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }

        // private void Update()
        // {
        //     if (isActive)
        //     {
        //         SetPos();
        //     }
        // }
        
        private void SetPos()
        {
            Vector2 pos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, Input.mousePosition, null, out pos))
            {
                // 考虑Canvas的pivot偏移
                Vector2 canvasSize = thisRect.rect.size;
                Vector2 canvasPivot = thisRect.pivot;
                
                // 调整坐标到Canvas中心为原点
                Vector2 adjustedPosition = new Vector2(
                    pos.x + canvasSize.x * (0.5f - canvasPivot.x),
                    pos.y + canvasSize.y * (0.5f - canvasPivot.y)
                );

                rect.anchoredPosition = adjustedPosition;
            }
        }
    }
}