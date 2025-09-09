using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class DecorationInfoPopup : ViewControllerBase
    {
        public RectTransform rect;
        public Image icon;

        private RectTransform thisRect;
        
        public void Init(int index)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var config = this.GetModel<IConfigModel>().ShopConfig;
            icon.sprite = config.sceneDecorations[mapIndex].decorations[index].preview;
        }

        private void Start()
        {
            thisRect = transform as RectTransform;
            SetPos();
        }

        private void Update()
        {
            SetPos();
        }

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