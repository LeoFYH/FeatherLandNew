using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface ICursorSystem : ISystem
    {
        bool IsPlayingAnim();
        void SetCursorState(CursorState state);
        void Feed();
        //void Stroke();
    }

    public class CursorSystem : AbstractSystem, ICursorSystem
    {
        private Dictionary<CursorState, CursorItem> cursorItems = new Dictionary<CursorState, CursorItem>();
        private Sequence feedAnim;
        private Sequence strokeAnim;
        private bool isPlayingFeed;

        private CursorState currentState;
        
        protected override void OnInit()
        {
            this.GetSystem<IAssetSystem>().LoadAssetAsync<CursorConfig>("CursorConfig", config =>
            {
                foreach (var item in config.mouseStates)
                {
                    if (!cursorItems.ContainsKey(item.state))
                    {
                        cursorItems.Add(item.state, item);
                    }
                }
                SetCursorState(CursorState.Default);
            });
        }

        public bool IsPlayingAnim()
        {
            return isPlayingFeed;
        }

        public void SetCursorState(CursorState state)
        {
            if(currentState == state)
                return;

            currentState = state;
            feedAnim?.Kill(true);
            strokeAnim?.Kill(true);
            feedAnim = null;
            strokeAnim = null;
            var item = cursorItems[state];
            Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
        }

        public void Feed()
        {
            feedAnim?.Kill();
            strokeAnim?.Kill();
            isPlayingFeed = true;
            feedAnim = null;
            strokeAnim = null;
            var item = cursorItems[CursorState.Feed1];
            Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
            currentState = CursorState.Feed1;
            feedAnim = DOTween.Sequence();
            feedAnim.AppendInterval(0.2f);
            feedAnim.AppendCallback(() =>
            {
                item = cursorItems[CursorState.Feed2];
                currentState = CursorState.Feed2;
                Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
            });
            feedAnim.AppendInterval(0.2f);
            feedAnim.AppendCallback(() =>
            {
                item = cursorItems[CursorState.Feed1];
                currentState = CursorState.Feed1;
                Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
                feedAnim = null;
            });
            feedAnim.OnComplete(() =>
            {
                isPlayingFeed = false;
            });
        }

        // public void Stroke()
        // {
        //     feedAnim?.Kill(true);
        //     strokeAnim?.Kill(true);
        //     isPlayingStroke = true;
        //     var item = cursorItems[CursorState.Stroke1];
        //     Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
        //     currentState = CursorState.Stroke1;
        //     strokeAnim = DOTween.Sequence();
        //     strokeAnim.AppendInterval(0.2f);
        //     strokeAnim.AppendCallback(() =>
        //     {
        //         item = cursorItems[CursorState.Stroke2];
        //         Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
        //         currentState = CursorState.Stroke2;
        //     });
        //     strokeAnim.AppendInterval(0.2f);
        //     strokeAnim.AppendCallback(() =>
        //     {
        //         item = cursorItems[CursorState.Stroke1];
        //         Cursor.SetCursor(item.cursorTexture, item.hotspot, CursorMode.Auto);
        //         currentState = CursorState.Stroke1;
        //         strokeAnim = null;
        //     });
        //     strokeAnim.AppendInterval(1f);
        //     strokeAnim.OnComplete(() =>
        //     {
        //         isPlayingStroke = false;
        //     });
        // }
    }
} 