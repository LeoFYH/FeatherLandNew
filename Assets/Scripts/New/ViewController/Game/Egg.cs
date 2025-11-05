using System.Collections;
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
        public int BirdIndex { get; private set; }

        public Sprite[] eggSprites; // 蛋动画的每一帧图片
        public SpriteRenderer spriteRenderer;
        public GameObject effect1;
        public GameObject effect2;
        private int currentFrame = 0; // 当前显示的帧索引
        private Tweener anim;
        private Tweener floatAnim; // 浮动动画
        private int clickCount = 0;
        private bool isSetBird;
        private int eggId;

        public void SetEggIndex(int index, int id)
        {
            EggItemIndex = index;
            eggId = id;
            isSetBird = false;
        }

        public void SetBirdIndex(int index)
        {
            BirdIndex = index;
            eggId = -1;
            isSetBird = true;
        }

        /// <summary>
        /// 开始缓慢的上下浮动动画
        /// </summary>
        private void StartFloatingAnimation()
        {
            // 停止之前的浮动动画
            floatAnim?.Kill();
            
            // 为每个蛋创建轻微不同的动画参数
            float baseDuration = 2f;
            float durationVariation = Random.Range(-0.3f, 0.3f); // 轻微变化动画时长
            float finalDuration = baseDuration + durationVariation;
            
            // 根据蛋的索引创建轻微的相位差
            float phaseOffset = EggItemIndex * 0.3f; // 每个蛋偏移0.3秒的相位
            
            // 创建缓慢的上下浮动动画
            floatAnim = transform.DOMoveY(transform.position.y + 0.3f, finalDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo) // 无限循环，来回摆动
                .SetDelay(phaseOffset); // 设置轻微的延迟
        }

        private void Start()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer not found on the Egg object!");
            }
            effect1.SetActive(true);
            effect2.SetActive(false);

            // 开始缓慢的上下浮动动画
            StartFloatingAnimation();

            this.RegisterEvent<HideEggEvent>(evt =>
            {
                gameObject.SetActive(false);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<ShowEggEvent>(evt =>
            {
                gameObject.SetActive(true);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void OnClick()
        {
            if(clickCount >= 3)
                return;
            
            if(this.GetModel<IGameModel>().OpenEggIndex != -1 && this.GetModel<IGameModel>().OpenEggIndex != eggId)
                return;
            
            if (this.GetModel<IGameModel>().OpenEggIndex == -1)
            {
                this.GetModel<IGameModel>().OpenEggIndex = eggId;
            }

            anim?.Kill();
            anim = spriteRenderer.transform.DOShakeScale(0.2f, 0.05f, 50, 180f);
            clickCount++;
            if (clickCount >= 3)
            {
                StartCoroutine(OpenEgg());
            }

            // PlayNextFrame();
            //
            // if (currentFrame >= eggSprites.Length)
            // {
            //     SpawnBird();
            // }
        }

        private IEnumerator OpenEgg()
        {
            effect2.SetActive(true);
            effect1.SetActive(false);
            
            // 停止effect1的粒子特效
            if (effect1 != null)
            {
                var particleSystem1 = effect1.GetComponent<ParticleSystem>();
                if (particleSystem1 != null)
                {
                    particleSystem1.Stop();
                }
            }
            
            while (currentFrame < eggSprites.Length)
            {
                PlayNextFrame();
                
                // 从第7帧开始播放速度变慢
                float frameDelay = currentFrame >= 7 ? 0.1f : 0.07f;
                yield return new WaitForSeconds(frameDelay);
            }
            
            SpawnBird();
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
            floatAnim?.Kill(); // 停止浮动动画
            
            // 停止所有粒子特效
            if (effect1 != null)
            {
                var particleSystem1 = effect1.GetComponent<ParticleSystem>();
                if (particleSystem1 != null)
                {
                    particleSystem1.Stop();
                }
            }
            
            if (effect2 != null)
            {
                var particleSystem2 = effect2.GetComponent<ParticleSystem>();
                if (particleSystem2 != null)
                {
                    particleSystem2.Stop();
                }
            }
        }

        private void SpawnBird()
        {
            if(isSetBird)
                this.SendCommand(new SpawnBirdCommand(BirdIndex, isSetBird));
            else
                this.SendCommand(new SpawnBirdCommand(EggItemIndex, isSetBird));
            this.GetModel<IGameModel>().OpenEggIndex = -1;
            // 销毁当前蛋对象
            Destroy(gameObject);
        }
    }
}