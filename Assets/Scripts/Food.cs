using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Food : MonoBehaviour
{
    public bool isTargeted = false;
    public int hp = 1;
    float y;
    private SpriteRenderer spriteRenderer;
    private float fadeDuration = 4f;  // 总淡出时间
    private float fadeTimer = 0f;     // 淡出计时器

    void Start()
    {
        y = transform.position.y;
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(DelayedStart());
        StartCoroutine(nameof(DestroyDelay));
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        transform.DOMoveY(y - 0.2f, 0.2f).SetEase(Ease.OutQuad);
    }

    private IEnumerator DestroyDelay()
    {
        var frame = new WaitForFixedUpdate();
        fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            if (isTargeted)
            {
                // 如果被目标选中，恢复完全不透明
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                }
                yield break;
            }

            fadeTimer += Time.deltaTime;
            // 计算当前透明度（从1逐渐变为0）
            float alpha = 1f - (fadeTimer / fadeDuration);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
            }
            yield return frame;
        }

        // 完全透明后销毁
        GameManager.Instance.RecycleFood(this);
    }
}
