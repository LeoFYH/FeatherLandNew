using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BirdGame
{
    public class CursorConfig : ScriptableObject
    {
        [ListDrawerSettings(Expanded = true)]
        public List<CursorItem> mouseStates;
    }

    [Serializable]
    public class CursorItem
    {
        [HorizontalGroup("Split", Width = 150)]
        [PreviewField(150, ObjectFieldAlignment.Center), HideLabel]
        public Texture2D cursorTexture;

        [VerticalGroup("Split/Settings")]
        [PropertySpace(5)]
        public CursorState state;

        [OnValueChanged(nameof(UpdatePreview))]
        [LabelText("Hotspot")]
        public Vector2 hotspot;

        [HideInInspector]
        public Rect previewRect;

        [HideInInspector]
        public bool previewInitialized;

        public void UpdatePreview() { }
    }

    public enum CursorState
    {
        Default,
        Click,
        Feed1,
        Feed2,
        Stroke1,
        Stroke2
    }
}