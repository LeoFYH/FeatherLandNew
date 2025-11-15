using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    [CustomEditor(typeof(CursorConfig))]
    public class CursorConfigEditor : OdinEditor
    {
        private Vector2 dragOffset;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var config = (CursorConfig)target;

            foreach (var state in config.mouseStates)
            {
                if (state.cursorTexture == null)
                    continue;

                GUILayout.Space(10);
                GUILayout.Label($"Hotspot Editor: {state.state.ToString()}", EditorStyles.boldLabel);

                float previewSize = 200f;
                Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
                GUI.Box(previewRect, GUIContent.none);

                // Draw texture
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawPreviewTexture(previewRect, state.cursorTexture, null, ScaleMode.ScaleToFit);
                }

                // Convert image rect to texture space
                Vector2 texSize = new Vector2(state.cursorTexture.width, state.cursorTexture.height);
                Rect imageArea = GetFittedRect(texSize, previewRect);

                // Draw hotspot red dot
                Vector2 hotspotPixel = new Vector2(
                    Mathf.Lerp(imageArea.x, imageArea.xMax, state.hotspot.x / texSize.x),
                    Mathf.Lerp(imageArea.y, imageArea.yMax, 1 - state.hotspot.y / texSize.y)
                );

                EditorGUI.DrawRect(new Rect(hotspotPixel.x - 3, hotspotPixel.y - 3, 6, 6), Color.red);

                // Handle drag
                EditorGUIUtility.AddCursorRect(imageArea, MouseCursor.Pan);
                HandleHotspotDrag(state, imageArea, texSize);
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private Rect GetFittedRect(Vector2 imageSize, Rect container)
        {
            float aspect = imageSize.x / imageSize.y;
            float containerAspect = container.width / container.height;

            if (aspect > containerAspect)
            {
                float height = container.width / aspect;
                return new Rect(container.x, container.y + (container.height - height) / 2, container.width, height);
            }
            else
            {
                float width = container.height * aspect;
                return new Rect(container.x + (container.width - width) / 2, container.y, width, container.height);
            }
        }

        private void HandleHotspotDrag(CursorItem state, Rect imageArea, Vector2 texSize)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && imageArea.Contains(e.mousePosition))
            {
                GUI.FocusControl(null);
                e.Use();
            }

            if (e.type == EventType.MouseDrag && imageArea.Contains(e.mousePosition))
            {
                Vector2 mousePos = e.mousePosition;
                float x = Mathf.Clamp01((mousePos.x - imageArea.x) / imageArea.width);
                float y = Mathf.Clamp01((mousePos.y - imageArea.y) / imageArea.height);

                state.hotspot = new Vector2(x * texSize.x, (1 - y) * texSize.y);
                GUI.changed = true;
                e.Use();
            }
        }
    }
}