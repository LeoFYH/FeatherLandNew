using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface ICursorSystem : ISystem
    {
        void SetCustomCursor(Texture2D cursorTexture, Vector2 hotSpot);
        void SetCustomCursor(Sprite cursorSprite, Vector2 hotSpot);
        void SetDefaultCursor();
        void SetCursorType(CursorType type);
    }

    public enum CursorType
    {
        Default,
        Hand,
        Point,
        Custom
    }

    public class CursorSystem : AbstractSystem, ICursorSystem
    {
        private Texture2D defaultCursor;
        private Vector2 defaultHotSpot = Vector2.zero;
        
        protected override void OnInit()
        {
            // Unity的Cursor类没有GetCursor方法，我们直接使用null作为默认值
            defaultCursor = null;
        }

        public void SetCustomCursor(Texture2D cursorTexture, Vector2 hotSpot)
        {
            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            }
        }

        public void SetCustomCursor(Sprite cursorSprite, Vector2 hotSpot)
        {
            if (cursorSprite != null)
            {
                Cursor.SetCursor(cursorSprite.texture, hotSpot, CursorMode.Auto);
            }
        }

        public void SetDefaultCursor()
        {
            // 恢复Unity默认鼠标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void SetCursorType(CursorType type)
        {
            switch (type)
            {
                case CursorType.Default:
                    SetDefaultCursor();
                    break;
                case CursorType.Hand:
                    // 可以在这里设置手型光标
                    break;
                case CursorType.Point:
                    // 可以在这里设置指针光标
                    break;
            }
        }
    }
} 