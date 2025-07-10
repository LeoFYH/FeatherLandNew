using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class Egg : ViewControllerBase
    {
        public Sprite[] eggSprites; // 蛋动画的每一帧图片
        public SpriteRenderer spriteRenderer;
        private int currentFrame = 0; // 当前显示的帧索引
        private Tweener anim;

        private void Start()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer not found on the Egg object!");
            }
        }

        private void OnMouseDown()
        {
            anim?.Kill();
            anim = spriteRenderer.transform.DOShakeScale(0.2f, 0.5f, 50, 180f);
            //spriteRenderer.transform.DOShakePosition(0.2f, 0.5f);
            PlayNextFrame();

            if (currentFrame >= eggSprites.Length)
            {
                SpawnBird();
            }
        }

        private void PlayNextFrame()
        {
            if (currentFrame < eggSprites.Length)
            {
                spriteRenderer.sprite = eggSprites[currentFrame];
                currentFrame++;
            }
        }

        private void OnDestroy()
        {
            anim?.Kill();
        }

        private void SpawnBird()
        {
            this.SendCommand<SpawnBirdCommand>();
            // 销毁当前蛋对象
            Destroy(gameObject);
        }
    }
}