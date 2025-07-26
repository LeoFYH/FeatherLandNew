using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class Egg : ViewControllerBase
    {
        [ShowInInspector, ReadOnly]
        public int EggItemIndex { get; private set; }

        public Sprite[] eggSprites; // 蛋动画的每一帧图片
        public SpriteRenderer spriteRenderer;
        private int currentFrame = 0; // 当前显示的帧索引
        private Tweener anim;

        public void SetEggIndex(int index)
        {
            EggItemIndex = index;
        }

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
            this.SendCommand(new SpawnBirdCommand(EggItemIndex));
            // 销毁当前蛋对象
            Destroy(gameObject);
        }
    }
}