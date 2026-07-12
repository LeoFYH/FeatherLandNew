using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 让当前对象下的所有 SpriteRenderer 始终充满主相机视野。
    /// 用于开蛋光效等需要全屏播放的序列帧动画。
    /// </summary>
    public class SpriteScreenFitter : MonoBehaviour
    {
        [Tooltip("图片的原始设计像素尺寸（例如 1920x1080）。如果填了，会按此尺寸计算而不是用 Sprite.bounds；如果填 0 则自动读取 Sprite.bounds。")]
        public Vector2 referencePixelSize;

        [Tooltip("适配模式：Stretch 直接拉伸铺满；Cover 保持比例覆盖整个屏幕（可能裁剪）；Contain 保持比例完整显示（可能有黑边）。")]
        public FitMode fitMode = FitMode.Stretch;

        public enum FitMode
        {
            Stretch,
            Cover,
            Contain
        }

        private Camera mainCamera;
        private Sprite[] lastSprites;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private float lastOrthoSize;

        private void OnEnable()
        {
            mainCamera = Camera.main;
            Fit();
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            bool needUpdate = false;
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (lastSprites == null || lastSprites.Length != renderers.Length)
            {
                lastSprites = new Sprite[renderers.Length];
                needUpdate = true;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sprite != lastSprites[i])
                {
                    lastSprites[i] = renderers[i].sprite;
                    needUpdate = true;
                }
            }

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                needUpdate = true;
            }

            if (!Mathf.Approximately(mainCamera.orthographicSize, lastOrthoSize))
            {
                lastOrthoSize = mainCamera.orthographicSize;
                needUpdate = true;
            }

            if (needUpdate)
                Fit();
        }

        private void Fit()
        {
            if (mainCamera == null) return;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return;

            // 让自身位于相机中心（保持 z）
            Vector3 camPos = mainCamera.transform.position;
            Vector3 pos = transform.position;
            pos.x = camPos.x;
            pos.y = camPos.y;
            transform.position = pos;

            // 计算相机视野的世界尺寸
            float screenHeight = mainCamera.orthographicSize * 2f;
            float screenWidth = screenHeight * mainCamera.aspect;

            // 父物体的世界缩放（必须考虑，否则 localScale 会被父物体缩放叠加）
            Vector3 parentWorldScale = transform.lossyScale;

            foreach (var sr in renderers)
            {
                if (sr == null || sr.sprite == null) continue;

                // 计算 Sprite 的世界尺寸
                Vector2 spriteSize;
                if (referencePixelSize.x > 0f && referencePixelSize.y > 0f)
                {
                    float ppu = sr.sprite.pixelsPerUnit > 0f ? sr.sprite.pixelsPerUnit : 100f;
                    spriteSize = referencePixelSize / ppu;
                }
                else
                {
                    spriteSize = sr.sprite.bounds.size;
                }

                if (spriteSize.x <= 0f || spriteSize.y <= 0f) continue;

                // 计算目标世界缩放
                float targetWorldScaleX = screenWidth / spriteSize.x;
                float targetWorldScaleY = screenHeight / spriteSize.y;

                if (fitMode == FitMode.Cover)
                {
                    float uniformScale = Mathf.Max(targetWorldScaleX, targetWorldScaleY);
                    targetWorldScaleX = uniformScale;
                    targetWorldScaleY = uniformScale;
                }
                else if (fitMode == FitMode.Contain)
                {
                    float uniformScale = Mathf.Min(targetWorldScaleX, targetWorldScaleY);
                    targetWorldScaleX = uniformScale;
                    targetWorldScaleY = uniformScale;
                }

                // 转换回 local scale（抵消父物体的世界缩放）
                Vector3 targetLocalScale = new Vector3(
                    targetWorldScaleX / parentWorldScale.x,
                    targetWorldScaleY / parentWorldScale.y,
                    1f);
                sr.transform.localScale = targetLocalScale;

                // 让子对象也居中于相机
                Vector3 childPos = sr.transform.position;
                childPos.x = camPos.x;
                childPos.y = camPos.y;
                sr.transform.position = childPos;

                Debug.Log(
                    $"[SpriteScreenFitter] {sr.name}: screen={screenWidth:F2}x{screenHeight:F2}, " +
                    $"spriteSize={spriteSize.x:F2}x{spriteSize.y:F2}, " +
                    $"targetWorldScale={targetWorldScaleX:F2},{targetWorldScaleY:F2}, " +
                    $"parentWorldScale={parentWorldScale.x:F4},{parentWorldScale.y:F4}, " +
                    $"localScale={targetLocalScale.x:F4},{targetLocalScale.y:F4}, " +
                    $"bounds={sr.sprite.bounds.size}, rect={sr.sprite.rect.size}, ppu={sr.sprite.pixelsPerUnit}",
                    this);
            }
        }
    }
}
