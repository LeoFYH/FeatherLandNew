using System;
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
        void RefreshCursor();
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

#if UNITY_STANDALONE_OSX
        private const float MacCursorScale = 0.65f;
#endif
        
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

        private void SetCustomCursor(Texture2D texture, Vector2 hotspot)
        {
#if UNITY_STANDALONE_OSX
            Vector2 scaledHotspot = hotspot * MacCursorScale;
            Cursor.SetCursor(texture, scaledHotspot, CursorMode.ForceSoftware);
#else
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
#endif
        }

        public void SetCursorState(CursorState state)
        {
            if(currentState == state && !isPlayingFeed)
                return;

            feedAnim?.Kill(false);
            strokeAnim?.Kill(false);
            isPlayingFeed = false;
            feedAnim = null;
            strokeAnim = null;
            currentState = state;
            var item = cursorItems[state];
            try
            {
                if (this.GetModel<ISaveModel>().AccountData.sceneTools.Count == 0)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools.Add(new SceneToolInfo());
                }

                while (this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Count <= 6)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Add(new ToolInfo());
                }

                int index = this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[6].equipedId;
                SetCustomCursor(item.cursorTextures[index], item.hotspot);
            }
            catch (Exception e)
            {

            }
        }

        public void RefreshCursor()
        {
            var item = cursorItems[currentState];
            try
            {
                if (this.GetModel<ISaveModel>().AccountData.sceneTools.Count == 0)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools.Add(new SceneToolInfo());
                }

                while (this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Count <= 6)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Add(new ToolInfo());
                }

                int index = this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[6].equipedId;
                SetCustomCursor(item.cursorTextures[index], item.hotspot);
            }
            catch (Exception e)
            {

            }
        }

        public void Feed()
        {
            feedAnim?.Kill();
            strokeAnim?.Kill();
            isPlayingFeed = true;
            feedAnim = null;
            strokeAnim = null;
            var item = cursorItems[CursorState.Feed1];
            try
            {
                if (this.GetModel<ISaveModel>().AccountData.sceneTools.Count == 0)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools.Add(new SceneToolInfo());
                }
                while (this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Count <= 6)
                {
                    this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Add(new ToolInfo());
                }
                int index =this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[6].equipedId;
                SetCustomCursor(item.cursorTextures[index], item.hotspot);
            }
            catch (Exception e)
            {
                
            }
            
            currentState = CursorState.Feed1;
            feedAnim = DOTween.Sequence();
            feedAnim.AppendInterval(0.2f);
            feedAnim.AppendCallback(() =>
            {
                item = cursorItems[CursorState.Feed2];
                currentState = CursorState.Feed2;
                try
                {
                    if (this.GetModel<ISaveModel>().AccountData.sceneTools.Count == 0)
                    {
                        this.GetModel<ISaveModel>().AccountData.sceneTools.Add(new SceneToolInfo());
                    }
                    while (this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Count <= 6)
                    {
                        this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Add(new ToolInfo());
                    }
                    int index =this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[6].equipedId;
                    SetCustomCursor(item.cursorTextures[index], item.hotspot);
                }
                catch (Exception e)
                {
                    
                }
                
            });
            feedAnim.AppendInterval(0.2f);
            feedAnim.AppendCallback(() =>
            {
                item = cursorItems[CursorState.Feed1];
                currentState = CursorState.Feed1;
                try
                {
                    if (this.GetModel<ISaveModel>().AccountData.sceneTools.Count == 0)
                    {
                        this.GetModel<ISaveModel>().AccountData.sceneTools.Add(new SceneToolInfo());
                    }
                    while (this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Count <= 6)
                    {
                        this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools.Add(new ToolInfo());
                    }
                    int index =this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[6].equipedId;
                    SetCustomCursor(item.cursorTextures[index], item.hotspot);
                }
                catch (Exception e)
                {
                }
                
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
